using CompositeKey.SourceGeneration.Core;

namespace CompositeKey.SourceGeneration.Model.Key;

public sealed record PrimaryKeySpec(
    bool InvariantFormatting,
    ImmutableEquatableArray<KeyPart> Parts)
    : KeySpec(InvariantFormatting);
