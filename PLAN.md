# Plan: Scaffold TONL.NET Project

## Overview
Create a new .NET solution for a high-performance TONL (Token-Optimized Notation Language) serializer library with source generators.

## Project Structure

```
~/dev/TONL.NET/
├── TONL.sln
├── CLAUDE.md                          # Research findings and implementation plans
├── README.md                          # Project documentation
├── src/
│   ├── TONL.Core/                     # Core serialization library
│   │   ├── TONL.Core.csproj
│   │   ├── TonlSerializer.cs          # Main public API
│   │   ├── TonlReader.cs              # ref struct reader
│   │   ├── TonlWriter.cs              # ref struct writer
│   │   ├── TonlDocument.cs            # Document API (like JsonDocument)
│   │   ├── TonlBufferWriter.cs        # ArrayPool-backed IBufferWriter
│   │   ├── TonlOptions.cs             # Serialization options
│   │   └── Attributes/
│   │       └── TonlSerializableAttribute.cs
│   └── TONL.SourceGenerator/          # Roslyn source generator
│       ├── TONL.SourceGenerator.csproj
│       └── TonlSourceGenerator.cs
├── tests/
│   └── TONL.Tests/                    # Unit tests
│       └── TONL.Tests.csproj
└── benchmarks/
    └── TONL.Benchmarks/               # BenchmarkDotNet performance tests
        └── TONL.Benchmarks.csproj
```

## Implementation Steps

1. **Create solution structure**
   - Create src/, tests/, benchmarks/ directories
   - Create TONL.Core class library (.NET 8.0)
   - Create TONL.SourceGenerator analyzer library
   - Create TONL.Tests xunit project
   - Create TONL.Benchmarks console project
   - Add all projects to solution

2. **Configure project files**
   - TONL.Core: Add CommunityToolkit.HighPerformance reference
   - TONL.SourceGenerator: Configure as analyzer with Microsoft.CodeAnalysis.CSharp
   - TONL.Tests: Add xunit references
   - TONL.Benchmarks: Add BenchmarkDotNet reference

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
- ~/dev/TONL.NET/CLAUDE.md
- ~/dev/TONL.NET/src/TONL.Core/TONL.Core.csproj
- ~/dev/TONL.NET/src/TONL.Core/TonlSerializer.cs
- ~/dev/TONL.NET/src/TONL.Core/TonlReader.cs
- ~/dev/TONL.NET/src/TONL.Core/TonlWriter.cs
- ~/dev/TONL.NET/src/TONL.Core/TonlBufferWriter.cs
- ~/dev/TONL.NET/src/TONL.Core/TonlOptions.cs
- ~/dev/TONL.NET/src/TONL.Core/Attributes/TonlSerializableAttribute.cs
- ~/dev/TONL.NET/src/TONL.SourceGenerator/TONL.SourceGenerator.csproj
- ~/dev/TONL.NET/src/TONL.SourceGenerator/TonlSourceGenerator.cs
- ~/dev/TONL.NET/tests/TONL.Tests/TONL.Tests.csproj
- ~/dev/TONL.NET/benchmarks/TONL.Benchmarks/TONL.Benchmarks.csproj
