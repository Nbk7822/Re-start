using System;
using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Api;

namespace Return.Game.Logic;

/// <summary>
/// The arithmetic of a day strip (ADR-0029): 24 cells, one per hour, as the Schedule list and the inspector draw them.
/// Which hour lies under the pointer, what a drag paints, the watch's start and end from a drag, and the summary line
/// under a strip. Godot-free so the tests compile it.
/// </summary>
public static class DayStrip
{
    public const int Hours = 24;

    /// <summary>The hour under a point <paramref name="x"/> pixels into a strip <paramref name="width"/> wide.</summary>
    public static int HourAt(float x, float width) => width <= 0 ? 0 : Math.Clamp((int)(x / width * Hours), 0, Hours - 1);

    /// <summary>Every hour from <paramref name="from"/> to <paramref name="to"/> (exclusive), wrapping past midnight; none when they are equal.</summary>
    public static IEnumerable<int> Range(int from, int to)
    {
        for (int h = Wrap(from); h != Wrap(to); h = (h + 1) % Hours) yield return h;
    }

    /// <summary>Every hour from <paramref name="first"/> to <paramref name="last"/>, both included, wrapping past midnight when first comes after last.</summary>
    public static IEnumerable<int> Cells(int first, int last)
    {
        int h = Wrap(first);
        for (int k = 0; k < Hours; k++)
        {
            yield return h;
            if (h == Wrap(last)) yield break;
            h = (h + 1) % Hours;
        }
    }

    /// <summary>The cells a drag across a strip paints: both ends and everything between, left to right (a strip never wraps under the pointer).</summary>
    public static (int First, int Last) Dragged(int press, int release) => (Math.Min(press, release), Math.Max(press, release));

    /// <summary>The cells from <paramref name="from"/> to <paramref name="to"/> (exclusive, wrapping), as RETURN_PRESS names them; null when they are equal.</summary>
    public static (int First, int Last)? FromTo(int from, int to) => Wrap(from) == Wrap(to) ? null : (Wrap(from), Wrap(to - 1));

    /// <summary>A camp day with cells painted with one block.</summary>
    public static List<DayBlock> Paint(IReadOnlyList<DayBlock> day, int first, int last, DayBlock brush)
    {
        var painted = new List<DayBlock>(day);
        foreach (int h in Cells(first, last)) painted[h] = brush;
        return painted;
    }

    /// <summary>Someone's own hours with cells painted: those hours become their own, with that block.</summary>
    public static DayBlock?[] PaintOwn(IReadOnlyList<DayBlock?> own, int first, int last, DayBlock brush)
    {
        var painted = new DayBlock?[Hours];
        for (int h = 0; h < Hours && h < own.Count; h++) painted[h] = own[h];
        foreach (int h in Cells(first, last)) painted[h] = brush;
        return painted;
    }

    /// <summary>The watch's hours from a drag along its row: it begins where the drag began and ends after the hour it ended on, wrapping past midnight when dragged leftward.</summary>
    public static (int From, int To) WatchHours(int press, int release) => (press, (release + 1) % Hours);

    /// <summary>Which hours a watch from <paramref name="from"/> to <paramref name="to"/> covers.</summary>
    public static bool[] WatchCells(int from, int to)
    {
        var cells = new bool[Hours];
        foreach (int h in Range(from, to)) cells[h] = true;
        return cells;
    }

    /// <summary>
    /// The line under a day strip: each block with its hours ("Sleep 22:00–06:00 · Work 06:00–19:00 · Free time
    /// 19:00–22:00"), from the run that holds midnight, ranges of one block joined.
    /// </summary>
    public static string Summary(World w, IReadOnlyList<HourState> hours)
    {
        var order = new List<string>();
        var ranges = new Dictionary<string, List<string>>(); // looked up by block id only; output follows `order`
        int n = hours.Count;
        // Begin with the run that holds midnight, so the camp's day reads "Sleep 22:00–06:00 · Work 06:00–19:00 · …".
        int start = 0;
        for (int back = 1; back < n && hours[(n - back) % n].Block == hours[0].Block; back++) start = n - back;
        for (int k = 0; k < n;)
        {
            int h = (start + k) % n;
            string block = hours[h].Block;
            int len = 1;
            while (len < n - k && hours[(h + len) % n].Block == block) len++;
            if (!ranges.ContainsKey(block)) { ranges[block] = new List<string>(); order.Add(block); }
            ranges[block].Add(Ui.F("schedule.range", ("from", Return.Sim.Api.EventText.Hour(w, h)), ("to", Return.Sim.Api.EventText.Hour(w, (h + len) % n))));
            k += len;
        }
        var parts = new List<string>(order.Count);
        foreach (string block in order)
            parts.Add(Ui.F("ui.schedule.range_block", ("block", Ui.T("block." + block)), ("ranges", string.Join(Ui.T("schedule.own.and"), ranges[block]))));
        return string.Join(Ui.T("ui.schedule.ranges_sep"), parts);
    }

    private static int Wrap(int h) => ((h % Hours) + Hours) % Hours;
}
