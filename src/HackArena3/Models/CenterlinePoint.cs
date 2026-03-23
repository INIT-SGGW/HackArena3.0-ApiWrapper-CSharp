using System.Collections.Immutable;

namespace src.HackArena3.Models;

public record CenterlinePoint(
    float DistanceFromStartM,
    Vec3 Position,
    Vec3 Tangent,
    Vec3 Normal,
    Vec3 Right,
    float LeftWidthM,
    float RightWidthM,
    float Curvature1Pm,
    float GradeRad,
    float BankRad,
    float MaxLeftWidthM,
    float MaxRightWidthM,
    ImmutableArray<GroundWidth> LeftGrounds,
    ImmutableArray<GroundWidth> RightGrounds
);
