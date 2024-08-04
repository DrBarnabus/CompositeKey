# CompositeId

#### Fast and Optimized CompositeIds utilizing Source Generation

[![GitHub Release][gh-release-badge]][gh-release]
[![Build Status][gh-actions-badge]][gh-actions]

---

## What is CompositeId

**CompositeId** is a library for source generating optimal parsing and formatting code for composite identifiers in dotnet.


Implementing concepts widely used in NoSQL databases, such as [Amazon DynamoDb][dynamodb], a composite key is where
multiple discrete keys are combined to form a more complex structure for use as the primary key of an item in the data.

```csharp
// They can be simple
[CompositeId("{PartitionKey}#{SortKey:N}")]
public sealed partial record PrimaryKey(Guid PartitionKey, Guid SortKey);

Console.WriteLine(PrimaryKey.Parse($"{Guid.NewGuid()}#{Guid.NewGuid():N}"));

// Or they can be more complex
[CompositeId("{PartitionKey}|{AnyParsableValue:0.00}#ConstantValueAsPartOfKey@{FirstPartOfSortKey}~{SecondPartOfSortKey}", PrimaryKeySeparator = '#')]
public sealed partial record ComplexKey(string PartitionKey, SomeEnum FirstPartOfSortKey, Guid SecondPartOfSortKey)
{
    public required int AnyParsableValue { get; init; }
}

var complexKey = new ComplexKey(Guid.NewGuid().ToString(), SomeEnum.Value, Guid.NewGuid()) { AnyParsableValue = 123 };
Console.WriteLine(complexKey.ToString());
Console.WriteLine(complexKey.ToPartitionKeyString());
Console.WriteLine(complexKey.ToSortKeyString());
```

<!-- Badges -->
[gh-release-badge]: https://img.shields.io/github/v/release/DrBarnabus/CompositeId?color=g&style=for-the-badge
[gh-release]: https://github.com/DrBarnabus/CompositeId/releases/latest
[gh-actions-badge]: https://img.shields.io/github/actions/workflow/status/DrBarnabus/CompositeId/ci.yml?logo=github&branch=main&style=for-the-badge
[gh-actions]: https://github.com/DrBarnabus/CompositeId/actions/workflows/ci.yml

<!-- Links -->
[dynamodb]: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html
