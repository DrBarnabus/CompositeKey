using System.Diagnostics.CodeAnalysis;

namespace CompositeKey;

public interface IPrimaryKey<TSelf> : IFormattable, ISpanParsable<TSelf> where TSelf : IPrimaryKey<TSelf>
{
    string ToString();

    string ToPartitionKeyString();

    static abstract TSelf Parse(string primaryKey);

    static abstract TSelf Parse(ReadOnlySpan<char> primaryKey);

    static abstract bool TryParse([NotNullWhen(true)] string? primaryKey, [MaybeNullWhen(false)] out TSelf result);

    static abstract bool TryParse(ReadOnlySpan<char> primaryKey, [MaybeNullWhen(false)] out TSelf result);
}
