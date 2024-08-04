using CompositeId.SourceGeneration.Core;
using CompositeId.SourceGeneration.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompositeId.SourceGeneration;

[Generator]
public sealed partial class CompositeSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationSpecs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                CompositeIdAttributeFullName,
                (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax,
                static (context, _) => (TypeDeclarationSyntax: (TypeDeclarationSyntax)context.TargetNode, context.SemanticModel))
            .Combine(context.CompilationProvider.Select(static (compilation, _) => new KnownTypeSymbols(compilation)))
            .Select(static (tuple, cancellationToken) =>
            {
                var parser = new Parser(tuple.Right);
                var generationSpec = parser.Parse(tuple.Left.TypeDeclarationSyntax, tuple.Left.SemanticModel, cancellationToken);

                return (GenerationSpec: generationSpec, parser.Diagnostics);
            })
            .WithTrackingName(nameof(GenerationSpec));

        context.RegisterSourceOutput(generationSpecs, ReportDiagnosticsAndEmitSource);
    }

    private static void ReportDiagnosticsAndEmitSource(
        SourceProductionContext sourceProductionContext,
        (Model.GenerationSpec? GenerationSpec, ImmutableEquatableArray<DiagnosticInfo> Diagnostics) input)
    {
        foreach (var diagnostic in input.Diagnostics)
            sourceProductionContext.ReportDiagnostic(diagnostic.CreateDiagnostic());

        if (input.GenerationSpec is null)
            return;

        var emitter = new Emitter(sourceProductionContext);
        emitter.Emit(input.GenerationSpec);
    }
}
