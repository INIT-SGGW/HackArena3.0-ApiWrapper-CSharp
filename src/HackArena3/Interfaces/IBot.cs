using HA3.Proto.Achievement.V1;
using src.HackArena3.Models;

namespace src.HackArena3.Interfaces;

public interface IBot
{
    public void OnTick(RaceSnapshot snapshot, BotContext ctx);
}
