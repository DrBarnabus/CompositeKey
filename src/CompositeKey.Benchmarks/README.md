# CompositeKey.Benchmarks

Performance benchmarks for generated CompositeKey code using [BenchmarkDotNet](https://benchmarkdotnet.org/).

These benchmarks are **manual, developer-run** and are **not** part of the CI test suite.

## Running Benchmarks

Run all benchmarks:

```bash
dotnet run -c Release --project src/CompositeKey.Benchmarks -- --filter "*"
```

Run a specific benchmark class:

```bash
dotnet run -c Release --project src/CompositeKey.Benchmarks -- --filter "*GuidPrimaryKeyBenchmarks*"
```

Run a specific benchmark method:

```bash
dotnet run -c Release --project src/CompositeKey.Benchmarks -- --filter "*ToString_GuidPrimaryKey*"
```

Target a specific TFM for cross-runtime comparison:

```bash
dotnet run -c Release --project src/CompositeKey.Benchmarks --framework net10.0 -- --filter "*"
```

## Interpreting Results

BenchmarkDotNet produces a summary table with these key columns:

| Column         | Description                                          |
|----------------|------------------------------------------------------|
| **Mean**       | Average execution time per operation                 |
| **Error**      | Half of the 99.9% confidence interval                |
| **StdDev**     | Standard deviation of measurements                   |
| **Gen0**       | Number of Gen 0 GC collections per 1000 operations   |
| **Allocated**  | Heap memory allocated per operation (bytes)           |

Lower values are better for all columns. An `Allocated` value of `0 B` or `-` indicates a zero-allocation code path.

Results are written to `BenchmarkDotNet.Artifacts/` which is git-ignored.
