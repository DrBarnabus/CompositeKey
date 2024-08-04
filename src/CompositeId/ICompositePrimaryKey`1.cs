using System.Diagnostics.CodeAnalysis;

namespace CompositeId;

public interface ICompositePrimaryKey<TSelf> : IPrimaryKey<TSelf> where TSelf : ICompositePrimaryKey<TSelf>
{
    string ToSortKeyString();

    static abstract TSelf Parse(string partitionKey, string sortKey);

    static abstract TSelf Parse(ReadOnlySpan<char> partitionKey, ReadOnlySpan<char> sortKey);

    static abstract bool TryParse([NotNullWhen(true)] string? partitionKey, [NotNullWhen(true)] string? sortKey, [MaybeNullWhen(false)] out TSelf result);

    static abstract bool TryParse(ReadOnlySpan<char> partitionKey, ReadOnlySpan<char> sortKey, [MaybeNullWhen(false)] out TSelf result);
}
