using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace DiceRoller.UserAccess.IntegrationTests.Infrastructure;

/// <summary>Requests against the service, with valid defaults that a test overrides where it matters.</summary>
public static class UserAccessApi
{
    public const string UsersPath = "/api/v1/users";
    public const string TokensPath = "/api/v1/tokens";
    public const string ValidPassword = "Secret123";

    public static CancellationToken Ct => TestContext.Current.CancellationToken;

    public static string UniqueEmail() => $"user-{Guid.NewGuid():N}@example.com";

    public static Task<HttpResponseMessage> RegisterAsync(
        HttpClient client,
        string? email = null,
        string password = ValidPassword,
        byte[]? photo = null,
        string photoContentType = "image/png")
    {
        var photoContent = new ByteArrayContent(photo ?? TestPhotos.Png);
        photoContent.Headers.ContentType = new MediaTypeHeaderValue(photoContentType);

        var form = new MultipartFormDataContent
        {
            { new StringContent("Ada"), "firstName" },
            { new StringContent("Lovelace"), "lastName" },
            { new StringContent(email ?? UniqueEmail()), "email" },
            { new StringContent(password), "password" },
            { photoContent, "photo", "photo.png" },
        };

        return client.PostAsync(UsersPath, form, Ct);
    }

    /// <summary>Registers a user and returns its id, read from the <c>Location</c> header.</summary>
    public static async Task<Guid> RegisterUserAsync(HttpClient client, string email)
    {
        using var response = await RegisterAsync(client, email);
        response.EnsureSuccessStatusCode();
        return Guid.Parse(response.Headers.Location!.OriginalString.Split('/')[^1]);
    }

    public static Task<HttpResponseMessage> CreateTokenAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync(TokensPath, new { email, password }, Ct);

    public static async Task<HttpResponseMessage> GetUserAsync(HttpClient client, Guid id, string? accessToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{UsersPath}/{id}");
        if (accessToken is not null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        return await client.SendAsync(request, Ct);
    }
}
