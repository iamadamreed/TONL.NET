using Xunit;

namespace TONL.Tests;

/// <summary>
/// Tests for delimiter support per TONL specification.
/// TONL supports 4 delimiters: comma (,), pipe (|), tab (\t), and semicolon (;)
/// </summary>
public class DelimiterTests
{
    // ===========================================
    // Comma Delimiter (Default)
    // ===========================================

    [Fact]
    public void Comma_IsDefaultDelimiter()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<int> { 1, 2, 3 }
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        // Default comma should not require a header
        Assert.DoesNotContain("#delimiter", tonl);
        // Compact format by default: values separated by comma without space
        Assert.Contains("1,2,3", tonl);
    }

    [Fact]
    public void Comma_PrettyDelimiters_AddsSpaces()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<int> { 1, 2, 3 }
        };

        var options = new TonlOptions { PrettyDelimiters = true };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        // Pretty format: values separated by comma with space
        Assert.Contains(", ", tonl);
    }

    [Fact]
    public void Comma_ExplicitOption_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["a"] = 1, ["b"] = 2, ["c"] = 3
        };

        var options = new TonlOptions { Delimiter = ',' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal(1, result["a"]);
        Assert.Equal(2, result["b"]);
        Assert.Equal(3, result["c"]);
    }

    [Fact]
    public void Comma_ValueContainingComma_IsQuoted()
    {
        var dict = new Dictionary<string, object?>
        {
            ["name"] = "Smith, John"
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"Smith, John\"", tonl);
    }

    // ===========================================
    // Pipe Delimiter
    // ===========================================

    [Fact]
    public void Pipe_WritesDelimiterHeader()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<int> { 1, 2, 3 }
        };

        var options = new TonlOptions { Delimiter = '|', PrettyDelimiters = true };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        Assert.Contains("#delimiter |", tonl);
        Assert.Contains("| ", tonl);
    }

    [Fact]
    public void Pipe_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { "a", "b", "c" }
        };

        var options = new TonlOptions { Delimiter = '|' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var items = result["items"] as IList<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        Assert.Equal("a", items[0]);
        Assert.Equal("b", items[1]);
        Assert.Equal("c", items[2]);
    }

    [Fact]
    public void Pipe_TabularArray_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["users"] = new List<Dictionary<string, object?>>
            {
                new() { ["id"] = 1, ["name"] = "Alice" },
                new() { ["id"] = 2, ["name"] = "Bob" }
            }
        };

        var options = new TonlOptions { Delimiter = '|' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.Contains("#delimiter |", tonl);
        Assert.NotNull(result);
        var users = result["users"] as IList<Dictionary<string, object?>>;
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Equal("Alice", users[0]["name"]);
        Assert.Equal("Bob", users[1]["name"]);
    }

    [Fact]
    public void Pipe_ValueContainingPipe_IsQuoted()
    {
        var dict = new Dictionary<string, object?>
        {
            ["cmd"] = "echo a | grep b"
        };

        var options = new TonlOptions { Delimiter = '|' };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        Assert.Contains("\"echo a | grep b\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("echo a | grep b", result!["cmd"]);
    }

    // ===========================================
    // Tab Delimiter
    // ===========================================

    [Fact]
    public void Tab_WritesDelimiterHeader()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<int> { 1, 2, 3 }
        };

        var options = new TonlOptions { Delimiter = '\t' };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        Assert.Contains("#delimiter \\t", tonl);
        Assert.Contains("\t", tonl);
    }

    [Fact]
    public void Tab_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["data"] = new List<object?> { 10, 20, 30 }
        };

        var options = new TonlOptions { Delimiter = '\t' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var data = result["data"] as IList<object?>;
        Assert.NotNull(data);
        Assert.Equal(3, data.Count);
        Assert.Equal(10, data[0]);
        Assert.Equal(30, data[2]);
    }

    [Fact]
    public void Tab_TabularArray_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["records"] = new List<Dictionary<string, object?>>
            {
                new() { ["x"] = 1, ["y"] = 2 },
                new() { ["x"] = 3, ["y"] = 4 }
            }
        };

        var options = new TonlOptions { Delimiter = '\t' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.Contains("#delimiter \\t", tonl);
        Assert.NotNull(result);
        var records = result["records"] as IList<Dictionary<string, object?>>;
        Assert.NotNull(records);
        Assert.Equal(2, records.Count);
    }

    // ===========================================
    // Semicolon Delimiter
    // ===========================================

    [Fact]
    public void Semicolon_WritesDelimiterHeader()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<int> { 1, 2, 3 }
        };

        var options = new TonlOptions { Delimiter = ';', PrettyDelimiters = true };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        Assert.Contains("#delimiter ;", tonl);
        Assert.Contains("; ", tonl);
    }

    [Fact]
    public void Semicolon_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["values"] = new List<object?> { "x", "y", "z" }
        };

        var options = new TonlOptions { Delimiter = ';' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var values = result["values"] as IList<object?>;
        Assert.NotNull(values);
        Assert.Equal(3, values.Count);
        Assert.Equal("x", values[0]);
        Assert.Equal("z", values[2]);
    }

    [Fact]
    public void Semicolon_ValueContainingSemicolon_IsQuoted()
    {
        var dict = new Dictionary<string, object?>
        {
            ["code"] = "a; b; c"
        };

        var options = new TonlOptions { Delimiter = ';' };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        Assert.Contains("\"a; b; c\"", tonl);

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.Equal("a; b; c", result!["code"]);
    }

    // ===========================================
    // Auto-Detection Tests
    // ===========================================

    [Fact]
    public void AutoDetect_Pipe_FromHeader()
    {
        var tonl = """
            #version 1.0
            #delimiter |
            root{items}:
              items[3]: a| b| c
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var items = result["items"] as IList<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        Assert.Equal("a", items[0]);
    }

    [Fact]
    public void AutoDetect_Tab_FromHeader()
    {
        // Note: Use actual tab characters in the data, but \\t in the header
        var tonl = "#version 1.0\n#delimiter \\t\nroot{items}:\n  items[3]: 1\t2\t3";

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var items = result["items"] as IList<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
    }

    [Fact]
    public void AutoDetect_Semicolon_FromHeader()
    {
        var tonl = """
            #version 1.0
            #delimiter ;
            root{a,b,c}:
              a: 1
              b: 2
              c: 3
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal(1, result["a"]);
        Assert.Equal(2, result["b"]);
        Assert.Equal(3, result["c"]);
    }

    [Fact]
    public void AutoDetect_DefaultComma_WhenNoHeader()
    {
        var tonl = """
            #version 1.0
            root{items}:
              items[3]: a, b, c
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var items = result["items"] as IList<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
    }

    // ===========================================
    // Delimiter in Data Values Tests
    // ===========================================

    [Fact]
    public void DelimiterInValue_CommaInPipeDelimited_NotQuoted()
    {
        var dict = new Dictionary<string, object?>
        {
            ["text"] = "a,b,c"
        };

        var options = new TonlOptions { Delimiter = '|' };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        // Comma doesn't need quoting when pipe is the delimiter
        Assert.Contains("text: a,b,c", tonl);
        Assert.DoesNotContain("\"a,b,c\"", tonl);
    }

    [Fact]
    public void DelimiterInValue_PipeInCommaDelimited_NotQuoted()
    {
        var dict = new Dictionary<string, object?>
        {
            ["text"] = "a|b|c"
        };

        // Default comma delimiter
        var tonl = TonlSerializer.SerializeToString(dict);

        // Pipe doesn't need quoting when comma is the delimiter
        Assert.Contains("text: a|b|c", tonl);
        Assert.DoesNotContain("\"a|b|c\"", tonl);
    }

    // ===========================================
    // Mixed Content Tests
    // ===========================================

    [Fact]
    public void MixedTypes_WithPipeDelimiter_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["name"] = "test",
            ["count"] = 42,
            ["active"] = true,
            ["tags"] = new List<object?> { "a", "b" }
        };

        var options = new TonlOptions { Delimiter = '|' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("test", result["name"]);
        Assert.Equal(42, result["count"]);
        Assert.Equal(true, result["active"]);
    }

    [Fact]
    public void ComplexNested_WithTabDelimiter_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["config"] = new Dictionary<string, object?>
            {
                ["host"] = "localhost",
                ["port"] = 8080
            }
        };

        var options = new TonlOptions { Delimiter = '\t' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var config = result["config"] as Dictionary<string, object?>;
        Assert.NotNull(config);
        Assert.Equal("localhost", config["host"]);
        Assert.Equal(8080, config["port"]);
    }
}
