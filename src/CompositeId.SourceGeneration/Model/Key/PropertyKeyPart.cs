namespace CompositeId.SourceGeneration.Model.Key;

public sealed record PropertyKeyPart(PropertySpec Property, string? Format, ParseType ParseType, FormatType FormatType)
    : ValueKeyPart;
