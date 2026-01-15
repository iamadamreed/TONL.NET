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
            }
        }
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

        // Find the opening brace
        int braceIdx = trimmed.IndexOf((byte)'{');
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

        // Must end with :
        if (trimmed.Length == 0 || trimmed[^1] != (byte)':')
        {
            return false;
        }

        // Extract key
        key = Encoding.UTF8.GetString(trimmed.Slice(0, braceIdx));

        // Extract columns
        var columnsSpan = trimmed.Slice(braceIdx + 1, closeBraceIdx - braceIdx - 1);
        if (columnsSpan.IsEmpty)
        {
            columns = Array.Empty<string>();
        }
        else
        {
            var columnsStr = Encoding.UTF8.GetString(columnsSpan);
            columns = columnsStr.Split(',', StringSplitOptions.TrimEntries);
        }

        TokenType = TonlTokenType.ObjectHeader;
        return true;
    }

    /// <summary>
    /// Parses an array header: key[N]{col1,col2,...}: or key[N]:
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

        // Find the opening bracket
        int bracketIdx = trimmed.IndexOf((byte)'[');
        if (bracketIdx < 0)
        {
            return false;
        }

        // Find the closing bracket
        int closeBracketIdx = trimmed.IndexOf((byte)']');
        if (closeBracketIdx < bracketIdx)
        {
            return false;
        }

        // Extract key
        key = Encoding.UTF8.GetString(trimmed.Slice(0, bracketIdx));

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
                if (!columnsSpan.IsEmpty)
                {
                    var columnsStr = Encoding.UTF8.GetString(columnsSpan);
                    columns = columnsStr.Split(',', StringSplitOptions.TrimEntries);
                }
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

        // Find the colon
        int colonIdx = trimmed.IndexOf((byte)':');
        if (colonIdx < 0)
        {
            return false;
        }

        // Make sure this isn't a header (check for { before :)
        int braceIdx = trimmed.IndexOf((byte)'{');
        if (braceIdx >= 0 && braceIdx < colonIdx)
        {
            return false;
        }

        // Make sure this isn't an array header (check for [ before :)
        int bracketIdx = trimmed.IndexOf((byte)'[');
        if (bracketIdx >= 0 && bracketIdx < colonIdx)
        {
            return false;
        }

        key = Encoding.UTF8.GetString(TrimWhitespace(trimmed.Slice(0, colonIdx)));
        var valueSpan = trimmed.Slice(colonIdx + 1);
        value = ParsePrimitiveValue(valueSpan);

        TokenType = TonlTokenType.KeyValue;
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

    private string UnquoteString(ReadOnlySpan<byte> quoted)
    {
        // Check for triple quotes
        if (quoted.Length >= 6 && quoted[0] == (byte)'"' && quoted[1] == (byte)'"' && quoted[2] == (byte)'"')
        {
            // Find closing triple quotes
            var inner = quoted.Slice(3);
            int endIdx = inner.Length;
            if (inner.Length >= 3 && inner[^1] == (byte)'"' && inner[^2] == (byte)'"' && inner[^3] == (byte)'"')
            {
                endIdx = inner.Length - 3;
            }
            var content = Encoding.UTF8.GetString(inner.Slice(0, endIdx));
            // Unescape
            return content.Replace("\\\"\"\"", "\"\"\"").Replace("\\\\", "\\");
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
