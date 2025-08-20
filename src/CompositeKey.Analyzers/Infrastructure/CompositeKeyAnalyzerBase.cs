using System.Collections.Immutable;
using CompositeKey.Analyzers.Common.Validation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CompositeKey.Analyzers.Infrastructure;

/// <summary>
/// Base class for CompositeKey analyzers providing common functionality
/// for syntax analysis, location targeting, and diagnostic reporting.
/// </summary>
public abstract class CompositeKeyAnalyzerBase : DiagnosticAnalyzer
{
    /// <summary>
    /// Gets the supported diagnostics for this analyzer.
    /// Derived classes must implement this to specify which diagnostics they support.
    /// </summary>
    public abstract override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }

    /// <summary>
    /// Initializes the analyzer by registering for syntax node analysis.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeTypeDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.ClassDeclaration);
    }

    /// <summary>
    /// Abstract method that derived analyzers implement to perform their specific analysis.
    /// </summary>
    /// <param name="context">The syntax node analysis context.</param>
    /// <param name="typeDeclaration">The type declaration being analyzed.</param>
    /// <param name="typeSymbol">The symbol for the type declaration.</param>
    /// <param name="compositeKeyAttribute">The CompositeKey attribute if present, null otherwise.</param>
    protected abstract void AnalyzeCompositeKeyType(
        SyntaxNodeAnalysisContext context,
        TypeDeclarationSyntax typeDeclaration,
        INamedTypeSymbol typeSymbol,
        AttributeData? compositeKeyAttribute);

    /// <summary>
    /// Analyzes a type declaration to determine if it has a CompositeKey attribute
    /// and delegates to the derived analyzer if it does.
    /// </summary>
    private void AnalyzeTypeDeclaration(SyntaxNodeAnalysisContext context)
    {
        var typeDeclaration = (TypeDeclarationSyntax)context.Node;
        var typeSymbol = context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken);

        if (typeSymbol is null)
            return;

        var compositeKeyAttribute = FindCompositeKeyAttribute(typeSymbol);
        if (compositeKeyAttribute is null)
            return;

        AnalyzeCompositeKeyType(context, typeDeclaration, typeSymbol, compositeKeyAttribute);
    }

    /// <summary>
    /// Finds the CompositeKey attribute on a type symbol.
    /// </summary>
    /// <param name="typeSymbol">The type symbol to examine.</param>
    /// <returns>The CompositeKey attribute data if found, null otherwise.</returns>
    protected static AttributeData? FindCompositeKeyAttribute(INamedTypeSymbol typeSymbol)
    {
        const string CompositeKeyAttributeName = "CompositeKey.CompositeKeyAttribute";

        return typeSymbol.GetAttributes()
            .FirstOrDefault(attr => attr.AttributeClass?.ToDisplayString() == CompositeKeyAttributeName);
    }


    /// <summary>
    /// Gets the precise location for a diagnostic based on the diagnostic type and syntax elements.
    /// </summary>
    /// <param name="typeDeclaration">The type declaration containing the error.</param>
    /// <param name="compositeKeyAttribute">The CompositeKey attribute if the error is attribute-related.</param>
    /// <param name="diagnosticType">The type of diagnostic being reported.</param>
    /// <returns>The most precise location for the diagnostic.</returns>
    protected static Location GetPreciseLocation(
        TypeDeclarationSyntax typeDeclaration,
        AttributeData? compositeKeyAttribute = null,
        DiagnosticType diagnosticType = DiagnosticType.Type)
    {
        return diagnosticType switch
        {
            DiagnosticType.Attribute when compositeKeyAttribute is not null =>
                GetAttributeLocation(typeDeclaration, compositeKeyAttribute) ?? typeDeclaration.Identifier.GetLocation(),

            _ => typeDeclaration.Identifier.GetLocation()
        };
    }

    /// <summary>
    /// Attempts to get the location of a specific attribute on a type declaration.
    /// </summary>
    private static Location? GetAttributeLocation(TypeDeclarationSyntax typeDeclaration, AttributeData attributeData)
    {
        // Find the attribute syntax that corresponds to this attribute data
        var attributeLists = typeDeclaration.AttributeLists;

        foreach (var attributeList in attributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                // Check if this attribute syntax matches our CompositeKey attribute
                var name = attribute.Name.ToString();
                if (name.Contains("CompositeKey") || name.Contains("CompositeKeyAttribute"))
                {
                    return attribute.GetLocation();
                }
            }
        }

        return null;
    }


    /// <summary>
    /// Reports a diagnostic from a validation result with precise location targeting.
    /// </summary>
    /// <param name="context">The analysis context.</param>
    /// <param name="validationResult">The validation result containing diagnostic information.</param>
    /// <param name="typeDeclaration">The type declaration for location targeting.</param>
    /// <param name="compositeKeyAttribute">The CompositeKey attribute for attribute-related diagnostics.</param>
    /// <param name="diagnosticType">The type of diagnostic for location targeting.</param>
    protected static void ReportDiagnostic(
        SyntaxNodeAnalysisContext context,
        TypeValidationResult validationResult,
        TypeDeclarationSyntax typeDeclaration,
        AttributeData? compositeKeyAttribute = null,
        DiagnosticType diagnosticType = DiagnosticType.Type)
    {
        if (validationResult.IsSuccess || validationResult.Descriptor is null)
            return;

        var location = GetPreciseLocation(typeDeclaration, compositeKeyAttribute, diagnosticType);
        var diagnostic = Diagnostic.Create(
            validationResult.Descriptor,
            location,
            validationResult.MessageArgs ?? []);

        context.ReportDiagnostic(diagnostic);
    }


}

/// <summary>
/// Enumeration of diagnostic types for precise location targeting.
/// </summary>
public enum DiagnosticType
{
    /// <summary>
    /// Diagnostic relates to the type declaration itself.
    /// </summary>
    Type,

    /// <summary>
    /// Diagnostic relates to an attribute on the type.
    /// </summary>
    Attribute
}
