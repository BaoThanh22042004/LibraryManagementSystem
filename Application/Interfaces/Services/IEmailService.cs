namespace Application.Interfaces.Services;

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string body, bool isHtml = true);
    Task<bool> SendEmailAsync(string to, string subject, string body, string? attachmentPath, bool isHtml = true);
    Task<bool> SendBulkEmailAsync(List<string> recipients, string subject, string body, bool isHtml = true);
}