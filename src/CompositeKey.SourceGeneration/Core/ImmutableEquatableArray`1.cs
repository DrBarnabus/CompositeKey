using System.Collections;

namespace CompositeKey.SourceGeneration.Core;

/// <summary>
/// Provides an immutable list implementation which implements sequence equality.
/// </summary>
public sealed class ImmutableEquatableArray<T> : IEquatable<ImmutableEquatableArray<T>>, IReadOnlyList<T>
    where T : IEquatable<T>
{
    public static ImmutableEquatableArray<T> Empty { get; } = new(Array.Empty<T>());

    private readonly T[] _values;

    public T this[int index] => _values[index];

    public int Count => _values.Length;

    public ImmutableEquatableArray(IEnumerable<T> values) => _values = values.ToArray();

    public bool Equals(ImmutableEquatableArray<T>? other) => other != null && ((ReadOnlySpan<T>)_values).SequenceEqual(other._values);

    public override bool Equals(object? obj) => obj is ImmutableEquatableArray<T> other && Equals(other);

    public override int GetHashCode()
    {
        int hash = 0;
        foreach (var value in _values)
        {
            hash = Combine(hash, value.GetHashCode());
        }

        return hash;

        static int Combine(int h1, int h2)
        {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    public Enumerator GetEnumerator() => new(_values);

    [MustDisposeResource]
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _values.GetEnumerator();

    public struct Enumerator
    {
        private readonly T[] _values;
        private int _index;

        internal Enumerator(T[] values)
        {
            _values = values;
            _index = -1;
        }

        public bool MoveNext()
        {
            int newIndex = _index + 1;

            if ((uint)newIndex < (uint)_values.Length)
            {
                _index = newIndex;
                return true;
            }

            return false;
        }

        public readonly T Current => _values[_index];
    }
}

internal static class ImmutableEquatableArray
{
    public static ImmutableEquatableArray<T> ToImmutableEquatableArray<T>(this IEnumerable<T>? values)
        where T : IEquatable<T>
    {
        return values is null ? ImmutableEquatableArray<T>.Empty : new ImmutableEquatableArray<T>(values);
    }
}
