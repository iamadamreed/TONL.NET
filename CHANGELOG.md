# Changelog

All notable changes to TONL.NET will be documented in this file.

## [1.1.0] - 2026-03-08

### Added

- **IEnumerable\<T\> support** — root-level `IEnumerable<T>` collections now serialize correctly
- **Type argument discovery** — source generator recursively discovers referenced types for complete code generation
- **Recursive type discovery** — `SerializeToString` now performs deep type traversal to find all dependent serializable types

### Fixed

- **Spec compliance: block format for nested objects** — nested objects now correctly emit block format (`key{cols}:\n  prop: val`) instead of incorrect inline format (`key{cols}: val1, val2`), per TONL specification §5
- **Spec compliance: block-format array headers** — arrays of complex objects now include column names in the header (`key[N]{col1,col2}:`) instead of omitting them (`key[N]:`)
- **Spec compliance: deserializer block detection** — `DeserializeDocument` now correctly parses block-format arrays with column headers; previously treated all `key[N]{cols}:` headers as tabular, breaking round-trips for complex nested structures
- **Root-level collection serialization** — fixed serialization of `List<T>` and `IEnumerable<T>` as the root object
- **Nested collection and object property serialization** — fixed direct serializers writing `.ToString()` for collection/object properties in tabular rows
- **Source generator: duplicate hintName** — fixed hint name collisions for generic types in the source generator
- **Source generator: duplicate class names** — fixed class name collisions for generic types
- **Source generator: context-registered types** — fixed issues with types registered via `[TonlSerializable]` on a context class
- **Source generator: non-instantiable types** — skip serializer generation for interfaces and abstract classes
- **`SerializeToString` missing `Flush()`** — fixed missing buffer flush causing truncated output

### Tests

- 360 tests (up from 273) covering spec compliance, complex nested structures, tabular-vs-block decision logic, and round-trip fidelity

## [1.0.0] - 2026-01-16

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
  - Supports records, classes, and structs
  - Automatic property ordering for record constructors
  - AggressiveInlining for optimal performance

- **Test Suite**
  - 273 tests covering full TONL specification compliance
  - Data type tests, delimiter tests, edge cases, error handling
  - String handling including multiline and escape sequences
  - Source generator integration tests

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
