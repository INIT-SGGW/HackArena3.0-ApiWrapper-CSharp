namespace src.HackArena3.Models;

public record CommandCooldownState(
    int BackToTrackRemainingMs,
    int EmergencyPitstopRemainingMs
);
