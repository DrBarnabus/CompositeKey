using System.Diagnostics.CodeAnalysis;
using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.Tokenization;
using Microsoft.CodeAnalysis;

namespace CompositeKey.Analyzers.Common.Validation;

public static class TemplateValidation
{
    public record TemplateValidationResult
    {
        [MemberNotNullWhen(false, nameof(Descriptor), nameof(MessageArgs))]
        public required bool IsSuccess { get; init; }

        public DiagnosticDescriptor? Descriptor { get; init; }
        public object?[]? MessageArgs { get; init; }

        public static TemplateValidationResult Success() => new()
        {
            IsSuccess = true
        };

        public static TemplateValidationResult Failure(DiagnosticDescriptor descriptor, params object?[] messageArgs) => new()
        {
            IsSuccess = false,
            Descriptor = descriptor,
            MessageArgs = messageArgs
        };
    }

    public record PropertyInfo(string Name, bool HasGetter, bool HasSetter);

    public static TemplateValidationResult ValidateTemplateString(string? templateString)
    {
        if (string.IsNullOrWhiteSpace(templateString))
        {
            return TemplateValidationResult.Failure(
                DiagnosticDescriptors.EmptyOrInvalidTemplateString,
                templateString ?? string.Empty);
        }

        return TemplateValidationResult.Success();
    }

    public static TemplateValidationResult ValidatePrimaryKeySeparator(
        string templateString,
        char? primaryKeySeparator,
        List<TemplateToken> tokens)
    {
        if (primaryKeySeparator.HasValue && tokens.All(t => t.Type != TemplateToken.TemplateTokenType.PrimaryDelimiter))
        {
            return TemplateValidationResult.Failure(
                DiagnosticDescriptors.PrimaryKeySeparatorMissingFromTemplateString,
                templateString, primaryKeySeparator.Value);
        }

        return TemplateValidationResult.Success();
    }

    public static TemplateValidationResult ValidatePropertyReferences(
        List<TemplateToken> tokens,
        List<PropertyInfo> availableProperties)
    {
        var propertyTokens = tokens.OfType<PropertyTemplateToken>();
        foreach (var token in propertyTokens)
        {
            var property = availableProperties.FirstOrDefault(p => p.Name == token.Name);
            if (property == null)
            {
                return TemplateValidationResult.Failure(
                    DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter,
                    token.Name);
            }

            var accessibilityInfo = new PropertyValidation.PropertyAccessibilityInfo(
                property.Name,
                property.HasGetter,
                property.HasSetter);

            var accessibilityResult = PropertyValidation.ValidatePropertyAccessibility(accessibilityInfo);
            if (!accessibilityResult.IsSuccess)
            {
                return TemplateValidationResult.Failure(
                    accessibilityResult.Descriptor,
                    accessibilityResult.MessageArgs);
            }
        }

        return TemplateValidationResult.Success();
    }

    public static bool HasValidTemplateStructure(List<TemplateToken> tokens)
    {
        return tokens.Any() && tokens.Any(t => t.Type is TemplateToken.TemplateTokenType.Property or TemplateToken.TemplateTokenType.Constant);
    }

    public static bool ValidatePartitionAndSortKeyStructure(
        List<TemplateToken> tokens,
        out int primaryDelimiterIndex)
    {
        primaryDelimiterIndex = -1;

        var primaryDelimiterToken = tokens.FirstOrDefault(t => t.Type == TemplateToken.TemplateTokenType.PrimaryDelimiter);
        if (primaryDelimiterToken == null)
            return true; // No composite key, just simple key

        primaryDelimiterIndex = tokens.IndexOf(primaryDelimiterToken);

        // Check if there are value parts before the delimiter (partition key)
        var hasPartitionKeyValues = tokens
            .Take(primaryDelimiterIndex)
            .Any(t => t.Type == TemplateToken.TemplateTokenType.Property || t.Type == TemplateToken.TemplateTokenType.Constant);

        // Check if there are value parts after the delimiter (sort key)
        var hasSortKeyValues = tokens
            .Skip(primaryDelimiterIndex + 1)
            .Any(t => t.Type == TemplateToken.TemplateTokenType.Property || t.Type == TemplateToken.TemplateTokenType.Constant);

        return hasPartitionKeyValues && hasSortKeyValues;
    }

    public static TokenizeResult TokenizeTemplateString(string templateString, char? primaryKeySeparator)
    {
        var tokenizer = new TemplateStringTokenizer(primaryKeySeparator);
        return tokenizer.Tokenize(templateString.AsSpan());
    }

    public static TemplateValidationResult ValidateTemplateFormat(
        string templateString,
        char? primaryKeySeparator,
        List<PropertyInfo> availableProperties)
    {
        // Basic validation
        var basicValidation = ValidateTemplateString(templateString);
        if (!basicValidation.IsSuccess)
            return basicValidation;

        // Tokenize the template
        var tokenizationResult = TokenizeTemplateString(templateString, primaryKeySeparator);
        if (!tokenizationResult.Success)
        {
            return TemplateValidationResult.Failure(
                DiagnosticDescriptors.EmptyOrInvalidTemplateString,
                templateString);
        }

        // Validate primary key separator if specified
        var separatorValidation = ValidatePrimaryKeySeparator(templateString, primaryKeySeparator, tokenizationResult.Tokens);
        if (!separatorValidation.IsSuccess)
            return separatorValidation;

        // Validate template structure
        if (!HasValidTemplateStructure(tokenizationResult.Tokens))
        {
            return TemplateValidationResult.Failure(
                DiagnosticDescriptors.EmptyOrInvalidTemplateString,
                templateString);
        }

        // If it's a composite key, validate partition and sort key structure
        if (primaryKeySeparator.HasValue)
        {
            if (!ValidatePartitionAndSortKeyStructure(tokenizationResult.Tokens, out _))
            {
                return TemplateValidationResult.Failure(
                    DiagnosticDescriptors.EmptyOrInvalidTemplateString,
                    templateString);
            }
        }

        // Validate property references
        var propertyValidation = ValidatePropertyReferences(tokenizationResult.Tokens, availableProperties);
        if (!propertyValidation.IsSuccess)
            return propertyValidation;

        return TemplateValidationResult.Success();
    }
}
