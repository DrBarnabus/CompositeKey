using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.Tokenization;
using CompositeKey.Analyzers.Common.Validation;

namespace CompositeKey.Analyzers.Common.UnitTests.Validation;

public static class TemplateValidationTests
{
    public class ValidateTemplateStringTests
    {
        [Fact]
        public void ValidTemplate_ShouldReturnSuccess()
        {
            // Arrange
            const string templateString = "USER#{UserId}";

            // Act
            var result = TemplateValidation.ValidateTemplateString(templateString);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("   ")]
        [InlineData("\t")]
        [InlineData("\n")]
        public void EmptyOrWhitespaceTemplate_ShouldReturnFailure(string? templateString)
        {
            // Act
            var result = TemplateValidation.ValidateTemplateString(templateString);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.EmptyOrInvalidTemplateString);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs.Length.ShouldBe(1);
            result.MessageArgs[0].ShouldBe(templateString ?? string.Empty);
        }
    }

    public class ValidatePrimaryKeySeparatorTests
    {
        [Fact]
        public void TemplateWithPrimaryKeySeparator_WhenSeparatorPresent_ShouldReturnSuccess()
        {
            // Arrange
            const string templateString = "USER#{UserId}#POST#{PostId}";
            const char separator = '#';
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.Property("UserId"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Constant("POST"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("PostId")
            };

            // Act
            var result = TemplateValidation.ValidatePrimaryKeySeparator(templateString, separator, tokens);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void TemplateWithoutPrimaryKeySeparator_WhenSeparatorNotRequired_ShouldReturnSuccess()
        {
            // Arrange
            const string templateString = "USER#{UserId}";
            char? separator = null;
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId")
            };

            // Act
            var result = TemplateValidation.ValidatePrimaryKeySeparator(templateString, separator, tokens);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void TemplateWithoutPrimaryKeySeparator_WhenSeparatorRequired_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "USER#{UserId}";
            const char separator = '#';
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId")
            };

            // Act
            var result = TemplateValidation.ValidatePrimaryKeySeparator(templateString, separator, tokens);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PrimaryKeySeparatorMissingFromTemplateString);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs.Length.ShouldBe(2);
            result.MessageArgs[0].ShouldBe(templateString);
            result.MessageArgs[1].ShouldBe(separator);
        }
    }

    public class ValidatePropertyReferencesTests
    {
        [Fact]
        public void AllPropertiesExistWithGettersAndSetters_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("Name")
            };

            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("UserId", HasGetter: true, HasSetter: true),
                new TemplateValidation.PropertyInfo("Name", HasGetter: true, HasSetter: true),
                new TemplateValidation.PropertyInfo("Email", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidatePropertyReferences(tokens, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void PropertyDoesNotExist_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Property("NonExistentProperty")
            };

            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("UserId", HasGetter: true, HasSetter: true),
                new TemplateValidation.PropertyInfo("Name", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidatePropertyReferences(tokens, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs.Length.ShouldBe(1);
            result.MessageArgs[0].ShouldBe("NonExistentProperty");
        }

        [Fact]
        public void PropertyWithoutGetter_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Property("WriteOnlyProperty")
            };

            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("WriteOnlyProperty", HasGetter: false, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidatePropertyReferences(tokens, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
        }

        [Fact]
        public void PropertyWithoutSetter_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Property("ReadOnlyProperty")
            };

            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("ReadOnlyProperty", HasGetter: true, HasSetter: false)
            };

            // Act
            var result = TemplateValidation.ValidatePropertyReferences(tokens, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
        }

        [Fact]
        public void NoPropertyTokens_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Constant("123")
            };

            var availableProperties = Array.Empty<TemplateValidation.PropertyInfo>();

            // Act
            var result = TemplateValidation.ValidatePropertyReferences(tokens, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void RepeatingPropertyExistsWithGetterAndSetter_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("PREFIX"),
                TemplateToken.Delimiter('#'),
                TemplateToken.RepeatingProperty("Tags", '#')
            };

            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("Tags", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidatePropertyReferences(tokens, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void RepeatingPropertyDoesNotExist_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.RepeatingProperty("NonExistent", '#')
            };

            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("Tags", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidatePropertyReferences(tokens, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("NonExistent");
        }

        [Fact]
        public void RepeatingPropertyWithoutSetter_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.RepeatingProperty("Tags", '#')
            };

            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("Tags", HasGetter: true, HasSetter: false)
            };

            // Act
            var result = TemplateValidation.ValidatePropertyReferences(tokens, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
        }
    }

    public class TokenizeTemplateStringTests
    {
        [Fact]
        public void SimpleTemplate_ShouldTokenizeCorrectly()
        {
            // Arrange
            const string templateString = "USER#{UserId}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeTrue();
            result.Tokens.Count.ShouldBe(3);
            result.Tokens[0].ShouldBeOfType<ConstantTemplateToken>();
            ((ConstantTemplateToken)result.Tokens[0]).Value.ShouldBe("USER");
            result.Tokens[1].ShouldBeOfType<DelimiterTemplateToken>();
            result.Tokens[2].ShouldBeOfType<PropertyTemplateToken>();
            ((PropertyTemplateToken)result.Tokens[2]).Name.ShouldBe("UserId");
        }

        [Fact]
        public void TemplateWithFormatSpecifier_ShouldTokenizeCorrectly()
        {
            // Arrange
            const string templateString = "{Id:D}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeTrue();
            result.Tokens.Count.ShouldBe(1);
            result.Tokens[0].ShouldBeOfType<PropertyTemplateToken>();
            var propertyToken = (PropertyTemplateToken)result.Tokens[0];
            propertyToken.Name.ShouldBe("Id");
            propertyToken.Format.ShouldBe("D");
        }

        [Fact]
        public void TemplateWithSinglePrimaryKeySeparator_ShouldIdentifyPrimaryDelimiter()
        {
            // Arrange
            const string templateString = "USER_{UserId}#{PostId}";
            const char separator = '#';

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeTrue();
            result.Tokens.OfType<PrimaryDelimiterTemplateToken>().Count().ShouldBe(1);
            result.Tokens.OfType<DelimiterTemplateToken>().Count().ShouldBe(1); // Only the '_'
            var primaryDelimiterIndex = result.Tokens.FindIndex(t => t is PrimaryDelimiterTemplateToken);
            primaryDelimiterIndex.ShouldBe(3); // After "USER", "_", and "{UserId}"
        }

        [Fact]
        public void TemplateWithMultiplePrimaryKeySeparators_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "USER#{UserId}#POST#{PostId}";
            const char separator = '#';

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeFalse(); // Multiple '#' when '#' is the primary separator is invalid
        }

        [Fact]
        public void EmptyTemplate_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public void MalformedProperty_UnclosedBrace_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "{UserId";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public void NestedBraces_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "{User{Id}}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public void MultipleDelimiters_ShouldTokenizeCorrectly()
        {
            // Arrange
            const string templateString = "A-B_C#D";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeTrue();
            result.Tokens.Count.ShouldBe(7);
            result.Tokens[0].ShouldBeOfType<ConstantTemplateToken>();
            result.Tokens[1].ShouldBeOfType<DelimiterTemplateToken>();
            result.Tokens[2].ShouldBeOfType<ConstantTemplateToken>();
            result.Tokens[3].ShouldBeOfType<DelimiterTemplateToken>();
            result.Tokens[4].ShouldBeOfType<ConstantTemplateToken>();
            result.Tokens[5].ShouldBeOfType<DelimiterTemplateToken>();
            result.Tokens[6].ShouldBeOfType<ConstantTemplateToken>();
        }
    }

    public class TokenizeRepeatingPropertyTests
    {
        [Fact]
        public void RepeatingPropertyWithoutFormat_ShouldTokenizeCorrectly()
        {
            // Arrange
            const string templateString = "{Prop...#}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeTrue();
            result.Tokens.Count.ShouldBe(1);
            result.Tokens[0].ShouldBeOfType<RepeatingPropertyTemplateToken>();
            var token = (RepeatingPropertyTemplateToken)result.Tokens[0];
            token.Name.ShouldBe("Prop");
            token.Separator.ShouldBe('#');
            token.Format.ShouldBeNull();
        }

        [Fact]
        public void RepeatingPropertyWithFormat_ShouldTokenizeCorrectly()
        {
            // Arrange
            const string templateString = "{Prop:D...#}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeTrue();
            result.Tokens.Count.ShouldBe(1);
            result.Tokens[0].ShouldBeOfType<RepeatingPropertyTemplateToken>();
            var token = (RepeatingPropertyTemplateToken)result.Tokens[0];
            token.Name.ShouldBe("Prop");
            token.Format.ShouldBe("D");
            token.Separator.ShouldBe('#');
        }

        [Fact]
        public void RepeatingPropertyWithNoSeparator_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "{Prop...}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public void RepeatingPropertyWithMultiCharSeparator_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "{Prop...##}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public void RepeatingPropertyWithDigitSeparator_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "{Prop...1}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public void RepeatingPropertyWithLetterSeparator_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "{Prop...a}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeFalse();
        }

        [Fact]
        public void RepeatingPropertyWithConstantAndDelimiter_ShouldTokenizeCorrectly()
        {
            // Arrange
            const string templateString = "LOCATION#{Prop...#}";
            char? separator = null;

            // Act
            var result = TemplateValidation.TokenizeTemplateString(templateString, separator);

            // Assert
            result.Success.ShouldBeTrue();
            result.Tokens.Count.ShouldBe(3);
            result.Tokens[0].ShouldBeOfType<ConstantTemplateToken>();
            ((ConstantTemplateToken)result.Tokens[0]).Value.ShouldBe("LOCATION");
            result.Tokens[1].ShouldBeOfType<DelimiterTemplateToken>();
            result.Tokens[2].ShouldBeOfType<RepeatingPropertyTemplateToken>();
            var token = (RepeatingPropertyTemplateToken)result.Tokens[2];
            token.Name.ShouldBe("Prop");
            token.Separator.ShouldBe('#');
        }
    }

    public class HasValidTemplateStructureTests
    {
        [Fact]
        public void TemplateWithPropertyToken_ShouldReturnTrue()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Property("UserId")
            };

            // Act
            var result = TemplateValidation.HasValidTemplateStructure(tokens);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public void TemplateWithConstantToken_ShouldReturnTrue()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER")
            };

            // Act
            var result = TemplateValidation.HasValidTemplateStructure(tokens);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public void TemplateWithMixedValueTokens_ShouldReturnTrue()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId")
            };

            // Act
            var result = TemplateValidation.HasValidTemplateStructure(tokens);

            // Assert
            result.ShouldBeTrue();
        }

        [Fact]
        public void TemplateWithOnlyDelimiters_ShouldReturnFalse()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Delimiter('#'),
                TemplateToken.Delimiter('-'),
                TemplateToken.PrimaryDelimiter('#')
            };

            // Act
            var result = TemplateValidation.HasValidTemplateStructure(tokens);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void EmptyTokenList_ShouldReturnFalse()
        {
            // Arrange
            var tokens = new List<TemplateToken>();

            // Act
            var result = TemplateValidation.HasValidTemplateStructure(tokens);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void TemplateWithRepeatingPropertyToken_ShouldReturnTrue()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.RepeatingProperty("Tags", '#')
            };

            // Act
            var result = TemplateValidation.HasValidTemplateStructure(tokens);

            // Assert
            result.ShouldBeTrue();
        }
    }

    public class ValidatePartitionAndSortKeyStructureTests
    {
        [Fact]
        public void ValidCompositeKey_ShouldReturnTrue()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId"),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.Constant("POST"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("PostId")
            };

            // Act
            var result = TemplateValidation.ValidatePartitionAndSortKeyStructure(tokens, out int primaryDelimiterIndex);

            // Assert
            result.ShouldBeTrue();
            primaryDelimiterIndex.ShouldBe(3);
        }

        [Fact]
        public void NoPrimaryDelimiter_ShouldReturnTrue()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId")
            };

            // Act
            var result = TemplateValidation.ValidatePartitionAndSortKeyStructure(tokens, out int primaryDelimiterIndex);

            // Assert
            result.ShouldBeTrue();
            primaryDelimiterIndex.ShouldBe(-1);
        }

        [Fact]
        public void PrimaryDelimiterWithoutPartitionKeyValues_ShouldReturnFalse()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.Constant("POST"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("PostId")
            };

            // Act
            var result = TemplateValidation.ValidatePartitionAndSortKeyStructure(tokens, out _);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void PrimaryDelimiterWithoutSortKeyValues_ShouldReturnFalse()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId"),
                TemplateToken.PrimaryDelimiter('#')
            };

            // Act
            var result = TemplateValidation.ValidatePartitionAndSortKeyStructure(tokens, out _);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void PrimaryDelimiterWithOnlyDelimitersOnBothSides_ShouldReturnFalse()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Delimiter('#'),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.Delimiter('#')
            };

            // Act
            var result = TemplateValidation.ValidatePartitionAndSortKeyStructure(tokens, out _);

            // Assert
            result.ShouldBeFalse();
        }

        [Fact]
        public void CompositeKeyWithRepeatingPropertyInSortKey_ShouldReturnTrue()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId"),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.Constant("TAG"),
                TemplateToken.Delimiter('#'),
                TemplateToken.RepeatingProperty("Tags", '#')
            };

            // Act
            var result = TemplateValidation.ValidatePartitionAndSortKeyStructure(tokens, out int primaryDelimiterIndex);

            // Assert
            result.ShouldBeTrue();
            primaryDelimiterIndex.ShouldBe(3);
        }
    }

    public class ValidateRepeatingPropertyPositionTests
    {
        [Fact]
        public void RepeatingPropertyAsLastPart_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId"),
                TemplateToken.Delimiter('#'),
                TemplateToken.RepeatingProperty("Tags", '#')
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyPosition(tokens);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void RepeatingPropertyNotLastPart_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.RepeatingProperty("Tags", '#'),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId")
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyPosition(tokens);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.RepeatingPropertyMustBeLastPart);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("Tags");
        }

        [Fact]
        public void NoRepeatingProperties_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId")
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyPosition(tokens);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void CompositeKey_RepeatingPropertyLastInSortKey_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Property("UserId"),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.Constant("TAG"),
                TemplateToken.Delimiter('-'),
                TemplateToken.RepeatingProperty("Tags", ',')
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyPosition(tokens);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void CompositeKey_RepeatingPropertyNotLastInPartitionKey_ShouldReturnFailure()
        {
            // Arrange - repeating property in partition key section, not at the end
            var tokens = new List<TemplateToken>
            {
                TemplateToken.RepeatingProperty("Tags", ','),
                TemplateToken.Delimiter('-'),
                TemplateToken.Constant("SUFFIX"),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.Property("SortKey")
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyPosition(tokens);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.RepeatingPropertyMustBeLastPart);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("Tags");
        }

        [Fact]
        public void CompositeKey_RepeatingPropertyNotLastInSortKey_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Property("UserId"),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.RepeatingProperty("Tags", ','),
                TemplateToken.Delimiter('-'),
                TemplateToken.Constant("SUFFIX")
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyPosition(tokens);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.RepeatingPropertyMustBeLastPart);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("Tags");
        }
    }

    public class ValidateRepeatingPropertyCountTests
    {
        [Fact]
        public void SingleRepeatingProperty_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("PREFIX"),
                TemplateToken.Delimiter('#'),
                TemplateToken.RepeatingProperty("Tags", '#')
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyCount(tokens);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void NoRepeatingProperties_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Constant("USER"),
                TemplateToken.Delimiter('#'),
                TemplateToken.Property("UserId")
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyCount(tokens);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void MultipleRepeatingPropertiesInSimpleKey_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.RepeatingProperty("Tags", '#'),
                TemplateToken.Delimiter('-'),
                TemplateToken.RepeatingProperty("Items", ',')
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyCount(tokens);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.RepeatingPropertyMustBeLastPart);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("Items");
        }

        [Fact]
        public void CompositeKey_OneRepeatingPerSection_ShouldReturnSuccess()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.RepeatingProperty("Tags", ','),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.RepeatingProperty("Items", ',')
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyCount(tokens);

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void CompositeKey_MultipleRepeatingInSortKey_ShouldReturnFailure()
        {
            // Arrange
            var tokens = new List<TemplateToken>
            {
                TemplateToken.Property("UserId"),
                TemplateToken.PrimaryDelimiter('#'),
                TemplateToken.RepeatingProperty("Tags", ','),
                TemplateToken.Delimiter('-'),
                TemplateToken.RepeatingProperty("Items", ',')
            };

            // Act
            var result = TemplateValidation.ValidateRepeatingPropertyCount(tokens);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.RepeatingPropertyMustBeLastPart);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs[0].ShouldBe("Items");
        }
    }

    public class ValidateTemplateFormatTests
    {
        [Fact]
        public void ValidCompleteTemplate_ShouldReturnSuccess()
        {
            // Arrange
            const string templateString = "USER_{UserId}#{PostId}";
            const char separator = '#';
            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("UserId", HasGetter: true, HasSetter: true),
                new TemplateValidation.PropertyInfo("PostId", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }

        [Fact]
        public void EmptyTemplate_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "";
            char? separator = null;
            var availableProperties = Array.Empty<TemplateValidation.PropertyInfo>();

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.EmptyOrInvalidTemplateString);
        }

        [Fact]
        public void InvalidCompositeKeyStructure_NoPartitionKey_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "#{UserId}"; // No partition key part before separator
            const char separator = '#';
            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("UserId", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.EmptyOrInvalidTemplateString); // Invalid structure - no partition key
        }

        [Fact]
        public void InvalidCompositeKeyStructure_NoSortKey_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "{UserId}#"; // No sort key part after separator
            const char separator = '#';
            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("UserId", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.EmptyOrInvalidTemplateString); // Invalid structure - no sort key
        }

        [Fact]
        public void TemplateWithoutSeparatorChar_WhenSeparatorRequired_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "USER_{UserId}"; // No '#' character at all
            const char separator = '#';
            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("UserId", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PrimaryKeySeparatorMissingFromTemplateString);
        }

        [Fact]
        public void InvalidPropertyReference_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "{NonExistentProperty}";
            char? separator = null;
            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("UserId", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.PropertyMustHaveAccessibleGetterAndSetter);
        }

        [Fact]
        public void OnlyDelimiters_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "###";
            char? separator = null;
            var availableProperties = Array.Empty<TemplateValidation.PropertyInfo>();

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.EmptyOrInvalidTemplateString);
        }

        [Fact]
        public void InvalidPartitionKeyStructure_ShouldReturnFailure()
        {
            // Arrange
            const string templateString = "#{PostId}";
            const char separator = '#';
            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("PostId", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.EmptyOrInvalidTemplateString);
        }

        [Fact]
        public void ComplexValidTemplate_WithFormatSpecifiers_ShouldReturnSuccess()
        {
            // Arrange
            const string templateString = "USER_{UserId:D}#POST_{PostId:N}-{Timestamp:yyyy-MM-dd}";
            const char separator = '#';
            var availableProperties = new[]
            {
                new TemplateValidation.PropertyInfo("UserId", HasGetter: true, HasSetter: true),
                new TemplateValidation.PropertyInfo("PostId", HasGetter: true, HasSetter: true),
                new TemplateValidation.PropertyInfo("Timestamp", HasGetter: true, HasSetter: true)
            };

            // Act
            var result = TemplateValidation.ValidateTemplateFormat(templateString, separator, availableProperties.ToList());

            // Assert
            result.IsSuccess.ShouldBeTrue();
        }
    }
}
