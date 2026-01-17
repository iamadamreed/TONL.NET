using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace TONL.NET;

/// <summary>
/// Provides resolution of TonlTypeInfo for types.
/// </summary>
public interface ITonlTypeInfoResolver
{
    /// <summary>
    /// Gets the type info for the specified type.
    /// </summary>
    /// <param name="type">The type to get info for.</param>
    /// <returns>The type info, or null if not found.</returns>
    TonlTypeInfo? GetTypeInfo(Type type);
}

/// <summary>
/// Base class for TONL type metadata.
/// </summary>
public abstract class TonlTypeInfo
{
    /// <summary>
    /// Gets the type that this info describes.
    /// </summary>
    public abstract Type Type { get; }

    /// <summary>
    /// Gets or sets the originating context that created this type info.
    /// </summary>
    public TonlSerializerContext? OriginatingContext { get; init; }
}

/// <summary>
/// Typed metadata for TONL serialization of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">The type being serialized.</typeparam>
public sealed class TonlTypeInfo<T> : TonlTypeInfo
{
    /// <inheritdoc/>
    public override Type Type => typeof(T);

    /// <summary>
    /// Gets or sets the fast-path serialization handler that writes directly to TonlWriter.
    /// When set, bypasses metadata-based serialization for maximum performance.
    /// </summary>
    public SerializeHandler? Serialize { get; init; }

    /// <summary>
    /// Gets or sets the factory function to create instances of <typeparamref name="T"/>.
    /// </summary>
    public Func<T>? CreateObject { get; init; }

    /// <summary>
    /// Gets or sets the property metadata for metadata-based serialization.
    /// </summary>
    public IReadOnlyList<TonlPropertyInfo<T>>? Properties { get; init; }

    /// <summary>
    /// Gets or sets the deserialize handler that reads from TonlReader.
    /// </summary>
    public DeserializeHandler? Deserialize { get; init; }

    /// <summary>
    /// Gets or sets whether this type is a collection (List, Array, Dictionary, etc.).
    /// When true, the serializer writes collection elements instead of CLR properties.
    /// </summary>
    public bool IsCollection { get; init; }

    /// <summary>
    /// Gets or sets whether this type is a dictionary collection.
    /// </summary>
    public bool IsDictionary { get; init; }

    /// <summary>
    /// Gets or sets the property names for collection elements (used for tabular headers).
    /// For List&lt;T&gt; of complex objects, these are the property names of T.
    /// </summary>
    public string[]? CollectionElementPropertyNames { get; init; }

    /// <summary>
    /// Delegate for fast-path serialization directly to TonlWriter.
    /// </summary>
    /// <param name="writer">The TONL writer.</param>
    /// <param name="value">The value to serialize.</param>
    public delegate void SerializeHandler(ref TonlWriter writer, T value);

    /// <summary>
    /// Delegate for deserialization from TonlReader.
    /// </summary>
    /// <param name="reader">The TONL reader.</param>
    /// <returns>The deserialized value.</returns>
    public delegate T? DeserializeHandler(ref TonlReader reader);
}

/// <summary>
/// Metadata for a property of type <typeparamref name="TParent"/>.
/// </summary>
/// <typeparam name="TParent">The type that contains this property.</typeparam>
public sealed class TonlPropertyInfo<TParent>
{
    /// <summary>
    /// Gets or sets the property name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Gets or sets the property type.
    /// </summary>
    public required Type PropertyType { get; init; }

    /// <summary>
    /// Gets or sets whether the property is nullable.
    /// </summary>
    public bool IsNullable { get; init; }

    /// <summary>
    /// Gets or sets the getter delegate.
    /// </summary>
    public required Func<TParent, object?> GetValue { get; init; }

    /// <summary>
    /// Gets or sets the setter delegate.
    /// </summary>
    public Action<TParent, object?>? SetValue { get; init; }
}
