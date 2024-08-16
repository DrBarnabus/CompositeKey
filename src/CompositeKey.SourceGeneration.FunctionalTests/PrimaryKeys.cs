namespace CompositeKey.SourceGeneration.FunctionalTests;

[CompositeKey("{First}#{Second}")]
public sealed partial record GuidOnlyPrimaryKey(Guid First, Guid Second);

[CompositeKey("{GuidValue}#{IntValue}~{StringValue}#{EnumValue}")]
public sealed partial record MixedTypePrimaryKey(Guid GuidValue, int IntValue, string StringValue, MixedTypePrimaryKey.EnumType EnumValue)
{
    public enum EnumType { One, Two, Three };
}

[CompositeKey("ConstantValue#{DynamicValue}@ConstantStringAtEndOfKey")]
public sealed partial record PrimaryKeyWithConstants(Guid DynamicValue);
