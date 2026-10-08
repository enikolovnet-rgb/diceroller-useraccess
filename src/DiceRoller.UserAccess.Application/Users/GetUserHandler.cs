using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Domain.Users;
using MediatR;

namespace DiceRoller.UserAccess.Application.Users;

public sealed class GetUserHandler(IUserRepository users, IPhotoStorage photoStorage, ICurrentUser currentUser)
    : IRequestHandler<GetUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetUserQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        // Checked before the lookup so another user's id never reveals whether that user exists.
        if (currentUser.UserId != query.Id)
        {
            return UserErrors.Forbidden;
        }

        var user = await users.FindByIdAsync(query.Id, cancellationToken);
        if (user is null)
        {
            return UserErrors.NotFound;
        }

        return user.ToDto(photoStorage);
    }
}
