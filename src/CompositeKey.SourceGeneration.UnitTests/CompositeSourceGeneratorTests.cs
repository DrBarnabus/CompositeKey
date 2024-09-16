using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CompositeKey.SourceGeneration.UnitTests;

public static class CompositeSourceGeneratorTests
{
    [Fact]
    public static void ProgramThatDoesNotUseCompositeKey_ShouldStillSuccessfullyCompile()
    {
        const string Source = """
                              using System;

                              public class Program
                              {
                                  public static void Main()
                                  {
                                      Console.WriteLine("Hello World");
                                  }
                              }
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void BasicPrimaryKey_ShouldSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithBasicPrimaryKey();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void BasicCompositePrimaryKey_ShouldSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithBasicCompositePrimaryKey();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void ClashingKeyNames_ShouldStillSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithClashingKeyNames();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void KeyHasInitOnlyProperties_ShouldSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithInitOnlyProperties();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void KeyHasConstructableInitOnlyProperties_ShouldSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithConstructableInitOnlyProperties();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void KeyHasRequiredProperties_ShouldSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithRequiredProperties();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void KeyHasConstructorThatSetsRequiredProperties_ShouldSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithConstructorThatSetsRequiredProperties();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void KeyHasNestedTypeDeclarations_ShouldSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithNestedTypeDeclarations();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void KeyIsPrivateInNestedTypeDeclarations_ShouldSuccessfullyGenerateSource()
    {
        var compilation = CompilationHelper.CreateCompilationWithNestedPrivateTypeDeclarations();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp11)]
    [InlineData(LanguageVersion.CSharp12)]
    [InlineData(LanguageVersion.LatestMajor)]
    [InlineData(LanguageVersion.Preview)]
    [InlineData(LanguageVersion.Latest)]
    [InlineData(LanguageVersion.Default)]
    public static void SupportedLanguageVersion_ShouldSuccessfullyCompile(LanguageVersion languageVersion)
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                              public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                              """;

        var parseOptions = CompilationHelper.CreateParseOptions(languageVersion);
        var compilation = CompilationHelper.CreateCompilation(Source, parseOptions: parseOptions);
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Theory]
    [InlineData(LanguageVersion.CSharp9)]
    [InlineData(LanguageVersion.CSharp10)]
    public static void UnsupportedLanguageVersion_ShouldFailCompilation(LanguageVersion languageVersion)
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                              public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                              """;

        var parseOptions = CompilationHelper.CreateParseOptions(languageVersion);
        var compilation = CompilationHelper.CreateCompilation(Source, parseOptions: parseOptions);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, $"The CompositeKey source generator does not support C# {languageVersion.ToDisplayString()}. Please use language version 11.0 or greater.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Theory]
    [InlineData("class")]
    [InlineData("struct")]
    public static void NonRecordType_ShouldFailCompilation(string typeDeclaration)
    {
        string source = $$"""
                          using System;
                          using CompositeKey;

                          namespace UnitTests;

                          [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                          public partial {{typeDeclaration}} BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                          """;

        var compilation = CompilationHelper.CreateCompilation(source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'CompositeKey' type 'BasicPrimaryKey' is not currently supported, at present, only record types are supported.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void NonPartialType_ShouldFailCompilation()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                              public record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'CompositeKey' type 'BasicPrimaryKey' (and all containing types) must be partial to kick off source generation.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void ParentIsNonPartialType_ShouldFailCompilation()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              public partial static class PartialParent
                              {
                                  public static class NonPartialParent
                                  {
                                      [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                                      public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                                  }
                              }
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'CompositeKey' type 'BasicPrimaryKey' (and all containing types) must be partial to kick off source generation.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void TypeDoesNotHaveObviousConstructor_ShouldFailCompilation()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                              public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart)
                              {
                                  // Type has two constructors, none of which are a default constructor
                                  public BasicPrimaryKey(Guid FirstPart) : this(FirstPart, Guid.NewGuid(), Guid.NewGuid())
                                  {
                                  }
                              }
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'CompositeKey' type 'BasicPrimaryKey' has no obvious constructor, at present, only types with either a single constructor or types with a parameterless constructor are supported.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void TypeHasDefaultConstructor_ShouldSuccessfullyCompile()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                              public partial record BasicPrimaryKey
                              {
                                  // Default constructor
                                  public BasicPrimaryKey()
                                  {
                                  }

                                  public required Guid FirstPart { get; init; }
                                  public required Guid SecondPart { get; init; }
                                  public required Guid ThirdPart { get; init; }
                              }
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void EmptyTemplateString_ShouldFailCompilation()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("")]
                              public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart);
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'TemplateString' of '' is either empty or could not be successfully parsed.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Theory]
    [InlineData("#")]
    [InlineData("{UnfinishedProperty#{PropertyPart}")]
    public static void InvalidTemplateString_ShouldFailCompilation(string templateString)
    {
        string source = $"""
                         using System;
                         using CompositeKey;

                         namespace UnitTests;

                         [CompositeKey("{templateString}")]
                         public partial record BasicPrimaryKey(Guid PropertyPart);
                         """;

        var compilation = CompilationHelper.CreateCompilation(source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, $"The 'TemplateString' of '{templateString}' is either empty or could not be successfully parsed.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void PrimaryKeySeparatorDefinedButNotUsedInTemplate_ShouldFailCompilation()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{PropertyPart}#{SecondaryPart}", PrimaryKeySeparator = '|')]
                              public partial record BasicPrimaryKey(Guid PrimaryPart, Guid SecondaryPart);
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'TemplateString' of '{PropertyPart}#{SecondaryPart}' does not use the configured 'PrimaryKeySeparator' of '|' so it failed validation.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Theory]
    [InlineData("#|#")]
    [InlineData("{PropertyPart}|#")]
    [InlineData("{PropertyPart}#{UnfinishedProperty")]
    public static void InvalidTemplateStringWithPrimaryKeySeparatorDefined_ShouldFailCompilation(string templateString)
    {
        string source = $"""
                         using System;
                         using CompositeKey;

                         namespace UnitTests;

                         [CompositeKey("{templateString}", PrimaryKeySeparator = '|')]
                         public partial record BasicPrimaryKey(Guid PropertyPart);
                         """;

        var compilation = CompilationHelper.CreateCompilation(source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, $"The 'TemplateString' of '{templateString}' is either empty or could not be successfully parsed.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void PropertyIsMissingGetter_ShouldFailCompilation()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{FirstPart}#{SecondPart}")]
                              public partial record BasicPrimaryKey
                              {
                                  public BasicPrimaryKey()
                                  {
                                  }

                                  public Guid FirstPart { get; set; }

                                  public Guid SecondPart { set; }
                              }
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The property 'SecondPart' is missing either a setter or getter, this is not supported the property must have both accessible set/get methods."),
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'TemplateString' of '{FirstPart}#{SecondPart}' is either empty or could not be successfully parsed.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void PropertyIsMissingSetter_ShouldFailCompilation()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{FirstPart}#{SecondPart}")]
                              public partial record BasicPrimaryKey
                              {
                                  public BasicPrimaryKey()
                                  {
                                  }

                                  public Guid FirstPart { get; }

                                  public Guid SecondPart { get; set; }
                              }
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The property 'FirstPart' is missing either a setter or getter, this is not supported the property must have both accessible set/get methods."),
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'TemplateString' of '{FirstPart}#{SecondPart}' is either empty or could not be successfully parsed.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void GuidPropertyHasInvalidFormat_ShouldFailCompilation()
    {
        const string Source = """
                              using System;
                              using CompositeKey;

                              namespace UnitTests;

                              [CompositeKey("{FirstPart}#{SecondPart:q}")]
                              public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart);
                              """;

        var compilation = CompilationHelper.CreateCompilation(Source);
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The property 'SecondPart' uses format 'q' which is either invalid for the properties type or unsupported by the 'CompositeKey' source generator."),
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'TemplateString' of '{FirstPart}#{SecondPart:q}' is either empty or could not be successfully parsed.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }

    [Fact]
    public static void KeyHasExplicitlyMarkedConstructor_ShouldSuccessfullyCompile()
    {
        var compilation = CompilationHelper.CreateCompilationWithExplicitlyMarkedConstructor();
        CompilationHelper.RunCompositeSourceGenerator(compilation);
    }

    [Fact]
    public static void KeyHasMultipleExplicitlyMarkedConstructors_ShouldFailCompilation()
    {
        var compilation = CompilationHelper.CreateCompilationWithMultipleExplicitlyMarkedConstructors();
        var result = CompilationHelper.RunCompositeSourceGenerator(compilation, disableDiagnosticValidation: true);

        var location = compilation.GetSymbolsWithName("BasicPrimaryKey").First().Locations[0];

        CompilationHelper.DiagnosticData[] expectedDiagnostics =
        [
            new CompilationHelper.DiagnosticData(
                DiagnosticSeverity.Error, location, "The 'CompositeKey' type 'BasicPrimaryKey' has no obvious constructor, at present, only types with either a single constructor or types with a parameterless constructor are supported.")
        ];

        CompilationHelper.AssertDiagnostics(expectedDiagnostics, result.Diagnostics);
    }
}
