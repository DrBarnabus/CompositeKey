namespace CompositeId.SourceGeneration.Core.Extensions;

public static class StringExtensions
{
    public static string FirstToUpperInvariant(this string value)
    {
        char[] chars = value.ToCharArray();
        chars[0] = char.ToUpperInvariant(chars[0]);
        return new string(chars);
    }

    public static string FirstToLowerInvariant(this string value)
    {
        char[] chars = value.ToCharArray();
        chars[0] = char.ToLowerInvariant(chars[0]);
        return new string(chars);
    }
}
