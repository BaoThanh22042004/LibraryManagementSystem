using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Services;

/// <summary>
/// Implementation of the loan management service
/// Supports UC018-UC021 (checkout, return, renewal, and history)
/// </summary>
public class LoanService : ILoanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<LoanService> _logger;

    private const int DefaultLoanPeriodDays = 14;
    private const int MaxLoanPeriodDays = 30;
    private const int MaxActiveLoansPerMember = 5;

    public LoanService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<LoanService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new loan (checkout) - UC018
    /// Supports staff override for eligibility and loan limits.
    /// </summary>
    /// <param name="request">Loan creation details</param>
    /// <param name="allowOverride">If true, allows staff to override certain business rules</param>
    /// <param name="overrideReason">Reason for override (required if allowOverride is true)</param>
    /// <returns>Result with created loan information and override context</returns>
    public async Task<Result<LoanDetailDto>> CreateLoanAsync(CreateLoanRequest request, bool allowOverride = false, string? overrideReason = null)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var member = await _unitOfWork.Repository<Member>().GetAsync(
                m => m.Id == request.MemberId,
                m => m.User, m => m.Loans);

            if (member == null)
                return Result.Failure<LoanDetailDto>($"Member with ID {request.MemberId} not found.");

            if (member.MembershipStatus != MembershipStatus.Active && !allowOverride)
                return Result.Failure<LoanDetailDto>($"Member has inactive membership status: {member.MembershipStatus}.");

            if (member.OutstandingFines > 0 && !allowOverride)
                return Result.Failure<LoanDetailDto>($"Member has outstanding fines of {member.OutstandingFines:C}. Please clear fines before borrowing.");

            var activeLoansCount = member.Loans.Count(l => l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue);
            if (activeLoansCount >= MaxActiveLoansPerMember && !allowOverride)
                return Result.Failure<LoanDetailDto>($"Member has reached the maximum number of active loans ({MaxActiveLoansPerMember}).");

            var bookCopy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                bc => bc.Id == request.BookCopyId,
                bc => bc.Book);

            if (bookCopy == null)
                return Result.Failure<LoanDetailDto>($"Book copy with ID {request.BookCopyId} not found.");

            if (bookCopy.Status != CopyStatus.Available && bookCopy.Status != CopyStatus.Reserved && !allowOverride)
                return Result.Failure<LoanDetailDto>($"Book copy with ID {request.BookCopyId} is not available for loan. Current status: {bookCopy.Status}.");

			var loan = _mapper.Map<Loan>(request);
            loan.LoanDate = DateTime.UtcNow;
            loan.DueDate = request.CustomDueDate ?? DateTime.UtcNow.AddDays(DefaultLoanPeriodDays);
            loan.Status = LoanStatus.Active;

            if ((loan.DueDate - loan.LoanDate).TotalDays > MaxLoanPeriodDays && !allowOverride)
                return Result.Failure<LoanDetailDto>($"Maximum loan period is {MaxLoanPeriodDays} days.");

            if (loan.DueDate <= DateTime.UtcNow && !allowOverride)
                return Result.Failure<LoanDetailDto>("Due date must be in the future.");

            bookCopy.Status = CopyStatus.Borrowed;

            await _unitOfWork.Repository<Loan>().AddAsync(loan);
            _unitOfWork.Repository<BookCopy>().Update(bookCopy);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();

            var createdLoan = await _unitOfWork.Repository<Loan>().GetAsync(
                l => l.Id == loan.Id,
                l => l.Member.User,
                l => l.BookCopy.Book,
                l => l.Fines);

            var loanDto = _mapper.Map<LoanDetailDto>(createdLoan);
            if (allowOverride && !string.IsNullOrWhiteSpace(overrideReason))
            {
                loanDto.OverrideContext = new LoanOverrideContext
                {
                    IsOverride = true,
                    Reason = overrideReason,
                    OverriddenRules = GetOverriddenRulesForCreateLoan(request, member, bookCopy, activeLoansCount)
                };
            }
            return Result.Success(loanDto);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating loan: {ErrorMessage}", ex.Message);
            return Result.Failure<LoanDetailDto>($"Failed to create loan: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns a borrowed book - UC019
    /// </summary>
    /// <param name="request">Return details</param>
    /// <returns>Result with updated loan information</returns>
    public async Task<Result<LoanDetailDto>> ReturnBookAsync(ReturnBookRequest request)
    {
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();

            // Get the loan with related entities
            var loan = await _unitOfWork.Repository<Loan>().GetAsync(
                l => l.Id == request.LoanId,
                l => l.Member.User,
                l => l.BookCopy.Book,
                l => l.Fines);

            if (loan == null)
                return Result.Failure<LoanDetailDto>($"Loan with ID {request.LoanId} not found.");

            // Check if loan can be returned (must be active or overdue)
            if (loan.Status != LoanStatus.Active && loan.Status != LoanStatus.Overdue)
                return Result.Failure<LoanDetailDto>($"Loan cannot be returned. Current status: {loan.Status}.");

            // Get the book copy
            var bookCopy = loan.BookCopy;

            // Process the return
            loan.ReturnDate = DateTime.UtcNow;
            loan.Status = LoanStatus.Returned;

            // Calculate overdue days
            var daysOverdue = (loan.ReturnDate.Value > loan.DueDate) ? (int)(loan.ReturnDate.Value - loan.DueDate).TotalDays : 0;

			// Update book copy status based on condition - BR-10
			bookCopy.Status = request.BookCondition switch
			{
				BookCondition.Good => CopyStatus.Available,
				BookCondition.Damaged => CopyStatus.Damaged,
				BookCondition.Lost => CopyStatus.Lost,
				_ => CopyStatus.Available,
			};

			// Calculate and create fine if overdue - BR-15
			if (daysOverdue > 0)
            {
                // Create overdue fine
                var fine = new Fine
                {
                    MemberId = loan.MemberId,
                    LoanId = loan.Id,
                    Type = FineType.Overdue,
                    Amount = daysOverdue * 0.50m, // $0.50 per day overdue
                    FineDate = DateTime.UtcNow,
                    Status = FineStatus.Pending,
                    Description = $"Overdue fine for {daysOverdue} days. Book: '{bookCopy.Book.Title}'"
                };

                await _unitOfWork.Repository<Fine>().AddAsync(fine);

                // Update member's outstanding fines
                var member = loan.Member;
                member.OutstandingFines += fine.Amount;
                _unitOfWork.Repository<Member>().Update(member);
            }

            // Create fine for damaged or lost books
            if (request.BookCondition == BookCondition.Damaged || request.BookCondition == BookCondition.Lost)
            {
                var fineType = request.BookCondition == BookCondition.Damaged ? FineType.Damaged : FineType.Lost;
                var fineAmount = request.BookCondition == BookCondition.Damaged ? 10.00m : 25.00m; // Example fine amounts

                var fine = new Fine
                {
                    MemberId = loan.MemberId,
                    LoanId = loan.Id,
                    Type = fineType,
                    Amount = fineAmount,
                    FineDate = DateTime.UtcNow,
                    Status = FineStatus.Pending,
                    Description = $"{fineType} fine for book: '{bookCopy.Book.Title}'. {request.Notes}"
                };

                await _unitOfWork.Repository<Fine>().AddAsync(fine);

                // Update member's outstanding fines
                var member = loan.Member;
                member.OutstandingFines += fine.Amount;
                _unitOfWork.Repository<Member>().Update(member);
            }

            // Update entities
            _unitOfWork.Repository<Loan>().Update(loan);
            _unitOfWork.Repository<BookCopy>().Update(bookCopy);
            await _unitOfWork.SaveChangesAsync();

            // Check if there are active reservations for this book
            if (bookCopy.Status == CopyStatus.Available)
            {
                var activeReservations = await _unitOfWork.Repository<Reservation>().ListAsync(
                    r => r.BookId == bookCopy.BookId && r.Status == ReservationStatus.Active,
                    r => r.OrderBy(x => x.ReservationDate));

                if (activeReservations.Any())
                {
                    // Update book copy status to reserved
                    bookCopy.Status = CopyStatus.Reserved;
                    _unitOfWork.Repository<BookCopy>().Update(bookCopy);

                    // Update reservation (will be completed by FulfillReservationAsync)
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            // Reload loan with updated fines
            loan = await _unitOfWork.Repository<Loan>().GetAsync(
                l => l.Id == request.LoanId,
                l => l.Member.User,
                l => l.BookCopy.Book,
                l => l.Fines);

            // Map and return the result
            var loanDto = _mapper.Map<LoanDetailDto>(loan);
            return Result.Success(loanDto);
        }
        catch (Exception ex)
        {
            // Rollback on error
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error returning book: {ErrorMessage}", ex.Message);
            return Result.Failure<LoanDetailDto>($"Failed to return book: {ex.Message}");
        }
    }

    /// <summary>
    /// Renews/extends a loan - UC020
    /// Supports staff override for extension and reservation block.
    /// </summary>
    /// <param name="request">Renewal details</param>
    /// <param name="allowOverride">If true, allows staff to override certain business rules</param>
    /// <param name="overrideReason">Reason for override (required if allowOverride is true)</param>
    /// <returns>Result with updated loan information and override context</returns>
    public async Task<Result<LoanDetailDto>> RenewLoanAsync(RenewLoanRequest request, bool allowOverride = false, string? overrideReason = null)
    {
        try
        {
            await _unitOfWork.BeginTransactionAsync();

            var loan = await _unitOfWork.Repository<Loan>().GetAsync(
                l => l.Id == request.LoanId,
                l => l.Member.User,
                l => l.BookCopy.Book,
                l => l.Fines);

            if (loan == null)
                return Result.Failure<LoanDetailDto>($"Loan with ID {request.LoanId} not found.");

            if (loan.Status != LoanStatus.Active && !allowOverride)
                return Result.Failure<LoanDetailDto>($"Loan cannot be renewed. Current status: {loan.Status}.");

            if (loan.Member.OutstandingFines > 0 && !allowOverride)
                return Result.Failure<LoanDetailDto>($"Member has outstanding fines of {loan.Member.OutstandingFines:C}. Please clear fines before renewal.");

            var hasActiveReservations = await _unitOfWork.Repository<Reservation>().ExistsAsync(
                r => r.BookId == loan.BookCopy.BookId && r.Status == ReservationStatus.Active);
            if (hasActiveReservations && !allowOverride)
                return Result.Failure<LoanDetailDto>("Book has active reservations and cannot be renewed.");

            if (request.NewDueDate.HasValue)
            {
                if (request.NewDueDate.Value <= DateTime.UtcNow && !allowOverride)
                    return Result.Failure<LoanDetailDto>("New due date must be in the future.");
                if ((request.NewDueDate.Value - DateTime.UtcNow).TotalDays > MaxLoanPeriodDays && !allowOverride)
                    return Result.Failure<LoanDetailDto>($"Maximum extension period is {MaxLoanPeriodDays} days from current date.");
                loan.DueDate = request.NewDueDate.Value;
            }
            else
            {
                loan.DueDate = DateTime.UtcNow.AddDays(DefaultLoanPeriodDays);
            }
            loan.Status = LoanStatus.Active;
            _unitOfWork.Repository<Loan>().Update(loan);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
            var loanDto = _mapper.Map<LoanDetailDto>(loan);
            if (allowOverride && !string.IsNullOrWhiteSpace(overrideReason))
            {
                loanDto.OverrideContext = new LoanOverrideContext
                {
                    IsOverride = true,
                    Reason = overrideReason,
                    OverriddenRules = GetOverriddenRulesForRenewLoan(request, loan)
                };
            }
            return Result<LoanDetailDto>.Success(loanDto);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error renewing loan: {ErrorMessage}", ex.Message);
            return Result.Failure<LoanDetailDto>($"Failed to renew loan: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets loan details by ID
    /// </summary>
    /// <param name="loanId">ID of the loan to retrieve</param>
    /// <returns>Result with loan details</returns>
    public async Task<Result<LoanDetailDto>> GetLoanByIdAsync(int loanId)
    {
        try
        {
            var loan = await _unitOfWork.Repository<Loan>().GetAsync(
                l => l.Id == loanId,
                l => l.Member.User,
                l => l.BookCopy.Book,
                l => l.Fines);

            if (loan == null)
                return Result.Failure<LoanDetailDto>($"Loan with ID {loanId} not found.");

            var loanDto = _mapper.Map<LoanDetailDto>(loan);
            return Result<LoanDetailDto>.Success(loanDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loan: {ErrorMessage}", ex.Message);
            return Result.Failure<LoanDetailDto>($"Failed to retrieve loan: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all active loans for a member
    /// </summary>
    /// <param name="memberId">ID of the member</param>
    /// <returns>Result with list of active loans</returns>
    public async Task<Result<IEnumerable<LoanBasicDto>>> GetActiveLoansByMemberIdAsync(int memberId)
    {
        try
        {
            // Check if member exists
            var memberExists = await _unitOfWork.Repository<Member>().ExistsAsync(m => m.Id == memberId);
            if (!memberExists)
                return Result.Failure<IEnumerable<LoanBasicDto>>($"Member with ID {memberId} not found.");

            // Get all active loans for the member
            var loans = await _unitOfWork.Repository<Loan>().ListAsync(
                l => l.MemberId == memberId && (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue),
                l => l.OrderByDescending(x => x.LoanDate),
                true,
                l => l.Member.User,
                l => l.BookCopy.Book);

            var loanDtos = _mapper.Map<IEnumerable<LoanBasicDto>>(loans);
            return Result.Success(loanDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active loans: {ErrorMessage}", ex.Message);
            return Result.Failure<IEnumerable<LoanBasicDto>>($"Failed to retrieve active loans: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all loans with search parameters and pagination - UC021
    /// </summary>
    /// <param name="request">Search parameters</param>
    /// <returns>Paged result with loans matching criteria</returns>
    public async Task<Result<PagedResult<LoanBasicDto>>> GetLoansAsync(LoanSearchRequest request)
    {
        try
        {
            // Build the predicate for filtering
            Expression<Func<Loan, bool>> predicate = BuildLoanSearchPredicate(request);

            // Get paged loans
            var pagedLoans = await _unitOfWork.Repository<Loan>().PagedListAsync(
				request,
                predicate,
                q => q.OrderByDescending(l => l.LoanDate),
                true,
                l => l.Member.User,
                l => l.BookCopy.Book);

            // Map to DTOs
            var loanDtos = _mapper.Map<IEnumerable<LoanBasicDto>>(pagedLoans.Items);

            // Create and return paged result
            return Result.Success(new PagedResult<LoanBasicDto>
            {
                Items = [..loanDtos],
                Page = pagedLoans.Page,
                PageSize = pagedLoans.PageSize,
                Count = pagedLoans.Count,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loans: {ErrorMessage}", ex.Message);
            return Result.Failure<PagedResult<LoanBasicDto>>($"Failed to retrieve loans: {ex.Message}");
		}
    }

    /// <summary>
    /// Gets loans for a specific book copy
    /// </summary>
    /// <param name="bookCopyId">ID of the book copy</param>
    /// <returns>Result with list of loans for the book copy</returns>
    public async Task<Result<IEnumerable<LoanBasicDto>>> GetLoansByBookCopyIdAsync(int bookCopyId)
    {
        try
        {
            // Check if book copy exists
            var bookCopyExists = await _unitOfWork.Repository<BookCopy>().ExistsAsync(bc => bc.Id == bookCopyId);
            if (!bookCopyExists)
                return Result.Failure<IEnumerable<LoanBasicDto>>($"Book copy with ID {bookCopyId} not found.");

            // Get all loans for the book copy
            var loans = await _unitOfWork.Repository<Loan>().ListAsync(
                l => l.BookCopyId == bookCopyId,
                l => l.OrderByDescending(x => x.LoanDate),
                true,
                l => l.Member.User,
                l => l.BookCopy.Book);

            var loanDtos = _mapper.Map<IEnumerable<LoanBasicDto>>(loans);
            return Result.Success(loanDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving loans by book copy: {ErrorMessage}", ex.Message);
            return Result.Failure<IEnumerable<LoanBasicDto>>($"Failed to retrieve loans: {ex.Message}");
        }
    }

    /// <summary>
    /// Updates overdue loan statuses across the system
    /// </summary>
    /// <returns>Count of loans updated to overdue status</returns>
    public async Task<Result<int>> UpdateOverdueLoansAsync()
    {
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();

            // Get all active loans that are now overdue
            var overdueLoans = await _unitOfWork.Repository<Loan>().ListAsync(
                l => l.Status == LoanStatus.Active && l.DueDate < DateTime.UtcNow,
                null,
                false,
                l => l.Member,
                l => l.BookCopy.Book);

            int updatedCount = 0;

            // Update each overdue loan
            foreach (var loan in overdueLoans)
            {
                loan.Status = LoanStatus.Overdue;
                _unitOfWork.Repository<Loan>().Update(loan);

                updatedCount++;
            }

            // Save changes
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            return Result.Success(updatedCount);
        }
        catch (Exception ex)
        {
            // Rollback on error
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error updating overdue loans: {ErrorMessage}", ex.Message);
            return Result.Failure<int>($"Failed to update overdue loans: {ex.Message}");
		}
    }

    /// <summary>
    /// Gets a paged report of overdue loans for staff (UC042)
    /// </summary>
    public async Task<Result<PagedResult<LoanBasicDto>>> GetOverdueLoansReportPagedAsync(PagedRequest request)
    {
        var now = DateTime.UtcNow;
        var pagedLoans = await _unitOfWork.LoanRepository.GetOverdueLoansPagedAsync(request, now);
        var dtos = pagedLoans.Items.Select(_mapper.Map<LoanBasicDto>).ToList();
        return Result.Success(new PagedResult<LoanBasicDto>(dtos, pagedLoans.Count, pagedLoans.Page, pagedLoans.PageSize));
    }

    /// <summary>
    /// Gets all overdue loans for staff (UC042)
    /// </summary>
    public async Task<Result<List<OverdueLoanReportDto>>> GetOverdueLoansReportAsync()
    {
        var now = DateTime.UtcNow;
        var loans = await _unitOfWork.LoanRepository.GetOverdueLoansAsync(now);
        var dtos = loans.Select(_mapper.Map<OverdueLoanReportDto>).ToList();
        return Result.Success(dtos);
    }

    #region Private Helper Methods

    /// <summary>
    /// Builds a predicate for searching loans based on provided parameters
    /// </summary>
    private static Expression<Func<Loan, bool>> BuildLoanSearchPredicate(LoanSearchRequest searchParams)
    {
        Expression<Func<Loan, bool>> predicate = l => true;

        // Apply member filter
        if (searchParams.MemberId.HasValue)
        {
            var memberId = searchParams.MemberId.Value;
            predicate = predicate.And(l => l.MemberId == memberId);
        }

        // Apply book copy filter
        if (searchParams.BookCopyId.HasValue)
        {
            var bookCopyId = searchParams.BookCopyId.Value;
            predicate = predicate.And(l => l.BookCopyId == bookCopyId);
        }

        // Apply book filter
        if (searchParams.BookId.HasValue)
        {
            var bookId = searchParams.BookId.Value;
            predicate = predicate.And(l => l.BookCopy.BookId == bookId);
        }

        // Apply status filter
        if (searchParams.Status.HasValue)
        {
            var status = searchParams.Status.Value;
            predicate = predicate.And(l => l.Status == status);
        }

        // Apply date range filter
        if (searchParams.FromDate.HasValue)
        {
            var fromDate = searchParams.FromDate.Value.Date;
            predicate = predicate.And(l => l.LoanDate >= fromDate);
        }

        if (searchParams.ToDate.HasValue)
        {
            var toDate = searchParams.ToDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            predicate = predicate.And(l => l.LoanDate <= toDate);
        }

        return predicate;
    }

    /// <summary>
    /// Helper to get overridden rules for CreateLoan
    /// </summary>
    private static List<string> GetOverriddenRulesForCreateLoan(CreateLoanRequest request, Member member, BookCopy bookCopy, int activeLoansCount)
    {
        var overridden = new List<string>();
        if (member.MembershipStatus != MembershipStatus.Active)
            overridden.Add("MembershipStatus");
        if (member.OutstandingFines > 0)
            overridden.Add("OutstandingFines");
        if (activeLoansCount >= MaxActiveLoansPerMember)
            overridden.Add("MaxActiveLoans");
        if (bookCopy.Status != CopyStatus.Available)
            overridden.Add("CopyStatus");
        if (request.CustomDueDate.HasValue && (request.CustomDueDate.Value - DateTime.UtcNow).TotalDays > MaxLoanPeriodDays)
            overridden.Add("MaxLoanPeriod");
        if (request.CustomDueDate.HasValue && request.CustomDueDate.Value <= DateTime.UtcNow)
            overridden.Add("DueDateFuture");
        return overridden;
    }

    /// <summary>
    /// Helper to get overridden rules for RenewLoan
    /// </summary>
    private static List<string> GetOverriddenRulesForRenewLoan(RenewLoanRequest request, Loan loan)
    {
        var overridden = new List<string>();
        if (loan.Status != LoanStatus.Active)
            overridden.Add("LoanStatus");
        if (loan.Member.OutstandingFines > 0)
            overridden.Add("OutstandingFines");
        if (request.NewDueDate.HasValue && (request.NewDueDate.Value - DateTime.UtcNow).TotalDays > MaxLoanPeriodDays)
            overridden.Add("MaxExtensionPeriod");
        if (request.NewDueDate.HasValue && request.NewDueDate.Value <= DateTime.UtcNow)
            overridden.Add("DueDateFuture");
        return overridden;
    }

    #endregion
}

// Extension method to combine expressions with logical AND
public static class ExpressionExtensions
{
    public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
    {
        var parameter = Expression.Parameter(typeof(T));

        var leftVisitor = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter);
        var left = leftVisitor.Visit(expr1.Body) ?? Expression.Constant(true);

        var rightVisitor = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter);
        var right = rightVisitor.Visit(expr2.Body) ?? Expression.Constant(true);

        return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
    }

    private sealed class ReplaceExpressionVisitor : System.Linq.Expressions.ExpressionVisitor
    {
        private readonly Expression _oldValue;
        private readonly Expression _newValue;

        public ReplaceExpressionVisitor(Expression oldValue, Expression newValue)
        {
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public override Expression? Visit(Expression? node)
        {
            if (node == _oldValue)
                return _newValue;
            return base.Visit(node);
        }
    }
}