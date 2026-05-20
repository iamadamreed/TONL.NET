namespace TONL.NET;

/// <summary>
/// Opaque wrapper over pre-rendered TONL bytes. Use to embed a payload whose
/// shape is determined at runtime (e.g., a dynamic-schema database result set)
/// inside a source-gen-serialized envelope. The source generator special-cases
/// this type to emit the bytes directly via WriteKeyRawValue.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Lifetime contract:</strong> <see cref="TonlElement"/> holds the bytes the caller hands it.
/// If the caller passed <c>TonlBufferWriter.WrittenMemory</c> (pool-backed), they own the lifetime.
/// Callers that use <see cref="TonlBufferWriter"/> should call <c>.ToArray()</c> on the
/// <see cref="ReadOnlyMemory{T}"/> before constructing the <see cref="TonlElement"/> to disconnect
/// from the underlying pool.
/// </para>
/// </remarks>
public readonly struct TonlElement
{
    /// <summary>
    /// Gets the pre-rendered TONL bytes held by this element.
    /// </summary>
    public ReadOnlyMemory<byte> Bytes { get; }

    /// <summary>
    /// Initializes a new <see cref="TonlElement"/> wrapping the given bytes.
    /// </summary>
    /// <param name="bytes">Pre-rendered TONL bytes. The caller retains lifetime ownership.</param>
    public TonlElement(ReadOnlyMemory<byte> bytes) { Bytes = bytes; }

    /// <summary>
    /// Gets an empty <see cref="TonlElement"/> (default instance with no bytes).
    /// </summary>
    public static TonlElement Empty => default;

    /// <summary>
    /// Gets a value indicating whether this element contains no bytes.
    /// </summary>
    public bool IsEmpty => Bytes.IsEmpty;
}
