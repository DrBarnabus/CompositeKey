using System.Collections.Immutable;
using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.Validation;
using CompositeKey.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CompositeKey.Analyzers.Analyzers;

/// <summary>
/// Analyzer that validates template string format and primary key separator requirements
/// for CompositeKey-annotated types in real-time.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TemplateStringAnalyzer : CompositeKeyAnalyzerBase
{
    /// <summary>
    /// Gets the supported diagnostics for template string validation.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
        DiagnosticDescriptors.EmptyOrInvalidTemplateString,
        DiagnosticDescriptors.PrimaryKeySeparatorMissingFromTemplateString,
        DiagnosticDescriptors.RepeatingPropertyMustBeLastPart);

    /// <summary>
    /// Analyzes a CompositeKey-annotated type for template string requirements.
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

        var templateString = ExtractTemplateString(compositeKeyAttribute);
        if (templateString is null)
        {
            ReportEmptyOrInvalidTemplate(context, typeDeclaration, string.Empty);
            return;
        }

        var basicValidation = TemplateValidation.ValidateTemplateString(templateString);
        if (!basicValidation.IsSuccess)
        {
            ReportTemplateValidationError(context, typeDeclaration, basicValidation, templateString);
            return;
        }

        var primaryKeySeparator = ExtractPrimaryKeySeparator(compositeKeyAttribute);

        var tokenizationResult = TemplateValidation.TokenizeTemplateString(templateString, primaryKeySeparator);
        if (!tokenizationResult.Success)
        {
            ReportEmptyOrInvalidTemplate(context, typeDeclaration, templateString);
            return;
        }

        if (!TemplateValidation.HasValidTemplateStructure(tokenizationResult.Tokens))
        {
            ReportEmptyOrInvalidTemplate(context, typeDeclaration, templateString);
            return;
        }

        if (primaryKeySeparator.HasValue)
        {
            var separatorValidation = TemplateValidation.ValidatePrimaryKeySeparator(
                templateString,
                primaryKeySeparator,
                tokenizationResult.Tokens);

            if (!separatorValidation.IsSuccess)
            {
                ReportSeparatorValidationError(context, typeDeclaration, separatorValidation);
                return;
            }

            if (!TemplateValidation.ValidatePartitionAndSortKeyStructure(tokenizationResult.Tokens, out _))
            {
                ReportEmptyOrInvalidTemplate(context, typeDeclaration, templateString);
                return;
            }
        }

        // Validate repeating property count (at most one per key section)
        var repeatingCountValidation = TemplateValidation.ValidateRepeatingPropertyCount(tokenizationResult.Tokens);
        if (!repeatingCountValidation.IsSuccess)
        {
            ReportTemplateValidationError(context, typeDeclaration, repeatingCountValidation, templateString);
            return;
        }

        // Validate repeating property position (must be last value part in section)
        var repeatingPositionValidation = TemplateValidation.ValidateRepeatingPropertyPosition(tokenizationResult.Tokens);
        if (!repeatingPositionValidation.IsSuccess)
        {
            ReportTemplateValidationError(context, typeDeclaration, repeatingPositionValidation, templateString);
        }
    }

    /// <summary>
    /// Extracts the template string from the CompositeKey attribute.
    /// </summary>
    private static string? ExtractTemplateString(AttributeData attributeData)
    {
        if (attributeData.ConstructorArguments.Length > 0)
        {
            var arg = attributeData.ConstructorArguments[0];
            if (arg.Value is string templateString)
            {
                return templateString;
            }
        }

        return null;
    }

    /// <summary>
    /// Extracts the PrimaryKeySeparator property value from the CompositeKey attribute.
    /// </summary>
    private static char? ExtractPrimaryKeySeparator(AttributeData attributeData)
    {
        foreach (var namedArgument in attributeData.NamedArguments)
        {
            if (namedArgument is { Key: "PrimaryKeySeparator", Value.Value: char separator })
            {
                return separator;
            }
        }

        return null;
    }

    /// <summary>
    /// Reports an empty or invalid template string diagnostic.
    /// </summary>
    private static void ReportEmptyOrInvalidTemplate(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration,
        string templateString)
    {
        var location = GetTemplateStringLocation(typeDeclaration);

        var diagnostic = Diagnostic.Create(
            DiagnosticDescriptors.EmptyOrInvalidTemplateString,
            location,
            templateString);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Reports a template validation error with precise location targeting.
    /// </summary>
    private static void ReportTemplateValidationError(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration,
        TemplateValidation.TemplateValidationResult validationResult,
        string templateString)
    {
        if (validationResult.IsSuccess || validationResult.Descriptor is null)
            return;

        var location = GetTemplateStringLocation(typeDeclaration);

        var diagnostic = Diagnostic.Create(
            validationResult.Descriptor,
            location,
            validationResult.MessageArgs ?? new[] { templateString });

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Reports a separator validation error with precise location targeting.
    /// </summary>
    private static void ReportSeparatorValidationError(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration,
        TemplateValidation.TemplateValidationResult validationResult)
    {
        if (validationResult.IsSuccess || validationResult.Descriptor is null)
            return;

        var location = GetPrimaryKeySeparatorLocation(typeDeclaration);

        var diagnostic = Diagnostic.Create(
            validationResult.Descriptor,
            location,
            validationResult.MessageArgs ?? []);

        context.ReportDiagnostic(diagnostic);
    }

    /// <summary>
    /// Gets the location of the template string argument in the attribute.
    /// </summary>
    private static Location? GetTemplateStringLocation(TypeDeclarationSyntax typeDeclaration)
    {
        var attributeSyntax = FindAttributeSyntax(typeDeclaration);
        if (attributeSyntax?.ArgumentList?.Arguments.Count > 0)
        {
            var templateArgument = attributeSyntax.ArgumentList.Arguments[0];
            return templateArgument.Expression.GetLocation();
        }

        return null;
    }

    /// <summary>
    /// Gets the location of the PrimaryKeySeparator property in the attribute.
    /// </summary>
    private static Location? GetPrimaryKeySeparatorLocation(TypeDeclarationSyntax typeDeclaration)
    {
        var attributeSyntax = FindAttributeSyntax(typeDeclaration);
        if (attributeSyntax?.ArgumentList != null)
        {
            foreach (var argument in attributeSyntax.ArgumentList.Arguments)
            {
                if (argument.NameEquals?.Name.Identifier.Text == "PrimaryKeySeparator")
                {
                    return argument.GetLocation();
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Finds the attribute syntax node that corresponds to the attribute data.
    /// </summary>
    private static AttributeSyntax? FindAttributeSyntax(TypeDeclarationSyntax typeDeclaration)
    {
        foreach (var attribute in typeDeclaration.AttributeLists.SelectMany(al => al.Attributes))
        {
            var name = attribute.Name.ToString();
            if (name.Contains("CompositeKey") || name.Contains("CompositeKeyAttribute"))
            {
                return attribute;
            }
        }

        return null;
    }
}
