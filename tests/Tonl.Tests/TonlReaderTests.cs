using System.Text;
using Xunit;

namespace Tonl.Tests;

public class TonlReaderTests
{
    [Fact]
    public void ParseHeaders_ExtractsVersion()
    {
        var tonl = "#version 1.0\nroot{name}: name: Alice"u8;
        var reader = new TonlReader(tonl);

        reader.ParseHeaders();

        Assert.Equal("1.0", reader.Version);
    }

    [Fact]
    public void ParseHeaders_ExtractsDelimiter()
    {
        var tonl = "#version 1.0\n#delimiter |\ndata[2]: a | b"u8;
        var reader = new TonlReader(tonl);

        reader.ParseHeaders();

        Assert.Equal('|', reader.Delimiter);
    }

    [Fact]
    public void ParseHeaders_TabDelimiter()
    {
        var tonl = "#version 1.0\n#delimiter \\t\ndata[2]: a\tb"u8;
        var reader = new TonlReader(tonl);

        reader.ParseHeaders();

        Assert.Equal('\t', reader.Delimiter);
    }

    [Fact]
    public void ReadLine_ReturnsLines()
    {
        var tonl = "line1\nline2\nline3"u8;
        var reader = new TonlReader(tonl);

        Assert.True(reader.ReadLine(out var line1));
        Assert.Equal("line1", Encoding.UTF8.GetString(line1));

        Assert.True(reader.ReadLine(out var line2));
        Assert.Equal("line2", Encoding.UTF8.GetString(line2));

        Assert.True(reader.ReadLine(out var line3));
        Assert.Equal("line3", Encoding.UTF8.GetString(line3));

        Assert.False(reader.ReadLine(out _));
    }

    [Fact]
    public void ReadLine_HandlesWindowsLineEndings()
    {
        var tonl = "line1\r\nline2\r\n"u8;
        var reader = new TonlReader(tonl);

        Assert.True(reader.ReadLine(out var line1));
        Assert.Equal("line1", Encoding.UTF8.GetString(line1));

        Assert.True(reader.ReadLine(out var line2));
        Assert.Equal("line2", Encoding.UTF8.GetString(line2));
    }

    [Fact]
    public void ParsePrimitiveValue_Null()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("null"u8);
        Assert.Null(result);
    }

    [Fact]
    public void ParsePrimitiveValue_True()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("true"u8);
        Assert.Equal(true, result);
    }

    [Fact]
    public void ParsePrimitiveValue_False()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("false"u8);
        Assert.Equal(false, result);
    }

    [Fact]
    public void ParsePrimitiveValue_Integer()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("42"u8);
        Assert.Equal(42, result);
    }

    [Fact]
    public void ParsePrimitiveValue_NegativeInteger()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("-123"u8);
        Assert.Equal(-123, result);
    }

    [Fact]
    public void ParsePrimitiveValue_Double()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("3.14"u8);
        Assert.Equal(3.14, result);
    }

    [Fact]
    public void ParsePrimitiveValue_Infinity()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("Infinity"u8);
        Assert.Equal(double.PositiveInfinity, result);
    }

    [Fact]
    public void ParsePrimitiveValue_NegativeInfinity()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("-Infinity"u8);
        Assert.Equal(double.NegativeInfinity, result);
    }

    [Fact]
    public void ParsePrimitiveValue_NaN()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("NaN"u8);
        Assert.True(result is double d && double.IsNaN(d));
    }

    [Fact]
    public void ParsePrimitiveValue_QuotedString()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("\"Hello, world\""u8);
        Assert.Equal("Hello, world", result);
    }

    [Fact]
    public void ParsePrimitiveValue_QuotedStringWithEscapedQuotes()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("\"She said \"\"Hi\"\"\""u8);
        Assert.Equal("She said \"Hi\"", result);
    }

    [Fact]
    public void ParsePrimitiveValue_UnquotedString()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var result = reader.ParsePrimitiveValue("Alice"u8);
        Assert.Equal("Alice", result);
    }

    [Fact]
    public void TryParseObjectHeader_ValidHeader()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "user{id,name,age}:"u8;

        Assert.True(reader.TryParseObjectHeader(line, out var key, out var columns));
        Assert.Equal("user", key);
        Assert.Equal(new[] { "id", "name", "age" }, columns);
    }

    [Fact]
    public void TryParseObjectHeader_EmptyColumns()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "empty{}:"u8;

        Assert.True(reader.TryParseObjectHeader(line, out var key, out var columns));
        Assert.Equal("empty", key);
        Assert.Empty(columns);
    }

    [Fact]
    public void TryParseArrayHeader_WithColumns()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "users[3]{id,name}:"u8;

        Assert.True(reader.TryParseArrayHeader(line, out var key, out var count, out var columns));
        Assert.Equal("users", key);
        Assert.Equal(3, count);
        Assert.Equal(new[] { "id", "name" }, columns);
    }

    [Fact]
    public void TryParseArrayHeader_PrimitiveArray()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "numbers[5]:"u8;

        Assert.True(reader.TryParseArrayHeader(line, out var key, out var count, out var columns));
        Assert.Equal("numbers", key);
        Assert.Equal(5, count);
        Assert.Empty(columns);
    }

    [Fact]
    public void TryParseKeyValue_SimpleValue()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "name: Alice"u8;

        Assert.True(reader.TryParseKeyValue(line, out var key, out var value));
        Assert.Equal("name", key);
        Assert.Equal("Alice", value);
    }

    [Fact]
    public void TryParseKeyValue_IntegerValue()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "age: 30"u8;

        Assert.True(reader.TryParseKeyValue(line, out var key, out var value));
        Assert.Equal("age", key);
        Assert.Equal(30, value);
    }

    [Fact]
    public void GetIndentLevel_NoIndent()
    {
        var line = "key: value"u8;
        Assert.Equal(0, TonlReader.GetIndentLevel(line));
    }

    [Fact]
    public void GetIndentLevel_OneLevel()
    {
        var line = "  key: value"u8;
        Assert.Equal(1, TonlReader.GetIndentLevel(line));
    }

    [Fact]
    public void GetIndentLevel_TwoLevels()
    {
        var line = "    key: value"u8;
        Assert.Equal(2, TonlReader.GetIndentLevel(line));
    }

    [Fact]
    public void ParseFields_CommaSeparated()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "Alice, 30, admin"u8;

        Span<Range> fields = stackalloc Range[5];
        int count = reader.ParseFields(line, fields);

        Assert.Equal(3, count);
        Assert.Equal("Alice", Encoding.UTF8.GetString(line[fields[0]]));
        Assert.Equal("30", Encoding.UTF8.GetString(line[fields[1]]));
        Assert.Equal("admin", Encoding.UTF8.GetString(line[fields[2]]));
    }

    [Fact]
    public void ParseFields_QuotedValue()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "\"Hello, world\", 42"u8;

        Span<Range> fields = stackalloc Range[5];
        int count = reader.ParseFields(line, fields);

        Assert.Equal(2, count);
        Assert.Equal("\"Hello, world\"", Encoding.UTF8.GetString(line[fields[0]]));
        Assert.Equal("42", Encoding.UTF8.GetString(line[fields[1]]));
    }

    // Quoted Keys Parsing Tests

    [Fact]
    public void TryParseObjectHeader_QuotedKey()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "\"@type\"{id,name}:"u8;

        Assert.True(reader.TryParseObjectHeader(line, out var key, out var columns));
        Assert.Equal("@type", key);
        Assert.Equal(new[] { "id", "name" }, columns);
    }

    [Fact]
    public void TryParseObjectHeader_QuotedColumns()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "data{\"field-1\",\"field-2\"}:"u8;

        Assert.True(reader.TryParseObjectHeader(line, out var key, out var columns));
        Assert.Equal("data", key);
        Assert.Equal(new[] { "field-1", "field-2" }, columns);
    }

    [Fact]
    public void TryParseArrayHeader_QuotedKey()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "\"@items\"[3]{id,name}:"u8;

        Assert.True(reader.TryParseArrayHeader(line, out var key, out var count, out var columns));
        Assert.Equal("@items", key);
        Assert.Equal(3, count);
        Assert.Equal(new[] { "id", "name" }, columns);
    }

    [Fact]
    public void TryParseKeyValue_QuotedKey()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "\"@type\": User"u8;

        Assert.True(reader.TryParseKeyValue(line, out var key, out var value));
        Assert.Equal("@type", key);
        Assert.Equal("User", value);
    }

    [Fact]
    public void TryParseKeyValue_QuotedKeyWithEscapedQuote()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "\"key\"\"name\": value"u8;

        Assert.True(reader.TryParseKeyValue(line, out var key, out var value));
        Assert.Equal("key\"name", key);
        Assert.Equal("value", value);
    }

    // Indexed Header Parsing Tests

    [Fact]
    public void TryParseIndexedHeader_PrimitiveValue()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "[0]: value"u8;

        Assert.True(reader.TryParseIndexedHeader(line, out var index, out var columns, out var hasValue));
        Assert.Equal(0, index);
        Assert.Empty(columns);
        Assert.True(hasValue);
    }

    [Fact]
    public void TryParseIndexedHeader_ObjectWithColumns()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "[1]{name,age}:"u8;

        Assert.True(reader.TryParseIndexedHeader(line, out var index, out var columns, out var hasValue));
        Assert.Equal(1, index);
        Assert.Equal(new[] { "name", "age" }, columns);
        Assert.False(hasValue);
    }

    [Fact]
    public void TryParseIndexedHeader_ObjectWithQuotedColumns()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "[2]{\"@id\",\"first-name\"}:"u8;

        Assert.True(reader.TryParseIndexedHeader(line, out var index, out var columns, out var hasValue));
        Assert.Equal(2, index);
        Assert.Equal(new[] { "@id", "first-name" }, columns);
        Assert.False(hasValue);
    }

    [Fact]
    public void TryParseIndexedHeader_NoInlineValue()
    {
        var reader = new TonlReader(ReadOnlySpan<byte>.Empty);
        var line = "[5]:"u8;

        Assert.True(reader.TryParseIndexedHeader(line, out var index, out var columns, out var hasValue));
        Assert.Equal(5, index);
        Assert.Empty(columns);
        Assert.False(hasValue);
    }

    // Delimiter Auto-Detection Tests

    [Fact]
    public void ParseHeaders_AutoDetectsDelimiter_Pipe()
    {
        var tonl = "#version 1.0\nusers[2]{id,name}:\n1|Alice\n2|Bob"u8;
        var reader = new TonlReader(tonl);

        reader.ParseHeaders();

        Assert.Equal('|', reader.Delimiter);
    }

    [Fact]
    public void ParseHeaders_AutoDetectsDelimiter_Tab()
    {
        var tonl = "#version 1.0\nusers[2]{id,name}:\n1\tAlice\n2\tBob"u8;
        var reader = new TonlReader(tonl);

        reader.ParseHeaders();

        Assert.Equal('\t', reader.Delimiter);
    }

    [Fact]
    public void ParseHeaders_AutoDetectsDelimiter_Semicolon()
    {
        var tonl = "#version 1.0\nusers[2]{id,name}:\n1;Alice\n2;Bob"u8;
        var reader = new TonlReader(tonl);

        reader.ParseHeaders();

        Assert.Equal(';', reader.Delimiter);
    }

    [Fact]
    public void ParseHeaders_DefaultsToComma_WhenNoData()
    {
        var tonl = "#version 1.0\nroot{name}: name: Alice"u8;
        var reader = new TonlReader(tonl);

        reader.ParseHeaders();

        Assert.Equal(',', reader.Delimiter);
    }
}
