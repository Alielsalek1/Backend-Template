using Domain.Models;

namespace Application.Services.Interfaces;
public interface ITokenProvider
{
    string GenerateAccessToken(User user);
}