using CompositeKey.SourceGeneration.Emission.Parse;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Parse;

public partial class ParseStrategyTests
{
    [Fact]
    public void StringParseStrategy_EmitSingleParse_EmitsLengthCheckAndToString()
    {
        var part = CreateStringPart();
        var result = EmitToString(w => StringParseStrategy.Instance.EmitSingleParse(w, part, "input", "name", true));

        result.ShouldContain("input.Length == 0");
        result.ShouldContain("string name = input.ToString()");
    }

    [Fact]
    public void StringParseStrategy_EmitSingleParse_WithShouldThrowFalse_EmitsReturnFalse()
    {
        var part = CreateStringPart();
        var result = EmitToString(w => StringParseStrategy.Instance.EmitSingleParse(w, part, "input", "name", false));

        result.ShouldContain("return false");
        result.ShouldNotContain("throw new FormatException");
    }

    [Fact]
    public void StringParseStrategy_EmitRepeatingItemParse_EmitsCorrectCode()
    {
        var part = CreateRepeatingPart(ParseType.String, FormatType.String);
        var result = EmitToString(w => StringParseStrategy.Instance.EmitRepeatingItemParse(w, part, "item", "itemVar", "list", true));

        result.ShouldContain("item.Length == 0");
        result.ShouldContain("list.Add(item.ToString())");
    }
}
