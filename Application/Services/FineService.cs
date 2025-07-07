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
/// Implementation of the fine management service
/// Supports UC026-UC029 (fine calculation, payment, waiver, and history)
/// </summary>
public class FineService : IFineService
{
	private readonly IUnitOfWork _unitOfWork;
	private readonly IMapper _mapper;
	private readonly ILogger<FineService> _logger;

	private const decimal DefaultDailyFineRate = 0.50m; // $0.50 per day
	private const decimal DefaultMaximumFineAmount = 25.00m; // $25.00 cap for overdue fines

	public FineService(
		IUnitOfWork unitOfWork,
		IMapper mapper,
		ILogger<FineService> logger)
	{
		_unitOfWork = unitOfWork;
		_mapper = mapper;
		_logger = logger;
	}

	/// <summary>
	/// Creates a new fine manually - UC026
	/// Supports staff override for fine creation restrictions.
	/// </summary>
	/// <param name="request">Fine creation details</param>
	/// <param name="allowOverride">If true, allows staff to override certain business rules</param>
	/// <param name="overrideReason">Reason for override (required if allowOverride is true)</param>
	/// <returns>Result with created fine information and override context</returns>
	public async Task<Result<FineDetailDto>> CreateFineAsync(CreateFineRequest request, bool allowOverride = false, string? overrideReason = null)
	{
		try
		{
			await _unitOfWork.BeginTransactionAsync();

			var member = await _unitOfWork.Repository<Member>().GetAsync(
				m => m.Id == request.MemberId,
				m => m.User);

			if (member == null)
				return Result.Failure<FineDetailDto>($"Member with ID {request.MemberId} not found.");

			Loan? loan = null;
			if (request.LoanId.HasValue)
			{
				loan = await _unitOfWork.Repository<Loan>().GetAsync(
					l => l.Id == request.LoanId.Value && l.MemberId == request.MemberId,
					l => l.BookCopy.Book);

				if (loan == null && !allowOverride)
					return Result.Failure<FineDetailDto>($"Loan with ID {request.LoanId.Value} not found or does not belong to the specified member.");
			}

			// Validate fine amount
			if ((request.Amount <= 0 || request.Amount > 1000) && !allowOverride)
				return Result.Failure<FineDetailDto>("Fine amount must be between $0.01 and $1000.");

			// Validate description
			if (string.IsNullOrWhiteSpace(request.Description) && !allowOverride)
				return Result.Failure<FineDetailDto>("Description is required.");

			var fine = _mapper.Map<Fine>(request);
			fine.FineDate = DateTime.UtcNow;
			fine.Status = FineStatus.Pending;

			await _unitOfWork.Repository<Fine>().AddAsync(fine);
			member.OutstandingFines += fine.Amount;
			_unitOfWork.Repository<Member>().Update(member);
			await _unitOfWork.SaveChangesAsync();
			await _unitOfWork.CommitTransactionAsync();
			var createdFine = await _unitOfWork.Repository<Fine>().GetAsync(
				f => f.Id == fine.Id,
				f => f.Member.User,
				f => f.Loan!.BookCopy.Book);
			var fineDto = _mapper.Map<FineDetailDto>(createdFine);
			if (loan != null)
			{
				fineDto.BookTitle = loan.BookCopy.Book.Title;
				fineDto.DueDate = loan.DueDate;
				fineDto.ReturnDate = loan.ReturnDate;
				if (loan.ReturnDate.HasValue && loan.ReturnDate.Value > loan.DueDate)
				{
					fineDto.DaysOverdue = (int)(loan.ReturnDate.Value - loan.DueDate).TotalDays;
				}
			}
			if (allowOverride && !string.IsNullOrWhiteSpace(overrideReason))
			{
				fineDto.OverrideContext = new FineOverrideContext
				{
					IsOverride = true,
					Reason = overrideReason,
					OverriddenRules = GetOverriddenRulesForCreateFine(request, loan)
				};
			}
			return Result.Success(fineDto);
		}
		catch (Exception ex)
		{
			await _unitOfWork.RollbackTransactionAsync();
			_logger.LogError(ex, "Error creating fine: {ErrorMessage}", ex.Message);
			return Result.Failure<FineDetailDto>($"Failed to create fine: {ex.Message}");
		}
	}

	/// <summary>
	/// Calculates a fine for an overdue loan - UC026
	/// </summary>
	/// <param name="request">Calculation details</param>
	/// <returns>Result with calculated fine information</returns>
	public async Task<Result<FineDetailDto>> CalculateFineAsync(CalculateFineRequest request)
	{
		try
		{
			// Begin transaction
			await _unitOfWork.BeginTransactionAsync();

			// Get the loan with related entities
			var loan = await _unitOfWork.Repository<Loan>().GetAsync(
				l => l.Id == request.LoanId,
				l => l.Member.User,
				l => l.BookCopy.Book);

			if (loan == null)
				return Result.Failure<FineDetailDto>($"Loan with ID {request.LoanId} not found.");

			// Check if the loan is overdue
			bool isOverdue = loan.Status == LoanStatus.Overdue;

			// If the loan is returned, check if it was returned late
			bool isReturnedLate = loan.ReturnDate.HasValue && loan.ReturnDate.Value > loan.DueDate;

			if (!isOverdue && !isReturnedLate)
				return Result.Failure<FineDetailDto>("The loan is not overdue. No fine calculation needed.");

			// Calculate days overdue - BR-15
			int daysOverdue;
			if (loan.ReturnDate.HasValue)
			{
				// For returned loans, calculate based on actual return date
				daysOverdue = (int)(loan.ReturnDate.Value - loan.DueDate).TotalDays;
			}
			else
			{
				// For active loans, calculate based on current date
				daysOverdue = (int)(DateTime.UtcNow - loan.DueDate).TotalDays;
			}

			if (daysOverdue <= 0)
				return Result.Failure<FineDetailDto>("The loan is not overdue. No fine calculation needed.");

			// Determine daily rate (use custom rate if provided)
			decimal dailyRate = request.CustomDailyRate ?? DefaultDailyFineRate;

			// Calculate fine amount
			decimal fineAmount = daysOverdue * dailyRate;

			// Apply maximum fine limit if specified or use default cap
			decimal maximumFine = request.MaximumFineAmount ?? DefaultMaximumFineAmount;
			if (fineAmount > maximumFine)
			{
				fineAmount = maximumFine;
			}

			// Check if this loan already has an overdue fine
			var existingFine = await _unitOfWork.Repository<Fine>().GetAsync(
				f => f.LoanId == loan.Id && f.Type == FineType.Overdue && f.Status == FineStatus.Pending);

			if (existingFine != null)
				return Result.Failure<FineDetailDto>($"This loan already has an overdue fine (ID: {existingFine.Id}). Update the existing fine instead of creating a new one.");

			// Create the fine entity
			var fine = new Fine
			{
				MemberId = loan.MemberId,
				LoanId = loan.Id,
				Type = FineType.Overdue,
				Amount = fineAmount,
				FineDate = DateTime.UtcNow,
				Status = FineStatus.Pending,
				Description = $"Overdue fine for {daysOverdue} days. Book: '{loan.BookCopy.Book.Title}'. " +
							 $"{request.AdditionalDescription}".Trim()
			};

			// Save the fine
			await _unitOfWork.Repository<Fine>().AddAsync(fine);

			// Update the member's outstanding fine amount
			var member = loan.Member;
			member.OutstandingFines += fine.Amount;
			_unitOfWork.Repository<Member>().Update(member);

			await _unitOfWork.SaveChangesAsync();

			// Commit transaction
			await _unitOfWork.CommitTransactionAsync();

			// Reload the fine with related entities
			var createdFine = await _unitOfWork.Repository<Fine>().GetAsync(
				f => f.Id == fine.Id,
				f => f.Member.User,
				f => f.Loan!.BookCopy.Book);

			// Map and return the result
			var fineDto = _mapper.Map<FineDetailDto>(createdFine);
			fineDto.BookTitle = loan.BookCopy.Book.Title;
			fineDto.DueDate = loan.DueDate;
			fineDto.ReturnDate = loan.ReturnDate;
			fineDto.DaysOverdue = daysOverdue;

			return Result.Success(fineDto);
		}
		catch (Exception ex)
		{
			// Rollback on error
			await _unitOfWork.RollbackTransactionAsync();
			_logger.LogError(ex, "Error calculating fine: {ErrorMessage}", ex.Message);
			return Result.Failure<FineDetailDto>($"Failed to calculate fine: {ex.Message}");
		}
	}

	/// <summary>
	/// Processes payment for a fine - UC027
	/// </summary>
	/// <param name="request">Payment details</param>
	/// <returns>Result with updated fine information</returns>
	public async Task<Result<FineDetailDto>> PayFineAsync(PayFineRequest request)
	{
		try
		{
			// Begin transaction
			await _unitOfWork.BeginTransactionAsync();

			// Get the fine with related entities
			var fine = await _unitOfWork.Repository<Fine>().GetAsync(
				f => f.Id == request.FineId,
				f => f.Member.User,
				f => f.Loan!.BookCopy.Book);

			if (fine == null)
				return Result.Failure<FineDetailDto>($"Fine with ID {request.FineId} not found.");

			// Check if fine can be paid (must be pending)
			if (fine.Status != FineStatus.Pending)
				return Result.Failure<FineDetailDto>($"Fine cannot be paid. Current status: {fine.Status}.");

			// Validate payment amount
			if (request.PaymentAmount <= 0)
				return Result.Failure<FineDetailDto>("Payment amount must be greater than zero.");

			if (request.PaymentAmount > fine.Amount)
				return Result.Failure<FineDetailDto>($"Payment amount ({request.PaymentAmount:C}) exceeds fine amount ({fine.Amount:C}).");

			// Process the payment
			if (request.PaymentAmount < fine.Amount)
			{
				// Partial payment - create a new fine for the remaining amount
				var remainingAmount = fine.Amount - request.PaymentAmount;

				var remainingFine = new Fine
				{
					MemberId = fine.MemberId,
					LoanId = fine.LoanId,
					Type = fine.Type,
					Amount = remainingAmount,
					FineDate = DateTime.UtcNow,
					Status = FineStatus.Pending,
					Description = $"Remaining balance after partial payment. Original fine: {fine.Description}"
				};

				await _unitOfWork.Repository<Fine>().AddAsync(remainingFine);

				// Update member's outstanding balance to reflect the payment
				fine.Member.OutstandingFines -= request.PaymentAmount;
			}
			else
			{
				// Full payment - update member's outstanding balance
				fine.Member.OutstandingFines -= fine.Amount;
			}

			// Update the fine
			fine.Status = FineStatus.Paid;
			_unitOfWork.Repository<Member>().Update(fine.Member);
			_unitOfWork.Repository<Fine>().Update(fine);

			await _unitOfWork.SaveChangesAsync();

			// Commit transaction
			await _unitOfWork.CommitTransactionAsync();

			// Map and return the result
			var fineDto = _mapper.Map<FineDetailDto>(fine);

			// Set payment details
			fineDto.PaymentDate = DateTime.UtcNow;
			fineDto.PaymentMethod = request.PaymentMethod;
			fineDto.PaymentReference = request.PaymentReference;

			// Set loan-related fields if applicable
			if (fine.Loan != null)
			{
				fineDto.BookTitle = fine.Loan.BookCopy.Book.Title;
				fineDto.DueDate = fine.Loan.DueDate;
				fineDto.ReturnDate = fine.Loan.ReturnDate;

				if (fine.Loan.ReturnDate.HasValue && fine.Loan.ReturnDate.Value > fine.Loan.DueDate)
				{
					fineDto.DaysOverdue = (int)(fine.Loan.ReturnDate.Value - fine.Loan.DueDate).TotalDays;
				}
			}

			return Result.Success(fineDto);
		}
		catch (Exception ex)
		{
			// Rollback on error
			await _unitOfWork.RollbackTransactionAsync();
			_logger.LogError(ex, "Error processing fine payment: {ErrorMessage}", ex.Message);
			return Result.Failure<FineDetailDto>($"Failed to process payment: {ex.Message}");
		}
	}

	/// <summary>
	/// Waives a fine - UC028
	/// Supports staff override for waiver restrictions.
	/// </summary>
	/// <param name="request">Waiver details</param>
	/// <param name="allowOverride">If true, allows staff to override certain business rules</param>
	/// <param name="overrideReason">Reason for override (required if allowOverride is true)</param>
	/// <returns>Result with updated fine information and override context</returns>
	public async Task<Result<FineDetailDto>> WaiveFineAsync(WaiveFineRequest request, bool allowOverride = false, string? overrideReason = null)
	{
		try
		{
			await _unitOfWork.BeginTransactionAsync();

			var fine = await _unitOfWork.Repository<Fine>().GetAsync(
				f => f.Id == request.FineId,
				f => f.Member.User,
				f => f.Loan!.BookCopy.Book);

			if (fine == null)
				return Result.Failure<FineDetailDto>($"Fine with ID {request.FineId} not found.");

			if (fine.Status != FineStatus.Pending && !allowOverride)
				return Result.Failure<FineDetailDto>($"Fine cannot be waived. Current status: {fine.Status}.");

			var staffMember = await _unitOfWork.Repository<User>().GetAsync(u => u.Id == request.StaffId);
			if (staffMember == null && !allowOverride)
				return Result.Failure<FineDetailDto>($"Staff member with ID {request.StaffId} not found.");

			if (string.IsNullOrWhiteSpace(request.WaiverReason) && !allowOverride)
				return Result.Failure<FineDetailDto>("Waiver reason is required.");

			fine.Status = FineStatus.Waived;
			fine.Member.OutstandingFines -= fine.Amount;
			_unitOfWork.Repository<Member>().Update(fine.Member);
			_unitOfWork.Repository<Fine>().Update(fine);
			await _unitOfWork.SaveChangesAsync();
			await _unitOfWork.CommitTransactionAsync();
			var fineDto = _mapper.Map<FineDetailDto>(fine);
			fineDto.WaiverReason = request.WaiverReason;
			fineDto.ProcessedByStaffId = request.StaffId;
			fineDto.ProcessedByStaffName = staffMember?.FullName;
			if (fine.Loan != null)
			{
				fineDto.BookTitle = fine.Loan.BookCopy.Book.Title;
				fineDto.DueDate = fine.Loan.DueDate;
				fineDto.ReturnDate = fine.Loan.ReturnDate;
				if (fine.Loan.ReturnDate.HasValue && fine.Loan.ReturnDate.Value > fine.Loan.DueDate)
				{
					fineDto.DaysOverdue = (int)(fine.Loan.ReturnDate.Value - fine.Loan.DueDate).TotalDays;
				}
			}
			if (allowOverride && !string.IsNullOrWhiteSpace(overrideReason))
			{
				fineDto.OverrideContext = new FineOverrideContext
				{
					IsOverride = true,
					Reason = overrideReason,
					OverriddenRules = GetOverriddenRulesForWaiveFine(fine, staffMember, request)
				};
			}
			return Result.Success(fineDto);
		}
		catch (Exception ex)
		{
			await _unitOfWork.RollbackTransactionAsync();
			_logger.LogError(ex, "Error waiving fine: {ErrorMessage}", ex.Message);
			return Result.Failure<FineDetailDto>($"Failed to waive fine: {ex.Message}");
		}
	}

	/// <summary>
	/// Helper to get overridden rules for CreateFine
	/// </summary>
	private static List<string> GetOverriddenRulesForCreateFine(CreateFineRequest request, Loan? loan)
	{
		var overridden = new List<string>();
		if (request.Amount <= 0 || request.Amount > 1000)
			overridden.Add("FineAmount");
		if (string.IsNullOrWhiteSpace(request.Description))
			overridden.Add("Description");
		if (request.LoanId.HasValue && loan == null)
			overridden.Add("LoanExists");
		return overridden;
	}

	/// <summary>
	/// Helper to get overridden rules for WaiveFine
	/// </summary>
	private static List<string> GetOverriddenRulesForWaiveFine(Fine fine, User? staffMember, WaiveFineRequest request)
	{
		var overridden = new List<string>();
		if (fine.Status != FineStatus.Pending)
			overridden.Add("FineStatus");
		if (staffMember == null)
			overridden.Add("StaffExists");
		if (string.IsNullOrWhiteSpace(request.WaiverReason))
			overridden.Add("WaiverReason");
		return overridden;
	}

	/// <summary>
	/// Gets fine details by ID
	/// </summary>
	/// <param name="fineId">ID of the fine to retrieve</param>
	/// <returns>Result with fine details</returns>
	public async Task<Result<FineDetailDto>> GetFineByIdAsync(int fineId)
	{
		try
		{
			var fine = await _unitOfWork.Repository<Fine>().GetAsync(
				f => f.Id == fineId,
				f => f.Member.User,
				f => f.Loan!.BookCopy.Book);

			if (fine == null)
				return Result.Failure<FineDetailDto>($"Fine with ID {fineId} not found.");

			var fineDto = _mapper.Map<FineDetailDto>(fine);

			// Set loan-related fields if applicable
			if (fine.Loan != null)
			{
				fineDto.BookTitle = fine.Loan.BookCopy.Book.Title;
				fineDto.DueDate = fine.Loan.DueDate;
				fineDto.ReturnDate = fine.Loan.ReturnDate;

				if (fine.Loan.ReturnDate.HasValue && fine.Loan.ReturnDate.Value > fine.Loan.DueDate)
				{
					fineDto.DaysOverdue = (int)(fine.Loan.ReturnDate.Value - fine.Loan.DueDate).TotalDays;
				}
			}

			return Result.Success(fineDto);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving fine: {ErrorMessage}", ex.Message);
			return Result.Failure<FineDetailDto>($"Failed to retrieve fine: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets all pending fines for a member
	/// </summary>
	/// <param name="memberId">ID of the member</param>
	/// <returns>Result with list of pending fines</returns>
	public async Task<Result<IEnumerable<FineBasicDto>>> GetPendingFinesByMemberIdAsync(int memberId)
	{
		try
		{
			// Check if member exists
			var memberExists = await _unitOfWork.Repository<Member>().ExistsAsync(m => m.Id == memberId);
			if (!memberExists)
				return Result.Failure<IEnumerable<FineBasicDto>>($"Member with ID {memberId} not found.");

			// Get all pending fines for the member
			var fines = await _unitOfWork.Repository<Fine>().ListAsync(
				f => f.MemberId == memberId && f.Status == FineStatus.Pending,
				f => f.OrderByDescending(x => x.FineDate),
				true,
				f => f.Member.User,
				f => f.Loan!.BookCopy.Book);

			var fineDtos = _mapper.Map<IEnumerable<FineBasicDto>>(fines);
			return Result.Success(fineDtos);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving pending fines: {ErrorMessage}", ex.Message);
			return Result.Failure<IEnumerable<FineBasicDto>>($"Failed to retrieve pending fines: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets the total pending fine amount for a member
	/// </summary>
	/// <param name="memberId">ID of the member</param>
	/// <returns>Result with total pending fine amount</returns>
	public async Task<Result<decimal>> GetTotalPendingFineAmountAsync(int memberId)
	{
		try
		{
			// Check if member exists
			var member = await _unitOfWork.Repository<Member>().GetAsync(m => m.Id == memberId);
			if (member == null)
				return Result.Failure<decimal>($"Member with ID {memberId} not found.");

			// Return the cached total from the member entity
			return Result.Success(member.OutstandingFines);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving total pending fine amount: {ErrorMessage}", ex.Message);
			return Result.Failure<decimal>($"Failed to retrieve total pending fine amount: {ex.Message}");
		}
	}

	/// <summary>
	/// Gets all fines with search parameters and pagination - UC029
	/// </summary>
	/// <param name="request">Search parameters</param>
	/// <returns>Paged result with fines matching criteria</returns>
	public async Task<Result<PagedResult<FineBasicDto>>> GetFinesAsync(FineSearchRequest request)
	{
		try
		{
			// Build the predicate for filtering
			Expression<Func<Fine, bool>> predicate = BuildFineSearchPredicate(request);

			// Get paged fines
			var pagedFines = await _unitOfWork.Repository<Fine>().PagedListAsync(
				request,
				predicate,
				q => q.OrderByDescending(f => f.FineDate),
				true,
				f => f.Member.User,
				f => f.Loan!.BookCopy.Book);

			// Map to DTOs
			var fineDtos = _mapper.Map<IEnumerable<FineBasicDto>>(pagedFines.Items);

			// Create and return paged result
			return Result.Success(new PagedResult<FineBasicDto>
			{
				Items = [.. fineDtos],
				Page = pagedFines.Page,
				PageSize = pagedFines.PageSize,
				Count = pagedFines.Count,
			});
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving fines: {ErrorMessage}", ex.Message);
			return Result.Failure<PagedResult<FineBasicDto>>($"Failed to retrieve fines: {ex.Message}");

		}
	}

	/// <summary>
	/// Gets all fines for a specific loan
	/// </summary>
	/// <param name="loanId">ID of the loan</param>
	/// <returns>Result with list of fines for the loan</returns>
	public async Task<Result<IEnumerable<FineBasicDto>>> GetFinesByLoanIdAsync(int loanId)
	{
		try
		{
			// Check if loan exists
			var loanExists = await _unitOfWork.Repository<Loan>().ExistsAsync(l => l.Id == loanId);
			if (!loanExists)
				return Result.Failure<IEnumerable<FineBasicDto>>($"Loan with ID {loanId} not found.");

			// Get all fines for the loan
			var fines = await _unitOfWork.Repository<Fine>().ListAsync(
				f => f.LoanId == loanId,
				f => f.OrderByDescending(x => x.FineDate),
				true,
				f => f.Member.User,
				f => f.Loan!.BookCopy.Book);

			var fineDtos = _mapper.Map<IEnumerable<FineBasicDto>>(fines);
			return Result.Success(fineDtos);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error retrieving fines by loan: {ErrorMessage}", ex.Message);
			return Result.Failure<IEnumerable<FineBasicDto>>($"Failed to retrieve fines: {ex.Message}");
		}
	}

	/// <summary>
	/// Automatically generates fines for all overdue loans
	/// </summary>
	/// <returns>Count of fines generated</returns>
	public async Task<Result<int>> GenerateOverdueFinesAsync()
	{
		try
		{
			// Begin transaction
			await _unitOfWork.BeginTransactionAsync();

			// Get all overdue loans without existing pending overdue fines
			var overdueLoans = await _unitOfWork.Repository<Loan>().ListAsync(
				l => l.Status == LoanStatus.Overdue,
				null,
				false,
				l => l.Member,
				l => l.BookCopy.Book,
				l => l.Fines);

			// Filter out loans that already have a pending overdue fine
			var loansToFine = overdueLoans.Where(l => !l.Fines.Any(f => f.Type == FineType.Overdue && f.Status == FineStatus.Pending)).ToList();

			int finesGenerated = 0;

			// Process each loan
			foreach (var loan in loansToFine)
			{
				// Calculate days overdue - BR-15
				int daysOverdue = (int)(DateTime.UtcNow - loan.DueDate).TotalDays;

				if (daysOverdue <= 0)
					continue;

				// Calculate fine amount
				decimal fineAmount = daysOverdue * DefaultDailyFineRate;

				// Apply maximum fine limit
				if (fineAmount > DefaultMaximumFineAmount)
				{
					fineAmount = DefaultMaximumFineAmount;
				}

				// Create the fine entity
				var fine = new Fine
				{
					MemberId = loan.MemberId,
					LoanId = loan.Id,
					Type = FineType.Overdue,
					Amount = fineAmount,
					FineDate = DateTime.UtcNow,
					Status = FineStatus.Pending,
					Description = $"Automatically generated overdue fine for {daysOverdue} days. Book: '{loan.BookCopy.Book.Title}'"
				};

				// Save the fine
				await _unitOfWork.Repository<Fine>().AddAsync(fine);

				// Update the member's outstanding fine amount
				loan.Member.OutstandingFines += fine.Amount;
				_unitOfWork.Repository<Member>().Update(loan.Member);

				finesGenerated++;
			}

			// Save changes
			await _unitOfWork.SaveChangesAsync();

			// Commit transaction
			await _unitOfWork.CommitTransactionAsync();

			return Result.Success(finesGenerated);
		}
		catch (Exception ex)
		{
			// Rollback on error
			await _unitOfWork.RollbackTransactionAsync();
			_logger.LogError(ex, "Error generating overdue fines: {ErrorMessage}", ex.Message);
			return Result.Failure<int>($"Error generating overdue fines: {ex.Message}"); // Updated error handling
		}
	}

	/// <summary>
	/// Gets a paged report of unpaid fines for staff (UC043)
	/// </summary>
	public async Task<Result<PagedResult<FineBasicDto>>> GetFinesReportPagedAsync(PagedRequest request)
	{
		var pagedFines = await _unitOfWork.FineRepository.GetUnpaidFinesPagedAsync(request);
		var dtos = pagedFines.Items.Select(_mapper.Map<FineBasicDto>).ToList();
		return Result.Success(new PagedResult<FineBasicDto>(dtos, pagedFines.Count, pagedFines.Page, pagedFines.PageSize));
	}

	/// <summary>
	/// Gets all unpaid fines for staff (UC043)
	/// </summary>
	public async Task<Result<List<FineBasicDto>>> GetFinesReportAsync()
	{
		var fines = await _unitOfWork.FineRepository.GetUnpaidFinesAsync();
		var dtos = fines.Select(_mapper.Map<FineBasicDto>).ToList();
		return Result.Success(dtos);
	}

	/// <summary>
	/// Gets the outstanding fines for a specific member (UC044)
	/// </summary>
	public async Task<Result<OutstandingFinesDto>> GetOutstandingFinesAsync(int memberId)
	{
		var total = await _unitOfWork.FineRepository.GetOutstandingFinesForMemberAsync(memberId);
		var member = await _unitOfWork.Repository<Member>().GetAsync(m => m.Id == memberId);
		if (member == null)
			return Result.Failure<OutstandingFinesDto>("Member not found.");
		var dto = new OutstandingFinesDto
		{
			MemberId = member.Id,
			MemberName = member.User?.FullName ?? string.Empty,
			TotalOutstanding = total
		};
		return Result.Success(dto);
	}

	#region Private Helper Methods

	/// <summary>
	/// Builds a predicate for searching fines based on provided parameters
	/// </summary>
	private static Expression<Func<Fine, bool>> BuildFineSearchPredicate(FineSearchRequest searchParams)
	{
		Expression<Func<Fine, bool>> predicate = f => true;

		// Apply member filter
		if (searchParams.MemberId.HasValue)
		{
			var memberId = searchParams.MemberId.Value;
			predicate = predicate.And(f => f.MemberId == memberId);
		}

		// Apply loan filter
		if (searchParams.LoanId.HasValue)
		{
			var loanId = searchParams.LoanId.Value;
			predicate = predicate.And(f => f.LoanId == loanId);
		}

		// Apply type filter
		if (searchParams.Type.HasValue)
		{
			var type = searchParams.Type.Value;
			predicate = predicate.And(f => f.Type == type);
		}

		// Apply status filter
		if (searchParams.Status.HasValue)
		{
			var status = searchParams.Status.Value;
			predicate = predicate.And(f => f.Status == status);
		}

		// Apply amount range filter
		if (searchParams.MinAmount.HasValue)
		{
			var minAmount = searchParams.MinAmount.Value;
			predicate = predicate.And(f => f.Amount >= minAmount);
		}

		if (searchParams.MaxAmount.HasValue)
		{
			var maxAmount = searchParams.MaxAmount.Value;
			predicate = predicate.And(f => f.Amount <= maxAmount);
		}

		// Apply date range filter
		if (searchParams.FromDate.HasValue)
		{
			var fromDate = searchParams.FromDate.Value.Date;
			predicate = predicate.And(f => f.FineDate >= fromDate);
		}

		if (searchParams.ToDate.HasValue)
		{
			var toDate = searchParams.ToDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
			predicate = predicate.And(f => f.FineDate <= toDate);
		}

		return predicate;
	}

	#endregion
}