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

    /// <summary>Creates the world for a seed and runs it for whole years. `run` and `soak` both use this.</summary>
    internal static Simulation Simulate(ContentDb content, ulong seed, int years)
    {
        Simulation sim = Simulation.Create(content, seed);
        sim.Advance(TicksFor(content.World, years));
        return sim;
    }

    internal static long TicksFor(WorldDef def, int years) => years * def.TicksPerYear;

    /// <summary>
    /// Stories as <see cref="StoryAnalyzer"/> finds them (a named character makes a chain count), and
    /// the notable events: any with a persistent effect, any from Incident or Combat, and every event
    /// in a story chain.
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
            if (e.Effects.Count > 0 || e.System is SystemTag.Incident or SystemTag.Combat || inStory.Contains(e.Id))
                notable.Add(e);
        return new LogAnalysis(stories, notable);
    }

    internal static void Render(World w, LogAnalysis analysis, TextWriter writer)
    {
        LocTable loc = w.Content.Loc;
        WriteHeader(w, loc, writer);
        WriteCast(w, loc, writer);
        WriteCamp(w, loc, writer);
        WriteEvents(w, loc, analysis.Notable, writer);
        WriteStories(w, loc, analysis.Stories, writer);
        Line(writer, Format(loc, "log.counts",
            ("events", Num(w.Log.All.Count)), ("notable", Num(analysis.Notable.Count)), ("stories", Num(analysis.Stories.Count))));
    }

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
            Line(o, Indent + Format(loc, "log.cast.line",
                ("name", s.Name), ("species", s.Species), ("traits", List(loc, s.Traits)), ("status", s.Status)));
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
        foreach (SimEvent e in notable)
        {
            int day = Calendar.Day(e.Tick, w.Content.World);
            if (lastDay >= 0 && day != lastDay) Line(o);
            lastDay = day;
            Line(o, Indent + Format(loc, "log.events.line", ("when", EventText.When(w, e.Tick)), ("text", EventText.Describe(w, e))));
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
            for (int k = 0; k < story.Chain.Count; k++)
            {
                SimEvent e = w.Log.Get(story.Chain[k])
                    ?? throw new InvalidOperationException($"Story {n + 1} cites event {story.Chain[k]}, which is not in the log.");
                Line(o, Indent + Indent + Format(loc, "log.story.step",
                    ("n", Num(k + 1)), ("when", EventText.When(w, e.Tick)), ("text", EventText.Describe(w, e))));
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
