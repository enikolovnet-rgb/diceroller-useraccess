using System.Net;
using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Domain.Users;
using DiceRoller.UserAccess.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace DiceRoller.UserAccess.IntegrationTests.Errors;

public sealed class UnexpectedErrorTests(UserAccessApiFactory factory)
{
    [Fact]
    public async Task CreateToken_RepositoryThrows_Returns500WithStandardBody()
    {
        var users = new Mock<IUserRepository>();
        users.Setup(u => u.FindByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database is down."));

        using var app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services.AddScoped(_ => users.Object)));
        using var client = app.CreateClient();

        using var response = await UserAccessApi.CreateTokenAsync(client, UserAccessApi.UniqueEmail(), UserAccessApi.ValidPassword);

        await response.ShouldBeProblemAsync(HttpStatusCode.InternalServerError, "General.Unexpected");
    }
}
