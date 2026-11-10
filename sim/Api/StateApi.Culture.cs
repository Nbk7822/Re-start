using System.Globalization;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems.Culture;

namespace Return.Sim.Api;

/// <summary>A god in plain words: "Ostrel", "Keeper of Oaths", "stern", "hates a broken promise and loves a kept one".</summary>
public sealed record GodState(string Name, string Epithet, string Temperament, string Cares);

/// <summary>
/// Where someone belongs, in plain words and no numbers (ADR-0023): the one-line summary ("Of the
/// Hearthfolk · follows the Sworn Word (devout)"), its parts, the rules they live by (customs, the
/// lines their ethics give, how their people regard others), their faith's gods, and the ways of other
/// peoples they have learned here.
/// </summary>
public sealed record BelongingState(string Line, string Culture, string Faith, string Devotion,
    IReadOnlyList<string> Ways, IReadOnlyList<string> Gods, IReadOnlyList<string> Learned);

/// <summary>A people of this world: its name, the named people raised in it (ids), its main faith, its customs (festivals dated), and its stances toward other peoples.</summary>
public sealed record CultureState(string Id, string Name, IReadOnlyList<int> Members, string Faith,
    IReadOnlyList<string> Ways, IReadOnlyList<string> Stances, IReadOnlyList<string> Festivals);

/// <summary>A faith of this world: its name, its named followers (ids), its gods and its customs (festivals dated).</summary>
public sealed record FaithState(string Id, string Name, IReadOnlyList<int> Followers, IReadOnlyList<GodState> Gods, IReadOnlyList<string> Ways);

public static partial class StateApi
{
    public static BelongingState Belonging(World w, Character c)
    {
        LocTable loc = w.Content.Loc;
        SeededCulture? culture = Ways.Culture(w, c);
        SeededFaith? faith = Ways.Faith(w, c);
        var ways = new List<string>();
        var gods = new List<string>();
        var learned = new List<string>();

        foreach (string id in c.LearnedCustoms)
        {
            SeededCulture? from = null;
            foreach (SeededCulture s in w.Culture.Cultures)
                if (s.CustomIds.Contains(id)) { from = s; break; }
            if (from == null) continue;
            learned.Add(loc.Format("belong.learned", Args(("culture", loc.Get(from.NameKey)), ("custom", loc.Get(w.Content.CustomById(id).NameKey)))));
        }

        if (culture == null)
        {
            if (learned.Count == 0) ways.Add(loc.Get("belong.no_ways"));
            return new BelongingState(loc.Get("belong.outsider"), "", "", "", ways, gods, learned);
        }

        foreach (KeptCustom k in Ways.KeptCustoms(w, c)) ways.Add(loc.Get(k.Def.RuleKey));
        int oath = Ways.Axis(w, c, EthicAxis.OathboundFree), signs = Ways.Axis(w, c, EthicAxis.ArcaneMundane);
        if (oath >= 1) ways.Add(loc.Get("ethic.oath_heavy"));
        else if (oath <= -1) ways.Add(loc.Get("ethic.oath_light"));
        if (signs >= 1) ways.Add(loc.Get("ethic.signs_many"));
        else if (signs <= -1) ways.Add(loc.Get("ethic.signs_few"));
        foreach (AttitudeDef a in w.Content.CultureById(culture.TemplateId).Attitudes)
            ways.Add(loc.Format("stance." + a.Stance, Args(("people", loc.Get("species." + a.Species + ".plural")))));

        string cultureName = loc.Get(culture.NameKey);
        if (faith == null)
            return new BelongingState(loc.Format("belong.line_no_faith", Args(("culture", cultureName))), cultureName, "", "", ways, gods, learned);

        foreach (SeededGod g in faith.Gods)
        {
            GodState god = God(w, g);
            gods.Add(loc.Format("belong.god", Args(("god", god.Name), ("epithet", god.Epithet), ("temperament", god.Temperament), ("cares", god.Cares))));
        }
        string faithName = loc.Get(faith.NameKey), devotion = loc.Get(Ways.DevotionKey(c.Devotion));
        string line = loc.Format("belong.line", Args(("culture", cultureName), ("faith", faithName), ("devotion", devotion)));
        return new BelongingState(line, cultureName, faithName, devotion, ways, gods, learned);
    }

    public static IReadOnlyList<CultureState> Cultures(World w)
    {
        var list = new List<CultureState>(w.Culture.Cultures.Count);
        foreach (SeededCulture s in w.Culture.Cultures) list.Add(Culture(w, s.TemplateId));
        return list;
    }

    public static CultureState Culture(World w, string id)
    {
        SeededCulture s = Ways.CultureById(w, id) ?? throw new KeyNotFoundException("No culture with id " + id);
        LocTable loc = w.Content.Loc;
        var members = new List<int>();
        foreach (Character c in w.Characters)
            if (c.Named && c.CultureId == id) members.Add(c.Id);
        var stances = new List<string>();
        foreach (AttitudeDef a in w.Content.CultureById(id).Attitudes)
            stances.Add(loc.Format("stance." + a.Stance + ".many", Args(("people", loc.Get("species." + a.Species + ".plural")))));
        (List<string> ways, List<string> festivals) = CustomNames(w, s.CustomIds, id, false);
        SeededFaith? faith = Ways.FaithById(w, s.FaithId);
        return new CultureState(id, loc.Get(s.NameKey), members, faith == null ? "" : loc.Get(faith.NameKey), ways, stances, festivals);
    }

    public static IReadOnlyList<FaithState> Faiths(World w)
    {
        var list = new List<FaithState>(w.Culture.Faiths.Count);
        foreach (SeededFaith s in w.Culture.Faiths) list.Add(Faith(w, s.TemplateId));
        return list;
    }

    public static FaithState Faith(World w, string id)
    {
        SeededFaith s = Ways.FaithById(w, id) ?? throw new KeyNotFoundException("No faith with id " + id);
        var followers = new List<int>();
        foreach (Character c in w.Characters)
            if (c.Named && c.FaithId == id) followers.Add(c.Id);
        var gods = new List<GodState>(s.Gods.Count);
        foreach (SeededGod g in s.Gods) gods.Add(God(w, g));
        (List<string> ways, List<string> _) = CustomNames(w, s.CustomIds, id, true);
        return new FaithState(id, w.Content.Loc.Get(s.NameKey), followers, gods, ways);
    }

    private static GodState God(World w, SeededGod g)
    {
        LocTable loc = w.Content.Loc;
        return new GodState(loc.Get(g.NameKey), loc.Get(g.EpithetKey), loc.Get(Ways.TemperamentKey(g)), loc.Get(w.Content.DomainById(g.DomainId).CaresKey));
    }

    /// <summary>
    /// Custom names in held order, a festival with its day ("the harvest fire (day 4 of autumn)": the
    /// season's mid-sentence form, "season.&lt;id&gt;_mid", when the table has one); and the festivals alone.
    /// </summary>
    private static (List<string> Ways, List<string> Festivals) CustomNames(World w, List<string> customIds, string holderId, bool holderIsFaith)
    {
        LocTable loc = w.Content.Loc;
        WorldDef def = w.Content.World;
        var ways = new List<string>(customIds.Count);
        var festivals = new List<string>();
        foreach (string id in customIds)
        {
            string name = loc.Get(w.Content.CustomById(id).NameKey);
            FestivalDate? date = null;
            foreach (FestivalDate f in w.Culture.FestivalDates)
                if (f.CustomId == id && f.HolderId == holderId && f.HolderIsFaith == holderIsFaith) { date = f; break; }
            if (date != null)
            {
                string season = "";
                if (date.Season < def.SeasonKeys.Length)
                {
                    string key = def.SeasonKeys[date.Season];
                    season = loc.Get(loc.Has(key + "_mid") ? key + "_mid" : key);
                }
                name = loc.Format("belong.dated", Args(("custom", name), ("day", (date.Day + 1).ToString(CultureInfo.InvariantCulture)), ("season", season)));
                festivals.Add(name);
            }
            ways.Add(name);
        }
        return (ways, festivals);
    }

    /// <summary>"Tree (sacred to the Old Roots)", or for a felled one "Stump (once sacred to the Old Roots)"; null if no sacred tree stands or stood here.</summary>
    private static string? SacredTreeLine(World w, Point p)
    {
        foreach (SacredPlace s in w.Culture.SacredPlaces)
        {
            Tree? tree = w.Map.Trees.Find(t => t.Id == s.TreeId);
            if (tree == null || tree.Pos != p) continue;
            string? holder = Ways.HolderNameKey(w, s.HolderId, s.HolderIsFaith);
            if (holder == null) continue;
            return w.Content.Loc.Format(tree.Felled ? "place.stump_sacred" : "place.tree_sacred", Args(("ways", w.Content.Loc.Get(holder))));
        }
        return null;
    }

    /// <summary>"Hearth (keeping the harvest fire)" while a festival is under way; null otherwise.</summary>
    private static string? FestivalHearthLine(World w)
    {
        if (w.Culture.Festivals.Count == 0) return null;
        FestivalRun run = w.Culture.Festivals[0];
        return w.Content.Loc.Format("place.hearth_festival", Args(("festival", w.Content.Loc.Get(w.Content.CustomById(run.CustomId).NameKey))));
    }

    private static KeyValuePair<string, string>[] Args(params (string Name, string Value)[] args)
    {
        var pairs = new KeyValuePair<string, string>[args.Length];
        for (int i = 0; i < args.Length; i++) pairs[i] = new KeyValuePair<string, string>(args[i].Name, args[i].Value);
        return pairs;
    }
}
