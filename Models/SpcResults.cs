namespace SpcApp.Models;

public class SpcResults
{
    public double GrandMean { get; set; }
    public double RBar { get; set; }
    public int SubgroupSize { get; set; }

    // X-bar chart limits
    public double UCL_XBar { get; set; }
    public double CL_XBar { get; set; }
    public double LCL_XBar { get; set; }

    // R chart limits
    public double UCL_R { get; set; }
    public double CL_R { get; set; }
    public double LCL_R { get; set; }

    // Process capability
    public double SigmaEstimate { get; set; }
    public double Cp { get; set; }
    public double Cpk { get; set; }
    public double Cpl { get; set; }
    public double Cpu { get; set; }
    public double Pp { get; set; }
    public double Ppk { get; set; }

    // Per-subgroup data
    public List<double> SubgroupMeans { get; set; } = new();
    public List<double> SubgroupRanges { get; set; } = new();
    public List<string> Labels { get; set; } = new();

    // Control chart signals
    public List<int> XBarViolations { get; set; } = new();
    public List<int> RViolations { get; set; } = new();

    public bool IsCapable => Cpk >= 1.33;
}
