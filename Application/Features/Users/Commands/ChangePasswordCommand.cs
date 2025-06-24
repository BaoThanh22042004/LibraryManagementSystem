using Application.Common;
using Application.Common.Security;
using Application.DTOs;
using Application.Interfaces;
using MediatR;

namespace Application.Features.Users.Commands;

public record ChangePasswordCommand(int UserId, ChangePasswordDto PasswordDto) : IRequest<Result>;

public class ChangePasswordCommandHandler : IRequestHandler<ChangePasswordCommand, Result>
{
    private readonly IUnitOfWork _unitOfWork;

    public ChangePasswordCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // Validate new password match
        if (request.PasswordDto.NewPassword != request.PasswordDto.ConfirmNewPassword)
        {
            return Result.Failure("New password and confirmation password do not match.");
        }
        
        var userRepository = _unitOfWork.Repository<Domain.Entities.User>();
        
        // Get current user using GetAsync instead of GetByIdAsync
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        
        if (user == null)
        {
            return Result.Failure($"User with ID {request.UserId} not found.");
        }
        
        // Verify current password
        if (!PasswordHasher.VerifyPassword(request.PasswordDto.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure("Current password is incorrect.");
        }
        
        // Update password hash
        user.PasswordHash = PasswordHasher.HashPassword(request.PasswordDto.NewPassword);
        
        userRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        return Result.Success();
    }
}