using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Application.Users;
using DiceRoller.UserAccess.Domain.Users;
using Moq;

namespace DiceRoller.UserAccess.UnitTests.Application.Users;

public sealed class GetUserHandlerTests
{
    private readonly User _user = User.Register(
        PersonName.Create("Ada", "Lovelace"), Email.Create("ada@example.com"), "hash", "photo.png", TimeProvider.System);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPhotoStorage> _photoStorage = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly GetUserHandler _handler;

    public GetUserHandlerTests()
    {
        _photoStorage.Setup(s => s.GetUrl("photo.png")).Returns("/photos/photo.png");
        _handler = new GetUserHandler(_users.Object, _photoStorage.Object, _currentUser.Object);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_OwnId_ReturnsUser()
    {
        _currentUser.Setup(c => c.UserId).Returns(_user.Id);
        _users.Setup(r => r.FindByIdAsync(_user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(_user);

        var result = await _handler.Handle(new GetUserQuery(_user.Id), CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(new UserDto(_user.Id, "Ada", "Lovelace", "ada@example.com", "/photos/photo.png"));
    }

    [Fact]
    public async Task Handle_AnotherUsersId_ReturnsForbiddenWithoutLookup()
    {
        _currentUser.Setup(c => c.UserId).Returns(Guid.CreateVersion7());

        var result = await _handler.Handle(new GetUserQuery(_user.Id), CancellationToken);

        result.Error.ShouldBe(UserErrors.Forbidden);
        _users.Verify(r => r.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AnonymousCaller_ReturnsForbidden()
    {
        _currentUser.Setup(c => c.UserId).Returns((Guid?)null);

        var result = await _handler.Handle(new GetUserQuery(_user.Id), CancellationToken);

        result.Error.ShouldBe(UserErrors.Forbidden);
    }

    [Fact]
    public async Task Handle_OwnIdButUserMissing_ReturnsNotFound()
    {
        _currentUser.Setup(c => c.UserId).Returns(_user.Id);

        var result = await _handler.Handle(new GetUserQuery(_user.Id), CancellationToken);

        result.Error.ShouldBe(UserErrors.NotFound);
    }
}
