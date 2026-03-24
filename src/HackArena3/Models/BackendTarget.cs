namespace src.HackArena3.Models;

/// <summary>
/// Reprezentuje zweryfikowany i dostępny endpoint backendu.
/// </summary>
public record BackendTarget
{
    public required string BackendId { get; init; }
    public required string UserId { get; init; }
    public string? UserName { get; init; }
    public required string Host { get; init; }
    public required int Port { get; init; }

    /// <summary>
    /// Zwraca cel w formacie "host:port" dla klienta gRPC.
    /// </summary>
    public string GrpcTarget => $"http://{Host}:{Port}";

    /// <summary>
    /// Zwraca etykietę do logowania.
    /// </summary>
    public string Label => $"{UserId}/{BackendId}/{Host}:{Port}";

    /// <summary>
    /// Zwraca nazwę użytkownika do wyświetlenia.
    /// </summary>
    public string UserDisplay => string.IsNullOrWhiteSpace(UserName) ? "-" : UserName;
}
