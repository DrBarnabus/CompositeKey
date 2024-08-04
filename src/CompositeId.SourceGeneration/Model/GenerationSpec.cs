using CompositeId.SourceGeneration.Model.Key;

namespace CompositeId.SourceGeneration.Model;

public sealed record GenerationSpec(TargetTypeSpec TargetType, KeySpec Key);
