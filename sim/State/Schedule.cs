namespace Return.Sim;

// The Schedule (ADR-0029): the camp's day, a person's own hours, what each hour was lived as, and the
// night watch kept in turns. All of it is plain state, serialised in snapshots; lists are fixed length
// (24 hours) or kept in id or hour order.

/// <summary>What an hour of the day is for. A code enum: the kinds of day in rhythms.json are made of these.</summary>
public enum DayBlock
{
    /// <summary>In bed until the block ends; up early only for hunger, the sun or being woken.</summary>
    Sleep,
    /// <summary>Needs first, then work by priority; idle when there is none.</summary>
    Work,
    /// <summary>Needs first and urgent help, no ordinary work; a nap if tired, then company and the fire.</summary>
    Free,
}

/// <summary>What someone mostly did in an hour, as their inspector's lived strip shows it.</summary>
public enum LivedKind
{
    Slept,
    Worked,
    Watched,
    Free,
    /// <summary>Eating, seeking cover, asking for blood, arriving, tending a need of their nature, or lying downed.</summary>
    Needs,
}

/// <summary>One of a person's own hours: the block they keep at that hour instead of their kind's camp day.</summary>
public sealed class HourBlock
{
    public int Hour { get; set; }
    public DayBlock Block { get; set; }
}

/// <summary>The camp's day for one kind of day (diurnal, nocturnal, sleepless): 24 blocks the player paints.</summary>
public sealed class CampDay
{
    public string Rhythm { get; set; } = "";
    public List<DayBlock> Hours { get; set; } = new();

    /// <summary>The event that last set it (0 while it is content's default).</summary>
    public long SetEvent { get; set; }
}

/// <summary>Samples of what someone did in the hour under way, ten an hour; written into their lived strip when the hour ends.</summary>
public sealed class HourTally
{
    public int Slept { get; set; }
    public int Worked { get; set; }
    public int Watched { get; set; }
    public int Free { get; set; }
    public int Needs { get; set; }

    /// <summary>Samples spent awake by the fire with someone else there (for an evening by the fire).</summary>
    public int Fire { get; set; }

    public int Total => Slept + Worked + Watched + Free + Needs;

    public void Clear() => Slept = Worked = Watched = Free = Needs = Fire = 0;
}

/// <summary>A refusal of the watch since the turn ended: who, the event, and whether the reason was one to sympathise with (hurt, tired, hungry).</summary>
public sealed class WatchRefusal
{
    public int RefuserId { get; set; }
    public long Event { get; set; }
    public bool Sympathetic { get; set; }
}

/// <summary>
/// The night watch as the camp keeps it right now (WatchSystem). One person holds the post at a time, in
/// turns; handing over walks someone to the next sleeper, who decides; the post can stand empty for a reason
/// (GapEvent), which a theft then cites.
/// </summary>
public sealed class WatchState
{
    /// <summary>Who holds the post (a KeepWatch job); 0 if nobody.</summary>
    public int WatcherId { get; set; }

    /// <summary>When the current turn ends.</summary>
    public long TurnEndsTick { get; set; }

    /// <summary>The event that began the current turn.</summary>
    public long TurnEvent { get; set; }

    /// <summary>The current turn was already stretched once by staying on or keeping it in someone's place (one extra turn at most).</summary>
    public bool Extended { get; set; }

    /// <summary>Who is walking to the post to take the next turn; 0 if nobody.</summary>
    public int IncomingId { get; set; }

    /// <summary>The event their "took the watch" will cite.</summary>
    public long IncomingCause { get; set; }

    /// <summary>Who is walking to wake someone for the watch (a WakeForWatch job), and whom; 0 if nobody.</summary>
    public int WakerId { get; set; }
    public int WakeTargetId { get; set; }

    /// <summary>The "went to wake" event of the walk under way.</summary>
    public long WakeEvent { get; set; }

    /// <summary>Sleepers asked since the turn ended.</summary>
    public int Tries { get; set; }

    /// <summary>Refusals since the turn ended, in order; cleared when a turn begins and when the watch hours end.</summary>
    public List<WatchRefusal> Refusals { get; set; } = new();

    /// <summary>The event that explains an empty post right now (a walk to wake someone, a watcher who left or fell asleep, a festival); 0 if none.</summary>
    public long GapEvent { get; set; }

    /// <summary>When the post last fell empty during watch hours, or -1 while someone holds it or is on the way (lead's call: "Nobody kept the watch." waits a whole hour).</summary>
    public long EmptySinceTick { get; set; } = -1;

    /// <summary>"Nobody kept the watch." has been said for this stretch of empty post.</summary>
    public bool UnkeptSaid { get; set; }

    /// <summary>Tonight's first turn has been taken (it cites the decision that set the watch).</summary>
    public bool FirstTurnTaken { get; set; }

    /// <summary>The watch hours were under way at the last step (the night's state is cleared when they end).</summary>
    public bool Active { get; set; }
}
