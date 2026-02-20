# COMPOSITE0005: Empty or invalid template string

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0005 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

The template string provided to the `[CompositeKey]` attribute is either empty or could not be parsed. Template strings must contain at least one dynamic property reference and follow the correct syntax.

## How to fix

Provide a valid template string using `{PropertyName}` for dynamic values and literal characters for delimiters/constants:

```csharp
// Before (error — empty template)
[CompositeKey("")]
public sealed partial record MyKey(Guid Id);

// After (fixed)
[CompositeKey("{Id}")]
public sealed partial record MyKey(Guid Id);
```

Template syntax reference:

| Syntax | Description | Example |
|--------|-------------|---------|
| `{PropertyName}` | Dynamic property value | `{Id}` |
| `{PropertyName:format}` | Dynamic value with format specifier | `{Id:N}` |
| `{PropertyName...separator}` | Repeating collection with separator | `{Tags...,}` |
| Literal characters | Constants or delimiters outside braces | `PREFIX#`, `~`, `@` |
