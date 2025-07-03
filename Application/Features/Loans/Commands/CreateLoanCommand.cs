using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

/// <summary>
/// Command to check out a book to a member (UC018 - Check Out).
/// </summary>
/// <remarks>
/// This implementation follows UC018 specifications:
/// - Validates member exists with active status
/// - Validates book copy exists and is available
/// - Ensures member is eligible to borrow (no excessive fines or restrictions)
/// - Creates loan record with active status
/// - Updates book copy status to borrowed
/// - Establishes due date based on library policy (default 14 days)
/// - Records checkout transaction for audit purposes
/// </remarks>
public record CreateLoanCommand(CreateLoanDto LoanDto) : IRequest<Result<int>>;

public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private const int DefaultLoanDays = 14;

    public CreateLoanCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<int>> Handle(CreateLoanCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var memberRepository = _unitOfWork.Repository<Member>();
            var bookCopyRepository = _unitOfWork.Repository<BookCopy>();
            var loanRepository = _unitOfWork.Repository<Loan>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Validate member exists (PRE-3: Member must have active membership status)
            var member = await memberRepository.GetAsync(m => m.Id == request.LoanDto.MemberId);
            if (member == null)
                return Result.Failure<int>($"Member with ID {request.LoanDto.MemberId} not found."); // UC018.E1: Member Not Found
            
            // Check if member is active
            if (member.MembershipStatus != MembershipStatus.Active)
                return Result.Failure<int>($"Member with ID {request.LoanDto.MemberId} is not active. Current status: {member.MembershipStatus}."); // UC018.E2: Member Not Active
            
            // Check if member has excessive fines (PRE-5: Member must be eligible to borrow)
            if (member.OutstandingFines > 10.00m) // Threshold for excessive fines
                return Result.Failure<int>($"Member has excessive outstanding fines (${member.OutstandingFines:F2}). Maximum allowed is $10.00.");
            
            // Check active loans count
            var activeLoansCount = await loanRepository.CountAsync(l => 
                l.MemberId == member.Id && 
                (l.Status == LoanStatus.Active || l.Status == LoanStatus.Overdue));
                
            if (activeLoansCount >= 5) // Maximum allowed loans
                return Result.Failure<int>("Member has reached the maximum number of active loans (5).");
            
            // Validate book copy exists (PRE-4: Book copy must be available for borrowing)
            var bookCopy = await bookCopyRepository.GetAsync(
                bc => bc.Id == request.LoanDto.BookCopyId,
                bc => bc.Book
            );
            
            if (bookCopy == null)
                return Result.Failure<int>($"Book copy with ID {request.LoanDto.BookCopyId} not found.");
            
            // Check if book copy is available
            if (bookCopy.Status != CopyStatus.Available)
                return Result.Failure<int>($"Book copy is not available. Current status: {bookCopy.Status}."); // UC018.E3: Copy Not Available
            
            // Create loan record (POST-1: Loan record is created with active status)
            var loan = new Loan
            {
                MemberId = request.LoanDto.MemberId,
                BookCopyId = request.LoanDto.BookCopyId,
                LoanDate = DateTime.Now,
                Status = LoanStatus.Active
            };
            
            // Set due date (POST-3: Due date is established based on library policy)
            if (request.LoanDto.CustomDueDate.HasValue)
            {
                // Validate custom due date (UC018.1: Custom Due Date)
                if (request.LoanDto.CustomDueDate.Value <= loan.LoanDate)
                    return Result.Failure<int>("Due date must be after loan date."); // UC018.E4: Invalid Due Date
                
                loan.DueDate = request.LoanDto.CustomDueDate.Value;
            }
            else
            {
                // Use default loan period (14 days)
                loan.DueDate = loan.LoanDate.AddDays(DefaultLoanDays);
            }
            
            await loanRepository.AddAsync(loan);
            
            // Update book copy status (POST-2: Book copy status is updated to borrowed)
            bookCopy.Status = CopyStatus.Borrowed;
            bookCopyRepository.Update(bookCopy);
            
            // Record checkout in audit log (POST-5: Checkout transaction is recorded for audit purposes)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Loan",
                EntityId = loan.Id.ToString(),
                EntityName = $"Checkout of '{bookCopy.Book.Title}'",
                ActionType = AuditActionType.BookCheckout,
                Details = $"Book '{bookCopy.Book.Title}' (Copy #{bookCopy.CopyNumber}) checked out to member ID {member.Id}. Due date: {loan.DueDate:yyyy-MM-dd}.",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(loan.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create loan: {ex.Message}"); // UC018.E5: System Error
        }
    }
}