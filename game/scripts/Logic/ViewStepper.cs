using System;
using Return.Sim;

namespace Return.Game.Logic;

/// <summary>
/// How the view runs the sim forward: one tick at a time, letting a witness (the map's gore log)
/// see the world after every tick (Simulation.Advance is the same loop). Blows are health drops
/// between looks and marks land where people stand when they are looked at, so a witness that saw
/// only every frame would leave blood that depends on how many ticks a frame happened to run. The
/// warm-up and the live loop both step through here, so the same history leaves the same marks.
/// </summary>
public static class ViewStepper
{
    public static void Run(Simulation sim, long ticks, Action<World>? witness)
    {
        for (long t = 0; t < ticks; t++)
        {
            sim.Step();
            witness?.Invoke(sim.World);
        }
    }
}
