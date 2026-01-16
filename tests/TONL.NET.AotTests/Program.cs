using TONL.NET;

// ============================================================
// Test Types
// ============================================================

// Simple record
public record TestRecord(int Id, string Name, bool IsActive);

// ============================================================
// Collection and Nested Object Test Types
// ============================================================

// TimeSpan (missing AOT coverage)
public record TimeSpanRecord(TimeSpan Duration);

// Collections of primitives
public record CollectionRecord(List<int> Numbers, string[] Tags);

// Dictionary
public record DictionaryRecord(Dictionary<string, int> Scores);

// Nested object
public record Address(string City, string Country);
public record PersonRecord(string Name, Address HomeAddress);

// List of objects
public record OrderItem(string Product, int Qty, decimal Price);
public record OrderRecord(int OrderId, List<OrderItem> Items);

// IEnumerable (important interface case)
public record EnumerableRecord(IEnumerable<string> Values);

// Concurrent collection
public record ConcurrentRecord(System.Collections.Concurrent.ConcurrentDictionary<string, int> ThreadSafeScores);

// Simple POCO with settable properties
public class TestPoco
{
    public int Age { get; set; }
    public string Email { get; set; } = string.Empty;
}

// Record with all primitive types
public record AllPrimitivesRecord(
    bool BoolValue,
    byte ByteValue,
    sbyte SByteValue,
    short ShortValue,
    ushort UShortValue,
    int IntValue,
    uint UIntValue,
    long LongValue,
    ulong ULongValue,
    float FloatValue,
    double DoubleValue,
    decimal DecimalValue,
    char CharValue,
    string StringValue);

// Record with special types
public record SpecialTypesRecord(
    DateTime DateTimeValue,
    Guid GuidValue,
    Status EnumValue);

// Record with nullable types
public record NullableTypesRecord(
    int? NullableInt,
    bool? NullableBool,
    DateTime? NullableDateTime,
    string? NullableString);

// Simple struct
public struct TestStruct
{
    public int X { get; set; }
    public int Y { get; set; }
}

// Large record with many properties
public record LargeRecord(
    int Field1, string Field2, int Field3, string Field4, int Field5,
    string Field6, int Field7, string Field8, int Field9, string Field10,
    int Field11, string Field12, int Field13, string Field14, int Field15,
    string Field16, int Field17, string Field18, int Field19, string Field20);

public enum Status
{
    Inactive = 0,
    Active = 1,
    Pending = 2
}

// ============================================================
// Context class with source generation
// ============================================================
[TonlSourceGenerationOptions]
[TonlSerializable(typeof(TestRecord))]
[TonlSerializable(typeof(TestPoco))]
[TonlSerializable(typeof(AllPrimitivesRecord))]
[TonlSerializable(typeof(SpecialTypesRecord))]
[TonlSerializable(typeof(NullableTypesRecord))]
[TonlSerializable(typeof(TestStruct))]
[TonlSerializable(typeof(LargeRecord))]
// New types for collection/object coverage
[TonlSerializable(typeof(TimeSpanRecord))]
[TonlSerializable(typeof(CollectionRecord))]
[TonlSerializable(typeof(DictionaryRecord))]
[TonlSerializable(typeof(Address))]
[TonlSerializable(typeof(PersonRecord))]
[TonlSerializable(typeof(OrderItem))]
[TonlSerializable(typeof(OrderRecord))]
[TonlSerializable(typeof(EnumerableRecord))]
[TonlSerializable(typeof(ConcurrentRecord))]
public partial class AotTestContext : TonlSerializerContext { }

// ============================================================
// Test Runner
// ============================================================
public class Program
{
    private static int _testCount = 0;
    private static int _passCount = 0;

    public static int Main()
    {
        try
        {
            Console.WriteLine("Running AOT Serialization Tests...\n");

            // Basic tests
            TestSimpleRecord();
            TestSimplePoco();
            TestSimpleStruct();

            // Type coverage tests
            TestAllPrimitives();
            TestSpecialTypes();
            TestNullableTypes();
            TestNullableTypesWithValues();

            // Large type test
            TestLargeRecord();

            // New tests for collection/object coverage
            TestTimeSpan();
            TestCollections();
            TestDictionary();
            TestNestedObject();
            TestListOfObjects();
            TestEnumerable();
            TestConcurrentCollection();

            // Context API tests
            TestContextGetTypeInfo();
            TestContextSingleton();
            TestSerializeToBytes();

            // Print summary
            Console.WriteLine($"\n{'=',-50}");
            Console.WriteLine($"Results: {_passCount}/{_testCount} tests passed");

            if (_passCount == _testCount)
            {
                Console.WriteLine("All AOT tests passed!");
                return 0;
            }
            else
            {
                Console.WriteLine($"FAILED: {_testCount - _passCount} tests failed");
                return 1;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FATAL ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    private static void TestSimpleRecord()
    {
        var record = new TestRecord(42, "Test User", true);
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.TestRecord);

        Assert("SimpleRecord serialization",
            tonl.Contains("42") && tonl.Contains("Test User") && tonl.Contains("true"));
    }

    private static void TestSimplePoco()
    {
        var poco = new TestPoco { Age = 25, Email = "test@example.com" };
        var tonl = TonlSerializer.SerializeToString(poco, AotTestContext.Default.TestPoco);

        Assert("SimplePoco serialization",
            tonl.Contains("25") && tonl.Contains("test@example.com"));
    }

    private static void TestSimpleStruct()
    {
        var s = new TestStruct { X = 10, Y = 20 };
        var tonl = TonlSerializer.SerializeToString(s, AotTestContext.Default.TestStruct);

        Assert("SimpleStruct serialization",
            tonl.Contains("10") && tonl.Contains("20"));
    }

    private static void TestAllPrimitives()
    {
        var record = new AllPrimitivesRecord(
            BoolValue: true,
            ByteValue: 255,
            SByteValue: -128,
            ShortValue: -32768,
            UShortValue: 65535,
            IntValue: -2147483648,
            UIntValue: 4294967295,
            LongValue: -9223372036854775808,
            ULongValue: 18446744073709551615,
            FloatValue: 3.14f,
            DoubleValue: 2.718281828,
            DecimalValue: 99999.99999m,
            CharValue: 'X',
            StringValue: "Hello, World!");

        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.AllPrimitivesRecord);

        Assert("AllPrimitives - bool", tonl.Contains("true"));
        Assert("AllPrimitives - byte", tonl.Contains("255"));
        Assert("AllPrimitives - int", tonl.Contains("-2147483648"));
        Assert("AllPrimitives - float", tonl.Contains("3.14")); // FloatValue
        Assert("AllPrimitives - double", tonl.Contains("2.718")); // DoubleValue - formatting may vary
        Assert("AllPrimitives - decimal", tonl.Contains("99999")); // DecimalValue
        Assert("AllPrimitives - char", tonl.Contains("X")); // CharValue
        Assert("AllPrimitives - string", tonl.Contains("Hello, World!"));
    }

    private static void TestSpecialTypes()
    {
        var timestamp = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc);
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789012");

        var record = new SpecialTypesRecord(
            DateTimeValue: timestamp,
            GuidValue: guid,
            EnumValue: Status.Active);

        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.SpecialTypesRecord);

        Assert("SpecialTypes - DateTime", tonl.Contains("2025-06-15"));
        Assert("SpecialTypes - Guid", tonl.Contains("12345678-1234-1234-1234-123456789012"));
        Assert("SpecialTypes - Enum", tonl.Contains("1")); // Active = 1
    }

    private static void TestNullableTypes()
    {
        var record = new NullableTypesRecord(null, null, null, null);
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.NullableTypesRecord);

        // Nulls should not throw - output depends on implementation
        Assert("NullableTypes - all null", tonl != null);
    }

    private static void TestNullableTypesWithValues()
    {
        var record = new NullableTypesRecord(42, true, DateTime.UtcNow, "test");
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.NullableTypesRecord);

        Assert("NullableTypes - with values", tonl.Contains("42") && tonl.Contains("true"));
    }

    private static void TestLargeRecord()
    {
        var record = new LargeRecord(
            1, "F1", 2, "F2", 3, "F3", 4, "F4", 5, "F5",
            6, "F6", 7, "F7", 8, "F8", 9, "F9", 10, "F10");

        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.LargeRecord);

        Assert("LargeRecord - first field", tonl.Contains("Field1"));
        Assert("LargeRecord - last field", tonl.Contains("Field20"));
        Assert("LargeRecord - values", tonl.Contains("F10"));
    }

    private static void TestContextGetTypeInfo()
    {
        var typeInfo = AotTestContext.Default.GetTypeInfo(typeof(TestRecord));
        Assert("GetTypeInfo returns non-null", typeInfo != null);
        Assert("GetTypeInfo returns correct type", typeInfo?.Type == typeof(TestRecord));

        var unknownTypeInfo = AotTestContext.Default.GetTypeInfo(typeof(string));
        Assert("GetTypeInfo returns null for unregistered", unknownTypeInfo == null);
    }

    private static void TestContextSingleton()
    {
        var ctx1 = AotTestContext.Default;
        var ctx2 = AotTestContext.Default;
        Assert("Context is singleton", ReferenceEquals(ctx1, ctx2));
    }

    private static void TestSerializeToBytes()
    {
        var record = new TestRecord(1, "Bytes Test", false);
        var bytes = TonlSerializer.SerializeToBytes(record, AotTestContext.Default.TestRecord);

        Assert("SerializeToBytes - not empty", bytes.Length > 0);

        var str = System.Text.Encoding.UTF8.GetString(bytes);
        Assert("SerializeToBytes - contains data", str.Contains("Bytes Test"));
    }

    private static void TestTimeSpan()
    {
        var record = new TimeSpanRecord(TimeSpan.FromHours(2.5));
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.TimeSpanRecord);

        Assert("TimeSpan - serialization", tonl.Contains("02:30:00") || tonl.Contains("2:30:00"));
    }

    private static void TestCollections()
    {
        var record = new CollectionRecord(
            Numbers: [1, 2, 3, 4, 5],
            Tags: ["alpha", "beta", "gamma"]);
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.CollectionRecord);

        Console.WriteLine("\n--- CollectionRecord TONL Output ---");
        Console.WriteLine(tonl);
        Console.WriteLine("--- End Output ---\n");

        // Must NOT contain type names (bug detection)
        Assert("Collections - no System.Collections in output",
            !tonl.Contains("System.Collections"));
        Assert("Collections - no System.Generic in output",
            !tonl.Contains("System.Generic"));
        Assert("Collections - no System.Int32[] in output",
            !tonl.Contains("System.Int32[]"));
        Assert("Collections - no System.String[] in output",
            !tonl.Contains("System.String[]"));

        // Should contain actual values
        Assert("Collections - contains Numbers", tonl.Contains("Numbers"));
        Assert("Collections - contains Tags", tonl.Contains("Tags"));
    }

    private static void TestDictionary()
    {
        var record = new DictionaryRecord(
            Scores: new Dictionary<string, int> { ["Alice"] = 100, ["Bob"] = 85 });
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.DictionaryRecord);

        // Must NOT contain type names
        Assert("Dictionary - no System.Collections in output",
            !tonl.Contains("System.Collections"));
        Assert("Dictionary - no Generic.Dictionary in output",
            !tonl.Contains("Generic.Dictionary"));

        // Should contain key "Scores"
        Assert("Dictionary - contains Scores key", tonl.Contains("Scores"));
    }

    private static void TestNestedObject()
    {
        var record = new PersonRecord(
            Name: "John Doe",
            HomeAddress: new Address(City: "Seattle", Country: "USA"));
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.PersonRecord);

        Console.WriteLine("\n--- PersonRecord TONL Output ---");
        Console.WriteLine(tonl);
        Console.WriteLine("--- End Output ---\n");

        // Must NOT contain type names as values (bug detection)
        // Note: "HomeAddress{" is correct - it's the property name, not the type name
        Assert("NestedObject - no Address type name as value",
            !tonl.Contains(": Address\n") && !tonl.Contains(": Address\r") && !tonl.Contains(": Address "));

        // Should contain actual values
        Assert("NestedObject - contains John Doe", tonl.Contains("John Doe"));
        Assert("NestedObject - contains Seattle", tonl.Contains("Seattle"));
        Assert("NestedObject - contains USA", tonl.Contains("USA"));
    }

    private static void TestListOfObjects()
    {
        var record = new OrderRecord(
            OrderId: 12345,
            Items: [
                new OrderItem(Product: "Widget", Qty: 2, Price: 19.99m),
                new OrderItem(Product: "Gadget", Qty: 1, Price: 49.99m)
            ]);
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.OrderRecord);

        Console.WriteLine("\n--- OrderRecord TONL Output ---");
        Console.WriteLine(tonl);
        Console.WriteLine("--- End Output ---\n");

        // Must NOT contain type names
        Assert("ListOfObjects - no System.Collections in output",
            !tonl.Contains("System.Collections"));
        Assert("ListOfObjects - no OrderItem type name in output",
            !tonl.Contains("OrderItem {") && !tonl.Contains("OrderItem{"));

        // Should contain actual values
        Assert("ListOfObjects - contains OrderId", tonl.Contains("12345"));
        Assert("ListOfObjects - contains Widget", tonl.Contains("Widget"));
        Assert("ListOfObjects - contains Gadget", tonl.Contains("Gadget"));
    }

    private static void TestEnumerable()
    {
        var record = new EnumerableRecord(Values: new[] { "one", "two", "three" });
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.EnumerableRecord);

        // Must NOT contain type names
        Assert("Enumerable - no System.String[] in output",
            !tonl.Contains("System.String[]"));
        Assert("Enumerable - no IEnumerable in output",
            !tonl.Contains("IEnumerable"));

        // Should contain key
        Assert("Enumerable - contains Values key", tonl.Contains("Values"));
    }

    private static void TestConcurrentCollection()
    {
        var scores = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();
        scores["Player1"] = 500;
        scores["Player2"] = 350;
        var record = new ConcurrentRecord(ThreadSafeScores: scores);
        var tonl = TonlSerializer.SerializeToString(record, AotTestContext.Default.ConcurrentRecord);

        // Must NOT contain type names
        Assert("Concurrent - no ConcurrentDictionary in output",
            !tonl.Contains("ConcurrentDictionary"));
        Assert("Concurrent - no System.Collections.Concurrent in output",
            !tonl.Contains("System.Collections.Concurrent"));

        // Should contain key
        Assert("Concurrent - contains ThreadSafeScores key", tonl.Contains("ThreadSafeScores"));
    }

    private static void Assert(string testName, bool condition)
    {
        _testCount++;
        if (condition)
        {
            _passCount++;
            Console.WriteLine($"  ✓ {testName}");
        }
        else
        {
            Console.WriteLine($"  ✗ {testName} - FAILED");
        }
    }
}
