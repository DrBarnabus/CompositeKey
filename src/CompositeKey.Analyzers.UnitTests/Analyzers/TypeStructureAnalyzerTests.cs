using CompositeKey.Analyzers.Analyzers;
using CompositeKey.Analyzers.UnitTests.Infrastructure;

namespace CompositeKey.Analyzers.UnitTests.Analyzers;

/// <summary>
/// Comprehensive tests for TypeStructureAnalyzer validating type structure requirements
/// for CompositeKey-annotated types including location precision verification.
/// </summary>
public class TypeStructureAnalyzerTests
{
    /// <summary>
    /// Tests for supported type validation (COMPOSITE0002).
    /// </summary>
    public class SupportedTypeValidation
    {
        [Fact]
        public async Task Record_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = TestCodeTemplates.MinimalValidRecord
            };

            // Act & Assert
            await test.RunAsync();
        }


        [Fact]
        public async Task Class_ReportsUnsupportedType()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public class {|COMPOSITE0002:UserKey|}
                    {
                        public string UserId { get; set; }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task Interface_ReportsCompilerError()
        {
            // Arrange - CompositeKey attribute is not valid on interfaces
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [{|CS0592:CompositeKey|}("User_{UserId}")]
                    public interface IUserKey
                    {
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task Struct_ReportsCompilerError()
        {
            // Arrange - CompositeKey attribute is not valid on structs
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [{|CS0592:CompositeKey|}("User_{UserId}")]
                    public struct UserKey
                    {
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task Enum_ReportsCompilerError()
        {
            // Arrange - CompositeKey attribute is not valid on enums
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [{|CS0592:CompositeKey|}("Type_{Type}")]
                    public enum UserType
                    {
                        Admin,
                        User
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task GenericRecord_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey<T>(string UserId, T Data);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

    }

    /// <summary>
    /// Tests for partial type requirements (COMPOSITE0003).
    /// </summary>
    public class PartialRequirements
    {

        [Fact]
        public async Task NonPartialRecord_ReportsPartialRequired()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = string.Format(TestCodeTemplates.NonPartialRecord,
                    "{|COMPOSITE0003:UserKey|}", "User_{UserId}", "string", "UserId")
            };

            // Act & Assert
            await test.RunAsync();
        }


        [Fact]
        public async Task NestedPartialRecord_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    public partial class Container
                    {
                        [CompositeKey("User_{UserId}")]
                        public partial record UserKey(string UserId);
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task NestedNonPartialRecord_ReportsPartialRequired()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    public partial class Container
                    {
                        [CompositeKey("User_{UserId}")]
                        public record {|COMPOSITE0003:UserKey|}(string UserId);
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MultiplePartialDeclarations_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey(string UserId);

                    public partial record UserKey
                    {
                        public string GetDisplayName() => $"User: {UserId}";
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }
    }

    /// <summary>
    /// Tests for constructor accessibility (COMPOSITE0004).
    /// </summary>
    public class ConstructorAccessibility
    {

        [Fact]
        public async Task RecordWithImplicitConstructor_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
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
        public async Task RecordWithExplicitPublicConstructor_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey
                    {
                        public string UserId { get; set; }
                        
                        public UserKey(string userId)
                        {
                            UserId = userId;
                        }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithPrivateConstructors_ProducesNoDiagnostics()
        {
            // Arrange - Private constructors are allowed since generated code is in the same class
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey
                    {
                        public string UserId { get; set; }
                        
                        private UserKey()
                        {
                        }
                        
                        private UserKey(string userId)
                        {
                            UserId = userId;
                        }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithParameterlessConstructor_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey
                    {
                        public string UserId { get; set; }
                        
                        public UserKey()
                        {
                        }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithMixedConstructors_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey
                    {
                        public string UserId { get; set; }
                        
                        public UserKey(string userId)
                        {
                            UserId = userId;
                        }
                        
                        private UserKey()
                        {
                        }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithMultiplePublicConstructorsAndNoParameterless_ProducesNoObviousDefaultConstructorDiagnostic()
        {
            // Arrange - This should trigger COMPOSITE0004: multiple public constructors with no parameterless constructor
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [{|COMPOSITE0004:CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")|}]
                    public partial record BasicPrimaryKey(Guid FirstPart, Guid SecondPart, Guid ThirdPart)
                    {
                        // Type has two constructors, none of which are a default constructor
                        public BasicPrimaryKey(Guid FirstPart) : this(FirstPart, Guid.NewGuid(), Guid.NewGuid())
                        {
                        }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithMultipleConstructorsButOneMarkedWithAttribute_ProducesNoDiagnostics()
        {
            // Arrange - Multiple constructors but one is explicitly marked with [CompositeKeyConstructor] - should NOT emit COMPOSITE0004
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                    public partial record BasicPrimaryKey
                    {
                        [CompositeKeyConstructor]
                        public BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
                        {
                            FirstPart = firstPart;
                            SecondPart = secondPart;
                            ThirdPart = thirdPart;
                        }

                        public BasicPrimaryKey(Guid firstPart, string secondPart)
                            : this(firstPart, secondPart, default(int))
                        {
                        }

                        public Guid FirstPart { get; init; }
                        public string SecondPart { get; init; }
                        public int ThirdPart { get; set; }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithMultipleConstructorsMarkedWithAttribute_ProducesNoObviousDefaultConstructorDiagnostic()
        {
            // Arrange - Multiple constructors marked with [CompositeKeyConstructor] - should emit COMPOSITE0004
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [{|COMPOSITE0004:CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")|}]
                    public partial record BasicPrimaryKey
                    {
                        [CompositeKeyConstructor]
                        public BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
                        {
                            FirstPart = firstPart;
                            SecondPart = secondPart;
                            ThirdPart = thirdPart;
                        }

                        [CompositeKeyConstructor]
                        public BasicPrimaryKey(Guid firstPart, string secondPart)
                            : this(firstPart, secondPart, default(int))
                        {
                        }

                        public Guid FirstPart { get; init; }
                        public string SecondPart { get; init; }
                        public int ThirdPart { get; set; }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RegularRecordWithMultipleConstructorsAndNoParameterless_ProducesNoObviousDefaultConstructorDiagnostic()
        {
            // Arrange - Regular record (no primary constructor) with multiple constructors, no parameterless - should emit COMPOSITE0004
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [{|COMPOSITE0004:CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")|}]
                    public partial record BasicPrimaryKey
                    {
                        public BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
                        {
                            FirstPart = firstPart;
                            SecondPart = secondPart;
                            ThirdPart = thirdPart;
                        }

                        public BasicPrimaryKey(Guid firstPart, string secondPart)
                            : this(firstPart, secondPart, default(int))
                        {
                        }

                        public Guid FirstPart { get; init; }
                        public string SecondPart { get; init; }
                        public int ThirdPart { get; set; }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithMultiplePrivateConstructorsAndNoParameterless_ProducesNoObviousDefaultConstructorDiagnostic()
        {
            // Arrange - Multiple private constructors with no parameterless - should emit COMPOSITE0004 (accessibility doesn't matter)
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [{|COMPOSITE0004:CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")|}]
                    public partial record BasicPrimaryKey
                    {
                        private BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
                        {
                            FirstPart = firstPart;
                            SecondPart = secondPart;
                            ThirdPart = thirdPart;
                        }

                        private BasicPrimaryKey(Guid firstPart, string secondPart)
                            : this(firstPart, secondPart, default(int))
                        {
                        }

                        public Guid FirstPart { get; init; }
                        public string SecondPart { get; init; }
                        public int ThirdPart { get; set; }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithInternalConstructorMarkedWithAttribute_ProducesNoDiagnostics()
        {
            // Arrange - Single internal constructor with [CompositeKeyConstructor] - should NOT emit COMPOSITE0004
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using System;
                    using CompositeKey;

                    [CompositeKey("{FirstPart}#{SecondPart}#{ThirdPart}")]
                    public partial record BasicPrimaryKey
                    {
                        [CompositeKeyConstructor]
                        internal BasicPrimaryKey(Guid firstPart, string secondPart, int thirdPart)
                        {
                            FirstPart = firstPart;
                            SecondPart = secondPart;
                            ThirdPart = thirdPart;
                        }

                        public Guid FirstPart { get; init; }
                        public string SecondPart { get; init; }
                        public int ThirdPart { get; set; }
                    }
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
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = TestCodeTemplates.RecordWithoutAttribute
            };

            // Act & Assert
            await test.RunAsync();
        }


        [Fact]
        public async Task ComplexValidRecord_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    namespace MyApp.Keys
                    {
                        [CompositeKey("User_{UserId}")]
                        public partial record UserKey(string UserId)
                        {
                            public string GetDisplayName() => $"User: {UserId}";
                        }
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task RecordWithPrimaryKeySeparator_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
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
        public async Task PerformanceTest_HandlesLargeRecord()
        {
            // Arrange - record with many properties to test performance
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("User_{UserId}")]
                    public partial record UserKey(
                        string UserId,
                        string FirstName,
                        string LastName,
                        string Email,
                        string PhoneNumber,
                        string Address,
                        string City,
                        string State,
                        string ZipCode,
                        string Country);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task MultipleTypesInSameFile_AnalyzesOnlyAttributedTypes()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    // This should not be analyzed
                    public partial record RegularRecord(string Name);

                    // This should be analyzed and produce a diagnostic
                    [CompositeKey("User_{UserId}")]
                    public class {|COMPOSITE0002:UserKey|}
                    {
                        public string UserId { get; set; }
                    }

                    // This should be analyzed and pass
                    [CompositeKey("Product_{ProductId}")]
                    public partial record ProductKey(string ProductId);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task NestedTypes_HandlesCorrectly()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    public partial class Container
                    {
                        [CompositeKey("User_{UserId}")]
                        public partial record UserKey(string UserId);

                        public class RegularNestedClass
                        {
                            public string Name { get; set; }
                        }
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
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;

                    [CompositeKey("Entity_{Id}")]
                    public partial record EntityKey<T>(string Id, T Data);
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task ComplexRecordWithAllFeatures_ProducesNoDiagnostics()
        {
            // Arrange
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;
                    using System;
                    using System.Collections.Generic;

                    [CompositeKey("Complex_{Id}_{Type}_{Version:000}", PrimaryKeySeparator = '_')]
                    public partial record ComplexKey(
                        string Id,
                        string Type,
                        int Version)
                    {
                        public string DisplayName { get; init; } = "";
                        public DateTime CreatedAt { get; init; } = DateTime.Now;
                        public List<string> Tags { get; init; } = new();
                        
                        // Methods
                        public string GetFullKey() => $"{Id}_{Type}_{Version:000}";
                        public override string ToString() => GetFullKey();
                        
                        // Static members
                        public static ComplexKey CreateDefault(string id) => new(id, "default", 1);
                    }
                    """
            };

            // Act & Assert
            await test.RunAsync();
        }

        [Fact]
        public async Task LargeCodeBase_PerformsEfficiently()
        {
            // Arrange - Create a larger codebase to test performance
            var test = new TypeStructureAnalyzerTest
            {
                TestCode = """
                    using CompositeKey;
                    using System;

                    namespace MyApp.Keys
                    {
                        // Multiple valid records
                        [CompositeKey("User_{UserId}")]
                        public partial record UserKey(string UserId);

                        [CompositeKey("Product_{ProductId}")]
                        public partial record ProductKey(string ProductId);

                        [CompositeKey("Order_{OrderId}")]
                        public partial record OrderKey(string OrderId);

                        [CompositeKey("Customer_{CustomerId}")]
                        public partial record CustomerKey(string CustomerId);

                        [CompositeKey("Invoice_{InvoiceId}")]
                        public partial record InvoiceKey(string InvoiceId);

                        // Non-attributed types (should be ignored)
                        public record RegularRecord1(string Name);
                        public record RegularRecord2(string Name);
                        public record RegularRecord3(string Name);
                        public class RegularClass1 { }
                        public class RegularClass2 { }
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
        public void TypeStructureAnalyzer_SupportsAllExpectedDiagnostics()
        {
            // Arrange
            var analyzer = new TypeStructureAnalyzer();

            // Act
            var supportedDiagnostics = analyzer.SupportedDiagnostics;

            // Assert
            supportedDiagnostics.ShouldNotBeEmpty();
            supportedDiagnostics.Length.ShouldBe(3);
            
            var diagnosticIds = supportedDiagnostics.Select(d => d.Id).ToList();
            diagnosticIds.ShouldContain("COMPOSITE0002"); // UnsupportedCompositeType
            diagnosticIds.ShouldContain("COMPOSITE0003"); // CompositeTypeMustBePartial
            diagnosticIds.ShouldContain("COMPOSITE0004"); // NoObviousDefaultConstructor
        }
    }

    /// <summary>
    /// Test helper class for TypeStructureAnalyzer tests.
    /// </summary>
    private class TypeStructureAnalyzerTest : CompositeKeyAnalyzerTestBase<TypeStructureAnalyzer>
    {
    }
}