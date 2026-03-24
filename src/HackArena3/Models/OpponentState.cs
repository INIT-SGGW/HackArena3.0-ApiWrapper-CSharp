using HA3.Proto.Race.V1;

namespace src.HackArena3.Models;

public record class OpponentState(
    int CarId,
    Vec3 Position,
    Quaternion Orientation,
    GhostModeState GhostMode
);
