namespace src.HackArena3.Models;

internal record BackendTarget
{
    public required string BackendId { get; init; }
    public required string UserId { get; init; }
    public string? UserName { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }

    public string GrpcTarget => $"http://{Host}:{Port}";

    public string Label => $"{UserId}/{BackendId}/{Host}:{Port}";

    public string UserDisplay => string.IsNullOrWhiteSpace(UserName) ? "-" : UserName;
}
