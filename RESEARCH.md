# TONL.NET Research Document

## Table of Contents
- [TONL Format Overview](#tonl-format-overview)
- [Format Specification](#format-specification)
- [High-Performance C# Serialization Patterns](#high-performance-c-serialization-patterns)
- [Implementation Architecture](#implementation-architecture)
- [Code Patterns](#code-patterns)
- [Performance Benchmarks](#performance-benchmarks)
- [Sources](#sources)

---

## TONL Format Overview

**TONL (Token-Optimized Notation Language)** is a text-based serialization format designed for LLM token efficiency while remaining human-readable.

### Key Benefits

| Feature | Benefit |
|---------|---------|
| **Token Reduction** | 32-45% fewer tokens than JSON |
| **Size Reduction** | Up to 60% smaller than JSON |
| **Human-Readable** | Text-based, not binary |
| **Round-Trip Safe** | Perfect JSON <-> TONL conversion |
| **Schema Support** | TSL (TONL Schema Language) for validation |
| **Query API** | JSONPath-like syntax |

### Format Comparison

**JSON (245 bytes, 89 tokens):**
```json
{
  "users": [
    { "id": 1, "name": "Alice", "role": "admin" },
    { "id": 2, "name": "Bob, Jr.", "role": "user" },
    { "id": 3, "name": "Carol", "role": "editor" }
  ]
}
```

**TONL (158 bytes, 49 tokens - 45% reduction):**
```tonl
#version 1.0
users[3]{id:u32,name:str,role:str}:
  1, Alice, admin
  2, "Bob, Jr.", user
  3, Carol, editor
```

### Nested Object Example

**JSON:**
```json
{
  "user": {
    "id": 1,
    "name": "Alice",
    "contact": {
      "email": "alice@example.com",
      "phone": "+123456789"
    },
    "roles": ["admin", "editor"]
  }
}
```

**TONL:**
```tonl
#version 1.0
user{id:u32,name:str,contact:obj,roles:list}:
  id: 1
  name: Alice
  contact{email:str,phone:str}:
    email: alice@example.com
    phone: +123456789
  roles[2]: admin, editor
```

---

## Format Specification

### Document Structure

A TONL document consists of:
1. **Optional header section** with metadata
2. **Data section** with one or more blocks

```
[Headers]
[Block 1]
[Block 2]
...
[Block N]
```

### Header Lines

#### Version Header
```
#version <major.minor>
```
- **Required**: No (defaults to 1.0)
- **Example**: `#version 1.0`

#### Delimiter Header
```
#delimiter <delimiter>
```
- **Required**: No (defaults to comma)
- **Supported delimiters**: `,` `|` `\t` `;`
- **Example**: `#delimiter "|"`

### Data Types

| Type | Description | JSON Equivalent | TONL Syntax |
|------|-------------|----------------|-------------|
| `null` | Null value | `null` | `null` |
| `bool` | Boolean | `true`/`false` | `true`/`false` |
| `u32` | Unsigned 32-bit integer | `>= 0` | `123` |
| `i32` | Signed 32-bit integer | integer | `-456` |
| `f64` | 64-bit float | number | `3.14159` |
| `str` | String | string | `"hello"` |
| `obj` | Object/dictionary | object | Nested block |
| `list` | Array/list | array | Tabular or inline |

### Quoting Rules

A key **must be quoted** if it contains:
- Empty string `""`
- Hash symbol `#`
- At symbol `@`
- Colon `:`
- Comma `,`
- Braces `{` `}`
- Quote character `"`
- Leading/trailing whitespace
- Tab `\t` or newline `\n` `\r`

### Block Header Patterns

```
Object Header:       key{col1,col2,col3}:
Array Header:        key[N]{col1,col2}:
Primitive Array:     key[N]: val1, val2, val3
Key-Value Pair:      key: value
Indexed Key:         [0]: value
```

### Encoding Algorithm (Pseudo-code)

```python
function encodeTONL(data, options):
  delimiter = options.delimiter or ","

  # Generate headers
  output "#version " + (options.version or "1.0")
  if delimiter != ",":
    output "#delimiter " + escape_delimiter(delimiter)

  # Encode root value
  output encodeValue(data, "root", context)

function encodeValue(value, key, context):
  if value is null: return key + ": null"
  if is_primitive(value): return encodePrimitive(value, key, context)
  if is_array(value): return encodeArray(value, key, context)
  if is_object(value): return encodeObject(value, key, context)

function encodeArray(arr, key, context):
  if arr.length == 0: return key + "[0]:"
  if isUniformObjectArray(arr): return encodeTabularArray(arr, key, context)
  if all(is_primitive(item) for item in arr): return encodePrimitiveArray(arr, key, context)
  return encodeMixedArray(arr, key, context)
```

### Parsing Algorithm (Pseudo-code)

```python
function parseHeaders(lines):
  context = { version: "1.0", delimiter: "," }

  for line in lines:
    if line.startswith("#version"): context.version = parse_version(line)
    elif line.startswith("#delimiter"): context.delimiter = parse_delimiter(line)
    else: break

  return context

function parsePrimitiveValue(value_str):
  trimmed = value_str.strip()

  if trimmed.startswith('"') and trimmed.endswith('"'): return unquote(trimmed)
  if trimmed == "null" or trimmed == "": return null
  if trimmed == "true": return true
  if trimmed == "false": return false
  if is_numeric(trimmed): return parse_number(trimmed)
  return trimmed  # Unquoted string
```

---

## High-Performance C# Serialization Patterns

### Performance Tier Comparison

| Approach | Serialize Perf | Deserialize Perf | Memory | AOT Compatible |
|----------|---------------|------------------|--------|----------------|
| **Source Generator + Span<T>** | 10-200x faster | 10-50x faster | Zero alloc | Yes |
| Reflection-based | 1x (baseline) | 1x | High alloc | Limited |
| Jint (JS interop) | 0.1x | 0.1x | Very high | No |

### Top Performing Libraries (2025 Benchmarks)

- **MemoryPack**: 10x faster for standard objects, 50-200x faster for struct arrays
- **HyperSerializer**: Up to 16x faster than MessagePack/Protobuf
- **System.Text.Json**: 30-80% faster than Json.NET with zero allocations

### Core Techniques

1. **Span<T>/Memory<T>** - Zero-copy buffer access
2. **IBufferWriter<byte>** - Direct buffer writing
3. **Source Generators** - Compile-time code generation
4. **ArrayPool<T>** - Memory pooling
5. **Utf8JsonReader/Writer patterns** - Low-level text processing

---

## Implementation Architecture

### Recommended API Surface

```csharp
public interface ITonlSerializer
{
    // High-performance: write to IBufferWriter
    void Serialize<T>(IBufferWriter<byte> writer, T value);

    // Zero-copy: read from ReadOnlySpan
    T Deserialize<T>(ReadOnlySpan<byte> data);

    // Convenience: return byte array
    byte[] SerializeToBytes<T>(T value);

    // Stream support: for large files
    Task<T> DeserializeAsync<T>(Stream stream);
}
```

### Project Structure

```
Tonl.NET/
├── src/
│   ├── Tonl.Core/                 # Core serializer
│   │   ├── TonlReader.cs          # ref struct reader
│   │   ├── TonlWriter.cs          # ref struct writer
│   │   ├── TonlSerializer.cs      # Public API
│   │   └── TonlBufferWriter.cs    # ArrayPool-backed buffer
│   └── Tonl.SourceGenerator/      # Roslyn source generator
├── tests/
│   └── Tonl.Tests/                # Unit tests
└── benchmarks/
    └── Tonl.Benchmarks/           # BenchmarkDotNet tests
```

---

## Code Patterns

### 1. Zero-Copy Reader (ref struct)

```csharp
public ref struct TonlReader
{
    private ReadOnlySpan<byte> _buffer;
    private int _position;

    public TonlReader(ReadOnlySpan<byte> utf8Data)
    {
        _buffer = utf8Data;
        _position = 0;
    }

    // Pre-encoded property names for O(1) comparison
    private static readonly byte[] s_version = "#version"u8.ToArray();
    private static readonly byte[] s_delimiter = "#delimiter"u8.ToArray();

    public bool TryReadLine(out ReadOnlySpan<byte> line)
    {
        if (_position >= _buffer.Length) { line = default; return false; }

        int start = _position;
        int newlineIdx = _buffer.Slice(_position).IndexOf((byte)'\n');

        if (newlineIdx < 0)
        {
            line = _buffer.Slice(start);
            _position = _buffer.Length;
        }
        else
        {
            line = _buffer.Slice(start, newlineIdx);
            _position += newlineIdx + 1;
        }
        return true;
    }

    // Zero-allocation number parsing
    public int GetInt32(ReadOnlySpan<byte> token)
    {
        return int.Parse(token, provider: CultureInfo.InvariantCulture);
    }

    // Efficient property name comparison
    public bool ValueTextEquals(ReadOnlySpan<byte> token, ReadOnlySpan<byte> expected)
    {
        return token.SequenceEqual(expected);
    }
}
```

### 2. IBufferWriter-Based Writer

```csharp
public ref struct TonlWriter
{
    private IBufferWriter<byte> _output;
    private Span<byte> _buffer;
    private int _position;

    private static readonly byte[] s_newline = "\n"u8.ToArray();
    private static readonly byte[] s_versionHeader = "#version 1.0\n"u8.ToArray();

    public TonlWriter(IBufferWriter<byte> output)
    {
        _output = output;
        _buffer = output.GetSpan(512);
        _position = 0;
    }

    public void WriteHeader()
    {
        s_versionHeader.CopyTo(_buffer.Slice(_position));
        _position += s_versionHeader.Length;
    }

    public void WriteValue(ReadOnlySpan<byte> value)
    {
        EnsureCapacity(value.Length + 2);
        value.CopyTo(_buffer.Slice(_position));
        _position += value.Length;
    }

    public void WriteNewLine()
    {
        EnsureCapacity(1);
        _buffer[_position++] = (byte)'\n';
    }

    private void EnsureCapacity(int needed)
    {
        if (_position + needed > _buffer.Length)
        {
            _output.Advance(_position);
            _buffer = _output.GetSpan(Math.Max(needed, 512));
            _position = 0;
        }
    }

    public void Flush() => _output.Advance(_position);
}
```

### 3. ArrayPool-Backed Buffer Writer

```csharp
public sealed class TonlBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[] _buffer;
    private int _position;

    public TonlBufferWriter(int initialCapacity = 256)
    {
        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_position);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_position);
    }

    public void Advance(int count) => _position += count;

    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _position);

    public int WrittenCount => _position;

    public void Clear() => _position = 0;

    private void EnsureCapacity(int sizeHint)
    {
        if (_position + sizeHint <= _buffer.Length) return;

        int newSize = Math.Max(_buffer.Length * 2, _position + sizeHint);
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);
        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    public void Dispose()
    {
        if (_buffer != null)
        {
            ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
            _buffer = null!;
        }
    }
}
```

### 4. Source Generator Pattern

```csharp
[Generator]
public class TonlSourceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "Tonl.TonlSerializableAttribute",
                predicate: static (node, _) => node is ClassDeclarationSyntax or RecordDeclarationSyntax,
                transform: static (ctx, _) => GetTypeInfo(ctx))
            .Where(static t => t is not null);

        context.RegisterSourceOutput(types, GenerateSerializer);
    }

    private static void GenerateSerializer(SourceProductionContext ctx, TypeInfo? typeInfo)
    {
        if (typeInfo is null) return;

        var code = $$"""
            // <auto-generated/>
            #nullable enable

            namespace {{typeInfo.Namespace}}
            {
                public static partial class TonlSerializer
                {
                    public static void Serialize(
                        IBufferWriter<byte> writer,
                        {{typeInfo.FullName}} value)
                    {
                        var w = new TonlWriter(writer);
                        w.WriteHeader();
                        {{GeneratePropertyWrites(typeInfo)}}
                        w.Flush();
                    }

                    public static {{typeInfo.FullName}} Deserialize(
                        ReadOnlySpan<byte> data)
                    {
                        var reader = new TonlReader(data);
                        {{GeneratePropertyReads(typeInfo)}}
                    }
                }
            }
            """;

        ctx.AddSource($"{typeInfo.Name}.Tonl.g.cs", code);
    }
}
```

### 5. Streaming for Large Files

```csharp
public static async Task<T> DeserializeFromStreamAsync<T>(
    Stream stream,
    CancellationToken cancellationToken = default)
{
    byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
    try
    {
        int totalRead = 0;
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(
            buffer.AsMemory(totalRead), cancellationToken)) > 0)
        {
            totalRead += bytesRead;

            if (totalRead == buffer.Length)
            {
                byte[] newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                buffer.AsSpan(0, totalRead).CopyTo(newBuffer);
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = newBuffer;
            }
        }

        return TonlSerializer.Deserialize<T>(buffer.AsSpan(0, totalRead));
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
    }
}
```

### 6. BenchmarkDotNet Setup

```csharp
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public class TonlSerializationBenchmarks
{
    private TestModel _model = null!;
    private byte[] _serialized = null!;
    private TonlBufferWriter _pooledWriter = null!;

    [GlobalSetup]
    public void Setup()
    {
        _model = CreateTestModel();
        _pooledWriter = new TonlBufferWriter(4096);
        _serialized = TonlSerializer.SerializeToBytes(_model);
    }

    [GlobalCleanup]
    public void Cleanup() => _pooledWriter?.Dispose();

    [Benchmark(Baseline = true)]
    public byte[] Serialize_Json()
    {
        return JsonSerializer.SerializeToUtf8Bytes(_model);
    }

    [Benchmark]
    public byte[] Serialize_Tonl()
    {
        return TonlSerializer.SerializeToBytes(_model);
    }

    [Benchmark]
    public int Serialize_Tonl_Pooled()
    {
        _pooledWriter.Clear();
        TonlSerializer.Serialize(_pooledWriter, _model);
        return _pooledWriter.WrittenCount;
    }

    [Benchmark]
    public TestModel Deserialize_Tonl()
    {
        return TonlSerializer.Deserialize<TestModel>(_serialized);
    }
}
```

---

## Performance Benchmarks

### Expected Performance (Based on MemoryPack Patterns)

| Scenario | Expected Performance |
|----------|---------------------|
| Simple object serialize | ~44 nanoseconds |
| Struct array (1000 items) | 50-200x faster than JSON |
| Large document (1MB) | <100MB memory usage |
| Deserialization | Zero-allocation fast path |

### Memory Characteristics

- **ArrayPool<byte>.Shared.Rent()**: ~44 nanoseconds, zero allocation
- **Arrays >85KB**: Use ArrayPool to avoid LOH allocations
- **ref struct readers/writers**: Stack-allocated, no GC pressure

---

## Sources

### TONL Format
- [GitHub - tonl-dev/tonl](https://github.com/tonl-dev/tonl)
- [TONL Homepage](https://tonl.dev/)
- [TONL Implementation Reference](https://github.com/tonl-dev/tonl/blob/main/docs/IMPLEMENTATION_REFERENCE.md)
- [TONL Format Specification](https://github.com/tonl-dev/tonl/blob/main/docs/SPECIFICATION.md)

### High-Performance .NET Serialization
- [MemoryPack - Zero encoding extreme performance binary serializer](https://github.com/Cysharp/MemoryPack)
- [How to make the fastest .NET Serializer](https://neuecc.medium.com/how-to-make-the-fastest-net-serializer-with-net-7-c-11-case-of-memorypack-ad28c0366516)

### Span<T> and Memory<T>
- [All About Span - Microsoft Learn](https://learn.microsoft.com/en-us/archive/msdn-magazine/2018/january/csharp-all-about-span-exploring-a-new-net-mainstay)
- [Improve C# code performance with Span<T>](https://blog.ndepend.com/improve-c-code-performance-with-spant/)
- [Creating Efficient String Parsers in C#](https://www.w3computing.com/articles/creating-efficient-string-parsers-and-tokenizers-in-csharp/)

### ArrayPool and Memory Management
- [Pooling large arrays with ArrayPool - Adam Sitnik](https://adamsitnik.com/Array-Pool/)
- [ArrayPool: The most underused memory optimization](https://medium.com/@vladamisici1/arraypool-the-most-underused-memory-optimization-in-net-8c47f5dffbbd)
- [Optimizing Array Performance in .NET](https://dotnettips.wordpress.com/2025/10/01/optimizing-array-performance-in-net-getting-the-most-from-arraypool/)

### Source Generators
- [Incremental Roslyn Source Generators - Thinktecture](https://www.thinktecture.com/en/net/roslyn-source-generators-introduction/)
- [System.Text.Json Source Generation - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/source-generation-modes)
- [Source Generators in Real World Scenarios](https://www.cazzulino.com/source-generators.html)

### IBufferWriter and Modern APIs
- [MemoryOwner<T> - CommunityToolkit](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/high-performance/memoryowner)
- [Pooling IBufferWriter - Laszlo](https://blog.ladeak.net/posts/pooling-bufferwriter)
- [MessagePack for C# v2 - I/O Pipelines](https://neuecc.medium.com/messagepack-for-c-v2-new-era-of-net-core-unity-i-o-pipelines-6950643c1053)

### Utf8JsonReader/Writer
- [How to use Utf8JsonReader - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader)
- [How to use Utf8JsonWriter - Microsoft Learn](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonwriter)

### Benchmarking
- [BenchmarkDotNet GitHub](https://github.com/dotnet/BenchmarkDotNet)
- [Proper benchmarking for .NET serialization - Scott Hanselman](https://www.hanselman.com/blog/proper-benchmarking-to-diagnose-and-solve-a-net-serialization-bottleneck)

### Memory Pooling
- [Microsoft.IO.RecyclableMemoryStream](https://github.com/microsoft/Microsoft.IO.RecyclableMemoryStream)
- [CommunityToolkit.HighPerformance](https://www.nuget.org/packages/CommunityToolkit.HighPerformance/)
