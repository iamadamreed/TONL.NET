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
- **TONL.NET.AotTests** - AOT compilation verification tests (native publish)
- **TONL.NET.Benchmarks** - BenchmarkDotNet performance tests

### Key Types

- `TonlSerializer` - Main public API for serialize/deserialize operations
- `TonlReader` / `TonlWriter` - Low-level ref struct types for zero-allocation parsing/writing
- `TonlDocument` - Document-based API similar to JsonDocument for tree navigation
- `TonlBufferWriter` - ArrayPool-backed IBufferWriter<byte> implementation
- `TonlOptions` - Configuration options for serialization behavior

**Source Generation (STJ-like pattern):**
- `TonlSerializerContext` - Base class for generated contexts (like `JsonSerializerContext`)
- `TonlTypeInfo<T>` - Type metadata with fast-path serialize delegates
- `[TonlSourceGenerationOptions]` - Marks context classes for source generation
- `[TonlSerializable]` - Registers types on a context or marks standalone types

### Design Patterns

- **Ref Struct Pattern**: TonlReader and TonlWriter are ref structs for stack allocation
- **Source Generation**: Compile-time code generation eliminates runtime reflection
- **Buffer Writer Pattern**: IBufferWriter<byte> with ArrayPool for efficient memory usage
- **System.Text.Json-like API**: Familiar patterns for .NET developers
- **Context-Based Generation**: AOT-compatible pattern mirroring System.Text.Json source generation

### Source Generator Constraints

The source generator handles these edge cases:
- **Interfaces/Abstract classes**: Type info generated but no `CreateObject`
- **Init-only properties**: No `SetValue` delegate (serialization only)
- **Required properties**: No `SetValue` delegate (must use object initializer)
- **No parameterless constructor**: No `CreateObject` delegate
- **Primitive types**: Filtered from context generation (handled by core)

### Numeric Precision

To prevent precision loss:
- `float` → G9 format (round-trip fidelity)
- `double` → G17 format
- `decimal` → string format (preserves 28-29 digits)
- `enum` → Int64 (handles all underlying types)
- `ulong` → string (values > long.MaxValue)

### AOT Testing

```bash
# Build and run AOT tests
dotnet publish tests/TONL.NET.AotTests -c Release -r osx-arm64
./tests/TONL.NET.AotTests/bin/Release/net9.0/osx-arm64/publish/TONL.NET.AotTests
```

### Dependencies

- CommunityToolkit.HighPerformance (TONL.NET.Core)
- Microsoft.CodeAnalysis.CSharp (TONL.NET.SourceGenerator)
- xunit (TONL.NET.Tests)
- BenchmarkDotNet (TONL.NET.Benchmarks)
