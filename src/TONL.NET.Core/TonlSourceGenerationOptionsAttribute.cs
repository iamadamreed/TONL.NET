namespace TONL.NET;

/// <summary>
/// Marks a partial class as a TONL serializer context and configures source generation options.
/// The marked class must be partial and inherit from <see cref="TonlSerializerContext"/>.
/// </summary>
/// <remarks>
/// This attribute triggers the TONL source generator to produce optimized serialization
/// code at compile time, eliminating runtime reflection overhead.
///
/// Example usage:
/// <code>
/// [TonlSourceGenerationOptions(GenerationMode = TonlSourceGenerationMode.Default)]
/// [TonlSerializable(typeof(User))]
/// [TonlSerializable(typeof(Order))]
/// public partial class AppTonlContext : TonlSerializerContext { }
/// </code>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class TonlSourceGenerationOptionsAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the source generation mode.
    /// Default is <see cref="TonlSourceGenerationMode.Default"/>.
    /// </summary>
    public TonlSourceGenerationMode GenerationMode { get; set; } = TonlSourceGenerationMode.Default;

    /// <summary>
    /// Gets or sets the delimiter character used for serialization.
    /// Default is comma (',').
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Gets or sets whether to add spaces after delimiters for readability.
    /// Default is false for compact output.
    /// </summary>
    public bool PrettyDelimiters { get; set; }

    /// <summary>
    /// Gets or sets the number of spaces per indentation level.
    /// Default is 2.
    /// </summary>
    public int IndentSize { get; set; } = 2;
}
