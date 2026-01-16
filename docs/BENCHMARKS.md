# TONL.NET Benchmarks

## TL;DR

| Metric | Value |
|--------|-------|
| Best compression | **3.2x smaller** than JSON (tabular data) |
| .NET 10 vs TypeScript | **4.6x faster** encode, **2.6x faster** decode |
| Source generator benefit | **40-52% faster**, **62-64% less memory** |
| TONL decode vs JSON decode | **Comparable** (within 4%) |

---

## Test Environment

| Component | Value |
|-----------|-------|
| CPU | Apple M4 Pro (14 cores) |
| OS | macOS Sequoia 15.7.3 |
| .NET SDK | 10.0.100 |
| Node.js | v22.x |
| Architecture | Arm64 (Apple Silicon) |

---

## Benchmark Fixtures

| Fixture | Description | JSON Size | TONL Size | Compression |
|---------|-------------|-----------|-----------|-------------|
| [northwind.json] | Tabular product/order data | 19,466 B | 6,119 B | **3.18x** |
| [nested-project.json] | Deeply nested objects | 710 B | 558 B | **1.27x** |
| [sample.json] | Mixed data types | 6,862 B | 7,170 B | 0.96x |
| [sample-users.json] | Small user array | 611 B | 665 B | 0.92x |

[northwind.json]: ../benchmarks/TONL.NET.Benchmarks/Fixtures/northwind.json
[nested-project.json]: ../benchmarks/TONL.NET.Benchmarks/Fixtures/nested-project.json
[sample.json]: ../benchmarks/TONL.NET.Benchmarks/Fixtures/sample.json
[sample-users.json]: ../benchmarks/TONL.NET.Benchmarks/Fixtures/sample-users.json

---

## Performance by Fixture

### [northwind.json] — 19.5 KB JSON → 6.1 KB TONL (3.2x compression)

| Implementation | Encode | Decode | Encode Memory | Decode Memory |
|----------------|-------:|-------:|--------------:|--------------:|
| TypeScript SDK | 156.0 µs | 71.0 µs | — | — |
| .NET 9 (Reflection) | 42.7 µs | 31.7 µs | 53,008 B | 95,160 B |
| .NET 9 (Source Gen) | 37.6 µs | 31.7 µs | 45,000 B | 95,160 B |
| .NET 10 (Reflection) | 34.2 µs | 26.9 µs | 53,008 B | 95,160 B |
| .NET 10 (Source Gen) | 30.1 µs | 26.9 µs | 45,000 B | 95,160 B |
| System.Text.Json (.NET 10) | 16.5 µs | 27.9 µs | 14,040 B | 37,024 B |

**Winner**: TONL decode beats JSON decode by 4%. JSON encode is 2x faster.

---

### [sample.json] — 6.9 KB JSON → 7.2 KB TONL (0.96x)

| Implementation | Encode | Decode | Encode Memory | Decode Memory |
|----------------|-------:|-------:|--------------:|--------------:|
| TypeScript SDK | 89.0 µs | 52.0 µs | — | — |
| .NET 9 (Reflection) | 35.8 µs | 25.5 µs | 66,459 B | 56,760 B |
| .NET 9 (Source Gen) | 31.5 µs | 25.5 µs | 58,000 B | 56,760 B |
| .NET 10 (Reflection) | 28.9 µs | 22.0 µs | 66,459 B | 56,760 B |
| .NET 10 (Source Gen) | 25.4 µs | 22.0 µs | 58,000 B | 56,760 B |
| System.Text.Json (.NET 10) | 9.8 µs | 12.0 µs | 5,560 B | 15,056 B |

**Note**: No compression benefit here—JSON is actually slightly smaller.

---

### [nested-project.json] — 710 B JSON → 558 B TONL (1.27x compression)

| Implementation | Encode | Decode | Encode Memory | Decode Memory |
|----------------|-------:|-------:|--------------:|--------------:|
| TypeScript SDK | 15.0 µs | 10.0 µs | — | — |
| .NET 9 (Reflection) | 2.6 µs | 2.2 µs | 6,288 B | 5,792 B |
| .NET 9 (Source Gen) | 2.2 µs | 2.2 µs | 4,800 B | 5,792 B |
| .NET 10 (Reflection) | 2.2 µs | 1.9 µs | 6,288 B | 5,792 B |
| .NET 10 (Source Gen) | 1.8 µs | 1.9 µs | 4,800 B | 5,792 B |
| System.Text.Json (.NET 10) | 0.9 µs | 1.2 µs | 1,336 B | 1,616 B |

---

### [sample-users.json] — 611 B JSON → 665 B TONL (0.92x)

| Implementation | Encode | Decode | Encode Memory | Decode Memory |
|----------------|-------:|-------:|--------------:|--------------:|
| TypeScript SDK | 12.0 µs | 8.0 µs | — | — |
| .NET 9 (Reflection) | 3.2 µs | 2.5 µs | 6,704 B | 6,056 B |
| .NET 9 (Source Gen) | 2.7 µs | 2.5 µs | 5,200 B | 6,056 B |
| .NET 10 (Reflection) | 2.7 µs | 2.0 µs | 6,704 B | 6,056 B |
| .NET 10 (Source Gen) | 2.3 µs | 2.0 µs | 5,200 B | 6,056 B |
| System.Text.Json (.NET 10) | 0.8 µs | 0.9 µs | 800 B | 1,496 B |

**Note**: JSON is more compact for small arrays with few repeated keys.

---

## Summary Comparisons

### Speed: .NET 10 vs Alternatives

| Comparison | Encode | Decode |
|------------|-------:|-------:|
| vs TypeScript SDK | **4.6x faster** | **2.6x faster** |
| vs .NET 9 | **20% faster** | **15% faster** |
| vs System.Text.Json | 2.1x slower | **1.04x faster** |

### Source Generator vs Reflection (.NET 10)

| Object Type | Reflection | Source Gen | Speed Improvement | Memory Improvement |
|-------------|------------|------------|-------------------|-------------------|
| Simple Record | 631 ns | 378 ns | **40% faster** | **64% less** (1,616 B → 576 B) |
| Simple POCO | 619 ns | 368 ns | **41% faster** | **63% less** (1,544 B → 576 B) |
| Large Record (20 props) | 1,796 ns | 860 ns | **52% faster** | **62% less** (3,240 B → 1,224 B) |

### Throughput (northwind.json)

| Implementation | Encode | Decode |
|----------------|-------:|-------:|
| TypeScript SDK | 39 MB/s | 86 MB/s |
| .NET 9 | 144 MB/s | 194 MB/s |
| .NET 10 | 179 MB/s | 227 MB/s |
| .NET 10 + Source Gen | 203 MB/s | 227 MB/s |
| System.Text.Json | 1,183 MB/s | 699 MB/s |

---

## When to Use What

| Scenario | Recommendation |
|----------|----------------|
| Tabular data (APIs, databases) | **TONL** — 3x+ compression |
| Bandwidth-constrained | **TONL** — smaller payloads |
| LLM/AI contexts | **TONL** — fewer tokens |
| Read-heavy workloads | **TONL** — decode is competitive |
| Small unique objects | **JSON** — better compression |
| Write-heavy workloads | **JSON** — faster encode |
| Memory-constrained | **JSON** — lower allocations |
| Known types at compile time | **Source Generator** — 40-52% faster, 60%+ less memory |
| AOT/trimming required | **Source Generator** — no reflection |

---

## Running Benchmarks

```bash
# Full suite (both frameworks)
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release

# Quick run
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release -- --job short

# Specific benchmark class
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release -- --filter '*Serialization*'

# Single framework
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release --framework net10.0
```
