using ProtoRace = HA3.Proto.Race.V1;

namespace src.HackArena3.Enums;

public enum PitEntrySource
{
    Unspecified = ProtoRace.PitEntrySource.Unspecified,
    BotDecision = ProtoRace.PitEntrySource.BotDecision,
    Requested = ProtoRace.PitEntrySource.Requested,
    Emergency = ProtoRace.PitEntrySource.Emergency
}
