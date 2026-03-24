using src.HackArena3.Enums;
using src.HackArena3.Flags;

namespace src.HackArena3.Models;

public record CarState(
    int CarId,
    Vec3 Position,
    Quaternion Orientation,
    float SpeedMps,
    DriveGear Gear,
    float EngineRpm,
    int LastAppliedClientSeq,
    PitstopZoneFlag PitstopZoneFlags,
    int WheelsInPitstop,
    GhostModeState GhostMode,
    TireType TireType,
    TireType NextPitTireType,
    TireWearPerWheel TireWear,
    TireTemperaturePerWheel TireTemperatureCelsius,
    TireSlipPerWheel TireSlip,
    bool PitRequestActive,
    int PitEmergencyLockRemainingMs,
    int LastPitTimeMs,
    int LastPitSource
){
    public float SpeedKmh => SpeedMps * 3.6f;
}
