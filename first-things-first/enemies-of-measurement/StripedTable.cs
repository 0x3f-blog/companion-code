using System.Collections.Concurrent;

/// <summary>
/// ConcurrentDictionary (lock striping). Threads hitting different stripes never contend.
/// Compared against LockTable's single global lock.
/// </summary>
public sealed class StripedTable<TKey, TValue> : ITable<TKey, TValue>
    where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, TValue> _data = new();
    private readonly FileStream? _wal;

    public StripedTable(string? walPath = null)
    {
        if (walPath is not null)
            _wal = new FileStream(walPath, FileMode.Create, FileAccess.Write,
                FileShare.None, 4096, FileOptions.WriteThrough);
    }

    public void Insert(TKey key, TValue value)
    {
        _data[key] = value;
    }

    public TValue? Get(TKey key)
    {
        _data.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>ToArray() for atomic snapshot, then write + fsync — 12B per entry.</summary>
    public void FlushWAL()
    {
        if (_wal is null) return;

        var snapshot = _data.ToArray();
        Span<byte> entry = stackalloc byte[12];
        foreach (var kvp in snapshot)
        {
            BitConverter.TryWriteBytes(entry, kvp.Key.GetHashCode());
            BitConverter.TryWriteBytes(entry[4..], Environment.TickCount64);
            _wal.Write(entry);
        }
        _wal.Flush(flushToDisk: true);
    }

    public int Count => _data.Count;

    public void Dispose() => _wal?.Dispose();
}
