using Return.Sim.Events;

namespace Return.Sim;

/// <summary>
/// Your people and gift chosen for testing (the runner's --people and --gift), instead of rolled. Null fields
/// are rolled as usual; a gift of "" or "none" means no gift. A chosen run says so in its log.
/// </summary>
public sealed record RebirthChoice(string? People = null, string? Gift = null)
{
    public const string NoGift = "none";
}

/// <summary>
/// Run memory (the player design §8.1, ADR-0030): the one block of the world that a return does not restore.
/// It holds your Truths (your people, gift and the end of your soul's span), what you know of your gift,
/// the timelines and their fate knots (metadata only: the snapshots live in the Simulation's archive),
/// everything you carry across a death (lost futures, memories, Familiar faces' echoes, whom you met, every
/// timeline's commands for replay), and how the run ended. A return restores a knot's snapshot and then
/// overwrites its copy of this block with the dying world's, carried forward.
/// </summary>
public sealed class FateBook
{
    /// <summary>The people you were reborn as (a species id): a Truth, or chosen for testing.</summary>
    public string PeopleId { get; set; } = "";

    /// <summary>Your one gift (a gifts.json id), "" for none: a Truth, or chosen for testing.</summary>
    public string GiftId { get; set; } = "";

    /// <summary>The dawn after which your soul's thread runs out, whatever the body.</summary>
    public long SoulEndTick { get; set; }

    /// <summary>Your people, and your gift, chosen for testing (--people, --gift) rather than rolled.</summary>
    public bool ChosenPeople { get; set; }
    public bool ChosenGift { get; set; }

    /// <summary>Anything about you was chosen for testing.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool Chosen => ChosenPeople || ChosenGift;

    /// <summary>You have found your gift; once found it is known in every timeline.</summary>
    public bool GiftAwakened { get; set; }

    /// <summary>The best stage your gift reached in any timeline (0 = its first). A stage reached before needs half the count.</summary>
    public int GiftMastery { get; set; }

    /// <summary>Which timeline this is: 0 for your first life here, one more for each return.</summary>
    public int Timeline { get; set; }

    /// <summary>How many times you have died and come back.</summary>
    public int Returns { get; set; }

    /// <summary>The fate knots the director has tied that still lie on your timeline, oldest first.</summary>
    public List<KnotRecord> Knots { get; set; } = new();
    public int NextKnotId { get; set; } = 1;

    /// <summary>Every timeline you lost, oldest first.</summary>
    public List<LostFuture> LostFutures { get; set; } = new();

    /// <summary>Feelings carried back from lost futures, one per person (the newest wins).</summary>
    public List<Memory> Memories { get; set; } = new();

    /// <summary>Familiar faces: each cast member's last opinion of you when a timeline was lost (the newest wins).</summary>
    public List<Echo> Echoes { get; set; } = new();

    /// <summary>Everyone you have spoken with, in any timeline (whom you met; ascending ids).</summary>
    public List<int> Met { get; set; } = new();

    /// <summary>The finished timelines, with the commands given in each, for <c>Simulation.Replay</c>.</summary>
    public List<TimelineRecord> Timelines { get; set; } = new();

    /// <summary>When this timeline began (the knot's tick), and the commands given in it so far, as enqueued.</summary>
    public long TimelineStart { get; set; }
    public List<TimedCommand> Commands { get; set; } = new();

    /// <summary>This timeline's death was forced by the soak harness or a test (<c>Fate.ForceDeath</c>), for replay.</summary>
    public long ForcedDeathTick { get; set; } = -1;

    /// <summary>The death of yours this tick, to be answered by a return at the end of the tick; 0 = none.</summary>
    public long PendingDeath { get; set; }

    /// <summary>The run is over: old age or the soul's end, the only deaths with no return. Nothing steps after this.</summary>
    public bool Ended { get; set; }

    /// <summary>How it ended: "old_age" or "soul_spent".</summary>
    public string EndKind { get; set; } = "";
    public long EndTick { get; set; }
}

/// <summary>Why the director tied a knot.</summary>
public enum KnotReason
{
    /// <summary>The first tick: you have just woken here. Never dropped, always eligible.</summary>
    Arrival,

    /// <summary>A turn in the story: an event on rebirth.json's trigger list.</summary>
    Turn,

    /// <summary>A dawn after hours with no knot.</summary>
    Quiet,
}

/// <summary>A fate knot's metadata (the snapshot itself lives in the Simulation's archive under <see cref="Id"/>).</summary>
public sealed class KnotRecord
{
    public int Id { get; set; }

    /// <summary>The tick the restored world resumes at (the knot is tied at the end of the tick before).</summary>
    public long Tick { get; set; }
    public KnotReason Reason { get; set; }

    /// <summary>The trigger event (in the knot's own log, so valid whenever it is restored), and its key; 0 and "" for arrival and quiet knots.</summary>
    public long TriggerEvent { get; set; }
    public string Trigger { get; set; } = "";

    /// <summary>How many returns have come back to it. Each use halves its weight in the pick, so a knot leading straight back to death frays.</summary>
    public int Uses { get; set; }

    /// <summary>The timeline it was tied in.</summary>
    public int Timeline { get; set; }
}

/// <summary>
/// A timeline that never came (the player design §5.5), frozen from the dying world's log: each event stored
/// once as a loc key and arguments, so it reads the same in any later world. Cast ids are stable (CreateCast
/// runs in file order); anyone made during play is frozen to a name key.
/// </summary>
public sealed class LostFuture
{
    /// <summary>The timeline that was lost (0 = your first life).</summary>
    public int Timeline { get; set; }

    /// <summary>Where the world went back to, and when you died.</summary>
    public long KnotTick { get; set; }
    public long DeathTick { get; set; }

    /// <summary>The frozen lines, in the order they happened: the kept stories' events after the knot, what memories quote, growth beats and your death.</summary>
    public List<LostLine> Lines { get; set; } = new();

    /// <summary>The index in <see cref="Lines"/> of your death.</summary>
    public int DeathLine { get; set; } = -1;

    /// <summary>The stories that ended after the knot and were kept (the eight longest, then those touching most systems).</summary>
    public List<LostStory> Stories { get; set; } = new();

    /// <summary>Lines that are companions' growth beats after the knot ("In that future, Hollis wanted to protect you.").</summary>
    public List<int> GrowthLines { get; set; } = new();

    /// <summary>How many stories ended after the knot (counted separately from the final log's: nothing counts twice).</summary>
    public int StoriesFound { get; set; }
}

/// <summary>One frozen event: when, from which system, its text as a key and arguments, and whom it was about.</summary>
public sealed class LostLine
{
    public long Tick { get; set; }
    public SystemTag System { get; set; }
    public string Key { get; set; } = "";
    public List<EventArg> Args { get; set; } = new();
    public List<int> Actors { get; set; } = new();
}

/// <summary>A story of a lost future: its lines after the knot (indices into the future's lines), its systems and its full length.</summary>
public sealed class LostStory
{
    public List<int> Lines { get; set; } = new();
    public List<SystemTag> Systems { get; set; } = new();
    public int Length { get; set; }
}

/// <summary>What kind of feeling a memory carries.</summary>
public enum MemoryKind
{
    /// <summary>A grudge you formed against them.</summary>
    Wrong,

    /// <summary>Their death while you were friends.</summary>
    Death,

    /// <summary>A kindness they did you.</summary>
    Kindness,
}

/// <summary>
/// A feeling carried back from a lost future about one person, at half strength (the player design §5.3): it acts
/// in the new timeline as a thought and a change of opinion, without your say, as everything your character feels does.
/// </summary>
public sealed class Memory
{
    public int PersonId { get; set; }
    public MemoryKind Kind { get; set; }

    /// <summary>The change it makes to your opinion of them (half what the lost future made it).</summary>
    public int Opinion { get; set; }

    /// <summary>A grudge you held against them in the lost future comes back at half its intensity, and fades as grudges do; 0 for none.</summary>
    public int Grudge { get; set; }

    /// <summary>The lost future (index in FateBook.LostFutures) and the line in it the memory quotes.</summary>
    public int Future { get; set; }
    public int Line { get; set; }

    /// <summary>Identifies this memory wherever it has been applied (the lost future and the person).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string Key => Future + ":" + PersonId;
}

/// <summary>A cast member's last opinion of you when a timeline was lost (Familiar faces).</summary>
public sealed class Echo
{
    public int PersonId { get; set; }
    public int Opinion { get; set; }
}

/// <summary>A command as it was given: the tick the world stood at when it was enqueued, and the command.</summary>
public sealed class TimedCommand
{
    public long EnqueuedAt { get; set; }
    public Command Command { get; set; } = null!;
}

/// <summary>A finished timeline: when it began and ended, whether its death was forced, and the commands given in it.</summary>
public sealed class TimelineRecord
{
    public int Timeline { get; set; }
    public long StartTick { get; set; }
    public long DeathTick { get; set; }
    public bool Forced { get; set; }
    public List<TimedCommand> Commands { get; set; } = new();
}

/// <summary>
/// Your gift's state in this timeline (the player design §8.1): it goes back with the world on a return, unlike what
/// you know of it (<see cref="FateBook.GiftAwakened"/>, <see cref="FateBook.GiftMastery"/>).
/// </summary>
public sealed class GiftState
{
    /// <summary>The stage reached in this timeline (0 = the first tier).</summary>
    public int Tier { get; set; }

    /// <summary>How many of the events that make the gift grow have happened in this timeline.</summary>
    public int Progress { get; set; }

    /// <summary>Bottomless satchel: berries in it.</summary>
    public int Held { get; set; }

    /// <summary>Fortune's favourite: the season (counted from the world's start) death last passed you by; -1 = never.</summary>
    public long LastDeathPassedSeason { get; set; } = -1;

    /// <summary>Bottomless satchel: who has seen something vanish from nowhere in this timeline (ascending ids).</summary>
    public List<int> SeenBy { get; set; } = new();

    /// <summary>Familiar faces: who has felt they know you since the last return, in the order they did.</summary>
    public List<int> Familiar { get; set; } = new();

    /// <summary>Familiar faces: the line that said so, for each of <see cref="Familiar"/> (parallel).</summary>
    public List<long> FamiliarEvents { get; set; } = new();

    /// <summary>
    /// Familiar faces: those who were already your friend before they felt they knew you (ascending ids). They do not
    /// become your friend, so their friendship cannot count toward the gift's growth; a kindness of theirs still can.
    /// </summary>
    public List<int> FamiliarFriends { get; set; } = new();

    /// <summary>Familiar faces: those whose friendship or kindness has already counted toward the gift's growth (ascending ids).</summary>
    public List<int> Counted { get; set; } = new();

    /// <summary>When you last came back, and the line that said so (Familiar faces counts friendships within days of it); -1 and 0 = never.</summary>
    public long ReturnedTick { get; set; } = -1;
    public long ReturnedEvent { get; set; }
}

/// <summary>A memory applied in this world's history, so a restore that already holds it does not apply it twice.</summary>
public sealed class AppliedMemory
{
    public int PersonId { get; set; }
    public string Key { get; set; } = "";
    public int Opinion { get; set; }
}

/// <summary>A standing order: one of a gift's choices (the player design §8.3), set by <c>SetStandingOrder</c>.</summary>
public sealed class StandingOrder
{
    public string OrderId { get; set; } = "";
    public string ChoiceId { get; set; } = "";
}
