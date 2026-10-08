namespace DiceRoller.UserAccess.Application.Users;

public static class UserValidationCodes
{
    public const string IdRequired = "User.IdRequired";

    public const string FirstNameRequired = "User.FirstNameRequired";
    public const string FirstNameTooLong = "User.FirstNameTooLong";
    public const string FirstNameInvalidCharacters = "User.FirstNameInvalidCharacters";

    public const string LastNameRequired = "User.LastNameRequired";
    public const string LastNameTooLong = "User.LastNameTooLong";
    public const string LastNameInvalidCharacters = "User.LastNameInvalidCharacters";

    public const string EmailRequired = "User.EmailRequired";
    public const string EmailTooLong = "User.EmailTooLong";
    public const string EmailInvalidFormat = "User.EmailInvalidFormat";

    public const string PasswordRequired = "User.PasswordRequired";
    public const string PasswordInvalidLength = "User.PasswordInvalidLength";
    public const string PasswordMissingUppercase = "User.PasswordMissingUppercase";
    public const string PasswordMissingLowercase = "User.PasswordMissingLowercase";
    public const string PasswordMissingDigit = "User.PasswordMissingDigit";

    public const string PhotoRequired = "User.PhotoRequired";
    public const string PhotoTooLarge = "User.PhotoTooLarge";
    public const string PhotoUnsupportedType = "User.PhotoUnsupportedType";
    public const string PhotoContentMismatch = "User.PhotoContentMismatch";
}
