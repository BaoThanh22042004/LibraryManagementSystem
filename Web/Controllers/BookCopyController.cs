using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Extensions;

namespace Web.Controllers
{
	/// <summary>
	/// Controller for book copy management operations.
	/// Implements UC015 (Add Copy), UC016 (Update Copy Status), and UC017 (Remove Copy).
	/// Business Rules: BR-06 (Book Management Rights), BR-08 (Copy Deletion Restriction), 
	/// BR-09 (Copy Status Rules), BR-10 (Copy Return Validation), BR-22 (Audit Logging)
	/// </summary>
	[Authorize(Roles = "Admin,Librarian")]
	public class BookCopyController : Controller
	{
		private readonly IBookCopyService _bookCopyService;
		private readonly IBookService _bookService;
		private readonly IAuditService _auditService;
		private readonly ILogger<BookCopyController> _logger;
		private readonly IValidator<CreateBookCopyRequest> _createBookCopyValidator;
		private readonly IValidator<CreateMultipleBookCopiesRequest> _createMultipleBookCopiesValidator;
		private readonly IValidator<UpdateBookCopyStatusRequest> _updateBookCopyStatusValidator;

		public BookCopyController(
			IBookCopyService bookCopyService,
			IBookService bookService,
			IAuditService auditService,
			ILogger<BookCopyController> logger,
			IValidator<CreateBookCopyRequest> createBookCopyValidator,
			IValidator<CreateMultipleBookCopiesRequest> createMultipleBookCopiesValidator,
			IValidator<UpdateBookCopyStatusRequest> updateBookCopyStatusValidator)
		{
			_bookCopyService = bookCopyService;
			_bookService = bookService;
			_auditService = auditService;
			_logger = logger;
			_createBookCopyValidator = createBookCopyValidator;
			_createMultipleBookCopiesValidator = createMultipleBookCopiesValidator;
			_updateBookCopyStatusValidator = updateBookCopyStatusValidator;
		}

		/// <summary>
		/// Displays book copy details.
		/// Supports BR-06 (Book Management Rights), BR-22 (Audit Logging)
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Details(int id)
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var result = await _bookCopyService.GetBookCopyByIdAsync(id);
				if (!result.IsSuccess)
				{
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction("Index", "Book");
				}

				// Audit successful copy access
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Read,
					EntityType = "BookCopy",
					EntityId = id.ToString(),
					EntityName = result.Value.CopyNumber,
					Details = $"Viewed book copy details: {result.Value.Book.Title}",
					IsSuccess = true
				});

				return View(result.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving book copy details for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An unexpected error occurred while retrieving book copy details.";
				return RedirectToAction("Index", "Book");
			}
		}

		/// <summary>
		/// Displays create book copy form.
		/// Implements UC015 (Add Copy).
		/// Supports BR-06 (Book Management Rights)
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Create(int bookId)
		{
			try
			{
				// Verify that the book exists
				var bookResult = await _bookService.GetBookByIdAsync(bookId);
				if (!bookResult.IsSuccess)
				{
					_logger.LogWarning("Failed to retrieve book for copy creation: {Error}", bookResult.Error);
					TempData["ErrorMessage"] = bookResult.Error;
					return RedirectToAction("Index", "Book");
				}

				ViewBag.Book = bookResult.Value;

				// Get a suggested unique copy number
				var copyNumberResult = await _bookCopyService.GenerateUniqueCopyNumberAsync(bookId);
				var copyNumber = copyNumberResult.IsSuccess ? copyNumberResult.Value : string.Empty;

				return View(new CreateBookCopyRequest
				{
					BookId = bookId,
					CopyNumber = copyNumber,
					Status = CopyStatus.Available
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading create book copy form for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An unexpected error occurred while loading the create book copy form.";
				return RedirectToAction("Details", "Book", new { id = bookId });
			}
		}

		/// <summary>
		/// Processes create book copy request.
		/// Implements UC015 (Add Copy).
		/// Supports BR-06 (Book Management Rights), BR-09 (Copy Status Rules), BR-22 (Audit Logging)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(CreateBookCopyRequest model)
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = _createBookCopyValidator.Validate(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);

					// Retrieve book information again
					var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
					if (bookResult.IsSuccess)
					{
						ViewBag.Book = bookResult.Value;
					}

					return View(model);
				}

				// Check if copy number already exists for this book
				if (!string.IsNullOrEmpty(model.CopyNumber))
				{
					var copyNumberExistsResult = await _bookCopyService.CopyNumberExistsAsync(model.BookId, model.CopyNumber);
					if (copyNumberExistsResult.IsSuccess && copyNumberExistsResult.Value)
					{
						ModelState.AddModelError("CopyNumber", "This copy number already exists for the book.");

						// Retrieve book information again
						var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
						if (bookResult.IsSuccess)
						{
							ViewBag.Book = bookResult.Value;
						}

						return View(model);
					}
				}

				var result = await _bookCopyService.CreateBookCopyAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);

					// Retrieve book information again
					var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
					if (bookResult.IsSuccess)
					{
						ViewBag.Book = bookResult.Value;
					}

					return View(model);
				}

				// Audit successful copy creation
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Create,
					EntityType = "BookCopy",
					EntityId = result.Value.Id.ToString(),
					EntityName = result.Value.CopyNumber,
					Details = $"Book copy created successfully: {result.Value.CopyNumber} for book {result.Value.Book.Title}",
					IsSuccess = true
				});

				_logger.LogInformation("Book copy created successfully by user {UserId}: {CopyNumber} at {Time}",
					userId, result.Value.CopyNumber, DateTime.UtcNow);

				TempData["SuccessMessage"] = $"Book copy '{result.Value.CopyNumber}' created successfully.";
				return RedirectToAction("Details", "Book", new { id = model.BookId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating book copy for user {UserId}", User.GetUserId());
				ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating the book copy.");

				// Retrieve book information again
				var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
				if (bookResult.IsSuccess)
				{
					ViewBag.Book = bookResult.Value;
				}

				return View(model);
			}
		}

		/// <summary>
		/// Displays create multiple book copies form.
		/// Implements UC015 (Add Copy) - bulk creation alternative flow.
		/// Supports BR-06 (Book Management Rights)
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> CreateMultiple(int bookId)
		{
			try
			{
				// Verify that the book exists
				var bookResult = await _bookService.GetBookByIdAsync(bookId);
				if (!bookResult.IsSuccess)
				{
					_logger.LogWarning("Failed to retrieve book for bulk copy creation: {Error}", bookResult.Error);
					TempData["ErrorMessage"] = bookResult.Error;
					return RedirectToAction("Index", "Book");
				}

				ViewBag.Book = bookResult.Value;

				return View(new CreateMultipleBookCopiesRequest
				{
					BookId = bookId,
					Quantity = 1,
					Status = CopyStatus.Available
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading create multiple book copies form for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An unexpected error occurred while loading the create multiple book copies form.";
				return RedirectToAction("Details", "Book", new { id = bookId });
			}
		}

		/// <summary>
		/// Processes create multiple book copies request.
		/// Implements UC015 (Add Copy) - bulk creation alternative flow.
		/// Supports BR-06 (Book Management Rights), BR-09 (Copy Status Rules), BR-22 (Audit Logging)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> CreateMultiple(CreateMultipleBookCopiesRequest model)
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = _createMultipleBookCopiesValidator.Validate(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);

					// Retrieve book information again
					var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
					if (bookResult.IsSuccess)
					{
						ViewBag.Book = bookResult.Value;
					}

					return View(model);
				}

				var result = await _bookCopyService.CreateMultipleBookCopiesAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);

					// Retrieve book information again
					var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
					if (bookResult.IsSuccess)
					{
						ViewBag.Book = bookResult.Value;
					}

					return View(model);
				}

				// Audit successful multiple copies creation
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Create,
					EntityType = "BookCopy",
					EntityId = model.BookId.ToString(),
					EntityName = "Multiple Book Copies",
					Details = $"Multiple book copies created successfully: {model.Quantity} copies for book {model.BookId}",
					IsSuccess = true
				});

				_logger.LogInformation("Multiple book copies created successfully by user {UserId}: {BookId}, Quantity: {Quantity} at {Time}",
					userId, model.BookId, model.Quantity, DateTime.UtcNow);

				TempData["SuccessMessage"] = $"{model.Quantity} book copies created successfully.";
				return RedirectToAction("Details", "Book", new { id = model.BookId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating multiple book copies for user {UserId}", User.GetUserId());
				ModelState.AddModelError(string.Empty, "An unexpected error occurred while creating book copies.");

				// Retrieve book information again
				var bookResult = await _bookService.GetBookByIdAsync(model.BookId);
				if (bookResult.IsSuccess)
				{
					ViewBag.Book = bookResult.Value;
				}

				return View(model);
			}
		}

		/// <summary>
		/// Displays update book copy status form.
		/// Implements UC016 (Update Copy Status).
		/// Supports BR-06 (Book Management Rights), BR-09 (Copy Status Rules), BR-10 (Copy Return Validation)
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> UpdateStatus(int id)
		{
			try
			{
				var result = await _bookCopyService.GetBookCopyByIdAsync(id);
				if (!result.IsSuccess)
				{
					_logger.LogWarning("Failed to retrieve book copy for status update: {Error}", result.Error);
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction("Index", "Book");
				}

				var copy = result.Value;
				var updateDto = new UpdateBookCopyStatusRequest
				{
					Id = copy.Id,
					Status = copy.Status,
					Notes = string.Empty
				};

				ViewBag.Copy = copy;

				// BR-10: Check for active loans
				var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(id);
				ViewBag.HasActiveLoans = hasActiveLoans.IsSuccess && hasActiveLoans.Value;

				// Check for active reservations
				var hasActiveReservations = await _bookCopyService.CopyHasActiveReservationsAsync(id);
				ViewBag.HasActiveReservations = hasActiveReservations.IsSuccess && hasActiveReservations.Value;

				// Get valid status transitions for the current status
				var validTransitions = GetValidStatusTransitions(copy.Status, hasActiveLoans.IsSuccess && hasActiveLoans.Value);
				ViewBag.ValidStatusTransitions = validTransitions;

				return View(updateDto);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving book copy for status update for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the book copy.";
				return RedirectToAction("Index", "Book");
			}
		}

		/// <summary>
		/// Processes update book copy status request.
		/// Implements UC016 (Update Copy Status).
		/// Supports BR-06 (Book Management Rights), BR-09 (Copy Status Rules), BR-10 (Copy Return Validation), BR-22 (Audit Logging)
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> UpdateStatus(UpdateBookCopyStatusRequest model)
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = _updateBookCopyStatusValidator.Validate(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					await LoadUpdateStatusViewData(model.Id);
					return View(model);
				}

				// Get current copy information for business rule validation
				var copyResult = await _bookCopyService.GetBookCopyByIdAsync(model.Id);
				if (!copyResult.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, "Book copy not found.");
					return View(model);
				}

				var currentCopy = copyResult.Value;

				// BR-10: Special validation for status transitions involving Available status
				if (model.Status == CopyStatus.Available)
				{
					var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(model.Id);
					if (hasActiveLoans.IsSuccess && hasActiveLoans.Value)
					{
						ModelState.AddModelError(string.Empty, "Cannot mark copy as Available while it has active loans. Process the return first.");
						await LoadUpdateStatusViewData(model.Id);
						return View(model);
					}
				}

				// BR-09: Validate status transition rules
				if (!IsValidStatusTransition(currentCopy.Status, model.Status))
				{
					ModelState.AddModelError(string.Empty, $"Cannot change status from {currentCopy.Status} to {model.Status}.");
					await LoadUpdateStatusViewData(model.Id);
					return View(model);
				}

				var result = await _bookCopyService.UpdateBookCopyStatusAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					await LoadUpdateStatusViewData(model.Id);
					return View(model);
				}

				// Audit successful status update
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Update,
					EntityType = "BookCopy",
					EntityId = model.Id.ToString(),
					EntityName = result.Value.CopyNumber,
					Details = $"Book copy status updated: {currentCopy.Status} → {model.Status}. Notes: {model.Notes}",
					IsSuccess = true
				});

				_logger.LogInformation("Book copy status updated successfully by user {UserId}: {CopyId} to {Status} at {Time}",
					userId, model.Id, model.Status, DateTime.UtcNow);

				TempData["SuccessMessage"] = $"Book copy status updated successfully to {model.Status}.";
				return RedirectToAction("Details", "Book", new { id = result.Value.Book.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error updating book copy status for user {UserId}", User.GetUserId());
				ModelState.AddModelError(string.Empty, "An unexpected error occurred while updating the book copy status.");
				await LoadUpdateStatusViewData(model.Id);
				return View(model);
			}
		}

		/// <summary>
		/// Displays delete book copy confirmation page.
		/// Implements UC017 (Remove Copy).
		/// Supports BR-06 (Book Management Rights), BR-08 (Copy Deletion Restriction)
		/// </summary>
		[HttpGet]
		public async Task<IActionResult> Delete(int id)
		{
			try
			{
				var result = await _bookCopyService.GetBookCopyByIdAsync(id);
				if (!result.IsSuccess)
				{
					_logger.LogWarning("Failed to retrieve book copy for deletion: {Error}", result.Error);
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction("Index", "Book");
				}

				// BR-08: Check for active loans
				var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(id);
				if (hasActiveLoans.IsSuccess && hasActiveLoans.Value)
				{
					TempData["ErrorMessage"] = "Cannot delete copy because it has active loans.";
					return RedirectToAction("Details", "Book", new { id = result.Value.BookId });
				}

				// BR-08: Check for active reservations
				var hasActiveReservations = await _bookCopyService.CopyHasActiveReservationsAsync(id);
				if (hasActiveReservations.IsSuccess && hasActiveReservations.Value)
				{
					TempData["ErrorMessage"] = "Cannot delete copy because it has active reservations.";
					return RedirectToAction("Details", "Book", new { id = result.Value.BookId });
				}

				ViewBag.HasActiveLoans = hasActiveLoans.IsSuccess && hasActiveLoans.Value;
				ViewBag.HasActiveReservations = hasActiveReservations.IsSuccess && hasActiveReservations.Value;
				ViewBag.CanDelete = !(hasActiveLoans.IsSuccess && hasActiveLoans.Value) &&
								   !(hasActiveReservations.IsSuccess && hasActiveReservations.Value);

				return View(result.Value);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error retrieving book copy for deletion for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An unexpected error occurred while retrieving the book copy.";
				return RedirectToAction("Index", "Book");
			}
		}

		/// <summary>
		/// Processes delete book copy request.
		/// Implements UC017 (Remove Copy).
		/// Supports BR-06 (Book Management Rights), BR-08 (Copy Deletion Restriction), BR-22 (Audit Logging)
		/// </summary>
		[HttpPost, ActionName("Delete")]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> DeleteConfirmed(int id)
		{
			try
			{
				if (!User.TryGetUserId(out int userId))
				{
					return RedirectToAction("Login", "Auth");
				}

				// Get copy details for audit logging and redirection
				var copyResult = await _bookCopyService.GetBookCopyByIdAsync(id);
				if (!copyResult.IsSuccess)
				{
					_logger.LogWarning("Failed to retrieve book copy for deletion confirmation: {Error}", copyResult.Error);
					TempData["ErrorMessage"] = copyResult.Error;
					return RedirectToAction("Index", "Book");
				}

				var copy = copyResult.Value;
				var bookId = copy.BookId;

				// BR-08: Re-check for active loans (to prevent deletion if loans were created after showing the delete page)
				var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(id);
				if (hasActiveLoans.IsSuccess && hasActiveLoans.Value)
				{
					TempData["ErrorMessage"] = "Cannot delete copy because it has active loans.";
					return RedirectToAction("Details", "Book", new { id = bookId });
				}

				// BR-08: Re-check for active reservations
				var hasActiveReservations = await _bookCopyService.CopyHasActiveReservationsAsync(id);
				if (hasActiveReservations.IsSuccess && hasActiveReservations.Value)
				{
					TempData["ErrorMessage"] = "Cannot delete copy because it has active reservations.";
					return RedirectToAction("Details", "Book", new { id = bookId });
				}

				var result = await _bookCopyService.DeleteBookCopyAsync(id);
				if (!result.IsSuccess)
				{
					TempData["ErrorMessage"] = result.Error;
					return RedirectToAction("Details", "Book", new { id = bookId });
				}

				// Audit successful deletion
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = userId,
					ActionType = AuditActionType.Delete,
					EntityType = "BookCopy",
					EntityId = id.ToString(),
					EntityName = copy.CopyNumber,
					Details = $"Book copy deleted successfully: {copy.CopyNumber} for book {copy.Book.Title}",
					IsSuccess = true
				});

				_logger.LogInformation("Book copy deleted successfully by user {UserId}: {CopyId} at {Time}",
					userId, id, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Book copy deleted successfully.";
				return RedirectToAction("Details", "Book", new { id = bookId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error deleting book copy for user {UserId}", User.GetUserId());
				TempData["ErrorMessage"] = "An unexpected error occurred while deleting the book copy.";

				// Try to get book ID for redirection
				try
				{
					var copyResult = await _bookCopyService.GetBookCopyByIdAsync(id);
					if (copyResult.IsSuccess)
					{
						return RedirectToAction("Details", "Book", new { id = copyResult.Value.BookId });
					}
				}
				catch
				{
					// Fallback if we can't get the book ID
					return RedirectToAction("Index", "Book");
				}

				return RedirectToAction("Index", "Book");
			}
		}

		#region Helper Methods

		/// <summary>
		/// Helper method to load view data for update status view.
		/// </summary>
		private async Task LoadUpdateStatusViewData(int copyId)
		{
			try
			{
				var copyResult = await _bookCopyService.GetBookCopyByIdAsync(copyId);
				if (copyResult.IsSuccess)
				{
					ViewBag.Copy = copyResult.Value;

					var hasActiveLoans = await _bookCopyService.CopyHasActiveLoansAsync(copyId);
					ViewBag.HasActiveLoans = hasActiveLoans.IsSuccess && hasActiveLoans.Value;

					var hasActiveReservations = await _bookCopyService.CopyHasActiveReservationsAsync(copyId);
					ViewBag.HasActiveReservations = hasActiveReservations.IsSuccess && hasActiveReservations.Value;

					var validTransitions = GetValidStatusTransitions(copyResult.Value.Status, hasActiveLoans.IsSuccess && hasActiveLoans.Value);
					ViewBag.ValidStatusTransitions = validTransitions;
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error loading update status view data for copy {CopyId}", copyId);
			}
		}

		/// <summary>
		/// Helper method to get valid status transitions based on current status and constraints.
		/// Implements BR-09 (Copy Status Rules) and BR-10 (Copy Return Validation).
		/// </summary>
		private static List<CopyStatus> GetValidStatusTransitions(CopyStatus currentStatus, bool hasActiveLoans)
		{
			var validTransitions = new List<CopyStatus>();

			switch (currentStatus)
			{
				case CopyStatus.Available:
					validTransitions.AddRange([CopyStatus.Borrowed, CopyStatus.Reserved, CopyStatus.Damaged, CopyStatus.Lost]);
					break;

				case CopyStatus.Borrowed:
					// Can only mark as Available if no active loans (BR-10)
					if (!hasActiveLoans)
					{
						validTransitions.Add(CopyStatus.Available);
					}
					validTransitions.AddRange([CopyStatus.Damaged, CopyStatus.Lost]);
					break;

				case CopyStatus.Reserved:
					validTransitions.AddRange([CopyStatus.Available, CopyStatus.Borrowed, CopyStatus.Damaged, CopyStatus.Lost]);
					break;

				case CopyStatus.Damaged:
					validTransitions.AddRange([CopyStatus.Available, CopyStatus.Lost]);
					break;

				case CopyStatus.Lost:
					validTransitions.Add(CopyStatus.Available); // Only if found again
					break;
			}

			return validTransitions;
		}

		/// <summary>
		/// Helper method to validate status transitions.
		/// Implements BR-09 (Copy Status Rules).
		/// </summary>
		private static bool IsValidStatusTransition(CopyStatus fromStatus, CopyStatus toStatus)
		{
			if (fromStatus == toStatus)
				return true; // No change is always valid

			var validTransitions = GetValidStatusTransitions(fromStatus, false); // Conservative check
			return validTransitions.Contains(toStatus);
		}

		#endregion
	}
}