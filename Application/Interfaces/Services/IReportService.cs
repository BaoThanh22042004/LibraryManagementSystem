using Application.DTOs;
using Domain.Enums;

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
    
    /// <summary>
    /// Gets a comprehensive report of all fines in the system
    /// </summary>
    /// <param name="includeStatus">Optional filter to include specific fine statuses. If null, all statuses are included.</param>
    /// <returns>A report containing all fines with member and loan details</returns>
    Task<FineReportDto> GetFinesReportAsync(FineStatus[]? includeStatus = null);
    
    /// <summary>
    /// Gets a summary of outstanding fines for a specific member
    /// </summary>
    /// <param name="memberId">ID of the member to check</param>
    /// <returns>A summary of the member's outstanding fines</returns>
    Task<OutstandingFineDto> GetOutstandingFinesAsync(int memberId);
    
    /// <summary>
    /// Checks if a member has any outstanding fines
    /// </summary>
    /// <param name="memberId">ID of the member to check</param>
    /// <returns>True if the member has outstanding fines, false otherwise</returns>
    Task<bool> HasOutstandingFinesAsync(int memberId);
    
    /// <summary>
    /// Gets the total amount of outstanding fines for a member
    /// </summary>
    /// <param name="memberId">ID of the member to check</param>
    /// <returns>Total amount of outstanding fines</returns>
    Task<decimal> GetTotalOutstandingFineAmountAsync(int memberId);
    
    /// <summary>
    /// Gets comprehensive system statistics for the dashboard
    /// </summary>
    /// <returns>A dashboard DTO containing all system statistics</returns>
    Task<DashboardDto> GetDashboardStatisticsAsync();
    
    /// <summary>
    /// Gets statistics for a specific date range
    /// </summary>
    /// <param name="startDate">The start date for the statistical period</param>
    /// <param name="endDate">The end date for the statistical period</param>
    /// <returns>A dashboard DTO containing filtered statistics for the given period</returns>
    Task<DashboardDto> GetDashboardStatisticsAsync(DateTime startDate, DateTime endDate);
}