using DiceRoller.UserAccess.Application.Common;
using DiceRoller.UserAccess.Application.Users;
using FluentValidation.TestHelper;

namespace DiceRoller.UserAccess.UnitTests.Application.Users;

public sealed class RegisterUserRequestValidatorTests
{
    private static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];
    private static readonly byte[] Webp = [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBP"u8];

    private readonly RegisterUserRequestValidator _validator = new();

    [Fact]
    public async Task Validate_ValidRequest_HasNoErrors()
    {
        var result = await ValidateAsync(ValidRequest());

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("Ada")]
    [InlineData("Mary-Jane")]
    [InlineData("O'Brien")]
    [InlineData("Jean Luc")]
    [InlineData("Zoë")]
    public async Task Validate_AllowedNameCharacters_HasNoErrors(string name)
    {
        var result = await ValidateAsync(ValidRequest() with { FirstName = name, LastName = name });

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_MissingFirstName_HasFirstNameRequired(string? firstName)
    {
        var result = await ValidateAsync(ValidRequest() with { FirstName = firstName! });

        result.ShouldHaveValidationErrorFor(x => x.FirstName).WithErrorCode(UserValidationCodes.FirstNameRequired);
    }

    [Fact]
    public async Task Validate_FirstNameTooLong_HasFirstNameTooLong()
    {
        var result = await ValidateAsync(ValidRequest() with { FirstName = new string('a', 101) });

        result.ShouldHaveValidationErrorFor(x => x.FirstName).WithErrorCode(UserValidationCodes.FirstNameTooLong);
    }

    [Fact]
    public async Task Validate_FirstNameAtMaxLength_HasNoFirstNameError()
    {
        var result = await ValidateAsync(ValidRequest() with { FirstName = new string('a', 100) });

        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    [Theory]
    [InlineData("Ada1")]
    [InlineData("Ada!")]
    [InlineData("Ada\tLovelace")]
    public async Task Validate_FirstNameInvalidCharacters_HasFirstNameInvalidCharacters(string firstName)
    {
        var result = await ValidateAsync(ValidRequest() with { FirstName = firstName });

        result.ShouldHaveValidationErrorFor(x => x.FirstName).WithErrorCode(UserValidationCodes.FirstNameInvalidCharacters);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_MissingLastName_HasLastNameRequired(string? lastName)
    {
        var result = await ValidateAsync(ValidRequest() with { LastName = lastName! });

        result.ShouldHaveValidationErrorFor(x => x.LastName).WithErrorCode(UserValidationCodes.LastNameRequired);
    }

    [Fact]
    public async Task Validate_LastNameTooLong_HasLastNameTooLong()
    {
        var result = await ValidateAsync(ValidRequest() with { LastName = new string('a', 101) });

        result.ShouldHaveValidationErrorFor(x => x.LastName).WithErrorCode(UserValidationCodes.LastNameTooLong);
    }

    [Fact]
    public async Task Validate_LastNameInvalidCharacters_HasLastNameInvalidCharacters()
    {
        var result = await ValidateAsync(ValidRequest() with { LastName = "Lovelace_" });

        result.ShouldHaveValidationErrorFor(x => x.LastName).WithErrorCode(UserValidationCodes.LastNameInvalidCharacters);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_MissingEmail_HasEmailRequired(string? email)
    {
        var result = await ValidateAsync(ValidRequest() with { Email = email! });

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorCode(UserValidationCodes.EmailRequired);
    }

    [Fact]
    public async Task Validate_EmailTooLong_HasEmailTooLong()
    {
        var email = new string('a', 245) + "@example.com";

        var result = await ValidateAsync(ValidRequest() with { Email = email });

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorCode(UserValidationCodes.EmailTooLong);
    }

    [Theory]
    [InlineData("ada")]
    [InlineData("ada@example")]
    [InlineData("ada@@example.com")]
    [InlineData("ada lovelace@example.com")]
    public async Task Validate_EmailInvalidFormat_HasEmailInvalidFormat(string email)
    {
        var result = await ValidateAsync(ValidRequest() with { Email = email });

        result.ShouldHaveValidationErrorFor(x => x.Email).WithErrorCode(UserValidationCodes.EmailInvalidFormat);
    }

    [Fact]
    public async Task Validate_EmailWithSurroundingWhitespace_HasNoEmailError()
    {
        var result = await ValidateAsync(ValidRequest() with { Email = "  Ada@Example.com  " });

        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Validate_MissingPassword_HasPasswordRequired(string? password)
    {
        var result = await ValidateAsync(ValidRequest() with { Password = password! });

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorCode(UserValidationCodes.PasswordRequired);
    }

    [Theory]
    [InlineData(7)]
    [InlineData(129)]
    public async Task Validate_PasswordLengthOutOfRange_HasPasswordInvalidLength(int length)
    {
        var password = "Aa1" + new string('x', length - 3);

        var result = await ValidateAsync(ValidRequest() with { Password = password });

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorCode(UserValidationCodes.PasswordInvalidLength);
    }

    [Theory]
    [InlineData(8)]
    [InlineData(128)]
    public async Task Validate_PasswordLengthAtBounds_HasNoPasswordError(int length)
    {
        var password = "Aa1" + new string('x', length - 3);

        var result = await ValidateAsync(ValidRequest() with { Password = password });

        result.ShouldNotHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public async Task Validate_PasswordWithoutUppercase_HasPasswordMissingUppercase()
    {
        var result = await ValidateAsync(ValidRequest() with { Password = "secret123" });

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorCode(UserValidationCodes.PasswordMissingUppercase);
    }

    [Fact]
    public async Task Validate_PasswordWithoutLowercase_HasPasswordMissingLowercase()
    {
        var result = await ValidateAsync(ValidRequest() with { Password = "SECRET123" });

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorCode(UserValidationCodes.PasswordMissingLowercase);
    }

    [Fact]
    public async Task Validate_PasswordWithoutDigit_HasPasswordMissingDigit()
    {
        var result = await ValidateAsync(ValidRequest() with { Password = "SecretPass" });

        result.ShouldHaveValidationErrorFor(x => x.Password).WithErrorCode(UserValidationCodes.PasswordMissingDigit);
    }

    [Fact]
    public async Task Validate_MissingPhoto_HasPhotoRequired()
    {
        var result = await ValidateAsync(ValidRequest() with { Photo = null });

        result.ShouldHaveValidationErrorFor(x => x.Photo).WithErrorCode(UserValidationCodes.PhotoRequired);
    }

    [Fact]
    public async Task Validate_EmptyPhoto_HasPhotoRequired()
    {
        var result = await ValidateAsync(ValidRequest() with { Photo = Photo([], "image/png") });

        result.ShouldHaveValidationErrorFor(x => x.Photo).WithErrorCode(UserValidationCodes.PhotoRequired);
    }

    [Fact]
    public async Task Validate_PhotoTooLarge_HasPhotoTooLarge()
    {
        var photo = Photo(Png, "image/png") with { Length = RegisterUserRequestValidator.PhotoMaxBytes + 1 };

        var result = await ValidateAsync(ValidRequest() with { Photo = photo });

        result.ShouldHaveValidationErrorFor(x => x.Photo).WithErrorCode(UserValidationCodes.PhotoTooLarge);
    }

    [Fact]
    public async Task Validate_PhotoAtMaxSize_HasNoPhotoError()
    {
        var photo = Photo(Png, "image/png") with { Length = RegisterUserRequestValidator.PhotoMaxBytes };

        var result = await ValidateAsync(ValidRequest() with { Photo = photo });

        result.ShouldNotHaveValidationErrorFor(x => x.Photo);
    }

    [Theory]
    [InlineData("image/gif")]
    [InlineData("application/pdf")]
    [InlineData("")]
    public async Task Validate_UnsupportedPhotoType_HasPhotoUnsupportedType(string contentType)
    {
        var result = await ValidateAsync(ValidRequest() with { Photo = Photo(Png, contentType) });

        result.ShouldHaveValidationErrorFor(x => x.Photo).WithErrorCode(UserValidationCodes.PhotoUnsupportedType);
    }

    public static TheoryData<byte[], string> MatchingPhotos => new()
    {
        { Jpeg, "image/jpeg" },
        { Png, "image/png" },
        { Webp, "image/webp" },
        { Png, "IMAGE/PNG" },
    };

    [Theory]
    [MemberData(nameof(MatchingPhotos))]
    public async Task Validate_PhotoContentMatchesType_HasNoPhotoError(byte[] content, string contentType)
    {
        var result = await ValidateAsync(ValidRequest() with { Photo = Photo(content, contentType) });

        result.ShouldNotHaveValidationErrorFor(x => x.Photo);
    }

    public static TheoryData<byte[], string> MismatchedPhotos => new()
    {
        { Png, "image/jpeg" },
        { Jpeg, "image/png" },
        { Jpeg, "image/webp" },
        { [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WAVE"u8], "image/webp" },
        { [0xFF, 0xD8], "image/jpeg" },
        { "<svg xmlns=\"http://www.w3.org/2000/svg\"/>"u8.ToArray(), "image/png" },
    };

    [Theory]
    [MemberData(nameof(MismatchedPhotos))]
    public async Task Validate_PhotoContentDoesNotMatchType_HasPhotoContentMismatch(byte[] content, string contentType)
    {
        var result = await ValidateAsync(ValidRequest() with { Photo = Photo(content, contentType) });

        result.ShouldHaveValidationErrorFor(x => x.Photo).WithErrorCode(UserValidationCodes.PhotoContentMismatch);
    }

    [Fact]
    public async Task Validate_PhotoStreamNotSeekable_HasPhotoContentMismatch()
    {
        var photo = new FileUpload(new NonSeekableStream(Png), "photo.png", "image/png", Png.Length);

        var result = await ValidateAsync(ValidRequest() with { Photo = photo });

        result.ShouldHaveValidationErrorFor(x => x.Photo).WithErrorCode(UserValidationCodes.PhotoContentMismatch);
    }

    [Fact]
    public async Task Validate_PhotoStream_IsRewoundToItsStartingPosition()
    {
        var photo = Photo(Png, "image/png");

        await ValidateAsync(ValidRequest() with { Photo = photo });

        photo.Content.Position.ShouldBe(0);
    }

    private static RegisterUserRequest ValidRequest() =>
        new("Ada", "Lovelace", "ada@example.com", "Secret123", Photo(Png, "image/png"));

    private static FileUpload Photo(byte[] content, string contentType) =>
        new(new MemoryStream(content), "photo", contentType, content.Length);

    private Task<TestValidationResult<RegisterUserRequest>> ValidateAsync(RegisterUserRequest request) =>
        _validator.TestValidateAsync(request, cancellationToken: TestContext.Current.CancellationToken);

    private sealed class NonSeekableStream(byte[] content) : MemoryStream(content)
    {
        public override bool CanSeek => false;
    }
}
