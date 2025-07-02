using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface ILoanService
{
    Task<PagedResult<LoanDto>> GetPaginatedLoansAsync(PagedRequest request, string? searchTerm = null);
    Task<LoanDto?> GetLoanByIdAsync(int id);
    Task<List<LoanDto>> GetLoansByMemberIdAsync(int memberId);
    Task<List<LoanDto>> GetLoansByBookCopyIdAsync(int bookCopyId);
    Task<int> CreateLoanAsync(CreateLoanDto loanDto);
    Task UpdateLoanAsync(int id, UpdateLoanDto loanDto);
    Task<bool> ExtendLoanAsync(ExtendLoanDto extendLoanDto);
    Task<bool> ReturnBookAsync(int loanId);
    Task<bool> ExistsAsync(int id);
    Task<List<LoanDto>> GetOverdueLoansAsync();
    Task<LoanEligibilityDto> CheckLoanEligibilityAsync(int memberId);
    Task<List<int>> BulkCheckoutAsync(BulkCheckoutDto bulkCheckoutDto);
    Task<List<(int LoanId, bool Success, string? ErrorMessage)>> BulkReturnAsync(BulkReturnDto bulkReturnDto);
    Task<LoanHistoryDto> GetLoanHistoryAsync(int memberId, int? recentLoansCount = 5);
}