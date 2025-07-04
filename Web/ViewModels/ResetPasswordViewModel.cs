namespace Web.ViewModels
{
    /// <summary>
    /// ViewModel for resetting password.
    /// </summary>
    public class ResetPasswordViewModel
    {
        public required string Email { get; set; }
        public required string Token { get; set; }
        public required string NewPassword { get; set; }
        public required string ConfirmPassword { get; set; }
    }
} 