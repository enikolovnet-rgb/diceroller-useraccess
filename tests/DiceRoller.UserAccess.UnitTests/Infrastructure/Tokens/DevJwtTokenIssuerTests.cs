using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Web;
using DiceRoller.UserAccess.Domain.Users;
using DiceRoller.UserAccess.Infrastructure.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Moq;

namespace DiceRoller.UserAccess.UnitTests.Infrastructure.Tokens;

public sealed class DevJwtTokenIssuerTests : IDisposable
{
    private readonly User _user = User.Register(
        PersonName.Create("Ada", "Lovelace"), Email.Create("ada@example.com"), "stored-hash", "photo.png", TimeProvider.System);

    // The same registration the service uses, so the token is checked against the real validation parameters.
    private readonly ServiceProvider _services = new ServiceCollection()
        .AddLogging()
        .AddJwtAuthentication(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "test-issuer",
                ["Jwt:Audience"] = "test-audience",
                ["Jwt:SigningKey"] = "unit-test-signing-key-of-at-least-32-bytes",
                ["Jwt:ExpiryMinutes"] = "60",
            })
            .Build())
        .BuildServiceProvider();

    private TokenValidationParameters ValidationParameters =>
        _services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>().Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;

    public void Dispose() => _services.Dispose();

    [Fact]
    public async Task Issue_ValidUser_TokenValidatesWithServiceValidationParameters()
    {
        var token = CreateIssuer(TimeProvider.System).Issue(_user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.AccessToken, ValidationParameters);

        result.Exception.ShouldBeNull();
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task Issue_ValidUser_ContainsExpectedClaims()
    {
        var token = CreateIssuer(TimeProvider.System).Issue(_user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.AccessToken, ValidationParameters);

        result.Claims[JwtClaimNames.Subject].ShouldBe(_user.Id.ToString());
        result.Claims[JwtClaimNames.Email].ShouldBe("ada@example.com");
        result.Claims[JwtClaimNames.GivenName].ShouldBe("Ada");
        result.Claims[JwtClaimNames.FamilyName].ShouldBe("Lovelace");
        Guid.TryParse(result.Claims[JwtClaimNames.JwtId].ToString(), out _).ShouldBeTrue();
        result.Claims.ShouldContainKey(JwtRegisteredClaimNames.Iat);
        ((JsonWebToken)result.SecurityToken).Alg.ShouldBe(SecurityAlgorithms.HmacSha256);
    }

    [Fact]
    public void Issue_ConfiguredExpiry_ReturnsExpiresInSecondsAndBearerType()
    {
        var token = CreateIssuer(TimeProvider.System).Issue(_user);

        token.ExpiresIn.ShouldBe(3600);
        token.TokenType.ShouldBe("Bearer");
    }

    [Fact]
    public async Task Issue_IssuedLongerAgoThanExpiry_FailsLifetimeValidation()
    {
        var past = new Mock<TimeProvider>();
        past.Setup(t => t.GetUtcNow()).Returns(TimeProvider.System.GetUtcNow().AddHours(-2));

        var token = CreateIssuer(past.Object).Issue(_user);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.AccessToken, ValidationParameters);

        result.IsValid.ShouldBeFalse();
        result.Exception.ShouldBeOfType<SecurityTokenExpiredException>();
    }

    private DevJwtTokenIssuer CreateIssuer(TimeProvider timeProvider) =>
        new(_services.GetRequiredService<IOptions<JwtOptions>>(), timeProvider);
}
