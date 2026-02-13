namespace CompositeKey.Analyzers.Common.Tokenization;

public abstract record TemplateToken(TemplateToken.TemplateTokenType Type)
{
    public static TemplateToken PrimaryDelimiter(char value) => new PrimaryDelimiterTemplateToken(value);

    public static TemplateToken Delimiter(char value) => new DelimiterTemplateToken(value);

    public static TemplateToken Property(string name, string? format = null) => new PropertyTemplateToken(name, format);

    public static TemplateToken Constant(string value) => new ConstantTemplateToken(value);

    public static TemplateToken RepeatingProperty(string name, char separator, string? format = null) => new RepeatingPropertyTemplateToken(name, separator, format);

    public enum TemplateTokenType
    {
        PrimaryDelimiter,
        Delimiter,
        Property,
        Constant,
        RepeatingProperty
    }
}

public sealed record PrimaryDelimiterTemplateToken(char Value) : TemplateToken(TemplateTokenType.PrimaryDelimiter);

public sealed record DelimiterTemplateToken(char Value) : TemplateToken(TemplateTokenType.Delimiter);

public sealed record PropertyTemplateToken(string Name, string? Format = null) : TemplateToken(TemplateTokenType.Property);

public sealed record ConstantTemplateToken(string Value) : TemplateToken(TemplateTokenType.Constant);

public sealed record RepeatingPropertyTemplateToken(string Name, char Separator, string? Format = null) : TemplateToken(TemplateTokenType.RepeatingProperty);
