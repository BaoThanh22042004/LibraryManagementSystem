using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IFineService
{
    Task<PagedResult<FineDto>> GetPaginatedFinesAsync(PagedRequest request, string? searchTerm = null);
    Task<FineDto?> GetFineByIdAsync(int id);
    Task<List<FineDto>> GetFinesByMemberIdAsync(int memberId);
    Task<List<FineDto>> GetFinesByLoanIdAsync(int loanId);
    Task<int> CreateFineAsync(CreateFineDto fineDto);
    Task UpdateFineAsync(int id, UpdateFineDto fineDto);
    Task DeleteFineAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> PayFineAsync(int id);
    Task<bool> WaiveFineAsync(int id);
    Task<List<FineDto>> GetPendingFinesAsync();
    Task<decimal> CalculateOverdueFineAsync(int loanId);
}