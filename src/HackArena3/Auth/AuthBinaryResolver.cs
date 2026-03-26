using System.Runtime.InteropServices;

namespace src.HackArena3.Auth;

internal static class AuthBinaryResolver
{
    private const string EnvHaAuthBin = "HA3_WRAPPER_HA_AUTH_BIN";
    private const string BinaryName = "ha-auth";
    private const string BinaryNameWindows = "ha-auth.exe";

    internal static string ResolveHaAuthBinary(string? haAuthBinOverride)
    {
        var candidates = new List<string?>();

        candidates.Add(haAuthBinOverride);
        candidates.Add(Environment.GetEnvironmentVariable(EnvHaAuthBin));

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var localAppData = Environment.GetEnvironmentVariable("LOCALAPPDATA");
            if (!string.IsNullOrEmpty(localAppData))
            {
                candidates.Add(Path.Combine(localAppData, "HackArena", "bin", BinaryNameWindows));
            }
        }
        else
        {
            var xdgDataHome = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            if (!string.IsNullOrEmpty(xdgDataHome))
            {
                candidates.Add(Path.Combine(xdgDataHome, "hackarena", "bin", BinaryName));
            }

            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            candidates.Add(Path.Combine(home, ".local", "share", "hackarena", "bin", BinaryName));
        }

        candidates.Add(BinaryName);

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

        throw new AuthException(
            $"Cannot find `{BinaryName}` binary. Run `hackarena install auth` or set {EnvHaAuthBin}."
        );
    }

    private static string? ResolveFromCandidate(string candidate)
    {
        if (Path.IsPathRooted(candidate) && File.Exists(candidate))
        {
            return Path.GetFullPath(candidate);
        }

        var combinedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), candidate));
        if (File.Exists(combinedPath))
        {
            return combinedPath;
        }

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