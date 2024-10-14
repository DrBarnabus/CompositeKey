﻿using AutoFixture.Xunit2;
using FluentAssertions;

namespace CompositeKey.SourceGeneration.FunctionalTests;

public static class PrimaryKeyTests
{
    #region GuidOnlyPrimaryKey

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey(GuidOnlyPrimaryKey primaryKey)
    {
        var result = GuidOnlyPrimaryKey.Parse(primaryKey.ToString());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(primaryKey);
    }

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_ToString_ShouldReturnCorrectlyFormattedString(GuidOnlyPrimaryKey primaryKey)
    {
        string result = primaryKey.ToString();

        result.Should().NotBeNullOrEmpty();
        result.Should().Be($"{primaryKey.First}#{primaryKey.Second}");
    }

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(GuidOnlyPrimaryKey primaryKey)
    {
        string result = primaryKey.ToPartitionKeyString();

        result.Should().NotBeNullOrEmpty();
        result.Should().Be($"{primaryKey.First}#{primaryKey.Second}");
    }

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord(Guid first, Guid second)
    {
        var result = GuidOnlyPrimaryKey.Parse($"{first}#{second}");

        result.Should().NotBeNull();
        result.First.Should().Be(first);
        result.Second.Should().Be(second);
    }

    [Theory, MemberData(nameof(GuidOnlyPrimaryKey_InvalidInputs))]
    public static void GuidOnlyPrimaryKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => GuidOnlyPrimaryKey.Parse(input);
        act.Should().Throw<FormatException>();
    }

    [Theory, AutoData]
    public static void GuidOnlyPrimaryKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(Guid first, Guid second)
    {
        GuidOnlyPrimaryKey.TryParse($"{first}#{second}", out var result).Should().BeTrue();

        result.Should().NotBeNull();
        result!.First.Should().Be(first);
        result.Second.Should().Be(second);
    }

    [Theory, MemberData(nameof(GuidOnlyPrimaryKey_InvalidInputs))]
    public static void GuidOnlyPrimaryKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        GuidOnlyPrimaryKey.TryParse(input, out var result).Should().BeFalse();

        result.Should().BeNull();
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

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(primaryKey);
    }

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_ToString_ShouldReturnCorrectlyFormattedString(MixedTypePrimaryKey primaryKey)
    {
        string result = primaryKey.ToString();

        result.Should().NotBeNull();
        result.Should().Be($"{primaryKey.GuidValue}#{primaryKey.IntValue}~{primaryKey.StringValue}#{primaryKey.EnumValue}");
    }

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(MixedTypePrimaryKey primaryKey)
    {
        string result = primaryKey.ToPartitionKeyString();

        result.Should().NotBeNull();
        result.Should().Be($"{primaryKey.GuidValue}#{primaryKey.IntValue}~{primaryKey.StringValue}#{primaryKey.EnumValue}");
    }

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord(
        Guid guidValue, int intValue, string stringValue, MixedTypePrimaryKey.EnumType enumValue)
    {
        var result = MixedTypePrimaryKey.Parse($"{guidValue}#{intValue}~{stringValue}#{enumValue}");

        result.Should().NotBeNull();
        result.GuidValue.Should().Be(guidValue);
        result.IntValue.Should().Be(intValue);
        result.StringValue.Should().Be(stringValue);
        result.EnumValue.Should().Be(enumValue);
    }

    [Theory, MemberData(nameof(MixedTypePrimaryKey_InvalidInputs))]
    public static void MixedTypePrimaryKey_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => MixedTypePrimaryKey.Parse(input);
        act.Should().Throw<FormatException>();
    }

    [Theory, AutoData]
    public static void MixedTypePrimaryKey_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(
        Guid guidValue, int intValue, string stringValue, MixedTypePrimaryKey.EnumType enumValue)
    {
        MixedTypePrimaryKey.TryParse($"{guidValue}#{intValue}~{stringValue}#{enumValue}", out var result).Should().BeTrue();

        result.Should().NotBeNull();
        result!.GuidValue.Should().Be(guidValue);
        result.IntValue.Should().Be(intValue);
        result.StringValue.Should().Be(stringValue);
        result.EnumValue.Should().Be(enumValue);
    }

    [Theory, MemberData(nameof(MixedTypePrimaryKey_InvalidInputs))]
    public static void MixedTypePrimaryKey_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        MixedTypePrimaryKey.TryParse(input, out var result).Should().BeFalse();

        result.Should().BeNull();
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

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(primaryKey);
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_ToString_ShouldReturnCorrectlyFormattedString(PrimaryKeyWithConstants primaryKey)
    {
        string result = primaryKey.ToString();

        result.Should().NotBeNull();
        result.Should().Be($"ConstantValue#{primaryKey.DynamicValue}@ConstantStringAtEndOfKey");
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(PrimaryKeyWithConstants primaryKey)
    {
        string result = primaryKey.ToPartitionKeyString();

        result.Should().NotBeNull();
        result.Should().Be($"ConstantValue#{primaryKey.DynamicValue}@ConstantStringAtEndOfKey");
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_Parse_WithValidKey_ShouldReturnCorrectlyParsedRecord(
        Guid dynamicValue)
    {
        var result = PrimaryKeyWithConstants.Parse($"ConstantValue#{dynamicValue}@ConstantStringAtEndOfKey");

        result.Should().NotBeNull();
        result.DynamicValue.Should().Be(dynamicValue);
    }

    [Theory, MemberData(nameof(PrimaryKeyWithConstants_InvalidInputs))]
    public static void PrimaryKeyWithConstants_Parse_WithInvalidKey_ShouldThrowFormatException(string input)
    {
        var act = () => MixedTypePrimaryKey.Parse(input);
        act.Should().Throw<FormatException>();
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithConstants_TryParse_WithValidKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(
        Guid dynamicValue)
    {
        PrimaryKeyWithConstants.TryParse($"ConstantValue#{dynamicValue}@ConstantStringAtEndOfKey", out var result).Should().BeTrue();

        result.Should().NotBeNull();
        result!.DynamicValue.Should().Be(dynamicValue);
    }

    [Theory, MemberData(nameof(PrimaryKeyWithConstants_InvalidInputs))]
    public static void PrimaryKeyWithConstants_TryParse_WithInvalidKey_ShouldReturnFalse(string input)
    {
        MixedTypePrimaryKey.TryParse(input, out var result).Should().BeFalse();

        result.Should().BeNull();
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

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(primaryKey);
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithFastPathFormatting_ToString_ShouldReturnCorrectlyFormattedString(
        PrimaryKeyWithFastPathFormatting primaryKey)
    {
        string result = primaryKey.ToString();

        result.Should().NotBeNull();
        result.Should().Be($"{primaryKey.GuidValue}#Constant#{primaryKey.EnumValue}@{primaryKey.AnotherGuid}");
    }

    [Theory, AutoData]
    public static void PrimaryKeyWithFastPathFormatting_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(
        PrimaryKeyWithFastPathFormatting primaryKey)
    {
        string result = primaryKey.ToPartitionKeyString();

        result.Should().NotBeNull();
        result.Should().Be($"{primaryKey.GuidValue}#Constant#{primaryKey.EnumValue}@{primaryKey.AnotherGuid}");
    }

    #endregion
}
