using Application.Common;
using Application.DTOs;
using Application.Interfaces;
using Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Web.Extensions;

namespace Web.Controllers
{
    /// <summary>
    /// Controller for fine management operations.
    /// Implements UC026 (Calculate Fine), UC027 (Pay Fine),
    /// UC028 (Waive Fine), and UC029 (View Fine History).
    /// </summary>
    public class FineController : Controller
    {
		private readonly IFineService _fineService;
		private readonly ILoanService _loanService;
		private readonly IAuditService _auditService;
		private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly ILogger<FineController> _logger;
		private readonly IValidator<CreateFineRequest> _createFineValidator;
		private readonly IValidator<CalculateFineRequest> _calculateFineValidator;
		private readonly IValidator<PayFineRequest> _payFineValidator;
		private readonly IValidator<WaiveFineRequest> _waiveFineValidator;

		public FineController(
			IFineService fineService,
			ILoanService loanService,
			IAuditService auditService,
			INotificationService notificationService,
            IUserService userService,
            ILogger<FineController> logger,
			IValidator<CreateFineRequest> createFineValidator,
			IValidator<CalculateFineRequest> calculateFineValidator,
			IValidator<PayFineRequest> payFineValidator,
			IValidator<WaiveFineRequest> waiveFineValidator)
		{
			_fineService = fineService;
			_loanService = loanService;
			_auditService = auditService;
			_notificationService = notificationService;
            _userService = userService;
            _logger = logger;
			_createFineValidator = createFineValidator;
			_calculateFineValidator = calculateFineValidator;
			_payFineValidator = payFineValidator;
			_waiveFineValidator = waiveFineValidator;
		}

		/// <summary>
		/// Displays fine management index page with search and filter options.
		/// For staff use only. Part of UC029 (View Fine History).
		/// </summary>
		[HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Index(FineSearchRequest? search = null)
        {
            try
            {
                search ??= new FineSearchRequest { Page = 1, PageSize = 20 };

                var result = await _fineService.GetFinesAsync(search);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return View(new PagedResult<FineBasicDto>());
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fines");
                TempData["ErrorMessage"] = "An error occurred while retrieving fines.";
                return View(new PagedResult<FineBasicDto>());
            }
        }

        /// <summary>
        /// Displays fine details.
        /// For staff use only. Part of UC029 (View Fine History).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var result = await _fineService.GetFineByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fine details");
                TempData["ErrorMessage"] = "An error occurred while retrieving fine details.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays fines for a specific member.
        /// For staff use only. Part of UC029 (View Fine History).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> MemberFines(int memberId, int page = 1, int pageSize = 10)
        {
            try
            {
                var userDetailsResult = await _userService.GetUserDetailsByMemberIdAsync(memberId);

                if (userDetailsResult.IsSuccess)
                {
                    ViewBag.MemberDetails = userDetailsResult.Value;
                }

                var search = new FineSearchRequest
                {
                    MemberId = memberId,
                    Page = page,
                    PageSize = pageSize
                };

                var result = await _fineService.GetFinesAsync(search);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                // Get total pending fine amount for the member
                var totalFinesResult = await _fineService.GetTotalPendingFineAmountAsync(memberId);
                if (totalFinesResult.IsSuccess)
                {
                    ViewBag.TotalPendingFines = totalFinesResult.Value;
                }

                ViewBag.MemberId = memberId;
                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member fines");
                TempData["ErrorMessage"] = "An error occurred while retrieving member fines.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays fines for a specific loan.
        /// For staff use only. Part of UC029 (View Fine History).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> LoanFines(int loanId)
        {
            try
            {
                var result = await _fineService.GetFinesByLoanIdAsync(loanId);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                // Get loan details
                var loanResult = await _loanService.GetLoanByIdAsync(loanId);
                if (loanResult.IsSuccess)
                {
                    ViewBag.LoanDetails = loanResult.Value;
                }

                ViewBag.LoanId = loanId;
                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving loan fines");
                TempData["ErrorMessage"] = "An error occurred while retrieving loan fines.";
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task PrepareFineCreateViewData(int? selectedMemberId = null, int? loanId = null)
        {
            var memberSearchRequest = new UserSearchRequest
            {
                Role = UserRole.Member,
                Status = UserStatus.Active,
                PageSize = int.MaxValue
            };
            var membersResult = await _userService.SearchUsersAsync(memberSearchRequest);
            if (membersResult.IsSuccess && membersResult.Value.Items.Any())
            {
                ViewBag.MemberList = new SelectList(membersResult.Value.Items, "Id", "FullName", selectedMemberId);
            }
            else
            {
                _logger.LogWarning("Could not load active member list for fine creation.");
                ViewBag.MemberList = new SelectList(Enumerable.Empty<SelectListItem>(), "Id", "FullName");
            }

            if (loanId.HasValue)
            {
                var loanResult = await _loanService.GetLoanByIdAsync(loanId.Value);
                if (loanResult.IsSuccess)
                {
                    ViewBag.LoanDetails = loanResult.Value;
                }
            }
        }

        /// <summary>
        /// Displays the create fine form.
        /// This version is updated to use a dropdown for member selection.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(int? memberId = null, int? loanId = null)
        {
            var model = new CreateFineRequest();
            int? userIdForDropdown = null;

            if (loanId.HasValue)
            {
                model.LoanId = loanId.Value;
                var loanResult = await _loanService.GetLoanByIdAsync(loanId.Value);
                if (loanResult.IsSuccess)
                {
                    ViewBag.LoanDetails = loanResult.Value;
                    model.MemberId = loanResult.Value.MemberId;

                    var userResult = await _userService.GetUserDetailsByMemberIdAsync(loanResult.Value.MemberId);
                    if (userResult.IsSuccess)
                    {
                        userIdForDropdown = userResult.Value.Id;
                    }
                }
            }
            else if (memberId.HasValue)
            {
                userIdForDropdown = memberId.Value;
            }

            await PrepareFineCreateViewData(userIdForDropdown, loanId);

            model.Type = (loanId.HasValue) ? FineType.Overdue : FineType.Other;
            model.IsAutomaticCalculation = false;

            if (userIdForDropdown.HasValue && !loanId.HasValue)
            {
                model.MemberId = userIdForDropdown.Value;
            }

            return View(model);
        }

        /// <summary>
        /// Processes create fine request.
        /// For staff use only. Implements UC026 (Calculate Fine).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(CreateFineRequest model, bool allowOverride = false, string? overrideReason = null)
        {
            try
            {
                if (!User.TryGetUserId(out int staffId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                int userIdFromForm = model.MemberId;

                if (!model.LoanId.HasValue)
                {
                    var userDetailsResult = await _userService.GetUserDetailsAsync(userIdFromForm);
                    if (!userDetailsResult.IsSuccess || userDetailsResult.Value.MemberDetails == null)
                    {
                        TempData["ErrorMessage"] = "The selected user is not a valid member.";
                        await PrepareFineCreateViewData(userIdFromForm, model.LoanId);
                        return View(model);
                    }
                    model.MemberId = userDetailsResult.Value.MemberDetails.Id;
                }

                var validationResult = await _createFineValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    TempData["ErrorMessage"] = "The request has validation errors.";

                    await PrepareFineCreateViewData(userIdFromForm, model.LoanId);
                    return View(model);
                }

                var result = await _fineService.CreateFineAsync(model, allowOverride, overrideReason);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.Error);
                    TempData["ErrorMessage"] = result.Error;

                    await PrepareFineCreateViewData(userIdFromForm, model.LoanId);
                    return View(model);
                }

                // Audit successful fine creation, including override context if present
                var auditDetails = $"Fine created successfully. Amount: {result.Value.Amount:C}. Type: {result.Value.Type}.";
                if (result.Value.OverrideContext?.IsOverride == true)
                {
                    auditDetails += $" [OVERRIDE] Reason: {result.Value.OverrideContext.Reason}; Rules: {string.Join(", ", result.Value.OverrideContext.OverriddenRules)}";
                }
                await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                {
                    UserId = staffId,
                    ActionType = AuditActionType.Create,
                    EntityType = "Fine",
                    EntityId = result.Value.Id.ToString(),
                    EntityName = $"Fine - {result.Value.MemberName}",
                    Details = auditDetails,
                    IsSuccess = true
                });

                _logger.LogInformation("Staff {StaffId} created fine {FineId} of {Amount:C} for member {MemberId} at {Time}",
                    staffId, result.Value.Id, model.Amount, model.MemberId, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Fine created successfully.";

                // Send fine incurred notification
                await _notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = result.Value.UserId,
                    Type = NotificationType.LoanReminder, // Consider extending NotificationType for FineIncurred
                    Subject = $"Fine Incurred: {result.Value.BookTitle ?? "Library Fine"}",
                    Message = $"A fine of {result.Value.Amount:C} has been added to your account. Reason: {result.Value.Description}"
                });

                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fine");
                TempData["ErrorMessage"] = "An error occurred while creating the fine.";
                await PrepareFineCreateViewData(model.MemberId, model.LoanId);
                return View(model);
            }
        }

        /// <summary>
        /// Displays the calculate fine form for a specific loan.
        /// For staff use only. Part of UC026 (Calculate Fine).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Calculate(int loanId)
        {
            try
            {
                var loanResult = await _loanService.GetLoanByIdAsync(loanId);
                if (!loanResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = loanResult.Error;
                    return RedirectToAction(nameof(Index));
                }

                var model = new CalculateFineRequest
                {
                    LoanId = loanId
                };

                ViewBag.LoanDetails = loanResult.Value;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing fine calculation");
                TempData["ErrorMessage"] = "An error occurred while preparing fine calculation.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Processes calculate fine request.
        /// For staff use only. Implements UC026 (Calculate Fine).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Calculate(CalculateFineRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int staffId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var validationResult = await _calculateFineValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    
                    // Reload loan details for the view
                    var loanResult = await _loanService.GetLoanByIdAsync(model.LoanId);
                    if (loanResult.IsSuccess)
                    {
                        ViewBag.LoanDetails = loanResult.Value;
                    }
                    
                    return View(model);
                }

                var result = await _fineService.CalculateFineAsync(model);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.Error);
                    
                    // Reload loan details for the view
                    var loanResult = await _loanService.GetLoanByIdAsync(model.LoanId);
                    if (loanResult.IsSuccess)
                    {
                        ViewBag.LoanDetails = loanResult.Value;
                    }
                    
                    return View(model);
                }

                _logger.LogInformation("Staff {StaffId} calculated fine {FineId} of {Amount:C} for loan {LoanId} at {Time}",
                    staffId, result.Value.Id, result.Value.Amount, model.LoanId, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Fine calculated successfully.";

                // Send fine incurred notification
                await _notificationService.CreateNotificationAsync(new NotificationCreateDto
                {
                    UserId = result.Value.UserId,
                    Type = NotificationType.LoanReminder, // Consider extending NotificationType for FineIncurred
                    Subject = $"Fine Incurred: {result.Value.BookTitle ?? "Library Fine"}",
                    Message = $"A fine of {result.Value.Amount:C} has been added to your account. Reason: {result.Value.Description}"
                });

                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating fine");
                TempData["ErrorMessage"] = "An error occurred while calculating the fine.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Displays pay fine form.
        /// For staff use only. Part of UC027 (Pay Fine).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Pay(int id)
        {
            try
            {
                var result = await _fineService.GetFineByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                if (result.Value.Status != FineStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Only pending fines can be paid.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                var payRequest = new PayFineRequest
                {
                    FineId = id,
                    PaymentAmount = result.Value.Amount,
                    PaymentMethod = PaymentMethod.Cash
                };

                ViewBag.FineDetails = result.Value;
                return View(payRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing fine payment");
                TempData["ErrorMessage"] = "An error occurred while preparing fine payment.";
                return RedirectToAction(nameof(Index));
            }
        }

		/// <summary>
		/// Processes fine payment with audit logging.
		/// Implements UC027 (Pay Fine) - BR-22
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin,Librarian")]
		public async Task<IActionResult> Pay(PayFineRequest model)
		{
			try
			{
				if (!User.TryGetUserId(out int staffId))
				{
					return RedirectToAction("Login", "Auth");
				}

				var validationResult = await _payFineValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);

					var fineResult = await _fineService.GetFineByIdAsync(model.FineId);
					if (fineResult.IsSuccess)
					{
						ViewBag.FineDetails = fineResult.Value;
					}

					return View(model);
				}

				var result = await _fineService.PayFineAsync(model);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);

					var fineResult = await _fineService.GetFineByIdAsync(model.FineId);
					if (fineResult.IsSuccess)
					{
						ViewBag.FineDetails = fineResult.Value;
					}

					return View(model);
				}

				// Audit successful payment
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = staffId,
					ActionType = AuditActionType.Update,
					EntityType = "Fine",
					EntityId = result.Value.Id.ToString(),
					EntityName = $"Fine Payment - {result.Value.MemberName}",
					Details = $"Fine payment processed successfully. Amount: {model.PaymentAmount:C}. Method: {model.PaymentMethod}",
					IsSuccess = true
				});

				_logger.LogInformation("Staff {StaffId} processed payment of {Amount:C} for fine {FineId} at {Time}",
					staffId, model.PaymentAmount, model.FineId, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Fine payment processed successfully.";

				// Send fine payment confirmation notification
				await _notificationService.CreateNotificationAsync(new NotificationCreateDto
				{
					UserId = result.Value.UserId,
					Type = NotificationType.LoanReminder, // Consider extending NotificationType for FinePaid
					Subject = $"Fine Paid: {result.Value.BookTitle ?? "Library Fine"}",
					Message = $"Your payment of {result.Value.Amount:C} for the fine has been received. Thank you!"
				});

				return RedirectToAction(nameof(Details), new { id = model.FineId });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error processing fine payment");
				TempData["ErrorMessage"] = "An error occurred while processing fine payment.";
				return RedirectToAction(nameof(Index));
			}
		}

		/// <summary>
		/// Displays waive fine form.
		/// For staff use only. Part of UC028 (Waive Fine).
		/// </summary>
		[HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Waive(int id)
        {
            try
            {
                var result = await _fineService.GetFineByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(Index));
                }

                if (result.Value.Status != FineStatus.Pending)
                {
                    TempData["ErrorMessage"] = "Only pending fines can be waived.";
                    return RedirectToAction(nameof(Details), new { id });
                }

                if (!User.TryGetUserId(out int staffId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var waiveRequest = new WaiveFineRequest
                {
                    FineId = id,
                    StaffId = staffId,
                    WaiverReason = string.Empty
                };

                ViewBag.FineDetails = result.Value;
                return View(waiveRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing fine waiver");
                TempData["ErrorMessage"] = "An error occurred while preparing fine waiver.";
                return RedirectToAction(nameof(Index));
            }
        }

		/// <summary>
		/// Processes fine waiver with enhanced authorization and audit logging.
		/// Implements UC028 (Waive Fine) - BR-01, BR-22
		/// </summary>
		[HttpPost]
		[ValidateAntiForgeryToken]
		[Authorize(Roles = "Admin,Librarian")]
		public async Task<IActionResult> Waive(WaiveFineRequest model, bool allowOverride = false, string? overrideReason = null)
		{
			try
			{
				if (!User.TryGetUserId(out int staffId))
				{
					return RedirectToAction("Login", "Auth");
				}

				// Enhanced authorization check - BR-01
				if (!User.IsInRole("Admin") && !User.IsInRole("Librarian"))
				{
					TempData["ErrorMessage"] = "You do not have permission to waive fines.";
					return RedirectToAction(nameof(Index));
				}

				model.StaffId = staffId;

				var validationResult = await _waiveFineValidator.ValidateAsync(model);
				if (!validationResult.IsValid)
				{
					validationResult.AddToModelState(ModelState);

					var fineResult = await _fineService.GetFineByIdAsync(model.FineId);
					if (fineResult.IsSuccess)
					{
						ViewBag.FineDetails = fineResult.Value;
					}

					return View(model);
				}

				// Pass override parameters to service
				var result = await _fineService.WaiveFineAsync(model, allowOverride, overrideReason);
				if (!result.IsSuccess)
				{
					ModelState.AddModelError(string.Empty, result.Error);

					var fineResult = await _fineService.GetFineByIdAsync(model.FineId);
					if (fineResult.IsSuccess)
					{
						ViewBag.FineDetails = fineResult.Value;
					}

					return View(model);
				}

				// Audit successful waiver, including override context if present
				var auditDetails = $"Fine waived successfully. Amount: {result.Value.Amount:C}. Reason: {model.WaiverReason}";
				if (result.Value.OverrideContext?.IsOverride == true)
				{
					auditDetails += $" [OVERRIDE] Reason: {result.Value.OverrideContext.Reason}; Rules: {string.Join(", ", result.Value.OverrideContext.OverriddenRules)}";
				}
				await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
				{
					UserId = staffId,
					ActionType = AuditActionType.Update,
					EntityType = "Fine",
					EntityId = result.Value.Id.ToString(),
					EntityName = $"Fine Waiver - {result.Value.MemberName}",
					Details = auditDetails,
					IsSuccess = true
				});

				_logger.LogInformation("Staff {StaffId} waived fine {FineId} of {Amount:C} at {Time}",
					staffId, model.FineId, result.Value.Amount, DateTime.UtcNow);

				TempData["SuccessMessage"] = "Fine waived successfully.";

				// Send fine waiver notification
				await _notificationService.CreateNotificationAsync(new NotificationCreateDto
				{
					UserId = result.Value.UserId,
					Type = NotificationType.LoanReminder, // Consider extending NotificationType for FineWaived
					Subject = $"Fine Waived: {result.Value.BookTitle ?? "Library Fine"}",
					Message = $"Your fine of {result.Value.Amount:C} has been waived. Reason: {result.Value.WaiverReason ?? "N/A"}"
				});

				return RedirectToAction(nameof(Details), new { id = result.Value.Id });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error waiving fine");
				TempData["ErrorMessage"] = "An error occurred while waiving the fine.";
				return View(model);
			}
		}

        /// <summary>
        /// Allows members to view their fines.
        /// Implements member-facing part of UC029 (View Fine History).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyFines(int page = 1, int pageSize = 10)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _fineService.GetMyFinesAsync(userId, page, pageSize);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Index", "Home");
                }

                var userDetailsResult = await _userService.GetUserDetailsAsync(userId);
                if (userDetailsResult.IsSuccess && userDetailsResult.Value.MemberDetails != null)
                {
                    var memberId = userDetailsResult.Value.MemberDetails.Id;

                    var totalFinesResult = await _fineService.GetTotalPendingFineAmountAsync(memberId);
                    if (totalFinesResult.IsSuccess)
                    {
                        ViewBag.TotalPendingFines = totalFinesResult.Value;
                    }
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member fines", ex);
                TempData["ErrorMessage"] = "An error occurred while retrieving your fines.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Allows members to view details of their own fines.
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> MyFineDetails(int id)
        {
            try
            {
                if (!User.TryGetUserId(out int userId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _fineService.GetMyFineDetailsAsync(id, userId);

                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(MyFines));
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving fine details");
                TempData["ErrorMessage"] = "An error occurred while retrieving fine details.";
                return RedirectToAction(nameof(MyFines));
            }
        }

        /// <summary>
        /// Displays the fines report (paged)
        /// UC043
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> FinesReport(int page = 1, int pageSize = 20)
        {
            try
            {
                var request = new Application.Common.PagedRequest { Page = page, PageSize = pageSize };
                var result = await _fineService.GetFinesReportPagedAsync(request);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return View("FinesReport", new Application.Common.PagedResult<Application.DTOs.FineBasicDto>());
                }
                // Audit log
                if (User.TryGetUserId(out int staffId))
                {
                    await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
                    {
                        UserId = staffId,
                        ActionType = Domain.Enums.AuditActionType.Read,
                        EntityType = "Fine",
                        Details = $"Viewed fines report (page {page})",
                        IsSuccess = true
                    });
                }
                return View("FinesReport", result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading fines report");
                TempData["ErrorMessage"] = "Unable to load pending fines report.";
                return View("FinesReport", new Application.Common.PagedResult<Application.DTOs.FineBasicDto>());
            }
        }

        /// <summary>
        /// Downloads the full fines report as CSV
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> DownloadFinesReportCsv()
        {
            try
            {
                var result = await _fineService.GetFinesReportAsync();
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(FinesReport));
                }
                var csv = CsvExportExtensions.ToCsv(result.Value);
                var fileName = $"FinesReport_{DateTime.UtcNow:yyyyMMdd}.csv";
                // Audit log
                if (User.TryGetUserId(out int staffId))
                {
                    await _auditService.CreateAuditLogAsync(new Application.DTOs.CreateAuditLogRequest
                    {
                        UserId = staffId,
                        ActionType = Domain.Enums.AuditActionType.Export,
                        EntityType = "Fine",
                        Details = "Exported fines report (CSV)",
                        IsSuccess = true
                    });
                }
                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting fines report");
                TempData["ErrorMessage"] = "Unable to export fines report.";
                return RedirectToAction(nameof(FinesReport));
            }
        }

        /// <summary>
        /// Gets the outstanding fines for a specific member (staff view)
        /// UC044
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> OutstandingFines(int memberId)
        {
            try
            {
                var userDetailsResult = await _userService.GetUserDetailsByMemberIdAsync(memberId);
                if (!userDetailsResult.IsSuccess)
                {
                    TempData["ErrorMessage"] = userDetailsResult.Error;
                    return RedirectToAction("Index", "User");
                }
                var userDetails = userDetailsResult.Value;
                ViewBag.MemberDetails = userDetails;

                var result = await _fineService.GetOutstandingFinesAsync(memberId);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction("Details", "User", new { id = userDetails.Id });
                }

                if (User.TryGetUserId(out int staffId))
                {
                    await _auditService.CreateAuditLogAsync(new CreateAuditLogRequest
                    {
                        UserId = staffId,
                        ActionType = Domain.Enums.AuditActionType.Read,
                        EntityType = "Fine",
                        EntityId = memberId.ToString(),
                        Details = "Viewed outstanding fines for member",
                        IsSuccess = true
                    });
                }

                return View("OutstandingFines", result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading outstanding fines for member {memberId}", memberId);
                TempData["ErrorMessage"] = "Unable to load outstanding fines.";
                return RedirectToAction("Index");
            }
        }
    }
}