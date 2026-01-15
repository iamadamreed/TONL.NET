# Changelog

All notable changes to TONL.NET will be documented in this file.

## [1.0.0-alpha] - 2025-01-15

### Added

- **Core Serialization Library**
  - `TonlSerializer` - Main public API for serialize/deserialize operations
  - `TonlReader` - Zero-allocation ref struct parser
  - `TonlWriter` - Zero-allocation ref struct writer
  - `TonlDocument` - Document-based API for tree navigation
  - `TonlBufferWriter` - ArrayPool-backed buffer writer

- **Configuration Options**
  - `TonlOptions.Delimiter` - Configurable delimiter character (`,`, `|`, `;`)
  - `TonlOptions.PrettyDelimiters` - Toggle spaces after delimiters for readability

- **Source Generator**
  - `[TonlSerializable]` attribute for compile-time code generation
  - Eliminates runtime reflection overhead

- **Test Suite**
  - 263 tests covering full TONL specification compliance
  - Data type tests, delimiter tests, edge cases, error handling
  - String handling including multiline and escape sequences

- **Benchmark Suite**
  - Serialization and deserialization speed comparisons
  - Memory allocation profiling
  - Size compression analysis

### Performance

- 3.1x compression ratio vs JSON on typical datasets
- Deserialization 1.1x faster than System.Text.Json
- Competitive with official TypeScript implementation

### Specification Compliance

- Full compliance with TONL specification
- Proper handling of all data types (strings, numbers, booleans, null, arrays, objects)
- Multiline string support with `"""` triple-quote syntax
- Configurable delimiters (`,`, `|`, `;`)
