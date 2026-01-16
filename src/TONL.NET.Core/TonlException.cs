namespace TONL.NET;

/// <summary>
/// Exception thrown when TONL parsing or serialization fails.
/// </summary>
public class TonlException : Exception
{
    /// <summary>
    /// Gets the line number where the error occurred (1-based), or null if not applicable.
    /// </summary>
    public int? LineNumber { get; }

    /// <summary>
    /// Gets the character position within the line (1-based), or null if not applicable.
    /// </summary>
    public int? Position { get; }

    /// <summary>
    /// Gets the byte offset in the input where the error occurred, or null if not applicable.
    /// </summary>
    public long? ByteOffset { get; }

    /// <summary>
    /// Initializes a new instance of TonlException with a message.
    /// </summary>
    /// <param name="message">The error message.</param>
    public TonlException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of TonlException with a message and inner exception.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="innerException">The inner exception.</param>
    public TonlException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of TonlException with location information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="lineNumber">The line number where the error occurred.</param>
    /// <param name="position">The character position within the line.</param>
    public TonlException(string message, int lineNumber, int position)
        : base(FormatMessage(message, lineNumber, position))
    {
        LineNumber = lineNumber;
        Position = position;
    }

    /// <summary>
    /// Initializes a new instance of TonlException with byte offset information.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="byteOffset">The byte offset where the error occurred.</param>
    public TonlException(string message, long byteOffset)
        : base($"{message} (at byte offset {byteOffset})")
    {
        ByteOffset = byteOffset;
    }

    private static string FormatMessage(string message, int lineNumber, int position)
    {
        return $"{message} (line {lineNumber}, position {position})";
    }
}

/// <summary>
/// Exception thrown when a circular reference is detected during serialization.
/// </summary>
public class TonlCircularReferenceException : TonlException
{
    /// <summary>
    /// Initializes a new instance of TonlCircularReferenceException.
    /// </summary>
    /// <param name="path">The object path where the circular reference was detected.</param>
    public TonlCircularReferenceException(string path)
        : base($"Circular reference detected at: {path}")
    {
    }
}
