using System.Threading.RateLimiting;
using DiceRoller.BuildingBlocks.Web;
using DiceRoller.UserAccess.Api.ModelBinding;
using DiceRoller.UserAccess.Api.RateLimiting;
using DiceRoller.UserAccess.Api.Security;
using DiceRoller.UserAccess.Application;
using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Infrastructure;
using DiceRoller.UserAccess.Infrastructure.Persistence;
using DiceRoller.UserAccess.Infrastructure.Photos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

builder.Services.Configure<MvcOptions>(options =>
{
    options.ModelBinderProviders.Insert(0, new FileUploadModelBinderProvider());

    // Missing fields are reported by the FluentValidation rules (with their error codes), not by MVC's implicit [Required].
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddOptions<TokenRateLimitOptions>()
    .Bind(builder.Configuration.GetSection(TokenRateLimitOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy(TokenRateLimitOptions.PolicyName, httpContext =>
    {
        var limit = httpContext.RequestServices.GetRequiredService<IOptions<TokenRateLimitOptions>>().Value;

        return RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = limit.PermitLimit,
                Window = TimeSpan.FromSeconds(limit.WindowSeconds),
                QueueLimit = 0,
            });
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("DOTNET_RUNNING_IN_CONTAINER"))
{
    await app.Services.MigrateUserAccessDatabaseAsync();
}

// Photos are public static files under unguessable keys, so <img> tags work without a bearer token.
// Registered before UseServiceDefaults so the authentication fallback policy does not apply to them.
var photoOptions = app.Services.GetRequiredService<IOptions<PhotoStorageOptions>>().Value;
var photoRoot = photoOptions.GetFullRootPath(app.Environment.ContentRootPath);
Directory.CreateDirectory(photoRoot);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(photoRoot),
    RequestPath = photoOptions.RequestPath,
    OnPrepareResponse = context => context.Context.Response.Headers.XContentTypeOptions = "nosniff",
});

app.UseServiceDefaults();
app.UseRateLimiter();

await app.RunAsync();

/// <summary>Entry point; public so integration tests can use it with <c>WebApplicationFactory</c>.</summary>
public partial class Program;
