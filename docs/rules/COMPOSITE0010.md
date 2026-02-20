# COMPOSITE0010: Collection property without repeating syntax

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0010 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

A property in the template is a collection type (`List<T>`, `IReadOnlyList<T>`, or `ImmutableArray<T>`) but the template does not use repeating syntax for it. Collection properties must use the `{Property...separator}` syntax so the generator knows how to format and parse multiple values.

## How to fix

Add repeating syntax with a separator character:

```csharp
// Before (error — collection without repeating syntax)
[CompositeKey("{Ids}")]
public sealed partial record MyKey(IReadOnlyList<Guid> Ids);

// After (fixed — repeating syntax with '#' separator)
[CompositeKey("{Ids...#}")]
public sealed partial record MyKey(IReadOnlyList<Guid> Ids);
```
