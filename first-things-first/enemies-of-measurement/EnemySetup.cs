/// <summary>
/// Shared constants for enemy benchmarks.
/// Uses the same storage engine from FTF-1 (LockTable / StripedTable) without WAL —
/// fsync dominated FTF-1; here we measure the in-memory path where enemies hide.
/// </summary>
internal static class EnemySetup
{
    /// <summary>Default insert count for E1-E3 benchmarks.</summary>
    public const int N = 100_000;

    /// <summary>Key space larger than N — forces random distribution across hash buckets.</summary>
    public const int KeySpace = 200_000;
}
