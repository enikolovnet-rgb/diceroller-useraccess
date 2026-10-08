using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.UserAccess.Domain.Users;

/// <summary>A person's first and last name, each trimmed.</summary>
public sealed record PersonName
{
    public const int MaxLength = 100;

    private PersonName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public string FirstName { get; }

    public string LastName { get; }

    /// <exception cref="DomainException"><see cref="UserErrors.InvalidName"/> when either part is empty or too long.</exception>
    public static PersonName Create(string? firstName, string? lastName) =>
        new(Normalize(firstName), Normalize(lastName));

    private static string Normalize(string? part)
    {
        var trimmed = part?.Trim() ?? string.Empty;

        Guard.Against(trimmed.Length is 0 or > MaxLength, UserErrors.InvalidName);

        return trimmed;
    }
}
