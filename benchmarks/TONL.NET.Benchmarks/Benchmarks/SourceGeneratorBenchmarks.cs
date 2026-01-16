using TONL.NET;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace TONL.NET.Benchmarks;

/// <summary>
/// Benchmarks comparing reflection-based serialization to source-generated serialization.
/// Uses the context-based pattern (similar to System.Text.Json) for AOT-compatible serialization.
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
    private string _recordTonl = null!;
    private string _pocoTonl = null!;
    private string _largeRecordTonl = null!;

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

        // Pre-serialize for deserialization benchmarks
        _recordTonl = TonlSerializer.SerializeToString(_record, BenchmarkTonlContext.Default.BenchmarkRecord);
        _pocoTonl = TonlSerializer.SerializeToString(_poco, BenchmarkTonlContext.Default.BenchmarkPoco);
        _largeRecordTonl = TonlSerializer.SerializeToString(_largeRecord, BenchmarkTonlContext.Default.LargeRecord);
    }

    // --- Serialization Benchmarks ---

    [Benchmark(Description = "Reflection - Serialize Record")]
    public string Reflection_Serialize_Record() => TonlSerializer.SerializeToString(_record);

    [Benchmark(Description = "SourceGen - Serialize Record")]
    public string SourceGen_Serialize_Record() => TonlSerializer.SerializeToString(_record, BenchmarkTonlContext.Default.BenchmarkRecord);

    [Benchmark(Description = "Reflection - Serialize POCO")]
    public string Reflection_Serialize_Poco() => TonlSerializer.SerializeToString(_poco);

    [Benchmark(Description = "SourceGen - Serialize POCO")]
    public string SourceGen_Serialize_Poco() => TonlSerializer.SerializeToString(_poco, BenchmarkTonlContext.Default.BenchmarkPoco);

    [Benchmark(Description = "Reflection - Serialize Large Record (20 props)")]
    public string Reflection_Serialize_Large() => TonlSerializer.SerializeToString(_largeRecord);

    [Benchmark(Description = "SourceGen - Serialize Large Record (20 props)")]
    public string SourceGen_Serialize_Large() => TonlSerializer.SerializeToString(_largeRecord, BenchmarkTonlContext.Default.LargeRecord);

    // --- Serialization to Bytes (AOT path) ---

    [Benchmark(Description = "SourceGen - Serialize Record to Bytes")]
    public byte[] SourceGen_Serialize_Record_Bytes() => TonlSerializer.SerializeToBytes(_record, BenchmarkTonlContext.Default.BenchmarkRecord);

    [Benchmark(Description = "SourceGen - Serialize Large to Bytes")]
    public byte[] SourceGen_Serialize_Large_Bytes() => TonlSerializer.SerializeToBytes(_largeRecord, BenchmarkTonlContext.Default.LargeRecord);
}

// Benchmark models for source generation

public record BenchmarkRecord(int Id, string Name, bool IsActive, double Score, DateTime CreatedAt);

public class BenchmarkPoco
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public double Score { get; set; }
    public DateTime CreatedAt { get; set; }
}

public record LargeRecord(
    int Field1, string Field2, int Field3, string Field4, int Field5,
    string Field6, int Field7, string Field8, int Field9, string Field10,
    int Field11, string Field12, int Field13, string Field14, int Field15,
    string Field16, int Field17, string Field18, int Field19, string Field20);

/// <summary>
/// Source generation context for benchmark types.
/// Uses the STJ-like pattern for AOT-compatible serialization.
/// </summary>
[TonlSourceGenerationOptions]
[TonlSerializable(typeof(BenchmarkRecord))]
[TonlSerializable(typeof(BenchmarkPoco))]
[TonlSerializable(typeof(LargeRecord))]
public partial class BenchmarkTonlContext : TonlSerializerContext { }
