using Enums = src.HackArena3.Enums;
using Flags = src.HackArena3.Flags;
using Models = src.HackArena3.Models;

namespace src.HackArena3.Models;

public record CarState(
    int CarId,
    Vec3 Position,
    Quaternion Orientation,
    float SpeedMps,
    Enums.DriveGear Gear,
    float EngineRpm,
    int LastAppliedClientSeq,
    Flags.PitstopZoneFlag PitstopZoneFlags,
    int WheelsInPitstop,
    GhostModeState GhostMode,
    Enums.TireType TireType,
    Enums.TireType NextPitTireType,
    TireWearPerWheel TireWear,
    TireTemperaturePerWheel TireTemperatureCelsius,
    TireSlipPerWheel TireSlip,
    bool PitRequestActive,
    int PitEmergencyLockRemainingMs,
    int LastPitTimeMs,
    Enums.PitEntrySource LastPitSource,
    int LastPitLap,
    Models.CommandCooldownState CommandCooldowns
)
{
    public float SpeedKmh => SpeedMps * 3.6f;
}
