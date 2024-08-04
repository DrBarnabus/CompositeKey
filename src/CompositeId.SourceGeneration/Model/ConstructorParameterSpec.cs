namespace CompositeId.SourceGeneration.Model;

public sealed record ConstructorParameterSpec(
    TypeRef Type,
    string Name,
    string CamelCaseName,
    int ParameterIndex);
