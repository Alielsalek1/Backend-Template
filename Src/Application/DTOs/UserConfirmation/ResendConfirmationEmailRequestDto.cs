namespace Application.DTOs.InternalAuth;

public class ResendConfirmationEmailRequestDto
{
    public string Email { get; init; } = default!;
}