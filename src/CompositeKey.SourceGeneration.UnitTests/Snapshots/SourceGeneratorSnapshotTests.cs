using Microsoft.CodeAnalysis;
using VerifyXunit;

namespace CompositeKey.SourceGeneration.UnitTests;

public class SourceGeneratorSnapshotTests
{
    [Fact]
    public Task BasicPrimaryKey_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithBasicPrimaryKey();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task BasicPrimaryKey_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithBasicPrimaryKey();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task BasicCompositePrimaryKey_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithBasicCompositePrimaryKey();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task BasicCompositePrimaryKey_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithBasicCompositePrimaryKey();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task BasicNonSequentialEnumPrimaryKey_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithBasicNonSequentialEnumPrimaryKey();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task BasicNonSequentialEnumPrimaryKey_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithBasicNonSequentialEnumPrimaryKey();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task InvariantCultureDisabled_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithInvariantCultureDisabled();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task InvariantCultureDisabled_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithInvariantCultureDisabled();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task ClashingKeyNames_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithClashingKeyNames();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task ClashingKeyNames_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithClashingKeyNames();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task InitOnlyProperties_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithInitOnlyProperties();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task InitOnlyProperties_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithInitOnlyProperties();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task ConstructableInitOnlyProperties_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithConstructableInitOnlyProperties();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task ConstructableInitOnlyProperties_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithConstructableInitOnlyProperties();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task RequiredProperties_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithRequiredProperties();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task RequiredProperties_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithRequiredProperties();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task ConstructorThatSetsRequiredProperties_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithConstructorThatSetsRequiredProperties();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task ConstructorThatSetsRequiredProperties_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithConstructorThatSetsRequiredProperties();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task ExplicitlyMarkedConstructor_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithExplicitlyMarkedConstructor();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task ExplicitlyMarkedConstructor_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithExplicitlyMarkedConstructor();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task MultipleExplicitlyMarkedConstructors_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithMultipleExplicitlyMarkedConstructors();
        var driver = RunDriver(compilation, disableDiagnosticValidation: true);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task MultipleExplicitlyMarkedConstructors_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithMultipleExplicitlyMarkedConstructors();
        var result = CompilationHelper.RunSourceGenerator(compilation, disableDiagnosticValidation: true);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task NestedTypeDeclarations_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithNestedTypeDeclarations();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task NestedTypeDeclarations_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithNestedTypeDeclarations();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task NestedPrivateTypeDeclarations_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithNestedPrivateTypeDeclarations();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task NestedPrivateTypeDeclarations_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithNestedPrivateTypeDeclarations();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task SamePropertyUsedTwice_SourceOutput()
    {
        var compilation = CompilationHelper.CreateCompilationWithSamePropertyUsedTwice();
        var driver = RunDriver(compilation);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task SamePropertyUsedTwice_GenerationSpec()
    {
        var compilation = CompilationHelper.CreateCompilationWithSamePropertyUsedTwice();
        var result = CompilationHelper.RunSourceGenerator(compilation);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task RepeatingPropertyKey_SourceOutput()
    {
        // disableDiagnosticValidation: test source uses C# 12 collection expressions with C# 11 parse options
        var compilation = CompilationHelper.CreateCompilationWithRepeatingPropertyKey();
        var driver = RunDriver(compilation, disableDiagnosticValidation: true);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task RepeatingPropertyKey_GenerationSpec()
    {
        // disableDiagnosticValidation: test source uses C# 12 collection expressions with C# 11 parse options
        var compilation = CompilationHelper.CreateCompilationWithRepeatingPropertyKey();
        var result = CompilationHelper.RunSourceGenerator(compilation, disableDiagnosticValidation: true);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    [Fact]
    public Task RepeatingPropertyCompositeKey_SourceOutput()
    {
        // disableDiagnosticValidation: test source uses C# 12 collection expressions with C# 11 parse options
        var compilation = CompilationHelper.CreateCompilationWithRepeatingPropertyCompositeKey();
        var driver = RunDriver(compilation, disableDiagnosticValidation: true);
        return Verifier.Verify(driver);
    }

    [Fact]
    public Task RepeatingPropertyCompositeKey_GenerationSpec()
    {
        // disableDiagnosticValidation: test source uses C# 12 collection expressions with C# 11 parse options
        var compilation = CompilationHelper.CreateCompilationWithRepeatingPropertyCompositeKey();
        var result = CompilationHelper.RunSourceGenerator(compilation, disableDiagnosticValidation: true);
        return Verifier.Verify(result.GenerationSpecs.ToArray());
    }

    private static GeneratorDriver RunDriver(Compilation compilation, bool disableDiagnosticValidation = false)
    {
        var generator = new SourceGenerator();
        GeneratorDriver driver = CompilationHelper.CreateSourceGeneratorDriver(compilation, generator);
        driver = driver.RunGenerators(compilation);

        if (!disableDiagnosticValidation)
        {
            var result = driver.GetRunResult();
            result.Diagnostics.Where(d => d.Severity > DiagnosticSeverity.Info).ShouldBeEmpty();
        }

        return driver;
    }
}
