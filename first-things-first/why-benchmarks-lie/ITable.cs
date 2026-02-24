/// <summary>
/// Common interface for benchmark backends (LockTable, StripedTable).
/// Backend is resolved once in GlobalSetup — no branching on every insert.
/// </summary>
public interface ITable<TKey, TValue> : IDisposable
    where TKey : notnull
{
    void Insert(TKey key, TValue value);
    TValue? Get(TKey key);
    void FlushWAL();
    int Count { get; }
}
