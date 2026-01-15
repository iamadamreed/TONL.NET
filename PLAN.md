# Plan: Scaffold Tonl.NET Project

## Overview
Create a new .NET solution for a high-performance TONL (Token-Optimized Notation Language) serializer library with source generators.

## Project Structure

```
~/dev/Tonl.NET/
├── Tonl.sln
├── CLAUDE.md                          # Research findings and implementation plans
├── README.md                          # Project documentation
├── src/
│   ├── Tonl.Core/                     # Core serialization library
│   │   ├── Tonl.Core.csproj
│   │   ├── TonlSerializer.cs          # Main public API
│   │   ├── TonlReader.cs              # ref struct reader
│   │   ├── TonlWriter.cs              # ref struct writer
│   │   ├── TonlDocument.cs            # Document API (like JsonDocument)
│   │   ├── TonlBufferWriter.cs        # ArrayPool-backed IBufferWriter
│   │   ├── TonlOptions.cs             # Serialization options
│   │   └── Attributes/
│   │       └── TonlSerializableAttribute.cs
│   └── Tonl.SourceGenerator/          # Roslyn source generator
│       ├── Tonl.SourceGenerator.csproj
│       └── TonlSourceGenerator.cs
├── tests/
│   └── Tonl.Tests/                    # Unit tests
│       └── Tonl.Tests.csproj
└── benchmarks/
    └── Tonl.Benchmarks/               # BenchmarkDotNet performance tests
        └── Tonl.Benchmarks.csproj
```

## Implementation Steps

1. **Create solution structure**
   - Create src/, tests/, benchmarks/ directories
   - Create Tonl.Core class library (.NET 8.0)
   - Create Tonl.SourceGenerator analyzer library
   - Create Tonl.Tests xunit project
   - Create Tonl.Benchmarks console project
   - Add all projects to solution

2. **Configure project files**
   - Tonl.Core: Add CommunityToolkit.HighPerformance reference
   - Tonl.SourceGenerator: Configure as analyzer with Microsoft.CodeAnalysis.CSharp
   - Tonl.Tests: Add xunit references
   - Tonl.Benchmarks: Add BenchmarkDotNet reference

3. **Create stub files**
   - Create placeholder classes with key interfaces defined
   - Add XML documentation stubs

4. **Create CLAUDE.md**
   - Document research findings
   - Include architecture decisions
   - Add implementation roadmap
   - Include code patterns and examples

## Verification
- Run `dotnet build` to verify solution compiles
- Run `dotnet test` to verify test project is configured

## Files to Create
- ~/dev/Tonl.NET/CLAUDE.md
- ~/dev/Tonl.NET/src/Tonl.Core/Tonl.Core.csproj
- ~/dev/Tonl.NET/src/Tonl.Core/TonlSerializer.cs
- ~/dev/Tonl.NET/src/Tonl.Core/TonlReader.cs
- ~/dev/Tonl.NET/src/Tonl.Core/TonlWriter.cs
- ~/dev/Tonl.NET/src/Tonl.Core/TonlBufferWriter.cs
- ~/dev/Tonl.NET/src/Tonl.Core/TonlOptions.cs
- ~/dev/Tonl.NET/src/Tonl.Core/Attributes/TonlSerializableAttribute.cs
- ~/dev/Tonl.NET/src/Tonl.SourceGenerator/Tonl.SourceGenerator.csproj
- ~/dev/Tonl.NET/src/Tonl.SourceGenerator/TonlSourceGenerator.cs
- ~/dev/Tonl.NET/tests/Tonl.Tests/Tonl.Tests.csproj
- ~/dev/Tonl.NET/benchmarks/Tonl.Benchmarks/Tonl.Benchmarks.csproj
