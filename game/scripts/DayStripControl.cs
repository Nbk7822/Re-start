using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Art;
using Return.Game.Logic;

namespace Return.Game;

/// <summary>
/// One day as 24 cells (ADR-0029): an hour ruler above, each hour's colour, dimmed where it is the camp's rather than
/// the person's own, hatched where the sun keeps them under cover, a notch where the hour was lived otherwise than
/// planned, a mark on the watch's hours. A press and a drag pick a span of hours, reported on release; hovering names
/// the hour. Only draws and reports: what a span does is the caller's (a command through SimHost.Order).
/// </summary>
public partial class DayStripControl : Control
{
    /// <summary>One hour as drawn: its colour, whether it is dimmed, hatched, notched or marked, and its tooltip.</summary>
    public readonly record struct Cell(Color Fill, bool Dim, bool Hatched, bool Notch, bool Marked, string Tooltip);

    private const float Ruler = 11f, CellHeight = 15f, NotchRoom = 3f;
    private static readonly Color Back = new(0.10f, 0.095f, 0.085f);
    private static readonly Color Line = new(0.05f, 0.045f, 0.04f);

    private IReadOnlyList<Cell> _cells = Array.Empty<Cell>();
    private int _press = -1, _hover = -1;

    /// <summary>Whether a press and drag picks hours (the lived strip only shows).</summary>
    public bool Picks { get; set; }

    /// <summary>A queued or drafted change is shown: an accent edge says it waits for time to move.</summary>
    public bool Pending { get; set; }

    /// <summary>Raised on release with the hour pressed and the hour released.</summary>
    public event Action<int, int>? Picked;

    public DayStripControl()
    {
        CustomMinimumSize = new Vector2(0, Ruler + CellHeight + NotchRoom);
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Stop;
        TooltipText = " "; // non-empty, so Godot asks _GetTooltip for each hour
    }

    public void SetCells(IReadOnlyList<Cell> cells, bool pending)
    {
        _cells = cells;
        Pending = pending;
        QueueRedraw();
    }

    public override string _GetTooltip(Vector2 atPosition)
    {
        if (_cells.Count == 0) return "";
        return _cells[DayStrip.HourAt(atPosition.X, Size.X)].Tooltip;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (!Picks) return;
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } b when b.Pressed:
                _press = _hover = DayStrip.HourAt(b.Position.X, Size.X);
                AcceptEvent();
                QueueRedraw();
                break;
            case InputEventMouseMotion m when _press >= 0:
                _hover = DayStrip.HourAt(m.Position.X, Size.X);
                QueueRedraw();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } b when !b.Pressed && _press >= 0:
                int press = _press, release = DayStrip.HourAt(b.Position.X, Size.X);
                _press = _hover = -1;
                AcceptEvent();
                QueueRedraw();
                Picked?.Invoke(press, release);
                break;
        }
    }

    /// <summary>Picks a span as a drag from <paramref name="press"/> to <paramref name="release"/> would (RETURN_PRESS checks).</summary>
    public void Pick(int press, int release) => Picked?.Invoke(press, release);

    /// <summary>Diagonal strokes across a cell, clipped to it: the hours the sun keeps someone under cover.</summary>
    private void Hatch(Rect2 r)
    {
        float hgt = r.Size.Y, left = r.Position.X, right = r.End.X, bottom = r.End.Y;
        for (float x0 = left - hgt; x0 < right; x0 += 4f)
        {
            float t0 = Math.Max(0f, (left - x0) / hgt), t1 = Math.Min(1f, (right - x0) / hgt);
            if (t0 >= t1) continue;
            DrawLine(new Vector2(x0 + t0 * hgt, bottom - t0 * hgt), new Vector2(x0 + t1 * hgt, bottom - t1 * hgt), new Color(0f, 0f, 0f, 0.4f), 1f);
        }
    }

    public override void _Draw()
    {
        float w = Size.X, cell = w / DayStrip.Hours;
        Font font = GetThemeDefaultFont();
        for (int h = 0; h < DayStrip.Hours; h += 3)
            DrawString(font, new Vector2(h * cell + 1f, Ruler - 2f), Ui.N(h), HorizontalAlignment.Left, -1, 9, Ui.Dim);
        var strip = new Rect2(0, Ruler, w, CellHeight);
        DrawRect(strip, Back);
        for (int h = 0; h < _cells.Count && h < DayStrip.Hours; h++)
        {
            Cell c = _cells[h];
            var r = new Rect2(h * cell + 0.5f, Ruler + 0.5f, cell - 1f, CellHeight - 1f);
            DrawRect(r, c.Dim ? c.Fill.Lerp(Back, 0.5f) : c.Fill);
            if (c.Hatched) Hatch(r);
            if (c.Marked)
            {
                Vector2 m = r.GetCenter();
                DrawColoredPolygon(new[] { m + new Vector2(0, -3.5f), m + new Vector2(3f, 0), m + new Vector2(0, 3.5f), m + new Vector2(-3f, 0) }, Palette.DayWatch.Lightened(0.35f));
            }
            if (c.Notch)
                DrawColoredPolygon(new[] { new Vector2(r.Position.X + cell * 0.5f - 3f, Ruler - 1f), new Vector2(r.Position.X + cell * 0.5f + 3f, Ruler - 1f), new Vector2(r.Position.X + cell * 0.5f, Ruler + 4f) }, Ui.Accent);
        }
        for (int h = 6; h < DayStrip.Hours; h += 6) DrawLine(new Vector2(h * cell, Ruler), new Vector2(h * cell, Ruler + CellHeight), Line, 1f);
        DrawRect(strip, Pending ? Ui.Accent : Ui.Edge, false, 1f);
        if (_press >= 0)
        {
            (int first, int last) = DayStrip.Dragged(_press, _hover);
            DrawRect(new Rect2(first * cell, Ruler - 0.5f, (last - first + 1) * cell, CellHeight + 1f), Ui.Ink, false, 2f);
        }
    }
}
