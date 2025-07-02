using Application.Common;
using Application.Interfaces;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands;

public record ValidateResetTokenCommand(string Email, string Token) : IRequest<Result<bool>>;

public class ValidateResetTokenCommandHandler : IRequestHandler<ValidateResetTokenCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;

    public ValidateResetTokenCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<bool>> Handle(ValidateResetTokenCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        var resetTokenRepository = _unitOfWork.Repository<PasswordResetToken>();
        
        // Get user by email
        var user = await userRepository.GetAsync(u => u.Email.Equals(request.Email, StringComparison.CurrentCultureIgnoreCase));
        
        if (user == null)
        {
            return Result.Failure<bool>("Invalid or expired password reset token.");
        }
        
        // Check if token is valid and not expired
        var tokenExists = await resetTokenRepository.ExistsAsync(
            t => t.UserId == user.Id && 
                 t.Token == request.Token && 
                 !t.IsUsed && 
                 t.ExpiresAt > DateTime.UtcNow);
                 
        if (!tokenExists)
        {
            return Result.Failure<bool>("Invalid or expired password reset token.");
        }
        
        return Result.Success(true);
    }
}