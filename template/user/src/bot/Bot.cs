using src.HackArena3.Interfaces;
using src.HackArena3.Models;

namespace Bot.user.src.bot;

internal class Bot : IBot
{
    private bool isInitTick = true;
    private float throttle = 0.01f;
    private float tireSlipThreshold = 0.3f;
    private float steer = 1;

    public void OnTick(RaceSnapshot snapshot, BotContext ctx)
    {
        if (isInitTick)
        {
            isInitTick = false;
            Console.WriteLine($"{snapshot.Car.TireType}");
        }

        List<float> tireSlips = [ snapshot.Car.TireSlip.RearLeft, snapshot.Car.TireSlip.FrontLeft, snapshot.Car.TireSlip.RearRight, snapshot.Car.TireSlip.FrontRight ];

        if (tireSlips.Max() > tireSlipThreshold)
        {
            throttle -= 0.01f;
        }
        else
        {
            throttle += 0.01f;
        }

        throttle = Math.Max(throttle, 0);
        throttle = Math.Min(throttle, 1);

        var smallestDistance = double.MaxValue;
        CenterlinePoint? nearestCenterlinePoint = null;
        foreach(var centerline in ctx.Track.Centerline)
        {
            var deltaX = centerline.Position.X - snapshot.Car.Position.X;
            var deltaY = centerline.Position.Y - snapshot.Car.Position.Y;
            var deltaZ = centerline.Position.Z - snapshot.Car.Position.Z;
            var distance = Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ));

            if(distance < smallestDistance)
            {
                smallestDistance = distance;
                nearestCenterlinePoint = centerline;
            }
        }

        Vec3 baseOrientation = new(0, 0, 1);
        var eulerOrientation = GetForwardVector(baseOrientation, snapshot.Car.Orientation);

        Console.WriteLine($"TANG: ({nearestCenterlinePoint!.Tangent.X}, {nearestCenterlinePoint!.Tangent.Y}, {nearestCenterlinePoint!.Tangent.Z})");
        Console.WriteLine($"EULR: ({eulerOrientation.X}, {eulerOrientation.Y}, {eulerOrientation.Z})");

        var crossProd = CorssProduct2d(nearestCenterlinePoint!.Tangent, eulerOrientation);
        Console.WriteLine($"crossProd: {crossProd}");
        if (Math.Abs(crossProd) > 0.02)
        {
            if (crossProd > 0)
            {
                steer *= 0.99f;
            }
            else
            {
                steer *= 1.01f;
            }
        }

        var vectorToCenterlinePoint = VectorFromTo(snapshot.Car.Position, nearestCenterlinePoint.Position);
        var crossProd2 = CorssProduct2d(eulerOrientation, vectorToCenterlinePoint);

        if (smallestDistance > 3f)
        {
            if (crossProd2 <= 1f)
            {
                steer *= 0.99f;
            }
            
            if( crossProd2 >= -1)
            {
                steer *= 1.01f;
            }
        }

        steer = Math.Max(steer, 0.9f);
        steer = Math.Min(steer, 1.1f);

        Console.WriteLine($"throttle: {throttle} steer: {steer-1}");
        ctx.SetControls(throttle, 0, steer-1);
    }

    private float CorssProduct2d(Vec3 u, Vec3 v)
    {
        return u.X * v.Z - u.Z * v.X;
    }

    private Vec3 GetForwardVector(Vec3 baseVector, Quaternion q)
    {
        float num = q.X * 2f;
        float num2 = q.Y * 2f;
        float num3 = q.Z * 2f;

        float num4 = q.X * num;
        float num5 = q.Y * num2;
        float num6 = q.Z * num3;
        float num7 = q.X * num2;
        float num8 = q.X * num3;
        float num9 = q.Y * num3;
        float num10 = q.W * num;
        float num11 = q.W * num2;
        float num12 = q.W * num3;

        var x = (1f - (num5 + num6)) * baseVector.X + (num7 - num12) * baseVector.Y + (num8 + num11) * baseVector.Z;
        var y = (num7 + num12) * baseVector.X + (1f - (num4 + num6)) * baseVector.Y + (num9 - num10) * baseVector.Z;
        var z = (num8 - num11) * baseVector.X + (num9 + num10) * baseVector.Y + (1f - (num4 + num5)) * baseVector.Z;

        return new Vec3(x,y,z);
    }

    private Vec3 VectorFromTo(Vec3 start, Vec3 end)
    {
        var x = end.X - start.X;
        var y = end.Y - start.Y;
        var z = end.Z - start.Z;

        return new Vec3(x, y, z);
    }
}
