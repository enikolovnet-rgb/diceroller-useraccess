using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DiceRoller.UserAccess.Infrastructure.Persistence;

internal sealed class UserAccessDbHealthCheck(UserAccessDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await dbContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("The UserAccess database is unreachable.");
}
