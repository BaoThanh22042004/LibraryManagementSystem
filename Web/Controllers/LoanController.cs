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
    /// Controller for loan management operations.
	/// Implements UC018 (Check Out), UC019 (Return Book), 
	/// UC020 (Renew Loan), and UC021 (View Loan History).
	/// Business Rules: BR-13, BR-14, BR-15, BR-16, BR-22
	/// </summary>
	[Authorize(Roles = "Admin,Librarian")]
	public class LoanController : Controller
	{
		private readonly ILoanService _loanService;
		private readonly IBookCopyService _bookCopyService;
		private readonly IFineService _fineService;
		private readonly IAuditService _auditService;
		private readonly ILogger<LoanController> _logger;
		private readonly IValidator<CreateLoanRequest> _createLoanValidator;
		private readonly IValidator<ReturnBookRequest> _returnBookValidator;
		private readonly IValidator<RenewLoanRequest> _renewLoanValidator;

		public LoanController(
			ILoanService loanService,
			IBookCopyService bookCopyService,
			IFineService fineService,
			IAuditService auditService,
			ILogger<LoanController> logger,
			IValidator<CreateLoanRequest> createLoanValidator,
			IValidator<ReturnBookRequest> returnBookValidator,
			IValidator<RenewLoanRequest> renewLoanValidator)
		{
			_loanService = loanService;
			_bookCopyService = bookCopyService;
			_fineService = fineService;
			_auditService = auditService;
			_logger = logger;
			_createLoanValidator = createLoanValidator;
			_returnBookValidator = returnBookValidator;
			_renewLoanValidator = renewLoanValidator;
		}

		/// <summary>
		/// Displays loan management index page with search and filter options.
		/// Part of UC021 (View Loan History).
		/// </summary>
		[HttpGet]
        public async Task<IActionResult> Index(LoanSearchRequest? search = null)
        {
            try
            {
                search ??= new LoanSearchRequest { Page = 1, PageSize = 20 };

                var result = await _loanService.GetLoansAsync(search);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return View(new PagedResult<LoanBasicDto>());
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loans");
                TempData["ErrorMessage"] = "An error occurred while retrieving loans.";
                return View(new PagedResult<LoanBasicDto>());
            }
        }

        /// <summary>
        /// Displays loan details.
        /// Part of UC021 (View Loan History).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _loanService.GetLoanByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loan details");
                TempData["ErrorMessage"] = "An error occurred while retrieving loan details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays loans for a specific member.
        /// Part of UC021 (View Loan History).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> MemberLoans(int memberId, int page = 1, int pageSize = 10)
        {
            try
            {
                var search = new LoanSearchRequest
                {
                    MemberId = memberId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _loanService.GetLoansAsync(search);
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
                _logger.LogError(ex, "Error retrieving member loans");
                TempData["ErrorMessage"] = "An error occurred while retrieving member loans.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays the checkout form.
        /// Part of UC018 (Check Out).
        /// </summary>
        [HttpGet]
        public IActionResult Checkout()
        {
            return View(new CreateLoanRequest());
        }

		/// <summary>
		/// Processes checkout request.
		/// Implements UC018 (Check Out) - BR-13, BR-14, BR-16, BR-22
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Checkout(CreateLoanRequest model, bool allowOverride = false, string? overrideReason = null)
		{
			try
			{
				if (!User.TryGetUserId(out int staffId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = await _createLoanValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					return View(model);
				}

				var result = await _loanService.CreateLoanAsync(model, allowOverride, overrideReason);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					return View(model);
				}

				var auditDetails = $"Book checkout successful. Due Date: {result.Value.DueDate:yyyy-MM-dd}. Copy ID: {model.BookCopyId}";
				if (result.Value.OverrideContext?.IsOverride == true)
				{
					auditDetails += $" [OVERRIDE] Reason: {result.Value.OverrideContext.Reason}; Rules: {string.Join(", ", result.Value.OverrideContext.OverriddenRules)}";
				}
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = staffId,
					ActionType = AuditActionType.Create,
					EntityType = "Loan",
					EntityId = result.Value.Id.ToString(),
					EntityName = $"{result.Value.MemberName} - {result.Value.BookTitle}",
					Details = auditDetails,
					IsSuccess = true
				});

				_logger.LogInformation("Staff {StaffId} checked out book copy {BookCopyId} to member {MemberId} at {Time}",
					staffId, model.BookCopyId, model.MemberId, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Book checked out successfully.";
				return RedirectToAction(nameof(Details), new { id = result.Value.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing checkout");
				TempData["ErrorMessage"] = "An error occurred while processing checkout.";
				return View(model);
			}
		}

		/// <summary>
		/// Displays return book form.
		/// Part of UC019 (Return Book).
		/// </summary>
		[HttpGet]
        public async Task<IActionResult> Return(int id)
        {
            try
            {
                var result = await _loanService.GetLoanByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                if (result.Value.Status == LoanStatus.Returned)
                {
                    TempData["WarningMessage"] = "This book has already been returned.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var returnRequest = new ReturnBookRequest
                {
                    LoanId = id,
                    BookCondition = BookCondition.Good
                };

                ViewBag.LoanDetails = result.Value;
                return View(returnRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing book return");
                TempData["ErrorMessage"] = "An error occurred while preparing book return.";
                return RedirectToAction(nameof(Index));
            }
        }

		/// <summary>
		/// Processes return book request.
		/// Implements UC019 (Return Book) - BR-10, BR-15, BR-22
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Return(ReturnBookRequest model)
		{
			try
			{
				if (!User.TryGetUserId(out int staffId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = await _returnBookValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);

					var loanResult = await _loanService.GetLoanByIdAsync(model.LoanId);
					if (loanResult.IsSuccess)
					{
						ViewBag.LoanDetails = loanResult.Value;
					}

					return View(model);
				}

				var result = await _loanService.ReturnBookAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);

					var loanResult = await _loanService.GetLoanByIdAsync(model.LoanId);
					if (loanResult.IsSuccess)
					{
						ViewBag.LoanDetails = loanResult.Value;
					}

					return View(model);
				}

				// Audit successful return
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = staffId,
					ActionType = AuditActionType.Update,
					EntityType = "Loan",
					EntityId = result.Value.Id.ToString(),
					EntityName = $"{result.Value.MemberName} - {result.Value.BookTitle}",
					Details = $"Book return successful. Condition: {model.BookCondition}. Return Date: {result.Value.ReturnDate:yyyy-MM-dd}",
					IsSuccess = true
				});

				_logger.LogInformation("Staff {StaffId} processed return of book copy {BookCopyId} from member {MemberId} at {Time}",
					staffId, result.Value.BookCopyId, result.Value.MemberId, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Book returned successfully.";
				return RedirectToAction(nameof(Details), new { id = result.Value.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing book return");
				TempData["ErrorMessage"] = "An error occurred while processing book return.";
				return RedirectToAction(nameof(Index));
			}
		}

		/// <summary>
		/// Displays renew loan form.
		/// Part of UC020 (Renew Loan).
		/// </summary>
		[HttpGet]
        public async Task<IActionResult> Renew(int id)
        {
            try
            {
                var result = await _loanService.GetLoanByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                if (result.Value.Status != LoanStatus.Active)
                {
                    TempData["ErrorMessage"] = "Only active loans can be renewed.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var renewRequest = new RenewLoanRequest
                {
                    LoanId = id,
                    // Default new due date to 14 days from now
                    NewDueDate = DateTime.Today.AddDays(14)
                };

                ViewBag.LoanDetails = result.Value;
                return View(renewRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing loan renewal");
                TempData["ErrorMessage"] = "An error occurred while preparing loan renewal.";
                return RedirectToAction(nameof(Index));
            }
        }

		/// <summary>
		/// Processes renew loan request.
		/// Implements UC020 (Renew Loan) - BR-14, BR-16, BR-22
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Renew(RenewLoanRequest model, bool allowOverride = false, string? overrideReason = null)
		{
			try
			{
				if (!User.TryGetUserId(out int staffId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = await _renewLoanValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);
					var loanResult = await _loanService.GetLoanByIdAsync(model.LoanId);
					if (loanResult.IsSuccess)
					{
						ViewBag.LoanDetails = loanResult.Value;
					}
					return View(model);
				}

				var result = await _loanService.RenewLoanAsync(model, allowOverride, overrideReason);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);
					var loanResult = await _loanService.GetLoanByIdAsync(model.LoanId);
					if (loanResult.IsSuccess)
					{
						ViewBag.LoanDetails = loanResult.Value;
					}
					return View(model);
				}

				var auditDetails = $"Loan renewal successful. New Due Date: {result.Value.DueDate:yyyy-MM-dd}.";
				if (result.Value.OverrideContext?.IsOverride == true)
				{
					auditDetails += $" [OVERRIDE] Reason: {result.Value.OverrideContext.Reason}; Rules: {string.Join(", ", result.Value.OverrideContext.OverriddenRules)}";
				}
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = staffId,
					ActionType = AuditActionType.Update,
					EntityType = "Loan",
					EntityId = result.Value.Id.ToString(),
					EntityName = $"{result.Value.MemberName} - {result.Value.BookTitle}",
					Details = auditDetails,
					IsSuccess = true
				});

				_logger.LogInformation("Staff {StaffId} renewed loan {LoanId} for member {MemberId} at {Time}",
					staffId, result.Value.Id, result.Value.MemberId, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Loan renewed successfully.";
				return RedirectToAction(nameof(Details), new { id = result.Value.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing loan renewal");
				TempData["ErrorMessage"] = "An error occurred while processing loan renewal.";
				return View(model);
			}
		}

        /// <summary>
        /// Allows members to view their loan history.
        /// Implements member-facing part of UC021 (View Loan History).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyLoans(int page = 1, int pageSize = 10)
        {
            try
            {
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var search = new LoanSearchRequest
                {
                    MemberId = memberId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _loanService.GetLoansAsync(search);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Index", "Home");
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member loans");
                TempData["ErrorMessage"] = "An error occurred while retrieving your loans.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Allows members to view details of their own loans.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyLoanDetails(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _loanService.GetLoanByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(MyLoans));
                }

                // Verify this loan belongs to the current member (BR-03: User Information Access)
                if (result.Value.MemberId != memberId)
                {
                    TempData["ErrorMessage"] = "You can only view your own loans.";
                    return RedirectToAction(nameof(MyLoans));
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loan details");
                TempData["ErrorMessage"] = "An error occurred while retrieving loan details.";
                return RedirectToAction(nameof(MyLoans));
            }
        }

        /// <summary>
        /// Allows members to request loan renewal.
        /// Provides self-service implementation of UC020 (Renew Loan).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> RequestRenewal(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _loanService.GetLoanByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(MyLoans));
                }

                // Verify this loan belongs to the current member (BR-03: User Information Access)
                if (result.Value.MemberId != memberId)
                {
                    TempData["ErrorMessage"] = "You can only renew your own loans.";
                    return RedirectToAction(nameof(MyLoans));
                }

                if (result.Value.Status != LoanStatus.Active)
                {
                    TempData["ErrorMessage"] = "Only active loans can be renewed.";
                    return RedirectToAction(nameof(MyLoanDetails), new { id });
                }

                var renewRequest = new RenewLoanRequest
                {
                    LoanId = id,
                    // NewDueDate will be calculated by the service based on standard loan period
                };

                var renewResult = await _loanService.RenewLoanAsync(renewRequest);
                if (!renewResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = renewResult.Error;
                    return RedirectToAction(nameof(MyLoanDetails), new { id });
                }

                _logger.LogInformation("Member {MemberId} renewed loan {LoanId} until {NewDueDate} at {Time}",
                    memberId, id, renewResult.Value.DueDate, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Loan renewed successfully.";
                return RedirectToAction(nameof(MyLoanDetails), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing member loan renewal");
                TempData["ErrorMessage"] = "An error occurred while processing your renewal request.";
                return RedirectToAction(nameof(MyLoans));
            }
        }

        /// <summary>
        /// Displays the overdue loans report (paged)
        /// UC042
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> OverdueReport(int page = 1, int pageSize = 20)
        {
            try
            {
                var request = new Application.Common.PagedRequest { Page = page, PageSize = pageSize };
                var result = await _loanService.GetOverdueLoansReportPagedAsync(request);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return View("OverdueReport", new Application.Common.PagedResult<Application.DTOs.LoanBasicDto>());
                }
                // Audit log
                if (User.TryGetUserId(out int staffId))
                {
                    await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
                    {
                        UserId = staffId,
                        ActionType = Domain.Enums.AuditActionType.Read,
                        EntityType = "Loan",
                        Details = $"Viewed overdue loans report (page {page})",
                        IsSuccess = true
                    });
                }
                return View("OverdueReport", result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading overdue loans report");
                TempData["ErrorMessage"] = "Unable to load overdue loans report.";
                return View("OverdueReport", new Application.Common.PagedResult<Application.DTOs.LoanBasicDto>());
            }
        }

        /// <summary>
        /// Downloads the full overdue loans report as CSV
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> DownloadOverdueReportCsv()
        {
            try
            {
                var result = await _loanService.GetOverdueLoansReportAsync();
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(OverdueReport));
                }
                var csv = CsvExportExtensions.ToCsv(result.Value);
                var fileName = $"OverdueLoans_{DateTime.UtcNow:yyyyMMdd}.csv";
                // Audit log
                if (User.TryGetUserId(out int staffId))
                {
                    await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
                    {
                        UserId = staffId,
                        ActionType = Domain.Enums.AuditActionType.Export,
                        EntityType = "Loan",
                        Details = "Exported overdue loans report (CSV)",
                        IsSuccess = true
                    });
                }
                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting overdue loans report");
                TempData["ErrorMessage"] = "Unable to export overdue loans report.";
                return RedirectToAction(nameof(OverdueReport));
            }
        }
    }
}