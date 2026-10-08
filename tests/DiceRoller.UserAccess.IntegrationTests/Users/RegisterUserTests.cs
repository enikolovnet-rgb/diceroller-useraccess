using System.Net;
using System.Net.Http.Json;
using DiceRoller.UserAccess.Application.Users;
using DiceRoller.UserAccess.IntegrationTests.Infrastructure;

namespace DiceRoller.UserAccess.IntegrationTests.Users;

public sealed class RegisterUserTests(UserAccessApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Register_ValidRequest_Returns201WithLocation()
    {
        var email = UserAccessApi.UniqueEmail();

        using var response = await UserAccessApi.RegisterAsync(_client, email);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(UserAccessApi.Ct);
        user.ShouldNotBeNull();
        response.Headers.Location.ShouldNotBeNull();
        response.Headers.Location.AbsolutePath.ShouldBe($"{UserAccessApi.UsersPath}/{user.Id}");
        user.Email.ShouldBe(email);
        user.FirstName.ShouldBe("Ada");
        user.LastName.ShouldBe("Lovelace");
    }

    [Fact]
    public async Task Register_ValidRequest_PhotoUrlServesThePhoto()
    {
        using var response = await UserAccessApi.RegisterAsync(_client);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(UserAccessApi.Ct);

        using var photo = await _client.GetAsync(user!.PhotoUrl, UserAccessApi.Ct);

        photo.StatusCode.ShouldBe(HttpStatusCode.OK);
        photo.Content.Headers.ContentType?.MediaType.ShouldBe("image/png");
        (await photo.Content.ReadAsByteArrayAsync(UserAccessApi.Ct)).ShouldBe(TestPhotos.Png);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns409EmailTaken()
    {
        var email = UserAccessApi.UniqueEmail();
        await UserAccessApi.RegisterUserAsync(_client, email);

        // Emails are normalized to lower case, so a differently cased address is the same user.
        using var response = await UserAccessApi.RegisterAsync(_client, email.ToUpperInvariant());

        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "User.EmailTaken");
    }

    [Theory]
    [InlineData("not-an-email", UserAccessApi.ValidPassword, "png", "image/png", "Email")]
    [InlineData(null, "Ab1", "png", "image/png", "Password")]
    [InlineData(null, UserAccessApi.ValidPassword, "gif", "image/gif", "Photo")]
    [InlineData(null, UserAccessApi.ValidPassword, "html", "image/png", "Photo")]
    public async Task Register_InvalidInput_Returns400WithFieldErrors(
        string? email, string password, string photo, string photoContentType, string expectedField)
    {
        var photoBytes = photo switch
        {
            "gif" => TestPhotos.Gif,
            "html" => TestPhotos.Html,
            _ => TestPhotos.Png,
        };

        using var response = await UserAccessApi.RegisterAsync(_client, email, password, photoBytes, photoContentType);

        var body = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Request.Invalid");
        var errors = body.GetProperty("errors");
        errors.TryGetProperty(expectedField, out var messages).ShouldBeTrue($"No errors for '{expectedField}': {errors}");
        messages.GetArrayLength().ShouldBeGreaterThan(0);
    }
}
