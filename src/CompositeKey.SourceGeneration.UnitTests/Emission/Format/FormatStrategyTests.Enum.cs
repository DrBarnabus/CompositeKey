using CompositeKey.SourceGeneration.Emission.Format;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Format;

public partial class FormatStrategyTests
{
    [Fact]
    public void EnumFormatStrategy_SupportsSpanFormat_ReturnsTrueForFormatG()
    {
        var part = CreateEnumPart(format: "g");
        EnumFormatStrategy.Instance.SupportsSpanFormat(part).ShouldBeTrue();
    }

    [Fact]
    public void EnumFormatStrategy_SupportsSpanFormat_ReturnsFalseForOtherFormats()
    {
        var part = CreateEnumPart(format: "d");
        EnumFormatStrategy.Instance.SupportsSpanFormat(part).ShouldBeFalse();
    }

    [Fact]
    public void EnumFormatStrategy_SupportsSpanFormat_ReturnsFalseForNullFormat()
    {
        var part = CreateEnumPart() with { Format = null };
        EnumFormatStrategy.Instance.SupportsSpanFormat(part).ShouldBeFalse();
    }

    [Fact]
    public void EnumFormatStrategy_GetVariableLengthExpression_ReturnsHelperCall()
    {
        var part = CreateEnumPart();
        EnumFormatStrategy.Instance.GetVariableLengthExpression(part).ShouldBe("StatusHelper.GetFormattedLength(Status)");
    }

    [Fact]
    public void EnumFormatStrategy_GetVariableLengthExpression_ThrowsWhenEnumSpecIsNull()
    {
        var part = CreateStringPart() with
        {
            TypeDescriptor = new PropertyTypeDescriptor(ParseType.Enum, FormatType.Enum),
            Format = "g"
        };

        Should.Throw<InvalidOperationException>(() => EnumFormatStrategy.Instance.GetVariableLengthExpression(part));
    }

    [Fact]
    public void EnumFormatStrategy_EmitSpanFormat_EmitsHelperTryFormat()
    {
        var part = CreateEnumPart();
        var result = EmitToString(w => EnumFormatStrategy.Instance.EmitSpanFormat(w, part, "position", true));

        result.ShouldContain("StatusHelper.TryFormat(state.Status, destination[position..], out int statusCharsWritten)");
        result.ShouldContain("position += statusCharsWritten");
    }

    [Fact]
    public void EnumFormatStrategy_EmitSpanFormat_ThrowsWhenEnumSpecIsNull()
    {
        var part = CreateStringPart() with
        {
            TypeDescriptor = new PropertyTypeDescriptor(ParseType.Enum, FormatType.Enum),
            Format = "g"
        };

        Should.Throw<InvalidOperationException>(() =>
            EmitToString(w => EnumFormatStrategy.Instance.EmitSpanFormat(w, part, "position", true)));
    }
}
