# COMPOSITE0009: Repeating syntax used on non-collection property

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0009 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

The template uses repeating syntax (`{Property...separator}`) for a property that is not a supported collection type.

Supported collection types are:
- `IReadOnlyList<T>`
- `List<T>`
- `ImmutableArray<T>`

## How to fix

Either change the property to a supported collection type or remove the repeating syntax:

```csharp
// Before (error — Guid is not a collection)
[CompositeKey("{Id...#}")]
public sealed partial record MyKey(Guid Id);

// After (fixed — use a collection type)
[CompositeKey("{Ids...#}")]
public sealed partial record MyKey(IReadOnlyList<Guid> Ids);
```
