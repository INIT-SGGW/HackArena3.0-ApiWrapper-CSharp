using src.HackArena3.Enums;
using src.HackArena3.Interfaces;
using src.HackArena3.Models;

namespace Bot;

internal class Bot : IBot
{
    private int tick = 0;

    public void OnTick(RaceSnapshot snapshot, BotContext ctx)
    {
        this.tick++;

        if (this.tick <= 50)
        {
            return;
        }

        if ((this.tick / 100) % 2 != 0)
        {
            if (snapshot.Car.Gear != DriveGear.Reverse)
            {
                ctx.SetControls(
                    throttle: 0.0f,
                    brake: 0.5f,
                    steer: 0.0f,
                    gearShift: GearShift.Downshift
                );
                return;
            }
        }
        else
        {
            if (snapshot.Car.Gear == DriveGear.Reverse || snapshot.Car.Gear == DriveGear.Neutral)
            {
                ctx.SetControls(
                    throttle: 0.0f,
                    brake: 0.5f,
                    steer: 0.0f,
                    gearShift: GearShift.Upshift
                );
                return;
            }
        }

        float[] tireSlips = {
            snapshot.Car.TireSlip.FrontLeft,
            snapshot.Car.TireSlip.FrontRight,
            snapshot.Car.TireSlip.RearLeft,
            snapshot.Car.TireSlip.RearRight
        };

        if (tireSlips.Max() > 1.0f)
        {
            ctx.SetControls(
                throttle: 0.0f,
                brake: 0.1f,
                steer: 0.0f
            );
            return;
        }

        ctx.SetControls(
            throttle: 0.55f,
            brake: 0.0f,
            steer: 0.0f
        );
    }
}
