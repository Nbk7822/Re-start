using Return.Sim.Content;

namespace Return.Sim.Systems;

/// <summary>Reads a character's trait modifiers from content. Integer arithmetic only.</summary>
public static class Traits
{
    public static int Sum(World w, Character c, Func<TraitDef, int> pick)
    {
        int s = 0;
        foreach (string t in c.Traits) s += pick(w.Content.TraitById(t));
        return s;
    }

    public static int Max(World w, Character c, Func<TraitDef, int> pick)
    {
        int m = 0;
        foreach (string t in c.Traits) m = Math.Max(m, pick(w.Content.TraitById(t)));
        return m;
    }

    public static bool Any(World w, Character c, Func<TraitDef, bool> pick)
    {
        foreach (string t in c.Traits)
            if (pick(w.Content.TraitById(t))) return true;
        return false;
    }

    /// <summary>Multiplies percentage modifiers (each defaulting to 100) and returns the combined percentage.</summary>
    public static int ScalePct(World w, Character c, Func<TraitDef, int> pick)
    {
        int pct = 100;
        foreach (string t in c.Traits) pct = pct * pick(w.Content.TraitById(t)) / 100;
        return pct;
    }

    public static int WorkBias(World w, Character c, string workId)
    {
        int bias = 0;
        foreach (string t in c.Traits)
            foreach (WorkBias b in w.Content.TraitById(t).WorkBias)
                if (b.Work == workId) bias += b.Add;
        return bias;
    }
}
