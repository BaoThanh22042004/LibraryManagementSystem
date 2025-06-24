using Domain.Enums;

namespace Application.DTOs;

public class FineDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime FineDate { get; set; }
    public FineStatus Status { get; set; }
    public FineType Type { get; set; }
    public int MemberId { get; set; }
    public string MemberName { get; set; } = string.Empty;
    public int? LoanId { get; set; }
}

public class CreateFineDto
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public FineType Type { get; set; }
    public int MemberId { get; set; }
    public int? LoanId { get; set; }
}

public class UpdateFineDto
{
    public FineStatus Status { get; set; }
    public string Description { get; set; } = string.Empty;
}