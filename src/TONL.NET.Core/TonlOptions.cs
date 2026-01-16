namespace TONL.NET;

/// <summary>
/// Configuration options for TONL serialization and deserialization.
/// </summary>
public sealed class TonlOptions
{
    /// <summary>
    /// Gets or sets the delimiter character used to separate values.
    /// Supported delimiters: ',' (default), '|', '\t', ';'
    /// </summary>
    public char Delimiter { get; set; } = ',';

    /// <summary>
    /// Gets or sets the TONL format version.
    /// </summary>
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Gets or sets whether to include type hints in the output.
    /// When true, column definitions include type annotations (e.g., "id:u32,name:str").
    /// </summary>
    public bool IncludeTypeHints { get; set; }

    /// <summary>
    /// Gets or sets the number of spaces per indentation level.
    /// </summary>
    public int IndentSize { get; set; } = 2;

    /// <summary>
    /// Gets or sets whether to use strict parsing mode.
    /// When true, array length mismatches and other inconsistencies throw exceptions.
    /// </summary>
    public bool StrictMode { get; set; }

    /// <summary>
    /// Gets or sets whether to add spaces after delimiters for readability.
    /// When false (default), produces compact output: "1,2,3"
    /// When true, adds spaces for readability: "1, 2, 3"
    /// </summary>
    public bool PrettyDelimiters { get; set; }

    /// <summary>
    /// Default options instance.
    /// </summary>
    public static TonlOptions Default { get; } = new();

    /// <summary>
    /// Validates that the delimiter is a supported character.
    /// </summary>
    internal void Validate()
    {
        if (Delimiter is not (',' or '|' or '\t' or ';'))
        {
            throw new ArgumentException($"Unsupported delimiter: '{Delimiter}'. Supported: ',' '|' '\\t' ';'", nameof(Delimiter));
        }
    }
}
