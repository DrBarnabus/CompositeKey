﻿using System.Collections;
using System.Reflection;
using FluentAssertions;
using Microsoft.CodeAnalysis;

namespace CompositeKey.SourceGeneration.UnitTests;

public static class CompositeSourceGeneratorIncrementalTests
{
    [Theory, MemberData(nameof(GetCompilationHelperFactories))]
    public static void CompilingTheSameSource_ShouldResultInStructurallyEqualModels(Func<Compilation> factory)
    {
        var firstResult = CompilationHelper.RunCompositeSourceGenerator(factory(), disableDiagnosticValidation: true);
        var secondResult = CompilationHelper.RunCompositeSourceGenerator(factory(), disableDiagnosticValidation: true);

        firstResult.GenerationSpecs.Length.Should().Be(secondResult.GenerationSpecs.Length);

        for (int i = 0; i < firstResult.GenerationSpecs.Length; i++)
        {
            var firstGenerationSpec = firstResult.GenerationSpecs[i];
            var secondGenerationSpec = secondResult.GenerationSpecs[i];

            firstGenerationSpec.Should().NotBeSameAs(secondGenerationSpec);
            AssertStructurallyEqual(firstGenerationSpec, secondGenerationSpec);

            firstGenerationSpec.Should().Be(secondGenerationSpec);
            firstGenerationSpec.GetHashCode().Should().Be(secondGenerationSpec.GetHashCode());
        }
    }

    [Fact]
    public static void CompilingEquivalentSources_ShouldResultInStructurallyEqualModels()
    {
        const string FirstSource = """
                                   using System;
                                   using CompositeKey;

                                   namespace UnitTests;

                                   [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                                   public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                                   """;

        const string SecondSource = """
                                    using System;
                                    using CompositeKey;

                                    namespace UnitTests
                                    {
                                        [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                                        public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart)
                                        {
                                            public static int AdditionalMethod() => 42;
                                        }
                                    }
                                    """;

        var firstResult = CompilationHelper.RunCompositeSourceGenerator(CompilationHelper.CreateCompilation(FirstSource));
        var secondResult = CompilationHelper.RunCompositeSourceGenerator(CompilationHelper.CreateCompilation(SecondSource));

        firstResult.GenerationSpecs.Should().HaveCount(1);
        var firstGenerationSpec = firstResult.GenerationSpecs[0];

        secondResult.GenerationSpecs.Should().HaveCount(1);
        var secondGenerationSpec = secondResult.GenerationSpecs[0];

        firstGenerationSpec.Should().NotBeSameAs(secondGenerationSpec);
        AssertStructurallyEqual(firstGenerationSpec, secondGenerationSpec);

        firstGenerationSpec.Should().Be(secondGenerationSpec);
        firstGenerationSpec.GetHashCode().Should().Be(secondGenerationSpec.GetHashCode());
    }

    [Fact]
    public static void CompilingDifferentSources_ShouldResultInUnequalModels()
    {
        const string FirstSource = """
                                   using System;
                                   using CompositeKey;

                                   namespace UnitTests;

                                   [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                                   public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                                   """;

        const string SecondSource = """
                                   using System;
                                   using CompositeKey;

                                   namespace UnitTests;

                                   [CompositeKey("{DifferentPartName}#{SecondPart}#{ThirdPart}")]
                                   public partial record BasicPrimaryKey(Guid DifferentPartName, Guid SecondPart, Guid ThirdPart);
                                   """;

        var firstResult = CompilationHelper.RunCompositeSourceGenerator(CompilationHelper.CreateCompilation(FirstSource));
        var secondResult = CompilationHelper.RunCompositeSourceGenerator(CompilationHelper.CreateCompilation(SecondSource));

        firstResult.GenerationSpecs.Should().HaveCount(1);
        var firstGenerationSpec = firstResult.GenerationSpecs[0];

        secondResult.GenerationSpecs.Should().HaveCount(1);
        var secondGenerationSpec = secondResult.GenerationSpecs[0];

        firstGenerationSpec.Should().NotBeSameAs(secondGenerationSpec);
        firstGenerationSpec.Should().NotBe(secondGenerationSpec);
        firstGenerationSpec.GetHashCode().Should().NotBe(secondGenerationSpec.GetHashCode());
    }

    [Theory, MemberData(nameof(GetCompilationHelperFactories))]
    public static void SourceGenerationSpecModel_ShouldNotEncapsulateSymbolsOrCompilationData(Func<Compilation> factory)
    {
        var result = CompilationHelper.RunCompositeSourceGenerator(factory(), disableDiagnosticValidation: true);
        WalkObjectGraphAndAssert(result.GenerationSpecs, []);
        WalkObjectGraphAndAssert(result.Diagnostics, []);

        return;

        static void WalkObjectGraphAndAssert(object? node, HashSet<object> visited)
        {
            if (node is null || !visited.Add(node))
                return;

            (node is Compilation or ISymbol).Should().BeFalse();

            var type = node.GetType();
            if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                return;

            if (node is IEnumerable collection and not string)
            {
                foreach (object? element in collection)
                    WalkObjectGraphAndAssert(element, visited);

                return;
            }

            foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                WalkObjectGraphAndAssert(field.GetValue(node), visited);
        }
    }

    [Theory, MemberData(nameof(GetCompilationHelperFactories))]
    public static void CompilingSameSource_ShouldNotRegenerate(Func<Compilation> factory)
    {
        var compilation = factory();

        GeneratorDriver generatorDriver = CompilationHelper.CreateCompositeSourceGeneratorDriver(compilation);

        generatorDriver = generatorDriver.RunGenerators(compilation);
        var result = generatorDriver.GetRunResult().Results[0];

        var steps = GetGeneratorRunSteps(result);
        if (steps != null)
        {
            foreach (var step in steps)
            {
                foreach ((var source, int outputIndex) in step.Inputs)
                    source.Outputs[outputIndex].Reason.Should().Be(IncrementalStepRunReason.New);

                foreach (var output in step.Outputs)
                    output.Reason.Should().Be(IncrementalStepRunReason.New);
            }
        }

        // Execute the generator again, but confirm that the output is cached
        generatorDriver = generatorDriver.RunGenerators(compilation);
        result = generatorDriver.GetRunResult().Results[0];

        var newSteps = GetGeneratorRunSteps(result);
        if (steps != null)
        {
            newSteps.Should().NotBeNull();

            foreach (var step in newSteps!)
            {
                foreach ((var source, int outputIndex) in step.Inputs)
                    source.Outputs[outputIndex].Reason.Should().Be(IncrementalStepRunReason.Cached);

                foreach (var output in step.Outputs)
                    output.Reason.Should().Be(IncrementalStepRunReason.Cached);
            }
        }
        else
        {
            newSteps.Should().BeNull();
        }

        return;

        static IncrementalGeneratorRunStep[]? GetGeneratorRunSteps(GeneratorRunResult result) =>
            !result.TrackedSteps.TryGetValue(CompositeSourceGenerator.GenerationSpecTrackingName, out var steps) ? null : steps.ToArray();
    }

    [Fact]
    public static void CompilingEquivalentSource_ShouldNotRegenerate()
    {
        const string InitialSource = """
                                     using System;
                                     using CompositeKey;

                                     namespace UnitTests;

                                     [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                                     public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                                     """;

        const string ModifiedSource = """
                                      using System;
                                      using CompositeKey;

                                      namespace UnitTests;

                                      [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                                      public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart)
                                      {
                                          public int NewMethodThatDoesNotAffectEquality() => 123;
                                      }
                                      """;

        var compilation = CompilationHelper.CreateCompilation(InitialSource);
        GeneratorDriver generatorDriver = CompilationHelper.CreateCompositeSourceGeneratorDriver(compilation);

        generatorDriver = generatorDriver.RunGenerators(compilation);
        var result = generatorDriver.GetRunResult().Results[0];

        foreach (var step in result.TrackedSteps[CompositeSourceGenerator.GenerationSpecTrackingName])
        {
            foreach ((var source, int outputIndex) in step.Inputs)
                source.Outputs[outputIndex].Reason.Should().Be(IncrementalStepRunReason.New);

            foreach (var output in step.Outputs)
                output.Reason.Should().Be(IncrementalStepRunReason.New);
        }

        // Execute the generator again with an updated *but equivalent* syntax tree, confirm the output is unchanged
        compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), CompilationHelper.ParseSource(ModifiedSource));
        generatorDriver = generatorDriver.RunGenerators(compilation);
        result = generatorDriver.GetRunResult().Results[0];

        foreach (var step in result.TrackedSteps[CompositeSourceGenerator.GenerationSpecTrackingName])
        {
            foreach ((var source, int outputIndex) in step.Inputs)
                source.Outputs[outputIndex].Reason.Should().Be(IncrementalStepRunReason.Modified);

            foreach (var output in step.Outputs)
                output.Reason.Should().Be(IncrementalStepRunReason.Unchanged);
        }
    }

    [Fact]
    public static void CompilingDifferentSource_ShouldRegenerate()
    {
        const string InitialSource = """
                                     using System;
                                     using CompositeKey;

                                     namespace UnitTests;

                                     [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                                     public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                                     """;

        const string ModifiedSource = """
                                      using System;
                                      using CompositeKey;

                                      namespace NewNamespace;

                                      [CompositeKey("{FirstPart}#{DifferentPartName}")]
                                      public partial record BasicPrimaryKey(string FirstPart, Guid DifferentPartName);
                                      """;

        var compilation = CompilationHelper.CreateCompilation(InitialSource);
        GeneratorDriver generatorDriver = CompilationHelper.CreateCompositeSourceGeneratorDriver(compilation);

        generatorDriver = generatorDriver.RunGenerators(compilation);
        var result = generatorDriver.GetRunResult().Results[0];

        foreach (var step in result.TrackedSteps[CompositeSourceGenerator.GenerationSpecTrackingName])
        {
            foreach ((var source, int outputIndex) in step.Inputs)
                source.Outputs[outputIndex].Reason.Should().Be(IncrementalStepRunReason.New);

            foreach (var output in step.Outputs)
                output.Reason.Should().Be(IncrementalStepRunReason.New);
        }

        // Execute the generator again with an updated *but equivalent* syntax tree, confirm the output is unchanged
        compilation = compilation.ReplaceSyntaxTree(compilation.SyntaxTrees.First(), CompilationHelper.ParseSource(ModifiedSource));
        generatorDriver = generatorDriver.RunGenerators(compilation);
        result = generatorDriver.GetRunResult().Results[0];

        foreach (var step in result.TrackedSteps[CompositeSourceGenerator.GenerationSpecTrackingName])
        {
            foreach ((var source, int outputIndex) in step.Inputs)
                source.Outputs[outputIndex].Reason.Should().Be(IncrementalStepRunReason.Modified);

            foreach (var output in step.Outputs)
                output.Reason.Should().Be(IncrementalStepRunReason.Modified);
        }
    }

    public static IEnumerable<object[]> GetCompilationHelperFactories()
    {
        return typeof(CompilationHelper).GetMethods(BindingFlags.Static | BindingFlags.Public)
            .Where(m => m.ReturnType == typeof(Compilation) && m.GetParameters().Length == 0)
            .Select(m => new[] { Delegate.CreateDelegate(typeof(Func<Compilation>), m) });
    }

    private static void AssertStructurallyEqual<T>(T actual, T expected)
    {
        AssertStructurallyEqualInternal(expected, actual, new Stack<string>());

        return;

        static void AssertStructurallyEqualInternal(object? expected, object? actual, Stack<string> path)
        {
            if (expected is null || actual is null)
            {
                if (expected is not null || actual is not null)
                    Fail();

                return;
            }

            var expectedType = expected.GetType();
            if (expectedType != actual.GetType())
            {
                Fail();
                return;
            }

            if (expected is IEnumerable expectedCollection)
            {
                if (actual is not IEnumerable actualCollection)
                {
                    Fail();
                    return;
                }

                object?[] expectedValues = expectedCollection.Cast<object?>().ToArray();
                object?[] actualValues = actualCollection.Cast<object?>().ToArray();

                for (int i = 0; i < Math.Max(expectedValues.Length, actualValues.Length); i++)
                {
                    object? expectedValue = i < expectedValues.Length ? expectedValues[i] : "<end of collection>";
                    object? actualValue = i < actualValues.Length ? actualValues[i] : "<end of collection>";

                    path.Push($"[{i}]");
                    AssertStructurallyEqualInternal(expectedValue, actualValue, path);
                    path.Pop();
                }
            }

            if (expectedType.GetProperty("EqualityContract", BindingFlags.Instance | BindingFlags.NonPublic, null, typeof(Type), [], null) != null)
            {
                foreach (var property in expectedType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    path.Push("." + property.Name);
                    AssertStructurallyEqualInternal(property.GetValue(expected), property.GetValue(actual), path);
                    path.Pop();
                }

                return;
            }

            if (!expected.Equals(actual))
            {
                Fail();
            }

            return;

            void Fail() => Assert.Fail($"Value not equal in {string.Join("", path.Reverse())}: expected {expected} but was {actual}.");
        }
    }
}
