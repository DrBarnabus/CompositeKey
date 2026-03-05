using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission;

internal sealed class SpanFormattableFormatStrategy : IFormatStrategy
{
    public static readonly SpanFormattableFormatStrategy Instance = new();

    public bool SupportsSpanFormat(PropertyKeyPart part) => false;

    public string GetVariableLengthExpression(PropertyKeyPart part) =>
        throw new InvalidOperationException("SpanFormattable types do not support span format path.");

    public void EmitSpanFormat(SourceWriter writer, PropertyKeyPart part, string positionVar, bool invariantFormatting) =>
        throw new InvalidOperationException("SpanFormattable types do not support span format path.");
}
