using Domain.Models.User;

namespace Application.Services.Interfaces;
public interface IJwtTokenProvider
{
    string GenerateAccessToken(User user);
}