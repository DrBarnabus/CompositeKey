﻿using System.Diagnostics;
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

            string hintName = $"{generationSpec.TargetType.Type.FullyQualifiedName.Replace("global::", string.Empty)}.g.cs";
            AddSource(hintName, CompleteSourceFileAndReturnSourceText(writer));
        }

        private static void EmitForPrimaryKey(SourceWriter writer, TargetTypeSpec targetTypeSpec, PrimaryKeySpec keySpec)
        {
            var keyParts = keySpec.Parts.ToList();

            WriteFormatMethodBodyForKeyParts(writer, "public override string ToString()", keyParts);
            WriteFormatMethodBodyForKeyParts(writer, "public string ToPartitionKeyString()", keyParts);

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
                if (keyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, keyParts, "primaryKey", out string primaryKeyPartRangesVariable, true);
                    getPrimaryKeyPartInputVariable = i => $"primaryKey[{primaryKeyPartRangesVariable}[{i}]]";
                }

                WriteParsePropertiesImplementation(writer, keyParts, getPrimaryKeyPartInputVariable, true);

                writer.WriteLine($"return {WriteConstructor(targetTypeSpec)};");

                writer.Indentation--;
                writer.WriteLine("}");
                writer.WriteLine();
            }

            void WriteTryParseMethodImplementation()
            {
                writer.WriteLines($$"""
                                    public static bool TryParse([{{NotNullWhen}}(true)] string? primaryKey, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}}? result)
                                    {
                                        if(primaryKey is null)
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
                if (keyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, keyParts, "primaryKey", out string primaryKeyPartRangesVariable, false);
                    getPrimaryKeyPartInputVariable = i => $"primaryKey[{primaryKeyPartRangesVariable}[{i}]]";
                }

                WriteParsePropertiesImplementation(writer, keyParts, getPrimaryKeyPartInputVariable, false);

                writer.WriteLines($"""
                                   result = {WriteConstructor(targetTypeSpec)};
                                   return true;
                                   """);

                writer.Indentation--;
                writer.WriteLine("}");
                writer.WriteLine();
            }
        }

        private static void EmitForCompositePrimaryKey(SourceWriter writer, TargetTypeSpec targetTypeSpec, CompositePrimaryKeySpec keySpec)
        {
            var partitionKeyParts = keySpec.PartitionKeyParts.ToList();
            var sortKeyParts = keySpec.SortKeyParts.ToList();

            WriteFormatMethodBodyForKeyParts(writer, "public override string ToString()", keySpec.AllParts);
            WriteFormatMethodBodyForKeyParts(writer, "public string ToPartitionKeyString()", partitionKeyParts);
            WriteFormatMethodBodyForKeyParts(writer, "public string ToSortKeyString()", sortKeyParts);

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

                writer.Indentation--;
                writer.WriteLine("}");
                writer.WriteLine();
            }

            void WriteTryParseMethodImplementation()
            {
                writer.WriteLines($$"""
                                    public static bool TryParse([{{NotNullWhen}}(true)] string? primaryKey, [{{MaybeNullWhen}}(false)] out {{targetTypeSpec.TypeName}}? result)
                                    {
                                        if(primaryKey is null)
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

                writer.Indentation--;
                writer.WriteLine("}");
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
                if (partitionKeyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, partitionKeyParts, "partitionKey", out string partitionKeyPartRangesVariable, true);
                    getPartitionKeyPartInputVariable = i => $"partitionKey[{partitionKeyPartRangesVariable}[{i}]]";
                }

                Func<int, string> getSortKeyPartInputVariable = static _ => "sortKey";
                if (sortKeyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, sortKeyParts, "sortKey", out string sortKeyPartRangesVariable, true);
                    getSortKeyPartInputVariable = i => $"sortKey[{sortKeyPartRangesVariable}[{i}]]";
                }

                WriteParsePropertiesImplementation(writer, partitionKeyParts, getPartitionKeyPartInputVariable, true);
                WriteParsePropertiesImplementation(writer, sortKeyParts, getSortKeyPartInputVariable, true);

                writer.WriteLine($"return {WriteConstructor(targetTypeSpec)};");

                writer.Indentation--;
                writer.WriteLine("}");
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
                if (partitionKeyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, partitionKeyParts, "partitionKey", out string partitionKeyPartRangesVariable, false);
                    getPartitionKeyPartInputVariable = i => $"partitionKey[{partitionKeyPartRangesVariable}[{i}]]";
                }

                Func<int, string> getSortKeyPartInputVariable = static _ => "sortKey";
                if (sortKeyParts.Count > 1)
                {
                    WriteSplitImplementation(writer, sortKeyParts, "sortKey", out string sortKeyPartRangesVariable, false);
                    getSortKeyPartInputVariable = i => $"sortKey[{sortKeyPartRangesVariable}[{i}]]";
                }

                WriteParsePropertiesImplementation(writer, partitionKeyParts, getPartitionKeyPartInputVariable, false);
                WriteParsePropertiesImplementation(writer, sortKeyParts, getSortKeyPartInputVariable, false);

                writer.WriteLines($"""
                                   result = {WriteConstructor(targetTypeSpec)};
                                   return true;
                                   """);

                writer.Indentation--;
                writer.WriteLine("}");
                writer.WriteLine();
            }
        }

        private static void EmitCommonImplementations(SourceWriter writer, TargetTypeSpec targetTypeSpec)
        {
            writer.WriteLines($$"""
                                /// <inheritdoc cref="IFormattable.ToString(string?, IFormatProvider?)" />
                                string IFormattable.ToString(string? format, IFormatProvider? formatProvider) => ToString();

                                /// <inheritdoc cref="IParsable{{{targetTypeSpec.TypeName}}.Parse(string, IFormatProvider?)" />
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

        private static void WriteSplitImplementation(SourceWriter writer, List<KeyPart> parts, string inputName, out string partRangesVariableName, bool shouldThrow)
        {
            var uniqueDelimiters = parts.OfType<DelimiterKeyPart>().Select(d => d.Value).Distinct().ToList();
            int expectedParts = parts.OfType<ValueKeyPart>().Count();

            (string method, string delimiters) = uniqueDelimiters switch
            {
                { Count: 1 } => ("Split", $"'{uniqueDelimiters[0]}'"),
                { Count: > 1 } => ("SplitAny", $"\"{string.Join(string.Empty, uniqueDelimiters)}\""),
                _ => throw new InvalidOperationException()
            };

            string expectedPartsVariableName = $"expected{inputName.FirstToUpperInvariant()}Parts";
            partRangesVariableName = $"{inputName}PartRanges";

            writer.WriteLines($"""
                               const int {expectedPartsVariableName} = {expectedParts};
                               Span<Range> {partRangesVariableName} = stackalloc Range[{expectedPartsVariableName} + 1];
                               if ({inputName}.{method}({partRangesVariableName}, {delimiters}, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) != {expectedPartsVariableName})
                                   {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                               """);
        }

        private static void WriteParsePropertiesImplementation(SourceWriter writer, List<KeyPart> parts, Func<int, string> getPartInputVariable, bool shouldThrow)
        {
            var valueParts = parts.OfType<ValueKeyPart>().ToArray();
            for (int i = 0; i < valueParts.Length; i++)
            {
                string partInputVariable = getPartInputVariable(i);

                switch (valueParts[i])
                {
                    case ConstantKeyPart c:
                        writer.WriteLines($"""
                                           if (!{partInputVariable}.Equals("{c.Value}", StringComparison.Ordinal))
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           """);
                        break;

                    case PropertyKeyPart { ParseType: ParseType.Guid } part:
                        writer.WriteLines($"""
                                           if ({ToStrictLengthCheck(part, partInputVariable)}!Guid.TryParseExact({partInputVariable}, "{part.Format}", out var {part.Property.CamelCaseName}))
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           """);
                        break;

                    case PropertyKeyPart { ParseType: ParseType.String } part:
                        writer.WriteLines($"""
                                           if ({partInputVariable}.Length == 0)
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           string {part.Property.CamelCaseName} = {partInputVariable}.ToString();

                                           """);
                        break;

                    case PropertyKeyPart { ParseType: ParseType.Enum } part:
                        writer.WriteLines($"""
                                           if (!Enum.TryParse<{part.Property.Type.FullyQualifiedName}>({partInputVariable}, out var {part.Property.CamelCaseName}))
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           """);
                        break;

                    case PropertyKeyPart { ParseType: ParseType.SpanParsable } part:
                        writer.WriteLines($"""
                                           if (!{part.Property.Type.FullyQualifiedName}.TryParse({partInputVariable}, out var {part.Property.CamelCaseName}))
                                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                                           """);
                        break;
                }
            }

            return;

            static string ToStrictLengthCheck(KeyPart part, string input) =>
                part.ExactLengthRequirement ? $"{input}.Length != {part.LengthRequired} || " : string.Empty;
        }

        private static string WriteConstructor(TargetTypeSpec targetTypeSpec)
        {
            var builder = new StringBuilder();
            builder.Append($"new {targetTypeSpec.TypeName}(");

            if (targetTypeSpec.ConstructorParameters.Count > 0)
            {
                foreach (var parameter in targetTypeSpec.ConstructorParameters)
                    builder.Append($"{parameter.CamelCaseName}, ");

                builder.Length -= 2; // Remove the last ", "
            }

            builder.Append(')');

            if (targetTypeSpec.PropertyInitializers.Count > 0)
            {
                builder.Append(" { ");

                foreach (var initializer in targetTypeSpec.PropertyInitializers)
                    builder.Append($"{initializer.Name} = {initializer.CamelCaseName}, ");

                builder.Length -= 2; // Remove the last ", "
                builder.Append(" }");
            }

            return builder.ToString();
        }

        private static void WriteFormatMethodBodyForKeyParts(SourceWriter writer, string methodDeclaration, IReadOnlyList<KeyPart> keyParts)
        {
            writer.WriteLine(methodDeclaration);
            writer.WriteLine("{");
            writer.Indentation++;

            if (keyParts.All(kp => kp is DelimiterKeyPart or ConstantKeyPart or PropertyKeyPart { FormatType: FormatType.Guid, ExactLengthRequirement: true }))
            {
                int lengthRequired = keyParts.Sum(kp => kp switch
                {
                    DelimiterKeyPart => 1,
                    ConstantKeyPart c => c.Value.Length,
                    PropertyKeyPart c => c.LengthRequired,
                    _ => throw new InvalidOperationException()
                });

                writer.WriteLine($"return string.Create({lengthRequired}, this, static (destination, state) =>");
                writer.WriteLine("{");
                writer.Indentation++;

                int position = 0;
                foreach (var keyPart in keyParts)
                {
                    switch (keyPart)
                    {
                        case DelimiterKeyPart d:
                            writer.WriteLine($"destination[{position}] = '{d.Value}';");
                            position += 1;
                            break;
                        case ConstantKeyPart c:
                            writer.WriteLine($"\"{c.Value}\".CopyTo(destination[{position}..{position + c.Value.Length}]);");
                            position += c.Value.Length;
                            break;
                        case PropertyKeyPart p:
                            writer.WriteLine($"if (!state.{p.Property.Name}.TryFormat(destination[{position}..{position + p.LengthRequired}], out _, \"{p.Format ?? "d"}\"))");
                            writer.WriteLine("\tthrow new FormatException();");
                            position += p.LengthRequired;
                            break;
                        default:
                            throw new InvalidOperationException();
                    }

                    if (position != lengthRequired)
                        writer.WriteLine();
                }

                writer.Indentation--;
                writer.WriteLine("});");
            }
            else
            {
                string formatString = string.Empty;
                foreach (var keyPart in keyParts)
                {
                    formatString += keyPart switch
                    {
                        DelimiterKeyPart d => d.Value,
                        ConstantKeyPart c => c.Value,
                        PropertyKeyPart p => $"{{{p.Property.Name}{(p.Format is not null ? $":{p.Format}" : string.Empty)}}}",
                        _ => throw new InvalidOperationException()
                    };
                }

                writer.WriteLine($"return $\"{formatString}\";");
            }

            writer.Indentation--;
            writer.WriteLine("}");
            writer.WriteLine();
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
            {
                writer.WriteLine($"namespace {generationSpec.TargetType.Namespace}");
                writer.WriteLine("{");
                writer.Indentation++;
            }

            var nestedTypeDeclarations = generationSpec.TargetType.TypeDeclarations;
            Debug.Assert(nestedTypeDeclarations.Count > 0);

            for (int i = nestedTypeDeclarations.Count - 1; i > 0; i--)
            {
                writer.WriteLine(nestedTypeDeclarations[i]);
                writer.WriteLine("{");
                writer.Indentation++;
            }

            // Annotate context class with the GeneratedCodeAttribute
            writer.WriteLine($"""[global::System.CodeDom.Compiler.GeneratedCodeAttribute("{AssemblyName}", "{AssemblyVersion}")]""");

            // Emit the main class declaration
            writer.WriteLine($"{nestedTypeDeclarations[0]} : {(generationSpec.Key is PrimaryKeySpec ? "IPrimaryKey" : "ICompositePrimaryKey")}<{generationSpec.TargetType.TypeName}>");
            writer.WriteLine("{");
            writer.Indentation++;

            return writer;
        }

        private static SourceText CompleteSourceFileAndReturnSourceText(SourceWriter writer)
        {
            while (writer.Indentation > 0)
            {
                writer.Indentation--;
                writer.WriteLine("}");
            }

            return writer.ToSourceText();
        }
    }
}