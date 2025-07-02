using Application.DTOs;

namespace Application.Interfaces.Services;

/// <summary>
/// Interface for report generation services
/// </summary>
public interface IReportService
{
    /// <summary>
    /// Gets a comprehensive report of all loans that are past their due date
    /// </summary>
    /// <returns>A report containing all overdue loans with member and book details</returns>
    Task<OverdueReportDto> GetOverdueReportAsync();
}