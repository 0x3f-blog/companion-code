namespace StatisticsThatMatter.Analysis;

/// <summary>
/// Post-processing utility for benchmark interpretation. Not called by benchmarks.
///
/// Cohen's d measures the distance between two means in units of the pooled standard deviation.
/// Standard thresholds: 0.2 (small), 0.5 (medium), 0.8 (large) — calibrated by Cohen
/// for behavioral science experiments where within-group variance is naturally high.
///
/// In microbenchmarks, BDN achieves sub-1% coefficient of variation for compute-bound
/// loops. When the pooled SD denominator is tiny, even a 0.8% mean difference produces
/// d = 4.13 — classified as "large" alongside a 1058× algorithmic change (d = 1149).
/// The standard thresholds do not distinguish practical scale in this domain.
///
/// For microbenchmark interpretation, use BDN's Ratio column:
/// - Ratio ≈ 1.00 → no practical difference (regardless of d)
/// - Ratio ≈ 0.001 → algorithmic change (d will also be enormous, but Ratio tells you more)
/// - Ratio between → context-dependent (hot loop called 10⁹ times? 2% matters. Once per request? It doesn't.)
/// </summary>
public static class StatisticalReport
{
    /// <summary>
    /// Compute Cohen's d — the standardized mean difference.
    /// d = |mean₁ − mean₂| / pooled SD
    ///
    /// For BDN results, use the Mean and StdDev columns directly.
    /// Returns 0 when both standard deviations are zero (identical measurements).
    /// </summary>
    public static double CohensD(double mean1, double stdDev1, double mean2, double stdDev2)
    {
        double pooledSd = PooledStdDev(stdDev1, stdDev2);
        if (pooledSd == 0) return 0;
        return Math.Abs(mean1 - mean2) / pooledSd;
    }

    /// <summary>
    /// Classify effect size using Cohen's standard thresholds.
    /// 0.2 = small, 0.5 = medium, 0.8 = large.
    ///
    /// These thresholds are misleading for microbenchmarks — a 0.8% difference
    /// with sub-1% CoV produces d &gt; 4.0 ("large"), while a meaningful optimization
    /// also lands in "large." The classification tells you nothing about practical
    /// significance. Included for completeness; prefer BDN's Ratio column.
    /// </summary>
    public static string ClassifyEffect(double d) => Math.Abs(d) switch
    {
        < 0.2 => "negligible",
        < 0.5 => "small",
        < 0.8 => "medium",
        _     => "large"
    };

    /// <summary>
    /// Pooled standard deviation — the denominator of Cohen's d.
    /// Uses the root-mean-square formula: √((sd₁² + sd₂²) / 2).
    ///
    /// This assumes equal sample sizes (which BDN typically provides within a job).
    /// For unequal n, use the weighted pooled SD — but if your BDN samples differ
    /// in size, you have a configuration problem, not a statistics problem.
    /// </summary>
    public static double PooledStdDev(double sd1, double sd2)
    {
        return Math.Sqrt((sd1 * sd1 + sd2 * sd2) / 2.0);
    }
}
