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
    IReadOnlyList<string> RecentThoughts);
public sealed record FactionState(string Id, string Name, IReadOnlyList<int> Members, int Berries, int Timber, int RoofSound, int RoofTotal, string RationPolicy);
public sealed record LocationState(int X, int Y, string Terrain, IReadOnlyList<string> Contents, IReadOnlyList<int> Characters);
public sealed record IncidentState(long EventId, long Tick, string When, string Key, string Text, IReadOnlyList<long> Consequences);

/// <summary>A person's priority for one kind of work: 1 is done first, then 2, 3, 4; 0 means never. Pending: a queued order will change it.</summary>
public sealed record WorkPriorityState(string WorkId, string Work, int Priority, bool Pending);

/// <summary>The camp's ration policy: what applies now, what will apply once pending orders land, and that policy in words.</summary>
public sealed record PolicyState(RationPolicy Current, RationPolicy Effective, bool Pending, string Text);

/// <summary>
/// The text-state API: structured, localised reads of the world for the view, the headless runner,
/// mods and AI agents. Reads never change state.
/// </summary>
public static class StateApi
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

        var needs = new List<NeedState>
        {
            species.DrinksBlood ? new("blood", loc.Get("need.blood"), c.Food) : new("food", loc.Get("need.food"), c.Food),
            new("rest", loc.Get("need.rest"), c.Rest),
            new("social", loc.Get("need.social"), c.Social),
            new("health", loc.Get("need.health"), c.Health),
        };

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
            c.Pos.X, c.Pos.Y, job, needs, c.Mood, c.Health, traits, opinions, grudges, debts, thoughts);
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
        if (m.TreeAt(p) != null) contents.Add(loc.Get("place.tree"));
        Bush? bush = m.BushAt(p);
        if (bush != null) contents.Add(loc.Format(bush.Berries == 1 ? "place.bush_one" : "place.bush", new[] { new KeyValuePair<string, string>("n", bush.Berries.ToString(Inv)) }));
        foreach (Bed b in m.Beds)
            if (b.Pos == p) contents.Add(loc.Get("place.bed"));
        if (m.Hearth == p) contents.Add(loc.Get("place.hearth"));
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
