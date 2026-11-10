using System;
using System.Collections.Generic;
using System.Globalization;
using SimPoint = Return.Sim.Core.Point;

namespace Return.Game.Logic;

/// <summary>
/// Reads the RETURN_PRESS check hook (SimHost): a comma-separated list of presses such as
/// "policy:1,build:wall:19,16,6,1,wait:600". A press begins with a word; the numbers that follow it
/// belong to it, so a press may carry coordinates ("build:wall:x,y,w,h"). Godot-free so the tests
/// compile it.
/// </summary>
public static class CheckPresses
{
    /// <summary>The presses in a list, in order, each as written ("build:wall:19,16,6,1").</summary>
    public static List<string> Split(string list)
    {
        var presses = new List<string>();
        foreach (string raw in list.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (char.IsLetter(raw[0]) || presses.Count == 0) presses.Add(raw);
            else presses[^1] += "," + raw;
        }
        return presses;
    }

    /// <summary>
    /// A gesture from "x,y" (a click on that tile) or "x,y,w,h" (a drag from (x, y) across w by h tiles, to
    /// (x + w - 1, y + h - 1)). False unless it is two or four whole numbers with w and h at least 1.
    /// </summary>
    public static bool TryGesture(string coords, out SimPoint from, out SimPoint to)
    {
        from = to = SimPoint.None;
        string[] parts = coords.Split(',');
        if (parts.Length is not (2 or 4)) return false;
        var n = new int[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out n[i])) return false;
        int w = parts.Length == 4 ? n[2] : 1, h = parts.Length == 4 ? n[3] : 1;
        if (w < 1 || h < 1) return false;
        from = new SimPoint(n[0], n[1]);
        to = new SimPoint(n[0] + w - 1, n[1] + h - 1);
        return true;
    }
}
