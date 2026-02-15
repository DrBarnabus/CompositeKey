using CompositeKey.Analyzers.Common.Resources;
using Microsoft.CodeAnalysis;

namespace CompositeKey.Analyzers.Common.Diagnostics;

/// <summary>
/// Contains diagnostic descriptors for CompositeKey validation errors.
/// These diagnostics are shared between the source generator and analyzers.
/// </summary>
public static class DiagnosticDescriptors
{
    private const string Category = "CompositeKey.SourceGeneration";

    /// <summary>
    /// COMPOSITE0001: C# language version not supported by the source generator.
    /// </summary>
    public static DiagnosticDescriptor UnsupportedLanguageVersion { get; } = new(
        id: "COMPOSITE0001",
        title: Strings.UnsupportedLanguageVersionTitle,
        messageFormat: Strings.UnsupportedLanguageVersionMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0002: The type annotated with the 'CompositeKey' attribute is not supported.
    /// </summary>
    public static DiagnosticDescriptor UnsupportedCompositeType { get; } = new(
        id: "COMPOSITE0002",
        title: Strings.UnsupportedCompositeTypeTitle,
        messageFormat: Strings.UnsupportedCompositeTypeMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0003: The type annotated with the 'CompositeKey' attribute must be partial.
    /// </summary>
    public static DiagnosticDescriptor CompositeTypeMustBePartial { get; } = new(
        id: "COMPOSITE0003",
        title: Strings.CompositeTypeMustBePartialTitle,
        messageFormat: Strings.CompositeTypeMustBePartialMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0004: The type annotated with the 'CompositeKey' attribute has no obvious constructor.
    /// </summary>
    public static DiagnosticDescriptor NoObviousDefaultConstructor { get; } = new(
        id: "COMPOSITE0004",
        title: Strings.NoObviousDefaultConstructorTitle,
        messageFormat: Strings.NoObviousDefaultConstructorMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0005: The TemplateString provided to the 'CompositeKey' attribute is invalid.
    /// </summary>
    public static DiagnosticDescriptor EmptyOrInvalidTemplateString { get; } = new(
        id: "COMPOSITE0005",
        title: Strings.EmptyOrInvalidTemplateStringTitle,
        messageFormat: Strings.EmptyOrInvalidTemplateStringMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0006: The 'PrimaryKeySeparator' provided to the 'CompositeKey' attribute is unused.
    /// </summary>
    public static DiagnosticDescriptor PrimaryKeySeparatorMissingFromTemplateString { get; } = new(
        id: "COMPOSITE0006",
        title: Strings.PrimaryKeySeparatorMissingFromTemplateStringTitle,
        messageFormat: Strings.PrimaryKeySeparatorMissingFromTemplateStringMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0007: Property used in 'TemplateString' must have accessible get/set methods.
    /// </summary>
    public static DiagnosticDescriptor PropertyMustHaveAccessibleGetterAndSetter { get; } = new(
        id: "COMPOSITE0007",
        title: Strings.PropertyMustHaveAccessibleGetterAndSetterTitle,
        messageFormat: Strings.PropertyMustHaveAccessibleGetterAndSetterMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0008: Property used in 'TemplateString' has invalid or unsupported format specifier.
    /// </summary>
    public static DiagnosticDescriptor PropertyHasInvalidOrUnsupportedFormat { get; } = new(
        id: "COMPOSITE0008",
        title: Strings.PropertyHasInvalidOrUnsupportedFormatTitle,
        messageFormat: Strings.PropertyHasInvalidOrUnsupportedFormatMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0009: Repeating property must use a collection type.
    /// </summary>
    public static DiagnosticDescriptor RepeatingPropertyMustUseCollectionType { get; } = new(
        id: "COMPOSITE0009",
        title: Strings.RepeatingPropertyMustUseCollectionTypeTitle,
        messageFormat: Strings.RepeatingPropertyMustUseCollectionTypeMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0010: Repeating type must use repeating syntax.
    /// </summary>
    public static DiagnosticDescriptor RepeatingTypeMustUseRepeatingSyntax { get; } = new(
        id: "COMPOSITE0010",
        title: Strings.RepeatingTypeMustUseRepeatingSyntaxTitle,
        messageFormat: Strings.RepeatingTypeMustUseRepeatingSyntaxMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    /// <summary>
    /// COMPOSITE0011: Repeating property must be the last part in its key section.
    /// </summary>
    public static DiagnosticDescriptor RepeatingPropertyMustBeLastPart { get; } = new(
        id: "COMPOSITE0011",
        title: Strings.RepeatingPropertyMustBeLastPartTitle,
        messageFormat: Strings.RepeatingPropertyMustBeLastPartMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
