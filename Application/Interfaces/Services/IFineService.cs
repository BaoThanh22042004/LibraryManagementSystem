using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

/// <summary>
/// Interface for the fine management service.
/// </summary>
public interface IFineService
{
    /// <summary>
    /// Gets a paginated list of fines with optional search functionality.
    /// </summary>
    Task<PagedResult<FineDto>> GetPaginatedFinesAsync(PagedRequest request, string? searchTerm = null);
    
    /// <summary>
    /// Gets a specific fine by its ID.
    /// </summary>
    Task<FineDto?> GetFineByIdAsync(int id);
    
    /// <summary>
    /// Gets all fines for a specific member.
    /// </summary>
    Task<List<FineDto>> GetFinesByMemberIdAsync(int memberId);
    
    /// <summary>
    /// Gets all fines associated with a specific loan.
    /// </summary>
    Task<List<FineDto>> GetFinesByLoanIdAsync(int loanId);
    
    /// <summary>
    /// Creates a new fine record.
    /// </summary>
    Task<int> CreateFineAsync(CreateFineDto fineDto);
    
    /// <summary>
    /// Updates an existing fine record.
    /// </summary>
    Task UpdateFineAsync(int id, UpdateFineDto fineDto);
    
    /// <summary>
    /// Deletes a fine record that is no longer needed.
    /// </summary>
    Task DeleteFineAsync(int id);
    
    /// <summary>
    /// Checks if a fine with the specified ID exists.
    /// </summary>
    Task<bool> ExistsAsync(int id);
    
    /// <summary>
    /// Processes payment for a fine.
    /// </summary>
    Task<bool> PayFineAsync(int id);
    
    /// <summary>
    /// Waives a fine due to special circumstances.
    /// </summary>
    Task<bool> WaiveFineAsync(int id);
    
    /// <summary>
    /// Gets all pending fines that need payment or waiver.
    /// </summary>
    Task<List<FineDto>> GetPendingFinesAsync();
    
    /// <summary>
    /// Calculates the overdue fine amount for a specific loan.
    /// </summary>
    Task<decimal> CalculateOverdueFineAsync(int loanId);
    
    /// <summary>
    /// Calculates and creates a fine for an overdue loan.
    /// </summary>
    Task<int> CalculateAndCreateOverdueFineAsync(int loanId);
}