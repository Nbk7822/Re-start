using System;
using System.Collections.Generic;
using Godot;
using Return.Game.Art;
using Return.Game.Logic;
using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Systems;
using SimPoint = Return.Sim.Core.Point;
using Tree = Return.Sim.Tree;

namespace Return.Game;

/// <summary>
/// The map, drawn in the grounded three-quarter style (ADR-0009, darker and grimier per its
/// 2026-11-09 update). Orchestrates four passes: the ground (terrain, mud, grime; baked once and
/// again when the season turns), the water (the grid and the flowing river), the world (wet
/// ground, blood, grass, then everything that stands up sorted by y so things in front hide things
/// behind: trees, bushes, goods, walls, roofs, the hearth and the people, then smoke and flies), and
/// an overlay above the lighting (rain, a vignette, names and mood marks). Day and night come from
/// Art.Lighting. Read-only over World: the only state here is view smoothing, walk phases, view-only
/// nudges and the gore log, all derived from the sim. The Build menu's part (beds, plans under way,
/// plan marks, the tool's input) is in MapView.Build.cs.
/// </summary>
public partial class MapView : Node2D
{
    public const int TileSize = 24;

    /// <summary>Figure units to map pixels: a standing person is about 1.3 tiles tall (ADR-0009).</summary>
    private const float FigureScale = TileSize * 1.32f / Figures.Height;

    /// <summary>Feet stand a little below the tile centre, so a figure rises into the tile behind.</summary>
    private const float FeetOffset = TileSize * 0.3f;

    /// <summary>Someone lying on the ground (not in a bed) lies across the tile's centre, clear of a wall top in front.</summary>
    private const float LyingOffset = TileSize * 0.05f;

    /// <summary>People sharing a tile stand this far apart, side by side (view only).</summary>
    private const float SpreadStep = TileSize * 0.3f;

    /// <summary>Someone sitting by the fire sits back this far from the hearth's stones (view only).</summary>
    private const float HearthPush = TileSize * 0.22f;

    /// <summary>Walk cycles per tile walked.</summary>
    private const float StridesPerTile = 0.85f;

    private const int NameFontSize = 12;
    private const int MarkerFontSize = 16;
    private const int CountFontSize = 10;

    private static readonly string[] Numbers = BuildNumbers(100);

    private SimHost? _host;
    private Font _font = null!;
    private ArtTextures _tex = null!;
    private LookBook _looks = null!;
    private WorldArt _art = null!;
    private Lighting _lighting = null!;
    private DrawLayer _ground = null!;
    private DrawLayer _water = null!;
    private DrawLayer _world = null!;
    private DrawLayer _overlay = null!;
    private GoreLog _gore = new();
    private readonly Brush _brush = new();
    private readonly Dictionary<int, Shown> _shown = new();
    private readonly List<Drawable> _drawables = new();
    private readonly List<(int Id, Rect2 Box)> _hits = new();
    private readonly List<NameTag> _labels = new();
    private readonly List<TextMark> _marks = new();
    private readonly List<Rect2> _placed = new();
    private readonly List<(int Id, Rect2 Box)> _bodies = new();
    private readonly List<(int Id, Rect2 Box)> _bodiesMap = new();
    private string _sleepMark = "";
    private readonly Dictionary<string, Color> _castColors = new(StringComparer.Ordinal);
    private readonly Dictionary<int, float> _roofAlpha = new();
    private float _ruinAlpha = 1f;
    private bool _winterDrawn;
    private float _time;
    private DaylightState _light;
    private Transform2D _toScreen = Transform2D.Identity;
    private Vector2[] _rain = Array.Empty<Vector2>();
    private readonly System.Diagnostics.Stopwatch _paintWatch = new();
    private double _paintMs;
    private float _groundZoom = 1f;
    private ImageTexture? _groundTex;
    private SubViewport? _bakeView;
    private int _bakeFrames;

    /// <summary>How many people (and bodies) are drawn this frame.</summary>
    public int PeopleShown => _shown.Count;

    /// <summary>Milliseconds spent in the map's own update and drawing since the last call (FrameProbe).</summary>
    public double TakePaintMilliseconds()
    {
        double ms = _paintMs;
        _paintMs = 0;
        return ms;
    }

    /// <summary>Per-person view state: where they are drawn, how far through a stride, what they look like.</summary>
    private sealed class Shown
    {
        /// <summary>Where the sim has them, eased (a tile centre, or between tiles while stepping).</summary>
        public Vector2 At;

        /// <summary>A view-only nudge off that point, eased: side by side on a shared tile, back from the fire.</summary>
        public Vector2 Spread;
        public float Phase;
        public bool Moving;
        public bool HasLook;
        public CharStatus LookStatus;

        /// <summary>How they look by day (cached until their status changes).</summary>
        public FigureLook BaseLook;

        /// <summary>How they are drawn this frame (moonlit at night).</summary>
        public FigureLook Look;
        public FigureState State;

        /// <summary>Where the figure's feet are drawn this frame (everything attached to a person reads this).</summary>
        public Vector2 Feet;

        /// <summary>How far above the feet the top of the head is this frame (mood marks sit there).</summary>
        public float HeadAbove;
    }

    private enum Kind : byte
    {
        Tree,
        Stump,
        Bush,
        Stack,
        Wall,
        Door,
        Roof,
        RuinRoof,
        Hearth,
        WatchPost,
        Figure,
        Bed,
        Plan,
        PlanLogs,
    }

    private readonly record struct Drawable(float Y, Kind Kind, int Index, float Order);

    private readonly record struct NameTag(int Id, Vector2 Feet, string Name, bool Dead, bool Hostile);

    private readonly record struct TextMark(Vector2 At, string Text, int Size, Color Color, Vector2 Align, int Outline);

    private sealed class ByDepth : IComparer<Drawable>
    {
        public static readonly ByDepth Instance = new();

        public int Compare(Drawable a, Drawable b)
        {
            int c = a.Y.CompareTo(b.Y);
            if (c != 0) return c;
            c = a.Order.CompareTo(b.Order);
            return c != 0 ? c : a.Index.CompareTo(b.Index);
        }
    }

    public void Bind(SimHost host)
    {
        _host = host;
        _font = ThemeDB.FallbackFont;
        _sleepMark = Ui.T("ui.emote.zzz");
        _tex = ArtTextures.Load();
        _looks = LoadLooks();
        World w = host.World;
        _art = new WorldArt(w.Map, _tex);
        _artRevision = w.Map.Revision;

        _ground = DrawLayer.Make("Ground", PaintGround);
        _ground.TextureRepeat = TextureRepeatEnum.Enabled;
        _ground.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        // The grid and the river surface, between the ground and the world. The water texture does not
        // tile seamlessly, so it repeats mirrored: no seams, however finely it is sampled.
        _water = DrawLayer.Make("Water", PaintWater);
        _water.TextureRepeat = TextureRepeatEnum.Mirror;
        _water.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        _world = DrawLayer.Make("World", PaintWorld);
        _world.TextureRepeat = TextureRepeatEnum.Enabled;
        _world.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        AddChild(_ground);
        AddChild(_water);
        AddChild(_world);
        _lighting = new Lighting(this, _tex, _art.HearthCenter, _art.LanternAt);

        // The overlay sits above the lit, tinted map and below the menu bar: unshaded, untinted.
        var layer = new CanvasLayer { Name = "MapOverlay", Layer = 0 };
        _overlay = DrawLayer.Make("Overlay", PaintOverlay);
        _overlay.Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        _overlay.TextureFilter = TextureFilterEnum.Linear;
        layer.AddChild(_overlay);
        AddChild(layer);

        _rain = new Vector2[260 * 2];
        _gore.Observe(w);
        _light = Daylight.At(w.Tick, w.Content.World, w.Weather.Raining);
        FrameProbe.AttachIfAsked(host, this);
    }

    /// <summary>Lets the gore log see every tick the view runs (ViewStepper), drawn or not, so blood lands where it fell.</summary>
    public void Witness(World w) => _gore.Observe(w);

    /// <summary>
    /// After a return (ADR-0030): the map shows the world gone back to. Its blood and bodies, its trees and stumps, and
    /// where everyone stands are that world's; the season's ground re-bakes on its own if the season differs.
    /// </summary>
    public void ResetFor(World w)
    {
        _gore = new GoreLog();
        _gore.Observe(w);
        _shown.Clear();
        if (_host != null) _art = new WorldArt(w.Map, _tex); // before Bind (a warm-up), Bind builds it from the world shown
    }

    /// <summary>Centre of a tile in map pixels.</summary>
    public static Vector2 TileCenter(SimPoint p) => new((p.X + 0.5f) * TileSize, (p.Y + 0.5f) * TileSize);

    public Vector2I TileUnderMouse()
    {
        Vector2 m = GetLocalMousePosition();
        return new Vector2I(Mathf.FloorToInt(m.X / TileSize), Mathf.FloorToInt(m.Y / TileSize));
    }

    public bool InBounds(Vector2I tile) =>
        _host != null && tile.X >= 0 && tile.Y >= 0 && tile.X < _host.World.Map.Width && tile.Y < _host.World.Map.Height;

    /// <summary>The person drawn under the mouse (topmost first), or 0.</summary>
    public int CharacterUnderMouse()
    {
        Vector2 m = GetLocalMousePosition();
        for (int i = _hits.Count - 1; i >= 0; i--)
            if (_hits[i].Box.HasPoint(m)) return _hits[i].Id;
        return 0;
    }

    public override void _Process(double delta)
    {
        if (_host == null) return;
        _paintWatch.Restart();
        World w = _host.World;
        WorldDef def = w.Content.World;
        _time += (float)delta;
        UpdateBuild(w);
        Smooth(w, delta);
        _gore.Observe(w);
        _light = Daylight.At(w.Tick, def, w.Weather.Raining);
        _lighting.Update(_light, _time, LanternLit(w));
        UpdateGlows(w);
        UpdateRoofs(w, (float)delta);

        bool winter = Daylight.IsWinter(w.Tick, def);
        float zoom = GetGlobalTransformWithCanvas().X.Length();
        // The ground is baked once, and again when the season turns. Drawn directly (before the bake
        // is ready), it is redrawn when the zoom changes enough that its outlines' feather would show.
        if (winter != _winterDrawn || _bakeView == null && _groundTex == null && _bakeFrames == 0)
        {
            _winterDrawn = winter;
            StartGroundBake();
            _ground.QueueRedraw();
        }
        else if (_groundTex == null && MathF.Abs(zoom / _groundZoom - 1f) > 0.25f)
        {
            _groundZoom = zoom;
            _ground.QueueRedraw();
        }
        FinishGroundBake();
        _water.QueueRedraw();
        _world.QueueRedraw();
        _overlay.QueueRedraw();
        _paintMs += _paintWatch.Elapsed.TotalMilliseconds;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_host == null) return;
        if (BuildInput(e))
        {
            GetViewport().SetInputAsHandled();
            return;
        }
        if (_host.Build.Active || e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
        _host.Select(CharacterUnderMouse());
        GetViewport().SetInputAsHandled();
    }

    // ================= view state =================

    /// <summary>
    /// Eases each person's drawn position toward where the sim has them: between their tile and the
    /// next one on their path, by how far through the step they are. Faster at higher speeds,
    /// snapping on big jumps. The walk cycle advances with the distance actually covered, so feet
    /// never slide.
    /// </summary>
    private void Smooth(World w, double delta)
    {
        float speed = Mathf.Max(1, _host!.Clock.MultiplierOf(_host.Clock.Speed));
        float alpha = 1f - Mathf.Exp(-(float)delta * 18f * speed);
        // Stepping aside is eased at its own pace, so someone walking through a busy tile causes a small shuffle, not a jump.
        float settle = 1f - Mathf.Exp(-(float)delta * 9f);
        foreach (Character c in w.Characters)
        {
            bool drawn = (c.IsActive || c.Status == CharStatus.Dead) && w.Map.InBounds(c.Pos);
            if (!drawn)
            {
                _shown.Remove(c.Id);
                continue;
            }
            Vector2 target = TileCenter(c.Pos);
            bool stepping = c.Path.Count > 0 && !c.Downed && !c.Asleep && c.IsActive;
            if (stepping && c.MoveTicks > 0)
            {
                SimPoint next = c.Path[0];
                int ticks = TravelSystem.TicksFor(w, c, c.Pos, next);
                target = target.Lerp(TileCenter(next), Mathf.Clamp(c.MoveTicks / (float)Math.Max(1, ticks), 0f, 1f));
            }
            Vector2 nudge = Settle(w, c, stepping);
            if (!_shown.TryGetValue(c.Id, out Shown? s))
            {
                s = new Shown { At = target, Spread = nudge };
                _shown[c.Id] = s;
            }
            Vector2 was = s.At;
            s.At = was.DistanceTo(target) > TileSize * 3 ? target : was.Lerp(target, alpha);
            s.Spread = s.Spread.Lerp(nudge, settle);
            float moved = s.At.DistanceTo(was);
            s.Moving = c.IsActive && !c.Downed && !c.Asleep && (stepping || s.At.DistanceTo(target) > 0.6f);
            if (s.Moving) s.Phase = (s.Phase + moved / TileSize * StridesPerTile) % 1f;
        }
    }

    /// <summary>
    /// Where someone stands off their point, view only: people on the same tile stand side by side
    /// (ordered by id, so the order never flickers), and someone sitting by the hearth sits back
    /// from the stones. The dead and the downed stay on the blood they lie in; a carried patient
    /// goes with the bearer.
    /// </summary>
    private static Vector2 Settle(World w, Character c, bool stepping)
    {
        var nudge = Vector2.Zero;
        if (Spreads(c))
        {
            int n = 0, k = 0;
            foreach (Character o in w.Characters)
            {
                if (o.Pos != c.Pos || !Spreads(o)) continue;
                n++;
                if (o.Id < c.Id) k++;
            }
            if (n > 1) nudge.X = (k - (n - 1) / 2f) * SpreadStep;
        }
        SimPoint hearth = w.Map.Hearth;
        if (!stepping && c.Pos != hearth && c.Pos.ChebyshevTo(hearth) <= Poses.HearthSitRange && Poses.For(w, c, false) == Pose.Sit)
            nudge += (TileCenter(c.Pos) - TileCenter(hearth)).Normalized() * HearthPush;
        return nudge;
    }

    private static bool Spreads(Character c) => c.IsActive && !c.Downed;

    private static readonly Color GroveLight = new(0.62f, 0.95f, 0.62f);
    private static readonly Color DjinnWarmth = new(1f, 0.58f, 0.22f);
    private static readonly Color DjinnHeart = new(1f, 0.78f, 0.42f);
    private static readonly Color EmberLight = new(1f, 0.52f, 0.22f);
    private readonly List<Lighting.Glow> _glowList = new();

    /// <summary>
    /// The light some natures give at night: a dryad glows faintly green beside the tree she is bound
    /// to (Kinship.GroveGlow), a djinn smoulders, brighter when restless or unbound.
    /// </summary>
    private void UpdateGlows(World w)
    {
        _glowList.Clear();
        if (_light.Night > 0.05f)
            foreach (Character c in w.Characters)
            {
                if (c.Status != CharStatus.Member || !_shown.TryGetValue(c.Id, out Shown? s) || !s.HasLook) continue;
                Vector2 feet = s.At + s.Spread + new Vector2(0f, FeetOffset);
                if (s.BaseLook.Has(Kin.Dryad))
                {
                    float glow = Kinship.GroveGlow(w, c, _light.Night);
                    if (glow > 0.01f)
                        _glowList.Add(new Lighting.Glow(feet - new Vector2(0f, TileSize * 0.55f), GroveLight, glow * (0.5f + 0.08f * MathF.Sin(_time * 1.1f + c.Id)), 2.3f));
                }
                if (s.BaseLook.Has(Kin.Djinn))
                {
                    float flicker = 1f + 0.12f * MathF.Sin(_time * 7.7f + c.Id) + 0.06f * MathF.Sin(_time * 13.1f + c.Id * 2f);
                    float burn = 0.5f + 0.2f * Kinship.NatureStage(c);
                    _glowList.Add(new Lighting.Glow(feet - new Vector2(0f, TileSize * 0.8f), EmberLight, _light.Night * burn * flicker, 1.8f));
                }
            }
        _lighting.Glows(_glowList);
    }

    /// <summary>Roofs fade while someone is under them, so they can be seen: each section (and each roof going up) by its id.</summary>
    private void UpdateRoofs(World w, float delta)
    {
        Map m = w.Map;
        float k = 1f - Mathf.Exp(-delta * 6f);
        foreach (RoofSection r in m.Roof)
            if (!r.Removed) _roofAlpha[r.Id] = Mathf.Lerp(RoofAlpha(r.Id), Occupied(w, r.Area) ? 0.3f : 0.94f, k);
        foreach (Blueprint bp in w.Blueprints)
            if (bp.Progress > 0 && bp.Area.W * bp.Area.H > 1) _roofAlpha[bp.Id] = Mathf.Lerp(RoofAlpha(bp.Id), Occupied(w, bp.Area) ? 0.3f : 0.94f, k);
        _ruinAlpha = Mathf.Lerp(_ruinAlpha, Occupied(w, m.CoveredRuins) ? 0.32f : 0.95f, k);
    }

    private float RoofAlpha(int id) => _roofAlpha.TryGetValue(id, out float a) ? a : 1f;

    private static bool Occupied(World w, Return.Sim.Core.Rect area)
    {
        foreach (Character c in w.Characters)
            if ((c.IsActive || c.Status == CharStatus.Dead) && area.Contains(c.Pos)) return true;
        return false;
    }

    private static bool LanternLit(World w)
    {
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member && c.Job is { Kind: JobKind.KeepWatch, Stage: >= 1 }) return true;
        return false;
    }

    // ================= ground =================

    /// <summary>
    /// The ground is static, so it is drawn once into an offscreen texture (three texels per map
    /// pixel, mipmapped) and then shown as a single quad: its grime overlays and soft blobs would
    /// otherwise cost several full-screen passes every frame. Until the bake is ready (or if it
    /// fails, as in a headless run) the ground is drawn directly.
    /// </summary>
    private void PaintGround(DrawLayer layer)
    {
        if (_host == null) return;
        Map m = _host.World.Map;
        // The margins around the map, zoomed out, take the same day and night as the map (the window's
        // clear colour is not tinted). Four bands around it, so nothing is drawn twice under the map.
        float w = m.Width * TileSize, h = m.Height * TileSize, g = 4000f;
        layer.DrawRect(new Rect2(-g, -g, w + 2f * g, g), Palette.Margin);
        layer.DrawRect(new Rect2(-g, h, w + 2f * g, g), Palette.Margin);
        layer.DrawRect(new Rect2(-g, 0f, g, h), Palette.Margin);
        layer.DrawRect(new Rect2(w, 0f, g, h), Palette.Margin);
        if (_groundTex != null)
        {
            layer.DrawTextureRect(_groundTex, new Rect2(0f, 0f, w, h), false);
            return;
        }
        _groundZoom = layer.GetGlobalTransformWithCanvas().X.Length();
        _brush.Begin(layer, PixelsPerUnit(layer));
        _art.DrawGround(_brush, _winterDrawn);
        _brush.End();
    }

    private float BakeScale
    {
        get
        {
            Map m = _host!.World.Map;
            float largest = Mathf.Max(m.Width, m.Height) * TileSize;
            return Mathf.Clamp(Mathf.Floor(8192f / largest), 1f, 3f);
        }
    }

    private void StartGroundBake()
    {
        _bakeView?.QueueFree();
        Map m = _host!.World.Map;
        float s = BakeScale;
        bool winter = _winterDrawn;
        var view = new SubViewport
        {
            Name = "GroundBake",
            Size = new Vector2I((int)(m.Width * TileSize * s), (int)(m.Height * TileSize * s)),
            Disable3D = true,
            TransparentBg = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Once,
        };
        DrawLayer painter = DrawLayer.Make("Painter", layer =>
        {
            _brush.Begin(layer, s);
            _art.DrawGround(_brush, winter);
            _brush.End();
        });
        painter.Scale = new Vector2(s, s);
        painter.TextureRepeat = TextureRepeatEnum.Enabled;
        painter.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        view.AddChild(painter);
        AddChild(view);
        _bakeView = view;
        _bakeFrames = 0;
    }

    private void FinishGroundBake()
    {
        if (_bakeView == null || ++_bakeFrames < 3) return;
        Image? image = _bakeView.GetTexture()?.GetImage();
        if (image != null && !image.IsEmpty())
        {
            image.GenerateMipmaps();
            _groundTex = ImageTexture.CreateFromImage(image);
        }
        _bakeView.QueueFree();
        _bakeView = null;
        _ground.QueueRedraw();
    }

    /// <summary>Physical screen pixels per unit of a layer's space (the window may be HiDPI and the camera zoomed).</summary>
    private float PixelsPerUnit(CanvasItem layer)
    {
        Vector2 logical = GetViewport().GetVisibleRect().Size;
        float stretch = logical.X > 0f ? DisplayServer.WindowGetSize().X / logical.X : 1f;
        return Mathf.Max(0.01f, stretch * layer.GetGlobalTransformWithCanvas().X.Length());
    }

    // ================= water and world =================

    private void PaintWater(DrawLayer layer)
    {
        if (_host == null) return;
        _paintWatch.Restart();
        _brush.Begin(layer, PixelsPerUnit(layer));
        _art.DrawGrid(_brush, _host.Build.Active);
        _art.DrawWater(_brush, _time, _host.World.Weather.Raining);
        _brush.End();
        _paintMs += _paintWatch.Elapsed.TotalMilliseconds;
    }

    private void PaintWorld(DrawLayer layer)
    {
        if (_host == null) return;
        _paintWatch.Restart();
        World w = _host.World;
        Map m = w.Map;
        WorldDef def = w.Content.World;
        bool raining = w.Weather.Raining;
        bool winter = _winterDrawn;
        _toScreen = GetGlobalTransformWithCanvas();
        _brush.Begin(layer, PixelsPerUnit(layer));
        _hits.Clear();
        _bodiesMap.Clear();

        // Wet ground darkens, less so at night (as the rain's grey does), so rain never sinks the dark further.
        if (raining) _brush.Rect(0f, 0f, m.Width * TileSize, m.Height * TileSize, new Color(0.03f, 0.05f, 0.08f, 0.24f * (1f - Daylight.RainAtNight * _light.Night)));
        Gore.DrawDecals(_brush, _tex, _gore.Decals, w.Tick, def.TicksPerHour);
        _art.DrawGrass(_brush, _time, winter, raining ? 1.6f : 0.5f);
        if (raining) RainSplashes();

        // Shadows first, all together (one texture, so one batch), then everything that stands, by depth.
        float shadowAlpha = 0.36f * (1f - 0.8f * _light.Night) * (raining ? 0.6f : 1f);
        var shadow = new Vector2(-_light.SunX * (1f - _light.SunHeight) * 5f, 0.5f);
        CollectDrawables(w);
        _drawables.Sort(ByDepth.Instance);
        foreach (Drawable d in _drawables) PaintShadow(w, d, shadow, shadowAlpha);
        foreach (Drawable d in _drawables) Paint(w, d, winter);

        Effects(w);
        _brush.End();
        _paintMs += _paintWatch.Elapsed.TotalMilliseconds;
    }

    private void CollectDrawables(World w)
    {
        Map m = w.Map;
        _drawables.Clear();
        for (int i = 0; i < m.Trees.Count; i++)
        {
            Tree t = m.Trees[i];
            _drawables.Add(t.Felled ? new Drawable((t.Pos.Y + 0.6f) * TileSize, Kind.Stump, i, 0) : new Drawable(WorldArt.TreeBaseY(t), Kind.Tree, i, 0));
        }
        for (int i = 0; i < m.Bushes.Count; i++) _drawables.Add(new Drawable(WorldArt.BushBaseY(m.Bushes[i]), Kind.Bush, i, 0));
        for (int i = 0; i < w.Items.Count; i++)
            if (w.Items[i].Count > 0) _drawables.Add(new Drawable(WorldArt.StackSortY(w.Items[i]), Kind.Stack, i, 0));
        for (int y = 0; y < m.Height; y++)
            for (int x = 0; x < m.Width; x++)
            {
                TerrainKind k = (TerrainKind)m.Terrain[y * m.Width + x];
                if (k == TerrainKind.Wall) _drawables.Add(new Drawable(WorldArt.WallSortY(new SimPoint(x, y)), Kind.Wall, y * m.Width + x, 1));
                else if (k == TerrainKind.Door) _drawables.Add(new Drawable(WorldArt.DoorSortY(new SimPoint(x, y)), Kind.Door, y * m.Width + x, 1));
            }
        for (int i = 0; i < m.Roof.Count; i++)
            if (!m.Roof[i].Removed) _drawables.Add(new Drawable(_art.RoofSortY(m.Roof[i].Area), Kind.Roof, i, 2));
        CollectBuild(w);
        _drawables.Add(new Drawable(_art.RuinRoofSortY, Kind.RuinRoof, 0, 2));
        _drawables.Add(new Drawable(_art.HearthSortY, Kind.Hearth, 0, 0));
        _drawables.Add(new Drawable(_art.WatchPostSortY, Kind.WatchPost, 0, 0));
        for (int i = 0; i < w.Characters.Count; i++)
        {
            Character c = w.Characters[i];
            if (!_shown.TryGetValue(c.Id, out Shown? s)) continue;
            Character? bearer = Bearer(w, c);
            s.Look = Moonlit(Fading(LookOf(w, c, s), Kinship.NatureStage(c)), _light.Night);
            s.State = StateOf(w, c, s);
            s.State.Carried = bearer != null;
            bool lying = s.State.Pose is Pose.Sleep or Pose.Downed or Pose.Dead;
            float anchor = lying && !s.State.OnBed && !s.State.Carried ? LyingOffset : FeetOffset;
            Vector2 at = bearer != null && _shown.TryGetValue(bearer.Id, out Shown? bs) ? bs.At + bs.Spread : s.At + s.Spread;
            s.Feet = at + new Vector2(0f, anchor);
            // A dryad's crown rises above the head: mood marks and names keep clear of it.
            s.HeadAbove = HeadAbove(s.State) + (s.Look.Has(Kin.Dryad) && !lying ? CrownRise : 0f);
            _drawables.Add(new Drawable(s.Feet.Y + (bearer != null ? 0.1f : 0f), Kind.Figure, i, 0.5f));
        }
    }

    /// <summary>How far a dryad's crown of leaves rises above the top of the head, in map pixels.</summary>
    private const float CrownRise = 3.5f * FigureScale;

    /// <summary>How far above the feet the top of a figure's head is, by pose (mood marks sit there).</summary>
    private static float HeadAbove(in FigureState s)
    {
        if (s.Carried) return TileSize * 1.2f;
        return s.Pose switch
        {
            Pose.Sleep when s.OnBed => TileSize * 0.85f,
            Pose.Sleep or Pose.Downed or Pose.Dead => TileSize * 0.42f,
            Pose.Sit => TileSize * 1.32f - 9f * FigureScale,
            Pose.Crouch => TileSize * 1.32f - 5f * FigureScale,
            _ => TileSize * 1.32f,
        };
    }

    private void PaintShadow(World w, Drawable d, Vector2 shadow, float alpha)
    {
        if (alpha <= 0.01f) return;
        Map m = w.Map;
        switch (d.Kind)
        {
            case Kind.Tree: _art.TreeShadow(_brush, m.Trees[d.Index], shadow, alpha); break;
            case Kind.Bush: _art.BushShadow(_brush, m.Bushes[d.Index], shadow, alpha); break;
            case Kind.Stack: _art.StackShadow(_brush, w.Items[d.Index], alpha); break;
            case Kind.Hearth: _art.HearthShadow(_brush); break;
            case Kind.WatchPost: _art.PostShadow(_brush, alpha); break;
            case Kind.Figure:
            {
                Shown s = _shown[w.Characters[d.Index].Id];
                _brush.Push();
                _brush.Translate(s.Feet.X, s.Feet.Y);
                _brush.Scale(FigureScale, FigureScale);
                Figures.PersonShadow(_brush, s.Look, s.State, _tex, shadow / FigureScale, alpha);
                _brush.Pop();
                break;
            }
        }
    }

    /// <summary>Whoever is carrying this downed person to a bed right now (they share a tile), or null.</summary>
    private static Character? Bearer(World w, Character patient)
    {
        if (!patient.Downed || patient.Status != CharStatus.Member) return null;
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member && c.Job is { Kind: JobKind.Rescue, Stage: >= 1 } j && j.TargetId == patient.Id && c.Pos == patient.Pos)
                return c;
        return null;
    }

    /// <summary>Is this tree (or its stump) held sacred by a culture or faith (ADR-0023)? Read from the sim; the view never changes it.</summary>
    private static bool Sacred(World w, Tree t)
    {
        foreach (SacredPlace p in w.Culture.SacredPlaces)
            if (p.TreeId == t.Id) return true;
        return false;
    }

    private void Paint(World w, Drawable d, bool winter)
    {
        Map m = w.Map;
        switch (d.Kind)
        {
            case Kind.Tree:
                _art.DrawTree(_brush, m.Trees[d.Index], _time, winter);
                if (Sacred(w, m.Trees[d.Index])) WorldArt.DrawRibbons(_brush, m.Trees[d.Index], _time);
                break;
            case Kind.Stump:
                WorldArt.DrawStump(_brush, m.Trees[d.Index]);
                if (Sacred(w, m.Trees[d.Index])) WorldArt.DrawTornRibbon(_brush, m.Trees[d.Index]);
                break;
            case Kind.Bush: WorldArt.DrawBush(_brush, m.Bushes[d.Index], _time, winter); break;
            case Kind.Stack: WorldArt.DrawStack(_brush, w.Items[d.Index]); break;
            case Kind.Wall: _art.DrawWall(_brush, new SimPoint(d.Index % m.Width, d.Index / m.Width)); break;
            case Kind.Door: _art.DrawDoor(_brush, new SimPoint(d.Index % m.Width, d.Index / m.Width)); break;
            case Kind.Roof: _art.DrawRoof(_brush, m.Roof[d.Index], RoofAlpha(m.Roof[d.Index].Id)); break;
            case Kind.Bed or Kind.Plan or Kind.PlanLogs: PaintBuild(w, d); break;
            case Kind.RuinRoof: _art.DrawRuinRoof(_brush, _ruinAlpha); break;
            case Kind.Hearth: _art.DrawHearth(_brush, _time); break;
            case Kind.WatchPost: _art.DrawWatchPost(_brush, LanternLit(w) && _light.Night > 0.3f, _time); break;
            case Kind.Figure: PaintFigure(w.Characters[d.Index], w.Weather.Raining ? 1.4f : 0.6f); break;
        }
    }

    private void PaintFigure(Character c, float wind)
    {
        Shown s = _shown[c.Id];
        Vector2 feet = s.Feet;
        FigureState state = s.State;
        bool lying = state.Pose is Pose.Sleep or Pose.Downed or Pose.Dead;

        if (c.Id == _host!.SelectedId && !state.Carried)
        {
            _brush.Clear().Ellipse(feet.X, feet.Y + (lying ? -1f : 0.5f), lying ? 14f : 10f, lying ? 6f : 4.6f, 20).Stroke(Palette.Select.WithAlpha(0.9f), 1.6f);
            _brush.Clear().Ellipse(feet.X, feet.Y + (lying ? -1f : 0.5f), lying ? 15.6f : 11.6f, lying ? 7f : 5.6f, 20).Stroke(Palette.Ink.WithAlpha(0.7f), 0.8f);
        }

        // Someone burning in the sun glows like the fire they are: readable at any zoom, by day.
        if (state.Burning && _tex.Light != null)
        {
            float flick = 0.85f + 0.1f * MathF.Sin(_time * 11.3f + c.Id) + 0.05f * MathF.Sin(_time * 23.1f + c.Id * 2f);
            float size = TileSize * 3.3f * (0.94f + 0.06f * flick);
            _brush.Texture(_tex.Light, feet.X, feet.Y - TileSize * 0.6f, size, size * 1.1f, 0f, new Color(1f, 0.45f, 0.12f, 0.85f * flick));
            _brush.Texture(_tex.Light, feet.X, feet.Y - TileSize * 0.65f, size * 0.55f, size * 0.7f, 0f, new Color(1f, 0.62f, 0.25f, 0.75f * flick));
        }

        // A djinn is lit from within: a low, steady warmth behind the body that reads at the default zoom by
        // day (at night its point light does the same); warmer when restless, warmest when unbound.
        if (s.Look.Has(Kin.Djinn) && c.Status != CharStatus.Dead && _tex.Light != null)
        {
            float flick = 0.92f + 0.05f * MathF.Sin(_time * 3.7f + c.Id) + 0.03f * MathF.Sin(_time * 9.3f + c.Id * 2f);
            float warm = state.Nature switch { 0 => 0.5f, 1 => 0.6f, _ => 0.72f } * flick * (1f - 0.5f * _light.Night);
            float size = TileSize * 2.1f, up = lying ? TileSize * 0.15f : TileSize * 0.62f, squash = lying ? 0.7f : 1.25f;
            _brush.Texture(_tex.Light, feet.X, feet.Y - up, size, size * squash, 0f, DjinnWarmth.WithAlpha(warm * 0.75f));
            _brush.Texture(_tex.Light, feet.X, feet.Y - up - TileSize * 0.1f, size * 0.6f, size * squash * 0.65f, 0f, DjinnHeart.WithAlpha(warm * 0.6f));
            DjinnFire(c, s, wind); // behind the body: only the heat rising past the head and shoulders shows
        }

        _brush.Push();
        _brush.Translate(feet.X, feet.Y);
        _brush.Scale(FigureScale, FigureScale);
        Figures.Person(_brush, s.Look, state, _tex, Vector2.Zero, 0f);
        _brush.Pop();

        // Small flames lick up off the shoulders and head of the burning.
        if (state.Burning && !lying)
        {
            float top = feet.Y - s.HeadAbove;
            WorldArt.Flames(_brush, feet.X - 3.5f, top + TileSize * 0.38f, _time * 1.3f + c.Id, 0.42f);
            WorldArt.Flames(_brush, feet.X + 3f, top + TileSize * 0.3f, _time * 1.1f + c.Id * 2.3f, 0.38f);
            WorldArt.Flames(_brush, feet.X + 0.5f, top + TileSize * 0.08f, _time * 1.2f + c.Id * 3.1f, 0.34f);
        }

        Rect2 box = state.Carried ? new Rect2(feet.X - TileSize * 0.7f, feet.Y - TileSize * 1.5f, TileSize * 1.4f, TileSize * 0.8f)
            : lying && state.OnBed ? new Rect2(feet.X - TileSize * 0.4f, feet.Y - TileSize * 0.95f, TileSize * 0.8f, TileSize * 1.15f)
            : lying ? new Rect2(feet.X - TileSize * 0.7f, feet.Y - TileSize * 0.5f, TileSize * 1.4f, TileSize * 0.65f)
            : new Rect2(feet.X - TileSize * 0.42f, feet.Y - s.HeadAbove, TileSize * 0.84f, s.HeadAbove + TileSize * 0.13f);
        _hits.Add((c.Id, box));
        // What names keep off: the body as drawn, head to feet for someone upright (the hit box is
        // more generous, for clicking), the whole shape for someone lying down.
        _bodiesMap.Add((c.Id, lying || state.Carried ? box : new Rect2(feet.X - TileSize * 0.3f, feet.Y - s.HeadAbove, TileSize * 0.6f, s.HeadAbove)));
    }

    private FigureState StateOf(World w, Character c, Shown s)
    {
        Pose pose = Poses.For(w, c, s.Moving);
        var state = new FigureState
        {
            Pose = pose,
            Facing = Poses.FacingOf(w, c, s.Moving),
            Phase = s.Phase,
            Time = _time,
            Seed = c.Id * 1.37f,
            Tool = Poses.ToolFor(c, pose),
            Carry = c.Carrying == null ? Carried.None : c.Carrying.DefId switch { "timber" => Carried.Timber, "berries" => Carried.Berries, _ => Carried.Bundle },
            Wounds = GoreLog.WoundLevel(w, c),
            Burning = GoreLog.Burning(w, c),
            HeadLeft = pose == Pose.Dead ? ArtHash.Unit(c.Id, 5) < 0.5f : c.Facing == Facing.W,
            OnBed = pose == Pose.Sleep && OnBed(w.Map, c.Pos),
            Blanket = pose == Pose.Sleep && Building.BedAt(w, c.Pos) is { } bed ? BlanketOf(w, bed) : default,
            Nature = Kinship.NatureStage(c),
            Leaves = Kinship.LeavesAt(w.Tick, w.Content.World),
            Night = _light.Night,
        };
        if (pose == Pose.Dead && _gore.RemainsOf(w, c) is Remains r)
        {
            state.Death = r.Kind;
            state.DeadHours = Math.Max(0, w.Tick - r.Tick) / (float)w.Content.World.TicksPerHour;
        }
        return state;
    }

    private static bool OnBed(Map m, SimPoint p)
    {
        foreach (Bed b in m.Beds)
            if (b.Pos == p) return true;
        return false;
    }

    /// <summary>
    /// How someone looks: hair, skin and build from game/art/looks.json; cloth from looks.json or
    /// cast.json, darkened and muted; the demo's chest marks by role; a vampire's collar and diamond;
    /// the silhouette of their people and what their wounds give, from their species' nature (Kinship).
    /// </summary>
    private FigureLook LookOf(World w, Character c, Shown s)
    {
        if (s.HasLook && s.LookStatus == c.Status) return s.BaseLook;
        bool hostile = c.Status == CharStatus.Hostile || (c.Status == CharStatus.Dead && !c.Named);
        Look look = hostile ? _looks.Hostile : _looks.For(c.CastKey);
        SpeciesDef species = w.Content.SpeciesById(c.SpeciesId);
        bool vampire = species.DrinksBlood;
        Kin kin = Kinship.Of(species);
        Rgb cloth = look.Cloth ?? CastRgb(c.CastKey) ?? new Rgb(0.45f, 0.43f, 0.4f);
        (float wide, float tall) = look.Scale;
        Mark mark = hostile ? Mark.None
            : c.Id == w.PlayerId || c.Otherworlder ? Mark.You
            : vampire ? Mark.Vampire
            : c.RoleId == "forager" ? Mark.Forager
            : Mark.Cross;
        s.BaseLook = new FigureLook
        {
            Cloth = Palette.Of(hostile ? cloth : cloth.Muted(0.8f, 0.25f)),
            Hair = Palette.Of(look.Hair),
            Skin = Palette.Of(look.Skin),
            Mark = mark,
            Vampire = vampire,
            Hostile = hostile,
            HairStyle = look.HairStyle,
            Beard = look.Beard,
            Hood = look.Hood || hostile,
            Wide = wide,
            Tall = tall,
            Kin = kin,
            Stain = Palette.Of(Kinship.StainOf(Kinship.IchorOf(kin)), 0.9f),
        };
        s.HasLook = true;
        s.LookStatus = c.Status;
        return s.BaseLook;
    }

    private static readonly Color Moon = new(0.70f, 0.76f, 0.90f);

    /// <summary>The undead fade toward the grey of the grave as the purpose that keeps them slips: skin and clothes alike.</summary>
    private static FigureLook Fading(FigureLook look, int nature)
    {
        if (!look.Has(Kin.Undead) || nature <= 0) return look;
        look.Skin = look.Skin.Lerp(Palette.GraveGrey, 0.22f * nature);
        look.Cloth = look.Cloth.Lerp(Palette.GraveGrey, 0.16f * nature);
        return look;
    }

    /// <summary>
    /// At night people catch the moonlight: cloth, hair and skin are lifted toward a cool light in
    /// proportion to the dark, so a dark cloak still reads against the ground away from the fire
    /// (the tint over the map multiplies everything alike, so it cannot do that by itself). A thief
    /// in black catches less of it.
    /// </summary>
    private static FigureLook Moonlit(FigureLook look, float night)
    {
        if (night <= 0.01f) return look;
        float k = night * (look.Hostile ? 0.5f : 1f);
        look.Cloth = look.Cloth.Lerp(Moon, 0.32f * k);
        look.Hair = look.Hair.Lerp(Moon, 0.22f * k);
        look.Skin = look.Skin.Lerp(Moon, 0.1f * k);
        return look;
    }

    private Rgb? CastRgb(string castKey)
    {
        foreach (CastDef def in _host!.Content.Cast)
            if (def.Key == castKey && Rgb.TryParse(def.Color, out Rgb rgb)) return rgb;
        return null;
    }

    private void Effects(World w)
    {
        float wind = w.Weather.Raining ? 1.4f : 0.6f;
        _art.DrawSmoke(_brush, _art.HearthCenter + new Vector2(0f, -8f), _time, w.Weather.Raining ? 1.3f : 1f, wind, 0);
        foreach (Character c in w.Characters)
        {
            if (!_shown.TryGetValue(c.Id, out Shown? s)) continue;
            Vector2 feet = s.Feet;
            if (s.State.Burning)
            {
                _art.DrawSmoke(_brush, feet + new Vector2(0f, -TileSize * 0.6f), _time, 1f, wind, c.Id, dark: true);
                Gore.Embers(_brush, feet.X, feet.Y - TileSize * 0.5f, _time, c.Id);
            }
            // Unbound, a djinn throws off sparks (over everything that stands, like the burning's embers).
            if (s.Look.Has(Kin.Djinn) && c.Status != CharStatus.Dead && s.State.Nature >= 2 && s.State.Pose is not (Pose.Sleep or Pose.Downed))
                Gore.Embers(_brush, feet.X, feet.Y - s.HeadAbove, _time, c.Id);
            if (c.Status != CharStatus.Dead || _gore.RemainsOf(w, c) is not Remains r) continue;
            float hours = Math.Max(0, w.Tick - r.Tick) / (float)w.Content.World.TicksPerHour;
            if (r.Kind == DeathKind.Sun && hours < 12f && _light.Night < 0.5f)
                _art.DrawSmoke(_brush, feet + new Vector2(0f, -4f), _time * 0.8f, 0.55f, wind, c.Id, dark: true);
            if (hours > 1f && _light.Night < 0.6f && !w.Weather.Raining)
                Gore.Flies(_brush, feet.X, feet.Y - 4f, _time, c.Id, hours > 8f ? 6 : 3);
        }
    }

    /// <summary>
    /// A djinn is a spirit of living flame: heat wavers up from behind the head and shoulders (from the
    /// body, lying down), trailing behind as it walks; restless without a bargain it burns brighter.
    /// Drawn behind the figure, so it never crosses the face. (Unbound, it also sparks: Effects.)
    /// </summary>
    private void DjinnFire(Character c, Shown s, float wind)
    {
        FigureState st = s.State;
        bool lying = st.Pose is Pose.Sleep or Pose.Downed or Pose.Dead;
        Vector2 from = lying ? s.Feet + new Vector2(0f, -TileSize * 0.25f) : s.Feet - new Vector2(0f, s.HeadAbove * 0.72f);
        float trail = s.Moving ? st.Facing switch { Facing.E => -7f, Facing.W => 7f, _ => 0f } : 0f;
        float strength = st.Nature switch { 0 => 0.8f, 1 => 1.15f, _ => 1.5f };
        Figures.Shimmer(_brush, from, _time, c.Id, strength, wind * 4f + trail);
    }

    private void RainSplashes()
    {
        Rect2 view = VisibleMapRect();
        for (int i = 0; i < 90; i++)
        {
            float slot = MathF.Floor(_time * 2.2f + ArtHash.Unit(i, 300));
            float k = (_time * 2.2f + ArtHash.Unit(i, 300)) - slot;
            float x = view.Position.X + ArtHash.Unit(i, (long)slot, 301) * view.Size.X;
            float y = view.Position.Y + ArtHash.Unit(i, (long)slot, 302) * view.Size.Y;
            _brush.Clear().Ellipse(x, y, 1f + k * 3f, 0.6f + k * 1.6f, 10).Stroke(new Color(0.75f, 0.8f, 0.86f, 0.35f * (1f - k)), 0.6f);
        }
    }

    /// <summary>The part of the map on screen, in map pixels.</summary>
    private Rect2 VisibleMapRect()
    {
        Transform2D inv = _toScreen.AffineInverse();
        Vector2 size = GetViewportRect().Size;
        Vector2 a = inv * Vector2.Zero, b = inv * size;
        return new Rect2(a, b - a);
    }

    // ================= overlay (screen space, above the lighting) =================

    private void PaintOverlay(DrawLayer layer)
    {
        if (_host == null) return;
        _paintWatch.Restart();
        World w = _host.World;
        Vector2 screen = layer.GetViewportRect().Size;
        _brush.Begin(layer, PixelsPerUnit(layer));
        PaintPlanMarks(w);
        if (w.Weather.Raining) RainStreaks(screen);
        // A flat vignette: deepening it at night would black out the corners.
        _brush.TextureRect(_tex.Vignette, new Rect2(Vector2.Zero, screen), new Color(1f, 1f, 1f, 0.5f));
        Glints(w);
        _brush.End(); // text is drawn directly, after everything batched
        Labels(w);
        _paintMs += _paintWatch.Elapsed.TotalMilliseconds;
    }

    private void RainStreaks(Vector2 screen)
    {
        int n = _rain.Length / 2;
        var slant = new Vector2(-0.22f, 1f);
        for (int i = 0; i < n; i++)
        {
            float speed = 520f + 260f * ArtHash.Unit(i, 310);
            float len = 9f + 9f * ArtHash.Unit(i, 311);
            float x0 = ArtHash.Unit(i, 312) * (screen.X + 200f);
            float y = (ArtHash.Unit(i, 313) * (screen.Y + 40f) + _time * speed) % (screen.Y + 40f) - 20f;
            float x = (x0 + slant.X * (y + 20f) % (screen.X + 200f) + screen.X + 200f) % (screen.X + 200f) - 100f;
            _rain[i * 2] = new Vector2(x, y);
            _rain[i * 2 + 1] = new Vector2(x + slant.X * len, y + slant.Y * len);
        }
        _brush.Segments(_rain, null, new Color(0.72f, 0.78f, 0.86f, 0.32f), 1f);
    }

    /// <summary>
    /// What shows in the dark, above the night's tint: a thief's eyes catch the light; a djinn's eyes
    /// burn like embers; the undead's hollow eyes hold a faint cold gleam; a fae's glimmer twinkles.
    /// </summary>
    private void Glints(World w)
    {
        if (_light.Night < 0.15f) return;
        foreach (Character c in w.Characters)
        {
            if (!_shown.TryGetValue(c.Id, out Shown? peer)) continue;
            if (c.Status == CharStatus.Member && peer.Look.Has(Kin.Djinn | Kin.Undead | Kin.Fae)) NatureGlints(peer);
        }
        if (_light.Night < 0.35f) return;
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Hostile || c.Downed || !_shown.TryGetValue(c.Id, out Shown? s)) continue;
            Facing f = s.State.Facing;
            if (f == Facing.N) continue;
            Vector2 head = _toScreen * (s.Feet - new Vector2(0f, 33.4f * FigureScale));
            float z = _toScreen.X.Length() * FigureScale;
            var glint = new Color(0.95f, 0.82f, 0.55f, 0.85f * _light.Night);
            if (f == Facing.S)
            {
                _brush.Dot(head.X - 1.2f * z, head.Y + 0.8f * z, 0.7f * z, glint);
                _brush.Dot(head.X + 1.2f * z, head.Y + 0.8f * z, 0.7f * z, glint);
            }
            else _brush.Dot(head.X + (f == Facing.E ? 3.2f : -3.2f) * z, head.Y + 0.8f * z, 0.7f * z, glint);
        }
    }

    private static readonly Color EmberGlint = new(1f, 0.72f, 0.36f);
    private static readonly Color EmberGlow = new(1f, 0.45f, 0.15f);
    private static readonly Color ColdGlint = new(0.72f, 0.84f, 0.95f);
    private static readonly Color MoteGlint = new(1f, 0.96f, 0.82f);
    private readonly Vector2[] _eyes = new Vector2[2];

    /// <summary>The night glints of the four peoples (Glints), at the eyes and motes the figure is drawn with.</summary>
    private void NatureGlints(Shown s)
    {
        float night = Mathf.Clamp((_light.Night - 0.15f) / 0.6f, 0f, 1f);
        float z = _toScreen.X.Length() * FigureScale;
        FigureLook look = s.Look;
        FigureState st = s.State;
        int eyes = Figures.EyePoints(look, st, _eyes);
        for (int i = 0; i < eyes; i++)
        {
            Vector2 at = _toScreen * (s.Feet + _eyes[i] * FigureScale);
            if (look.Has(Kin.Djinn))
            {
                float burn = st.Nature switch { 0 => 0.8f, 1 => 0.95f, _ => 1.1f };
                _brush.Dot(at.X, at.Y, 1.9f * z, EmberGlow.WithAlpha(0.22f * night * burn));
                _brush.Dot(at.X, at.Y, 0.62f * z, EmberGlint.WithAlpha(Mathf.Min(1f, 0.95f * night * burn)));
            }
            else if (look.Has(Kin.Undead))
                _brush.Dot(at.X - 0.12f * z, at.Y, 0.3f * z, ColdGlint.WithAlpha(0.6f * night));
        }
        if (!look.Has(Kin.Fae) || st.Pose is Pose.Sleep or Pose.Downed or Pose.Dead) return;
        float strength = Figures.GlimmerStrength(st.Nature) * night;
        for (int i = 0; i < Figures.GlimmerMotes; i++)
        {
            Vector2 p = Figures.GlimmerMote(i, st, look.Tall, out float bright);
            float a = bright * strength;
            if (a <= 0.02f) continue;
            Vector2 at = _toScreen * (s.Feet + p * FigureScale);
            _brush.Dot(at.X, at.Y, 0.6f * z, MoteGlint.WithAlpha(0.9f * a));
            if (bright > 0.55f)
            {
                _brush.Line(at.X - 1.6f * z, at.Y, at.X + 1.6f * z, at.Y, MoteGlint.WithAlpha(0.45f * a), Mathf.Max(0.6f, 0.3f * z));
                _brush.Line(at.X, at.Y - 1.6f * z, at.X, at.Y + 1.6f * z, MoteGlint.WithAlpha(0.45f * a), Mathf.Max(0.6f, 0.3f * z));
            }
        }
    }

    /// <summary>
    /// Names, mood marks and stack counts, placed so they cover neither each other nor anyone's head
    /// and chest. Marks attach to heads (the mood mark at the upper right, the sleep mark at the upper
    /// left); names go under the feet, or beside them, or one line lower, whichever is clear first.
    /// </summary>
    private void Labels(World w)
    {
        int selected = _host!.SelectedId;
        float tile = TileSize * _toScreen.X.Length();
        _labels.Clear();
        _placed.Clear();
        _marks.Clear();

        // Obstacles first: every figure (a name keeps off everyone but its owner) and every stack count.
        _bodies.Clear();
        foreach ((int id, Rect2 box) in _bodiesMap) _bodies.Add((id, ScreenRect(box)));
        foreach (ItemStack stack in w.Items)
        {
            if (stack.Count <= 0) continue;
            Vector2 corner = (_toScreen * new Vector2((stack.Pos.X + 1f) * TileSize - 1f, (stack.Pos.Y + 1f) * TileSize - 1f)).Round();
            AddMark(corner, stack.Count < Numbers.Length ? Numbers[stack.Count] : Ui.N(stack.Count), CountFontSize, Palette.Name, new Vector2(1f, 1f), 3);
        }
        BuildCaptions();

        foreach (Character c in w.Characters)
        {
            if (!_shown.TryGetValue(c.Id, out Shown? s)) continue;
            bool dead = c.Status == CharStatus.Dead;
            Vector2 head = (_toScreen * (s.Feet - new Vector2(0f, s.HeadAbove))).Round();
            string? emote = dead ? null : ViewText.MoodEmote(w, c);
            string? glyph = emote == null ? null : ViewText.EmoteGlyph(emote);
            if (glyph != null) AddMark(head + new Vector2(tile * 0.45f, 0f), glyph, MarkerFontSize, EmoteColor(emote!), new Vector2(0.5f, 1f), 4);
            if (c.Asleep)
            {
                float bob = MathF.Sin(_time * 2f + c.Id) * 2f;
                AddMark(head + new Vector2(-tile * 0.4f, bob), _sleepMark, 11, new Color(0.85f, 0.85f, 0.9f, 0.85f), new Vector2(0.5f, 1f), 3);
            }
            _labels.Add(new NameTag(c.Id, s.Feet + new Vector2(0f, 2f), Ui.T(c.NameKey), dead, c.Status == CharStatus.Hostile));
        }

        // Names left to right: under the feet; else beside them (close, then clear of a neighbour's
        // body); else one line lower; failing all of those, wherever overlaps least.
        _labels.Sort(static (a, b) => a.Feet.X != b.Feet.X ? a.Feet.X.CompareTo(b.Feet.X) : a.Id.CompareTo(b.Id));
        foreach (NameTag label in _labels)
        {
            Vector2 size = _font.GetStringSize(label.Name, HorizontalAlignment.Left, -1, NameFontSize);
            Vector2 at = (_toScreen * label.Feet).Round();
            float h = size.Y - 2f, near = size.X / 2f + 4f, far = size.X / 2f + tile * 0.3f + 4f;
            Rect2 best = default;
            float least = float.MaxValue;
            for (int k = 0; k < 6; k++)
            {
                float dx = k switch { 1 => near, 2 => -near, 3 => far, 4 => -far, _ => 0f };
                var box = new Rect2(at.X + dx - size.X / 2f - 3f, at.Y + (k == 5 ? h : 0f), size.X + 6f, h);
                float overlap = OverlapArea(box, _placed);
                foreach ((int id, Rect2 body) in _bodies)
                    if (id != label.Id) overlap += OverlapArea(box, body);
                if (overlap < least)
                {
                    least = overlap;
                    best = box;
                }
                if (overlap <= 0f) break;
            }
            _placed.Add(best);
            Color color = label.Id == selected ? Palette.Select
                : label.Dead ? new Color(0.72f, 0.66f, 0.6f, 0.85f)
                : label.Hostile ? new Color(0.92f, 0.55f, 0.48f)
                : Palette.Name;
            Text(new Vector2(best.GetCenter().X, best.Position.Y), label.Name, NameFontSize, color, new Vector2(0.5f, 0f), 4);
        }

        foreach (TextMark m in _marks) Text(m.At, m.Text, m.Size, m.Color, m.Align, m.Outline);
    }

    /// <summary>A mark that stays where it is put (on a head, at a stack's corner); names keep clear of it.</summary>
    private void AddMark(Vector2 at, string text, int fontSize, Color color, Vector2 align, int outline)
    {
        Vector2 size = _font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        _placed.Add(new Rect2(at.X - size.X * align.X, at.Y - size.Y * align.Y, size.X, size.Y));
        _marks.Add(new TextMark(at, text, fontSize, color, align, outline));
    }

    /// <summary>A map-space rectangle on screen (the overlay's space).</summary>
    private Rect2 ScreenRect(Rect2 map)
    {
        Vector2 a = _toScreen * map.Position, b = _toScreen * map.End;
        return new Rect2(a, b - a).Abs();
    }

    private void Text(Vector2 screen, string text, int fontSize, Color color, Vector2 align, int outline)
    {
        Vector2 size = _font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        float ascent = _font.GetAscent(fontSize);
        var pos = new Vector2(Mathf.Round(screen.X - size.X * align.X), Mathf.Round(screen.Y - size.Y * align.Y + ascent));
        if (outline > 0) _overlay.DrawStringOutline(_font, pos, text, HorizontalAlignment.Left, -1, fontSize, outline, Palette.NameShadow);
        _overlay.DrawString(_font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }

    private static float OverlapArea(Rect2 box, List<Rect2> placed)
    {
        float area = 0f;
        foreach (Rect2 r in placed) area += OverlapArea(box, r);
        return area;
    }

    private static float OverlapArea(Rect2 a, Rect2 b)
    {
        Rect2 i = a.Intersection(b);
        return i.Size.X > 0f && i.Size.Y > 0f ? i.Size.X * i.Size.Y : 0f;
    }

    private static Color EmoteColor(string emote) => emote switch
    {
        "anger" => new Color(0.93f, 0.36f, 0.27f),
        "sweat" => new Color(0.58f, 0.78f, 0.93f),
        "hearts" => new Color(0.94f, 0.52f, 0.66f),
        "sparkle" => new Color(0.98f, 0.87f, 0.47f),
        "alert" => new Color(0.98f, 0.60f, 0.28f),
        "question" => new Color(0.82f, 0.82f, 0.97f),
        "zzz" => new Color(0.76f, 0.80f, 0.90f),
        "gloom" => new Color(0.66f, 0.68f, 0.74f),
        "music" => new Color(0.66f, 0.87f, 0.63f),
        _ => Palette.Name,
    };

    private static LookBook LoadLooks()
    {
        string path = System.IO.Path.Combine(ArtTextures.ArtDir, "looks.json");
        try
        {
            return LookBook.Load(path);
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or FormatException or InvalidOperationException or System.Text.Json.JsonException)
        {
            GD.PushWarning("Return: could not read " + path + " (" + ex.Message + "); everyone gets the default look.");
            return LookBook.Fallback();
        }
    }

    private static string[] BuildNumbers(int n)
    {
        var a = new string[n];
        for (int i = 0; i < n; i++) a[i] = Ui.N(i);
        return a;
    }
}
