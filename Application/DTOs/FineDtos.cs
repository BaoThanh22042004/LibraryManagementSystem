using Application.Common;
using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// DTO for creating a new fine - UC026
/// </summary>
public record CreateFineRequest
{
    /// <summary>
    /// The ID of the member receiving the fine.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The ID of the associated loan (optional for manual fines).
    /// </summary>
    public int? LoanId { get; set; }

    /// <summary>
    /// The type of fine.
    /// </summary>
    public FineType Type { get; set; }

    /// <summary>
    /// The monetary amount of the fine.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Description or reason for the fine.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if this is an automatic fine calculation or manual creation.
    /// </summary>
    public bool IsAutomaticCalculation { get; set; } = true;
}

/// <summary>
/// DTO for calculating a fine based on overdue days - UC026
/// </summary>
public record CalculateFineRequest
{
    /// <summary>
    /// The ID of the loan to calculate the fine for.
    /// </summary>
    public int LoanId { get; set; }

    /// <summary>
    /// Override the standard daily rate (optional).
    /// </summary>
    public decimal? CustomDailyRate { get; set; }

    /// <summary>
    /// Maximum fine amount (optional, for capping excessive fines).
    /// </summary>
    public decimal? MaximumFineAmount { get; set; }

    /// <summary>
    /// Additional description or reason for the fine calculation.
    /// </summary>
    public string? AdditionalDescription { get; set; }
}

/// <summary>
/// DTO for paying a fine - UC027
/// </summary>
public record PayFineRequest
{
    /// <summary>
    /// The ID of the fine to pay.
    /// </summary>
    public int FineId { get; set; }

    /// <summary>
    /// The payment amount (could be partial payment).
    /// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// The payment method.
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Payment reference or receipt number.
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Additional notes about the payment.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for waiving a fine - UC028
/// </summary>
public record WaiveFineRequest
{
    /// <summary>
    /// The ID of the fine to waive.
    /// </summary>
    public int FineId { get; set; }

    /// <summary>
    /// The reason for waiving the fine.
    /// </summary>
    public string WaiverReason { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the staff member authorizing the waiver.
    /// </summary>
    public int StaffId { get; set; }
}

/// <summary>
/// DTO for fine search parameters - UC029
/// </summary>
public record FineSearchRequest : PagedRequest
{
    /// <summary>
    /// Optional member ID to filter fines by member.
    /// </summary>
    public int? MemberId { get; set; }

    /// <summary>
    /// Optional loan ID to filter fines by loan.
    /// </summary>
    public int? LoanId { get; set; }

    /// <summary>
    /// Optional fine type to filter by.
    /// </summary>
    public FineType? Type { get; set; }

    /// <summary>
    /// Optional status to filter fines by their status.
    /// </summary>
    public FineStatus? Status { get; set; }

    /// <summary>
    /// Optional minimum amount for filtering.
    /// </summary>
    public decimal? MinAmount { get; set; }

    /// <summary>
    /// Optional maximum amount for filtering.
    /// </summary>
    public decimal? MaxAmount { get; set; }

    /// <summary>
    /// Optional start date for filtering fines by date range.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Optional end date for filtering fines by date range.
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Enum for payment methods
/// </summary>
public enum PaymentMethod
{
    Cash = 1,
    CreditCard = 2,
    DebitCard = 3,
    BankTransfer = 4,
    Other = 5
}

/// <summary>
/// Basic DTO for fine information
/// </summary>
public record FineBasicDto
{
    /// <summary>
    /// The ID of the fine.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The ID of the member.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The name of the member.
    /// </summary>
    public string MemberName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the related loan, if applicable.
    /// </summary>
    public int? LoanId { get; set; }

    /// <summary>
    /// The type of fine.
    /// </summary>
    public FineType Type { get; set; }

    /// <summary>
    /// The monetary amount of the fine.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The date the fine was imposed.
    /// </summary>
    public DateTime FineDate { get; set; }

    /// <summary>
    /// The status of the fine.
    /// </summary>
    public FineStatus Status { get; set; }

    /// <summary>
    /// Description or reason for the fine.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The email of the member.
    /// </summary>
    public string MemberEmail { get; set; } = string.Empty;

    /// <summary>
    /// The phone number of the member.
    /// </summary>
    public string? MemberPhone { get; set; }

    /// <summary>
    /// The address of the member.
    /// </summary>
    public string? MemberAddress { get; set; }
}

/// <summary>
/// FineOverrideContext class
/// </summary>
public class FineOverrideContext
{
    public bool IsOverride { get; set; }
    public string? Reason { get; set; }
    public List<string> OverriddenRules { get; set; } = new();
}

/// <summary>
/// Detailed DTO for fine information
/// </summary>
public record FineDetailDto : FineBasicDto
{
    /// <summary>
    /// Book title for loan-related fines.
    /// </summary>
    public string? BookTitle { get; set; }

    /// <summary>
    /// Due date for overdue fines.
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Return date for overdue fines.
    /// </summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>
    /// Number of days overdue for overdue fines.
    /// </summary>
    public int? DaysOverdue { get; set; }

    /// <summary>
    /// Payment date if the fine has been paid.
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Payment method if the fine has been paid.
    /// </summary>
    public PaymentMethod? PaymentMethod { get; set; }

    /// <summary>
    /// Payment reference if the fine has been paid.
    /// </summary>
    public string? PaymentReference { get; set; }

    /// <summary>
    /// Waiver reason if the fine has been waived.
    /// </summary>
    public string? WaiverReason { get; set; }

    /// <summary>
    /// The ID of the staff member who processed the payment or waiver.
    /// </summary>
    public int? ProcessedByStaffId { get; set; }

    /// <summary>
    /// The name of the staff member who processed the payment or waiver.
    /// </summary>
    public string? ProcessedByStaffName { get; set; }

    /// <summary>
    /// Override context for the fine.
    /// </summary>
    public FineOverrideContext? OverrideContext { get; set; }
}

public record OutstandingFinesDto
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public decimal TotalOutstanding { get; set; }
}