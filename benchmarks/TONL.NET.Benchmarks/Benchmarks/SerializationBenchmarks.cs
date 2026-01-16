using TONL.NET;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TONL.NET.Benchmarks;

/// <summary>
/// Serialization speed benchmarks comparing TONL.NET to System.Text.Json.
/// Uses fixtures aligned with TypeScript TONL SDK for comparable results.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class SerializationBenchmarks
{
    private object? _smallData;       // sample-users.json (611 B)
    private object? _mediumData;      // sample.json (6.8 KB)
    private object? _largeData;       // northwind.json (19.5 KB)

    private JsonSerializerOptions _jsonOptions = null!;
    private TonlBufferWriter _bufferWriter = null!;

    [GlobalSetup]
    public void Setup()
    {
        var fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures");

        _smallData = LoadFixture(Path.Combine(fixturesPath, "sample-users.json"));
        _mediumData = LoadFixture(Path.Combine(fixturesPath, "sample.json"));
        _largeData = LoadFixture(Path.Combine(fixturesPath, "northwind.json"));

        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
        _bufferWriter = new TonlBufferWriter(32768);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _bufferWriter?.Dispose();
    }

    // ============================================================
    // SMALL DATASET (sample-users.json - 611 B)
    // ============================================================

    [Benchmark(Description = "JSON - Small (611 B)")]
    public byte[] Json_Small() => JsonSerializer.SerializeToUtf8Bytes(_smallData, _jsonOptions);

    [Benchmark(Description = "TONL - Small (611 B)")]
    public byte[] Tonl_Small() => TonlSerializer.SerializeToBytes(_smallData);

    [Benchmark(Description = "TONL BufferWriter - Small")]
    public int Tonl_BufferWriter_Small()
    {
        _bufferWriter.Clear();
        TonlSerializer.Serialize(_bufferWriter, _smallData);
        return _bufferWriter.WrittenCount;
    }

    // ============================================================
    // MEDIUM DATASET (sample.json - 6.8 KB)
    // ============================================================

    [Benchmark(Description = "JSON - Medium (6.8 KB)")]
    public byte[] Json_Medium() => JsonSerializer.SerializeToUtf8Bytes(_mediumData, _jsonOptions);

    [Benchmark(Description = "TONL - Medium (6.8 KB)")]
    public byte[] Tonl_Medium() => TonlSerializer.SerializeToBytes(_mediumData);

    // ============================================================
    // LARGE DATASET (northwind.json - 19.5 KB)
    // ============================================================

    [Benchmark(Description = "JSON - Large (19.5 KB)")]
    public byte[] Json_Large() => JsonSerializer.SerializeToUtf8Bytes(_largeData, _jsonOptions);

    [Benchmark(Description = "TONL - Large (19.5 KB)")]
    public byte[] Tonl_Large() => TonlSerializer.SerializeToBytes(_largeData);

    // ============================================================
    // HELPER: Load JSON fixture to native types
    // ============================================================

    private static object? LoadFixture(string path)
    {
        var json = File.ReadAllText(path);
        return ConvertJsonToNative(json);
    }

    private static object? ConvertJsonToNative(string json)
    {
        var node = JsonNode.Parse(json);
        return ConvertNode(node);
    }

    private static object? ConvertNode(JsonNode? node)
    {
        if (node is null) return null;

        if (node is JsonArray array)
            return array.Select(ConvertNode).ToList();

        if (node is JsonObject obj)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var kvp in obj)
                dict[kvp.Key] = ConvertNode(kvp.Value);
            return dict;
        }

        if (node is JsonValue value)
        {
            var element = value.GetValue<JsonElement>();
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString(),
                JsonValueKind.Number when element.TryGetInt32(out var i) => i,
                JsonValueKind.Number when element.TryGetInt64(out var l) => l,
                JsonValueKind.Number => element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                _ => element.ToString()
            };
        }

        return null;
    }
}
