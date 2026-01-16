# srs-mcp Tool Testing Issues

**Tested**: 2026-01-16
**Database**: srsdb (MySQL 8.0)

---

## Summary

The MCP server is connecting to the database and executing queries successfully (counts are correct), but **response serialization is broken** for collections and complex objects.

### Root Cause

When returning `List<T>`, `Dictionary<K,V>`, or arrays, the MCP server is outputting `.ToString()` representations instead of JSON-serialized data:

```
Items: "System.Collections.Generic.List`1[SRS.Service.Mcp.Models.Responses.SchemaResponse]"
```

Should be:
```json
Items: [{"schemaName": "collectionsmax", "charset": "latin1", ...}, ...]
```

---

## Tool Categories

### WORKING (Full Data Returned)

| Tool | Notes |
|------|-------|
| `get_table_statistics` | All scalar properties returned correctly |
| `describe_stored_procedure` | Full `RoutineDefinition` returned |
| `describe_view` | Full `ViewDefinition` returned |
| `describe_event` | Full `EventDefinition` returned |

These tools return flat objects with scalar/string properties - no collections.

---

### PARTIALLY WORKING (Metadata Only)

#### Paginated Responses (`PaginatedResponse<T>`)

All tools returning `PaginatedResponse<T>` have the same issue:

| Tool | TotalCount Works | Items Broken |
|------|------------------|--------------|
| `list_schemas` | 34 | `List`1[SchemaResponse]` |
| `list_tables` | 251 | `List`1[TableResponse]` |
| `list_stored_procedures` | 1079 | `List`1[StoredProcedureResponse]` |
| `list_views` | 15 | `List`1[ViewResponse]` |
| `list_triggers` | 83 | `List`1[TriggerResponse]` |
| `list_events` | 1 | `List`1[EventResponse]` |
| `describe_table` | 6 | `List`1[ColumnResponse]` |
| `describe_table_indexes` | 4 | `List`1[IndexInfo]` |
| `describe_table_constraints` | 1 | `List`1[ConstraintInfo]` |
| `get_server_status` | 4 | `List`1[ServerStatusResponse]` |
| `get_server_variables` | 1 | `List`1[ServerVariableResponse]` |
| `search_variables` | 18 | `List`1[ServerVariableResponse]` |
| `search_stored_procedures` | 129 | `List`1[StoredProcedureResponse]` |
| `search_views` | 1 | `List`1[ViewResponse]` |
| `search_triggers` | 39 | `List`1[TriggerResponse]` |
| `search_events` | 1 | `List`1[EventResponse]` |
| `get_top_queries` | 8100 | `List`1[QueryDigestSummary]` |
| `get_inefficient_queries` | 131 | `List`1[InefficientQuery]` |
| `get_queries_with_full_scans` | 1263 | `List`1[QueryDigestSummary]` |
| `get_queries_with_temp_tables` | 959 | `List`1[TempTableQuery]` |
| `get_queries_with_lock_contention` | 47 | `List`1[QueryWithLockContention]` |
| `search_query_digests` | 111 | `List`1[QueryDigestSearchResult]` |
| `get_process_list` | 3 | `List`1[ProcessResponse]` |
| `get_full_process_list` | 2 | `List`1[FullProcessResponse]` |
| `get_index_usage_statistics` | 512 | `List`1[IndexUsageStatistics]` |
| `get_unused_indexes` | 262 | `List`1[UnusedIndex]` |
| `get_memory_usage_global` | 199 | `List`1[MemoryUsageSummary]` |
| `get_memory_usage_by_thread` | 92 | `List`1[MemoryByThread]` |
| `get_table_io_summary` | 99 | `List`1[TableIoSummary]` |
| `get_table_lock_waits` | 98 | `List`1[TableLockWaitSummary]` |
| `get_file_io_summary` | 15 | `List`1[FileIoSummary]` |
| `get_wait_events_summary` | 20 | `List`1[WaitEventSummary]` |
| `get_procedure_statistics` | 313 | `List`1[ProcedureStatistics]` |
| `get_slowest_procedures` | 313 | `List`1[ProcedureStatistics]` |
| `get_procedures_with_errors` | 19 | `List`1[ProcedureStatistics]` |

#### Dictionary Responses

| Tool | Issue |
|------|-------|
| `get_server_info` | Shows `Count: 15` but `Keys` and `Values` are type strings |
| `describe_tables_batch` | Shows `Count: 1` but no actual table data |
| `describe_stored_procedures_batch` | Shows `Count: 2` but no actual SP data |
| `describe_table_foreign_keys` | Shows `Count: 0` (empty dict serialization issue) |

#### Mixed Scalar + Collection

| Tool | Scalar Works | Collection Broken |
|------|--------------|-------------------|
| `get_connection_pool_info` | `ThreadsConnected: 91`, `MaxConnections: 5461`, etc. | `ActiveThreads: "List`1[ThreadInfo]"` |
| `estimate_query_cost` | `EstimatedCost: 270`, `TotalRowsExamined: 135` | `IndexesUsed`, `TablesAccessed`, `Warnings` show type names |
| `describe_stored_procedure` | All fields work | `Parameters: "List`1[ParameterInfo]"` |

---

### NOT RETURNING DATA

| Tool | Output |
|------|--------|
| `list_databases` | Empty object `root{}:` |
| `explain_query` | Empty object `root{}:` (JSON format tested) |

---

### WORKING CORRECTLY (No Data / Expected Empty)

| Tool | Output | Status |
|------|--------|--------|
| `get_current_data_locks` | `TotalCount: 0` with message | Correct (no locks held) |
| `get_data_lock_waits` | `TotalCount: 0` with message | Correct (no waits) |
| `get_long_running_queries` | `TotalCount: 0` with message | Correct (none running) |
| `get_digest_details` | "No query digest found" | Correct (fake digest) |
| `get_query_stages` | "No execution stages found" | Correct (consumer disabled) |
| `get_query_execution_history` | Error about disabled consumer | Correct (informative error) |
| `describe_trigger` | "Trigger not found" | Correct (trigger doesn't exist) |

---

## Technical Analysis

### Output Format

All responses have this header format:
```
#version 1.0
root{}:
  PropertyName: Value
```

This appears to be YAML-like output from a custom serializer, not standard JSON.

### Pattern: Scalar vs Collection

**Works**: Direct scalar properties (`string`, `int`, `bool`, `DateTime`)
```
TableRows: 709268
TableCollation: latin1_swedish_ci
RoutineDefinition: "BEGIN..."
```

**Broken**: Collection properties (`List<T>`, `T[]`, `Dictionary<K,V>`)
```
Items: "System.Collections.Generic.List`1[...]"
Parameters: "System.Collections.Generic.List`1[...]"
```

### Likely Code Issue

The serializer is probably doing something like:
```csharp
// Current (broken)
output[propertyName] = value?.ToString() ?? "null";

// Should be
output[propertyName] = JsonSerializer.Serialize(value);
```

Or there's a custom `TypeConverter` or `JsonConverter` missing for collection types.

---

## Recommendations

### Priority 1: Fix Collection Serialization

The `PaginatedResponse<T>.Items` property must serialize the actual list contents:

```csharp
// In your MCP response handler/serializer
if (value is IEnumerable enumerable && value is not string)
{
    // Serialize as JSON array
    return JsonSerializer.Serialize(enumerable);
}
```

### Priority 2: Fix Dictionary Serialization

`get_server_info` returns `Dictionary<string, string>` - serialize as JSON object:
```json
{
  "version": "8.0.40",
  "hostname": "...",
  ...
}
```

### Priority 3: Fix explain_query

Returns empty - may be a different issue (query not executing or result not captured).

### Priority 4: Fix list_databases

Returns empty - check if the connection strings are being enumerated.

---

## Test Commands Used

```bash
# Tested via Claude Code MCP integration
# Each tool called with databaseName: "srsdb"
# Various filters (schemaPattern, limit, etc.) applied
```
