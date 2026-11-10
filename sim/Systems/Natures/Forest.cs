using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems.Jobs;

namespace Return.Sim.Systems;

/// <summary>
/// The dryad's need (ADR-0022). The tree nymphs of Greek myth draw their life from the trees, and a
/// hamadryad is bound to one tree, living and dying with it. Here the forest is what feeds a dryad:
/// the need rises among living trees (their own counts for more) and falls away from them. Each is
/// bound to one tree near the camp, a Truth of their nature. Felling any tree grieves them and turns
/// them against the feller; felling their own wounds them, is never forgotten, and leaves them with
/// less to draw on. Those who think well of a dryad leave their tree standing.
/// </summary>
public static class Forest
{
    public const string NeedId = "forest";

    /// <summary>
    /// Binds the dryad to one tree near the camp: trees ranked by distance from the hearth (then id),
    /// skipping the nearest few (the roof's first timber), one picked from Truths keyed by cast key.
    /// </summary>
    public static void Bind(World w, Character c, NatureNeed n, NeedDef def)
    {
        var ranked = new List<Tree>(w.Map.Trees);
        Point hearth = w.Map.Hearth;
        ranked.Sort((a, b) =>
        {
            int byDistance = a.Pos.ChebyshevTo(hearth).CompareTo(b.Pos.ChebyshevTo(hearth));
            return byDistance != 0 ? byDistance : a.Id.CompareTo(b.Id);
        });
        int from = Math.Min(def.Param("bondSkipNearest"), ranked.Count - 1);
        int span = Math.Min(def.Param("bondCandidates"), ranked.Count - from);
        if (from < 0 || span <= 0) return;
        n.BondId = ranked[from + w.Rng.Truths.Range(TruthId.BoundTree, Fnv1a.Hash64(c.CastKey), 0, 0, span)].Id;
    }

    /// <summary>Living trees within reach feed the need each hour; their own tree counts for more.</summary>
    public static int Restore(World w, Character c, NatureNeed n, NeedDef def)
    {
        int range = def.Param("treeRange"), own = def.Param("ownTreeWeight"), weight = 0;
        foreach (Tree t in w.Map.Trees)
            if (!t.Felled && t.Pos.ChebyshevTo(c.Pos) <= range) weight += t.Id == n.BondId ? own : 1;
        if (weight == 0) return 0;
        MoodSystem.AddThought(w, c, "among_trees", 0, 0);
        return Math.Min(def.Param("maxRestorePerHour"), weight * def.Param("restorePerTree"));
    }

    /// <summary>Faint from too long away, they go and stand by their own tree (or, if it is gone, in the thickest grove left).</summary>
    public static Job? Answer(World w, Character c, NatureNeed n, bool sunBlocked)
    {
        if (sunBlocked) return null;
        Tree? tree = TreeById(w, n.BondId) is { Felled: false } own ? own : Grove(w, c.Pos, w.Content.NeedById(NeedId).Param("treeRange"));
        if (tree == null) return null;
        Point spot = Places.Adjacent(w, tree.Pos, c.Pos);
        return spot == Point.None ? null : new Job { Kind = JobKind.TendTree, TargetId = tree.Id, Target = spot, CauseEvent = n.LastEvent };
    }

    /// <summary>The TendTree job: walk to the tree, then stay by it until the need is met (or a while has passed). The hourly upkeep does the feeding.</summary>
    public static bool Tend(World w, Character c, Job j)
    {
        Tree? tree = TreeById(w, j.TargetId);
        if (tree == null || tree.Felled) return true;
        if (j.Stage == 0)
        {
            if (!JobRunner.At(w, c, j)) return JobRunner.Failed(j);
            j.Stage = 1;
            c.Facing = TravelSystem.FacingFor(c.Pos, tree.Pos);
            return false;
        }
        j.Progress++;
        NeedDef def = w.Content.NeedById(NeedId);
        return NatureNeeds.Get(c, NeedId) is not { } n || n.Value >= def.SatisfiedAt || j.Progress >= def.Param("tendMaxTicks");
    }

    /// <summary>Would this person leave the tree standing? A dryad's own tree is spared by the dryad and by anyone who thinks well enough of them.</summary>
    public static bool Spares(World w, Character feller, Tree tree)
    {
        Character? dryad = BoundTo(w, tree);
        if (dryad == null) return false;
        int respect = w.Content.NeedById(NeedId).Param("respectOpinion");
        return dryad.Id == feller.Id || Relations.Opinion(w, feller.Id, dryad.Id) >= respect;
    }

    /// <summary>The camp member bound to this tree, if any.</summary>
    public static Character? BoundTo(World w, Tree tree)
    {
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member && NatureNeeds.Get(c, NeedId) is { } n && n.BondId == tree.Id) return c;
        return null;
    }

    /// <summary>
    /// A tree came down. The dryad bound to it cries out, is wounded, loses much of what feeds them and
    /// holds the feller to account; every other dryad grieves it, and repeated felling turns grief into a grudge.
    /// </summary>
    public static void Felled(World w, Character feller, Tree tree, SimEvent felled)
    {
        NeedDef def = w.Content.NeedById(NeedId);
        foreach (Character d in w.Characters)
        {
            if (d.Status != CharStatus.Member || d.Id == feller.Id || NatureNeeds.Get(d, NeedId) is not { } n) continue;
            if (n.BondId == tree.Id) Wound(w, feller, d, n, def, tree, felled);
            else Mourn(w, feller, d, felled);
        }
    }

    private static void Wound(World w, Character feller, Character dryad, NatureNeed n, NeedDef def, Tree tree, SimEvent felled)
    {
        int damage = w.Rng.Dice[DiceSystem.Work].Range(DiceId.TreeWound, def.Param("woundMin"), def.Param("woundMax") + 1);
        SimEvent cry = w.Log.Emit(w, SystemTag.Needs, "event.needs.tree_wound", new[] { dryad.Id, feller.Id }, new[] { felled.Id },
            new[] { new Effect { Kind = EffectKind.BondSevered, A = dryad.Id, B = feller.Id, RefId = tree.Id } }, EventArg.Char("a", dryad.Id));
        NatureNeeds.Adjust(w, dryad, n, -def.Param("ownTreeLoss"), cry.Id);
        MoodSystem.AddThought(w, dryad, "own_tree_felled", feller.Id, cry.Id);
        CombatSystem.Hurt(w, dryad, damage, cry.Id, "event.needs.died_with_tree", SystemTag.Needs);
        if (dryad.Status == CharStatus.Member) Relations.FormGrudge(w, dryad, feller, def.Param("grudgeIntensity"), cry.Id);
    }

    private static void Mourn(World w, Character feller, Character dryad, SimEvent felled)
    {
        SimEvent grief = w.Log.Emit(w, SystemTag.Social, "event.social.mourned_tree", new[] { dryad.Id, feller.Id }, new[] { felled.Id }, null,
            EventArg.Char("a", dryad.Id), EventArg.Char("b", feller.Id));
        MoodSystem.AddThought(w, dryad, "tree_felled", feller.Id, grief.Id);
        if (Relations.Opinion(w, dryad.Id, feller.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, dryad.Id, feller.Id) == null)
            Relations.FormGrudge(w, dryad, feller, w.Content.NeedById(NeedId).Param("griefGrudgeIntensity"), grief.Id);
    }

    public static Tree? TreeById(World w, int id)
    {
        foreach (Tree t in w.Map.Trees)
            if (t.Id == id) return t;
        return null;
    }

    /// <summary>The standing tree with the most living trees within reach of it (a grove), the nearest of those if several tie.</summary>
    private static Tree? Grove(World w, Point from, int range)
    {
        Tree? best = null;
        int bestCount = -1, bestDist = int.MaxValue;
        foreach (Tree t in w.Map.Trees)
        {
            if (t.Felled || Places.Adjacent(w, t.Pos, from) == Point.None) continue;
            int count = 0;
            foreach (Tree o in w.Map.Trees)
                if (!o.Felled && o.Pos.ChebyshevTo(t.Pos) <= range) count++;
            int d = t.Pos.ChebyshevTo(from);
            if (count > bestCount || (count == bestCount && d < bestDist)) { best = t; bestCount = count; bestDist = d; }
        }
        return best;
    }
}
