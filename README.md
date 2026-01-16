# TONL.NET

A high-performance .NET implementation of [TONL (Token-Optimized Notation Language)](https://github.com/tonl-dev/tonl) - the token-optimized data serialization format.

## Features

- **Full TONL Spec Compliance** - 273 tests passing against the official specification
- **High Performance** - Competitive deserialization speed with System.Text.Json
- **Excellent Compression** - Up to 3.2x smaller output than JSON on typical datasets
- **Zero-Allocation Design** - Ref struct reader/writer for minimal GC pressure
- **Source Generation** - Compile-time serialization code generation with `[TonlSerializable]`
- **Familiar API** - System.Text.Json-like patterns for easy adoption

## Installation

```bash
dotnet add package TONL.NET
```

## Quick Start

### Basic Serialization

```csharp
using TONL.NET;

// Serialize an object to TONL
var user = new User { Name = "Alice", Age = 30 };
byte[] tonlBytes = TonlSerializer.SerializeToBytes(user);
string tonlString = TonlSerializer.Serialize(user);

// Deserialize from TONL
User? user = TonlSerializer.Deserialize<User>(tonlBytes);

// Deserialize to dynamic dictionary
var dict = TonlSerializer.DeserializeToDictionary(tonlString);
```

### Configuration Options

```csharp
var options = new TonlOptions
{
    Delimiter = ',',           // Default delimiter character
    PrettyDelimiters = false,  // Compact output (no spaces after delimiters)
};

string tonl = TonlSerializer.Serialize(data, options);
```

### Low-Level API

For maximum performance, use the ref struct reader/writer directly:

```csharp
// Writing
using var bufferWriter = new TonlBufferWriter(4096);
var writer = new TonlWriter(bufferWriter, TonlOptions.Default);
writer.WriteStartObject();
writer.WriteKey("name");
writer.WriteString("Alice");
writer.WriteEndObject();

// Reading
var reader = new TonlReader(tonlBytes);
while (reader.Read())
{
    switch (reader.TokenType)
    {
        case TonlTokenType.Key:
            Console.WriteLine($"Key: {reader.GetString()}");
            break;
        // ... handle other token types
    }
}
```

### Source Generator

For optimal performance, use the source generator to eliminate runtime reflection:

```csharp
using TONL.NET;

// Mark your types with [TonlSerializable]
[TonlSerializable]
public record User(int Id, string Name, bool IsActive);

// Generated serializer provides typed, reflection-free operations
var user = new User(1, "Alice", true);

// Serialize to dictionary (for further processing)
Dictionary<string, object?> dict = UserTonlSerializer.Serialize(user);

// Serialize directly to TONL string
string tonl = UserTonlSerializer.SerializeToString(user);

// Deserialize from dictionary
User restored = UserTonlSerializer.Deserialize(dict);
```

The source generator creates a `{TypeName}TonlSerializer` class with:
- `Serialize(T value)` → `Dictionary<string, object?>`
- `SerializeToString(T value, TonlOptions?)` → `string`
- `Deserialize(Dictionary<string, object?>)` → `T`

**Benefits:**
- 11-18% faster serialization than reflection
- Compile-time error checking
- AOT/trimming compatible
- Supports records, classes, and structs

## Performance

Benchmarks run on Apple M4 Pro (14 cores), .NET 10.0. See [detailed benchmark results](docs/BENCHMARKS.md) for full analysis.

### vs System.Text.Json

Benchmarks on the Northwind dataset (19 KB JSON → 6.1 KB TONL):

| Metric | TONL.NET | System.Text.Json | Ratio |
|--------|----------|------------------|-------|
| Output Size | 6.1 KB | 19.5 KB | **3.2x smaller** |
| Encode | 34.2 µs | 16.5 µs | 2.1x slower |
| Decode | 26.9 µs | 27.9 µs | **1.04x faster** |

TONL trades encode speed for compression. Decoding is competitive with JSON.

### Compression Ratios

| Dataset | JSON | TONL | Compression |
|---------|------|------|-------------|
| northwind.json | 19.5 KB | 6.1 KB | **3.2x** |
| nested-project.json | 710 B | 558 B | **1.3x** |
| sample.json | 6.9 KB | 7.2 KB | 0.96x |
| sample-users.json | 611 B | 665 B | 0.92x |

TONL compression is most effective on tabular/repetitive data structures.

### Ideal Use Cases

TONL excels at compression while maintaining competitive speed. The format is ideal for:
- Network transmission where bandwidth matters
- Storage-constrained environments
- Applications that read data more often than write
- LLM/AI contexts where token efficiency matters

## Project Structure

- **TONL.NET.Core** - Core serialization library
- **TONL.NET.SourceGenerator** - Roslyn-based source generator for compile-time serialization
- **TONL.NET.Tests** - Test suite with 273 spec compliance tests
- **TONL.NET.Benchmarks** - BenchmarkDotNet performance tests

## Building

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run benchmarks
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release
```

## Acknowledgments

This library is a .NET implementation of the **TONL format** created by the [TONL project](https://github.com/tonl-dev/tonl).

Special thanks to the official TONL project for:
- The TONL specification and format design
- Reference TypeScript implementation
- Comprehensive test fixtures used to validate this implementation

## License

MIT

## Related

- [TONL](https://github.com/tonl-dev/tonl) - Official TONL specification and TypeScript implementation
- [tonl.dev](https://tonl.dev) - TONL project homepage
