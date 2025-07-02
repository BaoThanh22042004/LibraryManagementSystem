using Domain.Enums;

namespace Application.DTOs;

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

public class CreateLoanDto
{
    public int MemberId { get; set; }
    public int BookCopyId { get; set; }
    public DateTime? CustomDueDate { get; set; } // Optional, system will use default if not specified
}

public class UpdateLoanDto
{
    public DateTime? ReturnDate { get; set; }
    public LoanStatus Status { get; set; }
}

public class ExtendLoanDto
{
    public int LoanId { get; set; }
    public DateTime NewDueDate { get; set; }
    public string? Reason { get; set; }
}

public class LoanDetailsDto : LoanDto
{
    public bool IsOverdue => Status == LoanStatus.Active && DueDate < DateTime.UtcNow;
    public int DaysOverdue => IsOverdue ? (int)(DateTime.UtcNow - DueDate).TotalDays : 0;
    public decimal CalculatedFine => IsOverdue ? DaysOverdue * 0.50m : 0; // $0.50 per day overdue
}

// DTO for checking out multiple books at once to the same member
public class BulkCheckoutDto
{
    public int MemberId { get; set; }
    public List<int> BookCopyIds { get; set; } = new List<int>();
    public DateTime? CustomDueDate { get; set; } // Optional, system will use default if not specified
}

// DTO for returning multiple books at once
public class BulkReturnDto
{
    public List<int> LoanIds { get; set; } = new List<int>();
}

// DTO for checking loan eligibility
public class LoanEligibilityDto
{
    public int MemberId { get; set; }
    public bool IsEligible { get; set; }
    public List<string> Reasons { get; set; } = new List<string>();
    public int AvailableLoanSlots { get; set; }
}

// DTO for loan history
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