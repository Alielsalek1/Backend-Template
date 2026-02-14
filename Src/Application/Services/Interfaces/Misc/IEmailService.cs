namespace Application.Services.Interfaces;
public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body, CancellationToken ct);
    Task SendConfirmationEmailAsync(string to, string token, CancellationToken ct);
    Task SendPasswordResetEmailAsync(string to, string token, CancellationToken ct);
}