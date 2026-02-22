using BenchmarkDotNet.Attributes;

namespace CompositeKey.Benchmarks;

/// <summary>
/// Repeating collection key — exercises DefaultInterpolatedStringHandler formatting
/// and variable-length stackalloc parsing.
/// </summary>
[CompositeKey("{Type}#{Tags...,}")]
public sealed partial record RepeatingCollectionKey(string Type, List<string> Tags);

[MemoryDiagnoser]
public class RepeatingCollectionKeyBenchmarks
{
    private RepeatingCollectionKey _key = null!;
    private string _formatted = null!;

    [GlobalSetup]
    public void Setup()
    {
        _key = new RepeatingCollectionKey("entity", ["alpha", "beta", "gamma", "delta"]);
        _formatted = _key.ToString();
    }

    [Benchmark]
    public string ToString_RepeatingCollectionKey() => _key.ToString();

    [Benchmark]
    public RepeatingCollectionKey Parse_RepeatingCollectionKey() => RepeatingCollectionKey.Parse(_formatted);

    [Benchmark]
    public bool TryParse_RepeatingCollectionKey() => RepeatingCollectionKey.TryParse(_formatted, out _);
}
