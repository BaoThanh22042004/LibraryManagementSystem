using AutoMapper;
using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for BookCopy entity and DTOs
/// </summary>
public class BookCopyProfile : Profile
{
    /// <summary>
    /// Configures the mappings between BookCopy entity and related DTOs
    /// </summary>
    public BookCopyProfile()
    {
        // Map from CreateBookCopyDto to BookCopy entity
        CreateMap<CreateBookCopyDto, BookCopy>()
            .ForMember(dest => dest.Book, opt => opt.Ignore())
            .ForMember(dest => dest.Loans, opt => opt.Ignore())
            .ForMember(dest => dest.Reservations, opt => opt.Ignore());

        // Map from BookCopy entity to BookCopyBasicDto
        CreateMap<BookCopy, BookCopyBasicDto>();

        // Map from BookCopy entity to BookCopyDetailDto
        CreateMap<BookCopy, BookCopyDetailDto>()
            .ForMember(dest => dest.Book, opt => opt.MapFrom(src => src.Book))
            .ForMember(dest => dest.HasActiveLoans, opt => opt.MapFrom(src => 
                src.Loans.Any(l => l.Status == Domain.Enums.LoanStatus.Active)))
            .ForMember(dest => dest.HasActiveReservations, opt => opt.MapFrom(src => 
                src.Reservations.Any(r => r.Status == Domain.Enums.ReservationStatus.Active)));
    }
}