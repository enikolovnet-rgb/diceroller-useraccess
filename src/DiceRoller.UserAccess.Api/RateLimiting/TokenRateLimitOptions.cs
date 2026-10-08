using System.ComponentModel.DataAnnotations;

namespace DiceRoller.UserAccess.Api.RateLimiting;

/// <summary>Fixed-window limit for <c>POST /api/v1/tokens</c>, applied per client IP.</summary>
public sealed class TokenRateLimitOptions
{
    public const string SectionName = "RateLimiting:Tokens";
    public const string PolicyName = "tokens";

    [Range(1, int.MaxValue)]
    public int PermitLimit { get; set; } = 10;

    [Range(1, int.MaxValue)]
    public int WindowSeconds { get; set; } = 60;
}
