using System.ComponentModel.DataAnnotations;

namespace DiceRoller.UserAccess.Infrastructure.Photos;

public sealed class PhotoStorageOptions
{
    public const string SectionName = "PhotoStorage";

    /// <summary>Folder the photos are written to; a relative path is resolved against the content root.</summary>
    [Required]
    public string RootPath { get; set; } = string.Empty;

    /// <summary>URL path the photos are served under.</summary>
    [Required]
    [RegularExpression("^/[A-Za-z0-9/_-]*[A-Za-z0-9_-]$", ErrorMessage = "RequestPath must start with '/' and must not end with '/'.")]
    public string RequestPath { get; set; } = "/photos";

    public string GetFullRootPath(string contentRootPath) => Path.GetFullPath(Path.Combine(contentRootPath, RootPath));
}
