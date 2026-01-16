using TONL.NET;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace TONL.NET.Benchmarks;

/// <summary>
/// Deserialization speed benchmarks comparing TONL.NET to System.Text.Json.
/// Uses fixtures aligned with TypeScript TONL SDK for comparable results.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net10_0)]
[MarkdownExporter]
public class DeserializationBenchmarks
{
    // Small: sample-users.json (611 B)
    private byte[] _smallJsonBytes = null!;
    private byte[] _smallTonlBytes = null!;

    // Medium: sample.json (6.8 KB)
    private byte[] _mediumJsonBytes = null!;
    private byte[] _mediumTonlBytes = null!;

    // Large: northwind.json (19.5 KB)
    private byte[] _largeJsonBytes = null!;
    private byte[] _largeTonlBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        var fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures");

        // Load small fixture
        _smallJsonBytes = File.ReadAllBytes(Path.Combine(fixturesPath, "sample-users.json"));
        _smallTonlBytes = ConvertJsonToTonl(_smallJsonBytes);

        // Load medium fixture
        _mediumJsonBytes = File.ReadAllBytes(Path.Combine(fixturesPath, "sample.json"));
        _mediumTonlBytes = ConvertJsonToTonl(_mediumJsonBytes);

        // Load large fixture
        _largeJsonBytes = File.ReadAllBytes(Path.Combine(fixturesPath, "northwind.json"));
        _largeTonlBytes = ConvertJsonToTonl(_largeJsonBytes);
    }

    // ============================================================
    // SMALL DATASET (sample-users.json - 611 B)
    // ============================================================

    [Benchmark(Description = "JSON Decode - Small (611 B)")]
    public JsonNode? Json_Decode_Small() => JsonNode.Parse(_smallJsonBytes);

    [Benchmark(Description = "TONL Decode - Small (611 B)")]
    public Dictionary<string, object?>? Tonl_Decode_Small() =>
        TonlSerializer.DeserializeToDictionary(_smallTonlBytes);

    // ============================================================
    // MEDIUM DATASET (sample.json - 6.8 KB)
    // ============================================================

    [Benchmark(Description = "JSON Decode - Medium (6.8 KB)")]
    public JsonNode? Json_Decode_Medium() => JsonNode.Parse(_mediumJsonBytes);

    [Benchmark(Description = "TONL Decode - Medium (6.8 KB)")]
    public Dictionary<string, object?>? Tonl_Decode_Medium() =>
        TonlSerializer.DeserializeToDictionary(_mediumTonlBytes);

    // ============================================================
    // LARGE DATASET (northwind.json - 19.5 KB)
    // ============================================================

    [Benchmark(Description = "JSON Decode - Large (19.5 KB)")]
    public JsonNode? Json_Decode_Large() => JsonNode.Parse(_largeJsonBytes);

    [Benchmark(Description = "TONL Decode - Large (19.5 KB)")]
    public Dictionary<string, object?>? Tonl_Decode_Large() =>
        TonlSerializer.DeserializeToDictionary(_largeTonlBytes);

    // ============================================================
    // HELPER: Convert JSON bytes to TONL bytes
    // ============================================================

    private static byte[] ConvertJsonToTonl(byte[] jsonBytes)
    {
        var json = System.Text.Encoding.UTF8.GetString(jsonBytes);
        var data = ConvertJsonToNative(json);
        return TonlSerializer.SerializeToBytes(data);
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
