using System.Collections.Immutable;
using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompositeKey.SourceGeneration;

[Generator]
public sealed class SourceGenerator : IIncrementalGenerator
{
    private const string CompositeKeyAttributeFullName = "CompositeKey.CompositeKeyAttribute";

    public const string GenerationSpecTrackingName = nameof(GenerationSpec);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var generationSpecs = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                CompositeKeyAttributeFullName,
                (node, _) => node is RecordDeclarationSyntax or ClassDeclarationSyntax or StructDeclarationSyntax,
                static (context, _) => (TypeDeclarationSyntax: (TypeDeclarationSyntax)context.TargetNode, context.SemanticModel))
            .Combine(context.CompilationProvider.Select(static (compilation, _) => new KnownTypeSymbols(compilation)))
            .Select(static (tuple, cancellationToken) =>
            {
                var parser = new Parser(tuple.Right);
                var generationSpec = parser.Parse(tuple.Left.TypeDeclarationSyntax, tuple.Left.SemanticModel, cancellationToken);

                return (GenerationSpec: generationSpec, parser.Diagnostics);
            })
            .WithTrackingName(GenerationSpecTrackingName);

        context.RegisterSourceOutput(generationSpecs, ReportDiagnosticsAndEmitSource);
    }

    private void ReportDiagnosticsAndEmitSource(
        SourceProductionContext sourceProductionContext,
        (GenerationSpec? GenerationSpec, ImmutableEquatableArray<DiagnosticInfo> Diagnostics) input)
    {
        foreach (var diagnostic in input.Diagnostics)
            sourceProductionContext.ReportDiagnostic(diagnostic.CreateDiagnostic());

        if (input.GenerationSpec is null)
            return;

        OnSourceEmitting?.Invoke(ImmutableArray.Create(input.GenerationSpec));

        var emitter = new Emitter(sourceProductionContext);
        emitter.Emit(input.GenerationSpec);
    }

    public Action<ImmutableArray<GenerationSpec>>? OnSourceEmitting { get; init; }
}
