using Application.Common;
using Application.DTOs;
using Application.Features.Users.Commands;
using Application.Features.Users.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Services;

public class UserService : IUserService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UserService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<UserDto>> GetPaginatedUsersAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetUsersQuery(request.PageNumber, request.PageSize, searchTerm));
    }

    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        return await _mediator.Send(new GetUserByIdQuery(id));
    }

    public async Task<UserDto?> GetUserByEmailAsync(string email)
    {
        return await _mediator.Send(new GetUserByEmailQuery(email));
    }

    public async Task<int> CreateUserAsync(CreateUserDto userDto, int currentUserId)
    {
        var result = await _mediator.Send(new CreateUserCommand(userDto, currentUserId));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateUserAsync(int id, UpdateUserDto userDto)
    {
        var result = await _mediator.Send(new UpdateUserCommand(id, userDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteUserAsync(int id, int currentUserId)
    {
        var result = await _mediator.Send(new DeleteUserCommand(id, currentUserId));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var userRepository = _unitOfWork.Repository<User>();
        return await userRepository.ExistsAsync(u => u.Id == id);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        var userRepository = _unitOfWork.Repository<User>();
        return await userRepository.ExistsAsync(u => u.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase));
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
    {
        var result = await _mediator.Send(new ChangePasswordCommand(userId, changePasswordDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return true;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginDto loginDto)
    {
        return await _mediator.Send(new LoginCommand(loginDto));
    }
    
    public async Task<Result<bool>> UpdateProfileAsync(int userId, UpdateProfileDto profileDto)
    {
        return await _mediator.Send(new UpdateProfileCommand(userId, profileDto));
    }
    
    public async Task<Result<bool>> ForgotPasswordAsync(ForgotPasswordDto forgotPasswordDto)
    {
        return await _mediator.Send(new ForgotPasswordCommand(forgotPasswordDto));
    }
    
    public async Task<Result<bool>> ResetPasswordAsync(ResetPasswordDto resetPasswordDto)
    {
        return await _mediator.Send(new ResetPasswordCommand(resetPasswordDto));
    }
    
    public async Task<Result<bool>> ValidateResetTokenAsync(string email, string token)
    {
        return await _mediator.Send(new ValidateResetTokenCommand(email, token));
    }
    
    public async Task<Result<bool>> AdminUpdateUserAsync(int id, AdminUpdateUserDto userDto, int currentUserId)
    {
        return await _mediator.Send(new AdminUpdateUserCommand(id, userDto, currentUserId));
    }
    
    public async Task<UserDetailsDto?> GetUserDetailsAsync(int id, int currentUserId)
	{
        return await _mediator.Send(new GetUserDetailsQuery(id, currentUserId));
    }
    
    public async Task<Result<bool>> ValidateUserDeletionAsync(int id)
    {
        return await _mediator.Send(new ValidateUserDeletionCommand(id));
    }
    
    public async Task<Result<int>> RegisterMemberAsync(RegisterMemberDto registerDto)
    {
        return await _mediator.Send(new RegisterMemberCommand(registerDto));
    }
}