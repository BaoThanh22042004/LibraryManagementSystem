using Application.DTOs;
using AutoMapper;
using Domain.Entities;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for user-related mapping.
/// </summary>
public class UserProfile : Profile
{
    public UserProfile()
    {
        // User to UserDetailsResponse
        CreateMap<User, UserDetailsResponse>()
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.LastModifiedAt))
            .ForMember(dest => dest.MemberDetails, opt => opt.MapFrom(src => src.Member))
            .ForMember(dest => dest.LibrarianDetails, opt => opt.MapFrom(src => src.Librarian));
        
        // Member to MemberDetailsResponse
        CreateMap<Member, MemberDetailsResponse>()
            .ForMember(dest => dest.ActiveLoans, opt => opt.MapFrom(src => 
                src.Loans.Count(l => l.Status == Domain.Enums.LoanStatus.Active)))
            .ForMember(dest => dest.ActiveReservations, opt => opt.MapFrom(src => 
                src.Reservations.Count(r => r.Status == Domain.Enums.ReservationStatus.Active)));
        
        // Librarian to LibrarianDetailsResponse
        CreateMap<Librarian, LibrarianDetailsResponse>();
        
        // User to UserSummaryDto
        CreateMap<User, UserSummaryDto>()
            .ForMember(dest => dest.MembershipNumber, opt => opt.MapFrom(src => 
                src.Member != null ? src.Member.MembershipNumber : null))
            .ForMember(dest => dest.MembershipStatus, opt => opt.MapFrom(src => 
                src.Member != null ? src.Member.MembershipStatus : (Domain.Enums.MembershipStatus?)null))
            .ForMember(dest => dest.EmployeeId, opt => opt.MapFrom(src => 
                src.Librarian != null ? src.Librarian.EmployeeId : null));
        
        // CreateUserRequest to User
        CreateMap<CreateUserRequest, User>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.PendingEmail, opt => opt.Ignore())
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => Domain.Enums.UserStatus.Active))
            .ForMember(dest => dest.FailedLoginAttempts, opt => opt.Ignore())
            .ForMember(dest => dest.LockoutEndTime, opt => opt.Ignore())
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.Librarian, opt => opt.Ignore())
            .ForMember(dest => dest.Notifications, opt => opt.Ignore())
            .ForMember(dest => dest.LastModifiedAt, opt => opt.Ignore());
    }
}
