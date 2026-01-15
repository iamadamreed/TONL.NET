using Xunit;

namespace Tonl.Tests;

public class RoundTripTests
{
    public record SimpleObject(string Name, int Age, bool Active);

    public record User(int Id, string Name, string Role);

    public record UsersContainer(List<User> Users);

    public record NestedObject(string Name, NestedChild Child);

    public record NestedChild(int Value, string Text);

    [Fact]
    public void RoundTrip_SimpleObject()
    {
        var original = new SimpleObject("Alice", 30, true);

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.Deserialize<SimpleObject>(tonl);

        Assert.NotNull(result);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.Age, result.Age);
        Assert.Equal(original.Active, result.Active);
    }

    [Fact]
    public void RoundTrip_Dictionary()
    {
        var original = new Dictionary<string, object?>
        {
            ["name"] = "Alice",
            ["age"] = 30,
            ["active"] = true
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("Alice", result["name"]);
        Assert.Equal(30, result["age"]);
        Assert.Equal(true, result["active"]);
    }

    [Fact]
    public void Serialize_SimpleObject_ContainsExpectedElements()
    {
        var obj = new SimpleObject("Alice", 30, true);

        var tonl = TonlSerializer.SerializeToString(obj);

        Assert.Contains("#version 1.0", tonl);
        Assert.Contains("root{", tonl);
        Assert.Contains("Active", tonl);
        Assert.Contains("Age", tonl);
        Assert.Contains("Name", tonl);
    }

    [Fact]
    public void Serialize_Null_WritesNullLiteral()
    {
        var dict = new Dictionary<string, object?>
        {
            ["value"] = null
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("null", tonl);
    }

    [Fact]
    public void Serialize_PrimitiveArray_WritesInlineFormat()
    {
        var dict = new Dictionary<string, object?>
        {
            ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("numbers[5]:", tonl);
        Assert.Contains("1", tonl);
        Assert.Contains("5", tonl);
    }

    [Fact]
    public void Serialize_StringWithComma_QuotesValue()
    {
        var dict = new Dictionary<string, object?>
        {
            ["text"] = "Hello, world"
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"Hello, world\"", tonl);
    }

    [Fact]
    public void Serialize_BooleanStrings_QuotesValue()
    {
        var dict = new Dictionary<string, object?>
        {
            ["text"] = "true"
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"true\"", tonl);
    }

    [Fact]
    public void Serialize_NumberStrings_QuotesValue()
    {
        var dict = new Dictionary<string, object?>
        {
            ["text"] = "123"
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"123\"", tonl);
    }

    [Fact]
    public void Serialize_EmptyArray_WritesEmptyArrayHeader()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<int>()
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("items[0]:", tonl);
    }

    [Fact]
    public void Serialize_SpecialNumbers_WritesLiterals()
    {
        var dict = new Dictionary<string, object?>
        {
            ["inf"] = double.PositiveInfinity,
            ["negInf"] = double.NegativeInfinity,
            ["nan"] = double.NaN
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("Infinity", tonl);
        Assert.Contains("-Infinity", tonl);
        Assert.Contains("NaN", tonl);
    }

    [Fact]
    public void Serialize_WithCustomDelimiter_UsesDelimiter()
    {
        var dict = new Dictionary<string, object?>
        {
            ["numbers"] = new List<int> { 1, 2, 3 }
        };

        var options = new TonlOptions { Delimiter = '|', PrettyDelimiters = true };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        Assert.Contains("#delimiter |", tonl);
        Assert.Contains("| ", tonl);
    }

    [Fact]
    public void Serialize_CircularReference_ThrowsException()
    {
        var obj = new Dictionary<string, object?>();
        obj["self"] = obj;

        Assert.Throws<TonlCircularReferenceException>(() =>
            TonlSerializer.SerializeToString(obj));
    }

    [Fact]
    public void Deserialize_SimpleKeyValue()
    {
        var tonl = """
            #version 1.0
            root{name,age}:
              name: Alice
              age: 30
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("Alice", result["name"]);
        Assert.Equal(30, result["age"]);
    }

    [Fact]
    public void Deserialize_NullValue()
    {
        var tonl = """
            #version 1.0
            root{value}:
              value: null
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Null(result["value"]);
    }

    [Fact]
    public void Deserialize_BooleanValues()
    {
        var tonl = """
            #version 1.0
            root{active,disabled}:
              active: true
              disabled: false
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal(true, result["active"]);
        Assert.Equal(false, result["disabled"]);
    }

    [Fact]
    public void Deserialize_QuotedString()
    {
        var tonl = """
            #version 1.0
            root{text}:
              text: "Hello, world"
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("Hello, world", result["text"]);
    }

    [Fact]
    public void Deserialize_QuotedBooleanString()
    {
        var tonl = """
            #version 1.0
            root{text}:
              text: "true"
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("true", result["text"]); // String, not boolean
    }

    [Fact]
    public void Deserialize_SpecialNumbers()
    {
        var tonl = """
            #version 1.0
            root{inf,negInf,nan}:
              inf: Infinity
              negInf: -Infinity
              nan: NaN
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal(double.PositiveInfinity, result["inf"]);
        Assert.Equal(double.NegativeInfinity, result["negInf"]);
        Assert.True(result["nan"] is double d && double.IsNaN(d));
    }

    // Quoted Keys Tests

    [Fact]
    public void Serialize_SpecialCharacterKey_QuotesKey()
    {
        var dict = new Dictionary<string, object?>
        {
            ["@type"] = "User",
            ["field-name"] = "value"
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("\"@type\"", tonl);
        Assert.Contains("\"field-name\"", tonl);
    }

    [Fact]
    public void Deserialize_QuotedKeys()
    {
        var tonl = """
            #version 1.0
            root{"@type","field-name"}:
              "@type": User
              "field-name": value
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("User", result["@type"]);
        Assert.Equal("value", result["field-name"]);
    }

    [Fact]
    public void RoundTrip_SpecialCharacterKeys()
    {
        var original = new Dictionary<string, object?>
        {
            ["@type"] = "User",
            ["field-name"] = "test",
            ["key.with.dots"] = 42
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("User", result["@type"]);
        Assert.Equal("test", result["field-name"]);
        Assert.Equal(42, result["key.with.dots"]);
    }

    // Mixed Array Tests

    [Fact]
    public void Serialize_MixedArray_UsesIndexedFormat()
    {
        // Mixed array with different types (including object) uses indexed format
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                "string",
                new Dictionary<string, object?> { ["name"] = "test" },
                42
            }
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("items[3]:", tonl);
        Assert.Contains("[0]:", tonl);
        Assert.Contains("[1]", tonl);
        Assert.Contains("[2]:", tonl);
    }

    [Fact]
    public void Serialize_AllPrimitiveArray_UsesInlineFormat()
    {
        // Array of only primitives uses inline format (more compact)
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { "string", 42, true }
        };

        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("items[3]:", tonl);
        // Inline format doesn't have indexed headers
        Assert.DoesNotContain("[0]:", tonl);
    }

    [Fact]
    public void Deserialize_IndexedMixedArray()
    {
        var tonl = """
            #version 1.0
            root{items}:
              items[3]:
                [0]: hello
                [1]: 42
                [2]: true
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.True(result.ContainsKey("items"));
        var items = result["items"] as List<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        Assert.Equal("hello", items[0]);
        Assert.Equal(42, items[1]);
        Assert.Equal(true, items[2]);
    }

    [Fact]
    public void RoundTrip_MixedArray()
    {
        var original = new Dictionary<string, object?>
        {
            ["mixed"] = new List<object?> { "text", 123, false, null }
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var mixed = result["mixed"] as List<object?>;
        Assert.NotNull(mixed);
        Assert.Equal(4, mixed.Count);
        Assert.Equal("text", mixed[0]);
        Assert.Equal(123, mixed[1]);
        Assert.Equal(false, mixed[2]);
        Assert.Null(mixed[3]);
    }

    // Delimiter Tests

    [Fact]
    public void Serialize_WithPipeDelimiter_WritesDelimiterHeader()
    {
        var dict = new Dictionary<string, object?>
        {
            ["numbers"] = new List<int> { 1, 2, 3 }
        };

        var options = new TonlOptions { Delimiter = '|', PrettyDelimiters = true };
        var tonl = TonlSerializer.SerializeToString(dict, options);

        Assert.Contains("#delimiter |", tonl);
        Assert.Contains("| ", tonl); // Values separated by pipe with space
    }

    [Fact]
    public void RoundTrip_WithPipeDelimiter()
    {
        var original = new Dictionary<string, object?>
        {
            ["a"] = 1,
            ["b"] = 2,
            ["c"] = 3
        };

        var options = new TonlOptions { Delimiter = '|' };
        var tonl = TonlSerializer.SerializeToString(original, options);
        var result = TonlSerializer.DeserializeToDictionary(tonl, options);

        Assert.NotNull(result);
        Assert.Equal(1, result["a"]);
        Assert.Equal(2, result["b"]);
        Assert.Equal(3, result["c"]);
    }

    // Dictionary Array Tests (Tabular Format)

    [Fact]
    public void Serialize_DictionaryArray_UsesTabularFormat()
    {
        var users = new List<Dictionary<string, object?>>
        {
            new() { ["id"] = 1, ["name"] = "Alice" },
            new() { ["id"] = 2, ["name"] = "Bob" }
        };

        var tonl = TonlSerializer.SerializeToString(users);

        // Should use tabular format: root[2]{id,name}:
        Assert.Contains("{id,name}:", tonl);
        Assert.DoesNotContain("[0]{", tonl); // No indexed format
        Assert.DoesNotContain("[1]{", tonl);
    }

    [Fact]
    public void Serialize_MixedKeyDictionaries_UsesIndexedFormat()
    {
        var items = new List<Dictionary<string, object?>>
        {
            new() { ["id"] = 1, ["name"] = "Alice" },
            new() { ["id"] = 2, ["email"] = "bob@test.com" } // Different keys!
        };

        var tonl = TonlSerializer.SerializeToString(items);

        // Should use indexed format since keys differ
        Assert.Contains("[0]{", tonl);
        Assert.Contains("[1]{", tonl);
    }

    [Fact]
    public void RoundTrip_DictionaryArray()
    {
        var original = new Dictionary<string, object?>
        {
            ["users"] = new List<Dictionary<string, object?>>
            {
                new() { ["id"] = 1, ["name"] = "Alice", ["active"] = true },
                new() { ["id"] = 2, ["name"] = "Bob", ["active"] = false }
            }
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var users = result["users"] as IList<Dictionary<string, object?>>;
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);

        var user1 = users[0];
        Assert.NotNull(user1);
        Assert.Equal(1, user1["id"]);
        Assert.Equal("Alice", user1["name"]);
        Assert.Equal(true, user1["active"]);

        var user2 = users[1];
        Assert.NotNull(user2);
        Assert.Equal(2, user2["id"]);
        Assert.Equal("Bob", user2["name"]);
        Assert.Equal(false, user2["active"]);
    }

    [Fact]
    public void Serialize_DictionaryArray_IsSmallerThanIndexed()
    {
        var users = new List<Dictionary<string, object?>>
        {
            new() { ["id"] = 1, ["name"] = "Alice", ["email"] = "alice@example.com" },
            new() { ["id"] = 2, ["name"] = "Bob", ["email"] = "bob@example.com" }
        };

        var tonl = TonlSerializer.SerializeToString(users);

        // Tabular format should NOT repeat keys for each item
        // Count occurrences of "id:" - should appear in header only, not as "id:" key-value lines
        var idKeyValueCount = tonl.Split("  id:").Length - 1;
        Assert.Equal(0, idKeyValueCount); // No "  id:" lines (indented key-value format)
    }

    [Fact]
    public void Serialize_DictionaryWithNestedValue_UsesIndexedFormat()
    {
        // Dictionaries with non-primitive values should fall back to indexed format
        var items = new List<Dictionary<string, object?>>
        {
            new() { ["id"] = 1, ["data"] = new Dictionary<string, object?> { ["nested"] = true } },
            new() { ["id"] = 2, ["data"] = new Dictionary<string, object?> { ["nested"] = false } }
        };

        var tonl = TonlSerializer.SerializeToString(items);

        // Should use indexed format since values are not all primitives
        Assert.Contains("[0]{", tonl);
        Assert.Contains("[1]{", tonl);
    }
}
