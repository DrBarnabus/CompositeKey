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
        DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat);

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
            if (token is not PropertyTemplateToken propertyToken)
                continue;

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
