using Microsoft.CodeAnalysis;

namespace CompositeKey.SourceGeneration.Core;

/// <summary>
/// Descriptor for diagnostic instances using structural equality comparison.
/// </summary>
internal readonly struct DiagnosticInfo : IEquatable<DiagnosticInfo>
{
    public DiagnosticDescriptor Descriptor { get; private init; }
    public object?[] MessageArgs { get; private init; }
    public Location? Location { get; private init; }

    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location? location, object?[]? messageArgs)
    {
        return new DiagnosticInfo
        {
            Descriptor = descriptor,
            Location = location is null ? null : GetTrimmedLocation(location),
            MessageArgs = messageArgs ?? []
        };

        // Creates a copy of the Location instance that does not capture a reference to Compilation.
        static Location GetTrimmedLocation(Location location)
            => Location.Create(location.SourceTree?.FilePath ?? "", location.SourceSpan, location.GetLineSpan().Span);
    }

    public Diagnostic CreateDiagnostic() => Diagnostic.Create(Descriptor, Location, MessageArgs);

    public override bool Equals(object? obj) => obj is DiagnosticInfo info && Equals(info);

    public bool Equals(DiagnosticInfo other)
    {
        return Descriptor.Equals(other.Descriptor) &&
               MessageArgs.SequenceEqual(other.MessageArgs) &&
               Location == other.Location;
    }

    public override int GetHashCode()
    {
        int hashCode = Descriptor.GetHashCode();
        foreach (object? messageArg in MessageArgs)
        {
            hashCode = Combine(hashCode, messageArg?.GetHashCode() ?? 0);
        }

        hashCode = Combine(hashCode, Location?.GetHashCode() ?? 0);
        return hashCode;

        static int Combine(int h1, int h2)
        {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }
}
