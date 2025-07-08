using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for authentication-related mapping.
/// </summary>
public class AuthProfile : Profile
{
    public AuthProfile()
    {
        // User to LoginResponse
        CreateMap<User, LoginResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id));

		// RegisterRequest to User
		CreateMap<RegisterRequest, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Domain.Enums.UserRole.Member))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.UserStatus.Active))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PendingEmail, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginAttempts, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEndTime, opt => opt.Ignore())
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.Librarian, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore())
            .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore());
    }
}
