using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Interface for fine management operations
/// Supports UC026-UC029 (fine calculation, payment, waiver, and history)
/// </summary>
public interface IFineService
{
    Task<Result<FineDetailDto>> CreateFineAsync(CreateFineRequest request, bool allowOverride = false, string? overrideReason = null);

    Task<Result<FineDetailDto>> CalculateFineAsync(CalculateFineRequest request);

    Task<Result<FineDetailDto>> PayFineAsync(PayFineRequest request);

    Task<Result<FineDetailDto>> WaiveFineAsync(WaiveFineRequest request, bool allowOverride = false, string? overrideReason = null);

    Task<Result<FineDetailDto>> GetFineByIdAsync(int fineId);

    Task<Result<IEnumerable<FineBasicDto>>> GetPendingFinesByMemberIdAsync(int memberId);

    Task<Result<decimal>> GetTotalPendingFineAmountAsync(int memberId);

    Task<Result<PagedResult<FineBasicDto>>> GetFinesAsync(FineSearchRequest request);

    Task<Result<IEnumerable<FineBasicDto>>> GetFinesByLoanIdAsync(int loanId);

    Task<Result<int>> GenerateOverdueFinesAsync();

    Task<Result<PagedResult<FineBasicDto>>> GetFinesReportPagedAsync(PagedRequest request);

    Task<Result<List<FinesReportExportDto>>> GetFinesReportAsync();

    Task<Result<OutstandingFinesDto>> GetOutstandingFinesAsync(int memberId);
}