using DiceRoller.BuildingBlocks.Domain;
using MediatR;

namespace DiceRoller.UserAccess.Application.Tokens;

public sealed record CreateTokenRequest(string Email, string Password) : IRequest<Result<TokenDto>>;
