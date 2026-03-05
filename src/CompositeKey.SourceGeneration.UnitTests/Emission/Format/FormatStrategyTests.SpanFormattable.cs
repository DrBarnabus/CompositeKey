using CompositeKey.SourceGeneration.Emission.Format;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Format;

public partial class FormatStrategyTests
{
    [Fact]
    public void SpanFormattableFormatStrategy_SupportsSpanFormat_AlwaysReturnsFalse()
    {
        var part = CreateSpanFormattablePart();
        SpanFormattableFormatStrategy.Instance.SupportsSpanFormat(part).ShouldBeFalse();
    }

    [Fact]
    public void SpanFormattableFormatStrategy_GetVariableLengthExpression_ThrowsAsUnreachable()
    {
        var part = CreateSpanFormattablePart();
        Should.Throw<InvalidOperationException>(() => SpanFormattableFormatStrategy.Instance.GetVariableLengthExpression(part));
    }

    [Fact]
    public void SpanFormattableFormatStrategy_EmitSpanFormat_ThrowsAsUnreachable()
    {
        var part = CreateSpanFormattablePart();
        Should.Throw<InvalidOperationException>(() =>
            EmitToString(w => SpanFormattableFormatStrategy.Instance.EmitSpanFormat(w, part, "position", true)));
    }
}
