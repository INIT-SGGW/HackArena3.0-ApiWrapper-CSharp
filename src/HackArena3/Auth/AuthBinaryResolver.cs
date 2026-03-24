using System.Runtime.InteropServices;

namespace src.HackArena3.Auth;

public static class AuthBinaryResolver
{
    private const string EnvHaAuthBin = "HA3_WRAPPER_HA_AUTH_BIN";
    private const string BinaryName = "ha-auth";
    private const string BinaryNameWindows = "ha-auth.exe";

    /// <summary>
    /// Wyszukuje plik binarny 'ha-auth' w predefiniowanych lokalizacjach oraz w PATH.
    /// </summary>
    /// <param name="haAuthBinOverride">Bezpośrednia ścieżka z konfiguracji, ma najwyższy priorytet.</param>
    /// <returns>Pełna, znormalizowana ścieżka do pliku binarnego.</returns>
    /// <exception cref="AuthException">Rzucany, gdy plik binarny nie zostanie znaleziony.</exception>
    public static string ResolveHaAuthBinary(string? haAuthBinOverride)
    {
        var candidates = new List<string?>();

        // Krok 1: Dodaj kandydatów w odpowiedniej kolejności priorytetów
        candidates.Add(haAuthBinOverride);
        candidates.Add(Environment.GetEnvironmentVariable(EnvHaAuthBin));

        // Krok 2: Dodaj kandydatów specyficznych dla systemu operacyjnego
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                candidates.Add(Path.Combine(localAppData, "HackArena", "bin", BinaryNameWindows));
            }
        }
        else // Zakładamy systemy typu Unix (Linux, macOS)
        {
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (!string.IsNullOrEmpty(xdgDataHome))
            {
                candidates.Add(Path.Combine(xdgDataHome, "hackarena", "bin", BinaryName));
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            candidates.Add(Path.Combine(home, ".local", "share", "hackarena", "bin", BinaryName));
        }

        // Krok 3: Dodaj nazwę binarki, aby przeszukać PATH
        candidates.Add(BinaryName);

        // Krok 4: Przetwórz kandydatów i zwróć pierwszego pasującego
        foreach (var candidate in candidates)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                continue;
            }

            var resolvedPath = ResolveFromCandidate(candidate);
            if (resolvedPath != null)
            {
                return resolvedPath;
            }
        }

        // Krok 5: Jeśli nic nie znaleziono, rzuć wyjątek
        throw new AuthException(
            $"Cannot find `{BinaryName}` binary. Run `hackarena install auth` or set {EnvHaAuthBin}."
        );
    }

    /// <summary>
    /// Sprawdza, czy kandydat jest prawidłową ścieżką do pliku lub czy można go znaleźć w PATH.
    /// Odpowiednik _resolve_from_candidate z Pythona.
    /// </summary>
    private static string? ResolveFromCandidate(string candidate)
    {
        // Jeśli kandydat jest pełną ścieżką
        if (Path.IsPathRooted(candidate) && File.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }

        // Jeśli kandydat jest ścieżką względną
        var combinedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), candidate));
        if (File.Exists(combinedPath))
        {
            return combinedPath;
        }

        // Wyszukaj w zmiennej środowiskowej PATH (odpowiednik shutil.which)
        var pathVariable = Environment.GetEnvironmentVariable("PATH");
        if (pathVariable != null)
        {
            var paths = pathVariable.Split(Path.PathSeparator);
            var executableCandidate = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !candidate.EndsWith(".exe")
                ? $"{candidate}.exe"
                : candidate;

            foreach (var path in paths)
            {
                var fullPath = Path.Combine(path, executableCandidate);
                if (File.Exists(fullPath))
                {
                    return fullPath;
                }
            }
        }

        return null;
    }
}