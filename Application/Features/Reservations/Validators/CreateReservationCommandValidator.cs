using Application.Features.Reservations.Commands;
using Application.Interfaces.Services;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Reservations.Validators;

public class CreateReservationCommandValidator : AbstractValidator<CreateReservationCommand>
{
    private readonly IReservationService _reservationService;
    private readonly IBookService _bookService;
    private readonly IMemberService _memberService;

    public CreateReservationCommandValidator(
        IReservationService reservationService,
        IBookService bookService,
        IMemberService memberService)
    {
        _reservationService = reservationService;
        _bookService = bookService;
        _memberService = memberService;

        RuleFor(x => x.ReservationDto.MemberId)
            .GreaterThan(0).WithMessage("Member ID is required")
            .MustAsync(async (memberId, cancellation) => {
                var memberExists = await _memberService.ExistsAsync(memberId);
                return memberExists;
            }).WithMessage("Member does not exist")
            .MustAsync(async (memberId, cancellation) => {
                var isActive = await _memberService.IsMemberActiveAsync(memberId);
                return isActive;
            }).WithMessage("Member account is not active");

        RuleFor(x => x.ReservationDto.BookId)
            .GreaterThan(0).WithMessage("Book ID is required")
            .MustAsync(async (bookId, cancellation) => {
                var bookExists = await _bookService.ExistsAsync(bookId);
                return bookExists;
            }).WithMessage("Book does not exist");

        RuleFor(x => x.ReservationDto)
            .MustAsync(async (dto, cancellation) => {
                // Check if the member already has an active reservation for this book
                var hasActiveReservation = await _reservationService.HasActiveReservationAsync(dto.MemberId, dto.BookId);
                return !hasActiveReservation;
            }).WithMessage("Member already has an active reservation for this book")
            .MustAsync(async (dto, cancellation) => {
                // Check if the member has reached their reservation limit
                var reservationCount = await _reservationService.GetReservationCountForMemberAsync(dto.MemberId);
                return reservationCount < 3; // Maximum 3 active reservations
            }).WithMessage("Member has reached the maximum number of active reservations (3)")
            .MustAsync(async (dto, cancellation) => {
                // Check if there are any available copies of the book
                var availableCopies = await _bookService.GetAvailableCopyCountAsync(dto.BookId);
                return availableCopies == 0; // Reservations only allowed if no copies available
            }).WithMessage("Reservations are only allowed when no copies are available");
    }
}