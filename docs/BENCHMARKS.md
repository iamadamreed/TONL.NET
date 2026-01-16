# TONL.NET Benchmark Results

Comprehensive benchmark results comparing TONL.NET to System.Text.Json and the official TypeScript TONL SDK.

## Hardware Specifications

```
CPU: Apple M4 Pro, 14 logical/physical cores
OS: macOS Sequoia 15.7.3 (Darwin 24.6.0)
.NET SDK: 10.0.100
Runtimes: .NET 9.0.0, .NET 10.0.0
Architecture: Arm64 (Apple Silicon)
```

## Byte Size Comparison

### TypeScript TONL SDK Results

| File | JSON (bytes) | TONL (bytes) | Compression |
|------|--------------|--------------|-------------|
| sample-users.json | 611 | 665 | 0.92x |
| nested-project.json | 710 | 558 | **1.27x** |
| sample.json | 6,862 | 7,170 | 0.96x |
| northwind.json | 19,466 | 6,119 | **3.18x** |

### TONL.NET Results (identical output)

The .NET implementation produces identical TONL output to the TypeScript SDK, ensuring cross-platform compatibility.

**Key Insight:** TONL compression is most effective on:
- Tabular data with repeated keys (e.g., northwind.json)
- Data with homogeneous array structures
- Datasets where key names are verbose

For small objects with unique keys, JSON may actually be more compact.

## Encode/Decode Performance

### .NET 10.0 Results

| Fixture | TONL Encode | JSON Encode | TONL Decode | JSON Decode |
|---------|-------------|-------------|-------------|-------------|
| nested-project.json (710 B) | 2,161 ns | 937 ns | 1,892 ns | 1,213 ns |
| northwind.json (19.5 KB) | 34,174 ns | 16,467 ns | 26,934 ns | 27,877 ns |
| sample-users.json (611 B) | 2,687 ns | 798 ns | 2,025 ns | 916 ns |
| sample.json (6.9 KB) | 28,895 ns | 9,836 ns | 22,020 ns | 11,988 ns |

### .NET 9.0 Results

| Fixture | TONL Encode | JSON Encode | TONL Decode | JSON Decode |
|---------|-------------|-------------|-------------|-------------|
| nested-project.json (710 B) | 2,608 ns | 1,123 ns | 2,195 ns | 1,237 ns |
| northwind.json (19.5 KB) | 42,737 ns | 23,296 ns | 31,705 ns | 28,926 ns |
| sample-users.json (611 B) | 3,159 ns | 1,045 ns | 2,545 ns | 995 ns |
| sample.json (6.9 KB) | 35,779 ns | 12,420 ns | 25,454 ns | 12,047 ns |

### .NET 10 vs .NET 9 Improvement

| Metric | .NET 9 | .NET 10 | Improvement |
|--------|--------|---------|-------------|
| TONL Encode (northwind) | 42.7 µs | 34.2 µs | **20% faster** |
| TONL Decode (northwind) | 31.7 µs | 26.9 µs | **15% faster** |
| JSON Encode (northwind) | 23.3 µs | 16.5 µs | **29% faster** |
| JSON Decode (northwind) | 28.9 µs | 27.9 µs | **3% faster** |

## Memory Allocation

### .NET 10.0 Memory Usage

| Fixture | TONL Encode | JSON Encode | TONL Decode | JSON Decode |
|---------|-------------|-------------|-------------|-------------|
| nested-project.json | 6,288 B | 1,336 B | 5,792 B | 1,616 B |
| northwind.json | 53,008 B | 14,040 B | 95,160 B | 37,024 B |
| sample-users.json | 6,704 B | 800 B | 6,056 B | 1,496 B |
| sample.json | 66,459 B | 5,560 B | 56,760 B | 15,056 B |

TONL currently allocates more memory than JSON. Future optimizations will focus on:
- Reducing intermediate string allocations
- Pooling dictionaries and lists
- Span-based parsing improvements

## Source Generator Performance

The source generator eliminates reflection overhead for typed serialization:

| Benchmark | Reflection | Generated | Improvement |
|-----------|------------|-----------|-------------|
| Serialize Record (5 props) | 1,160 ns | 970 ns | **16% faster** |
| Serialize POCO (5 props) | 1,180 ns | 1,020 ns | **14% faster** |
| Serialize Large (20 props) | 3,890 ns | 3,440 ns | **12% faster** |

Generated serializers also provide:
- Compile-time type checking
- AOT/trimming compatibility
- Predictable performance characteristics

## Analysis

### When to Use TONL

**Best for:**
- Tabular data with repeated structures (3x+ compression)
- Network transmission where bandwidth matters
- LLM/AI contexts where token count matters
- Read-heavy workloads (decode is competitive)

**Consider JSON for:**
- Small, unique-keyed objects
- Write-heavy workloads
- Memory-constrained environments

### Throughput Comparison

Based on northwind.json (19.5 KB JSON, 6.1 KB TONL):

| Implementation | Encode Throughput | Decode Throughput |
|----------------|-------------------|-------------------|
| TONL.NET (.NET 10) | ~179 MB/s | ~227 MB/s |
| TONL.NET (.NET 9) | ~144 MB/s | ~194 MB/s |
| System.Text.Json (.NET 10) | ~1,183 MB/s | ~699 MB/s |

Note: TONL throughput is calculated against TONL output size (6.1 KB), while JSON throughput is against JSON size (19.5 KB).

## Running Benchmarks

```bash
# Run all benchmarks
dotnet run --project benchmarks/TONL.Benchmarks -c Release

# Run specific benchmark class
dotnet run --project benchmarks/TONL.Benchmarks -c Release -- --filter '*Serialization*'

# Run on specific framework
dotnet run --project benchmarks/TONL.Benchmarks -c Release --framework net10.0
```

## Benchmark Fixtures

All benchmarks use fixtures from `benchmarks/TONL.Benchmarks/Fixtures/`, which are identical to the TypeScript SDK's `bench/fixtures/` for cross-platform comparison:

- `sample-users.json` - Small array of user objects (611 B)
- `nested-project.json` - Nested object structure (710 B)
- `sample.json` - Medium mixed data (6.9 KB)
- `northwind.json` - Large tabular dataset (19.5 KB)
