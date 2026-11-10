using Return.Sim.Content;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// What you carry back about people (the player design §5.3, lead's call): for each cast member, the strongest thing
/// between you after the knot becomes a memory at half the feeling the lost future built (rebirth.json's strengthPct),
/// at most four per return. In order: a grudge you formed against them (which you hold again, at half its intensity); else their death while you were friends; else
/// a feeling the lost future moved by at least minFeeling, a wrong if it fell (a refusal, an insult, a felled tree) or a
/// kindness if it rose (tending, blood, a rescue, a kept promise), quoting the thought about them that moved it most.
/// Memories gather across returns, one per person, the newest winning. In the new timeline each acts as a thought and a
/// change of your opinion, without your say, as everything your character feels does; it is applied only where the
/// restored world does not already hold it, so a memory never stacks.
/// </summary>
public static class Memories
{
    /// <summary>A memory found in a lost future: whom it is about, what kind, the change it makes to your opinion, the event it quotes, and the grudge it brings back (0 for none).</summary>
    public readonly record struct Found(int PersonId, MemoryKind Kind, int Opinion, long Event, int Grudge = 0);

    /// <summary>A death counts as a friend's when your regard for them stood at least this high (the friend_died line in Lifecycle).</summary>
    public const int FriendAt = 30;

    /// <summary>
    /// The memories a death leaves, strongest first. <paramref name="cut"/> is the restored knot's log length: the dying
    /// log's events from there on are the lost future. Your opinion in the restored world is what you felt at the knot.
    /// </summary>
    public static List<Found> Find(World dying, World restored, int cut)
    {
        var list = new List<Found>();
        Character you = dying.Get(dying.PlayerId);
        int pct = dying.Content.Rebirth.Memory.StrengthPct;
        long deathTick = dying.Log.Get(dying.Fate.PendingDeath)?.Tick ?? dying.Tick;
        foreach (Character x in dying.Characters)
        {
            if (x.Id == you.Id || !LostFutures.IsCast(dying, x.Id)) continue;
            int now = Relations.Opinion(dying, you.Id, x.Id);
            int then = Relations.Opinion(restored, restored.PlayerId, x.Id);

            Grudge? wrong = null;
            foreach (Grudge g in dying.Grudges)
                if (g.HolderId == you.Id && g.TargetId == x.Id && g.FormedEvent > cut) wrong = g;
            if (wrong != null)
            {
                list.Add(new Found(x.Id, MemoryKind.Wrong, Math.Min(-1, (now - then) * pct / 100), wrong.CauseEvent > cut ? wrong.CauseEvent : wrong.FormedEvent,
                    Math.Max(1, wrong.Intensity * pct / 100)));
                continue;
            }

            SimEvent? death = DeathAfter(dying, x.Id, cut);
            if (death != null)
            {
                // Regard for the dead only fades, a point a day, so what it was when they died is what it is now plus the days since.
                int atDeath = now > 0 ? Math.Min(100, now + (int)((deathTick - death.Tick) / dying.Content.World.TicksPerDay)) : now;
                if (atDeath >= FriendAt)
                {
                    list.Add(new Found(x.Id, MemoryKind.Death, Math.Max(1, atDeath * pct / 100), death.Id));
                    continue;
                }
            }

            int delta = now - then;
            if (Math.Abs(delta) < dying.Content.Rebirth.Memory.MinFeeling) continue;
            long moved = StrongestThought(dying, you, x.Id, cut, delta < 0);
            if (moved != 0)
                list.Add(new Found(x.Id, delta < 0 ? MemoryKind.Wrong : MemoryKind.Kindness, delta * pct / 100 == 0 ? Math.Sign(delta) : delta * pct / 100, moved));
        }
        list.Sort((a, b) =>
        {
            int byStrength = Math.Abs(b.Opinion).CompareTo(Math.Abs(a.Opinion));
            return byStrength != 0 ? byStrength : a.PersonId.CompareTo(b.PersonId);
        });
        int max = dying.Content.Rebirth.Memory.MaxPerReturn;
        if (list.Count > max) list.RemoveRange(max, list.Count - max);
        return list;
    }

    /// <summary>
    /// The event behind your thought about someone that moved your regard most in one direction, of those thought after
    /// the knot (a refusal's sting, a tending's warmth); 0 if none can be quoted.
    /// </summary>
    private static long StrongestThought(World w, Character you, int aboutId, int cut, bool negative)
    {
        long best = 0;
        int strongest = 0;
        foreach (Thought t in you.Thoughts)
        {
            if (t.AboutId != aboutId || t.CauseEvent <= cut) continue;
            int opinion = w.Content.ThoughtById(t.DefId).Opinion;
            if (negative ? opinion >= 0 : opinion <= 0) continue;
            if (Math.Abs(opinion) >= strongest) { strongest = Math.Abs(opinion); best = t.CauseEvent; }
        }
        return best;
    }

    private static SimEvent? DeathAfter(World w, int id, int cut)
    {
        List<SimEvent> log = w.Log.All;
        for (int i = log.Count - 1; i >= cut; i--)
            foreach (Effect f in log[i].Effects)
                if (f.Kind == EffectKind.Death && f.A == id) return log[i];
        return null;
    }

    /// <summary>The thought each kind of memory becomes.</summary>
    public static string ThoughtOf(MemoryKind kind) => kind switch
    {
        MemoryKind.Wrong => "remembered_wrong",
        MemoryKind.Death => "remembered_death",
        _ => "remembered_kindness",
    };

    /// <summary>The line that says you remembered it.</summary>
    public static string EventKeyOf(MemoryKind kind) => kind switch
    {
        MemoryKind.Wrong => "event.fate.remembered_wrong",
        MemoryKind.Death => "event.fate.remembered_death",
        _ => "event.fate.remembered_kindness",
    };

    /// <summary>Whether a line is one saying you remembered something from a lost future (of any kind).</summary>
    public static bool IsRememberedKey(string key) =>
        key == EventKeyOf(MemoryKind.Wrong) || key == EventKeyOf(MemoryKind.Death) || key == EventKeyOf(MemoryKind.Kindness);

    /// <summary>
    /// Applies every memory you carry to a world just returned to, where it lacks it: a line quoting the lost future
    /// ("You remembered a wrong from a future that never came: Hollis took the meal set aside for you."), citing the
    /// return; the thought; and the change to your opinion. A newer memory of someone replaces an older one the world
    /// already holds by the difference, so the newest wins and nothing doubles.
    /// </summary>
    public static void Apply(World w, long returned)
    {
        if (w.Find(w.PlayerId) is not { } you) return;
        foreach (Memory m in w.Fate.Memories)
        {
            if (w.Find(m.PersonId) is not { } x || x.Status == CharStatus.Dead) continue;
            AppliedMemory? had = w.Remembered.Find(a => a.PersonId == m.PersonId);
            if (had != null && had.Key == m.Key) continue;
            SimEvent e = w.Log.Emit(w, SystemTag.Mood, EventKeyOf(m.Kind), new[] { you.Id, x.Id }, new[] { returned }, null,
                EventArg.Char("a", you.Id), EventArg.Char("b", x.Id), new EventArg { Name = "what", Value = "e:" + m.Future + ":" + m.Line });
            MoodSystem.AddThought(w, you, ThoughtOf(m.Kind), x.Id, e.Id);
            Relations.ChangeOpinion(w, you, x.Id, m.Opinion - (had?.Opinion ?? 0), e.Id);
            // A grudge you held there you hold again, at half its intensity, fading a point a day as grudges do.
            if (m.Grudge > 0 && x.Status != CharStatus.Dead && Relations.ActiveGrudge(w, you.Id, x.Id) == null) Relations.FormGrudge(w, you, x, m.Grudge, e.Id);
            if (had == null) w.Remembered.Add(new AppliedMemory { PersonId = m.PersonId, Key = m.Key, Opinion = m.Opinion });
            else
            {
                had.Key = m.Key;
                had.Opinion = m.Opinion;
            }
        }
    }
}
