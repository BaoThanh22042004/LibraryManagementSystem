using Domain.Enums;

namespace Application.DTOs;

public class LoanDto
{
    public int Id { get; set; }
    public DateTime LoanDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public LoanStatus Status { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int BookCopyId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string CopyNumber { get; set; } = string.Empty;
    public bool IsOverdue => Status == LoanStatus.Active && DueDate < DateTime.Now;
}

public class CreateLoanDto
{
    public int MemberId { get; set; }
    public int BookCopyId { get; set; }
    public DateTime LoanDate { get; set; } = DateTime.Now;
    public DateTime DueDate { get; set; } = DateTime.Now.AddDays(14); // Default loan period: 14 days
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
}