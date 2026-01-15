using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Text.Json;
using Tonl.Benchmarks.Models;

namespace Tonl.Benchmarks;

/// <summary>
/// Memory allocation benchmarks comparing allocation patterns.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
[MarkdownExporter]
public class MemoryBenchmarks
{
    private User[] _users = null!;
    private byte[] _usersTonlBytes = null!;
    private byte[] _usersJsonBytes = null!;
    private TonlBufferWriter _reusableBuffer = null!;

    [GlobalSetup]
    public void Setup()
    {
        var json = File.ReadAllText("Fixtures/sample-users.json");
        _users = JsonSerializer.Deserialize<User[]>(json)!;
        _usersTonlBytes = TonlSerializer.SerializeToBytes(_users);
        _usersJsonBytes = JsonSerializer.SerializeToUtf8Bytes(_users);
        _reusableBuffer = new TonlBufferWriter(4096);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _reusableBuffer?.Dispose();
    }

    // --- Serialization Allocation Patterns ---

    /// <summary>
    /// Baseline: New byte[] allocation each call.
    /// </summary>
    [Benchmark(Baseline = true, Description = "TONL Serialize (new byte[])")]
    public byte[] Tonl_NewAllocation() => TonlSerializer.SerializeToBytes(_users);

    /// <summary>
    /// Optimized: Reuse TonlBufferWriter (ArrayPool backed).
    /// </summary>
    [Benchmark(Description = "TONL Serialize (reused BufferWriter)")]
    public int Tonl_ReusedBuffer()
    {
        _reusableBuffer.Clear();
        TonlSerializer.Serialize(_reusableBuffer, _users);
        return _reusableBuffer.WrittenCount;
    }

    /// <summary>
    /// JSON baseline for allocation comparison.
    /// </summary>
    [Benchmark(Description = "JSON Serialize")]
    public byte[] Json_Serialize() => JsonSerializer.SerializeToUtf8Bytes(_users);

    // --- Deserialization Allocation Patterns ---

    /// <summary>
    /// TONL deserialization allocation measurement.
    /// </summary>
    [Benchmark(Description = "TONL Deserialize")]
    public User[]? Tonl_Deserialize() => TonlSerializer.Deserialize<User[]>(_usersTonlBytes);

    /// <summary>
    /// JSON deserialization allocation baseline.
    /// </summary>
    [Benchmark(Description = "JSON Deserialize")]
    public User[]? Json_Deserialize() => JsonSerializer.Deserialize<User[]>(_usersJsonBytes);

    /// <summary>
    /// TONL to Dictionary (untyped deserialization).
    /// </summary>
    [Benchmark(Description = "TONL to Dictionary")]
    public Dictionary<string, object?>? Tonl_ToDictionary() =>
        TonlSerializer.DeserializeToDictionary(_usersTonlBytes);
}
