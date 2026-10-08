using System.Text.RegularExpressions;
using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.UserAccess.Domain.Users;

/// <summary>An email address, trimmed and lower-cased.</summary>
public sealed partial record Email
{
    public const int MaxLength = 256;

    private Email(string value)
    {
        Value = value;
    }

    public string Value { get; }

    /// <exception cref="DomainException"><see cref="UserErrors.InvalidEmail"/> when the address is missing, too long or malformed.</exception>
    public static Email Create(string? value)
    {
        Guard.Against(string.IsNullOrWhiteSpace(value), UserErrors.InvalidEmail);

        var normalized = value.Trim().ToLowerInvariant();

        Guard.Against(normalized.Length > MaxLength, UserErrors.InvalidEmail);
        Guard.Against(!EmailFormat().IsMatch(normalized), UserErrors.InvalidEmail);

        return new Email(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailFormat();
}
