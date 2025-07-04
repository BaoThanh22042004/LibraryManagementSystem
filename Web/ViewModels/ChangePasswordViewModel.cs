namespace Web.ViewModels
{
    /// <summary>
    /// ViewModel for changing password.
    /// </summary>
    public class ChangePasswordViewModel
    {
        public required string CurrentPassword { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
} 