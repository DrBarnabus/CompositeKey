namespace CompositeKey.SourceGeneration.Model;

public sealed record PropertySpec(
    TypeRef Type,
    string Name,
    string CamelCaseName,
    bool IsRequired,
    bool HasGetter,
    bool HasSetter,
    bool IsInitOnlySetter,
    EnumSpec? EnumSpec,
    CollectionType CollectionType = CollectionType.None);
