using SpcApp.Models;

namespace SpcApp.Services;

public class SpcCalculationService
{
    // SPC control chart constants indexed by subgroup size n (2..10)
    private static readonly Dictionary<int, (double A2, double D3, double D4, double d2)> Constants = new()
    {
        [2]  = (1.880, 0.000, 3.267, 1.128),
        [3]  = (1.023, 0.000, 2.575, 1.693),
        [4]  = (0.729, 0.000, 2.282, 2.059),
        [5]  = (0.577, 0.000, 2.114, 2.326),
        [6]  = (0.483, 0.000, 2.004, 2.534),
        [7]  = (0.419, 0.076, 1.924, 2.704),
        [8]  = (0.373, 0.136, 1.864, 2.847),
        [9]  = (0.337, 0.184, 1.816, 2.970),
        [10] = (0.308, 0.223, 1.777, 3.078),
    };

    public SpcResults Calculate(SpcDataSet dataSet)
    {
        var samples = dataSet.Samples.Where(s => s.Measurements.Count > 0).ToList();
        if (samples.Count < 2)
            return new SpcResults { Labels = new(), SubgroupMeans = new(), SubgroupRanges = new() };

        int n = samples.Max(s => s.Measurements.Count);
        n = Math.Clamp(n, 2, 10);

        var (A2, D3, D4, d2) = Constants.TryGetValue(n, out var c) ? c : Constants[5];

        var means = samples.Select(s => s.Mean).ToList();
        var ranges = samples.Select(s => s.Range).ToList();

        double grandMean = means.Average();
        double rBar = ranges.Average();

        double uclXBar = grandMean + A2 * rBar;
        double lclXBar = grandMean - A2 * rBar;
        double uclR = D4 * rBar;
        double lclR = D3 * rBar;

        double sigmaEstimate = rBar / d2;

        double usl = dataSet.NominalValue + dataSet.UpperTolerance;
        double lsl = dataSet.NominalValue + dataSet.LowerTolerance;
        double toleranceRange = usl - lsl;

        double cp = sigmaEstimate > 0 ? toleranceRange / (6 * sigmaEstimate) : 0;
        double cpu = sigmaEstimate > 0 ? (usl - grandMean) / (3 * sigmaEstimate) : 0;
        double cpl = sigmaEstimate > 0 ? (grandMean - lsl) / (3 * sigmaEstimate) : 0;
        double cpk = Math.Min(cpu, cpl);

        // Performance indices using actual standard deviation
        double stdDev = StdDev(samples.SelectMany(s => s.Measurements).ToList());
        double pp = stdDev > 0 ? toleranceRange / (6 * stdDev) : 0;
        double ppk = stdDev > 0 ? Math.Min((usl - grandMean) / (3 * stdDev), (grandMean - lsl) / (3 * stdDev)) : 0;

        var labels = samples.Select((s, i) =>
            s.Date != default ? s.Date.ToString("dd/MM/yy") : $"S{s.SampleNumber}").ToList();

        var xBarViolations = means.Select((m, i) => m > uclXBar || m < lclXBar ? i : -1)
                                  .Where(i => i >= 0).ToList();
        var rViolations = ranges.Select((r, i) => r > uclR || r < lclR ? i : -1)
                                .Where(i => i >= 0).ToList();

        return new SpcResults
        {
            GrandMean = Math.Round(grandMean, 4),
            RBar = Math.Round(rBar, 4),
            SubgroupSize = n,
            UCL_XBar = Math.Round(uclXBar, 4),
            CL_XBar = Math.Round(grandMean, 4),
            LCL_XBar = Math.Round(lclXBar, 4),
            UCL_R = Math.Round(uclR, 4),
            CL_R = Math.Round(rBar, 4),
            LCL_R = Math.Round(lclR, 4),
            SigmaEstimate = Math.Round(sigmaEstimate, 4),
            Cp = Math.Round(cp, 3),
            Cpk = Math.Round(cpk, 3),
            Cpu = Math.Round(cpu, 3),
            Cpl = Math.Round(cpl, 3),
            Pp = Math.Round(pp, 3),
            Ppk = Math.Round(ppk, 3),
            SubgroupMeans = means.Select(m => Math.Round(m, 4)).ToList(),
            SubgroupRanges = ranges.Select(r => Math.Round(r, 4)).ToList(),
            Labels = labels,
            XBarViolations = xBarViolations,
            RViolations = rViolations,
        };
    }

    private static double StdDev(List<double> values)
    {
        if (values.Count < 2) return 0;
        double mean = values.Average();
        double sumSq = values.Sum(v => (v - mean) * (v - mean));
        return Math.Sqrt(sumSq / (values.Count - 1));
    }
}
