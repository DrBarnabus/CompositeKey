namespace CompositeKey.SourceGeneration.Model.Key;

public sealed record PropertyKeyPart(
    PropertySpec Property,
    string? Format,
    PropertyTypeDescriptor TypeDescriptor,
    CollectionSemantics? CollectionSemantics = null) : ValueKeyPart;
