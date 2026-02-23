using System.Diagnostics;
using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.Tokenization;
using CompositeKey.Analyzers.Common.Validation;
using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Core.Extensions;
using CompositeKey.SourceGeneration.Model;
using CompositeKey.SourceGeneration.Model.Key;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompositeKey.SourceGeneration;

public sealed record CompositeKeyAttributeValues(string TemplateString, char? PrimaryKeySeparator, bool InvariantCulture);

internal sealed class Parser(KnownTypeSymbols knownTypeSymbols)
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
            // If we reach this branch then it's just a Primary Key
            key = new PrimaryKeySpec(compositeKeyAttributeValues!.InvariantCulture, keyParts.ToImmutableEquatableArray());
        }
        else
        {
            // If we reach this branch then it's a "Composite" Primary Key
            var (partitionKeyParts, sortKeyParts) = SplitKeyPartsIntoPartitionAndSortKey(keyParts);
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

        var (tokenizationSuccessful, templateTokens) = TemplateValidation.TokenizeTemplateString(templateString, primaryKeySeparator);
        if (!tokenizationSuccessful)
        {
            ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, templateString);
            return null;
        }

        var separatorValidation = TemplateValidation.ValidatePrimaryKeySeparator(templateString, primaryKeySeparator, templateTokens);
        if (!separatorValidation.IsSuccess)
        {
            ReportDiagnostic(separatorValidation.Descriptor, _location, separatorValidation.MessageArgs);
            return null;
        }

        if (!TemplateValidation.HasValidTemplateStructure(templateTokens))
        {
            ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, templateString);
            return null;
        }

        if (primaryKeySeparator.HasValue && !TemplateValidation.ValidatePartitionAndSortKeyStructure(templateTokens, out _))
        {
            ReportDiagnostic(DiagnosticDescriptors.EmptyOrInvalidTemplateString, _location, templateString);
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
                RepeatingPropertyTemplateToken rp => ToRepeatingPropertyKeyPart(rp),
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

        // Validate: repeating type used without repeating syntax -> COMPOSITE0010
        foreach (var keyPart in keyParts)
        {
            if (keyPart is PropertyKeyPart pkp && pkp.Property.CollectionType != CollectionType.None)
            {
                ReportDiagnostic(DiagnosticDescriptors.RepeatingTypeMustUseRepeatingSyntax, _location, pkp.Property.Name);
                return null;
            }
        }

        // Validate: repeating property must be the last value part in its key section -> COMPOSITE0011
        var valueParts = keyParts.Where(kp => kp is ValueKeyPart).ToList();
        if (valueParts.Count > 0 && valueParts[^1] is not RepeatingPropertyKeyPart)
        {
            // Only report if there's a repeating part that isn't last
            if (valueParts.Any(kp => kp is RepeatingPropertyKeyPart))
            {
                var repeatingPart = valueParts.First(kp => kp is RepeatingPropertyKeyPart) as RepeatingPropertyKeyPart;
                ReportDiagnostic(DiagnosticDescriptors.RepeatingPropertyMustBeLastPart, _location, repeatingPart!.Property.Name);
                return null;
            }
        }

        // For composite keys, also validate repeating position within each section
        if (keyParts.Any(kp => kp is PrimaryDelimiterKeyPart))
        {
            int delimiterIndex = keyParts.FindIndex(kp => kp is PrimaryDelimiterKeyPart);

            var partitionValueParts = keyParts.Take(delimiterIndex).Where(kp => kp is ValueKeyPart).ToList();
            if (partitionValueParts.Count > 0 && partitionValueParts.Any(kp => kp is RepeatingPropertyKeyPart) && partitionValueParts[^1] is not RepeatingPropertyKeyPart)
            {
                var repeatingPart = partitionValueParts.First(kp => kp is RepeatingPropertyKeyPart) as RepeatingPropertyKeyPart;
                ReportDiagnostic(DiagnosticDescriptors.RepeatingPropertyMustBeLastPart, _location, repeatingPart!.Property.Name);
                return null;
            }

            var sortValueParts = keyParts.Skip(delimiterIndex + 1).Where(kp => kp is ValueKeyPart).ToList();
            if (sortValueParts.Count > 0 && sortValueParts.Any(kp => kp is RepeatingPropertyKeyPart) && sortValueParts[^1] is not RepeatingPropertyKeyPart)
            {
                var repeatingPart = sortValueParts.First(kp => kp is RepeatingPropertyKeyPart) as RepeatingPropertyKeyPart;
                ReportDiagnostic(DiagnosticDescriptors.RepeatingPropertyMustBeLastPart, _location, repeatingPart!.Property.Name);
                return null;
            }
        }

        return keyParts;

        PropertyKeyPart? ToPropertyKeyPart(PropertyTemplateToken templateToken)
        {
            var availableProperties = properties
                .Select(p => new TemplateValidation.PropertyInfo(p.Spec.Name, p.Spec.HasGetter, p.Spec.HasSetter))
                .ToList();

            var propertyValidation = TemplateValidation.ValidatePropertyReferences([templateToken], availableProperties);
            if (!propertyValidation.IsSuccess)
            {
                ReportDiagnostic(propertyValidation.Descriptor, _location, propertyValidation.MessageArgs);
                return null;
            }

            var property = properties.First(p => p.Spec.Name == templateToken.Name);

            propertiesUsedInKey.Add(property);
            var (propertySpec, typeSymbol) = property;

            // Repeating type properties must use repeating syntax
            if (propertySpec.CollectionType != CollectionType.None)
            {
                ReportDiagnostic(DiagnosticDescriptors.RepeatingTypeMustUseRepeatingSyntax, _location, propertySpec.Name);
                return null;
            }

            var interfaces = typeSymbol.AllInterfaces;
            bool isSpanParsable = interfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::System.ISpanParsable"));
            bool isSpanFormattable = interfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Equals("global::System.ISpanFormattable"));

            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: propertySpec.Type.FullyQualifiedName,
                IsGuid: SymbolEqualityComparer.Default.Equals(typeSymbol, _knownTypeSymbols.GuidType),
                IsString: SymbolEqualityComparer.Default.Equals(typeSymbol, _knownTypeSymbols.StringType),
                IsEnum: typeSymbol.TypeKind == TypeKind.Enum,
                IsSpanParsable: isSpanParsable,
                IsSpanFormattable: isSpanFormattable);

            var formatValidation = PropertyValidation.ValidatePropertyFormat(
                propertySpec.Name,
                typeInfo,
                templateToken.Format);

            if (!formatValidation.IsSuccess)
            {
                ReportDiagnostic(formatValidation.Descriptor, _location, formatValidation.MessageArgs);
                return null;
            }

            var typeCompatibility = PropertyValidation.ValidatePropertyTypeCompatibility(
                propertySpec.Name,
                typeInfo);

            if (!typeCompatibility.IsSuccess)
            {
                throw new NotSupportedException($"Unsupported property of type '{propertySpec.Type.FullyQualifiedName}'");
            }

            var lengthInfo = PropertyValidation.GetFormattedLength(typeInfo, templateToken.Format);
            int lengthRequired = lengthInfo?.length ?? 1;
            bool exactLengthRequirement = lengthInfo?.isExact ?? false;

            ParseType parseType;
            FormatType formatType;
            string? format = templateToken.Format;

            if (typeInfo.IsGuid)
            {
                parseType = ParseType.Guid;
                formatType = FormatType.Guid;
                format = templateToken.Format?.ToLowerInvariant() ?? "d";
            }
            else if (typeInfo.IsString)
            {
                parseType = ParseType.String;
                formatType = FormatType.String;
                format = null;
            }
            else if (typeInfo.IsEnum)
            {
                parseType = ParseType.Enum;
                formatType = FormatType.Enum;
                format = templateToken.Format?.ToLowerInvariant() ?? "g";
            }
            else
            {
                parseType = ParseType.SpanParsable;
                formatType = FormatType.SpanFormattable;
            }

            return new PropertyKeyPart(propertySpec, format, parseType, formatType)
            {
                LengthRequired = lengthRequired,
                ExactLengthRequirement = exactLengthRequirement
            };
        }

        RepeatingPropertyKeyPart? ToRepeatingPropertyKeyPart(RepeatingPropertyTemplateToken templateToken)
        {
            var availableProperties = properties
                .Select(p => new TemplateValidation.PropertyInfo(p.Spec.Name, p.Spec.HasGetter, p.Spec.HasSetter))
                .ToList();

            var propertyValidation = TemplateValidation.ValidatePropertyReferences([templateToken], availableProperties);
            if (!propertyValidation.IsSuccess)
            {
                ReportDiagnostic(propertyValidation.Descriptor, _location, propertyValidation.MessageArgs);
                return null;
            }

            var property = properties.First(p => p.Spec.Name == templateToken.Name);
            var (propertySpec, typeSymbol) = property;

            // Validate that the property is a collection type
            if (propertySpec.CollectionType == CollectionType.None)
            {
                ReportDiagnostic(DiagnosticDescriptors.RepeatingPropertyMustUseCollectionType, _location, propertySpec.Name);
                return null;
            }

            // Extract inner type from the collection
            var namedTypeSymbol = (INamedTypeSymbol)typeSymbol;
            var innerTypeSymbol = namedTypeSymbol.TypeArguments[0];

            var innerInterfaces = innerTypeSymbol.AllInterfaces;
            bool isSpanParsable = innerInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).StartsWith("global::System.ISpanParsable"));
            bool isSpanFormattable = innerInterfaces.Any(i => i.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Equals("global::System.ISpanFormattable"));

            var innerTypeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: innerTypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                IsGuid: SymbolEqualityComparer.Default.Equals(innerTypeSymbol, _knownTypeSymbols.GuidType),
                IsString: SymbolEqualityComparer.Default.Equals(innerTypeSymbol, _knownTypeSymbols.StringType),
                IsEnum: innerTypeSymbol.TypeKind == TypeKind.Enum,
                IsSpanParsable: isSpanParsable,
                IsSpanFormattable: isSpanFormattable);

            var formatValidation = PropertyValidation.ValidatePropertyFormat(
                propertySpec.Name,
                innerTypeInfo,
                templateToken.Format);

            if (!formatValidation.IsSuccess)
            {
                ReportDiagnostic(formatValidation.Descriptor, _location, formatValidation.MessageArgs);
                return null;
            }

            var typeCompatibility = PropertyValidation.ValidatePropertyTypeCompatibility(
                propertySpec.Name,
                innerTypeInfo);

            if (!typeCompatibility.IsSuccess)
            {
                throw new NotSupportedException($"Unsupported inner type '{innerTypeInfo.TypeName}' for repeating property '{propertySpec.Name}'");
            }

            propertiesUsedInKey.Add(property);

            ParseType innerParseType;
            FormatType innerFormatType;
            string? format = templateToken.Format;

            if (innerTypeInfo.IsGuid)
            {
                innerParseType = ParseType.Guid;
                innerFormatType = FormatType.Guid;
                format = templateToken.Format?.ToLowerInvariant() ?? "d";
            }
            else if (innerTypeInfo.IsString)
            {
                innerParseType = ParseType.String;
                innerFormatType = FormatType.String;
                format = null;
            }
            else if (innerTypeInfo.IsEnum)
            {
                innerParseType = ParseType.Enum;
                innerFormatType = FormatType.Enum;
                format = templateToken.Format?.ToLowerInvariant() ?? "g";
            }
            else
            {
                innerParseType = ParseType.SpanParsable;
                innerFormatType = FormatType.SpanFormattable;
            }

            return new RepeatingPropertyKeyPart(propertySpec, templateToken.Separator, format, innerParseType, innerFormatType, new TypeRef(innerTypeSymbol))
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

    private List<(PropertySpec Spec, ITypeSymbol TypeSymbol)> ParseProperties(INamedTypeSymbol typeSymbol)
    {
        List<(PropertySpec Spec, ITypeSymbol TypeSymbol)> properties = [];
        foreach (var propertySymbol in typeSymbol.GetMembers().OfType<IPropertySymbol>())
        {
            if (propertySymbol is { IsImplicitlyDeclared: true, Name: "EqualityContract" })
                continue;

            if (propertySymbol.IsStatic || propertySymbol.Parameters.Length > 0)
                continue;

            // Detect collection types
            var collectionType = CollectionType.None;
            ITypeSymbol effectiveTypeSymbol = propertySymbol.Type;

            if (propertySymbol.Type is INamedTypeSymbol namedType)
            {
                var originalDefinition = namedType.OriginalDefinition;
                if (SymbolEqualityComparer.Default.Equals(originalDefinition, _knownTypeSymbols.ListType))
                    collectionType = CollectionType.List;
                else if (SymbolEqualityComparer.Default.Equals(originalDefinition, _knownTypeSymbols.ReadOnlyListType))
                    collectionType = CollectionType.IReadOnlyList;
                else if (SymbolEqualityComparer.Default.Equals(originalDefinition, _knownTypeSymbols.ImmutableArrayType))
                    collectionType = CollectionType.ImmutableArray;
            }

            // For collection types, extract inner type for EnumSpec
            EnumSpec? enumSpec = null;
            if (collectionType != CollectionType.None)
            {
                var innerType = ((INamedTypeSymbol)propertySymbol.Type).TypeArguments[0];
                if (innerType is INamedTypeSymbol { TypeKind: TypeKind.Enum } innerEnumType)
                    enumSpec = ExtractEnumDefinition(innerEnumType);
            }
            else
            {
                if (propertySymbol.Type is INamedTypeSymbol { TypeKind: TypeKind.Enum } enumType)
                    enumSpec = ExtractEnumDefinition(enumType);
            }

            var propertySpec = new PropertySpec(
                new TypeRef(propertySymbol.Type),
                propertySymbol.Name,
                propertySymbol.Name.FirstToLowerInvariant(),
                propertySymbol.IsRequired,
                propertySymbol.GetMethod is not null,
                propertySymbol.SetMethod is not null,
                propertySymbol.SetMethod is { IsInitOnly: true },
                enumSpec,
                collectionType);

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
