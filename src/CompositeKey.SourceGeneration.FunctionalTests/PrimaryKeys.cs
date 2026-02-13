using System.Collections.Immutable;

namespace CompositeKey.SourceGeneration.FunctionalTests;

[CompositeKey("{First}#{Second}")]
public sealed partial record GuidOnlyPrimaryKey(Guid First, Guid Second);

[CompositeKey("{GuidValue}#{IntValue}~{StringValue}#{EnumValue}")]
[method: CompositeKeyConstructor]
public sealed partial record MixedTypePrimaryKey(Guid GuidValue, int IntValue, string StringValue, MixedTypePrimaryKey.EnumType EnumValue)
{
    // ReSharper disable once UnusedMember.Global
    public MixedTypePrimaryKey() : this(Guid.NewGuid(), 0, string.Empty, EnumType.One)
    {
    }

    public enum EnumType { One, Two, Three };
}

[CompositeKey("ConstantValue#{DynamicValue}@ConstantStringAtEndOfKey")]
public sealed partial record PrimaryKeyWithConstants(Guid DynamicValue);

[CompositeKey("{GuidValue}#Constant#{EnumValue}@{StringValue}")]
public sealed partial record PrimaryKeyWithFastPathFormatting(Guid GuidValue, PrimaryKeyWithFastPathFormatting.EnumType EnumValue, string StringValue)
{
    public enum EnumType { One, Two, Three, Four, Five, Six, Seven, Eight, Nine };
}

[CompositeKey("{LocationId...#}")]
public sealed partial record RepeatingGuidPrimaryKey(IReadOnlyList<Guid> LocationId);

[CompositeKey("LOCATION#{LocationId...#}")]
public sealed partial record HierarchicalLocationKey(IReadOnlyList<Guid> LocationId);

[CompositeKey("{Type}#{Tags...,}")]
public sealed partial record TaggedEntityKey(string Type, List<string> Tags);

[CompositeKey("{TenantId}#{LocationId:D...#}")]
public sealed partial record FastPathRepeatingKey(Guid TenantId, IReadOnlyList<Guid> LocationId);
