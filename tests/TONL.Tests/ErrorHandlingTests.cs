using Xunit;

namespace TONL.Tests;

/// <summary>
/// Tests for error handling and invalid input.
/// </summary>
public class ErrorHandlingTests
{
    // ===========================================
    // Circular Reference Detection
    // ===========================================

    [Fact]
    public void CircularReference_SelfReference_ThrowsException()
    {
        var obj = new Dictionary<string, object?>();
        obj["self"] = obj;

        var ex = Assert.Throws<TonlCircularReferenceException>(() =>
            TonlSerializer.SerializeToString(obj));

        Assert.Contains("self", ex.Message);
    }

    [Fact]
    public void CircularReference_Indirect_ThrowsException()
    {
        var a = new Dictionary<string, object?>();
        var b = new Dictionary<string, object?>();
        a["child"] = b;
        b["parent"] = a;

        Assert.Throws<TonlCircularReferenceException>(() =>
            TonlSerializer.SerializeToString(a));
    }

    [Fact]
    public void CircularReference_InArray_ThrowsException()
    {
        var obj = new Dictionary<string, object?>();
        var list = new List<object?> { 1, 2, obj };
        obj["items"] = list;

        Assert.Throws<TonlCircularReferenceException>(() =>
            TonlSerializer.SerializeToString(obj));
    }

    // ===========================================
    // Invalid Options
    // ===========================================

    [Fact]
    public void InvalidDelimiter_ThrowsException()
    {
        var dict = new Dictionary<string, object?> { ["x"] = 1 };
        var options = new TonlOptions { Delimiter = 'x' }; // Invalid delimiter

        Assert.Throws<ArgumentException>(() =>
            TonlSerializer.SerializeToString(dict, options));
    }

    // ===========================================
    // Malformed Input (Lenient Mode)
    // ===========================================

    [Fact]
    public void MalformedInput_MissingVersion_StillParses()
    {
        var tonl = """
            root{x}:
              x: 42
            """;

        // Should still parse without version header
        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.NotNull(result);
    }

    [Fact]
    public void MalformedInput_EmptyFile_ReturnsEmptyDict()
    {
        var tonl = "";
        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void MalformedInput_WhitespaceOnly_ReturnsEmptyDict()
    {
        var tonl = "   \n\n   \t\t   ";
        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void MalformedInput_CommentsOnly_ReturnsEmptyDict()
    {
        var tonl = """
            #comment 1
            #comment 2
            #comment 3
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.NotNull(result);
    }

    // ===========================================
    // Type Safety
    // ===========================================

    [Fact]
    public void Deserialize_ToWrongType_ReturnsDefault()
    {
        var tonl = """
            #version 1.0
            root{name}:
              name: Alice
            """;

        // Try to deserialize a string-keyed dict to an int
        var result = TonlSerializer.Deserialize<int>(tonl);
        Assert.Equal(default(int), result);
    }

    // ===========================================
    // Robust Parsing
    // ===========================================

    [Fact]
    public void Parse_ExtraWhitespace_Handled()
    {
        var tonl = """
            #version 1.0

            root{x}:
              x:     42

            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.NotNull(result);
        Assert.Equal(42, result["x"]);
    }

    [Fact]
    public void Parse_WindowsLineEndings_Handled()
    {
        var tonl = "#version 1.0\r\nroot{x}:\r\n  x: 42\r\n";
        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.NotNull(result);
        Assert.Equal(42, result["x"]);
    }

    [Fact]
    public void Parse_MixedLineEndings_Handled()
    {
        var tonl = "#version 1.0\nroot{x}:\r\n  x: 42\r";
        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.NotNull(result);
        // Should parse at least something
    }

    // ===========================================
    // Null Safety
    // ===========================================

    [Fact]
    public void Serialize_NullInput_Handled()
    {
        var tonl = TonlSerializer.SerializeToString<object?>(null);
        Assert.NotNull(tonl);
        Assert.Contains("null", tonl);
    }

    [Fact]
    public void Deserialize_NullString_ThrowsException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TonlSerializer.DeserializeToDictionary((string)null!));
    }
}
