using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Api;

public sealed record NeedState(string Need, string Label, int Value);
public sealed record OpinionState(int TargetId, string Target, int Value);
public sealed record GrudgeState(int Id, int TargetId, string Target, int Intensity, long CauseEvent, string Cause);
public sealed record DebtState(int Id, bool IOwe, int OtherId, string Other, string Kind, int Weight, string Status, long DueTick, long CauseEvent);
public sealed record CharacterState(
    int Id, string Name, string Species, string Status, int X, int Y, string Job,
    IReadOnlyList<NeedState> Needs, int Mood, int Health, IReadOnlyList<string> Traits,
    IReadOnlyList<OpinionState> Opinions, IReadOnlyList<GrudgeState> Grudges, IReadOnlyList<DebtState> Debts,
    IReadOnlyList<string> RecentThoughts, GrowthState Growth, BelongingState Belonging);

/// <summary>Where a trait came from: born with it (generated) or changed by an event (mutated); Text says which in plain words.</summary>
public sealed record TraitOriginState(string TraitId, string Trait, TraitOriginKind Origin, long CauseEvent, long Tick, string Text);

/// <summary>
/// What a person wants: its wants.json type, the want in plain words (in the second person for the
/// player), what it is about (a person's id and name, or a place or item id and name), and whether it is still held.
/// </summary>
public sealed record WantState(string TypeId, string Text, WantTargetKind TargetKind, int TargetId, string TargetRef, string Target,
    WantStatus Status, string StatusLabel, long FormedTick, long FormedEvent);

/// <summary>How far a person's arc toward their want has come, and since when it has not moved.</summary>
public sealed record ArcState(ArcStage Stage, string StageLabel, long StalledSinceTick, long LastEvent);

/// <summary>A person's growth (fixed decision 12): each trait with its origin, their want and their arc. Want and Arc are null until they enter the world.</summary>
public sealed record GrowthState(IReadOnlyList<TraitOriginState> Traits, WantState? Want, ArcState? Arc);
public sealed record FactionState(string Id, string Name, IReadOnlyList<int> Members, int Berries, int Timber, int RoofSound, int RoofTotal, string RationPolicy);
public sealed record LocationState(int X, int Y, string Terrain, IReadOnlyList<string> Contents, IReadOnlyList<int> Characters);
public sealed record IncidentState(long EventId, long Tick, string When, string Key, string Text, IReadOnlyList<long> Consequences);

/// <summary>A person's priority for one kind of work: 1 is done first, then 2, 3, 4; 0 means never. Pending: a queued order will change it.</summary>
public sealed record WorkPriorityState(string WorkId, string Work, int Priority, bool Pending);

/// <summary>The camp's ration policy: what applies now, what will apply once pending orders land, and that policy in words.</summary>
public sealed record PolicyState(RationPolicy Current, RationPolicy Effective, bool Pending, string Text);

/// <summary>A need from a person's nature (ADR-0022): its value (0..1000), how it stands (met, low, critical) and what binds it, in words.</summary>
public sealed record NatureNeedState(string Need, string Label, int Value, string Standing, string Bond);

/// <summary>A bargain (ADR-0022): who is bound to serve, who struck it for the camp, the service and price, and how it stands.</summary>
public sealed record ContractState(
    int Id, int BoundId, string Bound, int PatronId, string Patron, string Service, string Price, int Amount, string Status,
    long NextDueTick, long EndsTick, long FormedEvent);

/// <summary>
/// The text-state API: structured, localised reads of the world for the view, the headless runner,
/// mods and AI agents. Reads never change state.
/// </summary>
public static partial class StateApi
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string Json<T>(T state) => JsonSerializer.Serialize(state, JsonOptions);

    /// <summary>
    /// A person's work priorities for every kind of work priorities apply to, counting orders that
    /// are queued but not yet applied (so a paused game shows what the player just chose).
    /// </summary>
    public static IReadOnlyList<WorkPriorityState> WorkPriorities(World w, int id)
    {
        Character c = w.Get(id);
        var list = new List<WorkPriorityState>();
        foreach (string workId in Systems.JobPicker.PrioritizedWork)
        {
            int applied = Systems.JobPicker.PriorityOf(c, workId), priority = applied;
            foreach (Command cmd in w.PendingCommands)
                if (cmd is SetWorkPriority p && p.CharacterId == id && p.WorkId == workId) priority = p.Priority;
            list.Add(new WorkPriorityState(workId, w.Content.Loc.Get(w.Content.WorkById(workId).NameKey), priority, priority != applied));
        }
        return list;
    }

    /// <summary>The ration policy, counting a queued order that has not applied yet.</summary>
    public static PolicyState Policy(World w)
    {
        RationPolicy effective = w.Policy;
        bool pending = false;
        foreach (Command cmd in w.PendingCommands)
            if (cmd is SetRationPolicy p) { effective = p.Policy; pending = true; }
        return new PolicyState(w.Policy, effective, pending && effective != w.Policy, w.Content.Loc.Get(SetRationPolicy.PolicyKey(effective)));
    }

    public static CharacterState Character(World w, int id)
    {
        Character c = w.Get(id);
        LocTable loc = w.Content.Loc;
        SpeciesDef species = w.Content.SpeciesById(c.SpeciesId);

        // Only the needs this person's nature has: no food bar for those who eat nothing, no rest bar for those who never tire.
        var needs = new List<NeedState>();
        if (species.DrinksBlood) needs.Add(new("blood", loc.Get("need.blood"), c.Food));
        else if (!species.EatsNothing) needs.Add(new("food", loc.Get("need.food"), c.Food));
        foreach (NatureNeed n in c.NatureNeeds) needs.Add(new(n.Id, loc.Get(w.Content.NeedById(n.Id).NameKey), n.Value));
        if (species.Tires) needs.Add(new("rest", loc.Get("need.rest"), c.Rest));
        needs.Add(new("social", loc.Get("need.social"), c.Social));
        needs.Add(new("health", loc.Get("need.health"), c.Health));

        var traits = new List<string>();
        foreach (string t in c.Traits) traits.Add(loc.Get(w.Content.TraitById(t).NameKey));

        var opinions = new List<OpinionState>();
        foreach (Opinion o in c.Opinions) opinions.Add(new OpinionState(o.TargetId, EventText.Name(w, o.TargetId), o.Value));

        var grudges = new List<GrudgeState>();
        foreach (Grudge g in w.Grudges)
            if (g.HolderId == id && g.Active)
                grudges.Add(new GrudgeState(g.Id, g.TargetId, EventText.Name(w, g.TargetId), g.Intensity, g.CauseEvent, DescribeEvent(w, g.CauseEvent)));

        var debts = new List<DebtState>();
        foreach (Debt d in w.Debts)
        {
            if (d.DebtorId != id && d.CreditorId != id) continue;
            bool iOwe = d.DebtorId == id;
            int other = iOwe ? d.CreditorId : d.DebtorId;
            debts.Add(new DebtState(d.Id, iOwe, other, EventText.Name(w, other), loc.Get("debt." + Names.Snake(d.Kind.ToString())),
                d.Weight, loc.Get("debt_status." + Names.Snake(d.Status.ToString())), d.DueTick, d.CauseEvent));
        }

        var thoughts = new List<string>();
        for (int i = Math.Max(0, c.Thoughts.Count - 6); i < c.Thoughts.Count; i++)
        {
            ThoughtDef def = w.Content.ThoughtById(c.Thoughts[i].DefId);
            string sign = def.Mood > 0 ? "+" : "";
            thoughts.Add(loc.Get(def.TextKey) + " (" + sign + def.Mood.ToString(Inv) + ")");
        }

        string job = loc.Get("job." + Names.Snake((c.Job?.Kind ?? JobKind.Idle).ToString()));
        return new CharacterState(c.Id, loc.Get(c.NameKey), loc.Get(species.NameKey), loc.Get("status." + Names.Snake(c.Status.ToString())),
            c.Pos.X, c.Pos.Y, job, needs, c.Mood, c.Health, traits, opinions, grudges, debts, thoughts, Growth(w, id), Belonging(w, c));
    }

    /// <summary>
    /// A person's growth (fixed decision 12, ADR-0021): each trait with where it came from ("born with
    /// it", or "since day 12: " and the event that changed it), what they want in plain words, and how
    /// far their arc has come.
    /// </summary>
    public static GrowthState Growth(World w, int id)
    {
        Character c = w.Get(id);
        LocTable loc = w.Content.Loc;
        List<TraitOrigin> origins = c.Growth.TraitOrigins;
        var traits = new List<TraitOriginState>(c.Traits.Count);
        for (int i = 0; i < c.Traits.Count; i++)
        {
            string traitId = c.Traits[i];
            TraitOrigin? o = i < origins.Count && origins[i].TraitId == traitId ? origins[i] : origins.Find(x => x.TraitId == traitId);
            TraitOriginKind kind = o?.Kind ?? TraitOriginKind.Generated;
            string text = loc.Get("trait_origin.generated");
            if (o != null && kind == TraitOriginKind.Mutated)
            {
                string day = (Core.Calendar.Day(o.Tick, w.Content.World) + 1).ToString(Inv);
                string cause = DescribeEvent(w, o.CauseEvent);
                text = cause.Length == 0
                    ? loc.Format("trait_origin.mutated_bare", new[] { new KeyValuePair<string, string>("day", day) })
                    : loc.Format("trait_origin.mutated", new[] { new KeyValuePair<string, string>("day", day), new KeyValuePair<string, string>("cause", cause) });
            }
            traits.Add(new TraitOriginState(traitId, loc.Get(w.Content.TraitById(traitId).NameKey), kind, o?.CauseEvent ?? 0, o?.Tick ?? 0, text));
        }

        Want? want = c.Growth.Want;
        WantState? wantState = null;
        if (want != null)
        {
            WantDef def = w.Content.WantById(want.TypeId);
            string key = c.Id == w.PlayerId && loc.Has(def.TextKey + "_you") ? def.TextKey + "_you" : def.TextKey;
            (string target, string inText) = want.TargetKind switch
            {
                WantTargetKind.Person => (EventText.Name(w, want.TargetId), EventText.ResolveMid(w, "c:" + want.TargetId.ToString(Inv))),
                WantTargetKind.Place => (loc.Get("place." + want.TargetRef), loc.Get("place." + want.TargetRef)),
                WantTargetKind.Item => (loc.Get(w.Content.ItemById(want.TargetRef).NameKey), loc.Get(w.Content.ItemById(want.TargetRef).NameKey)),
                _ => ("", ""),
            };
            wantState = new WantState(want.TypeId, loc.Format(key, new[] { new KeyValuePair<string, string>("target", inText) }),
                want.TargetKind, want.TargetId, want.TargetRef, target, want.Status, loc.Get("want_status." + Names.Snake(want.Status.ToString())),
                want.FormedTick, want.FormedEvent);
        }

        Arc? arc = c.Growth.Arc;
        ArcState? arcState = arc == null ? null
            : new ArcState(arc.Stage, loc.Get("arc_stage." + Names.Snake(arc.Stage.ToString())), arc.StalledSinceTick, arc.LastEvent);
        return new GrowthState(traits, wantState, arcState);
    }

    /// <summary>
    /// The needs a person's nature adds (ADR-0022), each with how it stands and what binds it: the tree a
    /// dryad is bound to (or that it was cut down), the work an undead came back for, a djinn's terms.
    /// </summary>
    public static IReadOnlyList<NatureNeedState> NatureNeeds(World w, int id)
    {
        Character c = w.Get(id);
        LocTable loc = w.Content.Loc;
        var list = new List<NatureNeedState>();
        foreach (NatureNeed n in c.NatureNeeds)
        {
            NeedDef def = w.Content.NeedById(n.Id);
            string standing = loc.Get(n.Value < def.CriticalBelow ? "need_stage.critical" : n.Value < def.LowBelow ? "need_stage.low" : "need_stage.met");
            list.Add(new NatureNeedState(n.Id, loc.Get(def.NameKey), n.Value, standing, Bond(w, c, n)));
        }
        return list;
    }

    /// <summary>The bargains a person is party to, bound or patron, oldest first.</summary>
    public static IReadOnlyList<ContractState> Contracts(World w, int id)
    {
        LocTable loc = w.Content.Loc;
        var list = new List<ContractState>();
        foreach (Contract k in w.Contracts)
        {
            if (k.BoundId != id && k.PatronId != id) continue;
            list.Add(new ContractState(k.Id, k.BoundId, EventText.Name(w, k.BoundId), k.PatronId, EventText.Name(w, k.PatronId),
                loc.Get(Systems.Bargains.ServiceKey(k.ServiceWork)), loc.Get(Systems.Bargains.PriceKey(k.PriceItem)), k.PriceAmount,
                loc.Get("contract_status." + Names.Snake(k.Status.ToString())), k.NextDueTick, k.EndsTick, k.FormedEvent));
        }
        return list;
    }

    private static string Bond(World w, Character c, NatureNeed n)
    {
        LocTable loc = w.Content.Loc;
        string Say(string key, params (string Name, string Value)[] args)
        {
            var kv = new List<KeyValuePair<string, string>>(args.Length);
            foreach ((string name, string value) in args) kv.Add(new KeyValuePair<string, string>(name, value));
            return loc.Format(key, kv);
        }
        if (n.BondId != 0 && Systems.Forest.TreeById(w, n.BondId) is { } tree)
            return tree.Felled ? loc.Get("bond.tree_felled")
                : Say("bond.tree", ("x", tree.Pos.X.ToString(Inv)), ("y", tree.Pos.Y.ToString(Inv)));
        if (n.BondItem.Length > 0)
        {
            string terms = Say("bond.bargain_terms", ("service", loc.Get(Systems.Bargains.ServiceKey(n.BondWork))), ("price", loc.Get(Systems.Bargains.PriceKey(n.BondItem))));
            return Systems.Bargains.Binding(w, c.Id) is { } k
                ? Say("bond.bargain_open", ("terms", terms), ("patron", EventText.ResolveMid(w, "c:" + k.PatronId.ToString(Inv))), ("when", EventText.When(w, k.EndsTick)))
                : terms;
        }
        if (n.BondWork.Length > 0) return Say("bond.work", ("work", loc.Get(w.Content.WorkById(n.BondWork).NameKey)));
        return "";
    }

    public static FactionState Faction(World w, string id = "camp")
    {
        var members = new List<int>();
        foreach (Character c in w.Members()) members.Add(c.Id);
        int berries = 0, timber = 0;
        foreach (ItemStack s in w.Items)
        {
            if (!w.Map.Stockpile.Contains(s.Pos)) continue;
            if (s.DefId == "berries") berries += s.Count;
            else if (s.DefId == "timber") timber += s.Count;
        }
        int sound = 0;
        foreach (RoofSection r in w.Map.Roof)
            if (r.Sound) sound++;
        LocTable loc = w.Content.Loc;
        return new FactionState(id, loc.Get("faction.camp.name"), members, berries, timber, sound, w.Map.Roof.Count,
            loc.Get(SetRationPolicy.PolicyKey(w.Policy)));
    }

    public static LocationState Location(World w, int x, int y)
    {
        var p = new Point(x, y);
        Map m = w.Map;
        if (!m.InBounds(p)) throw new ArgumentOutOfRangeException(nameof(x), $"Tile {p} is outside the map.");
        LocTable loc = w.Content.Loc;
        var contents = new List<string>();
        if (m.TreeAt(p) != null) contents.Add(SacredTreeLine(w, p) ?? loc.Get("place.tree"));
        else if (SacredTreeLine(w, p) is { } stump) contents.Add(stump);
        Bush? bush = m.BushAt(p);
        if (bush != null) contents.Add(loc.Format(bush.Berries == 1 ? "place.bush_one" : "place.bush", new[] { new KeyValuePair<string, string>("n", bush.Berries.ToString(Inv)) }));
        foreach (Bed b in m.Beds)
            if (b.Pos == p) contents.Add(loc.Get("place.bed"));
        if (m.Hearth == p) contents.Add(FestivalHearthLine(w) ?? loc.Get("place.hearth"));
        if (m.Stockpile.Contains(p)) contents.Add(loc.Get("place.stockpile"));
        if (m.WatchPost == p) contents.Add(loc.Get("place.watch_post"));
        if (m.CoveredRuins.Contains(p)) contents.Add(loc.Get("place.covered_ruins"));
        foreach (RoofSection r in m.Roof)
            if (r.Area.Contains(p)) contents.Add(loc.Get(r.Sound ? "place.roof_sound" : "place.roof_broken"));
        foreach (ItemStack s in w.Items)
            if (s.Pos == p)
                contents.Add(loc.Format("place.item", new[]
                {
                    new KeyValuePair<string, string>("item", loc.Get(w.Content.ItemById(s.DefId).NameKey)),
                    new KeyValuePair<string, string>("n", s.Count.ToString(Inv)),
                }));
        var people = new List<int>();
        foreach (Character c in w.Characters)
            if (c.IsActive && c.Pos == p) people.Add(c.Id);
        return new LocationState(x, y, loc.Get("terrain." + Names.Snake(m.At(p).ToString())), contents, people);
    }

    public static IncidentState Incident(World w, long eventId)
    {
        SimEvent e = w.Log.Get(eventId) ?? throw new KeyNotFoundException("No event with id " + eventId);
        var consequences = new List<long>();
        for (int i = (int)eventId; i < w.Log.All.Count; i++)
            if (w.Log.All[i].Causes.Contains(eventId)) consequences.Add(w.Log.All[i].Id);
        return new IncidentState(e.Id, e.Tick, EventText.When(w, e.Tick), e.Key, EventText.Describe(w, e), consequences);
    }

    private static string DescribeEvent(World w, long id)
    {
        SimEvent? e = w.Log.Get(id);
        return e == null ? "" : EventText.Describe(w, e);
    }
}
