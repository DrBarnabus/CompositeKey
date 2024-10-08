using CompositeKey.SourceGeneration.Resources;
using Microsoft.CodeAnalysis;

namespace CompositeKey.SourceGeneration;

internal static class DiagnosticDescriptors
{
    private const string Category = "CompositeKey.SourceGeneration";

    public static DiagnosticDescriptor UnsupportedLanguageVersion { get; } = new(
        id: "COMPOSITE0001",
        title: Strings.UnsupportedLanguageVersionTitle,
        messageFormat: Strings.UnsupportedLanguageVersionMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor UnsupportedCompositeType { get; } = new(
        id: "COMPOSITE0002",
        title: Strings.UnsupportedCompositeTypeTitle,
        messageFormat: Strings.UnsupportedCompositeTypeMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor CompositeTypeMustBePartial { get; } = new(
        id: "COMPOSITE0003",
        title: Strings.CompositeTypeMustBePartialTitle,
        messageFormat: Strings.CompositeTypeMustBePartialMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor NoObviousDefaultConstructor { get; } = new(
        id: "COMPOSITE0004",
        title: Strings.NoObviousDefaultConstructorTitle,
        messageFormat: Strings.NoObviousDefaultConstructorMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor EmptyOrInvalidTemplateString { get; } = new(
        id: "COMPOSITE0005",
        title: Strings.EmptyOrInvalidTemplateStringTitle,
        messageFormat: Strings.EmptyOrInvalidTemplateStringMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PrimaryKeySeparatorMissingFromTemplateString { get; } = new(
        id: "COMPOSITE0006",
        title: Strings.PrimaryKeySeparatorMissingFromTemplateStringTitle,
        messageFormat: Strings.PrimaryKeySeparatorMissingFromTemplateStringMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PropertyMustHaveAccessibleGetterAndSetter { get; } = new(
        id: "COMPOSITE0007",
        title: Strings.PropertyMustHaveAccessibleGetterAndSetterTitle,
        messageFormat: Strings.PropertyMustHaveAccessibleGetterAndSetterMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static DiagnosticDescriptor PropertyHasInvalidOrUnsupportedFormat { get; } = new(
        id: "COMPOSITE0008",
        title: Strings.PropertyHasInvalidOrUnsupportedFormatTitle,
        messageFormat: Strings.PropertyHasInvalidOrUnsupportedFormatMessageFormat,
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
}
