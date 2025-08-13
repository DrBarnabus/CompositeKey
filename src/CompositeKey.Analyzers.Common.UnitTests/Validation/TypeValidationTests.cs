using CompositeKey.Analyzers.Common.Diagnostics;
using CompositeKey.Analyzers.Common.UnitTests.TestHelpers;
using CompositeKey.Analyzers.Common.Validation;

namespace CompositeKey.Analyzers.Common.UnitTests.Validation;

public static class TypeValidationTests
{
    public class ValidateTypeForCompositeKeyTests
    {
        [Fact]
        public void ValidRecord_ShouldReturnSuccess()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                [CompositeKey("{Id}")]
                public partial record TestKey(Guid Id);
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (semanticModel, typeDeclaration, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            var result = TypeValidation.ValidateTypeForCompositeKey(
                typeSymbol,
                typeDeclaration,
                semanticModel,
                attributeSymbol,
                CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.Constructor.ShouldNotBeNull();
            result.TargetTypeDeclarations.ShouldNotBeNull();
            result.TargetTypeDeclarations.ShouldContain("public partial record TestKey");
        }

        [Theory]
        [InlineData("class", "TestClass")]
        [InlineData("struct", "TestStruct")]
        [InlineData("interface", "TestInterface")]
        public void NonRecordType_ShouldReturnFailureWithUnsupportedCompositeType(string typeKind, string typeName)
        {
            // Arrange
            string source = typeKind switch
            {
                "interface" => $$"""
                    using System;
                    using CompositeKey;

                    [CompositeKey("{Id}")]
                    public partial {{typeKind}} {{typeName}}
                    {
                        Guid Id { get; set; }
                    }
                    """,
                _ => $$"""
                    using System;
                    using CompositeKey;

                    [CompositeKey("{Id}")]
                    public partial {{typeKind}} {{typeName}}
                    {
                        public Guid Id { get; set; }
                    }
                    """
            };

            var compilation = CompilationTestHelper.CreateCompilation(source);
            var (semanticModel, typeDeclaration, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, typeName);
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            var result = TypeValidation.ValidateTypeForCompositeKey(
                typeSymbol,
                typeDeclaration,
                semanticModel,
                attributeSymbol,
                CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.UnsupportedCompositeType);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs![0].ShouldBe(typeName);
        }

        [Fact]
        public void NonPartialRecord_ShouldReturnFailureWithCompositeTypeMustBePartial()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                [CompositeKey("{Id}")]
                public record TestKey(Guid Id);
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (semanticModel, typeDeclaration, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            var result = TypeValidation.ValidateTypeForCompositeKey(
                typeSymbol,
                typeDeclaration,
                semanticModel,
                attributeSymbol,
                CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.CompositeTypeMustBePartial);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs![0].ShouldBe("TestKey");
        }

        [Fact]
        public void RecordWithMultiplePublicConstructorsAndNoParameterless_ShouldReturnFailureWithNoObviousDefaultConstructor()
        {
            // Arrange - Multiple constructors but none parameterless and none attributed
            const string Source = """
                using System;
                using CompositeKey;

                [CompositeKey("{Id}")]
                public partial record TestKey
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; }

                    public TestKey(Guid id) { Id = id; Name = ""; }
                    public TestKey(Guid id, string name) { Id = id; Name = name; }
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (semanticModel, typeDeclaration, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            var result = TypeValidation.ValidateTypeForCompositeKey(
                typeSymbol,
                typeDeclaration,
                semanticModel,
                attributeSymbol,
                CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.NoObviousDefaultConstructor);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs![0].ShouldBe("TestKey");
        }

        [Fact]
        public void NestedPartialRecord_ShouldReturnSuccessWithAllDeclarations()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial class Outer
                {
                    [CompositeKey("{Id}")]
                    public partial record InnerKey(Guid Id);
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (semanticModel, typeDeclaration, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "InnerKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            var result = TypeValidation.ValidateTypeForCompositeKey(
                typeSymbol,
                typeDeclaration,
                semanticModel,
                attributeSymbol,
                CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeTrue();
            result.TargetTypeDeclarations.ShouldNotBeNull();
            result.TargetTypeDeclarations.Count.ShouldBe(2);
            result.TargetTypeDeclarations.ShouldContain("public partial record InnerKey");
            result.TargetTypeDeclarations.ShouldContain("public partial class Outer");
        }

        [Fact]
        public void RecordStruct_ShouldReturnSuccessIfValid()
        {
            // Arrange - Record structs are supported
            const string Source = """
                using System;
                using CompositeKey;

                [CompositeKey("{Id}")]
                public partial record struct TestRecordStruct(Guid Id);
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (semanticModel, typeDeclaration, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestRecordStruct");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            var result = TypeValidation.ValidateTypeForCompositeKey(
                typeSymbol,
                typeDeclaration,
                semanticModel,
                attributeSymbol,
                CancellationToken.None);

            // Assert
            // Record structs are valid composite key types
            result.IsSuccess.ShouldBeTrue();
            result.Constructor.ShouldNotBeNull();
            result.TargetTypeDeclarations.ShouldNotBeNull();
        }

        [Fact]
        public void NestedTypeWithNonPartialParent_ShouldReturnFailureWithCompositeTypeMustBePartial()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public class NonPartialOuter
                {
                    [CompositeKey("{Id}")]
                    public partial record InnerKey(Guid Id);
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (semanticModel, typeDeclaration, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "InnerKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            var result = TypeValidation.ValidateTypeForCompositeKey(
                typeSymbol,
                typeDeclaration,
                semanticModel,
                attributeSymbol,
                CancellationToken.None);

            // Assert
            result.IsSuccess.ShouldBeFalse();
            result.Descriptor.ShouldBe(DiagnosticDescriptors.CompositeTypeMustBePartial);
            result.MessageArgs.ShouldNotBeNull();
            result.MessageArgs![0].ShouldBe("InnerKey");
        }
    }

    public class TryGetObviousOrExplicitlyMarkedConstructorTests
    {
        [Fact]
        public void RecordWithPrimaryConstructor_ShouldReturnPrimaryConstructor()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey(Guid Id, string Name);
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                attributeSymbol,
                out var constructor);

            // Assert
            result.ShouldBeTrue();
            constructor.ShouldNotBeNull();
            constructor.Parameters.Length.ShouldBe(2);
        }

        [Fact]
        public void RecordWithParameterlessConstructor_ShouldReturnParameterlessConstructor()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey
                {
                    public Guid Id { get; set; }
                    public TestKey() { }
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                attributeSymbol,
                out var constructor);

            // Assert
            result.ShouldBeTrue();
            constructor.ShouldNotBeNull();
            constructor.Parameters.Length.ShouldBe(0);
        }

        [Fact]
        public void RecordWithMultipleParameterizedConstructors_ShouldReturnFalse()
        {
            // Arrange - Multiple constructors, no parameterless, no attributed
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; }

                    public TestKey(Guid id) { Id = id; Name = ""; }
                    public TestKey(Guid id, string name) { Id = id; Name = name; }
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                attributeSymbol,
                out var constructor);

            // Assert
            result.ShouldBeFalse();
            constructor.ShouldBeNull();
        }

        [Fact]
        public void RecordWithSinglePublicConstructor_ShouldReturnThatConstructor()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; }

                    public TestKey(Guid id, string name)
                    {
                        Id = id;
                        Name = name;
                    }
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                attributeSymbol,
                out var constructor);

            // Assert
            result.ShouldBeTrue();
            constructor.ShouldNotBeNull();
            constructor.Parameters.Length.ShouldBe(2);
        }

        [Fact]
        public void RecordWithExplicitAttributeMarkedConstructor_ShouldReturnAttributedConstructor()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; }

                    public TestKey() { }

                    [CompositeKeyConstructor]
                    public TestKey(Guid id, string name)
                    {
                        Id = id;
                        Name = name;
                    }
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                attributeSymbol,
                out var constructor);

            // Assert
            result.ShouldBeTrue();
            constructor.ShouldNotBeNull();
            constructor.Parameters.Length.ShouldBe(2);
            constructor.GetAttributes().Any(a => a.AttributeClass?.Name == "CompositeKeyConstructorAttribute").ShouldBeTrue();
        }

        [Fact]
        public void RecordWithMultipleAttributedConstructors_ShouldReturnFalse()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; }

                    [CompositeKeyConstructor]
                    public TestKey(Guid id)
                    {
                        Id = id;
                        Name = "";
                    }

                    [CompositeKeyConstructor]
                    public TestKey(Guid id, string name)
                    {
                        Id = id;
                        Name = name;
                    }
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                attributeSymbol,
                out var constructor);

            // Assert
            result.ShouldBeFalse();
            constructor.ShouldBeNull();
        }

        [Fact]
        public void RecordWithCopyConstructor_ShouldIgnoreCopyConstructor()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey(Guid Id)
                {
                    // Copy constructor should be ignored
                    public TestKey(TestKey original) : this(original.Id) { }
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                attributeSymbol,
                out var constructor);

            // Assert
            result.ShouldBeTrue();
            constructor.ShouldNotBeNull();
            // Should return primary constructor, not copy constructor
            constructor.Parameters.Length.ShouldBe(1);
            constructor.Parameters[0].Type.Name.ShouldBe("Guid");
        }

        [Fact]
        public void RecordWithNullAttributeSymbol_ShouldStillWork()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey(Guid Id);
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");

            // Act - Pass null for attribute symbol
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                null,
                out var constructor);

            // Assert
            result.ShouldBeTrue();
            constructor.ShouldNotBeNull();
            constructor.Parameters.Length.ShouldBe(1);
        }

        [Fact]
        public void RecordWithMultipleConstructorsIncludingParameterless_ShouldPreferParameterless()
        {
            // Arrange
            const string Source = """
                using System;
                using CompositeKey;

                public partial record TestKey
                {
                    public Guid Id { get; set; }
                    public string Name { get; set; }

                    public TestKey() { }
                    public TestKey(Guid id) { Id = id; Name = ""; }
                    public TestKey(Guid id, string name) { Id = id; Name = name; }
                }
                """;

            var compilation = CompilationTestHelper.CreateCompilation(Source);
            var (_, _, typeSymbol) = CompilationTestHelper.GetFirstTypeInfo(compilation, "TestKey");
            var attributeSymbol = CompilationTestHelper.GetCompositeKeyConstructorAttributeSymbol(compilation);

            // Act
            bool result = TypeValidation.TryGetObviousOrExplicitlyMarkedConstructor(
                typeSymbol,
                attributeSymbol,
                out var constructor);

            // Assert
            result.ShouldBeTrue();
            constructor.ShouldNotBeNull();
            // Should prefer the parameterless constructor
            constructor.Parameters.Length.ShouldBe(0);
        }
    }
}
