# COMPOSITE0002: Unsupported type

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0002 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

The `[CompositeKey]` attribute was applied to a type that is not a record. Only record types are currently supported.

## How to fix

Change the type to a record:

```csharp
// Before (error)
[CompositeKey("{Id}#{Name}")]
public sealed partial class MyKey { ... }

// After (fixed)
[CompositeKey("{Id}#{Name}")]
public sealed partial record MyKey(Guid Id, string Name);
```
