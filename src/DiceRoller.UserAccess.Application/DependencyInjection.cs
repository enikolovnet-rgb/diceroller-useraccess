using DiceRoller.UserAccess.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DiceRoller.UserAccess.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<RegisterUserRequestValidator>();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RegisterUserRequestValidator>());

        services.TryAddSingleton(TimeProvider.System);

        return services;
    }
}
