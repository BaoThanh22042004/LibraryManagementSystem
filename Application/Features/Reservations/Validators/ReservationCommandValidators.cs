using Application.Features.Reservations.Commands;
using Application.Interfaces.Services;
using Domain.Enums;
using FluentValidation;

namespace Application.Features.Reservations.Validators;

public class UpdateReservationCommandValidator : AbstractValidator<UpdateReservationCommand>
{
    private readonly IReservationService _reservationService;
    private readonly IBookService _bookService;

    public UpdateReservationCommandValidator(
        IReservationService reservationService,
        IBookService bookService)
    {
        _reservationService = reservationService;
        _bookService = bookService;

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Reservation ID is required")
            .MustAsync(async (id, cancellation) => {
                var exists = await _reservationService.ExistsAsync(id);
                return exists;
            }).WithMessage("Reservation does not exist");

        RuleFor(x => x.ReservationDto.Status)
            .IsInEnum().WithMessage("Invalid reservation status");

        When(x => x.ReservationDto.BookCopyId.HasValue, () => {
            RuleFor(x => x.ReservationDto.BookCopyId!.Value)
                .GreaterThan(0).WithMessage("Book copy ID must be greater than 0")
                .MustAsync(async (copyId, cancellation) => {
                    var exists = await _bookService.ExistsCopyAsync(copyId);
                    return exists;
                }).WithMessage("Book copy does not exist");
        });
    }
}

public class CancelReservationCommandValidator : AbstractValidator<CancelReservationCommand>
{
    private readonly IReservationService _reservationService;

    public CancelReservationCommandValidator(IReservationService reservationService)
    {
        _reservationService = reservationService;

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Reservation ID is required")
            .MustAsync(async (id, cancellation) => {
                var exists = await _reservationService.ExistsAsync(id);
                return exists;
            }).WithMessage("Reservation does not exist");
    }
}

public class FulfillReservationCommandValidator : AbstractValidator<FulfillReservationCommand>
{
    private readonly IReservationService _reservationService;
    private readonly IBookService _bookService;

    public FulfillReservationCommandValidator(
        IReservationService reservationService,
        IBookService bookService)
    {
        _reservationService = reservationService;
        _bookService = bookService;

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Reservation ID is required")
            .MustAsync(async (id, cancellation) => {
                var exists = await _reservationService.ExistsAsync(id);
                return exists;
            }).WithMessage("Reservation does not exist");

        RuleFor(x => x.BookCopyId)
            .GreaterThan(0).WithMessage("Book copy ID is required")
            .MustAsync(async (copyId, cancellation) => {
                var exists = await _bookService.ExistsCopyAsync(copyId);
                return exists;
            }).WithMessage("Book copy does not exist")
            .MustAsync(async (copyId, cancellation) => {
                var isCopyAvailable = await _bookService.IsCopyAvailableAsync(copyId);
                return isCopyAvailable;
            }).WithMessage("Book copy is not available");
    }
}

public class DeleteReservationCommandValidator : AbstractValidator<DeleteReservationCommand>
{
    private readonly IReservationService _reservationService;

    public DeleteReservationCommandValidator(IReservationService reservationService)
    {
        _reservationService = reservationService;

        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Reservation ID is required")
            .MustAsync(async (id, cancellation) => {
                var exists = await _reservationService.ExistsAsync(id);
                return exists;
            }).WithMessage("Reservation does not exist");
    }
}