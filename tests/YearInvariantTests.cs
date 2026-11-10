using Return.Sim;
using Return.Sim.Events;

namespace Return.Tests;

/// <summary>Review Focus 1: a whole year of unattended play keeps every value in range and every cause behind its effect.</summary>
public class YearInvariantTests
{
    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void FullYearKeepsInvariants(ulong seed)
    {
        var s = Simulation.Create(TestContent.Db, seed);
        int perHour = s.World.Content.World.TicksPerHour;
        long year = s.World.Content.World.TicksPerYear;
        int checkedEvents = 0;
        // A return sends the world back and hands the simulation the restored world (ADR-0030), so the year runs to its
        // last tick rather than for a fixed number of hours, and the world is read afresh after every hour.
        for (long hours = 0; s.World.Tick < year && !s.World.Fate.Ended; hours++)
        {
            Assert.True(hours < 4 * year / perHour, $"seed {seed}: the year never ended");
            s.Advance(Math.Min(perHour, year - s.World.Tick));
            World w = s.World;
            checkedEvents = Math.Min(checkedEvents, w.Log.All.Count);
            foreach (Character c in w.Characters)
            {
                if (c.Status is not (CharStatus.Member or CharStatus.Hostile)) continue;
                string who = $"character {c.Id} ({c.CastKey}) at tick {w.Tick}";
                Assert.True(c.Food is >= 0 and <= 1000, $"{who}: food {c.Food}");
                Assert.True(c.Rest is >= 0 and <= 1000, $"{who}: rest {c.Rest}");
                Assert.True(c.Social is >= 0 and <= 1000, $"{who}: social {c.Social}");
                Assert.True(c.Health is > 0 and <= 1000, $"{who}: health {c.Health}");
                Assert.True(c.Mood is >= 0 and <= 100, $"{who}: mood {c.Mood}");
                Assert.True(w.Map.Passable(c.Pos), $"{who}: standing on impassable {c.Pos}");
                foreach (Opinion o in c.Opinions)
                    Assert.True(o.Value is >= -100 and <= 100, $"{who}: opinion of {o.TargetId} is {o.Value}");
            }
            foreach (ItemStack st in w.Items)
                Assert.True(st.Count > 0, $"stack {st.Id} of {st.DefId} has count {st.Count} at tick {w.Tick}");
            foreach (Reservation r in w.Reservations)
                Assert.True(r.Count > 0 && w.Find(r.CharacterId)?.Status == CharStatus.Member, $"stale reservation for {r.CharacterId} at tick {w.Tick}");
            for (; checkedEvents < w.Log.All.Count; checkedEvents++)
            {
                SimEvent e = w.Log.All[checkedEvents];
                foreach (long cause in e.Causes)
                    Assert.True(cause >= 1 && cause < e.Id, $"event {e.Id} ({e.Key}) cites cause {cause}");
            }
        }
        Assert.Equal(year, s.World.Tick);
    }

    /// <summary>
    /// A year with the stand-in's camp plan (ADR-0027): nobody stands where they cannot pass, every wall or door tile
    /// is the old camp's or a standing structure, no two plans overlap (but a roof over a bed), and no plan holds more
    /// timber than it costs.
    /// </summary>
    [Theory]
    [InlineData(4UL)]
    [InlineData(5UL)]
    public void AYearOfBuildingKeepsItsInvariants(ulong seed)
    {
        var s = Simulation.Create(TestContent.Db, seed);
        World w = s.World;
        Return.Runner.Autoplay.Begin(s);
        var oldCamp = new HashSet<int>();
        for (int i = 0; i < w.Map.Terrain.Length; i++)
            if ((TerrainKind)w.Map.Terrain[i] is TerrainKind.Wall or TerrainKind.Door) oldCamp.Add(i);
        int perHour = w.Content.World.TicksPerHour;
        long year = w.Content.World.TicksPerYear;
        bool built = false;
        // As above: a return hands the simulation the restored world (ADR-0030), so it is read afresh every hour.
        for (long hours = 0; s.World.Tick < year && !s.World.Fate.Ended; hours++)
        {
            Assert.True(hours < 4 * year / perHour, $"seed {seed}: the year never ended");
            Return.Runner.Autoplay.Run(s, Math.Min(perHour, year - s.World.Tick), "camp");
            w = s.World;
            string when = $"seed {seed} at tick {w.Tick}";
            foreach (Character c in w.Characters)
                if (c.Status is CharStatus.Member or CharStatus.Hostile)
                    Assert.True(w.Map.Passable(c.Pos), $"{when}: {c.CastKey} stands on impassable {c.Pos}");
            for (int i = 0; i < w.Map.Terrain.Length; i++)
            {
                var kind = (TerrainKind)w.Map.Terrain[i];
                if (kind is not (TerrainKind.Wall or TerrainKind.Door) || oldCamp.Contains(i)) continue;
                var p = new Return.Sim.Core.Point(i % w.Map.Width, i / w.Map.Width);
                Structure? st = Return.Sim.Systems.Building.StructureAt(w, p);
                Assert.True(st != null && w.Content.BuildingById(st.DefId).Kind == (kind == TerrainKind.Wall ? "wall" : "door"), $"{when}: {kind} at {p} is neither old camp nor a standing structure");
                built = true;
            }
            foreach (Structure st in w.Map.Structures)
                if (!st.Removed) Assert.True(w.Map.At(st.Pos) is TerrainKind.Wall or TerrainKind.Door, $"{when}: structure {st.Id} at {st.Pos} stands on {w.Map.At(st.Pos)}");
            for (int a = 0; a < w.Blueprints.Count; a++)
            {
                Blueprint x = w.Blueprints[a];
                Assert.True(x.Delivered <= w.Content.BuildingById(x.DefId).Cost(x.Area), $"{when}: plan {x.Id} holds {x.Delivered} timber");
                for (int b = a + 1; b < w.Blueprints.Count; b++)
                {
                    Blueprint y = w.Blueprints[b];
                    string kx = w.Content.BuildingById(x.DefId).Kind, ky = w.Content.BuildingById(y.DefId).Kind;
                    bool overhead = (kx, ky) is ("roof", "bed") or ("bed", "roof");
                    Assert.True(overhead || !Overlap(x.Area, y.Area), $"{when}: plans {x.Id} and {y.Id} overlap");
                }
            }
        }
        Assert.True(built, $"seed {seed}: nothing was built in a year of the camp plan");
    }

    private static bool Overlap(Return.Sim.Core.Rect a, Return.Sim.Core.Rect b) =>
        a.X < b.X + b.W && b.X < a.X + a.W && a.Y < b.Y + b.H && b.Y < a.Y + a.H;
}
