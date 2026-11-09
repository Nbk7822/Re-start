using System;
using Godot;
using Return.Game.Logic;

namespace Return.Game.Art;

/// <summary>
/// Day and night with Godot's 2D lighting: a CanvasModulate tints the whole map by the time of day
/// (Logic.Daylight: blue nights, warm dawn, golden then red dusk; darker and cooler in rain and
/// winter), and a warm, flickering PointLight2D at the hearth lights everything near it with a
/// soft falloff (the Inkscape light texture). It matters in proportion to the dark, so by day it is
/// a faint warmth and at night it is the only place colour survives. The watch post's lantern
/// gives a small second light while someone keeps watch at night.
/// </summary>
public sealed class Lighting
{
    private const float FireRadiusTiles = 9.5f;
    private const float LanternRadiusTiles = 3.2f;

    private readonly CanvasModulate _tint;
    private readonly PointLight2D _fire;
    private readonly PointLight2D _lantern;
    private readonly float _fireScale;
    private readonly float _lanternScale;

    public Lighting(Node parent, ArtTextures tex, Vector2 hearth, Vector2 lantern)
    {
        Texture2D falloff = tex.Light ?? Gradient();
        float half = falloff.GetWidth() / 2f;
        _fireScale = FireRadiusTiles * MapView.TileSize / half;
        _lanternScale = LanternRadiusTiles * MapView.TileSize / half;

        _tint = new CanvasModulate { Name = "DayNight", Color = Colors.White };
        _fire = new PointLight2D
        {
            Name = "Firelight", Texture = falloff, TextureScale = _fireScale, Color = Palette.FireLight,
            Energy = 0f, Position = hearth + new Vector2(0f, -4f), BlendMode = Light2D.BlendModeEnum.Add, ShadowEnabled = false,
        };
        _lantern = new PointLight2D
        {
            Name = "Lantern", Texture = falloff, TextureScale = _lanternScale, Color = new Color(1f, 0.74f, 0.42f),
            Energy = 0f, Position = lantern, BlendMode = Light2D.BlendModeEnum.Add, ShadowEnabled = false,
        };
        parent.AddChild(_tint);
        parent.AddChild(_fire);
        parent.AddChild(_lantern);
    }

    public void Update(in DaylightState light, float time, bool lanternLit)
    {
        _tint.Color = Palette.Of(light.Ambient);
        float flicker = 1f + MathF.Sin(time * 9.1f) * 0.05f + MathF.Sin(time * 15.3f + 0.7f) * 0.035f + MathF.Sin(time * 23.7f + 1.9f) * 0.02f;
        _fire.Energy = (0.3f + 1.6f * light.Night) * flicker;
        _fire.TextureScale = _fireScale * (1f + MathF.Sin(time * 6.3f) * 0.025f);
        float lantern = lanternLit ? 0.9f * light.Night : 0f;
        _lantern.Energy = lantern * (1f + MathF.Sin(time * 11.7f + 2f) * 0.04f);
        _lantern.Visible = lantern > 0.01f;
    }

    private static Texture2D Gradient()
    {
        var g = new Gradient();
        g.SetColor(0, Colors.White);
        g.SetColor(1, new Color(1f, 1f, 1f, 0f));
        return new GradientTexture2D
        {
            Gradient = g, Width = 256, Height = 256, Fill = GradientTexture2D.FillEnum.Radial,
            FillFrom = new Vector2(0.5f, 0.5f), FillTo = new Vector2(0.5f, 0f),
        };
    }
}
