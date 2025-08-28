using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;

namespace CompositeKey.Analyzers.UnitTests.Infrastructure;

/// <summary>
/// Base class for CompositeKey analyzer tests providing common test infrastructure,
/// diagnostic verification utilities, and test data management.
/// </summary>
/// <typeparam name="TAnalyzer">The type of analyzer being tested.</typeparam>
public abstract class CompositeKeyAnalyzerTestBase<TAnalyzer> : CSharpAnalyzerTest<TAnalyzer, DefaultVerifier>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    /// <summary>
    /// Initializes the test with default configuration.
    /// </summary>
    protected CompositeKeyAnalyzerTestBase()
    {
        // Add CompositeKey reference by default
        TestState.AdditionalReferences.Add(MetadataReferences.CompositeKeyReference);

        // Set default language version to C# 12
        TestState.AdditionalFiles.Add(("Directory.Build.props", CreateDirectoryBuildProps()));

        // Set ReferenceAssemblies to prevent assembly version conflicts
        ReferenceAssemblies = ReferenceAssemblies.Net.Net80;
    }

    /// <summary>
    /// Creates a Directory.Build.props file content for test projects.
    /// </summary>
    private static string CreateDirectoryBuildProps()
    {
        return """
            <Project>
                <PropertyGroup>
                    <LangVersion>12</LangVersion>
                    <ImplicitUsings>enable</ImplicitUsings>
                    <Nullable>enable</Nullable>
                </PropertyGroup>
            </Project>
            """;
    }
}
