using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission.Format;

internal sealed class EnumFormatStrategy : IFormatStrategy
{
    public static readonly EnumFormatStrategy Instance = new();

    public bool SupportsSpanFormat(PropertyKeyPart part) => part.Format == "g";

    public string GetVariableLengthExpression(PropertyKeyPart part)
    {
        if (part.Property.EnumSpec is null)
            throw new InvalidOperationException($"{nameof(part.Property.EnumSpec)} is null");

        return $"{part.Property.EnumSpec.Name}Helper.GetFormattedLength({part.Property.Name})";
    }

    public void EmitSpanFormat(SourceWriter writer, PropertyKeyPart part, string positionVar, bool invariantFormatting)
    {
        if (part.Property.EnumSpec is null)
            throw new InvalidOperationException($"{nameof(part.Property.EnumSpec)} is null");

        string charsWritten = $"{part.Property.CamelCaseName}CharsWritten";

        writer.StartBlock();
        writer.WriteLine($"if (!{part.Property.EnumSpec.Name}Helper.TryFormat(state.{part.Property.Name}, destination[{positionVar}..], out int {charsWritten}))");
        writer.WriteLine("\tthrow new FormatException();\n");
        writer.WriteLine($"{positionVar} += {charsWritten};");
        writer.EndBlock();
    }
}
