using System.Diagnostics.CodeAnalysis;
using System.Text;
using CompositeKey.Analyzers.Common.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompositeKey.Analyzers.Common.Validation;

public static class TypeValidation
{
    public static TypeValidationResult ValidateTypeForCompositeKey(
        INamedTypeSymbol targetTypeSymbol,
        TypeDeclarationSyntax typeDeclarationSyntax,
        SemanticModel semanticModel,
        INamedTypeSymbol? compositeKeyConstructorAttributeType,
        CancellationToken cancellationToken)
    {
        if (!targetTypeSymbol.IsRecord)
        {
            return TypeValidationResult.Failure(DiagnosticDescriptors.UnsupportedCompositeType, targetTypeSymbol.Name);
        }

        if (!TryGetTargetTypeDeclarations(typeDeclarationSyntax, semanticModel, out var targetTypeDeclarations, cancellationToken))
        {
            return TypeValidationResult.Failure(DiagnosticDescriptors.CompositeTypeMustBePartial, targetTypeSymbol.Name);
        }

        if (!TryGetObviousOrExplicitlyMarkedConstructor(targetTypeSymbol, compositeKeyConstructorAttributeType, out var constructor))
        {
            return TypeValidationResult.Failure(DiagnosticDescriptors.NoObviousDefaultConstructor, targetTypeSymbol.Name);
        }

        return TypeValidationResult.Success(constructor, targetTypeDeclarations);
    }

    public static bool TryGetObviousOrExplicitlyMarkedConstructor(
        INamedTypeSymbol typeSymbol,
        INamedTypeSymbol? compositeKeyConstructorAttributeType,
        [NotNullWhen(true)] out IMethodSymbol? constructor)
    {
        constructor = null;

        var publicConstructors = typeSymbol.Constructors
            .Where(c => !c.IsStatic && !(c.IsImplicitlyDeclared && typeSymbol.IsValueType && c.Parameters.Length == 0))
            .Where(c => !(c.Parameters.Length == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, typeSymbol)))
            .ToArray();

        var lonePublicConstructor = publicConstructors.Length == 1 ? publicConstructors[0] : null;
        IMethodSymbol? constructorWithAttribute = null;
        IMethodSymbol? publicParameterlessConstructor = null;

        foreach (var ctor in publicConstructors)
        {
            if (compositeKeyConstructorAttributeType != null &&
                ctor.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, compositeKeyConstructorAttributeType)))
            {
                if (constructorWithAttribute is not null)
                    return false; // Multiple constructors with attribute

                constructorWithAttribute = ctor;
            }
            else if (ctor.Parameters.Length == 0)
            {
                publicParameterlessConstructor = ctor;
            }
        }

        constructor = constructorWithAttribute ?? publicParameterlessConstructor ?? lonePublicConstructor;
        return constructor is not null;
    }

    private static bool TryGetTargetTypeDeclarations(
        TypeDeclarationSyntax typeDeclarationSyntax,
        SemanticModel semanticModel,
        [NotNullWhen(true)] out List<string>? targetTypeDeclarations,
        CancellationToken cancellationToken)
    {
        targetTypeDeclarations = null;

        for (var current = typeDeclarationSyntax; current != null; current = current.Parent as TypeDeclarationSyntax)
        {
            var stringBuilder = new StringBuilder();

            bool isPartialType = false;
            foreach (var modifier in current.Modifiers)
            {
                stringBuilder.Append(modifier.Text);
                stringBuilder.Append(' ');

                isPartialType |= modifier.IsKind(SyntaxKind.PartialKeyword);
            }

            if (!isPartialType)
                return false;

            stringBuilder.Append(GetTypeKindKeyword(current));
            stringBuilder.Append(' ');

            var typeSymbol = semanticModel.GetDeclaredSymbol(current, cancellationToken);
            if (typeSymbol is null)
                return false;

            stringBuilder.Append(typeSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));

            (targetTypeDeclarations ??= []).Add(stringBuilder.ToString());
        }

        return targetTypeDeclarations?.Count > 0;
    }

    private static string GetTypeKindKeyword(TypeDeclarationSyntax typeDeclarationSyntax) =>
        typeDeclarationSyntax.Kind() switch
        {
            SyntaxKind.ClassDeclaration => "class",
            SyntaxKind.InterfaceDeclaration => "interface",
            SyntaxKind.StructDeclaration => "struct",
            SyntaxKind.RecordDeclaration => "record",
            SyntaxKind.RecordStructDeclaration => "record struct",
            SyntaxKind.EnumDeclaration => "enum",
            SyntaxKind.DelegateDeclaration => "delegate",
            _ => throw new ArgumentOutOfRangeException(nameof(typeDeclarationSyntax))
        };
}

public record TypeValidationResult
{
    [MemberNotNullWhen(true, nameof(Constructor), nameof(TargetTypeDeclarations))]
    [MemberNotNullWhen(false, nameof(Descriptor), nameof(MessageArgs))]
    public required bool IsSuccess { get; init; }

    public DiagnosticDescriptor? Descriptor { get; init; }
    public object?[]? MessageArgs { get; init; }
    public IMethodSymbol? Constructor { get; init; }
    public IReadOnlyList<string>? TargetTypeDeclarations { get; init; }

    public static TypeValidationResult Success(IMethodSymbol constructor, IReadOnlyList<string> targetTypeDeclarations) => new()
    {
        IsSuccess = true,
        Constructor = constructor,
        TargetTypeDeclarations = targetTypeDeclarations
    };

    public static TypeValidationResult Failure(DiagnosticDescriptor descriptor, params object?[]? messageArgs) => new()
    {
        IsSuccess = false,
        Descriptor = descriptor,
        MessageArgs = messageArgs
    };
}
