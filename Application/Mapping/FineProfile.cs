using AutoMapper;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for Fine entity and DTOs
/// </summary>
public class FineProfile : Profile
{
    /// <summary>
    /// Configures the mappings between Fine entity and related DTOs
    /// </summary>
    public FineProfile()
    {
        // Map from CreateFineDto to Fine entity
        CreateMap<CreateFineRequest, Fine>()
            .ForMember(dest => dest.FineDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => FineStatus.Pending))
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.Loan, opt => opt.Ignore());

        // Map from Fine entity to FineBasicDto
        CreateMap<Fine, FineBasicDto>()
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member.User.FullName))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Member.UserId))
            .ForMember(dest => dest.MemberEmail, opt => opt.MapFrom(src => src.Member.User.Email))
            .ForMember(dest => dest.MemberPhone, opt => opt.MapFrom(src => src.Member.User.Phone))
            .ForMember(dest => dest.MemberAddress, opt => opt.MapFrom(src => src.Member.User.Address));

        // Map from Fine entity to FineDetailDto
        CreateMap<Fine, FineDetailDto>()
            .IncludeBase<Fine, FineBasicDto>()
            .ForMember(dest => dest.BookTitle, opt => 
                opt.MapFrom(src => src.Loan != null ? src.Loan.BookCopy.Book.Title : null))
            .ForMember(dest => dest.DueDate, opt => 
                opt.MapFrom(src => src.Loan != null ? (DateTime?)src.Loan.DueDate : null))
            .ForMember(dest => dest.ReturnDate, opt => 
                opt.MapFrom(src => src.Loan != null ? src.Loan.ReturnDate : null))
            .ForMember(dest => dest.DaysOverdue, opt => 
                opt.MapFrom(src => src.Loan != null && src.Loan.ReturnDate.HasValue && src.Loan.ReturnDate > src.Loan.DueDate ? 
                    (int?)(src.Loan.ReturnDate.Value - src.Loan.DueDate).TotalDays : null))
            .ForMember(dest => dest.PaymentDate, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.PaymentReference, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.WaiverReason, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.ProcessedByStaffId, opt => opt.Ignore()) // Set in service
            .ForMember(dest => dest.ProcessedByStaffName, opt => opt.Ignore()); // Set in service
    }
}