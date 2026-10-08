using DiceRoller.BuildingBlocks.Domain;

namespace DiceRoller.UserAccess.Domain.Users;

public sealed class User : Entity<Guid>
{
    public const int MaxPhotoKeyLength = 256;

    private User(Guid id, PersonName name, Email email, string passwordHash, string photoKey, DateTime createdAtUtc)
        : base(id)
    {
        Name = name;
        Email = email;
        PasswordHash = passwordHash;
        PhotoKey = photoKey;
        CreatedAtUtc = createdAtUtc;
    }

    private User()
    {
        Name = null!;
        Email = null!;
        PasswordHash = null!;
        PhotoKey = null!;
    }

    public PersonName Name { get; private set; }

    public Email Email { get; private set; }

    public string PasswordHash { get; private set; }

    public string PhotoKey { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    /// <exception cref="DomainException">The password hash is blank, or the photo key is blank or too long.</exception>
    public static User Register(PersonName name, Email email, string passwordHash, string photoKey, TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(email);
        ArgumentNullException.ThrowIfNull(timeProvider);

        Guard.Against(string.IsNullOrWhiteSpace(passwordHash), UserErrors.InvalidPasswordHash);
        Guard.Against(string.IsNullOrWhiteSpace(photoKey) || photoKey.Length > MaxPhotoKeyLength, UserErrors.InvalidPhotoKey);

        var now = timeProvider.GetUtcNow();

        return new User(Guid.CreateVersion7(now), name, email, passwordHash, photoKey, now.UtcDateTime);
    }
}
