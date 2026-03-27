using Grpc.Core;
using src.HackArena3.Auth;
using src.HackArena3.Grpc;
using src.HackArena3.Interfaces;
using src.HackArena3.Models;
using src.HackArena3.Services;
using ProtoRace = HA3.Proto.Race.V1;

namespace src.HackArena3.Runtime;

internal class RuntimeOrchestrator
{
    private readonly IBot _bot;

    public RuntimeOrchestrator(IBot bot)
    {
        _bot = bot;
    }

    public async Task RunSandboxModeAsync(RuntimeConfig config, CancellationToken cancellationToken)
    {
        GameTokenProvider? tokenProvider = null;
        try
        {
            var jwtProvider = new MemberJwtProvider(config.HaAuthBin);
            var memberJwt = await jwtProvider.FetchMemberJwtAsync(cancellationToken);

            var discoverer = new SandboxDiscoverer(config, memberJwt);
            var selectedSandbox = await discoverer.DiscoverAndChooseSandboxAsync(cancellationToken);

            tokenProvider = new GameTokenProvider(config.ApiAddr, memberJwt);
            await tokenProvider.RefreshAsync(cancellationToken);

            var gameLoop = new GameLoop(_bot, selectedSandbox.Backend.GrpcTarget, tokenProvider);
            await gameLoop.RunSandboxAsync(selectedSandbox, cancellationToken);
        }
        finally
        {
            if (tokenProvider != null)
            {
                await tokenProvider.DisposeAsync();
            }
        }
    }

    public async Task RunOfficialModeAsync(OfficialRuntimeConfig config, CancellationToken cancellationToken = default)
    {
        var channel = GrpcChannelFactory.CreateOfficialChannel(config.GrpcTarget, config.RpcPrefix);

        var raceClient = new ProtoRace.RaceParticipantService.RaceParticipantServiceClient(channel);
        var trackClient = new ProtoRace.TrackService.TrackServiceClient(channel);

        var metadata = new Metadata
        {
            { "x-ha3-game-token", config.TeamToken },
            { "cookie", $"auth_token={config.AuthToken}" }
        };

        Console.Error.WriteLine("[ha3-wrapper] Preparing official join...");
        var prepareResponse = await raceClient.PrepareOfficialJoinAsync(
            new ProtoRace.PrepareOfficialJoinRequest(),
            metadata,
            cancellationToken: cancellationToken);

        Console.Error.WriteLine("[ha3-wrapper] Fetching official track data...");
        var trackData = await trackClient.GetTrackDataAsync(
            new ProtoRace.GetTrackDataRequest { MapId = prepareResponse.MapId },
            metadata,
            cancellationToken: cancellationToken);

        var gameLoop = new GameLoop(_bot, channel);
        await gameLoop.RunOfficialAsync(prepareResponse, trackData.Track, metadata, cancellationToken);
    }
}