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