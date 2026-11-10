using Return.Sim.Events;

namespace Return.Sim.Modding;

/// <summary>
/// A mod's entry point into the sim. Hooks see every event at the end of the tick it happened in,
/// in order, once all systems have run for that tick; a hook may change the world there. A hook that
/// changes state must draw randomness only from <c>w.Rng</c>, or determinism breaks. Hooks are not
/// part of snapshots: re-add them after <see cref="Simulation.Restore"/>. A hook that changes zones
/// calls <c>Systems.Zones.Mark</c>, <c>Remove</c> or <c>Keep</c>, never edits <c>Map.Zones</c> directly
/// (ADR-0028).
/// </summary>
public interface IModHook
{
    void OnWorldCreated(World w);

    void OnEvent(World w, SimEvent e);
}
