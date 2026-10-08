using System.Net;
using System.Text.Json;

namespace DiceRoller.UserAccess.IntegrationTests.Infrastructure;

public static class ProblemAssertions
{
    private static readonly string[] StandardProperties = ["status", "title", "detail", "errorCode", "errors", "traceId"];

    /// <summary>Asserts the response is the standard RFC 9457 error body with the given status and code, and returns it.</summary>
    public static async Task<JsonElement> ShouldBeProblemAsync(this HttpResponseMessage response, HttpStatusCode status, string errorCode)
    {
        response.StatusCode.ShouldBe(status);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync(UserAccessApi.Ct)).RootElement.Clone();

        foreach (var property in StandardProperties)
        {
            body.TryGetProperty(property, out _).ShouldBeTrue($"Error body is missing '{property}'.");
        }

        body.GetProperty("status").GetInt32().ShouldBe((int)status);
        body.GetProperty("errorCode").GetString().ShouldBe(errorCode);
        body.GetProperty("traceId").GetString().ShouldNotBeNullOrWhiteSpace();

        return body;
    }
}
