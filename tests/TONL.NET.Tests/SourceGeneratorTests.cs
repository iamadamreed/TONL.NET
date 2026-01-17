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
