using AutoFixture.Xunit2;
using FluentAssertions;

namespace CompositeKey.SourceGeneration.FunctionalTests;

public static class CompositePrimaryKeyTests
{
    [Theory, AutoData]
    public static void CompositePrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey(CompositePrimaryKey compositeKey)
    {
        var result = CompositePrimaryKey.Parse(compositeKey.ToString());

        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(compositeKey);
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_ToString_ShouldReturnCorrectlyFormattedString(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        string result = compositeKey.ToString();

        result.Should().NotBeNullOrEmpty();
        result.Should().Be($"{guidValue}#{decimalValue}|Constant~{enumValue}@{stringValue}");
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, _, _) = compositeKey;

        string result = compositeKey.ToPartitionKeyString();

        result.Should().NotBeNullOrEmpty();
        result.Should().Be($"{guidValue}#{decimalValue}");
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_ToSortKeyString_ShouldReturnCorrectlyFormattedString(CompositePrimaryKey compositeKey)
    {
        (_, _, var enumValue, string stringValue) = compositeKey;

        string result = compositeKey.ToSortKeyString();

        result.Should().NotBeNullOrEmpty();
        result.Should().Be($"Constant~{enumValue}@{stringValue}");
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_Parse_WithValidPrimaryKey_ShouldReturnCorrectlyParsedRecord(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        var result = CompositePrimaryKey.Parse($"{guidValue}#{decimalValue}|Constant~{enumValue}@{stringValue}");

        result.Should().NotBeNull();
        result.GuidValue.Should().Be(guidValue);
        result.DecimalValue.Should().Be(decimalValue);
        result.EnumValue.Should().Be(enumValue);
        result.StringValue.Should().Be(stringValue);
    }

    [Theory, MemberData(nameof(CompositePrimaryKey_InvalidPrimaryKeys))]
    public static void CompositePrimaryKey_Parse_WithInvalidPrimaryKey_ShouldThrowFormatException(string primaryKey)
    {
        var act = () => CompositePrimaryKey.Parse(primaryKey);
        act.Should().Throw<FormatException>();
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_Parse_WithValidPartitionKeyAndSortKey_ShouldReturnCorrectlyParsedRecord(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        var result = CompositePrimaryKey.Parse($"{guidValue}#{decimalValue}", $"Constant~{enumValue}@{stringValue}");

        result.Should().NotBeNull();
        result.GuidValue.Should().Be(guidValue);
        result.DecimalValue.Should().Be(decimalValue);
        result.EnumValue.Should().Be(enumValue);
        result.StringValue.Should().Be(stringValue);
    }

    [Theory, MemberData(nameof(CompositePrimaryKey_InvalidPartitionKeyAndSortKeys))]
    public static void CompositePrimaryKey_Parse_WithInvalidPartitionKeyAndSortKey_ShouldThrowFormatException(string partitionKey, string sortKey)
    {
        var act = () => CompositePrimaryKey.Parse(partitionKey, sortKey);
        act.Should().Throw<FormatException>();
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_TryParse_WithValidPrimaryKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        CompositePrimaryKey.TryParse($"{guidValue}#{decimalValue}|Constant~{enumValue}@{stringValue}", out var result).Should().BeTrue();

        result.Should().NotBeNull();
        result!.GuidValue.Should().Be(guidValue);
        result.DecimalValue.Should().Be(decimalValue);
        result.EnumValue.Should().Be(enumValue);
        result.StringValue.Should().Be(stringValue);
    }

    [Theory, MemberData(nameof(CompositePrimaryKey_InvalidPrimaryKeys))]
    public static void CompositePrimaryKey_TryParse_WithInvalidPrimaryKey_ShouldThrowFormatException(string primaryKey)
    {
        CompositePrimaryKey.TryParse(primaryKey, out var result).Should().BeFalse();

        result.Should().BeNull();
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_TryParse_WithValidPartitionKeyAndSortKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        CompositePrimaryKey.TryParse($"{guidValue}#{decimalValue}", $"Constant~{enumValue}@{stringValue}", out var result).Should().BeTrue();

        result.Should().NotBeNull();
        result!.GuidValue.Should().Be(guidValue);
        result.DecimalValue.Should().Be(decimalValue);
        result.EnumValue.Should().Be(enumValue);
        result.StringValue.Should().Be(stringValue);
    }

    [Theory, MemberData(nameof(CompositePrimaryKey_InvalidPartitionKeyAndSortKeys))]
    public static void CompositePrimaryKey_TryParse_WithInvalidPartitionKeyAndSortKey_ShouldThrowFormatException(string partitionKey, string sortKey)
    {
        CompositePrimaryKey.TryParse(partitionKey, sortKey, out var result).Should().BeFalse();

        result.Should().BeNull();
    }

    public static object[][] CompositePrimaryKey_InvalidPrimaryKeys() =>
    [
        ["a"],
        ["a#b"],
        ["a#b|c"],
        ["a#b|c~d"],
        ["a#b|c~d@e"],
        ["a#123|Constant~Three@String"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#b|Constant~Three@String"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#123|c~Three@String"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#123|Constant~d@String"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#123|Constant~Three@"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#123|Constant~Three@String~Extra"]
    ];

    public static object[][] CompositePrimaryKey_InvalidPartitionKeyAndSortKeys() =>
    [
        ["a", "c"],
        ["a#b", "c"],
        ["a#b", "c"],
        ["a#b", "c~d"],
        ["a#b", "c~d@e"],
        ["a#123", "Constant~Three@String"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#b", "Constant~Three@String"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#123", "c~Three@String"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#123", "Constant~d@String"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#123", "Constant~Three@"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#123", "Constant~Three@String~Extra"]
    ];
}
