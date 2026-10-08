using DiceRoller.BuildingBlocks.Domain;
using MediatR;

namespace DiceRoller.UserAccess.Application.Users;

public sealed record GetUserQuery(Guid Id) : IRequest<Result<UserDto>>;
