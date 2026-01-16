using TONL.NET;
using Xunit;

namespace TONL.NET.Tests;

public class TonlWriterTests
{
    [Fact]
    public void WriteHeader_WritesVersionAndDelimiter()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteHeader();
        writer.Flush();

        var result = buffer.ToString();
        Assert.StartsWith("#version 1.0", result);
    }

    [Fact]
    public void WriteHeader_CustomDelimiter_WritesDelimiterHeader()
    {
        using var buffer = new TonlBufferWriter();
        var options = new TonlOptions { Delimiter = '|' };
        var writer = new TonlWriter(buffer, options);

        writer.WriteHeader();
        writer.Flush();

        var result = buffer.ToString();
        Assert.Contains("#delimiter |", result);
    }

    [Fact]
    public void WriteHeader_TabDelimiter_EscapesTab()
    {
        using var buffer = new TonlBufferWriter();
        var options = new TonlOptions { Delimiter = '\t' };
        var writer = new TonlWriter(buffer, options);

        writer.WriteHeader();
        writer.Flush();

        var result = buffer.ToString();
        Assert.Contains("#delimiter \\t", result);
    }

    [Fact]
    public void WriteObjectHeader_WritesCorrectFormat()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteObjectHeader("user", new[] { "id", "name", "age" });
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("user{id,name,age}:", result);
    }

    [Fact]
    public void WriteArrayHeader_WritesCorrectFormat()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteArrayHeader("users", 3, new[] { "id", "name" });
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("users[3]{id,name}:", result);
    }

    [Fact]
    public void WritePrimitiveArrayHeader_WritesCorrectFormat()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WritePrimitiveArrayHeader("numbers", 5);
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("numbers[5]:", result);
    }

    [Fact]
    public void WriteKeyValue_String_WritesCorrectly()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteKeyValue("name", "Alice");
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("name: Alice", result);
    }

    [Fact]
    public void WriteKeyInt32_WritesCorrectly()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteKeyInt32("age", 30);
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("age: 30", result);
    }

    [Fact]
    public void WriteKeyBoolean_True_WritesLowercase()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteKeyBoolean("active", true);
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("active: true", result);
    }

    [Fact]
    public void WriteKeyNull_WritesNullLiteral()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteKeyNull("value");
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("value: null", result);
    }

    [Fact]
    public void WriteDouble_Infinity_WritesLiteral()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteKeyDouble("value", double.PositiveInfinity);
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("value: Infinity", result);
    }

    [Fact]
    public void WriteDouble_NaN_WritesLiteral()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteKeyDouble("value", double.NaN);
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("value: NaN", result);
    }

    [Theory]
    [InlineData("", true)] // Empty string
    [InlineData("true", true)] // Reserved literal
    [InlineData("false", true)]
    [InlineData("null", true)]
    [InlineData("123", true)] // Number-like
    [InlineData("3.14", true)]
    [InlineData("Hello, world", true)] // Contains delimiter
    [InlineData("key: value", true)] // Contains colon
    [InlineData("  leading", true)] // Leading whitespace
    [InlineData("trailing  ", true)] // Trailing whitespace
    [InlineData("Alice", false)] // Normal string
    [InlineData("Hello world", false)] // Space in middle is ok
    public void NeedsQuoting_ReturnsCorrectResult(string value, bool expected)
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        var result = writer.NeedsQuoting(value);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteStringValue_QuotesWhenNeeded()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteStringValue("Hello, world");
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("\"Hello, world\"", result);
    }

    [Fact]
    public void WriteStringValue_EscapesQuotes()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteStringValue("She said \"Hi\"");
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("\"She said \"\"Hi\"\"\"", result);
    }

    [Fact]
    public void WriteIndent_WritesCorrectSpaces()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteIndent(2);
        writer.WriteKeyValue("key", "value");
        writer.Flush();

        var result = buffer.ToString();
        Assert.StartsWith("    ", result); // 2 levels * 2 spaces = 4 spaces
    }

    // Quoted Keys Tests

    [Theory]
    [InlineData("@type", true)] // At symbol
    [InlineData("field-name", true)] // Hyphen
    [InlineData("field.name", true)] // Dot
    [InlineData("key with spaces", true)] // Spaces
    [InlineData("#comment", true)] // Hash
    [InlineData("1key", true)] // Starts with digit
    [InlineData("normalKey", false)] // Normal key
    [InlineData("_private", false)] // Underscore start
    public void KeyNeedsQuoting_ReturnsCorrectResult(string key, bool expected)
    {
        var result = TonlWriter.KeyNeedsQuoting(key);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void WriteKey_QuotesSpecialCharacterKeys()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteKey("@type");
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("\"@type\"", result);
    }

    [Fact]
    public void WriteKey_DoesNotQuoteNormalKeys()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteKey("normalKey");
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("normalKey", result);
    }

    [Fact]
    public void WriteObjectHeader_QuotesSpecialKeys()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteObjectHeader("@data", new[] { "field-1", "field-2" });
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("\"@data\"{\"field-1\",\"field-2\"}:", result);
    }

    // Indexed Array Tests

    [Fact]
    public void WriteIndexedArrayHeader_WritesCorrectFormat()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteIndexedArrayHeader(0);
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("[0]:", result);
    }

    [Fact]
    public void WriteIndexedObjectHeader_WritesCorrectFormat()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteIndexedObjectHeader(1, new[] { "name", "age" });
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("[1]{name,age}:", result);
    }

    [Fact]
    public void WriteIndexedObjectHeader_QuotesSpecialColumns()
    {
        using var buffer = new TonlBufferWriter();
        var writer = new TonlWriter(buffer);

        writer.WriteIndexedObjectHeader(2, new[] { "@id", "first-name" });
        writer.Flush();

        var result = buffer.ToString();
        Assert.Equal("[2]{\"@id\",\"first-name\"}:", result);
    }
}
