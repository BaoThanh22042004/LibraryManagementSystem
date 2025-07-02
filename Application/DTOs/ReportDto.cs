using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// DTO for representing overdue loan reports
/// </summary>
public class OverdueReportDto
{
    /// <summary>
    /// List of overdue loans
    /// </summary>
    public List<OverdueLoanDto> OverdueLoans { get; set; } = [];
    
    /// <summary>
    /// Total number of overdue loans
    /// </summary>
    public int TotalCount => OverdueLoans.Count;
    
    /// <summary>
    /// Total potential fine amount based on current overdue status
    /// </summary>
    public decimal TotalPotentialFineAmount => OverdueLoans.Sum(l => l.CalculatedFine);
    
    /// <summary>
    /// Date when report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// DTO for representing details of an overdue loan
/// </summary>
public class OverdueLoanDto
{
    /// <summary>
    /// Loan ID
    /// </summary>
    public int LoanId { get; set; }
    
    /// <summary>
    /// Member ID
    /// </summary>
    public int MemberId { get; set; }
    
    /// <summary>
    /// Member full name
    /// </summary>
    public string MemberName { get; set; } = string.Empty;
    
    /// <summary>
    /// Member email address for contact
    /// </summary>
    public string MemberEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Book title
    /// </summary>
    public string BookTitle { get; set; } = string.Empty;
    
    /// <summary>
    /// Book ISBN
    /// </summary>
    public string ISBN { get; set; } = string.Empty;
    
    /// <summary>
    /// Copy number of the borrowed book
    /// </summary>
    public string CopyNumber { get; set; } = string.Empty;
    
    /// <summary>
    /// Date when the book was borrowed
    /// </summary>
    public DateTime LoanDate { get; set; }
    
    /// <summary>
    /// Original due date for the loan
    /// </summary>
    public DateTime DueDate { get; set; }
    
    /// <summary>
    /// Number of days the loan is overdue
    /// </summary>
    public int DaysOverdue => (int)(DateTime.UtcNow - DueDate).TotalDays;
    
    /// <summary>
    /// Current status of the loan
    /// </summary>
    public LoanStatus Status { get; set; }
    
    /// <summary>
    /// Calculated fine amount based on days overdue (typically $0.50 per day)
    /// </summary>
    public decimal CalculatedFine => DaysOverdue * 0.50m; // $0.50 per day overdue
}

/// <summary>
/// DTO for representing fine reports
/// </summary>
public class FineReportDto
{
    /// <summary>
    /// List of fines in the report
    /// </summary>
    public List<FineReportItemDto> Fines { get; set; } = [];
    
    /// <summary>
    /// Total number of fines
    /// </summary>
    public int TotalCount => Fines.Count;
    
    /// <summary>
    /// Total amount of all fines in the report
    /// </summary>
    public decimal TotalAmount => Fines.Sum(f => f.Amount);
    
    /// <summary>
    /// Date when report was generated
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Total number of fines with pending status
    /// </summary>
    public int PendingCount => Fines.Count(f => f.Status == FineStatus.Pending);
    
    /// <summary>
    /// Total number of fines with paid status
    /// </summary>
    public int PaidCount => Fines.Count(f => f.Status == FineStatus.Paid);
    
    /// <summary>
    /// Total number of fines with waived status
    /// </summary>
    public int WaivedCount => Fines.Count(f => f.Status == FineStatus.Waived);
    
    /// <summary>
    /// Total amount of pending fines
    /// </summary>
    public decimal PendingAmount => Fines.Where(f => f.Status == FineStatus.Pending).Sum(f => f.Amount);
    
    /// <summary>
    /// Total amount of paid fines
    /// </summary>
    public decimal PaidAmount => Fines.Where(f => f.Status == FineStatus.Paid).Sum(f => f.Amount);
    
    /// <summary>
    /// Total amount of waived fines
    /// </summary>
    public decimal WaivedAmount => Fines.Where(f => f.Status == FineStatus.Waived).Sum(f => f.Amount);
}

/// <summary>
/// DTO for representing details of a fine in a report
/// </summary>
public class FineReportItemDto
{
    /// <summary>
    /// Fine ID
    /// </summary>
    public int FineId { get; set; }
    
    /// <summary>
    /// Member ID
    /// </summary>
    public int MemberId { get; set; }
    
    /// <summary>
    /// Member full name
    /// </summary>
    public string MemberName { get; set; } = string.Empty;
    
    /// <summary>
    /// Member email address for contact
    /// </summary>
    public string MemberEmail { get; set; } = string.Empty;
    
    /// <summary>
    /// Fine amount
    /// </summary>
    public decimal Amount { get; set; }
    
    /// <summary>
    /// Date when the fine was created
    /// </summary>
    public DateTime FineDate { get; set; }
    
    /// <summary>
    /// Current status of the fine (Pending, Paid, Waived)
    /// </summary>
    public FineStatus Status { get; set; }
    
    /// <summary>
    /// Type of fine (Overdue, Lost, Damaged)
    /// </summary>
    public FineType Type { get; set; }
    
    /// <summary>
    /// Fine description
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Associated loan ID if applicable
    /// </summary>
    public int? LoanId { get; set; }
    
    /// <summary>
    /// Associated book title if this fine is related to a loan
    /// </summary>
    public string? BookTitle { get; set; }
}

/// <summary>
/// DTO for representing member's outstanding fine balance
/// </summary>
public class OutstandingFineDto
{
    /// <summary>
    /// Member ID
    /// </summary>
    public int MemberId { get; set; }
    
    /// <summary>
    /// Member full name
    /// </summary>
    public string MemberName { get; set; } = string.Empty;
    
    /// <summary>
    /// Total outstanding fine amount
    /// </summary>
    public decimal OutstandingAmount { get; set; }
    
    /// <summary>
    /// Number of pending fines
    /// </summary>
    public int PendingFinesCount { get; set; }
    
    /// <summary>
    /// List of pending fine details
    /// </summary>
    public List<FineReportItemDto> PendingFines { get; set; } = [];
    
    /// <summary>
    /// Date when the data was calculated
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}