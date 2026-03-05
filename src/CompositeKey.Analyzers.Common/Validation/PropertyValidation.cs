using System.Diagnostics.CodeAnalysis;
using CompositeKey.Analyzers.Common.Diagnostics;
using Microsoft.CodeAnalysis;

namespace CompositeKey.Analyzers.Common.Validation;

public static class PropertyValidation
{
    public record PropertyValidationResult
    {
        [MemberNotNullWhen(false, nameof(Descriptor), nameof(MessageArgs))]
        public required bool IsSuccess { get; init; }

        public DiagnosticDescriptor? Descriptor { get; init; }
        public object?[]? MessageArgs { get; init; }

        public static PropertyValidationResult Success() => new()
        {
            IsSuccess = true
        };

        public static PropertyValidationResult Failure(DiagnosticDescriptor descriptor, params object?[] messageArgs) => new()
        {
            IsSuccess = false,
            Descriptor = descriptor,
            MessageArgs = messageArgs
        };
    }

    public record PropertyTypeInfo(
        string TypeName,
        bool IsGuid,
        bool IsString,
        bool IsEnum,
        bool IsSpanParsable,
        bool IsSpanFormattable);

    public record PropertyAccessibilityInfo(
        string Name,
        bool HasGetter,
        bool HasSetter);

    public record RepeatingPropertyTypeInfo(
        string TypeName,
        bool IsList,
        bool IsReadOnlyList,
        bool IsImmutableArray)
    {
        public bool IsRepeatingType => IsList || IsReadOnlyList || IsImmutableArray;
    }

    /// <summary>
    /// Validates that a property has accessible getter and setter.
    /// </summary>
    public static PropertyValidationResult ValidatePropertyAccessibility(PropertyAccessibilityInfo property)
    {
        if (!property.HasGetter || !property.HasSetter)
        {
            return PropertyValidationResult.Failure(
                DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter,
                property.Name);
        }

        return PropertyValidationResult.Success();
    }

    /// <summary>
    /// Validates that a property used with repeating syntax is a supported collection type.
    /// </summary>
    public static PropertyValidationResult ValidateRepeatingPropertyType(
        string propertyName,
        RepeatingPropertyTypeInfo repeatingTypeInfo)
    {
        if (!repeatingTypeInfo.IsRepeatingType)
        {
            return PropertyValidationResult.Failure(
                DiagnosticDescriptors.RepeatingPropertyMustUseCollectionType,
                propertyName);
        }

        return PropertyValidationResult.Success();
    }

    /// <summary>
    /// Validates that a repeating type property uses repeating syntax.
    /// </summary>
    public static PropertyValidationResult ValidateNonRepeatingPropertyType(
        string propertyName,
        RepeatingPropertyTypeInfo repeatingTypeInfo)
    {
        if (repeatingTypeInfo.IsRepeatingType)
        {
            return PropertyValidationResult.Failure(
                DiagnosticDescriptors.RepeatingTypeMustUseRepeatingSyntax,
                propertyName);
        }

        return PropertyValidationResult.Success();
    }

    /// <summary>
    /// Validates that a property format specifier is compatible with the property type.
    /// </summary>
    public static PropertyValidationResult ValidatePropertyFormat(
        string propertyName,
        PropertyTypeInfo typeInfo,
        string? formatSpecifier)
    {
        if (typeInfo.IsGuid && formatSpecifier != null)
        {
            string format = formatSpecifier.ToLowerInvariant();
            if (format != "d" && format != "n" && format != "b" && format != "p" && format != "x")
            {
                return PropertyValidationResult.Failure(
                    DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat,
                    propertyName,
                    formatSpecifier);
            }
        }

        if (typeInfo.IsString && formatSpecifier != null)
        {
            return PropertyValidationResult.Failure(
                DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat,
                propertyName,
                formatSpecifier);
        }

        if (typeInfo.IsEnum && formatSpecifier != null)
        {
            string format = formatSpecifier.ToLowerInvariant();
            if (format != "g")
            {
                return PropertyValidationResult.Failure(
                    DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat,
                    propertyName,
                    formatSpecifier);
            }
        }

        return PropertyValidationResult.Success();
    }

    /// <summary>
    /// Validates that a property type is compatible with CompositeKey requirements.
    /// </summary>
    public static PropertyValidationResult ValidatePropertyTypeCompatibility(
        string propertyName,
        PropertyTypeInfo typeInfo)
    {
        bool isSupported = typeInfo.IsGuid ||
                          typeInfo.IsString ||
                          typeInfo.IsEnum ||
                          typeInfo is { IsSpanParsable: true, IsSpanFormattable: true };

        if (!isSupported)
        {
            return PropertyValidationResult.Failure(
                DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat,
                propertyName,
                "Type is not supported");
        }

        return PropertyValidationResult.Success();
    }

    /// <summary>
    /// Creates a PropertyTypeInfo for the given type symbol by detecting well-known types and span interfaces.
    /// </summary>
    public static PropertyTypeInfo CreatePropertyTypeInfo(
        ITypeSymbol typeSymbol,
        INamedTypeSymbol? guidType,
        INamedTypeSymbol? stringType)
    {
        var (isSpanParsable, isSpanFormattable) = DetectSpanInterfaces(typeSymbol);

        return new PropertyTypeInfo(
            TypeName: typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IsGuid: guidType is not null && SymbolEqualityComparer.Default.Equals(typeSymbol, guidType),
            IsString: stringType is not null && SymbolEqualityComparer.Default.Equals(typeSymbol, stringType),
            IsEnum: typeSymbol.TypeKind == TypeKind.Enum,
            IsSpanParsable: isSpanParsable,
            IsSpanFormattable: isSpanFormattable);
    }

    /// <summary>
    /// Detects whether a type implements ISpanParsable&lt;T&gt; and/or ISpanFormattable.
    /// </summary>
    public static (bool IsSpanParsable, bool IsSpanFormattable) DetectSpanInterfaces(ITypeSymbol typeSymbol)
    {
        var interfaces = typeSymbol.AllInterfaces;
        bool isSpanParsable = false;
        bool isSpanFormattable = false;

        foreach (var iface in interfaces)
        {
            string name = iface.MetadataName;
            if (!isSpanParsable && name == "ISpanParsable`1" && iface.ContainingNamespace?.ToDisplayString() == "System")
                isSpanParsable = true;
            else if (!isSpanFormattable && name == "ISpanFormattable" && iface.ContainingNamespace?.ToDisplayString() == "System")
                isSpanFormattable = true;

            if (isSpanParsable && isSpanFormattable)
                break;
        }

        return (isSpanParsable, isSpanFormattable);
    }

    /// <summary>
    /// Gets the required length for a formatted property value.
    /// </summary>
    public static (int length, bool isExact)? GetFormattedLength(PropertyTypeInfo typeInfo, string? formatSpecifier)
    {
        if (typeInfo.IsGuid)
        {
            string format = formatSpecifier?.ToLowerInvariant() ?? "d";
            return format switch
            {
                "d" => (36, true),
                "n" => (32, true),
                "b" or "p" => (38, true),
                "x" => (32, false),
                _ => null
            };
        }

        if (typeInfo.IsString || typeInfo.IsEnum || typeInfo is { IsSpanParsable: true, IsSpanFormattable: true })
        {
            return (1, false);
        }

        return null;
    }
}
