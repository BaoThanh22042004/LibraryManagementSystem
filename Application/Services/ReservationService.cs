using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using AutoMapper;
using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace Application.Services;

/// <summary>
/// Implementation of the reservation management service
/// Supports UC022-UC025 (reserve, cancel, fulfill, and view reservations)
/// </summary>
public class ReservationService : IReservationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;
    private readonly ILogger<ReservationService> _logger;

    private const int MaxActiveReservationsPerMember = 3;
    private const int DefaultPickupDeadlineHours = 72; // 3 days

    public ReservationService(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        ILogger<ReservationService> logger)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new reservation - UC022
    /// </summary>
    /// <param name="request">Reservation creation details</param>
    /// <returns>Result with created reservation information</returns>
    public async Task<Result<ReservationDetailDto>> CreateReservationAsync(CreateReservationRequest request)
    {
        try
        {
            // Begin transaction for data consistency
            await _unitOfWork.BeginTransactionAsync();

            // Get the member
            var member = await _unitOfWork.Repository<Member>().GetAsync(
                m => m.Id == request.MemberId,
                m => m.User, m => m.Reservations);

            if (member == null)
                return Result.Failure<ReservationDetailDto>($"Member with ID {request.MemberId} not found.");

            // Check if member is active
            if (member.MembershipStatus != MembershipStatus.Active)
                return Result.Failure<ReservationDetailDto>($"Member has inactive membership status: {member.MembershipStatus}.");

            // Check if member has unpaid fines - BR-16
            if (member.OutstandingFines > 0)
                return Result.Failure<ReservationDetailDto>($"Member has outstanding fines of {member.OutstandingFines:C}. Please clear fines before making reservations.");

            // Check if member has reached maximum reservations - BR-18
            var activeReservationsCount = member.Reservations.Count(r => r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled);
            if (activeReservationsCount >= MaxActiveReservationsPerMember)
                return Result.Failure<ReservationDetailDto>($"Member has reached the maximum number of active reservations ({MaxActiveReservationsPerMember}).");

            // Get the book
            var book = await _unitOfWork.Repository<Book>().GetAsync(
                b => b.Id == request.BookId,
                b => b.Copies);

            if (book == null)
                return Result.Failure<ReservationDetailDto>($"Book with ID {request.BookId} not found.");

            // Check if book is unavailable (all copies borrowed) - BR-17
            bool anyAvailableCopies = book.Copies.Any(c => c.Status == CopyStatus.Available);
            if (anyAvailableCopies)
                return Result.Failure<ReservationDetailDto>("Cannot create reservation because copies of this book are currently available. Please borrow an available copy instead.");

            // Check for existing active reservation by this member for this book
            bool hasExistingReservation = await _unitOfWork.Repository<Reservation>().ExistsAsync(
                r => r.MemberId == request.MemberId && 
                     r.BookId == request.BookId && 
                     (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled));

            if (hasExistingReservation)
                return Result.Failure<ReservationDetailDto>("Member already has an active reservation for this book.");

            // Create the reservation entity
            var reservation = _mapper.Map<Reservation>(request);
            reservation.ReservationDate = DateTime.UtcNow;
            reservation.Status = ReservationStatus.Active;

            // Save entity
            await _unitOfWork.Repository<Reservation>().AddAsync(reservation);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            // Get the full reservation details
            var createdReservation = await _unitOfWork.Repository<Reservation>().GetAsync(
                r => r.Id == reservation.Id,
                r => r.Member.User,
                r => r.Book,
                r => r.BookCopy!);

            // Calculate queue position
            var queuePosition = await GetReservationQueuePositionAsync(reservation.Id);

            // Map and return the result
            var reservationDto = _mapper.Map<ReservationDetailDto>(createdReservation);
            reservationDto.QueuePosition = queuePosition.IsSuccess ? queuePosition.Value : null;
            
            // Estimate availability date based on typical loan period
            reservationDto.EstimatedAvailabilityDate = EstimateAvailabilityDate(reservationDto.QueuePosition);

            return Result.Success(reservationDto);
        }
        catch (Exception ex)
        {
            // Rollback on error
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error creating reservation: {ErrorMessage}", ex.Message);
            return Result.Failure<ReservationDetailDto>($"Failed to create reservation: {ex.Message}");
        }
    }

    /// <summary>
    /// Cancels an existing reservation - UC023
    /// </summary>
    /// <param name="request">Cancellation details</param>
    /// <returns>Result with updated reservation information</returns>
    public async Task<Result<ReservationDetailDto>> CancelReservationAsync(CancelReservationRequest request)
    {
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();

            // Get the reservation with related entities
            var reservation = await _unitOfWork.Repository<Reservation>().GetAsync(
                r => r.Id == request.ReservationId,
                r => r.Member.User,
                r => r.Book,
                r => r.BookCopy!);

            if (reservation == null)
                return Result.Failure<ReservationDetailDto>($"Reservation with ID {request.ReservationId} not found.");

            // Check if reservation can be cancelled (must be active or fulfilled)
            if (reservation.Status != ReservationStatus.Active && reservation.Status != ReservationStatus.Fulfilled)
                return Result.Failure<ReservationDetailDto>($"Reservation cannot be cancelled. Current status: {reservation.Status}.");

            // Cancel the reservation
            reservation.Status = ReservationStatus.Cancelled;

            // If the reservation was fulfilled and had a book copy assigned, update the copy status
            if (reservation.BookCopyId.HasValue)
            {
                var bookCopy = await _unitOfWork.Repository<BookCopy>().GetAsync(bc => bc.Id == reservation.BookCopyId.Value);
                if (bookCopy != null && bookCopy.Status == CopyStatus.Reserved)
                {
                    bookCopy.Status = CopyStatus.Available;
                    _unitOfWork.Repository<BookCopy>().Update(bookCopy);
                }
            }

            // Update reservation entity
            _unitOfWork.Repository<Reservation>().Update(reservation);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            // Map and return the result
            var reservationDto = _mapper.Map<ReservationDetailDto>(reservation);
            return Result.Success(reservationDto);
        }
        catch (Exception ex)
        {
            // Rollback on error
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error cancelling reservation: {ErrorMessage}", ex.Message);
            return Result.Failure<ReservationDetailDto>($"Failed to cancel reservation: {ex.Message}");
        }
    }

    /// <summary>
    /// Fulfills a reservation when a book becomes available - UC024
    /// </summary>
    /// <param name="request">Fulfillment details</param>
    /// <returns>Result with updated reservation information</returns>
    public async Task<Result<ReservationDetailDto>> FulfillReservationAsync(FulfillReservationRequest request)
    {
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();

            // Get the reservation with related entities
            var reservation = await _unitOfWork.Repository<Reservation>().GetAsync(
                r => r.Id == request.ReservationId,
                r => r.Member.User,
                r => r.Book);

            if (reservation == null)
                return Result.Failure<ReservationDetailDto>($"Reservation with ID {request.ReservationId} not found.");

            // Check if reservation can be fulfilled (must be active)
            if (reservation.Status != ReservationStatus.Active)
                return Result.Failure<ReservationDetailDto>($"Reservation cannot be fulfilled. Current status: {reservation.Status}.");

            // Get the book copy
            var bookCopy = await _unitOfWork.Repository<BookCopy>().GetAsync(
                bc => bc.Id == request.BookCopyId && bc.BookId == reservation.BookId,
                bc => bc.Book);

            if (bookCopy == null)
                return Result.Failure<ReservationDetailDto>($"Book copy with ID {request.BookCopyId} not found or doesn't match reserved book.");

            // Check if book copy is available
            if (bookCopy.Status != CopyStatus.Available)
                return Result.Failure<ReservationDetailDto>($"Book copy is not available. Current status: {bookCopy.Status}.");

            // Check if this is the first reservation in the queue - BR-19
            var queuePosition = await GetReservationQueuePositionAsync(reservation.Id);
            if (queuePosition.IsSuccess && queuePosition.Value > 1)
                return Result.Failure<ReservationDetailDto>($"Cannot fulfill this reservation. It is not first in the queue (current position: {queuePosition.Value}).");

            // Set pickup deadline
            var pickupDeadline = request.PickupDeadline ?? DateTime.UtcNow.AddHours(DefaultPickupDeadlineHours);

            // Update reservation
            reservation.Status = ReservationStatus.Fulfilled;
            reservation.BookCopyId = bookCopy.Id;

            // Update book copy status
            bookCopy.Status = CopyStatus.Reserved;

            // Update entities
            _unitOfWork.Repository<Reservation>().Update(reservation);
            _unitOfWork.Repository<BookCopy>().Update(bookCopy);
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            // Map and return the result
            var reservationDto = _mapper.Map<ReservationDetailDto>(reservation);
            reservationDto.PickupDeadline = pickupDeadline;
            reservationDto.CopyNumber = bookCopy.CopyNumber;
            return Result.Success(reservationDto);
        }
        catch (Exception ex)
        {
            // Rollback on error
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error fulfilling reservation: {ErrorMessage}", ex.Message);
            return Result.Failure<ReservationDetailDto>($"Failed to fulfill reservation: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets reservation details by ID
    /// </summary>
    /// <param name="reservationId">ID of the reservation to retrieve</param>
    /// <returns>Result with reservation details</returns>
    public async Task<Result<ReservationDetailDto>> GetReservationByIdAsync(int reservationId)
    {
        try
        {
            var reservation = await _unitOfWork.Repository<Reservation>().GetAsync(
                r => r.Id == reservationId,
                r => r.Member.User,
                r => r.Book,
                r => r.BookCopy!);

            if (reservation == null)
                return Result.Failure<ReservationDetailDto>($"Reservation with ID {reservationId} not found.");

            var reservationDto = _mapper.Map<ReservationDetailDto>(reservation);

            // Set additional properties if active
            if (reservation.Status == ReservationStatus.Active)
            {
                // Calculate queue position
                var queuePosition = await GetReservationQueuePositionAsync(reservationId);
                reservationDto.QueuePosition = queuePosition.IsSuccess ? queuePosition.Value : null;
                
                // Estimate availability date
                reservationDto.EstimatedAvailabilityDate = EstimateAvailabilityDate(reservationDto.QueuePosition);
            }
            else if (reservation.Status == ReservationStatus.Fulfilled)
            {
                // For fulfilled reservations, calculate pickup deadline if not explicitly set
                reservationDto.PickupDeadline = DateTime.UtcNow.AddHours(DefaultPickupDeadlineHours);
            }

            return Result.Success(reservationDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservation: {ErrorMessage}", ex.Message);
            return Result.Failure<ReservationDetailDto>($"Failed to retrieve reservation: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all active reservations for a member
    /// </summary>
    /// <param name="memberId">ID of the member</param>
    /// <returns>Result with list of active reservations</returns>
    public async Task<Result<IEnumerable<ReservationBasicDto>>> GetActiveReservationsByMemberIdAsync(int memberId)
    {
        try
        {
            // Check if member exists
            var memberExists = await _unitOfWork.Repository<Member>().ExistsAsync(m => m.Id == memberId);
            if (!memberExists)
                return Result.Failure<IEnumerable<ReservationBasicDto>>($"Member with ID {memberId} not found.");

            // Get all active reservations for the member
            var reservations = await _unitOfWork.Repository<Reservation>().ListAsync(
                r => r.MemberId == memberId && (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled),
                r => r.OrderByDescending(x => x.ReservationDate),
                true,
                r => r.Member.User,
                r => r.Book,
                r => r.BookCopy!);

            var reservationDtos = _mapper.Map<IEnumerable<ReservationBasicDto>>(reservations);

            // Calculate queue positions for active reservations
            foreach (var dto in reservationDtos.Where(r => r.Status == ReservationStatus.Active))
            {
                var queuePosition = await GetReservationQueuePositionAsync(dto.Id);
                dto.QueuePosition = queuePosition.IsSuccess ? queuePosition.Value : null;
            }

            // Set pickup deadlines for fulfilled reservations
            foreach (var dto in reservationDtos.Where(r => r.Status == ReservationStatus.Fulfilled))
            {
                // If pickup deadline wasn't explicitly set, calculate a default one
                if (!dto.PickupDeadline.HasValue)
                {
                    dto.PickupDeadline = DateTime.UtcNow.AddHours(DefaultPickupDeadlineHours);
                }
            }

            return Result.Success(reservationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active reservations: {ErrorMessage}", ex.Message);
            return Result.Failure<IEnumerable<ReservationBasicDto>>($"Failed to retrieve active reservations: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all reservations with search parameters and pagination - UC025
    /// </summary>
    /// <param name="request">Search parameters</param>
    /// <returns>Paged result with reservations matching criteria</returns>
    public async Task<Result<PagedResult<ReservationBasicDto>>> GetReservationsAsync(ReservationSearchRequest request)
    {
        try
        {
            // Build the predicate for filtering
            Expression<Func<Reservation, bool>> predicate = BuildReservationSearchPredicate(request);

            // Get paged reservations
            var pagedReservations = await _unitOfWork.Repository<Reservation>().PagedListAsync(
				request,
                predicate,
                q => q.OrderByDescending(r => r.ReservationDate),
                true,
                r => r.Member.User,
                r => r.Book,
                r => r.BookCopy!);

            // Map to DTOs
            var reservationDtos = _mapper.Map<IEnumerable<ReservationBasicDto>>(pagedReservations.Items);

            // Calculate queue positions for active reservations
            foreach (var dto in reservationDtos.Where(r => r.Status == ReservationStatus.Active))
            {
                var queuePosition = await GetReservationQueuePositionAsync(dto.Id);
                dto.QueuePosition = queuePosition.IsSuccess ? queuePosition.Value : null;
            }

            // Set pickup deadlines for fulfilled reservations
            foreach (var dto in reservationDtos.Where(r => r.Status == ReservationStatus.Fulfilled))
            {
                // If pickup deadline wasn't explicitly set, calculate a default one
                if (!dto.PickupDeadline.HasValue)
                {
                    dto.PickupDeadline = DateTime.UtcNow.AddHours(DefaultPickupDeadlineHours);
                }
            }

            // Create and return paged result
            return Result.Success(new PagedResult<ReservationBasicDto>
            {
                Items = [.. reservationDtos],
                Page = pagedReservations.Page,
                PageSize = pagedReservations.PageSize,
                Count = pagedReservations.Count,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reservations: {ErrorMessage}", ex.Message);
            return Result.Failure<PagedResult<ReservationBasicDto>>($"Failed to retrieve reservations: {ex.Message}");
		}
    }

    /// <summary>
    /// Gets the queue position for a specific reservation
    /// </summary>
    /// <param name="reservationId">ID of the reservation</param>
    /// <returns>Result with queue position (1-based)</returns>
    public async Task<Result<int>> GetReservationQueuePositionAsync(int reservationId)
    {
        try
        {
            // Get the reservation
            var reservation = await _unitOfWork.Repository<Reservation>().GetAsync(r => r.Id == reservationId);
            if (reservation == null)
                return Result.Failure<int>($"Reservation with ID {reservationId} not found.");

            // If not active, return error
            if (reservation.Status != ReservationStatus.Active)
                return Result.Failure<int>($"Cannot determine queue position. Reservation status is {reservation.Status}.");

            // Get all active reservations for the same book, ordered by date
            var queue = await _unitOfWork.Repository<Reservation>().ListAsync(
                r => r.BookId == reservation.BookId && r.Status == ReservationStatus.Active,
                r => r.OrderBy(x => x.ReservationDate));

            // Find position in the queue (1-based)
            int position = 0;
            foreach (var item in queue)
            {
                position++;
                if (item.Id == reservationId)
                    break;
            }

            return Result.Success(position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining queue position: {ErrorMessage}", ex.Message);
            return Result.Failure<int>($"Failed to determine queue position: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets all active reservations for a book
    /// </summary>
    /// <param name="bookId">ID of the book</param>
    /// <returns>Result with list of active reservations for the book</returns>
    public async Task<Result<IEnumerable<ReservationBasicDto>>> GetActiveReservationsByBookIdAsync(int bookId)
    {
        try
        {
            // Check if book exists
            var bookExists = await _unitOfWork.Repository<Book>().ExistsAsync(b => b.Id == bookId);
            if (!bookExists)
                return Result.Failure<IEnumerable<ReservationBasicDto>>($"Book with ID {bookId} not found.");

            // Get all active reservations for the book
            var reservations = await _unitOfWork.Repository<Reservation>().ListAsync(
                r => r.BookId == bookId && (r.Status == ReservationStatus.Active || r.Status == ReservationStatus.Fulfilled),
                r => r.OrderBy(x => x.ReservationDate),
                true,
                r => r.Member.User,
                r => r.Book,
                r => r.BookCopy!);

            var reservationDtos = _mapper.Map<IEnumerable<ReservationBasicDto>>(reservations);

            // Calculate queue positions for active reservations
            var activeReservations = reservationDtos.Where(r => r.Status == ReservationStatus.Active).ToList();
            for (int i = 0; i < activeReservations.Count; i++)
            {
                activeReservations[i].QueuePosition = i + 1;
            }

            // Set pickup deadlines for fulfilled reservations
            foreach (var dto in reservationDtos.Where(r => r.Status == ReservationStatus.Fulfilled))
            {
                // If pickup deadline wasn't explicitly set, calculate a default one
                if (!dto.PickupDeadline.HasValue)
                {
                    dto.PickupDeadline = DateTime.UtcNow.AddHours(DefaultPickupDeadlineHours);
                }
            }

            return Result.Success(reservationDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving active reservations for book: {ErrorMessage}", ex.Message);
            return Result.Failure<IEnumerable<ReservationBasicDto>>($"Failed to retrieve active reservations: {ex.Message}");
        }
    }

    /// <summary>
    /// Processes expired reservations across the system
    /// </summary>
    /// <returns>Count of reservations marked as expired</returns>
    public async Task<Result<int>> ProcessExpiredReservationsAsync()
    {
        try
        {
            // Begin transaction
            await _unitOfWork.BeginTransactionAsync();

            // Calculate the cutoff time for expired fulfilled reservations (typically 72 hours after fulfillment)
            var cutoffTime = DateTime.UtcNow.AddHours(-DefaultPickupDeadlineHours);

            // Find all fulfilled reservations that have expired pickup deadlines
            // Note: This would require a "FulfillmentDate" field which we don't have in our model
            // As a workaround, we'll check all fulfilled reservations created more than 72 hours ago
            var expiredReservations = await _unitOfWork.Repository<Reservation>().ListAsync(
                r => r.Status == ReservationStatus.Fulfilled && r.ReservationDate < cutoffTime,
                null,
                false,
                r => r.BookCopy!);

            int expiredCount = 0;

            // Process each expired reservation
            foreach (var reservation in expiredReservations)
            {
                // Update reservation status
                reservation.Status = ReservationStatus.Expired;
                _unitOfWork.Repository<Reservation>().Update(reservation);

                // If the reservation had a book copy assigned, update the copy status
                if (reservation.BookCopyId.HasValue && reservation.BookCopy != null)
                {
                    // Make the copy available again
                    var bookCopy = reservation.BookCopy;
                    bookCopy.Status = CopyStatus.Available;
                    _unitOfWork.Repository<BookCopy>().Update(bookCopy);
                }

                expiredCount++;
            }

            // Save changes
            await _unitOfWork.SaveChangesAsync();

            // Commit transaction
            await _unitOfWork.CommitTransactionAsync();

            return Result.Success(expiredCount);
        }
        catch (Exception ex)
        {
            // Rollback on error
            await _unitOfWork.RollbackTransactionAsync();
            _logger.LogError(ex, "Error processing expired reservations: {ErrorMessage}", ex.Message);
            return Result.Failure<int>($"Failed to process expired reservations: {ex.Message}");
		}
    }

    #region Private Helper Methods

    /// <summary>
    /// Builds a predicate for searching reservations based on provided parameters
    /// </summary>
    private static Expression<Func<Reservation, bool>> BuildReservationSearchPredicate(ReservationSearchRequest searchParams)
    {
        Expression<Func<Reservation, bool>> predicate = r => true;

        // Apply member filter
        if (searchParams.MemberId.HasValue)
        {
            var memberId = searchParams.MemberId.Value;
            predicate = predicate.And(r => r.MemberId == memberId);
        }

        // Apply book filter
        if (searchParams.BookId.HasValue)
        {
            var bookId = searchParams.BookId.Value;
            predicate = predicate.And(r => r.BookId == bookId);
        }

        // Apply status filter
        if (searchParams.Status.HasValue)
        {
            var status = searchParams.Status.Value;
            predicate = predicate.And(r => r.Status == status);
        }

        // Apply date range filter
        if (searchParams.FromDate.HasValue)
        {
            var fromDate = searchParams.FromDate.Value.Date;
            predicate = predicate.And(r => r.ReservationDate >= fromDate);
        }

        if (searchParams.ToDate.HasValue)
        {
            var toDate = searchParams.ToDate.Value.Date.AddDays(1).AddTicks(-1); // End of day
            predicate = predicate.And(r => r.ReservationDate <= toDate);
        }

        return predicate;
    }

    /// <summary>
    /// Estimates availability date based on queue position
    /// </summary>
    private static DateTime? EstimateAvailabilityDate(int? queuePosition)
    {
        if (!queuePosition.HasValue || queuePosition.Value <= 0)
            return null;

        // Simple estimation: assume each borrower keeps a book for the standard loan period
        // Position 1 = approximately 14 days, Position 2 = approximately 28 days, etc.
        int daysEstimated = (queuePosition.Value - 1) * 14; // Standard loan period is 14 days
        
        // Add a buffer for processing time
        daysEstimated += 2;

        return DateTime.UtcNow.AddDays(daysEstimated);
    }

    #endregion
}