using CompositeKey.Analyzers.Analyzers;
using CompositeKey.Analyzers.UnitTests.Infrastructure;
using Microsoft.CodeAnalysis;

namespace CompositeKey.Analyzers.UnitTests.Analyzers;

/// <summary>
/// Comprehensive tests for TemplateStringAnalyzer validating template string format,
/// primary key separator requirements, and precise diagnostic location targeting.
/// </summary>
public static class TemplateStringAnalyzerTests
{
    /// <summary>
    /// Tests for template format validation (COMPOSITE0005).
    /// </summary>
    public class TemplateFormatValidation
    {
        [Fact]
        public async Task ValidTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("{Id}#{Name}")]
                    public partial record TestRecord(string Id, string Name);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ValidTemplateWithConstants_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("USER_{UserId}_TENANT_{TenantId}")]
                    public partial record UserKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ValidTemplateWithFormatting_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;
                    using System;

                    [CompositeKey("Order_{OrderId:D8}_{Date:yyyy-MM-dd}")]
                    public partial record OrderKey(int OrderId, DateTime Date);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task EmptyTemplate_ReportsAtTemplateString()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:""|})]
                    public partial record TestRecord(string Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task WhitespaceOnlyTemplate_ReportsAtTemplateString()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"   "|})]
                    public partial record TestRecord(string Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task TemplateWithOnlyProperty_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("{Id}")]
                    public partial record TestRecord(string Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task TemplateWithOnlyConstant_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("CONSTANT_KEY")]
                    public partial record TestRecord();
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ComplexTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;
                    using System;

                    [CompositeKey("PREFIX_{Part1}_{Part2:D3}_{Part3}_SUFFIX")]
                    public partial record ComplexKey(string Part1, int Part2, Guid Part3);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task TemplateWithOnlyDelimiter_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"#"|})]
                    public partial record TestKey(string Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task TemplateWithUnclosedPropertyBrace_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"{UnfinishedProperty"|})]
                    public partial record TestKey(string UnfinishedProperty);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task TemplateWithNestedBraces_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"{Property{Nested}}"|})]
                    public partial record TestKey(string Property);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Tests for primary key separator validation (COMPOSITE0006).
    /// </summary>
    public class PrimaryKeySeparatorValidation
    {
        [Fact]
        public async Task TemplateWithSeparator_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}|{TenantId}", PrimaryKeySeparator = '|')]
                    public partial record UserKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MissingSeparator_ReportsAtPrimaryKeySeparatorProperty()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}_{TenantId}", {|COMPOSITE0006:PrimaryKeySeparator = '#'|})]
                    public partial record UserKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task OnlySeparator_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"|"|}, PrimaryKeySeparator = '|')]
                    public partial record UserKey(string UserId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task SeparatorAtBeginning_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"|{UserId}_{TenantId}"|}, PrimaryKeySeparator = '|')]
                    public partial record UserKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task SeparatorAtEnd_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"{UserId}_{TenantId}|"|}, PrimaryKeySeparator = '|')]
                    public partial record UserKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MultipleSeparatorOccurrences_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("{Part1}|{Part2}_{Part3}", PrimaryKeySeparator = '|')]
                    public partial record ComplexKey(string Part1, string Part2, string Part3);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task SeparatorWithoutProperty_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("{UserId}#{TenantId}")]
                    public partial record UserKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ComplexTemplateWithMissingSeparator_ReportsError()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;
                    using System;

                    [CompositeKey("PREFIX_{Part1}_{Part2:D3}_{Part3}_SUFFIX", {|COMPOSITE0006:PrimaryKeySeparator = '|'|})]
                    public partial record ComplexKey(string Part1, int Part2, Guid Part3);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task SeparatorBetweenDelimitersOnly_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"_|_"|}, PrimaryKeySeparator = '|')]
                    public partial record TestKey(string Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MultipleSeparatorsWithInvalidStructure_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"{Part1}|{Part2}|"|}, PrimaryKeySeparator = '|')]
                    public partial record TestKey(string Part1, string Part2);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task SeparatorAsFirstAndLastChar_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"|{UserId}|"|}, PrimaryKeySeparator = '|')]
                    public partial record TestKey(string UserId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ConsecutiveSeparators_ReportsInvalidTemplate()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0005:"{Part1}||{Part2}"|}, PrimaryKeySeparator = '|')]
                    public partial record TestKey(string Part1, string Part2);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

    }

    /// <summary>
    /// Tests for precise location targeting within template strings.
    /// </summary>
    public class LocationPrecision
    {
        [Fact]
        public async Task EmptyTemplate_TargetsTemplateStringArgument()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey(
                        {|COMPOSITE0005:""|}
                    )]
                    public partial record TestRecord(string Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MissingSeparator_TargetsPrimaryKeySeparatorProperty()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey(
                        "User_{UserId}_{TenantId}",
                        {|COMPOSITE0006:PrimaryKeySeparator = '#'|}
                    )]
                    public partial record UserKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task AttributeWithNamedArguments_TargetsCorrectly()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey(
                        template: {|COMPOSITE0005:""|},
                        InvariantCulture = true
                    )]
                    public partial record TestRecord(string Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MultipleNamedArguments_TargetsSeparatorCorrectly()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey(
                        "User_{UserId}_{TenantId}",
                        InvariantCulture = false,
                        {|COMPOSITE0006:PrimaryKeySeparator = '|'|}
                    )]
                    public partial record UserKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Tests for edge cases and integration scenarios.
    /// </summary>
    public class EdgeCasesAndIntegration
    {
        [Fact]
        public async Task RecordWithoutAttribute_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    public partial record UserKey(string UserId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MultipleTypesWithDifferentTemplateIssues()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    // Valid template
                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey(string UserId);

                    // Empty template error
                    [CompositeKey({|COMPOSITE0005:""|})]
                    public partial record EmptyKey(string Id);

                    // Missing separator error
                    [CompositeKey("Product_{ProductId}_{Version}", {|COMPOSITE0006:PrimaryKeySeparator = '|'|})]
                    public partial record ProductKey(string ProductId, int Version);

                    // Valid composite key
                    [CompositeKey("Order_{OrderId}|{CustomerId}", PrimaryKeySeparator = '|')]
                    public partial record OrderKey(string OrderId, string CustomerId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task NestedTypes_HandlesCorrectly()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    public partial class Container
                    {
                        [CompositeKey({|COMPOSITE0005:""|})]
                        public partial record EmptyKey(string Id);

                        [CompositeKey("Valid_{Id}")]
                        public partial record ValidKey(string Id);
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task GenericTypes_HandlesCorrectly()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("Entity_{Id}")]
                    public partial record EntityKey<T>(string Id, T Data);

                    [CompositeKey({|COMPOSITE0005:""|})]
                    public partial record EmptyGenericKey<T>(T Value);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ComplexRealWorldTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;
                    using System;

                    [CompositeKey("ORD_{Year:0000}_{Month:00}_{Day:00}#{OrderNumber:000000}_{CustomerId}", PrimaryKeySeparator = '#')]
                    public partial record OrderIdentifier(
                        int Year,
                        int Month,
                        int Day,
                        int OrderNumber,
                        Guid CustomerId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task SpecialCharactersInTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("@{Type}!{Id}~{Version}")]
                    public partial record SpecialKey(string Type, string Id, int Version);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task UnicodeCharactersInTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("用户_{UserId}_租户_{TenantId}")]
                    public partial record InternationalKey(string UserId, string TenantId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task TemplateWithComplexFormat_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;
                    using System;

                    [CompositeKey("{Date:yyyy-MM-dd_HH-mm-ss}_{Counter:D10}")]
                    public partial record TestKey(DateTime Date, int Counter);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task TemplateWithMixedCaseProperties_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("{ID}_{UserName}_{GUID}")]
                    public partial record TestKey(string ID, string UserName, string GUID);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task TemplateWithLongPropertyNames_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("{VeryLongPropertyNameThatExceedsNormalLength}_{AnotherLongPropertyName}")]
                    public partial record TestKey(
                        string VeryLongPropertyNameThatExceedsNormalLength,
                        string AnotherLongPropertyName);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Tests for repeating property position validation (COMPOSITE0011).
    /// </summary>
    public class RepeatingPropertyPositionValidation
    {
        [Fact]
        public async Task RepeatingPropertyAtEnd_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using System.Collections.Generic;
                    using CompositeKey;

                    [CompositeKey("PREFIX_{Tags...#}")]
                    public partial record TestKey
                    {
                        public List<string> Tags { get; set; } = [];
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RepeatingPropertyInCompositeKeySortKeyAtEnd_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using System.Collections.Generic;
                    using CompositeKey;

                    [CompositeKey("{UserId}|TAG_{Tags...#}", PrimaryKeySeparator = '|')]
                    public partial record TestKey
                    {
                        public string UserId { get; set; } = "";
                        public List<string> Tags { get; set; } = [];
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RepeatingPropertyNotAtEnd_ReportsError()
        {
            // Arrange
            var test = new TemplateStringAnalyzerTest
            {
                TestCode = """
                    using System.Collections.Generic;
                    using CompositeKey;

                    [CompositeKey({|COMPOSITE0011:"{Tags...#}_{Suffix}"|})]
                    public partial record TestKey
                    {
                        public List<string> Tags { get; set; } = [];
                        public string Suffix { get; set; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Tests for analyzer public API and metadata.
    /// </summary>
    public class AnalyzerApiTests
    {
        [Fact]
        public void TemplateStringAnalyzer_SupportsExpectedDiagnostics()
        {
            // Arrange
            var analyzer = new TemplateStringAnalyzer();

            // Act
            var supportedDiagnostics = analyzer.SupportedDiagnostics;

            // Assert
            supportedDiagnostics.ShouldNotBeEmpty();
            supportedDiagnostics.Length.ShouldBe(3);

            var diagnosticIds = supportedDiagnostics.Select(d => d.Id).ToList();
            diagnosticIds.ShouldContain("COMPOSITE0005"); // EmptyOrInvalidTemplateString
            diagnosticIds.ShouldContain("COMPOSITE0006"); // PrimaryKeySeparatorMissingFromTemplateString
            diagnosticIds.ShouldContain("COMPOSITE0011"); // RepeatingPropertyMustBeLastPart
        }

        [Fact]
        public void TemplateStringAnalyzer_DiagnosticsHaveCorrectSeverity()
        {
            // Arrange
            var analyzer = new TemplateStringAnalyzer();

            // Act
            var supportedDiagnostics = analyzer.SupportedDiagnostics;

            // Assert
            foreach (var diagnostic in supportedDiagnostics)
            {
                diagnostic.DefaultSeverity.ShouldBe(DiagnosticSeverity.Error);
                diagnostic.IsEnabledByDefault.ShouldBeTrue();
            }
        }

        [Fact]
        public void TemplateStringAnalyzer_DiagnosticsHaveCorrectCategory()
        {
            // Arrange
            var analyzer = new TemplateStringAnalyzer();

            // Act
            var supportedDiagnostics = analyzer.SupportedDiagnostics;

            // Assert
            foreach (var diagnostic in supportedDiagnostics)
            {
                diagnostic.Category.ShouldBe("CompositeKey.SourceGeneration");
            }
        }
    }

    /// <summary>
    /// Test helper class for TemplateStringAnalyzer tests.
    /// </summary>
    private class TemplateStringAnalyzerTest : CompositeKeyAnalyzerTestBase<TemplateStringAnalyzer>;
}
