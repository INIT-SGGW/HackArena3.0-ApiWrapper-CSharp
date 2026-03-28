namespace src.HackArena3.Flags;

[Flags]
public enum PitstopZoneFlag
{
    Unspecified = HA3.Proto.Race.V1.PitEntrySource.Unspecified,
    BotDecision = HA3.Proto.Race.V1.PitEntrySource.BotDecision,
    Requested = HA3.Proto.Race.V1.PitEntrySource.Requested,
    Emergency = HA3.Proto.Race.V1.PitEntrySource.Emergency
}
