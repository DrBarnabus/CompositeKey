namespace CompositeKey.SourceGeneration.Model.Key;

public sealed record RepeatingPropertyKeyPart(
    PropertySpec Property,
    char Separator,
    string? Format,
    ParseType InnerParseType,
    FormatType InnerFormatType,
    TypeRef InnerType) : ValueKeyPart;
