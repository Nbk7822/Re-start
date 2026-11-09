namespace Return.Sim.Systems;

/// <summary>The relationship graph: directed opinions (Task 10); grudges, debts and promises are added in Task 11.</summary>
public static class Relations
{
    public static int Opinion(World w, int fromId, int toId)
    {
        Character? from = w.Find(fromId);
        if (from == null) return 0;
        foreach (Opinion o in from.Opinions)
            if (o.TargetId == toId) return o.Value;
        return 0;
    }

    /// <summary>Changes from's opinion of toId by delta (clamped to −100..100), keeping the list ordered by target id.</summary>
    public static void ChangeOpinion(World w, Character from, int toId, int delta, long cause)
    {
        if (toId == from.Id || delta == 0) return;
        List<Opinion> list = from.Opinions;
        int i = 0;
        while (i < list.Count && list[i].TargetId < toId) i++;
        if (i < list.Count && list[i].TargetId == toId)
            list[i].Value = Math.Clamp(list[i].Value + delta, -100, 100);
        else
            list.Insert(i, new Opinion { TargetId = toId, Value = Math.Clamp(delta, -100, 100) });
    }
}
