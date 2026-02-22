using BenchmarkDotNet.Attributes;

namespace CompositeKey.Benchmarks;

/// <summary>
/// Mixed-type composite key with partition/sort separation — exercises string.Create with exact length,
/// static lambda formatting, partition/sort splitting, and multi-type parsing.
/// </summary>
[CompositeKey("{GuidValue}#{DecimalValue}|Constant~{EnumValue}@{StringValue}", PrimaryKeySeparator = '|')]
public sealed partial record MixedCompositeKey(
    Guid GuidValue,
    decimal DecimalValue,
    MixedCompositeKey.KeyEnumType EnumValue,
    string StringValue)
{
    public enum KeyEnumType { One, Two, Three }
}

[MemoryDiagnoser]
public class MixedCompositeKeyBenchmarks
{
    private MixedCompositeKey _key = null!;
    private string _formatted = null!;

    [GlobalSetup]
    public void Setup()
    {
        _key = new MixedCompositeKey(Guid.NewGuid(), 123.45m, MixedCompositeKey.KeyEnumType.Two, "test-value");
        _formatted = _key.ToString();
    }

    [Benchmark]
    public string ToString_MixedCompositeKey() => _key.ToString();

    [Benchmark]
    public string ToPartitionKeyString_MixedCompositeKey() => _key.ToPartitionKeyString();

    [Benchmark]
    public string ToSortKeyString_MixedCompositeKey() => _key.ToSortKeyString();

    [Benchmark]
    public MixedCompositeKey Parse_MixedCompositeKey() => MixedCompositeKey.Parse(_formatted);

    [Benchmark]
    public bool TryParse_MixedCompositeKey() => MixedCompositeKey.TryParse(_formatted, out _);
}
