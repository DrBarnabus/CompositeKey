using CompositeKey.SourceGeneration.Emission.Parse;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Parse;

public partial class ParseStrategyTests
{
    [Fact]
    public void GuidParseStrategy_EmitSingleParse_WithExactLength_EmitsStrictLengthCheck()
    {
        var part = CreateGuidPart();
        var result = EmitToString(w => GuidParseStrategy.Instance.EmitSingleParse(w, part, "input", "id", true, false));

        result.ShouldContain("input.Length != 36 || ");
        result.ShouldContain("Guid.TryParseExact(input, \"D\", out var id)");
        result.ShouldContain("throw new FormatException(\"Unrecognized format.\")");
    }

    [Fact]
    public void GuidParseStrategy_EmitSingleParse_WithoutExactLength_OmitsStrictLengthCheck()
    {
        var part = CreateGuidPart(exactLength: false);
        var result = EmitToString(w => GuidParseStrategy.Instance.EmitSingleParse(w, part, "input", "id", true, false));

        result.ShouldNotContain("input.Length !=");
        result.ShouldContain("Guid.TryParseExact(input, \"D\", out var id)");
    }

    [Fact]
    public void GuidParseStrategy_EmitSingleParse_WithShouldThrowFalse_EmitsReturnFalse()
    {
        var part = CreateGuidPart();
        var result = EmitToString(w => GuidParseStrategy.Instance.EmitSingleParse(w, part, "input", "id", false, false));

        result.ShouldContain("return false");
        result.ShouldNotContain("throw new FormatException");
    }

    [Fact]
    public void GuidParseStrategy_EmitSingleParse_UsesFormatSpecifier()
    {
        var part = CreateGuidPart(format: "N", length: 32);
        var result = EmitToString(w => GuidParseStrategy.Instance.EmitSingleParse(w, part, "input", "id", true, false));

        result.ShouldContain("Guid.TryParseExact(input, \"N\", out var id)");
    }

    [Fact]
    public void GuidParseStrategy_EmitSingleParse_WithSkipRedundantLengthCheck_OmitsLengthCheck()
    {
        var part = CreateGuidPart();
        var result = EmitToString(w => GuidParseStrategy.Instance.EmitSingleParse(w, part, "input", "id", true, true));

        result.ShouldNotContain("input.Length !=");
        result.ShouldContain("Guid.TryParseExact(input, \"D\", out var id)");
    }

    [Fact]
    public void GuidParseStrategy_EmitRepeatingItemParse_EmitsCorrectCode()
    {
        var part = CreateRepeatingPart(ParseType.Guid, FormatType.Guid);
        var result = EmitToString(w => GuidParseStrategy.Instance.EmitRepeatingItemParse(w, part, "item", "itemVar", "list", true));

        result.ShouldContain("Guid.TryParseExact(item, \"D\", out var itemVar)");
        result.ShouldContain("list.Add(itemVar)");
    }

    [Fact]
    public void GuidParseStrategy_EmitRepeatingItemParse_WithExactLength_EmitsStrictLengthCheck()
    {
        var part = CreateRepeatingPart(ParseType.Guid, FormatType.Guid) with
        {
            ExactLengthRequirement = true,
            LengthRequired = 36
        };
        var result = EmitToString(w => GuidParseStrategy.Instance.EmitRepeatingItemParse(w, part, "item", "itemVar", "list", true));

        result.ShouldContain("item.Length != 36 || ");
        result.ShouldContain("Guid.TryParseExact(item, \"D\", out var itemVar)");
    }

    [Fact]
    public void GuidParseStrategy_EmitRepeatingItemParse_WithoutExactLength_OmitsStrictLengthCheck()
    {
        var part = CreateRepeatingPart(ParseType.Guid, FormatType.Guid);
        var result = EmitToString(w => GuidParseStrategy.Instance.EmitRepeatingItemParse(w, part, "item", "itemVar", "list", true));

        result.ShouldNotContain("item.Length !=");
    }
}
