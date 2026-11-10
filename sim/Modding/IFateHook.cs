namespace Return.Sim.Modding;

/// <summary>
/// An optional second interface for a mod hook (ADR-0030): a hook that also implements it hears when you die and the
/// world goes back, and when your lifetime ends. Hooks that implement only <see cref="IModHook"/> are unaffected; the
/// return's own events reach <see cref="IModHook.OnEvent"/> at the end of the tick like any other.
/// </summary>
public interface IFateHook
{
    /// <summary>After a return: <paramref name="w"/> is the world gone back to (the hook's world from now on) and <paramref name="lost"/> the future that never came.</summary>
    void OnReturned(World w, LostFuture lost);

    /// <summary>Your lifetime ended (old age, or your soul's span): no return follows, and the world steps no further.</summary>
    void OnLifeEnded(World w);
}
