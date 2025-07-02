using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using Domain.Enums;
using MediatR;

namespace Application.Features.Users.Commands;

public record ValidateUserDeletionCommand(int UserId) : IRequest<Result<bool>>;

public class ValidateUserDeletionCommandHandler : IRequestHandler<ValidateUserDeletionCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ValidateUserDeletionCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ValidateUserDeletionCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Get user
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        if (user == null)
        {
            return Result.Failure<bool>($"User with ID {request.UserId} not found.");
        }
        
        // Validate based on user role
        if (user.Role == UserRole.Member)
        {
            // For members, check if they have active loans, reservations, or unpaid fines
            var memberRepository = _unitOfWork.Repository<Member>();
            var member = await memberRepository.GetAsync(
                m => m.UserId == request.UserId,
                m => m.Loans,
                m => m.Reservations,
                m => m.Fines);
                
            if (member == null)
            {
                // If there's no member record, it's safe to delete
                return Result.Success(true);
            }
            
            // Check for active loans
            if (member.Loans.Any(l => !l.ReturnDate.HasValue))
            {
                return Result.Failure<bool>("Member has active loans and cannot be deleted.");
            }
            
            // Check for active reservations
            if (member.Reservations.Any(r => r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled))
            {
                return Result.Failure<bool>("Member has active reservations and cannot be deleted.");
            }
            
            // Check for unpaid fines
            if (member.OutstandingFines > 0 || member.Fines.Any(f => f.Status == FineStatus.Pending))
            {
                return Result.Failure<bool>("Member has unpaid fines and cannot be deleted.");
            }
        }
        else if (user.Role == UserRole.Librarian)
        {
            // For librarians, potentially check for any pending assignments or responsibilities
            // This would depend on your specific business requirements
            
            var librarianRepository = _unitOfWork.Repository<Librarian>();
            var librarian = await librarianRepository.GetAsync(l => l.UserId == request.UserId);
            
            if (librarian == null)
            {
                // If there's no librarian record, it's safe to delete
                return Result.Success(true);
            }
            
            // Check if this is the only admin/librarian in the system
            var adminCount = await userRepository.CountAsync(u => u.Role == UserRole.Admin);
            var librarianCount = await userRepository.CountAsync(u => u.Role == UserRole.Librarian);
            
            if (user.Role == UserRole.Admin && adminCount <= 1)
            {
                return Result.Failure<bool>("Cannot delete the only administrator account.");
            }
            
            if (user.Role == UserRole.Librarian && librarianCount <= 1 && adminCount == 0)
            {
                return Result.Failure<bool>("Cannot delete the only staff account.");
            }
            
            // Additional checks for librarian responsibilities could be added here
        }
        
        return Result.Success(true);
    }
}