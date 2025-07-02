using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Features.Users.Commands;

public record UpdateProfileCommand(int UserId, UpdateProfileDto ProfileDto) : IRequest<Result<bool>>;

public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateProfileCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<Result<bool>> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
    {
        var userRepository = _unitOfWork.Repository<User>();
        
        // Get user by ID
        var user = await userRepository.GetAsync(u => u.Id == request.UserId);
        
        if (user == null)
        {
            return Result.Failure<bool>($"User with ID {request.UserId} not found.");
        }
        
        // Check if the email is changed and if so, check if it's already in use
        if (!user.Email.Equals(request.ProfileDto.Email, StringComparison.CurrentCultureIgnoreCase))
        {
            var emailExists = await userRepository.ExistsAsync(u => 
                u.Id != request.UserId && 
                u.Email.Equals(request.ProfileDto.Email, StringComparison.CurrentCultureIgnoreCase));
                
            if (emailExists)
            {
                return Result.Failure<bool>($"Email '{request.ProfileDto.Email}' is already in use.");
            }
        }
        
        try
        {
            // Update the user profile
            user.FullName = request.ProfileDto.FullName;
            user.Email = request.ProfileDto.Email;
            user.LastModifiedAt = DateTime.UtcNow;
            
            userRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return Result.Success(true);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>($"Failed to update profile: {ex.Message}");
        }
    }
}