using System.Text;
using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.BuildingBlocks.Web;
using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Application.Tokens;
using DiceRoller.UserAccess.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DiceRoller.UserAccess.Infrastructure.Tokens;

/// <summary>
/// The mock token issuer: signs an HS256 JWT with the shared key from <see cref="JwtOptions"/>.
/// It stands in for a real identity provider; there are no refresh tokens and no revocation.
/// </summary>
public sealed class DevJwtTokenIssuer(IOptions<JwtOptions> options, TimeProvider timeProvider) : ITokenIssuer
{
    private readonly JsonWebTokenHandler _handler = new();

    public TokenDto Issue(User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var jwt = options.Value;
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var lifetime = TimeSpan.FromMinutes(jwt.ExpiryMinutes);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = jwt.Issuer,
            Audience = jwt.Audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now + lifetime,
            Claims = new Dictionary<string, object>
            {
                [JwtClaimNames.Subject] = user.Id.ToString(),
                [JwtClaimNames.Email] = user.Email.Value,
                [JwtClaimNames.GivenName] = user.Name.FirstName,
                [JwtClaimNames.FamilyName] = user.Name.LastName,
                [JwtClaimNames.JwtId] = Guid.NewGuid().ToString(),
            },
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)), SecurityAlgorithms.HmacSha256),
        };

        return new TokenDto(_handler.CreateToken(descriptor), (int)lifetime.TotalSeconds);
    }
}
