using Application.DTOs.InternalAuth;
using Application.Utils;
using Domain.Shared;

namespace Application.Services.Interfaces;
public interface IUserConfirmationService
{
    Task<Result<SuccessApiResponse>> ConfirmEmailAsync(ConfirmEmailRequestDto confirmEmailRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse>> ResendConfirmationEmailAsync(ResendConfirmationEmailRequestDto resendConfirmationEmailRequest, CancellationToken cancellationToken);
}