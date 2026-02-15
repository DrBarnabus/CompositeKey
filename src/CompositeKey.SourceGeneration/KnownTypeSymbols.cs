using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace CompositeKey.SourceGeneration;

internal sealed class KnownTypeSymbols(Compilation compilation)
{
    private Option<INamedTypeSymbol?> _compositeKeyAttributeType;
    private Option<INamedTypeSymbol?> _compositeKeyConstructorAttributeType;
    private Option<INamedTypeSymbol?> _setsRequiredMembersAttributeType;
    private Option<INamedTypeSymbol?> _guidType;
    private Option<INamedTypeSymbol?> _stringType;
    private Option<INamedTypeSymbol?> _listType;
    private Option<INamedTypeSymbol?> _readOnlyListType;
    private Option<INamedTypeSymbol?> _immutableArrayType;

    public Compilation Compilation { get; } = compilation;

    public INamedTypeSymbol? CompositeKeyAttributeType => GetOrResolveType(typeof(CompositeKeyAttribute), ref _compositeKeyAttributeType);

    public INamedTypeSymbol? CompositeKeyConstructorAttributeType => GetOrResolveType(typeof(CompositeKeyConstructorAttribute), ref _compositeKeyConstructorAttributeType);

    public INamedTypeSymbol? SetsRequiredMembersAttributeType => GetOrResolveType(typeof(SetsRequiredMembersAttribute), ref _setsRequiredMembersAttributeType);

    public INamedTypeSymbol? GuidType => GetOrResolveType(typeof(Guid), ref _guidType);

    public INamedTypeSymbol? StringType => GetOrResolveType(typeof(string), ref _stringType);

    public INamedTypeSymbol? ListType => GetOrResolveType("System.Collections.Generic.List`1", ref _listType);

    public INamedTypeSymbol? ReadOnlyListType => GetOrResolveType("System.Collections.Generic.IReadOnlyList`1", ref _readOnlyListType);

    public INamedTypeSymbol? ImmutableArrayType => GetOrResolveType("System.Collections.Immutable.ImmutableArray`1", ref _immutableArrayType);

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
