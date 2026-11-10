using Return.Sim;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Runner;

/// <summary>
/// The Schedule's soak measures for one world (ADR-0029; design 8.2), sampled every 30 ticks between steps: it only
/// reads, so a world's log is the same whether it is measured or not. Hours asleep per human-day and turn lengths are
/// kept as histograms in tenths of an hour, so totals over worlds never depend on order.
/// </summary>
internal sealed class DaysMeter
{
    public const int EveryTicks = 30;
    private const int Bins = 241;

    public readonly int[] AsleepTenths = new int[Bins];
    public readonly int[] TurnTenths = new int[Bins];
    public long NightSamples, NightAwakeOffWatch, WatchSamples, WatchKept;

    private readonly List<(int Id, int Asleep)> _today = new();
    private int _day = -1, _watcher;
    private long _watcherSince;

    public void Sample(World w)
    {
        var def = w.Content.World;
        int day = Calendar.Day(w.Tick, def), hour = Calendar.Hour(w.Tick, def);
        if (day != _day)
        {
            FlushDay();
            _day = day;
        }
        Character? watcher = null;
        foreach (Character m in w.Characters)
            if (m.Status == CharStatus.Member && !m.Asleep && m.Job is { Kind: JobKind.KeepWatch, Stage: >= 1 } j && m.Pos == j.Target) { watcher = m; break; }
        int id = watcher?.Id ?? 0;
        if (id != _watcher)
        {
            if (_watcher != 0) Add(TurnTenths, (int)((w.Tick - _watcherSince) * 10 / def.TicksPerHour));
            _watcher = id;
            _watcherSince = w.Tick;
        }
        if (ScheduleSystem.InWatchHours(w, hour) && !Return.Sim.Systems.Culture.Festivals.RunsAt(w, w.Tick))
        {
            WatchSamples++;
            if (watcher != null) WatchKept++;
        }
        foreach (Character m in w.Characters)
        {
            if (m.Status != CharStatus.Member || m.SpeciesId != "human") continue;
            int at = _today.FindIndex(x => x.Id == m.Id);
            if (at < 0) { _today.Add((m.Id, 0)); at = _today.Count - 1; }
            if (m.Asleep) _today[at] = (m.Id, _today[at].Asleep + 1);
            if (hour is >= 1 and < 5)
            {
                NightSamples++;
                if (!m.Asleep && m.Job is not { Kind: JobKind.KeepWatch or JobKind.WakeForWatch }) NightAwakeOffWatch++;
            }
        }
    }

    public void Finish()
    {
        FlushDay();
        _watcher = 0;
    }

    private void FlushDay()
    {
        // Samples are six minutes apart: ten make an hour, so a day's count is its hours asleep in tenths.
        foreach ((int _, int asleep) in _today) Add(AsleepTenths, asleep);
        _today.Clear();
    }

    private static void Add(int[] hist, int tenths) => hist[Math.Clamp(tenths, 0, Bins - 1)]++;

    /// <summary>The value at a fraction of a histogram's count (0.5 the median, 0.9 the 90th percentile), in hours.</summary>
    public static double Quantile(int[] hist, double fraction)
    {
        long total = 0;
        foreach (int n in hist) total += n;
        if (total == 0) return 0;
        long target = (long)(total * fraction), seen = 0;
        for (int i = 0; i < hist.Length; i++)
        {
            seen += hist[i];
            if (seen > target) return i / 10.0;
        }
        return (hist.Length - 1) / 10.0;
    }
}
