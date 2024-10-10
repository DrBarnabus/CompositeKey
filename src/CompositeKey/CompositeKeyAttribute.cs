using System.Diagnostics.CodeAnalysis;

namespace CompositeKey;

/// <summary>
/// Instructs the CompositeKey source generator to generate source code to format and parse a record as a composite
/// key structure.
/// </summary>
/// <param name="template">The <see cref="Template"/> for how the composite key should be formatted and parsed.</param>
[AttributeUsage(AttributeTargets.Class)]
[ExcludeFromCodeCoverage]
#if BUILDING_SOURCE_GENERATOR
internal
#else
public
#endif
sealed class CompositeKeyAttribute(string template) : Attribute
{
    /// <summary>
    /// The template for how the composite key should be formatted and parsed. It can consist of Dynamic Values (properties
    /// on the record), Constant Values and delimiters.
    /// </summary>
    /// <example>{DynamicValue}#Constant@{DynamicValue:format}</example>
    public string Template { get; } = template;

    /// <summary>
    /// Optional, configures this composite key a composite primary key (partition + sort key combined).
    /// Set this value to the delimiter in the <see cref="Template"/> you'd like to use to separate the partition key
    /// and sort key values in the key.
    /// </summary>
    /// <remarks>
    /// This value is optional, but when it is set the <see cref="PrimaryKeySeparator"/> must exist as part of the
    /// <see cref="Template"/> or you'll get a compilation error.
    /// </remarks>
    /// <example>|</example>
    public char PrimaryKeySeparator { get; set; }

    /// <summary>
    /// Optional, when enabled the formatting of values in generated ToString/Format methods will use
    /// <see cref="System.Globalization.CultureInfo.InvariantCulture"/> instead of the default/current culture.
    /// </summary>
    /// <remarks>This is enabled by default, to use the current culture instead set this property to <c>false</c>.</remarks>
    public bool InvariantCulture { get; set; } = true;
}
