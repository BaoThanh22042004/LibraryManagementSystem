using AutoMapper;
using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for Book entity and DTOs
/// </summary>
public class BookProfile : Profile
{
    /// <summary>
    /// Configures the mappings between Book entity and related DTOs
    /// </summary>
    public BookProfile()
    {
        // Map from CreateBookDto to Book entity
        CreateMap<CreateBookRequest, Book>()
            .ForMember(dest => dest.Categories, opt => opt.Ignore()) // Categories handled separately
            .ForMember(dest => dest.Copies, opt => opt.Ignore()); // Copies created separately
            
        // Map from UpdateBookDto to Book entity
        CreateMap<UpdateBookRequest, Book>()
            .ForMember(dest => dest.Categories, opt => opt.Ignore()) // Categories handled separately
            .ForMember(dest => dest.Copies, opt => opt.Ignore()); // Copies not updated through book update
            
        // Map from Book entity to BookBasicDto
        CreateMap<Book, BookBasicDto>()
            .ForMember(dest => dest.TotalCopies, opt => opt.MapFrom(src => src.Copies.Count))
            .ForMember(dest => dest.AvailableCopies, opt => opt.MapFrom(src => 
                src.Copies.Count(c => c.Status == Domain.Enums.CopyStatus.Available)));
                
        // Map from Book entity to BookDetailDto
        CreateMap<Book, BookDetailDto>()
            .ForMember(dest => dest.TotalCopies, opt => opt.MapFrom(src => src.Copies.Count))
            .ForMember(dest => dest.AvailableCopies, opt => opt.MapFrom(src => 
                src.Copies.Count(c => c.Status == Domain.Enums.CopyStatus.Available)))
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => src.Categories))
            .ForMember(dest => dest.Copies, opt => opt.MapFrom(src => src.Copies));
    }
}