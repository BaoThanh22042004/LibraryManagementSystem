using Domain.Enums;

namespace Application.DTOs;

/// <summary>
/// Data transfer object for member information.
/// Member management is always tied to user management. All member creation, update, and deletion is handled through user commands/handlers (UC001, UC006, UC008, UC009).
/// </summary>
public class MemberDto
{
    public int Id { get; set; }
    public string MembershipNumber { get; set; } = string.Empty;
    public MembershipStatus MembershipStatus { get; set; }
    public DateTime MembershipStartDate { get; set; }
    public decimal OutstandingFines { get; set; }
    public UserDto User { get; set; } = null!;
}

/// <summary>
/// Extended member information for detailed views, including active loans, reservations, and unpaid fines.
/// Member management is always tied to user management. All member creation, update, and deletion is handled through user commands/handlers.
/// Used in UC007 (View User Info), UC021 (View Loan History), UC025 (View Reservations), UC029 (View Fine History).
/// </summary>
public class MemberDetailsDto : MemberDto
{
    public List<LoanDto> ActiveLoans { get; set; } = [];
    public List<ReservationDto> ActiveReservations { get; set; } = [];
    public List<FineDto> UnpaidFines { get; set; } = [];
}

/// <summary>
/// Data transfer object for creating a new member record.
/// Member creation is always performed as part of user creation (UC001, UC008).
/// </summary>
public class CreateMemberDto
{
    public string MembershipNumber { get; set; } = string.Empty;
    public int UserId { get; set; }
}

/// <summary>
/// Data transfer object for updating an existing member record.
/// Member update is always performed as part of user update (UC006).
/// </summary>
public class UpdateMemberDto
{
    public MembershipStatus MembershipStatus { get; set; }
    public DateTime? MembershipStartDate { get; set; }
}

/// <summary>
/// Data transfer object for member self-signup/registration.
/// Member registration is always performed as part of user registration (UC008).
/// </summary>
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