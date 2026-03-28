using src.HackArena3.Enums;

namespace src.HackArena3.Models;

public record BotContextActions(
    Action<Controls> SetControls,
    Action RequestBackToTrack,
    Action RequestEmergencyPitstop,
    Action<TireType> SetNextPitTireType
)
{
    public BotContextActions() : this(
        UnboundSetControls,
        UnboundCommand,
        UnboundCommand,
        UnboundSetNextPitTireType
    )
    { }

    private static void UnboundCommand() => throw new InvalidOperationException("Action not bound");
    private static void UnboundSetControls(Controls c) => throw new InvalidOperationException("Action not bound");
    private static void UnboundSetNextPitTireType(TireType t) => throw new InvalidOperationException("Action not bound");
}