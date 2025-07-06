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
    /// Controller for fine management operations.
    /// Implements UC026 (Calculate Fine), UC027 (Pay Fine),
    /// UC028 (Waive Fine), and UC029 (View Fine History).
    /// </summary>
    public class FineController : Controller
    {
        private readonly IFineService _fineService;
        private readonly ILoanService _loanService;
        private readonly ILogger<FineController> _logger;
        private readonly IValidator<CreateFineRequest> _createFineValidator;
        private readonly IValidator<CalculateFineRequest> _calculateFineValidator;
        private readonly IValidator<PayFineRequest> _payFineValidator;
        private readonly IValidator<WaiveFineRequest> _waiveFineValidator;

        public FineController(
            IFineService fineService,
            ILoanService loanService,
            ILogger<FineController> logger,
            IValidator<CreateFineRequest> createFineValidator,
            IValidator<CalculateFineRequest> calculateFineValidator,
            IValidator<PayFineRequest> payFineValidator,
            IValidator<WaiveFineRequest> waiveFineValidator)
        {
            _fineService = fineService;
            _loanService = loanService;
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
                return View("Index", result.Value);
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

        /// <summary>
        /// Displays the create fine form.
        /// For staff use only. Part of UC026 (Calculate Fine).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(int? memberId = null, int? loanId = null)
        {
            var model = new CreateFineRequest();
            
            if (memberId.HasValue)
            {
                model.MemberId = memberId.Value;
            }
            
            if (loanId.HasValue)
            {
                model.LoanId = loanId.Value;
                
                // Get loan details to display
                var loanResult = await _loanService.GetLoanByIdAsync(loanId.Value);
                if (loanResult.IsSuccess)
                {
                    ViewBag.LoanDetails = loanResult.Value;
                    model.MemberId = loanResult.Value.MemberId;
                }
            }
            
            // Set default fine type to Other (manual fine)
            model.Type = FineType.Other;
            model.IsAutomaticCalculation = false;
            
            return View(model);
        }

        /// <summary>
        /// Processes create fine request.
        /// For staff use only. Implements UC026 (Calculate Fine).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Create(CreateFineRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int staffId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var validationResult = await _createFineValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    
                    // Reload loan details for the view if applicable
                    if (model.LoanId.HasValue)
                    {
                        var loanResult = await _loanService.GetLoanByIdAsync(model.LoanId.Value);
                        if (loanResult.IsSuccess)
                        {
                            ViewBag.LoanDetails = loanResult.Value;
                        }
                    }
                    
                    return View(model);
                }

                var result = await _fineService.CreateFineAsync(model);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.Error);
                    
                    // Reload loan details for the view if applicable
                    if (model.LoanId.HasValue)
                    {
                        var loanResult = await _loanService.GetLoanByIdAsync(model.LoanId.Value);
                        if (loanResult.IsSuccess)
                        {
                            ViewBag.LoanDetails = loanResult.Value;
                        }
                    }
                    
                    return View(model);
                }

                _logger.LogInformation("Staff {StaffId} created fine {FineId} of {Amount:C} for member {MemberId} at {Time}",
                    staffId, result.Value.Id, model.Amount, model.MemberId, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Fine created successfully.";
                return RedirectToAction(nameof(Details), new { id = result.Value.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating fine");
                TempData["ErrorMessage"] = "An error occurred while creating the fine.";
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
        /// Processes pay fine request.
        /// For staff use only. Implements UC027 (Pay Fine).
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
                    
                    // Reload fine details for the view
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
                    
                    // Reload fine details for the view
                    var fineResult = await _fineService.GetFineByIdAsync(model.FineId);
                    if (fineResult.IsSuccess)
                    {
                        ViewBag.FineDetails = fineResult.Value;
                    }
                    
                    return View(model);
                }

                _logger.LogInformation("Staff {StaffId} processed payment of {Amount:C} for fine {FineId} at {Time}",
                    staffId, model.PaymentAmount, model.FineId, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Fine payment processed successfully.";
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
        /// Processes waive fine request.
        /// For staff use only. Implements UC028 (Waive Fine).
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Librarian")]
        public async Task<IActionResult> Waive(WaiveFineRequest model)
        {
            try
            {
                if (!User.TryGetUserId(out int staffId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                // Ensure staff ID is set correctly
                model.StaffId = staffId;

                var validationResult = await _waiveFineValidator.ValidateAsync(model);
                if (!validationResult.IsValid)
                {
                    validationResult.AddToModelState(ModelState);
                    
                    // Reload fine details for the view
                    var fineResult = await _fineService.GetFineByIdAsync(model.FineId);
                    if (fineResult.IsSuccess)
                    {
                        ViewBag.FineDetails = fineResult.Value;
                    }
                    
                    return View(model);
                }

                var result = await _fineService.WaiveFineAsync(model);
                if (!result.IsSuccess)
                {
                    ModelState.AddModelError(string.Empty, result.Error);
                    
                    // Reload fine details for the view
                    var fineResult = await _fineService.GetFineByIdAsync(model.FineId);
                    if (fineResult.IsSuccess)
                    {
                        ViewBag.FineDetails = fineResult.Value;
                    }
                    
                    return View(model);
                }

                _logger.LogInformation("Staff {StaffId} waived fine {FineId} at {Time}",
                    staffId, model.FineId, DateTime.UtcNow);
                
                TempData["SuccessMessage"] = "Fine waived successfully.";
                return RedirectToAction(nameof(Details), new { id = model.FineId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing fine waiver");
                TempData["ErrorMessage"] = "An error occurred while processing fine waiver.";
                return RedirectToAction(nameof(Index));
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
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
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
                    return RedirectToAction("Index", "Home");
                }

                // Get total pending fine amount
                var totalFinesResult = await _fineService.GetTotalPendingFineAmountAsync(memberId);
                if (totalFinesResult.IsSuccess)
                {
                    ViewBag.TotalPendingFines = totalFinesResult.Value;
                }

                return View(result.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving member fines");
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
                if (!User.TryGetUserId(out int memberId))
                {
                    return RedirectToAction("Login", "Auth");
                }

                var result = await _fineService.GetFineByIdAsync(id);
                if (!result.IsSuccess)
                {
                    TempData["ErrorMessage"] = result.Error;
                    return RedirectToAction(nameof(MyFines));
                }

                // Verify this fine belongs to the current member (BR-03: User Information Access)
                if (result.Value.MemberId != memberId)
                {
                    TempData["ErrorMessage"] = "You can only view your own fines.";
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
    }
}