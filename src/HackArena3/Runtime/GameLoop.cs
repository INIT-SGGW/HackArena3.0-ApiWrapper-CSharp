 using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using src.HackArena3.Grpc;
using src.HackArena3.Interfaces;
using src.HackArena3.Models;
using src.HackArena3.Services;
using ProtoRace = HA3.Proto.Race.V1;

namespace src.HackArena3.Runtime;

internal class GameLoop
{
    private readonly IBot _bot;
    private readonly GrpcChannel? _providedChannel;
    private readonly string? _backendTarget;
    private readonly GameTokenProvider? _tokenProvider;

    public GameLoop(IBot bot, string backendTarget, GameTokenProvider tokenProvider)
    {
        _bot = bot;
        _backendTarget = backendTarget;
        _tokenProvider = tokenProvider;
    }

    public GameLoop(IBot bot, GrpcChannel providedChannel)
    {
        _bot = bot;
        _providedChannel = providedChannel;
    }

    public async Task RunSandboxAsync(DiscoveredSandbox sandbox, CancellationToken cancellationToken)
    {
        using var channel = GrpcChannelFactory.CreateInsecureBackendChannel(_backendTarget!);
        var raceParticipantClient = new ProtoRace.RaceParticipantService.RaceParticipantServiceClient(channel);
        var trackClient = new ProtoRace.TrackService.TrackServiceClient(channel);

        var joinResponse = await raceParticipantClient.LocalSandboxJoinAsync(
            new ProtoRace.LocalSandboxJoinRequest { SandboxId = sandbox.SandboxId },
            CreateSandboxMetadata(),
            cancellationToken: cancellationToken);

        var trackData = await trackClient.GetTrackDataAsync(
            new ProtoRace.GetTrackDataRequest { MapId = joinResponse.MapId },
            CreateSandboxMetadata(),
            cancellationToken: cancellationToken);

        await RunParticipantLoopAsync(
            channel,
            (int)joinResponse.CarId,
            ProtoConverter.ToTrackLayout(trackData.Track),
            CreateSandboxMetadata,
            allowTokenRefresh: true,
            cancellationToken: cancellationToken);
    }

    public async Task RunOfficialAsync(
        ProtoRace.PrepareOfficialJoinResponse prepareResponse,
        ProtoRace.TrackData trackData,
        Metadata staticMetadata,
        CancellationToken cancellationToken)
    {
        await RunParticipantLoopAsync(
            _providedChannel!,
            (int)prepareResponse.CarId,
            ProtoConverter.ToTrackLayout(trackData),
            () => staticMetadata,
            allowTokenRefresh: false,
            cancellationToken: cancellationToken);
    }
    private async Task RunParticipantLoopAsync(
        GrpcChannel channel,
        int carId,
        TrackLayout trackLayout,
        Func<Metadata> metadataProvider,
        bool allowTokenRefresh,
        CancellationToken cancellationToken)
    {
        var participantClient = new ProtoRace.RaceParticipantService.RaceParticipantServiceClient(channel);

        var outboundChannel = Channel.CreateUnbounded<ProtoRace.ParticipantClientMessage>();

        var context = new BotContext { 
            CarId = carId, 
            MapId = trackLayout.MapId, 
            Track = trackLayout, 
            EffectiveHz = 30, 
            RequestedHz = 30, 
            Tick = 0, 
            CarDimension = new CarDimension(123,123) 
        };

        var realActions = new BotContextActions(
            SetControls: controls =>
            {
                var message = MessageBuilder.ControlsMessage(controls);
                outboundChannel.Writer.TryWrite(message);
            },
            RequestBackToTrack: () =>
            {
                var message = MessageBuilder.BackToTrack();
                outboundChannel.Writer.TryWrite(message);
            },
            RequestEmergencyPitstop: () =>
            {
                var message = MessageBuilder.EmergencyPitstop();
                outboundChannel.Writer.TryWrite(message);
            },
            SetNextPitTireType: tireType =>
            {
                var message = MessageBuilder.SetNextPitTireTypeMessage(tireType);
                outboundChannel.Writer.TryWrite(message);
            }
        );

        context.BindActions(realActions);

        using var call = participantClient.Stream(metadataProvider(), cancellationToken: cancellationToken);

        var readerTask = Task.Run(async () =>
        {
            await foreach (var message in call.ResponseStream.ReadAllAsync(cancellationToken))
            {
                if (message.PayloadCase == ProtoRace.ParticipantServerEvent.PayloadOneofCase.Snapshot)
                {
                    var snapshot = ProtoConverter.ToRaceSnapshot(message.Snapshot);
                    try
                    {
                        _bot.OnTick(snapshot, context);
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"[ha3-wrapper] Bot OnTick failed: {ex.Message}");
                    }
                }
            }
        }, cancellationToken);

        var writerTask = Task.Run(async () =>
        {
            await call.RequestStream.WriteAsync(new ProtoRace.ParticipantClientMessage
            {
                Init = new ProtoRace.ParticipantStreamInit()
            }, cancellationToken);

            await foreach (var message in outboundChannel.Reader.ReadAllAsync(cancellationToken))
            {
                await call.RequestStream.WriteAsync(message, cancellationToken);
            }
        }, cancellationToken);

        var tokenRefreshTask = Task.Run(async () =>
        {
            if (!allowTokenRefresh) return;

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                if (await _tokenProvider!.EnsureFreshAsync())
                {
                    Console.Error.WriteLine("[ha3-wrapper] Game token rotated. Restarting stream is required.");
                }
            }
        }, cancellationToken);

        await Task.WhenAny(readerTask, writerTask, tokenRefreshTask);

        await call.RequestStream.CompleteAsync();
        outboundChannel.Writer.Complete();
    }

    private Metadata CreateSandboxMetadata()
    {
        var metadata = new Metadata();
        if (_tokenProvider?.CurrentToken != null)
        {
            metadata.Add("x-ha3-game-token", _tokenProvider.CurrentToken.Token);
            metadata.Add("cookie", $"auth_token={_tokenProvider.MemberJwt}");
        }
        return metadata;
    }
}