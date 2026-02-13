using AutoFixture.Xunit2;

namespace CompositeKey.SourceGeneration.FunctionalTests;

public static class CompositePrimaryKeyTests
{
    #region CompositePrimaryKey

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

    [Theory]
    [InlineAutoData(0, false)]
    [InlineAutoData(0, true)]
    [InlineAutoData(1, false)]
    public static void CompositePrimaryKey_ToPartitionKeyString_WithSpecificPartIndexAndDelimiterRequirements_ShouldReturnCorrectlyFormattedString(
            int throughPartIndex, bool includeTrailingDelimiter, CompositePrimaryKey compositeKey)
    {
        (var guidValue, decimal decimalValue, _, _) = compositeKey;

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
            case 1:
                result.ShouldBe($"{guidValue}#{decimalValue}");
                break;
        }
    }

    [Theory]
    [InlineAutoData(1, true)]
    [InlineAutoData(2, false)]
    public static void CompositePrimaryKey_ToPartitionKeyString_WithInvalidPartIndexOrDelimiterRequirements_ShouldThrowInvalidOperationException(
        int throughPartIndex, bool includeTrailingDelimiter, CompositePrimaryKey compositeKey)
    {
        var act = () => compositeKey.ToPartitionKeyString(throughPartIndex, includeTrailingDelimiter);
        act.ShouldThrow<InvalidOperationException>();
    }

    [Theory, AutoData]
    public static void CompositePrimaryKey_ToSortKeyString_ShouldReturnCorrectlyFormattedString(CompositePrimaryKey compositeKey)
    {
        (_, _, var enumValue, string stringValue) = compositeKey;

        string result = compositeKey.ToSortKeyString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"Constant~{enumValue}@{stringValue}");
    }

    [Theory]
    [InlineAutoData(0, false)]
    [InlineAutoData(0, true)]
    [InlineAutoData(1, false)]
    [InlineAutoData(1, true)]
    [InlineAutoData(2, false)]
    public static void CompositePrimaryKey_ToSortKeyString_WithSpecificPartIndexAndDelimiterRequirements_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter, CompositePrimaryKey compositeKey)
    {
        (_, _, var enumValue, string stringValue) = compositeKey;

        string result = compositeKey.ToSortKeyString(throughPartIndex, includeTrailingDelimiter);

        result.ShouldNotBeNullOrEmpty();

        switch (throughPartIndex)
        {
            case 0 when !includeTrailingDelimiter:
                result.ShouldBe($"Constant");
                break;
            case 0 when includeTrailingDelimiter:
                result.ShouldBe($"Constant~");
                break;
            case 1 when !includeTrailingDelimiter:
                result.ShouldBe($"Constant~{enumValue}");
                break;
            case 1 when includeTrailingDelimiter:
                result.ShouldBe($"Constant~{enumValue}@");
                break;
            case 2:
                result.ShouldBe($"Constant~{enumValue}@{stringValue}");
                break;
        }
    }

    [Theory]
    [InlineAutoData(2, true)]
    [InlineAutoData(3, false)]
    public static void CompositePrimaryKey_ToSortKeyString_WithInvalidPartIndexOrDelimiterRequirements_ShouldThrowInvalidOperationException(
        int throughPartIndex, bool includeTrailingDelimiter, CompositePrimaryKey compositeKey)
    {
        var act = () => compositeKey.ToSortKeyString(throughPartIndex, includeTrailingDelimiter);
        act.ShouldThrow<InvalidOperationException>();
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

    #endregion

    #region CompositePrimaryKeyWithSamePropertyUsedTwice

    [Theory, AutoData]
    public static void CompositePrimaryKeyWithSamePropertyUsedTwice_RoundTripToStringAndParse_ShouldResultInEquivalentKey(CompositePrimaryKeyWithSamePropertyUsedTwice compositeKey)
    {
        var result = CompositePrimaryKeyWithSamePropertyUsedTwice.Parse(compositeKey.ToString());

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(compositeKey);
    }

    [Theory, AutoData]
    public static void CompositePrimaryKeyWithSamePropertyUsedTwice_ToString_ShouldReturnCorrectlyFormattedString(CompositePrimaryKeyWithSamePropertyUsedTwice compositeKey)
    {
        var id = compositeKey.Id;

        string result = compositeKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{id}#{id}");
    }

    [Theory, AutoData]
    public static void CompositePrimaryKeyWithSamePropertyUsedTwice_Parse_WithValidPrimaryKey_ShouldReturnCorrectlyParsedRecord(CompositePrimaryKeyWithSamePropertyUsedTwice compositeKey)
    {
        var id = compositeKey.Id;

        var result = CompositePrimaryKeyWithSamePropertyUsedTwice.Parse($"{id}#{id}");

        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
    }

    [Theory, MemberData(nameof(CompositePrimaryKeyWithSamePropertyUsedTwice_InvalidPrimaryKeys))]
    public static void CompositePrimaryKeyWithSamePropertyUsedTwice_Parse_WithInvalidPrimaryKey_ShouldThrowFormatException(string primaryKey)
    {
        var act = () => CompositePrimaryKeyWithSamePropertyUsedTwice.Parse(primaryKey);
        act.ShouldThrow<FormatException>();
    }

    [Theory, AutoData]
    public static void CompositePrimaryKeyWithSamePropertyUsedTwice_TryParse_WithValidPrimaryKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord(CompositePrimaryKeyWithSamePropertyUsedTwice compositeKey)
    {
        var id = compositeKey.Id;

        CompositePrimaryKeyWithSamePropertyUsedTwice.TryParse($"{id}#{id}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.Id.ShouldBe(id);
    }

    [Theory, MemberData(nameof(CompositePrimaryKeyWithSamePropertyUsedTwice_InvalidPrimaryKeys))]
    public static void CompositePrimaryKeyWithSamePropertyUsedTwice_TryParse_WithInvalidPrimaryKey_ShouldReturnFalse(string primaryKey)
    {
        CompositePrimaryKeyWithSamePropertyUsedTwice.TryParse(primaryKey, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    public static object[][] CompositePrimaryKeyWithSamePropertyUsedTwice_InvalidPrimaryKeys() =>
    [
        ["a"],
        ["a#b"],
        ["#"],
        ["a#"],
        ["#a"],
        [""],
        ["invalid-guid#invalid-guid"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#different-guid"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#eccd98c4-d484-4429-896d-8fcdd77c6328"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327#eccd98c4-d484-4429-896d-8fcdd77c6327#extra"]
    ];

    #endregion

    #region CompositeWithRepeatingSort

    [Fact]
    public static void CompositeWithRepeatingSort_RoundTripToStringAndParse_ShouldResultInEquivalentKey()
    {
        var compositeKey = new CompositeWithRepeatingSort(Guid.NewGuid(), [Guid.NewGuid(), Guid.NewGuid()]);

        var result = CompositeWithRepeatingSort.Parse(compositeKey.ToString());

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(compositeKey.TenantId);
        result.LocationId.ShouldBe(compositeKey.LocationId);
    }

    [Fact]
    public static void CompositeWithRepeatingSort_ToString_ShouldReturnCorrectlyFormattedString()
    {
        var tenantId = Guid.NewGuid();
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var compositeKey = new CompositeWithRepeatingSort(tenantId, ids);

        string result = compositeKey.ToString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{tenantId}|LOCATION#{ids[0]}#{ids[1]}");
    }

    [Fact]
    public static void CompositeWithRepeatingSort_ToPartitionKeyString_ShouldReturnCorrectlyFormattedString()
    {
        var tenantId = Guid.NewGuid();
        var compositeKey = new CompositeWithRepeatingSort(tenantId, [Guid.NewGuid()]);

        string result = compositeKey.ToPartitionKeyString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"{tenantId}");
    }

    [Fact]
    public static void CompositeWithRepeatingSort_ToSortKeyString_ShouldReturnCorrectlyFormattedString()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var compositeKey = new CompositeWithRepeatingSort(Guid.NewGuid(), ids);

        string result = compositeKey.ToSortKeyString();

        result.ShouldNotBeNullOrEmpty();
        result.ShouldBe($"LOCATION#{ids[0]}#{ids[1]}");
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [InlineData(2, false)]
    public static void CompositeWithRepeatingSort_ToSortKeyString_WithSpecificPartIndex_ShouldReturnCorrectlyFormattedString(
        int throughPartIndex, bool includeTrailingDelimiter)
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var compositeKey = new CompositeWithRepeatingSort(Guid.NewGuid(), ids);

        string result = compositeKey.ToSortKeyString(throughPartIndex, includeTrailingDelimiter);

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

    [Fact]
    public static void CompositeWithRepeatingSort_Parse_WithValidPrimaryKey_ShouldReturnCorrectlyParsedRecord()
    {
        var tenantId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var result = CompositeWithRepeatingSort.Parse($"{tenantId}|LOCATION#{id1}#{id2}");

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Theory, MemberData(nameof(CompositeWithRepeatingSort_InvalidPrimaryKeys))]
    public static void CompositeWithRepeatingSort_Parse_WithInvalidPrimaryKey_ShouldThrowFormatException(string primaryKey)
    {
        var act = () => CompositeWithRepeatingSort.Parse(primaryKey);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void CompositeWithRepeatingSort_Parse_WithValidPartitionKeyAndSortKey_ShouldReturnCorrectlyParsedRecord()
    {
        var tenantId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var result = CompositeWithRepeatingSort.Parse($"{tenantId}", $"LOCATION#{id1}#{id2}");

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Theory, MemberData(nameof(CompositeWithRepeatingSort_InvalidPartitionKeyAndSortKeys))]
    public static void CompositeWithRepeatingSort_Parse_WithInvalidPartitionKeyAndSortKey_ShouldThrowFormatException(string partitionKey, string sortKey)
    {
        var act = () => CompositeWithRepeatingSort.Parse(partitionKey, sortKey);
        act.ShouldThrow<FormatException>();
    }

    [Fact]
    public static void CompositeWithRepeatingSort_TryParse_WithValidPrimaryKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        var tenantId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        CompositeWithRepeatingSort.TryParse($"{tenantId}|LOCATION#{id1}#{id2}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Theory, MemberData(nameof(CompositeWithRepeatingSort_InvalidPrimaryKeys))]
    public static void CompositeWithRepeatingSort_TryParse_WithInvalidPrimaryKey_ShouldReturnFalse(string primaryKey)
    {
        CompositeWithRepeatingSort.TryParse(primaryKey, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    [Fact]
    public static void CompositeWithRepeatingSort_TryParse_WithValidPartitionKeyAndSortKey_ShouldReturnTrueAndOutputCorrectlyParsedRecord()
    {
        var tenantId = Guid.NewGuid();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        CompositeWithRepeatingSort.TryParse($"{tenantId}", $"LOCATION#{id1}#{id2}", out var result).ShouldBeTrue();

        result.ShouldNotBeNull();
        result.TenantId.ShouldBe(tenantId);
        result.LocationId.Count.ShouldBe(2);
        result.LocationId[0].ShouldBe(id1);
        result.LocationId[1].ShouldBe(id2);
    }

    [Theory, MemberData(nameof(CompositeWithRepeatingSort_InvalidPartitionKeyAndSortKeys))]
    public static void CompositeWithRepeatingSort_TryParse_WithInvalidPartitionKeyAndSortKey_ShouldReturnFalse(string partitionKey, string sortKey)
    {
        CompositeWithRepeatingSort.TryParse(partitionKey, sortKey, out var result).ShouldBeFalse();

        result.ShouldBeNull();
    }

    public static object[][] CompositeWithRepeatingSort_InvalidPrimaryKeys() =>
    [
        [""],
        ["a"],
        ["a|b"],
        ["not-a-guid|LOCATION#not-a-guid"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327|WRONG#eccd98c4-d484-4429-896d-8fcdd77c6328"]
    ];

    public static object[][] CompositeWithRepeatingSort_InvalidPartitionKeyAndSortKeys() =>
    [
        ["a", "b"],
        ["not-a-guid", "LOCATION#not-a-guid"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327", "WRONG#eccd98c4-d484-4429-896d-8fcdd77c6328"],
        ["eccd98c4-d484-4429-896d-8fcdd77c6327", "LOCATION#not-a-guid"]
    ];

    #endregion
}
