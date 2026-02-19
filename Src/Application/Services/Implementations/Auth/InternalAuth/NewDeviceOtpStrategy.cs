using System.Text.Json;
using Application.Services.Interfaces.Auth.InternalAuth;
using Application.Services.Implementations.Misc;
using Application.DTOs.Auth.InternalAuth;

namespace Application.Services.Implementations.Auth.InternalAuth;

public class NewDeviceOtpStrategy : IOtpStrategy<NewDeviceOtpPayload>
{
    public string KeyPrefix => "new_device:";
    public TimeSpan Expiration => TimeSpan.FromMinutes(10);

}