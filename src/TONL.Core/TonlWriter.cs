using System.Buffers;
using System.Globalization;
using System.Text;

namespace TONL.NET;

/// <summary>
/// A high-performance, low-allocation writer for TONL format.
/// </summary>
public ref struct TonlWriter
{
    private readonly IBufferWriter<byte> _output;
    private readonly TonlOptions _options;
    private Span<byte> _buffer;
    private int _buffered;

    private static ReadOnlySpan<byte> VersionPrefix => "#version "u8;
    private static ReadOnlySpan<byte> DelimiterPrefix => "#delimiter "u8;
    private static ReadOnlySpan<byte> NewLine => "\n"u8;
    private static ReadOnlySpan<byte> ColonSpace => ": "u8;
    private static ReadOnlySpan<byte> NullLiteral => "null"u8;
    private static ReadOnlySpan<byte> TrueLiteral => "true"u8;
    private static ReadOnlySpan<byte> FalseLiteral => "false"u8;
    private static ReadOnlySpan<byte> InfinityLiteral => "Infinity"u8;
    private static ReadOnlySpan<byte> NegInfinityLiteral => "-Infinity"u8;
    private static ReadOnlySpan<byte> NaNLiteral => "NaN"u8;

    /// <summary>
    /// Creates a new TONL writer.
    /// </summary>
    public TonlWriter(IBufferWriter<byte> output, TonlOptions? options = null)
    {
        _output = output ?? throw new ArgumentNullException(nameof(output));
        _options = options ?? TonlOptions.Default;
        _buffer = _output.GetSpan(512);
        _buffered = 0;
    }

    /// <summary>
    /// Writes the TONL version header.
    /// </summary>
    public void WriteVersionHeader()
    {
        WriteRaw(VersionPrefix);
        WriteRaw(Encoding.UTF8.GetBytes(_options.Version));
        WriteNewLine();
    }

    /// <summary>
    /// Writes the delimiter header if delimiter is not the default comma.
    /// </summary>
    public void WriteDelimiterHeader()
    {
        if (_options.Delimiter == ',')
        {
            return;
        }

        WriteRaw(DelimiterPrefix);

        if (_options.Delimiter == '\t')
        {
            WriteRaw("\\t"u8);
        }
        else
        {
            WriteByte((byte)_options.Delimiter);
        }

        WriteNewLine();
    }

    /// <summary>
    /// Writes a complete TONL header (version + optional delimiter).
    /// </summary>
    public void WriteHeader()
    {
        WriteVersionHeader();
        WriteDelimiterHeader();
    }

    /// <summary>
    /// Writes an object block header: key{col1,col2,...}:
    /// </summary>
    public void WriteObjectHeader(ReadOnlySpan<char> key, ReadOnlySpan<string> columns)
    {
        WriteKey(key);
        WriteByte((byte)'{');

        for (int i = 0; i < columns.Length; i++)
        {
            if (i > 0)
            {
                WriteByte((byte)',');
            }
            WriteKey(columns[i]);
        }

        WriteByte((byte)'}');
        WriteByte((byte)':');
    }

    /// <summary>
    /// Writes an array block header: key[N]{col1,col2,...}:
    /// </summary>
    public void WriteArrayHeader(ReadOnlySpan<char> key, int count, ReadOnlySpan<string> columns)
    {
        WriteKey(key);
        WriteByte((byte)'[');
        WriteInt32(count);
        WriteByte((byte)']');

        if (columns.Length > 0)
        {
            WriteByte((byte)'{');
            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0)
                {
                    WriteByte((byte)',');
                }
                WriteKey(columns[i]);
            }
            WriteByte((byte)'}');
        }

        WriteByte((byte)':');
    }

    /// <summary>
    /// Writes a primitive array header: key[N]:
    /// </summary>
    public void WritePrimitiveArrayHeader(ReadOnlySpan<char> key, int count)
    {
        WriteKey(key);
        WriteByte((byte)'[');
        WriteInt32(count);
        WriteByte((byte)']');
        WriteByte((byte)':');
    }

    /// <summary>
    /// Writes an indexed array element header: [N]:
    /// </summary>
    public void WriteIndexedArrayHeader(int index)
    {
        WriteByte((byte)'[');
        WriteInt32(index);
        WriteByte((byte)']');
        WriteByte((byte)':');
    }

    /// <summary>
    /// Writes an indexed array element with object columns: [N]{col1,col2,...}:
    /// </summary>
    public void WriteIndexedObjectHeader(int index, ReadOnlySpan<string> columns)
    {
        WriteByte((byte)'[');
        WriteInt32(index);
        WriteByte((byte)']');
        WriteByte((byte)'{');

        for (int i = 0; i < columns.Length; i++)
        {
            if (i > 0)
            {
                WriteByte((byte)',');
            }
            WriteKey(columns[i]);
        }

        WriteByte((byte)'}');
        WriteByte((byte)':');
    }

    /// <summary>
    /// Writes a key, quoting it if necessary for special characters.
    /// </summary>
    public void WriteKey(ReadOnlySpan<char> key)
    {
        if (KeyNeedsQuoting(key))
        {
            WriteQuotedKey(key);
        }
        else
        {
            WriteUtf8String(key);
        }
    }

    /// <summary>
    /// Determines if a key needs to be quoted.
    /// </summary>
    public static bool KeyNeedsQuoting(ReadOnlySpan<char> key)
    {
        if (key.IsEmpty)
        {
            return true; // Empty keys must be quoted
        }

        // Keys starting with a digit need quoting (would look like numbers)
        if (char.IsDigit(key[0]))
        {
            return true;
        }

        // Check for special characters that require quoting
        foreach (char c in key)
        {
            if (c == '#' || c == '@' || c == ':' || c == ',' ||
                c == '{' || c == '}' || c == '[' || c == ']' ||
                c == '"' || c == ' ' || c == '-' || c == '.' ||
                c == '\t' || c == '\n' || c == '\r')
            {
                return true;
            }
        }

        // Check for leading/trailing whitespace
        if (char.IsWhiteSpace(key[0]) || char.IsWhiteSpace(key[^1]))
        {
            return true;
        }

        return false;
    }

    private void WriteQuotedKey(ReadOnlySpan<char> key)
    {
        WriteByte((byte)'"');

        foreach (char c in key)
        {
            if (c == '"')
            {
                // Escape quotes by doubling
                WriteByte((byte)'"');
                WriteByte((byte)'"');
            }
            else if (c == '\\')
            {
                // Escape backslashes
                WriteByte((byte)'\\');
                WriteByte((byte)'\\');
            }
            else if (c < 128)
            {
                WriteByte((byte)c);
            }
            else
            {
                // Write multi-byte UTF-8 directly to buffer
                WriteUtf8Char(c);
            }
        }

        WriteByte((byte)'"');
    }

    /// <summary>
    /// Writes a key-value pair: key: value
    /// </summary>
    public void WriteKeyValue(ReadOnlySpan<char> key, ReadOnlySpan<char> value)
    {
        WriteKey(key);
        WriteRaw(ColonSpace);
        WriteStringValue(value);
    }

    /// <summary>
    /// Writes a null key-value pair: key: null
    /// </summary>
    public void WriteKeyNull(ReadOnlySpan<char> key)
    {
        WriteKey(key);
        WriteRaw(ColonSpace);
        WriteRaw(NullLiteral);
    }

    /// <summary>
    /// Writes a boolean key-value pair: key: true/false
    /// </summary>
    public void WriteKeyBoolean(ReadOnlySpan<char> key, bool value)
    {
        WriteKey(key);
        WriteRaw(ColonSpace);
        WriteRaw(value ? TrueLiteral : FalseLiteral);
    }

    /// <summary>
    /// Writes an integer key-value pair: key: 123
    /// </summary>
    public void WriteKeyInt32(ReadOnlySpan<char> key, int value)
    {
        WriteKey(key);
        WriteRaw(ColonSpace);
        WriteInt32(value);
    }

    /// <summary>
    /// Writes a long key-value pair: key: 123456789
    /// </summary>
    public void WriteKeyInt64(ReadOnlySpan<char> key, long value)
    {
        WriteKey(key);
        WriteRaw(ColonSpace);
        WriteInt64(value);
    }

    /// <summary>
    /// Writes a double key-value pair: key: 3.14
    /// </summary>
    public void WriteKeyDouble(ReadOnlySpan<char> key, double value)
    {
        WriteKey(key);
        WriteRaw(ColonSpace);
        WriteDouble(value);
    }

    /// <summary>
    /// Writes a string value with appropriate quoting.
    /// </summary>
    public void WriteStringValue(ReadOnlySpan<char> value)
    {
        if (NeedsQuoting(value))
        {
            WriteQuotedString(value);
        }
        else
        {
            WriteUtf8String(value);
        }
    }

    /// <summary>
    /// Writes a null value.
    /// </summary>
    public void WriteNull()
    {
        WriteRaw(NullLiteral);
    }

    /// <summary>
    /// Writes a boolean value.
    /// </summary>
    public void WriteBoolean(bool value)
    {
        WriteRaw(value ? TrueLiteral : FalseLiteral);
    }

    /// <summary>
    /// Writes an integer value.
    /// </summary>
    public void WriteInt32(int value)
    {
        EnsureCapacity(16);
        if (System.Buffers.Text.Utf8Formatter.TryFormat(value, _buffer.Slice(_buffered), out int bytesWritten))
        {
            _buffered += bytesWritten;
        }
        else
        {
            // Fallback for edge cases
            WriteUtf8String(value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Writes a long value.
    /// </summary>
    public void WriteInt64(long value)
    {
        EnsureCapacity(24);
        if (System.Buffers.Text.Utf8Formatter.TryFormat(value, _buffer.Slice(_buffered), out int bytesWritten))
        {
            _buffered += bytesWritten;
        }
        else
        {
            WriteUtf8String(value.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Writes a double value.
    /// </summary>
    public void WriteDouble(double value)
    {
        if (double.IsPositiveInfinity(value))
        {
            WriteRaw(InfinityLiteral);
        }
        else if (double.IsNegativeInfinity(value))
        {
            WriteRaw(NegInfinityLiteral);
        }
        else if (double.IsNaN(value))
        {
            WriteRaw(NaNLiteral);
        }
        else
        {
            // Use G17 format to ensure round-trip fidelity
            WriteUtf8String(value.ToString("G17", CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// Writes the delimiter character, optionally followed by a space.
    /// </summary>
    public void WriteDelimiter()
    {
        WriteByte((byte)_options.Delimiter);
        if (_options.PrettyDelimiters)
        {
            WriteByte((byte)' ');
        }
    }

    /// <summary>
    /// Writes a newline character.
    /// </summary>
    public void WriteNewLine()
    {
        WriteRaw(NewLine);
    }

    /// <summary>
    /// Writes indentation for the specified nesting level.
    /// </summary>
    public void WriteIndent(int level)
    {
        int spaces = level * _options.IndentSize;
        for (int i = 0; i < spaces; i++)
        {
            WriteByte((byte)' ');
        }
    }

    /// <summary>
    /// Flushes any buffered data to the output.
    /// </summary>
    public void Flush()
    {
        if (_buffered > 0)
        {
            _output.Advance(_buffered);
            _buffered = 0;
            _buffer = _output.GetSpan(512);
        }
    }

    /// <summary>
    /// Determines if a string value needs to be quoted.
    /// </summary>
    public bool NeedsQuoting(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return true; // Empty strings must be quoted
        }

        // Check for reserved literals
        if (value.SequenceEqual("true") || value.SequenceEqual("false") ||
            value.SequenceEqual("null") || value.SequenceEqual("undefined") ||
            value.SequenceEqual("Infinity") || value.SequenceEqual("-Infinity") ||
            value.SequenceEqual("NaN"))
        {
            return true;
        }

        // Check if it looks like a number
        if (LooksLikeNumber(value))
        {
            return true;
        }

        // Check for leading/trailing whitespace
        if (char.IsWhiteSpace(value[0]) || char.IsWhiteSpace(value[^1]))
        {
            return true;
        }

        // Check for special characters
        char delimiter = _options.Delimiter;
        foreach (char c in value)
        {
            if (c == delimiter || c == ':' || c == '{' || c == '}' ||
                c == '[' || c == ']' || c == '#' || c == '"' ||
                c == '\n' || c == '\r' || c == '\t')
            {
                return true;
            }
        }

        return false;
    }

    private static bool LooksLikeNumber(ReadOnlySpan<char> value)
    {
        if (value.IsEmpty)
        {
            return false;
        }

        int i = 0;

        // Optional leading sign (+ or -)
        if (value[0] == '-' || value[0] == '+')
        {
            i++;
            if (i >= value.Length)
            {
                return false;
            }
        }

        // Must have at least one digit
        if (!char.IsDigit(value[i]))
        {
            return false;
        }

        // Skip digits
        while (i < value.Length && char.IsDigit(value[i]))
        {
            i++;
        }

        // Check for decimal part
        if (i < value.Length && value[i] == '.')
        {
            i++;
            while (i < value.Length && char.IsDigit(value[i]))
            {
                i++;
            }
        }

        // Check for exponent
        if (i < value.Length && (value[i] == 'e' || value[i] == 'E'))
        {
            i++;
            if (i < value.Length && (value[i] == '+' || value[i] == '-'))
            {
                i++;
            }
            while (i < value.Length && char.IsDigit(value[i]))
            {
                i++;
            }
        }

        return i == value.Length;
    }

    private void WriteQuotedString(ReadOnlySpan<char> value)
    {
        // Check if triple quotes needed (contains newlines)
        bool hasNewlines = value.Contains('\n') || value.Contains('\r');

        if (hasNewlines)
        {
            WriteTripleQuotedString(value);
        }
        else
        {
            WriteDoubleQuotedString(value);
        }
    }

    private void WriteDoubleQuotedString(ReadOnlySpan<char> value)
    {
        WriteByte((byte)'"');

        foreach (char c in value)
        {
            if (c == '"')
            {
                // Escape quotes by doubling
                WriteByte((byte)'"');
                WriteByte((byte)'"');
            }
            else if (c == '\\')
            {
                // Escape backslashes
                WriteByte((byte)'\\');
                WriteByte((byte)'\\');
            }
            else if (c < 128)
            {
                WriteByte((byte)c);
            }
            else
            {
                // Write multi-byte UTF-8 directly to buffer
                WriteUtf8Char(c);
            }
        }

        WriteByte((byte)'"');
    }

    private void WriteTripleQuotedString(ReadOnlySpan<char> value)
    {
        WriteRaw("\"\"\""u8);

        foreach (char c in value)
        {
            if (c == '\\')
            {
                WriteRaw("\\\\"u8);
            }
            else if (c < 128)
            {
                WriteByte((byte)c);
            }
            else
            {
                // Write multi-byte UTF-8 directly to buffer
                WriteUtf8Char(c);
            }
        }

        WriteRaw("\"\"\""u8);
    }

    private void WriteUtf8String(ReadOnlySpan<char> value)
    {
        int maxBytes = Encoding.UTF8.GetMaxByteCount(value.Length);
        EnsureCapacity(maxBytes);

        int bytesWritten = Encoding.UTF8.GetBytes(value, _buffer.Slice(_buffered));
        _buffered += bytesWritten;
    }

    private void WriteRaw(scoped ReadOnlySpan<byte> data)
    {
        EnsureCapacity(data.Length);
        data.CopyTo(_buffer.Slice(_buffered));
        _buffered += data.Length;
    }

    /// <summary>
    /// Writes a single byte to the output.
    /// </summary>
    public void WriteByte(byte b)
    {
        EnsureCapacity(1);
        _buffer[_buffered++] = b;
    }

    private void EnsureCapacity(int needed)
    {
        if (_buffered + needed > _buffer.Length)
        {
            _output.Advance(_buffered);
            _buffer = _output.GetSpan(Math.Max(needed, 512));
            _buffered = 0;
        }
    }

    private void WriteUtf8Char(char c)
    {
        // Handle multi-byte UTF-8 encoding for non-ASCII characters
        Span<char> chars = stackalloc char[1];
        chars[0] = c;
        int maxBytes = Encoding.UTF8.GetMaxByteCount(1);
        EnsureCapacity(maxBytes);
        int bytesWritten = Encoding.UTF8.GetBytes(chars, _buffer.Slice(_buffered));
        _buffered += bytesWritten;
    }
}

// Helper for Utf8Formatter since it's in System.Buffers.Text
file static class Utf8Formatter
{
    public static bool TryFormat(int value, Span<byte> destination, out int bytesWritten)
    {
        return System.Buffers.Text.Utf8Formatter.TryFormat(value, destination, out bytesWritten);
    }

    public static bool TryFormat(long value, Span<byte> destination, out int bytesWritten)
    {
        return System.Buffers.Text.Utf8Formatter.TryFormat(value, destination, out bytesWritten);
    }
}
