using System.Collections.Immutable;
using BenchmarkDotNet.Attributes;

namespace CompositeKey.Benchmarks;

/// <summary>
/// Repeating SpanParsable key with same separator and ImmutableArray — exercises the same-separator
/// parse path (variable part count from initial split), ImmutableArray.CreateRange() construction,
/// and SpanParsable item parsing within the repeat loop.
/// </summary>
[CompositeKey("NODES#{Scores...#}")]
public sealed partial record RepeatingSpanParsableSameSeparatorKey(ImmutableArray<int> Scores);

[MemoryDiagnoser]
public class RepeatingSpanParsableBenchmarks
{
    private RepeatingSpanParsableSameSeparatorKey _key = null!;
    private string _formatted = null!;

    [GlobalSetup]
    public void Setup()
    {
        _key = new RepeatingSpanParsableSameSeparatorKey([100, 200, 300, 400]);
        _formatted = _key.ToString();
    }

    [Benchmark]
    public string ToString_RepeatingSpanParsable() => _key.ToString();

    [Benchmark]
    public RepeatingSpanParsableSameSeparatorKey Parse_RepeatingSpanParsable() =>
        RepeatingSpanParsableSameSeparatorKey.Parse(_formatted);

    [Benchmark]
    public bool TryParse_RepeatingSpanParsable() =>
        RepeatingSpanParsableSameSeparatorKey.TryParse(_formatted, out _);
}
