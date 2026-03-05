using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Emission.Parse;
using CompositeKey.SourceGeneration.Model;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Parse;

public partial class ParseStrategyTests
{
    [Fact]
    public void EnumParseStrategy_EmitSingleParse_EmitsHelperTryParse()
    {
        var part = CreateEnumPart();
        var result = EmitToString(w => EnumParseStrategy.Instance.EmitSingleParse(w, part, "input", "status", true));

        result.ShouldContain("StatusHelper.TryParse(input, out var status)");
    }

    [Fact]
    public void EnumParseStrategy_EmitSingleParse_ThrowsWhenEnumSpecIsNull()
    {
        var part = CreateStringPart() with
        {
            TypeDescriptor = new PropertyTypeDescriptor(ParseType.Enum, FormatType.Enum)
        };

        Should.Throw<InvalidOperationException>(() =>
            EmitToString(w => EnumParseStrategy.Instance.EmitSingleParse(w, part, "input", "val", true)));
    }

    [Fact]
    public void EnumParseStrategy_EmitRepeatingItemParse_EmitsCorrectCode()
    {
        var enumSpec = new EnumSpec(
            "Status", "global::Status", "int",
            new ImmutableEquatableArray<EnumSpec.Member>([
                new EnumSpec.Member("Active", 0),
                new EnumSpec.Member("Inactive", 1)
            ]),
            IsSequentialFromZero: true);

        var part = CreateRepeatingPart(ParseType.Enum, FormatType.Enum, enumSpec);
        var result = EmitToString(w => EnumParseStrategy.Instance.EmitRepeatingItemParse(w, part, "item", "itemVar", "list", true));

        result.ShouldContain("StatusHelper.TryParse(item, out var itemVar)");
        result.ShouldContain("list.Add(itemVar)");
    }

    [Fact]
    public void EnumParseStrategy_EmitRepeatingItemParse_ThrowsWhenEnumSpecIsNull()
    {
        var part = CreateRepeatingPart(ParseType.Enum, FormatType.Enum);

        Should.Throw<InvalidOperationException>(() =>
            EmitToString(w => EnumParseStrategy.Instance.EmitRepeatingItemParse(w, part, "item", "itemVar", "list", true)));
    }
}
