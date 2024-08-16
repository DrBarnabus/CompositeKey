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
