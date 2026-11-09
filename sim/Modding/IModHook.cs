using Return.Sim.Events;

namespace Return.Sim.Modding;

/// <summary>
/// A mod's entry point into the sim. Hooks see every event as it is emitted. A hook that changes
/// state must draw randomness only from <c>w.Rng</c>, or determinism breaks. Hooks are not part of
/// snapshots: re-add them after <see cref="Simulation.Restore"/>.
/// </summary>
public interface IModHook
{
    void OnWorldCreated(World w);

    void OnEvent(World w, SimEvent e);
}
