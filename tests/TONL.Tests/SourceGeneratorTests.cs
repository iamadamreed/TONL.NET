using TONL.NET;
using TONL.NET.Tests.Generated;
using Xunit;

namespace TONL.NET.Tests;

/// <summary>
/// Integration tests for the TONL source generator.
/// These tests verify that generated serializers produce correct output.
/// </summary>
public class SourceGeneratorTests
{
    [Fact]
    public void GeneratedSerializer_SimpleRecord_RoundTrips()
    {
        var original = new SimpleRecord(42, "Hello", true);

        var dict = SimpleRecordTonlSerializer.Serialize(original);
        var result = SimpleRecordTonlSerializer.Deserialize(dict);

        Assert.Equal(original.Id, result.Id);
        Assert.Equal(original.Name, result.Name);
        Assert.Equal(original.IsActive, result.IsActive);
    }

    [Fact]
    public void GeneratedSerializer_SimpleRecord_SerializeMatchesReflectionOutput()
    {
        var original = new SimpleRecord(42, "Hello", true);

        // Compare string output between generated and reflection-based serializers
        var generatedTonl = SimpleRecordTonlSerializer.SerializeToString(original);
        var reflectionTonl = TonlSerializer.SerializeToString(original);

        // Both should produce valid TONL with the same data
        // Note: order might differ, so we parse and compare
        var generatedDict = TonlSerializer.DeserializeToDictionary(generatedTonl);
        var reflectionDict = TonlSerializer.DeserializeToDictionary(reflectionTonl);

        Assert.NotNull(generatedDict);
        Assert.NotNull(reflectionDict);
        Assert.Equal(reflectionDict!.Count, generatedDict!.Count);
        foreach (var key in reflectionDict.Keys)
        {
            Assert.True(generatedDict.ContainsKey(key), $"Missing key: {key}");
            Assert.Equal(reflectionDict[key]?.ToString(), generatedDict[key]?.ToString());
        }
    }

    [Fact]
    public void GeneratedSerializer_SimplePoco_RoundTrips()
    {
        var original = new SimplePoco
        {
            Age = 25,
            Email = "test@example.com",
            Score = 99.5
        };

        var dict = SimplePocoTonlSerializer.Serialize(original);
        var result = SimplePocoTonlSerializer.Deserialize(dict);

        Assert.Equal(original.Age, result.Age);
        Assert.Equal(original.Email, result.Email);
        Assert.Equal(original.Score, result.Score);
    }

    [Fact]
    public void GeneratedSerializer_TypesWithDateTime_SerializesToIso8601()
    {
        var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var original = new RecordWithDateTime(timestamp);

        var dict = RecordWithDateTimeTonlSerializer.Serialize(original);

        Assert.Contains("2025-06-15", dict["Timestamp"]?.ToString());
    }

    [Fact]
    public void GeneratedSerializer_TypesWithGuid_SerializesToString()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var original = new RecordWithGuid(guid);

        var dict = RecordWithGuidTonlSerializer.Serialize(original);

        Assert.Equal("12345678-1234-1234-1234-123456789012", dict["Id"]);
    }

    [Fact]
    public void GeneratedSerializer_TypesWithEnum_SerializesToInt()
    {
        var original = new RecordWithEnum(Status.Active);

        var dict = RecordWithEnumTonlSerializer.Serialize(original);

        Assert.Equal((int)Status.Active, dict["CurrentStatus"]);
    }

    [Fact]
    public void GeneratedSerializer_TypesWithNullableProperties_HandlesNulls()
    {
        var original = new RecordWithNullables(null, null);

        var dict = RecordWithNullablesTonlSerializer.Serialize(original);
        var result = RecordWithNullablesTonlSerializer.Deserialize(dict);

        Assert.Null(result.OptionalName);
        Assert.Null(result.OptionalValue);
    }

    [Fact]
    public void GeneratedSerializer_Struct_SerializesWithoutBoxing()
    {
        var original = new SimpleStruct { X = 10, Y = 20 };

        var dict = SimpleStructTonlSerializer.Serialize(original);
        var result = SimpleStructTonlSerializer.Deserialize(dict);

        Assert.Equal(original.X, result.X);
        Assert.Equal(original.Y, result.Y);
    }

    [Fact]
    public void GeneratedSerializer_SerializeToString_ProducesValidTonl()
    {
        var original = new SimpleRecord(42, "Hello", true);

        var tonl = SimpleRecordTonlSerializer.SerializeToString(original);

        Assert.Contains("Id", tonl);
        Assert.Contains("42", tonl);
        Assert.Contains("Name", tonl);
        Assert.Contains("Hello", tonl);
    }

    [Fact]
    public void GeneratedSerializer_PropertyNames_MatchesTypeProperties()
    {
        Assert.Contains("Id", SimpleRecordTonlSerializer.PropertyNames);
        Assert.Contains("Name", SimpleRecordTonlSerializer.PropertyNames);
        Assert.Contains("IsActive", SimpleRecordTonlSerializer.PropertyNames);
    }
}

// Test types marked with [TonlSerializable]

[TonlSerializable]
public record SimpleRecord(int Id, string Name, bool IsActive);

[TonlSerializable]
public class SimplePoco
{
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
    public double Score { get; set; }
}

[TonlSerializable]
public record RecordWithDateTime(DateTime Timestamp);

[TonlSerializable]
public record RecordWithGuid(Guid Id);

[TonlSerializable]
public record RecordWithEnum(Status CurrentStatus);

[TonlSerializable]
public record RecordWithNullables(string? OptionalName, int? OptionalValue);

[TonlSerializable]
public struct SimpleStruct
{
    public int X { get; set; }
    public int Y { get; set; }
}

public enum Status
{
    Inactive = 0,
    Active = 1,
    Pending = 2
}
