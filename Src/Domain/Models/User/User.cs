using Domain.Constraints;
using Domain.Constraints.User;
using Domain.Enums;

namespace Domain.Models.User;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public bool IsEmailVerified { get; set; } = false;
    public Roles Role { get; private set; }
    public AuthScheme AuthScheme { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public string PhoneNumber { get; private set; } = string.Empty;
    public Guid RefreshToken { get; set; } = Guid.NewGuid();
    public DateTime RefreshTokenExpiryTime { get; set; }    

    // for EF Core
    private User() { }

    public User(UserCreationParams userCreationParams)
    {
        UserGuard.ValidateUsername(userCreationParams.Username);
        UserGuard.ValidatePasswordHash(userCreationParams.PasswordHash);
        UserGuard.ValidateEmail(userCreationParams.Email);
        UserGuard.ValidatePhoneNumber(userCreationParams.PhoneNumber);
        UserGuard.ValidateAddress(userCreationParams.Address);

        Username = userCreationParams.Username;
        PasswordHash = userCreationParams.PasswordHash;
        Email = userCreationParams.Email;
        Role = userCreationParams.Role;
        AuthScheme = userCreationParams.AuthScheme;
        Address = userCreationParams.Address ?? string.Empty;
        PhoneNumber = userCreationParams.PhoneNumber ?? string.Empty;
    }

    public void UpdateProfile(string? address, string? phoneNumber)
    {
        if (address is not null)
        {
            UserGuard.ValidateAddress(address);
            Address = address;
        }

        if (phoneNumber is not null)
        {
            UserGuard.ValidatePhoneNumber(phoneNumber);
            PhoneNumber = phoneNumber;
        }
    }
}
