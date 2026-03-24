using dotenv.net;
using Microsoft.Extensions.Configuration;
using src.HackArena3.Models;

namespace Hackarena3.Wrapper.Configuration;

public static class ConfigLoader
{
    // Definicje kluczy zmiennych środowiskowych, aby uniknąć "magicznych stringów".
    private const string EnvApiUrl = "HA3_WRAPPER_API_URL";
    private const string EnvHaAuthBin = "HA3_WRAPPER_HA_AUTH_BIN";

    /// <summary>
    /// Ładuje konfigurację ze zmiennych środowiskowych oraz pliku .env.
    /// Nie uwzględnia argumentów linii poleceń.
    /// </summary>
    /// <returns>Obiekt RuntimeConfig z wczytanymi wartościami.</returns>
    /// <exception cref="ConfigException">Rzucany, gdy brakuje wymaganych zmiennych.</exception>
    public static RuntimeConfig LoadConfigurationFromEnvironment()
    {
        DotEnv.Load(options: new DotEnvOptions(
            ignoreExceptions: true,
            envFilePaths: new[] { "user/.env" })
        );

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var apiUrl = configuration[EnvApiUrl];
        if (string.IsNullOrWhiteSpace(apiUrl))
        {
            throw new ConfigException($"Missing required runtime env: {EnvApiUrl}");
        }

        var haAuthBin = configuration[EnvHaAuthBin];

        return new RuntimeConfig
        {
            ApiAddr = apiUrl.Trim(),
            HaAuthBin = string.IsNullOrWhiteSpace(haAuthBin) ? null : haAuthBin.Trim(),
            SandboxId = null
        };
    }
}

/// <summary>
/// Wyjątek rzucany w przypadku błędów konfiguracji.
/// </summary>
public class ConfigException(string message) : Exception(message);