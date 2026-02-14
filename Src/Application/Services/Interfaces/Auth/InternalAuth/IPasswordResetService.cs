using Application.DTOs.Auth;
using Application.Utils;
using Domain.Shared;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Services.Interfaces;

public interface IPasswordResetService
{
    Task<Result<SuccessApiResponse>> ForgetPasswordAsync(ForgetPasswordRequestDto forgetPasswordRequest, CancellationToken cancellationToken);
    Task<Result<SuccessApiResponse>> ResetPasswordAsync(ResetPasswordRequestDto resetPasswordRequest, CancellationToken cancellationToken);
}
