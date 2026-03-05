using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission.Parse;

internal sealed class StringParseStrategy : IParseStrategy
{
    public static readonly StringParseStrategy Instance = new();

    public void EmitSingleParse(SourceWriter writer, PropertyKeyPart part, string inputVar, string outputVar, bool shouldThrow)
    {
        writer.WriteLines($"""
                           if ({inputVar}.Length == 0)
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                           string {outputVar} = {inputVar}.ToString();

                           """);
    }

    public void EmitRepeatingItemParse(SourceWriter writer, PropertyKeyPart part, string itemInput, string itemVar, string listVar, bool shouldThrow)
    {
        writer.WriteLines($"""
                           if ({itemInput}.Length == 0)
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};
                           {listVar}.Add({itemInput}.ToString());
                           """);
    }
}
