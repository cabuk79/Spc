using ClosedXML.Excel;
using SpcApp.Models;

namespace SpcApp.Services;

public class ExcelImportService
{
    /// <summary>
    /// Parses an Excel file where:
    ///   Row 1 = headers (Date | Sample No | M1 | M2 | ... | Mn)
    ///   Rows 2+ = data rows
    /// Returns samples, or an error message.
    /// </summary>
    public (List<SpcSample> Samples, string? Error) ParseExcel(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var usedRange = ws.RangeUsed();
            if (usedRange == null)
                return (new(), "The spreadsheet appears to be empty.");

            var rows = usedRange.RowsUsed().ToList();
            if (rows.Count < 2)
                return (new(), "The spreadsheet must have at least a header row and one data row.");

            // Determine column count from header row
            var headerRow = rows[0];
            int totalCols = headerRow.CellsUsed().Count();

            // Expect: col1=Date, col2=SampleNo, col3..n=Measurements
            if (totalCols < 3)
                return (new(), "Expected at least 3 columns: Date, Sample No, and at least one measurement.");

            int measurementCols = totalCols - 2;
            var samples = new List<SpcSample>();

            foreach (var row in rows.Skip(1))
            {
                var dateCell = row.Cell(1);
                var sampleNoCell = row.Cell(2);

                DateTime date = default;
                if (dateCell.DataType == XLDataType.DateTime)
                    date = dateCell.GetDateTime();
                else if (DateTime.TryParse(dateCell.GetString(), out var parsed))
                    date = parsed;

                int.TryParse(sampleNoCell.GetString(), out int sampleNo);

                var measurements = new List<double>();
                for (int col = 3; col <= totalCols; col++)
                {
                    var cell = row.Cell(col);
                    if (double.TryParse(cell.GetString(), System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out double val))
                    {
                        measurements.Add(val);
                    }
                }

                if (measurements.Count == 0) continue;

                samples.Add(new SpcSample
                {
                    Date = date,
                    SampleNumber = sampleNo > 0 ? sampleNo : samples.Count + 1,
                    Measurements = measurements,
                });
            }

            if (samples.Count == 0)
                return (new(), "No valid data rows were found in the spreadsheet.");

            return (samples, null);
        }
        catch (Exception ex)
        {
            return (new(), $"Failed to read Excel file: {ex.Message}");
        }
    }

    /// <summary>
    /// Generates a byte array for a sample Excel template.
    /// </summary>
    public byte[] GenerateTemplate(int measurementsPerSample = 5)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.AddWorksheet("SPC Data");

        // Headers
        ws.Cell(1, 1).Value = "Date";
        ws.Cell(1, 2).Value = "Sample No";
        for (int i = 1; i <= measurementsPerSample; i++)
            ws.Cell(1, i + 2).Value = $"M{i}";

        // Style header row
        var headerRange = ws.Range(1, 1, 1, measurementsPerSample + 2);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.FromArgb(0, 112, 192);
        headerRange.Style.Font.FontColor = XLColor.White;

        // Sample data rows
        var baseDate = DateTime.Today;
        var rng = new Random(42);
        for (int row = 2; row <= 26; row++)
        {
            ws.Cell(row, 1).Value = baseDate.AddDays(row - 2);
            ws.Cell(row, 2).Value = row - 1;
            for (int col = 3; col <= measurementsPerSample + 2; col++)
                ws.Cell(row, col).Value = Math.Round(25.0 + (rng.NextDouble() - 0.5) * 0.4, 3);
        }

        ws.Column(1).Width = 14;
        ws.Column(2).Width = 12;
        for (int i = 3; i <= measurementsPerSample + 2; i++)
            ws.Column(i).Width = 10;

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
