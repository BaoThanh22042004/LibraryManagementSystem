using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using MediatR;
using System.Linq.Expressions;

namespace Application.Features.Reservations.Queries;

public record GetPaginatedReservationsQuery(PagedRequest PagedRequest, string? SearchTerm = null) : IRequest<PagedResult<ReservationDto>>;

public class GetPaginatedReservationsQueryHandler : IRequestHandler<GetPaginatedReservationsQuery, PagedResult<ReservationDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPaginatedReservationsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<PagedResult<ReservationDto>> Handle(GetPaginatedReservationsQuery request, CancellationToken cancellationToken)
    {
        var reservationRepository = _unitOfWork.Repository<Reservation>();
        
        // Build search predicate if search term is provided
        Expression<Func<Reservation, bool>>? predicate = null;
        if (!string.IsNullOrEmpty(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            predicate = r => 
                r.Member.User.FullName.ToLower().Contains(searchTerm) || 
                r.Book.Title.ToLower().Contains(searchTerm);
        }
        
        // Get paginated reservations
        var pagedReservations = await reservationRepository.PagedListAsync(
            pagedRequest: request.PagedRequest,
            predicate: predicate,
            orderBy: q => q.OrderByDescending(r => r.ReservationDate),
            asNoTracking: true,
            r => r.Member.User,
            r => r.Book
        );
        
        // Map to DTOs
        var reservationDtos = _mapper.Map<List<ReservationDto>>(pagedReservations.Items);
        
        // Set additional properties from navigation properties
        for (int i = 0; i < pagedReservations.Items.Count; i++)
        {
            reservationDtos[i].MemberName = pagedReservations.Items[i].Member?.User?.FullName ?? string.Empty;
            reservationDtos[i].BookTitle = pagedReservations.Items[i].Book?.Title ?? string.Empty;
        }
        
        return new PagedResult<ReservationDto>(
            reservationDtos,
            pagedReservations.TotalCount,
            pagedReservations.PageNumber,
            pagedReservations.PageSize
        );
    }
}