using DiceRoller.UserAccess.Application.Tokens;
using FluentValidation.TestHelper;

namespace DiceRoller.UserAccess.UnitTests.Application.Tokens;

public sealed class CreateTokenRequestValidatorTests
{
    private readonly CreateTokenRequestValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateTokenRequest("ada@example.com", "Secret123"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_MissingEmail_HasEmailRequired(string? email)
    {
        var result = _validator.TestValidate(new CreateTokenRequest(email!, "Secret123"));

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorCode(TokenValidationCodes.EmailRequired);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Validate_MissingPassword_HasPasswordRequired(string? password)
    {
        var result = _validator.TestValidate(new CreateTokenRequest("ada@example.com", password!));

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorCode(TokenValidationCodes.PasswordRequired);
    }
}
