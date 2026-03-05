using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission;

internal sealed class GuidFormatStrategy : IFormatStrategy
{
    public static readonly GuidFormatStrategy Instance = new();

    private const string InvariantCulture = "global::System.Globalization.CultureInfo.InvariantCulture";

    public bool SupportsSpanFormat(PropertyKeyPart part) => part.ExactLengthRequirement;

    public string GetVariableLengthExpression(PropertyKeyPart part) =>
        throw new InvalidOperationException("Guid types with variable length are not supported in span format path.");

    public void EmitSpanFormat(SourceWriter writer, PropertyKeyPart part, string positionVar, bool invariantFormatting)
    {
        string formatProvider = invariantFormatting ? InvariantCulture : "null";
        string charsWritten = $"{part.Property.CamelCaseName}CharsWritten";

        writer.StartBlock();
        writer.WriteLine($"if (!((ISpanFormattable)state.{part.Property.Name}).TryFormat(destination[{positionVar}..], out int {charsWritten}, \"{part.Format ?? "d"}\", {formatProvider}))");
        writer.WriteLine("\tthrow new FormatException();\n");
        writer.WriteLine($"{positionVar} += {charsWritten};");
        writer.EndBlock();
    }
}
