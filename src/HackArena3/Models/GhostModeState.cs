using System.Collections.Immutable;
using Enums = src.HackArena3.Models;

namespace src.HackArena3.Models;

public record GhostModeState(
    bool CanCollideNow,
    Enums.GhostModePhase Phase,
    ImmutableArray<Enums.GhostModeBlocker> Blockers,
    int ExitDelayRemainingMs
) {
    public bool IsGhost => !CanCollideNow;
}
