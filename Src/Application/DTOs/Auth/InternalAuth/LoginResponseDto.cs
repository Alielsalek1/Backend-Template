namespace Application.DTOs.Auth;
public record LoginResponseDto
{
    public Guid UserId { get; init; }
    public string AccessToken { get; init; } = null!;
    public string RefreshToken { get; init; } = null!;
}