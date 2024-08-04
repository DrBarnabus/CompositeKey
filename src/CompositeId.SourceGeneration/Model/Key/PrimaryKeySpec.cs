using CompositeId.SourceGeneration.Core;

namespace CompositeId.SourceGeneration.Model.Key;

public sealed record PrimaryKeySpec(
    ImmutableEquatableArray<KeyPart> Parts)
    : KeySpec;
