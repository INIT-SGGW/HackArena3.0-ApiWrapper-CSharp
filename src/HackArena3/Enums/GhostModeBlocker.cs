namespace src.HackArena3.Enums;

public enum GhostModeBlocker
{
    Unspecified = HA3.Proto.Race.V1.GhostModeBlocker.Unspecified,
    LapsRequirementNotMet = HA3.Proto.Race.V1.GhostModeBlocker.LapsRequirementNotMet,
    ExitSpeedNotMet = HA3.Proto.Race.V1.GhostModeBlocker.ExitSpeedNotMet,
    ExitDelayRunning = HA3.Proto.Race.V1.GhostModeBlocker.ExitDelayRunning,
    VehicleOverlapActive = HA3.Proto.Race.V1.GhostModeBlocker.VehicleOverlapActive,
    OverlapExitDelayRunning = HA3.Proto.Race.V1.GhostModeBlocker.OverlapExitDelayRunning,
    InPit = HA3.Proto.Race.V1.GhostModeBlocker.InPit
}
