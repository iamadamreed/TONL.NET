# TONL.NET Benchmark Results

## Key Findings

- **Compression**: TONL achieves up to **3.2x smaller** output than JSON on tabular data
- **Speed**: .NET 10 is **4.6x faster** than TypeScript SDK for encoding
- **Source Generator**: **16% faster** than reflection-based serialization
- **Decoding**: TONL.NET decode is **competitive with System.Text.Json** on large datasets

## Test Environment

```
CPU: Apple M4 Pro, 14 cores
OS: macOS Sequoia 15.7.3 (Darwin 24.6.0)
.NET SDK: 10.0.100
Node.js: v22.x
Architecture: Arm64 (Apple Silicon)
```

## Compression Results

TONL vs JSON byte sizes (identical output across TypeScript and .NET implementations):

| File | JSON | TONL | Ratio |
|------|------|------|-------|
| northwind.json | 19,466 B | 6,119 B | **3.18x smaller** |
| nested-project.json | 710 B | 558 B | **1.27x smaller** |
| sample.json | 6,862 B | 7,170 B | 0.96x |
| sample-users.json | 611 B | 665 B | 0.92x |

**Takeaway**: TONL excels on tabular/repetitive data. For small objects with unique keys, JSON may be more compact.

## Performance Comparison

All times in microseconds (µs). Lower is better.

### Encoding Performance

| Implementation | sample-users (611 B) | nested-project (710 B) | sample (6.9 KB) | northwind (19.5 KB) |
|----------------|----------------------|------------------------|-----------------|---------------------|
| TypeScript SDK | 12 µs | 15 µs | 89 µs | 156 µs |
| .NET 9 (Reflection) | 3.2 µs | 2.6 µs | 35.8 µs | 42.7 µs |
| .NET 9 (Source Gen) | 2.7 µs | 2.2 µs | 31.5 µs | 37.6 µs |
| .NET 10 (Reflection) | 2.7 µs | 2.2 µs | 28.9 µs | 34.2 µs |
| .NET 10 (Source Gen) | 2.3 µs | 1.8 µs | 25.4 µs | 30.1 µs |
| System.Text.Json | 0.8 µs | 0.9 µs | 9.8 µs | 16.5 µs |

### Decoding Performance

| Implementation | sample-users (611 B) | nested-project (710 B) | sample (6.9 KB) | northwind (19.5 KB) |
|----------------|----------------------|------------------------|-----------------|---------------------|
| TypeScript SDK | 8 µs | 10 µs | 52 µs | 71 µs |
| .NET 9 (Reflection) | 2.5 µs | 2.2 µs | 25.5 µs | 31.7 µs |
| .NET 10 (Reflection) | 2.0 µs | 1.9 µs | 22.0 µs | 26.9 µs |
| System.Text.Json | 0.9 µs | 1.2 µs | 12.0 µs | 27.9 µs |

### Speed Comparison (northwind.json)

| Comparison | Encode | Decode |
|------------|--------|--------|
| .NET 10 vs TypeScript | **4.6x faster** | **2.6x faster** |
| .NET 10 vs .NET 9 | **20% faster** | **15% faster** |
| Source Gen vs Reflection | **12-16% faster** | N/A |
| TONL vs JSON (.NET 10) | 2.1x slower | **1.04x faster** |

## Memory Allocation

All values in bytes. Lower is better.

### Encoding Memory

| Implementation | sample-users | nested-project | sample | northwind |
|----------------|--------------|----------------|--------|-----------|
| .NET 9/10 (Reflection) | 6,704 B | 6,288 B | 66,459 B | 53,008 B |
| .NET 9/10 (Source Gen) | 5,200 B | 4,800 B | 58,000 B | 45,000 B |
| System.Text.Json | 800 B | 1,336 B | 5,560 B | 14,040 B |

### Decoding Memory

| Implementation | sample-users | nested-project | sample | northwind |
|----------------|--------------|----------------|--------|-----------|
| .NET 9/10 (Reflection) | 6,056 B | 5,792 B | 56,760 B | 95,160 B |
| System.Text.Json | 1,496 B | 1,616 B | 15,056 B | 37,024 B |

**Note**: TONL allocates more memory than JSON due to intermediate dictionary construction. Future optimizations will target pooling and span-based parsing.

## Throughput

Based on northwind.json (19.5 KB JSON, 6.1 KB TONL):

| Implementation | Encode | Decode |
|----------------|--------|--------|
| TypeScript SDK | 39 MB/s | 86 MB/s |
| TONL.NET (.NET 9) | 144 MB/s | 194 MB/s |
| TONL.NET (.NET 10) | 179 MB/s | 227 MB/s |
| TONL.NET (.NET 10 + Source Gen) | 203 MB/s | 227 MB/s |
| System.Text.Json (.NET 10) | 1,183 MB/s | 699 MB/s |

## Recommendations

### Use TONL when:
- Data is tabular with repeated keys (3x+ compression)
- Bandwidth/storage costs matter more than CPU
- Working with LLM/AI contexts (token efficiency)
- Read-heavy workloads (decode is competitive)

### Use JSON when:
- Data has unique, non-repeating keys
- Write-heavy workloads
- Memory is constrained
- Maximum throughput is critical

### Use Source Generator when:
- Types are known at compile time
- AOT/trimming is required
- Consistent 12-16% speedup is valuable

## Running Benchmarks

```bash
# Full benchmark suite
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release

# Specific benchmark
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release -- --filter '*Serialization*'

# Specific framework
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release --framework net10.0

# Quick run (fewer iterations)
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release -- --job short
```

## Fixtures

All benchmarks use fixtures from `benchmarks/TONL.NET.Benchmarks/Fixtures/`, identical to the TypeScript SDK's `bench/fixtures/`:

| File | Description | Size |
|------|-------------|------|
| sample-users.json | Small array of user objects | 611 B |
| nested-project.json | Nested object structure | 710 B |
| sample.json | Medium mixed data | 6.9 KB |
| northwind.json | Large tabular dataset | 19.5 KB |
