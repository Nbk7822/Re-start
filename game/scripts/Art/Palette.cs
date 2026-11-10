using Godot;
using Return.Game.Logic;

namespace Return.Game.Art;

/// <summary>
/// The grounded palette (ADR-0009, option B), a shade darker and dirtier than the mockup: olive
/// grass, brown soil and mud, slate river, weathered timber and stone, thin warm-black ink.
/// </summary>
public static class Palette
{
    public static readonly Color Ink = new(0.075f, 0.065f, 0.05f);
    public static readonly Color InkSoft = new(0.075f, 0.065f, 0.05f, 0.55f);

    public static readonly Color Grass = Hex(0x3d4a33);
    public static readonly Color Grass2 = Hex(0x47563b);
    public static readonly Color Grass3 = Hex(0x364430);
    public static readonly Color GrassDry = Hex(0x56553b);
    public static readonly Color GrassWinter = Hex(0x4f5148);
    public static readonly Color Soil = Hex(0x5b4c3b);
    public static readonly Color Soil2 = Hex(0x4f4233);
    public static readonly Color Mud = Hex(0x3e3226);
    public static readonly Color MudWet = Hex(0x2c231a);
    public static readonly Color Ash = Hex(0x2a2826);

    public static readonly Color River = Hex(0x1f343b);
    public static readonly Color RiverShallow = Hex(0x2c454a);
    public static readonly Color Bank = Hex(0x231f19);
    public static readonly Color Foam = new(0.62f, 0.70f, 0.68f, 0.55f);

    public static readonly Color[] Canopy = { Hex(0x1f2f22), Hex(0x29402c), Hex(0x345036) };
    public static readonly Color CanopyHi = Hex(0x4a6544);
    public static readonly Color Pine = Hex(0x1c2b22);
    public static readonly Color PineHi = Hex(0x2b4233);
    public static readonly Color Trunk = Hex(0x3f3226);
    public static readonly Color TrunkLo = Hex(0x2c231b);
    public static readonly Color Branch = Hex(0x2e261e);

    public static readonly Color Bush = Hex(0x2a412e);
    public static readonly Color BushHi = Hex(0x385438);
    public static readonly Color Berry = Hex(0x7c2c30);
    public static readonly Color BerryHi = Hex(0xb0525a);

    public static readonly Color Stone = Hex(0x5c5950);
    public static readonly Color StoneHi = Hex(0x7a766a);
    public static readonly Color StoneLo = Hex(0x3e3c35);
    public static readonly Color Moss = Hex(0x3b4a2f);

    public static readonly Color WallTop = Hex(0x7a7262);
    public static readonly Color WallFace = Hex(0x4b453a);
    public static readonly Color WallGrime = Hex(0x231f19);
    public static readonly Color Floor = Hex(0x56493a);
    public static readonly Color RuinFloor = Hex(0x47493f);
    public static readonly Color Roof = Hex(0x5b4431);
    public static readonly Color RoofLine = Hex(0x33271c);
    public static readonly Color RoofHi = Hex(0x7a5f45);
    public static readonly Color Broken = new(0.63f, 0.33f, 0.27f, 0.85f);

    public static readonly Color Log = Hex(0x6b5538);
    public static readonly Color LogEnd = Hex(0x8e7553);
    public static readonly Color Basket = Hex(0x5c4a2e);
    public static readonly Color Straw = Hex(0x7d6c48);
    public static readonly Color Cloth = Hex(0x5b5040);

    public static readonly Color Trousers = Hex(0x2b2722);
    public static readonly Color Boots = Hex(0x1e1a16);
    public static readonly Color Eye = Hex(0x231b16);
    public static readonly Color Collar = Hex(0x332d3a);
    public static readonly Color VampMark = Hex(0xd2c5ad);
    public static readonly Color YouGold = Hex(0xd8b56c);
    public static readonly Color Steel = Hex(0x8d8f8c);

    /// <summary>The grey of the grave the undead fade toward (ADR-0022's look).</summary>
    public static readonly Color GraveGrey = Hex(0x8f948f);
    public static readonly Color Handle = Hex(0x4a3826);

    public static readonly Color BloodFresh = new(0.62f, 0.05f, 0.04f);
    public static readonly Color BloodDry = new(0.27f, 0.07f, 0.05f);
    public static readonly Color Stain = new(0.36f, 0.04f, 0.04f, 0.9f);
    public static readonly Color Fly = new(0.05f, 0.05f, 0.05f, 0.9f);

    public static readonly Color Flame1 = Hex(0xb8562e);
    public static readonly Color Flame2 = Hex(0xe39a4a);
    public static readonly Color Flame3 = Hex(0xf6d48a);
    public static readonly Color Ember = Hex(0xff9a45);
    public static readonly Color Smoke = new(0.55f, 0.53f, 0.50f);
    public static readonly Color FireLight = new(1f, 0.62f, 0.32f);

    /// <summary>Beyond the map's edge (seen zoomed out).</summary>
    public static readonly Color Margin = Hex(0x29292a);

    public static readonly Color Select = Hex(0xe1bd70);
    public static readonly Color Name = new(0.93f, 0.90f, 0.83f);
    public static readonly Color NameShadow = new(0.04f, 0.035f, 0.03f, 0.92f);

    public static Color Hex(uint rgb) => new(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f);

    public static Color Of(Rgb c, float alpha = 1f) => new(c.R, c.G, c.B, alpha);

    public static Color WithAlpha(this Color c, float a) => new(c.R, c.G, c.B, a);

    public static Color Mix(Color a, Color b, float t) => a.Lerp(b, Mathf.Clamp(t, 0f, 1f));
}
