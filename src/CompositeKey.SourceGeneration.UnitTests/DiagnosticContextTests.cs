using CompositeKey.SourceGeneration.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CompositeKey.SourceGeneration.UnitTests;

public static class DiagnosticContextTests
{
    #pragma warning disable RS2008 // Enable analyzer release tracking
    private static readonly DiagnosticDescriptor TestDescriptor = new(
        id: "TEST0001",
        title: "Test Diagnostic",
        messageFormat: "Test message: {0}",
        category: "Test",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    #pragma warning restore RS2008

    private static Compilation CreateTestCompilation()
    {
        return CompilationHelper.CreateCompilation("public class Placeholder { }");
    }

    private static Location CreateLocationInCompilation(Compilation compilation)
    {
        var tree = compilation.SyntaxTrees.First();
        return Location.Create(tree, TextSpan.FromBounds(0, 1));
    }

    private static Location CreateLocationOutsideCompilation()
    {
        var externalTree = CSharpSyntaxTree.ParseText("public class External { }");
        return Location.Create(externalTree, TextSpan.FromBounds(0, 1));
    }

    [Fact]
    public static void ReportDiagnostic_WithValidLocation_UsesProvidedLocation()
    {
        var compilation = CreateTestCompilation();
        var context = new DiagnosticContext(compilation);
        var currentLocation = CreateLocationInCompilation(compilation);
        context.CurrentLocation = currentLocation;

        var providedLocation = CreateLocationInCompilation(compilation);
        context.ReportDiagnostic(TestDescriptor, providedLocation, "arg1");

        context.Diagnostics.Count.ShouldBe(1);
        context.Diagnostics[0].Location.ShouldNotBeNull();
        context.Diagnostics[0].Location!.SourceSpan.ShouldBe(providedLocation.SourceSpan);
    }

    [Fact]
    public static void ReportDiagnostic_WithNullLocation_FallsBackToCurrentLocation()
    {
        var compilation = CreateTestCompilation();
        var context = new DiagnosticContext(compilation);
        var currentLocation = CreateLocationInCompilation(compilation);
        context.CurrentLocation = currentLocation;

        context.ReportDiagnostic(TestDescriptor, null, "arg1");

        context.Diagnostics.Count.ShouldBe(1);
        context.Diagnostics[0].Location.ShouldNotBeNull();
        context.Diagnostics[0].Location!.SourceSpan.ShouldBe(currentLocation.SourceSpan);
    }

    [Fact]
    public static void ReportDiagnostic_WithLocationFromUnknownTree_FallsBackToCurrentLocation()
    {
        var compilation = CreateTestCompilation();
        var context = new DiagnosticContext(compilation);
        var currentLocation = CreateLocationInCompilation(compilation);
        context.CurrentLocation = currentLocation;

        var externalLocation = CreateLocationOutsideCompilation();
        context.ReportDiagnostic(TestDescriptor, externalLocation, "arg1");

        context.Diagnostics.Count.ShouldBe(1);
        context.Diagnostics[0].Location.ShouldNotBeNull();
        context.Diagnostics[0].Location!.SourceSpan.ShouldBe(currentLocation.SourceSpan);
    }

    [Fact]
    public static void ReportDiagnostic_AccumulatesMultipleDiagnostics()
    {
        var compilation = CreateTestCompilation();
        var context = new DiagnosticContext(compilation);
        context.CurrentLocation = CreateLocationInCompilation(compilation);

        context.ReportDiagnostic(TestDescriptor, null, "first");
        context.ReportDiagnostic(TestDescriptor, null, "second");
        context.ReportDiagnostic(TestDescriptor, null, "third");

        context.Diagnostics.Count.ShouldBe(3);
    }

    [Fact]
    public static void ReportDiagnostic_PassesMessageArgsCorrectly()
    {
        var compilation = CreateTestCompilation();
        var context = new DiagnosticContext(compilation);
        context.CurrentLocation = CreateLocationInCompilation(compilation);

        context.ReportDiagnostic(TestDescriptor, null, "test-value");

        context.Diagnostics.Count.ShouldBe(1);
        var diagnostic = context.Diagnostics[0].CreateDiagnostic();
        diagnostic.GetMessage().ShouldBe("Test message: test-value");
    }

    [Fact]
    public static void Diagnostics_IsEmptyByDefault()
    {
        var compilation = CreateTestCompilation();
        var context = new DiagnosticContext(compilation);

        context.Diagnostics.Count.ShouldBe(0);
    }

    [Fact]
    public static void CurrentLocation_IsNullByDefault()
    {
        var compilation = CreateTestCompilation();
        var context = new DiagnosticContext(compilation);

        context.CurrentLocation.ShouldBeNull();
    }

    [Fact]
    public static void CurrentLocation_CanBeSet()
    {
        var compilation = CreateTestCompilation();
        var context = new DiagnosticContext(compilation);
        var location = CreateLocationInCompilation(compilation);

        context.CurrentLocation = location;

        context.CurrentLocation.ShouldBe(location);
    }
}
