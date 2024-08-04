using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using CompositeId.SourceGeneration.Core;
using CompositeId.SourceGeneration.Core.Extensions;
using CompositeId.SourceGeneration.Core.Tokenization;
using CompositeId.SourceGeneration.Model;
using CompositeId.SourceGeneration.Model.Key;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompositeId.SourceGeneration;

public sealed record CompositeIdAttributeValues(string TemplateString, char? PrimaryKeySeparator);

public sealed partial class CompositeSourceGenerator
{
    private const string CompositeIdAttributeFullName = "CompositeId.CompositeIdAttribute";

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

            var compositeIdAttributeValues = ParseCompositeIdAttributeValues(targetTypeSymbol);
            Debug.Assert(compositeIdAttributeValues is not null);

            if (TryGetAccessibleConstructor(targetTypeSymbol) is not { } constructor)
            {
                // TODO: Report a diagnostic
                return null;
            }

            var constructorParameters = ParseConstructorParameters(constructor, out var constructionStrategy, out bool constructorSetsRequiredMembers);
            var properties = ParseProperties(targetTypeSymbol);
            var propertyInitializers = ParsePropertyInitializers(constructorParameters, properties, ref constructionStrategy, constructorSetsRequiredMembers);

            var keyParts = ParseTemplateStringIntoKeyParts(compositeIdAttributeValues!, properties);
            if (keyParts is null)
                return null; // Should have already reported diagnostics by this point...

            var primaryDelimiterKeyPart = keyParts.OfType<PrimaryDelimiterKeyPart>().FirstOrDefault();

            KeySpec key;
            if (primaryDelimiterKeyPart is null)
            {
                if (!keyParts.OfType<ValueKeyPart>().Any())
                    return null; // TODO: Report a diagnostic

                // If we reach this branch then it's just a Primary Key
                key = new PrimaryKeySpec(keyParts.ToImmutableEquatableArray());
            }
            else
            {
                // If we reach this branch then it's a "Composite" Primary Key
                var (partitionKeyParts, sortKeyParts) = SplitKeyPartsIntoPartitionAndSortKey(keyParts);

                if (!partitionKeyParts.OfType<ValueKeyPart>().Any())
                    return null; // TODO: Report a diagnostic

                if (!sortKeyParts.OfType<ValueKeyPart>().Any())
                    return null; // TODO: Report a diagnostic

                key = new CompositePrimaryKeySpec(
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
                    properties.ToImmutableEquatableArray(),
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
            CompositeIdAttributeValues compositeIdAttributeValues,
            List<PropertySpec> properties)
        {
            var templateStringTokenizer = new TemplateStringTokenizer(compositeIdAttributeValues.PrimaryKeySeparator);
            var templateTokens = templateStringTokenizer.Tokenize(compositeIdAttributeValues.TemplateString.AsSpan());
            if (templateTokens.Count == 0)
                return null; // TODO: Report a diagnostic

            if (compositeIdAttributeValues.PrimaryKeySeparator is not null && !templateTokens.Any(tt => tt is PrimaryDelimiterTemplateToken))
                return null; // TODO: Report a diagnostic

            List<KeyPart> keyParts = [];
            foreach (var templateToken in templateTokens)
            {
                KeyPart? keyPart = templateToken switch
                {
                    PrimaryDelimiterTemplateToken pd => new PrimaryDelimiterKeyPart(pd.Value) { LengthRequired = 1 },
                    DelimiterTemplateToken d => new DelimiterKeyPart(d.Value) { LengthRequired = 1 },
                    PropertyTemplateToken p => ToPropertyKeyPart(p),
                    ConstantTemplateToken c => new ConstantKeyPart(c.Value) { LengthRequired = c.Value.Length },
                    _ => null // TODO: Report a diagnostic
                };

                if (keyPart is null)
                    return null;

                keyParts.Add(keyPart);
            }

            return keyParts;

            PropertyKeyPart? ToPropertyKeyPart(PropertyTemplateToken templateToken)
            {
                var property = properties.FirstOrDefault(p => p.Name == templateToken.Name);
                if (property is not { HasGetter: true, HasSetter: true })
                    return null; // TODO: Report a diagnostic

                if (SymbolEqualityComparer.Default.Equals(property.Type.TypeSymbol, _knownTypeSymbols.GuidType))
                {
                    string? format = templateToken.Format?.ToLowerInvariant() ?? "d";
                    (int lengthRequired, bool exactLengthRequirement) = format switch
                    {
                        "d" => (36, true),
                        "n" => (32, true),
                        "b" or "p" => (28, true),
                        "x" => (32, false),
                        _ => throw new InvalidOperationException($"Invalid Guid Format of '{format}' specified.") // TODO: Report a diagnostic
                    };

                    return new PropertyKeyPart(property, format, ParseType.Guid, FormatType.Guid)
                    {
                        LengthRequired = lengthRequired,
                        ExactLengthRequirement = exactLengthRequirement
                    };
                }

                if (SymbolEqualityComparer.Default.Equals(property.Type.TypeSymbol, _knownTypeSymbols.StringType))
                {
                    return new PropertyKeyPart(property, null, ParseType.String, FormatType.String)
                    {
                        LengthRequired = 1,
                        ExactLengthRequirement = false
                    };
                }

                if (property.Type.TypeSymbol.TypeKind == TypeKind.Enum)
                {
                    string format = templateToken.Format?.ToLowerInvariant() ?? "g";
                    return new PropertyKeyPart(property, format, ParseType.Enum, FormatType.Enum)
                    {
                        LengthRequired = 1,
                        ExactLengthRequirement = false
                    };
                }

                var interfaces = property.Type.TypeSymbol.AllInterfaces;
                bool isSpanParsable = interfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::System.ISpanParsable"));
                bool isSpanFormattable = interfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Equals("global::System.ISpanFormattable"));

                if (!isSpanParsable || !isSpanFormattable)
                    throw new NotSupportedException($"Unsupported property of type '{property.Type.FullyQualifiedName}'");

                return new PropertyKeyPart(property, templateToken.Format, ParseType.SpanParsable, FormatType.SpanFormattable)
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

        private static List<PropertySpec> ParseProperties(INamedTypeSymbol typeSymbol)
        {
            List<PropertySpec> properties = [];
            foreach (var propertySymbol in typeSymbol.GetMembers().OfType<IPropertySymbol>())
            {
                if (propertySymbol is { IsImplicitlyDeclared: true, Name: "EqualityContract" })
                    continue;

                if (propertySymbol.IsStatic || propertySymbol.Parameters.Length > 0)
                    continue;

                properties.Add(new PropertySpec(
                    new TypeRef(propertySymbol.Type),
                    propertySymbol.Name,
                    propertySymbol.Name.FirstToLowerInvariant(),
                    propertySymbol.IsRequired,
                    propertySymbol.GetMethod is not null,
                    propertySymbol.SetMethod is not null,
                    propertySymbol.SetMethod is { IsInitOnly: true }));
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

        private static IMethodSymbol? TryGetAccessibleConstructor(INamedTypeSymbol typeSymbol)
        {
            var publicConstructors = typeSymbol.Constructors
                .Where(c => !c.IsStatic && !(c.IsImplicitlyDeclared && typeSymbol.IsValueType && c.Parameters.Length == 0))
                .Where(c => !(c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, typeSymbol)))
                .ToArray();

            return publicConstructors.Length == 1
                ? publicConstructors[0]
                : publicConstructors.FirstOrDefault(c => c.Parameters.Length == 0);
        }

        private CompositeIdAttributeValues? ParseCompositeIdAttributeValues(INamedTypeSymbol targetTypeSymbol)
        {
            Debug.Assert(_knownTypeSymbols.CompositeIdAttributeType is not null);

            CompositeIdAttributeValues? attributeValues = null;

            foreach (var attributeData in targetTypeSymbol.GetAttributes())
            {
                Debug.Assert(attributeValues is null, $"There should only ever be one {nameof(CompositeIdAttribute)} per type");

                if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, _knownTypeSymbols.CompositeIdAttributeType))
                    attributeValues = TryExtractAttributeValues(attributeData);
            }

            return attributeValues;

            static CompositeIdAttributeValues? TryExtractAttributeValues(AttributeData attributeData)
            {
                Debug.Assert(attributeData.ConstructorArguments.Length is 1);

                string? templateString = (string?)attributeData.ConstructorArguments[0].Value;
                if (templateString is null)
                    return null;

                char? primaryKeySeparator = null;
                foreach (var namedArgument in attributeData.NamedArguments)
                {
                    (string? key, var value) = (namedArgument.Key, namedArgument.Value);

                    switch (key)
                    {
                        case nameof(CompositeIdAttribute.PrimaryKeySeparator):
                            primaryKeySeparator = (char?)value.Value;
                            break;

                        default:
                            throw new InvalidOperationException();
                    }
                }

                return new CompositeIdAttributeValues(templateString, primaryKeySeparator);
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

                stringBuilder.Append(GetTypeKindKeyword(typeDeclarationSyntax));
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
