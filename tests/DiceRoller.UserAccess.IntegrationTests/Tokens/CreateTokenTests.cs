using System.Net;
using System.Net.Http.Json;
using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.UserAccess.Application.Tokens;
using DiceRoller.UserAccess.IntegrationTests.Infrastructure;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DiceRoller.UserAccess.IntegrationTests.Tokens;

public sealed class CreateTokenTests(UserAccessApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateToken_ValidCredentials_Returns200WithExpectedClaims()
    {
        var email = UserAccessApi.UniqueEmail();
        var id = await UserAccessApi.RegisterUserAsync(_client, email);

        using var response = await UserAccessApi.CreateTokenAsync(_client, email, UserAccessApi.ValidPassword);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var token = await response.Content.ReadFromJsonAsync<TokenDto>(UserAccessApi.Ct);
        token.ShouldNotBeNull();
        token.TokenType.ShouldBe("Bearer");
        token.ExpiresIn.ShouldBe(3600);

        var result = await new JsonWebTokenHandler().ValidateTokenAsync(token.AccessToken, TestJwt.ValidationParameters);
        result.Exception.ShouldBeNull();
        result.Claims[JwtClaimNames.Subject].ShouldBe(id.ToString());
        result.Claims[JwtClaimNames.Email].ShouldBe(email);
        result.Claims[JwtClaimNames.GivenName].ShouldBe("Ada");
        result.Claims[JwtClaimNames.FamilyName].ShouldBe("Lovelace");
        result.Claims.ShouldContainKey(JwtClaimNames.JwtId);
        result.Claims.ShouldContainKey(JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public async Task CreateToken_WrongPassword_Returns401InvalidCredentials()
    {
        var email = UserAccessApi.UniqueEmail();
        await UserAccessApi.RegisterUserAsync(_client, email);

        using var response = await UserAccessApi.CreateTokenAsync(_client, email, "Wrong1234");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "User.InvalidCredentials");
    }

    [Fact]
    public async Task CreateToken_UnknownEmail_ReturnsSameErrorAsWrongPassword()
    {
        var email = UserAccessApi.UniqueEmail();
        await UserAccessApi.RegisterUserAsync(_client, email);

        using var wrongPassword = await UserAccessApi.CreateTokenAsync(_client, email, "Wrong1234");
        using var unknownEmail = await UserAccessApi.CreateTokenAsync(_client, UserAccessApi.UniqueEmail(), UserAccessApi.ValidPassword);

        var expected = await wrongPassword.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "User.InvalidCredentials");
        var actual = await unknownEmail.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "User.InvalidCredentials");
        actual.GetProperty("title").GetString().ShouldBe(expected.GetProperty("title").GetString());
        actual.GetProperty("detail").GetString().ShouldBe(expected.GetProperty("detail").GetString());
    }
}
