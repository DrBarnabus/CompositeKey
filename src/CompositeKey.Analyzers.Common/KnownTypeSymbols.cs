using Microsoft.CodeAnalysis;

namespace CompositeKey.Analyzers.Common;

public sealed class KnownTypeSymbols(Compilation compilation)
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

    public INamedTypeSymbol? CompositeKeyAttributeType =>
        GetOrResolveType("CompositeKey.CompositeKeyAttribute", ref _compositeKeyAttributeType);

    public INamedTypeSymbol? CompositeKeyConstructorAttributeType =>
        GetOrResolveType("CompositeKey.CompositeKeyConstructorAttribute", ref _compositeKeyConstructorAttributeType);

    public INamedTypeSymbol? SetsRequiredMembersAttributeType =>
        GetOrResolveType("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute", ref _setsRequiredMembersAttributeType);

    public INamedTypeSymbol? GuidType =>
        GetOrResolveType("System.Guid", ref _guidType);

    public INamedTypeSymbol? StringType =>
        GetOrResolveType("System.String", ref _stringType);

    public INamedTypeSymbol? ListType =>
        GetOrResolveType("System.Collections.Generic.List`1", ref _listType);

    public INamedTypeSymbol? ReadOnlyListType =>
        GetOrResolveType("System.Collections.Generic.IReadOnlyList`1", ref _readOnlyListType);

    public INamedTypeSymbol? ImmutableArrayType =>
        GetOrResolveType("System.Collections.Immutable.ImmutableArray`1", ref _immutableArrayType);

    private INamedTypeSymbol? GetOrResolveType(string fullyQualifiedName, ref Option<INamedTypeSymbol?> field)
    {
        if (field.HasValue)
            return field.Value;

        var type = Compilation.GetTypeByMetadataName(fullyQualifiedName);
        field = new Option<INamedTypeSymbol?>(type);

        return type;
    }

    private struct Option<T>
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
