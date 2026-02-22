# COMPOSITE0007: Property missing accessible getter or setter

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0007 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

A property referenced in the template string does not have both an accessible getter and setter. The source generator needs both to format (get) and parse (set) the value.

## How to fix

Ensure the property has both accessible `get` and `set` (or `init`) methods:

```csharp
// Before (error — no setter)
[CompositeKey("{Id}#{Name}")]
public sealed partial record MyKey(Guid Id)
{
    public string Name { get; }
}

// After (fixed)
[CompositeKey("{Id}#{Name}")]
public sealed partial record MyKey(Guid Id)
{
    public required string Name { get; init; }
}
```

Record primary constructor parameters automatically have accessible getters and init setters, so they work by default.
