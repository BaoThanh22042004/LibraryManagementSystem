using AutoMapper;
using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for Category entity and DTOs
/// </summary>
public class CategoryProfile : Profile
{
    /// <summary>
    /// Configures the mappings between Category entity and related DTOs
    /// </summary>
    public CategoryProfile()
    {
        // Map from CreateCategoryDto to Category entity
        CreateMap<CreateCategoryRequest, Category>()
            .ForMember(dest => dest.Books, opt => opt.Ignore()); // Ignore Books collection

        // Map from UpdateCategoryDto to Category entity
        CreateMap<UpdateCategoryRequest, Category>()
            .ForMember(dest => dest.Books, opt => opt.Ignore()); // Ignore Books collection

        // Map from Category entity to CategoryDto
        CreateMap<Category, CategoryDto>();

        // Map from Category entity to CategoryWithBooksDto
        CreateMap<Category, CategoryWithBooksDto>()
            .ForMember(dest => dest.Books, opt => opt.MapFrom(src => src.Books));
            
        // Map from Book entity to BookBasicDto for use in CategoryWithBooksDto
        CreateMap<Book, BookBasicDto>()
            .ForMember(dest => dest.TotalCopies, opt => opt.MapFrom(src => src.Copies.Count))
            .ForMember(dest => dest.AvailableCopies, opt => opt.MapFrom(src => 
                src.Copies.Count(c => c.Status == Domain.Enums.CopyStatus.Available)));
    }
}