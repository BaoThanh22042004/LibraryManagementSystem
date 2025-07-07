using AutoMapper;
using Application.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mapping;

/// <summary>
/// AutoMapper profile for Loan entity and DTOs
/// </summary>
public class LoanProfile : Profile
{
    /// <summary>
    /// Configures the mappings between Loan entity and related DTOs
    /// </summary>
    public LoanProfile()
    {
        // Map from CreateLoanDto to Loan entity
        CreateMap<CreateLoanRequest, Loan>()
            .ForMember(dest => dest.LoanDate, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.CustomDueDate ?? DateTime.UtcNow.AddDays(14))) // Default loan period is 14 days
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => LoanStatus.Active))
            .ForMember(dest => dest.ReturnDate, opt => opt.Ignore())
            .ForMember(dest => dest.Member, opt => opt.Ignore())
            .ForMember(dest => dest.BookCopy, opt => opt.Ignore())
            .ForMember(dest => dest.Fines, opt => opt.Ignore());

        // Map from Loan entity to LoanBasicDto
        CreateMap<Loan, LoanBasicDto>()
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member.User.FullName))
            .ForMember(dest => dest.BookId, opt => opt.MapFrom(src => src.BookCopy.BookId))
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.BookCopy.Book.Title))
            .ForMember(dest => dest.MemberEmail, opt => opt.MapFrom(src => src.Member.User.Email))
            .ForMember(dest => dest.MemberPhone, opt => opt.MapFrom(src => src.Member.User.Phone))
            .ForMember(dest => dest.MemberAddress, opt => opt.MapFrom(src => src.Member.User.Address))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Member.UserId));

        // Map from Loan entity to LoanDetailDto
        CreateMap<Loan, LoanDetailDto>()
            .IncludeBase<Loan, LoanBasicDto>()
            .ForMember(dest => dest.Fines, opt => opt.MapFrom(src => src.Fines))
            .ForMember(dest => dest.CopyNumber, opt => opt.MapFrom(src => src.BookCopy.CopyNumber))
            .ForMember(dest => dest.BookAuthor, opt => opt.MapFrom(src => src.BookCopy.Book.Author))
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.BookCopy.Book.ISBN));

        // Map from Loan entity to OverdueLoanReportDto
        CreateMap<Loan, OverdueLoanReportDto>()
            .ForMember(dest => dest.LoanId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member.User.FullName))
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.BookCopy.Book.Title))
            .ForMember(dest => dest.LoanDate, opt => opt.MapFrom(src => src.LoanDate))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => src.DueDate))
            .ForMember(dest => dest.DaysOverdue, opt => opt.MapFrom(src => src.Status == Domain.Enums.LoanStatus.Overdue ? (int)(DateTime.UtcNow - src.DueDate).TotalDays : 0))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
    }
}