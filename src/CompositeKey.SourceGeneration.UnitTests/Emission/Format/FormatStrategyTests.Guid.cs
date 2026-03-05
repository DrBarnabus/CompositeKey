using CompositeKey.SourceGeneration.Emission.Format;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Format;

public partial class FormatStrategyTests
{
    [Fact]
    public void GuidFormatStrategy_SupportsSpanFormat_ReturnsTrueWhenExactLength()
    {
        var part = CreateGuidPart(exactLength: true);
        GuidFormatStrategy.Instance.SupportsSpanFormat(part).ShouldBeTrue();
    }

    [Fact]
    public void GuidFormatStrategy_SupportsSpanFormat_ReturnsFalseWhenNotExactLength()
    {
        var part = CreateGuidPart(exactLength: false);
        GuidFormatStrategy.Instance.SupportsSpanFormat(part).ShouldBeFalse();
    }

    [Fact]
    public void GuidFormatStrategy_GetVariableLengthExpression_ThrowsAsUnreachable()
    {
        var part = CreateGuidPart();
        Should.Throw<InvalidOperationException>(() => GuidFormatStrategy.Instance.GetVariableLengthExpression(part));
    }

    [Fact]
    public void GuidFormatStrategy_EmitSpanFormat_WithInvariantFormatting_EmitsInvariantCulture()
    {
        var part = CreateGuidPart();
        var result = EmitToString(w => GuidFormatStrategy.Instance.EmitSpanFormat(w, part, "position", true));

        result.ShouldContain("((ISpanFormattable)state.Id).TryFormat(destination[position..], out int idCharsWritten, \"D\"");
        result.ShouldContain("global::System.Globalization.CultureInfo.InvariantCulture");
        result.ShouldContain("position += idCharsWritten");
    }

    [Fact]
    public void GuidFormatStrategy_EmitSpanFormat_WithoutInvariantFormatting_EmitsNull()
    {
        var part = CreateGuidPart();
        var result = EmitToString(w => GuidFormatStrategy.Instance.EmitSpanFormat(w, part, "position", false));

        result.ShouldContain(", null)");
        result.ShouldNotContain("InvariantCulture");
    }

    [Fact]
    public void GuidFormatStrategy_EmitSpanFormat_UsesFormatSpecifier()
    {
        var part = CreateGuidPart(format: "N", length: 32);
        var result = EmitToString(w => GuidFormatStrategy.Instance.EmitSpanFormat(w, part, "position", true));

        result.ShouldContain("\"N\"");
    }

    [Fact]
    public void GuidFormatStrategy_EmitSpanFormat_DefaultsToFormatD()
    {
        var part = CreateGuidPart() with { Format = null };
        var result = EmitToString(w => GuidFormatStrategy.Instance.EmitSpanFormat(w, part, "position", true));

        result.ShouldContain("\"d\"");
    }
}
