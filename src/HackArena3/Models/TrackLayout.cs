using System.Collections.Immutable;

namespace src.HackArena3.Models;

public record TrackLayout(
    string MapId,
    float LapLengthM,
    ImmutableArray<CenterlinePoint> Centerline,
    PitstopLayout Pitstop
);
