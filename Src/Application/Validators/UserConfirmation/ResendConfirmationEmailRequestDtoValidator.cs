using Application.DTOs.InternalAuth;
using FluentValidation;

namespace Application.Validators.UserConfirmation;

public class ResendConfirmationEmailRequestDtoValidator : AbstractValidator<ResendConfirmationEmailRequestDto>
{
    public ResendConfirmationEmailRequestDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format");
    }
}
