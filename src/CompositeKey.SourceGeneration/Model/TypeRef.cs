using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace CompositeKey.SourceGeneration.Model;

[DebuggerDisplay("Name = {Name}")]
public sealed class TypeRef : IEquatable<TypeRef>
{
    public ITypeSymbol TypeSymbol { get; }

    public string Name { get; }

    public string FullyQualifiedName { get; }

    public TypeRef(ITypeSymbol typeSymbol)
    {
        TypeSymbol = typeSymbol;
        Name = typeSymbol.Name;
        FullyQualifiedName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }

    public bool Equals(TypeRef? other) => other is not null && FullyQualifiedName == other.FullyQualifiedName;

    public override bool Equals(object? obj) => Equals(obj as TypeRef);

    public override int GetHashCode() => FullyQualifiedName.GetHashCode();
}
