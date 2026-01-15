using TONL.NET;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Text.Json;
using TONL.NET.Benchmarks.Models;

namespace TONL.NET.Benchmarks;

/// <summary>
/// Scalability benchmarks testing performance with varying data sizes.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[MarkdownExporter]
public class ScalabilityBenchmarks
{
    private User[] _users100 = null!;
    private User[] _users1000 = null!;
    private User[] _users10000 = null!;

    private JsonSerializerOptions _jsonOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };

        // Generate datasets of varying sizes
        _users100 = GenerateUsers(100);
        _users1000 = GenerateUsers(1000);
        _users10000 = GenerateUsers(10000);
    }

    private static User[] GenerateUsers(int count)
    {
        var random = new Random(42); // Fixed seed for reproducibility
        var roles = new[] { "admin", "user", "editor", "viewer" };
        var tags = new[] { "engineering", "marketing", "sales", "support", "hr" };

        return Enumerable.Range(0, count).Select(i => new User(
            i,
            $"User {i}",
            $"user{i}@example.com",
            roles[random.Next(roles.Length)],
            random.Next(2) == 1,
            tags.Take(random.Next(1, 4)).ToArray(),
            DateTime.UtcNow.AddDays(-random.Next(365))
        )).ToArray();
    }

    // --- 100 Items ---

    [Benchmark(Description = "JSON Serialize - 100 items")]
    public byte[] Json_100() => JsonSerializer.SerializeToUtf8Bytes(_users100, _jsonOptions);

    [Benchmark(Description = "TONL Serialize - 100 items")]
    public byte[] Tonl_100() => TonlSerializer.SerializeToBytes(_users100);

    // --- 1,000 Items ---

    [Benchmark(Description = "JSON Serialize - 1000 items")]
    public byte[] Json_1000() => JsonSerializer.SerializeToUtf8Bytes(_users1000, _jsonOptions);

    [Benchmark(Description = "TONL Serialize - 1000 items")]
    public byte[] Tonl_1000() => TonlSerializer.SerializeToBytes(_users1000);

    // --- 10,000 Items ---

    [Benchmark(Description = "JSON Serialize - 10000 items")]
    public byte[] Json_10000() => JsonSerializer.SerializeToUtf8Bytes(_users10000, _jsonOptions);

    [Benchmark(Description = "TONL Serialize - 10000 items")]
    public byte[] Tonl_10000() => TonlSerializer.SerializeToBytes(_users10000);

    // --- Size comparison for scalability ---

    [Benchmark(Description = "JSON Size - 1000 items")]
    public int JsonSize_1000() => JsonSerializer.SerializeToUtf8Bytes(_users1000, _jsonOptions).Length;

    [Benchmark(Description = "TONL Size - 1000 items")]
    public int TonlSize_1000() => TonlSerializer.SerializeToBytes(_users1000).Length;
}
