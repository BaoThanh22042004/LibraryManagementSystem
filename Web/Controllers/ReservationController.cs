using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Application.Validators;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Extensions;

namespace Web.Controllers
{
    /// <summary>
    /// Controller for reservation management operations.
    /// Implements UC022 (Reserve Book), UC023 (Cancel Reservation), 
    /// UC024 (Fulfill Reservation), and UC025 (View Reservations).
    /// </summary>
    public class ReservationController : Controller
    {
		private readonly IReservationService _reservationService;
		private readonly IBookService _bookService;
		private readonly IAuditService _auditService;
		private readonly INotificationService _notificationService;
		private readonly ILogger<ReservationController> _logger;
		private readonly IValidator<CreateReservationRequest> _createReservationValidator;
		private readonly IValidator<CancelReservationRequest> _cancelReservationValidator;
		private readonly IValidator<FulfillReservationRequest> _fulfillReservationValidator;

		public ReservationController(
			IReservationService reservationService,
			IBookService bookService,
			IAuditService auditService,
			INotificationService notificationService,
			ILogger<ReservationController> logger,
			IValidator<CreateReservationRequest> createReservationValidator,
			IValidator<CancelReservationRequest> cancelReservationValidator,
			IValidator<FulfillReservationRequest> fulfillReservationValidator)
		{
			_reservationService = reservationService;
			_bookService = bookService;
			_auditService = auditService;
			_notificationService = notificationService;
			_logger = logger;
			_createReservationValidator = createReservationValidator;
			_cancelReservationValidator = cancelReservationValidator;
			_fulfillReservationValidator = fulfillReservationValidator;
		}

		/// <summary>
		/// Displays reservation management index page with search and filter options.
		/// For staff use only. Part of UC025 (View Reservations).
		/// </summary>
		[HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Index(ReservationSearchRequest? search = null)
        {
            try
            {
                search ??= new ReservationSearchRequest { Page = 1, PageSize = 20 };

                var result = await _reservationService.GetReservationsAsync(search);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return View(new PagedResult<ReservationBasicDto>());
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservations");
                TempData["ErrorMessage"] = "An error occurred while retrieving reservations.";
                return View(new PagedResult<ReservationBasicDto>());
            }
        }

        /// <summary>
        /// Displays reservation details.
        /// For staff use only. Part of UC025 (View Reservations).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _reservationService.GetReservationByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                // Get queue position for active reservations
                if (result.Value.Status == ReservationStatus.Active)
                {
                    var positionResult = await _reservationService.GetReservationQueuePositionAsync(id);
                    if (positionResult.IsSuccess)
                    {
                        ViewBag.QueuePosition = positionResult.Value;
                    }
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation details");
                TempData["ErrorMessage"] = "An error occurred while retrieving reservation details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays reservations for a specific member.
        /// For staff use only. Part of UC025 (View Reservations).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> MemberReservations(int memberId, int page = 1, int pageSize = 10)
        {
            try
            {
                var search = new ReservationSearchRequest
                {
                    MemberId = memberId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _reservationService.GetReservationsAsync(search);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.MemberId = memberId;
                return View("Index", result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member reservations");
                TempData["ErrorMessage"] = "An error occurred while retrieving member reservations.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays reservations for a specific book.
        /// For staff use only. Part of UC025 (View Reservations).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> BookReservations(int bookId, int page = 1, int pageSize = 10)
        {
            try
            {
                var search = new ReservationSearchRequest
                {
                    BookId = bookId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _reservationService.GetReservationsAsync(search);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.BookId = bookId;
                return View("Index", result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving book reservations");
                TempData["ErrorMessage"] = "An error occurred while retrieving book reservations.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays the create reservation form.
        /// For staff use. Part of UC022 (Reserve Book).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(int? bookId = null)
        {
            var model = new CreateReservationRequest();
            
            if (bookId.HasValue)
            {
                var bookResult = await _bookService.GetBookByIdAsync(bookId.Value);
                if (bookResult.IsSuccess)
                {
                    model.BookId = bookId.Value;
                    ViewBag.BookTitle = bookResult.Value.Title;
                }
            }
            
            return View(model);
        }

		/// <summary>
		/// Processes create reservation request with audit logging.
		/// Implements UC022 (Reserve Book) - BR-17, BR-18, BR-22
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin,Librarian")]
		public async Task<IActionResult> Create(CreateReservationRequest model)
		{
			try
			{
				if (!User.TryGetUserId(out int staffId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = await _createReservationValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);

					var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
					if (bookResult.IsSuccess)
					{
						ViewBag.BookTitle = bookResult.Value.Title;
					}

					return View(model);
				}

				var result = await _reservationService.CreateReservationAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);

					var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
					if (bookResult.IsSuccess)
					{
						ViewBag.BookTitle = bookResult.Value.Title;
					}

					return View(model);
				}

				// Audit successful reservation
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = staffId,
					ActionType = AuditActionType.Create,
					EntityType = "Reservation",
					EntityId = result.Value.Id.ToString(),
					EntityName = $"{result.Value.MemberName} - {result.Value.BookTitle}",
					Details = $"Reservation created successfully. Queue Position: {result.Value.QueuePosition}",
					IsSuccess = true
				});

				_logger.LogInformation("Staff {StaffId} created reservation {ReservationId} for book {BookId} for member {MemberId} at {Time}",
					staffId, result.Value.Id, model.BookId, model.MemberId, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Reservation created successfully.";

				// Send reservation confirmation notification
				await _notificationService.CreateNotificationAsync(new NotificationCreateDto
				{
					UserId = result.Value.UserId,
					Type = NotificationType.ReservationConfirmation,
					Subject = $"Reservation Confirmation: {result.Value.BookTitle}",
					Message = $"You have successfully reserved '{result.Value.BookTitle}'. Your queue position: {result.Value.QueuePosition ?? 1}."
				});

				return RedirectToAction(nameof(Details), new { id = result.Value.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating reservation");
				TempData["ErrorMessage"] = "An error occurred while creating the reservation.";
				return View(model);
			}
		}

		/// <summary>
		/// Processes cancel reservation request.
		/// For staff use. Implements UC023 (Cancel Reservation).
		/// </summary>
		[HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Cancel(CancelReservationRequest model, bool allowOverride = false, string? overrideReason = null)
        {
            try
            {
                if (!User.TryGetUserId(out int staffId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Set staff cancellation flag
                model.IsStaffCancellation = true;

                var validationResult = await _cancelReservationValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    TempData["ErrorMessage"] = "Invalid cancellation request.";
                    return RedirectToAction(nameof(Details), new { id = model.ReservationId });
                }

                // Pass override parameters to service
                var result = await _reservationService.CancelReservationAsync(model, allowOverride, overrideReason);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Details), new { id = model.ReservationId });
                }

                // Audit successful cancellation, including override context if present
                var auditDetails = $"Reservation cancelled successfully.";
                if (result.Value.OverrideContext?.IsOverride == true)
                {
                    auditDetails += $" [OVERRIDE] Reason: {result.Value.OverrideContext.Reason}; Rules: {string.Join(", ", result.Value.OverrideContext.OverriddenRules)}";
                }
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = staffId,
                    ActionType = AuditActionType.Update,
                    EntityType = "Reservation",
                    EntityId = model.ReservationId.ToString(),
                    EntityName = $"{result.Value.MemberName} - {result.Value.BookTitle}",
                    Details = auditDetails,
                    IsSuccess = true
                });

                _logger.LogInformation("Staff {StaffId} cancelled reservation {ReservationId} at {Time}",
                    staffId, model.ReservationId, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Reservation cancelled successfully.";

                // Send reservation cancellation notification
                await _notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = result.Value.UserId,
                    Type = NotificationType.ReservationCancellation,
                    Subject = $"Reservation Cancelled: {result.Value.BookTitle}",
                    Message = $"Your reservation for '{result.Value.BookTitle}' has been cancelled."
                });

                return RedirectToAction(nameof(Details), new { id = model.ReservationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling reservation");
                TempData["ErrorMessage"] = "An error occurred while cancelling the reservation.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays fulfill reservation form.
        /// For staff use only. Part of UC024 (Fulfill Reservation).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Fulfill(int id)
        {
            try
            {
                var result = await _reservationService.GetReservationByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                if (result.Value.Status != ReservationStatus.Active)
                {
                    TempData["ErrorMessage"] = "Only active reservations can be fulfilled.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var fulfillRequest = new FulfillReservationRequest
                {
                    ReservationId = id,
                    // Default pickup deadline to 72 hours from now
                    PickupDeadline = DateTime.Now.AddHours(72)
                };

                ViewBag.ReservationDetails = result.Value;
                return View(fulfillRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing reservation fulfillment");
                TempData["ErrorMessage"] = "An error occurred while preparing reservation fulfillment.";
                return RedirectToAction(nameof(Index));
            }
        }

		/// <summary>
		/// Processes fulfill reservation request with notification.
		/// Implements UC024 (Fulfill Reservation) - BR-19, BR-21, BR-22
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin,Librarian")]
		public async Task<IActionResult> Fulfill(FulfillReservationRequest model, bool allowOverride = false, string? overrideReason = null)
		{
			try
			{
				if (!User.TryGetUserId(out int staffId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = await _fulfillReservationValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);

					var reservationResult = await _reservationService.GetReservationByIdAsync(model.ReservationId);
					if (reservationResult.IsSuccess)
					{
						ViewBag.ReservationDetails = reservationResult.Value;
					}

					return View(model);
				}

				// Pass override parameters to service
				var result = await _reservationService.FulfillReservationAsync(model, allowOverride, overrideReason);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					var reservationResult = await _reservationService.GetReservationByIdAsync(model.ReservationId);
					if (reservationResult.IsSuccess)
					{
						ViewBag.ReservationDetails = reservationResult.Value;
					}
					return View(model);
				}

				// Audit successful fulfillment, including override context if present
				var auditDetails = $"Reservation fulfilled successfully. Pickup Deadline: {result.Value.PickupDeadline:yyyy-MM-dd HH:mm}. Copy: {result.Value.CopyNumber}";
				if (result.Value.OverrideContext?.IsOverride == true)
				{
					auditDetails += $" [OVERRIDE] Reason: {result.Value.OverrideContext.Reason}; Rules: {string.Join(", ", result.Value.OverrideContext.OverriddenRules)}";
				}
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = staffId,
					ActionType = AuditActionType.Update,
					EntityType = "Reservation",
					EntityId = result.Value.Id.ToString(),
					EntityName = $"{result.Value.MemberName} - {result.Value.BookTitle}",
					Details = auditDetails,
					IsSuccess = true
				});

				_logger.LogInformation("Staff {StaffId} fulfilled reservation {ReservationId} with copy {BookCopyId} at {Time}",
					staffId, result.Value.Id, result.Value.BookCopyId, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Reservation fulfilled successfully.";

				// Send reservation fulfillment notification
				await _notificationService.CreateNotificationAsync(new NotificationCreateDto
				{
					UserId = result.Value.UserId,
					Type = NotificationType.ReservationConfirmation,
					Subject = $"Reservation Fulfilled: {result.Value.BookTitle}",
					Message = $"Your reserved book '{result.Value.BookTitle}' is now available for pickup. Please collect it by {result.Value.PickupDeadline:yyyy-MM-dd}."
				});

				return RedirectToAction(nameof(Details), new { id = result.Value.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error fulfilling reservation");
				TempData["ErrorMessage"] = "An error occurred while fulfilling the reservation.";
				return View(model);
			}
		}

        /// <summary>
        /// Allows members to view their reservations.
        /// Implements member-facing part of UC025 (View Reservations).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyReservations(int page = 1, int pageSize = 10)
        {
            try
            {
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var search = new ReservationSearchRequest
                {
                    MemberId = memberId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _reservationService.GetReservationsAsync(search);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Index", "Home");
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member reservations");
                TempData["ErrorMessage"] = "An error occurred while retrieving your reservations.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Allows members to view details of their own reservations.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyReservationDetails(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _reservationService.GetReservationByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(MyReservations));
                }

                // Verify this reservation belongs to the current member (BR-03: User Information Access)
                if (result.Value.MemberId != memberId)
                {
                    TempData["ErrorMessage"] = "You can only view your own reservations.";
                    return RedirectToAction(nameof(MyReservations));
                }

                // Get queue position for active reservations
                if (result.Value.Status == ReservationStatus.Active)
                {
                    var positionResult = await _reservationService.GetReservationQueuePositionAsync(id);
                    if (positionResult.IsSuccess)
                    {
                        ViewBag.QueuePosition = positionResult.Value;
                    }
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving reservation details");
                TempData["ErrorMessage"] = "An error occurred while retrieving reservation details.";
                return RedirectToAction(nameof(MyReservations));
            }
        }

        /// <summary>
        /// Allows members to reserve a book.
        /// Implements member-facing part of UC022 (Reserve Book).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> ReserveBook(CreateReservationRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Ensure memberId is set to the current user
                model.MemberId = memberId;

                var validationResult = await _createReservationValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    TempData["ErrorMessage"] = "Invalid reservation request.";
                    return RedirectToAction("Details", "Book", new { id = model.BookId });
                }

                var result = await _reservationService.CreateReservationAsync(model);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Details", "Book", new { id = model.BookId });
                }

                _logger.LogInformation("Member {MemberId} created reservation {ReservationId} for book {BookId} at {Time}",
                    memberId, result.Value.Id, model.BookId, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Book reserved successfully. You will be notified when it becomes available.";

                // Send reservation confirmation notification
                await _notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = result.Value.UserId,
                    Type = NotificationType.ReservationConfirmation,
                    Subject = $"Reservation Confirmation: {result.Value.BookTitle}",
                    Message = $"You have successfully reserved '{result.Value.BookTitle}'. Your queue position: {result.Value.QueuePosition ?? 1}."
                });

                return RedirectToAction(nameof(MyReservationDetails), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating member reservation");
                TempData["ErrorMessage"] = "An error occurred while reserving the book.";
                return RedirectToAction("Details", "Book", new { id = model.BookId });
            }
        }

        /// <summary>
        /// Allows members to cancel their own reservations.
        /// Implements member-facing part of UC023 (Cancel Reservation).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> CancelMyReservation(CancelReservationRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Set member cancellation flag
                model.IsStaffCancellation = false;

                // Verify this reservation belongs to the current member before cancellation
                var reservationResult = await _reservationService.GetReservationByIdAsync(model.ReservationId);
                if (!reservationResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = reservationResult.Error;
                    return RedirectToAction(nameof(MyReservations));
                }

                if (reservationResult.Value.MemberId != memberId)
                {
                    TempData["ErrorMessage"] = "You can only cancel your own reservations.";
                    return RedirectToAction(nameof(MyReservations));
                }

                var validationResult = await _cancelReservationValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    TempData["ErrorMessage"] = "Invalid cancellation request.";
                    return RedirectToAction(nameof(MyReservationDetails), new { id = model.ReservationId });
                }

                var result = await _reservationService.CancelReservationAsync(model);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(MyReservationDetails), new { id = model.ReservationId });
                }

                _logger.LogInformation("Member {MemberId} cancelled reservation {ReservationId} at {Time}",
                    memberId, model.ReservationId, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Reservation cancelled successfully.";

                // Send reservation cancellation notification
                await _notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = result.Value.UserId,
                    Type = NotificationType.ReservationCancellation,
                    Subject = $"Reservation Cancelled: {result.Value.BookTitle}",
                    Message = $"Your reservation for '{result.Value.BookTitle}' has been cancelled."
                });

                return RedirectToAction(nameof(MyReservations));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling member reservation");
                TempData["ErrorMessage"] = "An error occurred while cancelling your reservation.";
                return RedirectToAction(nameof(MyReservations));
            }
        }
    }
}