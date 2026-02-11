
namespace Application.DTOs.InternalAuth;
public class ConfirmEmailRequestDto
{
    public string Email { get; init; } = default!;
    public string Token { get; init; } = default!;
}