using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission.Parse;

internal sealed class EnumParseStrategy : IParseStrategy
{
    public static readonly EnumParseStrategy Instance = new();

    public void EmitSingleParse(SourceWriter writer, PropertyKeyPart part, string inputVar, string outputVar, bool shouldThrow, bool skipRedundantLengthCheck)
    {
        if (part.Property.EnumSpec is null)
            throw new InvalidOperationException($"{nameof(part.Property.EnumSpec)} is null");

        writer.WriteLines($"""
                           if (!{part.Property.EnumSpec.Name}Helper.TryParse({inputVar}, out var {outputVar}))
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                           """);
    }

    public void EmitRepeatingItemParse(SourceWriter writer, PropertyKeyPart part, string itemInput, string itemVar, string listVar, bool shouldThrow)
    {
        if (part.Property.EnumSpec is null)
            throw new InvalidOperationException($"{nameof(part.Property.EnumSpec)} is null");

        writer.WriteLines($"""
                           if (!{part.Property.EnumSpec.Name}Helper.TryParse({itemInput}, out var {itemVar}))
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};
                           {listVar}.Add({itemVar});
                           """);
    }
}
