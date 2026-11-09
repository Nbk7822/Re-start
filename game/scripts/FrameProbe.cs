using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;

namespace Return.Game;

/// <summary>
/// A frame-time check hook, like SimHost's: RETURN_FPS=n runs at speed n (1 = ×1 … 3 = ×6), lets
/// the view settle for two seconds, measures ten seconds of frames, prints frames per second, the
/// slowest frames, the renderer's CPU and GPU time and the map's own drawing time, then quits.
/// </summary>
public partial class FrameProbe : Node
{
    private const double Settle = 2.0;
    private const double Measure = 10.0;

    private SimHost _host = null!;
    private MapView _map = null!;
    private double _elapsed;
    private readonly List<double> _frames = new(1200);
    private double _renderCpu;
    private double _renderGpu;
    private double _paint;
    private int _samples;

    public static void AttachIfAsked(SimHost host, MapView map)
    {
        string? value = System.Environment.GetEnvironmentVariable("RETURN_FPS");
        if (string.IsNullOrWhiteSpace(value)) return;
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int speed)) speed = 1;
        var probe = new FrameProbe { Name = "FrameProbe", _host = host, _map = map };
        host.SetSpeed(Math.Clamp(speed, 1, host.Clock.SpeedCount - 1));
        // Measure real work, not the display's refresh: frames would otherwise snap to vsync steps.
        DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
        RenderingServer.ViewportSetMeasureRenderTime(map.GetViewport().GetViewportRid(), true);
        map.AddChild(probe);
    }

    public override void _Process(double delta)
    {
        _elapsed += delta;
        double paint = _map.TakePaintMilliseconds();
        if (_elapsed < Settle) return;
        Rid vp = _map.GetViewport().GetViewportRid();
        _frames.Add(delta * 1000.0);
        _renderCpu += RenderingServer.ViewportGetMeasuredRenderTimeCpu(vp);
        _renderGpu += RenderingServer.ViewportGetMeasuredRenderTimeGpu(vp);
        _paint += paint;
        _samples++;
        if (_elapsed < Settle + Measure) return;

        _frames.Sort();
        double total = 0;
        foreach (double f in _frames) total += f;
        double avg = total / _frames.Count;
        double p99 = _frames[(int)(_frames.Count * 0.99)];
        string line = string.Format(CultureInfo.InvariantCulture,
            "Return: fps {0:F1} over {1} frames (avg {2:F2} ms, 99th pct {3:F2} ms, worst {4:F2} ms); render cpu {5:F2} ms, gpu {6:F2} ms; map drawing {7:F2} ms; window {8}; speed x{9}; {10} people on the map",
            1000.0 / avg, _frames.Count, avg, p99, _frames[^1], _renderCpu / _samples, _renderGpu / _samples, _paint / _samples,
            GetViewport().GetVisibleRect().Size, _host.Clock.MultiplierOf(_host.Clock.Speed), _map.PeopleShown);
        GD.Print(line);
        GetTree().Quit();
    }
}
