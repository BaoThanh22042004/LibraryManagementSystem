using Domain.Enums;

namespace Web.ViewModels
{
    /// <summary>
    /// ViewModel for editing user information.
    /// </summary>
    public class EditUserViewModel
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string Role { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public string? MembershipStatus { get; set; }
        public string? MembershipNumber { get; set; }
        public string? StatusChangeReason { get; set; }
    }
} 