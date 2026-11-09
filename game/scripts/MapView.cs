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
/// 2026-11-09 update). Orchestrates three passes: the ground (terrain, mud, grime, grid; redrawn
/// only when the season turns), the world (water, blood, grass, then everything that stands up
/// sorted by y so things in front hide things behind: trees, bushes, goods, walls, roofs, the
/// hearth and the people, then smoke and flies), and an overlay above the lighting (rain, a
/// vignette, names and mood marks). Day and night come from Art.Lighting. Read-only over World:
/// the only state here is view smoothing, walk phases and the gore log, all derived from the sim.
/// </summary>
public partial class MapView : Node2D
{
    public const int TileSize = 24;

    /// <summary>Figure units to map pixels: a standing person is about 1.3 tiles tall (ADR-0009).</summary>
    private const float FigureScale = TileSize * 1.32f / Figures.Height;

    /// <summary>Feet stand a little below the tile centre, so a figure rises into the tile behind.</summary>
    private const float FeetOffset = TileSize * 0.3f;

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
    private DrawLayer _world = null!;
    private DrawLayer _overlay = null!;
    private readonly GoreLog _gore = new();
    private readonly Brush _brush = new();
    private readonly Dictionary<int, Shown> _shown = new();
    private readonly List<Drawable> _drawables = new();
    private readonly List<(int Id, Rect2 Box)> _hits = new();
    private readonly List<NameTag> _labels = new();
    private readonly List<Rect2> _placed = new();
    private readonly Dictionary<string, Color> _castColors = new(StringComparer.Ordinal);
    private readonly float[] _roofAlpha = new float[16];
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
        public Vector2 At;
        public float Phase;
        public bool Moving;
        public bool HasLook;
        public CharStatus LookStatus;
        public FigureLook Look;
        public FigureState State;
        public Vector2 Feet;
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
    }

    private readonly record struct Drawable(float Y, Kind Kind, int Index, float Order);

    private readonly record struct NameTag(int Id, Vector2 Feet, Vector2 Head, string Name, string? Emote, bool Dead, bool Asleep, bool Hostile);

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
        _tex = ArtTextures.Load();
        _looks = LoadLooks();
        World w = host.World;
        _art = new WorldArt(w.Map, _tex);

        _ground = DrawLayer.Make("Ground", PaintGround);
        _ground.TextureRepeat = TextureRepeatEnum.Enabled;
        _ground.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        _world = DrawLayer.Make("World", PaintWorld);
        _world.TextureRepeat = TextureRepeatEnum.Enabled;
        _world.TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        AddChild(_ground);
        AddChild(_world);
        _lighting = new Lighting(this, _tex, _art.HearthCenter, _art.LanternAt);

        // The overlay sits above the lit, tinted map and below the menu bar: unshaded, untinted.
        var layer = new CanvasLayer { Name = "MapOverlay", Layer = 0 };
        _overlay = DrawLayer.Make("Overlay", PaintOverlay);
        _overlay.Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        _overlay.TextureFilter = TextureFilterEnum.Linear;
        layer.AddChild(_overlay);
        AddChild(layer);

        for (int i = 0; i < _roofAlpha.Length; i++) _roofAlpha[i] = 1f;
        _rain = new Vector2[260 * 2];
        _gore.Observe(w);
        _light = Daylight.At(w.Tick, w.Content.World, w.Weather.Raining);
        FrameProbe.AttachIfAsked(host, this);
    }

    /// <summary>Lets the gore log see a tick the view did not draw (SimHost's warm-up), so blood lands where it fell.</summary>
    public void Witness(World w) => _gore.Observe(w);

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
        Smooth(w, delta);
        _gore.Observe(w);
        _light = Daylight.At(w.Tick, def, w.Weather.Raining);
        _lighting.Update(_light, _time, LanternLit(w));
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
        _world.QueueRedraw();
        _overlay.QueueRedraw();
        _paintMs += _paintWatch.Elapsed.TotalMilliseconds;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (_host == null || e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
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
            if (!_shown.TryGetValue(c.Id, out Shown? s))
            {
                s = new Shown { At = target };
                _shown[c.Id] = s;
            }
            Vector2 was = s.At;
            s.At = was.DistanceTo(target) > TileSize * 3 ? target : was.Lerp(target, alpha);
            float moved = s.At.DistanceTo(was);
            s.Moving = c.IsActive && !c.Downed && !c.Asleep && (stepping || s.At.DistanceTo(target) > 0.6f);
            if (s.Moving) s.Phase = (s.Phase + moved / TileSize * StridesPerTile) % 1f;
        }
    }

    private void UpdateRoofs(World w, float delta)
    {
        Map m = w.Map;
        float k = 1f - Mathf.Exp(-delta * 6f);
        for (int i = 0; i < m.Roof.Count && i < _roofAlpha.Length; i++)
            _roofAlpha[i] = Mathf.Lerp(_roofAlpha[i], Occupied(w, m.Roof[i].Area) ? 0.3f : 0.94f, k);
        _ruinAlpha = Mathf.Lerp(_ruinAlpha, Occupied(w, m.CoveredRuins) ? 0.32f : 0.95f, k);
    }

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
        if (_groundTex != null)
        {
            layer.DrawTextureRect(_groundTex, new Rect2(0f, 0f, m.Width * TileSize, m.Height * TileSize), false);
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

    // ================= world =================

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

        _art.DrawGrid(_brush);
        if (raining) _brush.Rect(0f, 0f, m.Width * TileSize, m.Height * TileSize, new Color(0.03f, 0.05f, 0.08f, 0.24f));
        _art.DrawWater(_brush, _time, raining);
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
        for (int i = 0; i < m.Roof.Count; i++) _drawables.Add(new Drawable(_art.RoofSortY, Kind.Roof, i, 2));
        _drawables.Add(new Drawable(_art.RuinRoofSortY, Kind.RuinRoof, 0, 2));
        _drawables.Add(new Drawable(_art.HearthSortY, Kind.Hearth, 0, 0));
        _drawables.Add(new Drawable(_art.WatchPostSortY, Kind.WatchPost, 0, 0));
        for (int i = 0; i < w.Characters.Count; i++)
        {
            Character c = w.Characters[i];
            if (!_shown.TryGetValue(c.Id, out Shown? s)) continue;
            Character? bearer = Bearer(w, c);
            s.Feet = (bearer != null && _shown.TryGetValue(bearer.Id, out Shown? bs) ? bs.At : s.At) + new Vector2(0f, FeetOffset);
            s.Look = LookOf(w, c, s);
            s.State = StateOf(w, c, s);
            s.State.Carried = bearer != null;
            _drawables.Add(new Drawable(s.Feet.Y + (bearer != null ? 0.1f : 0f), Kind.Figure, i, 0.5f));
        }
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

    private void Paint(World w, Drawable d, bool winter)
    {
        Map m = w.Map;
        switch (d.Kind)
        {
            case Kind.Tree: _art.DrawTree(_brush, m.Trees[d.Index], _time, winter); break;
            case Kind.Stump: WorldArt.DrawStump(_brush, m.Trees[d.Index]); break;
            case Kind.Bush: WorldArt.DrawBush(_brush, m.Bushes[d.Index], _time, winter); break;
            case Kind.Stack: WorldArt.DrawStack(_brush, w.Items[d.Index]); break;
            case Kind.Wall: _art.DrawWall(_brush, new SimPoint(d.Index % m.Width, d.Index / m.Width)); break;
            case Kind.Door: WorldArt.DrawDoor(_brush, new SimPoint(d.Index % m.Width, d.Index / m.Width)); break;
            case Kind.Roof: _art.DrawRoof(_brush, m.Roof[d.Index], _roofAlpha[Math.Min(d.Index, _roofAlpha.Length - 1)]); break;
            case Kind.RuinRoof: _art.DrawRuinRoof(_brush, _ruinAlpha); break;
            case Kind.Hearth: _art.DrawHearth(_brush, _time); break;
            case Kind.WatchPost: _art.DrawWatchPost(_brush, LanternLit(w) && _light.Night > 0.3f, _time); break;
            case Kind.Figure: PaintFigure(w.Characters[d.Index]); break;
        }
    }

    private void PaintFigure(Character c)
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

        _brush.Push();
        _brush.Translate(feet.X, feet.Y);
        _brush.Scale(FigureScale, FigureScale);
        Figures.Person(_brush, s.Look, state, _tex, Vector2.Zero, 0f);
        _brush.Pop();

        Rect2 box = lying
            ? new Rect2(feet.X - TileSize * 0.7f, feet.Y - TileSize * (state.Carried ? 1.5f : 0.6f), TileSize * 1.4f, TileSize * 0.8f)
            : new Rect2(feet.X - TileSize * 0.42f, feet.Y - TileSize * 1.32f, TileSize * 0.84f, TileSize * 1.45f);
        _hits.Add((c.Id, box));
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
    /// cast.json, darkened and muted; the demo's chest marks by role; a vampire's collar and diamond.
    /// </summary>
    private FigureLook LookOf(World w, Character c, Shown s)
    {
        if (s.HasLook && s.LookStatus == c.Status) return s.Look;
        bool hostile = c.Status == CharStatus.Hostile || (c.Status == CharStatus.Dead && !c.Named);
        Look look = hostile ? _looks.Hostile : _looks.For(c.CastKey);
        SpeciesDef species = w.Content.SpeciesById(c.SpeciesId);
        bool vampire = species.DrinksBlood;
        Rgb cloth = look.Cloth ?? CastRgb(c.CastKey) ?? new Rgb(0.45f, 0.43f, 0.4f);
        (float wide, float tall) = look.Scale;
        Mark mark = hostile ? Mark.None
            : c.Id == w.PlayerId || c.Otherworlder ? Mark.You
            : vampire ? Mark.Vampire
            : c.RoleId == "forager" ? Mark.Forager
            : Mark.Cross;
        s.Look = new FigureLook
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
        };
        s.HasLook = true;
        s.LookStatus = c.Status;
        return s.Look;
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
            Vector2 feet = s.At + new Vector2(0f, FeetOffset);
            if (GoreLog.Burning(w, c))
            {
                _art.DrawSmoke(_brush, feet + new Vector2(0f, -TileSize * 0.6f), _time, 1f, wind, c.Id, dark: true);
                Gore.Embers(_brush, feet.X, feet.Y - TileSize * 0.5f, _time, c.Id);
            }
            if (c.Status != CharStatus.Dead || _gore.RemainsOf(w, c) is not Remains r) continue;
            float hours = Math.Max(0, w.Tick - r.Tick) / (float)w.Content.World.TicksPerHour;
            if (r.Kind == DeathKind.Sun && hours < 12f && _light.Night < 0.5f)
                _art.DrawSmoke(_brush, feet + new Vector2(0f, -4f), _time * 0.8f, 0.55f, wind, c.Id, dark: true);
            if (hours > 1f && _light.Night < 0.6f && !w.Weather.Raining)
                Gore.Flies(_brush, feet.X, feet.Y - 4f, _time, c.Id, hours > 8f ? 6 : 3);
        }
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
        if (w.Weather.Raining) RainStreaks(screen);
        _brush.TextureRect(_tex.Vignette, new Rect2(Vector2.Zero, screen), new Color(1f, 1f, 1f, 0.55f + 0.2f * _light.Night));
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

    /// <summary>A thief's eyes catch the light in the dark.</summary>
    private void Glints(World w)
    {
        if (_light.Night < 0.35f) return;
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Hostile || !_shown.TryGetValue(c.Id, out Shown? s)) continue;
            Facing f = Poses.FacingOf(w, c, s.Moving);
            if (f == Facing.N) continue;
            Vector2 head = _toScreen * (s.At + new Vector2(0f, FeetOffset - 33.4f * FigureScale));
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

    private void Labels(World w)
    {
        int selected = _host!.SelectedId;
        _labels.Clear();
        foreach (Character c in w.Characters)
        {
            if (!_shown.TryGetValue(c.Id, out Shown? s)) continue;
            bool dead = c.Status == CharStatus.Dead;
            Vector2 feet = s.At + new Vector2(0f, FeetOffset);
            bool lying = dead || c.Downed || c.Asleep;
            Vector2 head = feet - new Vector2(0f, lying ? TileSize * 0.65f : TileSize * 1.32f);
            _labels.Add(new NameTag(c.Id, feet + new Vector2(0f, 2f), head, Ui.T(c.NameKey), dead ? null : ViewText.MoodEmote(w, c), dead, c.Asleep, c.Status == CharStatus.Hostile));
        }

        // Names go under the feet, left to right; a name that would overlap one already placed drops a line.
        _labels.Sort(static (a, b) => a.Feet.X != b.Feet.X ? a.Feet.X.CompareTo(b.Feet.X) : a.Id.CompareTo(b.Id));
        _placed.Clear();
        foreach (NameTag label in _labels)
        {
            Vector2 size = _font.GetStringSize(label.Name, HorizontalAlignment.Left, -1, NameFontSize);
            Vector2 at = (_toScreen * label.Feet).Round();
            var box = new Rect2(at.X - size.X / 2 - 3, at.Y, size.X + 6, size.Y - 2);
            for (int tries = 0; tries < 4 && Overlaps(box, _placed); tries++) box.Position += new Vector2(0, size.Y - 2);
            _placed.Add(box);
            Color color = label.Id == selected ? Palette.Select
                : label.Dead ? new Color(0.72f, 0.66f, 0.6f, 0.85f)
                : label.Hostile ? new Color(0.92f, 0.55f, 0.48f)
                : Palette.Name;
            Text(new Vector2(at.X, box.Position.Y), label.Name, NameFontSize, color, new Vector2(0.5f, 0f), 4);

            Vector2 head = (_toScreen * label.Head).Round();
            if (label.Asleep)
            {
                float bob = MathF.Sin(_time * 2f + label.Id) * 2f;
                Text(head + new Vector2(10f, -2f + bob), "z", 11, new Color(0.85f, 0.85f, 0.9f, 0.85f), new Vector2(0.5f, 1f), 3);
            }
            if (label.Emote == null) continue;
            string? glyph = ViewText.EmoteGlyph(label.Emote);
            if (glyph != null) Text(head, glyph, MarkerFontSize, EmoteColor(label.Emote), new Vector2(0.5f, 1f), 4);
        }

        foreach (ItemStack stack in w.Items)
        {
            if (stack.Count <= 0) continue;
            Vector2 corner = _toScreen * new Vector2((stack.Pos.X + 1f) * TileSize - 1f, (stack.Pos.Y + 1f) * TileSize - 1f);
            Text(corner.Round(), stack.Count < Numbers.Length ? Numbers[stack.Count] : Ui.N(stack.Count), CountFontSize, Palette.Name, new Vector2(1f, 1f), 3);
        }
    }

    private void Text(Vector2 screen, string text, int fontSize, Color color, Vector2 align, int outline)
    {
        Vector2 size = _font.GetStringSize(text, HorizontalAlignment.Left, -1, fontSize);
        float ascent = _font.GetAscent(fontSize);
        var pos = new Vector2(Mathf.Round(screen.X - size.X * align.X), Mathf.Round(screen.Y - size.Y * align.Y + ascent));
        if (outline > 0) _overlay.DrawStringOutline(_font, pos, text, HorizontalAlignment.Left, -1, fontSize, outline, Palette.NameShadow);
        _overlay.DrawString(_font, pos, text, HorizontalAlignment.Left, -1, fontSize, color);
    }

    private static bool Overlaps(Rect2 box, List<Rect2> placed)
    {
        foreach (Rect2 r in placed)
            if (r.Intersects(box)) return true;
        return false;
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
        catch (Exception ex) when (ex is System.IO.IOException or FormatException or System.Text.Json.JsonException)
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
