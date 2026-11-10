using System;
using System.Globalization;

namespace Return.Game.Logic;

/// <summary>
/// A plain colour (components 0..1) for the Godot-free half of the view: the time-of-day tint and
/// the colours in game/art/looks.json. The Art layer converts it to a Godot colour.
/// </summary>
public readonly record struct Rgb(float R, float G, float B)
{
    public static readonly Rgb White = new(1f, 1f, 1f);

    public static Rgb Lerp(Rgb a, Rgb b, float t) =>
        new(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t);

    public Rgb Times(Rgb o) => new(R * o.R, G * o.G, B * o.B);

    public Rgb Scaled(float k) => new(R * k, G * k, B * k);

    public Rgb Clamped() => new(Math.Clamp(R, 0f, 1f), Math.Clamp(G, 0f, 1f), Math.Clamp(B, 0f, 1f));

    /// <summary>Relative luminance (Rec. 709 weights), 0..1.</summary>
    public float Luma => 0.2126f * R + 0.7152f * G + 0.0722f * B;

    /// <summary>
    /// The colour worn, dirtied and dimmed for the grounded look: darkened by <paramref name="darken"/>
    /// and pulled toward its own grey by <paramref name="desaturate"/>, then nudged toward a dirt brown.
    /// Cloth uses it on cast.json's colour (or a looks.json override).
    /// </summary>
    public Rgb Muted(float darken = 0.7f, float desaturate = 0.3f)
    {
        float grey = Luma;
        var flat = new Rgb(R + (grey - R) * desaturate, G + (grey - G) * desaturate, B + (grey - B) * desaturate);
        var dirt = new Rgb(0.30f, 0.25f, 0.19f);
        return Lerp(flat.Scaled(darken), dirt, 0.12f).Clamped();
    }

    /// <summary>Parses "#rrggbb" (or "rrggbb").</summary>
    public static bool TryParse(string? hex, out Rgb rgb)
    {
        rgb = default;
        if (string.IsNullOrEmpty(hex)) return false;
        string s = hex[0] == '#' ? hex.Substring(1) : hex;
        if (s.Length != 6 || !int.TryParse(s, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int v)) return false;
        rgb = new Rgb(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
        return true;
    }

    public static Rgb Parse(string hex) =>
        TryParse(hex, out Rgb rgb) ? rgb : throw new FormatException("Not a #rrggbb colour: '" + hex + "'.");

    public string ToHex()
    {
        Rgb c = Clamped();
        int r = (int)MathF.Round(c.R * 255f), g = (int)MathF.Round(c.G * 255f), b = (int)MathF.Round(c.B * 255f);
        return "#" + ((r << 16) | (g << 8) | b).ToString("x6", CultureInfo.InvariantCulture);
    }
}
