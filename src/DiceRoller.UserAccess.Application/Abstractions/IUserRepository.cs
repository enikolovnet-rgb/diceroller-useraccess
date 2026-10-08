using DiceRoller.UserAccess.Domain.Users;

namespace DiceRoller.UserAccess.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(Email email, CancellationToken cancellationToken);

    Task<User?> FindByEmailAsync(Email email, CancellationToken cancellationToken);

    Task<User?> FindByIdAsync(Guid id, CancellationToken cancellationToken);

    void Add(User user);
}
