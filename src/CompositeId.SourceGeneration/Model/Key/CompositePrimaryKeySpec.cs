using CompositeId.SourceGeneration.Core;

namespace CompositeId.SourceGeneration.Model.Key;

public sealed record CompositePrimaryKeySpec(
    ImmutableEquatableArray<KeyPart> AllParts,
    ImmutableEquatableArray<KeyPart> PartitionKeyParts,
    PrimaryDelimiterKeyPart PrimaryDelimiterKeyPart,
    ImmutableEquatableArray<KeyPart> SortKeyParts)
    : KeySpec;
