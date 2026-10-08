using FluentValidation;

namespace DiceRoller.UserAccess.Application.Users;

public sealed class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(UserValidationCodes.IdRequired)
            .WithMessage("User id is required.");
    }
}
