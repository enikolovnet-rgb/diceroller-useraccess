namespace DiceRoller.UserAccess.Application.Users;

public sealed record UserDto(Guid Id, string FirstName, string LastName, string Email, string PhotoUrl);
