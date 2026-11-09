using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems;
using Return.Sim.Systems.Culture;
using Return.Sim.Systems.Jobs;

namespace Return.Tests;

/// <summary>
/// Each culture hook (ADR-0023): its event key and system, what it cites and what it brings about.
/// Worlds are built, state set directly, and the system called, in the style of CausalityTests.
/// </summary>
public class CultureHookTests
{
    internal static Character Cast(World w, string key) => w.Characters.First(c => c.CastKey == key);

    /// <summary>Content where every custom of the named ids has its param forced (e.g. breach odds of 1000).</summary>
    internal static ContentDb Forced(params (string Custom, string Param, int Value)[] overrides)
    {
        var lines = new List<string>();
        foreach (CustomDef c in TestContent.Db.Customs)
        {
            var ps = new Dictionary<string, int>(c.Params);
            foreach ((string id, string param, int value) in overrides)
                if (id == c.Id) ps[param] = value;
            string args = string.Join(", ", ps.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"\"{p.Key}\": {p.Value}"));
            lines.Add($"{{ \"id\": \"{c.Id}\", \"kind\": \"{c.Kind}\", \"params\": {{ {args} }} }}");
        }
        string mod = CultureContentTests.Mod(("customs.json", "[" + string.Join(",\n", lines) + "]"));
        return ContentDb.Load(Path.Combine(TestPaths.Root, "content"), new[] { mod });
    }

    /// <summary>A camp of only these people, fed and rested by the hearth, from the given content.</summary>
    internal static World Camp(ContentDb db, ulong seed, params string[] keys)
    {
        World w = WorldGen.Create(db, seed);
        int i = 0;
        foreach (Character c in w.Characters)
        {
            if (!keys.Contains(c.CastKey)) { c.Status = CharStatus.OffMap; continue; }
            c.Status = CharStatus.Member;
            c.Food = 900; c.Rest = 900; c.Social = 900; c.Mood = 60;
            if (!w.Map.Passable(c.Pos)) c.Pos = new Point(w.Map.Hearth.X - 2 + i, w.Map.Hearth.Y + 2);
            i++;
        }
        return w;
    }

    /// <summary>Puts someone in a culture and faith, with a devotion; clears what they learned and whom they met.</summary>
    internal static void Belong(Character c, string culture, string faith = "", int devotion = 50)
    {
        c.CultureId = culture;
        c.FaithId = faith;
        c.Devotion = devotion;
        c.LearnedCustoms.Clear();
        c.Met.Clear();
    }

    /// <summary>Gives a seeded culture exactly these customs (the always-held greeting first, as seeding does).</summary>
    internal static void Hold(World w, string culture, params string[] customs)
    {
        SeededCulture s = Ways.CultureById(w, culture)!;
        s.CustomIds = customs.ToList();
    }

    internal static void HoldFaith(World w, string faith, params string[] customs)
    {
        SeededFaith s = Ways.FaithById(w, faith)!;
        s.CustomIds = customs.ToList();
    }

    // ---- G2, G4: first meetings ----

    [Fact]
    public void StrangersStumbleOnceAndLearn()
    {
        ContentDb db = Forced(("no_touch_greeting", "breachPermille", 1000), ("clasp_greeting", "breachPermille", 1000));
        World w = Camp(db, 1, "you", "mira", "oren", "nia", "bram");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren"), nia = Cast(w, "nia"), bram = Cast(w, "bram");
        Belong(you, "");
        Belong(mira, "reed"); Belong(oren, "reed");
        Belong(nia, "hearth"); Belong(bram, "hearth");
        Hold(w, "reed", "no_touch_greeting");
        Hold(w, "hearth", "clasp_greeting");

        int before = w.Log.All.Count;
        Assert.True(Greetings.FirstMeeting(w, you, mira));
        SimEvent breach = w.Log.All.Last();
        Assert.Equal("event.culture.no_touch_greeting.breach", breach.Key);
        Assert.Equal(SystemTag.Culture, breach.System);
        Assert.Equal(new[] { you.Id, mira.Id }, breach.Actors);
        Assert.Empty(breach.Causes);
        Assert.Contains(mira.Thoughts, t => t.DefId == "faux_pas" && t.AboutId == you.Id && t.CauseEvent == breach.Id);
        Assert.Equal(new[] { "no_touch_greeting" }, you.LearnedCustoms);
        Assert.Equal("You took Mira by the hand at their first meeting; " + db.Loc.Get(Ways.CultureById(w, "reed")!.NameKey) + " do not touch a stranger.",
            Return.Sim.Api.EventText.Describe(w, breach));

        // Learned: the next of that people is greeted properly.
        Assert.False(Greetings.FirstMeeting(w, you, oren));
        // Met before: nothing at all.
        Assert.False(Greetings.FirstMeeting(w, you, mira));
        Assert.Equal(before + 1, w.Log.All.Count);

        // Kin greet each other, with no event.
        Assert.False(Greetings.FirstMeeting(w, nia, bram));
        Assert.Contains(nia.Thoughts, t => t.DefId == "kin_greeting" && t.AboutId == bram.Id);
        Assert.Contains(bram.Thoughts, t => t.DefId == "kin_greeting" && t.AboutId == nia.Id);
        Assert.Equal(before + 1, w.Log.All.Count);
        Assert.Equal(new[] { bram.Id }, nia.Met);
    }

    [Fact]
    public void NobodyBreachesAGreetingTheyShare()
    {
        ContentDb db = Forced(("clasp_greeting", "breachPermille", 1000));
        World w = Camp(db, 1, "mira", "oren");
        Character mira = Cast(w, "mira"), oren = Cast(w, "oren");
        Belong(mira, "reed"); Belong(oren, "hearth");
        Hold(w, "reed", "clasp_greeting");
        Hold(w, "hearth", "clasp_greeting");
        Assert.False(Greetings.FirstMeeting(w, mira, oren));
        Assert.DoesNotContain(w.Log.All, e => e.System == SystemTag.Culture);
    }

    [Fact]
    public void TheOtherworlderStumblesThreeTimesAsOften()
    {
        int Breaches(bool outsider)
        {
            int n = 0;
            for (ulong seed = 1; seed <= 300; seed++)
            {
                World w = Camp(TestContent.Db, seed % 7 + 1, "you", "mira");
                w.Rng = Rng.FromSeed(seed);
                Character you = Cast(w, "you"), mira = Cast(w, "mira");
                Belong(you, outsider ? "" : "night");
                Belong(mira, "reed");
                Hold(w, "reed", "no_touch_greeting");
                Hold(w, "night", "veiled_greeting");
                if (Greetings.FirstMeeting(w, you, mira)) n++;
            }
            return n;
        }
        int outsider = Breaches(true), member = Breaches(false);
        Assert.InRange(member, 15, 60);      // 120 per mille of 300
        Assert.InRange(outsider, 75, 145);   // 360 per mille of 300
    }

    // ---- G1, G3, A1: warmth and insults ----

    [Fact]
    public void KinWarmStrangersCoolAndStancesLean()
    {
        World w = Camp(TestContent.Db, 1, "you", "mira", "oren", "ilex");
        Character you = Cast(w, "you"), mira = Cast(w, "mira"), oren = Cast(w, "oren"), ilex = Cast(w, "ilex");
        Belong(mira, "hearth"); Belong(oren, "hearth"); Belong(ilex, "night"); Belong(you, "");
        Assert.Equal(100 + CultureTuning.KinWarmth, Greetings.Warmth(w, mira, oren, 0));
        Assert.Equal(100, Greetings.Warmth(w, mira, you, 0));
        ilex.ArrivedTick = 1; w.Tick = 2;
        Assert.Equal(100 + CultureTuning.StrangerWarmth - 25, Greetings.Warmth(w, mira, ilex, 0)); // a newcomer, and the Hearthfolk are wary of vampires
        Assert.Equal(100 + CultureTuning.StrangerWarmth, Greetings.Warmth(w, ilex, mira, 0));
        Assert.Equal(0, Greetings.Warmth(w, mira, ilex, -2));
        Belong(mira, "reed");
        Assert.Equal(40, Ways.StanceInsult(w, mira, ilex));
        Assert.Equal(0, Ways.StanceInsult(w, ilex, mira));
    }

    [Fact]
    public void StancesShiftWarmthInsultAndConsent()
    {
        Assert.Equal((50, -20, 30), CultureTuning.Stance("revere"));
        Assert.Equal((-25, 20, -10), CultureTuning.Stance("wary"));
        Assert.Equal((-50, 40, -30), CultureTuning.Stance("fear"));
        Assert.Equal((-25, 60, -20), CultureTuning.Stance("scorn"));
        Assert.Equal((0, 0, 0), CultureTuning.Stance(null));

        World w = Camp(TestContent.Db, 1, "mira", "ilex");
        Character mira = Cast(w, "mira"), ilex = Cast(w, "ilex");
        foreach ((string culture, string? stance) in new[] { ("hearth", "wary"), ("reed", "fear"), ("ashen", "revere"), ("night", (string?)null) })
        {
            Belong(mira, culture);
            Assert.Equal(stance, Ways.Stance(w, mira, ilex));
            Assert.Equal(CultureTuning.Stance(stance).Consent, Ways.StanceConsent(w, mira, ilex));
        }
        Belong(mira, "");
        Assert.Null(Ways.Stance(w, mira, ilex));
    }
}
