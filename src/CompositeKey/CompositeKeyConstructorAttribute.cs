using System.Diagnostics.CodeAnalysis;

namespace CompositeKey;

/// <summary>
/// Instructs the CompositeKey source generator that this marked constructor should be used to create instances of the
/// type when parsing.
/// </summary>
[AttributeUsage(AttributeTargets.Constructor)]
[ExcludeFromCodeCoverage]
#if BUILDING_SOURCE_GENERATOR
internal
#else
public
#endif
sealed class CompositeKeyConstructorAttribute : Attribute;
