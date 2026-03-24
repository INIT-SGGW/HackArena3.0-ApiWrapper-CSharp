using src.HackArena3.Interfaces;
using src.HackArena3.Models;

namespace template.user.src.bot;

internal class Bot : IBot
{
    public void OnTick(RaceSnapshot snapshot, BotContext ctx)
    {
        ctx.SetControls(0.25f, 0, 0);
    }
}
