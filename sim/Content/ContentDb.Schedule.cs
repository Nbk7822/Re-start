namespace Return.Sim.Content;

/// <summary>Kinds of day and the schedule's text (ADR-0029): loading, lookup and the checks made at load.</summary>
public sealed partial class ContentDb
{
    private Dictionary<string, RhythmDef> _rhythms = new();

    public IReadOnlyList<RhythmDef> Rhythms { get; private set; } = Array.Empty<RhythmDef>();

    public RhythmDef RhythmById(string id) => Find(_rhythms, id, "rhythms.json");
    public RhythmDef? TryRhythm(string id) => _rhythms.TryGetValue(id, out RhythmDef? def) ? def : null;

    private void LoadSchedule(List<string> dirs)
    {
        Rhythms = ReadMerged<RhythmDef>(dirs, "rhythms.json", d => d.Id);
        _rhythms = Index(Rhythms, d => d.Id);
    }

    /// <summary>
    /// Every kind of day covers each hour exactly once with a known block and has its text; the kinds the code
    /// falls back to exist; a species names a known kind, and sleeps in it exactly when it tires; the watch and
    /// its tuning make sense; and every line the schedule writes has its text, so a missing key fails here,
    /// not hours into a soak (ADR-0018 item 4).
    /// </summary>
    private void ValidateSchedule()
    {
        foreach (RhythmDef r in Rhythms)
        {
            RequireLoc("rhythms.json", r.Id, r.NameKey, r.CampDayKey);
            var covered = new int[24];
            foreach (RhythmRange range in r.Day)
            {
                if (Systems.ScheduleSystem.ParseBlock(range.Block) == null) Fail("rhythms.json", $"'{r.Id}' has block '{range.Block}'; use sleep, work or free");
                if (range.From is < 0 or > 23 || range.To is < 0 or > 23) { Fail("rhythms.json", $"'{r.Id}' has a range outside 0..23"); continue; }
                if (range.From == range.To) Fail("rhythms.json", $"'{r.Id}' has a range from {range.From} to {range.To}; to never equals from");
                for (int h = range.From; h != range.To; h = (h + 1) % 24) covered[h]++;
            }
            for (int h = 0; h < 24; h++)
                if (covered[h] != 1) Fail("rhythms.json", $"'{r.Id}' covers hour {h} {covered[h]} times; every hour must be covered exactly once");
        }
        foreach (string id in RequiredContent.Rhythms)
            if (!_rhythms.ContainsKey(id)) Fail("rhythms.json", $"the simulation needs a kind of day '{id}'");
        foreach (SpeciesDef s in Species)
        {
            string id = s.Rhythm.Length > 0 ? s.Rhythm : s.Tires ? Systems.ScheduleSystem.Diurnal : Systems.ScheduleSystem.Sleepless;
            if (!_rhythms.TryGetValue(id, out RhythmDef? r)) { Fail("species.json", $"'{s.Id}' keeps unknown kind of day '{id}'"); continue; }
            bool sleeps = r.Day.Exists(x => Systems.ScheduleSystem.ParseBlock(x.Block) == DayBlock.Sleep);
            if (s.Tires && !sleeps) Fail("species.json", $"'{s.Id}' tires but keeps '{id}', which has no hours of sleep");
            if (!s.Tires && sleeps) Fail("species.json", $"'{s.Id}' never tires but keeps '{id}', which has hours of sleep");
        }
        ScheduleDef d = World.Schedule;
        if (World.HoursPerDay != Systems.ScheduleSystem.HoursPerDay) Fail("world.json", $"hoursPerDay is {World.HoursPerDay}, but kinds of day and the watch are laid out over {Systems.ScheduleSystem.HoursPerDay} hours");
        if (d.Watch.From is < 0 or > 23 || d.Watch.To is < 0 or > 23) Fail("world.json", "schedule.watch hours must lie in 0..23");
        if (d.WatchTurnHours < 1) Fail("world.json", "schedule.watchTurnHours must be at least 1");
        foreach (string id in RequiredContent.ScheduleThoughts)
            if (!_thoughts.ContainsKey(id)) Fail("thoughts.json", $"the simulation needs a thought '{id}'");
        RequireLoc("loc/en.schedule.json", "schedule", RequiredContent.ScheduleText.ToArray());
    }
}
