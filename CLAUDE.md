# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
# Build the solution
dotnet build

# Run tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run benchmarks
dotnet run --project benchmarks/TONL.Benchmarks -c Release
```

## Architecture

TONL.NET is a high-performance serialization library for TONL (Token-Optimized Notation Language) targeting .NET 8.0.

### Projects

- **TONL.Core** - Core serialization library with ref struct reader/writer for minimal allocations
- **TONL.SourceGenerator** - Roslyn-based source generator that generates serialization code at compile time
- **TONL.Tests** - xUnit test project
- **TONL.Benchmarks** - BenchmarkDotNet performance tests

### Key Types

- `TonlSerializer` - Main public API for serialize/deserialize operations
- `TonlReader` / `TonlWriter` - Low-level ref struct types for zero-allocation parsing/writing
- `TonlDocument` - Document-based API similar to JsonDocument for tree navigation
- `TonlBufferWriter` - ArrayPool-backed IBufferWriter<byte> implementation
- `TonlOptions` - Configuration options for serialization behavior
- `[TonlSerializable]` - Attribute to mark types for source-generated serialization

### Design Patterns

- **Ref Struct Pattern**: TonlReader and TonlWriter are ref structs for stack allocation
- **Source Generation**: Compile-time code generation eliminates runtime reflection
- **Buffer Writer Pattern**: IBufferWriter<byte> with ArrayPool for efficient memory usage
- **System.Text.Json-like API**: Familiar patterns for .NET developers

### Dependencies

- CommunityToolkit.HighPerformance (TONL.Core)
- Microsoft.CodeAnalysis.CSharp (TONL.SourceGenerator)
- xunit (TONL.Tests)
- BenchmarkDotNet (TONL.Benchmarks)
