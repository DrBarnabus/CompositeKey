using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission;

internal interface IParseStrategy
{
    void EmitSingleParse(SourceWriter writer, PropertyKeyPart part, string inputVar, string outputVar, bool shouldThrow);

    void EmitRepeatingItemParse(SourceWriter writer, PropertyKeyPart part, string itemInput, string itemVar, string listVar, bool shouldThrow);
}
