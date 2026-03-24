using System.Collections.Immutable;

namespace src.HackArena3.Models;

public record PitstopLayout(
    ImmutableArray<CenterlinePoint> Enter,
    ImmutableArray<CenterlinePoint> Fix,
    ImmutableArray<CenterlinePoint> Exit,
    float LengthM
);