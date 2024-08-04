using CompositeId.SourceGeneration.Core;

namespace CompositeId.SourceGeneration.Model;

public sealed record TargetTypeSpec(
    TypeRef Type,
    string? Namespace,
    ImmutableEquatableArray<string> TypeDeclarations,
    ImmutableEquatableArray<PropertySpec> Properties,
    ImmutableEquatableArray<ConstructorParameterSpec> ConstructorParameters,
    ImmutableEquatableArray<PropertyInitializerSpec> PropertyInitializers,
    ConstructionStrategy ConstructionStrategy);
