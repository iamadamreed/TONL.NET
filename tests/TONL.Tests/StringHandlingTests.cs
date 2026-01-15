using Xunit;

namespace TONL.Tests;

/// <summary>
/// Tests for string quoting and escaping per TONL specification.
/// </summary>
public class StringHandlingTests
{
    // ===========================================
    // Automatic Quoting Tests
    // ===========================================

    [Fact]
    public void String_ContainsComma_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "hello, world" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"hello, world\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("hello, world", result!["text"]);
    }

    [Fact]
    public void String_ContainsColon_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "time: 12:00" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"time: 12:00\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("time: 12:00", result!["text"]);
    }

    [Fact]
    public void String_ContainsBraces_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "obj {a: 1}" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"obj {a: 1}\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("obj {a: 1}", result!["text"]);
    }

    [Fact]
    public void String_ContainsBrackets_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "array [1, 2]" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"array [1, 2]\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("array [1, 2]", result!["text"]);
    }

    [Fact]
    public void String_ContainsHash_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "#comment" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"#comment\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("#comment", result!["text"]);
    }

    [Fact]
    public void String_LeadingWhitespace_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "  leading" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"  leading\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("  leading", result!["text"]);
    }

    [Fact]
    public void String_TrailingWhitespace_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "trailing  " };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"trailing  \"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("trailing  ", result!["text"]);
    }

    [Fact]
    public void String_Empty_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("text: \"\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("", result!["text"]);
    }

    // ===========================================
    // Number-Like String Tests
    // ===========================================

    [Fact]
    public void String_LooksLikeInteger_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "42" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"42\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("42", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    [Fact]
    public void String_LooksLikeDecimal_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "3.14" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"3.14\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("3.14", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    [Fact]
    public void String_LooksLikeScientific_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "1e10" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"1e10\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("1e10", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    [Fact]
    public void String_LeadingPlus_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["phone"] = "+15551234567" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"+15551234567\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("+15551234567", result!["phone"]);
        Assert.IsType<string>(result["phone"]);
    }

    [Fact]
    public void String_LeadingMinus_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "-123" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"-123\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("-123", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    [Fact]
    public void String_LeadingZeros_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["zip"] = "02134" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"02134\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("02134", result!["zip"]);
        Assert.IsType<string>(result["zip"]);
    }

    // ===========================================
    // Boolean/Null-Like String Tests
    // ===========================================

    [Fact]
    public void String_LooksLikeTrue_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "true" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"true\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("true", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    [Fact]
    public void String_LooksLikeFalse_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "false" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"false\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("false", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    [Fact]
    public void String_LooksLikeNull_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "null" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"null\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("null", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    [Fact]
    public void String_LooksLikeInfinity_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "Infinity" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"Infinity\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("Infinity", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    [Fact]
    public void String_LooksLikeNaN_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "NaN" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"NaN\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("NaN", result!["text"]);
        Assert.IsType<string>(result["text"]);
    }

    // ===========================================
    // Escape Sequence Tests
    // ===========================================

    [Fact]
    public void String_ContainsQuotes_EscapedWithDoubling()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "He said \"Hello\"" };
        var tonl = TonlSerializer.SerializeToString(dict);

        // Double quotes are escaped by doubling
        Assert.Contains("\"\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("He said \"Hello\"", result!["text"]);
    }

    [Fact]
    public void String_ContainsBackslash_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["path"] = @"C:\Users\test" };
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal(@"C:\Users\test", result!["path"]);
    }

    [Fact]
    public void String_ContainsMultipleQuotes_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "\"quoted\" and \"more\"" };
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("\"quoted\" and \"more\"", result!["text"]);
    }

    // ===========================================
    // Multiline String Tests
    // ===========================================

    [Fact]
    public void String_SingleNewline_UsesTripleQuotes()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "line1\nline2" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"\"\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("line1\nline2", result!["text"]);
    }

    [Fact]
    public void String_MultipleNewlines_RoundTrips()
    {
        var content = "First\nSecond\nThird\nFourth";
        var dict = new Dictionary<string, object?> { ["text"] = content };
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal(content, result!["text"]);
    }

    [Fact]
    public void String_EmptyLines_RoundTrips()
    {
        var content = "Before\n\n\nAfter";
        var dict = new Dictionary<string, object?> { ["text"] = content };
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal(content, result!["text"]);
    }

    [Fact]
    public void String_CodeBlock_RoundTrips()
    {
        var code = @"function test() {
    console.log(""Hello"");
    return 42;
}";
        var dict = new Dictionary<string, object?> { ["code"] = code };
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal(code, result!["code"]);
    }

    // ===========================================
    // Key Quoting Tests
    // ===========================================

    [Fact]
    public void Key_ContainsAtSymbol_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["@type"] = "User" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"@type\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("User", result!["@type"]);
    }

    [Fact]
    public void Key_ContainsDash_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["field-name"] = "value" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"field-name\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("value", result!["field-name"]);
    }

    [Fact]
    public void Key_ContainsDots_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["foo.bar.baz"] = 123 };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"foo.bar.baz\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal(123, result!["foo.bar.baz"]);
    }

    [Fact]
    public void Key_ContainsSpace_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["full name"] = "Alice" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"full name\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("Alice", result!["full name"]);
    }

    [Fact]
    public void Key_StartsWithDigit_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { ["123key"] = "value" };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"123key\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("value", result!["123key"]);
    }

    [Fact]
    public void Key_IsEmpty_IsQuoted()
    {
        var dict = new Dictionary<string, object?> { [""] = "empty key" };
        var tonl = TonlSerializer.SerializeToString(dict);

        // Empty key must be quoted as ""
        Assert.Contains("\"\": ", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("empty key", result![""]);
    }

    // ===========================================
    // Unicode Tests
    // ===========================================

    [Fact]
    public void String_Unicode_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "Hello \u4e16\u754c" }; // Chinese
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("Hello \u4e16\u754c", result!["text"]);
    }

    [Fact]
    public void String_Emoji_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "Hello \U0001F600" }; // Grinning face
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("Hello \U0001F600", result!["text"]);
    }

    [Fact]
    public void String_AccentedCharacters_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["text"] = "caf\u00e9" }; // cafe with accent
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("caf\u00e9", result!["text"]);
    }

    [Fact]
    public void Key_Unicode_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["\u540d\u524d"] = "Alice" }; // Japanese "name"
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("Alice", result!["\u540d\u524d"]);
    }

    // ===========================================
    // Plain (Unquoted) String Tests
    // ===========================================

    [Fact]
    public void String_Simple_NotQuoted()
    {
        var dict = new Dictionary<string, object?> { ["name"] = "Alice" };
        var tonl = TonlSerializer.SerializeToString(dict);

        // Simple alphanumeric string should not be quoted
        Assert.Contains("name: Alice", tonl);
        Assert.DoesNotContain("\"Alice\"", tonl);
    }

    [Fact]
    public void String_WithUnderscore_NotQuoted()
    {
        var dict = new Dictionary<string, object?> { ["name"] = "user_name" };
        var tonl = TonlSerializer.SerializeToString(dict);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("user_name", result!["name"]);
    }
}
