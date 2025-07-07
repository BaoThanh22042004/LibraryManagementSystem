using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Interface for loan management operations
/// Supports UC018-UC021 (checkout, return, renewal, and history)
/// </summary>
public interface ILoanService
{
    Task<Result<LoanDetailDto>> CreateLoanAsync(CreateLoanRequest request, bool allowOverride = false, string? overrideReason = null);

    Task<Result<LoanDetailDto>> ReturnBookAsync(ReturnBookRequest request);

    Task<Result<LoanDetailDto>> RenewLoanAsync(RenewLoanRequest request, bool allowOverride = false, string? overrideReason = null);

    Task<Result<LoanDetailDto>> GetLoanByIdAsync(int loanId);

    Task<Result<IEnumerable<LoanBasicDto>>> GetActiveLoansByMemberIdAsync(int memberId);

    Task<Result<PagedResult<LoanBasicDto>>> GetLoansAsync(LoanSearchRequest request);

    Task<Result<IEnumerable<LoanBasicDto>>> GetLoansByBookCopyIdAsync(int bookCopyId);

    Task<Result<int>> UpdateOverdueLoansAsync();
}