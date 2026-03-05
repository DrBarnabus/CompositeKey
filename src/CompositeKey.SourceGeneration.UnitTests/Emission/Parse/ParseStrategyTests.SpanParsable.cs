using CompositeKey.SourceGeneration.Emission.Parse;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Parse;

public partial class ParseStrategyTests
{
    [Fact]
    public void SpanParsableParseStrategy_EmitSingleParse_EmitsFullyQualifiedTryParse()
    {
        var part = CreateSpanParsablePart();
        var result = EmitToString(w => SpanParsableParseStrategy.Instance.EmitSingleParse(w, part, "input", "count", true));

        result.ShouldContain("int.TryParse(input, out var count)");
    }

    [Fact]
    public void SpanParsableParseStrategy_EmitRepeatingItemParse_UsesInnerTypeName()
    {
        var part = CreateRepeatingPart(ParseType.SpanParsable, FormatType.SpanFormattable);
        var result = EmitToString(w => SpanParsableParseStrategy.Instance.EmitRepeatingItemParse(w, part, "item", "itemVar", "list", true));

        result.ShouldContain("int.TryParse(item, out var itemVar)");
        result.ShouldContain("list.Add(itemVar)");
    }
}
