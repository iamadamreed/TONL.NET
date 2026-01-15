using System.Buffers;

namespace TONL.NET;

/// <summary>
/// An <see cref="IBufferWriter{T}"/> implementation backed by <see cref="ArrayPool{T}"/>
/// for efficient memory usage during TONL serialization.
/// </summary>
public sealed class TonlBufferWriter : IBufferWriter<byte>, IDisposable
{
    private byte[] _buffer;
    private int _position;
    private bool _disposed;

    /// <summary>
    /// Creates a new buffer writer with the specified initial capacity.
    /// </summary>
    /// <param name="initialCapacity">Initial buffer size in bytes.</param>
    public TonlBufferWriter(int initialCapacity = 256)
    {
        if (initialCapacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(initialCapacity), "Initial capacity must be positive.");
        }

        _buffer = ArrayPool<byte>.Shared.Rent(initialCapacity);
        _position = 0;
    }

    /// <summary>
    /// Gets a span over the written data.
    /// </summary>
    public ReadOnlySpan<byte> WrittenSpan
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.AsSpan(0, _position);
        }
    }

    /// <summary>
    /// Gets a memory over the written data.
    /// </summary>
    public ReadOnlyMemory<byte> WrittenMemory
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.AsMemory(0, _position);
        }
    }

    /// <summary>
    /// Gets the number of bytes written to the buffer.
    /// </summary>
    public int WrittenCount
    {
        get
        {
            ThrowIfDisposed();
            return _position;
        }
    }

    /// <summary>
    /// Gets the total capacity of the underlying buffer.
    /// </summary>
    public int Capacity
    {
        get
        {
            ThrowIfDisposed();
            return _buffer.Length;
        }
    }

    /// <summary>
    /// Gets a memory buffer to write to.
    /// </summary>
    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        ThrowIfDisposed();
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_position);
    }

    /// <summary>
    /// Gets a span buffer to write to.
    /// </summary>
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        ThrowIfDisposed();
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_position);
    }

    /// <summary>
    /// Advances the writer position by the specified number of bytes.
    /// </summary>
    public void Advance(int count)
    {
        ThrowIfDisposed();

        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count cannot be negative.");
        }

        if (_position + count > _buffer.Length)
        {
            throw new InvalidOperationException("Cannot advance past the end of the buffer.");
        }

        _position += count;
    }

    /// <summary>
    /// Resets the writer position to the beginning without returning the buffer to the pool.
    /// </summary>
    public void Clear()
    {
        ThrowIfDisposed();
        _position = 0;
    }

    /// <summary>
    /// Copies the written data to a new byte array.
    /// </summary>
    public byte[] ToArray()
    {
        ThrowIfDisposed();
        return WrittenSpan.ToArray();
    }

    /// <summary>
    /// Copies the written data to a new UTF-8 string.
    /// </summary>
    public override string ToString()
    {
        ThrowIfDisposed();
        return System.Text.Encoding.UTF8.GetString(WrittenSpan);
    }

    private void EnsureCapacity(int sizeHint)
    {
        int requiredCapacity = _position + Math.Max(sizeHint, 1);

        if (requiredCapacity <= _buffer.Length)
        {
            return;
        }

        // At least double the buffer, or grow to required size
        int newSize = Math.Max(_buffer.Length * 2, requiredCapacity);
        byte[] newBuffer = ArrayPool<byte>.Shared.Rent(newSize);

        _buffer.AsSpan(0, _position).CopyTo(newBuffer);
        ArrayPool<byte>.Shared.Return(_buffer);
        _buffer = newBuffer;
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TonlBufferWriter));
        }
    }

    /// <summary>
    /// Returns the underlying buffer to the pool.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        ArrayPool<byte>.Shared.Return(_buffer, clearArray: true);
        _buffer = null!;
        _disposed = true;
    }
}
