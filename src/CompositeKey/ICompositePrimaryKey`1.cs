using System.Diagnostics.CodeAnalysis;

namespace CompositeKey;

/// <summary>
/// Implemented by the CompositeKey source generator for composite primary keys annotated with <see cref="CompositeKeyAttribute"/>.
/// </summary>
/// <typeparam name="TSelf">A self reference to the type implementing this interface.</typeparam>
public interface ICompositePrimaryKey<TSelf> : IPrimaryKey<TSelf> where TSelf : ICompositePrimaryKey<TSelf>
{
    /// <summary>
    /// Formats the sort key portion of the current instance to a string.
    /// </summary>
    /// <returns>The sort key portion of the current instance formatted to a string.</returns>
    string ToSortKeyString();

    /// <summary>
    /// Parses both partition key and sort key together as strings into a <see cref="TSelf"/> instance.
    /// </summary>
    /// <param name="partitionKey">The partition key string to parse.</param>
    /// <param name="sortKey">The sort key string to parse.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="partitionKey"/> or <paramref name="sortKey"/> is null.</exception>
    /// <exception cref="T:System.FormatException"><paramref name="partitionKey"/> or <paramref name="sortKey"/> is not in the correct format.</exception>
    /// <returns>The result of parsing <paramref name="partitionKey"/> and <paramref name="sortKey"/>.</returns>
    static abstract TSelf Parse(string partitionKey, string sortKey);

    /// <summary>
    /// Parses both partition key and sort key together as spans of characters into a <see cref="TSelf"/> instance.
    /// </summary>
    /// <param name="partitionKey">The span of partition key characters to parse.</param>
    /// <param name="sortKey">The span of sort key characters to parse.</param>
    /// <exception cref="T:System.FormatException"><paramref name="partitionKey"/> or <paramref name="sortKey"/> is not in the correct format.</exception>
    /// <returns>The result of parsing <paramref name="partitionKey"/> and <paramref name="sortKey"/>.</returns>
    static abstract TSelf Parse(ReadOnlySpan<char> partitionKey, ReadOnlySpan<char> sortKey);

    /// <summary>
    /// Tries to parse both partition key and sort key together as strings into a <see cref="TSelf"/> instance.
    /// </summary>
    /// <param name="partitionKey">The partition key string to parse.</param>
    /// <param name="sortKey">The sort key string to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="partitionKey"/>
    /// and <paramref name="sortKey"/>, or an undefined value on failure.</param>
    /// <returns><see langword="true"/> if <paramref name="partitionKey"/> and <paramref name="sortKey"/> was successfully parsed; otherwise, false.</returns>
    static abstract bool TryParse([NotNullWhen(true)] string? partitionKey, [NotNullWhen(true)] string? sortKey, [MaybeNullWhen(false)] out TSelf result);

    /// <summary>
    /// Tries to parse both partition key and sort key together as spans of characters into a <see cref="TSelf"/> instance.
    /// </summary>
    /// <param name="partitionKey">The span of partition key characters to parse.</param>
    /// <param name="sortKey">The span of sort key characters to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="partitionKey"/>
    /// and <paramref name="sortKey"/>, or an undefined value on failure.</param>
    /// <returns><see langword="true"/> if <paramref name="partitionKey"/> and <paramref name="sortKey"/> was successfully parsed; otherwise, false.</returns>
    static abstract bool TryParse(ReadOnlySpan<char> partitionKey, ReadOnlySpan<char> sortKey, [MaybeNullWhen(false)] out TSelf result);
}
