using Application.Common;
using Application.DTOs;

namespace Application.Interfaces;

/// <summary>
/// Interface for reservation management operations
/// Supports UC022-UC025 (reserve, cancel, fulfill, and view reservations)
/// </summary>
public interface IReservationService
{
    Task<Result<ReservationDetailDto>> CreateReservationAsync(CreateReservationRequest request);

    Task<Result<ReservationDetailDto>> CancelReservationAsync(CancelReservationRequest request);

    Task<Result<ReservationDetailDto>> FulfillReservationAsync(FulfillReservationRequest request);

    Task<Result<ReservationDetailDto>> GetReservationByIdAsync(int reservationId);

    Task<Result<IEnumerable<ReservationBasicDto>>> GetActiveReservationsByMemberIdAsync(int memberId);

    Task<Result<PagedResult<ReservationBasicDto>>> GetReservationsAsync(ReservationSearchRequest request);

    Task<Result<int>> GetReservationQueuePositionAsync(int reservationId);

    Task<Result<IEnumerable<ReservationBasicDto>>> GetActiveReservationsByBookIdAsync(int bookId);

    Task<Result<int>> ProcessExpiredReservationsAsync();
}