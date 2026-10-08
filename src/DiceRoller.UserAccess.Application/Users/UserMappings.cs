using DiceRoller.UserAccess.Application.Abstractions;
using DiceRoller.UserAccess.Domain.Users;

namespace DiceRoller.UserAccess.Application.Users;

internal static class UserMappings
{
    public static UserDto ToDto(this User user, IPhotoStorage photoStorage) =>
        new(user.Id, user.Name.FirstName, user.Name.LastName, user.Email.Value, photoStorage.GetUrl(user.PhotoKey));
}
