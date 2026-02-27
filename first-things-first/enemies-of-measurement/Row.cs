/// <summary>
/// Row payload for benchmarks. Wraps a heap-allocated byte[] so the benchmark
/// pays the full cost: GC pressure, object overhead, and variable-size fragmentation.
/// </summary>
public readonly struct Row
{
    public readonly byte[] Data;

    public Row(byte[] data) => Data = data;

    /// <summary>Fixed 64-byte payload — uniform size, zero fragmentation. The comfortable lie.</summary>
    public static Row Default { get; } = new(new byte[64]);

    /// <summary>32–256 byte payload keyed by input — variable sizes fragment the heap.</summary>
    public static Row Generate(int key)
    {
        int size = 32 + (key % 225);
        var data = new byte[size];
        for (int i = 0; i < size; i++)
            data[i] = (byte)(key ^ i);
        return new Row(data);
    }
}
