using Application.DTOs.InternalAuth;
using Domain.Enums;
using Domain.Models;
using Domain.Models.Users;
using Mapster;

namespace Application.Mappings;

public class RegisterRequestDtoMapping : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<RegisterRequestDto, UserCreationParams>()
            .Map(dest => dest.Username, src => src.Username)
            .Map(dest => dest.Email, src => src.Email)
            .Map(dest => dest.Role, src => Roles.User)
            .Map(dest => dest.Address, src => src.Address)
            .Map(dest => dest.PhoneNumber, src => src.PhoneNumber);
    }
}