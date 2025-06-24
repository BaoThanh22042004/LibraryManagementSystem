using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Members.Commands;

public record DeleteMemberCommand(int Id) : IRequest<Result>;

public class DeleteMemberCommandHandler : IRequestHandler<DeleteMemberCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMemberCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteMemberCommand request, CancellationToken cancellationToken)
    {
        var memberRepository = _unitOfWork.Repository<Member>();
        var loanRepository = _unitOfWork.Repository<Loan>();
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        var fineRepository = _unitOfWork.Repository<Fine>();
        
        // Get member with related entities
        var member = await memberRepository.GetAsync(
            m => m.Id == request.Id,
            m => m.Loans,
            m => m.Reservations,
            m => m.Fines
        );
        
        if (member == null)
        {
            return Result.Failure($"Member with ID {request.Id} not found.");
        }
        
        // Check for active loans
        var hasActiveLoans = member.Loans.Any(l => l.Status == Domain.Enums.LoanStatus.Active);
        if (hasActiveLoans)
        {
            return Result.Failure("Cannot delete member with active loans. Please return all books first.");
        }
        
        // Check for active reservations
        var hasActiveReservations = member.Reservations.Any(r => r.Status == Domain.Enums.ReservationStatus.Active);
        if (hasActiveReservations)
        {
            return Result.Failure("Cannot delete member with active reservations. Please cancel all reservations first.");
        }
        
        // Check for unpaid fines
        var hasUnpaidFines = member.Fines.Any(f => f.Status == Domain.Enums.FineStatus.Pending);
        if (hasUnpaidFines)
        {
            return Result.Failure("Cannot delete member with unpaid fines. Please clear all fines first.");
        }
        
        // Delete member
        memberRepository.Delete(member);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}