using System.Globalization;
using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Events;
using Return.Sim.Stories;
using static Return.Runner.LocText;
using Calendar = Return.Sim.Core.Calendar;

namespace Return.Runner;

/// <summary>What the log takes from a world's history: its stories and its notable events, both in event-id order.</summary>
internal sealed record LogAnalysis(IReadOnlyList<Story> Stories, IReadOnlyList<SimEvent> Notable);

/// <summary>
/// The readable story log for one world, written for a stranger: who was there, how the camp ended
/// up, what happened, and the stories the analyzer found. The text is a pure function of world
/// state, so the same seed always gives the same bytes, whichever thread or machine writes it.
/// </summary>
public static class StoryLog
{
    private const string Indent = "  ";

    /// <summary>Writes the log for a simulation that has already been advanced.</summary>
    public static void Write(Simulation sim, TextWriter writer)
    {
        World w = sim.World;
        Render(w, Analyze(w), writer);
    }

    /// <summary>Creates the world for a seed, lets the stand-in player set its ration policy and lay out its zones, and runs it for whole years. `run` and `soak` both use this.</summary>
    internal static Simulation Simulate(ContentDb content, ulong seed, int years, string zones = "none", string days = "default")
    {
        Simulation sim = Simulation.Create(content, seed);
        Autoplay.Begin(sim, zones, days);
        sim.Advance(TicksFor(content.World, years));
        return sim;
    }

    internal static long TicksFor(WorldDef def, int years) => years * def.TicksPerYear;

    /// <summary>
    /// Stories as <see cref="StoryAnalyzer"/> finds them (a named character makes a chain count), and
    /// the notable events: any with a persistent effect, any from Incident, Combat or Culture, every Growth
    /// beat (a want forming, an arc moving, a trait changing: fixed decision 12), and every event in a story chain.
    /// </summary>
    internal static LogAnalysis Analyze(World w)
    {
        IReadOnlyList<SimEvent> all = w.Log.All;
        IReadOnlyList<Story> stories = StoryAnalyzer.Find(all, id => w.Find(id)?.Named == true);
        var inStory = new HashSet<long>(); // membership test only; never iterated
        foreach (Story s in stories)
            foreach (long id in s.Chain) inStory.Add(id);
        var notable = new List<SimEvent>();
        foreach (SimEvent e in all)
            if (e.Effects.Count > 0 || e.System is SystemTag.Incident or SystemTag.Combat or SystemTag.Growth or SystemTag.Culture || inStory.Contains(e.Id)
                || IsZoneEvent(e) || e.Key.StartsWith("event.mood.kept_away", StringComparison.Ordinal) || IsScheduleNotable(e.Key) || IsHomeLine(e.Key))
                notable.Add(e);
        return new LogAnalysis(stories, notable);
    }

    /// <summary>
    /// Schedule lines a stranger needs to follow the nights (ADR-0029): your decisions about the days and the watch, anyone
    /// going their own way over them, refusals, covers, stays, an empty post and why, and wear. Routine turns and wakings
    /// show only inside a story.
    /// </summary>
    internal static bool IsScheduleNotable(string key) =>
        StateApi.IsScheduleLine(key) && key is not ("event.work.took_watch" or "event.work.went_to_wake");

    /// <summary>Zone events explain behaviour to a stranger reader, as incidents do (ADR-0028): a stockpile marked or moved, a home area marked out.</summary>
    internal static bool IsZoneEvent(SimEvent e) =>
        e.Key.StartsWith("event.world.zone_", StringComparison.Ordinal) || e.Key.StartsWith("event.world.home_", StringComparison.Ordinal);

    /// <summary>The home area's lines (zones step 3): who went out after dark and why, who stayed in.</summary>
    internal static bool IsHomeLine(string key) => Soak.IsHomeStoryEvent(key);

    internal static void Render(World w, LogAnalysis analysis, TextWriter writer)
    {
        LocTable loc = w.Content.Loc;
        WriteHeader(w, loc, writer);
        WriteCast(w, loc, writer);
        WritePeoples(w, loc, writer);
        WriteCamp(w, loc, writer);
        WriteEvents(w, loc, analysis.Notable, writer);
        WriteStories(w, loc, analysis.Stories, writer);
        Line(writer, Format(loc, "log.counts",
            ("events", Num(w.Log.All.Count)), ("notable", Num(analysis.Notable.Count)), ("stories", Num(analysis.Stories.Count))));
    }

    /// <summary>The run of identical steps starting at <paramref name="start"/>, as the Chronicle folds it too (EventFold).</summary>
    internal static (int Unit, int Repeats, string Arg) Repetition(IReadOnlyList<SimEvent> chain, int start) => EventFold.Repetition(chain, start);

    private static string ArgValue(SimEvent e, string name) => EventFold.ArgValue(e, name);

    internal static string SystemKey(SystemTag system) => "log.system." + Names.Snake(system.ToString());

    private static void WriteHeader(World w, LocTable loc, TextWriter o)
    {
        WorldDef def = w.Content.World;
        string years = (w.Tick / (double)def.TicksPerYear).ToString("0.##", CultureInfo.InvariantCulture);
        Heading(o, loc.Get("log.title"), '=');
        Line(o, Format(loc, "log.header.seed", ("seed", Num(w.Seed))));
        Line(o, Format(loc, "log.header.years", ("years", years)));
        Line(o, Format(loc, "log.header.calendar", ("days", Num(def.TicksPerYear / def.TicksPerDay))));
        Line(o);
    }

    private static void WriteCast(World w, LocTable loc, TextWriter o)
    {
        Heading(o, loc.Get("log.cast.heading"), '-');
        foreach (Character c in w.Characters)
        {
            if (!c.Named) continue;
            CharacterState s = StateApi.Character(w, c.Id);
            BelongingState b = s.Belonging;
            string belongs = b.Culture.Length == 0 ? loc.Get("log.cast.outsider")
                : b.Faith.Length == 0 ? Format(loc, "log.cast.belongs_no_faith", ("culture", b.Culture))
                : Format(loc, "log.cast.belongs", ("culture", b.Culture), ("faith", b.Faith));
            Line(o, Indent + Format(loc, "log.cast.line",
                ("name", s.Name), ("species", s.Species), ("belongs", belongs), ("traits", List(loc, s.Traits)), ("status", s.Status)));
        }
        // Whose day differs from their kind's camp day, in plain words (ADR-0029).
        foreach (PersonDaySummary d in StateApi.Schedule(w).Different)
            Line(o, Indent + Format(loc, "log.cast.day", ("name", d.Name), ("summary", d.Summary)));
        Line(o);
    }

    /// <summary>
    /// The peoples and faiths the cast belongs to, in plain words: each culture's customs (festivals
    /// dated), its main faith and its stances; each faith's gods with their temperaments.
    /// </summary>
    private static void WritePeoples(World w, LocTable loc, TextWriter o)
    {
        Heading(o, loc.Get("log.peoples.heading"), '-');
        foreach (CultureState c in StateApi.Cultures(w))
        {
            if (c.Members.Count == 0) continue;
            string line = Format(loc, "log.peoples.culture", ("culture", c.Name), ("customs", List(loc, c.Ways)), ("faith", c.Faith));
            if (c.Stances.Count > 0) line += " " + Format(loc, "log.peoples.stances", ("stances", List(loc, c.Stances)));
            Line(o, Indent + line);
        }
        foreach (FaithState f in StateApi.Faiths(w))
        {
            if (f.Followers.Count == 0) continue;
            var gods = new List<string>(f.Gods.Count);
            foreach (GodState g in f.Gods)
                gods.Add(Format(loc, "log.peoples.god", ("god", g.Name), ("epithet", g.Epithet), ("temperament", g.Temperament)));
            string godList = string.Join(loc.Get("log.peoples.god_sep"), gods);
            Line(o, Indent + (f.Ways.Count == 0
                ? Format(loc, "log.peoples.faith", ("faith", f.Name), ("gods", godList))
                : Format(loc, "log.peoples.faith_ways", ("faith", f.Name), ("gods", godList), ("customs", List(loc, f.Ways)))));
        }
        Line(o);
    }

    private static void WriteCamp(World w, LocTable loc, TextWriter o)
    {
        FactionState camp = StateApi.Faction(w);
        Heading(o, Format(loc, "log.camp.heading", ("camp", camp.Name)), '-');
        if (camp.Members.Count == 0)
        {
            Line(o, Indent + loc.Get("log.camp.nobody"));
        }
        else
        {
            var names = new List<string>(camp.Members.Count);
            foreach (int id in camp.Members) names.Add(EventText.Name(w, id));
            Line(o, Indent + Format(loc, "log.camp.members", ("names", List(loc, names))));
        }
        Line(o, Indent + Format(loc, "log.camp.stockpile", ("berries", Num(camp.Berries)), ("timber", Num(camp.Timber))));
        var zones = new List<string>();
        foreach (Zone z in w.Map.Zones)
        {
            int n = Return.Sim.Systems.Zones.Count(z.Tiles);
            zones.Add(Format(loc, n == 1 ? "log.camp.zone_one" : "log.camp.zone", ("zone", loc.Get(Return.Sim.Systems.Zones.ArticleKey(z.Kind, indefinite: true))),
                ("place", loc.Get(Return.Sim.Systems.Zones.PlacePhraseKey(w, z.Tiles))), ("n", Num(n))));
        }
        if (zones.Count > 0) Line(o, Indent + Format(loc, "log.camp.zones", ("zones", string.Join(loc.Get("log.camp.zone_sep"), zones))));
        Line(o, Indent + Format(loc, "log.camp.roof", ("sound", Num(camp.RoofSound)), ("total", Num(camp.RoofTotal))));
        Line(o, Indent + Format(loc, "log.camp.rationing", ("policy", camp.RationPolicy)));
        Line(o);
    }

    /// <summary>One line per notable event, with a blank line wherever the day changes.</summary>
    private static void WriteEvents(World w, LocTable loc, IReadOnlyList<SimEvent> notable, TextWriter o)
    {
        Heading(o, loc.Get("log.events.heading"), '-');
        if (notable.Count == 0) Line(o, Indent + loc.Get("log.events.none"));
        int lastDay = -1;
        for (int k = 0; k < notable.Count;)
        {
            (int unit, int repeats, string arg) = Repetition(notable, k);
            for (int u = 0; u < unit; u++)
            {
                SimEvent e = notable[k + u];
                int day = Calendar.Day(e.Tick, w.Content.World);
                if (lastDay >= 0 && day != lastDay) Line(o);
                lastDay = day;
                Line(o, Indent + Format(loc, "log.events.line", ("when", EventText.When(w, e.Tick)), ("text", EventText.Describe(w, e))));
            }
            if (repeats > 1)
            {
                var others = new List<string>(repeats - 1);
                for (int r = 1; r < repeats; r++) others.Add(EventText.ResolveMid(w, ArgValue(notable[k + r * unit], arg)));
                SimEvent last = notable[k + repeats * unit - 1];
                Line(o, Indent + Indent + Format(loc, "log.story.repeat", ("names", List(loc, others)), ("when", EventText.When(w, last.Tick))));
                lastDay = Calendar.Day(last.Tick, w.Content.World);
            }
            k += unit * repeats;
        }
        Line(o);
    }

    /// <summary>Each story: its chain as numbered, dated sentences, then its systems in chain order and its people.</summary>
    private static void WriteStories(World w, LocTable loc, IReadOnlyList<Story> stories, TextWriter o)
    {
        Heading(o, loc.Get("log.stories.heading"), '-');
        if (stories.Count == 0) Line(o, Indent + loc.Get("log.stories.none"));
        for (int n = 0; n < stories.Count; n++)
        {
            Story story = stories[n];
            if (n > 0) Line(o);
            Line(o, Indent + Format(loc, "log.story.heading", ("n", Num(n + 1))));
            var chain = new List<SimEvent>(story.Chain.Count);
            foreach (long id in story.Chain)
                chain.Add(w.Log.Get(id) ?? throw new InvalidOperationException($"Story {n + 1} cites event {id}, which is not in the log."));
            int step = 0;
            for (int k = 0; k < chain.Count;)
            {
                (int unit, int repeats, string arg) = Repetition(chain, k);
                for (int u = 0; u < unit; u++)
                {
                    SimEvent e = chain[k + u];
                    Line(o, Indent + Indent + Format(loc, "log.story.step",
                        ("n", Num(++step)), ("when", EventText.When(w, e.Tick)), ("text", EventText.Describe(w, e))));
                }
                if (repeats > 1)
                {
                    var others = new List<string>(repeats - 1);
                    for (int r = 1; r < repeats; r++) others.Add(EventText.ResolveMid(w, ArgValue(chain[k + r * unit], arg)));
                    SimEvent last = chain[k + repeats * unit - 1];
                    Line(o, Indent + Indent + Indent + Format(loc, "log.story.repeat",
                        ("names", List(loc, others)), ("when", EventText.When(w, last.Tick))));
                }
                k += unit * repeats;
            }
            var systems = new List<string>(story.Systems.Count);
            foreach (SystemTag t in story.Systems) systems.Add(loc.Get(SystemKey(t)));
            Line(o, Indent + Indent + Format(loc, "log.story.systems", ("systems", string.Join(loc.Get("log.story.systems_sep"), systems))));
            var people = new List<string>(story.Characters.Count);
            foreach (int id in story.Characters) people.Add(EventText.Name(w, id));
            Line(o, Indent + Indent + Format(loc, "log.story.people", ("people", List(loc, people))));
        }
        Line(o);
    }
}
