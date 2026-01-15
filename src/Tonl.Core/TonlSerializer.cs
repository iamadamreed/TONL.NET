using System.Buffers;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Tonl;

/// <summary>
/// Provides methods for serializing and deserializing objects to/from TONL format.
/// </summary>
public static class TonlSerializer
{
    /// <summary>
    /// Serializes an object to TONL format and writes to the specified buffer writer.
    /// </summary>
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
    public static byte[] SerializeToBytes<T>(T value, TonlOptions? options = null)
    {
        using var bufferWriter = new TonlBufferWriter(256);
        Serialize(bufferWriter, value, options);
        return bufferWriter.ToArray();
    }

    /// <summary>
    /// Serializes an object to a TONL string.
    /// </summary>
    public static string SerializeToString<T>(T value, TonlOptions? options = null)
    {
        using var bufferWriter = new TonlBufferWriter(256);
        Serialize(bufferWriter, value, options);
        return bufferWriter.ToString();
    }

    /// <summary>
    /// Deserializes a TONL byte span to an object.
    /// </summary>
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
    public static T? Deserialize<T>(string tonl, TonlOptions? options = null)
    {
        var bytes = Encoding.UTF8.GetBytes(tonl);
        return Deserialize<T>(bytes, options);
    }

    /// <summary>
    /// Deserializes TONL data to a dictionary structure (untyped).
    /// </summary>
    public static Dictionary<string, object?>? DeserializeToDictionary(ReadOnlySpan<byte> utf8Data, TonlOptions? options = null)
    {
        return Deserialize<Dictionary<string, object?>>(utf8Data, options);
    }

    /// <summary>
    /// Deserializes TONL data to a dictionary structure (untyped).
    /// </summary>
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
            .Where(p => p.CanRead)
            .OrderBy(p => p.Name, StringComparer.Ordinal)
            .ToArray();

        // Get column names
        var columns = properties.Select(p => p.Name).ToArray();

        // Check if we can use single-line format
        bool hasNested = properties.Any(p =>
        {
            var val = p.GetValue(value);
            if (val is null) return false;
            var pType = val.GetType();
            return !IsPrimitiveType(pType) && val is not string;
        });

        writer.WriteIndent(indent);
        writer.WriteObjectHeader(key, columns);
        writer.WriteNewLine();

        foreach (var prop in properties)
        {
            var propValue = prop.GetValue(value);
            SerializeValue(ref writer, propValue, prop.Name, indent + 1, options, seen);
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

        // Check if uniform object array (all objects with same properties)
        if (IsUniformObjectArray(items, out var columns))
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
            SerializeValue(ref writer, item, $"[{i}]", indent + 1, options, seen);
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
            writer.WriteKeyDouble(key, f);
        }
        else if (value is decimal dec)
        {
            writer.WriteKeyDouble(key, (double)dec);
        }
        else if (value is string s)
        {
            writer.WriteKeyValue(key, s);
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
            writer.WriteDouble(f);
        }
        else if (value is decimal dec)
        {
            writer.WriteDouble((double)dec);
        }
        else if (value is string s)
        {
            writer.WriteStringValue(s);
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

            // Try to parse as object header
            if (reader.TryParseObjectHeader(trimmed, out var objKey, out var objColumns))
            {
                var newDict = new Dictionary<string, object?>();
                currentDict[objKey] = newDict;
                stack.Push((newDict, currentIndent));
                continue;
            }

            // Try to parse as array header
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

                    // Mixed array - create placeholder
                    currentDict[arrKey] = new List<object?>();
                }
                continue;
            }

            // Try to parse as key-value
            if (reader.TryParseKeyValue(trimmed, out var kvKey, out var kvValue))
            {
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

}
