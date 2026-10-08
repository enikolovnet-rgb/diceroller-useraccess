using System.Text.RegularExpressions;
using DiceRoller.UserAccess.Application.Common;
using DiceRoller.UserAccess.Domain.Users;
using FluentValidation;

namespace DiceRoller.UserAccess.Application.Users;

public sealed partial class RegisterUserRequestValidator : AbstractValidator<RegisterUserRequest>
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;
    public const long PhotoMaxBytes = 2 * 1024 * 1024;

    public static readonly IReadOnlyList<string> PhotoContentTypes = ["image/jpeg", "image/png", "image/webp"];

    public RegisterUserRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.FirstName)
            .NotEmpty()
                .WithErrorCode(UserValidationCodes.FirstNameRequired)
                .WithMessage("First name is required.")
            .MaximumLength(PersonName.MaxLength)
                .WithErrorCode(UserValidationCodes.FirstNameTooLong)
                .WithMessage($"First name must be at most {PersonName.MaxLength} characters.")
            .Matches(NameFormat())
                .WithErrorCode(UserValidationCodes.FirstNameInvalidCharacters)
                .WithMessage("First name may contain only letters, spaces, hyphens and apostrophes.");

        RuleFor(x => x.LastName)
            .NotEmpty()
                .WithErrorCode(UserValidationCodes.LastNameRequired)
                .WithMessage("Last name is required.")
            .MaximumLength(PersonName.MaxLength)
                .WithErrorCode(UserValidationCodes.LastNameTooLong)
                .WithMessage($"Last name must be at most {PersonName.MaxLength} characters.")
            .Matches(NameFormat())
                .WithErrorCode(UserValidationCodes.LastNameInvalidCharacters)
                .WithMessage("Last name may contain only letters, spaces, hyphens and apostrophes.");

        RuleFor(x => x.Email)
            .NotEmpty()
                .WithErrorCode(UserValidationCodes.EmailRequired)
                .WithMessage("Email is required.")
            .MaximumLength(Email.MaxLength)
                .WithErrorCode(UserValidationCodes.EmailTooLong)
                .WithMessage($"Email must be at most {Email.MaxLength} characters.")
            .Must(email => EmailFormat().IsMatch(email.Trim()))
                .WithErrorCode(UserValidationCodes.EmailInvalidFormat)
                .WithMessage("Email must be a valid email address.");

        RuleFor(x => x.Password)
            .NotEmpty()
                .WithErrorCode(UserValidationCodes.PasswordRequired)
                .WithMessage("Password is required.")
            .Length(PasswordMinLength, PasswordMaxLength)
                .WithErrorCode(UserValidationCodes.PasswordInvalidLength)
                .WithMessage($"Password must be {PasswordMinLength} to {PasswordMaxLength} characters.")
            .Matches(@"\p{Lu}")
                .WithErrorCode(UserValidationCodes.PasswordMissingUppercase)
                .WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"\p{Ll}")
                .WithErrorCode(UserValidationCodes.PasswordMissingLowercase)
                .WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d")
                .WithErrorCode(UserValidationCodes.PasswordMissingDigit)
                .WithMessage("Password must contain at least one digit.");

        RuleFor(x => x.Photo)
            .Must(photo => photo is { Length: > 0 })
                .WithErrorCode(UserValidationCodes.PhotoRequired)
                .WithMessage("Photo is required.")
            .Must(photo => photo!.Length <= PhotoMaxBytes)
                .WithErrorCode(UserValidationCodes.PhotoTooLarge)
                .WithMessage($"Photo must be at most {PhotoMaxBytes / (1024 * 1024)} MB.")
            .Must(photo => PhotoContentTypes.Contains(photo!.ContentType, StringComparer.OrdinalIgnoreCase))
                .WithErrorCode(UserValidationCodes.PhotoUnsupportedType)
                .WithMessage("Photo must be a JPEG, PNG or WebP image.")
            .MustAsync((photo, cancellationToken) => HasMatchingSignatureAsync(photo!, cancellationToken))
                .WithErrorCode(UserValidationCodes.PhotoContentMismatch)
                .WithMessage("Photo content does not match its declared type.");
    }

    private static ReadOnlySpan<byte> JpegSignature => [0xFF, 0xD8, 0xFF];

    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    private static ReadOnlySpan<byte> RiffSignature => "RIFF"u8;

    private static ReadOnlySpan<byte> WebpSignature => "WEBP"u8;

    // Reads the first bytes and rewinds, so the stream can still be stored afterwards.
    private static async Task<bool> HasMatchingSignatureAsync(FileUpload photo, CancellationToken cancellationToken)
    {
        if (!photo.Content.CanSeek)
        {
            return false;
        }

        var header = new byte[12];
        var start = photo.Content.Position;
        var read = await photo.Content.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false, cancellationToken);
        photo.Content.Position = start;

        return MatchesContentType(header.AsSpan(0, read), photo.ContentType);
    }

    private static bool MatchesContentType(ReadOnlySpan<byte> header, string contentType) =>
        contentType.ToLowerInvariant() switch
        {
            "image/jpeg" => header.StartsWith(JpegSignature),
            "image/png" => header.StartsWith(PngSignature),
            "image/webp" => header.Length >= 12 && header.StartsWith(RiffSignature) && header[8..12].SequenceEqual(WebpSignature),
            _ => false,
        };

    [GeneratedRegex(@"^[\p{L}\p{M} '’-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex NameFormat();

    // Same format the Email value object enforces, so a valid request never trips the domain guard.
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailFormat();
}
