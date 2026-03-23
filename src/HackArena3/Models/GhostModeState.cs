using System.Collections.Immutable;

namespace src.HackArena3.Models;

public record GhostModeState(bool CanCollideNow, int Phase, ImmutableArray<int> Blockers, int ExitDelayRemainingMs)
{
    public bool IsGhost => !CanCollideNow;
}
