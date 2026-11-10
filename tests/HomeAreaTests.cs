using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Stories;
using Return.Sim.Systems;
using Return.Sim.Systems.Jobs;

namespace Return.Tests;

/// <summary>
/// Zones step 3 (ADR-0028; zones design 5.5, tests under 9.2): the dark, which costs every camp whether or not it marks a
/// home area, and the home area itself, built on the Schedule's day and watch.
/// </summary>
public class HomeAreaTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    private static long Hours(World w, double h) => (long)(h * w.Content.World.TicksPerHour);

    /// <summary>A tile this far east of the hearth: beyond the firelight from 9 on.</summary>
    private static Point East(World w, int dx) => new(w.Map.Hearth.X + dx, w.Map.Hearth.Y);

    // ---- The dark (design question 2) ----

    [Fact]
    public void TheFirelightReachesEightTilesInAStraightLine()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Assert.True(Zones.Lit(w, East(w, 8)));
        Assert.False(Zones.Lit(w, East(w, 9)));
        Assert.True(Zones.Lit(w, new Point(w.Map.Hearth.X + 5, w.Map.Hearth.Y + 6))); // 25 + 36 = 61
        Assert.False(Zones.Lit(w, new Point(w.Map.Hearth.X + 6, w.Map.Hearth.Y + 6))); // 72
    }

    [Fact]
    public void TheNightFolkAreTheVampiresAndTheUndeadByTheirKindOfDay()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        Assert.True(Zones.NightFolk(w, Cast(w, "ilex")));
        Assert.True(Zones.NightFolk(w, Cast(w, "ketil"))); // Varrow, the undead
        foreach (string key in new[] { "you", "mira", "dryope", "qamar", "fionn" }) // a human, Thessil, Cindral, Sennet
            Assert.False(Zones.NightFolk(w, Cast(w, key)), key);
        // A human put on nights still does not see in the dark: it is their kind of day that counts, not their hours.
        Character mira = Cast(w, "mira");
        for (int h = 0; h < 24; h++) ScheduleSystem.SetOwn(mira, h, DayBlock.Work);
        Assert.False(Zones.NightFolk(w, mira));
    }

    /// <summary>Design 9.2: accident odds double in the dark beyond 8 tiles for humans, not for the night folk or within the light; worn out, four times, and the text says both.</summary>
    [Fact]
    public void WorkInTheDarkBeyondTheFirelightGoesWrongTwiceAsOften()
    {
        var risky = new WorkDef { Id = "test_risky", AccidentPermille = 200 };
        int Accidents(string key, double hour, int dx, bool worn, out List<SimEvent> lines)
        {
            World w = WorkTests.Only(9, key);
            Character c = Cast(w, key);
            w.Tick = Hours(w, hour);
            c.Pos = East(w, dx);
            if (worn) c.WornOutEvent = w.Log.Emit(w, SystemTag.Needs, "event.needs.worn_out", new[] { c.Id }, Array.Empty<long>()).Id;
            int first = w.Log.All.Count;
            for (int i = 0; i < 500; i++)
            {
                JobRunner.RollAccident(w, c, risky, 0);
                c.Health = 1000;
                c.Downed = false;
            }
            lines = w.Log.All.Skip(first).Where(e => e.Key.StartsWith("event.incident.work_accident", StringComparison.Ordinal)).ToList();
            return lines.Count;
        }
        int day = Accidents("oren", 12, 10, false, out var dayLines);
        int dark = Accidents("oren", 23, 10, false, out var darkLines);
        int lit = Accidents("oren", 23, 4, false, out var litLines);
        int vampire = Accidents("ilex", 23, 10, false, out var vampireLines);
        int tired = Accidents("oren", 23, 10, true, out var tiredLines);
        Assert.All(dayLines, e => Assert.Equal("event.incident.work_accident", e.Key));
        Assert.All(darkLines, e => Assert.Equal("event.incident.work_accident_dark", e.Key));
        Assert.All(litLines, e => Assert.Equal("event.incident.work_accident", e.Key));
        Assert.All(vampireLines, e => Assert.Equal("event.incident.work_accident", e.Key));
        Assert.All(tiredLines, e => Assert.Equal("event.incident.work_accident_tired_dark", e.Key));
        Assert.InRange(dark * 100 / day, 160, 240);
        Assert.InRange(lit * 100 / day, 70, 130);
        Assert.InRange(vampire * 100 / day, 70, 130);
        Assert.InRange(tired * 100 / day, 330, 470);
    }

    [Fact]
    public void AnAccidentInTheDarkSaysSoAndWornOutCitesTheWear()
    {
        World w = WorkTests.Only(1, "you", "oren");
        Character you = Cast(w, "you"), oren = Cast(w, "oren");
        w.Tick = Hours(w, 21);
        oren.Pos = East(w, 12);
        SimEvent work = w.Log.Emit(w, SystemTag.Work, "event.work.felled_tree", new[] { oren.Id }, Array.Empty<long>(), null, EventArg.Char("a", oren.Id));
        SimEvent hurt = IncidentSystem.Fire(w, "work_accident", work.Id, oren.Id)!;
        Assert.Equal("Oren was hurt in an accident while working in the dark.", EventText.Describe(w, hurt));
        Assert.Equal(new[] { work.Id }, hurt.Causes);

        you.Pos = East(w, 12);
        you.WornOutEvent = w.Log.Emit(w, SystemTag.Needs, "event.needs.worn_out", new[] { you.Id }, Array.Empty<long>()).Id;
        SimEvent yours = IncidentSystem.Fire(w, "work_accident", work.Id, you.Id)!;
        Assert.Equal("You, worn out, were hurt in an accident while working in the dark.", EventText.Describe(w, yours));
        Assert.Equal(new[] { work.Id, you.WornOutEvent }, yours.Causes);
    }

    /// <summary>The view bleeds every kind of accident where it happened: worn out, in the dark, or both (the worn-out one used to leave no mark).</summary>
    [Fact]
    public void EveryKindOfAccidentBleedsOnTheMap()
    {
        World w = WorkTests.Only(1, "mira");
        Character mira = Cast(w, "mira");
        var gore = new Return.Game.Logic.GoreLog();
        gore.Observe(w);
        int dx = 3;
        foreach (string key in new[] { "event.incident.work_accident", "event.incident.work_accident_tired", "event.incident.work_accident_dark", "event.incident.work_accident_tired_dark" })
        {
            mira.Pos = East(w, dx += 2);
            w.Log.Emit(w, SystemTag.Incident, key, new[] { mira.Id }, Array.Empty<long>(), null, EventArg.Char("a", mira.Id));
            gore.Observe(w);
            Assert.Contains(gore.Decals, d => d.Kind == Return.Game.Logic.DecalKind.Splatter && (int)MathF.Floor(d.X) == mira.Pos.X && (int)MathF.Floor(d.Y) == mira.Pos.Y);
        }
    }

    /// <summary>Design 9.2 (CausalityTests): starving → forage → foraged → accident in the dark → care → owes is one story across Needs, Work, Incident and Social.</summary>
    [Fact]
    public void AStarvingForagerHurtInTheDarkIsOneStoryFromTheHungerToTheDebt()
    {
        World w = WorkTests.Only(1, "you", "bram");
        Character you = Cast(w, "you"), bram = Cast(w, "bram");
        w.Tick = Hours(w, 23);
        Stock.Take(w, "berries", 99);
        SimEvent starving = w.Log.Emit(w, SystemTag.Needs, "event.needs.starving", new[] { you.Id }, Array.Empty<long>(), null, EventArg.Char("a", you.Id));
        you.Food = 100;
        you.LastFoodEvent = starving.Id;
        Job forage = JobPicker.Choose(w, you)!;
        Assert.Equal(JobKind.Forage, forage.Kind);
        Assert.Equal(starving.Id, forage.CauseEvent);

        // At a bush beyond the firelight the picking cites the hunger, and the accident cites the picking.
        you.Pos = East(w, 11);
        SimEvent picked = w.Log.Emit(w, SystemTag.Work, "event.work.foraged", new[] { you.Id }, new[] { forage.CauseEvent }, null,
            EventArg.Char("a", you.Id), EventArg.Num("n", 2), EventArg.Item("item", "berries"));
        JobRunner.RollAccident(w, you, new WorkDef { Id = "test_sure", AccidentPermille = 1000 }, picked.Id);
        SimEvent hurt = w.Log.All.Last(e => e.Key.StartsWith("event.incident.work_accident", StringComparison.Ordinal));
        Assert.Equal("event.incident.work_accident_dark", hurt.Key);
        you.Health = 300; // badly hurt, whatever the damage drawn

        bram.Pos = new Point(you.Pos.X - 1, you.Pos.Y);
        var care = new Job { Kind = JobKind.Care, TargetId = you.Id, Target = you.Pos, Stage = 1, CauseEvent = you.LastHealthEvent };
        for (int t = 0; t < 2_000 && !CareJobs.Care(w, bram, care); t++) { }
        SimEvent owes = w.Log.All.Last(e => e.Effects.Any(f => f.Kind == EffectKind.DebtFormed));
        Story story = StoryAnalyzer.Find(w.Log.All, id => w.Find(id)?.Named == true).Single(s => s.TerminalEventId == owes.Id);
        Assert.Contains(starving.Id, story.Chain);
        Assert.Contains(picked.Id, story.Chain);
        Assert.Contains(hurt.Id, story.Chain);
        foreach (SystemTag t in new[] { SystemTag.Needs, SystemTag.Work, SystemTag.Incident, SystemTag.Social })
            Assert.Contains(t, story.Systems);
    }

    // ---- The home area as a zone ----

    private static void Apply(Simulation s, Command c)
    {
        s.Enqueue(c with { Tick = s.World.Tick });
        s.Step();
    }

    /// <summary>The camp clearing, which the stand-in player marks as the home area.</summary>
    internal static Rect Clearing(World w) => WorldGen.ClearingOf(w.Map.Shelter);

    [Fact]
    public void TheHomeAreaIsMarkedOutChangedAndTakenAwayInItsOwnWords()
    {
        Simulation s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        Rect clearing = Clearing(w);
        Assert.Null(Zones.HomeOf(w));
        Assert.False(Zones.InHome(w, w.Map.Hearth));

        Apply(s, new MarkZone(0, 0, "home", Zones.FromRect(clearing)));
        Zone home = Zones.HomeOf(w)!;
        SimEvent marked = w.Log.All.Last(e => e.Key == "event.world.home_marked");
        Assert.Equal("You marked out a home area (418 tiles), asking everyone to keep inside it after dark.", EventText.Describe(w, marked));
        Assert.Equal(marked.Id, home.MadeEvent);
        Assert.Equal(w.PlayerId, home.MadeBy);
        // An area lies over anything (walls, the hearth, the stockpile, the meeting place) and takes no tiles from them.
        Assert.True(Zones.InHome(w, w.Map.Hearth) && Zones.InHome(w, w.Map.WatchPost) && Zones.InHome(w, w.Map.StartStockpile.Center));
        Assert.True(Zones.InHome(w, new Point(w.Map.Shelter.X, w.Map.Shelter.Y)));
        Assert.Equal(12, Zones.Count(w.Map.Zones.First(z => z.Kind == "stockpile").Tiles));
        Assert.Equal(24, Zones.Count(w.Map.Zones.First(z => z.Kind == "meeting_place").Tiles));

        // There is only one home area: a second marking joins the first.
        var beyond = new Rect(clearing.X + clearing.W, clearing.Y, 2, 2);
        Apply(s, new MarkZone(s.World.Tick, 0, "home", Zones.FromRect(beyond)));
        Assert.Single(w.Map.Zones, z => z.Kind == "home");
        Assert.Equal(422, Zones.Count(Zones.HomeOf(w)!.Tiles));
        SimEvent changed = w.Log.All.Last();
        Assert.Equal("You changed the home area; it now covers 422 tiles.", EventText.Describe(w, changed));
        Assert.Equal(changed.Id, Zones.HomeOf(w)!.MadeEvent);

        Apply(s, new RemoveZone(s.World.Tick, home.Id));
        Assert.Null(Zones.HomeOf(w));
        Assert.Equal("You took away the home area; people may go where they like after dark.", EventText.Describe(w, w.Log.All.Last()));
        Assert.False(Zones.InHome(w, w.Map.Hearth));
    }

    [Fact]
    public void TheTooltipNamesTheHomeAreaAndAfterDarkWhetherTheFireLightsTheTile()
    {
        Simulation s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        Apply(s, new MarkZone(0, 0, "home", Zones.FromRect(Clearing(w))));
        w.Tick = Hours(w, 12);
        Point hearthSide = new(w.Map.Hearth.X + 1, w.Map.Hearth.Y);
        IReadOnlyList<string> noon = StateApi.Location(w, hearthSide.X, hearthSide.Y).Contents;
        Assert.Contains("Home area", noon);
        Assert.DoesNotContain("By the fire's light", noon);
        w.Tick = Hours(w, 22);
        Assert.Contains("By the fire's light", StateApi.Location(w, hearthSide.X, hearthSide.Y).Contents);
        Point far = East(w, 10);
        Assert.Contains("In the dark", StateApi.Location(w, far.X, far.Y).Contents);
    }

    // ---- The curfew (design 5.5, tests under 9.2) ----

    /// <summary>A camp of these people with a home area of the tiles within <paramref name="radius"/> of the hearth (Chebyshev), marked by you at the given hour of day 2.</summary>
    private static World WithHome(int radius, double hour, params string[] keys)
    {
        World w = WorkTests.Only(1, keys);
        foreach (Character c in w.Members()) { c.Traits = new List<string> { "practical" }; c.CultureId = ""; c.FaithId = ""; }
        w.Tick = w.Content.World.TicksPerDay + Hours(w, hour);
        Point h = w.Map.Hearth;
        new MarkZone(w.Tick, 0, "home", Zones.FromRect(new Rect(h.X - radius, h.Y - radius, 2 * radius + 1, 2 * radius + 1))).Apply(w);
        return w;
    }

    private static Zone Home(World w) => Zones.HomeOf(w)!;

    /// <summary>Lets someone go their own way tonight, as if tonight's roll had said so.</summary>
    private static void Defiant(World w, Character c, string why = Compliance.OwnWay)
    {
        c.NightRollDay = Zones.Tonight(w);
        c.DefiesTonight = why;
    }

    [Fact]
    public void AtDuskAHumanOutAtWorkWalksHomeAndTheNightFolkDoNot()
    {
        World w = WithHome(4, 18.5, "oren", "ilex");
        Character oren = Cast(w, "oren"), ilex = Cast(w, "ilex");
        foreach (Character c in new[] { oren, ilex })
        {
            c.Pos = East(w, 10);
            c.Job = new Job { Kind = JobKind.GatherTimber, Target = East(w, 11), StartedTick = w.Tick };
        }
        w.Tick = w.Content.World.TicksPerDay + Hours(w, 19);
        Zones.Hourly(w);
        Assert.Null(oren.Job); // he leaves the job: his own choice at dusk
        Assert.Equal(JobKind.GatherTimber, ilex.Job?.Kind); // the dark is the vampire's day
        Job next = JobPicker.Choose(w, oren)!;
        Assert.True(Zones.InHome(w, next.Target), $"{next.Kind} to {next.Target}");
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.work.out_after_dark", StringComparison.Ordinal));
    }

    [Fact]
    public void MarkingAHomeAreaAtNightPullsNobodyOffTheJobInHandAndTheirNextChoiceObeysIt()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        foreach (Character c in w.Characters) if (c.CastKey != "oren") c.Status = CharStatus.OffMap;
        Character oren = Cast(w, "oren");
        oren.Food = 900; oren.Rest = 900; oren.Social = 900;
        w.WatchFrom = w.WatchTo = 0; // no watch to send him to
        w.Tick = w.Content.World.TicksPerDay + Hours(w, 21);
        oren.Pos = East(w, 10);
        Job walk = new() { Kind = JobKind.Wander, Target = East(w, 12), TargetId = 600, StartedTick = w.Tick };
        oren.Job = walk;
        Point h = w.Map.Hearth;
        Apply(s, new MarkZone(w.Tick, 0, "home", Zones.FromRect(new Rect(h.X - 3, h.Y - 3, 7, 7))));
        Assert.Same(walk, oren.Job);
        oren.Job = null;
        Job next = JobPicker.Choose(w, oren)!;
        Assert.True(Zones.InHome(w, next.Target), $"{next.Kind} to {next.Target}");
    }

    [Fact]
    public void AHomeAreaDrawnShortOfTheBedsNeverKeepsAnyoneFromTheirBed()
    {
        World w = WorkTests.Only(1, "oren");
        w.Tick = w.Content.World.TicksPerDay + Hours(w, 23);
        new MarkZone(w.Tick, 0, "home", Zones.FromRect(new Rect(w.Map.Hearth.X - 1, w.Map.Hearth.Y - 1, 3, 3))).Apply(w);
        Character oren = Cast(w, "oren");
        Job bed = JobPicker.Choose(w, oren)!;
        Assert.Equal(JobKind.Sleep, bed.Kind);
        Assert.False(Zones.InHome(w, bed.Target));
        WorkSystem.Assign(w, oren);
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.work.out_after_dark", StringComparison.Ordinal));
    }

    /// <summary>Empties the stores and every bush inside the home area, and leaves berries on the bushes outside it.</summary>
    private static void FoodOnlyOutside(World w)
    {
        Stock.Take(w, "berries", 999);
        foreach (Bush b in w.Map.Bushes) b.Berries = Zones.InHome(w, b.Pos) ? 0 : 4;
    }

    [Fact]
    public void TheStarvingGoOutForFoodAndSaySoOnceANightCitingTheirHungerAndTheHomeArea()
    {
        World w = WithHome(3, 23, "oren", "nia");
        FoodOnlyOutside(w);
        Character oren = Cast(w, "oren"), nia = Cast(w, "nia");
        SimEvent starving = w.Log.Emit(w, SystemTag.Needs, "event.needs.starving", new[] { oren.Id }, Array.Empty<long>(), null, EventArg.Char("a", oren.Id));
        oren.Food = 100;
        oren.LastFoodEvent = starving.Id;
        WorkSystem.Assign(w, oren);
        Assert.Equal(JobKind.Forage, oren.Job?.Kind);
        Assert.False(Zones.InHome(w, oren.Job!.Target));
        SimEvent breach = Assert.Single(w.Log.All, e => e.Key == "event.work.out_after_dark_starving");
        Assert.Equal(new[] { Home(w).MadeEvent, starving.Id }, breach.Causes);
        Assert.Equal("Oren, starving, went out after dark to find food.", EventText.Describe(w, breach));
        Assert.Equal(starving.Id, oren.Job.CauseEvent); // the forage's cause stays the hunger: the rule did not send him
        oren.Job = null;
        WorkSystem.Assign(w, oren);
        Assert.Single(w.Log.All, e => e.Key == "event.work.out_after_dark_starving"); // once a night

        // The Desperate count as starving from 220, as the Schedule gets them out of bed.
        nia.Traits = new List<string> { "desperate" };
        nia.Food = 200;
        WorkSystem.Assign(w, nia);
        Assert.Equal(JobKind.Forage, nia.Job?.Kind);
        Assert.Contains(w.Log.All, e => e.Key == "event.work.out_after_dark_starving" && e.Actors[0] == nia.Id);
    }

    [Fact]
    public void TheHungryStayInAsAskedAndThatBecomesTheCauseOfTheirHunger()
    {
        World w = WithHome(3, 20, "oren");
        FoodOnlyOutside(w);
        Character oren = Cast(w, "oren");
        SimEvent hungry = w.Log.Emit(w, SystemTag.Needs, "event.needs.hungry", new[] { oren.Id }, Array.Empty<long>(), null, EventArg.Char("a", oren.Id));
        oren.Food = 250;
        oren.LastFoodEvent = hungry.Id;
        Job? job = JobPicker.Choose(w, oren);
        Assert.NotEqual(JobKind.Forage, job?.Kind);
        Assert.True(job == null || Zones.InHome(w, job.Target));
        SimEvent stayed = Assert.Single(w.Log.All, e => e.Key == "event.work.stayed_in_hungry");
        Assert.Equal(new[] { Home(w).MadeEvent, hungry.Id }, stayed.Causes);
        Assert.Equal(stayed.Id, oren.LastFoodEvent);
        Assert.Equal("Oren, hungry, stayed in camp after dark as you asked.", EventText.Describe(w, stayed));
        JobPicker.Choose(w, oren);
        Assert.Single(w.Log.All, e => e.Key == "event.work.stayed_in_hungry");

        // You keep your own rule: never a roll, and the line in your own words.
        World v = WithHome(3, 20, "you");
        FoodOnlyOutside(v);
        Character you = Cast(v, "you");
        you.Food = 250;
        JobPicker.Choose(v, you);
        Assert.Equal("You, hungry, kept to your own rule and stayed in camp after dark.", EventText.Describe(v, v.Log.All.Single(e => e.Key == "event.work.stayed_in_hungry")));
    }

    [Fact]
    public void SomeoneGoingToTheHurtGoesOutAndSaysSo()
    {
        World w = WithHome(3, 23, "bram", "mira");
        Character bram = Cast(w, "bram"), mira = Cast(w, "mira");
        bram.Traits = new List<string> { "protective" };
        SimEvent hurt = w.Log.Emit(w, SystemTag.Incident, "event.incident.work_accident", new[] { mira.Id }, Array.Empty<long>(), null, EventArg.Char("a", mira.Id));
        mira.Pos = East(w, 10);
        mira.Health = 200;
        mira.Downed = true;
        mira.LastHealthEvent = hurt.Id;
        bram.Rest = 900;
        bram.Job = new Job { Kind = JobKind.Rescue, TargetId = mira.Id, Target = mira.Pos, CauseEvent = hurt.Id };
        Zones.Started(w, bram, bram.Job);
        SimEvent breach = Assert.Single(w.Log.All, e => e.Key == "event.work.out_after_dark_help");
        Assert.Equal(new[] { Home(w).MadeEvent, hurt.Id }, breach.Causes);
        Assert.Equal("Bram went out after dark to help Mira.", EventText.Describe(w, breach));
        Assert.DoesNotContain(w.Members(), m => m.Thoughts.Any(t => t.DefId == "saw_rule_broken")); // helping is no defiance
    }

    [Fact]
    public void ADefiantIndependentGoesOutAndRuleKeepersInSightThinkLessOfThem()
    {
        World w = WithHome(3, 23, "sera", "oren", "mira", "wren");
        Character sera = Cast(w, "sera"), oren = Cast(w, "oren"), mira = Cast(w, "mira"), wren = Cast(w, "wren");
        sera.Traits = new List<string> { "independent" };
        oren.Traits = new List<string> { "oathbound" };
        mira.Traits = new List<string> { "oathbound" };
        foreach (Character c in new[] { oren, mira, wren }) { c.Asleep = false; c.Job = null; }
        sera.Pos = East(w, 2);
        oren.Pos = new Point(sera.Pos.X + Compliance.WitnessRange, sera.Pos.Y);
        mira.Pos = new Point(sera.Pos.X - Compliance.WitnessRange - 1, sera.Pos.Y);
        wren.Pos = new Point(sera.Pos.X, sera.Pos.Y + 1); // keeps no rules herself
        Defiant(w, sera);
        var job = new Job { Kind = JobKind.Forage, Target = East(w, 11) };
        Assert.False(Zones.KeepsIn(w, sera, job.Target));
        Zones.Started(w, sera, job);
        SimEvent breach = Assert.Single(w.Log.All, e => e.Key == "event.work.out_after_dark_own_way");
        Assert.Equal("Never one to be told, Sera went out after dark anyway.", EventText.Describe(w, breach));
        Assert.Equal(new[] { Home(w).MadeEvent }, breach.Causes);
        Thought t = Assert.Single(oren.Thoughts, x => x.DefId == "saw_rule_broken");
        Assert.Equal((sera.Id, breach.Id), (t.AboutId, t.CauseEvent));
        Assert.DoesNotContain(mira.Thoughts, x => x.DefId == "saw_rule_broken"); // nine tiles away
        Assert.DoesNotContain(wren.Thoughts, x => x.DefId == "saw_rule_broken");

        // Going their own way out of dislike cites what they hold against you.
        Character nia = Cast(w, "nia");
        nia.Status = CharStatus.Member;
        nia.Traits = new List<string> { "practical" };
        SimEvent slight = w.Log.Emit(w, SystemTag.Social, "event.social.insulted", new[] { w.PlayerId, nia.Id }, Array.Empty<long>());
        Relations.FormGrudge(w, nia, w.Get(w.PlayerId), 30, slight.Id);
        Defiant(w, nia, Compliance.Dislike);
        Zones.Started(w, nia, new Job { Kind = JobKind.Wander, Target = East(w, 12) });
        SimEvent sour = w.Log.All.Single(e => e.Key == "event.work.out_after_dark_dislike");
        Assert.Equal(new[] { Home(w).MadeEvent, Relations.ActiveGrudge(w, nia.Id, w.PlayerId)!.FormedEvent }, sour.Causes);
        Assert.Equal("Nia went out after dark anyway, caring little for what you want.", EventText.Describe(w, sour));
    }

    [Fact]
    public void AnIndependentKeptFromWorkAfterDarkResentsYouOnceANight()
    {
        World w = WithHome(2, 23, "sera");
        FoodOnlyOutside(w);
        Character sera = Cast(w, "sera");
        sera.Traits = new List<string> { "independent" };
        for (int h = 0; h < 24; h++) ScheduleSystem.SetOwn(sera, h, DayBlock.Work); // on nights: work is hers to do now
        sera.NightRollDay = Zones.Tonight(w); // tonight's roll said she keeps the rule
        int before = Relations.Opinion(w, sera.Id, w.PlayerId);
        Job? job = JobPicker.Choose(w, sera);
        Assert.True(job == null || Zones.InHome(w, job.Target), $"{job?.Kind} to {job?.Target}");
        Thought kept = Assert.Single(sera.Thoughts, t => t.DefId == "kept_in");
        Assert.Equal((w.PlayerId, Home(w).MadeEvent), (kept.AboutId, kept.CauseEvent));
        JobPicker.Choose(w, sera);
        JobPicker.Choose(w, sera);
        Assert.Single(sera.Thoughts, t => t.DefId == "kept_in");
        Assert.Equal(before - 2, Relations.Opinion(w, sera.Id, w.PlayerId));

        // Going her own way tonight, she goes out to work instead, and says so.
        Defiant(w, sera);
        WorkSystem.Assign(w, sera);
        Assert.False(Zones.InHome(w, sera.Job!.Target));
        Assert.Contains(w.Log.All, e => e.Key == "event.work.out_after_dark_own_way");
    }

    [Fact]
    public void ThePlayerNeverRollsAndNobodyIsDefiantBeforeTonightsRoll()
    {
        World w = WithHome(3, 18, "you", "sera", "oren");
        Character you = Cast(w, "you"), sera = Cast(w, "sera"), oren = Cast(w, "oren");
        you.Traits = new List<string> { "independent" };
        you.Mood = 0;
        sera.Traits = new List<string> { "independent" };
        oren.Traits = new List<string> { "oathbound" };
        w.Tick = w.Content.World.TicksPerDay + Hours(w, 19);
        Assert.All(new[] { you, sera, oren }, c => Assert.Null(Zones.DefiantTonight(w, c)));
        DiceStream work = w.Rng.Dice[DiceSystem.Work];
        long draws = work.Draws;
        Zones.Hourly(w);
        Assert.Equal(draws + 1, work.Draws); // only Sera's odds are above 0
        Assert.Equal(DiceId.ZoneDefy, work.Recent[^1]);
        Assert.All(new[] { you, sera, oren }, c => Assert.Equal(Zones.Tonight(w), c.NightRollDay));
        Assert.Equal("", you.DefiesTonight);
        Assert.Equal("", oren.DefiesTonight);

        // Without a home area there is no roll at all.
        World v = WorkTests.Only(1, "sera");
        Cast(v, "sera").Traits = new List<string> { "independent" };
        v.Tick = v.Content.World.TicksPerDay + Hours(v, 19);
        long none = v.Rng.Dice[DiceSystem.Work].Draws;
        Zones.Hourly(v);
        Assert.Equal(none, v.Rng.Dice[DiceSystem.Work].Draws);
        Assert.Equal(-1, Cast(v, "sera").NightRollDay);
    }

    [Fact]
    public void AnIndependentGoesTheirOwnWayOnAboutThreeNightsInTen()
    {
        World w = WithHome(3, 18, "sera");
        Character sera = Cast(w, "sera");
        sera.Traits = new List<string> { "independent" };
        int defiant = 0;
        for (int night = 1; night <= 200; night++)
        {
            w.Tick = (long)night * w.Content.World.TicksPerDay + Hours(w, 19);
            Zones.Hourly(w);
            if (Zones.DefiantTonight(w, sera) is { } why) { defiant++; Assert.Equal(Compliance.OwnWay, why); }
            w.Tick += Hours(w, 12); // the next morning: last night's answer no longer counts
            Assert.Null(Zones.DefiantTonight(w, sera));
        }
        Assert.InRange(defiant, 40, 80);
    }

    // ---- The watch (design 5.5; Schedule's WatchSystem) ----

    /// <summary>A camp of these people (fed, rested, of no people) with the stand-in's short home area: the clearing without the rows from the watch post down.</summary>
    private static Simulation ShortHome(params string[] keys)
    {
        Simulation s = ScheduleCommandTests.Camp(1, keys);
        World w = s.World;
        foreach (Character c in w.Members()) c.Traits = new List<string> { "practical" };
        Rect clearing = Clearing(w);
        Apply(s, new MarkZone(0, 0, "home", Zones.FromRect(new Rect(clearing.X, clearing.Y, clearing.W, w.Map.WatchPost.Y - clearing.Y))));
        Assert.False(Zones.InHome(w, w.Map.WatchPost));
        return s;
    }

    private static void IdleAt(World w, Character c, Point p)
    {
        c.Pos = p;
        c.Path.Clear();
        c.Asleep = false;
        c.Job = new Job { Kind = JobKind.Wander, Target = p, TargetId = 3_000, StartedTick = w.Tick };
    }

    [Fact]
    public void WithThePostOutsideTheHomeAreaNobodyBoundIsAskedAndTheEmptyPostSaysWhy()
    {
        Simulation s = ShortHome("you", "mira", "oren");
        World w = s.World;
        w.Tick = w.Content.World.TicksPerDay + Hours(w, 19) + 30;
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren"), you = Cast(w, "you");
        int i = 0;
        foreach (Character c in new[] { you, mira, oren }) IdleAt(w, c, new Point(w.Map.Hearth.X - 1 + i++, w.Map.Hearth.Y + 1));
        for (int t = 0; t < 120; t++) s.Step();

        SimEvent stayed = Assert.Single(w.Log.All, e => e.Key == "event.work.stayed_in_watch");
        Assert.Equal(new[] { Zones.HomeOf(w)!.MadeEvent }, stayed.Causes);
        Assert.Equal(stayed.Id, w.Watch.GapEvent);
        Assert.Equal(0, w.Watch.WatcherId);
        Assert.DoesNotContain(w.Members(), c => c.Job is { Kind: JobKind.KeepWatch or JobKind.WakeForWatch });
        Assert.DoesNotContain(w.Members(), c => c.Thoughts.Any(x => x.DefId == "woken"));
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.social.watch_refused", StringComparison.Ordinal) || e.Key == "event.work.covered_watch");
        Assert.Matches("^(You|Mira|Oren) would not keep watch outside the home area after dark\\.$", EventText.Describe(w, stayed));

        // A thief who comes now finds the post empty, and the theft says why.
        Stock.Add(w, "berries", 4, w.Map.StartStockpile.Center);
        IncidentSystem.Fire(w, "night_thief");
        SimEvent? stolen = null;
        for (int t = 0; t < 6_000 && stolen == null; t++)
        {
            s.Step();
            stolen = w.Log.All.LastOrDefault(e => e.Key.StartsWith("event.incident.stolen", StringComparison.Ordinal));
        }
        Assert.Equal("event.incident.stolen_unwatched", stolen?.Key);
        Assert.Contains(stayed.Id, stolen!.Causes);
        Assert.Single(w.Log.All, e => e.Key == "event.work.stayed_in_watch"); // once a night for the camp
    }

    [Fact]
    public void TheNightFolkStillKeepAPostOutsideTheHomeArea()
    {
        Simulation s = ShortHome("you", "mira", "vale");
        World w = s.World;
        w.Tick = w.Content.World.TicksPerDay + Hours(w, 19) + 30;
        Character vale = Cast(w, "vale");
        int i = 0;
        foreach (Character c in w.Members()) IdleAt(w, c, new Point(w.Map.Hearth.X - 1 + i++, w.Map.Hearth.Y + 1));
        SimEvent? took = null;
        for (int t = 0; t < 3_000 && took == null; t++)
        {
            s.Step();
            took = w.Log.All.FirstOrDefault(e => e.Key == "event.work.took_watch");
        }
        Assert.Equal(vale.Id, took?.Actors[0]);
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.work.stayed_in_watch");
        Assert.DoesNotContain(w.Log.All, e => e.Key.StartsWith("event.work.out_after_dark", StringComparison.Ordinal));
    }

    [Fact]
    public void AWatcherAtThePostKeepsTheTurnAndAtTheHandoverNobodyBoundIsWoken()
    {
        Simulation s = ShortHome("you", "mira", "oren");
        World w = s.World;
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren");
        w.Tick = w.Content.World.TicksPerDay + Hours(w, 23);
        WatchTests.Asleep(w, mira);
        WatchTests.Asleep(w, oren);
        SimEvent began = WatchTests.OnWatch(w, you, w.Tick + Hours(w, 1)); // already at the post: the turn is theirs to finish
        for (int t = 0; t < (int)Hours(w, 0.9); t++) s.Step();
        Assert.Equal(you.Id, w.Watch.WatcherId);
        Assert.Equal(JobKind.KeepWatch, you.Job?.Kind);

        SimEvent left = RunUntilKey(s, "event.work.left_watch_unrelieved", (int)Hours(w, 1))!;
        SimEvent stayed = w.Log.All.Single(e => e.Key == "event.work.stayed_in_watch");
        Assert.Contains(stayed.Id, left.Causes);
        Assert.Contains(began.Id, left.Causes);
        Assert.Equal(left.Id, w.Watch.GapEvent);
        Assert.DoesNotContain(w.Log.All, e => e.Key == "event.work.went_to_wake");
        Assert.True(mira.Asleep && oren.Asleep);
        Assert.DoesNotContain(new[] { mira, oren }, c => c.Thoughts.Any(x => x.DefId == "woken"));
        Assert.DoesNotContain(w.Debts, d => d.Kind == DebtKind.Watch);
    }

    [Fact]
    public void ADefierStillTakesAPostOutsideAndHasGoneOutAfterDark()
    {
        Simulation s = ShortHome("you", "sera", "oren");
        World w = s.World;
        Character you = Cast(w, "you"), sera = Cast(w, "sera"), oren = Cast(w, "oren");
        w.Tick = w.Content.World.TicksPerDay + Hours(w, 19) + 30;
        sera.Traits = new List<string> { "independent" };
        oren.Traits = new List<string> { "oathbound" };
        foreach (Character c in new[] { you, oren }) IdleAt(w, c, new Point(w.Map.Hearth.X - 2, w.Map.Hearth.Y + 1));
        IdleAt(w, sera, new Point(w.Map.Hearth.X - 1, w.Map.Hearth.Y + 1));
        Defiant(w, sera);
        SimEvent? took = null;
        for (int t = 0; t < 3_000 && took == null; t++)
        {
            s.Step();
            took = w.Log.All.FirstOrDefault(e => e.Key == "event.work.took_watch");
        }
        Assert.Equal(sera.Id, took?.Actors[0]);
        SimEvent breach = w.Log.All.Single(e => e.Key == "event.work.out_after_dark_own_way");
        Assert.Equal(sera.Id, breach.Actors[0]);
        Assert.Contains(oren.Thoughts, x => x.DefId == "saw_rule_broken" && x.AboutId == sera.Id); // he saw her go from the fire
    }

    private static SimEvent? RunUntilKey(Simulation s, string key, int maxTicks)
    {
        int seen = s.World.Log.All.Count;
        for (int t = 0; t < maxTicks; t++)
        {
            s.Step();
            for (; seen < s.World.Log.All.Count; seen++)
                if (s.World.Log.All[seen].Key == key) return s.World.Log.All[seen];
        }
        return null;
    }
}
