namespace Tonl;

/// <summary>
/// Represents the type of token encountered during TONL parsing.
/// </summary>
public enum TonlTokenType
{
    /// <summary>
    /// No token has been read yet.
    /// </summary>
    None,

    /// <summary>
    /// A version header directive (#version 1.0).
    /// </summary>
    VersionHeader,

    /// <summary>
    /// A delimiter header directive (#delimiter |).
    /// </summary>
    DelimiterHeader,

    /// <summary>
    /// An object block header (key{col1,col2}:).
    /// </summary>
    ObjectHeader,

    /// <summary>
    /// An array block header (key[N]{col1,col2}:).
    /// </summary>
    ArrayHeader,

    /// <summary>
    /// A primitive array header (key[N]:).
    /// </summary>
    PrimitiveArrayHeader,

    /// <summary>
    /// A key-value pair (key: value).
    /// </summary>
    KeyValue,

    /// <summary>
    /// A data row in a tabular array.
    /// </summary>
    DataRow,

    /// <summary>
    /// End of the document.
    /// </summary>
    EndOfDocument,

    /// <summary>
    /// A comment line (lines starting with # that are not recognized directives).
    /// </summary>
    Comment
}
