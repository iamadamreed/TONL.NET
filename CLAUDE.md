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
dotnet run --project benchmarks/TONL.NET.Benchmarks -c Release
```

## Architecture

TONL.NET is a high-performance serialization library for TONL (Token-Optimized Notation Language) targeting .NET 9.0 and .NET 10.0.

### Namespace Convention

**IMPORTANT**: The root namespace is `TONL.NET`. All sub-namespaces follow `TONL.NET.X`:
- `TONL.NET` - Core types (TonlSerializer, TonlReader, TonlWriter, etc.)
- `TONL.NET.SourceGenerator` - Source generator types
- `TONL.NET.Tests` - Test types
- `TONL.NET.Benchmarks` - Benchmark types
- `TONL.NET.Generated` - Generated serializer code

### CLI Commands for Project Management

Always use CLI tools instead of manually creating project files:
```bash
# Create new class library
dotnet new classlib -n TONL.NET.NewProject -o src/TONL.NET.NewProject

# Add to solution
dotnet sln add src/TONL.NET.NewProject/TONL.NET.NewProject.csproj

# Add project reference
dotnet add src/TONL.NET.Core reference src/TONL.NET.NewProject

# Add package reference
dotnet add src/TONL.NET.Core package PackageName
```

### Projects

- **TONL.NET.Core** - Core serialization library with ref struct reader/writer for minimal allocations
- **TONL.NET.SourceGenerator** - Roslyn-based source generator that generates serialization code at compile time
- **TONL.NET.Tests** - xUnit test project
- **TONL.NET.Benchmarks** - BenchmarkDotNet performance tests

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

- CommunityToolkit.HighPerformance (TONL.NET.Core)
- Microsoft.CodeAnalysis.CSharp (TONL.NET.SourceGenerator)
- xunit (TONL.NET.Tests)
- BenchmarkDotNet (TONL.NET.Benchmarks)
