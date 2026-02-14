namespace Application.DTOs.ExternalAuth;

public record GoogleAuthResponseDto
{
    public Guid UserId { get; init; }
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
}