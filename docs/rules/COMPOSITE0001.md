# COMPOSITE0001: C# language version not supported

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0001 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

The project is using a C# language version that is not supported by the CompositeKey source generator. The source generator requires C# 11 or later.

## How to fix

Update the `LangVersion` in your project file to 11 or later:

```xml
<PropertyGroup>
    <LangVersion>11</LangVersion>
</PropertyGroup>
```

Or target a framework that defaults to C# 11+ (e.g. net7.0 or later).
