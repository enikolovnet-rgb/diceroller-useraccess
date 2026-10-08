using System.Net;
using System.Text.Json;
using DiceRoller.UserAccess.IntegrationTests.Infrastructure;

namespace DiceRoller.UserAccess.IntegrationTests.OpenApi;

public sealed class OpenApiDocumentTests(UserAccessApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetOpenApiDocument_RegisterUser_DescribesPhotoAsBinaryMultipartField()
    {
        using var response = await _client.GetAsync("/openapi/v1.json", UserAccessApi.Ct);
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(UserAccessApi.Ct));

        var content = document.RootElement
            .GetProperty("paths").GetProperty(UserAccessApi.UsersPath).GetProperty("post")
            .GetProperty("requestBody").GetProperty("content");

        content.EnumerateObject().Select(mediaType => mediaType.Name).ShouldBe(["multipart/form-data"]);
        var properties = content.GetProperty("multipart/form-data").GetProperty("schema").GetProperty("properties");
        properties.GetProperty("Photo").GetProperty("type").GetString().ShouldBe("string");
        properties.GetProperty("Photo").GetProperty("format").GetString().ShouldBe("binary");
        properties.EnumerateObject().ShouldNotContain(property => property.Name.StartsWith("Photo.", StringComparison.Ordinal));
    }
}
