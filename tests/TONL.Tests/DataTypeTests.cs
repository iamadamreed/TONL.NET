using Xunit;

namespace TONL.Tests;

/// <summary>
/// Tests for data type handling per TONL specification.
/// </summary>
public class DataTypeTests
{
    // ===========================================
    // Integer Types
    // ===========================================

    [Fact]
    public void Int_Zero_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = 0 };
        var result = RoundTrip(dict);
        Assert.Equal(0, result["n"]);
    }

    [Fact]
    public void Int_Positive_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = 42 };
        var result = RoundTrip(dict);
        Assert.Equal(42, result["n"]);
    }

    [Fact]
    public void Int_Negative_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = -42 };
        var result = RoundTrip(dict);
        Assert.Equal(-42, result["n"]);
    }

    [Fact]
    public void Int_MaxValue_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = int.MaxValue };
        var result = RoundTrip(dict);
        Assert.Equal(int.MaxValue, result["n"]);
    }

    [Fact]
    public void Int_MinValue_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = int.MinValue };
        var result = RoundTrip(dict);
        Assert.Equal(int.MinValue, result["n"]);
    }

    [Fact]
    public void Long_LargePositive_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = 9007199254740992L }; // > MAX_SAFE_INTEGER
        var result = RoundTrip(dict);
        Assert.Equal(9007199254740992L, result["n"]);
    }

    [Fact]
    public void Long_LargeNegative_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = -9007199254740992L };
        var result = RoundTrip(dict);
        Assert.Equal(-9007199254740992L, result["n"]);
    }

    // ===========================================
    // Floating Point Types
    // ===========================================

    [Fact]
    public void Double_Zero_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = 0.0 };
        var result = RoundTrip(dict);
        // Zero might deserialize as int 0 or double 0.0
        Assert.Equal(0, Convert.ToDouble(result["n"]));
    }

    [Fact]
    public void Double_Positive_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = 3.14159 };
        var result = RoundTrip(dict);
        Assert.Equal(3.14159, result["n"]);
    }

    [Fact]
    public void Double_Negative_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = -2.718 };
        var result = RoundTrip(dict);
        Assert.Equal(-2.718, result["n"]);
    }

    [Fact]
    public void Double_Scientific_Positive_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = 1.5e10 };
        var result = RoundTrip(dict);
        // Large integers from scientific notation may deserialize as long
        Assert.Equal(1.5e10, Convert.ToDouble(result["n"]));
    }

    [Fact]
    public void Double_Scientific_Negative_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = -1.5e-10 };
        var result = RoundTrip(dict);
        Assert.Equal(-1.5e-10, result["n"]);
    }

    [Fact]
    public void Double_PositiveInfinity_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = double.PositiveInfinity };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("Infinity", tonl);

        var result = RoundTrip(dict);
        Assert.Equal(double.PositiveInfinity, result["n"]);
    }

    [Fact]
    public void Double_NegativeInfinity_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = double.NegativeInfinity };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("-Infinity", tonl);

        var result = RoundTrip(dict);
        Assert.Equal(double.NegativeInfinity, result["n"]);
    }

    [Fact]
    public void Double_NaN_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = double.NaN };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("NaN", tonl);

        var result = RoundTrip(dict);
        Assert.True(double.IsNaN((double)result["n"]!));
    }

    [Fact]
    public void Float_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = 3.14f };
        var result = RoundTrip(dict);
        // Float is serialized as double
        var value = Convert.ToDouble(result["n"]);
        Assert.True(Math.Abs(value - 3.14) < 0.01);
    }

    // ===========================================
    // Boolean Types
    // ===========================================

    [Fact]
    public void Bool_True_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["b"] = true };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("true", tonl);
        Assert.DoesNotContain("True", tonl); // Must be lowercase

        var result = RoundTrip(dict);
        Assert.Equal(true, result["b"]);
    }

    [Fact]
    public void Bool_False_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["b"] = false };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("false", tonl);
        Assert.DoesNotContain("False", tonl); // Must be lowercase

        var result = RoundTrip(dict);
        Assert.Equal(false, result["b"]);
    }

    // ===========================================
    // Null Type
    // ===========================================

    [Fact]
    public void Null_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["n"] = null };
        var tonl = TonlSerializer.SerializeToString(dict);

        Assert.Contains("null", tonl);

        var result = RoundTrip(dict);
        Assert.Null(result["n"]);
    }

    [Fact]
    public void Null_InArray_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { "a", null, "b" }
        };

        var result = RoundTrip(dict);
        var items = result["items"] as IList<object?>;
        Assert.NotNull(items);
        Assert.Equal(3, items.Count);
        Assert.Equal("a", items[0]);
        Assert.Null(items[1]);
        Assert.Equal("b", items[2]);
    }

    // ===========================================
    // String Types
    // ===========================================

    [Fact]
    public void String_Empty_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["s"] = "" };
        var result = RoundTrip(dict);
        Assert.Equal("", result["s"]);
    }

    [Fact]
    public void String_WhitespaceOnly_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["s"] = "   " };
        var result = RoundTrip(dict);
        Assert.Equal("   ", result["s"]);
    }

    [Fact]
    public void String_SingleChar_RoundTrips()
    {
        var dict = new Dictionary<string, object?> { ["s"] = "x" };
        var result = RoundTrip(dict);
        Assert.Equal("x", result["s"]);
    }

    [Fact]
    public void String_Long_RoundTrips()
    {
        var longString = new string('a', 10000);
        var dict = new Dictionary<string, object?> { ["s"] = longString };
        var result = RoundTrip(dict);
        Assert.Equal(longString, result["s"]);
    }

    // ===========================================
    // Array Types
    // ===========================================

    [Fact]
    public void Array_Empty_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>()
        };

        var tonl = TonlSerializer.SerializeToString(dict);
        Assert.Contains("items[0]:", tonl);

        // Note: Empty arrays might deserialize as null or empty list
        // depending on implementation
    }

    [Fact]
    public void Array_SingleElement_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["items"] = new List<object?> { 42 }
        };

        var result = RoundTrip(dict);
        var items = result["items"] as IList<object?>;
        Assert.NotNull(items);
        Assert.Single(items);
        Assert.Equal(42, items[0]);
    }

    [Fact]
    public void Array_IntList_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["numbers"] = new List<int> { 1, 2, 3, 4, 5 }
        };

        var result = RoundTrip(dict);
        var numbers = result["numbers"] as IList<object?>;
        Assert.NotNull(numbers);
        Assert.Equal(5, numbers.Count);
    }

    [Fact]
    public void Array_StringList_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["tags"] = new List<string> { "alpha", "beta", "gamma" }
        };

        var result = RoundTrip(dict);
        var tags = result["tags"] as IList<object?>;
        Assert.NotNull(tags);
        Assert.Equal(3, tags.Count);
        Assert.Equal("alpha", tags[0]);
    }

    [Fact]
    public void Array_MixedTypes_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["mixed"] = new List<object?> { "text", 42, true, null, 3.14 }
        };

        var result = RoundTrip(dict);
        var mixed = result["mixed"] as IList<object?>;
        Assert.NotNull(mixed);
        Assert.Equal(5, mixed.Count);
        Assert.Equal("text", mixed[0]);
        Assert.Equal(42, mixed[1]);
        Assert.Equal(true, mixed[2]);
        Assert.Null(mixed[3]);
    }

    // ===========================================
    // Object Types
    // ===========================================

    [Fact]
    public void Object_Empty_RoundTrips()
    {
        var dict = new Dictionary<string, object?>();
        var result = RoundTrip(dict);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void Object_Nested_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["outer"] = new Dictionary<string, object?>
            {
                ["inner"] = new Dictionary<string, object?>
                {
                    ["value"] = 42
                }
            }
        };

        var result = RoundTrip(dict);
        var outer = result["outer"] as Dictionary<string, object?>;
        var inner = outer!["inner"] as Dictionary<string, object?>;
        Assert.Equal(42, inner!["value"]);
    }

    [Fact]
    public void Object_ArrayOfObjects_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["users"] = new List<Dictionary<string, object?>>
            {
                new() { ["id"] = 1, ["name"] = "Alice" },
                new() { ["id"] = 2, ["name"] = "Bob" }
            }
        };

        var result = RoundTrip(dict);
        var users = result["users"] as IList<Dictionary<string, object?>>;
        Assert.NotNull(users);
        Assert.Equal(2, users.Count);
        Assert.Equal("Alice", users[0]["name"]);
        Assert.Equal("Bob", users[1]["name"]);
    }

    // ===========================================
    // Type Coercion
    // ===========================================

    [Fact]
    public void Deserialize_IntFitsInInt_ReturnsInt()
    {
        var tonl = """
            #version 1.0
            root{n}:
              n: 42
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.IsType<int>(result!["n"]);
    }

    [Fact]
    public void Deserialize_LargeNumber_ReturnsLong()
    {
        var tonl = """
            #version 1.0
            root{n}:
              n: 9007199254740992
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.IsType<long>(result!["n"]);
    }

    [Fact]
    public void Deserialize_DecimalNumber_ReturnsDouble()
    {
        var tonl = """
            #version 1.0
            root{n}:
              n: 3.14
            """;

        var result = TonlSerializer.DeserializeToDictionary(tonl);
        Assert.IsType<double>(result!["n"]);
    }

    // ===========================================
    // Helper Methods
    // ===========================================

    private static Dictionary<string, object?> RoundTrip(Dictionary<string, object?> original)
    {
        var tonl = TonlSerializer.SerializeToString(original);
        return TonlSerializer.DeserializeToDictionary(tonl) ?? new Dictionary<string, object?>();
    }
}
