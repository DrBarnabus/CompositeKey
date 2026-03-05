using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission;

internal sealed class SpanParsableParseStrategy : IParseStrategy
{
    public static readonly SpanParsableParseStrategy Instance = new();

    public void EmitSingleParse(SourceWriter writer, PropertyKeyPart part, string inputVar, string outputVar, bool shouldThrow)
    {
        writer.WriteLines($"""
                           if (!{part.Property.Type.FullyQualifiedName}.TryParse({inputVar}, out var {outputVar}))
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                           """);
    }

    public void EmitRepeatingItemParse(SourceWriter writer, PropertyKeyPart part, string itemInput, string itemVar, string listVar, bool shouldThrow)
    {
        string innerTypeName = part.CollectionSemantics!.InnerType.FullyQualifiedName;

        writer.WriteLines($"""
                           if (!{innerTypeName}.TryParse({itemInput}, out var {itemVar}))
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};
                           {listVar}.Add({itemVar});
                           """);
    }
}
