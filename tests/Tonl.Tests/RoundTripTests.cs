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

        var options = new TonlOptions { Delimiter = '|' };
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
}
