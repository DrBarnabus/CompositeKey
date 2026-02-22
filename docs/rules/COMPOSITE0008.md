# COMPOSITE0008: Invalid or unsupported format specifier

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0008 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

A property in the template uses a format specifier that is invalid or unsupported for its type. The source generator validates format specifiers at compile time to ensure the generated code will work correctly.

## How to fix

Use a format specifier that is valid for the property's type:

| Type | Supported formats |
|------|-------------------|
| `Guid` | `D`, `N`, `B`, `P`, `X` |
| `Enum` | `G` |
| `string` | *(none — format specifiers are not supported)* |
| Numeric types | Standard .NET numeric format strings (e.g. `0.00`, `F2`) |

```csharp
// Before (error — 'Z' is not a valid Guid format)
[CompositeKey("{Id:Z}")]
public sealed partial record MyKey(Guid Id);

// After (fixed)
[CompositeKey("{Id:N}")]
public sealed partial record MyKey(Guid Id);
```
