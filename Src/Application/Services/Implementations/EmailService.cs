using Application.Services.Interfaces;
using FluentEmail.Core;
using Microsoft.Extensions.Logging;

namespace Application.Services.Implementations;

public class EmailService(IFluentEmail fluentEmail, ILogger<EmailService> logger) : IEmailService
{
    private readonly IFluentEmail _fluentEmail = fluentEmail;
    private readonly ILogger<EmailService> _logger = logger;

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