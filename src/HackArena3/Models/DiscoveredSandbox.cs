namespace src.HackArena3.Models;

internal record DiscoveredSandbox
{
    public required string SandboxId { get; init; }
    public required string SandboxName { get; init; }
    public required string MapId { get; init; }
    public required int ActivePlayerCount { get; init; }
    public required BackendTarget Backend { get; init; }
}
