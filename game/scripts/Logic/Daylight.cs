using System;
using Return.Sim.Content;
using SimCalendar = Return.Sim.Core.Calendar;

namespace Return.Game.Logic;

/// <summary>
/// The light over the map at a moment: the ambient tint the whole map is multiplied by (blue
/// nights, a warm dawn, golden then red dusk), how dark it is (0 day, 1 deep night: the hearth's
/// light matters in proportion), and where the sun stands for shadows.
/// </summary>
/// <param name="SunHeight">0 at dawn and dusk (and all night), 1 at midday.</param>
/// <param name="SunX">+1 when the sun is in the east (dawn), -1 in the west (dusk); shadows fall the other way.</param>
public readonly record struct DaylightState(Rgb Ambient, float Night, float SunHeight, float SunX);

/// <summary>
/// Time of day as light, from the sim clock and world.json's dawnHour and duskHour (the same hours
/// the sim uses for daylight, so a vampire burns exactly while the map is lit). Dawn and dusk are
/// smooth: keyframes an hour or two either side, eased between. Rain darkens and cools the light;
/// winter cools it. Godot-free so tests can check it.
/// </summary>
public static class Daylight
{
    public static readonly Rgb NightColor = new(0.23f, 0.26f, 0.39f);
    public static readonly Rgb DawnBlue = new(0.34f, 0.34f, 0.47f);
    public static readonly Rgb DawnColor = new(0.80f, 0.60f, 0.52f);
    public static readonly Rgb DayColor = new(0.94f, 0.92f, 0.87f);
    public static readonly Rgb NoonColor = new(1.00f, 0.98f, 0.94f);
    public static readonly Rgb GoldenColor = new(0.98f, 0.82f, 0.62f);
    public static readonly Rgb DuskColor = new(0.80f, 0.50f, 0.41f);
    public static readonly Rgb DuskBlue = new(0.35f, 0.29f, 0.43f);
    public static readonly Rgb RainFilter = new(0.74f, 0.78f, 0.85f);
    public static readonly Rgb WinterFilter = new(0.93f, 0.96f, 1.03f);

    // Keyframes in hours relative to dawn and to dusk.
    private static readonly (float At, Rgb Color)[] Morning =
    {
        (-2.0f, NightColor), (-1.0f, DawnBlue), (0.0f, DawnColor), (1.5f, DayColor),
    };

    private static readonly (float At, Rgb Color)[] Evening =
    {
        (-2.0f, DayColor), (-1.0f, GoldenColor), (0.0f, DuskColor), (1.0f, DuskBlue), (2.0f, NightColor),
    };

    /// <summary>The hour of the day with its fraction, 0 ≤ h &lt; hoursPerDay.</summary>
    public static float HourOf(long tick, WorldDef def)
    {
        long inDay = ((tick % def.TicksPerDay) + def.TicksPerDay) % def.TicksPerDay;
        return inDay / (float)def.TicksPerHour;
    }

    /// <summary>The last season of the year is winter (as in the sim's season turn).</summary>
    public static bool IsWinter(long tick, WorldDef def) =>
        def.SeasonsPerYear > 0 && def.DaysPerSeason > 0 && SimCalendar.Season(tick, def) == def.SeasonsPerYear - 1;

    public static DaylightState At(long tick, WorldDef def, bool raining) => At(HourOf(tick, def), def, raining, IsWinter(tick, def));

    public static DaylightState At(float hour, WorldDef def, bool raining, bool winter)
    {
        float day = Math.Max(1, def.HoursPerDay);
        float length = PositiveMod(def.DuskHour - def.DawnHour, day);
        float sinceDawn = PositiveMod(hour - def.DawnHour, day);
        bool daytime = sinceDawn < length;

        Rgb bare = BareAmbient(hour, def, out float arc);
        float lumaNight = NightColor.Luma, lumaDay = DayColor.Luma;
        float night = 1f - Math.Clamp((bare.Luma - lumaNight) / (lumaDay - lumaNight), 0f, 1f);

        Rgb ambient = bare;
        if (raining) ambient = ambient.Times(RainFilter);
        if (winter) ambient = ambient.Times(WinterFilter);

        float sunHeight = daytime ? MathF.Sin(arc * MathF.PI) : 0f;
        float sunX = daytime ? MathF.Cos(arc * MathF.PI) : 0f;
        return new DaylightState(ambient.Clamped(), night, sunHeight, sunX);
    }

    /// <summary>The ambient colour before rain or winter, and how far through the day the sun is (0..1).</summary>
    private static Rgb BareAmbient(float hour, WorldDef def, out float arc)
    {
        float day = Math.Max(1, def.HoursPerDay);
        float length = Math.Max(0.01f, PositiveMod(def.DuskHour - def.DawnHour, day));
        float sinceDawn = PositiveMod(hour - def.DawnHour, day);
        arc = sinceDawn < length ? sinceDawn / length : 0f;

        float fromDawn = SignedMod(hour - def.DawnHour, day), fromDusk = SignedMod(hour - def.DuskHour, day);
        if (fromDawn >= Morning[0].At && fromDawn <= Morning[^1].At) return Sample(Morning, fromDawn);
        if (fromDusk >= Evening[0].At && fromDusk <= Evening[^1].At) return Sample(Evening, fromDusk);
        if (sinceDawn >= length) return NightColor;
        float noon = MathF.Sin(arc * MathF.PI);
        return Rgb.Lerp(DayColor, NoonColor, Smooth(Math.Clamp((noon - 0.55f) / 0.45f, 0f, 1f)));
    }

    private static Rgb Sample((float At, Rgb Color)[] keys, float x)
    {
        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (x > keys[i + 1].At) continue;
            float t = (x - keys[i].At) / (keys[i + 1].At - keys[i].At);
            return Rgb.Lerp(keys[i].Color, keys[i + 1].Color, Smooth(Math.Clamp(t, 0f, 1f)));
        }
        return keys[^1].Color;
    }

    /// <summary>x mod m in [0, m).</summary>
    private static float PositiveMod(float x, float m)
    {
        float r = x % m;
        return r < 0 ? r + m : r;
    }

    /// <summary>x mod m in [-m/2, m/2).</summary>
    private static float SignedMod(float x, float m)
    {
        float r = PositiveMod(x, m);
        return r >= m / 2f ? r - m : r;
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);
}
