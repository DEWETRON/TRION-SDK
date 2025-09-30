using System;

namespace TRION_SDK_UI.Models;

internal sealed class DoubleRingBuffer
{
    private readonly double[] _buf;
    private int _start; // index of oldest element
    private int _count;

    public int Capacity => _buf.Length;
    public int Count => _count;

    public DoubleRingBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _buf = new double[capacity];
    }

    // Append single value; overwrites oldest when full. Returns number overwritten (0 or 1).
    public int Add(double value)
    {
        if (_count < Capacity)
        {
            _buf[(_start + _count) % Capacity] = value;
            _count++;
            return 0;
        }
        else
        {
            _buf[_start] = value;
            _start = (_start + 1) % Capacity;
            return 1;
        }
    }

    // Append many values; returns number of overwritten old values.
    public int AddRange(ReadOnlySpan<double> values)
    {
        int overwritten = 0;
        foreach (var v in values)
            overwritten += Add(v);
        return overwritten;
    }

    public void Clear()
    {
        _start = 0;
        _count = 0;
    }

    // Copy all current values (oldest -> newest)
    public double[] ToArray()
    {
        var result = new double[_count];
        if (_count == 0) return result;

        int firstChunk = Math.Min(_count, Capacity - _start);
        Array.Copy(_buf, _start, result, 0, firstChunk);
        int remaining = _count - firstChunk;
        if (remaining > 0)
            Array.Copy(_buf, 0, result, firstChunk, remaining);

        return result;
    }
}