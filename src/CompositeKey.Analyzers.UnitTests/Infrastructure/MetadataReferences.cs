using Microsoft.CodeAnalysis;
using System.Reflection;

namespace CompositeKey.Analyzers.UnitTests.Infrastructure;

/// <summary>
/// Provides metadata references for CompositeKey analyzer tests.
/// </summary>
public static class MetadataReferences
{
    /// <summary>
    /// Gets a metadata reference to the CompositeKey assembly.
    /// </summary>
    public static MetadataReference CompositeKeyReference { get; } = CreateCompositeKeyReference();

    /// <summary>
    /// Creates a metadata reference to the CompositeKey assembly.
    /// </summary>
    private static MetadataReference CreateCompositeKeyReference()
    {
        // Load the CompositeKey assembly from the same location as this test assembly
        var testAssemblyLocation = Assembly.GetExecutingAssembly().Location;
        var testDirectory = Path.GetDirectoryName(testAssemblyLocation)!;
        
        // Look for CompositeKey.dll in the same directory
        var compositeKeyPath = Path.Combine(testDirectory, "CompositeKey.dll");
        
        if (!File.Exists(compositeKeyPath))
        {
            // Fallback: try to find it relative to the test assembly
            var compositeKeyAssembly = typeof(CompositeKeyAttribute).Assembly;
            compositeKeyPath = compositeKeyAssembly.Location;
        }

        return MetadataReference.CreateFromFile(compositeKeyPath);
    }
}