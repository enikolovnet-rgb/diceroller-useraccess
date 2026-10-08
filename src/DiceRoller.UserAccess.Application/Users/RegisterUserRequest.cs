using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.UserAccess.Application.Common;
using MediatR;

namespace DiceRoller.UserAccess.Application.Users;

public sealed record RegisterUserRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    FileUpload? Photo) : IRequest<Result<UserDto>>;
