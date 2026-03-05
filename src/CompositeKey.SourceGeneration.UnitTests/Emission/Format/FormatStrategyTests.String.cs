using CompositeKey.SourceGeneration.Emission.Format;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Format;

public partial class FormatStrategyTests
{
    [Fact]
    public void StringFormatStrategy_SupportsSpanFormat_AlwaysReturnsTrue()
    {
        var part = CreateStringPart();
        StringFormatStrategy.Instance.SupportsSpanFormat(part).ShouldBeTrue();
    }

    [Fact]
    public void StringFormatStrategy_GetVariableLengthExpression_ReturnsPropertyLength()
    {
        var part = CreateStringPart();
        StringFormatStrategy.Instance.GetVariableLengthExpression(part).ShouldBe("Name.Length");
    }

    [Fact]
    public void StringFormatStrategy_EmitSpanFormat_EmitsCopyToAndPositionUpdate()
    {
        var part = CreateStringPart();
        var result = EmitToString(w => StringFormatStrategy.Instance.EmitSpanFormat(w, part, "position", true));

        result.ShouldContain("state.Name.CopyTo(destination[position..])");
        result.ShouldContain("position += state.Name.Length");
    }
}
