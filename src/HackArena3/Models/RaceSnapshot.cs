using System.Collections.Immutable;
using HA3.Proto.Race.V1;

namespace src.HackArena3.Models;

public record class RaceSnapshot(int Tick, int ServerTimeMs, CarState car, ImmutableArray<int> Opponents, ParticipantSnapshot Raw);
