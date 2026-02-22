using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using VerifyXunit;

namespace CompositeKey.SourceGeneration.UnitTests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Initialize();

        // Scrub the non-deterministic informational version (includes git commit hash)
        // from the GeneratedCodeAttribute emitted by the source generator.
        // e.g. [GeneratedCodeAttribute("CompositeKey.SourceGeneration", "1.6.0+7675481fa523...")]
        //   -> [GeneratedCodeAttribute("CompositeKey.SourceGeneration", "VERSION")]
        VerifierSettings.ScrubLinesWithReplace(line =>
            Regex.Replace(
                line,
                @"GeneratedCodeAttribute\(""CompositeKey\.SourceGeneration"", ""[^""]*""\)",
                @"GeneratedCodeAttribute(""CompositeKey.SourceGeneration"", ""VERSION"")"));
    }
}
