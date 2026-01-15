# Tonl.NET

A high-performance .NET implementation of the [TONL (Token-Optimized Notation Language)](https://github.com/iamadamreed/tonl) serialization format.

## Features

- **Full TONL Spec Compliance** - 263 tests passing against the official specification
- **High Performance** - Deserialization 1.1x faster than System.Text.Json
- **Excellent Compression** - 3.1x smaller output than JSON on typical datasets
- **Zero-Allocation Design** - Ref struct reader/writer for minimal GC pressure
- **Source Generation** - Optional compile-time serialization code generation
- **Familiar API** - System.Text.Json-like patterns for easy adoption

## Installation

```bash
dotnet add package Tonl.Core
```

## Quick Start

### Basic Serialization

```csharp
using Tonl;

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

## Performance

Benchmarks comparing Tonl.NET to System.Text.Json on the Northwind dataset (19 KB):

| Metric | Tonl.NET | System.Text.Json |
|--------|----------|------------------|
| Output Size | 6.1 KB | 19.0 KB |
| Compression | **3.1x smaller** | - |
| Deserialize | **1.1x faster** | baseline |
| Serialize | 1.9x slower | baseline |

TONL excels at compression while maintaining competitive deserialization speed. The format is ideal for:
- Network transmission where bandwidth matters
- Storage-constrained environments
- Applications that read data more often than write

## Project Structure

- **Tonl.Core** - Core serialization library
- **Tonl.SourceGenerator** - Roslyn-based source generator for compile-time serialization
- **Tonl.Tests** - Test suite with 263 spec compliance tests
- **Tonl.Benchmarks** - BenchmarkDotNet performance tests

## Building

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run benchmarks
dotnet run --project benchmarks/Tonl.Benchmarks -c Release
```

## License

MIT

## Related

- [TONL Specification](https://github.com/iamadamreed/tonl) - Official TONL format specification and TypeScript implementation
