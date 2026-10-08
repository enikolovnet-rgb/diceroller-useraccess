using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

[assembly: AssemblyFixture(typeof(DiceRoller.UserAccess.IntegrationTests.Infrastructure.UserAccessApiFactory))]

namespace DiceRoller.UserAccess.IntegrationTests.Infrastructure;

/// <summary>
/// The service running on <c>TestServer</c> against a real SQL Server in a container, shared by every test in the assembly.
/// Tests use unique emails, so they don't depend on each other's data.
/// </summary>
public sealed class UserAccessApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _database = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    private readonly string _photoRoot = Path.Combine(Path.GetTempPath(), "diceroller-useraccess-tests", Guid.NewGuid().ToString("N"));

    public async ValueTask InitializeAsync() => await _database.StartAsync(TestContext.Current.CancellationToken);

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        await _database.DisposeAsync();

        if (Directory.Exists(_photoRoot))
        {
            Directory.Delete(_photoRoot, recursive: true);
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Development makes Program apply the migrations at startup.
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:UserAccess", _database.GetConnectionString());
        builder.UseSetting("Jwt:Issuer", TestJwt.Issuer);
        builder.UseSetting("Jwt:Audience", TestJwt.Audience);
        builder.UseSetting("Jwt:SigningKey", TestJwt.SigningKey);
        builder.UseSetting("PhotoStorage:RootPath", _photoRoot);

        // TestServer has no remote IP, so every test shares one rate-limit partition.
        builder.UseSetting("RateLimiting:Tokens:PermitLimit", "100000");
    }
}
