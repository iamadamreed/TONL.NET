using TONL.NET;
using TONL.NET.Benchmarks.Generated;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace TONL.NET.Benchmarks;

/// <summary>
/// Benchmarks comparing reflection-based serialization to source-generated serialization.
/// Demonstrates the performance gains from compile-time code generation.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class SourceGeneratorBenchmarks
{
    private BenchmarkRecord _record = null!;
    private BenchmarkPoco _poco = null!;
    private LargeRecord _largeRecord = null!;
    private Dictionary<string, object?> _recordDict = null!;
    private Dictionary<string, object?> _pocoDict = null!;
    private Dictionary<string, object?> _largeRecordDict = null!;

    [GlobalSetup]
    public void Setup()
    {
        _record = new BenchmarkRecord(42, "Test User", true, 99.5, DateTime.UtcNow);
        _poco = new BenchmarkPoco
        {
            Id = 42,
            Name = "Test User",
            IsActive = true,
            Score = 99.5,
            CreatedAt = DateTime.UtcNow
        };
        _largeRecord = new LargeRecord(
            1, "Field1", 2, "Field2", 3, "Field3", 4, "Field4", 5, "Field5",
            6, "Field6", 7, "Field7", 8, "Field8", 9, "Field9", 10, "Field10");

        // Pre-create dictionaries for deserialization benchmarks
        _recordDict = BenchmarkRecordTonlSerializer.Serialize(_record);
        _pocoDict = BenchmarkPocoTonlSerializer.Serialize(_poco);
        _largeRecordDict = LargeRecordTonlSerializer.Serialize(_largeRecord);
    }

    // --- Serialization Benchmarks ---

    [Benchmark(Description = "Reflection - Serialize Record")]
    public string Reflection_Serialize_Record() => TonlSerializer.SerializeToString(_record);

    [Benchmark(Description = "Generated - Serialize Record")]
    public string Generated_Serialize_Record() => BenchmarkRecordTonlSerializer.SerializeToString(_record);

    [Benchmark(Description = "Reflection - Serialize POCO")]
    public string Reflection_Serialize_Poco() => TonlSerializer.SerializeToString(_poco);

    [Benchmark(Description = "Generated - Serialize POCO")]
    public string Generated_Serialize_Poco() => BenchmarkPocoTonlSerializer.SerializeToString(_poco);

    [Benchmark(Description = "Reflection - Serialize Large Record (20 props)")]
    public string Reflection_Serialize_Large() => TonlSerializer.SerializeToString(_largeRecord);

    [Benchmark(Description = "Generated - Serialize Large Record (20 props)")]
    public string Generated_Serialize_Large() => LargeRecordTonlSerializer.SerializeToString(_largeRecord);

    // --- Deserialization Benchmarks ---

    [Benchmark(Description = "Generated - Deserialize Record")]
    public BenchmarkRecord Generated_Deserialize_Record() => BenchmarkRecordTonlSerializer.Deserialize(_recordDict);

    [Benchmark(Description = "Generated - Deserialize POCO")]
    public BenchmarkPoco Generated_Deserialize_Poco() => BenchmarkPocoTonlSerializer.Deserialize(_pocoDict);

    [Benchmark(Description = "Generated - Deserialize Large Record")]
    public LargeRecord Generated_Deserialize_Large() => LargeRecordTonlSerializer.Deserialize(_largeRecordDict);

    // --- Dict-to-Dict Round Trip (Generated) ---

    [Benchmark(Description = "Generated - Round Trip Record")]
    public BenchmarkRecord Generated_RoundTrip_Record()
    {
        var dict = BenchmarkRecordTonlSerializer.Serialize(_record);
        return BenchmarkRecordTonlSerializer.Deserialize(dict);
    }

    [Benchmark(Description = "Generated - Round Trip Large")]
    public LargeRecord Generated_RoundTrip_Large()
    {
        var dict = LargeRecordTonlSerializer.Serialize(_largeRecord);
        return LargeRecordTonlSerializer.Deserialize(dict);
    }
}

// Benchmark models marked with [TonlSerializable]

[TonlSerializable]
public record BenchmarkRecord(int Id, string Name, bool IsActive, double Score, DateTime CreatedAt);

[TonlSerializable]
public class BenchmarkPoco
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public double Score { get; set; }
    public DateTime CreatedAt { get; set; }
}

[TonlSerializable]
public record LargeRecord(
    int Field1, string Field2, int Field3, string Field4, int Field5,
    string Field6, int Field7, string Field8, int Field9, string Field10,
    int Field11, string Field12, int Field13, string Field14, int Field15,
    string Field16, int Field17, string Field18, int Field19, string Field20);
