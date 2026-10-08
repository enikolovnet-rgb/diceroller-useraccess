using DiceRoller.UserAccess.Application.Users;
using FluentValidation.TestHelper;

namespace DiceRoller.UserAccess.UnitTests.Application.Users;

public sealed class GetUserQueryValidatorTests
{
    private readonly GetUserQueryValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_HasNoErrors()
    {
        var result = _validator.TestValidate(new GetUserQuery(Guid.CreateVersion7()));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_EmptyId_HasIdRequired()
    {
        var result = _validator.TestValidate(new GetUserQuery(Guid.Empty));

        result.ShouldHaveValidationErrorFor(x => x.Id).WithErrorCode(UserValidationCodes.IdRequired);
    }
}
