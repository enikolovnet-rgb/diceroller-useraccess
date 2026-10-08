using FluentValidation;

namespace DiceRoller.UserAccess.Application.Tokens;

public sealed class CreateTokenRequestValidator : AbstractValidator<CreateTokenRequest>
{
    public CreateTokenRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithErrorCode(TokenValidationCodes.EmailRequired)
            .WithMessage("Email is required.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithErrorCode(TokenValidationCodes.PasswordRequired)
            .WithMessage("Password is required.");
    }
}
