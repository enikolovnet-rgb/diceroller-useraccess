using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Application.Common;
using DiceRoller.UserAccess.Application.Users;
using DiceRoller.UserAccess.Domain.Users;
using Moq;

namespace DiceRoller.UserAccess.UnitTests.Application.Users;

public sealed class RegisterUserHandlerTests
{
    private const string PhotoKey = "3f2a9c.png";
    private const string PhotoUrl = "/photos/3f2a9c.png";

    private static readonly DateTimeOffset Now = new(2026, 10, 8, 12, 30, 0, TimeSpan.Zero);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IPhotoStorage> _photoStorage = new();
    private readonly Mock<TimeProvider> _timeProvider = new();
    private readonly RegisterUserHandler _handler;

    public RegisterUserHandlerTests()
    {
        _passwordHasher.Setup(h => h.Hash("Secret123")).Returns("hashed");
        _photoStorage
            .Setup(s => s.SaveAsync(It.IsAny<Stream>(), "ada.png", "image/png", It.IsAny<CancellationToken>()))
            .ReturnsAsync(PhotoKey);
        _photoStorage.Setup(s => s.GetUrl(PhotoKey)).Returns(PhotoUrl);
        _timeProvider.Setup(t => t.GetUtcNow()).Returns(Now);

        _handler = new RegisterUserHandler(
            _users.Object, _unitOfWork.Object, _passwordHasher.Object, _photoStorage.Object, _timeProvider.Object);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_ValidRequest_SavesUserAndReturnsDto()
    {
        User? added = null;
        _users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(user => added = user);

        var result = await _handler.Handle(Request(), CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        added.ShouldNotBeNull();
        added.Email.Value.ShouldBe("ada@example.com");
        added.PasswordHash.ShouldBe("hashed");
        added.PhotoKey.ShouldBe(PhotoKey);
        added.CreatedAtUtc.ShouldBe(Now.UtcDateTime);
        result.Value.ShouldBe(new UserDto(added.Id, "Ada", "Lovelace", "ada@example.com", PhotoUrl));
        _unitOfWork.Verify(u => u.SaveChangesAsync(CancellationToken), Times.Once);
        _photoStorage.Verify(s => s.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidRequest_NeverStoresPlainPassword()
    {
        User? added = null;
        _users.Setup(r => r.Add(It.IsAny<User>())).Callback<User>(user => added = user);

        await _handler.Handle(Request(), CancellationToken);

        added.ShouldNotBeNull();
        added.PasswordHash.ShouldNotContain("Secret123");
    }

    [Fact]
    public async Task Handle_EmailTaken_ReturnsEmailTaken()
    {
        _users
            .Setup(r => r.EmailExistsAsync(Email.Create("ada@example.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _handler.Handle(Request(email: "  ADA@example.com "), CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe(UserErrors.EmailTaken.Code);
    }

    [Fact]
    public async Task Handle_EmailTaken_DoesNotStorePhotoOrSave()
    {
        _users.Setup(r => r.EmailExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _handler.Handle(Request(), CancellationToken);

        _photoStorage.Verify(
            s => s.SaveAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _users.Verify(r => r.Add(It.IsAny<User>()), Times.Never);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SaveFails_DeletesStoredPhotoAndRethrows()
    {
        var failure = new InvalidOperationException("database unavailable");
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ThrowsAsync(failure);

        var exception = await Should.ThrowAsync<InvalidOperationException>(
            () => _handler.Handle(Request(), CancellationToken));

        exception.ShouldBeSameAs(failure);
        _photoStorage.Verify(s => s.DeleteAsync(PhotoKey, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveCancelled_StillDeletesStoredPhoto()
    {
        _unitOfWork
            .Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Should.ThrowAsync<OperationCanceledException>(() => _handler.Handle(Request(), CancellationToken));

        _photoStorage.Verify(s => s.DeleteAsync(PhotoKey, CancellationToken.None), Times.Once);
    }

    private static RegisterUserRequest Request(string email = "ada@example.com") =>
        new("Ada", "Lovelace", email, "Secret123", new FileUpload(new MemoryStream([1, 2, 3]), "ada.png", "image/png", 3));
}
