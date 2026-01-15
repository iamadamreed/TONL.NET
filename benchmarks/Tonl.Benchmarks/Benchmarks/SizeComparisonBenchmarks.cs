using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tonl.Benchmarks;

/// <summary>
/// Size comparison benchmarks - measures output byte sizes for TONL vs JSON.
/// This data is directly comparable to official TONL TypeScript benchmarks.
/// </summary>
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 1, iterationCount: 1)]
[MarkdownExporter]
public class SizeComparisonBenchmarks
{
    private static readonly string[] AllFixtures =
    {
        "sample-users.json",
        "ecommerce-products.json",
        "complex-nested.json",
        "configuration.json",
        "api-response.json",
        "nested-project.json",
        "northwind.json",
        "sample.json",
        "large-dataset.json"
    };

    [Params("sample-users.json", "ecommerce-products.json", "api-response.json", "northwind.json")]
    public string FixtureName { get; set; } = null!;

    private object? _data;
    private byte[] _jsonBytes = null!;
    private JsonSerializerOptions _jsonOptions = null!;

    [GlobalSetup]
    public void Setup()
    {
        var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", FixtureName);
        var json = File.ReadAllText(fixturePath);
        _jsonBytes = File.ReadAllBytes(fixturePath);
        _data = ConvertJsonToNative(json);
        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
    }

    [Benchmark(Baseline = true, Description = "JSON Size (bytes)")]
    public int JsonSize() => _jsonBytes.Length;

    [Benchmark(Description = "TONL Size (bytes)")]
    public int TonlSize() => TonlSerializer.SerializeToBytes(_data).Length;

    /// <summary>
    /// Convert JSON string to native .NET types (Dictionary/List) that TonlSerializer can handle.
    /// </summary>
    private static object? ConvertJsonToNative(string json)
    {
        var node = JsonNode.Parse(json);
        return ConvertNode(node);
    }

    private static object? ConvertNode(JsonNode? node)
    {
        if (node is null) return null;

        if (node is JsonArray array)
        {
            return array.Select(ConvertNode).ToList();
        }

        if (node is JsonObject obj)
        {
            var dict = new Dictionary<string, object?>();
            foreach (var kvp in obj)
            {
                dict[kvp.Key] = ConvertNode(kvp.Value);
            }
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

    /// <summary>
    /// Result record for size comparison reports.
    /// </summary>
    public record SizeResult(
        string Fixture,
        int JsonBytes,
        int TonlBytes,
        double CompressionRatio,
        double SavingsPercent,
        int EstimatedJsonTokens,
        int EstimatedTonlTokens
    );

    /// <summary>
    /// Generates a comprehensive size report for all fixtures.
    /// Call separately from benchmark: dotnet run -- --size-report
    /// </summary>
    public static List<SizeResult> GenerateSizeReport()
    {
        var results = new List<SizeResult>();

        foreach (var fixture in AllFixtures)
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fixture);
            if (!File.Exists(path)) continue;

            var json = File.ReadAllText(path);
            var jsonBytes = File.ReadAllBytes(path);
            var data = ConvertJsonToNative(json);
            var tonlBytes = TonlSerializer.SerializeToBytes(data);

            var ratio = (double)jsonBytes.Length / tonlBytes.Length;
            var savings = (1.0 - (double)tonlBytes.Length / jsonBytes.Length) * 100;

            // Token estimation (chars/4 approximation)
            var jsonTokens = EstimateTokens(json);
            var tonlTokens = EstimateTokens(System.Text.Encoding.UTF8.GetString(tonlBytes));

            results.Add(new SizeResult(
                fixture,
                jsonBytes.Length,
                tonlBytes.Length,
                ratio,
                savings,
                jsonTokens,
                tonlTokens
            ));
        }

        return results;
    }

    /// <summary>
    /// Simple token estimation (chars/4 heuristic for GPT-style tokenizers).
    /// </summary>
    public static int EstimateTokens(string text)
    {
        var words = text.Split(new[] { ' ', '\n', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return (int)Math.Ceiling(words.Length * 1.3 + text.Length / 4.0);
    }
}
