using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Application.Tokens;
using DiceRoller.UserAccess.Domain.Users;
using Moq;

namespace DiceRoller.UserAccess.UnitTests.Application.Tokens;

public sealed class IssueTokenHandlerTests
{
    private readonly User _user = User.Register(
        PersonName.Create("Ada", "Lovelace"), Email.Create("ada@example.com"), "stored-hash", "photo.png", TimeProvider.System);

    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ITokenIssuer> _tokenIssuer = new();
    private readonly IssueTokenHandler _handler;

    public IssueTokenHandlerTests()
    {
        _users
            .Setup(r => r.FindByEmailAsync(Email.Create("ada@example.com"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_user);
        _passwordHasher.Setup(h => h.Verify("stored-hash", "Secret123")).Returns(true);

        _handler = new IssueTokenHandler(_users.Object, _passwordHasher.Object, _tokenIssuer.Object);
    }

    private static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsIssuedToken()
    {
        var token = new TokenDto("jwt", 3600);
        _tokenIssuer.Setup(i => i.Issue(_user)).Returns(token);

        var result = await _handler.Handle(new CreateTokenRequest(" ADA@example.com", "Secret123"), CancellationToken);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe(token);
        result.Value.TokenType.ShouldBe("Bearer");
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsInvalidCredentials()
    {
        var result = await _handler.Handle(new CreateTokenRequest("nobody@example.com", "Secret123"), CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.InvalidCredentials);
        _tokenIssuer.Verify(i => i.Issue(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownEmail_StillHashesPasswordToEqualizeTiming()
    {
        await _handler.Handle(new CreateTokenRequest("nobody@example.com", "Secret123"), CancellationToken);

        _passwordHasher.Verify(h => h.Hash("Secret123"), Times.Once);
    }

    [Fact]
    public async Task Handle_WrongPassword_ReturnsInvalidCredentials()
    {
        var result = await _handler.Handle(new CreateTokenRequest("ada@example.com", "Wrong123"), CancellationToken);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(UserErrors.InvalidCredentials);
        _tokenIssuer.Verify(i => i.Issue(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownEmailAndWrongPassword_ReturnIdenticalErrors()
    {
        var unknownEmail = await _handler.Handle(new CreateTokenRequest("nobody@example.com", "Secret123"), CancellationToken);
        var wrongPassword = await _handler.Handle(new CreateTokenRequest("ada@example.com", "Wrong123"), CancellationToken);

        unknownEmail.Error.ShouldBe(wrongPassword.Error);
    }
}
