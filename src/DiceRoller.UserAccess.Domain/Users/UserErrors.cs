using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.UserAccess.Domain.Users;

public static class UserErrors
{
    public static readonly Error EmailTaken =
        Error.Conflict("User.EmailTaken", "A user with this email already exists.");

    public static readonly Error InvalidCredentials =
        Error.Unauthorized("User.InvalidCredentials", "The email or password is incorrect.");

    public static readonly Error NotFound =
        Error.NotFound("User.NotFound", "The user was not found.");

    public static readonly Error Forbidden =
        Error.Forbidden("User.Forbidden", "You are not allowed to access this user.");

    public static readonly Error InvalidEmail =
        Error.Validation("User.InvalidEmail", $"Email must be a valid address of at most {Email.MaxLength} characters.");

    public static readonly Error InvalidName =
        Error.Validation("User.InvalidName", $"First and last name must each be 1 to {PersonName.MaxLength} characters.");

    public static readonly Error InvalidPasswordHash =
        Error.Validation("User.InvalidPasswordHash", "Password hash is required.");

    public static readonly Error InvalidPhotoKey =
        Error.Validation("User.InvalidPhotoKey", $"Photo key is required and must be at most {User.MaxPhotoKeyLength} characters.");
}
