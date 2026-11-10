namespace Return.Sim.Content;

// Who you are and how a run works (ADR-0030): rebirth.json and gifts.json. Read-only after load.

/// <summary>A people you can be reborn as, with its weight in the roll and the opening card's words.</summary>
public sealed class RebirthPeopleDef
{
    public string Species { get; init; } = "";
    public int Weight { get; init; }
    public string OpeningKey { get; init; } = "";

    /// <summary>"a vampire", "one of the fae": how the woke line names this people.</summary>
    public string AsKey => "rebirth.as." + Species;
}

/// <summary>When the director ties a fate knot (the player design §5.1).</summary>
public sealed class KnotRulesDef
{
    /// <summary>Event keys after which a knot is tied, if you are safe: a turn in the story.</summary>
    public List<string> Triggers { get; init; } = new();
    public int QuietAfterHours { get; init; }
    public int MinHoursBetween { get; init; }
    public int MaxPerDay { get; init; }
    public int SafeHealthAtLeast { get; init; }
}

/// <summary>How a death picks the knot it goes back to (the player design §5.2).</summary>
public sealed class ReturnRulesDef
{
    public int RecentDays { get; init; }
    public int TailPermille { get; init; }
    public int MinHoursBeforeDeath { get; init; }
}

/// <summary>How strongly, and how many, feelings you carry back from a lost future (the player design §5.3).</summary>
public sealed class MemoryRulesDef
{
    public int StrengthPct { get; init; }
    public int MaxPerReturn { get; init; }

    /// <summary>Without a grudge or a friend's death, a feeling is remembered only if the lost future moved your regard by at least this much.</summary>
    public int MinFeeling { get; init; }
}

/// <summary>rebirth.json: the peoples you can be, your soul's span, and the rules of knots, returns and memories.</summary>
public sealed class RebirthDef
{
    public List<RebirthPeopleDef> Peoples { get; init; } = new();
    public int[] SoulSpanYears { get; init; } = Array.Empty<int>();
    public KnotRulesDef Knots { get; init; } = new();
    public ReturnRulesDef Return { get; init; } = new();
    public MemoryRulesDef Memory { get; init; } = new();

    public RebirthPeopleDef? People(string species) => Peoples.Find(p => p.Species == species);
}

/// <summary>When a gift wakes: its trigger's line, or by a day, in a dream.</summary>
public sealed class GiftAwakeningDef
{
    /// <summary>The day (Day 1 first) by which, if its trigger has not come, the gift arrives in a dream.</summary>
    public int ByDay { get; init; }
    public string TextKey { get; init; } = "";
    public string DreamKey { get; init; } = "";
}

/// <summary>A stage of a gift, reached at a count of the events that make it grow.</summary>
public sealed class GiftTierDef
{
    public int At { get; init; }
    public string StageKey { get; init; } = "";
    public Dictionary<string, int> Params { get; init; } = new();
}

/// <summary>The standing order a gift lets you give (only while you hold it).</summary>
public sealed class GiftOrderDef
{
    public string Id { get; init; } = "";
    public List<string> Choices { get; init; } = new();
    public string Default { get; init; } = "";

    public string TitleKey => "order." + Id + ".title";
    public string ChoiceKey(string choice) => "order." + Id + "." + choice;
    public string HintKey(string choice) => ChoiceKey(choice) + ".hint";
    public string DecidedKey(string choice) => ChoiceKey(choice) + ".decided";
}

/// <summary>
/// A gift (the player design §4.2): one per run, a Truth. Its id names its behaviour in code, as an
/// incident's or a need's does; its numbers are params, overridden stage by stage by its tiers.
/// </summary>
public sealed class GiftDef
{
    public string Id { get; init; } = "";
    public string NameKey { get; init; } = "";
    public string SummaryKey { get; init; } = "";
    public string GrowsKey { get; init; } = "";
    public int Weight { get; init; }

    /// <summary>A weight for a people in place of <see cref="Weight"/> (0 for a people the gift cannot serve).</summary>
    public Dictionary<string, int> Peoples { get; init; } = new();

    /// <summary>hidden, seen or loud.</summary>
    public string Seen { get; init; } = "";
    public GiftAwakeningDef Awakening { get; init; } = new();
    public List<GiftTierDef> Tiers { get; init; } = new();
    public GiftOrderDef? Order { get; init; }
    public Dictionary<string, int> Params { get; init; } = new();

    public int WeightFor(string species) => Peoples.TryGetValue(species, out int w) ? w : Weight;

    /// <summary>
    /// A number the gift's behaviour reads, at a stage (0 = the first tier): the latest tier up to that stage
    /// that sets it, else the gift's own params. (Named apart from incidents' Param and customs' Arg.)
    /// </summary>
    public int Num(string name, int tier = 0)
    {
        for (int t = Math.Min(tier, Tiers.Count - 1); t >= 0; t--)
            if (Tiers[t].Params.TryGetValue(name, out int v)) return v;
        return Params.TryGetValue(name, out int p) ? p : throw new ContentException($"gifts.json: '{Id}' has no param '{name}'.");
    }

    /// <summary>Whether the gift's params (at some stage or its own) set this number.</summary>
    public bool Has(string name, int tier) =>
        Params.ContainsKey(name) || Tiers.Take(Math.Min(tier, Tiers.Count - 1) + 1).Any(t => t.Params.ContainsKey(name));
}
