using AutoMapper;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for Reservation entity and DTOs
/// </summary>
public class ReservationProfile : Profile
{
    /// <summary>
    /// Configures the mappings between Reservation entity and related DTOs
    /// </summary>
    public ReservationProfile()
    {
        // Map from CreateReservationDto to Reservation entity
        CreateMap<CreateReservationRequest, Reservation>()
            .ForMember(dest => dest.ReservationDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => ReservationStatus.Active))
            .ForMember(dest => dest.BookCopyId, opt => opt.Ignore())
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.Book, opt => opt.Ignore())
            .ForMember(dest => dest.BookCopy, opt => opt.Ignore());

        // Map from Reservation entity to ReservationBasicDto
        CreateMap<Reservation, ReservationBasicDto>()
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member.User.FullName))
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.Book.Title))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Member.UserId))
            .ForMember(dest => dest.QueuePosition, opt => opt.Ignore()) // Queue position calculated in service
            .ForMember(dest => dest.PickupDeadline, opt => opt.Ignore()); // Pickup deadline calculated in service

        // Map from Reservation entity to ReservationDetailDto
        CreateMap<Reservation, ReservationDetailDto>()
            .IncludeBase<Reservation, ReservationBasicDto>()
            .ForMember(dest => dest.BookAuthor, opt => opt.MapFrom(src => src.Book.Author))
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.Book.ISBN))
            .ForMember(dest => dest.CopyNumber, opt => opt.MapFrom(src => src.BookCopy != null ? src.BookCopy.CopyNumber : null))
            .ForMember(dest => dest.EstimatedAvailabilityDate, opt => opt.Ignore()) // Calculated in service
            .ForMember(dest => dest.MemberEmail, opt => opt.MapFrom(src => src.Member.User.Email))
            .ForMember(dest => dest.MemberPhone, opt => opt.MapFrom(src => src.Member.User.Phone));
    }
}