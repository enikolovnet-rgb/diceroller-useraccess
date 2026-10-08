using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Domain.Users;
using MediatR;

namespace DiceRoller.UserAccess.Application.Tokens;

public sealed class IssueTokenHandler(IUserRepository users, IPasswordHasher passwordHasher, ITokenIssuer tokenIssuer)
    : IRequestHandler<CreateTokenRequest, Result<TokenDto>>
{
    /// <remarks>Expects a request that passed <see cref="CreateTokenRequestValidator"/>.</remarks>
    public async Task<Result<TokenDto>> Handle(CreateTokenRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await users.FindByEmailAsync(Email.Create(request.Email), cancellationToken);
        if (user is null)
        {
            // Hash anyway so an unknown email takes as long as a wrong password and timing reveals nothing.
            passwordHasher.Hash(request.Password);
            return UserErrors.InvalidCredentials;
        }

        if (!passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            return UserErrors.InvalidCredentials;
        }

        return tokenIssuer.Issue(user);
    }
}
