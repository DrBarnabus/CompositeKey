using System.Diagnostics;
using CompositeKey.SourceGeneration.Core;
using Microsoft.CodeAnalysis;

namespace CompositeKey.SourceGeneration;

internal sealed class DiagnosticContext(Compilation compilation)
{
    private readonly Compilation _compilation = compilation;
    private readonly List<DiagnosticInfo> _diagnostics = [];

    public Location? CurrentLocation { get; set; }

    public IReadOnlyList<DiagnosticInfo> Diagnostics => _diagnostics;

    public void ReportDiagnostic(DiagnosticDescriptor descriptor, Location? location, params object?[]? messageArgs)
    {
        Debug.Assert(CurrentLocation != null);

        if (location is null || (location.SourceTree is not null && !_compilation.ContainsSyntaxTree(location.SourceTree)))
            location = CurrentLocation;

        _diagnostics.Add(DiagnosticInfo.Create(descriptor, location, messageArgs));
    }

    public ImmutableEquatableArray<DiagnosticInfo> ToImmutableDiagnostics() => _diagnostics.ToImmutableEquatableArray();
}
