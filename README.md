# CompositeKey

#### Fast and Optimized Composite Keys utilizing Source Generation

[![GitHub Release][gh-release-badge]][gh-release]
[![NuGet Downloads][nuget-downloads-badge]][nuget-downloads]
[![Build Status][gh-actions-badge]][gh-actions]
[![Codecov][codecov-badge]][codecov]

---

## Installation

```shell
dotnet add package CompositeKey
```

## What is CompositeKey

**CompositeKey** is a library for source generating optimal parsing and formatting code for composite identifiers in dotnet.

Implementing concepts widely used in NoSQL databases, such as [Amazon DynamoDb][dynamodb], a composite key is where
multiple discrete keys are combined to form a more complex structure for use as the primary key of an item in the data.

```csharp
// They can be simple
[CompositeKey("{PartitionKey}#{SortKey:N}")]
public sealed partial record PrimaryKey(Guid PartitionKey, Guid SortKey);

Console.WriteLine(PrimaryKey.Parse($"{Guid.NewGuid()}#{Guid.NewGuid():N}"));

// Or they can be more complex, with partition/sort separation and repeating sections
[CompositeKey("{PartitionKey}|{AnyParsableValue:0.00}#ConstantValueAsPartOfKey@{FirstPartOfSortKey}~{Values...~}", PrimaryKeySeparator = '#')]
public sealed partial record ComplexKey(string PartitionKey, SomeEnum FirstPartOfSortKey, ImmutableArray<Guid> Values)
{
    public required int AnyParsableValue { get; init; }
}

var complexKey = new ComplexKey(Guid.NewGuid().ToString(), SomeEnum.Value, [Guid.NewGuid(), Guid.NewGuid()]) { AnyParsableValue = 123 };
Console.WriteLine(complexKey.ToString());
Console.WriteLine(complexKey.ToPartitionKeyString());
Console.WriteLine(complexKey.ToSortKeyString());
```

## Diagnostics

CompositeKey includes analyzers that provide real-time feedback in your IDE. See the [diagnostics documentation](docs/rules/) for a full list of rules and how to resolve them.

<!-- Badges -->
[gh-release-badge]: https://img.shields.io/github/v/release/DrBarnabus/CompositeKey?color=g&style=for-the-badge
[gh-release]: https://github.com/DrBarnabus/CompositeKey/releases/latest
[nuget-downloads-badge]: https://img.shields.io/nuget/dt/CompositeKey?color=g&logo=nuget&style=for-the-badge
[nuget-downloads]: https://www.nuget.org/packages/CompositeKey
[gh-actions-badge]: https://img.shields.io/github/actions/workflow/status/DrBarnabus/CompositeKey/ci.yml?logo=github&branch=main&style=for-the-badge
[gh-actions]: https://github.com/DrBarnabus/CompositeKey/actions/workflows/ci.yml
[codecov-badge]: https://img.shields.io/codecov/c/github/DrBarnabus/CompositeKey?token=nSylLUGX90&style=for-the-badge&logo=codecov&logoColor=white
[codecov]: https://codecov.io/gh/DrBarnabus/CompositeKey

<!-- Links -->
[dynamodb]: https://docs.aws.amazon.com/amazondynamodb/latest/developerguide/Introduction.html
