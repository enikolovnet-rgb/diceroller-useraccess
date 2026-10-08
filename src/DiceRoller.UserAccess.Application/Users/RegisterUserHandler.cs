using DiceRoller.BuildingBlocks.Domain;
using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Domain.Users;
using MediatR;

namespace DiceRoller.UserAccess.Application.Users;

public sealed class RegisterUserHandler(
    IUserRepository users,
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    IPhotoStorage photoStorage,
    TimeProvider timeProvider) : IRequestHandler<RegisterUserRequest, Result<UserDto>>
{
    /// <remarks>Expects a request that passed <see cref="RegisterUserRequestValidator"/>.</remarks>
    public async Task<Result<UserDto>> Handle(RegisterUserRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email = Email.Create(request.Email);
        if (await users.EmailExistsAsync(email, cancellationToken))
        {
            return UserErrors.EmailTaken;
        }

        var name = PersonName.Create(request.FirstName, request.LastName);
        var passwordHash = passwordHasher.Hash(request.Password);

        var photo = request.Photo!;
        var photoKey = await photoStorage.SaveAsync(photo.Content, photo.FileName, photo.ContentType, cancellationToken);

        try
        {
            var user = User.Register(name, email, passwordHash, photoKey, timeProvider);
            users.Add(user);
            await unitOfWork.SaveChangesAsync(cancellationToken);

            return user.ToDto(photoStorage);
        }
        catch
        {
            // Not cancellable: the orphaned photo must be removed even when the request was aborted.
            await photoStorage.DeleteAsync(photoKey, CancellationToken.None);
            throw;
        }
    }
}
