using AutoFixture.Xunit2;

namespace CompositeKey.SourceGeneration.FunctionalTests;

public static class PrimaryKeyTests
{
    #region GuidOnlyPrimaryKey

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey(GuidOnlyPrimaryKey primaryKey)
    {
        var result = GuidOnlyPrimaryKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(primaryKey);
    }

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_ToString_ShouldReturnCorrectlyFormattedString(GuidOnlyPrimaryKey primaryKey)
    {
        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{primaryKey.First}#{primaryKey.Second}");
    }

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(GuidOnlyPrimaryKey primaryKey)
    {
        string result = primaryKey.ToPartitionKeyString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{primaryKey.First}#{primaryKey.Second}");
    }

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord(Guid first, Guid second)
    {
        var result = GuidOnlyPrimaryKey.Parse($"{first}#{second}");

        result.ShouldNotBeNull();
        result.First.ShouldBe(first);
        result.Second.ShouldBe(second);
    }

    [Theory, MemberData(nameof(GuidOnlyPrimaryKey_InvalidInputs))]
    public static void GuidOnlyPrimaryKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => GuidOnlyPrimaryKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(Guid first, Guid second)
    {
        GuidOnlyPrimaryKey.TryParse($"{first}#{second}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.First.ShouldBe(first);
        result.Second.ShouldBe(second);
    }

    [Theory, MemberData(nameof(GuidOnlyPrimaryKey_InvalidInputs))]
    public static void GuidOnlyPrimaryKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        GuidOnlyPrimaryKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    public static object[][] GuidOnlyPrimaryKey_InvalidInputs() =>
    [
        ["a"],
        ["#"],
        ["a#b"],
        ["15cd670a-89c7-4c7f-8245-507ec9e41c8b#b"],
        ["a#850f53f5-749c-421e-98fb-a3b4fbeeaeb4"],
        ["15cd670a-89c7-4c7f-8245-507ec9e41c8b#850f53f5-749c-421e-98fb-a3b4fbeeaeb4#too-long"]
    ];

    #endregion

    #region MixedTypePrimaryKey

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey(MixedTypePrimaryKey primaryKey)
    {
        var result = MixedTypePrimaryKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(primaryKey);
    }

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_ToString_ShouldReturnCorrectlyFormattedString(MixedTypePrimaryKey primaryKey)
    {
        string result = primaryKey.ToString();

        result.ShouldNotBeNull();
        result.ShouldBe($"{primaryKey.GuidValue}#{primaryKey.IntValue}~{primaryKey.StringValue}#{primaryKey.EnumValue}");
    }

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(MixedTypePrimaryKey primaryKey)
    {
        string result = primaryKey.ToPartitionKeyString();

        result.ShouldNotBeNull();
        result.ShouldBe($"{primaryKey.GuidValue}#{primaryKey.IntValue}~{primaryKey.StringValue}#{primaryKey.EnumValue}");
    }

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord(
        Guid guidValue, int intValue, string stringValue, MixedTypePrimaryKey.EnumType enumValue)
    {
        var result = MixedTypePrimaryKey.Parse($"{guidValue}#{intValue}~{stringValue}#{enumValue}");

        result.ShouldNotBeNull();
        result.GuidValue.ShouldBe(guidValue);
        result.IntValue.ShouldBe(intValue);
        result.StringValue.ShouldBe(stringValue);
        result.EnumValue.ShouldBe(enumValue);
    }

    [Theory, MemberData(nameof(MixedTypePrimaryKey_InvalidInputs))]
    public static void MixedTypePrimaryKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => MixedTypePrimaryKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(
        Guid guidValue, int intValue, string stringValue, MixedTypePrimaryKey.EnumType enumValue)
    {
        MixedTypePrimaryKey.TryParse($"{guidValue}#{intValue}~{stringValue}#{enumValue}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.GuidValue.ShouldBe(guidValue);
        result.IntValue.ShouldBe(intValue);
        result.StringValue.ShouldBe(stringValue);
        result.EnumValue.ShouldBe(enumValue);
    }

    [Theory, MemberData(nameof(MixedTypePrimaryKey_InvalidInputs))]
    public static void MixedTypePrimaryKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        MixedTypePrimaryKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    public static object[][] MixedTypePrimaryKey_InvalidInputs() =>
    [
        ["a"],
        ["a#b"],
        ["a#b#c"],
        ["a#b#c#d"],
        ["a#b#c#d#e"],
        ["a#123~string#Two"],
        ["4b5912e8-7625-4668-b6a9-eed84972123e#h~string#Two"],
        ["4b5912e8-7625-4668-b6a9-eed84972123e#123~#Two"],
        ["4b5912e8-7625-4668-b6a9-eed84972123e#123~string#Invalid"],
        ["4b5912e8-7625-4668-b6a9-eed84972123e#123~string#Two#Extra"]
    ];

    #endregion

    #region PrimaryKeyWithConstants

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_RoundTripToStringAndParse_ShouldResultInEquivalentKey(PrimaryKeyWithConstants primaryKey)
    {
        var result = PrimaryKeyWithConstants.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(primaryKey);
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_ToString_ShouldReturnCorrectlyFormattedString(PrimaryKeyWithConstants primaryKey)
    {
        string result = primaryKey.ToString();

        result.ShouldNotBeNull();
        result.ShouldBe($"ConstantValue#{primaryKey.DynamicValue}@ConstantStringAtEndOfKey");
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(PrimaryKeyWithConstants primaryKey)
    {
        string result = primaryKey.ToPartitionKeyString();

        result.ShouldNotBeNull();
        result.ShouldBe($"ConstantValue#{primaryKey.DynamicValue}@ConstantStringAtEndOfKey");
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord(
        Guid dynamicValue)
    {
        var result = PrimaryKeyWithConstants.Parse($"ConstantValue#{dynamicValue}@ConstantStringAtEndOfKey");

        result.ShouldNotBeNull();
        result.DynamicValue.ShouldBe(dynamicValue);
    }

    [Theory, MemberData(nameof(PrimaryKeyWithConstants_InvalidInputs))]
    public static void PrimaryKeyWithConstants_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => PrimaryKeyWithConstants.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(
        Guid dynamicValue)
    {
        PrimaryKeyWithConstants.TryParse($"ConstantValue#{dynamicValue}@ConstantStringAtEndOfKey", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.DynamicValue.ShouldBe(dynamicValue);
    }

    [Theory, MemberData(nameof(PrimaryKeyWithConstants_InvalidInputs))]
    public static void PrimaryKeyWithConstants_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        PrimaryKeyWithConstants.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    public static object[][] PrimaryKeyWithConstants_InvalidInputs() =>
    [
        ["a"],
        ["a#b"],
        ["a#b@c"],
        ["wrong#9326f586-a5f7-4e97-89fa-ab72876de78f#ConstantStringAtEndOfKey"],
        ["ConstantValue#wrong@ConstantStringAtEndOfKey"],
        ["ConstantValue#9326f586-a5f7-4e97-89fa-ab72876de78f#ConstantAtEndOfKey"],
        ["ConstantValue#9326f586-a5f7-4e97-89fa-ab72876de78f@ConstantAtEndOfKey#Extra"],
    ];

    #endregion

    #region PrimaryKeyWithFastPathFormatting

    [Theory, AutoData]
    public static void PrimaryKeyWithFastPathFormatting_RoundTripToStringAndParse_ShouldResultInEquivalentKey(
        PrimaryKeyWithFastPathFormatting primaryKey)
    {
        var result = PrimaryKeyWithFastPathFormatting.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(primaryKey);
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithFastPathFormatting_ToString_ShouldReturnCorrectlyFormattedString(
        PrimaryKeyWithFastPathFormatting primaryKey)
    {
        string result = primaryKey.ToString();

        result.ShouldNotBeNull();
        result.ShouldBe($"{primaryKey.GuidValue}#Constant#{primaryKey.EnumValue}@{primaryKey.StringValue}");
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithFastPathFormatting_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(
        PrimaryKeyWithFastPathFormatting primaryKey)
    {
        string result = primaryKey.ToPartitionKeyString();

        result.ShouldNotBeNull();
        result.ShouldBe($"{primaryKey.GuidValue}#Constant#{primaryKey.EnumValue}@{primaryKey.StringValue}");
    }

    [Theory]
    [InlineAutoData(0, false)]
    [InlineAutoData(0, true)]
    [InlineAutoData(1, false)]
    [InlineAutoData(1, true)]
    [InlineAutoData(2, false)]
    [InlineAutoData(2, true)]
    [InlineAutoData(3, false)]
    public static void PrimaryKeyWithFastPathFormatting_ToPartitionKeyString_WithSpecificPartIndexAndDelimiterRequirements_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter, PrimaryKeyWithFastPathFormatting compositeKey)
    {
        (var guidValue, var enumValue, string stringValue) = compositeKey;

        string result = compositeKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        switch (throughPartIndex)
        {
            case 0 when !includeTrailingDelimiter:
                result.ShouldBe($"{guidValue}");
                break;
            case 0 when includeTrailingDelimiter:
                result.ShouldBe($"{guidValue}#");
                break;
            case 1 when !includeTrailingDelimiter:
                result.ShouldBe($"{guidValue}#Constant");
                break;
            case 1 when includeTrailingDelimiter:
                result.ShouldBe($"{guidValue}#Constant#");
                break;
            case 2 when !includeTrailingDelimiter:
                result.ShouldBe($"{guidValue}#Constant#{enumValue}");
                break;
            case 2 when includeTrailingDelimiter:
                result.ShouldBe($"{guidValue}#Constant#{enumValue}@");
                break;
            case 3:
                result.ShouldBe($"{guidValue}#Constant#{enumValue}@{stringValue}");
                break;
        }
    }

    [Theory]
    [InlineAutoData(3, true)]
    [InlineAutoData(4, false)]
    public static void PrimaryKeyWithFastPathFormatting_ToPartitionKeyString_WithInvalidPartIndexOrDelimiterRequirements_ShouldThrowInvalidOperationException(
        int throughPartIndex, bool includeTrailingDelimiter, PrimaryKeyWithFastPathFormatting compositeKey)
    {
        var act = () => compositeKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);
        act.ShouldThrow<InvalidOperationException>();
    }

    #endregion

    #region RepeatingGuidPrimaryKey

    [Fact]
    public static void RepeatingGuidPrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey()
    {
        var primaryKey = new RepeatingGuidPrimaryKey([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        var result = RepeatingGuidPrimaryKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.LocationId.ShouldBe(primaryKey.LocationId);
    }

    [Fact]
    public static void RepeatingGuidPrimaryKey_ToString_ShouldReturnCorrectlyFormattedString()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var primaryKey = new RepeatingGuidPrimaryKey(ids);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{ids[0]}#{ids[1]}");
    }

    [Fact]
    public static void RepeatingGuidPrimaryKey_ToString_WithSingleItem_ShouldReturnCorrectlyFormattedString()
    {
        var id = Guid.NewGuid();
        var primaryKey = new RepeatingGuidPrimaryKey([id]);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{id}");
    }

    [Fact]
    public static void RepeatingGuidPrimaryKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var result = RepeatingGuidPrimaryKey.Parse($"{id1}#{id2}#{id3}");

        result.ShouldNotBeNull();
        result.LocationId.Count.ShouldBe(3);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
        result.LocationId[2].ShouldBe(id3);
    }

    [Theory, MemberData(nameof(RepeatingGuidPrimaryKey_InvalidInputs))]
    public static void RepeatingGuidPrimaryKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => RepeatingGuidPrimaryKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void RepeatingGuidPrimaryKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        RepeatingGuidPrimaryKey.TryParse($"{id1}#{id2}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Theory, MemberData(nameof(RepeatingGuidPrimaryKey_InvalidInputs))]
    public static void RepeatingGuidPrimaryKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        RepeatingGuidPrimaryKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public static void RepeatingGuidPrimaryKey_ToPartitionKeyString_WithSpecificPartIndex_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter)
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var primaryKey = new RepeatingGuidPrimaryKey(ids);

        string result = primaryKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        string expected = throughPartIndex switch
        {
            0 when !includeTrailingDelimiter => $"{ids[0]}",
            0 when includeTrailingDelimiter => $"{ids[0]}#",
            1 when !includeTrailingDelimiter => $"{ids[0]}#{ids[1]}",
            1 when includeTrailingDelimiter => $"{ids[0]}#{ids[1]}#",
            2 => $"{ids[0]}#{ids[1]}#{ids[2]}",
            _ => throw new InvalidOperationException()
        };

        result.ShouldBe(expected);
    }

    public static object[][] RepeatingGuidPrimaryKey_InvalidInputs() =>
    [
        [""],
        ["not-a-guid"],
        ["#"],
        ["not-a-guid#also-not-a-guid"]
    ];

    #endregion

    #region HierarchicalLocationKey

    [Fact]
    public static void HierarchicalLocationKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey()
    {
        var primaryKey = new HierarchicalLocationKey([Guid.NewGuid(), Guid.NewGuid()]);

        var result = HierarchicalLocationKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.LocationId.ShouldBe(primaryKey.LocationId);
    }

    [Fact]
    public static void HierarchicalLocationKey_ToString_ShouldReturnCorrectlyFormattedString()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var primaryKey = new HierarchicalLocationKey(ids);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"LOCATION#{ids[0]}#{ids[1]}");
    }

    [Fact]
    public static void HierarchicalLocationKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var result = HierarchicalLocationKey.Parse($"LOCATION#{id1}#{id2}");

        result.ShouldNotBeNull();
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Fact]
    public static void HierarchicalLocationKey_Parse_WithSingleItem_ShouldReturnCorrectlyParsedRecord()
    {
        var id = Guid.NewGuid();

        var result = HierarchicalLocationKey.Parse($"LOCATION#{id}");

        result.ShouldNotBeNull();
        result.LocationId.Count.ShouldBe(1);
        result.LocationId[0].ShouldBe(id);
    }

    [Theory, MemberData(nameof(HierarchicalLocationKey_InvalidInputs))]
    public static void HierarchicalLocationKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => HierarchicalLocationKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void HierarchicalLocationKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        HierarchicalLocationKey.TryParse($"LOCATION#{id1}#{id2}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Theory, MemberData(nameof(HierarchicalLocationKey_InvalidInputs))]
    public static void HierarchicalLocationKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        HierarchicalLocationKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public static void HierarchicalLocationKey_ToPartitionKeyString_WithSpecificPartIndex_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter)
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var primaryKey = new HierarchicalLocationKey(ids);

        string result = primaryKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        string expected = throughPartIndex switch
        {
            0 when !includeTrailingDelimiter => "LOCATION",
            0 when includeTrailingDelimiter => "LOCATION#",
            1 when !includeTrailingDelimiter => $"LOCATION#{ids[0]}",
            1 when includeTrailingDelimiter => $"LOCATION#{ids[0]}#",
            2 => $"LOCATION#{ids[0]}#{ids[1]}",
            _ => throw new InvalidOperationException()
        };

        result.ShouldBe(expected);
    }

    public static object[][] HierarchicalLocationKey_InvalidInputs() =>
    [
        [""],
        ["a"],
        ["WRONG#15cd670a-89c7-4c7f-8245-507ec9e41c8b"],
        ["LOCATION#not-a-guid"],
        ["LOCATION"]
    ];

    #endregion

    #region TaggedEntityKey

    [Fact]
    public static void TaggedEntityKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey()
    {
        var primaryKey = new TaggedEntityKey("MyType", ["alpha", "beta", "gamma"]);

        var result = TaggedEntityKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.Type.ShouldBe(primaryKey.Type);
        result.Tags.ShouldBe(primaryKey.Tags);
    }

    [Fact]
    public static void TaggedEntityKey_ToString_ShouldReturnCorrectlyFormattedString()
    {
        var primaryKey = new TaggedEntityKey("Entity", ["tag1", "tag2"]);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe("Entity#tag1,tag2");
    }

    [Fact]
    public static void TaggedEntityKey_ToString_WithSingleTag_ShouldReturnCorrectlyFormattedString()
    {
        var primaryKey = new TaggedEntityKey("Entity", ["tag1"]);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe("Entity#tag1");
    }

    [Fact]
    public static void TaggedEntityKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord()
    {
        var result = TaggedEntityKey.Parse("MyType#alpha,beta,gamma");

        result.ShouldNotBeNull();
        result.Type.ShouldBe("MyType");
        result.Tags.Count.ShouldBe(3);
        result.Tags[0].ShouldBe("alpha");
        result.Tags[1].ShouldBe("beta");
        result.Tags[2].ShouldBe("gamma");
    }

    [Fact]
    public static void TaggedEntityKey_Parse_WithSingleTag_ShouldReturnCorrectlyParsedRecord()
    {
        var result = TaggedEntityKey.Parse("MyType#single");

        result.ShouldNotBeNull();
        result.Type.ShouldBe("MyType");
        result.Tags.Count.ShouldBe(1);
        result.Tags[0].ShouldBe("single");
    }

    [Theory, MemberData(nameof(TaggedEntityKey_InvalidInputs))]
    public static void TaggedEntityKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => TaggedEntityKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void TaggedEntityKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        TaggedEntityKey.TryParse("MyType#tag1,tag2", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.Type.ShouldBe("MyType");
        result.Tags.Count.ShouldBe(2);
        result.Tags[0].ShouldBe("tag1");
        result.Tags[1].ShouldBe("tag2");
    }

    [Theory, MemberData(nameof(TaggedEntityKey_InvalidInputs))]
    public static void TaggedEntityKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        TaggedEntityKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public static void TaggedEntityKey_ToPartitionKeyString_WithSpecificPartIndex_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter)
    {
        var primaryKey = new TaggedEntityKey("MyType", ["alpha", "beta", "gamma"]);

        string result = primaryKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        string expected = throughPartIndex switch
        {
            0 when !includeTrailingDelimiter => "MyType",
            0 when includeTrailingDelimiter => "MyType#",
            1 when !includeTrailingDelimiter => "MyType#alpha",
            1 when includeTrailingDelimiter => "MyType#alpha,",
            2 => "MyType#alpha,beta",
            _ => throw new InvalidOperationException()
        };

        result.ShouldBe(expected);
    }

    public static object[][] TaggedEntityKey_InvalidInputs() =>
    [
        [""],
        ["a"],
        ["#tag"],
        ["type#"]
    ];

    #endregion

    #region FastPathRepeatingKey

    [Fact]
    public static void FastPathRepeatingKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey()
    {
        var primaryKey = new FastPathRepeatingKey(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()]);

        var result = FastPathRepeatingKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(primaryKey.TenantId);
        result.LocationId.ShouldBe(primaryKey.LocationId);
    }

    [Fact]
    public static void FastPathRepeatingKey_ToString_ShouldReturnCorrectlyFormattedString()
    {
        var tenantId = Guid.NewGuid();
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var primaryKey = new FastPathRepeatingKey(tenantId, ids);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{tenantId}#{ids[0]}#{ids[1]}");
    }

    [Fact]
    public static void FastPathRepeatingKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord()
    {
        var tenantId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var result = FastPathRepeatingKey.Parse($"{tenantId}#{id1}#{id2}");

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Fact]
    public static void FastPathRepeatingKey_Parse_WithSingleRepeatingItem_ShouldReturnCorrectlyParsedRecord()
    {
        var tenantId = Guid.NewGuid();
        var id1 = Guid.NewGuid();

        var result = FastPathRepeatingKey.Parse($"{tenantId}#{id1}");

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.LocationId.Count.ShouldBe(1);
        result.LocationId[0].ShouldBe(id1);
    }

    [Theory, MemberData(nameof(FastPathRepeatingKey_InvalidInputs))]
    public static void FastPathRepeatingKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => FastPathRepeatingKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void FastPathRepeatingKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        var tenantId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        FastPathRepeatingKey.TryParse($"{tenantId}#{id1}#{id2}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Theory, MemberData(nameof(FastPathRepeatingKey_InvalidInputs))]
    public static void FastPathRepeatingKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        FastPathRepeatingKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public static void FastPathRepeatingKey_ToPartitionKeyString_WithSpecificPartIndex_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter)
    {
        var tenantId = Guid.NewGuid();
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var primaryKey = new FastPathRepeatingKey(tenantId, ids);

        string result = primaryKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        string expected = throughPartIndex switch
        {
            0 when !includeTrailingDelimiter => $"{tenantId}",
            0 when includeTrailingDelimiter => $"{tenantId}#",
            1 when !includeTrailingDelimiter => $"{tenantId}#{ids[0]}",
            1 when includeTrailingDelimiter => $"{tenantId}#{ids[0]}#",
            2 => $"{tenantId}#{ids[0]}#{ids[1]}",
            _ => throw new InvalidOperationException()
        };

        result.ShouldBe(expected);
    }

    public static object[][] FastPathRepeatingKey_InvalidInputs() =>
    [
        [""],
        ["a"],
        ["not-a-guid#also-not-a-guid"],
        ["15cd670a-89c7-4c7f-8245-507ec9e41c8b"]
    ];

    #endregion

    #region RepeatingEnumPrimaryKey

    [Fact]
    public static void RepeatingEnumPrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey()
    {
        var primaryKey = new RepeatingEnumPrimaryKey([RepeatingEnumPrimaryKey.ItemType.Alpha, RepeatingEnumPrimaryKey.ItemType.Beta, RepeatingEnumPrimaryKey.ItemType.Gamma]);

        var result = RepeatingEnumPrimaryKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.Items.ShouldBe(primaryKey.Items);
    }

    [Fact]
    public static void RepeatingEnumPrimaryKey_ToString_ShouldReturnCorrectlyFormattedString()
    {
        var primaryKey = new RepeatingEnumPrimaryKey([RepeatingEnumPrimaryKey.ItemType.Alpha, RepeatingEnumPrimaryKey.ItemType.Beta]);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe("ITEMS#Alpha,Beta");
    }

    [Fact]
    public static void RepeatingEnumPrimaryKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord()
    {
        var result = RepeatingEnumPrimaryKey.Parse("ITEMS#Alpha,Beta,Gamma");

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(3);
        result.Items[0].ShouldBe(RepeatingEnumPrimaryKey.ItemType.Alpha);
        result.Items[1].ShouldBe(RepeatingEnumPrimaryKey.ItemType.Beta);
        result.Items[2].ShouldBe(RepeatingEnumPrimaryKey.ItemType.Gamma);
    }

    [Fact]
    public static void RepeatingEnumPrimaryKey_Parse_WithSingleItem_ShouldReturnCorrectlyParsedRecord()
    {
        var result = RepeatingEnumPrimaryKey.Parse("ITEMS#Delta");

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(1);
        result.Items[0].ShouldBe(RepeatingEnumPrimaryKey.ItemType.Delta);
    }

    [Theory, MemberData(nameof(RepeatingEnumPrimaryKey_InvalidInputs))]
    public static void RepeatingEnumPrimaryKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => RepeatingEnumPrimaryKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void RepeatingEnumPrimaryKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        RepeatingEnumPrimaryKey.TryParse("ITEMS#Alpha,Beta", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.Items.Count.ShouldBe(2);
        result.Items[0].ShouldBe(RepeatingEnumPrimaryKey.ItemType.Alpha);
        result.Items[1].ShouldBe(RepeatingEnumPrimaryKey.ItemType.Beta);
    }

    [Theory, MemberData(nameof(RepeatingEnumPrimaryKey_InvalidInputs))]
    public static void RepeatingEnumPrimaryKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        RepeatingEnumPrimaryKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public static void RepeatingEnumPrimaryKey_ToPartitionKeyString_WithSpecificPartIndex_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter)
    {
        var primaryKey = new RepeatingEnumPrimaryKey([RepeatingEnumPrimaryKey.ItemType.Alpha, RepeatingEnumPrimaryKey.ItemType.Beta, RepeatingEnumPrimaryKey.ItemType.Gamma]);

        string result = primaryKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        string expected = throughPartIndex switch
        {
            0 when !includeTrailingDelimiter => "ITEMS",
            0 when includeTrailingDelimiter => "ITEMS#",
            1 when !includeTrailingDelimiter => "ITEMS#Alpha",
            1 when includeTrailingDelimiter => "ITEMS#Alpha,",
            2 => "ITEMS#Alpha,Beta",
            _ => throw new InvalidOperationException()
        };

        result.ShouldBe(expected);
    }

    public static object[][] RepeatingEnumPrimaryKey_InvalidInputs() =>
    [
        [""],
        ["ITEMS"],
        ["WRONG#Alpha,Beta"],
        ["ITEMS#InvalidValue"],
        ["ITEMS#"]
    ];

    #endregion

    #region RepeatingIntPrimaryKey

    [Fact]
    public static void RepeatingIntPrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey()
    {
        var primaryKey = new RepeatingIntPrimaryKey([10, 20, 30]);

        var result = RepeatingIntPrimaryKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.Scores.ShouldBe(primaryKey.Scores);
    }

    [Fact]
    public static void RepeatingIntPrimaryKey_ToString_ShouldReturnCorrectlyFormattedString()
    {
        var primaryKey = new RepeatingIntPrimaryKey([10, 20]);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe("SCORES#10,20");
    }

    [Fact]
    public static void RepeatingIntPrimaryKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord()
    {
        var result = RepeatingIntPrimaryKey.Parse("SCORES#1,2,3");

        result.ShouldNotBeNull();
        result.Scores.Count.ShouldBe(3);
        result.Scores[0].ShouldBe(1);
        result.Scores[1].ShouldBe(2);
        result.Scores[2].ShouldBe(3);
    }

    [Fact]
    public static void RepeatingIntPrimaryKey_Parse_WithSingleItem_ShouldReturnCorrectlyParsedRecord()
    {
        var result = RepeatingIntPrimaryKey.Parse("SCORES#42");

        result.ShouldNotBeNull();
        result.Scores.Count.ShouldBe(1);
        result.Scores[0].ShouldBe(42);
    }

    [Theory, MemberData(nameof(RepeatingIntPrimaryKey_InvalidInputs))]
    public static void RepeatingIntPrimaryKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => RepeatingIntPrimaryKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void RepeatingIntPrimaryKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        RepeatingIntPrimaryKey.TryParse("SCORES#10,20", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.Scores.Count.ShouldBe(2);
        result.Scores[0].ShouldBe(10);
        result.Scores[1].ShouldBe(20);
    }

    [Theory, MemberData(nameof(RepeatingIntPrimaryKey_InvalidInputs))]
    public static void RepeatingIntPrimaryKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        RepeatingIntPrimaryKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public static void RepeatingIntPrimaryKey_ToPartitionKeyString_WithSpecificPartIndex_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter)
    {
        var primaryKey = new RepeatingIntPrimaryKey([10, 20, 30]);

        string result = primaryKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        string expected = throughPartIndex switch
        {
            0 when !includeTrailingDelimiter => "SCORES",
            0 when includeTrailingDelimiter => "SCORES#",
            1 when !includeTrailingDelimiter => "SCORES#10",
            1 when includeTrailingDelimiter => "SCORES#10,",
            2 => "SCORES#10,20",
            _ => throw new InvalidOperationException()
        };

        result.ShouldBe(expected);
    }

    public static object[][] RepeatingIntPrimaryKey_InvalidInputs() =>
    [
        [""],
        ["SCORES"],
        ["WRONG#1,2,3"],
        ["SCORES#abc"],
        ["SCORES#"]
    ];

    #endregion

    #region ImmutableArrayPrimaryKey

    [Fact]
    public static void ImmutableArrayPrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey()
    {
        var primaryKey = new ImmutableArrayPrimaryKey([Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        var result = ImmutableArrayPrimaryKey.Parse(primaryKey.ToString());

        result.ShouldNotBeNull();
        result.NodeIds.ShouldBe(primaryKey.NodeIds);
    }

    [Fact]
    public static void ImmutableArrayPrimaryKey_ToString_ShouldReturnCorrectlyFormattedString()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var primaryKey = new ImmutableArrayPrimaryKey([id1, id2]);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"NODES#{id1}#{id2}");
    }

    [Fact]
    public static void ImmutableArrayPrimaryKey_ToString_WithSingleItem_ShouldReturnCorrectlyFormattedString()
    {
        var id = Guid.NewGuid();
        var primaryKey = new ImmutableArrayPrimaryKey([id]);

        string result = primaryKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"NODES#{id}");
    }

    [Fact]
    public static void ImmutableArrayPrimaryKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var result = ImmutableArrayPrimaryKey.Parse($"NODES#{id1}#{id2}#{id3}");

        result.ShouldNotBeNull();
        result.NodeIds.Length.ShouldBe(3);
        result.NodeIds[0].ShouldBe(id1);
        result.NodeIds[1].ShouldBe(id2);
        result.NodeIds[2].ShouldBe(id3);
    }

    [Fact]
    public static void ImmutableArrayPrimaryKey_Parse_WithSingleItem_ShouldReturnCorrectlyParsedRecord()
    {
        var id = Guid.NewGuid();

        var result = ImmutableArrayPrimaryKey.Parse($"NODES#{id}");

        result.ShouldNotBeNull();
        result.NodeIds.Length.ShouldBe(1);
        result.NodeIds[0].ShouldBe(id);
    }

    [Theory, MemberData(nameof(ImmutableArrayPrimaryKey_InvalidInputs))]
    public static void ImmutableArrayPrimaryKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => ImmutableArrayPrimaryKey.Parse(input);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void ImmutableArrayPrimaryKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        ImmutableArrayPrimaryKey.TryParse($"NODES#{id1}#{id2}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.NodeIds.Length.ShouldBe(2);
        result.NodeIds[0].ShouldBe(id1);
        result.NodeIds[1].ShouldBe(id2);
    }

    [Theory, MemberData(nameof(ImmutableArrayPrimaryKey_InvalidInputs))]
    public static void ImmutableArrayPrimaryKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        ImmutableArrayPrimaryKey.TryParse(input, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public static void ImmutableArrayPrimaryKey_ToPartitionKeyString_WithSpecificPartIndex_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter)
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var primaryKey = new ImmutableArrayPrimaryKey([ids[0], ids[1], ids[2]]);

        string result = primaryKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        string expected = throughPartIndex switch
        {
            0 when !includeTrailingDelimiter => "NODES",
            0 when includeTrailingDelimiter => "NODES#",
            1 when !includeTrailingDelimiter => $"NODES#{ids[0]}",
            1 when includeTrailingDelimiter => $"NODES#{ids[0]}#",
            2 => $"NODES#{ids[0]}#{ids[1]}",
            _ => throw new InvalidOperationException()
        };

        result.ShouldBe(expected);
    }

    public static object[][] ImmutableArrayPrimaryKey_InvalidInputs() =>
    [
        [""],
        ["NODES"],
        ["WRONG#15cd670a-89c7-4c7f-8245-507ec9e41c8b"],
        ["NODES#not-a-guid"],
        ["NODES#"]
    ];

    #endregion
}
