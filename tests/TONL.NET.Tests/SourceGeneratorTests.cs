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
    public void GeneratedSerializer_TypesWithEnum_SerializesToLong()
    {
        var original = new RecordWithEnum(Status.Active);

        var dict = RecordWithEnumTonlSerializer.Serialize(original);

        // Enums are serialized as long to safely handle all underlying types
        Assert.Equal(Convert.ToInt64(Status.Active), dict["CurrentStatus"]);
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

// Context-based pattern test types
public record ContextTestRecord(int Id, string Name, bool Active);

public class ContextTestPoco
{
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

[TonlSourceGenerationOptions]
[TonlSerializable(typeof(ContextTestRecord))]
[TonlSerializable(typeof(ContextTestPoco))]
public partial class TestTonlContext : TonlSerializerContext { }

/// <summary>
/// Tests for the new context-based serialization pattern (STJ-like).
/// </summary>
public class ContextBasedSerializerTests
{
    [Fact]
    public void Context_Default_ReturnsSingletonInstance()
    {
        var ctx1 = TestTonlContext.Default;
        var ctx2 = TestTonlContext.Default;

        Assert.Same(ctx1, ctx2);
    }

    [Fact]
    public void Context_GetTypeInfo_ReturnsCorrectTypeInfo()
    {
        var typeInfo = TestTonlContext.Default.GetTypeInfo(typeof(ContextTestRecord));

        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(ContextTestRecord), typeInfo.Type);
    }

    [Fact]
    public void Context_GetTypeInfo_ReturnsNullForUnregisteredType()
    {
        var typeInfo = TestTonlContext.Default.GetTypeInfo(typeof(string));

        Assert.Null(typeInfo);
    }

    [Fact]
    public void Context_TypeInfoProperty_ReturnsCorrectTypeInfo()
    {
        var typeInfo = TestTonlContext.Default.ContextTestRecord;

        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(ContextTestRecord), typeInfo.Type);
        Assert.NotNull(typeInfo.Serialize);
    }

    [Fact]
    public void Context_SerializeRecord_ProducesValidTonl()
    {
        var record = new ContextTestRecord(42, "Test", true);

        var tonl = TonlSerializer.SerializeToString(record, TestTonlContext.Default.ContextTestRecord);

        Assert.Contains("42", tonl);
        Assert.Contains("Test", tonl);
        Assert.Contains("true", tonl);
    }

    [Fact]
    public void Context_SerializePoco_ProducesValidTonl()
    {
        var poco = new ContextTestPoco { Age = 25, Email = "test@example.com" };

        var tonl = TonlSerializer.SerializeToString(poco, TestTonlContext.Default.ContextTestPoco);

        Assert.Contains("25", tonl);
        Assert.Contains("test@example.com", tonl);
    }

    [Fact]
    public void Context_SerializeToBytes_Works()
    {
        var record = new ContextTestRecord(1, "Bytes Test", false);

        var bytes = TonlSerializer.SerializeToBytes(record, TestTonlContext.Default.ContextTestRecord);

        Assert.NotEmpty(bytes);
        var str = System.Text.Encoding.UTF8.GetString(bytes);
        Assert.Contains("Bytes Test", str);
    }

    [Fact]
    public void Context_TypeInfoHasOriginatingContext()
    {
        var typeInfo = TestTonlContext.Default.ContextTestRecord;

        Assert.Same(TestTonlContext.Default, typeInfo.OriginatingContext);
    }

    [Fact]
    public void Context_PocoTypeInfo_HasCreateObject()
    {
        var typeInfo = TestTonlContext.Default.ContextTestPoco;

        Assert.NotNull(typeInfo.CreateObject);
        var instance = typeInfo.CreateObject();
        Assert.NotNull(instance);
        Assert.IsType<ContextTestPoco>(instance);
    }

    [Fact]
    public void Context_RecordTypeInfo_DoesNotHaveCreateObject()
    {
        // Records require constructor parameters, so CreateObject is not generated
        var typeInfo = TestTonlContext.Default.ContextTestRecord;

        Assert.Null(typeInfo.CreateObject);
    }

    [Fact]
    public void Context_TypeInfo_HasProperties()
    {
        var typeInfo = TestTonlContext.Default.ContextTestRecord;

        Assert.NotNull(typeInfo.Properties);
        Assert.Equal(3, typeInfo.Properties.Count);
        Assert.Contains(typeInfo.Properties, p => p.Name == "Id");
        Assert.Contains(typeInfo.Properties, p => p.Name == "Name");
        Assert.Contains(typeInfo.Properties, p => p.Name == "Active");
    }
}

// =============================================================================
// Test types for edge case scenarios
// =============================================================================

/// <summary>
/// Interface type - should NOT generate CreateObject or type info property.
/// </summary>
public interface ITestInterface
{
    int Id { get; }
    string Name { get; }
}

/// <summary>
/// Abstract class - should NOT generate CreateObject.
/// </summary>
public abstract class AbstractTestClass
{
    public int Id { get; set; }
    public abstract string Name { get; }
}

/// <summary>
/// Concrete implementation of abstract class.
/// </summary>
public class ConcreteTestClass : AbstractTestClass
{
    public override string Name { get; } = "Concrete";
}

/// <summary>
/// Class with init-only properties - should NOT generate SetValue for init props.
/// </summary>
public class InitOnlyPropsClass
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MutableValue { get; set; }
}

/// <summary>
/// Class with required properties (C# 11+).
/// </summary>
public class RequiredPropsClass
{
    public required int Id { get; set; }
    public required string Name { get; set; }
    public int OptionalValue { get; set; }
}

/// <summary>
/// Class without parameterless constructor.
/// </summary>
public class NoParameterlessCtorClass
{
    public int Id { get; }
    public string Name { get; }

    public NoParameterlessCtorClass(int id, string name)
    {
        Id = id;
        Name = name;
    }
}

// Context that includes edge case types
[TonlSourceGenerationOptions]
[TonlSerializable(typeof(ITestInterface))]           // Interface - should be filtered/handled
[TonlSerializable(typeof(AbstractTestClass))]        // Abstract - no CreateObject
[TonlSerializable(typeof(ConcreteTestClass))]        // Concrete subclass
[TonlSerializable(typeof(InitOnlyPropsClass))]       // Init-only props - no SetValue
[TonlSerializable(typeof(RequiredPropsClass))]       // Required props - no SetValue
[TonlSerializable(typeof(NoParameterlessCtorClass))] // No parameterless ctor - no CreateObject
[TonlSerializable(typeof(string))]                   // Primitive - should be filtered
[TonlSerializable(typeof(int))]                      // Primitive - should be filtered
[TonlSerializable(typeof(DateTime))]                 // Framework type - should be filtered
public partial class EdgeCaseTestContext : TonlSerializerContext { }

/// <summary>
/// Tests for edge cases: interfaces, abstract classes, init-only properties, primitives.
/// </summary>
public class SourceGeneratorEdgeCaseTests
{
    [Fact]
    public void Context_PrimitiveTypes_NotGenerated()
    {
        // Primitive types (string, int, DateTime) should return null from GetTypeInfo
        // because they are filtered out during generation
        Assert.Null(EdgeCaseTestContext.Default.GetTypeInfo(typeof(string)));
        Assert.Null(EdgeCaseTestContext.Default.GetTypeInfo(typeof(int)));
        Assert.Null(EdgeCaseTestContext.Default.GetTypeInfo(typeof(DateTime)));
    }

    [Fact]
    public void Context_InterfaceType_NoCreateObject()
    {
        // Interface types can have type info for serialization (useful when you
        // have a reference typed as the interface), but should not have CreateObject
        var typeInfo = EdgeCaseTestContext.Default.ITestInterface;

        Assert.NotNull(typeInfo);
        Assert.Null(typeInfo.CreateObject);
    }

    [Fact]
    public void Context_AbstractClass_NoCreateObject()
    {
        var typeInfo = EdgeCaseTestContext.Default.AbstractTestClass;

        Assert.NotNull(typeInfo);
        // Abstract classes should not have CreateObject
        Assert.Null(typeInfo.CreateObject);
    }

    [Fact]
    public void Context_ConcreteClass_HasCreateObject()
    {
        var typeInfo = EdgeCaseTestContext.Default.ConcreteTestClass;

        Assert.NotNull(typeInfo);
        Assert.NotNull(typeInfo.CreateObject);

        var instance = typeInfo.CreateObject();
        Assert.IsType<ConcreteTestClass>(instance);
    }

    [Fact]
    public void Context_InitOnlyProps_NoSetValue()
    {
        var typeInfo = EdgeCaseTestContext.Default.InitOnlyPropsClass;

        Assert.NotNull(typeInfo);
        Assert.NotNull(typeInfo.Properties);

        // Init-only properties should NOT have SetValue
        var idProp = typeInfo.Properties.FirstOrDefault(p => p.Name == "Id");
        var nameProp = typeInfo.Properties.FirstOrDefault(p => p.Name == "Name");
        var mutableProp = typeInfo.Properties.FirstOrDefault(p => p.Name == "MutableValue");

        Assert.NotNull(idProp);
        Assert.NotNull(nameProp);
        Assert.NotNull(mutableProp);

        // Init-only properties should NOT have SetValue
        Assert.Null(idProp!.SetValue);
        Assert.Null(nameProp!.SetValue);

        // Mutable property SHOULD have SetValue
        Assert.NotNull(mutableProp!.SetValue);
    }

    [Fact]
    public void Context_RequiredProps_NoSetValue()
    {
        var typeInfo = EdgeCaseTestContext.Default.RequiredPropsClass;

        Assert.NotNull(typeInfo);
        Assert.NotNull(typeInfo.Properties);

        // Required properties should NOT have SetValue (they must be set via initializer)
        var idProp = typeInfo.Properties.FirstOrDefault(p => p.Name == "Id");
        var nameProp = typeInfo.Properties.FirstOrDefault(p => p.Name == "Name");
        var optionalProp = typeInfo.Properties.FirstOrDefault(p => p.Name == "OptionalValue");

        Assert.NotNull(idProp);
        Assert.NotNull(nameProp);
        Assert.NotNull(optionalProp);

        // Required properties should NOT have SetValue
        Assert.Null(idProp!.SetValue);
        Assert.Null(nameProp!.SetValue);

        // Optional property SHOULD have SetValue
        Assert.NotNull(optionalProp!.SetValue);
    }

    [Fact]
    public void Context_NoParameterlessCtor_NoCreateObject()
    {
        var typeInfo = EdgeCaseTestContext.Default.NoParameterlessCtorClass;

        Assert.NotNull(typeInfo);
        // Types without parameterless constructor should NOT have CreateObject
        Assert.Null(typeInfo.CreateObject);
    }

    [Fact]
    public void Context_InitOnlyProps_CanSerialize()
    {
        // Even without SetValue, serialization should work
        var obj = new InitOnlyPropsClass { Id = 42, Name = "Test", MutableValue = 100 };

        var tonl = TonlSerializer.SerializeToString(obj, EdgeCaseTestContext.Default.InitOnlyPropsClass);

        Assert.Contains("42", tonl);
        Assert.Contains("Test", tonl);
        Assert.Contains("100", tonl);
    }

    [Fact]
    public void Context_RequiredProps_CanSerialize()
    {
        var obj = new RequiredPropsClass { Id = 1, Name = "Required", OptionalValue = 50 };

        var tonl = TonlSerializer.SerializeToString(obj, EdgeCaseTestContext.Default.RequiredPropsClass);

        Assert.Contains("1", tonl);
        Assert.Contains("Required", tonl);
        Assert.Contains("50", tonl);
    }

    [Fact]
    public void Context_NoParameterlessCtor_CanSerialize()
    {
        var obj = new NoParameterlessCtorClass(99, "NoCtor");

        var tonl = TonlSerializer.SerializeToString(obj, EdgeCaseTestContext.Default.NoParameterlessCtorClass);

        Assert.Contains("99", tonl);
        Assert.Contains("NoCtor", tonl);
    }
}

// =============================================================================
// Test types for type argument discovery (auto-discovering generic type parameters)
// =============================================================================

/// <summary>
/// Element type that should be auto-discovered from wrapper record.
/// </summary>
public class DiscoveredElement
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
}

/// <summary>
/// Element type used by multiple wrapper types to test de-duplication.
/// </summary>
public class SharedElement
{
    public int Id { get; set; }
    public string Description { get; set; } = "";
}

/// <summary>
/// Wrapper that contains List&lt;DiscoveredElement&gt; as a property.
/// Only this wrapper is registered - DiscoveredElement should be auto-discovered.
/// Using record to match AOT test pattern that works.
/// </summary>
public record DiscoveredElementWrapper(List<DiscoveredElement> Items);

/// <summary>
/// Wrapper that contains List&lt;SharedElement&gt; as a property.
/// </summary>
public record SharedElementListWrapper(List<SharedElement> Items);

/// <summary>
/// Wrapper that contains Dictionary&lt;string, SharedElement&gt; as a property.
/// </summary>
public record SharedElementDictWrapper(Dictionary<string, SharedElement> Data);

/// <summary>
/// Context that only registers DiscoveredElementWrapper, NOT DiscoveredElement directly.
/// The source generator should auto-discover DiscoveredElement from the generic type argument.
/// </summary>
[TonlSourceGenerationOptions]
[TonlSerializable(typeof(DiscoveredElementWrapper))]
public partial class TypeArgumentDiscoveryContext : TonlSerializerContext { }

/// <summary>
/// Context that registers multiple wrappers with the same element type.
/// Tests that de-duplication works correctly (SharedElement should only be discovered once).
/// </summary>
[TonlSourceGenerationOptions]
[TonlSerializable(typeof(SharedElementListWrapper))]
[TonlSerializable(typeof(SharedElementDictWrapper))]
public partial class DeduplicationTestContext : TonlSerializerContext { }

/// <summary>
/// Tests for auto-discovery of types from generic type arguments.
/// </summary>
public class TypeArgumentDiscoveryTests
{
    [Fact]
    public void GenericTypeArguments_AreAutoDiscovered()
    {
        // TypeArgumentDiscoveryContext only registers DiscoveredElementWrapper
        // but DiscoveredElement should be auto-discovered from List<DiscoveredElement> property

        var element = new DiscoveredElement { Name = "Test", Value = 42 };
        var wrapper = new DiscoveredElementWrapper([element]);

        var tonl = TonlSerializer.SerializeToString(wrapper, TypeArgumentDiscoveryContext.Default.DiscoveredElementWrapper);

        // Verify the serialized output contains the element data (not type names)
        Assert.Contains("Test", tonl);
        Assert.Contains("42", tonl);
        Assert.DoesNotContain("DiscoveredElement", tonl); // Should not contain type name as value
    }

    [Fact]
    public void GenericTypeArguments_DiscoveredElementHasTypeInfo()
    {
        // DiscoveredElement should be registered even though it wasn't explicitly listed
        // Access via the typed property accessor which includes Properties
        var typeInfo = TypeArgumentDiscoveryContext.Default.DiscoveredElement;

        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(DiscoveredElement), typeInfo.Type);
        Assert.NotNull(typeInfo.Properties);
        Assert.Equal(2, typeInfo.Properties.Count);
    }

    [Fact]
    public void GenericTypeArguments_DeduplicationWorks()
    {
        // DeduplicationTestContext registers wrappers for both List<SharedElement> and Dictionary<string, SharedElement>
        // SharedElement should be discovered once (not duplicated)
        // Access via typed property - if there were duplicates, compilation would fail with duplicate member names

        var typeInfo = DeduplicationTestContext.Default.SharedElement;

        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(SharedElement), typeInfo.Type);
    }

    [Fact]
    public void GenericTypeArguments_MultipleWrapperTypes_WorkCorrectly()
    {
        // Test that list wrappers work with auto-discovered element type
        var element = new SharedElement { Id = 1, Description = "Shared" };

        // Test List wrapper - verify serialization works with auto-discovered element type
        var listWrapper = new SharedElementListWrapper([element]);
        var listTonl = TonlSerializer.SerializeToString(listWrapper, DeduplicationTestContext.Default.SharedElementListWrapper);

        Assert.Contains("1", listTonl);
        Assert.Contains("Shared", listTonl);
        Assert.DoesNotContain("SharedElement", listTonl); // Should not contain type name as value
    }

    [Fact]
    public void GenericTypeArguments_DictionaryValueType_IsDiscovered()
    {
        // Verify that SharedElement is auto-discovered from Dictionary<string, SharedElement>
        // even when only the wrapper type is registered
        var typeInfo = DeduplicationTestContext.Default.SharedElement;

        Assert.NotNull(typeInfo);
        Assert.Equal(typeof(SharedElement), typeInfo.Type);
        Assert.NotNull(typeInfo.Properties);
        Assert.Equal(2, typeInfo.Properties.Count);
        Assert.Contains(typeInfo.Properties, p => p.Name == "Id");
        Assert.Contains(typeInfo.Properties, p => p.Name == "Description");
    }
}

// =============================================================================
// Test types for root-level collection serialization
// =============================================================================

/// <summary>
/// Table info for testing List of complex objects.
/// </summary>
public class TableInfo
{
    public string Name { get; set; } = "";
    public int RowCount { get; set; }
}

/// <summary>
/// Context for testing root-level collection types.
/// </summary>
[TonlSourceGenerationOptions]
[TonlSerializable(typeof(Dictionary<string, string>))]
[TonlSerializable(typeof(Dictionary<string, int>))]
[TonlSerializable(typeof(Dictionary<string, TableInfo>))]
[TonlSerializable(typeof(List<string>))]
[TonlSerializable(typeof(List<int>))]
[TonlSerializable(typeof(List<double>))]
[TonlSerializable(typeof(List<bool>))]
[TonlSerializable(typeof(List<TableInfo>))]
[TonlSerializable(typeof(string[]))]
[TonlSerializable(typeof(int[]))]
[TonlSerializable(typeof(IEnumerable<string>))]
[TonlSerializable(typeof(IEnumerable<TableInfo>))]
[TonlSerializable(typeof(TableInfo))] // Need to register element type for tabular format
public partial class RootCollectionContext : TonlSerializerContext { }

/// <summary>
/// Tests for root-level collection serialization.
/// </summary>
public class RootCollectionSerializationTests
{
    [Fact]
    public void Dictionary_RootLevel_SerializesKeyValuePairs()
    {
        var dict = new Dictionary<string, string>
        {
            ["version"] = "8.0.42",
            ["hostname"] = "server1"
        };

        var tonl = TonlSerializer.SerializeToString(dict, RootCollectionContext.Default.DictionaryOfStringAndString);

        // Should contain the key-value pairs
        Assert.Contains("version", tonl);
        Assert.Contains("8.0.42", tonl);
        Assert.Contains("hostname", tonl);
        Assert.Contains("server1", tonl);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Comparer", tonl);
        Assert.DoesNotContain("Count", tonl);
        Assert.DoesNotContain("Keys", tonl);
        Assert.DoesNotContain("Values", tonl);
    }

    [Fact]
    public void ListOfStrings_RootLevel_SerializesElements()
    {
        var list = new List<string> { "alpha", "beta", "gamma" };

        var tonl = TonlSerializer.SerializeToString(list, RootCollectionContext.Default.ListOfString);

        // Should contain the elements
        Assert.Contains("alpha", tonl);
        Assert.Contains("beta", tonl);
        Assert.Contains("gamma", tonl);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Count", tonl);
    }

    [Fact]
    public void ListOfInts_RootLevel_SerializesElements()
    {
        var list = new List<int> { 10, 20, 30 };

        var tonl = TonlSerializer.SerializeToString(list, RootCollectionContext.Default.ListOfInt32);

        // Should contain the elements
        Assert.Contains("10", tonl);
        Assert.Contains("20", tonl);
        Assert.Contains("30", tonl);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Count", tonl);
    }

    [Fact]
    public void ListOfComplexObjects_RootLevel_SerializesTabular()
    {
        var list = new List<TableInfo>
        {
            new() { Name = "Users", RowCount = 100 },
            new() { Name = "Orders", RowCount = 500 }
        };

        var tonl = TonlSerializer.SerializeToString(list, RootCollectionContext.Default.ListOfTableInfo);

        // Should contain the property values
        Assert.Contains("Users", tonl);
        Assert.Contains("100", tonl);
        Assert.Contains("Orders", tonl);
        Assert.Contains("500", tonl);

        // Should NOT contain CLR properties (Capacity: or Count: as key-value pairs)
        Assert.DoesNotContain("Capacity:", tonl);
        // Note: "RowCount" is a property name so we can't just check for "Count"
        // Instead verify it doesn't have "Count:" as a separate key
        Assert.DoesNotContain("\n  Count:", tonl);
        Assert.DoesNotContain("\n    Count:", tonl);
    }

    [Fact]
    public void EmptyList_RootLevel_DoesNotShowCapacity()
    {
        var list = new List<string>();

        var tonl = TonlSerializer.SerializeToString(list, RootCollectionContext.Default.ListOfString);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Count", tonl);
    }

    [Fact]
    public void EmptyDictionary_RootLevel_DoesNotShowCapacityOrComparer()
    {
        var dict = new Dictionary<string, string>();

        var tonl = TonlSerializer.SerializeToString(dict, RootCollectionContext.Default.DictionaryOfStringAndString);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Comparer", tonl);
        Assert.DoesNotContain("Count", tonl);
        Assert.DoesNotContain("Keys", tonl);
        Assert.DoesNotContain("Values", tonl);
    }

    [Fact]
    public void Dictionary_RootLevel_TypeInfoIsCollection()
    {
        var typeInfo = RootCollectionContext.Default.DictionaryOfStringAndString;

        Assert.True(typeInfo.IsCollection);
        Assert.True(typeInfo.IsDictionary);
        // Dictionaries don't have element property names (values are primitives)
        Assert.Null(typeInfo.CollectionElementPropertyNames);
    }

    [Fact]
    public void ListOfStrings_RootLevel_TypeInfoIsCollection()
    {
        var typeInfo = RootCollectionContext.Default.ListOfString;

        Assert.True(typeInfo.IsCollection);
        Assert.False(typeInfo.IsDictionary);
        // Primitives don't have element property names
        Assert.Null(typeInfo.CollectionElementPropertyNames);
    }

    [Fact]
    public void ListOfComplexObjects_RootLevel_TypeInfoHasElementPropertyNames()
    {
        var typeInfo = RootCollectionContext.Default.ListOfTableInfo;

        Assert.True(typeInfo.IsCollection);
        Assert.False(typeInfo.IsDictionary);
        // Should have element property names for tabular headers
        Assert.NotNull(typeInfo.CollectionElementPropertyNames);
        Assert.Contains("Name", typeInfo.CollectionElementPropertyNames);
        Assert.Contains("RowCount", typeInfo.CollectionElementPropertyNames);
    }

    [Fact]
    public void DictionaryOfStringAndInt_RootLevel_SerializesKeyValuePairs()
    {
        var dict = new Dictionary<string, int>
        {
            ["users"] = 100,
            ["orders"] = 500
        };

        var tonl = TonlSerializer.SerializeToString(dict, RootCollectionContext.Default.DictionaryOfStringAndInt32);

        // Should contain the key-value pairs
        Assert.Contains("users", tonl);
        Assert.Contains("100", tonl);
        Assert.Contains("orders", tonl);
        Assert.Contains("500", tonl);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Comparer", tonl);
    }

    [Fact]
    public void DictionaryOfComplexObjects_RootLevel_SerializesKeyValuePairs()
    {
        var dict = new Dictionary<string, TableInfo>
        {
            ["primary"] = new TableInfo { Name = "Users", RowCount = 100 },
            ["secondary"] = new TableInfo { Name = "Orders", RowCount = 500 }
        };

        var tonl = TonlSerializer.SerializeToString(dict, RootCollectionContext.Default.DictionaryOfStringAndTableInfo);

        // Should contain the key-value pairs with nested object data
        Assert.Contains("primary", tonl);
        Assert.Contains("secondary", tonl);
        Assert.Contains("Users", tonl);
        Assert.Contains("Orders", tonl);
        Assert.Contains("100", tonl);
        Assert.Contains("500", tonl);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Comparer", tonl);
    }

    [Fact]
    public void ListOfDoubles_RootLevel_SerializesElements()
    {
        // Use values that have exact binary representations to avoid G17 expansion
        var list = new List<double> { 1.5, 2.5, 3.5 };

        var tonl = TonlSerializer.SerializeToString(list, RootCollectionContext.Default.ListOfDouble);

        // Should contain the elements (floating point values)
        Assert.Contains("1.5", tonl);
        Assert.Contains("2.5", tonl);
        Assert.Contains("3.5", tonl);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Count", tonl);
    }

    [Fact]
    public void ListOfBools_RootLevel_SerializesElements()
    {
        var list = new List<bool> { true, false, true };

        var tonl = TonlSerializer.SerializeToString(list, RootCollectionContext.Default.ListOfBoolean);

        // Should contain the elements
        Assert.Contains("true", tonl);
        Assert.Contains("false", tonl);

        // Should NOT contain CLR properties
        Assert.DoesNotContain("Capacity", tonl);
        Assert.DoesNotContain("Count", tonl);
    }

    [Fact]
    public void StringArray_RootLevel_SerializesElements()
    {
        var array = new string[] { "first", "second", "third" };

        var tonl = TonlSerializer.SerializeToString(array, RootCollectionContext.Default.StringArray);

        // Should contain the elements
        Assert.Contains("first", tonl);
        Assert.Contains("second", tonl);
        Assert.Contains("third", tonl);

        // Arrays don't have Capacity property, but verify no Count/Length appears as key
        Assert.DoesNotContain("Length:", tonl);
    }

    [Fact]
    public void IntArray_RootLevel_SerializesElements()
    {
        var array = new int[] { 1, 2, 3 };

        var tonl = TonlSerializer.SerializeToString(array, RootCollectionContext.Default.Int32Array);

        // Should contain the elements
        Assert.Contains("1", tonl);
        Assert.Contains("2", tonl);
        Assert.Contains("3", tonl);

        // Arrays don't have Capacity property, but verify no Count/Length appears as key
        Assert.DoesNotContain("Length:", tonl);
    }

    [Fact]
    public void Dictionary_RootLevel_TypeInfo_HasEmptyProperties()
    {
        var typeInfo = RootCollectionContext.Default.DictionaryOfStringAndString;

        // Collection types should have empty Properties (not CLR properties)
        Assert.NotNull(typeInfo.Properties);
        Assert.Empty(typeInfo.Properties);
    }

    [Fact]
    public void List_RootLevel_TypeInfo_HasEmptyProperties()
    {
        var typeInfo = RootCollectionContext.Default.ListOfString;

        // Collection types should have empty Properties (not CLR properties)
        Assert.NotNull(typeInfo.Properties);
        Assert.Empty(typeInfo.Properties);
    }

    [Fact]
    public void Array_RootLevel_TypeInfo_IsCollection()
    {
        var typeInfo = RootCollectionContext.Default.StringArray;

        Assert.True(typeInfo.IsCollection);
        Assert.False(typeInfo.IsDictionary);
    }

    [Fact]
    public void DictionaryOfComplexObjects_RootLevel_TypeInfo_IsCollectionAndDictionary()
    {
        var typeInfo = RootCollectionContext.Default.DictionaryOfStringAndTableInfo;

        Assert.True(typeInfo.IsCollection);
        Assert.True(typeInfo.IsDictionary);
        // Dictionary values are complex objects, so should have element property names
        Assert.NotNull(typeInfo.CollectionElementPropertyNames);
        Assert.Contains("Name", typeInfo.CollectionElementPropertyNames);
        Assert.Contains("RowCount", typeInfo.CollectionElementPropertyNames);
    }

    [Fact]
    public void IEnumerableOfStrings_RootLevel_SerializesElements()
    {
        IEnumerable<string> enumerable = new List<string> { "one", "two", "three" };

        var tonl = TonlSerializer.SerializeToString(enumerable, RootCollectionContext.Default.IEnumerableOfString);

        // Should contain the elements
        Assert.Contains("one", tonl);
        Assert.Contains("two", tonl);
        Assert.Contains("three", tonl);

        // Should NOT contain CLR interface/type info
        Assert.DoesNotContain("IEnumerable", tonl);
    }

    [Fact]
    public void IEnumerableOfComplexObjects_RootLevel_SerializesTabular()
    {
        IEnumerable<TableInfo> enumerable = new List<TableInfo>
        {
            new() { Name = "Table1", RowCount = 50 },
            new() { Name = "Table2", RowCount = 75 }
        };

        var tonl = TonlSerializer.SerializeToString(enumerable, RootCollectionContext.Default.IEnumerableOfTableInfo);

        // Should contain the property values
        Assert.Contains("Table1", tonl);
        Assert.Contains("50", tonl);
        Assert.Contains("Table2", tonl);
        Assert.Contains("75", tonl);

        // Should NOT contain CLR interface/type info
        Assert.DoesNotContain("IEnumerable", tonl);
    }

    [Fact]
    public void IEnumerableOfStrings_RootLevel_TypeInfoIsCollection()
    {
        var typeInfo = RootCollectionContext.Default.IEnumerableOfString;

        Assert.True(typeInfo.IsCollection);
        Assert.False(typeInfo.IsDictionary);
        // Primitives don't have element property names
        Assert.Null(typeInfo.CollectionElementPropertyNames);
    }

    [Fact]
    public void IEnumerableOfComplexObjects_RootLevel_TypeInfoHasElementPropertyNames()
    {
        var typeInfo = RootCollectionContext.Default.IEnumerableOfTableInfo;

        Assert.True(typeInfo.IsCollection);
        Assert.False(typeInfo.IsDictionary);
        // Should have element property names for tabular headers
        Assert.NotNull(typeInfo.CollectionElementPropertyNames);
        Assert.Contains("Name", typeInfo.CollectionElementPropertyNames);
        Assert.Contains("RowCount", typeInfo.CollectionElementPropertyNames);
    }
}
