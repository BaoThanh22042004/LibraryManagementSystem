using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Application.Common;

public static class CsvExportExtensions
{
    public static string ToCsv<T>(IEnumerable<T> items)
    {
        if (items == null || !items.Any()) return string.Empty;
        var type = typeof(T);
        var props = type.GetProperties();
        var sb = new StringBuilder();
        // Header
        sb.AppendLine(string.Join(",", props.Select(p => p.Name)));
        // Rows
        foreach (var item in items)
        {
            var values = props.Select(p => {
                var val = p.GetValue(item, null);
                if (val == null) return "";
                var str = val.ToString();
                if (str != null && (str.Contains(",") || str.Contains("\"")))
                    str = $"\"{str.Replace("\"", "\"\"")}";
                return str;
            });
            sb.AppendLine(string.Join(",", values));
        }
        return sb.ToString();
    }
} 