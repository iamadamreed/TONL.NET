# Changelog

All notable changes to TONL.NET will be documented in this file.

## [1.2.2] - 2026-11-03

### Fixed

- **Source generator: absent keys in generated `Deserialize(Dictionary<string, object?>)`** — generated deserializers indexed `dict["Prop"]` directly and threw `KeyNotFoundException` when a key was absent. Since 1.2.1 omits missing tabular cells from decoded dictionaries, this made rows with missing cells crash when passed to a generated `XTonlSerializer.Deserialize`. Absent keys now read as `null`, and non-nullable `bool`/`decimal`/value-type parameters fall back to `default` instead of throwing `NullReferenceException` on unboxing.
- **Source generator: nullable warnings in generated code** — record/POCO members of collection, dictionary or nested-object type no longer emit CS8600/CS8604 in consumer builds with nullable enabled (14 warnings in the AOT test project → 0).

### Tests

- 387 tests (up from 383). AOT: 58/58.

## [1.2.1] - 2026-11-03

### Fixed

- **Spec compliance: missing fields in tabular rows** — an unquoted empty cell (e.g. `x, , z` or a trailing `x,`) is now treated as a missing field and omitted from the decoded dictionary, per TONL specification "Missing Fields in Tabular Format". Previously the key was added with a `null` value, making missing fields indistinguishable from explicit `null`. Quoted `""` continues to decode as an empty string. Typed deserialization of a missing cell leaves the property default in place (covered by a new test). Mirrors upstream tonl-dev/tonl `865391e`.

### Changed

- Tracked TONL reference implementation (`docs/tonl-spec`) updated from `d084d02` (v2.5.2) to `865391e`.
- `Microsoft.SourceLink.GitHub` 8.x → 10.x — clears NU1902 (GHSA-23fw-v26w-5fgq in transitive `Microsoft.Build.Tasks.Git` 8.0.0), which was failing restore under warnings-as-errors.
- Test runner `xunit.runner.visualstudio` 3.x → 4.x.
- GitHub Actions: `checkout` v7, `setup-dotnet` v6, `cache` v6, `action-gh-release` v3, `stale` v11, `dependabot/fetch-metadata` v3.

### Notes

- Upstream also began quoting integers beyond 2^53 on encode. That is a JavaScript `Number` precision workaround, not a spec rule; TONL.NET continues to emit `long`/`ulong` as bare integers. JS consumers will read such values as strings.

### Tests

- 383 tests (up from 377), including new tabular empty-string / missing-field / null regression tests. AOT: 58/58.

## [1.2.0] - 2026-05-20

### Added

- **`TonlElement`** — readonly struct wrapping pre-rendered TONL bytes. Use to embed dynamic-schema payloads inside source-gen-serialized envelopes (e.g., database result sets where the row schema is determined at runtime).
- **`TonlWriter.WriteRawValue(ReadOnlySpan<byte>)`** + **`WriteKeyRawValue(ReadOnlySpan<char>, ReadOnlySpan<byte>)`** — public primitives for emitting pre-rendered TONL fragments. Caller is responsible for fragment validity.
- **`TonlWriter.WriteUInt64(ulong)`** + **`WriteKeyUInt64(ReadOnlySpan<char>, ulong)`** — close the `ulong` API gap. Source generator now emits these for `ulong` properties instead of the previous `ToString()` fallback (which produced quoted strings for values > `long.MaxValue`).

### Changed

- Source generator now treats `TonlElement` as a leaf type during recursive type discovery — no recursion into its `Bytes` payload.
- `ulong` properties now serialize as bare integers (was: quoted strings via `.ToString()`). Existing serialized output of `ulong` values changes from `"123"` to `123` — non-breaking for spec conformance, but consumers parsing old output as strings may need updating.

### Tests

- 377 tests (up from 360) covering `WriteRawValue`, `WriteKeyRawValue`, `WriteUInt64`, `WriteKeyUInt64`, `TonlElement` source-gen special-case, and `ulong` bare-integer codegen. AOT test count: 58 (up from 51).

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
