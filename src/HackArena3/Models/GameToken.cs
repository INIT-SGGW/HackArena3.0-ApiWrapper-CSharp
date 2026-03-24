namespace src.HackArena3.Models;

/// <summary>
/// Reprezentuje token gry uzyskany z serwera API.
/// </summary>
public record GameToken
{
    /// <summary>
    /// Wartość tokenu JWT.
    /// </summary>
    public required string Token { get; init; }

    /// <summary>
    /// Czas wygaśnięcia tokenu w formacie Unix epoch (sekundy).
    /// </summary>
    public required long ExpirationEpoch { get; init; }

    /// <summary>
    /// Opcjonalne ID klucza (Key ID).
    /// </summary>
    public string? Kid { get; init; }
}
