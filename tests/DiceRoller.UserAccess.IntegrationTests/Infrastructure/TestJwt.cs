using System.Text;
using DiceRoller.BuildingBlocks.Contracts;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DiceRoller.UserAccess.IntegrationTests.Infrastructure;

/// <summary>The shared test signing key, issuer and audience, and a way to mint tokens with them.</summary>
public static class TestJwt
{
    public const string Issuer = "diceroller-tests";
    public const string Audience = "diceroller-tests";
    public const string SigningKey = "integration-test-signing-key-of-at-least-32-bytes";

    public static SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(SigningKey));

    public static TokenValidationParameters ValidationParameters => new()
    {
        ValidIssuer = Issuer,
        ValidAudience = Audience,
        IssuerSigningKey = Key,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
    };

    public static string Mint(Guid userId)
    {
        var now = DateTime.UtcNow;

        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = Issuer,
            Audience = Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(5),
            Claims = new Dictionary<string, object>
            {
                [JwtClaimNames.Subject] = userId.ToString(),
                [JwtClaimNames.JwtId] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(Key, SecurityAlgorithms.HmacSha256),
        });
    }
}
