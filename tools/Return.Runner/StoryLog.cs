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

    /// <summary>
    /// Creates the world for a seed (your people and gift rolled, or chosen for testing), lets the stand-in player set
    /// its ration policy, lay out its zones and its days (ADR-0028, ADR-0029) and make its building plan (ADR-0027), and
    /// runs it to the end of whole years. `run` and `soak` both use this.
    /// </summary>
    internal static Simulation Simulate(ContentDb content, ulong seed, int years, RunSetup? setup = null, string plan = "none",
        string zones = "none", string days = "default")
    {
        setup ??= RunSetup.Plain;
        Simulation sim = Simulation.Create(content, seed, setup.Choice);
        Autoplay.Begin(sim, zones, days);
        Run(sim, TicksFor(content.World, years), setup, Autoplay.PlanFor(plan, seed));
        return sim;
    }

    /// <summary>The same, with only the stand-in player's building plan (ADR-0027).</summary>
    internal static Simulation Simulate(ContentDb content, ulong seed, int years, string plan) => Simulate(content, seed, years, null, plan);

    /// <summary>
    /// Steps the world until it reaches <paramref name="target"/> or your lifetime ends. A return sends the world back,
    /// so this can take more steps than ticks (ADR-0030); a world that needs more than <see cref="RunSetup.StepsPerTickCap"/>
    /// times as many is in a death loop and fails. Under --fates, you are killed once, in your first timeline, at the
    /// seed's fate tick. Each step the stand-in player gives its standing order and, at 08:00, marks out what its
    /// building plan has due (ADR-0027), against the world as it is then, so a world's log stays a function of its
    /// seed and setup. <paramref name="afterStep"/>, if given, reads the world after every step (the soak's meters);
    /// it must only read.
    /// </summary>
    internal static void Run(Simulation sim, long target, RunSetup setup, string plan = "none", Action<Simulation>? afterStep = null)
    {
        long forceAt = setup.Fates ? RunSetup.FateTick(sim.World.Seed, sim.World.Content.World) : -1;
        bool forced = false;
        long cap = RunSetup.StepsPerTickCap * Math.Max(1, target);
        for (long steps = 0; sim.World.Tick < target && !sim.World.Fate.Ended; steps++)
        {
            if (steps >= cap) throw new InvalidOperationException($"the world did not reach tick {target} within {cap} steps: a death loop");
            World w = sim.World;
            if (!forced && forceAt >= 0 && w.Fate.Timeline == 0 && w.Tick >= forceAt) forced = sim.ForceDeath();
            Autoplay.Tick(sim);
            Autoplay.Hour(sim, plan);
            sim.Step();
            afterStep?.Invoke(sim);
        }
    }

    internal static long TicksFor(WorldDef def, int years) => years * def.TicksPerYear;

    /// <summary>
    /// Stories as <see cref="StoryAnalyzer"/> finds them (a named character makes a chain count), and
    /// the notable events: any with a persistent effect, any from Incident, Combat or Culture, every Growth
    /// beat (a want forming, an arc moving, a trait changing: fixed decision 12), every line of who you are, your
    /// returns and your gift whatever system it is told in (a familiar face knowing you is Social, a meal from the
    /// satchel Needs; ADR-0030), and every event in a story chain.
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
            if (e.Effects.Count > 0 || e.System is SystemTag.Incident or SystemTag.Combat or SystemTag.Growth or SystemTag.Culture
                    or SystemTag.Fate or SystemTag.Gift || e.Key.StartsWith("event.fate.", StringComparison.Ordinal)
                    || e.Key.StartsWith("event.gift.", StringComparison.Ordinal) || inStory.Contains(e.Id)
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
        WriteLifeEnd(w, loc, writer);
        WriteLostFutures(w, loc, writer);
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
        WriteRebirth(w, loc, o);
        Line(o);
    }

    /// <summary>Who you were reborn as, and with what gift (ADR-0030); whether that was chosen for testing; how many lives you lost.</summary>
    private static void WriteRebirth(World w, LocTable loc, TextWriter o)
    {
        FateBook f = w.Fate;
        if (f.PeopleId.Length == 0) return;
        string people = loc.Get(w.Content.Rebirth.People(f.PeopleId)?.AsKey ?? "rebirth.as." + f.PeopleId);
        Line(o, f.GiftId.Length == 0
            ? Format(loc, "log.header.rebirth_giftless", ("people", people))
            : Format(loc, "log.header.rebirth", ("people", people), ("gift", loc.Get(w.Content.GiftById(f.GiftId).NameKey))));
        if (f.Chosen)
            Line(o, loc.Get(f.ChosenPeople && f.ChosenGift ? "log.header.chosen" : f.ChosenPeople ? "log.header.chosen_people" : "log.header.chosen_gift"));
        if (f.Returns > 0) Line(o, Format(loc, "log.header.lives_lost", ("n", Num(f.Returns))));
    }

    private static void WriteCast(World w, LocTable loc, TextWriter o)
    {
        Heading(o, loc.Get("log.cast.heading"), '-');
        foreach (Character c in w.Characters)
        {
            if (!c.Named) continue;
            CharacterState s = StateApi.Character(w, c.Id);
            BelongingState b = s.Belonging;
            string belongs = c.Id == w.PlayerId && w.Fate.PeopleId.Length > 0
                ? (w.Fate.GiftId.Length == 0 ? loc.Get("log.cast.reborn")
                    : Format(loc, "log.cast.reborn_gift", ("gift", loc.Get(w.Content.GiftById(w.Fate.GiftId).NameKey))))
                : b.Culture.Length == 0 ? loc.Get("log.cast.outsider")
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
        Line(o, Indent + Format(loc, "log.camp.beds", ("beds", Num(camp.Beds)), ("claimed", Num(camp.BedsClaimed))));
        if (camp.OpenPlans > 0) Line(o, Indent + Format(loc, "log.camp.plans", ("plans", Num(camp.OpenPlans))));
        Line(o, Indent + Format(loc, "log.camp.trees", ("trees", Num(camp.TreesStanding))));
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
                if (e.Key == Sim.Systems.Fate.ReturnedKey && ReturnedFrom(w, e) is { } lost)
                {
                    Line(o);
                    Line(o, Indent + Format(loc, "log.events.returned", ("death", EventText.When(w, lost.DeathTick)), ("back", EventText.When(w, lost.KnotTick))));
                }
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

    /// <summary>When your lifetime ended (old age, your soul's span), the lines the Chronicle closes on (§6.3, ADR-0030).</summary>
    private static void WriteLifeEnd(World w, LocTable loc, TextWriter o)
    {
        if (!w.Fate.Ended || w.Find(w.PlayerId) == null) return;
        Heading(o, loc.Get("log.end.heading"), '-');
        foreach (string line in StateApi.Player(w).EndLines) Line(o, Indent + line);
        Line(o);
    }

    /// <summary>The lost future a return event in the final log came from (its "future" argument).</summary>
    private static LostFuture? ReturnedFrom(World w, SimEvent e)
    {
        foreach (EventArg a in e.Args)
            if (a.Name == "future" && int.TryParse(a.Value.AsSpan(2), NumberStyles.Integer, CultureInfo.InvariantCulture, out int i)
                && i >= 0 && i < w.Fate.LostFutures.Count)
                return w.Fate.LostFutures[i];
        return null;
    }

    /// <summary>
    /// The Chronicle of Lost Futures (ADR-0030), after the log: each timeline you lost, oldest first, with when it ran
    /// from and to, how many stories ended in it, and its lines in order (growth beats marked as such, your death last).
    /// Nothing is written for a run without a return.
    /// </summary>
    private static void WriteLostFutures(World w, LocTable loc, TextWriter o)
    {
        List<LostFuture> futures = w.Fate.LostFutures;
        if (futures.Count == 0) return;
        Heading(o, loc.Get("log.lost.heading"), '-');
        for (int n = 0; n < futures.Count; n++)
        {
            LostFuture f = futures[n];
            if (n > 0) Line(o);
            Line(o, Indent + Format(loc, "log.lost.future", ("from", EventText.When(w, f.KnotTick)), ("to", EventText.When(w, f.DeathTick)), ("n", Num(f.StoriesFound))));
            for (int i = 0; i < f.Lines.Count; i++)
            {
                string text = EventText.Describe(w, f.Lines[i]);
                if (f.GrowthLines.Contains(i)) text = Format(loc, "log.lost.growth", ("text", text));
                Line(o, Indent + Indent + Format(loc, "log.lost.line", ("when", EventText.When(w, f.Lines[i].Tick)), ("text", text)));
            }
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
