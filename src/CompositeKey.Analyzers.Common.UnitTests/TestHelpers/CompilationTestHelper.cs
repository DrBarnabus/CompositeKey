using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CompositeKey.Analyzers.Common.UnitTests.TestHelpers;

/// <summary>
/// Test helper for creating Roslyn compilation objects for testing validation logic.
/// </summary>
public static class CompilationTestHelper
{
    private static readonly Assembly SystemRuntimeAssembly = Assembly.Load(new AssemblyName("System.Runtime"));
    private static readonly CSharpParseOptions DefaultParseOptions = new(LanguageVersion.CSharp11);

    /// <summary>
    /// Creates a C# compilation from source code.
    /// </summary>
    public static CSharpCompilation CreateCompilation(string source, string assemblyName = "TestAssembly")
    {
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Guid).Assembly.Location),
            MetadataReference.CreateFromFile(SystemRuntimeAssembly.Location),
            MetadataReference.CreateFromFile(typeof(CompositeKeyAttribute).Assembly.Location),
        ];

        return CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText(source, DefaultParseOptions)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    /// <summary>
    /// Gets the semantic model and type declaration syntax for the first type in the compilation.
    /// </summary>
    public static (SemanticModel SemanticModel, TypeDeclarationSyntax TypeDeclaration, INamedTypeSymbol TypeSymbol)
        GetFirstTypeInfo(CSharpCompilation compilation, string typeName)
    {
        var syntaxTree = compilation.SyntaxTrees.First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        var typeDeclaration = syntaxTree.GetRoot()
            .DescendantNodes()
            .OfType<TypeDeclarationSyntax>()
            .First(t => t.Identifier.ValueText == typeName);

        var typeSymbol = semanticModel.GetDeclaredSymbol(typeDeclaration)
            ?? throw new InvalidOperationException($"Could not get type symbol for {typeName}");

        return (semanticModel, typeDeclaration, typeSymbol);
    }

    /// <summary>
    /// Gets the CompositeKeyConstructorAttribute type symbol from the compilation.
    /// </summary>
    public static INamedTypeSymbol? GetCompositeKeyConstructorAttributeSymbol(CSharpCompilation compilation)
    {
        return compilation.GetTypeByMetadataName("CompositeKey.CompositeKeyConstructorAttribute");
    }
}
