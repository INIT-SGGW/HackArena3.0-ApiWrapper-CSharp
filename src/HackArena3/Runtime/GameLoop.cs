using System.Threading.Channels;
using Grpc.Core;
using Grpc.Net.Client;
using src.HackArena3.Interfaces;
using src.HackArena3.Models;
using src.HackArena3.Services;
using ProtoRace = HA3.Proto.Race.V1;

namespace src.HackArena3.Runtime;

internal class GameLoop
{
    private readonly IBot _bot;
    private readonly DiscoveredSandbox _sandbox;
    private readonly GameTokenProvider _tokenProvider;

    public GameLoop(IBot bot, DiscoveredSandbox sandbox, GameTokenProvider tokenProvider)
    {
        _bot = bot;
        _sandbox = sandbox;
        _tokenProvider = tokenProvider;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Używamy niezabezpieczonego kanału, bo łączymy się bezpośrednio z backendem
        using var channel = GrpcChannel.ForAddress(_sandbox.Backend.GrpcTarget);
        var raceClient = new ProtoRace.RaceService.RaceServiceClient(channel);
        var participantClient = new ProtoRace.RaceParticipantService.RaceParticipantServiceClient(channel);
        var trackClient = new ProtoRace.TrackService.TrackServiceClient(channel);

        // 1. Dołącz do sandboxa
        var joinResponse = await participantClient.LocalSandboxJoinAsync(
            new ProtoRace.LocalSandboxJoinRequest { SandboxId = _sandbox.SandboxId },
            CreateMetadata(),
            deadline: DateTime.UtcNow.AddSeconds(10),
            cancellationToken: cancellationToken);

        Console.WriteLine($"[ha3-wrapper] Joined sandbox. Car ID: {joinResponse.CarId}, Map ID: {joinResponse.MapId}");

        // 2. Pobierz dane toru
        var trackData = await trackClient.GetTrackDataAsync(
            new ProtoRace.GetTrackDataRequest { MapId = joinResponse.MapId },
            CreateMetadata(),
            deadline: DateTime.UtcNow.AddSeconds(10),
            cancellationToken: cancellationToken);
        var trackLayout = ProtoConverter.ToTrackLayout(trackData.Track);
        Console.WriteLine($"[ha3-wrapper] Loaded track data for map: {trackLayout.MapId}");

        // 3. Uruchom główną pętlę komunikacji
        await RunParticipantLoopAsync(participantClient, (int)joinResponse.CarId, trackLayout, cancellationToken);
    }

    private async Task RunParticipantLoopAsync(
        ProtoRace.RaceParticipantService.RaceParticipantServiceClient client,
        int carId,
        TrackLayout trackLayout,
        CancellationToken cancellationToken)
    {
        // Kanał do wysyłania poleceń od bota do pętli piszącej
        var outboundChannel = Channel.CreateUnbounded<ProtoRace.ParticipantClientMessage>();

        //var context = new BotContext(controls =>
        //{
        //    // Ta akcja jest wywoływana przez bota w `ctx.SetControls(...)`
        //    var message = new ProtoRace.ParticipantClientMessage
        //    {
        //        Controls = new ProtoRace.ParticipantControlsInput { /* ... mapowanie pól ... */ }
        //    };
        //    outboundChannel.Writer.TryWrite(message);
        //})
        //{
        //    CarId = carId,
        //    Track = trackLayout
        //};

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

        using var call = client.Stream(CreateMetadata(), cancellationToken: cancellationToken);

        // Uruchom trzy współbieżne zadania: czytanie, pisanie i odświeżanie tokenu
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
                        // Można tu zaimplementować logikę zatrzymania pętli
                    }
                }
            }
        }, cancellationToken);

        var writerTask = Task.Run(async () =>
        {
            // Wiadomość inicjalizacyjna
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
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(15), cancellationToken);
                if (await _tokenProvider.EnsureFreshAsync())
                {
                    Console.Error.WriteLine("[ha3-wrapper] Game token rotated. Restarting stream is required.");
                    // W pełnej implementacji należałoby zasygnalizować potrzebę restartu pętli
                }
            }
        }, cancellationToken);

        // Czekaj na zakończenie któregokolwiek z zadań (co zwykle oznacza błąd lub koniec)
        await Task.WhenAny(readerTask, writerTask, tokenRefreshTask);

        // Zakończ wysyłanie i poczekaj na zamknięcie strumienia
        await call.RequestStream.CompleteAsync();
        outboundChannel.Writer.Complete();
    }

    private Metadata CreateMetadata()
    {
        var metadata = new Metadata();
        if (_tokenProvider.CurrentToken != null)
        {
            metadata.Add("x-ha3-game-token", _tokenProvider.CurrentToken.Token);
        }
        // Dodaj też 'cookie' z memberJwt, jeśli jest wymagane
        return metadata;
    }
}