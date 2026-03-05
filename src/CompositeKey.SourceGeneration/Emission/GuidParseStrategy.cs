using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model.Key;

namespace CompositeKey.SourceGeneration.Emission;

internal sealed class GuidParseStrategy : IParseStrategy
{
    public static readonly GuidParseStrategy Instance = new();

    public void EmitSingleParse(SourceWriter writer, PropertyKeyPart part, string inputVar, string outputVar, bool shouldThrow)
    {
        string strictLengthCheck = part.ExactLengthRequirement
            ? $"{inputVar}.Length != {part.LengthRequired} || "
            : string.Empty;

        writer.WriteLines($"""
                           if ({strictLengthCheck}!Guid.TryParseExact({inputVar}, "{part.Format}", out var {outputVar}))
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};

                           """);
    }

    public void EmitRepeatingItemParse(SourceWriter writer, PropertyKeyPart part, string itemInput, string itemVar, string listVar, bool shouldThrow)
    {
        writer.WriteLines($"""
                           if (!Guid.TryParseExact({itemInput}, "{part.Format}", out var {itemVar}))
                               {(shouldThrow ? "throw new FormatException(\"Unrecognized format.\")" : "return false")};
                           {listVar}.Add({itemVar});
                           """);
    }
}
