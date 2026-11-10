using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

public class NeedsMoodTests
{
    private static World Camp(ulong seed = 1) => WorldGen.Create(TestContent.Db, seed);
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);
    private static int Ticks(World w, int hours) => hours * w.Content.World.TicksPerHour;

    private static Character Join(World w, string key, Point at)
    {
        Character c = Cast(w, key);
        c.Status = CharStatus.Member;
        c.Pos = at;
        return c;
    }

    [Fact]
    public void FoodDecaysPerSpecies()
    {
        World w = Camp();
        Character you = Cast(w, "you");
        Character ilex = Join(w, "ilex", w.Map.CoveredRuins.Center);
        int youFood = you.Food, ilexFood = ilex.Food;
        NeedsSystem.Hourly(w);
        Assert.Equal(youFood - 25, you.Food);
        Assert.Equal(ilexFood - 18, ilex.Food);
    }

    [Fact]
    public void EatingRestoresAndLogs()
    {
        World w = Camp();
        Character you = Cast(w, "you");
        you.Food = 200;
        int before = Stock.Count(w, "berries");
        SimEvent e = NeedsSystem.Eat(w, you, 0)!;
        Assert.Equal(650, you.Food);
        Assert.Equal(before - 1, Stock.Count(w, "berries"));
        Assert.Equal("event.needs.ate", e.Key);
        Assert.Equal(SystemTag.Needs, e.System);
        Assert.Contains(you.Thoughts, t => t.DefId == "ate");
    }

    [Fact]
    public void HungryEventFiresOnce()
    {
        World w = Camp();
        Character you = Cast(w, "you");
        you.Food = 310;
        NeedsSystem.Hourly(w);
        w.Tick += Ticks(w, 1);
        NeedsSystem.Hourly(w);
        Assert.Equal(1, w.Log.All.Count(e => e.Key == "event.needs.hungry" && e.Actors.Contains(you.Id)));
        Assert.Contains(you.Thoughts, t => t.DefId == "hungry");
    }

    [Fact]
    public void StarvationKillsWithCauseChain()
    {
        World w = Camp();
        Character you = Cast(w, "you");
        SimEvent spoiled = w.Log.Emit(w, SystemTag.Incident, "event.test.spoiled", Array.Empty<int>(), Array.Empty<long>());
        you.LastFoodEvent = spoiled.Id;
        you.Food = 0;
        you.Health = NeedsSystem.StarvationDamagePerHour * 3 - 5;
        for (int h = 0; h < 3; h++)
        {
            NeedsSystem.Hourly(w);
            w.Tick += Ticks(w, 1);
        }
        Assert.Equal(CharStatus.Dead, you.Status);
        SimEvent death = w.Log.All.Single(e => e.Effects.Any(f => f.Kind == EffectKind.Death));
        Assert.Equal("event.needs.died_starving", death.Key);
        Assert.Contains(spoiled.Id, Ancestors(w, death.Id));
        Assert.All(w.Members(), m => Assert.Contains(m.Thoughts, t => t.DefId == "saw_death" && t.AboutId == you.Id));
    }

    [Fact]
    public void VampireBurnsInDaylightOutside()
    {
        World w = Camp();
        w.Tick = Ticks(w, 7);
        Character ilex = Join(w, "ilex", w.Map.StartStockpile.Center);
        NeedsSystem.Hourly(w);
        Assert.Equal(1000 - 60, ilex.Health);
        Assert.Contains(ilex.Thoughts, t => t.DefId == "burned_by_sun");
        Assert.Contains(w.Log.All, e => e.Key == "event.needs.sun_burn" && e.Actors.Contains(ilex.Id));
    }

    [Fact]
    public void VampireSafeUnderCover()
    {
        World w = Camp();
        w.Tick = Ticks(w, 7);
        Character ilex = Join(w, "ilex", w.Map.CoveredRuins.Center);
        NeedsSystem.Hourly(w);
        Assert.Equal(1000, ilex.Health);
    }

    [Theory]
    [InlineData(RationPolicy.Hungriest, new[] { "mira", "you" })]
    [InlineData(RationPolicy.PromisesFirst, new[] { "oren", "mira" })]
    [InlineData(RationPolicy.SelfFirst, new[] { "you", "mira" })]
    public void RationPolicyOrder(RationPolicy policy, string[] expected)
    {
        World w = Camp();
        Stock.Take(w, "berries", Stock.Count(w, "berries"));
        Stock.Add(w, "berries", 2, w.Map.StartStockpile.Center);
        Cast(w, "you").Food = 300;
        Cast(w, "mira").Food = 200;
        Cast(w, "oren").Food = 400;
        w.Debts.Add(new Debt { Id = w.NewId(), DebtorId = Cast(w, "you").Id, CreditorId = Cast(w, "oren").Id, Kind = DebtKind.PromiseMeal, DueTick = 99_999 });
        w.Policy = policy;
        Rations.Assign(w);
        Assert.Equal(expected, w.Reservations.Select(r => w.Get(r.CharacterId).CastKey));
        string skipped = new[] { "you", "mira", "oren" }.Except(expected).Single();
        Assert.Contains(w.Log.All, e => e.Key == "event.needs.went_without" && e.Actors.Contains(Cast(w, skipped).Id));
    }

    [Fact]
    public void NoRationingWhenFoodSuffices()
    {
        World w = Camp();
        Stock.Add(w, "berries", 10, w.Map.StartStockpile.Center);
        foreach (Character m in w.Members()) m.Food = 200;
        Rations.Assign(w);
        Assert.Empty(w.Reservations);
    }

    [Fact]
    public void ThoughtsExpire()
    {
        World w = Camp();
        Character you = Cast(w, "you");
        MoodSystem.AddThought(w, you, "ate", 0, 0);
        w.Tick += Ticks(w, 6);
        MoodSystem.Hourly(w);
        Assert.DoesNotContain(you.Thoughts, t => t.DefId == "ate");
    }

    [Fact]
    public void MoodClamped()
    {
        World w = Camp();
        Character you = Cast(w, "you");
        for (int i = 0; i < 6; i++) MoodSystem.AddThought(w, you, "friend_died", 100 + i, 0);
        MoodSystem.Hourly(w);
        Assert.Equal(0, you.Mood);
        you.Thoughts.Clear();
        for (int i = 0; i < 12; i++) MoodSystem.AddThought(w, you, "promise_kept", 100 + i, 0);
        MoodSystem.Hourly(w);
        Assert.Equal(100, you.Mood);
    }

    [Fact]
    public void StackLimitRefreshesButOpinionKeepsMoving()
    {
        World w = Camp();
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        for (int i = 0; i < 4; i++) MoodSystem.AddThought(w, mira, "insulted", you.Id, 0);
        Assert.Equal(3, mira.Thoughts.Count(t => t.DefId == "insulted"));
        Assert.Equal(-24, Relations.Opinion(w, mira.Id, you.Id));
    }

    [Fact]
    public void TraitsAreReadFromContent()
    {
        World w = Camp();
        Character nia = Cast(w, "nia");
        Assert.True(Traits.Max(w, nia, t => t.StealFoodBelow) >= 220);
        Assert.True(Traits.Any(w, nia, t => t.Id == "desperate"));
        Assert.True(Traits.ScalePct(w, Cast(w, "hollis"), t => t.InsultOpinionScalePct) >= 160);
    }

    internal static HashSet<long> Ancestors(World w, long id)
    {
        var seen = new HashSet<long>();
        var stack = new Stack<long>();
        stack.Push(id);
        while (stack.Count > 0)
            foreach (long c in w.Log.Get(stack.Pop())!.Causes)
                if (seen.Add(c)) stack.Push(c);
        return seen;
    }
}
