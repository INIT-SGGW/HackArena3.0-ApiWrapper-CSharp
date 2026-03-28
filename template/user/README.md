# HackArena3.0-ApiWrapper-CSharp

This API wrapper allows you to write an automated bot to play the racing game. By implementing the `IBot` interface, your code will be executed on every game tick, allowing you to read the current state of the race and send commands to your car.

## Core Concept: The `OnTick` Method

Every bot must implement the `IBot` interface, which contains a single crucial method:

```csharp
public void OnTick(RaceSnapshot snapshot, BotContext ctx)
```

- **`RaceSnapshot snapshot`**: Contains all the telemetry and data coming *from* the server about the current state of the world.
- **`BotContext ctx`**: The context used to send commands *to* the server and access static track information.

---

## 1. Reading Data: `RaceSnapshot`

The `RaceSnapshot` object gives you complete visibility into what is happening in the current tick.

### Key Fields in `RaceSnapshot`:
- `Tick` (int): The current simulation tick number.
- `ServerTimeMs` (int): The current server time in milliseconds.
- `Car` (`CarState`): The most important object. It contains the complete telemetry of your vehicle.
- `Opponents` (`ImmutableArray<OpponentState>`): A list containing the positions, orientations, and ghost states of other players on the track.
- `Raw` (`ParticipantSnapshot`): The raw protobuf data, useful if you need low-level access to the server's response.

### Deep Dive: `CarState`
The `snapshot.Car` property holds critical physics and status data required to make driving decisions:
- **Kinematics**: `Position` (Vec3), `Orientation` (Quaternion), `SpeedMps` (meters per second), and a convenient `SpeedKmh` property.
- **Engine & Transmission**: `EngineRpm`, `Gear` (Reverse, Neutral, First, Second, etc.).
- **Tires**: `TireType`, `TireSlip` (FrontLeft, FrontRight, etc. - crucial for detecting loss of traction), `TireWear`, and `TireTemperatureCelsius`.
- **Pitstop Status**: Information about pit requests, wheels in the pitstop, and remaining emergency lock time.

---

## 2. Sending Commands: `BotContext`

The `BotContext` object serves two purposes: providing static information about the race (like the track layout) and executing your bot's actions.

### Static Information Fields:
- `CarId` (int): Your unique vehicle identifier.
- `MapId` (string): The identifier of the current map.
- `Track` (`TrackLayout`): Contains the mathematical representation of the track, including the centerline (with tangents, normals, curvatures, and track widths) and the pitstop layout.
- `CarDimensions` (`CarDimensions`): The width and depth of your car.

### Action Methods:
To control your car, you must call methods on the `ctx` object inside your `OnTick` function. 

- **`SetControls(float throttle, float brake, float steer, GearShift gearShift = GearShift.None, float brakeBalancer = 0.5f, float differentialLock = 0.0f)`**
  This is the main function to drive the car. 
  - `throttle`: 0.0f to 1.0f.
  - `brake`: 0.0f to 1.0f.
  - `steer`: Typically -1.0f (full left) to 1.0f (full right).
  - `gearShift`: Used to manually Upshift or Downshift.

- **`RequestBackToTrack()`**
  Resets your car to the track if you are stuck or flipped over (subject to cooldowns).

- **`RequestEmergencyPitStop()`**
  Teleports your car to the pitstop in case of severe damage or being completely stuck (subject to cooldowns).

- **`SetNextPitTireType(TireType tireType)`**
  Pre-selects the type of tires the pit crew will install during your next pitstop.

---

## Example Bot

Below is a simple example of a bot. This bot waits for the race to start, ensures it is in the correct gear, drives forward, and implements a basic Traction Control System (TCS) by braking if the tires start slipping.

```csharp
using src.HackArena3.Enums;
using src.HackArena3.Interfaces;
using src.HackArena3.Models;

namespace Bot;

internal class MyFirstBot : IBot
{
    private int tick = 0;

    public void OnTick(RaceSnapshot snapshot, BotContext ctx)
    {
        this.tick++;

        // 1. Wait for the initial 50 ticks before doing anything
        if (this.tick <= 50)
        {
            return;
        }

        // 2. Basic Gear Management: If we are in Neutral or Reverse, shift up to drive forward
        if (snapshot.Car.Gear == DriveGear.Reverse || snapshot.Car.Gear == DriveGear.Neutral)
        {
            ctx.SetControls(
                throttle: 0.0f, 
                brake: 1.0f, 
                steer: 0.0f, 
                gearShift: GearShift.Upshift
            );
            return;
        }

        // 3. Gather tire slip data to check for traction loss
        float[] tireSlips = {
            snapshot.Car.TireSlip.FrontLeft,
            snapshot.Car.TireSlip.FrontRight,
            snapshot.Car.TireSlip.RearLeft,
            snapshot.Car.TireSlip.RearRight
        };

        // 4. Basic Traction Control: If any tire is slipping too much, cut throttle and brake slightly
        if (tireSlips.Max() > 1.0f)
        {
            ctx.SetControls(
                throttle: 0.0f,
                brake: 0.1f,
                steer: 0.0f
            );
            return;
        }

        // 5. Normal Driving: Apply throttle and drive straight
        ctx.SetControls(
            throttle: 0.55f,
            brake: 0.0f,
            steer: 0.0f
        );
    }
}
```

### Explanation of the Example:
1. We keep track of the ticks. We skip the first 50 ticks to let the physics engine settle.
2. We check `snapshot.Car.Gear`. If the car is not in a forward gear, we use `ctx.SetControls` and pass `GearShift.Upshift` to put the car into gear.
3. We extract the `TireSlip` values for all four wheels from `snapshot.Car`.
4. If the maximum slip exceeds 1.0f, the car is losing grip. We react by sending a control command with 0 throttle and a slight brake (0.1f) to regain traction.
5. If the tires have good grip, we apply 55% throttle and drive straight.

---

## Application Entry Point: `Program.cs`

The `Program.cs` file serves as the main entry point for the API Wrapper. By default, it contains the following code:

```csharp
using src.HackArena3;

return await Client.RunBot(new Bot.Bot(), args);
```

This is where your bot implementation is injected into the client loop. If you decide to change the name of your bot class (for example, renaming `Bot` to `MyFirstBot`), you must remember to update the instantiation in this file to match your new class name:

```csharp
return await Client.RunBot(new Bot.MyFirstBot(), args);
```

If you do not update `Program.cs` after renaming your class, the wrapper will not be able to compile and run your code.

---

## Command Line Interface (CLI)

Once you have written and tested your bot, you will use the provided `hackarena.exe` tool to interact with the servers and submit your code. Please remember that on linux and macos you should run `hackarena` cli as:
```bash
./hackarena
```
Here we will proceed as if we were working on windows.

### Submitting Your Bot
Every team has exactly 3 slots available for their bots (numbered 1, 2, and 3). To submit your bot to the server, use the following command:

```bash
hackarena.exe submit --slot 3 -d "My example bot"
```
- `--slot`: Specifies which of the 3 team slots to overwrite (must be 1, 2, or 3).
- `-d`: Provides a description or name for your bot, making it easier to identify on the website.

### Updating the API Wrapper
If the organizers release a new version of the API wrapper or fix bugs, you can easily update your local environment by running:

```bash
hackarena.exe update
```

This operation preserves your `user` folder.

### Need Help?
If you are ever unsure about the available commands, arguments, or how to configure the CLI tool, you can print the help menu at any time:

```bash
hackarena.exe --help
```