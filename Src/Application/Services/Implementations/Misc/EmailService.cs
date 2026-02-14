using Application.Services.Interfaces;
using FluentEmail.Core;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class EmailService(IFluentEmail fluentEmail, ILogger<EmailService> logger) : IEmailService
{
    private readonly IFluentEmail _fluentEmail = fluentEmail;
    private readonly ILogger<EmailService> _logger = logger;

    public async Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct)
    {
        _logger.LogInformation("Sending confirmation email to {To}", to);
        var subject = "Activate Your Realm - Verification Code";
                var emailBody = $"""
            <div style="font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;">
                <div style="background-color: #4f46e5; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                    <h1 style="margin: 0; font-size: 24px; font-weight: 700;">Welcome to The Forge</h1>
                </div>
                <div style="padding: 40px; line-height: 1.8; color: #1e293b;">
                    <p style="font-size: 16px; margin-bottom: 24px;">Greetings, traveler!</p>
                    <p style="font-size: 16px; margin-bottom: 24px;">Your journey into the <strong>Backend Odyssey</strong> is about to begin. To verify your identity and unlock your realm, please use the following mystical code:</p>
                    <div style="display: block; width: fit-content; margin: 32px auto; padding: 20px 40px; background-color: #f8fafc; border: 2px dashed #4f46e5; border-radius: 12px; font-size: 32px; font-weight: 800; color: #4f46e5; letter-spacing: 8px; text-align: center; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);">
                        {token}
                    </div>
                    <p style="font-size: 14px; color: #64748b; margin-top: 32px;">This code will lose its power in <strong>10 minutes</strong>. If you did not initiate this summoning, you may safely ignore this parchment.</p>
                </div>
                <div style="font-size: 12px; color: #94a3b8; text-align: center; padding: 20px; border-top: 1px solid #f1f5f9;">
                    &copy; 2026 The Architect's Forge. Powered by .NET 9 & Distributed Wisdom.
                </div>
            </div>
            """;

        await SendEmailAsync(to, subject, emailBody, ct);
    }

    public async Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct)
    {
        var emailBody = $"""
            <div style="font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px; background-color: #ffffff;">
                <div style="background-color: #4f46e5; color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0;">
                    <h1 style="margin: 0; font-size: 24px; font-weight: 700;">Reset Your Password</h1>
                </div>
                <div style="padding: 40px; line-height: 1.8; color: #1e293b;">
                    <p style="font-size: 16px; margin-bottom: 24px;">Hello,</p>
                    <p style="font-size: 16px; margin-bottom: 24px;">We received a request to reset your password. To secure your realm, please use the following reset code:</p>
                    <div style="display: block; width: fit-content; margin: 32px auto; padding: 20px 40px; background-color: #f8fafc; border: 2px dashed #4f46e5; border-radius: 12px; font-size: 32px; font-weight: 800; color: #4f46e5; letter-spacing: 8px; text-align: center; box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);">
                        {token}
                    </div>
                    <p style="font-size: 14px; color: #64748b; margin-top: 32px;">This code will expire in <strong>10 minutes</strong>. If you did not request this password reset, please ignore this email and your password will remain unchanged.</p>
                </div>
                <div style="font-size: 12px; color: #94a3b8; text-align: center; padding: 20px; border-top: 1px solid #f1f5f9;">
                    &copy; 2026 The Architect's Forge. Powered by .NET 9 & Distributed Wisdom.
                </div>
            </div>
            """;
        var subject = "Reset Your Password - Recovery Code";
        await SendEmailAsync(to, subject, emailBody, ct);
    }

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        _logger.LogInformation("Sending email to {To} with subject: {Subject}", to, subject);
        var response = await _fluentEmail
            .To(to)
            .Subject(subject)
            .Body(body, isHtml: true)
            .SendAsync(ct);

        if (!response.Successful)
        {
            _logger.LogError("Failed to send email to {To}. Errors: {Errors}", to, string.Join(", ", response.ErrorMessages));
        }
        else
        {
            _logger.LogInformation("Email sent successfully to {To}", to);
        }
    }
}