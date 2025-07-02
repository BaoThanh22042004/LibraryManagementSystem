namespace Application.Interfaces.Services;

/// <summary>
/// Service interface for exporting data to various file formats
/// </summary>
public interface IFileExportService
{
    /// <summary>
    /// Exports data to PDF format
    /// </summary>
    /// <param name="htmlContent">HTML content to convert to PDF</param>
    /// <param name="fileName">Name of the output file</param>
    /// <returns>Path to the generated PDF file</returns>
    Task<string> ExportToPdfAsync(string htmlContent, string fileName);
    
    /// <summary>
    /// Exports data to Excel format
    /// </summary>
    /// <param name="data">Data to export</param>
    /// <param name="fileName">Name of the output file</param>
    /// <returns>Path to the generated Excel file</returns>
    Task<string> ExportToExcelAsync(object data, string fileName);
    
    /// <summary>
    /// Exports data to CSV format
    /// </summary>
    /// <param name="data">Data to export</param>
    /// <param name="fileName">Name of the output file</param>
    /// <returns>Path to the generated CSV file</returns>
    Task<string> ExportToCsvAsync(object data, string fileName);
    
    /// <summary>
    /// Gets the directory where exported files are stored
    /// </summary>
    /// <returns>Path to the export directory</returns>
    string GetExportDirectory();
    
    /// <summary>
    /// Gets the full path to an exported file
    /// </summary>
    /// <param name="fileName">Name of the file</param>
    /// <returns>Full path to the file</returns>
    string GetExportFilePath(string fileName);
}