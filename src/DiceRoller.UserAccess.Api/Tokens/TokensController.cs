using DiceRoller.BuildingBlocks.Web;
using DiceRoller.UserAccess.Api.RateLimiting;
using DiceRoller.UserAccess.Application.Tokens;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DiceRoller.UserAccess.Api.Tokens;

[ApiController]
[Route("api/v1/tokens")]
public sealed class TokensController(ISender sender) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting(TokenRateLimitOptions.PolicyName)]
    [ProducesResponseType<TokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Create([FromBody] CreateTokenRequest request, CancellationToken cancellationToken) =>
        (await sender.Send(request, cancellationToken)).ToActionResult();
}
