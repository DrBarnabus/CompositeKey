using System.Diagnostics.CodeAnalysis;

namespace CompositeKey;

/// <summary>
/// Implemented by the CompositeKey source generator for primary keys annotated with <see cref="CompositeKeyAttribute"/>.
/// </summary>
/// <typeparam name="TSelf">A self reference to the type implementing this interface.</typeparam>
public interface IPrimaryKey<TSelf> : IFormattable, ISpanParsable<TSelf> where TSelf : IPrimaryKey<TSelf>
{
    /// <summary>
    /// Formats the full key of the current instance to a string.
    /// </summary>
    /// <returns>The full key of the current instance formatted to a string.</returns>
    string ToString();

    /// <summary>
    /// Formats the partition key portion of the current instance to a string.
    /// </summary>
    /// <returns>The partition key portion of the current instance formatted to a string.</returns>
    string ToPartitionKeyString();

    /// <summary>
    /// Parses a string into a <see cref="TSelf"/> instance.
    /// </summary>
    /// <param name="primaryKey">The string to parse.</param>
    /// <exception cref="T:System.ArgumentNullException"><paramref name="primaryKey"/> is null.</exception>
    /// <exception cref="T:System.FormatException"><paramref name="primaryKey"/> is not in the correct format.</exception>
    /// <returns>The result of parsing <paramref name="primaryKey"/>.</returns>
    static abstract TSelf Parse(string primaryKey);

    /// <summary>
    /// Parses a span of characters into a <see cref="TSelf"/> instance.
    /// </summary>
    /// <param name="primaryKey">The span of characters to parse.</param>
    /// <exception cref="T:System.FormatException"><paramref name="primaryKey"/> is not in the correct format.</exception>
    /// <returns>The result of parsing <paramref name="primaryKey"/>.</returns>
    static abstract TSelf Parse(ReadOnlySpan<char> primaryKey);

    /// <summary>
    /// Tries to parse a string into a <see cref="TSelf"/> instance.
    /// </summary>
    /// <param name="primaryKey">The string to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="primaryKey"/>,
    /// or an undefined value on failure.</param>
    /// <returns><see langword="true"/> if <paramref name="primaryKey"/> was successfully parsed; otherwise, false.</returns>
    static abstract bool TryParse([NotNullWhen(true)] string? primaryKey, [MaybeNullWhen(false)] out TSelf result);

    /// <summary>
    /// Tries to parse a span of characters into a <see cref="TSelf"/> instance.
    /// </summary>
    /// <param name="primaryKey">The span of characters to parse.</param>
    /// <param name="result">When this method returns, contains the result of successfully parsing <paramref name="primaryKey"/>,
    /// or an undefined value on failure.</param>
    /// <returns><see langword="true"/> if <paramref name="primaryKey"/> was successfully parsed; otherwise, false.</returns>
    static abstract bool TryParse(ReadOnlySpan<char> primaryKey, [MaybeNullWhen(false)] out TSelf result);
}
