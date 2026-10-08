using DiceRoller.BuildingBlocks.Contracts;
using DiceRoller.UserAccess.Application.Abstractions;

namespace DiceRoller.UserAccess.Api.Security;

/// <summary>Reads the user id from the <c>sub</c> claim of the validated access token.</summary>
public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public Guid? UserId =>
        Guid.TryParse(httpContextAccessor.HttpContext?.User.FindFirst(JwtClaimNames.Subject)?.Value, out var id) ? id : null;
}
