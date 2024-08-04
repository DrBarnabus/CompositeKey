using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace CompositeId.SourceGeneration;

internal sealed class KnownTypeSymbols(Compilation compilation)
{
    private Option<INamedTypeSymbol?> _compositeIdAttributeType;
    private Option<INamedTypeSymbol?> _setsRequiredMembersAttributeType;
    private Option<INamedTypeSymbol?> _guidType;
    private Option<INamedTypeSymbol?> _stringType;

    public Compilation Compilation { get; } = compilation;

    public INamedTypeSymbol? CompositeIdAttributeType => GetOrResolveType(typeof(CompositeIdAttribute), ref _compositeIdAttributeType);

    public INamedTypeSymbol? SetsRequiredMembersAttributeType => GetOrResolveType(typeof(SetsRequiredMembersAttribute), ref _setsRequiredMembersAttributeType);

    public INamedTypeSymbol? GuidType => GetOrResolveType(typeof(Guid), ref _guidType);

    public INamedTypeSymbol? StringType => GetOrResolveType(typeof(string), ref _stringType);

    private INamedTypeSymbol? GetOrResolveType(Type type, ref Option<INamedTypeSymbol?> field) => GetOrResolveType(type.FullName!, ref field);

    private INamedTypeSymbol? GetOrResolveType(string fullyQualifiedName, ref Option<INamedTypeSymbol?> field)
    {
        if (field.HasValue)
            return field.Value;

        var type = Compilation.GetTypeByMetadataName(fullyQualifiedName);
        field = new Option<INamedTypeSymbol?>(type);

        return type;
    }

    private readonly struct Option<T>
    {
        public readonly bool HasValue;
        public readonly T Value;

        public Option(T value)
        {
            HasValue = true;
            Value = value;
        }
    }
}
