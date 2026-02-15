namespace CompositeKey.SourceGeneration.FunctionalTests;

[CompositeKey("{GuidValue}#{DecimalValue}|Constant~{EnumValue}@{StringValue}", PrimaryKeySeparator = '|')]
public sealed partial record CompositePrimaryKey(
    Guid GuidValue,
    decimal DecimalValue,
    CompositePrimaryKey.EnumType EnumValue,
    string StringValue)
{
    public enum EnumType { One, Two, Three };
}

[CompositeKey("{Id}#{Id}", PrimaryKeySeparator = '#')]
public sealed partial record CompositePrimaryKeyWithSamePropertyUsedTwice(Guid Id);

[CompositeKey("{TenantId}|LOCATION#{LocationId...#}", PrimaryKeySeparator = '|')]
public sealed partial record CompositeWithRepeatingSort(Guid TenantId, IReadOnlyList<Guid> LocationId);
