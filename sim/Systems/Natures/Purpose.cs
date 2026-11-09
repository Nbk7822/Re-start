using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim.Systems;

/// <summary>
/// The undead's need (ADR-0022). The revenants and draugr of European folklore come back to do
/// something; here each is bound to one work, a Truth of their nature. Doing it restores them,
/// and they lean toward it over other work; without it they fade, and fading harms them until they
/// lie down at rest. Setting that work to "never" is what takes a purpose away, so the player's
/// order becomes the cause of what follows; winter does the same to one who came back to forage.
/// </summary>
public static class Purpose
{
    public const string NeedId = "purpose";

    public static void Bind(World w, Character c, NatureNeed n, NeedDef def)
    {
        if (def.BondWorks.Count == 0) return;
        int pick = w.Rng.Truths.Range(TruthId.PurposeWork, Fnv1a.Hash64(c.CastKey), 0, 0, def.BondWorks.Count);
        n.BondWork = def.BondWorks[pick];
    }

    /// <summary>Restores the need for each hour spent at the bound work; notes winter as the cause when it strips a forager's bushes.</summary>
    public static int Restore(World w, Character c, NatureNeed n, NeedDef def)
    {
        bool doing = Doing(c, n);
        if (!doing && n.LastEvent == 0 && n.BondWork == "forage" && w.WinterEvent != 0 && n.Value < def.SatisfiedAt) n.LastEvent = w.WinterEvent;
        if (!doing) return 0;
        MoodSystem.AddThought(w, c, "at_purpose", 0, 0);
        return def.Param("restorePerHour");
    }

    public static bool Doing(Character c, NatureNeed n) =>
        c.Job != null && n.BondWork.Length > 0 && Names.Snake(c.Job.Kind.ToString()) == n.BondWork;

    /// <summary>
    /// Is this the work they came back for, and is their purpose short of met? Then they lean toward it,
    /// and nobody else on watch, nor a full stockpile, keeps them from it. Once it is met they work like
    /// anyone else, so a sleepless forager does not strip every bush (soak: it ended winter hunger).
    /// </summary>
    public static bool Seeks(World w, Character c, string workId) =>
        NatureNeeds.Get(c, NeedId) is { } n && n.BondWork == workId && n.Value < w.Content.NeedById(NeedId).SatisfiedAt;

    /// <summary>How much they lean toward their bound work while their purpose is short of met (a work bias, as traits give).</summary>
    public static int WorkBias(World w, Character c, string workId) =>
        Seeks(w, c, workId) ? w.Content.NeedById(NeedId).Param("boundWorkBias") : 0;

    /// <summary>Their purpose slipping, they go to the bound work if there is any to do and their priorities allow it at all.</summary>
    public static Job? Answer(World w, Character c, NatureNeed n, bool sunBlocked) =>
        n.BondWork.Length == 0 || JobPicker.PriorityOf(c, n.BondWork) <= 0 ? null : JobPicker.WorkCandidate(w, c, n.BondWork, sunBlocked);

    /// <summary>A work priority changed: setting the work someone came back for to "never" takes their purpose away, and is the cause of their fading.</summary>
    public static void OnPriority(World w, Character c, string workId, int priority, long eventId)
    {
        if (priority > 0 || NatureNeeds.Get(c, NeedId) is not { } n || n.BondWork != workId) return;
        n.LastEvent = eventId;
    }
}
