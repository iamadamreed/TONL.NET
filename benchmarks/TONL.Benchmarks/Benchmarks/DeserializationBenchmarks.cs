using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Text;
using System.Text.Json;
using TONL.Benchmarks.Models;

namespace TONL.Benchmarks;

/// <summary>
/// Deserialization speed benchmarks comparing TONL.NET to System.Text.Json.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net90)]
[MarkdownExporter]
public class DeserializationBenchmarks
{
    private byte[] _usersJsonBytes = null!;
    private byte[] _usersTonlBytes = null!;
    private byte[] _productsJsonBytes = null!;
    private byte[] _productsTonlBytes = null!;
    private byte[] _apiResponseJsonBytes = null!;
    private byte[] _apiResponseTonlBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Load JSON data using absolute paths
        var fixturesPath = Path.Combine(AppContext.BaseDirectory, "Fixtures");

        _usersJsonBytes = File.ReadAllBytes(Path.Combine(fixturesPath, "sample-users.json"));
        _productsJsonBytes = File.ReadAllBytes(Path.Combine(fixturesPath, "ecommerce-products.json"));
        _apiResponseJsonBytes = File.ReadAllBytes(Path.Combine(fixturesPath, "api-response.json"));

        // Pre-serialize to TONL for deserialization benchmarks
        var users = JsonSerializer.Deserialize<User[]>(_usersJsonBytes)!;
        _usersTonlBytes = TonlSerializer.SerializeToBytes(users);

        var products = JsonSerializer.Deserialize<ProductsContainer>(_productsJsonBytes)!;
        _productsTonlBytes = TonlSerializer.SerializeToBytes(products);

        var apiResponse = JsonSerializer.Deserialize<ApiResponse>(_apiResponseJsonBytes)!;
        _apiResponseTonlBytes = TonlSerializer.SerializeToBytes(apiResponse);
    }

    // --- Users Dataset ---

    [Benchmark(Description = "JSON Deserialize - Users")]
    public User[]? Json_Deserialize_Users() =>
        JsonSerializer.Deserialize<User[]>(_usersJsonBytes);

    [Benchmark(Description = "TONL Deserialize - Users")]
    public User[]? Tonl_Deserialize_Users() =>
        TonlSerializer.Deserialize<User[]>(_usersTonlBytes);

    [Benchmark(Description = "TONL to Dictionary - Users")]
    public Dictionary<string, object?>? Tonl_Dictionary_Users() =>
        TonlSerializer.DeserializeToDictionary(_usersTonlBytes);

    // --- Products Dataset ---

    [Benchmark(Description = "JSON Deserialize - Products")]
    public ProductsContainer? Json_Deserialize_Products() =>
        JsonSerializer.Deserialize<ProductsContainer>(_productsJsonBytes);

    [Benchmark(Description = "TONL Deserialize - Products")]
    public ProductsContainer? Tonl_Deserialize_Products() =>
        TonlSerializer.Deserialize<ProductsContainer>(_productsTonlBytes);

    // --- API Response ---

    [Benchmark(Description = "JSON Deserialize - API Response")]
    public ApiResponse? Json_Deserialize_ApiResponse() =>
        JsonSerializer.Deserialize<ApiResponse>(_apiResponseJsonBytes);

    [Benchmark(Description = "TONL Deserialize - API Response")]
    public ApiResponse? Tonl_Deserialize_ApiResponse() =>
        TonlSerializer.Deserialize<ApiResponse>(_apiResponseTonlBytes);
}
