namespace DiceRoller.UserAccess.Application.Tokens;

/// <param name="AccessToken">The signed JWT.</param>
/// <param name="ExpiresIn">Lifetime of the token in seconds.</param>
public sealed record TokenDto(string AccessToken, int ExpiresIn)
{
    public string TokenType { get; init; } = "Bearer";
}
