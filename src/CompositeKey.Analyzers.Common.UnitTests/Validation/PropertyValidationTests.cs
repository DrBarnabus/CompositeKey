using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.Validation;

namespace CompositeKey.Analyzers.Common.UnitTests.Validation;

public static class PropertyValidationTests
{
    public class PropertyAccessibilityValidation
    {
        [Fact]
        public void PublicGetterAndSetter_ProducesNoErrors()
        {
            // Arrange
            var property = new PropertyValidation.PropertyAccessibilityInfo(
                Name: "TestProperty",
                HasGetter: true,
                HasSetter: true);

            // Act
            var result = PropertyValidation.ValidatePropertyAccessibility(property);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Descriptor.ShouldBeNull();
            result.MessageArgs.ShouldBeNull();
        }

        [Fact]
        public void MissingGetter_ReportsAccessibilityError()
        {
            // Arrange
            var property = new PropertyValidation.PropertyAccessibilityInfo(
                Name: "TestProperty",
                HasGetter: false,
                HasSetter: true);

            // Act
            var result = PropertyValidation.ValidatePropertyAccessibility(property);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("TestProperty");
        }

        [Fact]
        public void MissingSetter_ReportsAccessibilityError()
        {
            // Arrange
            var property = new PropertyValidation.PropertyAccessibilityInfo(
                Name: "TestProperty",
                HasGetter: true,
                HasSetter: false);

            // Act
            var result = PropertyValidation.ValidatePropertyAccessibility(property);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("TestProperty");
        }

        [Fact]
        public void NoGetterOrSetter_ReportsAccessibilityError()
        {
            // Arrange
            var property = new PropertyValidation.PropertyAccessibilityInfo(
                Name: "TestProperty",
                HasGetter: false,
                HasSetter: false);

            // Act
            var result = PropertyValidation.ValidatePropertyAccessibility(property);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("TestProperty");
        }
    }

    public class PropertyFormatValidation
    {
        [Theory]
        [InlineData("d")]
        [InlineData("n")]
        [InlineData("b")]
        [InlineData("p")]
        [InlineData("x")]
        [InlineData("D")]
        [InlineData("N")]
        [InlineData("B")]
        [InlineData("P")]
        [InlineData("X")]
        public void ValidGuidFormat_ProducesNoErrors(string format)
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.Guid",
                IsGuid: true,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyFormat("TestProperty", typeInfo, format);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Theory]
        [InlineData("z")]
        [InlineData("invalid")]
        [InlineData("123")]
        [InlineData("dd")]
        public void InvalidGuidFormat_ReportsFormatError(string format)
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.Guid",
                IsGuid: true,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyFormat("TestProperty", typeInfo, format);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("TestProperty");
            result.MessageArgs[1].ShouldBe(format);
        }

        [Fact]
        public void GuidWithNullFormat_ProducesNoErrors()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.Guid",
                IsGuid: true,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyFormat("TestProperty", typeInfo, null);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void StringWithNullFormat_ProducesNoErrors()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.String",
                IsGuid: false,
                IsString: true,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyFormat("TestProperty", typeInfo, null);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Theory]
        [InlineData("d")]
        [InlineData("n")]
        [InlineData("anything")]
        public void StringWithFormat_ReportsFormatError(string format)
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.String",
                IsGuid: false,
                IsString: true,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyFormat("TestProperty", typeInfo, format);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("TestProperty");
            result.MessageArgs[1].ShouldBe(format);
        }

        [Theory]
        [InlineData("g")]
        [InlineData("G")]
        [InlineData(null)]
        public void EnumWithValidFormat_ProducesNoErrors(string? format)
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "MyEnum",
                IsGuid: false,
                IsString: false,
                IsEnum: true,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyFormat("TestProperty", typeInfo, format);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Theory]
        [InlineData("d")]
        [InlineData("x")]
        [InlineData("f")]
        [InlineData("invalid")]
        public void EnumWithInvalidFormat_ReportsFormatError(string format)
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "MyEnum",
                IsGuid: false,
                IsString: false,
                IsEnum: true,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyFormat("TestProperty", typeInfo, format);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("TestProperty");
            result.MessageArgs[1].ShouldBe(format);
        }

        [Fact]
        public void SpanFormattableType_WithAnyFormat_ProducesNoErrors()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.DateTime",
                IsGuid: false,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: true,
                IsSpanFormattable: true);

            // Act
            var result = PropertyValidation.ValidatePropertyFormat("TestProperty", typeInfo, "yyyy-MM-dd");

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }
    }

    public class PropertyTypeCompatibilityValidation
    {
        [Fact]
        public void GuidType_IsCompatible()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.Guid",
                IsGuid: true,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyTypeCompatibility("TestProperty", typeInfo);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void StringType_IsCompatible()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.String",
                IsGuid: false,
                IsString: true,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyTypeCompatibility("TestProperty", typeInfo);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void EnumType_IsCompatible()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "MyEnum",
                IsGuid: false,
                IsString: false,
                IsEnum: true,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyTypeCompatibility("TestProperty", typeInfo);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void SpanParsableAndFormattableType_IsCompatible()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.DateTime",
                IsGuid: false,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: true,
                IsSpanFormattable: true);

            // Act
            var result = PropertyValidation.ValidatePropertyTypeCompatibility("TestProperty", typeInfo);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void OnlySpanParsableType_IsNotCompatible()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "CustomType",
                IsGuid: false,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: true,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyTypeCompatibility("TestProperty", typeInfo);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat);
        }

        [Fact]
        public void OnlySpanFormattableType_IsNotCompatible()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "CustomType",
                IsGuid: false,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: true);

            // Act
            var result = PropertyValidation.ValidatePropertyTypeCompatibility("TestProperty", typeInfo);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat);
        }

        [Fact]
        public void UnsupportedType_IsNotCompatible()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.Object",
                IsGuid: false,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.ValidatePropertyTypeCompatibility("TestProperty", typeInfo);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyHasInvalidOrUnsupportedFormat);
        }
    }

    public class CollectionPropertyTypeValidation
    {
        [Fact]
        public void ListType_IsCollection()
        {
            // Arrange
            var collectionTypeInfo = new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: "System.Collections.Generic.List<System.Guid>",
                IsList: true,
                IsReadOnlyList: false,
                IsImmutableArray: false);

            // Assert
            collectionTypeInfo.IsCollection.ShouldBeTrue();
        }

        [Fact]
        public void ReadOnlyListType_IsCollection()
        {
            // Arrange
            var collectionTypeInfo = new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: "System.Collections.Generic.IReadOnlyList<string>",
                IsList: false,
                IsReadOnlyList: true,
                IsImmutableArray: false);

            // Assert
            collectionTypeInfo.IsCollection.ShouldBeTrue();
        }

        [Fact]
        public void ImmutableArrayType_IsCollection()
        {
            // Arrange
            var collectionTypeInfo = new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: "System.Collections.Immutable.ImmutableArray<int>",
                IsList: false,
                IsReadOnlyList: false,
                IsImmutableArray: true);

            // Assert
            collectionTypeInfo.IsCollection.ShouldBeTrue();
        }

        [Fact]
        public void NonCollectionType_IsNotCollection()
        {
            // Arrange
            var collectionTypeInfo = new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: "System.String",
                IsList: false,
                IsReadOnlyList: false,
                IsImmutableArray: false);

            // Assert
            collectionTypeInfo.IsCollection.ShouldBeFalse();
        }

        [Fact]
        public void RepeatingPropertyWithCollectionType_ShouldReturnSuccess()
        {
            // Arrange
            var collectionTypeInfo = new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: "System.Collections.Generic.List<System.Guid>",
                IsList: true,
                IsReadOnlyList: false,
                IsImmutableArray: false);

            // Act
            var result = PropertyValidation.ValidateCollectionPropertyType("Tags", collectionTypeInfo);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void RepeatingPropertyWithNonCollectionType_ShouldReturnFailure()
        {
            // Arrange
            var collectionTypeInfo = new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: "System.String",
                IsList: false,
                IsReadOnlyList: false,
                IsImmutableArray: false);

            // Act
            var result = PropertyValidation.ValidateCollectionPropertyType("Name", collectionTypeInfo);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.RepeatingPropertyMustUseCollectionType);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("Name");
        }

        [Fact]
        public void NonRepeatingPropertyWithCollectionType_ShouldReturnFailure()
        {
            // Arrange
            var collectionTypeInfo = new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: "System.Collections.Generic.List<System.Guid>",
                IsList: true,
                IsReadOnlyList: false,
                IsImmutableArray: false);

            // Act
            var result = PropertyValidation.ValidateNonCollectionPropertyType("Tags", collectionTypeInfo);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.CollectionPropertyMustUseRepeatingSyntax);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("Tags");
        }

        [Fact]
        public void NonRepeatingPropertyWithNonCollectionType_ShouldReturnSuccess()
        {
            // Arrange
            var collectionTypeInfo = new PropertyValidation.CollectionPropertyTypeInfo(
                TypeName: "System.String",
                IsList: false,
                IsReadOnlyList: false,
                IsImmutableArray: false);

            // Act
            var result = PropertyValidation.ValidateNonCollectionPropertyType("Name", collectionTypeInfo);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }
    }

    public class FormattedLengthCalculation
    {
        [Theory]
        [InlineData("d", 36, true)]
        [InlineData("n", 32, true)]
        [InlineData("b", 38, true)]
        [InlineData("p", 38, true)]
        [InlineData("x", 32, false)]
        [InlineData("D", 36, true)]
        [InlineData("N", 32, true)]
        [InlineData("B", 38, true)]
        [InlineData("P", 38, true)]
        [InlineData("X", 32, false)]
        [InlineData(null, 36, true)]
        public void GuidFormattedLength_ReturnsCorrectValues(string? format, int expectedLength, bool expectedExact)
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.Guid",
                IsGuid: true,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.GetFormattedLength(typeInfo, format);

            // Assert
            result.ShouldNotBeNull();
            result.Value.length.ShouldBe(expectedLength);
            result.Value.isExact.ShouldBe(expectedExact);
        }

        [Fact]
        public void StringFormattedLength_ReturnsMinimumOne()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.String",
                IsGuid: false,
                IsString: true,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.GetFormattedLength(typeInfo, null);

            // Assert
            result.ShouldNotBeNull();
            result.Value.length.ShouldBe(1);
            result.Value.isExact.ShouldBe(false);
        }

        [Fact]
        public void EnumFormattedLength_ReturnsMinimumOne()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "MyEnum",
                IsGuid: false,
                IsString: false,
                IsEnum: true,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.GetFormattedLength(typeInfo, "g");

            // Assert
            result.ShouldNotBeNull();
            result.Value.length.ShouldBe(1);
            result.Value.isExact.ShouldBe(false);
        }

        [Fact]
        public void SpanFormattableFormattedLength_ReturnsMinimumOne()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.DateTime",
                IsGuid: false,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: true,
                IsSpanFormattable: true);

            // Act
            var result = PropertyValidation.GetFormattedLength(typeInfo, "yyyy-MM-dd");

            // Assert
            result.ShouldNotBeNull();
            result.Value.length.ShouldBe(1);
            result.Value.isExact.ShouldBe(false);
        }

        [Fact]
        public void GuidWithInvalidFormat_ReturnsNull()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.Guid",
                IsGuid: true,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.GetFormattedLength(typeInfo, "invalid");

            // Assert
            result.ShouldBeNull();
        }

        [Fact]
        public void UnsupportedType_ReturnsNull()
        {
            // Arrange
            var typeInfo = new PropertyValidation.PropertyTypeInfo(
                TypeName: "System.Object",
                IsGuid: false,
                IsString: false,
                IsEnum: false,
                IsSpanParsable: false,
                IsSpanFormattable: false);

            // Act
            var result = PropertyValidation.GetFormattedLength(typeInfo, null);

            // Assert
            result.ShouldBeNull();
        }
    }
}
