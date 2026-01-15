using TONL.NET;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Text.Json;
using TONL.NET.Benchmarks.Models;

namespace TONL.NET.Benchmarks;

/// <summary>
/// Serialization speed benchmarks comparing TONL.NET to System.Text.Json.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[MarkdownExporter]
public class SerializationBenchmarks
{
    private User[] _users = null!;
    private ProductsContainer _products = null!;
    private ApiResponse _apiResponse = null!;

    private JsonSerializerOptions _jsonOptions = null!;
    private TonlOptions _tonlOptions = null!;
    private TonlBufferWriter _bufferWriter = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Load fixture data using absolute paths
        var fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures");

        var usersJson = File.ReadAllText(Path.Combine(fixturesPath, "sample-users.json"));
        _users = JsonSerializer.Deserialize<User[]>(usersJson)!;

        var productsJson = File.ReadAllText(Path.Combine(fixturesPath, "ecommerce-products.json"));
        _products = JsonSerializer.Deserialize<ProductsContainer>(productsJson)!;

        var apiResponseJson = File.ReadAllText(Path.Combine(fixturesPath, "api-response.json"));
        _apiResponse = JsonSerializer.Deserialize<ApiResponse>(apiResponseJson)!;

        _jsonOptions = new JsonSerializerOptions { WriteIndented = false };
        _tonlOptions = TonlOptions.Default;
        _bufferWriter = new TonlBufferWriter(4096);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _bufferWriter?.Dispose();
    }

    // --- Users Dataset (Small - 3 items) ---

    [Benchmark(Description = "JSON - Users (3 items)")]
    public byte[] Json_Users() => JsonSerializer.SerializeToUtf8Bytes(_users, _jsonOptions);

    [Benchmark(Description = "TONL - Users (3 items)")]
    public byte[] Tonl_Users() => TonlSerializer.SerializeToBytes(_users, _tonlOptions);

    [Benchmark(Description = "TONL BufferWriter - Users")]
    public int Tonl_BufferWriter_Users()
    {
        _bufferWriter.Clear();
        TonlSerializer.Serialize(_bufferWriter, _users, _tonlOptions);
        return _bufferWriter.WrittenCount;
    }

    // --- Products Dataset (Medium - nested objects with arrays) ---

    [Benchmark(Description = "JSON - Products")]
    public byte[] Json_Products() => JsonSerializer.SerializeToUtf8Bytes(_products, _jsonOptions);

    [Benchmark(Description = "TONL - Products")]
    public byte[] Tonl_Products() => TonlSerializer.SerializeToBytes(_products, _tonlOptions);

    // --- API Response (Complex - deeply nested) ---

    [Benchmark(Description = "JSON - API Response")]
    public byte[] Json_ApiResponse() => JsonSerializer.SerializeToUtf8Bytes(_apiResponse, _jsonOptions);

    [Benchmark(Description = "TONL - API Response")]
    public byte[] Tonl_ApiResponse() => TonlSerializer.SerializeToBytes(_apiResponse, _tonlOptions);
}
