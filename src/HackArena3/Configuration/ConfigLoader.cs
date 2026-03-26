using dotenv.net;
using Microsoft.Extensions.Configuration;
using src.HackArena3.Models;

namespace Hackarena3.Wrapper.Configuration;

internal static class ConfigLoader
{
    private const string EnvApiUrl = "HA3_WRAPPER_API_URL";
    private const string EnvHaAuthBin = "HA3_WRAPPER_HA_AUTH_BIN";
    private const string EnvBackendEndpoint = "HA3_WRAPPER_BACKEND_ENDPOINT";
    private const string EnvTeamToken = "HA3_WRAPPER_TEAM_TOKEN";
    private const string EnvAuthToken = "HA3_WRAPPER_AUTH_TOKEN";

    public static RuntimeConfig LoadConfigurationFromEnvironment(bool requireApiAddr = true)
    {
        DotEnv.Load(options: new DotEnvOptions(
            ignoreExceptions: true,
            envFilePaths: new[] { "user/.env" })
        );

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .Build();

        var apiUrl = configuration[EnvApiUrl];
        if (requireApiAddr && string.IsNullOrWhiteSpace(apiUrl))
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

    public static OfficialRuntimeConfig LoadOfficialConfiguration()
    {
        DotEnv.Load(options: new DotEnvOptions(ignoreExceptions: true, envFilePaths: new[] { "user/.env" }));
        var configuration = new ConfigurationBuilder().AddEnvironmentVariables().Build();

        var endpoint = configuration[EnvBackendEndpoint];
        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ConfigException($"Missing required runtime env: {EnvBackendEndpoint}");

        var teamToken = configuration[EnvTeamToken];
        if (string.IsNullOrWhiteSpace(teamToken))
            throw new ConfigException($"Missing required runtime env: {EnvTeamToken}");

        var authToken = configuration[EnvAuthToken];
        if (string.IsNullOrWhiteSpace(authToken))
            throw new ConfigException($"Missing required runtime env: {EnvAuthToken}");

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme != "https")
            throw new ConfigException($"Invalid {EnvBackendEndpoint}: expected https:// URL.");

        if (string.IsNullOrEmpty(uri.Host))
            throw new ConfigException($"Invalid {EnvBackendEndpoint}: missing host in URL '{endpoint}'.");

        var rpcPrefix = uri.AbsolutePath.TrimEnd('/');
        if (string.IsNullOrEmpty(rpcPrefix) || rpcPrefix == "/")
            throw new ConfigException($"Invalid {EnvBackendEndpoint}: non-root path prefix is required.");

        return new OfficialRuntimeConfig
        {
            GrpcTarget = uri.Host + ":" + uri.Port,
            RpcPrefix = rpcPrefix,
            TeamToken = teamToken,
            AuthToken = authToken
        };
    }
}

internal class ConfigException(string message) : Exception(message);