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
            // The Schedule (ADR-0029, design test 14): days and lived strips keep 24 valid entries; own hours stay in order;
            // the watch's record names people who are really there.
            foreach (CampDay d in w.CampDays)
                Assert.True(d.Hours.Count == 24 && d.Hours.All(Enum.IsDefined), $"camp day {d.Rhythm} at tick {w.Tick}");
            foreach (Character c in w.Characters)
            {
                if (!c.Named || c.Status != CharStatus.Member) continue;
                Assert.True(c.Lived.Count == 24 && c.Lived.All(Enum.IsDefined), $"lived strip of {c.CastKey} at tick {w.Tick}");
                for (int i = 1; i < c.OwnHours.Count; i++) Assert.True(c.OwnHours[i - 1].Hour < c.OwnHours[i].Hour, $"own hours of {c.CastKey}");
            }
            if (w.Watch.WatcherId != 0)
                Assert.True(w.Find(w.Watch.WatcherId) is { Status: CharStatus.Member } wc && wc.Job?.Kind is JobKind.KeepWatch or JobKind.Fight or JobKind.Flee,
                    $"watcher {w.Watch.WatcherId} at tick {w.Tick}");
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
