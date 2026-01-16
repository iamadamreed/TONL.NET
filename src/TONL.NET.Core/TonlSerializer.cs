using System.Buffers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace TONL.NET;

/// <summary>
/// Provides methods for serializing and deserializing objects to/from TONL format.
/// </summary>
public static class TonlSerializer
{
    #region Source-Generated Overloads (AOT-safe)

    /// <summary>
    /// Serializes a value using source-generated type info.
    /// This is the preferred AOT-safe method for serialization.
    /// </summary>
    /// <typeparam name="T">The type to serialize.</typeparam>
    /// <param name="writer">The buffer writer to write to.</param>
    /// <param name="value">The value to serialize.</param>
    /// <param name="typeInfo">The source-generated type info.</param>
    /// <param name="options">Optional serialization options.</param>
    public static void Serialize<T>(
        IBufferWriter<byte> writer,
        T value,
        TonlTypeInfo<T> typeInfo,
        TonlOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        options ??= typeInfo.OriginatingContext?.Options ?? TonlOptions.Default;
        options.Validate();

        var tonlWriter = new TonlWriter(writer, options);
        tonlWriter.WriteHeader();

        // Use fast-path if available
        if (typeInfo.Serialize is not null)
        {
            tonlWriter.WriteIndent(0);
            tonlWriter.WriteObjectHeader("root", GetPropertyNames(typeInfo));
            tonlWriter.WriteNewLine();
            typeInfo.Serialize(ref tonlWriter, value);
        }
        else
        {
            // Fall back to metadata-based serialization
            SerializeWithMetadata(ref tonlWriter, value, typeInfo, 0);
        }

        tonlWriter.Flush();
    }

    /// <summary>
    /// Serializes a value to a TONL byte array using source-generated type info.
    /// </summary>
    public static byte[] SerializeToBytes<T>(T value, TonlTypeInfo<T> typeInfo, TonlOptions? options = null)
    {
        using var bufferWriter = new TonlBufferWriter(256);
        Serialize(bufferWriter, value, typeInfo, options);
        return bufferWriter.ToArray();
    }

    /// <summary>
    /// Serializes a value to a TONL string using source-generated type info.
    /// </summary>
    public static string SerializeToString<T>(T value, TonlTypeInfo<T> typeInfo, TonlOptions? options = null)
    {
        using var bufferWriter = new TonlBufferWriter(256);
        Serialize(bufferWriter, value, typeInfo, options);
        return bufferWriter.ToString();
    }

    /// <summary>
    /// Deserializes TONL data using source-generated type info.
    /// </summary>
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Data, TonlTypeInfo<T> typeInfo, TonlOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(typeInfo);

        var reader = new TonlReader(utf8Data, options ?? typeInfo.OriginatingContext?.Options);
        reader.ParseHeaders();

        // Use fast-path deserialize if available
        if (typeInfo.Deserialize is not null)
        {
            return typeInfo.Deserialize(ref reader);
        }

        // Fall back to dictionary-based deserialization
        var result = DeserializeDocument(ref reader, typeof(T));
        if (result is T typedResult)
        {
            return typedResult;
        }

        return default;
    }

    /// <summary>
    /// Deserializes a TONL string using source-generated type info.
    /// </summary>
    public static T? Deserialize<T>(string tonl, TonlTypeInfo<T> typeInfo, TonlOptions? options = null)
    {
        var bytes = Encoding.UTF8.GetBytes(tonl);
        return Deserialize<T>(bytes, typeInfo, options);
    }

    private static string[] GetPropertyNames<T>(TonlTypeInfo<T> typeInfo)
    {
        if (typeInfo.Properties is null || typeInfo.Properties.Count == 0)
        {
            return [];
        }

        var names = new string[typeInfo.Properties.Count];
        for (int i = 0; i < typeInfo.Properties.Count; i++)
        {
            names[i] = typeInfo.Properties[i].Name;
        }
        return names;
    }

    private static void SerializeWithMetadata<T>(
        ref TonlWriter writer,
        T value,
        TonlTypeInfo<T> typeInfo,
        int indent)
    {
        if (value is null)
        {
            writer.WriteIndent(indent);
            writer.WriteKeyNull("root");
            writer.WriteNewLine();
            return;
        }

        if (typeInfo.Properties is null || typeInfo.Properties.Count == 0)
        {
            return;
        }

        var columns = GetPropertyNames(typeInfo);
        writer.WriteIndent(indent);
        writer.WriteObjectHeader("root", columns);
        writer.WriteNewLine();

        foreach (var prop in typeInfo.Properties)
        {
            var propValue = prop.GetValue(value);
            writer.WriteIndent(indent + 1);

            if (propValue is null)
            {
                writer.WriteKeyNull(prop.Name);
            }
            else if (propValue is bool b)
            {
                writer.WriteKeyBoolean(prop.Name, b);
            }
            else if (propValue is int i)
            {
                writer.WriteKeyInt32(prop.Name, i);
            }
            else if (propValue is long l)
            {
                writer.WriteKeyInt64(prop.Name, l);
            }
            else if (propValue is double d)
            {
                writer.WriteKeyDouble(prop.Name, d);
            }
            else if (propValue is float f)
            {
                writer.WriteKeyDouble(prop.Name, f);
            }
            else if (propValue is string s)
            {
                writer.WriteKeyValue(prop.Name, s);
            }
            else
            {
                writer.WriteKeyValue(prop.Name, propValue.ToString() ?? "");
            }

            writer.WriteNewLine();
        }
    }

    #endregion

    #region Reflection-Based Methods

    private const string ReflectionSerializationMessage =
        "Reflection-based serialization is not compatible with trimming or AOT. Use the TonlTypeInfo<T> overload with source generation instead.";

    private const string ReflectionDeserializationMessage =
        "Reflection-based deserialization is not compatible with trimming or AOT. Use the TonlTypeInfo<T> overload with source generation instead.";

    /// <summary>
    /// Serializes an object to TONL format and writes to the specified buffer writer.
    /// </summary>
    /// <remarks>
    /// This method uses reflection and is not AOT-compatible. For AOT scenarios,
    /// use the overload that accepts <see cref="TonlTypeInfo{T}"/>.
    /// </remarks>
    [RequiresUnreferencedCode(ReflectionSerializationMessage)]
    [RequiresDynamicCode(ReflectionSerializationMessage)]
    public static void Serialize<T>(IBufferWriter<byte> writer, T value, TonlOptions? options = null)
    {
        options ??= TonlOptions.Default;
        options.Validate();

        var tonlWriter = new TonlWriter(writer, options);
        tonlWriter.WriteHeader();

        var seen = new HashSet<object>(ReferenceEqualityComparer.Instance);
        SerializeValue(ref tonlWriter, value, "root", 0, options, seen);

        tonlWriter.Flush();
    }

    /// <summary>
    /// Serializes an object to a TONL byte array.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionSerializationMessage)]
    [RequiresDynamicCode(ReflectionSerializationMessage)]
    public static byte[] SerializeToBytes<T>(T value, TonlOptions? options = null)
    {
        using var bufferWriter = new TonlBufferWriter(256);
        Serialize(bufferWriter, value, options);
        return bufferWriter.ToArray();
    }

    /// <summary>
    /// Serializes an object to a TONL string.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionSerializationMessage)]
    [RequiresDynamicCode(ReflectionSerializationMessage)]
    public static string SerializeToString<T>(T value, TonlOptions? options = null)
    {
        using var bufferWriter = new TonlBufferWriter(256);
        Serialize(bufferWriter, value, options);
        return bufferWriter.ToString();
    }

    /// <summary>
    /// Deserializes a TONL byte span to an object.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionDeserializationMessage)]
    [RequiresDynamicCode(ReflectionDeserializationMessage)]
    public static T? Deserialize<T>(ReadOnlySpan<byte> utf8Data, TonlOptions? options = null)
    {
        var reader = new TonlReader(utf8Data, options);
        reader.ParseHeaders();

        var result = DeserializeDocument(ref reader, typeof(T));

        if (result is T typedResult)
        {
            return typedResult;
        }

        return default;
    }

    /// <summary>
    /// Deserializes a TONL string to an object.
    /// </summary>
    [RequiresUnreferencedCode(ReflectionDeserializationMessage)]
    [RequiresDynamicCode(ReflectionDeserializationMessage)]
    public static T? Deserialize<T>(string tonl, TonlOptions? options = null)
    {
        var bytes = Encoding.UTF8.GetBytes(tonl);
        return Deserialize<T>(bytes, options);
    }

    /// <summary>
    /// Deserializes TONL data to a dictionary structure (untyped).
    /// </summary>
    [RequiresUnreferencedCode(ReflectionDeserializationMessage)]
    [RequiresDynamicCode(ReflectionDeserializationMessage)]
    public static Dictionary<string, object?>? DeserializeToDictionary(ReadOnlySpan<byte> utf8Data, TonlOptions? options = null)
    {
        return Deserialize<Dictionary<string, object?>>(utf8Data, options);
    }

    /// <summary>
    /// Deserializes TONL data to a dictionary structure (untyped).
    /// </summary>
    [RequiresUnreferencedCode(ReflectionDeserializationMessage)]
    [RequiresDynamicCode(ReflectionDeserializationMessage)]
    public static Dictionary<string, object?>? DeserializeToDictionary(string tonl, TonlOptions? options = null)
    {
        return Deserialize<Dictionary<string, object?>>(tonl, options);
    }

    private static void SerializeValue<T>(
        ref TonlWriter writer,
        T value,
        string key,
        int indent,
        TonlOptions options,
        HashSet<object> seen)
    {
        if (value is null)
        {
            writer.WriteIndent(indent);
            writer.WriteKeyNull(key);
            writer.WriteNewLine();
            return;
        }

        var type = value.GetType();

        // Check for circular reference
        if (!type.IsValueType && value is not string)
        {
            if (!seen.Add(value))
            {
                throw new TonlCircularReferenceException(key);
            }
        }

        try
        {
            if (IsPrimitiveType(type))
            {
                writer.WriteIndent(indent);
                WritePrimitiveKeyValue(ref writer, key, value);
                writer.WriteNewLine();
            }
            else if (value is IDictionary dict)
            {
                SerializeDictionary(ref writer, dict, key, indent, options, seen);
            }
            else if (value is IEnumerable enumerable and not string)
            {
                SerializeArray(ref writer, enumerable, key, indent, options, seen);
            }
            else
            {
                SerializeObject(ref writer, value, key, indent, options, seen);
            }
        }
        finally
        {
            if (!type.IsValueType && value is not string)
            {
                seen.Remove(value);
            }
        }
    }

    private static void SerializeObject(
        ref TonlWriter writer,
        object value,
        string key,
        int indent,
        TonlOptions options,
        HashSet<object> seen)
    {
        var type = value.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0) // Exclude indexers
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        // Get column names
        var columns = properties.Select(p => p.Name).ToArray();

        // Check if all property values are primitives (supports inline format)
        bool allPrimitive = properties.All(p =>
        {
            var val = p.GetValue(value);
            if (val is null) return true;
            var pType = val.GetType();
            return IsPrimitiveType(pType);
        });

        // Use inline format for nested objects with only primitive values
        // (indent > 0 means this is a nested object, not root)
        if (allPrimitive && indent > 0)
        {
            writer.WriteIndent(indent);
            writer.WriteObjectHeader(key, columns);
            writer.WriteByte((byte)' ');

            bool first = true;
            foreach (var prop in properties)
            {
                if (!first)
                {
                    writer.WriteDelimiter();
                }
                first = false;

                var propValue = prop.GetValue(value);
                WritePrimitiveValue(ref writer, propValue, options);
            }

            writer.WriteNewLine();
        }
        else
        {
            // Block format for root objects or objects with nested values
            writer.WriteIndent(indent);
            writer.WriteObjectHeader(key, columns);
            writer.WriteNewLine();

            foreach (var prop in properties)
            {
                var propValue = prop.GetValue(value);
                SerializeValue(ref writer, propValue, prop.Name, indent + 1, options, seen);
            }
        }
    }

    private static void SerializeDictionary(
        ref TonlWriter writer,
        IDictionary dict,
        string key,
        int indent,
        TonlOptions options,
        HashSet<object> seen)
    {
        var keys = dict.Keys.Cast<object>().Select(k => k?.ToString() ?? "")
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

        writer.WriteIndent(indent);
        writer.WriteObjectHeader(key, keys);
        writer.WriteNewLine();

        foreach (var k in keys)
        {
            var val = dict[k];
            SerializeValue(ref writer, val, k, indent + 1, options, seen);
        }
    }

    private static void SerializeArray(
        ref TonlWriter writer,
        IEnumerable enumerable,
        string key,
        int indent,
        TonlOptions options,
        HashSet<object> seen)
    {
        var items = enumerable.Cast<object?>().ToList();

        if (items.Count == 0)
        {
            writer.WriteIndent(indent);
            writer.WritePrimitiveArrayHeader(key, 0);
            writer.WriteNewLine();
            return;
        }

        // Check for uniform dictionary array first (structural equality)
        if (IsUniformDictionaryArray(items, out var dictColumns))
        {
            SerializeTabularDictionaryArray(ref writer, items, key, dictColumns, indent, options);
        }
        // Check if uniform object array (all objects with same properties)
        else if (IsUniformObjectArray(items, out var columns))
        {
            SerializeTabularArray(ref writer, items, key, columns, indent, options);
        }
        // Check if all primitives
        else if (items.All(item => item is null || IsPrimitiveType(item.GetType())))
        {
            SerializePrimitiveArray(ref writer, items, key, indent, options);
        }
        // Mixed array
        else
        {
            SerializeMixedArray(ref writer, items, key, indent, options, seen);
        }
    }

    private static void SerializeTabularArray(
        ref TonlWriter writer,
        List<object?> items,
        string key,
        string[] columns,
        int indent,
        TonlOptions options)
    {
        writer.WriteIndent(indent);
        writer.WriteArrayHeader(key, items.Count, columns);
        writer.WriteNewLine();

        foreach (var item in items)
        {
            if (item is null) continue;

            writer.WriteIndent(indent + 1);

            var type = item.GetType();
            var props = columns.Select(c => type.GetProperty(c)).ToArray();

            bool first = true;
            foreach (var prop in props)
            {
                if (!first)
                {
                    writer.WriteDelimiter();
                }
                first = false;

                var val = prop?.GetValue(item);
                WritePrimitiveValue(ref writer, val, options);
            }

            writer.WriteNewLine();
        }
    }

    private static void SerializeTabularDictionaryArray(
        ref TonlWriter writer,
        List<object?> items,
        string key,
        string[] columns,
        int indent,
        TonlOptions options)
    {
        writer.WriteIndent(indent);
        writer.WriteArrayHeader(key, items.Count, columns);
        writer.WriteNewLine();

        foreach (var item in items)
        {
            if (item is not IDictionary dict) continue;

            writer.WriteIndent(indent + 1);

            bool first = true;
            foreach (var col in columns)
            {
                if (!first)
                {
                    writer.WriteDelimiter();
                }
                first = false;

                var val = dict[col];
                WritePrimitiveValue(ref writer, val, options);
            }

            writer.WriteNewLine();
        }
    }

    private static void SerializePrimitiveArray(
        ref TonlWriter writer,
        List<object?> items,
        string key,
        int indent,
        TonlOptions options)
    {
        writer.WriteIndent(indent);
        writer.WritePrimitiveArrayHeader(key, items.Count);
        writer.WriteByte((byte)' ');

        bool first = true;
        foreach (var item in items)
        {
            if (!first)
            {
                writer.WriteDelimiter();
            }
            first = false;

            WritePrimitiveValue(ref writer, item, options);
        }

        writer.WriteNewLine();
    }

    private static void SerializeMixedArray(
        ref TonlWriter writer,
        List<object?> items,
        string key,
        int indent,
        TonlOptions options,
        HashSet<object> seen)
    {
        writer.WriteIndent(indent);
        writer.WritePrimitiveArrayHeader(key, items.Count);
        writer.WriteNewLine();

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            SerializeIndexedValue(ref writer, item, i, indent + 1, options, seen);
        }
    }

    private static void SerializeIndexedValue(
        ref TonlWriter writer,
        object? value,
        int index,
        int indent,
        TonlOptions options,
        HashSet<object> seen)
    {
        writer.WriteIndent(indent);

        if (value is null)
        {
            writer.WriteIndexedArrayHeader(index);
            writer.WriteByte((byte)' ');
            writer.WriteNull();
            writer.WriteNewLine();
            return;
        }

        var type = value.GetType();

        // Check for circular reference
        if (!type.IsValueType && value is not string)
        {
            if (!seen.Add(value))
            {
                throw new TonlCircularReferenceException($"[{index}]");
            }
        }

        try
        {
            if (IsPrimitiveType(type))
            {
                writer.WriteIndexedArrayHeader(index);
                writer.WriteByte((byte)' ');
                WritePrimitiveValue(ref writer, value, options);
                writer.WriteNewLine();
            }
            else if (value is IDictionary dict)
            {
                SerializeIndexedDictionary(ref writer, dict, index, indent, options, seen);
            }
            else if (value is IEnumerable enumerable and not string)
            {
                // Nested array within mixed array - use indexed format
                writer.WriteIndexedArrayHeader(index);
                writer.WriteNewLine();
                var nestedItems = enumerable.Cast<object?>().ToList();
                for (int i = 0; i < nestedItems.Count; i++)
                {
                    SerializeIndexedValue(ref writer, nestedItems[i], i, indent + 1, options, seen);
                }
            }
            else
            {
                SerializeIndexedObject(ref writer, value, index, indent, options, seen);
            }
        }
        finally
        {
            if (!type.IsValueType && value is not string)
            {
                seen.Remove(value);
            }
        }
    }

    private static void SerializeIndexedObject(
        ref TonlWriter writer,
        object value,
        int index,
        int indent,
        TonlOptions options,
        HashSet<object> seen)
    {
        var type = value.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.GetIndexParameters().Length == 0) // Exclude indexers
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        var columns = properties.Select(p => p.Name).ToArray();

        writer.WriteIndexedObjectHeader(index, columns);
        writer.WriteNewLine();

        foreach (var prop in properties)
        {
            var propValue = prop.GetValue(value);
            SerializeValue(ref writer, propValue, prop.Name, indent + 1, options, seen);
        }
    }

    private static void SerializeIndexedDictionary(
        ref TonlWriter writer,
        IDictionary dict,
        int index,
        int indent,
        TonlOptions options,
        HashSet<object> seen)
    {
        var keys = dict.Keys.Cast<object>().Select(k => k?.ToString() ?? "")
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

        writer.WriteIndexedObjectHeader(index, keys);
        writer.WriteNewLine();

        foreach (var k in keys)
        {
            var val = dict[k];
            SerializeValue(ref writer, val, k, indent + 1, options, seen);
        }
    }

    private static void WritePrimitiveKeyValue(ref TonlWriter writer, string key, object? value)
    {
        if (value is null)
        {
            writer.WriteKeyNull(key);
        }
        else if (value is bool b)
        {
            writer.WriteKeyBoolean(key, b);
        }
        else if (value is int i)
        {
            writer.WriteKeyInt32(key, i);
        }
        else if (value is long l)
        {
            writer.WriteKeyInt64(key, l);
        }
        else if (value is double d)
        {
            writer.WriteKeyDouble(key, d);
        }
        else if (value is float f)
        {
            writer.WriteKeyFloat(key, f);
        }
        else if (value is decimal dec)
        {
            writer.WriteKeyDecimal(key, dec);
        }
        else if (value is string s)
        {
            writer.WriteKeyValue(key, s);
        }
        else if (value is DateTime dt)
        {
            // ISO 8601 format for round-trip fidelity
            writer.WriteKeyValue(key, dt.ToString("O"));
        }
        else if (value is DateTimeOffset dto)
        {
            // ISO 8601 format for round-trip fidelity
            writer.WriteKeyValue(key, dto.ToString("O"));
        }
        else if (value is TimeSpan ts)
        {
            // Invariant culture format
            writer.WriteKeyValue(key, ts.ToString());
        }
        else
        {
            writer.WriteKeyValue(key, value.ToString() ?? "");
        }
    }

    private static void WritePrimitiveValue(ref TonlWriter writer, object? value, TonlOptions options)
    {
        if (value is null)
        {
            writer.WriteNull();
        }
        else if (value is bool b)
        {
            writer.WriteBoolean(b);
        }
        else if (value is int i)
        {
            writer.WriteInt32(i);
        }
        else if (value is long l)
        {
            writer.WriteInt64(l);
        }
        else if (value is double d)
        {
            writer.WriteDouble(d);
        }
        else if (value is float f)
        {
            writer.WriteFloat(f);
        }
        else if (value is decimal dec)
        {
            writer.WriteDecimal(dec);
        }
        else if (value is string s)
        {
            writer.WriteStringValue(s);
        }
        else if (value is DateTime dt)
        {
            // ISO 8601 format for round-trip fidelity
            writer.WriteStringValue(dt.ToString("O"));
        }
        else if (value is DateTimeOffset dto)
        {
            // ISO 8601 format for round-trip fidelity
            writer.WriteStringValue(dto.ToString("O"));
        }
        else if (value is TimeSpan ts)
        {
            // Invariant culture format
            writer.WriteStringValue(ts.ToString());
        }
        else
        {
            writer.WriteStringValue(value.ToString() ?? "");
        }
    }

    private static bool IsPrimitiveType(Type type)
    {
        return type.IsPrimitive ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(DateTimeOffset) ||
               type == typeof(TimeSpan) ||
               type == typeof(Guid);
    }

    private static bool IsUniformObjectArray(List<object?> items, out string[] columns)
    {
        columns = Array.Empty<string>();

        // Filter out nulls
        var nonNullItems = items.Where(i => i is not null).ToList();
        if (nonNullItems.Count == 0)
        {
            return false;
        }

        // All items must be objects (not primitives)
        var firstType = nonNullItems[0]!.GetType();
        if (IsPrimitiveType(firstType))
        {
            return false;
        }

        // All items must be the same type
        if (!nonNullItems.All(i => i!.GetType() == firstType))
        {
            return false;
        }

        // All properties must be primitives
        var props = firstType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        if (props.Any(p => !IsPrimitiveType(p.PropertyType) && p.PropertyType != typeof(string)))
        {
            return false;
        }

        columns = props.Select(p => p.Name).ToArray();
        return true;
    }

    private static bool IsUniformDictionaryArray(List<object?> items, out string[] columns)
    {
        columns = Array.Empty<string>();

        // Filter out nulls
        var nonNullItems = items.Where(i => i is not null).ToList();
        if (nonNullItems.Count == 0) return false;

        // All items must be dictionaries
        if (nonNullItems[0] is not IDictionary firstDict) return false;
        if (!nonNullItems.All(i => i is IDictionary)) return false;

        // Get keys from first dictionary (sorted for consistent ordering)
        var firstKeys = firstDict.Keys.Cast<object>()
            .Select(k => k?.ToString() ?? "")
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToArray();

        if (firstKeys.Length == 0) return false;

        // All dictionaries must have the same keys (structural equality)
        foreach (var item in nonNullItems.Skip(1))
        {
            var dict = (IDictionary)item!;
            var keys = dict.Keys.Cast<object>()
                .Select(k => k?.ToString() ?? "")
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToArray();

            if (!keys.SequenceEqual(firstKeys)) return false;
        }

        // All values must be primitives
        foreach (var item in nonNullItems)
        {
            var dict = (IDictionary)item!;
            foreach (var key in firstKeys)
            {
                var val = dict[key];
                if (val is not null && !IsPrimitiveType(val.GetType()) && val is not string)
                    return false;
            }
        }

        columns = firstKeys;
        return true;
    }

    private static object? DeserializeDocument(ref TonlReader reader, Type targetType)
    {
        var result = new Dictionary<string, object?>();
        var currentIndent = 0;
        var stack = new Stack<(Dictionary<string, object?> dict, int indent)>();
        stack.Push((result, -1));

        // Pre-allocate field buffer to avoid stackalloc inside loop (CA2014)
        Span<Range> fieldsBuffer = stackalloc Range[128];

        while (reader.ReadLine(out var line))
        {
            var trimmed = line;
            while (trimmed.Length > 0 && (trimmed[0] == (byte)' ' || trimmed[0] == (byte)'\t'))
            {
                trimmed = trimmed.Slice(1);
            }

            if (trimmed.IsEmpty)
            {
                continue;
            }

            // Skip comments
            if (trimmed[0] == (byte)'#')
            {
                continue;
            }

            currentIndent = TonlReader.GetIndentLevel(line);

            // Pop stack until we're at the right level
            while (stack.Count > 1 && stack.Peek().indent >= currentIndent)
            {
                stack.Pop();
            }

            var currentDict = stack.Peek().dict;

            // Try to parse as array header FIRST (more specific - has [N])
            if (reader.TryParseArrayHeader(trimmed, out var arrKey, out var arrCount, out var arrColumns))
            {
                if (arrColumns.Length > 0)
                {
                    // Tabular array - read the next arrCount lines as rows
                    var rows = new List<Dictionary<string, object?>>();
                    var fields = fieldsBuffer.Slice(0, Math.Min(arrColumns.Length + 1, fieldsBuffer.Length));
                    for (int i = 0; i < arrCount && reader.ReadLine(out var rowLine); i++)
                    {
                        var rowTrimmed = rowLine;
                        while (rowTrimmed.Length > 0 && (rowTrimmed[0] == (byte)' ' || rowTrimmed[0] == (byte)'\t'))
                        {
                            rowTrimmed = rowTrimmed.Slice(1);
                        }

                        if (rowTrimmed.IsEmpty) continue;

                        int fieldCount = reader.ParseFields(rowTrimmed, fields);

                        var row = new Dictionary<string, object?>();
                        for (int j = 0; j < Math.Min(fieldCount, arrColumns.Length); j++)
                        {
                            var fieldSpan = rowTrimmed[fields[j]];
                            row[arrColumns[j]] = reader.ParsePrimitiveValue(fieldSpan);
                        }
                        rows.Add(row);
                    }
                    currentDict[arrKey] = rows;
                }
                else
                {
                    // Primitive array on same line or mixed array
                    // Check if there's inline data after the :
                    int colonIdx = trimmed.LastIndexOf((byte)':');
                    if (colonIdx >= 0 && colonIdx < trimmed.Length - 1)
                    {
                        var valuesPart = trimmed.Slice(colonIdx + 1);
                        while (valuesPart.Length > 0 && valuesPart[0] == (byte)' ')
                        {
                            valuesPart = valuesPart.Slice(1);
                        }

                        if (!valuesPart.IsEmpty)
                        {
                            int maxFields = Math.Max(arrCount, 16);
                            var fields = maxFields <= fieldsBuffer.Length
                                ? fieldsBuffer.Slice(0, maxFields)
                                : new Range[maxFields];
                            int fieldCount = reader.ParseFields(valuesPart, fields);

                            var items = new List<object?>();
                            for (int i = 0; i < fieldCount; i++)
                            {
                                var fieldSpan = valuesPart[fields[i]];
                                items.Add(reader.ParsePrimitiveValue(fieldSpan));
                            }
                            currentDict[arrKey] = items;
                            continue;
                        }
                    }

                    // Mixed array - read indexed elements
                    var mixedItems = new List<object?>(arrCount);
                    var arrayIndent = currentIndent;

                    while (reader.TryPeekLine(out var nextLine))
                    {
                        var nextTrimmed = nextLine;
                        while (nextTrimmed.Length > 0 && (nextTrimmed[0] == (byte)' ' || nextTrimmed[0] == (byte)'\t'))
                        {
                            nextTrimmed = nextTrimmed.Slice(1);
                        }

                        // Check indentation - must be deeper than array header
                        int nextIndent = TonlReader.GetIndentLevel(nextLine);
                        if (nextIndent <= arrayIndent || nextTrimmed.IsEmpty)
                        {
                            break;
                        }

                        // Try to parse as indexed element
                        if (reader.TryParseIndexedHeader(nextTrimmed, out int elemIndex, out var elemColumns, out bool hasInlineValue))
                        {
                            reader.ReadLine(out _); // Consume the line

                            // Ensure list is big enough
                            while (mixedItems.Count <= elemIndex)
                            {
                                mixedItems.Add(null);
                            }

                            if (elemColumns.Length > 0)
                            {
                                // Indexed object element: [N]{col1,col2}:
                                var elemDict = new Dictionary<string, object?>();
                                mixedItems[elemIndex] = elemDict;
                                stack.Push((elemDict, nextIndent));
                            }
                            else if (hasInlineValue)
                            {
                                // Indexed primitive with inline value: [N]: value
                                int valueColonIdx = nextTrimmed.IndexOf((byte)':');
                                if (valueColonIdx >= 0)
                                {
                                    var valuePart = nextTrimmed.Slice(valueColonIdx + 1);
                                    mixedItems[elemIndex] = reader.ParsePrimitiveValue(valuePart);
                                }
                            }
                            else
                            {
                                // Nested structure follows
                                var elemDict = new Dictionary<string, object?>();
                                mixedItems[elemIndex] = elemDict;
                                stack.Push((elemDict, nextIndent));
                            }
                        }
                        else
                        {
                            break;
                        }
                    }

                    currentDict[arrKey] = mixedItems;
                }
                continue;
            }

            // Try to parse as inline object: key{col1,col2}: val1, val2
            if (reader.TryParseInlineObject(trimmed, out var inlineKey, out var inlineColumns, out var inlineValuesSpan))
            {
                var inlineDict = new Dictionary<string, object?>();
                var inlineFields = fieldsBuffer.Slice(0, Math.Min(inlineColumns.Length + 1, fieldsBuffer.Length));
                int inlineFieldCount = reader.ParseFields(inlineValuesSpan, inlineFields);

                for (int j = 0; j < Math.Min(inlineFieldCount, inlineColumns.Length); j++)
                {
                    var fieldSpan = inlineValuesSpan[inlineFields[j]];
                    inlineDict[inlineColumns[j]] = reader.ParsePrimitiveValue(fieldSpan);
                }
                currentDict[inlineKey] = inlineDict;
                continue;
            }

            // Try to parse as object header (block format)
            if (reader.TryParseObjectHeader(trimmed, out var objKey, out var objColumns))
            {
                var newDict = new Dictionary<string, object?>();
                currentDict[objKey] = newDict;
                stack.Push((newDict, currentIndent));
                continue;
            }

            // Try to parse as key-value
            if (reader.TryParseKeyValue(trimmed, out var kvKey, out var kvValue))
            {
                // Check for multiline string (triple-quoted that spans multiple lines)
                int colonIdx = FindKeyEndForKv(trimmed);
                if (colonIdx >= 0)
                {
                    var valueSpan = trimmed.Slice(colonIdx + 1);
                    if (TonlReader.StartsWithIncompleteTripleQuote(valueSpan))
                    {
                        // Read additional lines until we find closing """
                        var sb = new StringBuilder();
                        var afterOpening = TrimWhitespace(valueSpan).Slice(3); // Skip opening """
                        sb.Append(Encoding.UTF8.GetString(afterOpening));

                        while (reader.ReadLine(out var nextLine))
                        {
                            // Check if this line contains closing """
                            int closeIdx = FindClosingTripleQuote(nextLine);
                            if (closeIdx >= 0)
                            {
                                // Append content before closing quotes
                                sb.Append('\n');
                                sb.Append(Encoding.UTF8.GetString(nextLine.Slice(0, closeIdx)));
                                break;
                            }
                            else
                            {
                                // Append entire line
                                sb.Append('\n');
                                sb.Append(Encoding.UTF8.GetString(nextLine));
                            }
                        }

                        kvValue = sb.ToString();
                    }
                }
                currentDict[kvKey] = kvValue;
            }
        }

        // Unwrap root if present
        if (result.Count == 1 && result.ContainsKey("root"))
        {
            var rootValue = result["root"];
            if (targetType == typeof(Dictionary<string, object?>))
            {
                return rootValue as Dictionary<string, object?> ?? result;
            }
            return ConvertToType(rootValue, targetType);
        }

        if (targetType == typeof(Dictionary<string, object?>))
        {
            return result;
        }

        return ConvertToType(result, targetType);
    }

    private static object? ConvertToType(object? value, Type targetType)
    {
        if (value is null)
        {
            return null;
        }

        if (targetType.IsAssignableFrom(value.GetType()))
        {
            return value;
        }

        if (value is Dictionary<string, object?> dict)
        {
            // Try parameterless constructor first
            var parameterlessCtor = targetType.GetConstructor(Type.EmptyTypes);
            if (parameterlessCtor != null)
            {
                var instance = Activator.CreateInstance(targetType);
                if (instance is null) return null;

                foreach (var kvp in dict)
                {
                    var prop = targetType.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (prop?.CanWrite == true)
                    {
                        var convertedValue = ConvertToType(kvp.Value, prop.PropertyType);
                        prop.SetValue(instance, convertedValue);
                    }
                }

                return instance;
            }

            // Try to find a constructor with parameters matching the dictionary keys (for records)
            var constructors = targetType.GetConstructors();
            foreach (var ctor in constructors.OrderByDescending(c => c.GetParameters().Length))
            {
                var parameters = ctor.GetParameters();
                var args = new object?[parameters.Length];
                bool canUseConstructor = true;

                for (int i = 0; i < parameters.Length; i++)
                {
                    var param = parameters[i];
                    // Try to find a dictionary key matching the parameter name (case-insensitive)
                    var kvp = dict.FirstOrDefault(k =>
                        string.Equals(k.Key, param.Name, StringComparison.OrdinalIgnoreCase));

                    if (kvp.Key != null)
                    {
                        args[i] = ConvertToType(kvp.Value, param.ParameterType);
                    }
                    else if (param.HasDefaultValue)
                    {
                        args[i] = param.DefaultValue;
                    }
                    else
                    {
                        canUseConstructor = false;
                        break;
                    }
                }

                if (canUseConstructor)
                {
                    return ctor.Invoke(args);
                }
            }

            return null;
        }

        if (value is IList list && targetType.IsGenericType)
        {
            var elementType = targetType.GetGenericArguments()[0];
            var typedList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

            foreach (var item in list)
            {
                typedList.Add(ConvertToType(item, elementType));
            }

            if (targetType.IsArray)
            {
                var array = Array.CreateInstance(elementType, typedList.Count);
                typedList.CopyTo(array, 0);
                return array;
            }

            return typedList;
        }

        // Try to convert primitive types
        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch
        {
            return value;
        }
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
    }

    private static ReadOnlySpan<byte> TrimWhitespace(ReadOnlySpan<byte> span)
    {
        int start = 0;
        while (start < span.Length && (span[start] == ' ' || span[start] == '\t'))
        {
            start++;
        }
        int end = span.Length;
        while (end > start && (span[end - 1] == ' ' || span[end - 1] == '\t'))
        {
            end--;
        }
        return span.Slice(start, end - start);
    }

    /// <summary>
    /// Finds the colon position after a key (accounting for quoted keys).
    /// </summary>
    private static int FindKeyEndForKv(ReadOnlySpan<byte> line)
    {
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            byte c = line[i];
            if (c == (byte)'"')
            {
                // Handle escaped quotes
                if (i + 1 < line.Length && line[i + 1] == (byte)'"')
                {
                    i++; // Skip escaped quote
                    continue;
                }
                inQuotes = !inQuotes;
            }
            else if (c == (byte)':' && !inQuotes)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Finds the position of closing triple quotes (""") in a line.
    /// </summary>
    private static int FindClosingTripleQuote(ReadOnlySpan<byte> line)
    {
        for (int i = 0; i <= line.Length - 3; i++)
        {
            if (line[i] == (byte)'"' && line[i + 1] == (byte)'"' && line[i + 2] == (byte)'"')
            {
                return i;
            }
        }
        return -1;
    }

    #endregion
}
