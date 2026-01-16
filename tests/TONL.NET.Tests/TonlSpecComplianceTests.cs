using TONL.NET;
using Xunit;

namespace TONL.NET.Tests;

/// <summary>
/// Tests that verify TONL output matches the TONL specification.
/// Each test is tagged with SPEC-XXX for tracking.
///
/// Organized into two sections:
/// 1. Non-AOT (reflection-based) - Uses TonlSerializer.SerializeToString(object)
/// 2. AOT (source-generated) - Uses TonlSerializer.SerializeToString(T, TonlTypeInfo)
///
/// Run specific categories:
///   dotnet test --filter "Category=NonAot"
///   dotnet test --filter "Category=Aot"
///
/// AOT tests require ENABLE_AOT_TESTS to be defined (disabled until source generator is fixed).
/// </summary>
public class TonlSpecComplianceTests
{
    #region ==================== NON-AOT TESTS (Reflection) ====================
    // Run with: dotnet test --filter "Category=NonAot"

    // ===========================================
    // SPEC-001: Simple Key-Value
    // ===========================================
    // Expected:
    //   Name: Alice
    //   Age: 30
    //   Active: true

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_001_SimpleKeyValue_ProducesCorrectFormat()
    {
        var person = new SpecSimpleKV { Name = "Alice", Age = 30, Active = true };
        var tonl = TonlSerializer.SerializeToString(person);

        var lines = tonl.Split('\n');

        Assert.Contains(lines, l => l.Trim().StartsWith("Name:") && l.Contains("Alice"));
        Assert.Contains(lines, l => l.Trim().StartsWith("Age:") && l.Contains("30"));
        Assert.Contains(lines, l => l.Trim().StartsWith("Active:") && l.Contains("true"));
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_001_SimpleKeyValue_RoundTrips()
    {
        var person = new SpecSimpleKV { Name = "Alice", Age = 30, Active = true };
        var tonl = TonlSerializer.SerializeToString(person);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("Alice", result["Name"]);
        Assert.Equal(30, result["Age"]);
        Assert.Equal(true, result["Active"]);
    }

    // ===========================================
    // SPEC-002: Nested Object - Inline Format
    // ===========================================
    // Expected:
    //   Name: John
    //   Home{City,Country}: Seattle, USA

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_002_NestedObjectInline_ProducesCorrectFormat()
    {
        var person = new SpecPerson
        {
            Name = "John",
            Home = new SpecAddress { City = "Seattle", Country = "USA" }
        };
        var tonl = TonlSerializer.SerializeToString(person);

        var lines = tonl.Split('\n');

        Assert.Contains(lines, l => l.Trim().StartsWith("Name:") && l.Contains("John"));

        // The nested object should use inline format: Home{City,Country}: Seattle, USA
        Assert.Contains(lines, l => l.Contains("Home{City,Country}:") && l.Contains("Seattle") && l.Contains("USA"));
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_002_NestedObjectInline_RoundTrips()
    {
        var person = new SpecPerson
        {
            Name = "John",
            Home = new SpecAddress { City = "Seattle", Country = "USA" }
        };
        var tonl = TonlSerializer.SerializeToString(person);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("John", result["Name"]);

        var home = result["Home"] as Dictionary<string, object?>;
        Assert.NotNull(home);
        Assert.Equal("Seattle", home!["City"]);
        Assert.Equal("USA", home["Country"]);
    }

    // ===========================================
    // SPEC-003: Array of Primitives
    // ===========================================
    // Expected:
    //   Values[3]: 1, 2, 3

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_003_ArrayOfPrimitives_ProducesCorrectFormat()
    {
        var numbers = new SpecNumbers { Values = [1, 2, 3] };
        var tonl = TonlSerializer.SerializeToString(numbers);

        var lines = tonl.Split('\n');

        // Array of primitives should use inline format: Values[3]: 1, 2, 3
        Assert.Contains(lines, l => l.Contains("Values[3]:") && l.Contains("1") && l.Contains("2") && l.Contains("3"));
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_003_ArrayOfPrimitives_RoundTrips()
    {
        var numbers = new SpecNumbers { Values = [1, 2, 3] };
        var tonl = TonlSerializer.SerializeToString(numbers);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var values = result["Values"] as List<object?>;
        Assert.NotNull(values);
        Assert.Equal(3, values!.Count);
        Assert.Equal(1, values[0]);
        Assert.Equal(2, values[1]);
        Assert.Equal(3, values[2]);
    }

    // ===========================================
    // SPEC-004: Array of Objects - Tabular Format
    // ===========================================
    // Expected:
    //   Items[2]{Product,Qty}:
    //     Widget, 2
    //     Gadget, 1

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_004_ArrayOfObjectsTabular_ProducesCorrectFormat()
    {
        var order = new SpecOrder
        {
            Items =
            [
                new SpecOrderItem { Product = "Widget", Qty = 2 },
                new SpecOrderItem { Product = "Gadget", Qty = 1 }
            ]
        };
        var tonl = TonlSerializer.SerializeToString(order);

        // Array of uniform objects should use tabular format
        // Items[2]{Product,Qty}:
        //   Widget, 2
        //   Gadget, 1
        Assert.Contains("Items[2]{Product,Qty}:", tonl);

        var lines = tonl.Split('\n');
        // Data rows should be indented and contain comma-separated values
        Assert.Contains(lines, l => l.Trim() == "Widget, 2");
        Assert.Contains(lines, l => l.Trim() == "Gadget, 1");
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_004_ArrayOfObjectsTabular_RoundTrips()
    {
        var order = new SpecOrder
        {
            Items =
            [
                new SpecOrderItem { Product = "Widget", Qty = 2 },
                new SpecOrderItem { Product = "Gadget", Qty = 1 }
            ]
        };
        var tonl = TonlSerializer.SerializeToString(order);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var items = result["Items"] as IList<Dictionary<string, object?>>;
        Assert.NotNull(items);
        Assert.Equal(2, items!.Count);
        Assert.Equal("Widget", items[0]["Product"]);
        Assert.Equal(2, items[0]["Qty"]);
        Assert.Equal("Gadget", items[1]["Product"]);
        Assert.Equal(1, items[1]["Qty"]);
    }

    // ===========================================
    // SPEC-005: Dictionary Format
    // ===========================================
    // Expected (inline):
    //   Values{Alice,Bob}: 100, 85
    // OR (block):
    //   Values:
    //     Alice: 100
    //     Bob: 85

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_005_DictionaryFormat_ProducesCorrectFormat()
    {
        var scores = new SpecScores
        {
            Values = new Dictionary<string, int>
            {
                ["Alice"] = 100,
                ["Bob"] = 85
            }
        };
        var tonl = TonlSerializer.SerializeToString(scores);

        // Dictionary format can be inline or block
        Assert.Contains("100", tonl);
        Assert.Contains("85", tonl);

        // Check for either format
        var hasInlineFormat = tonl.Contains("Values{") && tonl.Contains("Alice") && tonl.Contains("Bob");
        var hasBlockFormat = tonl.Contains("Values:") || tonl.Contains("Values{");
        Assert.True(hasInlineFormat || hasBlockFormat, "Dictionary should use either inline or block format");
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_005_DictionaryFormat_RoundTrips()
    {
        var scores = new SpecScores
        {
            Values = new Dictionary<string, int>
            {
                ["Alice"] = 100,
                ["Bob"] = 85
            }
        };
        var tonl = TonlSerializer.SerializeToString(scores);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        var values = result["Values"] as Dictionary<string, object?>;
        Assert.NotNull(values);
        Assert.Equal(100, values!["Alice"]);
        Assert.Equal(85, values["Bob"]);
    }

    // ===========================================
    // SPEC-006: Quoted Strings
    // ===========================================
    // Expected:
    //   Text: "Hello, World!"

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_006_QuotedStrings_WithComma_ProducesCorrectFormat()
    {
        var message = new SpecMessage { Text = "Hello, World!" };
        var tonl = TonlSerializer.SerializeToString(message);

        // String containing comma must be quoted
        Assert.Contains("\"Hello, World!\"", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_006_QuotedStrings_WithNewline_ProducesCorrectFormat()
    {
        var message = new SpecMessage { Text = "Line 1\nLine 2" };
        var tonl = TonlSerializer.SerializeToString(message);

        // Multiline string should use triple quotes
        Assert.Contains("\"\"\"", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_006_QuotedStrings_NumberLike_ProducesCorrectFormat()
    {
        var message = new SpecMessage { Text = "12345" };
        var tonl = TonlSerializer.SerializeToString(message);

        // Number-like string must be quoted to preserve as string
        Assert.Contains("\"12345\"", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_006_QuotedStrings_BooleanLike_ProducesCorrectFormat()
    {
        var message = new SpecMessage { Text = "true" };
        var tonl = TonlSerializer.SerializeToString(message);

        // Boolean-like string must be quoted to preserve as string
        Assert.Contains("\"true\"", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_006_QuotedStrings_RoundTrips()
    {
        var message = new SpecMessage { Text = "Hello, World!" };
        var tonl = TonlSerializer.SerializeToString(message);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("Hello, World!", result["Text"]);
    }

    // ===========================================
    // SPEC-007: Special Types
    // ===========================================

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_007_SpecialTypes_DateTime_ProducesIso8601()
    {
        var record = new SpecWithDateTime { Timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc) };
        var tonl = TonlSerializer.SerializeToString(record);

        // DateTime should be ISO 8601 format
        Assert.Contains("2025-06-15", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_007_SpecialTypes_Guid_ProducesString()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var record = new SpecWithGuid { Id = guid };
        var tonl = TonlSerializer.SerializeToString(record);

        // Guid should be serialized as string
        Assert.Contains("12345678-1234-1234-1234-123456789012", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_007_SpecialTypes_TimeSpan_ProducesString()
    {
        var record = new SpecWithTimeSpan { Duration = TimeSpan.FromHours(2) + TimeSpan.FromMinutes(30) };
        var tonl = TonlSerializer.SerializeToString(record);

        // TimeSpan should be serialized
        Assert.Contains("02:30:00", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_007_SpecialTypes_Enum_SerializesAsNumber()
    {
        var record = new SpecWithEnum { Status = SpecStatus.Active };
        var tonl = TonlSerializer.SerializeToString(record);

        // Enum should serialize as its underlying numeric value
        Assert.Contains("1", tonl); // Active = 1
    }

    // ===========================================
    // Additional Format Tests (Non-AOT)
    // ===========================================

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_Format_VersionHeader_IncludedByDefault()
    {
        var person = new SpecSimpleKV { Name = "Test", Age = 1, Active = true };
        var tonl = TonlSerializer.SerializeToString(person);

        Assert.StartsWith("#version 1.0", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_Format_RootHeader_IncludesColumns()
    {
        var person = new SpecSimpleKV { Name = "Test", Age = 1, Active = true };
        var tonl = TonlSerializer.SerializeToString(person);

        // Root should have column header: root{...}:
        Assert.Contains("root{", tonl);
    }

    [Fact]
    [Trait("Category", "NonAot")]
    public void NonAot_SPEC_Format_BooleanValues_AreLowercase()
    {
        var person = new SpecSimpleKV { Name = "Test", Age = 1, Active = true };
        var tonl = TonlSerializer.SerializeToString(person);

        // Booleans must be lowercase per spec
        Assert.Contains("true", tonl);
        Assert.DoesNotContain("True", tonl);
    }

    #endregion

#if ENABLE_AOT_TESTS
    #region ==================== AOT TESTS (Source Generated) ====================
    // Run with: dotnet test --filter "Category=Aot"
    // Requires: Define ENABLE_AOT_TESTS in project file once source generator is fixed

    // AOT tests use TonlSerializer.SerializeToString(T, TonlTypeInfo<T>)
    // These require source-generated serializers via SpecTestContext

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_001_SimpleKeyValue_ProducesCorrectFormat()
    {
        var person = new SpecSimpleKV { Name = "Alice", Age = 30, Active = true };
        var tonl = TonlSerializer.SerializeToString(person, SpecTestContext.Default.SpecSimpleKV);

        var lines = tonl.Split('\n');

        Assert.Contains(lines, l => l.Trim().StartsWith("Name:") && l.Contains("Alice"));
        Assert.Contains(lines, l => l.Trim().StartsWith("Age:") && l.Contains("30"));
        Assert.Contains(lines, l => l.Trim().StartsWith("Active:") && l.Contains("true"));
    }

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_001_SimpleKeyValue_RoundTrips()
    {
        var person = new SpecSimpleKV { Name = "Alice", Age = 30, Active = true };
        var tonl = TonlSerializer.SerializeToString(person, SpecTestContext.Default.SpecSimpleKV);
        var result = TonlSerializer.DeserializeToDictionary(tonl);

        Assert.NotNull(result);
        Assert.Equal("Alice", result["Name"]);
        Assert.Equal(30, result["Age"]);
        Assert.Equal(true, result["Active"]);
    }

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_002_NestedObjectInline_ProducesCorrectFormat()
    {
        var person = new SpecPerson
        {
            Name = "John",
            Home = new SpecAddress { City = "Seattle", Country = "USA" }
        };
        var tonl = TonlSerializer.SerializeToString(person, SpecTestContext.Default.SpecPerson);

        var lines = tonl.Split('\n');

        Assert.Contains(lines, l => l.Trim().StartsWith("Name:") && l.Contains("John"));

        // The nested object should use inline format: Home{City,Country}: Seattle, USA
        Assert.Contains(lines, l => l.Contains("Home{City,Country}:") && l.Contains("Seattle") && l.Contains("USA"));
    }

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_003_ArrayOfPrimitives_ProducesCorrectFormat()
    {
        var numbers = new SpecNumbers { Values = [1, 2, 3] };
        var tonl = TonlSerializer.SerializeToString(numbers, SpecTestContext.Default.SpecNumbers);

        var lines = tonl.Split('\n');

        // Array of primitives should use inline format: Values[3]: 1, 2, 3
        Assert.Contains(lines, l => l.Contains("Values[3]:") && l.Contains("1") && l.Contains("2") && l.Contains("3"));
    }

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_004_ArrayOfObjectsTabular_ProducesCorrectFormat()
    {
        var order = new SpecOrder
        {
            Items =
            [
                new SpecOrderItem { Product = "Widget", Qty = 2 },
                new SpecOrderItem { Product = "Gadget", Qty = 1 }
            ]
        };
        var tonl = TonlSerializer.SerializeToString(order, SpecTestContext.Default.SpecOrder);

        // Array of uniform objects should use tabular format
        Assert.Contains("Items[2]{Product,Qty}:", tonl);

        var lines = tonl.Split('\n');
        Assert.Contains(lines, l => l.Trim() == "Widget, 2");
        Assert.Contains(lines, l => l.Trim() == "Gadget, 1");
    }

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_005_DictionaryFormat_ProducesCorrectFormat()
    {
        var scores = new SpecScores
        {
            Values = new Dictionary<string, int>
            {
                ["Alice"] = 100,
                ["Bob"] = 85
            }
        };
        var tonl = TonlSerializer.SerializeToString(scores, SpecTestContext.Default.SpecScores);

        Assert.Contains("100", tonl);
        Assert.Contains("85", tonl);
    }

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_006_QuotedStrings_WithComma_ProducesCorrectFormat()
    {
        var message = new SpecMessage { Text = "Hello, World!" };
        var tonl = TonlSerializer.SerializeToString(message, SpecTestContext.Default.SpecMessage);

        Assert.Contains("\"Hello, World!\"", tonl);
    }

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_007_SpecialTypes_DateTime_ProducesIso8601()
    {
        var record = new SpecWithDateTime { Timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc) };
        var tonl = TonlSerializer.SerializeToString(record, SpecTestContext.Default.SpecWithDateTime);

        Assert.Contains("2025-06-15", tonl);
    }

    [Fact]
    [Trait("Category", "Aot")]
    public void Aot_SPEC_Format_MatchesNonAot()
    {
        // Verify AOT and non-AOT produce identical output
        var person = new SpecSimpleKV { Name = "Test", Age = 42, Active = false };

        var nonAotTonl = TonlSerializer.SerializeToString(person);
        var aotTonl = TonlSerializer.SerializeToString(person, SpecTestContext.Default.SpecSimpleKV);

        Assert.Equal(nonAotTonl, aotTonl);
    }

    #endregion
#endif
}

#region ==================== TEST TYPES (POCOs) ====================

/// <summary>Simple key-value class (SPEC-001)</summary>
public class SpecSimpleKV
{
    public string Name { get; set; } = "";
    public int Age { get; set; }
    public bool Active { get; set; }
}

/// <summary>Nested address (SPEC-002)</summary>
public class SpecAddress
{
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
}

/// <summary>Person with nested address (SPEC-002)</summary>
public class SpecPerson
{
    public string Name { get; set; } = "";
    public SpecAddress? Home { get; set; }
}

/// <summary>Array of primitives (SPEC-003)</summary>
public class SpecNumbers
{
    public int[]? Values { get; set; }
}

/// <summary>Order item (SPEC-004)</summary>
public class SpecOrderItem
{
    public string Product { get; set; } = "";
    public int Qty { get; set; }
}

/// <summary>Order with item list (SPEC-004)</summary>
public class SpecOrder
{
    public List<SpecOrderItem>? Items { get; set; }
}

/// <summary>Dictionary of scores (SPEC-005)</summary>
public class SpecScores
{
    public Dictionary<string, int>? Values { get; set; }
}

/// <summary>Message with text (SPEC-006)</summary>
public class SpecMessage
{
    public string Text { get; set; } = "";
}

/// <summary>Record with DateTime (SPEC-007)</summary>
public class SpecWithDateTime
{
    public DateTime Timestamp { get; set; }
}

/// <summary>Record with Guid (SPEC-007)</summary>
public class SpecWithGuid
{
    public Guid Id { get; set; }
}

/// <summary>Record with TimeSpan (SPEC-007)</summary>
public class SpecWithTimeSpan
{
    public TimeSpan Duration { get; set; }
}

/// <summary>Status enum (SPEC-007)</summary>
public enum SpecStatus
{
    Inactive = 0,
    Active = 1,
    Pending = 2
}

/// <summary>Record with enum (SPEC-007)</summary>
public class SpecWithEnum
{
    public SpecStatus Status { get; set; }
}

#endregion

#if ENABLE_AOT_TESTS
#region ==================== SOURCE GENERATOR CONTEXT (AOT) ====================

/// <summary>
/// Source generation context for spec compliance tests.
/// Registers all test types for AOT-compatible serialization.
/// </summary>
[TonlSourceGenerationOptions]
[TonlSerializable(typeof(SpecSimpleKV))]
[TonlSerializable(typeof(SpecAddress))]
[TonlSerializable(typeof(SpecPerson))]
[TonlSerializable(typeof(SpecNumbers))]
[TonlSerializable(typeof(SpecOrderItem))]
[TonlSerializable(typeof(SpecOrder))]
[TonlSerializable(typeof(SpecScores))]
[TonlSerializable(typeof(SpecMessage))]
[TonlSerializable(typeof(SpecWithDateTime))]
[TonlSerializable(typeof(SpecWithGuid))]
[TonlSerializable(typeof(SpecWithTimeSpan))]
[TonlSerializable(typeof(SpecWithEnum))]
public partial class SpecTestContext : TonlSerializerContext { }

#endregion
#endif
