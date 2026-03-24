using System.Security.Cryptography;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using HA3.Proto.Race.V1;
using HackArena.Broker.V1;
using HackArena.Connect.V1;
using src.HackArena3.Grpc;
using src.HackArena3.Models;

namespace src.HackArena3.Services;

public class SandboxDiscoverer
{
    private const string ConnectProtocolVersion = "1";
    private const double ConnectValidateTimeoutSeconds = 2.0;
    private const int RpcTimeoutSeconds = 10;

    private readonly RuntimeConfig _config;
    private readonly string _memberJwt;

    public SandboxDiscoverer(RuntimeConfig config, string memberJwt)
    {
        _config = config;
        _memberJwt = memberJwt;
    }

    public async Task<DiscoveredSandbox> DiscoverAndChooseSandboxAsync()
    {
        Console.Error.WriteLine("[ha3-wrapper] Fetching team backends via BrokerService...");

        // Krok 1: Pobierz listę backendów z Brokera
        using var brokerChannel = GrpcChannelFactory.CreateBrokerChannel(_config.ApiAddr);
        var brokerClient = new BrokerService.BrokerServiceClient(brokerChannel);
        var backends = await FetchTeamBackendsAsync(brokerClient);

        Console.WriteLine($"FOUND {backends.Count}");

        if (backends.Count == 0)
        {
            throw new SandboxDiscoveryException("Broker returned no team backends.");
        }

        // Krok 2 i 3: Sprawdź backendy i pobierz z nich sandboxy
        var discoveredSandboxes = new List<DiscoveredSandbox>();
        foreach (var backendInfo in backends)
        {
            var reachableBackend = await ResolveReachableBackendAsync(backendInfo);
            if (reachableBackend == null)
            {
                Console.Error.WriteLine(
                    $"[ha3-wrapper] Broker backend skipped (no reachable endpoint after probe): user={backendInfo.UserId} backend_id={backendInfo.BackendId}");
                continue;
            }

            try
            {
                var sandboxes = await FetchSandboxesFromBackendAsync(reachableBackend);
                discoveredSandboxes.AddRange(sandboxes);
            }
            catch (RpcException ex)
            {
                Console.Error.WriteLine(
                    $"[ha3-wrapper] Backend skipped (GetLocalRuntimeState failed): {reachableBackend.Label}; code={ex.StatusCode}; details={ex.Status.Detail ?? "no details"}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(
                    $"[ha3-wrapper] Backend skipped (runtime fetch error): {reachableBackend.Label}; details={ex.Message}");
            }
        }

        if (discoveredSandboxes.Count == 0)
        {
            throw new SandboxDiscoveryException("No active sandboxes found in team backends.");
        }

        // Krok 4: Wybierz sandbox
        return ChooseSandbox(discoveredSandboxes, _config.SandboxId);
    }

    private async Task<List<BackendInfo>> FetchTeamBackendsAsync(BrokerService.BrokerServiceClient client)
    {
        try
        {
            var response = await client.GetTeamBackendsAsync(
                new GetTeamBackendsRequest(),
                headers: CreateAuthMetadata(),
                deadline: DateTime.UtcNow.AddSeconds(RpcTimeoutSeconds));
            return response.Backends.ToList();
        }
        catch (RpcException ex)
        {
            throw new SandboxDiscoveryException($"GetTeamBackends failed: {ex.StatusCode} {ex.Status.Detail ?? "no details"}");
        }
    }

    private async Task<BackendTarget?> ResolveReachableBackendAsync(BackendInfo backendInfo)
    {
        foreach (var endpoint in backendInfo.Endpoints)
        {
            if (string.IsNullOrWhiteSpace(endpoint.Host) || endpoint.Port <= 0)
            {
                continue;
            }

            var target = new BackendTarget
            {
                BackendId = backendInfo.BackendId,
                UserId = backendInfo.UserId,
                UserName = backendInfo.UserDisplayName,
                Host = endpoint.Host,
                Port = (int)endpoint.Port
            };

            if (await ValidateBackendConnectionAsync(target))
            {
                return target;
            }
        }
        return null;
    }

    private async Task<bool> ValidateBackendConnectionAsync(BackendTarget backend)
    {
        // Backendy są na lokalnych maszynach, więc używamy niezabezpieczonego kanału (HTTP)
        using var channel = GrpcChannelFactory.CreateInsecureBackendChannel(backend.GrpcTarget);
        var client = new ConnectService.ConnectServiceClient(channel);
        var nonce = ByteString.CopyFrom(RandomNumberGenerator.GetBytes(16));

        try
        {
            var response = await client.ValidateConnectionAsync(
                new ValidateConnectionRequest
                {
                    BackendId = backend.BackendId,
                    ProtocolVersion = ConnectProtocolVersion,
                    Nonce = nonce
                },
                headers: CreateAuthMetadata(),
                deadline: DateTime.UtcNow.AddSeconds(ConnectValidateTimeoutSeconds));

            if (response.Status != ConnectStatus.Ok)
            {
                Console.Error.WriteLine($"[ha3-wrapper] Endpoint probe rejected: {backend.Label}; status={response.Status} message='{response.Message}'");
                return false;
            }
            if (response.BackendId != backend.BackendId)
            {
                Console.Error.WriteLine($"[ha3-wrapper] Endpoint probe rejected: {backend.Label}; backend_id mismatch");
                return false;
            }
            if (!response.NonceEcho.Equals(nonce))
            {
                Console.Error.WriteLine($"[ha3-wrapper] Endpoint probe rejected: {backend.Label}; nonce echo mismatch");
                return false;
            }
            return true;
        }
        catch (RpcException ex)
        {
            Console.Error.WriteLine($"[ha3-wrapper] Endpoint probe failed: {backend.Label}; code={ex.StatusCode}; details={ex.Status.Detail ?? "no details"}");
            return false;
        }
    }

    private async Task<List<DiscoveredSandbox>> FetchSandboxesFromBackendAsync(BackendTarget backend)
    {
        using var channel = GrpcChannelFactory.CreateInsecureBackendChannel(backend.GrpcTarget);
        var client = new LocalSandboxAdminService.LocalSandboxAdminServiceClient(channel);

        var response = await client.GetLocalRuntimeStateAsync(
            new GetLocalRuntimeStateRequest(),
            headers: CreateAuthMetadata(),
            deadline: DateTime.UtcNow.AddSeconds(RpcTimeoutSeconds));

        return response.State.ActiveSandboxes.Select(s => new DiscoveredSandbox
        {
            SandboxId = s.SandboxId,
            SandboxName = s.SandboxName,
            MapId = s.MapId,
            ActivePlayerCount = (int)s.ActivePlayerCount,
            Backend = backend
        }).ToList();
    }

    private DiscoveredSandbox ChooseSandbox(List<DiscoveredSandbox> discovered, string? configuredSandboxId)
    {
        if (!string.IsNullOrWhiteSpace(configuredSandboxId))
        {
            var selected = discovered.FirstOrDefault(s => s.SandboxId == configuredSandboxId);
            if (selected == null)
            {
                var available = string.Join(", ", discovered.Select(s => s.SandboxId));
                throw new SandboxDiscoveryException(
                    $"--sandbox_id='{configuredSandboxId}' not found in active team sandboxes. Available sandbox IDs: {available}");
            }
            Console.Error.WriteLine($"[ha3-wrapper] Using sandbox selected by --sandbox_id: {selected.SandboxId} ({selected.Backend.Label})");
            return selected;
        }

        Console.WriteLine("[ha3-wrapper] Active team sandboxes (broker):");
        for (int i = 0; i < discovered.Count; i++)
        {
            var entry = discovered[i];
            Console.WriteLine(
                $"[ha3-wrapper] {i + 1}. {entry.SandboxName} | id={entry.SandboxId} " +
                $"| user={entry.Backend.UserDisplay} | map={entry.MapId} | players={entry.ActivePlayerCount} " +
                $"| endpoint={entry.Backend.Host}:{entry.Backend.Port}");
        }

        if (Console.IsInputRedirected)
        {
            var available = string.Join(", ", discovered.Select(s => s.SandboxId));
            throw new SandboxDiscoveryException(
                $"Non-interactive mode requires --sandbox_id. Available sandbox IDs: {available}");
        }

        while (true)
        {
            Console.Write($"Select sandbox [1-{discovered.Count}] (default 1): ");
            var rawInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(rawInput))
            {
                return discovered[0];
            }
            if (int.TryParse(rawInput, out int index) && index >= 1 && index <= discovered.Count)
            {
                return discovered[index - 1];
            }
            Console.WriteLine("[ha3-wrapper] Invalid selection. Try again.");
        }
    }

    private Metadata CreateAuthMetadata() => new() { { "cookie", $"auth_token={_memberJwt}" } };
}
