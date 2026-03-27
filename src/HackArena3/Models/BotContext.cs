using src.HackArena3.Enums;

namespace src.HackArena3.Models;

public class BotContext {
    public required int CarId { get; init; }
    public required string MapId { get; init; }
    public required CarDimensions CarDimensions { get; init; }
    public required int RequestedHz { get; init; }
    public required TrackLayout Track { get; init; }
    public required int? EffectiveHz { get; init; }
    public int Tick { get; internal set; }
    private BotContextActions _actions;

    public BotContext()
    {
        _actions = new();
    }

    internal void BindActions(BotContextActions actions)
    {
        _actions = actions;
    }

    public void SetControls(float throttle, float brake, float steer, GearShift gearShift = GearShift.None, float brakeBalancer = 0.5f, float differentialLock = 0.0f)
    {
        _actions.SetControls(new Controls(
            throttle,
            brake,
            steer,
            gearShift,
            brakeBalancer,
            differentialLock
        ));
    }

    public void RequestBackToTrack()
    {
        _actions.RequestBackToTrack();
    }

    public void RequestEmergencyPitStop()
    {
        _actions.RequestEmergencyPitstop();
    }

    public void SetNextPitTireType(TireType tireType)
    {
        _actions.SetNextPitTireType(tireType);
    }
};
