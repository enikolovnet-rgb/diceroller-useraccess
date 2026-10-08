using DiceRoller.UserAccess.Domain.Users;
using Microsoft.AspNetCore.Identity;
using IPasswordHasher = DiceRoller.UserAccess.Application.Abstractions.IPasswordHasher;

namespace DiceRoller.UserAccess.Infrastructure.Security;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    // PasswordHasher<TUser> ignores the user argument, so none is passed.
    public string Hash(string password) => _hasher.HashPassword(null!, password);

    public bool Verify(string passwordHash, string password) =>
        _hasher.VerifyHashedPassword(null!, passwordHash, password) != PasswordVerificationResult.Failed;
}
