using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.UserAccess.Domain.Users;
using Moq;

namespace DiceRoller.UserAccess.UnitTests.Domain;

public sealed class UserTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 30, 0, TimeSpan.Zero);

    private static readonly PersonName Name = PersonName.Create("Ada", "Lovelace");
    private static readonly Email Email = Email.Create("ada@example.com");

    private readonly TimeProvider _timeProvider = CreateTimeProvider();

    [Fact]
    public void Register_ValidArguments_SetsAllProperties()
    {
        var user = User.Register(Name, Email, "hash", "photos/key.png", _timeProvider);

        user.Name.ShouldBe(Name);
        user.Email.ShouldBe(Email);
        user.PasswordHash.ShouldBe("hash");
        user.PhotoKey.ShouldBe("photos/key.png");
        user.CreatedAtUtc.ShouldBe(Now.UtcDateTime);
        user.CreatedAtUtc.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void Register_ValidArguments_AssignsVersion7IdFromCurrentTime()
    {
        var user = User.Register(Name, Email, "hash", "photos/key.png", _timeProvider);

        user.Id.Version.ShouldBe(7);
        TimestampOf(user.Id).ShouldBe(Now);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_BlankPasswordHash_ThrowsInvalidPasswordHash(string? passwordHash)
    {
        var exception = Should.Throw<DomainException>(
            () => User.Register(Name, Email, passwordHash!, "photos/key.png", _timeProvider));

        exception.Error.Code.ShouldBe(UserErrors.InvalidPasswordHash.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Register_BlankPhotoKey_ThrowsInvalidPhotoKey(string? photoKey)
    {
        var exception = Should.Throw<DomainException>(
            () => User.Register(Name, Email, "hash", photoKey!, _timeProvider));

        exception.Error.Code.ShouldBe(UserErrors.InvalidPhotoKey.Code);
    }

    [Fact]
    public void Register_PhotoKeyTooLong_ThrowsInvalidPhotoKey()
    {
        var photoKey = new string('k', User.MaxPhotoKeyLength + 1);

        var exception = Should.Throw<DomainException>(
            () => User.Register(Name, Email, "hash", photoKey, _timeProvider));

        exception.Error.Code.ShouldBe(UserErrors.InvalidPhotoKey.Code);
    }

    [Fact]
    public void Register_PhotoKeyAtMaxLength_Succeeds()
    {
        var photoKey = new string('k', User.MaxPhotoKeyLength);

        User.Register(Name, Email, "hash", photoKey, _timeProvider).PhotoKey.ShouldBe(photoKey);
    }

    [Fact]
    public void Register_NullName_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => User.Register(null!, Email, "hash", "key", _timeProvider));
    }

    [Fact]
    public void Register_NullEmail_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => User.Register(Name, null!, "hash", "key", _timeProvider));
    }

    [Fact]
    public void Register_NullTimeProvider_ThrowsArgumentNullException()
    {
        Should.Throw<ArgumentNullException>(() => User.Register(Name, Email, "hash", "key", null!));
    }

    private static TimeProvider CreateTimeProvider()
    {
        var timeProvider = new Mock<TimeProvider>();
        timeProvider.Setup(t => t.GetUtcNow()).Returns(Now);
        return timeProvider.Object;
    }

    // A version-7 Guid starts with a 48-bit big-endian Unix timestamp in milliseconds.
    private static DateTimeOffset TimestampOf(Guid id)
    {
        Span<byte> bytes = stackalloc byte[16];
        id.TryWriteBytes(bytes, bigEndian: true, out _);

        var milliseconds = 0L;
        foreach (var b in bytes[..6])
        {
            milliseconds = (milliseconds << 8) | b;
        }

        return DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);
    }
}
