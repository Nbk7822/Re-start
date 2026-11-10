using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Systems;
using Return.Sim.Systems.Jobs;

namespace Return.Tests;

/// <summary>The stockpile as a zone (ADR-0028): what counts as stores, where goods go, and that moving goods never loses them.</summary>
public class StoresTests
{
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    [Fact]
    public void TheCampStartsWithItsStockpileAsAZoneKeepingEveryItem()
    {
        World w = WorldGen.Create(TestContent.Db, 3);
        Zone pile = Assert.Single(w.Map.Zones, z => z.Kind == "stockpile");
        Assert.Equal(Zones.FromRect(w.Map.StartStockpile), pile.Tiles);
        Assert.Equal(new[] { "berries", "timber" }, pile.Items);
        Assert.Equal(0, pile.MadeEvent);
        Assert.All(w.Items, s => Assert.True(s.Stored, $"start stack {s.DefId} should be stores"));
        Assert.True(Zones.Accepts(w, w.Map.StartStockpile.Center, "berries"));
        Assert.False(Zones.Accepts(w, w.Map.Hearth, "berries"));
    }

    [Fact]
    public void BerriesPutDownOnAStockpileAreStoresAndLooseOnesAreNot()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        int before = Stock.Count(w, "berries");
        ItemStack kept = Stock.Add(w, "berries", 2, w.Map.StartStockpile.Center);
        ItemStack loose = Stock.Add(w, "berries", 3, new Point(w.Map.Hearth.X + 3, w.Map.Hearth.Y + 3));
        Assert.True(kept.Stored);
        Assert.False(loose.Stored);
        Assert.Equal(before + 2, Stock.Count(w, "berries"));
    }

    [Fact]
    public void AStoredLoadDroppedOnTheWayStaysStoresAndNeverMergesWithLooseGoods()
    {
        World w = WorldGen.Create(TestContent.Db, 1);
        var off = new Point(w.Map.Hearth.X + 3, w.Map.Hearth.Y + 3);
        ItemStack loose = Stock.Add(w, "timber", 1, off);
        ItemStack carried = Stock.Add(w, "timber", 2, off, stored: true);
        Assert.NotSame(loose, carried);
        Assert.True(carried.Stored);
        Assert.False(loose.Stored);
    }

    [Fact]
    public void AHaulerCarryingStoresKeepsThemStoresWhenTheLoadIsDropped()
    {
        World w = WorkTests.Only(1, "oren");
        Character oren = Cast(w, "oren");
        var off = new Point(w.Map.Hearth.X + 3, w.Map.Hearth.Y + 3);
        ItemStack waiting = Stock.Add(w, "timber", 2, off, stored: true);
        oren.Pos = off;
        oren.Job = new Job { Kind = JobKind.Haul, TargetId = waiting.Id, Target = off };
        Assert.False(CampJobs.Haul(w, oren, oren.Job));
        Assert.True(oren.Carrying is { Stored: true, DefId: "timber", Count: 2 });
        int stores = Stock.Count(w, "timber");
        WorkSystem.Drop(w, oren);
        Assert.Equal(stores + 2, Stock.Count(w, "timber"));
        Assert.Contains(w.Items, s => s.Pos == off && s.DefId == "timber" && s.Stored);
    }

    [Fact]
    public void CanonicalRunsAreSortedMergedAndFreeOfDuplicates()
    {
        var tiles = new[] { new Point(3, 2), new Point(1, 1), new Point(2, 1), new Point(2, 1), new Point(5, 2), new Point(4, 2) };
        Assert.Equal(new[] { new TileRun(1, 1, 2), new TileRun(2, 3, 3) }, Zones.Canonical(tiles));
        Assert.Equal(5, Zones.Count(Zones.Canonical(tiles)));
        Assert.Equal(new Rect(1, 1, 5, 2), Zones.Bounds(Zones.Canonical(tiles)));
    }

    [Fact]
    public void ZonesAndStoresSurviveASnapshot()
    {
        var sim = Simulation.Create(TestContent.Db, 6);
        sim.Advance(3_000);
        Simulation r = Simulation.Restore(TestContent.Db, sim.Snapshot());
        Assert.Equal(sim.World.Map.Zones.Count, r.World.Map.Zones.Count);
        Assert.Equal(sim.World.Map.Zones[0].Tiles, r.World.Map.Zones[0].Tiles);
        Assert.Equal(sim.World.Items.Select(s => s.Stored), r.World.Items.Select(s => s.Stored));
        Assert.True(Zones.Accepts(r.World, r.World.Map.StartStockpile.Center, "timber"));
        Assert.Equal(sim.StateHash(), r.StateHash());
    }
}
