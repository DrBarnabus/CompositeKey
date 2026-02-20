# COMPOSITE0011: Repeating property is not last in its key section

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0011 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

A repeating property is not the last value part in its key section. Because repeating sections produce a variable number of values, they must appear at the end of their section (the partition key portion or the sort key portion) so the parser can correctly determine where the repeating values end.

## How to fix

Move the repeating property to the end of its key section:

```csharp
// Before (error — repeating property is followed by another property)
[CompositeKey("{Tags...,}#{Name}", PrimaryKeySeparator = '#')]
public sealed partial record MyKey(List<string> Tags, string Name);

// After (fixed — repeating property is last in the partition section)
[CompositeKey("{Name}#{Tags...,}", PrimaryKeySeparator = '#')]
public sealed partial record MyKey(string Name, List<string> Tags);
```

In a composite key with `PrimaryKeySeparator`, repeating properties can appear at the end of either the partition or sort section:

```csharp
[CompositeKey("{TenantId}|LOCATION#{LocationIds...#}", PrimaryKeySeparator = '|')]
public sealed partial record MyKey(Guid TenantId, IReadOnlyList<Guid> LocationIds);
```
