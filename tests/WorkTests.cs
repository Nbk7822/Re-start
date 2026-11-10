using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Stories;
using Return.Sim.Systems;

namespace Return.Tests;

public class WorkTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>A camp where only the named cast members are active, fed and rested, near the hearth.</summary>
    internal static World Only(ulong seed, params string[] keys)
    {
        World w = WorldGen.Create(TestContent.Db, seed);
        int i = 0;
        foreach (Character c in w.Characters)
        {
            if (!keys.Contains(c.CastKey)) { c.Status = CharStatus.OffMap; continue; }
            c.Status = CharStatus.Member;
            c.Food = 900; c.Rest = 900; c.Social = 900; c.Mood = 60;
            if (!w.Map.Passable(c.Pos)) c.Pos = new Point(w.Map.Hearth.X - 2 + i, w.Map.Hearth.Y + 2);
            i++;
        }
        return w;
    }

    private static void Run(World w, int ticks, Func<bool>? until = null)
    {
        for (int t = 0; t < ticks; t++)
        {
            TravelSystem.Step(w);
            WorkSystem.Step(w);
            w.Tick++;
            if (until != null && until()) return;
        }
    }

    [Fact]
    public void HungryHumanForagesWhenNoFood()
    {
        World w = Only(1, "you");
        Stock.Take(w, "berries", 99);
        Character you = Cast(w, "you");
        you.Food = 250;
        w.Tick = 10L * w.Content.World.TicksPerHour; // in waking hours; at night only real hunger sends anyone out (ADR-0029)
        Job? job = JobPicker.Choose(w, you);
        Assert.Equal(JobKind.Forage, job?.Kind);
    }

    [Fact]
    public void ForagedBerriesReachTheStockpile()
    {
        World w = Only(1, "you");
        Stock.Take(w, "berries", 99);
        Character you = Cast(w, "you");
        you.Food = 600;
        Run(w, 4_000, () => Stock.Count(w, "berries") > 0);
        Assert.True(Stock.Count(w, "berries") > 0);
        Assert.Contains(w.Log.All, e => e.Key == "event.work.foraged");
    }

    [Fact]
    public void BuilderRepairsRoofWithTimber()
    {
        World w = Only(2, "oren");
        Stock.Add(w, "timber", 4, w.Map.StartStockpile.Center);
        int timber = Stock.Count(w, "timber");
        Run(w, 6_000, () => w.Map.Roof.Any(r => r.Sound));
        Assert.Contains(w.Map.Roof, r => r.Sound);
        Assert.Equal(timber - 3, Stock.Count(w, "timber"));
        Assert.Contains(w.Log.All, e => e.Key == "event.work.roof_repaired");
        Assert.Contains(Cast(w, "oren").Thoughts, t => t.DefId == "roof_sound");
    }

    [Fact]
    public void VampireSeeksCoverAtDawn()
    {
        World w = Only(1, "ilex");
        Character ilex = Cast(w, "ilex");
        ilex.Pos = w.Map.StartStockpile.Center;
        w.Tick = 6 * w.Content.World.TicksPerHour;
        Assert.Equal(JobKind.SeekCover, JobPicker.Choose(w, ilex)?.Kind);
        Run(w, 600, () => w.Map.Covered(ilex.Pos));
        Assert.True(w.Map.Covered(ilex.Pos));
    }

    [Fact]
    public void ConsentingDonorCreatesDebt()
    {
        World w = Only(1, "ilex", "mira");
        w.Tick = 22 * w.Content.World.TicksPerHour;
        Character ilex = Cast(w, "ilex"), mira = Cast(w, "mira");
        ilex.Food = 250;
        SimEvent thirst = w.Log.Emit(w, SystemTag.Needs, "event.needs.thirsting", new[] { ilex.Id }, Array.Empty<long>());
        ilex.LastFoodEvent = thirst.Id;
        Relations.ChangeOpinion(w, ilex, mira.Id, 20, 0);
        Relations.ChangeOpinion(w, mira, ilex.Id, 20, 0);
        Run(w, 2_000, () => ilex.Food > 600);
        Assert.True(ilex.Food > 600);
        SimEvent debt = w.Log.All.Single(e => e.Effects.Any(f => f.Kind == EffectKind.DebtFormed && f.A == ilex.Id && f.B == mira.Id));
        HashSet<long> ancestors = NeedsMoodTests.Ancestors(w, debt.Id);
        Assert.Contains(thirst.Id, ancestors);
        Assert.Contains(w.Log.All, e => e.Key == "event.social.blood_request" && ancestors.Contains(e.Id));
        Assert.Contains(w.Log.All, e => e.Key == "event.work.gave_blood" && ancestors.Contains(e.Id));
        Assert.True(mira.Health < 1000);
    }

    [Fact]
    public void RefusalIsRemembered()
    {
        World w = Only(1, "ilex", "mira");
        w.Tick = 22 * w.Content.World.TicksPerHour;
        Character ilex = Cast(w, "ilex"), mira = Cast(w, "mira");
        mira.CultureId = ""; mira.FaithId = ""; // no people's stance or blood custom moves her consent (ADR-0023)
        ilex.Food = 250;
        Relations.ChangeOpinion(w, mira, ilex.Id, -60, 0);
        Run(w, 2_000, () => w.Log.All.Any(e => e.Key == "event.social.blood_refused"));
        Assert.Contains(w.Log.All, e => e.Key == "event.social.blood_refused");
        Assert.Contains(ilex.Thoughts, t => t.DefId == "was_refused" && t.AboutId == mira.Id);
        Assert.Contains(mira.Thoughts, t => t.DefId == "refused_someone" && t.AboutId == ilex.Id);
    }

    [Fact]
    public void ProtectiveCaresForInjured()
    {
        World w = Only(1, "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        w.Tick = 8L * w.Content.World.TicksPerHour; // waking hours: nobody leaves their bed at midnight to tend a wound (ADR-0029)
        oren.Health = 300;
        Run(w, 2_000, () => oren.Health > 450);
        Assert.True(oren.Health > 450);
        Assert.Contains(w.Log.All, e => e.Effects.Any(f => f.Kind == EffectKind.DebtFormed && f.A == oren.Id && f.B == mira.Id));
        Assert.Contains(oren.Thoughts, t => t.DefId == "cared_for" && t.AboutId == mira.Id);
    }

    [Fact]
    public void DesperateTakesReservedMeal()
    {
        World w = Only(1, "nia", "mira");
        Character nia = Cast(w, "nia"), mira = Cast(w, "mira");
        Stock.Take(w, "berries", 99);
        Stock.Add(w, "berries", 1, w.Map.StartStockpile.Center);
        nia.Pos = w.Map.StartStockpile.Center;
        mira.Pos = w.Map.WatchPost;
        SimEvent spoiled = w.Log.Emit(w, SystemTag.Incident, "event.test.spoiled", Array.Empty<int>(), Array.Empty<long>());
        nia.LastFoodEvent = spoiled.Id;
        nia.Food = 200;
        mira.Food = 100;
        NeedsSystem.Hourly(w);
        Rations.Assign(w);
        Assert.True(Rations.HasReservation(w, mira.Id));
        Run(w, 2_000, () => w.Grudges.Any());
        SimEvent theft = w.Log.All.Single(e => e.Key == "event.social.took_meal");
        Assert.Equal(new[] { nia.Id, mira.Id }, theft.Actors);
        Grudge g = Assert.Single(w.Grudges);
        Assert.Equal(mira.Id, g.HolderId);
        Story story = StoryAnalyzer.Find(w.Log.All, id => w.Find(id)?.Named == true).Single(s => s.TerminalEventId == g.FormedEvent);
        Assert.True(story.Chain.Count >= 3);
        Assert.Contains(SystemTag.Needs, story.Systems);
        Assert.Contains(SystemTag.Social, story.Systems);
    }

    /// <summary>At a watch hour one awake, willing person is sent to the post within one WatchSystem step; the job picker never sends anyone (ADR-0029).</summary>
    [Fact]
    public void WatchAtNight()
    {
        World w = Only(1, "vale");
        Character vale = Cast(w, "vale");
        w.Tick = 22 * w.Content.World.TicksPerHour;
        Assert.NotEqual(JobKind.KeepWatch, JobPicker.Choose(w, vale)?.Kind);
        vale.Job = null;
        WatchSystem.Step(w);
        Assert.Equal(JobKind.KeepWatch, vale.Job?.Kind);
        Assert.Equal(vale.Id, w.Watch.IncomingId);
        Run(w, 600, () => w.Watch.WatcherId == vale.Id);
        Assert.Equal(vale.Id, w.Watch.WatcherId);
        Assert.Contains(w.Log.All, e => e.Key == "event.work.took_watch" && e.Actors[0] == vale.Id);
    }

    [Fact]
    public void PriorityCommandChangesChoice()
    {
        var s = Simulation.Create(TestContent.Db, 1);
        World w = s.World;
        foreach (Character c in w.Characters) if (c.CastKey != "mira") c.Status = CharStatus.OffMap;
        Character mira = Cast(w, "mira");
        mira.Food = 900; mira.Rest = 900; mira.Social = 900;
        Stock.Take(w, "berries", 99);
        w.Items.Add(new ItemStack { Id = w.NewId(), DefId = "timber", Count = 2, Pos = new Point(w.Map.Hearth.X, w.Map.Hearth.Y + 2) });
        w.Tick = 10 * w.Content.World.TicksPerHour;
        Assert.Equal(JobKind.Forage, JobPicker.Choose(w, mira)?.Kind);
        s.Enqueue(new SetWorkPriority(w.Tick, mira.Id, "forage", 4));
        s.Enqueue(new SetWorkPriority(w.Tick, mira.Id, "haul", 1));
        mira.Job = null;
        s.Step();
        Assert.Equal(JobKind.Haul, mira.Job?.Kind);
    }
}
