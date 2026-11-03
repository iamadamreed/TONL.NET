using TONL.NET;
using Xunit;

namespace TONL.NET.Tests;

/// <summary>
/// Spec §"Missing Fields in Tabular Format": an unquoted empty cell is a missing field,
/// a quoted <c>""</c> is an explicit empty string. Mirrors upstream tonl-dev/tonl 865391e (BUG-A).
/// </summary>
public class TabularEmptyStringTests
{
    public class Item
    {
        public string? A { get; set; }
        public string? B { get; set; }
        public string? C { get; set; }
    }

    public class Container
    {
        public List<Item> Items { get; set; } = new();
    }

    private static List<Dictionary<string, object?>> Items(Dictionary<string, object?> doc) =>
        Assert.IsType<List<Dictionary<string, object?>>>(doc["items"]);

    [Fact]
    public void Dictionary_EmptyStringInTabular_RoundTrips()
    {
        var original = new Dictionary<string, object?>
        {
            ["items"] = new List<object?>
            {
                new Dictionary<string, object?> { ["a"] = "", ["b"] = "x" },
                new Dictionary<string, object?> { ["a"] = "y", ["b"] = "" },
            }
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var items = Items(TonlSerializer.DeserializeToDictionary(tonl)!);

        var first = items[0];
        var second = items[1];
        Assert.True(first.ContainsKey("a"));
        Assert.Equal("", first["a"]);
        Assert.Equal("x", first["b"]);
        Assert.Equal("y", second["a"]);
        Assert.True(second.ContainsKey("b"));
        Assert.Equal("", second["b"]);
    }

    [Fact]
    public void Typed_EmptyStringInTabular_RoundTrips()
    {
        var original = new Container
        {
            Items =
            {
                new Item { A = "", B = "mid", C = "" },
                new Item { A = "x", B = "", C = "z" },
            }
        };

        var tonl = TonlSerializer.SerializeToString(original);
        var result = TonlSerializer.Deserialize<Container>(tonl)!;

        Assert.Equal("", result.Items[0].A);
        Assert.Equal("mid", result.Items[0].B);
        Assert.Equal("", result.Items[0].C);
        Assert.Equal("x", result.Items[1].A);
        Assert.Equal("", result.Items[1].B);
        Assert.Equal("z", result.Items[1].C);
    }

    [Fact]
    public void Wire_DistinguishesEmptyStringMissingAndNull()
    {
        const string tonl = "items[3]{a,b,c}:\n  \"\", null, z\n  x, , z\n  , q, w\n";

        var items = Items(TonlSerializer.DeserializeToDictionary(tonl)!);

        var r0 = items[0];
        Assert.Equal("", r0["a"]);
        Assert.True(r0.ContainsKey("b"));
        Assert.Null(r0["b"]);
        Assert.Equal("z", r0["c"]);

        var r1 = items[1];
        Assert.Equal("x", r1["a"]);
        Assert.False(r1.ContainsKey("b"));
        Assert.Equal("z", r1["c"]);

        var r2 = items[2];
        Assert.False(r2.ContainsKey("a"));
        Assert.Equal("q", r2["b"]);
        Assert.Equal("w", r2["c"]);
    }

    [Fact]
    public void Wire_TrailingMissingField_IsOmitted()
    {
        const string tonl = "items[1]{a,b}:\n  x,\n";

        var items = Items(TonlSerializer.DeserializeToDictionary(tonl)!);
        var r0 = items[0];

        Assert.Equal("x", r0["a"]);
        Assert.False(r0.ContainsKey("b"));
    }

    [Fact]
    public void Wire_TypedQuotedEmptyVersusMissing()
    {
        const string tonl = "items[2]{A,B,C}:\n  \"\", , z\n  x, \"\", \n";

        var result = TonlSerializer.Deserialize<Container>(tonl)!;

        Assert.Equal("", result.Items[0].A);
        Assert.Null(result.Items[0].B);
        Assert.Equal("z", result.Items[0].C);
        Assert.Equal("x", result.Items[1].A);
        Assert.Equal("", result.Items[1].B);
        Assert.Null(result.Items[1].C);
    }
}

public class TabularMissingFieldDefaultsTests
{
    public class Row
    {
        public string? A { get; set; }
        public string B { get; set; } = "default";
        public int N { get; set; } = 7;
    }

    public class Holder
    {
        public List<Row> Items { get; set; } = new();
    }

    [Fact]
    public void Typed_MissingCell_KeepsPropertyDefault()
    {
        const string tonl = "items[1]{A,B,N}:\n  x, , \n";

        var result = TonlSerializer.Deserialize<Holder>(tonl)!;

        Assert.Equal("x", result.Items[0].A);
        Assert.Equal("default", result.Items[0].B);
        Assert.Equal(7, result.Items[0].N);
    }
}
