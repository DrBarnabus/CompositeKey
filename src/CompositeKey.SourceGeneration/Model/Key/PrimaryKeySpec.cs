using CompositeKey.SourceGeneration.Core;

namespace CompositeKey.SourceGeneration.Model.Key;

public sealed record PrimaryKeySpec(
    ImmutableEquatableArray<KeyPart> Parts)
    : KeySpec;
