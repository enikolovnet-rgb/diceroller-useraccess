namespace DiceRoller.UserAccess.Application.Abstractions;

public interface ICurrentUser
{
    /// <summary>The authenticated user's id, or <see langword="null"/> for an anonymous request.</summary>
    Guid? UserId { get; }
}
