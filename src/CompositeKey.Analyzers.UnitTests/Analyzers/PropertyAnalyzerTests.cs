using CompositeKey.Analyzers.Analyzers;
using CompositeKey.Analyzers.UnitTests.Infrastructure;

namespace CompositeKey.Analyzers.UnitTests.Analyzers;

/// <summary>
/// Comprehensive tests for PropertyAnalyzer validating property accessibility and format specifiers
/// for CompositeKey-annotated types including location precision verification.
/// </summary>
public static class PropertyAnalyzerTests
{
    /// <summary>
    /// Tests for property accessibility validation (COMPOSITE0007).
    /// </summary>
    public class PropertyAccessibilityValidation
    {
        [Fact]
        public async Task PublicGetterAndSetter_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey(string UserId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task PropertyWithPrivateGetter_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey
                    {
                        public string UserId { private get; set; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task PropertyWithPrivateSetter_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("Item_{ItemId}")]
                    public partial record ItemKey
                    {
                        public string ItemId { get; private set; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task PropertyWithProtectedSetter_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("Order_{OrderId}")]
                    public partial record OrderKey
                    {
                        public string OrderId { get; protected set; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task PropertyWithInitOnlySetter_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("Product_{ProductId}")]
                    public partial record ProductKey
                    {
                        public string ProductId { get; init; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ReadOnlyProperty_ReportsAccessibilityError()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("Entity_{EntityId}")]
                    public partial record EntityKey
                    {
                        private readonly string _entityId = "";
                        public string {|COMPOSITE0007:EntityId|} { get { return _entityId; } }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task PropertyWithNoGetter_ReportsAccessibilityError()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("Composite_{WriteOnly}")]
                    public partial record CompositeKeyRecord
                    {
                        private string _writeOnly = "";
                        public string {|COMPOSITE0007:WriteOnly|} { set { _writeOnly = value; } }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task PropertyNotReferencedInTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey
                    {
                        public string UserId { get; set; } = "";
                        public string UnusedProp { get; private set; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task InheritedProperties_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    public record BaseRecord
                    {
                        public string BaseId { get; private set; } = "";
                    }

                    [CompositeKey("Derived_{DerivedId}")]
                    public partial record DerivedKey : BaseRecord
                    {
                        public string DerivedId { get; set; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task EmptyTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("")]
                    public partial record EmptyKey
                    {
                        public string Id { private get; set; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Tests for property format validation (COMPOSITE0008).
    /// </summary>
    public class PropertyFormatValidation
    {
        [Fact]
        public async Task GuidWithValidFormat_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("Entity_{Id:D}")]
                    public partial record EntityKey(Guid Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Theory]
        [InlineData("d")]
        [InlineData("n")]
        [InlineData("b")]
        [InlineData("p")]
        [InlineData("x")]
        public async Task GuidWithAllValidFormats_ProducesNoDiagnostics(string format)
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = $$"""
                    using System;
                    using CompositeKey;

                    [CompositeKey("Entity_{Id:{{format}}}")]
                    public partial record EntityKey(Guid Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task GuidWithCaseInsensitiveFormat_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("Entity_{Id:D}")]
                    public partial record EntityKey(Guid Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task GuidWithInvalidFormat_ReportsFormatError()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("Entity_{Id:invalid}")]
                    public partial record EntityKey(Guid {|COMPOSITE0008:Id|});
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task StringWithFormat_ReportsFormatError()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{Name:upper}")]
                    public partial record UserKey(string {|COMPOSITE0008:Name|});
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task StringWithoutFormat_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{Name}")]
                    public partial record UserKey(string Name);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task EnumWithValidFormat_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    public enum Status { Active, Inactive }

                    [CompositeKey("Entity_{CurrentStatus:g}")]
                    public partial record EntityKey(Status CurrentStatus);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task EnumWithInvalidFormat_ReportsFormatError()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    public enum Status { Active, Inactive }

                    [CompositeKey("Entity_{CurrentStatus:d}")]
                    public partial record EntityKey(Status {|COMPOSITE0008:CurrentStatus|});
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task DateTimeWithFormat_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("Log_{Timestamp:yyyy-MM-dd}")]
                    public partial record LogKey(DateTime Timestamp);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MultiplePropertiesWithFormatIssues_ReportsMultipleDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("Mixed_{Id:invalid}_{Name:upper}")]
                    public partial record MixedKey(Guid {|COMPOSITE0008:Id|}, string {|COMPOSITE0008:Name|});
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ComplexPropertyTypes_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("Complex_{Id:d}_{Timestamp:yyyy-MM-dd}_{Status:g}")]
                    public partial record ComplexKey(Guid Id, DateTime Timestamp, DayOfWeek Status);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ConstantsInTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("CONST_{Id}")]
                    public partial record EntityKey(Guid Id);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Tests for composite primary key scenarios with property validation.
    /// </summary>
    public class CompositePrimaryKeyScenarios
    {
        [Fact]
        public async Task CompositePrimaryKey_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("PK_{PartitionKey}#SK_{SortKey}", PrimaryKeySeparator = '#')]
                    public partial record CompositeRecord(string PartitionKey, Guid SortKey);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task CompositePrimaryKey_WithPrivateAccessors_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("PK_{PartitionKey}#SK_{SortKey}", PrimaryKeySeparator = '#')]
                    public partial record CompositeRecord
                    {
                        public string PartitionKey { private get; set; } = "";
                        public Guid SortKey { get; private set; }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task CompositePrimaryKey_ReportsFormatIssues()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("PK_{PartitionKey:format}#SK_{SortKey:invalid}", PrimaryKeySeparator = '#')]
                    public partial record CompositeRecord(string {|COMPOSITE0008:PartitionKey|}, Guid {|COMPOSITE0008:SortKey|});
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task PropertyNotInTemplate_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("Entity_{NonExistent}")]
                    public partial record EntityKey
                    {
                        public Guid Id { get; set; }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Tests for repeating property collection type validation (COMPOSITE0009/COMPOSITE0010).
    /// </summary>
    public class RepeatingPropertyValidation
    {
        [Fact]
        public async Task RepeatingPropertyWithListType_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System.Collections.Generic;
                    using CompositeKey;

                    [CompositeKey("PREFIX_{Tags...#}")]
                    public partial record TaggedKey
                    {
                        public List<string> Tags { get; set; } = [];
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RepeatingPropertyWithIReadOnlyListType_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System.Collections.Generic;
                    using CompositeKey;

                    [CompositeKey("PREFIX_{Tags...#}")]
                    public partial record TaggedKey
                    {
                        public IReadOnlyList<string> Tags { get; set; } = [];
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RepeatingPropertyWithNonCollectionType_ReportsError()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("PREFIX_{Name...#}")]
                    public partial record InvalidKey
                    {
                        public string {|COMPOSITE0009:Name|} { get; set; } = "";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RepeatingPropertyWithImmutableArrayType_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System;
                    using System.Collections.Immutable;
                    using CompositeKey;

                    [CompositeKey("PREFIX_{Ids:D...#}")]
                    public partial record TestKey
                    {
                        public ImmutableArray<Guid> Ids { get; set; }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RepeatingTypeWithoutRepeatingSyntax_ReportsError()
        {
            // Arrange
            var test = new PropertyAnalyzerTest
            {
                TestCode = """
                    using System.Collections.Generic;
                    using CompositeKey;

                    [CompositeKey("PREFIX_{Tags}")]
                    public partial record InvalidKey
                    {
                        public List<string> {|COMPOSITE0010:{|COMPOSITE0008:Tags|}|} { get; set; } = [];
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Test fixture for PropertyAnalyzer using the analyzer test infrastructure.
    /// </summary>
    private sealed class PropertyAnalyzerTest : CompositeKeyAnalyzerTestBase<PropertyAnalyzer>;
}
