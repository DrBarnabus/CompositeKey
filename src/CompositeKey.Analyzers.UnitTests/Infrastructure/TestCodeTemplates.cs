namespace CompositeKey.Analyzers.UnitTests.Infrastructure;

/// <summary>
/// Provides reusable code templates for CompositeKey analyzer tests.
/// </summary>
public static class TestCodeTemplates
{
    /// <summary>
    /// A non-partial CompositeKey record.
    /// Parameters: {0} = record name, {1} = template string, {2} = property type, {3} = property name
    /// </summary>
    public const string NonPartialRecord = """
        using CompositeKey;

        [CompositeKey("{1}")]
        public record {0}({2} {3});
        """;

    /// <summary>
    /// A minimal valid record for basic tests.
    /// </summary>
    public const string MinimalValidRecord = """
        using CompositeKey;

        [CompositeKey("User_{UserId}")]
        public partial record UserKey(string UserId);
        """;

    /// <summary>
    /// A record without CompositeKey attribute (should not trigger analyzers).
    /// </summary>
    public const string RecordWithoutAttribute = """
        public partial record UserKey(string UserId);
        """;
}
