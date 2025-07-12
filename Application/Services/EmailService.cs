using Application.Common;
using Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Mail;

namespace Application.Services;

/// <summary>
/// Implementation of the IEmailService interface.
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result> SendEmailAsync(string email, string subject, string message)
    {
        try
        {
            // Get email settings from configuration
            var host = _configuration["EmailSettings:Host"] ?? throw new ArgumentNullException("EmailSettings:Host is not configured");
            var port = int.TryParse(_configuration["EmailSettings:Port"], out int p) ? p : 587; // Mặc định 587 nếu không hợp lệ
            var enableSsl = bool.TryParse(_configuration["EmailSettings:EnableSsl"], out bool ssl) ? ssl : true; // Mặc định true
            var userName = _configuration["EmailSettings:UserName"] ?? throw new ArgumentNullException("EmailSettings:UserName is not configured");
            var password = _configuration["EmailSettings:Password"] ?? throw new ArgumentNullException("EmailSettings:Password is not configured");
            var from = _configuration["EmailSettings:From"] ?? userName; // Mặc định từ UserName nếu không có
            var displayName = _configuration["EmailSettings:DisplayName"] ?? "Library Management System";

            // Validate settings
            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(userName) || 
                string.IsNullOrEmpty(password) || string.IsNullOrEmpty(from))
            {
                _logger.LogError("Email settings are not properly configured.");
                return Result.Failure("Email settings are not properly configured.");
            }

            // Create message
            var mailMessage = new MailMessage
            {
                From = new MailAddress(from, displayName),
                Subject = subject,
                Body = message,
                IsBodyHtml = true
            };
            
            mailMessage.To.Add(new MailAddress(email));

            // Create client
            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(userName, password)
            };

            // Send email
            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email}", email);
            
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Email}", email);
            return Result.Failure($"Failed to send email: {ex.Message}");
        }
    }

    /// <inheritdoc/>
    public async Task<Result> SendPasswordResetEmailAsync(string email, string fullName, string resetToken)
    {
        var subject = "Password Reset Request";
        var htmlBody = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #4CAF50; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .button {{ display: inline-block; background-color: #4CAF50; color: white; padding: 10px 20px; 
                              text-decoration: none; border-radius: 4px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Password Reset Request</h2>
                    </div>
                    <div class='content'>
                        <p>Hello {WebUtility.HtmlEncode(fullName)},</p>
                        <p>We received a request to reset your password. Please click on the button below to reset your password:</p>
                        <p style='text-align: center;'>
                            <a class='button' href='{_configuration["AppSettings:PasswordResetUrl"]}?token={WebUtility.UrlEncode(resetToken)}&email={WebUtility.UrlEncode(email)}'>
                                Reset Password
                            </a>
                        </p>
                        <p>If you did not request a password reset, please ignore this email or contact support if you have concerns.</p>
                        <p>This link will expire in 1 hour.</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(email, subject, htmlBody);
    }

    /// <inheritdoc/>
    public async Task<Result> SendEmailVerificationAsync(string email, string fullName, string verificationToken)
    {
        var subject = "Email Verification";
        var htmlBody = $@"
            <html>
            <head>
                <style>
                    body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                    .header {{ background-color: #2196F3; color: white; padding: 10px; text-align: center; }}
                    .content {{ padding: 20px; border: 1px solid #ddd; }}
                    .button {{ display: inline-block; background-color: #2196F3; color: white; padding: 10px 20px; 
                              text-decoration: none; border-radius: 4px; }}
                    .footer {{ margin-top: 20px; font-size: 12px; color: #777; text-align: center; }}
                </style>
            </head>
            <body>
                <div class='container'>
                    <div class='header'>
                        <h2>Verify Your Email Address</h2>
                    </div>
                    <div class='content'>
                        <p>Hello {WebUtility.HtmlEncode(fullName)},</p>
                        <p>Please click on the button below to verify your email address:</p>
                        <p style='text-align: center;'>
                            <a class='button' href='{_configuration["AppSettings:EmailVerificationUrl"]}?token={WebUtility.UrlEncode(verificationToken)}&email={WebUtility.UrlEncode(email)}'>
                                Verify Email
                            </a>
                        </p>
                        <p>If you did not request this verification, please ignore this email.</p>
                        <p>This link will expire in 24 hours.</p>
                    </div>
                    <div class='footer'>
                        <p>This is an automated message, please do not reply to this email.</p>
                    </div>
                </div>
            </body>
            </html>";

        return await SendEmailAsync(email, subject, htmlBody);
    }
}