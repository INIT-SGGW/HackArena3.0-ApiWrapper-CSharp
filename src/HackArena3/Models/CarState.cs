using src.HackArena3.Enums;

namespace src.HackArena3.Models;

public record CarState(
    int CarId,
    Vec3 Position,
    Quaternion Orientation,
    float SpeedMps,
    int GearRaw,
    DriveGear Gear,
    float EngineRpm,
    int LastAppliedClientSeq,
    int PitstopZoneFlags,
    int WheelsInPitstop,
    GhostModeState GhostMode,
    int TireTypeRaw,
    TireType TireType,
    int NextPitTireTypeRaw,
    TireType NextPitTireType,
    TireWearPerWheel TireWear,
    TireTemperaturePerWheel TireTemperatureCelsius,
    TireSlipPerWheel TireSlip,
    bool PitRequestActive,
    int PitEmergencyLockRemainingMs,
    int LastPitTimeMs,
    int LastPitSourceRaw
){
    public float SpeedKmh => SpeedMps * 3.6f;
}
