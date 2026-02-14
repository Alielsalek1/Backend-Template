using Application.DTOs.Auth;
using Domain.Enums;
using Domain.Models;
using Domain.Models.User;
using Mapster;

namespace Application.Mappings;

public class RegisterRequestDtoMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterRequestDto, UserCreationParams>()
            .Map(dest => (string)dest.Username, src => src.Username)
            .Map(dest => (string)dest.Email, src => src.Email)
            .Map(dest => (Roles)dest.Role, src => Roles.User)
            .Map<string, string>(dest => (string)dest.Address, src => src.Address)
            .Map<string, string>(dest => (string)dest.PhoneNumber, src => src.PhoneNumber);
    }
}