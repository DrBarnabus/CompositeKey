# COMPOSITE0003: Type must be partial

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0003 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

The type annotated with `[CompositeKey]` (and/or its containing types) is not declared as `partial`. The source generator needs the `partial` modifier to emit additional members into the type.

## How to fix

Add the `partial` modifier to the type and all containing types:

```csharp
// Before (error)
[CompositeKey("{Id}#{Name}")]
public sealed record MyKey(Guid Id, string Name);

// After (fixed)
[CompositeKey("{Id}#{Name}")]
public sealed partial record MyKey(Guid Id, string Name);
```

If the type is nested, the containing type must also be partial:

```csharp
public partial class Outer
{
    [CompositeKey("{Id}")]
    public sealed partial record MyKey(Guid Id);
}
```
