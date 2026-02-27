using BenchmarkDotNet.Attributes;

/// <summary>
/// Enemy 5 — Branch Predictor Training.
/// Scanning query results from the storage engine, counting rows above a size threshold.
/// Row.Generate(key) produces variable-size payloads: 32 + (key % 225) bytes.
///
/// Sorted data: all small sizes first, then all large. The branch predictor learns the
/// pattern — prediction rate 95%+. Practically zero mispredictions.
///
/// Random data: sizes arrive in unpredictable order — every branch is a coin flip.
/// ~50% miss rate. Same values, same algorithm, different order. 2-4x performance gap.
///
/// Both arrays are accessed sequentially (data[0], data[1], ...) — cache effects are
/// identical. The ONLY difference is branch predictability.
/// </summary>
public class E5_BranchPredictor
{
    [Params(8_000_000)]
    public int N { get; set; }

    private int[] _sorted = null!;
    private int[] _random = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Row sizes from storage engine formula: Row.Generate(key).Data.Length = 32 + (key % 225)
        _sorted = new int[N];
        for (int i = 0; i < N; i++)
            _sorted[i] = 32 + (i % 225);
        Array.Sort(_sorted);

        _random = _sorted.ToArray();
        new Random(42).Shuffle(_random);
    }

    /// <summary>
    /// Sorted: branch predictor learns — first all below threshold, then all above.
    /// Prediction rate 95%+. Fast.
    /// </summary>
    [Benchmark]
    public int ScanSorted()
    {
        int count = 0;
        int threshold = 150; // mid-range of 32-256
        var data = _sorted;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] > threshold)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Random: every branch is unpredictable — ~50% miss rate.
    /// Same data, same algorithm, different order. This is what production data looks like.
    /// </summary>
    [Benchmark(Baseline = true)]
    public int ScanRandom()
    {
        int count = 0;
        int threshold = 150;
        var data = _random;
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] > threshold)
                count++;
        }
        return count;
    }
}
