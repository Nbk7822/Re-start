using Return.Sim;
using Return.Sim.Events;
using Return.Sim.Systems;

namespace Return.Tests;

public class SocialTests
{
    private static World Camp(ulong seed = 1) => WorldGen.Create(TestContent.Db, seed);
    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    private static Character Join(World w, string key)
    {
        Character c = Cast(w, key);
        c.Status = CharStatus.Member;
        c.Pos = w.Map.Hearth with { Y = w.Map.Hearth.Y + 1 };
        return c;
    }

    [Fact]
    public void InsultLowersOpinionAndAddsThought()
    {
        World w = Camp();
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        SimEvent e = SocialSystem.Insult(w, you, mira);
        int scale = Traits.ScalePct(w, mira, t => t.InsultOpinionScalePct);
        Assert.Equal(-6 * scale / 100, Relations.Opinion(w, mira.Id, you.Id));
        Assert.Contains(mira.Thoughts, t => t.DefId == "insulted" && t.AboutId == you.Id);
        Assert.Equal("event.social.insulted", e.Key);
        Assert.Equal(SystemTag.Social, e.System);
    }

    [Fact]
    public void RepeatedInsultsFormGrudge()
    {
        World w = Camp();
        Character you = Cast(w, "you"), oren = Cast(w, "oren");
        SimEvent? last = null;
        for (int i = 0; i < 10 && !w.Grudges.Any(); i++) last = SocialSystem.Insult(w, you, oren);
        Grudge g = Assert.Single(w.Grudges);
        Assert.Equal(oren.Id, g.HolderId);
        Assert.Equal(you.Id, g.TargetId);
        Assert.Equal(last!.Id, g.CauseEvent);
        SimEvent formed = w.Log.Get(g.FormedEvent)!;
        Assert.Contains(formed.Effects, f => f.Kind == EffectKind.GrudgeFormed && f.A == oren.Id && f.B == you.Id);
        Assert.Contains(last.Id, formed.Causes);
    }

    [Fact]
    public void ProudTakesInsultsHarder()
    {
        World w = Camp();
        Character you = Cast(w, "you");
        Character hollis = Join(w, "hollis");
        SocialSystem.Insult(w, you, hollis);
        int scale = Traits.ScalePct(w, hollis, t => t.InsultOpinionScalePct);
        Assert.True(scale >= 160);
        Assert.Equal(-6 * scale / 100, Relations.Opinion(w, hollis.Id, you.Id));
        Assert.True(Relations.Opinion(w, hollis.Id, you.Id) <= -9);
    }

    [Fact]
    public void PromiseKeptRepaysAndRaisesOpinion()
    {
        World w = Camp();
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        Debt d = Relations.FormDebt(w, you, mira, DebtKind.PromiseMeal, 1, 0, w.Tick + 1_000);
        Relations.Repay(w, d, d.FormedEvent);
        Assert.Equal(DebtStatus.Repaid, d.Status);
        Assert.Equal(14, Relations.Opinion(w, mira.Id, you.Id));
        Assert.Contains(w.Log.All, e => e.Effects.Any(f => f.Kind == EffectKind.DebtRepaid && f.RefId == d.Id));
    }

    [Fact]
    public void OverduePromiseBreaksAndFormsGrudge()
    {
        World w = Camp();
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        Debt d = Relations.FormDebt(w, you, mira, DebtKind.PromiseMeal, 1, 0, w.Tick + 10);
        w.Tick += 300;
        SocialSystem.Hourly(w);
        Assert.Equal(DebtStatus.Broken, d.Status);
        Assert.Contains(w.Log.All, e => e.Effects.Any(f => f.Kind == EffectKind.DebtBroken && f.RefId == d.Id));
        Grudge g = Assert.Single(w.Grudges);
        Assert.Equal(mira.Id, g.HolderId);
        Assert.Equal(-30, Relations.Opinion(w, mira.Id, you.Id));
    }

    [Fact]
    public void DistrustDrivesDeparture()
    {
        World w = Camp();
        Character you = Cast(w, "you"), mira = Cast(w, "mira");
        Debt d = Relations.FormDebt(w, you, mira, DebtKind.PromiseMeal, 1, 0, w.Tick + 10);
        w.Tick += 300;
        SocialSystem.Hourly(w);
        Relations.ChangeOpinion(w, mira, you.Id, -20, 0);
        w.Tick += 300;
        SocialSystem.Hourly(w);
        Assert.Equal(CharStatus.Left, mira.Status);
        SimEvent left = w.Log.All.Single(e => e.Effects.Any(f => f.Kind == EffectKind.Departure));
        Assert.Contains(d.FormedEvent, NeedsMoodTests.Ancestors(w, left.Id));
    }

    [Fact]
    public void GrudgeDecaysAndEnds()
    {
        World w = Camp();
        Character you = Cast(w, "you"), oren = Cast(w, "oren");
        Grudge g = Relations.FormGrudge(w, oren, you, 2, 0);
        int day = w.Content.World.TicksPerDay;
        w.Tick = day;
        SocialSystem.Hourly(w);
        w.Tick = 2L * day;
        SocialSystem.Hourly(w);
        w.Tick = 3L * day;
        SocialSystem.Hourly(w);
        Assert.False(g.Active);
        Assert.Contains(w.Log.All, e => e.Effects.Any(f => f.Kind == EffectKind.GrudgeEnded && f.RefId == g.Id));
    }

    [Fact]
    public void NeighboursTalk()
    {
        World w = Camp(3);
        int every = w.Content.World.SocialEveryTicks;
        for (int t = 0; t < 600; t++)
        {
            w.Tick = (long)t * every;
            SocialSystem.Step(w);
        }
        Assert.Contains(w.Members(), m => m.Thoughts.Any(t => t.DefId is "chatted" or "deep_talk" or "insulted"));
        Assert.Contains(w.Members(), m => m.Opinions.Count > 0);
    }
}
