using DiceRoller.UserAccess.Application.Tokens;
using DiceRoller.UserAccess.Domain.Users;

namespace DiceRoller.UserAccess.Application.Abstractions;

public interface ITokenIssuer
{
    TokenDto Issue(User user);
}
