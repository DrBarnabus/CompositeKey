using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model;

namespace CompositeKey.SourceGeneration;

public static class EnumGenerationHelper
{
    private const string MaybeNullWhen = "global::System.Diagnostics.CodeAnalysis.MaybeNullWhen";

    public static void EmitEnumHelperClass(SourceWriter writer, EnumSpec enumSpec)
    {
        using var classBlock = writer.Block($"file class {enumSpec.Name}Helper");

        if (enumSpec.IsSequentialFromZero)
        {
            writer.WriteLine($"private static readonly int[] Lengths = new[] {{ {string.Join(", ", enumSpec.Members.Select(m => m.Name.Length))} }};");
            writer.WriteLine($"private static readonly string[] Names = new[] {{ {string.Join(", ", enumSpec.Members.Select(m => $"\"{m.Name}\""))} }};");
            writer.WriteLine();
        }

        WriteGetFormattedLengthMethod(writer, enumSpec);
        writer.WriteLine();

        WriteTryFormatMethod(writer, enumSpec);
        writer.WriteLine();

        WriteTryParseMethod(writer, enumSpec);
        writer.WriteLine();
    }

    private static void WriteGetFormattedLengthMethod(SourceWriter writer, EnumSpec enumSpec)
    {
        using var methodBlock = writer.Block($"public static int GetFormattedLength({enumSpec.FullyQualifiedName} value)");

        if (enumSpec.IsSequentialFromZero)
        {
            writer.WriteLine("return Lengths[(uint)value];");
        }
        else
        {
            using var switchStatement = writer.Block("return value switch", true);

            foreach ((string memberName, _) in enumSpec.Members)
                writer.WriteLine($"{enumSpec.FullyQualifiedName}.{memberName} => {memberName.Length},");

            writer.WriteLine("_ => throw new ArgumentOutOfRangeException(nameof(value), value, \"The value provided is out of range.\")");
        }
    }

    private static void WriteTryFormatMethod(SourceWriter writer, EnumSpec enumSpec)
    {
        using var methodBlock = writer.Block($"public static bool TryFormat({enumSpec.FullyQualifiedName} value, Span<char> destination, out int charsWritten)");

        if (enumSpec.IsSequentialFromZero)
        {
            writer.WriteLines("""
                              charsWritten = 0;
                              if ((uint)value >= Names.Length)
                                  return false;

                              int formattedLength = Lengths[(uint)value];
                              if (destination.Length < formattedLength)
                                  return false;

                              charsWritten = formattedLength;
                              Names[(uint)value].CopyTo(destination);
                              return true;
                              """);
        }
        else
        {
            writer.WriteLines("""
                              charsWritten = GetFormattedLength(value);
                              if (destination.Length < charsWritten)
                                  return false;

                              """);

            using var switchBlock = writer.Block("switch (value)");

            foreach ((string memberName, _) in enumSpec.Members)
                writer.WriteLines($"""
                                   case {enumSpec.FullyQualifiedName}.{memberName}:
                                       "{memberName}".CopyTo(destination);
                                       return true;
                                   """);

            writer.WriteLines("""
                              default:
                                  charsWritten = 0;
                                  return false;
                              """);
        }
    }

    private static void WriteTryParseMethod(SourceWriter writer, EnumSpec enumSpec)
    {
        using var methodBlock = writer.Block($"public static bool TryParse(in ReadOnlySpan<char> value, [{MaybeNullWhen}(false)] out {enumSpec.FullyQualifiedName} result)");
        using var switchBlock = writer.Block("switch (value)");

        foreach ((string memberName, _) in enumSpec.Members)
            writer.WriteLines($"""
                               case var _ when value.Equals(nameof({enumSpec.FullyQualifiedName}.{memberName}), StringComparison.Ordinal):
                                   result = {enumSpec.FullyQualifiedName}.{memberName};
                                   return true;
                               """);

        writer.WriteLines("""
                          default:
                              result = default;
                              return false;
                          """);
    }
}
