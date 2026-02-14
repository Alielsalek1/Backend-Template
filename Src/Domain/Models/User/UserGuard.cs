using System.Text.RegularExpressions;
using Domain.Constraints.User;

namespace Domain.Constraints.User;

public static class UserGuard
{
    public static void NotNullOrEmpty(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{parameterName} is required.", parameterName);
    }

    public static void ValidateUsername(string username)
    {
        NotNullOrEmpty(username, nameof(username));
        
        if (username.Length < UserConstraints.UsernameMinLength || username.Length > UserConstraints.UsernameMaxLength)
            throw new ArgumentException(
                $"Username must be between {UserConstraints.UsernameMinLength} and {UserConstraints.UsernameMaxLength} characters.",
                nameof(username));
    }

    public static void ValidatePasswordHash(string passwordHash)
    {
        NotNullOrEmpty(passwordHash, nameof(passwordHash));
        
        if (passwordHash.Length != UserConstraints.PasswordHashLength)
            throw new ArgumentException(
                $"Password hash must be exactly {UserConstraints.PasswordHashLength} characters.",
                nameof(passwordHash));
    }

    public static void ValidateEmail(string email)
    {
        NotNullOrEmpty(email, nameof(email));

        if (email.Length > UserConstraints.EmailMaxLength)
            throw new ArgumentException(
                $"Email cannot exceed {UserConstraints.EmailMaxLength} characters.",
                nameof(email));

        var emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(email, emailPattern))
            throw new ArgumentException("Email must be a valid email address.", nameof(email));
    }

    public static void ValidatePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber))
            return;

        // E.164 international format
        var phonePattern = @"^\+?[1-9]\d{1,14}$";
        if (!Regex.IsMatch(phoneNumber, phonePattern))
            throw new ArgumentException("Phone number must be a valid international phone number format.", nameof(phoneNumber));
    }

    public static void ValidateAddress(string? address)
    {
        if (string.IsNullOrEmpty(address))
            return;

        if (address.Length < 5)
            throw new ArgumentException("Address must be at least 5 characters long.", nameof(address));

        if (address.Length > 200)
            throw new ArgumentException("Address cannot exceed 200 characters.", nameof(address));
    }
}
