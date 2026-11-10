using Return.Sim.Events;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// The sacred tree (P1, P2; ADR-0023). One of the trees nearest the hearth is held sacred by a faith
/// (or a culture). Its keepers never fell it; anyone else may, a lapsed follower included. If any
/// keeper is in camp to see it, the felling is a desecration: a Culture event, each keeper's bitter
/// thought about the feller, and a grudge where it crosses the line. Unwitnessed, it is marked
/// silently. Either way the stump stays desecrated and the deed is remembered for the gods.
/// </summary>
public static class SacredTrees
{
    /// <summary>Will this person leave this tree standing? Yes, if it is sacred to their people or faith and not already felled.</summary>
    public static bool Spares(World w, Character feller, Tree tree)
    {
        foreach (SacredPlace p in w.Culture.SacredPlaces)
            if (p.TreeId == tree.Id && !p.Desecrated && Ways.IsKeeper(feller, p.HolderId, p.HolderIsFaith)) return true;
        return false;
    }

    /// <summary>Called when <paramref name="feller"/> has felled <paramref name="tree"/> (event <paramref name="felled"/>).</summary>
    public static void Felled(World w, Character feller, Tree tree, SimEvent felled)
    {
        foreach (SacredPlace p in w.Culture.SacredPlaces)
        {
            if (p.TreeId != tree.Id || p.Desecrated) continue;
            p.Desecrated = true;
            var keepers = new List<Character>();
            foreach (Character m in w.Members())
                if (m.Id != feller.Id && Ways.IsKeeper(m, p.HolderId, p.HolderIsFaith)) keepers.Add(m);
            long deedEvent = felled.Id;
            if (keepers.Count > 0)
            {
                SimEvent e = w.Log.Emit(w, SystemTag.Culture, w.Content.CustomById(p.CustomId).EventKey("desecrated"), new[] { feller.Id }, new[] { felled.Id }, null,
                    EventArg.Char("a", feller.Id), EventArg.Key("ways", Ways.HolderNameKey(w, p.HolderId, p.HolderIsFaith) ?? ""));
                p.DesecratedEvent = e.Id;
                deedEvent = e.Id;
                foreach (Character k in keepers)
                {
                    MoodSystem.AddThought(w, k, "sacred_tree_felled", feller.Id, e.Id);
                    if (Relations.Opinion(w, k.Id, feller.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, k.Id, feller.Id) == null)
                        Relations.FormGrudge(w, k, feller, CultureTuning.SacrilegeGrudge, e.Id);
                }
            }
            Omens.RecordDeed(w, DeedKind.SacredHarmed, deedEvent, feller.Id);
        }
    }
}
