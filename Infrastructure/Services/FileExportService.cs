using Application.Interfaces.Services;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;
using System.Text;

namespace Infrastructure.Services;

/// <summary>
/// Service implementation for exporting data to various file formats
/// </summary>
public class FileExportService : IFileExportService
{
    private readonly string _exportDirectory;
    
    public FileExportService()
    {
        // Create the export directory in the temp folder
        _exportDirectory = Path.Combine(Path.GetTempPath(), "LibraryExports");
        
        if (!Directory.Exists(_exportDirectory))
        {
            Directory.CreateDirectory(_exportDirectory);
        }
        
        // Set QuestPDF license to community
        QuestPDF.Settings.License = LicenseType.Community;
    }
    
    /// <summary>
    /// Exports data to PDF format
    /// </summary>
    /// <param name="htmlContent">HTML content to convert to PDF</param>
    /// <param name="fileName">Name of the output file</param>
    /// <returns>Path to the generated PDF file</returns>
    public async Task<string> ExportToPdfAsync(string htmlContent, string fileName)
    {
        string filePath = Path.Combine(_exportDirectory, fileName);
        
        await Task.Run(() => {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(10));
                    
                    page.Header().Text("Library Management System Report")
                        .SemiBold().FontSize(14).FontColor(Colors.Blue.Medium);
                    
                    page.Content().Text(htmlContent);
                    
                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Page ");
                            x.CurrentPageNumber();
                            x.Span(" of ");
                            x.TotalPages();
                        });
                });
            })
            .GeneratePdf(filePath);
        });
        
        return filePath;
    }
    
    /// <summary>
    /// Exports data to Excel format
    /// </summary>
    /// <param name="data">Data to export</param>
    /// <param name="fileName">Name of the output file</param>
    /// <returns>Path to the generated Excel file</returns>
    public async Task<string> ExportToExcelAsync(object data, string fileName)
    {
        string filePath = Path.Combine(_exportDirectory, fileName);
        
        await Task.Run(() => {
            using var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Report");
            
            // Create header row based on property names
            var headerRow = sheet.CreateRow(0);
            var properties = GetProperties(data);
            
            for (int i = 0; i < properties.Count; i++)
            {
                var cell = headerRow.CreateCell(i);
                cell.SetCellValue(properties[i].Name);
                
                // Style header row
                var headerStyle = workbook.CreateCellStyle();
                var font = workbook.CreateFont();
                font.IsBold = true;
                headerStyle.SetFont(font);
                cell.CellStyle = headerStyle;
            }
            
            // If data is a collection, add all items
            int rowIndex = 1;
            
            if (data is System.Collections.IEnumerable enumerable && !(data is string))
            {
                foreach (var item in enumerable)
                {
                    AddItemToSheet(sheet, item, properties, rowIndex++);
                }
            }
            else
            {
                // Single item
                AddItemToSheet(sheet, data, properties, rowIndex);
            }
            
            // Auto-size columns
            for (int i = 0; i < properties.Count; i++)
            {
                sheet.AutoSizeColumn(i);
            }
            
            // Write to file
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            workbook.Write(fileStream);
        });
        
        return filePath;
    }
    
    /// <summary>
    /// Exports data to CSV format
    /// </summary>
    /// <param name="data">Data to export</param>
    /// <param name="fileName">Name of the output file</param>
    /// <returns>Path to the generated CSV file</returns>
    public async Task<string> ExportToCsvAsync(object data, string fileName)
    {
        string filePath = Path.Combine(_exportDirectory, fileName);
        
        await Task.Run(() => {
            var properties = GetProperties(data);
            var sb = new StringBuilder();
            
            // Add header row
            sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsvField(p.Name))));
            
            // Add data rows
            if (data is System.Collections.IEnumerable enumerable && !(data is string))
            {
                foreach (var item in enumerable)
                {
                    AddItemToCsv(sb, item, properties);
                }
            }
            else
            {
                // Single item
                AddItemToCsv(sb, data, properties);
            }
            
            // Write to file
            File.WriteAllText(filePath, sb.ToString());
        });
        
        return filePath;
    }
    
    /// <summary>
    /// Gets the directory where exported files are stored
    /// </summary>
    /// <returns>Path to the export directory</returns>
    public string GetExportDirectory()
    {
        return _exportDirectory;
    }
    
    /// <summary>
    /// Gets the full path to an exported file
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <returns>Full path to the file</returns>
    public string GetExportFilePath(string fileName)
    {
        return Path.Combine(_exportDirectory, fileName);
    }
    
    #region Helper Methods
    
    private List<PropertyInfo> GetProperties(object data)
    {
        Type type;
        
        if (data is System.Collections.IEnumerable enumerable && !(data is string))
        {
            // For collections, get the element type
            var enumerator = enumerable.GetEnumerator();
            if (enumerator.MoveNext() && enumerator.Current != null)
            {
                type = enumerator.Current.GetType();
            }
            else
            {
                // Empty collection, use the generic argument if possible
                type = data.GetType().GetGenericArguments().FirstOrDefault() ?? typeof(object);
            }
        }
        else
        {
            // For single objects, use the object type
            type = data.GetType();
        }
        
        return type.GetProperties()
            .Where(p => p.CanRead && IsSimpleType(p.PropertyType))
            .ToList();
    }
    
    private bool IsSimpleType(Type type)
    {
        // Consider a type "simple" if it's a primitive, string, datetime, enum, or nullable of these
        return type.IsPrimitive
               || type == typeof(string)
               || type == typeof(decimal)
               || type == typeof(DateTime)
               || type == typeof(DateTimeOffset)
               || type == typeof(TimeSpan)
               || type == typeof(Guid)
               || type.IsEnum
               || (Nullable.GetUnderlyingType(type) != null && IsSimpleType(Nullable.GetUnderlyingType(type)!));
    }
    
    private void AddItemToSheet(ISheet sheet, object item, List<PropertyInfo> properties, int rowIndex)
    {
        var row = sheet.CreateRow(rowIndex);
        
        for (int i = 0; i < properties.Count; i++)
        {
            var property = properties[i];
            var value = property.GetValue(item);
            var cell = row.CreateCell(i);
            
            if (value == null)
            {
                cell.SetCellValue(string.Empty);
            }
            else if (value is DateTime dateTime)
            {
                cell.SetCellValue(dateTime);
            }
            else if (value is bool boolValue)
            {
                cell.SetCellValue(boolValue);
            }
            else if (value is double doubleValue)
            {
                cell.SetCellValue(doubleValue);
            }
            else if (value is int intValue)
            {
                cell.SetCellValue(intValue);
            }
            else if (value is decimal decimalValue)
            {
                cell.SetCellValue((double)decimalValue);
            }
            else
            {
                cell.SetCellValue(value.ToString());
            }
        }
    }
    
    private void AddItemToCsv(StringBuilder sb, object item, List<PropertyInfo> properties)
    {
        var values = new List<string>();
        
        foreach (var property in properties)
        {
            var value = property.GetValue(item);
            values.Add(EscapeCsvField(value?.ToString() ?? string.Empty));
        }
        
        sb.AppendLine(string.Join(",", values));
    }
    
    private string EscapeCsvField(string field)
    {
        // Escape quotes by doubling them and wrap in quotes if field contains comma or quotes
        bool needsQuotes = field.Contains(',') || field.Contains('"') || field.Contains('\n');
        
        if (needsQuotes)
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        
        return field;
    }
    
    #endregion
}