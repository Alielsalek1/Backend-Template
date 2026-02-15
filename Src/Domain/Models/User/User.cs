using Domain.Constraints;
using Domain.Constraints.User;
using Domain.Enums;

namespace Domain.Models.User;

public class User
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Username { get; init; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool IsEmailVerified { get; private set; } = false;
    public Roles Role { get; private set; }
    public AuthScheme AuthScheme { get; private set; }
    public string Address { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public Guid RefreshToken { get; private set; } = Guid.NewGuid();
    public DateTime RefreshTokenExpiryTime { get; private set; } = DateTime.UtcNow.AddDays(100);
    public bool IsGuest() => Role == Roles.Guest;

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

    public User(GuestUserCreationParams guestUserCreationParams)
    {
        Role = guestUserCreationParams.Role;
        AuthScheme = guestUserCreationParams.AuthScheme;
    }

    public void UpdateAddress(string? address)
    {
        UserGuard.ValidateAddress(address);
        Address = address ?? string.Empty;
    }
    public void UpdatePhoneNumber(string? phoneNumber)
    {
        UserGuard.ValidatePhoneNumber(phoneNumber);
        PhoneNumber = phoneNumber ?? string.Empty;
    }
    public void MarkEmailAsVerified()
    {
        IsEmailVerified = true;
    }
    public void GenerateNewRefreshToken()
    {
        RefreshToken = Guid.NewGuid();
        RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(100);
    }
    public void SetGuestId(Guid guestId)
    {
        Id = guestId;
    }
    public void PromoteGuestToExternalUser(string email, string username)
    {
        if (!IsGuest())
        {
            throw new InvalidOperationException("Only guest users can be promoted");
        }

        UserGuard.ValidateEmail(email);
        UserGuard.ValidateUsername(username);

        // Use reflection to set init-only properties
        var emailProperty = typeof(User).GetProperty(nameof(Email));
        var usernameProperty = typeof(User).GetProperty(nameof(Username));
        
        emailProperty?.SetValue(this, email);
        usernameProperty?.SetValue(this, username);
        
        PasswordHash = new string('0', 60);
        Role = Roles.User;
        AuthScheme = AuthScheme.External;
        IsEmailVerified = true;
    }
}
