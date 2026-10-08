using System.ComponentModel.DataAnnotations;

namespace DiceRoller.UserAccess.Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string ConnectionStringName = "UserAccess";

    [Required(ErrorMessage = $"ConnectionStrings:{ConnectionStringName} is required.")]
    public string ConnectionString { get; set; } = string.Empty;
}
