namespace src.HackArena3.Models;

public record GameToken
{
    public required string Token { get; init; }

    public required long ExpirationEpoch { get; init; }

    public string? Kid { get; init; }
}
