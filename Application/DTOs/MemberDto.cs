using Domain.Enums;

namespace Application.DTOs;

public class MemberDto
{
    public int Id { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public MembershipStatus MembershipStatus { get; set; }
    public DateTime MembershipStartDate { get; set; }
    public decimal OutstandingFines { get; set; }
    public UserDto User { get; set; } = null!;
}

public class MemberDetailsDto : MemberDto
{
    public List<LoanDto> ActiveLoans { get; set; } = [];
    public List<ReservationDto> ActiveReservations { get; set; } = [];
    public List<FineDto> UnpaidFines { get; set; } = [];
}

public class CreateMemberDto
{
    public string MembershipNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
}

public class UpdateMemberDto
{
    public MembershipStatus MembershipStatus { get; set; }
    public DateTime? MembershipStartDate { get; set; }
}

public class MemberSignUpDto
{
    // User information
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    
    // Member information (optional fields that can be auto-generated if not provided)
    public string? MembershipNumber { get; set; }
}