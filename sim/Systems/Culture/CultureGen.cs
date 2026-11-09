using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// Seeds a world's faiths (with their gods), cultures, festival dates, sacred trees and everyone's
/// belonging from the templates. Truths only, keyed by stable strings (template id, slot, cast key),
/// so the same seed gives the same peoples and gods in every timeline. It runs last in world
/// creation, takes no ids and emits no events (ADR-0023).
/// </summary>
public static class CultureGen
{
    public static ulong CultureKey(string templateId) => Fnv1a.Hash64("culture:" + templateId);

    public static ulong FaithKey(string templateId) => Fnv1a.Hash64("faith:" + templateId);

    public static ulong HolderKey(string holderId, bool holderIsFaith) => holderIsFaith ? FaithKey(holderId) : CultureKey(holderId);

    public static void Seed(World w)
    {
        ContentDb db = w.Content;
        TruthRng t = w.Rng.Truths;
        CultureBook book = w.Culture;

        foreach (FaithDef f in db.Faiths)
        {
            ulong key = FaithKey(f.Id);
            var faith = new SeededFaith
            {
                TemplateId = f.Id,
                NameKey = f.NameKeys[t.Range(TruthId.FaithNature, key, 0, 0, f.NameKeys.Count)],
                CustomIds = PickCustoms(db, t, TruthId.FaithNature, key, 1, f.Customs, f.CustomPicks),
            };
            for (int s = 0; s < f.Gods.Count; s++)
            {
                GodSlotDef slot = f.Gods[s];
                ulong gkey = Mix64.Combine(key, (ulong)s);
                var weights = new List<int>(slot.Domains.Count);
                foreach (DomainWeight d in slot.Domains) weights.Add(d.Weight);
                DomainDef domain = db.DomainById(slot.Domains[Ways.WeightedIndex(t, TruthId.GodNature, gkey, 0, weights)].Domain);
                faith.Gods.Add(new SeededGod
                {
                    Slot = s,
                    DomainId = domain.Id,
                    NameKey = slot.NameKeys[t.Range(TruthId.GodNature, gkey, 1, 0, slot.NameKeys.Count)],
                    EpithetKey = domain.EpithetKeys[t.Range(TruthId.GodNature, gkey, 2, 0, domain.EpithetKeys.Count)],
                    Wrath = t.Range(TruthId.GodNature, gkey, 3, slot.Wrath.Min, slot.Wrath.Max + 1),
                    Grace = t.Range(TruthId.GodNature, gkey, 4, slot.Grace.Min, slot.Grace.Max + 1),
                });
            }
            book.Faiths.Add(faith);
        }

        foreach (CultureDef c in db.Cultures)
        {
            ulong key = CultureKey(c.Id);
            var weights = new List<int>(c.Faiths.Count);
            foreach (FaithWeight fw in c.Faiths) weights.Add(fw.Weight);
            book.Cultures.Add(new SeededCulture
            {
                TemplateId = c.Id,
                NameKey = c.NameKeys[t.Range(TruthId.CultureNature, key, 0, 0, c.NameKeys.Count)],
                FaithId = c.Faiths[Ways.WeightedIndex(t, TruthId.CultureNature, key, 1, weights)].Faith,
                CustomIds = PickCustoms(db, t, TruthId.CultureNature, key, 2, c.Customs, c.CustomPicks),
            });
        }

        foreach ((string holder, bool isFaith, List<string> customs) in Holders(book))
            foreach (string id in customs)
            {
                CustomDef custom = db.CustomById(id);
                ulong key = Mix64.Combine(HolderKey(holder, isFaith), Fnv1a.Hash64(id));
                if (custom.Kind == "festival")
                    book.FestivalDates.Add(new FestivalDate
                    {
                        CustomId = id, HolderId = holder, HolderIsFaith = isFaith, Season = custom.Arg("season"),
                        Day = t.Range(TruthId.FestivalDate, key, 0, 1, Math.Max(2, w.Content.World.DaysPerSeason - 1)),
                    });
                else if (custom.Kind == "sacred_tree")
                {
                    List<Tree> near = TreesNearHearth(w, custom.Arg("nearestTrees"));
                    if (near.Count == 0) continue;
                    Tree tree = near[t.Range(TruthId.SacredTree, key, 0, 0, near.Count)];
                    book.SacredPlaces.Add(new SacredPlace { TreeId = tree.Id, HolderId = holder, HolderIsFaith = isFaith, CustomId = id });
                }
            }

        foreach (Character person in w.Characters)
            if (person.CastKey.Length > 0 && person.Named) Ways.Assign(w, person);
    }

    /// <summary>Faiths then cultures, in template order, with the customs each holds.</summary>
    private static IEnumerable<(string Holder, bool IsFaith, List<string> Customs)> Holders(CultureBook book)
    {
        foreach (SeededFaith f in book.Faiths) yield return (f.TemplateId, true, f.CustomIds);
        foreach (SeededCulture c in book.Cultures) yield return (c.TemplateId, false, c.CustomIds);
    }

    /// <summary>
    /// The always-held customs, then <paramref name="picks"/> distinct weighted draws from the pool
    /// (index <paramref name="firstIndex"/> + k), each skipping customs and kinds already held, so a
    /// holder keeps at most one custom of each kind.
    /// </summary>
    private static List<string> PickCustoms(ContentDb db, TruthRng t, TruthId id, ulong key, uint firstIndex, List<CustomRef> pool, int picks)
    {
        var held = new List<string>();
        var kinds = new List<string>();
        foreach (CustomRef r in pool)
            if (r.Always) { held.Add(r.Custom); kinds.Add(db.CustomById(r.Custom).Kind); }
        for (int k = 0; k < picks; k++)
        {
            int total = 0;
            foreach (CustomRef r in pool)
                if (Eligible(db, r, held, kinds)) total += r.Weight;
            if (total <= 0) break;
            int roll = t.Range(id, key, firstIndex + (uint)k, 0, total);
            foreach (CustomRef r in pool)
            {
                if (!Eligible(db, r, held, kinds)) continue;
                if (roll < r.Weight)
                {
                    held.Add(r.Custom);
                    kinds.Add(db.CustomById(r.Custom).Kind);
                    break;
                }
                roll -= r.Weight;
            }
        }
        return held;
    }

    private static bool Eligible(ContentDb db, CustomRef r, List<string> held, List<string> kinds) =>
        !r.Always && r.Weight > 0 && !held.Contains(r.Custom) && !kinds.Contains(db.CustomById(r.Custom).Kind);

    /// <summary>The standing trees nearest the hearth (Chebyshev distance, ties by id) that are not already sacred.</summary>
    private static List<Tree> TreesNearHearth(World w, int count)
    {
        var trees = new List<Tree>();
        foreach (Tree tree in w.Map.Trees)
        {
            if (tree.Felled) continue;
            bool sacred = false;
            foreach (SacredPlace p in w.Culture.SacredPlaces)
                if (p.TreeId == tree.Id) sacred = true;
            if (!sacred) trees.Add(tree);
        }
        Point hearth = w.Map.Hearth;
        trees.Sort((a, b) =>
        {
            int byDist = a.Pos.ChebyshevTo(hearth).CompareTo(b.Pos.ChebyshevTo(hearth));
            return byDist != 0 ? byDist : a.Id.CompareTo(b.Id);
        });
        if (trees.Count > count) trees.RemoveRange(count, trees.Count - count);
        return trees;
    }
}
