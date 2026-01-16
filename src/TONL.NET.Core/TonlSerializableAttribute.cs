namespace TONL.NET;

/// <summary>
/// Marks a type for TONL source-generated serialization code.
/// Can be applied in two ways:
/// 1. Directly on a class/struct/record - generates standalone serializer
/// 2. On a TonlSerializerContext class - registers the type for context-based serialization
/// </summary>
/// <remarks>
/// Usage 1: Direct application on types (legacy pattern)
/// <code>
/// [TonlSerializable]
/// public record User(int Id, string Name);
/// // Generates UserTonlSerializer class
/// </code>
///
/// Usage 2: Context-based pattern (preferred, mirrors System.Text.Json)
/// <code>
/// [TonlSourceGenerationOptions]
/// [TonlSerializable(typeof(User))]
/// [TonlSerializable(typeof(Order))]
/// public partial class AppTonlContext : TonlSerializerContext { }
///
/// // Usage:
/// var tonl = TonlSerializer.Serialize(user, AppTonlContext.Default.User);
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false, AllowMultiple = true)]
public sealed class TonlSerializableAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance for direct type annotation.
    /// </summary>
    public TonlSerializableAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance for context-based registration.
    /// </summary>
    /// <param name="type">The type to register for serialization.</param>
    public TonlSerializableAttribute(Type type)
    {
        Type = type;
    }

    /// <summary>
    /// Gets the type to serialize (when applied to a context class).
    /// Null when applied directly to a type.
    /// </summary>
    public Type? Type { get; }

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

    /// <summary>
    /// Gets or sets the source generation mode for this specific type.
    /// When not set, uses the context's default mode.
    /// </summary>
    public TonlSourceGenerationMode GenerationMode { get; set; } = TonlSourceGenerationMode.Default;

    /// <summary>
    /// Gets or sets the name of the generated TypeInfo property.
    /// When not set, uses the type name (e.g., "User" for User type).
    /// </summary>
    public string? TypeInfoPropertyName { get; set; }
}
