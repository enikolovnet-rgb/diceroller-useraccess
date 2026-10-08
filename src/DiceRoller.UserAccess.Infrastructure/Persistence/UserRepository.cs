using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DiceRoller.UserAccess.Infrastructure.Persistence;

internal sealed class UserRepository(UserAccessDbContext dbContext) : IUserRepository
{
    public Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Users.AnyAsync(u => u.Email == email, cancellationToken);

    public Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

    public Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id, cancellationToken);

    public void Add(User user) => dbContext.Users.Add(user);
}
