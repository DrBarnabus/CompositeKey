using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission.Format;

internal interface IFormatStrategy
{
    bool SupportsSpanFormat(PropertyKeyPart part);

    string GetVariableLengthExpression(PropertyKeyPart part);

    void EmitSpanFormat(SourceWriter writer, PropertyKeyPart part, string positionVar, bool invariantFormatting);
}
