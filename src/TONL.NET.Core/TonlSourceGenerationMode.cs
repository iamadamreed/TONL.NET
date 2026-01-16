namespace TONL.NET;

/// <summary>
/// Specifies the mode for source generation.
/// </summary>
public enum TonlSourceGenerationMode
{
    /// <summary>
    /// Default mode: generates both metadata and fast-path serialization.
    /// </summary>
    Default = 0,

    /// <summary>
    /// Metadata only: generates type metadata for reflection-free serialization.
    /// Supports all features but without optimized fast-path code generation.
    /// </summary>
    Metadata = 1,

    /// <summary>
    /// Serialization optimization: generates fast-path serialize handlers that
    /// write directly to TonlWriter. Maximum performance, limited features.
    /// Note: Fast-path deserialization is not supported.
    /// </summary>
    Serialization = 2
}
