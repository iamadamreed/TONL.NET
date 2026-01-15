using Xunit;

namespace TONL.Tests;

/// <summary>
/// Tests for the 17 mandatory spec compliance requirements from TONL Specification v2.5.2.
/// These tests ensure TONL.NET is 100% compliant with the official TONL format.
/// </summary>
public class SpecComplianceTests
{
    // ===========================================
    // 1. Empty Object
    // ===========================================
    [Fact]
    public void Spec01_EmptyObject_RoundTrips()
    {
        var original = new Dictionary<string, object?>();

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ===========================================
    // 2. Simple Object with Primitives
    // ===========================================
    [Fact]
    public void Spec02_SimpleObjectWithPrimitives_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["id"] = 1,
            ["name"] = "Alice",
            ["active"] = true
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal(1, result["id"]);
        Assert.Equal("Alice", result["name"]);
        Assert.Equal(true, result["active"]);
    }

    // ===========================================
    // 3. Empty Array
    // ===========================================
    [Fact]
    public void Spec03_EmptyArray_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>()
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Contains("items[0]:", tonl);
    }

    // ===========================================
    // 4. Primitive Array
    // ===========================================
    [Fact]
    public void Spec04_PrimitiveArray_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["tags"] = new List<object?> { "alpha", "beta", "gamma" },
            ["scores"] = new List<object?> { 95, 87, 92 }
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Contains("tags[3]:", tonl);
        Assert.Contains("scores[3]:", tonl);

        var tags = result["tags"] as IList<object?>;
        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Equal("alpha", tags[0]);
    }

    // ===========================================
    // 5. Uniform Object Array (Tabular Format)
    // ===========================================
    [Fact]
    public void Spec05_UniformObjectArray_UsesTabularFormat()
    {
        var original = new Dictionary<string, object?>
        {
            ["users"] = new List<Dictionary<string, object?>>
            {
                new() { ["id"] = 1, ["name"] = "Alice", ["role"] = "admin" },
                new() { ["id"] = 2, ["name"] = "Bob", ["role"] = "user" }
            }
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        // Verify tabular format is used (columns in header, not repeated per row)
        Assert.Contains("{id,name,role}:", tonl);
        Assert.DoesNotContain("[0]{", tonl); // Not indexed format

        Assert.NotNull(result);
        var users = result["users"] as IList<Dictionary<string, object?>>;
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Equal("Alice", users[0]["name"]);
    }

    // ===========================================
    // 6. Nested Object (Multi-Level)
    // ===========================================
    [Fact]
    public void Spec06_NestedObject_MultiLevel_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["user"] = new Dictionary<string, object?>
            {
                ["id"] = 1,
                ["contact"] = new Dictionary<string, object?>
                {
                    ["email"] = "alice@example.com",
                    ["phone"] = "+123456789"
                }
            }
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var user = result["user"] as Dictionary<string, object?>;
        Assert.NotNull(user);
        Assert.Equal(1, user["id"]);

        var contact = user["contact"] as Dictionary<string, object?>;
        Assert.NotNull(contact);
        Assert.Equal("alice@example.com", contact["email"]);
        Assert.Equal("+123456789", contact["phone"]);
    }

    // ===========================================
    // 7. Null Value
    // ===========================================
    [Fact]
    public void Spec07_NullValue_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["value"] = null,
            ["name"] = "test"
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.Contains("null", tonl);
        Assert.NotNull(result);
        Assert.Null(result["value"]);
        Assert.Equal("test", result["name"]);
    }

    // ===========================================
    // 8. Boolean Values
    // ===========================================
    [Fact]
    public void Spec08_BooleanValues_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["active"] = true,
            ["disabled"] = false
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        // Booleans must be lowercase per spec
        Assert.Contains("true", tonl);
        Assert.Contains("false", tonl);
        Assert.DoesNotContain("True", tonl);
        Assert.DoesNotContain("False", tonl);

        Assert.NotNull(result);
        Assert.Equal(true, result["active"]);
        Assert.Equal(false, result["disabled"]);
    }

    // ===========================================
    // 9. Quoted String with Comma
    // ===========================================
    [Fact]
    public void Spec09_QuotedString_WithComma_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["name"] = "Bob, Jr."
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        // Must be quoted because it contains a comma (delimiter)
        Assert.Contains("\"Bob, Jr.\"", tonl);

        Assert.NotNull(result);
        Assert.Equal("Bob, Jr.", result["name"]);
    }

    // ===========================================
    // 10. Quoted String with Quotes
    // ===========================================
    [Fact]
    public void Spec10_QuotedString_WithQuotes_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["quote"] = "He said \"Hello!\""
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        // Internal quotes must be escaped by doubling
        Assert.Contains("\"\"", tonl);

        Assert.NotNull(result);
        Assert.Equal("He said \"Hello!\"", result["quote"]);
    }

    // ===========================================
    // 11. Multiline String (Triple Quotes)
    // ===========================================
    [Fact]
    public void Spec11_MultilineString_TripleQuotes_RoundTrips()
    {
        var multiline = "Line 1\nLine 2\nLine 3";
        var original = new Dictionary<string, object?>
        {
            ["description"] = multiline
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        // Should use triple quotes for multiline content
        Assert.Contains("\"\"\"", tonl);

        Assert.NotNull(result);
        Assert.Equal(multiline, result["description"]);
    }

    // ===========================================
    // 12. Number-Like String (Must Be Quoted)
    // ===========================================
    [Fact]
    public void Spec12_NumberLikeString_MustBeQuoted_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["phone"] = "123456789",
            ["zip"] = "02134"
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        // Number-like strings must be quoted to preserve as strings
        Assert.Contains("\"123456789\"", tonl);
        Assert.Contains("\"02134\"", tonl);

        Assert.NotNull(result);
        Assert.Equal("123456789", result["phone"]);
        Assert.Equal("02134", result["zip"]);
        Assert.IsType<string>(result["phone"]);
    }

    // ===========================================
    // 13. Boolean-Like String (Must Be Quoted)
    // ===========================================
    [Fact]
    public void Spec13_BooleanLikeString_MustBeQuoted_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["status"] = "true",
            ["flag"] = "false",
            ["empty"] = "null"
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        // Boolean-like strings must be quoted to preserve as strings
        Assert.Contains("\"true\"", tonl);
        Assert.Contains("\"false\"", tonl);
        Assert.Contains("\"null\"", tonl);

        Assert.NotNull(result);
        Assert.Equal("true", result["status"]);
        Assert.IsType<string>(result["status"]);
        Assert.Equal("false", result["flag"]);
        Assert.IsType<string>(result["flag"]);
        Assert.Equal("null", result["empty"]);
        Assert.IsType<string>(result["empty"]);
    }

    // ===========================================
    // 14. Pipe Delimiter
    // ===========================================
    [Fact]
    public void Spec14_PipeDelimiter_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["data"] = new List<Dictionary<string, object?>>
            {
                new() { ["id"] = 1, ["name"] = "Alice" },
                new() { ["id"] = 2, ["name"] = "Bob" }
            }
        };

        var options = new TonlOptions { Delimiter = '|', PrettyDelimiters = true };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.Contains("#delimiter |", tonl);
        Assert.Contains("| ", tonl); // Pipe used as separator with space

        Assert.NotNull(result);
        var data = result["data"] as IList<Dictionary<string, object?>>;
        Assert.NotNull(data);
        Assert.Equal(2, data.Count);
    }

    // ===========================================
    // 15. Tab Delimiter
    // ===========================================
    [Fact]
    public void Spec15_TabDelimiter_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { "a", "b", "c" }
        };

        var options = new TonlOptions { Delimiter = '\t' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.Contains("#delimiter \\t", tonl);
        Assert.Contains("\t", tonl); // Tab used as separator

        Assert.NotNull(result);
        var items = result["items"] as IList<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
    }

    // ===========================================
    // 16. Type Hints (Optional Feature)
    // ===========================================
    [Fact]
    public void Spec16_TypeHints_ParsedCorrectly()
    {
        // Test that type hints in input are parsed (even if we don't generate them)
        var tonl = """
            #version 1.0
            root{id:u32,name:str,active:bool}:
              id: 42
              name: Alice
              active: true
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal(42, result["id"]);
        Assert.Equal("Alice", result["name"]);
        Assert.Equal(true, result["active"]);
    }

    // ===========================================
    // 17. Circular Reference (Must Throw Error)
    // ===========================================
    [Fact]
    public void Spec17_CircularReference_ThrowsError()
    {
        var obj = new Dictionary<string, object?>();
        obj["self"] = obj; // Circular reference

        var ex = Assert.Throws<TonlCircularReferenceException>(() =>
            TonlSerializer.SerializeToString(obj));

        Assert.Contains("self", ex.Message);
    }

    // ===========================================
    // Additional Spec Compliance Tests
    // ===========================================

    [Fact]
    public void Spec_VersionHeader_IncludedByDefault()
    {
        var original = new Dictionary<string, object?> { ["x"] = 1 };
        var tonl = TonlSerializer.SerializeToString(original);

        Assert.StartsWith("#version 1.0", tonl);
    }

    [Fact]
    public void Spec_SpecialNumbers_Infinity_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["posInf"] = double.PositiveInfinity,
            ["negInf"] = double.NegativeInfinity,
            ["nan"] = double.NaN
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.Contains("Infinity", tonl);
        Assert.Contains("-Infinity", tonl);
        Assert.Contains("NaN", tonl);

        Assert.NotNull(result);
        Assert.Equal(double.PositiveInfinity, result["posInf"]);
        Assert.Equal(double.NegativeInfinity, result["negInf"]);
        Assert.True(result["nan"] is double d && double.IsNaN(d));
    }

    [Fact]
    public void Spec_QuotedKeys_WithSpecialCharacters_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["@type"] = "User",
            ["field-name"] = "value",
            ["key.with.dots"] = 42
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        // Keys with special characters must be quoted
        Assert.Contains("\"@type\"", tonl);
        Assert.Contains("\"field-name\"", tonl);
        Assert.Contains("\"key.with.dots\"", tonl);

        Assert.NotNull(result);
        Assert.Equal("User", result["@type"]);
        Assert.Equal("value", result["field-name"]);
        Assert.Equal(42, result["key.with.dots"]);
    }

    [Fact]
    public void Spec_SemicolonDelimiter_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["values"] = new List<object?> { 1, 2, 3 }
        };

        var options = new TonlOptions { Delimiter = ';', PrettyDelimiters = true };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.Contains("#delimiter ;", tonl);
        Assert.Contains("; ", tonl); // Semicolon with space in pretty mode

        Assert.NotNull(result);
    }
}
