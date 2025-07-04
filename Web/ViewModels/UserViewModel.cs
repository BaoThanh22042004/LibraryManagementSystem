using Domain.Enums;

namespace Web.ViewModels
{
    /// <summary>
    /// ViewModel for user management.
    /// </summary>
    public class UserViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? MembershipStatus { get; set; } // Membership or account status (optional)
        public string? MembershipNumber { get; set; }
        public string? Password { get; set; } // For create/edit forms
    }
} 