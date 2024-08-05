namespace CompositeKey.SourceGeneration.Model;

public sealed record PropertyInitializerSpec(
    TypeRef PropertyType,
    string Name,
    string CamelCaseName,
    int ParameterIndex,
    bool MatchesConstructorParameter);
