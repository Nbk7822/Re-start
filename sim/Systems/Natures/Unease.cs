namespace Return.Sim.Systems;

/// <summary>
/// The living are uneasy around the dead at first and warm to them with time (ADR-0022). For the
/// days a species unsettles others after it arrives (species unsettlesForDays), a chat with one of
/// them gives the living a share of the usual warmth that grows day by day, and leaves them uneasy;
/// the unease costs them regard for the newcomer in proportion to how strange they still are, so it
/// fades as the warmth comes in. Opinions move in whole points, so in practice a chat costs a point
/// or two in the first days, nothing after the first week, and warms as usual once the days are out.
/// </summary>
public static class Unease
{
    /// <summary>The share (0–100%) of a chat's warmth the listener takes from talking with other.</summary>
    public static int WarmthPct(World w, Character listener, Character other)
    {
        int days = Places.Species(w, other).UnsettlesForDays;
        if (days <= 0 || Places.Species(w, listener).UnsettlesForDays > 0) return 100;
        long known = (w.Tick - other.ArrivedTick) / w.Content.World.TicksPerDay;
        return (int)Math.Clamp(known * 100 / days, 0, 100);
    }

    /// <summary>After a chat: the listener's unease, while it lasts, weighing on their regard as much as the newcomer is still strange to them.</summary>
    public static void Feel(World w, Character listener, Character other, int pct)
    {
        if (pct < 100) MoodSystem.AddThought(w, listener, "uneasy_near_dead", other.Id, 0, 100 - pct);
    }
}
