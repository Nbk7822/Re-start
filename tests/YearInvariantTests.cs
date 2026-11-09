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
        World w = s.World;
        int perHour = w.Content.World.TicksPerHour;
        int checkedEvents = 0;
        for (long hour = 0; hour < w.Content.World.TicksPerYear / perHour; hour++)
        {
            s.Advance(perHour);
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
        Assert.Equal(w.Content.World.TicksPerYear, w.Tick);
    }
}
