using TONL.NET;
using TONL.NET.Tests.Generated;
using Xunit;

namespace TONL.NET.Tests;

/// <summary>
/// Generated <c>Deserialize(Dictionary)</c> must tolerate absent keys. Since v1.2.1 a missing
/// tabular cell is omitted from the decoded dictionary rather than stored as null.
/// </summary>
public class GeneratedMissingKeyTests
{
    [Fact]
    public void GeneratedRecord_MissingKeys_UsesNullOrDefault()
    {
        var result = RecordWithNullablesTonlSerializer.Deserialize(new Dictionary<string, object?>());

        Assert.Null(result.OptionalName);
        Assert.Null(result.OptionalValue);
    }

    [Fact]
    public void GeneratedRecord_PartialKeys_DoesNotThrow()
    {
        var result = SimpleRecordTonlSerializer.Deserialize(new Dictionary<string, object?> { ["Id"] = 5 });

        Assert.Equal(5, result.Id);
        Assert.False(result.IsActive);
    }

    [Fact]
    public void GeneratedPoco_MissingKeys_DoesNotThrow()
    {
        var result = SimplePocoTonlSerializer.Deserialize(new Dictionary<string, object?> { ["Age"] = 30 });

        Assert.Equal(30, result.Age);
        Assert.Equal(0d, result.Score);
    }

    [Fact]
    public void GeneratedPoco_FromTabularRowWithMissingCell_DoesNotThrow()
    {
        const string tonl = "items[2]{Age,Email,Score}:\n  30, a@b.c, 1.5\n  40, , 2.5\n";

        var rows = Assert.IsType<List<Dictionary<string, object?>>>(TonlSerializer.DeserializeToDictionary(tonl)!["items"]);
        var second = SimplePocoTonlSerializer.Deserialize(rows[1]);

        Assert.Equal(40, second.Age);
        Assert.Equal(2.5, second.Score);
    }
}
