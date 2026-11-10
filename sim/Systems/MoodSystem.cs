using Return.Sim.Content;

namespace Return.Sim.Systems;

/// <summary>
/// Thoughts are remembered moments with a mood effect and, if about someone, an opinion effect.
/// Mood = 50 + the mood of every active thought, clamped to 0..100, recomputed hourly.
/// </summary>
public static class MoodSystem
{
    /// <param name="opinionScalePct">Scales the thought's opinion effect (traits such as Proud take insults harder).</param>
    public static void AddThought(World w, Character c, string thoughtId, int aboutId, long cause, int opinionScalePct = 100)
    {
        ThoughtDef def = w.Content.ThoughtById(thoughtId);
        long expires = w.Tick + (long)def.DurationHours * w.Content.World.TicksPerHour;
        int count = 0;
        Thought? oldest = null;
        foreach (Thought t in c.Thoughts)
        {
            if (t.DefId != thoughtId || t.AboutId != aboutId) continue;
            count++;
            if (oldest == null || t.ExpiresTick < oldest.ExpiresTick) oldest = t;
        }
        if (count >= def.StackLimit && oldest != null)
        {
            oldest.ExpiresTick = expires;
            oldest.CauseEvent = cause;
        }
        else
        {
            c.Thoughts.Add(new Thought { DefId = thoughtId, AboutId = aboutId, ExpiresTick = expires, CauseEvent = cause });
        }
        if (def.Opinion != 0 && aboutId != 0) Relations.ChangeOpinion(w, c, aboutId, def.Opinion * opinionScalePct / 100, cause);
    }

    public static void Hourly(World w)
    {
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member) continue;
            long now = w.Tick;
            c.Thoughts.RemoveAll(t => t.ExpiresTick <= now);
            int mood = 50;
            foreach (Thought t in c.Thoughts) mood += w.Content.ThoughtById(t.DefId).Mood;
            c.Mood = Math.Clamp(mood, 0, 100);
        }
    }

    /// <summary>The emote of the most recent thought, for the view's mood marker ("none" if none).</summary>
    public static string CurrentEmote(World w, Character c) =>
        c.Thoughts.Count == 0 ? "none" : w.Content.ThoughtById(c.Thoughts[^1].DefId).Emote;
}
