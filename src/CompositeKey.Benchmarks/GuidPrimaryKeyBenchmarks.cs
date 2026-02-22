using BenchmarkDotNet.Attributes;

namespace CompositeKey.Benchmarks;

/// <summary>
/// Simple Guid primary key — exercises stackalloc split and Guid.TryParseExact parse path.
/// </summary>
[CompositeKey("{First}#{Second}")]
public sealed partial record GuidPrimaryKey(Guid First, Guid Second);

[MemoryDiagnoser]
public class GuidPrimaryKeyBenchmarks
{
    private GuidPrimaryKey _key = null!;
    private string _formatted = null!;

    [GlobalSetup]
    public void Setup()
    {
        _key = new GuidPrimaryKey(Guid.NewGuid(), Guid.NewGuid());
        _formatted = _key.ToString();
    }

    [Benchmark]
    public string ToString_GuidPrimaryKey() => _key.ToString();

    [Benchmark]
    public GuidPrimaryKey Parse_GuidPrimaryKey() => GuidPrimaryKey.Parse(_formatted);

    [Benchmark]
    public bool TryParse_GuidPrimaryKey() => GuidPrimaryKey.TryParse(_formatted, out _);
}
