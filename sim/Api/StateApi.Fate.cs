using System.Globalization;
using Return.Sim.Content;
using Return.Sim.Systems;

namespace Return.Sim.Api;

/// <summary>A feeling you carry from a lost future: about whom, what kind, its pull on your opinion, and in plain words.</summary>
public sealed record MemoryState(int PersonId, string Person, MemoryKind Kind, int Opinion, string Text);

/// <summary>
/// Your gift as you know it (ADR-0030): its words, whether you have found it, its stage in this timeline, and (for the
/// satchel) what it holds. The soak and the text-state API see it before you find it; the view shows it only once awake.
/// </summary>
public sealed record PlayerGiftState(string Id, string Name, string Summary, string Grows, string Seen, bool Awake, int Stage, string StageText,
    int Held, int Capacity, string OrderId, string Order);

/// <summary>
/// You (the player design §10): your people, your age line and what clock you keep, your gift, the lives you have lost,
/// which timeline this is, the feelings you carry back, whether this run was chosen for testing, and how it ended.
/// </summary>
public sealed record PlayerState(int Id, string PeopleId, string People, int Age, string AgeLine, string LifeLine, PlayerGiftState? Gift,
    int LivesLost, int Timeline, IReadOnlyList<MemoryState> Memories, bool Chosen, bool Ended, string EndText);

/// <summary>A line of a lost future, read in the world as it is now.</summary>
public sealed record LostLineState(long Tick, string When, string Text, bool Growth, bool Death);

/// <summary>A timeline you lost: when it ran from and to, your death in it, how many stories ended in it, and its lines.</summary>
public sealed record LostFutureState(int Index, int Timeline, long KnotTick, long DeathTick, string From, string To, string Death,
    int Stories, IReadOnlyList<LostLineState> Lines);

/// <summary>A gift of the catalogue: its words, how it is seen, its stages and its order's choices.</summary>
public sealed record GiftInfo(string Id, string Name, string Summary, string Grows, string Seen, IReadOnlyList<string> Stages,
    string OrderTitle, IReadOnlyList<string> Choices);

public static partial class StateApi
{
    public static PlayerState Player(World w)
    {
        Character you = w.Get(w.PlayerId);
        LocTable loc = w.Content.Loc;
        FateBook f = w.Fate;
        string people = loc.Get(w.Content.SpeciesById(you.SpeciesId).NameKey);
        long now = you.Status == CharStatus.Dead && f.Ended ? f.EndTick : w.Tick;
        int age = Rebirth.AgeYears(w, you, now);
        SpeciesDef ages = w.Content.AgesAs(you.SpeciesId);
        string lifeLine = ages.Ageless ? loc.Get("life.ageless")
            : loc.Format("life.ages", Args(("years", (((ages.LifespanYears[0] + ages.LifespanYears[1]) / 2 + 5) / 10 * 10).ToString(CultureInfo.InvariantCulture))));

        PlayerGiftState? gift = null;
        if (Systems.Gifts.Held(w) is { } g)
        {
            int stage = Math.Min(w.Gift.Tier, g.Tiers.Count - 1);
            string orderId = g.Order?.Id ?? "", order = g.Order == null ? "" : Systems.Gifts.Order(w, g.Order);
            gift = new PlayerGiftState(g.Id, loc.Get(g.NameKey), loc.Get(g.SummaryKey), loc.Get(g.GrowsKey), loc.Get("gift_seen." + g.Seen),
                f.GiftAwakened, stage, loc.Get(g.Tiers[stage].StageKey), w.Gift.Held, g.Id == Systems.Gifts.SatchelId ? Satchel.Capacity(w, g) : 0, orderId, order);
        }

        var memories = new List<MemoryState>();
        foreach (Memory m in f.Memories)
        {
            string name = EventText.Name(w, m.PersonId);
            string key = m.Kind switch { MemoryKind.Wrong => "life.memory.wrong", MemoryKind.Death => "life.memory.death", _ => "life.memory.kindness" };
            string value = m.Opinion > 0 ? "+" + m.Opinion.ToString(CultureInfo.InvariantCulture) : m.Opinion.ToString(CultureInfo.InvariantCulture);
            memories.Add(new MemoryState(m.PersonId, name, m.Kind, m.Opinion, loc.Format(key, Args(("name", name), ("value", value)))));
        }

        string end = !f.Ended ? ""
            : f.EndKind == "soul_spent" ? loc.Format("life.end.soul_spent", Args(("age", age.ToString(CultureInfo.InvariantCulture))))
            : loc.Format("life.end.old_age", Args(("age", age.ToString(CultureInfo.InvariantCulture))));
        return new PlayerState(you.Id, you.SpeciesId, people, age,
            loc.Format("life.subtitle", Args(("people", people), ("age", age.ToString(CultureInfo.InvariantCulture)))), lifeLine, gift,
            f.Returns, f.Timeline, memories, f.Chosen, f.Ended, end);
    }

    /// <summary>Every timeline you lost, newest first, its lines read in the world as it is now.</summary>
    public static IReadOnlyList<LostFutureState> LostFutures(World w)
    {
        var list = new List<LostFutureState>();
        List<LostFuture> futures = w.Fate.LostFutures;
        for (int i = futures.Count - 1; i >= 0; i--)
        {
            LostFuture f = futures[i];
            var lines = new List<LostLineState>(f.Lines.Count);
            for (int k = 0; k < f.Lines.Count; k++)
            {
                LostLine line = f.Lines[k];
                lines.Add(new LostLineState(line.Tick, EventText.When(w, line.Tick), EventText.Describe(w, line), f.GrowthLines.Contains(k), k == f.DeathLine));
            }
            string death = f.DeathLine >= 0 ? EventText.Describe(w, f.Lines[f.DeathLine]) : "";
            list.Add(new LostFutureState(i, f.Timeline, f.KnotTick, f.DeathTick, EventText.When(w, f.KnotTick), EventText.When(w, f.DeathTick), death,
                f.StoriesFound, lines));
        }
        return list;
    }

    /// <summary>The gifts of the catalogue this content has behaviour for.</summary>
    public static IReadOnlyList<GiftInfo> Gifts(ContentDb content)
    {
        LocTable loc = content.Loc;
        var list = new List<GiftInfo>();
        foreach (GiftDef g in content.Gifts)
        {
            var stages = new List<string>();
            foreach (GiftTierDef t in g.Tiers) stages.Add(loc.Get(t.StageKey));
            var choices = new List<string>();
            if (g.Order != null)
                foreach (string c in g.Order.Choices) choices.Add(loc.Get(g.Order.ChoiceKey(c)));
            list.Add(new GiftInfo(g.Id, loc.Get(g.NameKey), loc.Get(g.SummaryKey), loc.Get(g.GrowsKey), loc.Get("gift_seen." + g.Seen), stages,
                g.Order == null ? "" : loc.Get(g.Order.TitleKey), choices));
        }
        return list;
    }
}
