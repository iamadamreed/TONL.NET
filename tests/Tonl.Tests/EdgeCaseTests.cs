using Xunit;

namespace Tonl.Tests;

/// <summary>
/// Tests for edge cases and boundary conditions.
/// </summary>
public class EdgeCaseTests
{
    // ===========================================
    // Deep Nesting
    // ===========================================

    [Fact]
    public void Nesting_5Levels_RoundTrips()
    {
        var dict = BuildNestedDict(5);
        var result = RoundTrip(dict);

        // Navigate to deepest value
        var current = result;
        for (int i = 0; i < 4; i++)
        {
            current = current![$"level{i}"] as Dictionary<string, object?>;
        }
        Assert.Equal(42, current!["value"]);
    }

    [Fact]
    public void Nesting_10Levels_RoundTrips()
    {
        var dict = BuildNestedDict(10);
        var result = RoundTrip(dict);

        // Navigate to deepest value
        var current = result;
        for (int i = 0; i < 9; i++)
        {
            current = current![$"level{i}"] as Dictionary<string, object?>;
        }
        Assert.Equal(42, current!["value"]);
    }

    // ===========================================
    // Large Arrays
    // ===========================================

    [Fact]
    public void Array_100Elements_RoundTrips()
    {
        var items = Enumerable.Range(0, 100).Select(i => (object?)i).ToList();
        var dict = new Dictionary<string, object?> { ["items"] = items };

        var result = RoundTrip(dict);
        var resultItems = result["items"] as IList<object?>;

        Assert.NotNull(resultItems);
        Assert.Equal(100, resultItems.Count);
        Assert.Equal(0, resultItems[0]);
        Assert.Equal(99, resultItems[99]);
    }

    [Fact]
    public void Array_1000Elements_RoundTrips()
    {
        var items = Enumerable.Range(0, 1000).Select(i => (object?)i).ToList();
        var dict = new Dictionary<string, object?> { ["items"] = items };

        var result = RoundTrip(dict);
        var resultItems = result["items"] as IList<object?>;

        Assert.NotNull(resultItems);
        Assert.Equal(1000, resultItems.Count);
    }

    // ===========================================
    // Large Objects
    // ===========================================

    [Fact]
    public void Object_100Keys_RoundTrips()
    {
        var dict = new Dictionary<string, object?>();
        for (int i = 0; i < 100; i++)
        {
            dict[$"key{i}"] = i;
        }

        var result = RoundTrip(dict);

        Assert.Equal(100, result.Count);
        Assert.Equal(0, result["key0"]);
        Assert.Equal(99, result["key99"]);
    }

    // ===========================================
    // Large Strings
    // ===========================================

    [Fact]
    public void String_1KB_RoundTrips()
    {
        var largeString = new string('x', 1024);
        var dict = new Dictionary<string, object?> { ["text"] = largeString };

        var result = RoundTrip(dict);

        Assert.Equal(largeString, result["text"]);
    }

    [Fact]
    public void String_100KB_RoundTrips()
    {
        var largeString = new string('y', 102400);
        var dict = new Dictionary<string, object?> { ["text"] = largeString };

        var result = RoundTrip(dict);

        Assert.Equal(largeString, result["text"]);
    }

    // ===========================================
    // Special String Values
    // ===========================================

    [Fact]
    public void String_AllPrintableAscii_RoundTrips()
    {
        var allPrintable = string.Concat(Enumerable.Range(32, 95).Select(i => (char)i));
        var dict = new Dictionary<string, object?> { ["chars"] = allPrintable };

        var result = RoundTrip(dict);

        Assert.Equal(allPrintable, result["chars"]);
    }

    [Fact]
    public void String_BinaryData_RoundTrips()
    {
        var binary = "\x00\x01\x02\x03\xff\xfe";
        var dict = new Dictionary<string, object?> { ["data"] = binary };

        var result = RoundTrip(dict);

        Assert.Equal(binary, result["data"]);
    }

    // ===========================================
    // Boundary Numbers
    // ===========================================

    [Fact]
    public void Number_MaxSafeInteger_RoundTrips()
    {
        // JavaScript MAX_SAFE_INTEGER
        var maxSafe = 9007199254740991L;
        var dict = new Dictionary<string, object?> { ["n"] = maxSafe };

        var result = RoundTrip(dict);

        Assert.Equal(maxSafe, result["n"]);
    }

    [Fact]
    public void Number_MinSafeInteger_RoundTrips()
    {
        // JavaScript MIN_SAFE_INTEGER
        var minSafe = -9007199254740991L;
        var dict = new Dictionary<string, object?> { ["n"] = minSafe };

        var result = RoundTrip(dict);

        Assert.Equal(minSafe, result["n"]);
    }

    [Fact]
    public void Number_VerySmallDecimal_RoundTrips()
    {
        var small = 1e-300;
        var dict = new Dictionary<string, object?> { ["n"] = small };

        var result = RoundTrip(dict);

        Assert.Equal(small, Convert.ToDouble(result["n"]));
    }

    [Fact]
    public void Number_VeryLargeDecimal_RoundTrips()
    {
        var large = 1e300;
        var dict = new Dictionary<string, object?> { ["n"] = large };

        var result = RoundTrip(dict);

        Assert.Equal(large, Convert.ToDouble(result["n"]));
    }

    // ===========================================
    // Empty/Null Edge Cases
    // ===========================================

    [Fact]
    public void EmptyInput_ReturnsEmptyDict()
    {
        var dict = new Dictionary<string, object?>();
        var result = RoundTrip(dict);
        Assert.Empty(result);
    }

    [Fact]
    public void AllNullValues_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["a"] = null,
            ["b"] = null,
            ["c"] = null
        };

        var result = RoundTrip(dict);

        Assert.Equal(3, result.Count);
        Assert.Null(result["a"]);
        Assert.Null(result["b"]);
        Assert.Null(result["c"]);
    }

    // ===========================================
    // Unicode Edge Cases
    // ===========================================

    [Fact]
    public void Unicode_Emoji_RoundTrips()
    {
        var emoji = "\U0001F600\U0001F601\U0001F602"; // Multiple emoji
        var dict = new Dictionary<string, object?> { ["text"] = emoji };

        var result = RoundTrip(dict);

        Assert.Equal(emoji, result["text"]);
    }

    [Fact]
    public void Unicode_MixedScripts_RoundTrips()
    {
        var mixed = "English \u4e2d\u6587 \u0420\u0443\u0441\u0441\u043a\u0438\u0439";
        var dict = new Dictionary<string, object?> { ["text"] = mixed };

        var result = RoundTrip(dict);

        Assert.Equal(mixed, result["text"]);
    }

    [Fact]
    public void Unicode_RightToLeft_RoundTrips()
    {
        var arabic = "\u0645\u0631\u062d\u0628\u0627"; // "Hello" in Arabic
        var dict = new Dictionary<string, object?> { ["text"] = arabic };

        var result = RoundTrip(dict);

        Assert.Equal(arabic, result["text"]);
    }

    // ===========================================
    // Complex Structures
    // ===========================================

    [Fact]
    public void ComplexStructure_MixedNesting_RoundTrips()
    {
        var dict = new Dictionary<string, object?>
        {
            ["metadata"] = new Dictionary<string, object?>
            {
                ["version"] = "1.0",
                ["tags"] = new List<object?> { "a", "b", "c" }
            },
            ["data"] = new List<Dictionary<string, object?>>
            {
                new() { ["id"] = 1, ["values"] = new List<object?> { 10, 20 } },
                new() { ["id"] = 2, ["values"] = new List<object?> { 30, 40 } }
            }
        };

        var result = RoundTrip(dict);

        Assert.NotNull(result);
        var metadata = result["metadata"] as Dictionary<string, object?>;
        Assert.Equal("1.0", metadata!["version"]);
    }

    // ===========================================
    // Helper Methods
    // ===========================================

    private static Dictionary<string, object?> BuildNestedDict(int levels)
    {
        var root = new Dictionary<string, object?>();
        var current = root;

        for (int i = 0; i < levels - 1; i++)
        {
            var next = new Dictionary<string, object?>();
            current[$"level{i}"] = next;
            current = next;
        }

        current["value"] = 42;
        return root;
    }

    private static Dictionary<string, object?> RoundTrip(Dictionary<string, object?> original)
    {
        var tonl = TonlSerializer.SerializeToString(original);
        return TonlSerializer.DeserializeToDictionary(tonl) ?? new Dictionary<string, object?>();
    }
}
