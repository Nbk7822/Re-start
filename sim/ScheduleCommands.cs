namespace Return.Sim;

// The Schedule's commands (ADR-0029). Each is an intention the player states, tick-stamped and recorded like
// the ration policy; one command carries a whole day, so one decision is one Chronicle line. A change of hours
// never pulls anyone off the job in hand: their next choice follows the new day.

/// <summary>Paints a kind of day's camp day (24 blocks). Those who keep it may go their own way over the change (Compliance).</summary>
public sealed record SetCampDay(long Tick, string Rhythm, IReadOnlyList<DayBlock> Hours) : Command(Tick)
{
    internal override void Apply(World w) => Systems.ScheduleSystem.ApplyCampDay(w, this);
}

/// <summary>Sets the hours the camp keeps a watch: from inclusive, to exclusive, wrapping; equal means no watch.</summary>
public sealed record SetWatchHours(long Tick, int From, int To) : Command(Tick)
{
    internal override void Apply(World w) => Systems.ScheduleSystem.ApplyWatchHours(w, this);
}

/// <summary>Sets one person's own hours (24 entries; null keeps the camp's day at that hour; all null: the camp's day again). They may go their own way (Compliance).</summary>
public sealed record SetPersonDay(long Tick, int CharacterId, IReadOnlyList<DayBlock?> Hours) : Command(Tick)
{
    internal override void Apply(World w) => Systems.ScheduleSystem.ApplyPersonDay(w, this);
}
