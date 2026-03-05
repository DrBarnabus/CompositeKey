using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission.Format;

internal sealed class StringFormatStrategy : IFormatStrategy
{
    public static readonly StringFormatStrategy Instance = new();

    public bool SupportsSpanFormat(PropertyKeyPart part) => true;

    public string GetVariableLengthExpression(PropertyKeyPart part) => $"{part.Property.Name}.Length";

    public void EmitSpanFormat(SourceWriter writer, PropertyKeyPart part, string positionVar, bool invariantFormatting)
    {
        writer.WriteLine($"state.{part.Property.Name}.CopyTo(destination[{positionVar}..]);");
        writer.WriteLine($"{positionVar} += state.{part.Property.Name}.Length;");
    }
}
