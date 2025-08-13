using System.Diagnostics;
using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.Validation;
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

            // Validate type structure using comprehensive shared validation
            var validationResult = TypeValidation.ValidateTypeForCompositeKey(
                targetTypeSymbol,
                typeDeclarationSyntax,
                semanticModel,
                _knownTypeSymbols.CompositeKeyConstructorAttributeType,
                cancellationToken);

            if (!validationResult.IsSuccess)
            {
                ReportDiagnostic(validationResult.Descriptor, _location, validationResult.MessageArgs);
                return null;
            }

            // Use validated data from the validation result (guaranteed non-null due to MemberNotNullWhen on IsSuccess)
            var targetTypeDeclarations = validationResult.TargetTypeDeclarations;
            var constructor = validationResult.Constructor;

            var compositeKeyAttributeValues = ParseCompositeKeyAttributeValues(targetTypeSymbol);
            Debug.Assert(compositeKeyAttributeValues is not null);

            var constructorParameters = ParseConstructorParameters(constructor, out var constructionStrategy, out bool constructorSetsRequiredMembers);
            var properties = ParseProperties(targetTypeSymbol);
            var propertyInitializers = ParsePropertyInitializers(constructorParameters, properties.Select(p => p.Spec).ToList(), ref constructionStrategy, constructorSetsRequiredMembers);

            var propertiesUsedInKey = new List<(PropertySpec Spec, ITypeSymbol TypeSymbol)>();
            var keyParts = ParseTemplateStringIntoKeyParts(compositeKeyAttributeValues!, properties, propertiesUsedInKey);
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
                    propertiesUsedInKey.Select(p => p.Spec).ToImmutableEquatableArray(),
                    constructorParameters.ToImmutableEquatableArray(),
                    (propertyInitializers?.Where(pi => propertiesUsedInKey.Any(p => p.Spec.Name == pi.Name))).ToImmutableEquatableArray(),
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
            List<(PropertySpec Spec, ITypeSymbol TypeSymbol)> properties,
            List<(PropertySpec Spec, ITypeSymbol TypeSymbol)> propertiesUsedInKey)
        {
            (string templateString, char? primaryKeySeparator, _) = compositeKeyAttributeValues;

            var templateStringTokenizer = new TemplateStringTokenizer(primaryKeySeparator);
            var tokenizeResult = templateStringTokenizer.Tokenize(templateString.AsSpan());
            if (!tokenizeResult.Success)
            {
                ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, templateString);
                return null;
            }
            var templateTokens = tokenizeResult.Tokens;

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

                propertiesUsedInKey.Add(property);
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

                EnumSpec? enumSpec = null;
                if (propertySymbol.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
                    enumSpec = ExtractEnumDefinition(enumType);

                var propertySpec = new PropertySpec(
                    new TypeRef(propertySymbol.Type),
                    propertySymbol.Name,
                    propertySymbol.Name.FirstToLowerInvariant(),
                    propertySymbol.IsRequired,
                    propertySymbol.GetMethod is not null,
                    propertySymbol.SetMethod is not null,
                    propertySymbol.SetMethod is { IsInitOnly: true },
                    enumSpec);

                properties.Add((propertySpec, propertySymbol.Type));
            }

            return properties;

            EnumSpec ExtractEnumDefinition(INamedTypeSymbol enumSymbol)
            {
                string name = enumSymbol.Name;
                string fullyQualifiedName = enumSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                string underlyingType = enumSymbol.EnumUnderlyingType?.ToString() ?? "int";

                List<EnumSpec.Member> members = [];
                foreach (var enumMember in enumSymbol.GetMembers())
                {
                    if (enumMember is not IFieldSymbol { ConstantValue: not null } fieldSymbol)
                        continue;

                    members.Add(new EnumSpec.Member(fieldSymbol.Name, fieldSymbol.ConstantValue));
                }

                bool isSequentialFromZero = true;
                for (int i = 0; i < members.Count; i++)
                {
                    if (Convert.ToUInt64(members[i].Value) == (uint)i)
                        continue;

                    isSequentialFromZero = false;
                    break;
                }

                return new EnumSpec(
                    name,
                    fullyQualifiedName,
                    underlyingType,
                    members.ToImmutableEquatableArray(),
                    isSequentialFromZero);
            }
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

    }
}
