using Return.Sim;
using Return.Sim.Api;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Stories;
using Return.Sim.Systems;

namespace Return.Tests;

/// <summary>
/// Danger (the player design §8.5 and §4.1, ADR-0030): wounds bleed until someone tends them, adding no draw; the
/// wounded wait where they are for the carer coming to them; and a need that would drive anyone else away wears you
/// down instead, until it can kill you.
/// </summary>
public class WoundTests
{
    private const int Day = 7_200, Hour = 300;

    private static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>Only these cast members in camp, fed, rested and by the hearth; content as given (you human and giftless by default).</summary>
    private static Simulation Camp(ulong seed, RebirthChoice? choice, params string[] keys)
    {
        var sim = Simulation.Create(choice == null ? TestContent.Db : TestContent.Rolled, seed, choice);
        World w = sim.World;
        int i = 0;
        foreach (Character c in w.Characters)
        {
            if (!keys.Contains(c.CastKey)) { c.Status = CharStatus.OffMap; continue; }
            c.Status = CharStatus.Member;
            c.Food = Math.Max(c.Food, 900); c.Rest = 900; c.Social = 900; c.Mood = 60;
            c.Pos = new Point(w.Map.Hearth.X - 2 + i, w.Map.Hearth.Y + 2);
            i++;
        }
        w.Arrivals.Clear();
        return sim;
    }

    private static SimEvent Wound(World w, Character c, int damage, long cause = 0)
    {
        SimEvent accident = w.Log.Emit(w, SystemTag.Incident, "event.incident.work_accident", new[] { c.Id }, new[] { cause }, null, EventArg.Char("a", c.Id));
        CombatSystem.Hurt(w, c, damage, accident.Id, "event.incident.died_accident", SystemTag.Incident, bleeds: true);
        return accident;
    }

    [Fact]
    public void UntendedWoundsKill()
    {
        Simulation sim = Camp(1, null, "mira");
        World w = sim.World;
        Character mira = Cast(w, "mira");
        SimEvent forage = w.Log.Emit(w, SystemTag.Work, "event.work.foraged", new[] { mira.Id }, Array.Empty<long>(), null,
            EventArg.Char("a", mira.Id), EventArg.Num("n", 2), EventArg.Item("item", "berries"));
        SimEvent wound = Wound(w, mira, 200, forage.Id);
        Assert.Equal(200 * CombatSystem.BleedSharePct / 100, mira.Bleed);
        Assert.Equal(wound.Id, mira.BleedCause);
        for (int i = 0; i < Day && mira.Status == CharStatus.Member; i++) sim.Step();
        Assert.Equal(CharStatus.Dead, mira.Status);
        SimEvent death = w.Log.All.Single(e => e.Effects.Any(f => f.Kind == EffectKind.Death && f.A == mira.Id));
        Assert.Equal("event.incident.died_accident", death.Key); // the wound's own line
        Assert.Equal("Mira died of their injuries.", EventText.Describe(w, death));
        Assert.Equal(new[] { wound.Id }, death.Causes); // the wound, not the collapse on the way
        // The work that hurt her, the wound and the death are one story across three systems.
        Story story = StoryAnalyzer.Find(w.Log.All, id => w.Find(id)?.Named == true).Single(s => s.TerminalEventId == death.Id);
        Assert.Equal(new[] { forage.Id, wound.Id, death.Id }, story.Chain.OrderBy(id => id));
        Assert.Equal(new[] { SystemTag.Work, SystemTag.Incident, SystemTag.Needs }.OrderBy(s => s), story.Systems.OrderBy(s => s));
    }

    /// <summary>
    /// A wound that bleeds adds no draw (§8.5): the same blow with and without the bleed, and the hours it bleeds through,
    /// draw the same from every stream, so the only change to determinism is who lives.
    /// </summary>
    [Fact]
    public void BleedingDrawsNothing()
    {
        Simulation bleeding = Camp(1, null, "mira", "bram"), dry = Camp(1, null, "mira", "bram");
        Character a = Cast(bleeding.World, "mira"), b = Cast(dry.World, "mira");
        long[] Draws(World w) => w.Rng.Dice.Streams.Select(s => s.Draws).ToArray();
        CombatSystem.Hurt(bleeding.World, a, 200, 0, "event.incident.died_accident", SystemTag.Incident, bleeds: true);
        CombatSystem.Hurt(dry.World, b, 200, 0, "event.incident.died_accident", SystemTag.Incident, bleeds: false);
        Assert.True(a.Bleed > 0);
        Assert.Equal(0, b.Bleed);
        for (int hour = 0; hour < 3; hour++)
        {
            NeedsSystem.Hourly(bleeding.World);
            NeedsSystem.Hourly(dry.World);
        }
        Assert.True(a.Health < b.Health, "the bleed cost her health");
        Assert.Equal(Draws(dry.World), Draws(bleeding.World));
    }

    [Fact]
    public void YourUntendedWoundKillsYouAndYouReturn()
    {
        Simulation sim = Camp(2, null, "you");
        sim.Advance(Day + 4 * Hour); // knots to go back to
        World w = sim.World;
        Character you = w.Get(w.PlayerId);
        SimEvent wound = Wound(w, you, 250);
        for (int i = 0; i < Day && sim.World.Fate.Returns == 0; i++) sim.Step();
        World back = sim.World;
        Assert.Equal(1, back.Fate.Returns);
        LostFuture lost = back.Fate.LostFutures.Single();
        Assert.Equal("event.incident.died_accident", lost.Lines[lost.DeathLine].Key);
        Assert.Equal("You died of your injuries.", EventText.Describe(back, lost.Lines[lost.DeathLine]));
        Assert.Equal(0, back.Get(back.PlayerId).Bleed); // the knot was tied while you were whole
    }

    [Fact]
    public void TendingStopsTheBleeding()
    {
        Simulation sim = Camp(3, null, "mira", "bram");
        World w = sim.World;
        w.Tick = 10 * Hour; // working hours: at midnight Bram is asleep, and a sleeper does not wake to tend (ADR-0029)
        Character mira = Cast(w, "mira"), bram = Cast(w, "bram");
        Wound(w, mira, 150);
        SimEvent? cared = null;
        for (int i = 0; i < 6 * Hour && cared == null; i++)
        {
            sim.Step();
            cared = w.Log.All.LastOrDefault(e => e.Key == "event.work.cared_for" && e.Actors.Contains(mira.Id));
        }
        Assert.NotNull(cared);
        Assert.Equal(0, mira.Bleed);
        Assert.Equal(CharStatus.Member, mira.Status);
        SimEvent owes = w.Log.All.Single(e => e.Key == "event.social.owes_care");
        Assert.Contains(cared!.Id, owes.Causes); // the tended owes the tender
        Assert.Equal(bram.Id, owes.Actors[1]);
        int health = mira.Health;
        sim.Advance(3 * Hour);
        Assert.True(mira.Health >= health, "a tended wound bleeds no more");
    }

    [Fact]
    public void TheWoundedWaitWhereTheyAreForTheirCarer()
    {
        // The undead never lie down; bleeding, one used to walk away from whoever came to tend them.
        Simulation sim = Camp(4, null, "ketil", "bram");
        World w = sim.World;
        w.Tick = 10 * Hour; // working hours: at midnight Bram is asleep, and a sleeper does not wake to tend (ADR-0029)
        Character ketil = Cast(w, "ketil"), bram = Cast(w, "bram");
        bram.Pos = new Point(ketil.Pos.X - 6, ketil.Pos.Y);
        Wound(w, ketil, 120);
        bool waited = false;
        for (int i = 0; i < 4 * Hour && ketil.Bleed > 0; i++)
        {
            sim.Step();
            if (WorkSystem.AwaitsTending(w, ketil) && ketil.Job is { Kind: JobKind.Wander } j && j.Target == ketil.Pos) waited = true;
        }
        Assert.True(waited);
        Assert.Equal(0, ketil.Bleed);
        Assert.Equal(CharStatus.Member, ketil.Status);
    }

    [Fact]
    public void YourNeedWearsYouDown()
    {
        // As the djinn, unbound and alone: once the hours that would drive a djinn away have run, the need harms you.
        Simulation sim = Camp(5, new RebirthChoice("djinn", RebirthChoice.NoGift), "you");
        World w = sim.World;
        Character you = w.Get(w.PlayerId);
        NatureNeed contract = you.NatureNeeds.Single(n => n.Id == "contract");
        contract.Value = 100;
        for (int i = 0; i < 2 * Hour; i++) sim.Step(); // it turns critical
        Assert.Equal(2, contract.Stage);
        int health = you.Health;
        sim.Advance(29 * Hour);
        Assert.True(you.Health >= health - 10, "no harm before the leaving clock has run");
        sim.Advance(4 * Hour);
        Assert.True(you.Health <= health - 2 * NeedsSystem.StarvationDamagePerHour, "hunger's harm once it has");
        Assert.Equal(CharStatus.Member, you.Status); // you never leave
        for (int i = 0; i < 12 * Day && sim.World.Fate.Returns == 0; i++) sim.Step();
        LostFuture lost = sim.World.Fate.LostFutures.Single();
        Assert.Equal("event.needs.died_contract", lost.Lines[lost.DeathLine].Key);
        Assert.Equal("You burned out, bound to no one.", EventText.Describe(sim.World, lost.Lines[lost.DeathLine]));

        // A djinn who is not you leaves instead, as before.
        Simulation other = Camp(5, null, "qamar", "mira");
        Character cindral = Cast(other.World, "qamar");
        NatureNeed theirs = cindral.NatureNeeds.Single(n => n.Id == "contract");
        theirs.Value = 100;
        for (int i = 0; i < 40 * Hour && cindral.Status == CharStatus.Member; i++)
        {
            other.World.Get(other.World.PlayerId).Status = CharStatus.OffMap; // nobody to strike a bargain with
            Cast(other.World, "mira").Status = CharStatus.OffMap;
            other.Step();
        }
        Assert.Equal(CharStatus.Left, cindral.Status);
    }

    [Fact]
    public void AsTheFaeTheBrokenWordsWearYouDownToo()
    {
        Simulation sim = Camp(6, new RebirthChoice("fae", RebirthChoice.NoGift), "you");
        World w = sim.World;
        Character you = w.Get(w.PlayerId);
        NatureNeed courtesy = you.NatureNeeds.Single(n => n.Id == "courtesy");
        SimEvent broken = w.Log.Emit(w, SystemTag.Social, "event.social.promise_broken", Array.Empty<int>(), Array.Empty<long>());
        NatureNeeds.Adjust(w, you, courtesy, -courtesy.Value + 100, broken.Id);
        sim.Advance(26 * Hour);
        Assert.True(NatureNeeds.WearsYouDown(w, you, courtesy, w.Content.NeedById("courtesy")));
        int health = you.Health;
        sim.Advance(Hour);
        Assert.True(you.Health < health);
        // The harm cites what last explained the need (its critical turn), which goes back to the broken word.
        Assert.Equal(courtesy.LastEvent, you.LastHealthEvent);
        long cause = you.LastHealthEvent;
        while (w.Log.Get(cause) is { } e && e.Id != broken.Id && e.Causes.Count > 0) cause = e.Causes[0];
        Assert.Equal(broken.Id, cause);
    }
}
