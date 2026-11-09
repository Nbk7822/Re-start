using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Events;
using SimCalendar = Return.Sim.Core.Calendar;

namespace Return.Game.Logic;

/// <summary>
/// Every sentence the view shows, built from the text-state API (StateApi, EventText) and loc
/// templates. Read-only over World; Godot-free so the tests compile it.
/// </summary>
public static class ViewText
{
    /// <summary>How many events the Chronicle popup lists.</summary>
    public const int ChronicleLength = 30;

    public readonly record struct PersonLine(int Id, string Text);

    /// <summary>"Day 3, 14:20 · Spring": EventText.When of the current tick plus the season name.</summary>
    public static string Clock(World w)
    {
        WorldDef def = w.Content.World;
        int season = SimCalendar.Season(w.Tick, def);
        string seasonName = season < def.SeasonKeys.Length ? Ui.T(def.SeasonKeys[season]) : "";
        return Ui.F("ui.clock", ("when", EventText.When(w, w.Tick)), ("season", seasonName));
    }

    /// <summary>Caption for a speed button: pause for 0, otherwise "×n".</summary>
    public static string SpeedLabel(int multiplier) =>
        multiplier <= 0 ? Ui.T("ui.speed.pause") : Ui.F("ui.speed.multiplier", ("n", Ui.N(multiplier)));

    /// <summary>The camp's members, each with what they are doing now.</summary>
    public static IReadOnlyList<PersonLine> People(World w)
    {
        var lines = new List<PersonLine>();
        foreach (Character c in w.Members())
        {
            CharacterState s = StateApi.Character(w, c.Id);
            lines.Add(new PersonLine(c.Id, Ui.F("ui.people.entry", ("name", s.Name), ("job", s.Job))));
        }
        return lines;
    }

    /// <summary>The most recent events, newest first, as "When — text".</summary>
    public static IReadOnlyList<string> Chronicle(World w, int max = ChronicleLength)
    {
        List<SimEvent> all = w.Log.All;
        var lines = new List<string>(max);
        for (int i = all.Count - 1; i >= 0 && lines.Count < max; i--)
        {
            SimEvent e = all[i];
            lines.Add(Ui.F("ui.chronicle.entry", ("when", EventText.When(w, e.Tick)), ("text", EventText.Describe(w, e))));
        }
        return lines;
    }

    /// <summary>
    /// The emote id of a person's most recent thought (latest start tick; ties go to the later
    /// entry), or null when they have no thoughts or that thought has no emote.
    /// </summary>
    public static string? MoodEmote(World w, Character c)
    {
        string? emote = null;
        long latest = long.MinValue;
        long ticksPerHour = w.Content.World.TicksPerHour;
        foreach (Thought t in c.Thoughts)
        {
            ThoughtDef def = w.Content.ThoughtById(t.DefId);
            long started = t.ExpiresTick - def.DurationHours * ticksPerHour;
            if (started < latest) continue;
            latest = started;
            emote = def.Emote;
        }
        return string.IsNullOrEmpty(emote) || emote == "none" ? null : emote;
    }

    /// <summary>The one-character mood marker over a person's head (see <see cref="MoodEmote"/>), or null.</summary>
    public static string? MoodMarker(World w, Character c)
    {
        string? emote = MoodEmote(w, c);
        return emote == null ? null : EmoteGlyph(emote);
    }

    /// <summary>The glyph for a content emote id ("hearts" → "♥"); null for "none".</summary>
    public static string? EmoteGlyph(string emote) =>
        string.IsNullOrEmpty(emote) || emote == "none" ? null : Ui.T("ui.emote." + emote);

    public static string OpinionLine(OpinionState o) =>
        Ui.F("ui.inspector.opinion", ("name", o.Target), ("value", Ui.Signed(o.Value)));

    public static string GrudgeLine(GrudgeState g) => g.Cause.Length == 0
        ? Ui.F("ui.inspector.grudge", ("target", g.Target), ("n", Ui.N(g.Intensity)))
        : Ui.F("ui.inspector.grudge_cause", ("target", g.Target), ("n", Ui.N(g.Intensity)), ("cause", g.Cause));

    public static string DebtLine(World w, DebtState d)
    {
        bool due = d.DueTick > 0;
        string key = d.IOwe
            ? (due ? "ui.inspector.debt_owe_due" : "ui.inspector.debt_owe")
            : (due ? "ui.inspector.debt_owed_due" : "ui.inspector.debt_owed");
        string when = due ? EventText.When(w, d.DueTick) : "";
        return Ui.F(key, ("other", d.Other), ("kind", d.Kind), ("status", d.Status), ("when", when));
    }

    /// <summary>Tooltip lines for a tile: terrain and coordinates, then contents, then who stands there.</summary>
    public static IReadOnlyList<string> TileLines(World w, LocationState s)
    {
        var lines = new List<string>
        {
            Ui.F("ui.tooltip.terrain", ("terrain", s.Terrain), ("x", Ui.N(s.X)), ("y", Ui.N(s.Y))),
        };
        lines.AddRange(s.Contents);
        foreach (int id in s.Characters) lines.Add(EventText.Name(w, id));
        return lines;
    }

    /// <summary>A ration policy as a choice in the Orders list ("The hungriest eat first").</summary>
    public static string PolicyOption(RationPolicy p) => Ui.T("ui.orders.policy." + Names.Snake(p.ToString()));

    /// <summary>What a ration policy means, for its tooltip.</summary>
    public static string PolicyHint(RationPolicy p) => Ui.T("ui.orders.policy." + Names.Snake(p.ToString()) + "_hint");

    /// <summary>The choices in a work priority picker, in order: 1 (done first) to 4, then never (0).</summary>
    public static readonly IReadOnlyList<int> PriorityValues = new[] { 1, 2, 3, 4, 0 };

    /// <summary>"1".."4", or the "never" mark for 0.</summary>
    public static string PriorityLabel(int priority) => priority <= 0 ? Ui.T("ui.priority.off") : Ui.N(priority);

    /// <summary>Each member with the work they do first (priority 1), for the Work list.</summary>
    public static IReadOnlyList<PersonLine> WorkLines(World w)
    {
        var lines = new List<PersonLine>();
        foreach (Character c in w.Members())
        {
            var first = new List<string>();
            foreach (WorkPriorityState p in StateApi.WorkPriorities(w, c.Id))
                if (p.Priority == 1) first.Add(p.Work);
            string name = EventText.Name(w, c.Id);
            lines.Add(new PersonLine(c.Id, first.Count == 0
                ? Ui.F("ui.work.entry_none", ("name", name))
                : Ui.F("ui.work.entry", ("name", name), ("works", JoinList(first)))));
        }
        return lines;
    }

    /// <summary>A person's traits, one per line, each with where it came from ("Practical — born with it"); "None" without traits.</summary>
    public static string TraitsText(GrowthState g)
    {
        if (g.Traits.Count == 0) return Ui.T("ui.inspector.none");
        var lines = new List<string>(g.Traits.Count);
        foreach (TraitOriginState t in g.Traits) lines.Add(Ui.F("ui.inspector.trait", ("trait", t.Trait), ("origin", t.Text)));
        return string.Join("\n", lines);
    }

    /// <summary>What a person wants, in plain words, and how far their arc has come once it has moved; "None" before they want anything.</summary>
    public static string WantLine(GrowthState g)
    {
        if (g.Want == null) return Ui.T("ui.inspector.none");
        if (g.Arc == null || g.Arc.Stage == ArcStage.Want) return g.Want.Text;
        return Ui.F("ui.inspector.want_stage", ("want", g.Want.Text), ("stage", g.Arc.StageLabel));
    }

    /// <summary>Items joined with the localised list separator.</summary>
    public static string JoinList(IReadOnlyList<string> items)
    {
        if (items.Count == 0) return Ui.T("ui.inspector.none");
        string sep = Ui.T("ui.list_separator");
        return string.Join(sep, items);
    }
}
