using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for loan information (borrowing transaction).
/// Used in UC018 (Check Out), UC019 (Return Book), UC020 (Renew Loan), UC021 (View Loan History).
/// </summary>
public class LoanDto
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public int BookCopyId { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public LoanStatus Status { get; set; }
    
    // Navigation properties
    public MemberDto Member { get; set; } = null!;
    public BookCopyDto BookCopy { get; set; } = null!;
}

/// <summary>
/// Data transfer object for creating a new loan (book checkout).
/// Used in UC018 (Check Out).
/// </summary>
public class CreateLoanDto
{
    public int MemberId { get; set; }
    public int BookCopyId { get; set; }
    public DateTime? CustomDueDate { get; set; } // Optional, system will use default if not specified
}

/// <summary>
/// Data transfer object for updating an existing loan (return, status change).
/// Used in UC019 (Return Book), UC020 (Renew Loan).
/// </summary>
public class UpdateLoanDto
{
    public DateTime? ReturnDate { get; set; }
    public LoanStatus Status { get; set; }
}

/// <summary>
/// Data transfer object for extending a loan (renewal).
/// Used in UC020 (Renew Loan).
/// </summary>
public class ExtendLoanDto
{
    public int LoanId { get; set; }
    public DateTime NewDueDate { get; set; }
    public string? Reason { get; set; }
}

/// <summary>
/// Extended loan information for detailed views, including calculated overdue and fine info.
/// Used in UC021 (View Loan History) and reporting.
/// </summary>
public class LoanDetailsDto : LoanDto
{
    public bool IsOverdue => Status == LoanStatus.Active && DueDate < DateTime.UtcNow;
    public int DaysOverdue => IsOverdue ? (int)(DateTime.UtcNow - DueDate).TotalDays : 0;
    public decimal CalculatedFine => IsOverdue ? DaysOverdue * 0.50m : 0; // $0.50 per day overdue
}

/// <summary>
/// Data transfer object for checking out multiple books at once to the same member.
/// Used in UC018 (Check Out) alternative flow (bulk checkout).
/// </summary>
public class BulkCheckoutDto
{
    public int MemberId { get; set; }
    public List<int> BookCopyIds { get; set; } = new List<int>();
    public DateTime? CustomDueDate { get; set; } // Optional, system will use default if not specified
}

/// <summary>
/// Data transfer object for returning multiple books at once.
/// Used in UC019 (Return Book) alternative flow (bulk return).
/// </summary>
public class BulkReturnDto
{
    public List<int> LoanIds { get; set; } = new List<int>();
}

/// <summary>
/// Data transfer object for checking loan eligibility for a member.
/// Used in UC018 (Check Out) and business rules BR-13, BR-16.
/// </summary>
public class LoanEligibilityDto
{
    public int MemberId { get; set; }
    public bool IsEligible { get; set; }
    public List<string> Reasons { get; set; } = new List<string>();
    public int AvailableLoanSlots { get; set; }
}

/// <summary>
/// Data transfer object for member loan history and statistics.
/// Used in UC021 (View Loan History).
/// </summary>
public class LoanHistoryDto
{
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int TotalLoans { get; set; }
    public int ActiveLoans { get; set; }
    public int OverdueLoans { get; set; }
    public int CompletedLoans { get; set; }
    public decimal TotalFinesPaid { get; set; }
    public decimal OutstandingFines { get; set; }
    public List<LoanDto> RecentLoans { get; set; } = new List<LoanDto>();
}