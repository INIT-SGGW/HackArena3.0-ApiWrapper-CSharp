using HA3.Proto.Race.V1;

namespace src.HackArena3.Models;

public class BotContext(int carId, string mapId, CarDimension carDimension, int requestedHz, TrackLayout track, int? effectiveHz, int tick) {
    public readonly int CarId = carId;
    public readonly string MapId = mapId;
    public readonly CarDimension CarDimension = carDimension;
    public readonly int RequestedHz = requestedHz;
    public readonly TrackLayout Track = track;
    public readonly int? EffectiveHz = effectiveHz;
    public readonly int Tick = tick;

    private BotContextActions actions = new();
};
