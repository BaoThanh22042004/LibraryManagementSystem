using Application.Common;
using Application.DTOs;
using Application.Features.Reservations.Commands;
using Application.Features.Reservations.Queries;
using Application.Interfaces;
using Application.Interfaces.Services;
using AutoMapper;
using Domain.Entities;
using MediatR;

namespace Application.Services;

public class ReservationService : IReservationService
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReservationService(IMediator mediator, IUnitOfWork unitOfWork, IMapper mapper)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<ReservationDto>> GetPaginatedReservationsAsync(PagedRequest request, string? searchTerm = null)
    {
        return await _mediator.Send(new GetPaginatedReservationsQuery(request, searchTerm));
    }

    public async Task<ReservationDto?> GetReservationByIdAsync(int id)
    {
        return await _mediator.Send(new GetReservationByIdQuery(id));
    }

    public async Task<List<ReservationDto>> GetReservationsByMemberIdAsync(int memberId)
    {
        return await _mediator.Send(new GetReservationsByMemberIdQuery(memberId));
    }

    public async Task<List<ReservationDto>> GetReservationsByBookIdAsync(int bookId)
    {
        return await _mediator.Send(new GetReservationsByBookIdQuery(bookId));
    }

    public async Task<int> CreateReservationAsync(CreateReservationDto reservationDto)
    {
        var result = await _mediator.Send(new CreateReservationCommand(reservationDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
            
        return result.Value;
    }

    public async Task UpdateReservationAsync(int id, UpdateReservationDto reservationDto)
    {
        var result = await _mediator.Send(new UpdateReservationCommand(id, reservationDto));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task DeleteReservationAsync(int id)
    {
        var result = await _mediator.Send(new DeleteReservationCommand(id));
        
        if (result.IsFailure)
            throw new InvalidOperationException(result.Error);
    }

    public async Task<bool> ExistsAsync(int id)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        return await reservationRepository.ExistsAsync(r => r.Id == id);
    }

    public async Task<bool> CancelReservationAsync(int id)
    {
        var result = await _mediator.Send(new CancelReservationCommand(id));
        return result.IsSuccess;
    }

    public async Task<bool> FulfillReservationAsync(int id, int bookCopyId)
    {
        var result = await _mediator.Send(new FulfillReservationCommand(id, bookCopyId));
        return result.IsSuccess;
    }

    public async Task<List<ReservationDto>> GetActiveReservationsAsync()
    {
        return await _mediator.Send(new GetActiveReservationsQuery());
    }

    public async Task<bool> HasActiveReservationAsync(int memberId, int bookId)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        return await reservationRepository.ExistsAsync(r => 
            r.MemberId == memberId && 
            r.BookId == bookId && 
            r.Status == Domain.Enums.ReservationStatus.Active);
    }
}