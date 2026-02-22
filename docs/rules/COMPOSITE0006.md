# COMPOSITE0006: PrimaryKeySeparator not found in template string

| Property | Value |
|----------|-------|
| Rule ID | COMPOSITE0006 |
| Category | CompositeKey.SourceGeneration |
| Severity | Error |

## Cause

The `PrimaryKeySeparator` character was specified in the `[CompositeKey]` attribute but does not appear as a delimiter in the template string. The separator must exist in the template to divide the partition key from the sort key.

## How to fix

Ensure the `PrimaryKeySeparator` character is used as a delimiter in the template:

```csharp
// Before (error — '|' not in template)
[CompositeKey("{PartitionKey}#{SortKey}", PrimaryKeySeparator = '|')]
public sealed partial record MyKey(Guid PartitionKey, Guid SortKey);

// After (fixed — '|' separates partition and sort)
[CompositeKey("{PartitionKey}|{SortKey}", PrimaryKeySeparator = '|')]
public sealed partial record MyKey(Guid PartitionKey, Guid SortKey);
```
