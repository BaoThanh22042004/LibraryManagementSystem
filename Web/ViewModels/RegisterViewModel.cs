namespace Web.ViewModels
{
    /// <summary>
    /// ViewModel for user registration.
    /// </summary>
    public class RegisterViewModel
    {
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Password { get; set; }
        public required string ConfirmPassword { get; set; }
        public required string Role { get; set; } // Member, Librarian, Admin
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? MembershipNumber { get; set; } // Optional for Member
        public string? Status { get; set; } // Membership or account status (optional)
    }
} 