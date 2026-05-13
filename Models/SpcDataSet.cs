namespace SpcApp.Models;

public class SpcDataSet
{
    public string ComponentCode { get; set; } = string.Empty;
    public string DimensionName { get; set; } = string.Empty;
    public double NominalValue { get; set; }
    public double UpperTolerance { get; set; }
    public double LowerTolerance { get; set; }
    public string Notes { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public List<SpcSample> Samples { get; set; } = new();
}

public class SpcSample
{
    public DateTime Date { get; set; }
    public int SampleNumber { get; set; }
    public List<double> Measurements { get; set; } = new();

    public double Mean => Measurements.Count > 0 ? Measurements.Average() : 0;
    public double Range => Measurements.Count > 0 ? Measurements.Max() - Measurements.Min() : 0;
}
