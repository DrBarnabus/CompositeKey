using CompositeKey.SourceGeneration.Core;
using CompositeKey.SourceGeneration.Model;
using CompositeKey.SourceGeneration.Model.Key;
using Microsoft.CodeAnalysis;

namespace CompositeKey.SourceGeneration.UnitTests.Emission.Parse;

public partial class ParseStrategyTests
{
    private static readonly Compilation TestCompilation = CompilationHelper.CreateCompilation("""
        using System;
        public class Placeholder { }
        public enum Status { Active, Inactive }
        """);

    private static TypeRef GetTypeRef(string fullyQualifiedMetadataName)
    {
        var symbol = TestCompilation.GetTypeByMetadataName(fullyQualifiedMetadataName)!;
        return new TypeRef(symbol);
    }

    private static PropertyKeyPart CreateGuidPart(string format = "D", bool exactLength = true, int length = 36) => new(
        Property: new PropertySpec(GetTypeRef("System.Guid"), "Id", "id", false, true, false, false, null),
        Format: format,
        TypeDescriptor: new PropertyTypeDescriptor(ParseType.Guid, FormatType.Guid))
    {
        LengthRequired = length,
        ExactLengthRequirement = exactLength
    };

    private static PropertyKeyPart CreateStringPart() => new(
        Property: new PropertySpec(GetTypeRef("System.String"), "Name", "name", false, true, false, false, null),
        Format: null,
        TypeDescriptor: new PropertyTypeDescriptor(ParseType.String, FormatType.String))
    {
        LengthRequired = 0,
        ExactLengthRequirement = false
    };

    private static PropertyKeyPart CreateEnumPart()
    {
        var enumSpec = new EnumSpec(
            "Status", "global::Status", "int",
            new ImmutableEquatableArray<EnumSpec.Member>([
                new EnumSpec.Member("Active", 0),
                new EnumSpec.Member("Inactive", 1)
            ]),
            IsSequentialFromZero: true);

        return new PropertyKeyPart(
            Property: new PropertySpec(GetTypeRef("Status"), "Status", "status", false, true, false, false, enumSpec),
            Format: "g",
            TypeDescriptor: new PropertyTypeDescriptor(ParseType.Enum, FormatType.Enum))
        {
            LengthRequired = 0,
            ExactLengthRequirement = false
        };
    }

    private static PropertyKeyPart CreateSpanParsablePart() => new(
        Property: new PropertySpec(GetTypeRef("System.Int32"), "Count", "count", false, true, false, false, null),
        Format: null,
        TypeDescriptor: new PropertyTypeDescriptor(ParseType.SpanParsable, FormatType.SpanFormattable))
    {
        LengthRequired = 0,
        ExactLengthRequirement = false
    };

    private static PropertyKeyPart CreateRepeatingPart(ParseType parseType, FormatType formatType, EnumSpec? enumSpec = null)
    {
        var innerType = parseType switch
        {
            ParseType.Guid => GetTypeRef("System.Guid"),
            ParseType.String => GetTypeRef("System.String"),
            ParseType.Enum => GetTypeRef("Status"),
            ParseType.SpanParsable => GetTypeRef("System.Int32"),
            _ => throw new ArgumentOutOfRangeException(nameof(parseType))
        };

        return new PropertyKeyPart(
            Property: new PropertySpec(GetTypeRef("System.String"), "Items", "items", false, true, false, false, enumSpec),
            Format: parseType == ParseType.Guid ? "D" : (parseType == ParseType.Enum ? "g" : null),
            TypeDescriptor: new PropertyTypeDescriptor(parseType, formatType),
            CollectionSemantics: new CollectionSemantics(',', innerType))
        {
            LengthRequired = 0,
            ExactLengthRequirement = false
        };
    }

    private static string EmitToString(Action<SourceWriter> action)
    {
        var writer = new SourceWriter();
        action(writer);
        return writer.ToSourceText().ToString();
    }
}
