using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Jobs;

namespace Return.Tests;

/// <summary>Beds and who owns them (build design 4.6; ADR-0027).</summary>
public class BedTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static int Hours(World w, int h) => h * w.Content.World.TicksPerHour;

    /// <summary>Night, everyone named tired enough to turn in.</summary>
    private static World Night(params string[] keys)
    {
        World w = WorkTests.Only(1, keys);
        w.Tick = Hours(w, 23);
        foreach (string k in keys) Cast(w, k).Rest = 300;
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
    public void LyingDownInAnUnclaimedBedClaimsIt()
    {
        World w = Night("you", "mira", "oren");
        Run(w, 2_000, () => w.Map.Beds.Count(b => b.OwnerId != 0) == 3);
        Assert.Equal(3, w.Map.Beds.Count(b => b.OwnerId != 0));
        foreach (string k in new[] { "you", "mira", "oren" })
        {
            Character c = Cast(w, k);
            Bed bed = Building.OwnedBed(w, c.Id)!;
            Assert.Equal(bed.Pos, c.Pos);
            SimEvent claim = w.Log.Get(bed.ClaimEvent)!;
            Assert.Equal("event.social.claimed_bed", claim.Key);
            Assert.Equal(SystemTag.Social, claim.System);
            Assert.Empty(claim.Effects); // a claim is not a story's ending
        }
        Assert.Equal("You claimed a bed.", EventText.Describe(w, w.Log.Get(Building.OwnedBed(w, w.PlayerId)!.ClaimEvent)!));
    }

    [Fact]
    public void NobodySleepsInSomeoneElsesBed()
    {
        World w = Night("you", "mira");
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        // Every bed but one is somebody's already: one of them takes the free one, and the other, with none left, sleeps elsewhere.
        foreach (Bed b in w.Map.Beds.Skip(1)) b.OwnerId = 999;
        Point free = w.Map.Beds[0].Pos;
        Assert.Equal(free, Places.SleepSpot(w, mira));
        Run(w, 2_000, () => mira.Asleep && you.Asleep);
        Character owner = w.Get(w.Map.Beds[0].OwnerId), other = owner == you ? mira : you;
        Assert.Equal(free, owner.Pos);
        Assert.True(other.Asleep);
        Assert.DoesNotContain(w.Map.Beds, b => b.Pos == other.Pos);
        Assert.Null(Building.OwnedBed(w, other.Id));
        // Nor does the one with a bed pick anyone else's, even the nearest.
        Assert.Equal(free, Places.SleepSpot(w, owner));
    }

    [Fact]
    public void ABedRestoresRestFasterAndWakingInItIsAComfort()
    {
        World w = WorkTests.Only(1, "you", "mira");
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        you.Pos = w.Map.Beds[0].Pos;
        mira.Pos = BuildPlanTests.ShelterFloor(w);
        you.Asleep = mira.Asleep = true;
        you.Rest = mira.Rest = 200;
        NeedsSystem.Hourly(w);
        int perHour = w.Content.SpeciesById("human").SleepRestorePerHour;
        Assert.Equal(200 + perHour + perHour * 20 / 100, you.Rest);
        Assert.Equal(200 + perHour, mira.Rest);

        w.Map.Beds[0].OwnerId = you.Id;
        Building.Woke(w, you);
        Building.Woke(w, mira);
        Assert.Contains(you.Thoughts, t => t.DefId == "slept_in_own_bed");
        Assert.DoesNotContain(you.Thoughts, t => t.DefId == "no_bed");
        Assert.Contains(mira.Thoughts, t => t.DefId == "no_bed");
    }

    [Fact]
    public void WhoeverMendedTheRoofIsThankedByThoseWhoSleepUnderItOnceADay()
    {
        World w = WorkTests.Only(1, "you", "oren");
        Character you = Cast(w, "you"), oren = Cast(w, "oren");
        RoofSection r = w.Map.Roof[0];
        r.Sound = true;
        r.MendedBy = oren.Id;
        you.Pos = new Point(r.Area.X, r.Area.Y + 1);
        int before = Relations.Opinion(w, you.Id, oren.Id);
        Building.Woke(w, you);
        Building.Woke(w, you); // the same day: once is enough
        Assert.Single(you.Thoughts, t => t.DefId == "sheltered_by" && t.AboutId == oren.Id);
        Assert.Equal(before + w.Content.ThoughtById("sheltered_by").Opinion, Relations.Opinion(w, you.Id, oren.Id));
        // Oren thanks nobody for his own work.
        oren.Pos = you.Pos;
        Building.Woke(w, oren);
        Assert.DoesNotContain(oren.Thoughts, t => t.DefId == "sheltered_by");
    }

    [Fact]
    public void WhoeverLeavesOrDiesLeavesTheirBedUnclaimedAndItRemembersWhose()
    {
        World w = Night("you", "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        Run(w, 2_000, () => w.Map.Beds.Count(b => b.OwnerId != 0) == 3);
        Bed miras = Building.OwnedBed(w, mira.Id)!;
        int events = w.Log.All.Count;
        SocialSystem.Leave(w, mira, w.Log.All[0].Id);
        Assert.Equal(0, miras.OwnerId);
        Assert.Equal(mira.Id, miras.PrevOwnerId);
        Assert.Equal(events + 1, w.Log.All.Count); // only the departure itself

        // The next to lie down in it, with no bed of their own, claims the bed that was Mira's.
        Bed orens = Building.OwnedBed(w, oren.Id)!;
        orens.OwnerId = 0;
        oren.Pos = miras.Pos;
        Assert.True(Building.LieDown(w, oren));
        SimEvent claim = w.Log.All.Last();
        Assert.Equal("event.social.claimed_bed_of", claim.Key);
        Assert.Equal("Oren claimed the bed that was Mira's.", EventText.Describe(w, claim));
        Lifecycle.Die(w, oren, SystemTag.Needs, "event.needs.died", w.Log.All[0].Id);
        Assert.Equal(oren.Id, miras.PrevOwnerId);
        Assert.Equal(0, miras.OwnerId);
    }

    [Fact]
    public void ABedClaimedWhileSomeoneWalkedToItSendsThemElsewhere()
    {
        World w = WorkTests.Only(1, "you", "mira");
        Character you = Cast(w, "you");
        Bed bed = w.Map.Beds[0];
        bed.OwnerId = Cast(w, "mira").Id;
        you.Pos = bed.Pos;
        Assert.False(Building.LieDown(w, you));
        Assert.Equal(Cast(w, "mira").Id, bed.OwnerId);
        var j = new Job { Kind = JobKind.Sleep, Target = bed.Pos };
        Assert.True(NeedsJobs.Sleep(w, you, j));
        Assert.False(you.Asleep);
    }

    [Fact]
    public void ClaimsSpeakToThePlayerInTheSecondPerson()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        int you = w.PlayerId, mira = Cast(w, "mira").Id, oren = Cast(w, "oren").Id;
        string Say(string key, params EventArg[] args) => EventText.Describe(w, new SimEvent { Key = key, Args = args.ToList() });
        Assert.Equal("You claimed the bed you built.", Say("event.social.claimed_own_bed", EventArg.Char("a", you)));
        Assert.Equal("Mira claimed the bed they built.", Say("event.social.claimed_own_bed", EventArg.Char("a", mira)));
        Assert.Equal("Mira claimed the bed you built.", Say("event.social.claimed_bed_built", EventArg.Char("a", mira), EventArg.Char("b", you)));
        Assert.Equal("Mira claimed the bed that was yours.", Say("event.social.claimed_bed_of", EventArg.Char("a", mira), EventArg.Char("b", you)));
        Assert.Equal("Oren took down your bed.", Say("event.work.took_down_bed_of", EventArg.Char("a", oren), EventArg.Char("b", you)));
        Assert.Equal("You took down Mira's bed.", Say("event.work.took_down_bed_of", EventArg.Char("a", you), EventArg.Char("b", mira)));
    }
}
