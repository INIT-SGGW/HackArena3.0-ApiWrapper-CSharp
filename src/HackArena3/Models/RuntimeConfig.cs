namespace src.HackArena3.Models;

public record RuntimeConfig
{
    public required string ApiAddr { get; init; }

    public string? HaAuthBin { get; init; }

    public string? SandboxId { get; init; }
}