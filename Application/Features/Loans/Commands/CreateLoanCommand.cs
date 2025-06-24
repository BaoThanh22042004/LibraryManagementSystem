using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Loans.Commands;

public record CreateLoanCommand(CreateLoanDto LoanDto) : IRequest<Result<int>>;

public class CreateLoanCommandHandler : IRequestHandler<CreateLoanCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

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
            
            // Validate member exists
            var member = await memberRepository.GetAsync(m => m.Id == request.LoanDto.MemberId);
            if (member == null)
                return Result.Failure<int>($"Member with ID {request.LoanDto.MemberId} not found.");
            
            // Check if member is active
            if (member.MembershipStatus != MembershipStatus.Active)
                return Result.Failure<int>($"Member with ID {request.LoanDto.MemberId} is not active. Current status: {member.MembershipStatus}.");
            
            // Validate book copy exists
            var bookCopy = await bookCopyRepository.GetAsync(
                bc => bc.Id == request.LoanDto.BookCopyId,
                bc => bc.Book
            );
            
            if (bookCopy == null)
                return Result.Failure<int>($"Book copy with ID {request.LoanDto.BookCopyId} not found.");
            
            // Check if book copy is available
            if (bookCopy.Status != CopyStatus.Available)
                return Result.Failure<int>($"Book copy is not available. Current status: {bookCopy.Status}.");
            
            // Check if due date is valid
            if (request.LoanDto.DueDate <= request.LoanDto.LoanDate)
                return Result.Failure<int>("Due date must be after loan date.");
            
            // Create loan
            var loan = _mapper.Map<Loan>(request.LoanDto);
            loan.Status = LoanStatus.Active;
            
            await loanRepository.AddAsync(loan);
            
            // Update book copy status
            bookCopy.Status = CopyStatus.Borrowed;
            bookCopyRepository.Update(bookCopy);
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(loan.Id);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create loan: {ex.Message}");
        }
    }
}