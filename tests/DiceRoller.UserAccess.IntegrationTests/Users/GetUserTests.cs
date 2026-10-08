using System.Net;
using System.Net.Http.Json;
using DiceRoller.UserAccess.Application.Users;
using DiceRoller.UserAccess.IntegrationTests.Infrastructure;

namespace DiceRoller.UserAccess.IntegrationTests.Users;

public sealed class GetUserTests(UserAccessApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_OwnUser_Returns200()
    {
        var email = UserAccessApi.UniqueEmail();
        var id = await UserAccessApi.RegisterUserAsync(_client, email);

        using var response = await UserAccessApi.GetUserAsync(_client, id, TestJwt.Mint(id));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(UserAccessApi.Ct);
        user.ShouldNotBeNull();
        user.Id.ShouldBe(id);
        user.Email.ShouldBe(email);
    }

    [Fact]
    public async Task Get_OtherUsersId_Returns403Forbidden()
    {
        var other = await UserAccessApi.RegisterUserAsync(_client, UserAccessApi.UniqueEmail());
        var me = await UserAccessApi.RegisterUserAsync(_client, UserAccessApi.UniqueEmail());

        using var response = await UserAccessApi.GetUserAsync(_client, other, TestJwt.Mint(me));

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden, "User.Forbidden");
    }

    [Fact]
    public async Task Get_NoToken_Returns401()
    {
        var id = await UserAccessApi.RegisterUserAsync(_client, UserAccessApi.UniqueEmail());

        using var response = await UserAccessApi.GetUserAsync(_client, id, accessToken: null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized, "Auth.Unauthorized");
    }

    [Fact]
    public async Task Get_OwnIdNotInDatabase_Returns404()
    {
        var id = Guid.NewGuid();

        using var response = await UserAccessApi.GetUserAsync(_client, id, TestJwt.Mint(id));

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "User.NotFound");
    }
}
