namespace TONL.NET;

/// <summary>
/// Marks a type for TONL source-generated serialization code.
/// When applied to a class, struct, or record, the source generator will
/// create optimized Serialize and Deserialize methods at compile time,
/// eliminating runtime reflection overhead.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class TonlSerializableAttribute : Attribute
{
    /// <summary>
    /// Gets or sets whether to generate serialization code.
    /// Default is true.
    /// </summary>
    public bool GenerateSerializer { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to generate deserialization code.
    /// Default is true.
    /// </summary>
    public bool GenerateDeserializer { get; set; } = true;
}
