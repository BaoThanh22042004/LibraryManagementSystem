using Application.DTOs;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<User, UserDetailsDto>();
        CreateMap<CreateUserDto, User>();
        CreateMap<UpdateUserDto, User>();
        CreateMap<UpdateProfileDto, User>();
        CreateMap<AdminUpdateUserDto, User>();

        // Book mappings
        CreateMap<Book, BookDto>()
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => 
                src.Categories.Select(c => c.Name).ToList()))
            .ForMember(dest => dest.CopiesCount, opt => opt.MapFrom(src => 
                src.Copies.Count))
            .ForMember(dest => dest.AvailableCopiesCount, opt => opt.MapFrom(src => 
                src.Copies.Count(c => c.Status == CopyStatus.Available)));
                
        CreateMap<Book, BookDetailsDto>()
            .ForMember(dest => dest.Categories, opt => opt.MapFrom(src => 
                src.Categories.Select(c => c.Name).ToList()))
            .ForMember(dest => dest.CopiesCount, opt => opt.MapFrom(src => 
                src.Copies.Count))
            .ForMember(dest => dest.AvailableCopiesCount, opt => opt.MapFrom(src => 
                src.Copies.Count(c => c.Status == CopyStatus.Available)))
            .ForMember(dest => dest.CategoryDetails, opt => opt.MapFrom(src => 
                src.Categories));
                
        CreateMap<CreateBookDto, Book>();
        CreateMap<UpdateBookDto, Book>();

        // Category mappings
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.BooksCount, opt => opt.MapFrom(src => 
                src.Books.Count));
        CreateMap<Category, CategoryDetailsDto>()
            .ForMember(dest => dest.BooksCount, opt => opt.MapFrom(src => 
                src.Books.Count));
        CreateMap<CreateCategoryDto, Category>();
        CreateMap<UpdateCategoryDto, Category>();

        // BookCopy mappings
        CreateMap<BookCopy, BookCopyDto>()
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => 
                src.Book.Title));
        CreateMap<CreateBookCopyDto, BookCopy>();
        CreateMap<UpdateBookCopyDto, BookCopy>();

        // Member mappings
        CreateMap<Member, MemberDto>();
        CreateMap<Member, MemberDetailsDto>()
            .ForMember(dest => dest.ActiveLoans, opt => opt.MapFrom(src => 
                src.Loans.Where(l => l.Status == LoanStatus.Active)))
            .ForMember(dest => dest.ActiveReservations, opt => opt.MapFrom(src => 
                src.Reservations.Where(r => r.Status == ReservationStatus.Active)))
            .ForMember(dest => dest.UnpaidFines, opt => opt.MapFrom(src => 
                src.Fines.Where(f => f.Status == FineStatus.Pending)));
        CreateMap<CreateMemberDto, Member>();
        CreateMap<UpdateMemberDto, Member>();

        // Loan mappings
        CreateMap<Loan, LoanDto>();
        CreateMap<CreateLoanDto, Loan>()
            .ForMember(dest => dest.LoanDate, opt => opt.MapFrom(src => DateTime.Now))
            .ForMember(dest => dest.DueDate, opt => opt.MapFrom(src => 
                src.CustomDueDate ?? DateTime.Now.AddDays(14)))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => LoanStatus.Active));
        CreateMap<UpdateLoanDto, Loan>();
        
        // Report mappings
        CreateMap<Loan, OverdueLoanDto>()
            .ForMember(dest => dest.LoanId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member.User.FullName))
            .ForMember(dest => dest.MemberEmail, opt => opt.MapFrom(src => src.Member.User.Email))
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => src.BookCopy.Book.Title))
            .ForMember(dest => dest.ISBN, opt => opt.MapFrom(src => src.BookCopy.Book.ISBN))
            .ForMember(dest => dest.CopyNumber, opt => opt.MapFrom(src => src.BookCopy.CopyNumber));
            
        // Fine Report mappings
        CreateMap<Fine, FineReportItemDto>()
            .ForMember(dest => dest.FineId, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => src.Member.User.FullName))
            .ForMember(dest => dest.MemberEmail, opt => opt.MapFrom(src => src.Member.User.Email))
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => 
                src.Loan != null ? src.Loan.BookCopy.Book.Title : null));

        // Reservation mappings
        CreateMap<Reservation, ReservationDto>()
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => 
                src.Member.User.FullName))
            .ForMember(dest => dest.BookTitle, opt => opt.MapFrom(src => 
                src.Book.Title));
        CreateMap<CreateReservationDto, Reservation>();
        CreateMap<UpdateReservationDto, Reservation>();

        // Fine mappings
        CreateMap<Fine, FineDto>()
            .ForMember(dest => dest.MemberName, opt => opt.MapFrom(src => 
                src.Member.User.FullName));
        CreateMap<CreateFineDto, Fine>();
        CreateMap<UpdateFineDto, Fine>();

        // Notification mappings
        CreateMap<Notification, NotificationDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => 
                src.User != null ? src.User.FullName : null));
        CreateMap<CreateNotificationDto, Notification>();
        CreateMap<UpdateNotificationDto, Notification>();

        // Librarian mappings
        CreateMap<Librarian, LibrarianDto>();
        CreateMap<Librarian, LibrarianDetailsDto>();
        CreateMap<CreateLibrarianDto, Librarian>();
        CreateMap<UpdateLibrarianDto, Librarian>();
        
        // Password Reset Token mappings
        CreateMap<PasswordResetToken, PasswordResetTokenDto>();
    }
}