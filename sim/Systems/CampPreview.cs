using Return.Sim.Content;
using Return.Sim.Core;
using ZoneRules = Return.Sim.Systems.Zones;

namespace Return.Sim.Systems;

/// <summary>
/// The camp's zones and plans as they will be once the queued commands, then a view's drafts, have applied (ADR-0027,
/// ADR-0028; the merge review). One walk, in the order they will apply, checks each zone marking against the plans
/// before it and each plan against the zones before it, by the checks the commands themselves make
/// (<see cref="ZoneRules.Check"/>, <see cref="Building.Check"/>), so what the view shows is what the sim will do.
/// Placed plans asked to be called off, queued or drafted, count as gone from the start, as the Build view has always
/// read them: a call-off is made before the plan that takes its tiles. A copy: no events and no ids handed out; a zone
/// drafted while paused keeps its negative id. Only the view reads it: a command checks the world as it is when it applies.
/// </summary>
public sealed class CampPreview
{
    private readonly World _w;
    private readonly List<int> _callingOff;
    private int _nextZoneId;

    private CampPreview(World w, List<int> callingOff)
    {
        _w = w;
        _callingOff = callingOff;
        Zones = ZoneRules.Clone(w.Map.Zones);
        _nextZoneId = w.NextId;
    }

    /// <summary>The zones as they will be, in id order with zones made since last.</summary>
    public List<Zone> Zones { get; private set; }

    /// <summary>The plan pieces asked for but not yet on the map, in the order they will be placed: each could go in its turn.</summary>
    public List<PlannedPiece> Planned { get; } = new();

    /// <summary>The placed plans asked to be called off but still on the map: what is planned or marked may take their tiles.</summary>
    public IReadOnlyList<int> CallingOff => _callingOff;

    /// <summary>The camp once the queued commands and then <paramref name="drafts"/> (if any) have applied.</summary>
    public static CampPreview Of(World w, IEnumerable<Command>? drafts = null)
    {
        List<Command> later = drafts == null ? new List<Command>() : new List<Command>(drafts);
        CampPreview preview = Queued(w, later);
        foreach (Command c in later) preview.Take(c);
        return preview;
    }

    /// <summary>
    /// The camp once the queued commands have applied, with the call-offs among <paramref name="drafts"/> already counted as
    /// gone, ready to <see cref="Take"/> the drafts one at a time (the Build view checks each draft against those before it).
    /// </summary>
    public static CampPreview Queued(World w, IEnumerable<Command>? drafts = null)
    {
        var gone = new List<int>();
        void CallOffs(IEnumerable<Command> commands)
        {
            foreach (Command c in commands)
                if (c is CancelBlueprints cancel)
                    foreach (int id in cancel.BlueprintIds)
                        if (!gone.Contains(id)) gone.Add(id);
        }
        CallOffs(w.PendingCommands);
        if (drafts != null) CallOffs(drafts);
        var preview = new CampPreview(w, gone);
        foreach (Command c in w.PendingCommands) preview.Take(c);
        return preview;
    }

    /// <summary>
    /// One more command, as it will apply after those taken so far: a plan places the pieces that can go (against the zones
    /// and plans so far), a zone command changes the zones by their own rules (its tiles checked against the plans so far)
    /// unless that would leave an item with nowhere to be kept; anything else changes nothing here.
    /// </summary>
    public void Take(Command c)
    {
        if (c is PlaceBlueprint plan)
        {
            foreach (Rect r in Building.Placeable(_w, plan.BuildingId, plan.Pieces, Planned, _callingOff, Zones))
                Planned.Add(new PlannedPiece(plan.BuildingId, r));
            return;
        }
        List<Zone> trial = ZoneRules.Clone(Zones);
        bool ok;
        switch (c)
        {
            case MarkZone m:
            {
                int id = m.ZoneId < 0 ? m.ZoneId : _nextZoneId;
                ZoneRules.Outcome? o = ZoneRules.Mark(_w, trial, m.ZoneId, m.Kind, m.Tiles, m.Items, () => id, Planned, _callingOff);
                ok = o?.Zone != null;
                if (ok && o!.Created && m.ZoneId >= 0) _nextZoneId++;
                break;
            }
            case RemoveZone r:
                ok = trial.RemoveAll(z => z.Id == r.ZoneId) > 0;
                break;
            case SetZoneItems s:
                ok = ZoneRules.SetItems(_w, trial, s.ZoneId, s.Items) != null;
                break;
            default:
                return;
        }
        if (ok && ZoneRules.EveryItemKept(_w, trial)) Zones = trial;
    }

    /// <summary>Why a zone of this kind may not lie on a tile once everything taken so far has applied (null if it may).</summary>
    public string? ZoneCheck(ZoneDef def, Point p) => ZoneRules.Check(_w, def, p, Planned, _callingOff);
}
