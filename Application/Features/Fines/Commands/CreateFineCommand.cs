using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Commands;

/// <summary>
/// Command to manually create a fine (UC026 - Calculate Fine, Alternative Flow 26.3).
/// </summary>
/// <remarks>
/// This implementation follows UC026 specifications for manual fine creation:
/// - Validates member exists in the system
/// - Validates loan exists if specified
/// - Creates a fine record with pending status
/// - Sets the fine amount, type, and description
/// - Updates member's outstanding balance
/// - Records fine creation in the audit log
/// </remarks>
public record CreateFineCommand(CreateFineDto FineDto) : IRequest<Result<int>>;

public class CreateFineCommandHandler : IRequestHandler<CreateFineCommand, Result<int>>
{
    private readonly IUnitOfWork _unitOfWork;

    public CreateFineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateFineCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var fineRepository = _unitOfWork.Repository<Fine>();
            var memberRepository = _unitOfWork.Repository<Member>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Check if member exists (PRE-3: Fine must be linked to a valid member account)
            var member = await memberRepository.GetAsync(
                m => m.Id == request.FineDto.MemberId,
                m => m.User
            );
            
            if (member == null)
                return Result.Failure<int>($"Member with ID {request.FineDto.MemberId} not found.");
            
            // Check if loan exists if loanId is provided
            var loanTitle = string.Empty;
            if (request.FineDto.LoanId.HasValue)
            {
                var loanRepository = _unitOfWork.Repository<Loan>();
                var loan = await loanRepository.GetAsync(
                    l => l.Id == request.FineDto.LoanId.Value,
                    l => l.BookCopy,
                    l => l.BookCopy.Book
                );
                
                if (loan == null)
                    return Result.Failure<int>($"Loan with ID {request.FineDto.LoanId.Value} not found.");
                    
                loanTitle = loan.BookCopy?.Book?.Title ?? "Unknown Book";
            }
            
            // Create fine (POST-2: Fine record is created with pending status)
            var fine = new Fine
            {
                MemberId = request.FineDto.MemberId,
                LoanId = request.FineDto.LoanId,
                Type = request.FineDto.Type,
                Amount = request.FineDto.Amount,
                Description = request.FineDto.Description,
                FineDate = DateTime.Now,
                Status = FineStatus.Pending
            };
            
            await fineRepository.AddAsync(fine);
            
            // Update member's outstanding fines (POST-3: Member's outstanding balance is updated)
            member.OutstandingFines += fine.Amount;
            memberRepository.Update(member);
            
            // Record fine creation in audit log
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Fine",
                EntityId = fine.Id.ToString(),
                EntityName = $"Manual Fine",
                ActionType = AuditActionType.FineCreated,
                Details = $"Manually created {fine.Type} fine of {fine.Amount:C} for member '{member.User?.FullName ?? $"ID: {member.Id}"}'. Description: {fine.Description}",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(fine.Id); // POST-4, POST-5: Fine is linked to member and changes saved
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure<int>($"Failed to create fine: {ex.Message}");
        }
    }
}