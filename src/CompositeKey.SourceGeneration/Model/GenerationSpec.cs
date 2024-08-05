using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Model;

public sealed record GenerationSpec(TargetTypeSpec TargetType, KeySpec Key);
