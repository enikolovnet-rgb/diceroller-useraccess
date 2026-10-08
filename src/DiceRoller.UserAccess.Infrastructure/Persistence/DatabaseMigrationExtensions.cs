using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DiceRoller.UserAccess.Infrastructure.Persistence;

public static class DatabaseMigrationExtensions
{
    public static async Task MigrateUserAccessDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<UserAccessDbContext>();

        await dbContext.Database.MigrateAsync(cancellationToken);
    }
}
