using Application.Common;
using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// DTO for creating a new loan (checkout) - UC018
/// </summary>
public record CreateLoanRequest
{
    /// <summary>
    /// The ID of the member borrowing the book.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The ID of the book copy being borrowed.
    /// </summary>
    public int BookCopyId { get; set; }

    /// <summary>
    /// Optional custom due date. If not provided, the system will calculate
    /// based on standard loan period (14 days).
    /// </summary>
    public DateTime? CustomDueDate { get; set; }

    /// <summary>
    /// Optional notes about the loan.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for processing a book return - UC019
/// </summary>
public record ReturnBookRequest
{
    /// <summary>
    /// The ID of the loan being returned.
    /// </summary>
    public int LoanId { get; set; }

    /// <summary>
    /// The condition of the returned book.
    /// </summary>
    public BookCondition BookCondition { get; set; } = BookCondition.Good;

    /// <summary>
    /// Optional notes about the return.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Enum representing the condition of a returned book.
/// </summary>
public enum BookCondition
{
    Good = 1,
    Damaged = 2,
    Lost = 3
}

/// <summary>
/// DTO for extending a loan - UC020
/// </summary>
public record RenewLoanRequest
{
    /// <summary>
    /// The ID of the loan to renew.
    /// </summary>
    public int LoanId { get; set; }

    /// <summary>
    /// Optional custom due date for the renewal.
    /// If not provided, the system will extend by the standard period.
    /// </summary>
    public DateTime? NewDueDate { get; set; }

    /// <summary>
    /// Reason for renewal (optional).
    /// </summary>
    public string? RenewalReason { get; set; }
}

/// <summary>
/// DTO for loan search parameters - UC021
/// </summary>
public record LoanSearchRequest : PagedRequest
{
    /// <summary>
    /// Optional member ID to filter loans by member.
    /// </summary>
    public int? MemberId { get; set; }

    /// <summary>
    /// Optional book copy ID to filter loans by book copy.
    /// </summary>
    public int? BookCopyId { get; set; }

    /// <summary>
    /// Optional book ID to filter loans by book.
    /// </summary>
    public int? BookId { get; set; }

    /// <summary>
    /// Optional status to filter loans by their status.
    /// </summary>
    public LoanStatus? Status { get; set; }

    /// <summary>
    /// Optional start date for filtering loans by date range.
    /// </summary>
    public DateTime? FromDate { get; set; }

    /// <summary>
    /// Optional end date for filtering loans by date range.
    /// </summary>
    public DateTime? ToDate { get; set; }
}

/// <summary>
/// Basic DTO for loan information
/// </summary>
public record LoanBasicDto
{
    /// <summary>
    /// The ID of the loan.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// The ID of the member.
    /// </summary>
    public int MemberId { get; set; }

    /// <summary>
    /// The User ID of the member (for notification targeting).
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// The name of the member.
    /// </summary>
    public string MemberName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the book copy.
    /// </summary>
    public int BookCopyId { get; set; }

    /// <summary>
    /// The ID of the book.
    /// </summary>
    public int BookId { get; set; }

    /// <summary>
    /// The title of the book.
    /// </summary>
    public string BookTitle { get; set; } = string.Empty;

    /// <summary>
    /// The date the loan was created.
    /// </summary>
    public DateTime LoanDate { get; set; }

    /// <summary>
    /// The due date for the loan.
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// The return date, if applicable.
    /// </summary>
    public DateTime? ReturnDate { get; set; }

    /// <summary>
    /// The status of the loan.
    /// </summary>
    public LoanStatus Status { get; set; }

    /// <summary>
    /// Indicates if the loan is overdue.
    /// </summary>
    public bool IsOverdue => Status == LoanStatus.Overdue;

    /// <summary>
    /// The number of days overdue (if applicable).
    /// </summary>
    public int DaysOverdue
    {
        get
        {
            if (ReturnDate.HasValue)
            {
                return (ReturnDate.Value > DueDate) ? (int)(ReturnDate.Value - DueDate).TotalDays : 0;
            }
            return (DateTime.UtcNow > DueDate) ? (int)(DateTime.UtcNow - DueDate).TotalDays : 0;
        }
    }

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
/// Detailed DTO for loan information with related fines
/// </summary>
public class LoanOverrideContext
{
    public bool IsOverride { get; set; }
    public string? Reason { get; set; }
    public List<string> OverriddenRules { get; set; } = new();
}

public record LoanDetailDto : LoanBasicDto
{
    /// <summary>
    /// Collection of associated fines.
    /// </summary>
    public ICollection<FineBasicDto> Fines { get; set; } = new List<FineBasicDto>();

    /// <summary>
    /// Copy identifier.
    /// </summary>
    public string CopyNumber { get; set; } = string.Empty;

    /// <summary>
    /// The book's author.
    /// </summary>
    public string BookAuthor { get; set; } = string.Empty;

    /// <summary>
    /// The ISBN of the book.
    /// </summary>
    public string ISBN { get; set; } = string.Empty;

    public LoanOverrideContext? OverrideContext { get; set; }
}