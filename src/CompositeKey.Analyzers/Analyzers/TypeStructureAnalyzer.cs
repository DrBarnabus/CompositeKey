using System.Collections.Immutable;
using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.Validation;
using CompositeKey.Analyzers.Infrastructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CompositeKey.Analyzers.Analyzers;

/// <summary>
/// Analyzer that validates type structure requirements for CompositeKey-annotated types.
/// Reports diagnostics for unsupported types, missing partial modifiers, and constructor accessibility.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TypeStructureAnalyzer : CompositeKeyAnalyzerBase
{
    /// <summary>
    /// Gets the supported diagnostics for type structure validation.
    /// </summary>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.UnsupportedCompositeType,
            DiagnosticDescriptors.CompositeTypeMustBePartial,
            DiagnosticDescriptors.NoObviousDefaultConstructor);

    /// <summary>
    /// Analyzes a CompositeKey-annotated type for structural requirements.
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
        // Resolve the CompositeKeyConstructor attribute type for constructor validation
        var compositeKeyConstructorAttributeType = context.Compilation
            .GetTypeByMetadataName("CompositeKey.CompositeKeyConstructorAttribute");

        // Use shared type validation logic to validate the type structure and constructor selection
        var validationResult = TypeValidation.ValidateTypeForCompositeKey(
            typeSymbol,
            typeDeclaration,
            context.SemanticModel,
            compositeKeyConstructorAttributeType,
            context.CancellationToken);

        // Report any validation failures with precise location targeting
        if (!validationResult.IsSuccess)
        {
            var diagnosticType = GetDiagnosticTypeForDescriptor(validationResult.Descriptor);
            ReportDiagnostic(
                context,
                validationResult,
                typeDeclaration,
                compositeKeyAttribute,
                diagnosticType);
        }
    }

    /// <summary>
    /// Determines the appropriate diagnostic type for precise location targeting.
    /// </summary>
    /// <param name="descriptor">The diagnostic descriptor.</param>
    /// <returns>The diagnostic type for location targeting.</returns>
    private static DiagnosticType GetDiagnosticTypeForDescriptor(DiagnosticDescriptor descriptor)
    {
        return descriptor.Id switch
        {
            "COMPOSITE0002" => DiagnosticType.Type,      // UnsupportedCompositeType - target type identifier
            "COMPOSITE0003" => DiagnosticType.Type,      // CompositeTypeMustBePartial - target type identifier
            "COMPOSITE0004" => DiagnosticType.Attribute, // NoObviousDefaultConstructor - target attribute since it's about constructor selection
            _ => DiagnosticType.Type
        };
    }
}
