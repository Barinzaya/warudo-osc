using System;

public class MemoryStream : IDisposable {
    private byte[] _buffer;
    private int _length;
    private int _position;

    public int Capacity => _buffer?.Length ?? 0;
    public int Length => (_length >= 0) ? _length : ~_length;

    public int Position {
        get => _position;
        set {
            if (value > Length) {
                throw new ArgumentOutOfRangeException("Position", "Attempted to set Position past the end of the stream.");
            }
            _position = value;
        }
    }

    public MemoryStream() {
        _buffer = null;
        _length = 0;
        _position = 0;
    }

    public MemoryStream(byte[] buffer) {
        _buffer = buffer;
        _length = ~buffer.Length;
        _position = 0;
    }

    public void Dispose() {}

    public byte[] ToArray() {
        var result = new byte[Length];
        Array.Copy(_buffer, result, result.Length);
        return result;
    }

    public void Write(byte[] b, int offset, int count) {
        EnsureCapacity(_position + count);

        Array.Copy(b, offset, _buffer, _position, count);
        _position += count;

        if (_length >= 0) {
            _length = Math.Max(_length, _position);
        }
    }

    public void WriteByte(byte b) {
        EnsureCapacity(_position + 1);

        _buffer[_position] = b;
        _position += 1;

        if (_length >= 0) {
            _length = Math.Max(_length, _position);
        }
    }

    private void EnsureCapacity(int requiredSize) {
        var currentSize = Capacity;
        if (requiredSize <= currentSize) return;

        if (_length < 0) {
            throw new InvalidOperationException("Attempted to expand a non-resizable MemoryStream.");
        }

        var newSize = Math.Max(256, currentSize);
        while (newSize < requiredSize) {
            newSize += newSize / 2;
            if (newSize < 0) {
                newSize = requiredSize;
            }
        }

        var newBuffer = new byte[newSize];
        Array.Copy(_buffer, newBuffer, _length);

        _buffer = newBuffer;
    }
}
