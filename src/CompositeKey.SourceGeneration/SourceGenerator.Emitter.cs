using System.Diagnostics;
using System.Reflection;
using System.Text;
using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Core.Extensions;
using CompositeKey.SourceGeneration.Model;
using CompositeKey.SourceGeneration.Model.Key;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace CompositeKey.SourceGeneration;

public sealed partial class SourceGenerator
{
    private sealed class Emitter(SourceProductionContext context)
    {
        private static readonly string AssemblyName = typeof(Emitter).Assembly.GetName().Name!;
        private static readonly string AssemblyVersion = typeof(Emitter).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;

        private const string NotNullWhen = "global::System.Diagnostics.CodeAnalysis.NotNullWhen";
        private const string MaybeNullWhen = "global::System.Diagnostics.CodeAnalysis.MaybeNullWhen";
        private const string InvariantCulture = "global::System.Globalization.CultureInfo.InvariantCulture";

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

            WriteParseMethodImplementation();
            WriteTryParseMethodImplementation();

            return;

            void WriteParseMethodImplementation()
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
                writer.Indentation++;

                WriteLengthCheck(writer, keyParts, "primaryKey", true);

                Func<int, string> getPrimaryKeyPartInputVariable = static _ => "primaryKey";
                string? primaryKeyPartCountVariable = null;
                if (keyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, keyParts, "primaryKey", out string primaryKeyPartRangesVariable, true, out primaryKeyPartCountVariable);
                    getPrimaryKeyPartInputVariable = i => $"primaryKey[{primaryKeyPartRangesVariable}[{i}]]";
                }

                WriteParsePropertiesImplementation(writer, keyParts, getPrimaryKeyPartInputVariable, true, primaryKeyPartCountVariable);

                writer.WriteLine($"return {WriteConstructor(targetTypeSpec)};");

                writer.EndBlock();
                writer.WriteLine();
            }

            void WriteTryParseMethodImplementation()
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
                writer.Indentation++;

                WriteLengthCheck(writer, keyParts, "primaryKey", false);

                Func<int, string> getPrimaryKeyPartInputVariable = static _ => "primaryKey";
                string? primaryKeyPartCountVariable = null;
                if (keyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, keyParts, "primaryKey", out string primaryKeyPartRangesVariable, false, out primaryKeyPartCountVariable);
                    getPrimaryKeyPartInputVariable = i => $"primaryKey[{primaryKeyPartRangesVariable}[{i}]]";
                }

                WriteParsePropertiesImplementation(writer, keyParts, getPrimaryKeyPartInputVariable, false, primaryKeyPartCountVariable);

                writer.WriteLines($"""
                                   result = {WriteConstructor(targetTypeSpec)};
                                   return true;
                                   """);

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

            WriteParseMethodImplementation();
            WriteTryParseMethodImplementation();
            WriteCompositeParseMethodImplementation();
            WriteCompositeTryParseMethodImplementation();

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

            void WriteParseMethodImplementation()
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
                writer.Indentation++;

                WritePrimaryKeySplit(true);

                writer.WriteLine("return Parse(primaryKey[primaryKeyPartRanges[0]], primaryKey[primaryKeyPartRanges[1]]);");

                writer.EndBlock();
                writer.WriteLine();
            }

            void WriteTryParseMethodImplementation()
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
                writer.Indentation++;

                WritePrimaryKeySplit(false);

                writer.WriteLine("return TryParse(primaryKey[primaryKeyPartRanges[0]], primaryKey[primaryKeyPartRanges[1]], out result);");

                writer.EndBlock();
                writer.WriteLine();
            }

            void WriteCompositeParseMethodImplementation()
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
                writer.Indentation++;

                WriteLengthCheck(writer, partitionKeyParts, "partitionKey", true);
                WriteLengthCheck(writer, sortKeyParts, "sortKey", true);

                Func<int, string> getPartitionKeyPartInputVariable = static _ => "partitionKey";
                string? partitionKeyPartCountVariable = null;
                if (partitionKeyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, partitionKeyParts, "partitionKey", out string partitionKeyPartRangesVariable, true, out partitionKeyPartCountVariable);
                    getPartitionKeyPartInputVariable = i => $"partitionKey[{partitionKeyPartRangesVariable}[{i}]]";
                }

                Func<int, string> getSortKeyPartInputVariable = static _ => "sortKey";
                string? sortKeyPartCountVariable = null;
                if (sortKeyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, sortKeyParts, "sortKey", out string sortKeyPartRangesVariable, true, out sortKeyPartCountVariable);
                    getSortKeyPartInputVariable = i => $"sortKey[{sortKeyPartRangesVariable}[{i}]]";
                }

                var propertyNameCounts = partitionKeyParts.Concat(sortKeyParts).OfType<PropertyKeyPart>().GroupBy(p => p.Property.CamelCaseName).ToDictionary(g => g.Key, _ => 0);
                WriteParsePropertiesImplementation(writer, partitionKeyParts, getPartitionKeyPartInputVariable, true, propertyNameCounts, partitionKeyPartCountVariable);
                WriteParsePropertiesImplementation(writer, sortKeyParts, getSortKeyPartInputVariable, true, propertyNameCounts, sortKeyPartCountVariable);

                writer.WriteLine($"return {WriteConstructor(targetTypeSpec)};");

                writer.EndBlock();
                writer.WriteLine();
            }

            void WriteCompositeTryParseMethodImplementation()
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
                writer.Indentation++;

                WriteLengthCheck(writer, partitionKeyParts, "partitionKey", false);
                WriteLengthCheck(writer, sortKeyParts, "sortKey", false);

                Func<int, string> getPartitionKeyPartInputVariable = static _ => "partitionKey";
                string? partitionKeyPartCountVariable = null;
                if (partitionKeyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, partitionKeyParts, "partitionKey", out string partitionKeyPartRangesVariable, false, out partitionKeyPartCountVariable);
                    getPartitionKeyPartInputVariable = i => $"partitionKey[{partitionKeyPartRangesVariable}[{i}]]";
                }

                Func<int, string> getSortKeyPartInputVariable = static _ => "sortKey";
                string? sortKeyPartCountVariable = null;
                if (sortKeyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, sortKeyParts, "sortKey", out string sortKeyPartRangesVariable, false, out sortKeyPartCountVariable);
                    getSortKeyPartInputVariable = i => $"sortKey[{sortKeyPartRangesVariable}[{i}]]";
                }

                var propertyNameCounts = partitionKeyParts.Concat(sortKeyParts).OfType<PropertyKeyPart>().GroupBy(p => p.Property.CamelCaseName).ToDictionary(g => g.Key, _ => 0);
                WriteParsePropertiesImplementation(writer, partitionKeyParts, getPartitionKeyPartInputVariable, false, propertyNameCounts, partitionKeyPartCountVariable);
                WriteParsePropertiesImplementation(writer, sortKeyParts, getSortKeyPartInputVariable, false, propertyNameCounts, sortKeyPartCountVariable);

                writer.WriteLines($"""
                                   result = {WriteConstructor(targetTypeSpec)};
                                   return true;
                                   """);

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
            var repeatingPart = parts.OfType<RepeatingPropertyKeyPart>().FirstOrDefault();
            var uniqueDelimiters = parts.OfType<DelimiterKeyPart>().Select(d => d.Value).Distinct().ToList();

            partRangesVariableName = $"{inputName}PartRanges";
            partCountVariableName = null;

            if (repeatingPart is not null)
            {
                bool sameSeparator = uniqueDelimiters.Contains(repeatingPart.Separator);

                if (sameSeparator)
                {
                    // Same separator as key delimiters: split produces variable number of parts
                    int fixedValueParts = parts.OfType<ValueKeyPart>().Count(p => p is not RepeatingPropertyKeyPart);

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
            SourceWriter writer, List<KeyPart> parts, Func<int, string> getPartInputVariable, bool shouldThrow, string? inputPartCountVariable = null)
        {
            var propertyNameCounts = parts.OfType<PropertyKeyPart>().GroupBy(p => p.Property.CamelCaseName).ToDictionary(g => g.Key, _ => 0);
            WriteParsePropertiesImplementation(writer, parts, getPartInputVariable, shouldThrow, propertyNameCounts, inputPartCountVariable);
        }

        private static void WriteParsePropertiesImplementation(
            SourceWriter writer, List<KeyPart> parts, Func<int, string> getPartInputVariable, bool shouldThrow, Dictionary<string, int> propertyNameCounts, string? inputPartCountVariable = null)
        {
            var valueParts = parts.OfType<ValueKeyPart>().ToArray();
            for (int i = 0; i < valueParts.Length; i++)
            {
                var valueKeyPart = valueParts[i];
                string partInputVariable = getPartInputVariable(i);

                if (valueKeyPart is ConstantKeyPart c)
                {
                    writer.WriteLines($"""
                                       if (!{partInputVariable}.Equals("{c.Value}", StringComparison.Ordinal))
                                           {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                       """);
                    continue;
                }

                if (valueKeyPart is RepeatingPropertyKeyPart repeatingPart)
                {
                    WriteRepeatingPropertyParse(writer, parts, repeatingPart, i, getPartInputVariable, shouldThrow, inputPartCountVariable);
                    continue;
                }

                (string camelCaseName, string? originalCamelCaseName) = valueKeyPart is PropertyKeyPart propertyPart
                    ? GetCamelCaseName(propertyPart.Property, propertyNameCounts)
                    : throw new InvalidOperationException($"Expected a {nameof(PropertyKeyPart)} but got a {valueKeyPart.GetType().Name}");

                switch (valueKeyPart)
                {
                    case PropertyKeyPart { ParseType: ParseType.Guid } part:
                        writer.WriteLines($"""
                                           if ({ToStrictLengthCheck(part, partInputVariable)}!Guid.TryParseExact({partInputVariable}, "{part.Format}", out var {camelCaseName}))
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           """);
                        break;

                    case PropertyKeyPart { ParseType: ParseType.String }:
                        writer.WriteLines($"""
                                           if ({partInputVariable}.Length == 0)
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           string {camelCaseName} = {partInputVariable}.ToString();

                                           """);
                        break;

                    case PropertyKeyPart { ParseType: ParseType.Enum } part:
                        if (part.Property.EnumSpec is null)
                            throw new InvalidOperationException($"{nameof(part.Property.EnumSpec)} is null");

                        writer.WriteLines($"""
                                           if (!{part.Property.EnumSpec.Name}Helper.TryParse({partInputVariable}, out var {camelCaseName}))
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           """);
                        break;

                    case PropertyKeyPart { ParseType: ParseType.SpanParsable } part:
                        writer.WriteLines($"""
                                           if (!{part.Property.Type.FullyQualifiedName}.TryParse({partInputVariable}, out var {camelCaseName}))
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           """);
                        break;
                }

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

            static string ToStrictLengthCheck(KeyPart part, string input) =>
                part.ExactLengthRequirement ? $"{input}.Length != {part.LengthRequired} || " : string.Empty;
        }

        private static void WriteRepeatingPropertyParse(
            SourceWriter writer,
            List<KeyPart> parts,
            RepeatingPropertyKeyPart repeatingPart,
            int valuePartIndex,
            Func<int, string> getPartInputVariable,
            bool shouldThrow,
            string? inputPartCountVariable)
        {
            string camelCaseName = repeatingPart.Property.CamelCaseName;
            string innerTypeName = repeatingPart.InnerType.FullyQualifiedName;
            var uniqueDelimiters = parts.OfType<DelimiterKeyPart>().Select(d => d.Value).Distinct().ToList();
            bool sameSeparator = uniqueDelimiters.Contains(repeatingPart.Separator);

            string itemVar = $"{camelCaseName}Item";
            string listVar = camelCaseName;

            if (sameSeparator && inputPartCountVariable is not null)
            {
                // Same separator: repeating items are at indices valuePartIndex..partCount-1
                writer.WriteLines($"""
                                   var {listVar} = new global::System.Collections.Generic.List<{innerTypeName}>();
                                   """);

                writer.StartBlock($"for (int ri = {valuePartIndex}; ri < {inputPartCountVariable}; ri++)");

                // Derive the access expression from getPartInputVariable pattern.
                // getPartInputVariable(i) produces something like "primaryKey[primaryKeyPartRanges[i]]"
                // We need "primaryKey[primaryKeyPartRanges[ri]]"
                string riAccess = getPartInputVariable(valuePartIndex).Replace($"[{valuePartIndex}]", "[ri]");

                WriteRepeatingItemParse(writer, repeatingPart, riAccess, itemVar, listVar, shouldThrow);

                writer.EndBlock();
                writer.WriteLine();
            }
            else
            {
                // Different separator: sub-split the part by the repeating separator
                string partInputVariable = getPartInputVariable(valuePartIndex);
                string repeatingRangesVar = $"{camelCaseName}Ranges";
                string repeatingCountVar = $"{camelCaseName}Count";

                writer.WriteLines($"""
                                   Span<Range> {repeatingRangesVar} = stackalloc Range[128];
                                   int {repeatingCountVar} = {partInputVariable}.Split({repeatingRangesVar}, '{repeatingPart.Separator}', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                                   if ({repeatingCountVar} < 1)
                                       {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                   var {listVar} = new global::System.Collections.Generic.List<{innerTypeName}>();
                                   """);

                // Need to get the base input variable (e.g., "primaryKey") for slicing
                // partInputVariable is like "primaryKey[primaryKeyPartRanges[2]]"
                // We need to reference the sub-span: partInputVariable[repeatingRangesVar[ri]]
                string baseInput = partInputVariable;

                writer.StartBlock($"for (int ri = 0; ri < {repeatingCountVar}; ri++)");

                string riAccess = $"{baseInput}[{repeatingRangesVar}[ri]]";
                WriteRepeatingItemParse(writer, repeatingPart, riAccess, itemVar, listVar, shouldThrow);

                writer.EndBlock();
                writer.WriteLine();
            }

            // Validate at least 1 item
            writer.WriteLines($"""
                               if ({listVar}.Count == 0)
                                   {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                               """);
        }

        private static void WriteRepeatingItemParse(
            SourceWriter writer,
            RepeatingPropertyKeyPart repeatingPart,
            string itemInput,
            string itemVar,
            string listVar,
            bool shouldThrow)
        {
            string innerTypeName = repeatingPart.InnerType.FullyQualifiedName;

            switch (repeatingPart.InnerParseType)
            {
                case ParseType.Guid:
                    writer.WriteLines($"""
                                       if (!Guid.TryParseExact({itemInput}, "{repeatingPart.Format}", out var {itemVar}))
                                           {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};
                                       {listVar}.Add({itemVar});
                                       """);
                    break;

                case ParseType.String:
                    writer.WriteLines($"""
                                       if ({itemInput}.Length == 0)
                                           {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};
                                       {listVar}.Add({itemInput}.ToString());
                                       """);
                    break;

                case ParseType.Enum:
                    if (repeatingPart.Property.EnumSpec is null)
                        throw new InvalidOperationException($"{nameof(repeatingPart.Property.EnumSpec)} is null");

                    writer.WriteLines($"""
                                       if (!{repeatingPart.Property.EnumSpec.Name}Helper.TryParse({itemInput}, out var {itemVar}))
                                           {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};
                                       {listVar}.Add({itemVar});
                                       """);
                    break;

                case ParseType.SpanParsable:
                    writer.WriteLines($"""
                                       if (!{innerTypeName}.TryParse({itemInput}, out var {itemVar}))
                                           {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};
                                       {listVar}.Add({itemVar});
                                       """);
                    break;
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

            bool hasRepeatingPart = keyParts.Any(kp => kp is RepeatingPropertyKeyPart);

            if (hasRepeatingPart)
            {
                WriteRepeatingFormatBody(writer, keyParts, invariantFormatting);
            }
            else if (keyParts.All(kp => kp is
                    DelimiterKeyPart
                    or ConstantKeyPart
                    or PropertyKeyPart { FormatType: FormatType.Guid, ExactLengthRequirement: true }
                    or PropertyKeyPart { FormatType: FormatType.Enum, Format: "g" }
                    or PropertyKeyPart { FormatType: FormatType.String }))
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

                    lengthRequired += keyPart switch
                    {
                        PropertyKeyPart { FormatType: FormatType.Enum, Property.EnumSpec: not null } p => $"{p.Property.EnumSpec.Name}Helper.GetFormattedLength({p.Property.Name})",
                        PropertyKeyPart { FormatType: FormatType.String } p => $"{p.Property.Name}.Length",
                        _ => throw new InvalidOperationException()
                    };
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
                        case PropertyKeyPart { FormatType: FormatType.Guid } p:
                            string formatProvider = invariantFormatting ? InvariantCulture : "null";
                            writer.StartBlock();
                            writer.WriteLine($"if (!((ISpanFormattable)state.{p.Property.Name}).TryFormat(destination[position..], out int {GetCharsWritten(p.Property)}, \"{p.Format ?? "d"}\", {formatProvider}))");
                            writer.WriteLine("\tthrow new FormatException();\n");
                            writer.WriteLine($"position += {GetCharsWritten(p.Property)};");
                            writer.EndBlock();
                            break;
                        case PropertyKeyPart { FormatType: FormatType.Enum, Property.EnumSpec: not null } p:
                            writer.StartBlock();
                            writer.WriteLine($"if (!{p.Property.EnumSpec.Name}Helper.TryFormat(state.{p.Property.Name}, destination[position..], out int {GetCharsWritten(p.Property)}))");
                            writer.WriteLine("\tthrow new FormatException();\n");
                            writer.WriteLine($"position += {GetCharsWritten(p.Property)};");
                            writer.EndBlock();
                            break;
                        case PropertyKeyPart { FormatType: FormatType.String } p:
                            writer.WriteLine($"state.{p.Property.Name}.CopyTo(destination[position..]);");
                            writer.WriteLine($"position += state.{p.Property.Name}.Length;");
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

            static string GetCharsWritten(PropertySpec p) => $"{p.CamelCaseName}CharsWritten";
        }

        private static void WriteRepeatingFormatBody(SourceWriter writer, IReadOnlyList<KeyPart> keyParts, bool invariantFormatting)
        {
            // Emit empty collection checks for all repeating parts
            foreach (var keyPart in keyParts.OfType<RepeatingPropertyKeyPart>())
            {
                string countExpression = keyPart.Property.CollectionType == CollectionType.ImmutableArray
                    ? $"{keyPart.Property.Name}.Length"
                    : $"{keyPart.Property.Name}.Count";

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
                    case PropertyKeyPart:
                        formattedCount++;
                        break;
                    case RepeatingPropertyKeyPart:
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
                    case PropertyKeyPart p:
                        if (p.Format is not null)
                            writer.WriteLine($"handler.AppendFormatted({p.Property.Name}, \"{p.Format}\");");
                        else
                            writer.WriteLine($"handler.AppendFormatted({p.Property.Name});");
                        break;
                    case RepeatingPropertyKeyPart rp:
                        WriteRepeatingPartFormatLoop(writer, rp);
                        break;
                }
            }

            writer.WriteLine();
            writer.WriteLine("return handler.ToStringAndClear();");
        }

        private static void WriteRepeatingPartFormatLoop(SourceWriter writer, RepeatingPropertyKeyPart rp)
        {
            string countExpression = rp.Property.CollectionType == CollectionType.ImmutableArray
                ? $"{rp.Property.Name}.Length"
                : $"{rp.Property.Name}.Count";

            writer.StartBlock($"for (int i = 0; i < {countExpression}; i++)");

            writer.WriteLines($"""
                               if (i > 0)
                                   handler.AppendLiteral("{rp.Separator}");

                               """);

            if (rp.Format is not null)
                writer.WriteLine($"handler.AppendFormatted({rp.Property.Name}[i], \"{rp.Format}\");");
            else
                writer.WriteLine($"handler.AppendFormatted({rp.Property.Name}[i]);");

            writer.EndBlock();
        }

        private static void WriteDynamicFormatMethodBodyForKeyParts(
            SourceWriter writer, string methodDeclaration, IReadOnlyList<KeyPart> keyParts, bool invariantFormatting)
        {
            var repeatingPart = keyParts.OfType<RepeatingPropertyKeyPart>().FirstOrDefault();

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

            // Generate early returns for fixed parts before the repeating section using a switch expression
            if (fixedKeyParts.Count > 0)
            {
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

            // Generate the repeating section handling
            string propName = repeatingPart.Property.Name;
            char separator = repeatingPart.Separator;
            string? format = repeatingPart.Format;

            string countExpression = repeatingPart.Property.CollectionType == CollectionType.ImmutableArray
                ? $"{propName}.Length"
                : $"{propName}.Count";

            writer.WriteLines($"""
                               int fixedPartCount = {fixedPartCount};
                               int repeatIndex = throughPartIndex - fixedPartCount;
                               int repeatCount = Math.Min(repeatIndex + 1, {countExpression});
                               if (repeatCount <= 0)
                                   throw new InvalidOperationException("Invalid throughPartIndex for repeating section.");

                               """);

            // Build the fixed prefix format string (everything before the repeating part).
            // This includes any structural delimiter immediately before the repeating section.
            string fixedPrefix = BuildFormatStringForKeyParts(fixedKeyParts);

            // Build the method using a DefaultInterpolatedStringHandler for efficiency
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

            // Write the loop over repeating items, separated by the repeating separator
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

            // If includeTrailingDelimiter, append the repeating separator after the last item
            writer.StartBlock("if (includeTrailingDelimiter)");
            writer.WriteLine($"handler.AppendLiteral(\"{separator}\");");
            writer.EndBlock();
            writer.WriteLine();

            writer.WriteLine("return handler.ToStringAndClear();");

            writer.EndBlock(); // end method
            writer.WriteLine();
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
}
