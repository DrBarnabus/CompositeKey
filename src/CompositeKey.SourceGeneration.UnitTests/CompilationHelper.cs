using System.Collections.Immutable;
using System.Reflection;
using CompositeKey.SourceGeneration.Model;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CompositeKey.SourceGeneration.UnitTests;

public sealed record SourceGeneratorResult
{
    public required Compilation OutputCompilation { get; set; }

    public ImmutableArray<GenerationSpec> GenerationSpecs { get; set; }

    public ImmutableArray<Diagnostic> Diagnostics { get; set; }
}

public static class CompilationHelper
{
    private static readonly Assembly SystemRuntimeAssembly = Assembly.Load(new AssemblyName("System.Runtime"));

    private static readonly CSharpParseOptions DefaultParseOptions = CreateParseOptions();

    public static CSharpParseOptions CreateParseOptions(LanguageVersion? version = null) => new(version ?? LanguageVersion.CSharp11);

    public static Compilation CreateCompilation(string source, string assemblyName = "TestAssembly", CSharpParseOptions? parseOptions = null)
    {
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Guid).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(SystemRuntimeAssembly.Location),

            MetadataReference.CreateFromFile(typeof(CompositeKeyAttribute).Assembly.Location),
        ];

        return CSharpCompilation.Create(
            assemblyName,
            [ParseSource(source, parseOptions)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    public static SyntaxTree ParseSource(string source, CSharpParseOptions? parseOptions = null)
    {
        return CSharpSyntaxTree.ParseText(source, parseOptions ?? DefaultParseOptions);
    }

    public static CSharpGeneratorDriver CreateSourceGeneratorDriver(
        Compilation compilation, SourceGenerator? generator = null)
    {
        generator ??= new SourceGenerator();

        var parseOptions = compilation.SyntaxTrees
            .OfType<CSharpSyntaxTree>()
            .Select(t => t.Options)
            .FirstOrDefault();

        return CSharpGeneratorDriver.Create(
            [generator.AsSourceGenerator()],
            parseOptions: parseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, true));
    }

    public static SourceGeneratorResult RunSourceGenerator(Compilation compilation, bool disableDiagnosticValidation = false)
    {
        var generationSpecs = ImmutableArray<GenerationSpec>.Empty;
        var generator = new SourceGenerator { OnSourceEmitting = specs => generationSpecs = specs };

        var driver = CreateSourceGeneratorDriver(compilation, generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        if (!disableDiagnosticValidation)
        {
            outputCompilation.GetDiagnostics().Where(d => d.Severity > DiagnosticSeverity.Info).Should().BeEmpty();
            diagnostics.Where(d => d.Severity > DiagnosticSeverity.Info).Should().BeEmpty();
        }

        return new SourceGeneratorResult()
        {
            OutputCompilation = outputCompilation,
            Diagnostics = diagnostics,
            GenerationSpecs = generationSpecs
        };
    }

    public static void AssertDiagnostics(IEnumerable<DiagnosticData> expectedDiagnostics, IEnumerable<Diagnostic> actualDiagnostics)
    {
        HashSet<DiagnosticData> expectedDiagnosticsSet = [..expectedDiagnostics];
        HashSet<DiagnosticData> actualDiagnosticsSet = [..actualDiagnostics.Select(d => new DiagnosticData(d.Severity, d.Location, d.GetMessage()))];

        if (!actualDiagnosticsSet.SetEquals(expectedDiagnosticsSet))
            Assert.Fail($"Expected: {string.Join(", ", expectedDiagnosticsSet)}{Environment.NewLine}Actual: {string.Join(", ", actualDiagnosticsSet)}");
    }

    public static Compilation CreateCompilationWithBasicPrimaryKey() => CreateCompilation("""
        using System;
        using CompositeKey;

        namespace UnitTests;

        [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart:N}")]
        public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
        """);

    public static Compilation CreateCompilationWithBasicCompositePrimaryKey() => CreateCompilation("""
        using System;
        using CompositeKey;

        namespace UnitTests;

        public enum CustomEnum { One = 1, Two = 2, Three = 3 };

        [CompositeKey("{FirstPart:B}#{SecondPart}|ConstantValue#{ThirdPart}", PrimaryKeySeparator = '|')]
        public partial record BasicPrimaryKey(Guid FirstPart, string SecondPart, CustomEnum ThirdPart);
        """);

    public static Compilation CreateCompilationWithClashingKeyNames() => CreateCompilation("""
        using System;
        using CompositeKey;

        namespace UnitTests
        {
            [CompositeKey("{FirstPart}#{SecondPart:P}#{ThirdPart}")]
            public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
        }

        namespace AnotherNamespace
        {
            [CompositeKey("{FirstPart}#{SecondPart}|ConstantValue#{ThirdPart:X}", PrimaryKeySeparator = '|')]
            public partial record BasicPrimaryKey(Guid FirstPart, string SecondPart, Guid ThirdPart);
        }
        """);

    public static Compilation CreateCompilationWithInitOnlyProperties() => CreateCompilation("""
        using System;
        using CompositeKey;

        namespace UnitTests;

        [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
        public partial record BasicPrimaryKey
        {
            public Guid FirstPart { get; init; }
            public Guid SecondPart { get; init; }
            public Guid ThirdPart { get; init; }
        }
        """);

    public static Compilation CreateCompilationWithConstructableInitOnlyProperties() => CreateCompilation("""
        using System;
        using CompositeKey;

        namespace UnitTests;

        [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
        public partial record BasicPrimaryKey
        {
            public BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
            {
                FirstPart = firstPart;
                SecondPart = secondPart;
                ThirdPart = thirdPart;
            }

            public Guid FirstPart { get; init; }
            public string SecondPart { get; init; }
            public int ThirdPart { get; init; }
        }
        """);

    public static Compilation CreateCompilationWithRequiredProperties() => CreateCompilation("""
        using System;
        using CompositeKey;

        namespace UnitTests;

        [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
        public partial record BasicPrimaryKey
        {
            public required Guid FirstPart { get; init; }
            public required string SecondPart { get; init; }
            public required int ThirdPart { get; init; }
        }
        """);

    public static Compilation CreateCompilationWithConstructorThatSetsRequiredProperties() => CreateCompilation("""
        using System;
        using System.Diagnostics.CodeAnalysis;
        using CompositeKey;

        namespace UnitTests;

        [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
        public partial record BasicPrimaryKey
        {
            [SetsRequiredMembers]
            public BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
            {
                FirstPart = firstPart;
                SecondPart = secondPart;
                ThirdPart = thirdPart;
            }

            public required Guid FirstPart { get; init; }
            public required string SecondPart { get; init; }
            public required int ThirdPart { get; set; }
        }
        """);

    public static Compilation CreateCompilationWithExplicitlyMarkedConstructor() => CreateCompilation("""
        using System;
        using System.Diagnostics.CodeAnalysis;
        using CompositeKey;

        namespace UnitTests;

        [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
        public partial record BasicPrimaryKey
        {
            [CompositeKeyConstructor]
            public BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
            {
                FirstPart = firstPart;
                SecondPart = secondPart;
                ThirdPart = thirdPart;
            }

            public BasicPrimaryKey(Guid firstPart, string secondPart)
                : this(firstPart, secondPart, default(int))
            {
            }

            public Guid FirstPart { get; init; }
            public string SecondPart { get; init; }
            public int ThirdPart { get; set; }
        }
        """);

    public static Compilation CreateCompilationWithMultipleExplicitlyMarkedConstructors() => CreateCompilation("""
        using System;
        using System.Diagnostics.CodeAnalysis;
        using CompositeKey;

        namespace UnitTests;

        [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
        public partial record BasicPrimaryKey
        {
            [CompositeKeyConstructor]
            public BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
            {
                FirstPart = firstPart;
                SecondPart = secondPart;
                ThirdPart = thirdPart;
            }

            [CompositeKeyConstructor]
            public BasicPrimaryKey(Guid firstPart, string secondPart)
                : this(firstPart, secondPart, default(int))
            {
            }

            public Guid FirstPart { get; init; }
            public string SecondPart { get; init; }
            public int ThirdPart { get; set; }
        }
        """);

    public static Compilation CreateCompilationWithNestedTypeDeclarations() => CreateCompilation("""
        using System;
        using CompositeKey;

        namespace UnitTests;

        public static partial class OutermostClass
        {
            [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
            public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
        }
        """);

    public static Compilation CreateCompilationWithNestedPrivateTypeDeclarations() => CreateCompilation("""
        using System;
        using CompositeKey;

        namespace UnitTests;

        public static partial class OutermostClass
        {
            [CompositeKey("{FirstPart}#Constant#{SecondPart}")]
            private partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart);
        }
        """);

    public record struct DiagnosticData(DiagnosticSeverity Severity, string FilePath, LinePositionSpan LinePositionSpan, string Message)
    {
        public DiagnosticData(DiagnosticSeverity severity, Location location, string message)
            : this(severity, location.SourceTree?.FilePath ?? "", location.GetLineSpan().Span, message)
        {
        }

        public override string ToString() => $"{Severity}, {Message}, {FilePath}@{LinePositionSpan}";
    }
}
