using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Core.Extensions;
using CompositeKey.SourceGeneration.Core.Tokenization;
using CompositeKey.SourceGeneration.Model;
using CompositeKey.SourceGeneration.Model.Key;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompositeKey.SourceGeneration;

public sealed record CompositeKeyAttributeValues(string TemplateString, char? PrimaryKeySeparator, bool InvariantCulture);

public sealed partial class SourceGenerator
{
    private const string CompositeKeyAttributeFullName = "CompositeKey.CompositeKeyAttribute";

    private sealed class Parser(KnownTypeSymbols knownTypeSymbols)
    {
        private const LanguageVersion MinimumSupportedLanguageVersion = LanguageVersion.CSharp11;

        private readonly KnownTypeSymbols _knownTypeSymbols = knownTypeSymbols;
        private readonly List<DiagnosticInfo> _diagnostics = [];

        private Location? _location;

        public ImmutableEquatableArray<DiagnosticInfo> Diagnostics => _diagnostics.ToImmutableEquatableArray();

        public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
        {
            Debug.Assert(_location != null);

            if (location is null || (location.SourceTree is not null && !_knownTypeSymbols.Compilation.ContainsSyntaxTree(location.SourceTree)))
                location = _location;

            _diagnostics.Add(DiagnosticInfo.Create(descriptor, location, messageArgs));
        }

        public GenerationSpec? Parse(
            TypeDeclarationSyntax typeDeclarationSyntax, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var targetTypeSymbol = semanticModel.GetDeclaredSymbol(typeDeclarationSyntax, cancellationToken);
            Debug.Assert(targetTypeSymbol != null);

            _location = targetTypeSymbol!.Locations.Length > 0 ? targetTypeSymbol.Locations[0] : null;
            Debug.Assert(_location is not null);

            var languageVersion = _knownTypeSymbols.Compilation is CSharpCompilation csc ? csc.LanguageVersion : (LanguageVersion?)null;
            if (languageVersion is null or < MinimumSupportedLanguageVersion)
            {
                ReportDiagnostic(DiagnosticDescriptors.UnsupportedLanguageVersion, _location, languageVersion?.ToDisplayString(), MinimumSupportedLanguageVersion.ToDisplayString());
                return null;
            }

            if (!targetTypeSymbol.IsRecord)
            {
                ReportDiagnostic(DiagnosticDescriptors.UnsupportedCompositeType, _location, targetTypeSymbol.Name);
                return null;
            }

            if (!TryGetTargetTypeDeclarations(typeDeclarationSyntax, semanticModel, out var targetTypeDeclarations, cancellationToken))
            {
                ReportDiagnostic(DiagnosticDescriptors.CompositeTypeMustBePartial, _location, targetTypeSymbol.Name);
                return null;
            }

            var compositeKeyAttributeValues = ParseCompositeKeyAttributeValues(targetTypeSymbol);
            Debug.Assert(compositeKeyAttributeValues is not null);

            if (TryGetObviousOrExplicitlyMarkedConstructor(targetTypeSymbol) is not { } constructor)
            {
                ReportDiagnostic(DiagnosticDescriptors.NoObviousDefaultConstructor, _location, targetTypeSymbol.Name);
                return null;
            }

            var constructorParameters = ParseConstructorParameters(constructor, out var constructionStrategy, out bool constructorSetsRequiredMembers);
            var properties = ParseProperties(targetTypeSymbol);
            var propertyInitializers = ParsePropertyInitializers(constructorParameters, properties.Select(p => p.Spec).ToList(), ref constructionStrategy, constructorSetsRequiredMembers);

            var keyParts = ParseTemplateStringIntoKeyParts(compositeKeyAttributeValues!, properties);
            if (keyParts is null)
                return null; // Should have already reported diagnostics by this point so just return null...

            var primaryDelimiterKeyPart = keyParts.OfType<PrimaryDelimiterKeyPart>().FirstOrDefault();

            KeySpec key;
            if (primaryDelimiterKeyPart is null)
            {
                if (!keyParts.OfType<ValueKeyPart>().Any())
                {
                    ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, compositeKeyAttributeValues!.TemplateString);
                    return null;
                }

                // If we reach this branch then it's just a Primary Key
                key = new PrimaryKeySpec(compositeKeyAttributeValues!.InvariantCulture, keyParts.ToImmutableEquatableArray());
            }
            else
            {
                // If we reach this branch then it's a "Composite" Primary Key
                var (partitionKeyParts, sortKeyParts) = SplitKeyPartsIntoPartitionAndSortKey(keyParts);

                if (!partitionKeyParts.OfType<ValueKeyPart>().Any())
                {
                    ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, compositeKeyAttributeValues!.TemplateString);
                    return null;
                }

                if (!sortKeyParts.OfType<ValueKeyPart>().Any())
                {
                    ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, compositeKeyAttributeValues!.TemplateString);
                    return null;
                }

                key = new CompositePrimaryKeySpec(
                    compositeKeyAttributeValues!.InvariantCulture,
                    keyParts.ToImmutableEquatableArray(),
                    partitionKeyParts.ToImmutableEquatableArray(),
                    primaryDelimiterKeyPart,
                    sortKeyParts.ToImmutableEquatableArray());
            }

            return new GenerationSpec(
                new TargetTypeSpec(
                    new TypeRef(targetTypeSymbol),
                    targetTypeSymbol.ContainingNamespace is { IsGlobalNamespace: false } ns ? ns.ToDisplayString() : null,
                    targetTypeDeclarations.ToImmutableEquatableArray(),
                    properties.Select(p => p.Spec).ToImmutableEquatableArray(),
                    constructorParameters.ToImmutableEquatableArray(),
                    propertyInitializers.ToImmutableEquatableArray(),
                    constructionStrategy),
                key);
        }

        private static (List<KeyPart> PartitionKeyParts, List<KeyPart> SortKeyParts) SplitKeyPartsIntoPartitionAndSortKey(List<KeyPart> keyParts)
        {
            int indexOfPrimaryKeyDelimiter = keyParts.FindIndex(kp => kp is PrimaryDelimiterKeyPart);
            Debug.Assert(indexOfPrimaryKeyDelimiter != -1);

            return (
                keyParts.Take(indexOfPrimaryKeyDelimiter).ToList(),
                keyParts.Skip(indexOfPrimaryKeyDelimiter + 1).ToList());
        }

        private List<KeyPart>? ParseTemplateStringIntoKeyParts(
            CompositeKeyAttributeValues compositeKeyAttributeValues,
            List<(PropertySpec Spec, ITypeSymbol TypeSymbol)> properties)
        {
            (string templateString, char? primaryKeySeparator, _) = compositeKeyAttributeValues;

            var templateStringTokenizer = new TemplateStringTokenizer(primaryKeySeparator);
            var templateTokens = templateStringTokenizer.Tokenize(templateString.AsSpan());
            if (templateTokens.Count == 0)
            {
                ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, templateString);
                return null;
            }

            if (primaryKeySeparator is not null && !templateTokens.Any(tt => tt is PrimaryDelimiterTemplateToken))
            {
                ReportDiagnostic(DiagnosticDescriptors.PrimaryKeySeparatorMissingFromTemplateString, _location, templateString, primaryKeySeparator);
                return null;
            }

            List<KeyPart> keyParts = [];
            foreach (var templateToken in templateTokens)
            {
                KeyPart? keyPart = templateToken switch
                {
                    PrimaryDelimiterTemplateToken pd => new PrimaryDelimiterKeyPart(pd.Value) { LengthRequired = 1 },
                    DelimiterTemplateToken d => new DelimiterKeyPart(d.Value) { LengthRequired = 1 },
                    PropertyTemplateToken p => ToPropertyKeyPart(p),
                    ConstantTemplateToken c => new ConstantKeyPart(c.Value) { LengthRequired = c.Value.Length },
                    _ => null
                };

                if (keyPart is null)
                {
                    ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, templateString);
                    return null;
                }

                keyParts.Add(keyPart);
            }

            return keyParts;

            PropertyKeyPart? ToPropertyKeyPart(PropertyTemplateToken templateToken)
            {
                var property = properties.FirstOrDefault(p => p.Spec.Name == templateToken.Name);
                if (property == default || property.Spec is not { HasGetter: true, HasSetter: true })
                {
                    ReportDiagnostic(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter, _location, templateToken.Name);
                    return null;
                }

                var (propertySpec, typeSymbol) = property;

                if (SymbolEqualityComparer.Default.Equals(typeSymbol, _knownTypeSymbols.GuidType))
                {
                    string format = templateToken.Format?.ToLowerInvariant() ?? "d";

                    int lengthRequired;
                    bool exactLengthRequirement = true;
                    switch (format)
                    {
                        case "d":
                            lengthRequired = 36;
                            break;

                        case "n":
                            lengthRequired = 32;
                            break;

                        case "b" or "p":
                            lengthRequired = 38;
                            break;

                        case "x":
                            lengthRequired = 32;
                            exactLengthRequirement = false;
                            break;

                        default:
                            ReportDiagnostic(DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat, _location, propertySpec.Name, templateToken.Format);
                            return null;
                    }

                    return new PropertyKeyPart(propertySpec, format, ParseType.Guid, FormatType.Guid)
                    {
                        LengthRequired = lengthRequired,
                        ExactLengthRequirement = exactLengthRequirement
                    };
                }

                if (SymbolEqualityComparer.Default.Equals(typeSymbol, _knownTypeSymbols.StringType))
                {
                    return new PropertyKeyPart(propertySpec, null, ParseType.String, FormatType.String)
                    {
                        LengthRequired = 1,
                        ExactLengthRequirement = false
                    };
                }

                if (typeSymbol.TypeKind == TypeKind.Enum)
                {
                    string format = templateToken.Format?.ToLowerInvariant() ?? "g";
                    return new PropertyKeyPart(propertySpec, format, ParseType.Enum, FormatType.Enum)
                    {
                        LengthRequired = 1,
                        ExactLengthRequirement = false
                    };
                }

                var interfaces = typeSymbol.AllInterfaces;
                bool isSpanParsable = interfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::System.ISpanParsable"));
                bool isSpanFormattable = interfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Equals("global::System.ISpanFormattable"));

                if (!isSpanParsable || !isSpanFormattable)
                    throw new NotSupportedException($"Unsupported property of type '{propertySpec.Type.FullyQualifiedName}'");

                return new PropertyKeyPart(propertySpec, templateToken.Format, ParseType.SpanParsable, FormatType.SpanFormattable)
                {
                    LengthRequired = 1,
                    ExactLengthRequirement = false
                };
            }
        }

        private static List<PropertyInitializerSpec>? ParsePropertyInitializers(
            ConstructorParameterSpec[] constructorParameters,
            List<PropertySpec> properties,
            ref ConstructionStrategy constructionStrategy,
            bool constructorSetsRequiredParameters)
        {
            if (properties is [])
                return [];

            HashSet<string>? propertyInitializerNames = null;
            List<PropertyInitializerSpec>? propertyInitializers = null;
            int parameterCount = constructorParameters.Length;

            foreach (var property in properties)
            {
                if (!property.HasSetter)
                    continue;

                if ((property.IsRequired || constructorSetsRequiredParameters) && !property.IsInitOnlySetter)
                    continue;

                if (!(propertyInitializerNames ??= []).Add(property.Name))
                    continue;

                var matchingConstructorParameter = constructorParameters.FirstOrDefault(cp => cp.Name.Equals(property.Name, StringComparison.OrdinalIgnoreCase));
                if (!property.IsRequired && matchingConstructorParameter is not null)
                    continue;

                constructionStrategy = ConstructionStrategy.ParameterizedConstructor;
                (propertyInitializers ??= []).Add(new PropertyInitializerSpec(
                    property.Type,
                    property.Name,
                    property.CamelCaseName,
                    matchingConstructorParameter?.ParameterIndex ?? parameterCount++,
                    matchingConstructorParameter is not null));
            }

            return propertyInitializers;
        }

        private static List<(PropertySpec Spec, ITypeSymbol TypeSymbol)> ParseProperties(INamedTypeSymbol typeSymbol)
        {
            List<(PropertySpec Spec, ITypeSymbol TypeSymbol)> properties = [];
            foreach (var propertySymbol in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (propertySymbol is { IsImplicitlyDeclared: true, Name: "EqualityContract" })
                    continue;

                if (propertySymbol.IsStatic || propertySymbol.Parameters.Length > 0)
                    continue;

                var propertySpec = new PropertySpec(
                    new TypeRef(propertySymbol.Type),
                    propertySymbol.Name,
                    propertySymbol.Name.FirstToLowerInvariant(),
                    propertySymbol.IsRequired,
                    propertySymbol.GetMethod is not null,
                    propertySymbol.SetMethod is not null,
                    propertySymbol.SetMethod is { IsInitOnly: true });

                properties.Add((propertySpec, propertySymbol.Type));
            }

            return properties;
        }

        private ConstructorParameterSpec[] ParseConstructorParameters(
            IMethodSymbol constructor, out ConstructionStrategy constructionStrategy, out bool constructorSetsRequiredMembers)
        {
            constructorSetsRequiredMembers = constructor.GetAttributes().Any(a =>
                SymbolEqualityComparer.Default.Equals(a.AttributeClass, _knownTypeSymbols.SetsRequiredMembersAttributeType));

            int parameterCount = constructor.Parameters.Length;
            if (parameterCount == 0)
            {
                constructionStrategy = ConstructionStrategy.ParameterlessConstructor;
                return [];
            }

            constructionStrategy = ConstructionStrategy.ParameterizedConstructor;

            var constructorParameters = new ConstructorParameterSpec[parameterCount];
            for (int i = 0; i < parameterCount; i++)
            {
                var parameterSymbol = constructor.Parameters[i];
                constructorParameters[i] = new ConstructorParameterSpec(new TypeRef(parameterSymbol.Type), parameterSymbol.Name, parameterSymbol.Name.FirstToLowerInvariant(), i);
            }

            return constructorParameters;
        }

        private IMethodSymbol? TryGetObviousOrExplicitlyMarkedConstructor(INamedTypeSymbol typeSymbol)
        {
            var publicConstructors = typeSymbol.Constructors
                .Where(c => !c.IsStatic && !(c.IsImplicitlyDeclared && typeSymbol.IsValueType && c.Parameters.Length == 0))
                .Where(c => !(c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, typeSymbol)))
                .ToArray();

            var lonePublicConstructor = publicConstructors.Length == 1 ? publicConstructors[0] : null;
            IMethodSymbol? constructorWithAttribute = null, publicParameterlessConstructor = null;

            foreach (var constructor in publicConstructors)
            {
                if (constructor.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, _knownTypeSymbols.CompositeKeyConstructorAttributeType)))
                {
                    if (constructorWithAttribute is not null)
                        return null; // Somehow we found a duplicate so let's just return null so the diagnostic is emitted

                    constructorWithAttribute = constructor;
                }
                else if (constructor.Parameters.Length == 0)
                {
                    publicParameterlessConstructor = constructor;
                }
            }

            return constructorWithAttribute ?? publicParameterlessConstructor ?? lonePublicConstructor;
        }

        private CompositeKeyAttributeValues? ParseCompositeKeyAttributeValues(INamedTypeSymbol targetTypeSymbol)
        {
            Debug.Assert(_knownTypeSymbols.CompositeKeyAttributeType is not null);

            CompositeKeyAttributeValues? attributeValues = null;

            foreach (var attributeData in targetTypeSymbol.GetAttributes())
            {
                Debug.Assert(attributeValues is null, $"There should only ever be one {nameof(CompositeKeyAttribute)} per type");

                if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, _knownTypeSymbols.CompositeKeyAttributeType))
                    attributeValues = TryExtractAttributeValues(attributeData);
            }

            return attributeValues;

            static CompositeKeyAttributeValues? TryExtractAttributeValues(AttributeData attributeData)
            {
                Debug.Assert(attributeData.ConstructorArguments.Length is 1);

                string? templateString = (string?)attributeData.ConstructorArguments[0].Value;
                if (templateString is null)
                    return null;

                char? primaryKeySeparator = null;
                bool? invariantCulture = null;
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    (string? key, var value) = (namedArgument.Key, namedArgument.Value);

                    switch (key)
                    {
                        case nameof(CompositeKeyAttribute.PrimaryKeySeparator):
                            primaryKeySeparator = (char?)value.Value;
                            break;

                        case nameof(CompositeKeyAttribute.InvariantCulture):
                            invariantCulture = (bool?)value.Value;
                            break;

                        default:
                            throw new InvalidOperationException();
                    }
                }

                return new CompositeKeyAttributeValues(templateString, primaryKeySeparator, invariantCulture ?? true);
            }
        }

        private static bool TryGetTargetTypeDeclarations(
            TypeDeclarationSyntax typeDeclarationSyntax,
            SemanticModel semanticModel,
            [NotNullWhen(true)] out List<string>? targetTypeDeclarations,
            CancellationToken cancellationToken)
        {
            targetTypeDeclarations = null;

            for (var current = typeDeclarationSyntax; current != null; current = current.Parent as TypeDeclarationSyntax)
            {
                StringBuilder stringBuilder = new();

                bool isPartialType = false;
                foreach (var modifier in current.Modifiers)
                {
                    stringBuilder.Append(modifier.Text);
                    stringBuilder.Append(' ');

                    isPartialType |= modifier.IsKind(SyntaxKind.PartialKeyword);
                }

                if (!isPartialType)
                    return false;

                stringBuilder.Append(GetTypeKindKeyword(current));
                stringBuilder.Append(' ');

                var typeSymbol = semanticModel.GetDeclaredSymbol(current, cancellationToken);
                Debug.Assert(typeSymbol is not null);

                stringBuilder.Append(typeSymbol!.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

                (targetTypeDeclarations ??= []).Add(stringBuilder.ToString());
            }

            return targetTypeDeclarations?.Count > 0;

            static string GetTypeKindKeyword(TypeDeclarationSyntax typeDeclarationSyntax) =>
                typeDeclarationSyntax.Kind() switch
                {
                    SyntaxKind.ClassDeclaration => "class",
                    SyntaxKind.InterfaceDeclaration => "interface",
                    SyntaxKind.StructDeclaration => "struct",
                    SyntaxKind.RecordDeclaration => "record",
                    SyntaxKind.RecordStructDeclaration => "record struct",
                    SyntaxKind.EnumDeclaration => "enum",
                    SyntaxKind.DelegateDeclaration => "delegate",
                    _ => throw new ArgumentOutOfRangeException(nameof(typeDeclarationSyntax))
                };
        }
    }
}
