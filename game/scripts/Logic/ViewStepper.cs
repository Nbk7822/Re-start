using System;
using Return.Sim;

namespace Return.Game.Logic;

/// <summary>
/// How the view runs the sim forward: one tick at a time, letting a witness (the map's gore log)
/// see the world after every tick (Simulation.Advance is the same loop). Blows are health drops
/// between looks and marks land where people stand when they are looked at, so a witness that saw
/// only every frame would leave blood that depends on how many ticks a frame happened to run. The
/// warm-up and the live loop both step through here, so the same history leaves the same marks.
/// A return (ADR-0030) swaps the world mid-batch: the batch stops there, unwitnessed, and the view
/// takes over (it pauses and plays the return); the end of your lifetime stops it too.
/// </summary>
public static class ViewStepper
{
    /// <summary>Steps up to <paramref name="ticks"/> times; returns how many steps ran (fewer when a return or the end of the run stopped the batch).</summary>
    public static long Run(Simulation sim, long ticks, Action<World>? witness)
    {
        for (long t = 0; t < ticks; t++)
        {
            World before = sim.World;
            if (before.Fate.Ended) return t;
            sim.Step();
            if (!ReferenceEquals(sim.World, before)) return t + 1; // the world went back: the new one is the view's to reset
            witness?.Invoke(sim.World);
            if (sim.World.Fate.Ended) return t + 1;
        }
        return ticks;
    }
}
