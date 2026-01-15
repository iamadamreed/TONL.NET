using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Tonl;

/// <summary>
/// A high-performance, low-allocation reader for TONL format.
/// </summary>
public ref partial struct TonlReader
{
    private ReadOnlySpan<byte> _buffer;
    private int _position;
    private int _lineNumber;
    private char _delimiter;
    private string _version;

    /// <summary>
    /// Gets the current token type.
    /// </summary>
    public TonlTokenType TokenType { get; private set; }

    /// <summary>
    /// Gets the current byte position in the buffer.
    /// </summary>
    public int Position => _position;

    /// <summary>
    /// Gets the current line number (1-based).
    /// </summary>
    public int LineNumber => _lineNumber;

    /// <summary>
    /// Gets the detected or configured delimiter.
    /// </summary>
    public char Delimiter => _delimiter;

    /// <summary>
    /// Gets the TONL version from the header.
    /// </summary>
    public string Version => _version;

    /// <summary>
    /// Gets whether there is more data to read.
    /// </summary>
    public bool HasMoreData => _position < _buffer.Length;

    /// <summary>
    /// Creates a new TONL reader.
    /// </summary>
    public TonlReader(ReadOnlySpan<byte> utf8Data, TonlOptions? options = null)
    {
        _buffer = utf8Data;
        _position = 0;
        _lineNumber = 1;
        _delimiter = options?.Delimiter ?? ',';
        _version = options?.Version ?? "1.0";
        TokenType = TonlTokenType.None;
    }

    /// <summary>
    /// Parses header directives (#version, #delimiter) from the beginning of the document.
    /// </summary>
    public void ParseHeaders()
    {
        bool hasDelimiterHeader = false;

        while (TryPeekLine(out var line))
        {
            var trimmed = TrimWhitespace(line);

            if (trimmed.IsEmpty || !trimmed[0].Equals((byte)'#'))
            {
                break;
            }

            ReadLine(out _);

            if (StartsWith(trimmed, "#version "u8))
            {
                TokenType = TonlTokenType.VersionHeader;
                var versionSpan = trimmed.Slice(9);
                _version = Encoding.UTF8.GetString(TrimWhitespace(versionSpan));
            }
            else if (StartsWith(trimmed, "#delimiter "u8))
            {
                TokenType = TonlTokenType.DelimiterHeader;
                var delimSpan = TrimWhitespace(trimmed.Slice(11));
                _delimiter = ParseDelimiter(delimSpan);
                hasDelimiterHeader = true;
            }
        }

        // Auto-detect delimiter if not specified in header
        if (!hasDelimiterHeader)
        {
            AutoDetectDelimiter();
        }
    }

    /// <summary>
    /// Auto-detects the delimiter from the first data line if not specified in header.
    /// Checks for common delimiters: tab, pipe, semicolon, comma (default).
    /// </summary>
    private void AutoDetectDelimiter()
    {
        // Save position to restore after detection
        int savedPosition = _position;
        int savedLineNumber = _lineNumber;

        // Skip to find first tabular data line (after a block header)
        while (TryPeekLine(out var line))
        {
            var trimmed = TrimWhitespace(line);

            // Skip empty lines and comments
            if (trimmed.IsEmpty || trimmed[0] == (byte)'#' || trimmed[0] == (byte)'@')
            {
                ReadLine(out _);
                continue;
            }

            // Look for array header with columns: key[N]{col1,col2}:
            if (TryParseArrayHeader(trimmed, out _, out var count, out var columns) && columns.Length > 1)
            {
                ReadLine(out _); // Consume header

                // Read first data row
                if (count > 0 && TryPeekLine(out var dataLine))
                {
                    var dataTrimmed = TrimWhitespace(dataLine);
                    if (!dataTrimmed.IsEmpty)
                    {
                        _delimiter = DetectDelimiterFromLine(dataTrimmed, columns.Length);
                    }
                }
                break;
            }

            ReadLine(out _);
        }

        // Restore position
        _position = savedPosition;
        _lineNumber = savedLineNumber;
    }

    /// <summary>
    /// Detects the most likely delimiter from a data line.
    /// </summary>
    private static char DetectDelimiterFromLine(ReadOnlySpan<byte> line, int expectedFields)
    {
        // Count occurrences of each potential delimiter (outside quotes)
        Span<int> counts = stackalloc int[4]; // tab, pipe, semicolon, comma
        ReadOnlySpan<byte> delimiters = stackalloc byte[] { (byte)'\t', (byte)'|', (byte)';', (byte)',' };

        bool inQuote = false;

        for (int i = 0; i < line.Length; i++)
        {
            byte c = line[i];

            if (inQuote)
            {
                if (c == (byte)'"')
                {
                    if (i + 1 < line.Length && line[i + 1] == (byte)'"')
                    {
                        i++; // Skip doubled quote
                    }
                    else
                    {
                        inQuote = false;
                    }
                }
            }
            else if (c == (byte)'"')
            {
                inQuote = true;
            }
            else
            {
                for (int d = 0; d < delimiters.Length; d++)
                {
                    if (c == delimiters[d])
                    {
                        counts[d]++;
                    }
                }
            }
        }

        // Return delimiter with count closest to expectedFields - 1
        int expectedCount = expectedFields - 1;
        int bestIdx = 3; // Default to comma
        int bestDiff = int.MaxValue;

        for (int d = 0; d < counts.Length; d++)
        {
            int diff = Math.Abs(counts[d] - expectedCount);
            if (diff < bestDiff && counts[d] > 0)
            {
                bestDiff = diff;
                bestIdx = d;
            }
        }

        return bestIdx switch
        {
            0 => '\t',
            1 => '|',
            2 => ';',
            _ => ','
        };
    }

    /// <summary>
    /// Reads the next line from the buffer.
    /// </summary>
    public bool ReadLine(out ReadOnlySpan<byte> line)
    {
        if (_position >= _buffer.Length)
        {
            line = default;
            TokenType = TonlTokenType.EndOfDocument;
            return false;
        }

        int start = _position;
        int newlineIdx = _buffer.Slice(_position).IndexOf((byte)'\n');

        if (newlineIdx < 0)
        {
            line = _buffer.Slice(start);
            _position = _buffer.Length;
        }
        else
        {
            // Handle \r\n line endings
            int lineEnd = start + newlineIdx;
            if (lineEnd > start && _buffer[lineEnd - 1] == (byte)'\r')
            {
                line = _buffer.Slice(start, newlineIdx - 1);
            }
            else
            {
                line = _buffer.Slice(start, newlineIdx);
            }
            _position = start + newlineIdx + 1;
        }

        _lineNumber++;
        return true;
    }

    /// <summary>
    /// Peeks at the next line without consuming it.
    /// </summary>
    public bool TryPeekLine(out ReadOnlySpan<byte> line)
    {
        if (_position >= _buffer.Length)
        {
            line = default;
            return false;
        }

        int newlineIdx = _buffer.Slice(_position).IndexOf((byte)'\n');

        if (newlineIdx < 0)
        {
            line = _buffer.Slice(_position);
        }
        else
        {
            int lineEnd = newlineIdx;
            if (lineEnd > 0 && _buffer[_position + lineEnd - 1] == (byte)'\r')
            {
                line = _buffer.Slice(_position, lineEnd - 1);
            }
            else
            {
                line = _buffer.Slice(_position, lineEnd);
            }
        }

        return true;
    }

    /// <summary>
    /// Parses a line and returns the fields separated by the delimiter.
    /// Handles quoted values correctly.
    /// </summary>
    public int ParseFields(ReadOnlySpan<byte> line, scoped Span<Range> fields)
    {
        int fieldCount = 0;
        int fieldStart = 0;
        bool inQuote = false;
        bool inTripleQuote = false;

        for (int i = 0; i < line.Length && fieldCount < fields.Length; i++)
        {
            byte c = line[i];

            if (inTripleQuote)
            {
                // Look for closing triple quotes
                if (c == (byte)'"' && i + 2 < line.Length &&
                    line[i + 1] == (byte)'"' && line[i + 2] == (byte)'"')
                {
                    inTripleQuote = false;
                    i += 2;
                }
                continue;
            }

            if (inQuote)
            {
                if (c == (byte)'"')
                {
                    // Check for doubled quote (escape)
                    if (i + 1 < line.Length && line[i + 1] == (byte)'"')
                    {
                        i++; // Skip the second quote
                    }
                    else
                    {
                        inQuote = false;
                    }
                }
                continue;
            }

            if (c == (byte)'"')
            {
                // Check for triple quote
                if (i + 2 < line.Length && line[i + 1] == (byte)'"' && line[i + 2] == (byte)'"')
                {
                    inTripleQuote = true;
                    i += 2;
                }
                else
                {
                    inQuote = true;
                }
                continue;
            }

            if (c == (byte)_delimiter)
            {
                fields[fieldCount++] = new Range(fieldStart, i);
                fieldStart = i + 1;

                // Skip whitespace after delimiter
                while (fieldStart < line.Length && line[fieldStart] == (byte)' ')
                {
                    fieldStart++;
                }
            }
        }

        // Add the last field
        if (fieldCount < fields.Length)
        {
            fields[fieldCount++] = new Range(fieldStart, line.Length);
        }

        return fieldCount;
    }

    /// <summary>
    /// Checks if a value starts with triple quotes but doesn't have closing triple quotes on the same line.
    /// </summary>
    public static bool StartsWithIncompleteTripleQuote(ReadOnlySpan<byte> value)
    {
        var trimmed = TrimWhitespace(value);
        if (trimmed.Length < 3)
        {
            return false;
        }

        // Check for opening triple quotes
        if (trimmed[0] != (byte)'"' || trimmed[1] != (byte)'"' || trimmed[2] != (byte)'"')
        {
            return false;
        }

        // Check if closing triple quotes exist on the same line
        var afterOpening = trimmed.Slice(3);
        if (afterOpening.Length >= 3)
        {
            // Search for closing triple quotes
            for (int i = 0; i <= afterOpening.Length - 3; i++)
            {
                if (afterOpening[i] == (byte)'"' && afterOpening[i + 1] == (byte)'"' && afterOpening[i + 2] == (byte)'"')
                {
                    return false; // Complete triple-quoted string on same line
                }
            }
        }

        return true; // Has opening but no closing triple quotes
    }

    /// <summary>
    /// Parses a primitive value from a byte span.
    /// </summary>
    public object? ParsePrimitiveValue(ReadOnlySpan<byte> value)
    {
        var trimmed = TrimWhitespace(value);

        if (trimmed.IsEmpty)
        {
            return null;
        }

        // Check for quoted strings
        if (trimmed[0] == (byte)'"')
        {
            return UnquoteString(trimmed);
        }

        // Check for null
        if (trimmed.SequenceEqual("null"u8))
        {
            return null;
        }

        // Check for booleans
        if (trimmed.SequenceEqual("true"u8))
        {
            return true;
        }
        if (trimmed.SequenceEqual("false"u8))
        {
            return false;
        }

        // Check for special numbers
        if (trimmed.SequenceEqual("Infinity"u8))
        {
            return double.PositiveInfinity;
        }
        if (trimmed.SequenceEqual("-Infinity"u8))
        {
            return double.NegativeInfinity;
        }
        if (trimmed.SequenceEqual("NaN"u8))
        {
            return double.NaN;
        }

        // Try to parse as number
        var str = Encoding.UTF8.GetString(trimmed);

        if (long.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longVal))
        {
            // Return int if it fits
            if (longVal >= int.MinValue && longVal <= int.MaxValue)
            {
                return (int)longVal;
            }
            return longVal;
        }

        if (double.TryParse(str, NumberStyles.Float | NumberStyles.AllowExponent, CultureInfo.InvariantCulture, out double doubleVal))
        {
            return doubleVal;
        }

        // Return as unquoted string
        return str;
    }

    /// <summary>
    /// Parses an object header: key{col1,col2,...}:
    /// Supports quoted keys like: "@type"{col1,col2}:
    /// </summary>
    public bool TryParseObjectHeader(ReadOnlySpan<byte> line, out string key, out string[] columns)
    {
        key = string.Empty;
        columns = Array.Empty<string>();

        var trimmed = TrimWhitespace(line);
        if (trimmed.IsEmpty)
        {
            return false;
        }

        // Must end with :
        if (trimmed[^1] != (byte)':')
        {
            return false;
        }

        // Find the opening brace (accounting for quoted key)
        int braceIdx = FindKeyEndSingle(trimmed, (byte)'{');
        if (braceIdx < 0)
        {
            return false;
        }

        // Find the closing brace
        int closeBraceIdx = trimmed.LastIndexOf((byte)'}');
        if (closeBraceIdx < braceIdx)
        {
            return false;
        }

        // Extract key (may be quoted)
        key = ParseKey(trimmed.Slice(0, braceIdx));

        // Extract columns (may contain quoted identifiers)
        var columnsSpan = trimmed.Slice(braceIdx + 1, closeBraceIdx - braceIdx - 1);
        columns = ParseColumns(columnsSpan);

        TokenType = TonlTokenType.ObjectHeader;
        return true;
    }

    /// <summary>
    /// Parses an array header: key[N]{col1,col2,...}: or key[N]:
    /// Supports quoted keys like: "@items"[3]{col1,col2}:
    /// </summary>
    public bool TryParseArrayHeader(ReadOnlySpan<byte> line, out string key, out int count, out string[] columns)
    {
        key = string.Empty;
        count = 0;
        columns = Array.Empty<string>();

        var trimmed = TrimWhitespace(line);
        if (trimmed.IsEmpty)
        {
            return false;
        }

        // Find the opening bracket (accounting for quoted key)
        int bracketIdx = FindKeyEndSingle(trimmed, (byte)'[');
        if (bracketIdx < 0)
        {
            return false;
        }

        // Find the closing bracket
        int closeBracketIdx = trimmed.Slice(bracketIdx).IndexOf((byte)']');
        if (closeBracketIdx < 0)
        {
            return false;
        }
        closeBracketIdx += bracketIdx; // Adjust to absolute position

        // Extract key (may be quoted)
        key = ParseKey(trimmed.Slice(0, bracketIdx));

        // Extract count
        var countSpan = trimmed.Slice(bracketIdx + 1, closeBracketIdx - bracketIdx - 1);
        var countStr = Encoding.UTF8.GetString(countSpan);
        if (!int.TryParse(countStr, out count))
        {
            return false;
        }

        // Check for columns (optional)
        var remaining = trimmed.Slice(closeBracketIdx + 1);
        int braceIdx = remaining.IndexOf((byte)'{');

        if (braceIdx >= 0)
        {
            int closeBraceIdx = remaining.LastIndexOf((byte)'}');
            if (closeBraceIdx > braceIdx)
            {
                var columnsSpan = remaining.Slice(braceIdx + 1, closeBraceIdx - braceIdx - 1);
                columns = ParseColumns(columnsSpan);
                TokenType = TonlTokenType.ArrayHeader;
            }
        }
        else
        {
            TokenType = TonlTokenType.PrimitiveArrayHeader;
        }

        return true;
    }

    /// <summary>
    /// Parses a key-value pair: key: value
    /// Supports quoted keys like: "@type": value
    /// </summary>
    public bool TryParseKeyValue(ReadOnlySpan<byte> line, out string key, out object? value)
    {
        key = string.Empty;
        value = null;

        var trimmed = TrimWhitespace(line);
        if (trimmed.IsEmpty)
        {
            return false;
        }

        // Find the colon, brace, or bracket (accounting for quoted key)
        int idx = FindKeyEndMultiple(trimmed, (byte)':', (byte)'{', (byte)'[', out byte foundChar);
        if (idx < 0)
        {
            return false;
        }

        // Make sure we found a colon (not { or [)
        if (foundChar != (byte)':')
        {
            return false; // This is a header, not a key-value pair
        }

        // Extract key (may be quoted)
        key = ParseKey(trimmed.Slice(0, idx));
        var valueSpan = trimmed.Slice(idx + 1);
        value = ParsePrimitiveValue(valueSpan);

        TokenType = TonlTokenType.KeyValue;
        return true;
    }

    /// <summary>
    /// Parses an indexed array element header: [N]: or [N]{col1,col2}:
    /// </summary>
    public bool TryParseIndexedHeader(ReadOnlySpan<byte> line, out int index, out string[] columns, out bool hasValue)
    {
        index = 0;
        columns = Array.Empty<string>();
        hasValue = false;

        var trimmed = TrimWhitespace(line);
        if (trimmed.IsEmpty || trimmed[0] != (byte)'[')
        {
            return false;
        }

        // Find the closing bracket
        int closeBracketIdx = trimmed.IndexOf((byte)']');
        if (closeBracketIdx < 1)
        {
            return false;
        }

        // Extract index
        var indexSpan = trimmed.Slice(1, closeBracketIdx - 1);
        var indexStr = Encoding.UTF8.GetString(indexSpan);
        if (!int.TryParse(indexStr, out index))
        {
            return false;
        }

        var remaining = trimmed.Slice(closeBracketIdx + 1);

        // Check for columns: [N]{col1,col2}:
        if (remaining.Length > 0 && remaining[0] == (byte)'{')
        {
            int closeBraceIdx = remaining.LastIndexOf((byte)'}');
            if (closeBraceIdx > 0)
            {
                var columnsSpan = remaining.Slice(1, closeBraceIdx - 1);
                columns = ParseColumns(columnsSpan);
                remaining = remaining.Slice(closeBraceIdx + 1);
            }
        }

        // Must have : after ] or }
        if (remaining.Length == 0 || remaining[0] != (byte)':')
        {
            return false;
        }

        // Check if there's inline value after :
        var valuesPart = remaining.Slice(1);
        valuesPart = TrimWhitespace(valuesPart);
        hasValue = !valuesPart.IsEmpty;

        TokenType = columns.Length > 0 ? TonlTokenType.ObjectHeader : TonlTokenType.KeyValue;
        return true;
    }

    /// <summary>
    /// Gets the indentation level of a line (number of leading spaces / indent size).
    /// </summary>
    public static int GetIndentLevel(ReadOnlySpan<byte> line, int indentSize = 2)
    {
        int spaces = 0;
        foreach (byte b in line)
        {
            if (b == (byte)' ')
            {
                spaces++;
            }
            else
            {
                break;
            }
        }
        return spaces / indentSize;
    }

    /// <summary>
    /// Parses a key that may be quoted.
    /// </summary>
    private string ParseKey(ReadOnlySpan<byte> keySpan)
    {
        var trimmed = TrimWhitespace(keySpan);
        if (trimmed.IsEmpty)
        {
            return string.Empty;
        }

        // Check if key is quoted
        if (trimmed[0] == (byte)'"')
        {
            return UnquoteString(trimmed);
        }

        return Encoding.UTF8.GetString(trimmed);
    }

    /// <summary>
    /// Parses columns that may contain quoted identifiers.
    /// Handles: col1,col2 or "col-1","col 2"
    /// </summary>
    private string[] ParseColumns(ReadOnlySpan<byte> columnsSpan)
    {
        if (columnsSpan.IsEmpty)
        {
            return Array.Empty<string>();
        }

        // Count columns first to pre-allocate array (avoid List<T> heap allocation)
        int columnCount = 1;
        bool inQuote = false;
        for (int i = 0; i < columnsSpan.Length; i++)
        {
            byte c = columnsSpan[i];
            if (inQuote)
            {
                if (c == (byte)'"' && !(i + 1 < columnsSpan.Length && columnsSpan[i + 1] == (byte)'"'))
                {
                    inQuote = false;
                }
                else if (c == (byte)'"')
                {
                    i++; // Skip doubled quote
                }
            }
            else if (c == (byte)'"')
            {
                inQuote = true;
            }
            else if (c == (byte)',')
            {
                columnCount++;
            }
        }

        var result = new string[columnCount];
        int resultIdx = 0;
        int start = 0;
        inQuote = false;

        for (int i = 0; i < columnsSpan.Length; i++)
        {
            byte c = columnsSpan[i];

            if (inQuote)
            {
                if (c == (byte)'"')
                {
                    if (i + 1 < columnsSpan.Length && columnsSpan[i + 1] == (byte)'"')
                    {
                        i++; // Skip doubled quote
                    }
                    else
                    {
                        inQuote = false;
                    }
                }
            }
            else if (c == (byte)'"')
            {
                inQuote = true;
            }
            else if (c == (byte)',')
            {
                var field = columnsSpan.Slice(start, i - start);
                result[resultIdx++] = ParseKey(field);
                start = i + 1;
            }
        }

        // Add the last field
        if (start <= columnsSpan.Length && resultIdx < result.Length)
        {
            var field = columnsSpan.Slice(start);
            result[resultIdx] = ParseKey(field);
        }

        return result;
    }

    /// <summary>
    /// Finds the position after the key, accounting for quoted keys.
    /// Returns the position of the first structural character after the key.
    /// </summary>
    private static int FindKeyEndSingle(ReadOnlySpan<byte> line, byte structuralChar)
    {
        bool inQuote = false;

        for (int i = 0; i < line.Length; i++)
        {
            byte c = line[i];

            if (inQuote)
            {
                if (c == (byte)'"')
                {
                    if (i + 1 < line.Length && line[i + 1] == (byte)'"')
                    {
                        i++; // Skip doubled quote
                    }
                    else
                    {
                        inQuote = false;
                    }
                }
            }
            else if (c == (byte)'"')
            {
                inQuote = true;
            }
            else if (c == structuralChar)
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Finds the position after the key, accounting for quoted keys.
    /// Returns the position of the first structural character ({, [, :) after the key.
    /// </summary>
    private static int FindKeyEndMultiple(ReadOnlySpan<byte> line, byte char1, byte char2, byte char3, out byte foundChar)
    {
        bool inQuote = false;
        foundChar = 0;

        for (int i = 0; i < line.Length; i++)
        {
            byte c = line[i];

            if (inQuote)
            {
                if (c == (byte)'"')
                {
                    if (i + 1 < line.Length && line[i + 1] == (byte)'"')
                    {
                        i++; // Skip doubled quote
                    }
                    else
                    {
                        inQuote = false;
                    }
                }
            }
            else if (c == (byte)'"')
            {
                inQuote = true;
            }
            else if (c == char1 || c == char2 || c == char3)
            {
                foundChar = c;
                return i;
            }
        }

        return -1;
    }

    private string UnquoteString(ReadOnlySpan<byte> quoted)
    {
        // Check for triple quotes - must have opening """ and closing """ with content
        // CRITICAL: The serializer ONLY uses triple quotes for strings with newlines.
        // So we only treat as triple-quoted if the content actually contains newlines.
        if (quoted.Length >= 6 && quoted[0] == (byte)'"' && quoted[1] == (byte)'"' && quoted[2] == (byte)'"')
        {
            // Check if it ends with triple quotes
            if (quoted[^1] == (byte)'"' && quoted[^2] == (byte)'"' && quoted[^3] == (byte)'"')
            {
                var inner = quoted.Slice(3, quoted.Length - 6);
                bool hasNewline = inner.IndexOf((byte)'\n') >= 0 || inner.IndexOf((byte)'\r') >= 0;

                // Only parse as triple-quoted if content has newlines
                // (that's the only case where serializer uses triple quotes)
                if (hasNewline)
                {
                    var content = Encoding.UTF8.GetString(inner);
                    return content.Replace("\\\\", "\\");
                }
                // No newlines = it's a double-quoted string that happens to
                // start with an escaped quote, fall through to double-quote handling
            }
        }

        // Single double quotes
        if (quoted.Length >= 2 && quoted[0] == (byte)'"' && quoted[^1] == (byte)'"')
        {
            var inner = quoted.Slice(1, quoted.Length - 2);
            var content = Encoding.UTF8.GetString(inner);
            // Unescape doubled quotes and backslashes
            return content.Replace("\"\"", "\"").Replace("\\\\", "\\");
        }

        return Encoding.UTF8.GetString(quoted);
    }

    private static char ParseDelimiter(ReadOnlySpan<byte> delimSpan)
    {
        if (delimSpan.SequenceEqual("\\t"u8))
        {
            return '\t';
        }
        if (delimSpan.Length == 1)
        {
            return (char)delimSpan[0];
        }
        return ',';
    }

    private static ReadOnlySpan<byte> TrimWhitespace(ReadOnlySpan<byte> span)
    {
        int start = 0;
        int end = span.Length;

        while (start < end && (span[start] == (byte)' ' || span[start] == (byte)'\t'))
        {
            start++;
        }

        while (end > start && (span[end - 1] == (byte)' ' || span[end - 1] == (byte)'\t'))
        {
            end--;
        }

        return span.Slice(start, end - start);
    }

    private static bool StartsWith(ReadOnlySpan<byte> span, ReadOnlySpan<byte> prefix)
    {
        return span.Length >= prefix.Length && span.Slice(0, prefix.Length).SequenceEqual(prefix);
    }
}
