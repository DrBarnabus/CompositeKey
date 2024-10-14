using CompositeKey.SourceGeneration.Core;

namespace CompositeKey.SourceGeneration.Model;

public sealed record EnumSpec(
    string Name,
    string FullyQualifiedName,
    string UnderlyingType,
    ImmutableEquatableArray<EnumSpec.Member> Members,
    bool IsSequentialFromZero)
{
    public sealed record Member(string Name, object Value);
}
