using System.Text.Json;
using Return.Sim;
using Return.Sim.Events;
using Return.Sim.Stories;
using Return.Sim.Systems;

namespace Return.Runner;

/// <summary>
/// What one world's run says about Return by Death and your gift (the player design §10, ADR-0030): who you were, your
/// returns (natural or forced), how far back each went, the lost futures and their stories, the final log's stories
/// that carry a Fate or gift event, those of them that run through a return, and those with a gift event, companions'
/// deaths, death loops, and whether your gift's own target event happened.
/// </summary>
public sealed record FateTally(
    string People, string Gift, int Returns, int NaturalReturns, IReadOnlyList<string> NaturalDeathKeys, IReadOnlyList<long> TicksBack,
    int Deep, int Futures, int FuturesWithStory, int LostStories, int Stories, int FateOrGiftStories, int CrossingStories, int GiftChainStories,
    int CompanionDeaths, bool ThreeInThree, bool FourInThree, bool GiftTarget, long TicksPerDay);

/// <summary>The soak's totals for Return by Death and the gifts; summary.json's "fates" object holds the same.</summary>
public sealed class FateSummary
{
    public int Returns { get; init; }
    public int ReturnWorlds { get; init; }
    public int NaturalReturns { get; init; }
    public int NaturalReturnWorlds { get; init; }
    public double AverageDaysBack { get; init; }
    public int DeepPermille { get; init; }
    public int LostFutures { get; init; }
    public int LostFuturesWithStory { get; init; }
    public int LostStories { get; init; }

    /// <summary>Final-log stories with a Fate or gift event on their chain (the design's --fates bar), and in how many worlds.</summary>
    public int FateOrGiftStories { get; init; }
    public int FateOrGiftWorlds { get; init; }

    /// <summary>Final-log stories whose chain runs through a return (its line, or a memory or familiar face citing it), and in how many worlds.</summary>
    public int CrossingStories { get; init; }
    public int CrossingWorlds { get; init; }
    public int GiftChainStories { get; init; }
    public int GiftChainWorlds { get; init; }
    public int CompanionDeaths { get; init; }
    public double CompanionDeathsPerWorldYear { get; init; }
    public int ThreeDeathsInThreeDaysWorlds { get; init; }
    public int FourDeathsInThreeDaysWorlds { get; init; }
    public double StoriesPerTimeline { get; init; }
    public IReadOnlyList<Tally> NaturalDeathsByCause { get; init; } = Array.Empty<Tally>();
    public IReadOnlyList<Tally> NaturalReturnWorldsByPeople { get; init; } = Array.Empty<Tally>();
    public IReadOnlyList<Tally> NaturalReturnWorldsByGift { get; init; } = Array.Empty<Tally>();
    public IReadOnlyList<Tally> WorldsByPeople { get; init; } = Array.Empty<Tally>();
    public IReadOnlyList<Tally> StoriesByPeople { get; init; } = Array.Empty<Tally>();
    public IReadOnlyList<Tally> WorldsByGift { get; init; } = Array.Empty<Tally>();

    /// <summary>Worlds where the gift's target event happened: a satchel event on a story chain; a turned harm; a familiar face.</summary>
    public IReadOnlyList<Tally> GiftTargetWorlds { get; init; } = Array.Empty<Tally>();
}

public static class SoakFates
{
    /// <summary>The tally of a finished world: its run memory, its final log's stories, and the gift's target.</summary>
    public static FateTally Tally(World w, IReadOnlyList<Story> stories)
    {
        FateBook f = w.Fate;
        WorldDefView def = new(w);
        var naturalKeys = new List<string>();
        var back = new List<long>();
        int deep = 0, withStory = 0, lost = 0, natural = 0;
        foreach (LostFuture lf in f.LostFutures)
        {
            string key = lf.DeathLine >= 0 ? lf.Lines[lf.DeathLine].Key : "";
            if (key != Fate.ForcedDeathKey)
            {
                natural++;
                naturalKeys.Add(key);
            }
            back.Add(lf.DeathTick - lf.KnotTick);
            if (lf.KnotTick < lf.DeathTick - (long)w.Content.Rebirth.Return.RecentDays * def.TicksPerDay) deep++;
            if (lf.StoriesFound > 0) withStory++;
            lost += lf.StoriesFound;
        }
        int fateOrGift = 0, crossing = 0, giftChains = 0;
        bool satchelChain = false;
        foreach (Story s in stories)
        {
            bool fate = false, gift = false, throughReturn = false;
            foreach (long id in s.Chain)
                if (w.Log.Get(id) is { } e)
                {
                    if (e.System is SystemTag.Fate or SystemTag.Gift || e.Key.StartsWith("event.fate.", StringComparison.Ordinal)
                        || e.Key.StartsWith("event.gift.", StringComparison.Ordinal)) fate = true;
                    if (e.System == SystemTag.Gift || e.Key.StartsWith("event.gift.", StringComparison.Ordinal)) gift = true;
                    if (e.Key.StartsWith("event.gift.satchel.", StringComparison.Ordinal)) satchelChain = true;
                    // Everything a return brings (a memory, a familiar face) cites its line, so a chain the return shaped holds it.
                    if (e.Key == Fate.ReturnedKey) throughReturn = true;
                }
            if (fate) fateOrGift++;
            if (throughReturn) crossing++;
            if (gift) giftChains++;
        }
        int companions = 0;
        foreach (SimEvent e in w.Log.All)
            foreach (Effect fx in e.Effects)
                if (fx.Kind == EffectKind.Death && fx.A != w.PlayerId && w.Find(fx.A) is { Named: true }) companions++;
        bool target = f.GiftId switch
        {
            Gifts.SatchelId => satchelChain,
            Gifts.LuckId => w.Log.All.Exists(e => e.Key.StartsWith("event.gift.luck.", StringComparison.Ordinal) && e.Key != "event.gift.luck.awakened" && e.Key != "event.gift.luck.dreamed"),
            Gifts.FamiliarId => w.Log.All.Exists(e => e.Key == "event.gift.familiar.knew_you"),
            _ => false,
        };
        return new FateTally(f.PeopleId, f.GiftId.Length == 0 ? "none" : f.GiftId, f.Returns, natural, naturalKeys, back, deep,
            f.LostFutures.Count, withStory, lost, stories.Count, fateOrGift, crossing, giftChains, companions,
            Loop(f.LostFutures, 3, 3L * def.TicksPerDay), Loop(f.LostFutures, 4, 3L * def.TicksPerDay), target, def.TicksPerDay);
    }

    /// <summary>Whether <paramref name="deaths"/> deaths in a row came within <paramref name="span"/> ticks of world time of each other.</summary>
    private static bool Loop(List<LostFuture> futures, int deaths, long span)
    {
        for (int i = 0; i + deaths <= futures.Count; i++)
        {
            long min = long.MaxValue, max = long.MinValue;
            for (int k = i; k < i + deaths; k++)
            {
                min = Math.Min(min, futures[k].DeathTick);
                max = Math.Max(max, futures[k].DeathTick);
            }
            if (max - min <= span) return true;
        }
        return false;
    }

    private readonly record struct WorldDefView(long TicksPerDay)
    {
        public WorldDefView(World w) : this(w.Content.World.TicksPerDay) { }
    }

    public static FateSummary Summarize(IReadOnlyList<FateTally> tallies, int worlds, int years)
    {
        long ticksPerDay = tallies.Count == 0 ? 1 : tallies[0].TicksPerDay;
        var causes = new Dictionary<string, int>(StringComparer.Ordinal);
        var naturalByPeople = new Dictionary<string, int>(StringComparer.Ordinal);
        var naturalByGift = new Dictionary<string, int>(StringComparer.Ordinal);
        var worldsByPeople = new Dictionary<string, int>(StringComparer.Ordinal);
        var storiesByPeople = new Dictionary<string, int>(StringComparer.Ordinal);
        var worldsByGift = new Dictionary<string, int>(StringComparer.Ordinal);
        var targets = new Dictionary<string, int>(StringComparer.Ordinal);
        int returns = 0, returnWorlds = 0, natural = 0, naturalWorlds = 0, deep = 0, futures = 0, withStory = 0, lost = 0;
        int fateOrGift = 0, fateOrGiftWorlds = 0, crossing = 0, crossingWorlds = 0, giftChains = 0, giftWorlds = 0;
        int companions = 0, three = 0, four = 0, stories = 0, timelines = 0;
        long back = 0;
        foreach (FateTally t in tallies)
        {
            returns += t.Returns;
            if (t.Returns > 0) returnWorlds++;
            natural += t.NaturalReturns;
            if (t.NaturalReturns > 0)
            {
                naturalWorlds++;
                naturalByPeople[t.People] = naturalByPeople.GetValueOrDefault(t.People) + 1;
                naturalByGift[t.Gift] = naturalByGift.GetValueOrDefault(t.Gift) + 1;
            }
            foreach (string key in t.NaturalDeathKeys) causes[key] = causes.GetValueOrDefault(key) + 1;
            foreach (long b in t.TicksBack) back += b;
            deep += t.Deep;
            futures += t.Futures;
            withStory += t.FuturesWithStory;
            lost += t.LostStories;
            fateOrGift += t.FateOrGiftStories;
            if (t.FateOrGiftStories > 0) fateOrGiftWorlds++;
            crossing += t.CrossingStories;
            if (t.CrossingStories > 0) crossingWorlds++;
            giftChains += t.GiftChainStories;
            if (t.GiftChainStories > 0) giftWorlds++;
            companions += t.CompanionDeaths;
            if (t.ThreeInThree) three++;
            if (t.FourInThree) four++;
            stories += t.Stories;
            timelines += t.Returns + 1;
            worldsByPeople[t.People] = worldsByPeople.GetValueOrDefault(t.People) + 1;
            storiesByPeople[t.People] = storiesByPeople.GetValueOrDefault(t.People) + t.Stories;
            worldsByGift[t.Gift] = worldsByGift.GetValueOrDefault(t.Gift) + 1;
            if (t.GiftTarget) targets[t.Gift] = targets.GetValueOrDefault(t.Gift) + 1;
        }
        static IReadOnlyList<Tally> Sorted(Dictionary<string, int> d)
        {
            var list = new List<Tally>();
            foreach (KeyValuePair<string, int> kv in d) list.Add(new Tally(kv.Key, kv.Value));
            list.Sort((a, b) => a.Count != b.Count ? b.Count.CompareTo(a.Count) : string.CompareOrdinal(a.Key, b.Key));
            return list;
        }
        int allBack = 0;
        foreach (FateTally t in tallies) allBack += t.TicksBack.Count;
        return new FateSummary
        {
            Returns = returns,
            ReturnWorlds = returnWorlds,
            NaturalReturns = natural,
            NaturalReturnWorlds = naturalWorlds,
            AverageDaysBack = allBack == 0 ? 0 : Math.Round(back / (double)allBack / ticksPerDay, 2, MidpointRounding.AwayFromZero),
            DeepPermille = allBack == 0 ? 0 : deep * 1000 / allBack,
            LostFutures = futures,
            LostFuturesWithStory = withStory,
            LostStories = lost,
            FateOrGiftStories = fateOrGift,
            FateOrGiftWorlds = fateOrGiftWorlds,
            CrossingStories = crossing,
            CrossingWorlds = crossingWorlds,
            GiftChainStories = giftChains,
            GiftChainWorlds = giftWorlds,
            CompanionDeaths = companions,
            CompanionDeathsPerWorldYear = worlds == 0 ? 0 : Math.Round(companions / (double)(worlds * years), 2, MidpointRounding.AwayFromZero),
            ThreeDeathsInThreeDaysWorlds = three,
            FourDeathsInThreeDaysWorlds = four,
            StoriesPerTimeline = timelines == 0 ? 0 : Math.Round(stories / (double)timelines, 2, MidpointRounding.AwayFromZero),
            NaturalDeathsByCause = Sorted(causes),
            NaturalReturnWorldsByPeople = Sorted(naturalByPeople),
            NaturalReturnWorldsByGift = Sorted(naturalByGift),
            WorldsByPeople = Sorted(worldsByPeople),
            StoriesByPeople = Sorted(storiesByPeople),
            WorldsByGift = Sorted(worldsByGift),
            GiftTargetWorlds = Sorted(targets),
        };
    }

    internal static void Write(Utf8JsonWriter json, FateSummary f)
    {
        json.WriteStartObject("fates");
        json.WriteNumber("returns", f.Returns);
        json.WriteNumber("returnWorlds", f.ReturnWorlds);
        json.WriteNumber("naturalReturns", f.NaturalReturns);
        json.WriteNumber("naturalReturnWorlds", f.NaturalReturnWorlds);
        json.WriteNumber("averageDaysBack", f.AverageDaysBack);
        json.WriteNumber("deepPermille", f.DeepPermille);
        json.WriteNumber("lostFutures", f.LostFutures);
        json.WriteNumber("lostFuturesWithStory", f.LostFuturesWithStory);
        json.WriteNumber("lostStories", f.LostStories);
        json.WriteNumber("fateOrGiftStories", f.FateOrGiftStories);
        json.WriteNumber("fateOrGiftWorlds", f.FateOrGiftWorlds);
        json.WriteNumber("crossingStories", f.CrossingStories);
        json.WriteNumber("crossingWorlds", f.CrossingWorlds);
        json.WriteNumber("giftChainStories", f.GiftChainStories);
        json.WriteNumber("giftChainWorlds", f.GiftChainWorlds);
        json.WriteNumber("companionDeaths", f.CompanionDeaths);
        json.WriteNumber("companionDeathsPerWorldYear", f.CompanionDeathsPerWorldYear);
        json.WriteNumber("threeDeathsInThreeDaysWorlds", f.ThreeDeathsInThreeDaysWorlds);
        json.WriteNumber("fourDeathsInThreeDaysWorlds", f.FourDeathsInThreeDaysWorlds);
        json.WriteNumber("storiesPerTimeline", f.StoriesPerTimeline);
        WriteTallies(json, "naturalDeathsByCause", f.NaturalDeathsByCause);
        WriteTallies(json, "naturalReturnWorldsByPeople", f.NaturalReturnWorldsByPeople);
        WriteTallies(json, "naturalReturnWorldsByGift", f.NaturalReturnWorldsByGift);
        WriteTallies(json, "worldsByPeople", f.WorldsByPeople);
        WriteTallies(json, "storiesByPeople", f.StoriesByPeople);
        WriteTallies(json, "worldsByGift", f.WorldsByGift);
        WriteTallies(json, "giftTargetWorlds", f.GiftTargetWorlds);
        json.WriteEndObject();
    }

    private static void WriteTallies(Utf8JsonWriter json, string name, IReadOnlyList<Tally> tallies)
    {
        json.WriteStartObject(name);
        foreach (Tally t in tallies) json.WriteNumber(t.Key, t.Count);
        json.WriteEndObject();
    }
}
