namespace Tonl;

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

    public TonlException(string message)
        : base(message)
    {
    }

    public TonlException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public TonlException(string message, int lineNumber, int position)
        : base(FormatMessage(message, lineNumber, position))
    {
        LineNumber = lineNumber;
        Position = position;
    }

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
    public TonlCircularReferenceException(string path)
        : base($"Circular reference detected at: {path}")
    {
    }
}
