using src.HackArena3.Enums;
using src.HackArena3.Flags;
using src.HackArena3.Models;
using ProtoRace = HA3.Proto.Race.V1;

namespace src.HackArena3.Runtime;

internal static class ProtoConverter
{
    public static RaceSnapshot ToRaceSnapshot(ProtoRace.ParticipantSnapshot proto)
    {

        CarState carState = new(
            CarId: (int)proto.Self.CarId,
            Position: ToVec3(proto.Self.Kinematics.Position),
            Orientation: ToQuaternion(proto.Self.Kinematics.Orientation),
            SpeedMps: proto.Self.Telemetry.SpeedMps,
            Gear: (DriveGear)proto.Self.Telemetry.Gear,
            EngineRpm: proto.Self.Telemetry.EngineRpm,
            LastAppliedClientSeq: (int)proto.Self.Telemetry.LastAppliedClientSeq,
            PitstopZoneFlags: (PitstopZoneFlag)proto.Self.Telemetry.PitstopZoneFlags,
            WheelsInPitstop: (int)proto.Self.Telemetry.WheelsInPitstop,
            GhostMode: ToGhostModeState(proto.Self.Telemetry.GhostMode),
            TireType: (TireType)proto.Self.Telemetry.TireType,
            NextPitTireType: (TireType)proto.Self.Telemetry.NextPitTireType,
            TireWear: ToTireWearPerWheel(proto.Self.Telemetry.TireWear),
            TireTemperatureCelsius: ToTireTemperaturePerWheel(proto.Self.Telemetry.TireTemperatureCelsius),
            TireSlip: ToTireSlipPerWheel(proto.Self.Telemetry.TireSlip),
            PitRequestActive: proto.Self.Telemetry.PitRuntime.PitRequestActive,
            PitEmergencyLockRemainingMs: (int)proto.Self.Telemetry.PitRuntime.EmergencyLockRemainingMs,
            LastPitTimeMs: (int)proto.Self.Telemetry.PitRuntime.LastPitTimeMs,
            LastPitSource: (PitEntrySource)proto.Self.Telemetry.PitRuntime.LastPitSource,
            LastPitLap: (int)proto.Self.Telemetry.PitRuntime.LastPitLap,
            CommandCooldowns: ToCommandCooldownState(proto.Self.Telemetry.CommandCooldowns)
        );

        List<OpponentState> opponents = [];
        foreach (var opponent in proto.Opponents)
        {
            opponents.Add(ToOpponentState(opponent));
        }

        return new RaceSnapshot(
            Tick: (int)proto.Tick,
            ServerTimeMs: (int)proto.ServerTimeMs,
            Opponents: [.. opponents],
            Car: carState,
            Raw: proto
        );
    }

    public static TrackLayout ToTrackLayout(ProtoRace.TrackData proto)
    {
        List<CenterlinePoint> centerlinePoints = [];
        foreach(var centerlinePoint in proto.CenterlineSamples)
        {
            centerlinePoints.Add(ToCenterlinePoint(centerlinePoint));
        }

        return new TrackLayout(
            MapId: proto.MapId,
            LapLengthM: (float)proto.LapLengthM,
            Centerline: [.. centerlinePoints],
            Pitstop: ToPitstopLayout(proto.PitstopData)
        );
    }

    public static Vec3 ToVec3(ProtoRace.Vector3 proto)
    {
        return new Vec3(
            X: proto.X,
            Y: proto.Y,
            Z: proto.Z
        );
    }

    public static Quaternion ToQuaternion(ProtoRace.Quaternion proto)
    {
        return new Quaternion(
            X: proto.X,
            Y: proto.Y,
            Z: proto.Z,
            W: proto.W
        );
    }

    public static GhostModeState ToGhostModeState(ProtoRace.GhostModeState proto)
    {
        List<GhostModeBlocker> blockers = [];
        foreach(var blocker in proto.Blockers)
        {
            blockers.Add((GhostModeBlocker)blocker);
        }

        return new GhostModeState(
            CanCollideNow: proto.CanCollideNow,
            Phase: (GhostModePhase)proto.Phase,
            Blockers: [.. blockers],
            ExitDelayRemainingMs: (int)proto.ExitDelayRemainingMs
        );
    }

    public static TireWearPerWheel ToTireWearPerWheel(ProtoRace.TireWearPerWheel proto)
    {
        return new TireWearPerWheel(
            proto.FrontLeft,
            proto.FrontRight,
            proto.RearLeft,
            proto.RearRight
        );
    }

    public static TireTemperaturePerWheel ToTireTemperaturePerWheel(ProtoRace.TireTemperaturePerWheel proto)
    {
        return new TireTemperaturePerWheel(
            proto.FrontLeftCelsius,
            proto.FrontRightCelsius,
            proto.RearLeftCelsius,
            proto.RearRightCelsius
        );
    }

    public static TireSlipPerWheel ToTireSlipPerWheel(ProtoRace.TireSlipPerWheel proto)
    {
        if(proto == null)
        {
            return new TireSlipPerWheel(0, 0, 0, 0);
        }

        return new TireSlipPerWheel(
            proto.FrontLeft,
            proto.FrontRight,
            proto.RearLeft,
            proto.RearRight
        );
    }

    public static OpponentState ToOpponentState(ProtoRace.ParticipantOpponentState proto)
    {
        return new OpponentState(
            CarId: (int)proto.CarId,
            Position: ToVec3(proto.Kinematics.Position),
            Orientation: ToQuaternion(proto.Kinematics.Orientation),
            GhostMode: ToGhostModeState(proto.GhostMode)
        );
    }

    public static CenterlinePoint ToCenterlinePoint(ProtoRace.CenterlineSample proto)
    {
        List<GroundWidth> leftGrounds = [];
        foreach(var leftGround in proto.LeftGrounds)
        {
            leftGrounds.Add(ToGroundWidth(leftGround));
        }

        List<GroundWidth> rightGrounds = [];
        foreach (var rightGround in proto.RightGrounds)
        {
            rightGrounds.Add(ToGroundWidth(rightGround));
        }

        return new CenterlinePoint(
            SM: (float)proto.SM,
            Position: ToVec3(proto.Position),
            Tangent: ToVec3(proto.Tangent),
            Normal: ToVec3(proto.Normal),
            Right: ToVec3(proto.Right),
            LeftWidthM: proto.LeftWidthM,
            RightWidthM: proto.RightWidthM,
            Curvature1Pm: proto.Curvature1Pm,
            GradeRad: proto.GradeRad,
            BankRad: proto.BankRad,
            MaxLeftWidthM: proto.MaxLeftWidthM,
            MaxRightWidthM: proto.MaxRightWidthM,
            LeftGrounds: [.. leftGrounds],
            RightGrounds: [.. rightGrounds]
        );
    }

    public static GroundWidth ToGroundWidth(ProtoRace.GroundWidth proto)
    {
        return new GroundWidth(
            WidthM: proto.WidthM,
            GroundType: (GroundType)proto.GroundType
        );
    }

    public static PitstopLayout ToPitstopLayout(ProtoRace.PitstopData proto)
    {
        List<CenterlinePoint> enter = [];
        foreach(var sample in proto.EnterCenterlineSamples)
        {
            enter.Add(ToCenterlinePoint(sample));
        }

        List<CenterlinePoint> fix = [];
        foreach (var sample in proto.FixCenterlineSamples)
        {
            fix.Add(ToCenterlinePoint(sample));
        }

        List<CenterlinePoint> exit = [];
        foreach (var sample in proto.ExitCenterlineSamples)
        {
            exit.Add(ToCenterlinePoint(sample));
        }

        return new PitstopLayout(
            Enter: [.. enter],
            Fix: [.. fix],
            Exit: [.. exit],
            LengthM: proto.LengthM
        );
    }

    public static CommandCooldownState ToCommandCooldownState(ProtoRace.CommandCooldownState proto)
    {
        return new CommandCooldownState(
            BackToTrackRemainingMs: (int)proto.BackToTrackRemainingMs,
            EmergencyPitstopRemainingMs: (int)proto.EmergencyPitstopRemainingMs
        );
    }
}