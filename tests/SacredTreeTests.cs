using Return.Sim;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;
using Return.Sim.Systems.Jobs;
using static Return.Tests.CultureHookTests;

namespace Return.Tests;

/// <summary>The sacred tree (P1, P2; ADR-0023): its keepers spare it, anyone else may fell it, and the faithful mourn it.</summary>
public class SacredTreeTests
{
    /// <summary>Only the sacred tree still stands; Mira and Nia keep the Old Roots, Oren has long stopped praying.</summary>
    private static (World W, Tree Tree, SacredPlace Place, Character Mira, Character Oren, Character Nia) Grove()
    {
        World w = Camp(TestContent.Db, 1, "mira", "oren", "nia");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren"), nia = Cast(w, "nia");
        SacredPlace place = Assert.Single(w.Culture.SacredPlaces);
        Assert.Equal("roots", place.HolderId);
        Assert.True(place.HolderIsFaith);
        Tree tree = w.Map.Trees.Single(t => t.Id == place.TreeId);
        foreach (Tree t in w.Map.Trees) if (t != tree) t.Felled = true;
        w.Map.RebuildCaches();
        Belong(mira, "night", "roots", 60);
        Belong(nia, "night", "roots", 80);
        Belong(oren, "night", "roots", 10);
        return (w, tree, place, mira, oren, nia);
    }

    private static SimEvent Fell(World w, Character feller, Tree tree)
    {
        var job = new Job { Kind = JobKind.GatherTimber, TargetId = tree.Id, Target = Places.Adjacent(w, tree.Pos, feller.Pos) };
        feller.Job = job;
        for (int t = 0; t < 5_000 && !tree.Felled; t++)
        {
            TravelSystem.Step(w);
            if (CampJobs.GatherTimber(w, feller, job)) break;
            w.Tick++;
        }
        Assert.True(tree.Felled);
        return w.Log.All.Last(e => e.Key == "event.work.felled_tree");
    }

    [Fact]
    public void TheFaithfulSpareTheSacredTreeAndMournIt()
    {
        (World w, Tree tree, SacredPlace place, Character mira, Character oren, Character nia) = Grove();
        Assert.Null(Places.StandingTree(w, mira.Pos, mira));
        Assert.Null(Places.StandingTree(w, nia.Pos, nia));
        Assert.Equal(tree, Places.StandingTree(w, oren.Pos, oren));
        Assert.Equal(tree, Places.StandingTree(w, oren.Pos)); // no feller named: any tree

        SimEvent felled = Fell(w, oren, tree);
        SimEvent desecrated = w.Log.All.Single(e => e.Key == "event.culture.sacred_grove.desecrated");
        Assert.Equal(SystemTag.Culture, desecrated.System);
        Assert.Equal(new[] { oren.Id }, desecrated.Actors);
        Assert.Equal(new[] { felled.Id }, desecrated.Causes);
        Assert.True(place.Desecrated);
        Assert.Equal(desecrated.Id, place.DesecratedEvent);
        string roots = w.Content.Loc.Get(Ways.FaithById(w, "roots")!.NameKey);
        Assert.Equal("Oren felled the old tree sacred to " + roots + ".", Return.Sim.Api.EventText.Describe(w, desecrated));
        foreach (Character k in new[] { mira, nia })
        {
            Assert.Contains(k.Thoughts, t => t.DefId == "sacred_tree_felled" && t.AboutId == oren.Id && t.CauseEvent == desecrated.Id);
            Grudge g = Relations.ActiveGrudge(w, k.Id, oren.Id)!;
            Assert.Equal(desecrated.Id, g.CauseEvent);
            Assert.Equal(CultureTuning.SacrilegeGrudge, g.Intensity);
        }
        Assert.Contains(w.Culture.Deeds, d => d.Kind == DeedKind.SacredHarmed && d.EventId == desecrated.Id && d.DoerId == oren.Id);
        Assert.Contains(w.Culture.Deeds, d => d.Kind == DeedKind.TreeFelled && d.EventId == felled.Id);
        Assert.Contains("Stump (once sacred to " + roots + ")", Return.Sim.Api.StateApi.Location(w, tree.Pos.X, tree.Pos.Y).Contents);
    }

    [Fact]
    public void UnwitnessedTheFellingIsMarkedSilently()
    {
        (World w, Tree tree, SacredPlace place, Character mira, Character oren, Character nia) = Grove();
        mira.Status = CharStatus.OffMap;
        nia.Status = CharStatus.OffMap;
        SimEvent felled = Fell(w, oren, tree);
        Assert.True(place.Desecrated);
        Assert.Equal(0, place.DesecratedEvent);
        Assert.DoesNotContain(w.Log.All, e => e.System == SystemTag.Culture);
        Assert.Contains(w.Culture.Deeds, d => d.Kind == DeedKind.SacredHarmed && d.EventId == felled.Id);
    }

    [Fact]
    public void ACultureMayHoldATreeSacredToo()
    {
        (World w, Tree tree, SacredPlace place, Character mira, Character oren, _) = Grove();
        place.HolderId = "night";
        place.HolderIsFaith = false;
        Belong(mira, "night"); // culture customs bind every member, devout or not
        Belong(oren, "reed");
        Assert.True(SacredTrees.Spares(w, mira, tree));
        Assert.False(SacredTrees.Spares(w, oren, tree));
    }
}
