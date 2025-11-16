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
    /// Formats the sort key portion of the current instance to a string through the specified index.
    /// </summary>
    /// <param name="throughPartIndex">The zero-based index of the key part to format through (inclusive). This counts only properties and constants, not delimiters.</param>
    /// <param name="includeTrailingDelimiter">Whether to include the following delimiter character in the formatted string, defaults to true.</param>
    /// <returns>The sort key portion of the current instance up to the specified index formatted to a string.</returns>
    /// <example>
    /// For a key with template <c>"{Country}#{County}#{Locality}"</c> and values "UK", "Derbyshire" and "Matlock":
    /// <code>
    /// key.ToSortKeyString(0); // Returns "UK#"
    /// key.ToSortKeyString(1, false); // Returns "UK#Derbyshire"
    /// key.ToSortKeyString(1, true); // Returns "UK#Derbyshire#"
    /// </code>
    /// </example>
    string ToSortKeyString(int throughPartIndex, bool includeTrailingDelimiter = true);

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
