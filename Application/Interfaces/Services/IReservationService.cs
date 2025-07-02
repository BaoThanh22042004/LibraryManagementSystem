using Application.Common;
using Application.DTOs;

namespace Application.Interfaces.Services;

public interface IReservationService
{
    Task<PagedResult<ReservationDto>> GetPaginatedReservationsAsync(PagedRequest request, string? searchTerm = null);
    Task<ReservationDto?> GetReservationByIdAsync(int id);
    Task<List<ReservationDto>> GetReservationsByMemberIdAsync(int memberId);
    Task<List<ReservationDto>> GetReservationsByBookIdAsync(int bookId);
    Task<int> CreateReservationAsync(CreateReservationDto reservationDto);
    Task UpdateReservationAsync(int id, UpdateReservationDto reservationDto);
    Task DeleteReservationAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> CancelReservationAsync(int id);
    Task<bool> FulfillReservationAsync(int id, int bookCopyId);
    Task<List<ReservationDto>> GetActiveReservationsAsync();
    Task<bool> HasActiveReservationAsync(int memberId, int bookId);
    Task<ReservationDto?> GetNextActiveReservationAsync(int bookId);
    Task<int> ProcessExpiredReservationsAsync();
    Task<int> GetReservationCountForMemberAsync(int memberId);
    Task<bool> HasReservationsForBookAsync(int bookId);
}