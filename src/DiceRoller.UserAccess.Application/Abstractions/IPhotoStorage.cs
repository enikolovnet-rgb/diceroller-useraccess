namespace DiceRoller.UserAccess.Application.Abstractions;

public interface IPhotoStorage
{
    /// <returns>The random key the photo was stored under.</returns>
    Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);

    Task DeleteAsync(string key, CancellationToken cancellationToken);

    string GetUrl(string key);
}
