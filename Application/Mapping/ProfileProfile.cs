using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for user profile-related mapping.
/// </summary>
public class ProfileProfile : Profile
{
    public ProfileProfile()
    {
        // User to ProfileResponse
        CreateMap<User, ProfileDto>();
        
        // UpdateProfileRequest to User
        CreateMap<UpdateProfileRequest, User>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.PendingEmail, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.Role, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.Ignore())
            .ForMember(dest => dest.FailedLoginAttempts, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEndTime, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore())
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.Librarian, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore());
    }
}
