using DiceRoller.BuildingBlocks.Web;
using DiceRoller.UserAccess.Application.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DiceRoller.UserAccess.Api.Users;

[ApiController]
[Route("api/v1/users")]
public sealed class UsersController(ISender sender) : ControllerBase
{
    private const string GetUserRoute = "GetUser";

    // Leaves headroom over the 2 MB photo limit for the other form fields and multipart framing.
    private const int MaxRequestBytes = 3 * 1024 * 1024;

    [HttpPost]
    [AllowAnonymous]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict, "application/problem+json")]
    public async Task<IActionResult> Register([FromForm] RegisterUserRequest request, CancellationToken cancellationToken)
    {
        var result = await sender.Send(request, cancellationToken);

        return result.ToCreatedResult(GetUserRoute, new { id = result.IsSuccess ? result.Value.Id : Guid.Empty });
    }

    [HttpGet("{id:guid}", Name = GetUserRoute)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden, "application/problem+json")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, "application/problem+json")]
    public async Task<IActionResult> Get([FromRoute] GetUserQuery query, CancellationToken cancellationToken) =>
        (await sender.Send(query, cancellationToken)).ToActionResult();
}
