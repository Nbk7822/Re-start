using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;
using static Return.Tests.CultureHookTests;

namespace Return.Tests;

/// <summary>Omens (M1, M2; ADR-0023): a deed, then a sign, then a faithful reader, and a Dice roll only when all three are there.</summary>
public class OmenTests
{
    /// <summary>A camp where Mira, devout in the Sworn Word, follows a stern god of oaths; Oren was promised a meal by you.</summary>
    private static (World W, Character You, Character Mira, Character Oren) Faithful(ulong seed)
    {
        World w = Camp(TestContent.Db, seed, "you", "mira", "oren");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren");
        Belong(mira, "night", "sworn", 100);
        Belong(oren, "reed");
        Hold(w, "night", "veiled_greeting");
        Hold(w, "reed", "no_touch_greeting");
        SeededFaith sworn = Ways.FaithById(w, "sworn")!;
        sworn.Gods[0].DomainId = "oaths";
        sworn.Gods[0].Wrath = 100;
        sworn.Gods[0].Grace = 100;
        sworn.Gods.RemoveRange(1, sworn.Gods.Count - 1);
        return (w, you, mira, oren);
    }

    private static SimEvent Sign(World w, string key) => w.Log.Emit(w, SystemTag.Incident, key, Array.Empty<int>(), Array.Empty<long>());

    [Fact]
    public void OmensNeedADeedASignAndAFaithfulReader()
    {
        int readings = 0;
        for (ulong seed = 1; seed <= 12; seed++)
        {
            (World w, Character you, Character mira, Character oren) = Faithful(seed);

            // No deed: no omen, and no roll.
            long draws = w.Rng.Dice[DiceSystem.Culture].Draws;
            Omens.OnSign(w, "rain", Sign(w, "event.incident.rain"));
            Assert.Equal(draws, w.Rng.Dice[DiceSystem.Culture].Draws);

            SimEvent why = Sign(w, "event.test.why");
            SimEvent broken = Relations.Break(w, Relations.FormDebt(w, you, oren, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
            w.Tick += 10L * w.Content.World.TicksPerHour;
            SimEvent rain = Sign(w, "event.incident.rain");
            Omens.OnSign(w, "rain", rain);
            Assert.Equal(draws + 1, w.Rng.Dice[DiceSystem.Culture].Draws);
            SimEvent? omen = w.Log.All.LastOrDefault(e => e.Key == "event.culture.omen_anger");
            if (omen == null) continue;
            readings++;
            Assert.Equal(SystemTag.Culture, omen.System);
            Assert.Equal(new[] { mira.Id, you.Id }, omen.Actors);
            Assert.Equal(new[] { broken.Id, rain.Id }, omen.Causes);
            Assert.Contains(mira.Thoughts, t => t.DefId == "omen_anger" && t.AboutId == you.Id && t.CauseEvent == omen.Id);
            string god = w.Content.Loc.Get(Ways.FaithById(w, "sworn")!.Gods[0].NameKey);
            Assert.Equal("To Mira, the rain was " + god + "'s anger at you for a broken promise.", Return.Sim.Api.EventText.Describe(w, omen));

            // Within the cooldown the same reader reads nothing more, and is not even rolled for.
            SimEvent second = Relations.Break(w, Relations.FormDebt(w, you, oren, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
            w.Tick += 20L * w.Content.World.TicksPerHour;
            long before = w.Rng.Dice[DiceSystem.Culture].Draws;
            Omens.OnSign(w, "rain", Sign(w, "event.incident.rain"));
            Assert.Equal(before, w.Rng.Dice[DiceSystem.Culture].Draws);
            Assert.Single(w.Log.All, e => e.Key == "event.culture.omen_anger");
            Assert.True(second.Id > 0);
        }
        Assert.True(readings >= 3, $"only {readings} of 12 devout readers read the rain");
    }

    /// <summary>
    /// No reading, and no roll, for a lapsed follower, for a sign the god does not speak through, or
    /// for a deed older than the deed memory. (A distant god's followers do read signs, at lower odds:
    /// temperament is a word for wrath and grace, which scale the odds.)
    /// </summary>
    [Fact]
    public void NoReadingForTheLapsedAnUnspokenSignOrAnOldDeed()
    {
        (World w, Character you, Character mira, Character oren) = Faithful(1);
        mira.Devotion = 20; // lapsed: below the Sworn Word's omen devotion
        SimEvent why = Sign(w, "event.test.why");
        Relations.Break(w, Relations.FormDebt(w, you, oren, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
        long draws = w.Rng.Dice[DiceSystem.Culture].Draws;
        Omens.OnSign(w, "rain", Sign(w, "event.incident.rain"));
        Assert.Equal(draws, w.Rng.Dice[DiceSystem.Culture].Draws);

        // A devout reader, but the sign is one this god does not speak through.
        mira.Devotion = 100;
        Omens.OnSign(w, "food_spoils", Sign(w, "event.incident.food_spoils"));
        Assert.Equal(draws, w.Rng.Dice[DiceSystem.Culture].Draws);

        // Deeds older than the deed memory are forgotten.
        w.Tick += (CultureTuning.DeedMemoryHours + 1) * w.Content.World.TicksPerHour;
        Omens.Prune(w);
        Omens.OnSign(w, "rain", Sign(w, "event.incident.rain"));
        Assert.Equal(draws, w.Rng.Dice[DiceSystem.Culture].Draws);
        Assert.DoesNotContain(w.Log.All, e => e.System == SystemTag.Culture && e.Key.StartsWith("event.culture.omen", StringComparison.Ordinal));
    }

    [Fact]
    public void ABlessingFollowsADeedTheGodLoves()
    {
        int blessings = 0;
        for (ulong seed = 1; seed <= 12; seed++)
        {
            (World w, Character you, Character mira, Character oren) = Faithful(seed);
            SimEvent why = Sign(w, "event.test.why");
            SimEvent kept = Relations.Repay(w, Relations.FormDebt(w, oren, you, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
            int opinion = Relations.Opinion(w, mira.Id, oren.Id);
            SimEvent windfall = Sign(w, "event.incident.windfall_berries");
            Omens.OnSign(w, "windfall", windfall);
            SimEvent? omen = w.Log.All.LastOrDefault(e => e.Key == "event.culture.omen_blessing");
            if (omen == null) continue;
            blessings++;
            Assert.Equal(new[] { kept.Id, windfall.Id }, omen.Causes);
            Assert.Equal(opinion + 10, Relations.Opinion(w, mira.Id, oren.Id));
        }
        Assert.True(blessings >= 3, $"only {blessings} of 12 devout readers saw the windfall as thanks");
    }

    [Fact]
    public void AnAngryReadingPastTheGrudgeLineHoldsAGrudge()
    {
        for (ulong seed = 1; seed <= 12; seed++)
        {
            (World w, Character you, Character mira, Character oren) = Faithful(seed);
            Relations.ChangeOpinion(w, mira, you.Id, -35, 0);
            SimEvent why = Sign(w, "event.test.why");
            Relations.Break(w, Relations.FormDebt(w, you, oren, DebtKind.PromiseMeal, 1, why.Id, w.Tick + 100), why.Id);
            Omens.OnSign(w, "rain", Sign(w, "event.incident.rain"));
            SimEvent? omen = w.Log.All.LastOrDefault(e => e.Key == "event.culture.omen_anger");
            if (omen == null) continue;
            Grudge g = Relations.ActiveGrudge(w, mira.Id, you.Id)!;
            Assert.Equal(omen.Id, g.CauseEvent);
            Assert.Equal(CultureTuning.OmenGrudge, g.Intensity);
            return;
        }
        Assert.Fail("no devout reader read the rain in 12 worlds");
    }
}
