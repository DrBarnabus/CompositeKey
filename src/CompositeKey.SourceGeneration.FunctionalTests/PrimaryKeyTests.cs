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
}
