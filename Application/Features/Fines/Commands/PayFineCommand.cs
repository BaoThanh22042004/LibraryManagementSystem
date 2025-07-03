using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Fines.Commands;

/// <summary>
/// Command to process a fine payment (UC027 - Pay Fine).
/// </summary>
/// <remarks>
/// This implementation follows UC027 specifications:
/// - Validates fine exists and is in pending status
/// - Updates fine status to paid
/// - Reduces member's outstanding balance
/// - Records payment transaction
/// - Supports individual and batch payment processing
/// </remarks>
public record PayFineCommand(int Id) : IRequest<Result>;

public class PayFineCommandHandler : IRequestHandler<PayFineCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public PayFineCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(PayFineCommand request, CancellationToken cancellationToken)
    {
        await _unitOfWork.BeginTransactionAsync();
        
        try
        {
            var fineRepository = _unitOfWork.Repository<Fine>();
            var memberRepository = _unitOfWork.Repository<Member>();
            var auditRepository = _unitOfWork.Repository<AuditLog>();
            
            // Get fine with member data (PRE-1, PRE-3: Valid fine record must exist and be linked to member)
            var fine = await fineRepository.GetAsync(
                f => f.Id == request.Id,
                f => f.Member,
                f => f.Member.User,
                f => f.Loan
            );
            
            if (fine == null || fine.Member == null)
                return Result.Failure($"Fine with ID {request.Id} not found or has invalid member data."); // UC027.E1: Fine Not Found
            
            // Check if the fine is already paid or waived (PRE-2: Fine must be in pending status)
            if (fine.Status != FineStatus.Pending)
                return Result.Failure($"Only pending fines can be paid. Current status: {fine.Status}"); // UC027.E2: Fine Already Processed
            
            // Store details for audit logging
            var memberName = fine.Member?.User?.FullName ?? $"Member ID: {fine.MemberId}";
            var fineDescription = fine.Description;
            var fineAmount = fine.Amount;
            
            // Update fine status (POST-1: Fine status is updated to paid)
            fine.Status = FineStatus.Paid;
            fine.LastModifiedAt = DateTime.Now;
            fineRepository.Update(fine);
            
            // Update member's outstanding fines (POST-2: Member's outstanding balance is reduced)
            var member = fine.Member;
            member.OutstandingFines -= fine.Amount;
            memberRepository.Update(member);
            
            // Record payment in audit log (POST-3: Payment transaction is recorded in the system)
            await auditRepository.AddAsync(new AuditLog
            {
                EntityType = "Fine",
                EntityId = fine.Id.ToString(),
                EntityName = $"Fine Payment",
                ActionType = AuditActionType.FinePaid,
                Details = $"Payment of {fineAmount:C} for fine: '{fineDescription}' by member '{memberName}'.",
                IsSuccess = true
            });
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitTransactionAsync();
            
            return Result.Success(); // POST-4, POST-5: Payment confirmation provided and changes saved
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync();
            return Result.Failure($"Failed to pay fine: {ex.Message}"); // UC027.E3: Payment Processing Error
        }
    }
}