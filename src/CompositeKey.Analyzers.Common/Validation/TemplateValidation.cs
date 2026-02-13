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

        var repeatingPropertyTokens = tokens.OfType<RepeatingPropertyTemplateToken>();
        foreach (var token in repeatingPropertyTokens)
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
        return tokens.Any() && tokens.Any(t => t.Type is
            TemplateToken.TemplateTokenType.Property or
            TemplateToken.TemplateTokenType.Constant or
            TemplateToken.TemplateTokenType.RepeatingProperty);
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
            .Any(t => t.Type is TemplateToken.TemplateTokenType.Property or TemplateToken.TemplateTokenType.Constant or TemplateToken.TemplateTokenType.RepeatingProperty);

        // Check if there are value parts after the delimiter (sort key)
        var hasSortKeyValues = tokens
            .Skip(primaryDelimiterIndex + 1)
            .Any(t => t.Type is TemplateToken.TemplateTokenType.Property or TemplateToken.TemplateTokenType.Constant or TemplateToken.TemplateTokenType.RepeatingProperty);

        return hasPartitionKeyValues && hasSortKeyValues;
    }

    /// <summary>
    /// Validates that repeating properties are the last value part in their key section.
    /// </summary>
    public static TemplateValidationResult ValidateRepeatingPropertyPosition(List<TemplateToken> tokens)
    {
        var repeatingTokens = tokens
            .Where(t => t.Type == TemplateToken.TemplateTokenType.RepeatingProperty)
            .ToList();

        if (repeatingTokens.Count == 0)
            return TemplateValidationResult.Success();

        // Determine if this is a composite key
        var primaryDelimiterIndex = tokens.FindIndex(t => t.Type == TemplateToken.TemplateTokenType.PrimaryDelimiter);

        foreach (var repeatingToken in repeatingTokens)
        {
            var tokenIndex = tokens.IndexOf(repeatingToken);
            var repeatingName = ((RepeatingPropertyTemplateToken)repeatingToken).Name;

            // Determine which section this token belongs to
            List<TemplateToken> section;
            if (primaryDelimiterIndex < 0)
            {
                // Simple key: the entire token list is the section
                section = tokens;
            }
            else if (tokenIndex < primaryDelimiterIndex)
            {
                // Partition key section
                section = tokens.Take(primaryDelimiterIndex).ToList();
            }
            else
            {
                // Sort key section
                section = tokens.Skip(primaryDelimiterIndex + 1).ToList();
            }

            // Find the last value token in the section
            var lastValueToken = section
                .LastOrDefault(t => t.Type is
                    TemplateToken.TemplateTokenType.Property or
                    TemplateToken.TemplateTokenType.Constant or
                    TemplateToken.TemplateTokenType.RepeatingProperty);

            if (lastValueToken != repeatingToken)
            {
                return TemplateValidationResult.Failure(
                    DiagnosticDescriptors.RepeatingPropertyMustBeLastPart,
                    repeatingName);
            }
        }

        return TemplateValidationResult.Success();
    }

    /// <summary>
    /// Validates that there is at most one repeating property per key section.
    /// </summary>
    public static TemplateValidationResult ValidateRepeatingPropertyCount(List<TemplateToken> tokens)
    {
        var repeatingTokens = tokens
            .Where(t => t.Type == TemplateToken.TemplateTokenType.RepeatingProperty)
            .Cast<RepeatingPropertyTemplateToken>()
            .ToList();

        if (repeatingTokens.Count <= 1)
            return TemplateValidationResult.Success();

        // Determine if this is a composite key
        var primaryDelimiterIndex = tokens.FindIndex(t => t.Type == TemplateToken.TemplateTokenType.PrimaryDelimiter);

        if (primaryDelimiterIndex < 0)
        {
            // Simple key: more than one repeating property is invalid
            return TemplateValidationResult.Failure(
                DiagnosticDescriptors.RepeatingPropertyMustBeLastPart,
                repeatingTokens[1].Name);
        }

        // Composite key: check each section separately
        var pkRepeating = repeatingTokens
            .Where(t => tokens.IndexOf(t) < primaryDelimiterIndex)
            .ToList();

        var skRepeating = repeatingTokens
            .Where(t => tokens.IndexOf(t) > primaryDelimiterIndex)
            .ToList();

        if (pkRepeating.Count > 1)
        {
            return TemplateValidationResult.Failure(
                DiagnosticDescriptors.RepeatingPropertyMustBeLastPart,
                pkRepeating[1].Name);
        }

        if (skRepeating.Count > 1)
        {
            return TemplateValidationResult.Failure(
                DiagnosticDescriptors.RepeatingPropertyMustBeLastPart,
                skRepeating[1].Name);
        }

        return TemplateValidationResult.Success();
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

        // Validate repeating property count (at most one per key section)
        var repeatingCountValidation = ValidateRepeatingPropertyCount(tokenizationResult.Tokens);
        if (!repeatingCountValidation.IsSuccess)
            return repeatingCountValidation;

        // Validate repeating property position (must be last value part in section)
        var repeatingPositionValidation = ValidateRepeatingPropertyPosition(tokenizationResult.Tokens);
        if (!repeatingPositionValidation.IsSuccess)
            return repeatingPositionValidation;

        return TemplateValidationResult.Success();
    }
}
