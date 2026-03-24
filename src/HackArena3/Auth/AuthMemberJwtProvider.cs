namespace src.HackArena3.Auth;

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

public class MemberJwtProvider
{
    private readonly string _haAuthBinaryPath;

    public MemberJwtProvider(string? haAuthBinOverride)
    {
        _haAuthBinaryPath = AuthBinaryResolver.ResolveHaAuthBinary(haAuthBinOverride);
    }

    private static string GetLoginHint(string binaryPath)
    {
        var binaryName = Path.GetFileName(binaryPath);
        return $"Run `hackarena auth login` or `{binaryName} login`.";
    }

    /// <summary>
    /// Uruchamia proces ha-auth w celu pobrania tokenu JWT członka zespołu.
    /// </summary>
    /// <returns>Pobrany token JWT.</returns>
    /// <exception cref="AuthException">Rzucany w przypadku błędów wykonania lub parsowania.</exception>
    public async Task<string> FetchMemberJwtAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = _haAuthBinaryPath,
            Arguments = "token -q",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex)
        {
            throw new AuthException($"Failed to run `{_haAuthBinaryPath}`: {ex.Message}", ex);
        }

        // Asynchronicznie odczytaj strumienie wyjścia i błędu
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        await process.WaitForExitAsync();

        var stdout = (await stdoutTask).Trim();
        var stderr = (await stderrTask).Trim();
        var exitCode = process.ExitCode;

        if (exitCode == 2)
        {
            throw new AuthException($"Auth login required. {GetLoginHint(_haAuthBinaryPath)}");
        }

        if (exitCode != 0)
        {
            var details = !string.IsNullOrEmpty(stderr) ? $" stderr: {stderr}" : "";
            throw new AuthException(
                $"Auth token retrieval failed with exit code {exitCode}. " +
                $"{GetLoginHint(_haAuthBinaryPath)} Check auth CLI diagnostics.{details}"
            );
        }

        if (string.IsNullOrEmpty(stdout))
        {
            throw new AuthException($"`{_haAuthBinaryPath} token -q` returned empty stdout.");
        }

        try
        {
            var payload = JsonNode.Parse(stdout);
            var token = payload?["token"]?.GetValue<string>();

            if (string.IsNullOrWhiteSpace(token))
            {
                throw new AuthException("Auth token response is missing `token` field.");
            }

            return token.Trim();
        }
        catch (JsonException ex)
        {
            throw new AuthException($"`{_haAuthBinaryPath} token -q` did not return valid JSON.", ex);
        }
    }
}
