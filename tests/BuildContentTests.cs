using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Tests;

/// <summary>buildings.json (ADR-0027): what loads, what is rejected at load and why, and the ids the code relies on.</summary>
public class BuildContentTests
{
    private static string ContentDir => Path.Combine(TestPaths.Root, "content");

    [Fact]
    public void EveryBuildingHasItsFourLocFormsAndOneMaterial()
    {
        ContentDb db = TestContent.Db;
        Assert.Equal(new[] { "wall", "door", "roof", "bed" }, db.Buildings.Select(b => b.Id));
        foreach (BuildingDef b in db.Buildings)
        {
            foreach (string key in new[] { b.NameKey, b.DescKey, b.AKey, b.PluralKey })
                Assert.True(db.Loc.Has(key), $"{b.Id} has no '{key}'");
            Assert.Single(b.Materials);
            Assert.NotNull(db.TryItem(b.Material.Item));
        }
        foreach ((string id, string kind) in RequiredContent.Buildings)
            Assert.Equal(kind, db.BuildingById(id).Kind);
    }

    [Fact]
    public void CostsAndWorkFollowTheDesign()
    {
        ContentDb db = TestContent.Db;
        var tile = new Rect(0, 0, 1, 1);
        Assert.Equal(1, db.BuildingById("wall").Cost(tile));
        Assert.Equal(2, db.BuildingById("door").Cost(tile));
        Assert.Equal(2, db.BuildingById("bed").Cost(tile));
        BuildingDef roof = db.BuildingById("roof");
        Assert.Equal(3, roof.Cost(new Rect(0, 0, 2, 3))); // as the old repair costs
        Assert.Equal(600, roof.WorkFor(new Rect(0, 0, 2, 3))); // as the old repair takes
        Assert.Equal(1, roof.Cost(new Rect(0, 0, 1, 1))); // rounded up
        Assert.Equal(8, roof.Cost(new Rect(0, 0, 4, 4)));
        Assert.Equal(1, db.BuildingById("bed").ReturnedOnTakeDown(tile));
        Assert.Equal(0, db.BuildingById("wall").ReturnedOnTakeDown(tile)); // half of one timber, rounded down
        Assert.Equal(225, db.BuildingById("bed").TakeDownWorkFor(tile));
    }

    [Fact]
    public void AKindWithNoBehaviourIsRejectedAtLoad()
    {
        // The schema keeps "grave" among the kinds, but its behaviour comes with laying the dead to rest (build design 4.8).
        string mod = ModDir("buildings.json",
            "[{ \"id\": \"grave\", \"nameKey\": \"building.bed.name\", \"descKey\": \"building.bed.desc\", \"kind\": \"grave\", \"category\": \"rites\"," +
            "  \"materials\": [{ \"item\": \"timber\", \"count\": 1 }], \"workTicks\": 600, \"takeDownPct\": 0, \"placeOn\": [\"grass\", \"soil\"] }]");
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(ContentDir, new[] { mod }));
        Assert.Contains("buildings.json", ex.Message);
        Assert.Contains("grave", ex.Message);
    }

    [Fact]
    public void AModCanAddABuildingOfAnExistingKind()
    {
        string mod = ModDir("buildings.json",
            "[{ \"id\": \"plank_bed\", \"nameKey\": \"building.bed.name\", \"descKey\": \"building.bed.desc\", \"kind\": \"bed\", \"category\": \"rest\"," +
            "  \"materials\": [{ \"item\": \"timber\", \"count\": 3 }], \"workTicks\": 600, \"takeDownPct\": 50, \"restBonusPct\": 30, \"placeOn\": [\"floor\"] }]");
        File.WriteAllText(Path.Combine(Directory.CreateDirectory(Path.Combine(mod, "loc")).FullName, "en.json"),
            "{ \"building.plank_bed.a\": \"a plank bed\", \"building.plank_bed.plural\": \"plank beds\" }");
        ContentDb db = ContentDb.Load(ContentDir, new[] { mod });
        Assert.Equal(5, db.Buildings.Count);
        Assert.Equal(3, db.BuildingById("plank_bed").Cost(new Rect(0, 0, 1, 1)));
    }

    [Fact]
    public void ABuildingWithoutItsArticleFormIsRejected()
    {
        string mod = ModDir("buildings.json",
            "[{ \"id\": \"plank_bed\", \"nameKey\": \"building.bed.name\", \"descKey\": \"building.bed.desc\", \"kind\": \"bed\", \"category\": \"rest\"," +
            "  \"materials\": [{ \"item\": \"timber\", \"count\": 3 }], \"workTicks\": 600, \"takeDownPct\": 50, \"restBonusPct\": 30, \"placeOn\": [\"floor\"] }]");
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(ContentDir, new[] { mod }));
        Assert.Contains("building.plank_bed.a", ex.Message);
    }

    [Fact]
    public void TwoMaterialsAreRejectedUntilTheItemsMilestone()
    {
        string mod = ModDir("buildings.json",
            "[{ \"id\": \"bed\", \"nameKey\": \"building.bed.name\", \"descKey\": \"building.bed.desc\", \"kind\": \"bed\", \"category\": \"rest\"," +
            "  \"materials\": [{ \"item\": \"timber\", \"count\": 2 }, { \"item\": \"berries\", \"count\": 1 }], \"workTicks\": 450, \"takeDownPct\": 50, \"restBonusPct\": 20, \"placeOn\": [\"floor\"] }]");
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(ContentDir, new[] { mod }));
        Assert.Contains("exactly one material", ex.Message);
    }

    [Fact]
    public void ABuildingTheCodeReliesOnMustKeepItsKind()
    {
        string mod = ModDir("buildings.json",
            "[{ \"id\": \"door\", \"nameKey\": \"building.door.name\", \"descKey\": \"building.door.desc\", \"kind\": \"wall\", \"category\": \"shelter\"," +
            "  \"materials\": [{ \"item\": \"timber\", \"count\": 2 }], \"workTicks\": 450, \"takeDownPct\": 50, \"placeOn\": [\"grass\"] }]");
        var ex = Assert.Throws<ContentException>(() => ContentDb.Load(ContentDir, new[] { mod }));
        Assert.Contains("'door'", ex.Message);
        Assert.Contains("kind 'door'", ex.Message);
    }

    private static string ModDir(string file, string json)
    {
        string mod = Path.Combine(Path.GetTempPath(), "return-mod-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(mod);
        File.WriteAllText(Path.Combine(mod, file), json);
        return mod;
    }
}
