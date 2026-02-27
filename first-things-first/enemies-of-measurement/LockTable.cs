/// <summary>
/// Dictionary + single global lock. Every Insert, Get, and FlushWAL contends on the
/// same Monitor — at 4+ threads this serializes all operations.
/// </summary>
public sealed class LockTable<TKey, TValue> : ITable<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _data = new();
    private readonly object _lock = new();
    private readonly FileStream? _wal;

    public LockTable(string? walPath = null)
    {
        if (walPath is not null)
            _wal = new FileStream(walPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, FileOptions.WriteThrough);
    }

    public void Insert(TKey key, TValue value)
    {
        lock (_lock)
        {
            _data[key] = value;
        }
    }

    public TValue? Get(TKey key)
    {
        lock (_lock)
        {
            _data.TryGetValue(key, out var value);
            return value;
        }
    }

    /// <summary>Snapshot under lock, write + fsync outside — 12B per entry (4B hash + 8B tick).</summary>
    public void FlushWAL()
    {
        if (_wal is null) return;

        KeyValuePair<TKey, TValue>[] snapshot;
        lock (_lock)
        {
            snapshot = new KeyValuePair<TKey, TValue>[_data.Count];
            int index = 0;
            foreach (var kvp in _data)
                snapshot[index++] = kvp;
        }

        Span<byte> entry = stackalloc byte[12];
        foreach (var kvp in snapshot)
        {
            BitConverter.TryWriteBytes(entry, kvp.Key.GetHashCode());
            BitConverter.TryWriteBytes(entry[4..], Environment.TickCount64);
            _wal.Write(entry);
        }
        _wal.Flush(flushToDisk: true);
    }

    public int Count
    {
        get { lock (_lock) { return _data.Count; } }
    }

    public void Dispose() => _wal?.Dispose();
}
