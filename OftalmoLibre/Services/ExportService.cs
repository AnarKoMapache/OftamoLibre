using System.Data;
using OftalmoLibre.Helpers;

namespace OftalmoLibre.Services;

public sealed class ExportService
{
    public void ExportDataTable(DataTable table, string filePath)
    {
        CsvHelper.SaveToFile(table, filePath);
    }
}
