using BenchmarkDotNet.Attributes;

namespace CompositeKey.Benchmarks;

/// <summary>
/// Fast-path enum key — exercises the generated enum helper fast path.
/// </summary>
[CompositeKey("{GuidValue}#Constant#{EnumValue}@{StringValue}")]
public sealed partial record FastPathEnumKey(Guid GuidValue, FastPathEnumKey.KeyEnumType EnumValue, string StringValue)
{
    public enum KeyEnumType { One, Two, Three, Four, Five, Six, Seven, Eight, Nine }
}

[MemoryDiagnoser]
public class FastPathEnumKeyBenchmarks
{
    private FastPathEnumKey _key = null!;
    private string _formatted = null!;

    [GlobalSetup]
    public void Setup()
    {
        _key = new FastPathEnumKey(Guid.NewGuid(), FastPathEnumKey.KeyEnumType.Five, "test-value");
        _formatted = _key.ToString();
    }

    [Benchmark]
    public string ToString_FastPathEnumKey() => _key.ToString();

    [Benchmark]
    public FastPathEnumKey Parse_FastPathEnumKey() => FastPathEnumKey.Parse(_formatted);

    [Benchmark]
    public bool TryParse_FastPathEnumKey() => FastPathEnumKey.TryParse(_formatted, out _);
}
