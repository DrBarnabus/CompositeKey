# COMPOSITE0004: No obvious constructor

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0004 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

The type annotated with `[CompositeKey]` has multiple constructors and the source generator cannot determine which one to use. The generator needs a single obvious constructor to know how to instantiate the type during parsing.

## How to fix

Either ensure the type has only one constructor, or mark the intended constructor with `[CompositeKeyConstructor]`:

```csharp
// Before (error — two constructors, generator can't choose)
[CompositeKey("{GuidValue}#{IntValue}~{StringValue}#{EnumValue}")]
public sealed partial record MixedKey(Guid GuidValue, int IntValue, string StringValue, MyEnum EnumValue)
{
    public MixedKey(Guid guidValue) : this(guidValue, default, string.Empty, default) { }
}

// After (fixed — attribute marks the primary constructor)
[CompositeKey("{GuidValue}#{IntValue}~{StringValue}#{EnumValue}")]
[method: CompositeKeyConstructor]
public sealed partial record MixedKey(Guid GuidValue, int IntValue, string StringValue, MyEnum EnumValue)
{
    public MixedKey(Guid guidValue) : this(guidValue, default, string.Empty, default) { }
}
```
