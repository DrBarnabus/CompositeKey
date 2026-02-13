using System.Collections.Immutable;
using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.Tokenization;
using CompositeKey.Analyzers.Common.Validation;
using CompositeKey.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CompositeKey.Analyzers.Analyzers;

/// <summary>
/// Analyzer that validates property accessibility and format specifiers for CompositeKey-annotated types.
/// Reports diagnostics for properties that lack accessible getters/setters or have invalid format specifiers.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PropertyAnalyzer : CompositeKeyAnalyzerBase
{
    /// <summary>
    /// Gets the supported diagnostics for property validation.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter,
        DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat,
        DiagnosticDescriptors.RepeatingPropertyMustUseCollectionType,
        DiagnosticDescriptors.CollectionPropertyMustUseRepeatingSyntax);

    /// <summary>
    /// Analyzes properties referenced in a CompositeKey template string.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="typeDeclaration">The type declaration being analyzed.</param>
    /// <param name="typeSymbol">The symbol for the type declaration.</param>
    /// <param name="compositeKeyAttribute">The CompositeKey attribute data.</param>
    protected override void AnalyzeCompositeKeyType(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration,
        INamedTypeSymbol typeSymbol,
        AttributeData? compositeKeyAttribute)
    {
        if (compositeKeyAttribute is null)
            return;

        var templateString = GetTemplateString(compositeKeyAttribute);
        if (string.IsNullOrWhiteSpace(templateString))
            return;

        var primaryKeySeparator = GetPrimaryKeySeparator(compositeKeyAttribute);

        var tokenizer = new TemplateStringTokenizer(primaryKeySeparator);
        var tokenizationResult = tokenizer.Tokenize(templateString.AsSpan());
        if (!tokenizationResult.Success)
            return;

        var properties = GetTypeProperties(typeSymbol);

        foreach (var token in tokenizationResult.Tokens)
        {
            if (token is PropertyTemplateToken propertyToken)
            {
                var property = properties.FirstOrDefault(p => p.Name == propertyToken.Name);
                if (property is null)
                    continue; // Property not found is handled by template validation

                var accessibilityInfo = new PropertyValidation.PropertyAccessibilityInfo(
                    Name: property.Name,
                    HasGetter: property.GetMethod is not null,
                    HasSetter: property.SetMethod is not null);

                var accessibilityResult = PropertyValidation.ValidatePropertyAccessibility(accessibilityInfo);
                if (!accessibilityResult.IsSuccess)
                {
                    ReportPropertyDiagnostic(
                        context,
                        typeDeclaration,
                        property,
                        accessibilityResult);
                }

                // Check if a regular property is a collection type (should use repeating syntax)
                var collectionTypeInfo = CreateCollectionPropertyTypeInfo(property, context.Compilation);
                var nonCollectionResult = PropertyValidation.ValidateNonCollectionPropertyType(
                    property.Name,
                    collectionTypeInfo);

                if (!nonCollectionResult.IsSuccess)
                {
                    ReportPropertyDiagnostic(
                        context,
                        typeDeclaration,
                        property,
                        nonCollectionResult);
                }

                var typeInfo = CreatePropertyTypeInfo(property, context.Compilation);

                if (!string.IsNullOrEmpty(propertyToken.Format))
                {
                    var formatResult = PropertyValidation.ValidatePropertyFormat(
                        property.Name,
                        typeInfo,
                        propertyToken.Format);

                    if (!formatResult.IsSuccess)
                    {
                        ReportPropertyDiagnostic(
                            context,
                            typeDeclaration,
                            property,
                            formatResult);
                    }
                }

                var typeCompatibilityResult = PropertyValidation.ValidatePropertyTypeCompatibility(
                    property.Name,
                    typeInfo);

                if (!typeCompatibilityResult.IsSuccess)
                {
                    ReportPropertyDiagnostic(
                        context,
                        typeDeclaration,
                        property,
                        typeCompatibilityResult);
                }
            }
            else if (token is RepeatingPropertyTemplateToken repeatingToken)
            {
                var property = properties.FirstOrDefault(p => p.Name == repeatingToken.Name);
                if (property is null)
                    continue; // Property not found is handled by template validation

                var accessibilityInfo = new PropertyValidation.PropertyAccessibilityInfo(
                    Name: property.Name,
                    HasGetter: property.GetMethod is not null,
                    HasSetter: property.SetMethod is not null);

                var accessibilityResult = PropertyValidation.ValidatePropertyAccessibility(accessibilityInfo);
                if (!accessibilityResult.IsSuccess)
                {
                    ReportPropertyDiagnostic(
                        context,
                        typeDeclaration,
                        property,
                        accessibilityResult);
                }

                // Check if the property is a valid collection type
                var collectionTypeInfo = CreateCollectionPropertyTypeInfo(property, context.Compilation);
                var collectionResult = PropertyValidation.ValidateCollectionPropertyType(
                    property.Name,
                    collectionTypeInfo);

                if (!collectionResult.IsSuccess)
                {
                    ReportPropertyDiagnostic(
                        context,
                        typeDeclaration,
                        property,
                        collectionResult);
                    continue;
                }

                // Validate format/type compatibility for the inner type T
                var innerTypeInfo = CreateInnerTypeInfo(property, context.Compilation);
                if (innerTypeInfo is null)
                    continue;

                if (!string.IsNullOrEmpty(repeatingToken.Format))
                {
                    var formatResult = PropertyValidation.ValidatePropertyFormat(
                        property.Name,
                        innerTypeInfo,
                        repeatingToken.Format);

                    if (!formatResult.IsSuccess)
                    {
                        ReportPropertyDiagnostic(
                            context,
                            typeDeclaration,
                            property,
                            formatResult);
                    }
                }

                var typeCompatibilityResult = PropertyValidation.ValidatePropertyTypeCompatibility(
                    property.Name,
                    innerTypeInfo);

                if (!typeCompatibilityResult.IsSuccess)
                {
                    ReportPropertyDiagnostic(
                        context,
                        typeDeclaration,
                        property,
                        typeCompatibilityResult);
                }
            }
        }
    }

    /// <summary>
    /// Extracts the template string from the CompositeKey attribute.
    /// </summary>
    private static string? GetTemplateString(AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length > 0)
        {
            var arg = attributeData.ConstructorArguments[0];
            if (arg.Value is string templateString)
                return templateString;
        }
        return null;
    }

    /// <summary>
    /// Extracts the primary key separator from the CompositeKey attribute.
    /// </summary>
    private static char? GetPrimaryKeySeparator(AttributeData attributeData)
    {
        var separatorArg = attributeData.NamedArguments
            .FirstOrDefault(arg => arg.Key == "PrimaryKeySeparator");

        if (separatorArg.Value.Value is char separator)
            return separator;

        return null;
    }

    /// <summary>
    /// Gets all properties from a type symbol.
    /// </summary>
    private static ImmutableArray<IPropertySymbol> GetTypeProperties(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsStatic && !p.IsImplicitlyDeclared)
            .ToImmutableArray();
    }

    /// <summary>
    /// Creates CollectionPropertyTypeInfo from a property symbol for collection type detection.
    /// </summary>
    private static PropertyValidation.CollectionPropertyTypeInfo CreateCollectionPropertyTypeInfo(
        IPropertySymbol property,
        Compilation compilation)
    {
        var propertyType = property.Type;

        if (propertyType is not INamedTypeSymbol namedType || !namedType.IsGenericType)
        {
            return new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: propertyType.ToDisplayString(),
                IsList: false,
                IsReadOnlyList: false,
                IsImmutableArray: false);
        }

        var originalDefinition = namedType.OriginalDefinition;

        var listType = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
        var readOnlyListType = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyList`1");
        var immutableArrayType = compilation.GetTypeByMetadataName("System.Collections.Immutable.ImmutableArray`1");

        return new PropertyValidation.CollectionPropertyTypeInfo(
            TypeName: propertyType.ToDisplayString(),
            IsList: listType is not null && SymbolEqualityComparer.Default.Equals(originalDefinition, listType),
            IsReadOnlyList: readOnlyListType is not null && SymbolEqualityComparer.Default.Equals(originalDefinition, readOnlyListType),
            IsImmutableArray: immutableArrayType is not null && SymbolEqualityComparer.Default.Equals(originalDefinition, immutableArrayType));
    }

    /// <summary>
    /// Creates PropertyTypeInfo for the inner type T of a collection property.
    /// </summary>
    private static PropertyValidation.PropertyTypeInfo? CreateInnerTypeInfo(
        IPropertySymbol property,
        Compilation compilation)
    {
        if (property.Type is not INamedTypeSymbol { IsGenericType: true, TypeArguments.Length: > 0 } namedType)
            return null;

        var innerType = namedType.TypeArguments[0];

        var guidType = compilation.GetTypeByMetadataName("System.Guid");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        var isGuid = SymbolEqualityComparer.Default.Equals(innerType, guidType);
        var isString = SymbolEqualityComparer.Default.Equals(innerType, stringType);
        var isEnum = innerType.TypeKind == TypeKind.Enum;

        var interfaces = innerType.AllInterfaces;
        var isSpanParsable = interfaces.Any(i => i.ToDisplayString().StartsWith("System.ISpanParsable", StringComparison.Ordinal));
        var isSpanFormattable = interfaces.Any(i => i.ToDisplayString().Equals("System.ISpanFormattable", StringComparison.Ordinal));

        return new PropertyValidation.PropertyTypeInfo(
            TypeName: innerType.ToDisplayString(),
            IsGuid: isGuid,
            IsString: isString,
            IsEnum: isEnum,
            IsSpanParsable: isSpanParsable,
            IsSpanFormattable: isSpanFormattable);
    }

    /// <summary>
    /// Creates PropertyTypeInfo from a property symbol for validation.
    /// </summary>
    private static PropertyValidation.PropertyTypeInfo CreatePropertyTypeInfo(
        IPropertySymbol property,
        Compilation compilation)
    {
        var guidType = compilation.GetTypeByMetadataName("System.Guid");
        var stringType = compilation.GetSpecialType(SpecialType.System_String);

        var isGuid = SymbolEqualityComparer.Default.Equals(property.Type, guidType);
        var isString = SymbolEqualityComparer.Default.Equals(property.Type, stringType);
        var isEnum = property.Type.TypeKind == TypeKind.Enum;

        var interfaces = property.Type.AllInterfaces;
        var isSpanParsable = interfaces.Any(i => i.ToDisplayString().StartsWith("System.ISpanParsable", StringComparison.Ordinal));
        var isSpanFormattable = interfaces.Any(i => i.ToDisplayString().Equals("System.ISpanFormattable", StringComparison.Ordinal));

        return new PropertyValidation.PropertyTypeInfo(
            TypeName: property.Type.ToDisplayString(),
            IsGuid: isGuid,
            IsString: isString,
            IsEnum: isEnum,
            IsSpanParsable: isSpanParsable,
            IsSpanFormattable: isSpanFormattable);
    }

    /// <summary>
    /// Reports a diagnostic for a property validation issue with precise location targeting.
    /// </summary>
    private static void ReportPropertyDiagnostic(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration,
        IPropertySymbol property,
        PropertyValidation.PropertyValidationResult validationResult)
    {
        if (validationResult.IsSuccess || validationResult.Descriptor is null)
            return;

        var location = GetPropertyLocation(typeDeclaration, property) ?? typeDeclaration.Identifier.GetLocation();
        var diagnostic = Diagnostic.Create(
            validationResult.Descriptor,
            location,
            validationResult.MessageArgs ?? []);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Gets the precise location of a property declaration.
    /// </summary>
    private static Location? GetPropertyLocation(
        TypeDeclarationSyntax typeDeclaration,
        IPropertySymbol property)
    {
        var propertyDeclaration = typeDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(p => p.Identifier.Text == property.Name);

        if (propertyDeclaration != null)
        {
            return propertyDeclaration.Identifier.GetLocation();
        }

        if (typeDeclaration is RecordDeclarationSyntax { ParameterList: not null } recordDeclaration)
        {
            var parameter = recordDeclaration.ParameterList.Parameters
                .FirstOrDefault(p => p.Identifier.Text == property.Name);

            if (parameter != null)
            {
                return parameter.Identifier.GetLocation();
            }
        }

        return null;
    }
}
