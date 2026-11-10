using System;
using System.Collections.Generic;

namespace Return.Game.Logic;

/// <summary>
/// Turns real time into whole simulation ticks. Speed 0 is paused; speed i (1..N) runs at
/// <c>baseTicksPerSecond × multipliers[i - 1]</c> (content world.json: 20 ticks/s × 1, 3, 6).
/// Fractions of a tick carry over between frames. A frame never runs more than
/// <see cref="MaxTicksPerFrame"/> ticks and drops the excess, so a hitch cannot snowball.
/// Real time only decides how many ticks run per frame; the sim itself stays deterministic.
/// </summary>
public sealed class TickClock
{
    public const int MaxTicksPerFrame = 400;

    private readonly int[] _rates;
    private double _carry;
    private int _lastRunning = 1;

    public TickClock(int baseTicksPerSecond, IReadOnlyList<int> multipliers)
    {
        if (baseTicksPerSecond <= 0) throw new ArgumentOutOfRangeException(nameof(baseTicksPerSecond), "Ticks per second must be positive.");
        if (multipliers.Count == 0) throw new ArgumentException("At least one speed multiplier is needed.", nameof(multipliers));
        _rates = new int[multipliers.Count + 1];
        var copy = new int[multipliers.Count];
        for (int i = 0; i < multipliers.Count; i++)
        {
            if (multipliers[i] <= 0) throw new ArgumentOutOfRangeException(nameof(multipliers), "Speed multipliers must be positive.");
            copy[i] = multipliers[i];
            _rates[i + 1] = baseTicksPerSecond * multipliers[i];
        }
        Multipliers = copy;
        Speed = 1;
    }

    /// <summary>0 = paused, 1..N = the content speed multipliers in order.</summary>
    public int Speed { get; private set; }

    /// <summary>Number of speeds including pause.</summary>
    public int SpeedCount => _rates.Length;

    public bool Paused => Speed == 0;

    public int TicksPerSecond => _rates[Speed];

    public IReadOnlyList<int> Multipliers { get; }

    /// <summary>The multiplier shown for a speed (0 for pause).</summary>
    public int MultiplierOf(int speed) => speed <= 0 || speed >= _rates.Length ? 0 : Multipliers[speed - 1];

    /// <summary>Selects a speed; out-of-range values are ignored.</summary>
    public void SetSpeed(int speed)
    {
        if (speed < 0 || speed >= _rates.Length) return;
        Speed = speed;
        if (speed > 0) _lastRunning = speed;
    }

    /// <summary>Pause, or resume at the last running speed.</summary>
    public void TogglePause() => SetSpeed(Paused ? _lastRunning : 0);

    /// <summary>How many ticks to run for a frame that took <paramref name="seconds"/> of real time.</summary>
    public int Advance(double seconds)
    {
        if (Paused || !double.IsFinite(seconds) || seconds <= 0) return 0;
        double total = _carry + seconds * _rates[Speed];
        if (total >= MaxTicksPerFrame)
        {
            _carry = 0;
            return MaxTicksPerFrame;
        }
        int ticks = (int)total;
        _carry = total - ticks;
        return ticks;
    }
}
