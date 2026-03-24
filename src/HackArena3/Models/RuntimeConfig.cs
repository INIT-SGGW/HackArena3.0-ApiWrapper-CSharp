namespace src.HackArena3.Models;

/// <summary>
/// Przechowuje konfigurację środowiska uruchomieniowego po wczytaniu
/// i zwalidowaniu wszystkich źródeł (środowisko, .env, CLI).
/// </summary>
public record RuntimeConfig
{
    /// <summary>
    /// Główny adres URL API HackAreny. Wymagane.
    /// </summary>
    public required string ApiAddr { get; init; }

    /// <summary>
    /// Opcjonalna, bezpośrednia ścieżka do pliku binarnego ha-auth.
    /// </summary>
    public string? HaAuthBin { get; init; }

    /// <summary>
    /// ID sandboxa, do którego bot ma dołączyć. Może być nadpisane przez argument CLI.
    /// </summary>
    public string? SandboxId { get; init; }
}