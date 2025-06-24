using Application.Common;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands;

public record DeleteUserCommand(int Id) : IRequest<Result>;

public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteUserCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        var memberRepository = _unitOfWork.Repository<Domain.Entities.Member>();
        var librarianRepository = _unitOfWork.Repository<Domain.Entities.Librarian>();
        
        // Get user
        var user = await userRepository.GetAsync(
            u => u.Id == request.Id,
            u => u.Member!,
            u => u.Librarian!,
            u => u.Notifications
        );
        
        if (user == null)
        {
            return Result.Failure($"User with ID {request.Id} not found.");
        }
        
        // Check for related entities and delete them first
        if (user.Member != null)
        {
            // Check if member has active loans or reservations
            var member = await memberRepository.GetAsync(
                m => m.Id == user.Member.Id,
                m => m.Loans,
                m => m.Reservations,
                m => m.Fines
            );
            
            if (member != null)
            {
                var hasActiveLoans = member.Loans.Any(l => l.Status == Domain.Enums.LoanStatus.Active);
                if (hasActiveLoans)
                {
                    return Result.Failure("Cannot delete user with active loans. Please return all books first.");
                }
                
                var hasActiveReservations = member.Reservations.Any(r => r.Status == Domain.Enums.ReservationStatus.Active);
                if (hasActiveReservations)
                {
                    return Result.Failure("Cannot delete user with active reservations. Please cancel all reservations first.");
                }
                
                var hasUnpaidFines = member.Fines.Any(f => f.Status == Domain.Enums.FineStatus.Pending);
                if (hasUnpaidFines)
                {
                    return Result.Failure("Cannot delete user with unpaid fines. Please clear all fines first.");
                }
                
                // Delete member
                memberRepository.Delete(member);
            }
        }
        
        if (user.Librarian != null)
        {
            // Delete librarian
            librarianRepository.Delete(user.Librarian);
        }
        
        // Delete user
        userRepository.Delete(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}