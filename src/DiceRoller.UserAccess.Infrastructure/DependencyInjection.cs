using DiceRoller.BuildingBlocks.Web;
using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Infrastructure.Persistence;
using DiceRoller.UserAccess.Infrastructure.Photos;
using DiceRoller.UserAccess.Infrastructure.Security;
using DiceRoller.UserAccess.Infrastructure.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DiceRoller.UserAccess.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString(DatabaseOptions.ConnectionStringName);

        services.AddOptions<DatabaseOptions>()
            .Configure(o => o.ConnectionString = connectionString ?? string.Empty)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddDbContext<UserAccessDbContext>(o => o.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));
        services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<UserAccessDbContext>());
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddSingleton<IPasswordHasher, AspNetPasswordHasher>();

        services.AddOptions<PhotoStorageOptions>()
            .Bind(configuration.GetSection(PhotoStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddSingleton<IPhotoStorage, LocalPhotoStorage>();

        services.AddSingleton<ITokenIssuer, DevJwtTokenIssuer>();

        services.AddHealthChecks()
            .AddCheck<UserAccessDbHealthCheck>("database", tags: [ServiceDefaultsExtensions.ReadyTag]);

        return services;
    }
}
