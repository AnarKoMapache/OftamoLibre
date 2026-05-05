using System.Data;
using System.Text;

namespace OftalmoLibre.Helpers;

public static class CsvHelper
{
    public static void SaveToFile(DataTable table, string filePath)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(';', table.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName))));

        foreach (DataRow row in table.Rows)
        {
            builder.AppendLine(string.Join(';', row.ItemArray.Select(value => Escape(Convert.ToString(value)))));
        }

        File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static string Escape(string? value)
    {
        value ??= string.Empty;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
