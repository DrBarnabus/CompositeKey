using System.Diagnostics;
using System.Reflection;
using System.Text;
using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Core.Extensions;
using CompositeKey.SourceGeneration.Emission.Format;
using CompositeKey.SourceGeneration.Emission.Parse;
using CompositeKey.SourceGeneration.Model;
using CompositeKey.SourceGeneration.Model.Key;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CompositeKey.SourceGeneration;

internal sealed class Emitter(SourceProductionContext context)
{
    private static readonly string AssemblyName = typeof(Emitter).Assembly.GetName().Name!;
    private static readonly string AssemblyVersion = typeof(Emitter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

    private const string NotNullWhen = "global::System.Diagnostics.CodeAnalysis.NotNullWhen";
    private const string MaybeNullWhen = "global::System.Diagnostics.CodeAnalysis.MaybeNullWhen";
    private const string InvariantCulture = "global::System.Globalization.CultureInfo.InvariantCulture";

    private static readonly Dictionary<ParseType, IParseStrategy> ParseStrategies = new()
    {
        [ParseType.Guid] = GuidParseStrategy.Instance,
        [ParseType.String] = StringParseStrategy.Instance,
        [ParseType.Enum] = EnumParseStrategy.Instance,
        [ParseType.SpanParsable] = SpanParsableParseStrategy.Instance,
    };

    private static readonly Dictionary<FormatType, IFormatStrategy> FormatStrategies = new()
    {
        [FormatType.Guid] = GuidFormatStrategy.Instance,
        [FormatType.String] = StringFormatStrategy.Instance,
        [FormatType.Enum] = EnumFormatStrategy.Instance,
        [FormatType.SpanFormattable] = SpanFormattableFormatStrategy.Instance,
    };

    private static IParseStrategy GetParseStrategy(ParseType parseType) =>
        ParseStrategies.TryGetValue(parseType, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"No parse strategy registered for {parseType}.");

    private static IFormatStrategy GetFormatStrategy(FormatType formatType) =>
        FormatStrategies.TryGetValue(formatType, out var strategy)
            ? strategy
            : throw new InvalidOperationException($"No format strategy registered for {formatType}.");

    private readonly SourceProductionContext _context = context;

    public void Emit(GenerationSpec generationSpec)
    {
        Debug.Assert(AssemblyName is not null);
        Debug.Assert(AssemblyVersion is not null);

        var writer = CreateSourceWriterWithHeader(generationSpec);

        if (generationSpec.Key is PrimaryKeySpec primaryKeySpec)
            EmitForPrimaryKey(writer, generationSpec.TargetType, primaryKeySpec);
        else if (generationSpec.Key is CompositePrimaryKeySpec compositePrimaryKeySpec)
            EmitForCompositePrimaryKey(writer, generationSpec.TargetType, compositePrimaryKeySpec);

        EmitCommonImplementations(writer, generationSpec.TargetType);
        writer.EndBlock();

        foreach (var enumSpec in generationSpec.TargetType.Properties.Select(p => p.EnumSpec).Where(es => es is not null))
            EnumGenerationHelper.EmitEnumHelperClass(writer, enumSpec!);

        string hintName = $"{generationSpec.TargetType.Type.FullyQualifiedName.Replace("global::", string.Empty)}.g.cs";
        AddSource(hintName, CompleteSourceFileAndReturnSourceText(writer));
    }

    private static void EmitForPrimaryKey(SourceWriter writer, TargetTypeSpec targetTypeSpec, PrimaryKeySpec keySpec)
    {
        var keyParts = keySpec.Parts.ToList();

        WriteFormatMethodBodyForKeyParts(writer, "public override string ToString()", keyParts, keySpec.InvariantFormatting);
        WriteFormatMethodBodyForKeyParts(writer, "public string ToPartitionKeyString()", keyParts, keySpec.InvariantFormatting);
        WriteDynamicFormatMethodBodyForKeyParts(writer, "public string ToPartitionKeyString(int throughPartIndex, bool includeTrailingDelimiter = true)", keyParts, keySpec.InvariantFormatting);

        WriteParseOrTryParseImplementation(shouldThrow: true);
        WriteParseOrTryParseImplementation(shouldThrow: false);

        return;

        void WriteParseOrTryParseImplementation(bool shouldThrow)
        {
            if (shouldThrow)
            {
                writer.WriteLines($$"""
                                    public static {{targetTypeSpec.TypeName}} Parse(string primaryKey)
                                    {
                                        ArgumentNullException.ThrowIfNull(primaryKey);

                                        return Parse((ReadOnlySpan<char>)primaryKey);
                                    }

                                    public static {{targetTypeSpec.TypeName}} Parse(ReadOnlySpan<char> primaryKey)
                                    {
                                    """);
            }
            else
            {
                writer.WriteLines($$"""
                                    public static bool TryParse([{{NotNullWhen}}(true)] string? primaryKey, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}}? result)
                                    {
                                        if (primaryKey is null)
                                        {
                                            result = null;
                                            return false;
                                        }

                                        return TryParse((ReadOnlySpan<char>)primaryKey, out result);
                                    }

                                    public static bool TryParse(ReadOnlySpan<char> primaryKey, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}}? result)
                                    {
                                        result = null;

                                    """);
            }

            writer.Indentation++;

            WriteLengthCheck(writer, keyParts, "primaryKey", shouldThrow);

            Func<string, string> getPrimaryKeyPartInputVariable = static _ => "primaryKey";
            string? primaryKeyPartCountVariable = null;
            if (keyParts.Count > 1)
            {
                WriteSplitImplementation(writer, keyParts, "primaryKey", out string primaryKeyPartRangesVariable, shouldThrow, out primaryKeyPartCountVariable);
                getPrimaryKeyPartInputVariable = indexExpr => $"primaryKey[{primaryKeyPartRangesVariable}[{indexExpr}]]";
            }

            WriteParsePropertiesImplementation(writer, keyParts, getPrimaryKeyPartInputVariable, shouldThrow, primaryKeyPartCountVariable);

            if (shouldThrow)
            {
                writer.WriteLine($"return {WriteConstructor(targetTypeSpec)};");
            }
            else
            {
                writer.WriteLines($"""
                                   result = {WriteConstructor(targetTypeSpec)};
                                   return true;
                                   """);
            }

            writer.EndBlock();
            writer.WriteLine();
        }
    }

    private static void EmitForCompositePrimaryKey(SourceWriter writer, TargetTypeSpec targetTypeSpec, CompositePrimaryKeySpec keySpec)
    {
        var partitionKeyParts = keySpec.PartitionKeyParts.ToList();
        var sortKeyParts = keySpec.SortKeyParts.ToList();

        WriteFormatMethodBodyForKeyParts(writer, "public override string ToString()", keySpec.AllParts, keySpec.InvariantFormatting);
        WriteFormatMethodBodyForKeyParts(writer, "public string ToPartitionKeyString()", partitionKeyParts, keySpec.InvariantFormatting);
        WriteDynamicFormatMethodBodyForKeyParts(writer, "public string ToPartitionKeyString(int throughPartIndex, bool includeTrailingDelimiter = true)", partitionKeyParts, keySpec.InvariantFormatting);
        WriteFormatMethodBodyForKeyParts(writer, "public string ToSortKeyString()", sortKeyParts, keySpec.InvariantFormatting);
        WriteDynamicFormatMethodBodyForKeyParts(writer, "public string ToSortKeyString(int throughPartIndex, bool includeTrailingDelimiter = true)", sortKeyParts, keySpec.InvariantFormatting);

        WriteParseOrTryParseImplementation(shouldThrow: true);
        WriteParseOrTryParseImplementation(shouldThrow: false);
        WriteCompositeParseOrTryParseImplementation(shouldThrow: true);
        WriteCompositeParseOrTryParseImplementation(shouldThrow: false);

        return;

        void WritePrimaryKeySplit(bool shouldThrow)
        {
            writer.WriteLines($"""
                               const int expectedPrimaryKeyParts = 2;
                               Span<Range> primaryKeyPartRanges = stackalloc Range[expectedPrimaryKeyParts + 1];
                               if (primaryKey.Split(primaryKeyPartRanges, '{keySpec.PrimaryDelimiterKeyPart.Value}', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) != expectedPrimaryKeyParts)
                                   {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                               """);
        }

        void WriteParseOrTryParseImplementation(bool shouldThrow)
        {
            if (shouldThrow)
            {
                writer.WriteLines($$"""
                                    public static {{targetTypeSpec.TypeName}} Parse(string primaryKey)
                                    {
                                        ArgumentNullException.ThrowIfNull(primaryKey);

                                        return Parse((ReadOnlySpan<char>)primaryKey);
                                    }

                                    public static {{targetTypeSpec.TypeName}} Parse(ReadOnlySpan<char> primaryKey)
                                    {
                                    """);
            }
            else
            {
                writer.WriteLines($$"""
                                    public static bool TryParse([{{NotNullWhen}}(true)] string? primaryKey, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}}? result)
                                    {
                                        if (primaryKey is null)
                                        {
                                            result = null;
                                            return false;
                                        }

                                        return TryParse((ReadOnlySpan<char>)primaryKey, out result);
                                    }

                                    public static bool TryParse(ReadOnlySpan<char> primaryKey, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}}? result)
                                    {
                                        result = null;

                                    """);
            }

            writer.Indentation++;

            WritePrimaryKeySplit(shouldThrow);

            if (shouldThrow)
                writer.WriteLine("return Parse(primaryKey[primaryKeyPartRanges[0]], primaryKey[primaryKeyPartRanges[1]]);");
            else
                writer.WriteLine("return TryParse(primaryKey[primaryKeyPartRanges[0]], primaryKey[primaryKeyPartRanges[1]], out result);");

            writer.EndBlock();
            writer.WriteLine();
        }

        void WriteCompositeParseOrTryParseImplementation(bool shouldThrow)
        {
            if (shouldThrow)
            {
                writer.WriteLines($$"""
                                    public static {{targetTypeSpec.TypeName}} Parse(string partitionKey, string sortKey)
                                    {
                                        ArgumentNullException.ThrowIfNull(partitionKey);
                                        ArgumentNullException.ThrowIfNull(sortKey);

                                        return Parse((ReadOnlySpan<char>)partitionKey, (ReadOnlySpan<char>)sortKey);
                                    }

                                    public static {{targetTypeSpec.TypeName}} Parse(ReadOnlySpan<char> partitionKey, ReadOnlySpan<char> sortKey)
                                    {
                                    """);
            }
            else
            {
                writer.WriteLines($$"""
                                    public static bool TryParse([{{NotNullWhen}}(true)] string partitionKey, string sortKey, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}}? result)
                                    {
                                        if (partitionKey is null || sortKey is null)
                                        {
                                            result = null;
                                            return false;
                                        }

                                        return TryParse((ReadOnlySpan<char>)partitionKey, (ReadOnlySpan<char>)sortKey, out result);
                                    }

                                    public static bool TryParse(ReadOnlySpan<char> partitionKey, ReadOnlySpan<char> sortKey, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}}? result)
                                    {
                                        result = null;

                                    """);
            }

            writer.Indentation++;

            WriteLengthCheck(writer, partitionKeyParts, "partitionKey", shouldThrow);
            WriteLengthCheck(writer, sortKeyParts, "sortKey", shouldThrow);

            Func<string, string> getPartitionKeyPartInputVariable = static _ => "partitionKey";
            string? partitionKeyPartCountVariable = null;
            if (partitionKeyParts.Count > 1)
            {
                WriteSplitImplementation(writer, partitionKeyParts, "partitionKey", out string partitionKeyPartRangesVariable, shouldThrow, out partitionKeyPartCountVariable);
                getPartitionKeyPartInputVariable = indexExpr => $"partitionKey[{partitionKeyPartRangesVariable}[{indexExpr}]]";
            }

            Func<string, string> getSortKeyPartInputVariable = static _ => "sortKey";
            string? sortKeyPartCountVariable = null;
            if (sortKeyParts.Count > 1)
            {
                WriteSplitImplementation(writer, sortKeyParts, "sortKey", out string sortKeyPartRangesVariable, shouldThrow, out sortKeyPartCountVariable);
                getSortKeyPartInputVariable = indexExpr => $"sortKey[{sortKeyPartRangesVariable}[{indexExpr}]]";
            }

            var propertyNameCounts = partitionKeyParts.Concat(sortKeyParts).OfType<PropertyKeyPart>().GroupBy(p => p.Property.CamelCaseName).ToDictionary(g => g.Key, _ => 0);
            WriteParsePropertiesImplementation(writer, partitionKeyParts, getPartitionKeyPartInputVariable, shouldThrow, propertyNameCounts, partitionKeyPartCountVariable);
            WriteParsePropertiesImplementation(writer, sortKeyParts, getSortKeyPartInputVariable, shouldThrow, propertyNameCounts, sortKeyPartCountVariable);

            if (shouldThrow)
            {
                writer.WriteLine($"return {WriteConstructor(targetTypeSpec)};");
            }
            else
            {
                writer.WriteLines($"""
                                   result = {WriteConstructor(targetTypeSpec)};
                                   return true;
                                   """);
            }

            writer.EndBlock();
            writer.WriteLine();
        }
    }

    private static void EmitCommonImplementations(SourceWriter writer, TargetTypeSpec targetTypeSpec)
    {
        writer.WriteLines($$"""
                            /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
                            string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

                            /// <inheritdoc cref="IParsable{{{targetTypeSpec.TypeName}}}.Parse(string, IFormatProvider?)" />
                            static {{targetTypeSpec.TypeName}} IParsable<{{targetTypeSpec.TypeName}}>.Parse(string s, IFormatProvider? provider) => Parse(s);

                            /// <inheritdoc cref="IParsable{{{targetTypeSpec.TypeName}}}.TryParse(string?, IFormatProvider?, out {{targetTypeSpec.TypeName}})" />
                            static bool IParsable<{{targetTypeSpec.TypeName}}>.TryParse([{{NotNullWhen}}(true)] string? s, IFormatProvider? provider, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}} result) => TryParse(s, out result);

                            /// <inheritdoc cref="ISpanParsable{{{targetTypeSpec.TypeName}}}.Parse(ReadOnlySpan{char}, IFormatProvider?)" />
                            static {{targetTypeSpec.TypeName}} ISpanParsable<{{targetTypeSpec.TypeName}}>.Parse(ReadOnlySpan<char> s, IFormatProvider? provider) => Parse(s);

                            /// <inheritdoc cref="ISpanParsable{{{targetTypeSpec.TypeName}}}.TryParse(ReadOnlySpan{char}, IFormatProvider?, out {{targetTypeSpec.TypeName}})" />
                            static bool ISpanParsable<{{targetTypeSpec.TypeName}}>.TryParse(ReadOnlySpan<char> s, IFormatProvider? provider, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}} result) => TryParse(s, out result);
                            """);
    }

    private static void WriteLengthCheck(SourceWriter writer, List<KeyPart> parts, string inputName, bool shouldThrow)
    {
        int lengthRequired = parts.Select(p => p.LengthRequired).Sum();
        bool exactLengthRequirement = parts.All(p => p.ExactLengthRequirement);

        writer.WriteLines($"""
                           if ({inputName}.Length {(exactLengthRequirement ? "!=" : "<")} {lengthRequired})
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                           """);
    }

    private static void WriteSplitImplementation(SourceWriter writer, List<KeyPart> parts, string inputName, out string partRangesVariableName, bool shouldThrow, out string? partCountVariableName)
    {
        var repeatingPart = parts.OfType<PropertyKeyPart>().FirstOrDefault(p => p.CollectionSemantics is not null);
        var uniqueDelimiters = parts.OfType<DelimiterKeyPart>().Select(d => d.Value).Distinct().ToList();

        partRangesVariableName = $"{inputName}PartRanges";
        partCountVariableName = null;

        if (repeatingPart is not null)
        {
            bool sameSeparator = uniqueDelimiters.Contains(repeatingPart.CollectionSemantics!.Separator);

            if (sameSeparator)
            {
                // Same separator as key delimiters: split produces variable number of parts
                int fixedValueParts = parts.OfType<ValueKeyPart>().Count(p => p is not PropertyKeyPart { CollectionSemantics: not null });

                (string method, string delimiters) = uniqueDelimiters switch
                {
                    { Count: 1 } => ("Split", $"'{uniqueDelimiters[0]}'"),
                    { Count: > 1 } => ("SplitAny", $"\"{string.Join(string.Empty, uniqueDelimiters)}\""),
                    _ => throw new InvalidOperationException()
                };

                string minPartsVariable = $"minExpected{inputName.FirstToUpperInvariant()}Parts";
                partCountVariableName = $"{inputName}PartCount";

                writer.WriteLines($"""
                                   const int {minPartsVariable} = {fixedValueParts + 1};
                                   Span<Range> {partRangesVariableName} = stackalloc Range[128];
                                   int {partCountVariableName} = {inputName}.{method}({partRangesVariableName}, {delimiters}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                   if ({partCountVariableName} < {minPartsVariable})
                                       {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                   """);
            }
            else
            {
                // Different separator: split by fixed delimiters, last part contains the repeating section
                int expectedParts = parts.OfType<ValueKeyPart>().Count();

                (string method, string delimiters) = uniqueDelimiters switch
                {
                    { Count: 1 } => ("Split", $"'{uniqueDelimiters[0]}'"),
                    { Count: > 1 } => ("SplitAny", $"\"{string.Join(string.Empty, uniqueDelimiters)}\""),
                    _ => throw new InvalidOperationException()
                };

                string expectedPartsVariableName = $"expected{inputName.FirstToUpperInvariant()}Parts";

                writer.WriteLines($"""
                                   const int {expectedPartsVariableName} = {expectedParts};
                                   Span<Range> {partRangesVariableName} = stackalloc Range[{expectedPartsVariableName} + 1];
                                   if ({inputName}.{method}({partRangesVariableName}, {delimiters}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) != {expectedPartsVariableName})
                                       {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                   """);
            }
        }
        else
        {
            int expectedParts = parts.OfType<ValueKeyPart>().Count();

            (string method, string delimiters) = uniqueDelimiters switch
            {
                { Count: 1 } => ("Split", $"'{uniqueDelimiters[0]}'"),
                { Count: > 1 } => ("SplitAny", $"\"{string.Join(string.Empty, uniqueDelimiters)}\""),
                _ => throw new InvalidOperationException()
            };

            string expectedPartsVariableName = $"expected{inputName.FirstToUpperInvariant()}Parts";

            writer.WriteLines($"""
                               const int {expectedPartsVariableName} = {expectedParts};
                               Span<Range> {partRangesVariableName} = stackalloc Range[{expectedPartsVariableName} + 1];
                               if ({inputName}.{method}({partRangesVariableName}, {delimiters}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) != {expectedPartsVariableName})
                                   {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                               """);
        }
    }

    private static void WriteParsePropertiesImplementation(
        SourceWriter writer, List<KeyPart> parts, Func<string, string> getPartInputVariable, bool shouldThrow, string? inputPartCountVariable = null)
    {
        var propertyNameCounts = parts.OfType<PropertyKeyPart>().GroupBy(p => p.Property.CamelCaseName).ToDictionary(g => g.Key, _ => 0);
        WriteParsePropertiesImplementation(writer, parts, getPartInputVariable, shouldThrow, propertyNameCounts, inputPartCountVariable);
    }

    private static void WriteParsePropertiesImplementation(
        SourceWriter writer, List<KeyPart> parts, Func<string, string> getPartInputVariable, bool shouldThrow, Dictionary<string, int> propertyNameCounts, string? inputPartCountVariable = null)
    {
        bool skipRedundantLengthCheck = parts.Count == 1;

        var valueParts = parts.OfType<ValueKeyPart>().ToArray();
        for (int i = 0; i < valueParts.Length; i++)
        {
            var valueKeyPart = valueParts[i];
            string partInputVariable = getPartInputVariable($"{i}");

            if (valueKeyPart is ConstantKeyPart c)
            {
                writer.WriteLines($"""
                                   if (!{partInputVariable}.Equals("{c.Value}", StringComparison.Ordinal))
                                       {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                   """);
                continue;
            }

            if (valueKeyPart is PropertyKeyPart { CollectionSemantics: not null } repeatingPart)
            {
                WriteRepeatingPropertyParse(repeatingPart, i);
                continue;
            }

            (string camelCaseName, string? originalCamelCaseName) = valueKeyPart is PropertyKeyPart propertyPart
                ? GetCamelCaseName(propertyPart.Property, propertyNameCounts)
                : throw new InvalidOperationException($"Expected a {nameof(PropertyKeyPart)} but got a {valueKeyPart.GetType().Name}");

            var parseStrategy = GetParseStrategy(propertyPart.TypeDescriptor.ParseType);
            parseStrategy.EmitSingleParse(writer, propertyPart, partInputVariable, camelCaseName, shouldThrow, skipRedundantLengthCheck);

            if (originalCamelCaseName is not null)
            {
                writer.WriteLines($"""
                                   if (!{originalCamelCaseName}.Equals({camelCaseName}))
                                       {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                   """);
            }
        }

        return;

        static (string camelCaseName, string? originalCamelCaseName) GetCamelCaseName(PropertySpec property, Dictionary<string, int> propertyNameCounts)
        {
            int propertyCount = propertyNameCounts[property.CamelCaseName]++;
            return propertyCount == 0
                ? (property.CamelCaseName, null)
                : ($"{property.CamelCaseName}{propertyCount}", property.CamelCaseName);
        }

        void WriteRepeatingPropertyParse(PropertyKeyPart repeatingPart, int valuePartIndex)
        {
            var collection = repeatingPart.CollectionSemantics!;
            string camelCaseName = repeatingPart.Property.CamelCaseName;
            string innerTypeName = collection.InnerType.FullyQualifiedName;
            var uniqueDelimiters = parts.OfType<DelimiterKeyPart>().Select(d => d.Value).Distinct().ToList();
            bool sameSeparator = uniqueDelimiters.Contains(collection.Separator);

            string itemVar = $"{camelCaseName}Item";
            string listVar = camelCaseName;

            if (sameSeparator && inputPartCountVariable is not null)
            {
                // Same separator: repeating items are at indices valuePartIndex..partCount-1
                writer.WriteLines($"""
                                   var {listVar} = new global::System.Collections.Generic.List<{innerTypeName}>();
                                   """);

                writer.StartBlock($"for (int ri = {valuePartIndex}; ri < {inputPartCountVariable}; ri++)");

                string riAccess = getPartInputVariable("ri");

                var repeatingStrategy = GetParseStrategy(repeatingPart.TypeDescriptor.ParseType);
                repeatingStrategy.EmitRepeatingItemParse(writer, repeatingPart, riAccess, itemVar, listVar, shouldThrow);

                writer.EndBlock();
                writer.WriteLine();
            }
            else
            {
                // Different separator: sub-split the part by the repeating separator
                string partInputVariable = getPartInputVariable($"{valuePartIndex}");
                string repeatingRangesVar = $"{camelCaseName}Ranges";
                string repeatingCountVar = $"{camelCaseName}Count";

                writer.WriteLines($"""
                                   Span<Range> {repeatingRangesVar} = stackalloc Range[128];
                                   int {repeatingCountVar} = {partInputVariable}.Split({repeatingRangesVar}, '{collection.Separator}', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                   if ({repeatingCountVar} < 1)
                                       {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                   var {listVar} = new global::System.Collections.Generic.List<{innerTypeName}>();
                                   """);

                writer.StartBlock($"for (int ri = 0; ri < {repeatingCountVar}; ri++)");

                string riAccess = $"{partInputVariable}[{repeatingRangesVar}[ri]]";

                var repeatingStrategy = GetParseStrategy(repeatingPart.TypeDescriptor.ParseType);
                repeatingStrategy.EmitRepeatingItemParse(writer, repeatingPart, riAccess, itemVar, listVar, shouldThrow);

                writer.EndBlock();
                writer.WriteLine();
            }

            // Validate at least 1 item
            writer.WriteLines($"""
                               if ({listVar}.Count == 0)
                                   {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                               """);
        }

    }

    private static string WriteConstructor(TargetTypeSpec targetTypeSpec)
    {
        var builder = new StringBuilder();
        builder.Append($"new {targetTypeSpec.TypeName}(");

        if (targetTypeSpec.ConstructorParameters.Count > 0)
        {
            foreach (var parameter in targetTypeSpec.ConstructorParameters)
            {
                var property = targetTypeSpec.Properties.FirstOrDefault(p => p.CamelCaseName == parameter.CamelCaseName);
                builder.Append(property?.CollectionType == CollectionType.ImmutableArray
                    ? $"global::System.Collections.Immutable.ImmutableArray.CreateRange({parameter.CamelCaseName}), "
                    : $"{parameter.CamelCaseName}, ");
            }

            builder.Length -= 2; // Remove the last ", "
        }

        builder.Append(')');

        if (targetTypeSpec.PropertyInitializers.Count > 0)
        {
            builder.Append(" { ");

            foreach (var initializer in targetTypeSpec.PropertyInitializers)
            {
                var property = targetTypeSpec.Properties.FirstOrDefault(p => p.CamelCaseName == initializer.CamelCaseName);
                builder.Append(property?.CollectionType == CollectionType.ImmutableArray
                    ? $"{initializer.Name} = global::System.Collections.Immutable.ImmutableArray.CreateRange({initializer.CamelCaseName}), "
                    : $"{initializer.Name} = {initializer.CamelCaseName}, ");
            }

            builder.Length -= 2; // Remove the last ", "
            builder.Append(" }");
        }

        return builder.ToString();
    }

    private static void WriteFormatMethodBodyForKeyParts(
        SourceWriter writer, string methodDeclaration, IReadOnlyList<KeyPart> keyParts, bool invariantFormatting)
    {
        writer.StartBlock(methodDeclaration);

        bool hasRepeatingPart = keyParts.Any(kp => kp is PropertyKeyPart { CollectionSemantics: not null });

        if (hasRepeatingPart)
        {
            WriteRepeatingFormatBody();
        }
        else if (keyParts.All(kp =>
            kp is not PropertyKeyPart pp || GetFormatStrategy(pp.TypeDescriptor.FormatType).SupportsSpanFormat(pp)))
        {
            string lengthRequired = keyParts
                .Where(kp => kp.ExactLengthRequirement)
                .Sum(kp => kp switch
                {
                    DelimiterKeyPart => 1,
                    ConstantKeyPart c => c.Value.Length,
                    PropertyKeyPart p => p.LengthRequired,
                    _ => throw new InvalidOperationException()
                })
                .ToString();

            foreach (var keyPart in keyParts.Where(kp => !kp.ExactLengthRequirement))
            {
                if (lengthRequired.Length != 0)
                    lengthRequired += " + ";

                lengthRequired += GetFormatStrategy(((PropertyKeyPart)keyPart).TypeDescriptor.FormatType)
                    .GetVariableLengthExpression((PropertyKeyPart)keyPart);
            }

            writer.StartBlock($"return string.Create({lengthRequired}, this, static (destination, state) =>");

            writer.WriteLine("int position = 0;");
            writer.WriteLine();

            for (int i = 0; i < keyParts.Count; i++)
            {
                var keyPart = keyParts[i];
                switch (keyPart)
                {
                    case DelimiterKeyPart d:
                        writer.WriteLine($"destination[position] = '{d.Value}';");
                        writer.WriteLine("position += 1;");
                        break;
                    case ConstantKeyPart c:
                        writer.WriteLine($"\"{c.Value}\".CopyTo(destination[position..]);");
                        writer.WriteLine($"position += {c.Value.Length};");
                        break;
                    case PropertyKeyPart p:
                        GetFormatStrategy(p.TypeDescriptor.FormatType)
                            .EmitSpanFormat(writer, p, "position", invariantFormatting);
                        break;
                    default:
                        throw new InvalidOperationException();
                }

                if (i != keyParts.Count - 1)
                    writer.WriteLine();
            }

            writer.Indentation--;
            writer.WriteLine("});");
        }
        else
        {
            string formatString = BuildFormatStringForKeyParts(keyParts);
            writer.WriteLine(invariantFormatting
                ? $"return string.Create({InvariantCulture}, $\"{formatString}\");"
                : $"return $\"{formatString}\";");
        }

        writer.EndBlock();
        writer.WriteLine();

        return;

        void WriteRepeatingFormatBody()
        {
            // Emit empty collection checks for all repeating parts
            foreach (var keyPart in keyParts.OfType<PropertyKeyPart>().Where(p => p.CollectionSemantics is not null))
            {
                string countExpression = GetRepeatingCountExpression(keyPart.Property);

                writer.WriteLines($"""
                                   if ({countExpression} == 0)
                                       throw new FormatException("Collection must contain at least one item.");

                                   """);
            }

            // Count fixed literal lengths and variable parts for DefaultInterpolatedStringHandler
            int fixedLiteralLength = 0;
            int formattedCount = 0;
            foreach (var keyPart in keyParts)
            {
                switch (keyPart)
                {
                    case DelimiterKeyPart:
                        fixedLiteralLength += 1;
                        break;
                    case ConstantKeyPart c:
                        fixedLiteralLength += c.Value.Length;
                        break;
                    case PropertyKeyPart { CollectionSemantics: null }:
                        formattedCount++;
                        break;
                    case PropertyKeyPart { CollectionSemantics: not null }:
                        // Will be handled dynamically in the loop
                        break;
                }
            }

            string formatProvider = invariantFormatting ? InvariantCulture : "null";

            writer.WriteLines($"""
                               var handler = new System.Runtime.CompilerServices.DefaultInterpolatedStringHandler({fixedLiteralLength}, {formattedCount}, {formatProvider});
                               """);

            foreach (var keyPart in keyParts)
            {
                switch (keyPart)
                {
                    case DelimiterKeyPart d:
                        writer.WriteLine($"handler.AppendLiteral(\"{d.Value}\");");
                        break;
                    case ConstantKeyPart c:
                        writer.WriteLine($"handler.AppendLiteral(\"{c.Value}\");");
                        break;
                    case PropertyKeyPart { CollectionSemantics: null } p:
                        if (p.Format is not null)
                            writer.WriteLine($"handler.AppendFormatted({p.Property.Name}, \"{p.Format}\");");
                        else
                            writer.WriteLine($"handler.AppendFormatted({p.Property.Name});");
                        break;
                    case PropertyKeyPart { CollectionSemantics: not null } rp:
                        WriteRepeatingPartFormatLoop(rp);
                        break;
                }
            }

            writer.WriteLine();
            writer.WriteLine("return handler.ToStringAndClear();");
        }

        void WriteRepeatingPartFormatLoop(PropertyKeyPart rp)
        {
            var collection = rp.CollectionSemantics!;
            string countExpression = GetRepeatingCountExpression(rp.Property);

            writer.StartBlock($"for (int i = 0; i < {countExpression}; i++)");

            writer.WriteLines($"""
                               if (i > 0)
                                   handler.AppendLiteral("{collection.Separator}");

                               """);

            if (rp.Format is not null)
                writer.WriteLine($"handler.AppendFormatted({rp.Property.Name}[i], \"{rp.Format}\");");
            else
                writer.WriteLine($"handler.AppendFormatted({rp.Property.Name}[i]);");

            writer.EndBlock();
        }
    }

    private static void WriteDynamicFormatMethodBodyForKeyParts(
        SourceWriter writer, string methodDeclaration, IReadOnlyList<KeyPart> keyParts, bool invariantFormatting)
    {
        var repeatingPart = keyParts.OfType<PropertyKeyPart>().FirstOrDefault(p => p.CollectionSemantics is not null);

        if (repeatingPart is null)
        {
            WriteDynamicFormatMethodBodyForFixedKeyParts(writer, methodDeclaration, keyParts, invariantFormatting);
            return;
        }

        // Find the index of the repeating part and count fixed value parts before it
        int repeatingKeyPartIndex = keyParts.ToList().IndexOf(repeatingPart);
        int fixedPartCount = keyParts.Take(repeatingKeyPartIndex).OfType<ValueKeyPart>().Count();
        var fixedKeyParts = keyParts.Take(repeatingKeyPartIndex).ToList();

        writer.StartBlock(methodDeclaration);

        WriteFixedPartCases();
        WriteRepeatingPartHandler();

        writer.EndBlock(); // end method
        writer.WriteLine();

        return;

        void WriteFixedPartCases()
        {
            if (fixedKeyParts.Count == 0)
                return;

            writer.StartBlock("switch (throughPartIndex, includeTrailingDelimiter)");

            for (int i = 0, keyPartIndex = -1; i < fixedKeyParts.Count; i++)
            {
                var keyPart = fixedKeyParts[i];

                bool isDelimiter = keyPart is DelimiterKeyPart;
                if (!isDelimiter)
                    keyPartIndex++;

                string switchCase = $"case ({keyPartIndex}, {(isDelimiter ? "true" : "false")}):";
                string formatString = BuildFormatStringForKeyParts(fixedKeyParts.Take(i + 1));

                writer.WriteLine(invariantFormatting
                    ? $"{switchCase} return string.Create({InvariantCulture}, $\"{formatString}\");"
                    : $"{switchCase} return $\"{formatString}\";");
            }

            writer.EndBlock();
            writer.WriteLine();
        }

        void WriteRepeatingPartHandler()
        {
            var collection = repeatingPart.CollectionSemantics!;
            string propName = repeatingPart.Property.Name;
            char separator = collection.Separator;
            string? format = repeatingPart.Format;
            string countExpression = GetRepeatingCountExpression(repeatingPart.Property);

            writer.WriteLines($"""
                               int fixedPartCount = {fixedPartCount};
                               int repeatIndex = throughPartIndex - fixedPartCount;
                               int repeatCount = Math.Min(repeatIndex + 1, {countExpression});
                               if (repeatCount <= 0)
                                   throw new InvalidOperationException("Invalid throughPartIndex for repeating section.");

                               """);

            string fixedPrefix = BuildFormatStringForKeyParts(fixedKeyParts);

            writer.WriteLines($$"""
                                var handler = new System.Runtime.CompilerServices.DefaultInterpolatedStringHandler(0, 0{{(invariantFormatting ? $", {InvariantCulture}" : "")}});
                                """);

            if (fixedPrefix.Length > 0)
            {
                writer.WriteLine(invariantFormatting
                    ? $"handler.AppendFormatted(string.Create({InvariantCulture}, $\"{fixedPrefix}\"));"
                    : $"handler.AppendFormatted($\"{fixedPrefix}\");");
            }

            writer.WriteLine();

            writer.StartBlock("for (int i = 0; i < repeatCount; i++)");

            writer.StartBlock("if (i > 0)");
            writer.WriteLine($"handler.AppendLiteral(\"{separator}\");");
            writer.EndBlock();

            writer.WriteLine();

            if (format is not null)
                writer.WriteLine($"handler.AppendFormatted({propName}[i], \"{format}\");");
            else
                writer.WriteLine($"handler.AppendFormatted({propName}[i]);");

            writer.EndBlock(); // end for loop
            writer.WriteLine();

            writer.StartBlock("if (includeTrailingDelimiter)");
            writer.WriteLine($"handler.AppendLiteral(\"{separator}\");");
            writer.EndBlock();
            writer.WriteLine();

            writer.WriteLine("return handler.ToStringAndClear();");
        }
    }

    private static void WriteDynamicFormatMethodBodyForFixedKeyParts(
        SourceWriter writer, string methodDeclaration, IReadOnlyList<KeyPart> keyParts, bool invariantFormatting)
    {
        writer.StartBlock(methodDeclaration);

        writer.StartBlock("return (throughPartIndex, includeTrailingDelimiter) switch");

        for (int i = 0, keyPartIndex = -1; i < keyParts.Count; i++)
        {
            var keyPart = keyParts[i];

            bool isDelimiter = keyPart is DelimiterKeyPart;
            if (!isDelimiter)
                keyPartIndex++;

            string switchCase = $"({keyPartIndex}, {(isDelimiter ? "true" : "false")}) =>";
            string formatString = BuildFormatStringForKeyParts(keyParts.Take(i + 1));

            writer.WriteLine(invariantFormatting
                ? $"{switchCase} string.Create({InvariantCulture}, $\"{formatString}\"),"
                : $"{switchCase} $\"{formatString}\",");
        }

        writer.WriteLine("_ => throw new InvalidOperationException(\"Invalid combination of throughPartIndex and includeTrailingDelimiter provided\")");

        writer.EndBlock(withSemicolon: true);

        writer.EndBlock();
        writer.WriteLine();
    }

    private static string GetRepeatingCountExpression(PropertySpec property) =>
        property.CollectionType == CollectionType.ImmutableArray
            ? $"{property.Name}.Length"
            : $"{property.Name}.Count";

    private static string BuildFormatStringForKeyParts(IEnumerable<KeyPart> keyParts)
    {
        var builder = new StringBuilder();
        foreach (var keyPart in keyParts)
        {
            builder.Append(keyPart switch
            {
                DelimiterKeyPart d => d.Value,
                ConstantKeyPart c => c.Value,
                PropertyKeyPart p => $"{{{p.Property.Name}{(p.Format is not null ? $":{p.Format}" : string.Empty)}}}",
                _ => throw new InvalidOperationException()
            });
        }

        return builder.ToString();
    }

    private void AddSource(string hintName, SourceText sourceText) => _context.AddSource(hintName, sourceText);

    private static SourceWriter CreateSourceWriterWithHeader(GenerationSpec generationSpec)
    {
        var writer = new SourceWriter();

        writer.WriteLines("""
                          // <auto-generated />

                          #nullable enable annotations
                          #nullable disable warnings

                          // Suppress warnings about [Obsolete] member usage in generated code.
                          #pragma warning disable CS0612, CS0618

                          using System;
                          using CompositeKey;

                          """);

        if (generationSpec.TargetType.Namespace is not null)
            writer.StartBlock($"namespace {generationSpec.TargetType.Namespace}");

        var nestedTypeDeclarations = generationSpec.TargetType.TypeDeclarations;
        Debug.Assert(nestedTypeDeclarations.Count > 0);

        for (int i = nestedTypeDeclarations.Count - 1; i > 0; i--)
            writer.StartBlock(nestedTypeDeclarations[i]);

        // Annotate the context class with the GeneratedCodeAttribute
        writer.WriteLine($"""[global::System.CodeDom.Compiler.GeneratedCodeAttribute("{AssemblyName}", "{AssemblyVersion}")]""");

        // Emit the main class declaration
        writer.StartBlock($"{nestedTypeDeclarations[0]} : {(generationSpec.Key is PrimaryKeySpec ? "IPrimaryKey" : "ICompositePrimaryKey")}<{generationSpec.TargetType.TypeName}>");

        return writer;
    }

    private static SourceText CompleteSourceFileAndReturnSourceText(SourceWriter writer)
    {
        while (writer.Indentation > 0)
            writer.EndBlock();

        return writer.ToSourceText();
    }
}
