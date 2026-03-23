using src.HackArena3.Enums;

namespace src.HackArena3.Models;

public record Controls(
    float Throttle,
    float Brake,
    float Steering,
    GearShift GearShift = GearShift.None,
    float BreakBalancer = 0.5f,
    float DifferentialLock = 0.0f
);