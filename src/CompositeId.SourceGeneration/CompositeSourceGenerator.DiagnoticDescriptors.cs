using CompositeId.SourceGeneration.Resources;
using Microsoft.CodeAnalysis;

namespace CompositeId.SourceGeneration;

public sealed partial class CompositeSourceGenerator
{
    internal static class DiagnosticDescriptors
    {
        private const string Category = "CompositeId.SourceGeneration";

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
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor CompositeTypeMustBePartial { get; } = new(
            id: "COMPOSITE0003",
            title: Strings.CompositeTypeMustBePartialTitle,
            messageFormat: Strings.CompositeTypeMustBePartialMessageFormat,
            category: Category,
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);
    }
}
