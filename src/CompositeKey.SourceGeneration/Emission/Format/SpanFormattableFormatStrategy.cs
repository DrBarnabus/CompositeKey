using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission.Format;

internal sealed class SpanFormattableFormatStrategy : IFormatStrategy
{
    public static readonly SpanFormattableFormatStrategy Instance = new();

    public bool SupportsSpanFormat(PropertyKeyPart part) => false;

    public string GetVariableLengthExpression(PropertyKeyPart part) =>
        throw new InvalidOperationException("Unreachable: SpanFormattable types are not eligible for the span format path.");

    public void EmitSpanFormat(SourceWriter writer, PropertyKeyPart part, string positionVar, bool invariantFormatting) =>
        throw new InvalidOperationException("Unreachable: SpanFormattable types are not eligible for the span format path.");
}
