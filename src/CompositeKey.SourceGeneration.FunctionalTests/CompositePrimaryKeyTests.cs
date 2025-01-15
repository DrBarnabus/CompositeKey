using AutoFixture.Xunit2;

namespace CompositeKey.SourceGeneration.FunctionalTests;

public static class CompositePrimaryKeyTests
{
    [Theory, AutoData]
    public static void CompositePrimaryKey_RoundTripToStringAndParse_ShouldResultInEquivalentKey(CompositePrimaryKey compositeKey)
    {
        var result = CompositePrimaryKey.Parse(compositeKey.ToString());

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(compositeKey);
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_ToString_ShouldReturnCorrectlyFormattedString(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        string result = compositeKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{guidValue}#{decimalValue}|Constant~{enumValue}@{stringValue}");
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, _, _) = compositeKey;

        string result = compositeKey.ToPartitionKeyString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{guidValue}#{decimalValue}");
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_ToSortKeyString_ShouldReturnCorrectlyFormattedString(CompositePrimaryKey compositeKey)
    {
        (_, _, var enumValue, string stringValue) = compositeKey;

        string result = compositeKey.ToSortKeyString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"Constant~{enumValue}@{stringValue}");
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_Parse_WithValidPrimaryKey_ShouldReturnCorrectlyParsedRecord(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        var result = CompositePrimaryKey.Parse($"{guidValue}#{decimalValue}|Constant~{enumValue}@{stringValue}");

        result.ShouldNotBeNull();
        result.GuidValue.ShouldBe(guidValue);
        result.DecimalValue.ShouldBe(decimalValue);
        result.EnumValue.ShouldBe(enumValue);
        result.StringValue.ShouldBe(stringValue);
    }

    [Theory, MemberData(nameof(CompositePrimaryKey_InvalidPrimaryKeys))]
    public static void CompositePrimaryKey_Parse_WithInvalidPrimaryKey_ShouldThrowFormatException(string primaryKey)
    {
        var act = () => CompositePrimaryKey.Parse(primaryKey);
        act.ShouldThrow<FormatException>();
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_Parse_WithValidPartitionKeyAndSortKey_ShouldReturnCorrectlyParsedRecord(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        var result = CompositePrimaryKey.Parse($"{guidValue}#{decimalValue}", $"Constant~{enumValue}@{stringValue}");

        result.ShouldNotBeNull();
        result.GuidValue.ShouldBe(guidValue);
        result.DecimalValue.ShouldBe(decimalValue);
        result.EnumValue.ShouldBe(enumValue);
        result.StringValue.ShouldBe(stringValue);
    }

    [Theory, MemberData(nameof(CompositePrimaryKey_InvalidPartitionKeyAndSortKeys))]
    public static void CompositePrimaryKey_Parse_WithInvalidPartitionKeyAndSortKey_ShouldThrowFormatException(string partitionKey, string sortKey)
    {
        var act = () => CompositePrimaryKey.Parse(partitionKey, sortKey);
        act.ShouldThrow<FormatException>();
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_TryParse_WithValidPrimaryKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        CompositePrimaryKey.TryParse($"{guidValue}#{decimalValue}|Constant~{enumValue}@{stringValue}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.GuidValue.ShouldBe(guidValue);
        result.DecimalValue.ShouldBe(decimalValue);
        result.EnumValue.ShouldBe(enumValue);
        result.StringValue.ShouldBe(stringValue);
    }

    [Theory, MemberData(nameof(CompositePrimaryKey_InvalidPrimaryKeys))]
    public static void CompositePrimaryKey_TryParse_WithInvalidPrimaryKey_ShouldThrowFormatException(string primaryKey)
    {
        CompositePrimaryKey.TryParse(primaryKey, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_TryParse_WithValidPartitionKeyAndSortKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, var enumValue, string stringValue) = compositeKey;

        CompositePrimaryKey.TryParse($"{guidValue}#{decimalValue}", $"Constant~{enumValue}@{stringValue}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.GuidValue.ShouldBe(guidValue);
        result.DecimalValue.ShouldBe(decimalValue);
        result.EnumValue.ShouldBe(enumValue);
        result.StringValue.ShouldBe(stringValue);
    }

    [Theory, MemberData(nameof(CompositePrimaryKey_InvalidPartitionKeyAndSortKeys))]
    public static void CompositePrimaryKey_TryParse_WithInvalidPartitionKeyAndSortKey_ShouldThrowFormatException(string partitionKey, string sortKey)
    {
        CompositePrimaryKey.TryParse(partitionKey, sortKey, out var result).ShouldBeFalse();

        result.ShouldBeNull();
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
        ["a#b", "c~"],
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
