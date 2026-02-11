using Application.Services.Interfaces;
using FluentEmail.Core;

namespace Application.Services.Implementations;

public class EmailService(IFluentEmail fluentEmail) : IEmailService
{
private readonly IFluentEmail _fluentEmail = fluentEmail;

    public async Task SendEmailAsync(string to, string subject, string body, CancellationToken ct)
    {
        await _fluentEmail
            .To(to)
            .Subject(subject)
            .Body(body, isHtml: true)
            .SendAsync(ct);
    }
}