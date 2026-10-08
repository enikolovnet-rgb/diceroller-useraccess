using System.Text.RegularExpressions;
using DiceRoller.UserAccess.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DiceRoller.UserAccess.Infrastructure.Photos;

/// <summary>Stores photos as files in a local folder (a volume in the container).</summary>
public sealed partial class LocalPhotoStorage(IOptions<PhotoStorageOptions> options, IHostEnvironment environment) : IPhotoStorage
{
    // The extension comes from the validated content type, never from the client's file name,
    // so an upload can't be served back as HTML or script.
    private static readonly Dictionary<string, string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
    };

    private readonly string _rootPath = options.Value.GetFullRootPath(environment.ContentRootPath);
    private readonly string _requestPath = options.Value.RequestPath;

    public async Task<string> SaveAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!Extensions.TryGetValue(contentType, out var extension))
        {
            throw new ArgumentException($"Unsupported photo content type '{contentType}'.", nameof(contentType));
        }

        Directory.CreateDirectory(_rootPath);

        var key = $"{Guid.NewGuid():N}{extension}";
        var path = PathFor(key);

        try
        {
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, bufferSize: 81920, useAsync: true);
            await content.CopyToAsync(file, cancellationToken);
        }
        catch
        {
            File.Delete(path);
            throw;
        }

        return key;
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        File.Delete(PathFor(key));
        return Task.CompletedTask;
    }

    public string GetUrl(string key) => $"{_requestPath}/{key}";

    // Only keys this class generated are accepted, so a key can never point outside the root folder.
    private string PathFor(string key)
    {
        if (string.IsNullOrEmpty(key) || !KeyFormat().IsMatch(key))
        {
            throw new ArgumentException("Invalid photo key.", nameof(key));
        }

        return Path.Combine(_rootPath, key);
    }

    [GeneratedRegex(@"^[0-9a-f]{32}\.(jpg|png|webp)$", RegexOptions.CultureInvariant)]
    private static partial Regex KeyFormat();
}
