// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Timing;

namespace Content.Trauma.Shared.Timing;

/// <summary>
/// A ringbuffer that stores items in order of a popping timespan.
/// Trying to add more items than the buffer supports will pop the oldest one.
/// If used in an entity system it should be <c>Reset</c> when the round restarts to avoid problems.
/// </summary>
public sealed class TimedRingBuffer<T>
{
    private IGameTiming _timing;

    private (TimeSpan, T)[] _items;

    private int _offset;

    /// <summary>
    /// How many items are stored
    /// </summary>
    public int Count { get; private set; } = 0;

    /// <summary>
    /// How many items can be stored.
    /// </summary>
    public int Capacity => _items.Length;

    private TimeSpan _popDelay;
    /// <summary>
    /// How long items last for before being popped.
    /// </summary>
    public TimeSpan PopDelay
    {
        get => _popDelay;
        set
        {
            if (_popDelay == value)
                return;

            var diff = value - _popDelay;
            _popDelay = value;
            for (int i = 0; i < Count; i++)
            {
                _items[Index(i)].Item1 += diff;
            }
        }
    }

    /// <summary>
    /// Create a ring buffer with a given capacity.
    /// </summary>
    public TimedRingBuffer(int capacity, TimeSpan popDelay, IGameTiming timing)
    {
        _items = new (TimeSpan, T)[capacity];
        _popDelay = popDelay;
        _timing = timing;
    }

    /// <summary>
    /// Push a new item to the buffer which will be popped after some time.
    /// If it is full, it will return the oldest item which has been removed to fit this one.
    /// The caller must handle the removal of the old item if it's not null.
    /// </summary>
    public bool Push(T item, out T old)
    {
        var popTime = _timing.CurTime + _popDelay;
        var popped = false;
        old = default!;
        if (Count == Capacity)
            popped = PopImmediate(out old);

        _items[NewIndex] = (popTime, item);
        Count++;
        return popped;
    }

    /// <summary>
    /// Immediately pops the oldest item, not checking it against the current time.
    /// </summary>
    public bool PopImmediate(out T item)
    {
        item = default!;
        if (Count == 0)
            return false;

        item = _items[OldIndex].Item2;
        Count--;
        _offset++;
        _offset %= Capacity; // prevent it wrapping if it gets spammed
        return true;
    }

    /// <summary>
    /// Pops the oldest item that should be popped by the current time.
    /// This is what you use in an update loop.
    /// <summary>
    public bool PopNext(out T item)
    {
        if (!Peek(out var popTime, out item))
            return false;

        var now = _timing.CurTime;
        if (now < popTime)
            return false; // not meant to be popped yet. items are sorted so no future item will be either, you don't have to check every one

        return PopImmediate(out item);
    }

    /// <summary>
    /// Gets the oldest item and when it is meant to be popped.
    /// </summary>
    public bool Peek(out TimeSpan popTime, out T item)
    {
        popTime = TimeSpan.Zero;
        item = default!;
        if (Count == 0)
            return false;

        (popTime, item) = _items[OldIndex];
        return true;
    }

    /// <summary>
    /// Clear all items and allow changing the backing array to have a new capacity.
    /// </summary>
    public void Reset(int capacity)
    {
        Reset();
        if (capacity != Capacity)
            _items = new (TimeSpan, T)[capacity];
    }

    /// <summary>
    /// Clear all items without changing the backing array's capacity.
    /// </summary>
    public void Reset()
    {
        _offset = 0;
        Count = 0;
    }

    // index of the oldest item
    private int OldIndex => Index(0);
    // index of where to insert a new item
    private int NewIndex => Index(Count);
    // index to the array for a given item number
    private int Index(int i)
        => (_offset + i) % Capacity;
}
