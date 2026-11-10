using System.IO;
using Godot;

namespace Return.Game.Art;

/// <summary>
/// The textures and decals rendered from tools/art/src by tools/art/build (game/art/*.png). They are
/// read straight from disk (Image.LoadFromFile on the globalised res:// path), so no Godot import
/// step is needed and the game runs without the editor. A missing file leaves its slot null and
/// the art draws without it.
/// </summary>
public sealed class ArtTextures
{
    public Texture2D? GrungeDark { get; private set; }
    public Texture2D? GrungeLight { get; private set; }
    public Texture2D? Grain { get; private set; }
    public Texture2D? Dirt { get; private set; }
    public Texture2D? Frost { get; private set; }
    public Texture2D? Water { get; private set; }
    public Texture2D? Planks { get; private set; }
    public Texture2D? Stone { get; private set; }
    public Texture2D? Shadow { get; private set; }
    public Texture2D? Light { get; private set; }
    public Texture2D? Vignette { get; private set; }
    public Texture2D? Smoke { get; private set; }
    public Texture2D? Pool { get; private set; }
    public Texture2D? Scorch { get; private set; }
    public Texture2D?[] Blobs { get; } = new Texture2D?[3];
    public Texture2D?[] Stains { get; } = new Texture2D?[2];
    public Texture2D?[] Splats { get; } = new Texture2D?[4];
    public Texture2D?[] Drops { get; } = new Texture2D?[2];

    /// <summary>How many textures could not be read (0 when the art is all there).</summary>
    public int Missing { get; private set; }

    public static ArtTextures Load()
    {
        var t = new ArtTextures();
        t.GrungeDark = t.Read("grunge_dark");
        t.GrungeLight = t.Read("grunge_light");
        t.Grain = t.Read("grain");
        t.Dirt = t.Read("dirt");
        t.Frost = t.Read("frost");
        t.Water = t.Read("water");
        t.Planks = t.Read("planks");
        t.Stone = t.Read("stone");
        t.Shadow = t.Read("shadow");
        t.Light = t.Read("light");
        t.Vignette = t.Read("vignette");
        t.Smoke = t.Read("smoke");
        t.Pool = t.Read("blood_pool");
        t.Scorch = t.Read("scorch");
        for (int i = 0; i < t.Blobs.Length; i++) t.Blobs[i] = t.Read("blob_" + i);
        for (int i = 0; i < t.Stains.Length; i++) t.Stains[i] = t.Read("stain_" + i);
        for (int i = 0; i < t.Splats.Length; i++) t.Splats[i] = t.Read("blood_splat_" + i);
        for (int i = 0; i < t.Drops.Length; i++) t.Drops[i] = t.Read("blood_drops_" + i);
        if (t.Missing > 0) GD.PushWarning("Return: " + t.Missing + " art texture(s) missing from res://art; drawing without them.");
        return t;
    }

    /// <summary>Path of the view's art folder on disk.</summary>
    public static string ArtDir => ProjectSettings.GlobalizePath("res://art");

    private Texture2D? Read(string name)
    {
        string path = Path.Combine(ArtDir, name + ".png");
        if (!File.Exists(path))
        {
            Missing++;
            return null;
        }
        Image? image = Image.LoadFromFile(path);
        if (image == null || image.IsEmpty())
        {
            Missing++;
            return null;
        }
        image.GenerateMipmaps();
        return ImageTexture.CreateFromImage(image);
    }
}
