using System;
using Godot;
using Return.Game.Logic;
using Return.Sim;

namespace Return.Game.Art;

/// <summary>The chest mark from the demo: the player's return arrow, a cross, a leaf, the vampire's diamond.</summary>
public enum Mark
{
    None,
    You,
    Forager,
    Cross,
    Vampire,
}

/// <summary>What someone carries in their arms.</summary>
public enum Carried
{
    None,
    Timber,
    Berries,
    Bundle,
}

/// <summary>How a person looks this frame (from looks.json, cast.json and their species).</summary>
public struct FigureLook
{
    public Color Cloth;
    public Color Hair;
    public Color Skin;
    public Mark Mark;
    public bool Vampire;
    public bool Hostile;
    public bool Ghoul;
    public HairStyle HairStyle;
    public bool Beard;
    public bool Hood;
    public float Wide;
    public float Tall;

    /// <summary>The peoples whose silhouette language they wear (Logic/Kin: dryad, djinn, fae, undead; flags, for hybrids).</summary>
    public Kin Kin;

    /// <summary>What their wounds soak into their clothes: blood, sap, grave dust or ash (Kinship.StainOf). Unset means blood.</summary>
    public Color Stain;

    public readonly bool Has(Kin kin) => (Kin & kin) != 0;
}

/// <summary>What a person is doing this frame, all of it derived from sim state.</summary>
public struct FigureState
{
    public Pose Pose;
    public Facing Facing;
    public float Phase;
    public float Time;
    public float Seed;
    public HeldTool Tool;
    public Carried Carry;
    public int Wounds;
    public bool Burning;
    public DeathKind Death;
    public float DeadHours;
    public bool HeadLeft;
    public bool OnBed;
    public bool Carried;

    /// <summary>How the need of their nature stands: 0 met, 1 low, 2 critical (a djinn smoulders, the fae dim, the undead fade, a dryad wilts).</summary>
    public int Nature;

    /// <summary>The season of a dryad's leaves.</summary>
    public Leaves Leaves;

    /// <summary>How dark it is, 0 day … 1 deep night: a fae's wings catch the moonlight.</summary>
    public float Night;
}

/// <summary>
/// People in the grounded three-quarter view (ADR-0009 option B), ported from figureB in
/// docs/art/art-direction-2026-11-09.html: anchored at the feet, about 39 units tall with a head
/// to body of about 1:3.8, legs and arms that swing with the walk cycle, four facings, thin warm
/// ink outlines, and a soft two-tone shading with light from the upper left. Added for the
/// darker, more realistic pass: builds and hair styles, belts and hem grime, tools in hand, goods
/// carried, blood soaking the clothes of the badly wounded, skin reddening in the sun, and the
/// lying poses (asleep, downed, dead: splayed, greyed, charred by the sun or wasted by hunger).
/// The four peoples of ADR-0022 each have a silhouette that reads at the default zoom, and their
/// nature's need shows on them: a dryad's crown of leaves keeps the season and wilts away from the
/// trees; a djinn's eyes are embers and heat wavers off it (drawn with the effects); a fae is slight, with
/// pointed ears, translucent wings and a glimmer that dims among broken words; the undead are
/// grey, hollow-eyed and ragged and walk stiffly (MapView fades them toward the grave as their
/// purpose slips).
/// Units are figure units; the caller sets the transform (feet position and scale).
/// </summary>
public static class Figures
{
    /// <summary>Height of a standing figure in figure units (feet to crown).</summary>
    public const float Height = 39f;

    private const float Lw = 0.95f;

    /// <summary>The fae's glimmer: this many motes about the head and shoulders.</summary>
    public const int GlimmerMotes = 4;

    private static readonly Color Vine = Palette.Hex(0x3d4f22);
    private static readonly Color Twig = Palette.Hex(0x3a2e22);
    private static readonly Color LeafGreen = Palette.Hex(0x58722f);
    private static readonly Color LeafGreen2 = Palette.Hex(0x6d8a39);
    private static readonly Color LeafGold = Palette.Hex(0xa27b2b);
    private static readonly Color LeafRusset = Palette.Hex(0x8b4b23);
    private static readonly Color LeafDead = Palette.Hex(0x5f4b31);
    private static readonly Color EmberEye = new(1f, 0.74f, 0.38f);
    private static readonly Color EmberHalo = new(1f, 0.5f, 0.18f, 0.22f);
    private static readonly Color ColdGleam = new(0.70f, 0.78f, 0.84f, 0.75f);
    private static readonly Color WingFill = new(0.82f, 0.88f, 0.93f, 0.26f);
    private static readonly Color WingEdge = new(0.90f, 0.95f, 1f, 0.6f);
    private static readonly Color WingVein = new(0.90f, 0.95f, 1f, 0.32f);
    private static readonly Color Mote = new(1f, 0.96f, 0.84f);

    public static void Person(Brush b, in FigureLook look, in FigureState s, ArtTextures tex, Vector2 shadow, float shadowAlpha)
    {
        if (s.Pose is Pose.Sleep or Pose.Downed or Pose.Dead)
        {
            Lying(b, look, s, tex, shadowAlpha);
            return;
        }

        Facing f = s.Facing;
        bool side = f is Facing.E or Facing.W;
        float d = f == Facing.W ? -1f : 1f;
        bool walking = s.Pose == Pose.Walk;
        bool undead = look.Has(Kin.Undead), dryad = look.Has(Kin.Dryad), fae = look.Has(Kin.Fae);
        float sw = walking ? MathF.Sin(s.Phase * MathF.Tau) : 0f;
        // The undead walk stiffly: a short stride and arms that hardly swing.
        float legSw = undead ? sw * 0.7f : sw, armSw = undead ? sw * 0.35f : sw;
        float breathe = MathF.Sin(s.Time * 2.1f + s.Seed) * 0.35f;
        bool sit = s.Pose == Pose.Sit, crouch = s.Pose == Pose.Crouch;
        float drop = sit ? 9f : crouch ? 5f : 0f;
        float W = look.Wide, T = look.Tall;
        Color cloth = look.Cloth;
        Color skin = s.Burning && look.Vampire ? look.Skin.Lerp(new Color(0.78f, 0.32f, 0.22f), 0.45f) : look.Skin;
        float hunch = Hunch(look, side, d);

        // Soft contact shadow, stretched away from the sun.
        Shadow(b, tex, shadow.X, shadow.Y, 9.5f * W + MathF.Abs(shadow.X) * 0.5f, 3.4f, shadowAlpha);

        float bob = Bob(look, s);

        // A seated person's legs rest on the ground (the feet frame); the body above drops onto them.
        if (sit) SeatedLegs(b, look, side, d, W);

        b.Push();
        b.Translate(0f, bob + drop);

        // Legs.
        if (!sit)
        {
            if (side)
            {
                Leg(b, look, 0f, 0f, -legSw * 0.5f * d, T, true);
                Leg(b, look, 0f, 0f, legSw * 0.5f * d, T, false);
            }
            else
            {
                float lift = crouch ? 4f : 0f;
                Leg(b, look, -2.6f * W, walking ? MathF.Max(0f, legSw) * 2.5f : lift, 0f, T, false);
                Leg(b, look, 2.6f * W, walking ? MathF.Max(0f, -legSw) * 2.5f : lift, 0f, T, false);
            }
        }

        // A fae's wings rise behind the shoulders (over the back when seen from behind, below).
        if (fae && f != Facing.N) Wings(b, s, side, d, W, T, walking);

        // Long hair falls behind the shoulders, seen from the front.
        if (f == Facing.S && look.HairStyle == HairStyle.Long && !look.Hood)
            b.Clear().Ellipse(0f, -31.5f * T, 6.4f, 6.8f, 12).Fill(look.Hair).Stroke(Palette.Ink, 0.7f);

        // Cloak or torso.
        BodyShape(b, look, side, d, hunch, W, T);
        b.Lit(cloth, 0.26f).Stroke(Palette.Ink, Lw);
        if (!look.Vampire && !look.Ghoul)
        {
            // Hem grime (a ragged hem has no straight line of it) and a belt (a dryad's is a vine).
            float hw = side ? 5.6f : 9.0f * W;
            if (!undead)
            {
                b.Clear().Add(-hw * 0.97f, -13.6f * T).Add(hw * 0.97f, -13.6f * T).Add(hw, -11f * T).Add(-hw, -11f * T);
                if (side) b.Offset(d * 0.2f, 0f);
                b.Fill(cloth.Darkened(0.45f).WithAlpha(0.55f));
            }
            float bw = side ? 4.9f : 7.7f * W;
            b.Line(-bw, -18.2f * T, bw, -18.2f * T, dryad ? Vine : Palette.Hex(0x261d16), dryad ? 1.1f : 1.3f);
        }
        if (undead) Tears(b, cloth, side, d, W, T);
        if (dryad) HemLeaves(b, s, side, d, W, T);
        Folds(b, cloth, side, d, T);
        if (s.Wounds > 0) Stains(b, look, s, side, W, T);

        if (f == Facing.S) ChestMark(b, look, T);

        // Carried goods are held in front of the chest (or on a shoulder for timber).
        bool armsBusy = s.Carry != Carried.None;

        if (side)
        {
            // Seen from the side, the near arm reaches toward the facing (Logic/Poses), so a blow lands
            // in front; the tool head is mirrored to lead the downstroke.
            float reach = Poses.SideArmReach(s.Pose, s.Phase, s.Time, s.Seed, armsBusy);
            if (undead && walking && !armsBusy) reach *= 0.35f;
            float ang = Poses.SideArmAngle(reach, f);
            Arm(b, look, skin, hunch * 0.7f, ang, true, s.Tool, -d, T);
        }
        else
        {
            float swing = walking ? armSw * 0.5f
                : s.Pose == Pose.Swing ? MathF.Sin(s.Time * 7f + s.Seed) * 0.9f - 0.6f
                : crouch ? 0.6f + MathF.Sin(s.Time * 3f + s.Seed) * 0.3f
                : sit ? 0.5f
                : 0f;
            if (armsBusy) swing *= 0.25f;
            bool front = f == Facing.S;
            float left = 0.1f + (front ? swing * 0.3f : -swing * 0.3f);
            float right = -0.1f - (s.Pose == Pose.Swing ? MathF.Abs(swing) * 1.6f : front ? swing * 0.3f : -swing * 0.3f);
            if (s.Pose == Pose.Talk) right = -0.35f - (0.45f + MathF.Sin(s.Time * 2.4f + s.Seed) * 0.3f);
            if (armsBusy) { left = -0.55f; right = 0.55f; }
            Arm(b, look, skin, -7.6f * W, left, front, HeldTool.None, 1f, T);
            Arm(b, look, skin, 7.6f * W, right, front, s.Tool, 1f, T);
        }

        // Seen from behind, a fae's wings lie over the back and shoulders.
        if (fae && f == Facing.N) Wings(b, s, side, d, W, T, walking);

        if (s.Carry != Carried.None) CarriedGoods(b, s.Carry, side, d, T);

        // Neck, collar, head.
        float hx = (side ? 1f * d : 0f) + hunch;
        float hy = (-33.5f + breathe * 0.4f + (look.Ghoul ? 2f : 0f)) * T;
        b.Clear().Add(hx - 1.6f, -30.5f * T).Add(hx + 1.6f, -30.5f * T).Add(hx + 1.6f, -27.5f * T).Add(hx - 1.6f, -27.5f * T)
            .Fill(skin.Darkened(0.18f)).Stroke(Palette.Ink, 0.7f);
        if (look.Vampire)
        {
            if (side) b.Clear().Add(hx - 3f * d, -29f * T).Add(hx - 5f * d, -39f * T).Add(hx + 1f * d, -30f * T);
            else b.Clear().Add(-7f, -28.5f * T).Add(-6.5f, -39f * T).Add(-2f, -30.5f * T).Add(2f, -30.5f * T).Add(6.5f, -39f * T).Add(7f, -28.5f * T);
            b.Lit(Palette.Collar, 0.3f).Stroke(Palette.Ink, Lw);
        }
        Head(b, look, s, skin, f, side, d, hx, hy);
        b.Pop();

        // The fae's glimmer, faint by day (at night the map's overlay lights the same motes).
        if (fae) Glimmer(b, s, T, GlimmerStrength(s.Nature) * 0.6f);
    }

    /// <summary>The ghoul's hunch, and the undead's lean seen from the side (the head carried forward).</summary>
    private static float Hunch(in FigureLook look, bool side, float d) =>
        look.Ghoul ? 3f * d : look.Has(Kin.Undead) && side ? 1.3f * d : 0f;

    /// <summary>How far the body rises and falls with the walk (the undead's tread is heavier).</summary>
    private static float Bob(in FigureLook look, in FigureState s) =>
        s.Pose == Pose.Walk ? -MathF.Abs(MathF.Cos(s.Phase * MathF.Tau)) * (look.Has(Kin.Undead) ? 1.6f : 1.2f) : 0f;

    /// <summary>Where the centre of a standing figure's head is drawn this frame, in figure units from the feet (as Person draws it).</summary>
    public static Vector2 HeadCenter(in FigureLook look, in FigureState s)
    {
        bool side = s.Facing is Facing.E or Facing.W;
        float d = s.Facing == Facing.W ? -1f : 1f;
        float drop = s.Pose == Pose.Sit ? 9f : s.Pose == Pose.Crouch ? 5f : 0f;
        float breathe = MathF.Sin(s.Time * 2.1f + s.Seed) * 0.35f;
        float hx = (side ? 1f * d : 0f) + Hunch(look, side, d);
        float hy = (-33.5f + breathe * 0.4f + (look.Ghoul ? 2f : 0f)) * look.Tall;
        return new Vector2(hx, hy + Bob(look, s) + drop);
    }

    /// <summary>
    /// The eyes as drawn this frame, in figure units from the feet: two seen from the front, one from
    /// the side, none from behind or lying down (asleep, downed, dead: closed). Returns how many.
    /// </summary>
    public static int EyePoints(in FigureLook look, in FigureState s, Span<Vector2> eyes)
    {
        if (s.Pose is Pose.Sleep or Pose.Downed or Pose.Dead || s.Facing == Facing.N || eyes.Length < 2) return 0;
        Vector2 h = HeadCenter(look, s);
        if (s.Facing == Facing.S)
        {
            eyes[0] = h + new Vector2(-1.9f, 0.9f);
            eyes[1] = h + new Vector2(1.9f, 0.9f);
            return 2;
        }
        eyes[0] = h + new Vector2((s.Facing == Facing.W ? -1f : 1f) * 2.8f, 0.8f);
        return 1;
    }

    /// <summary>How bright a fae's glimmer is: full among kept words, dimmed when courtesy runs low, all but gone when it fails.</summary>
    public static float GlimmerStrength(int nature) => nature switch { 0 => 1f, 1 => 0.45f, _ => 0.15f };

    /// <summary>
    /// Mote <paramref name="i"/> of a fae's glimmer: where it drifts about the head and shoulders now, in
    /// figure units from the feet, and how bright it is (0..1; the motes twinkle in turn).
    /// </summary>
    public static Vector2 GlimmerMote(int i, in FigureState s, float tall, out float bright)
    {
        float drop = s.Pose == Pose.Sit ? 9f : s.Pose == Pose.Crouch ? 5f : 0f;
        float a = s.Seed * 1.7f + i * 2.1f + s.Time * 0.33f;
        float r = 8.5f + 2.5f * MathF.Sin(s.Time * 0.47f + i * 1.3f);
        float twinkle = MathF.Max(0f, MathF.Sin(s.Time * 1.9f + i * 2.4f + s.Seed));
        bright = twinkle * twinkle * twinkle;
        return new Vector2(MathF.Cos(a) * r, -27f * tall + drop + MathF.Sin(a) * r * 0.6f - 2f * MathF.Sin(s.Time * 0.7f + i));
    }

    private static void Glimmer(Brush b, in FigureState s, float T, float strength)
    {
        if (strength <= 0.01f) return;
        for (int i = 0; i < GlimmerMotes; i++)
        {
            Vector2 p = GlimmerMote(i, s, T, out float bright);
            float a = bright * strength;
            if (a <= 0.02f) continue;
            b.Dot(p.X, p.Y, 0.55f, Mote.WithAlpha(0.9f * a));
            if (bright > 0.55f)
            {
                b.Line(p.X - 1.4f, p.Y, p.X + 1.4f, p.Y, Mote.WithAlpha(0.5f * a), 0.3f);
                b.Line(p.X, p.Y - 1.4f, p.X, p.Y + 1.4f, Mote.WithAlpha(0.5f * a), 0.3f);
            }
        }
    }

    /// <summary>
    /// A fae's wings: two pairs of thin, veined, translucent wings at the shoulder blades, a dragonfly's
    /// rather than a bird's, that stir slowly at rest and flutter as they walk. From the side only the
    /// far pair shows, behind the back.
    /// </summary>
    private static void Wings(Brush b, in FigureState s, bool side, float d, float W, float T, bool walking)
    {
        float flap = 1f - (walking ? 0.3f : 0.12f) * (0.5f + 0.5f * MathF.Sin(s.Time * (walking ? 13f : 2.2f) + s.Seed));
        // At night the wings catch the moonlight, so they still read under the dark.
        float moon = Math.Clamp(s.Night, 0f, 1f);
        Color fill = WingFill.WithAlpha(WingFill.A + 0.1f * moon), edge = WingEdge.WithAlpha(Math.Min(1f, WingEdge.A + 0.35f * moon));
        if (side)
        {
            Wing(b, new Vector2(-1.4f * d, -26.5f * T), -d, flap * 0.8f, T, fill, edge);
            return;
        }
        for (int k = -1; k <= 1; k += 2) Wing(b, new Vector2(k * 2.2f * W, -26.5f * T), k, flap, T, fill, edge);
    }

    private static void Wing(Brush b, Vector2 root, float k, float flap, float T, Color fill, Color edge)
    {
        b.Push();
        b.Translate(root.X, root.Y);
        b.Scale(k * flap, T);
        // The upper wing reaches up and out past the shoulder, rounded at the tip; the lower is smaller.
        b.Clear();
        Quad(b, new Vector2(0f, 0f), new Vector2(5f, -13f), new Vector2(9.5f, -10f), 6, true);
        Quad(b, new Vector2(9.5f, -10f), new Vector2(10.5f, -3f), new Vector2(0.5f, 1.5f), 6, false);
        b.Fill(fill).Stroke(edge, 0.45f);
        b.Line(0.4f, 0f, 8.2f, -9f, WingVein, 0.3f);
        b.Clear();
        Quad(b, new Vector2(0f, 2.5f), new Vector2(7.5f, 2.5f), new Vector2(7.2f, 9f), 5, true);
        Quad(b, new Vector2(7.2f, 9f), new Vector2(2.5f, 9.5f), new Vector2(0f, 3.5f), 5, false);
        b.Fill(fill).Stroke(edge, 0.45f);
        b.Line(0.4f, 3f, 6.2f, 8f, WingVein, 0.3f);
        b.Pop();
    }

    /// <summary>A fae's pointed ears: both seen from the front or behind (the head hides their roots), the near one from the side.</summary>
    private static void Ears(Brush b, Color skin, Facing f, bool side, float d, float hx, float hy)
    {
        if (side)
        {
            b.Clear().Add(hx - 1.6f * d, hy + 1.1f).Add(hx - 5f * d, hy - 3.9f).Add(hx - 0.1f * d, hy - 0.3f).Fill(skin).Stroke(Palette.Ink, 0.6f);
            b.Line(hx - 1.5f * d, hy + 0.4f, hx - 3.8f * d, hy - 2.8f, skin.Darkened(0.3f), 0.4f);
            return;
        }
        for (int k = -1; k <= 1; k += 2)
        {
            b.Clear().Add(hx + 4.6f * k, hy - 1.4f).Add(hx + 8.3f * k, hy - 4.5f).Add(hx + 4.9f * k, hy + 1.8f).Fill(skin).Stroke(Palette.Ink, 0.6f);
            if (f == Facing.S) b.Line(hx + 5.6f * k, hy - 0.4f, hx + 7.4f * k, hy - 3.4f, skin.Darkened(0.3f), 0.4f);
        }
    }

    /// <summary>
    /// A dryad's crown of leaves, the silhouette that reads at any zoom: a wreath that sits on the brow
    /// and over the crown of the head, its leaves splaying up and out and stirring with the trees' sway;
    /// green in spring and summer, gold and russet in autumn, bare twigs in winter. Cut off from the
    /// forest it wilts: browning, and thinning when it fails.
    /// </summary>
    private static void LeafCrown(Brush b, in FigureState s, bool side, float d, float hx, float hy)
    {
        float sway = MathF.Sin(s.Time * 0.9f + s.Seed * 1.3f);
        float cy = hy - 3.1f;
        const float rx = 5.4f, ry = 1.7f;
        // Twigs rise from the back of the wreath; in winter they are all that is left.
        for (int i = -1; i <= 1; i++)
        {
            float bx = hx + i * 2.3f + (side ? -0.8f * d : 0f), by = cy - ry * 0.8f;
            b.Line(bx, by, bx + i * 1.3f + sway * 0.25f, by - 4.2f + MathF.Abs(i) * 1.1f, Twig, 0.6f);
        }
        int n = s.Leaves == Leaves.Bare ? 2 : s.Nature >= 2 ? 6 : 11;
        float wilt = s.Nature * 0.35f;
        float turn = side ? 0.35f * d : 0f;
        // The back of the wreath first, then the leaves over the brow.
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < n; i++)
            {
                float th = (i + 0.5f) / n * MathF.Tau + turn;
                bool front = MathF.Sin(th) > 0f;
                if (front != (pass == 1)) continue;
                var root = new Vector2(hx + MathF.Cos(th) * rx, cy + MathF.Sin(th) * ry);
                float wobble = MathF.Sin(s.Time * 0.9f + s.Seed + i * 0.8f) * 0.08f + sway * 0.05f;
                var dir = new Vector2(MathF.Cos(th) * 0.85f, -0.8f + MathF.Sin(th) * 0.2f).Normalized().Rotated(wobble);
                Leaf(b, root, dir, 3.3f, 1.5f, LeafColour(s.Leaves, i, wilt));
            }
    }

    private static Color LeafColour(Leaves season, int i, float wilt)
    {
        Color c = season switch
        {
            Leaves.Turning => i % 2 == 0 ? LeafGold : LeafRusset,
            Leaves.Bare => LeafDead,
            _ => i % 2 == 0 ? LeafGreen : LeafGreen2,
        };
        return season == Leaves.Bare ? c : c.Lerp(LeafDead, Math.Clamp(wilt, 0f, 0.8f));
    }

    /// <summary>A pointed leaf from its root along <paramref name="dir"/>: two curves meeting at the tip.</summary>
    private static void Leaf(Brush b, Vector2 root, Vector2 dir, float len, float width, Color c)
    {
        Vector2 tip = root + dir * len, mid = root + dir * (len * 0.45f), across = new Vector2(-dir.Y, dir.X) * width;
        b.Clear();
        Quad(b, root, mid + across, tip, 4, true);
        Quad(b, tip, mid - across, root, 4, false);
        b.Fill(c).Stroke(Palette.Ink, 0.45f);
    }

    /// <summary>A dryad's hem of leaves in the season's colours, hanging over bare, bark-dark legs.</summary>
    private static void HemLeaves(Brush b, in FigureState s, bool side, float d, float W, float T)
    {
        int n = side ? 3 : 5;
        float half = side ? 5.2f : 8.6f * W;
        for (int i = 0; i < n; i++)
        {
            float t = (i + 0.5f) / n;
            float x = side ? (-4.9f + 10f * t) * d : -half + 2f * half * t;
            float a = MathF.PI / 2f + (t - 0.5f) * 0.7f + MathF.Sin(s.Time * 1.1f + s.Seed + i) * 0.08f;
            Leaf(b, new Vector2(x, -12.4f * T), new Vector2(MathF.Cos(a), MathF.Sin(a)), 3.2f, 1.25f, LeafColour(s.Leaves, i + 1, s.Nature * 0.35f).Darkened(0.18f));
        }
    }

    /// <summary>Rents in an undead's grave-worn clothes.</summary>
    private static void Tears(Brush b, Color cloth, bool side, float d, float W, float T)
    {
        Color rent = cloth.Darkened(0.6f);
        if (side)
        {
            b.Clear().Add(1.2f * d, -21f * T).Add(2.7f * d, -19.6f * T).Add(1.7f * d, -18.8f * T).Add(2.5f * d, -17.2f * T).Add(0.9f * d, -18.3f * T).Fill(rent);
            return;
        }
        b.Clear().Add(-4.3f * W, -22.6f * T).Add(-2.8f * W, -21.3f * T).Add(-3.7f * W, -20.4f * T).Add(-2.6f * W, -18.8f * T).Add(-4.5f * W, -20f * T).Fill(rent);
        b.Clear().Add(3.4f * W, -16.9f * T).Add(5f * W, -15.7f * T).Add(4.1f * W, -14.9f * T).Add(5.3f * W, -13.5f * T).Add(3.3f * W, -14.6f * T).Fill(rent);
    }

    /// <summary>
    /// The heat that rises off a djinn, a spirit of living flame: no smoke, only thin pale
    /// tongues of flame-light that waver up from the head and shoulders, fade as they rise, drift with the
    /// wind and trail behind a walker. Map units, drawn behind the figure (only what rises past the head
    /// and shoulders shows); brighter and quicker when restless, brightest when unbound.
    /// </summary>
    public static void Shimmer(Brush b, Vector2 at, float time, int salt, float strength, float drift)
    {
        if (strength <= 0.01f) return;
        // Seven small licks spread across the shoulders and head at staggered heights, so they scatter like
        // heat rising off the whole body and never pair up on the head (two points there would read as horns).
        for (int i = 0; i < 7; i++)
        {
            int slot = (i * 3) % 7; // where across the shoulders, shuffled against the phase
            float k = (time * (0.5f + 0.12f * strength) + i / 7f + salt * 0.17f) % 1f;
            float x = at.X + (slot - 3) * 1.9f + MathF.Sin(i * 4.1f + time * 2.3f + salt) * 1.2f + k * drift;
            float y = at.Y + 2f - k * 22f;
            float h = (4f + 1.8f * MathF.Sin(i * 1.7f + salt)) * (1f - 0.45f * k);
            float half = 1.3f * (1f - 0.35f * k);
            float wobble = MathF.Sin(time * 6.1f + i * 2.3f + salt) * 1.1f;
            float a = MathF.Min(1f, 0.62f * strength) * (1f - k) * MathF.Min(1f, k * 5f + 0.2f);
            if (a <= 0.02f) continue;
            // A pointed tongue of flame-light: pale gold at the heart, warmer at the edge, see-through.
            b.Clear();
            Quad(b, new Vector2(x - half, y), new Vector2(x - half * 1.1f, y - h * 0.55f), new Vector2(x + wobble, y - h), 5, true);
            Quad(b, new Vector2(x + wobble, y - h), new Vector2(x + half * 1.1f, y - h * 0.55f), new Vector2(x + half, y), 5, false);
            b.Fill(HeatEdge.WithAlpha(a * 0.8f));
            b.Clear();
            Quad(b, new Vector2(x - half * 0.45f, y - 0.4f), new Vector2(x - half * 0.5f, y - h * 0.45f), new Vector2(x + wobble * 0.7f, y - h * 0.75f), 4, true);
            Quad(b, new Vector2(x + wobble * 0.7f, y - h * 0.75f), new Vector2(x + half * 0.5f, y - h * 0.45f), new Vector2(x + half * 0.45f, y - 0.4f), 4, false);
            b.Fill(HeatCore.WithAlpha(a * 0.85f));
        }
    }

    private static readonly Color HeatCore = new(1f, 0.93f, 0.72f);
    private static readonly Color HeatEdge = new(1f, 0.66f, 0.3f);

    private static void BodyShape(Brush b, in FigureLook look, bool side, float d, float hunch, float W, float T)
    {
        b.Clear();
        if (side)
        {
            if (look.Vampire)
                b.Add((-4.5f * d + hunch), -29f * T).Add((4.5f * d + hunch), -29f * T).Add(7f * d, -10f * T).Add(3f * d, -12.5f * T).Add(-1f * d, -9.5f * T).Add(-6f * d, -11f * T);
            else if (look.Has(Kin.Undead))
                // Grave-worn: the hem hangs in rags.
                b.Add((-4f * d + hunch), -28.5f * T).Add((4.5f * d + hunch), -28.5f * T).Add(6.2f * d, -10.6f * T).Add(3.6f * d, -13f * T).Add(1.6f * d, -10.2f * T)
                    .Add(-1f * d, -12.9f * T).Add(-3.4f * d, -10.5f * T).Add(-5.6f * d, -12.6f * T);
            else
                b.Add((-4f * d + hunch), -28.5f * T).Add((4.5f * d + hunch), -28.5f * T).Add(6f * d, -11f * T).Add(-5.5f * d, -11f * T);
            return;
        }
        if (look.Ghoul)
            b.Add(-7f * W, -27.5f * T).Add(7f * W, -27.5f * T).Add(9.5f * W, -11f * T).Add(6f * W, -13.5f * T).Add(3.5f * W, -10f * T).Add(0f, -13f * T).Add(-3.5f * W, -10f * T).Add(-6f * W, -13.5f * T).Add(-9.5f * W, -11f * T);
        else if (look.Has(Kin.Undead))
            b.Add(-7f * W, -28.5f * T).Add(7f * W, -28.5f * T).Add(9.4f * W, -10.6f * T).Add(6.3f * W, -13.2f * T).Add(4f * W, -10.2f * T).Add(1.2f * W, -12.8f * T)
                .Add(-1.6f * W, -10.4f * T).Add(-4.3f * W, -13f * T).Add(-6.6f * W, -10.5f * T).Add(-8.2f * W, -12.6f * T).Add(-9.4f * W, -11.2f * T);
        else if (look.Vampire)
            b.Add(-7.5f * W, -29f * T).Add(7.5f * W, -29f * T).Add(10.5f * W, -10f * T).Add(5f * W, -12.5f * T).Add(0f, -9f * T).Add(-5f * W, -12.5f * T).Add(-10.5f * W, -10f * T);
        else
            b.Add(-7f * W, -28.5f * T).Add(7f * W, -28.5f * T).Add(9.2f * W, -11f * T).Add(-9.2f * W, -11f * T);
    }

    private static void Folds(Brush b, Color cloth, bool side, float d, float T)
    {
        Color fold = cloth.Darkened(0.32f);
        if (side)
        {
            b.Line(-1.5f * d, -24f * T, -2.5f * d, -12f * T, fold, 0.7f);
            return;
        }
        b.Line(-3f, -24f * T, -4f, -12f * T, fold, 0.7f);
        b.Line(3.5f, -22f * T, 5f, -12f * T, fold, 0.7f);
    }

    private static readonly Vector2[] StainSpots =
    {
        new(-2.6f, -22.5f), new(2.8f, -19f), new(-0.8f, -15.5f), new(3.6f, -25f), new(-4.2f, -17f), new(0.8f, -26f),
    };

    /// <summary>What a person's wounds soak into their clothes (blood unless their look says otherwise).</summary>
    private static Color StainOf(in FigureLook look) => look.Stain.A > 0f ? look.Stain : Palette.Stain;

    private static void Stains(Brush b, in FigureLook look, in FigureState s, bool side, float W, float T)
    {
        Color stain = StainOf(look);
        int n = s.Wounds switch { 1 => 1, 2 => 3, _ => 6 };
        float r = s.Wounds switch { 1 => 1.6f, 2 => 2.0f, _ => 2.5f };
        for (int i = 0; i < n; i++)
        {
            int k = (int)((uint)(s.Seed * 97f + i * 31f) % (uint)StainSpots.Length);
            Vector2 p = StainSpots[k];
            float x = side ? p.X * 0.55f : p.X * W;
            float rr = r * (0.8f + 0.4f * ((k * 37 + i * 11) % 7) / 6f);
            b.Clear().Ellipse(x, p.Y * T, rr * 1.1f, rr, 10).Fill(stain);
        }
        if (s.Wounds >= 3) b.Clear().Ellipse(side ? 0.5f : -0.5f, -19.5f * T, 3.6f, 4.6f, 12).Fill(stain.WithAlpha(stain.A * 0.83f));
    }

    private static void ChestMark(Brush b, in FigureLook look, float T)
    {
        if (look.Mark == Mark.None) return;
        b.Push();
        b.Translate(0f, -21f * T);
        b.Scale(0.55f, 0.55f);
        Color c = look.Mark switch
        {
            Mark.You => Palette.YouGold,
            Mark.Vampire => Palette.VampMark,
            _ => look.Cloth.Darkened(0.5f),
        };
        const float w = 1.7f;
        switch (look.Mark)
        {
            case Mark.You:
                b.Clear().Add(4f, 4f).Add(-3f, 4f).Add(-3f, -1f).StrokeOpen(c, w);
                b.Clear().Add(-6f, 2f).Add(-3f, -1f).Add(0f, 2f).StrokeOpen(c, w);
                break;
            case Mark.Cross:
                b.Line(-3.5f, 2f, 3.5f, 2f, c, w);
                b.Line(0f, -1.5f, 0f, 5.5f, c, w);
                break;
            case Mark.Forager:
                b.Clear();
                Quad(b, new Vector2(-4f, 4f), new Vector2(0f, -4f), new Vector2(4f, -1f), 5, true);
                Quad(b, new Vector2(4f, -1f), new Vector2(4f, 5f), new Vector2(-4f, 4f), 5, false);
                b.Stroke(c, w);
                b.Line(-3f, 3.5f, 2f, 0.5f, c, w * 0.8f);
                break;
            case Mark.Vampire:
                b.Clear().Add(0f, -3f).Add(3.5f, 1.5f).Add(0f, 6f).Add(-3.5f, 1.5f).Stroke(c, w);
                break;
        }
        b.Pop();
    }

    private static void Leg(Brush b, in FigureLook look, float x, float lift, float ang, float T, bool back)
    {
        b.Push();
        b.Translate(x, -13f * T);
        b.Rotate(ang);
        float len = 13f * T - lift;
        Color trousers = TrousersOf(look);
        if (back) trousers = trousers.Darkened(0.2f);
        b.Clear().RoundRect(-1.8f, 0f, 3.6f, len, 1.4f).Lit(trousers, 0.2f).Stroke(Palette.Ink, 0.8f);
        b.Clear().Ellipse(0.3f, len, 2.3f, 1.35f, 10).Fill(BootsOf(look)).Stroke(Palette.Ink, 0.7f);
        b.Pop();
    }

    /// <summary>What covers the legs: trousers, a ghoul's rags, a thief's black, or a dryad's bare bark.</summary>
    private static Color TrousersOf(in FigureLook look) =>
        look.Ghoul ? Palette.Hex(0x3a3d33) : look.Hostile ? Palette.Hex(0x1d1c1f) : look.Has(Kin.Dryad) ? look.Skin.Darkened(0.12f) : Palette.Trousers;

    /// <summary>Boots, or a dryad's bare feet, darker at the sole.</summary>
    private static Color BootsOf(in FigureLook look) => look.Has(Kin.Dryad) && !look.Hostile ? look.Skin.Darkened(0.4f) : Palette.Boots;

    private static void SeatedLegs(Brush b, in FigureLook look, bool side, float d, float W)
    {
        Color trousers = look.Ghoul ? Palette.Trousers : TrousersOf(look), boots = BootsOf(look);
        if (side)
        {
            b.Clear().Capsule(new Vector2(-1f * d, -3f), new Vector2(6f * d, -4.5f), 1.9f).Lit(trousers, 0.2f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Capsule(new Vector2(6f * d, -4.5f), new Vector2(8f * d, 0f), 1.7f).Lit(trousers, 0.2f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Ellipse(8.8f * d, 0.4f, 2.3f, 1.3f, 10).Fill(boots).Stroke(Palette.Ink, 0.7f);
            return;
        }
        for (int k = -1; k <= 1; k += 2)
        {
            b.Clear().Capsule(new Vector2(2.4f * k * W, -3f), new Vector2(3.2f * k * W, 1.2f), 1.9f).Lit(trousers, 0.2f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Ellipse(3.3f * k * W, 2f, 2.2f, 1.4f, 10).Fill(boots).Stroke(Palette.Ink, 0.7f);
        }
    }

    private static void Arm(Brush b, in FigureLook look, Color skin, float x, float ang, bool front, HeldTool tool, float d, float T)
    {
        b.Push();
        b.Translate(x, -27f * T);
        b.Rotate(ang);
        Color sleeve = look.Cloth.Darkened(front ? 0.1f : 0.25f);
        b.Clear().RoundRect(-1.7f, 0f, 3.4f, 11.5f * T, 1.6f).Lit(sleeve, 0.2f).Stroke(Palette.Ink, 0.8f);
        if (tool != HeldTool.None) Tool(b, tool, 12.2f * T, d);
        b.Clear().Ellipse(0f, 12.2f * T, 1.6f, 1.7f, 10).Fill(skin).Stroke(Palette.Ink, 0.7f);
        b.Pop();
    }

    /// <summary>A tool held in the hand at (0, handY), pointing on along the arm.</summary>
    private static void Tool(Brush b, HeldTool tool, float handY, float d)
    {
        switch (tool)
        {
            case HeldTool.Axe:
                b.Clear().RoundRect(-0.7f, handY - 2f, 1.4f, 11f, 0.6f).Fill(Palette.Handle).Stroke(Palette.Ink, 0.6f);
                b.Clear().Add(0.4f, handY + 6.5f).Add(4.6f * d, handY + 5.2f).Add(5.2f * d, handY + 9.8f).Add(0.4f, handY + 9.2f).Fill(Palette.Steel).Stroke(Palette.Ink, 0.6f);
                break;
            case HeldTool.Hammer:
                b.Clear().RoundRect(-0.6f, handY - 1.5f, 1.2f, 9f, 0.5f).Fill(Palette.Handle).Stroke(Palette.Ink, 0.6f);
                b.Clear().RoundRect(-2.6f, handY + 6.6f, 5.2f, 2.6f, 0.6f).Fill(Palette.Steel.Darkened(0.25f)).Stroke(Palette.Ink, 0.6f);
                break;
            case HeldTool.Club:
                b.Clear().Capsule(new Vector2(0f, handY - 1.5f), new Vector2(0.6f * d, handY + 10f), 1.2f).Fill(Palette.Handle.Lightened(0.08f)).Stroke(Palette.Ink, 0.6f);
                break;
            case HeldTool.Knife:
                b.Clear().Add(-0.5f, handY + 1f).Add(0.5f, handY + 1f).Add(0.2f, handY + 6.5f).Add(-0.2f, handY + 6.5f).Fill(Palette.Steel.Lightened(0.2f)).Stroke(Palette.Ink, 0.4f);
                break;
        }
    }

    private static void CarriedGoods(Brush b, Carried carry, bool side, float d, float T)
    {
        switch (carry)
        {
            case Carried.Timber:
                b.Push();
                b.Translate(side ? -1f * d : 0f, -30.5f * T);
                b.Rotate(side ? 0.12f * d : 0.15f);
                b.Clear().RoundRect(-11f, -2.8f, 22f, 5.6f, 2.4f).Lit(Palette.Log, 0.2f).Stroke(Palette.Ink, 0.8f);
                b.Clear().Ellipse(10.6f, 0f, 2f, 2.7f, 10).Fill(Palette.LogEnd).Stroke(Palette.Ink, 0.6f);
                b.Line(-8f, -0.6f, 7f, -0.6f, Palette.Log.Darkened(0.35f), 0.5f);
                b.Pop();
                break;
            case Carried.Berries:
            case Carried.Bundle:
                float bx = side ? 4.5f * d : 0f;
                b.Clear().Ellipse(bx, -18.5f * T, 5f, 3.6f, 12).Lit(Palette.Basket, 0.25f).Stroke(Palette.Ink, 0.8f);
                if (carry == Carried.Berries)
                    for (int i = 0; i < 5; i++)
                        b.Dot(bx - 3f + i * 1.5f, -20f * T + (i % 2) * 0.8f, 1.1f, Palette.Berry);
                break;
        }
    }

    private static void Head(Brush b, in FigureLook look, in FigureState s, Color skin, Facing f, bool side, float d, float hx, float hy)
    {
        if (look.Hostile && look.Hood)
        {
            // A hood over a masked face: only the eyes show (they catch the firelight at night).
            b.Clear().Ellipse(hx, hy, 5.7f, 5.9f, 16).Lit(look.Cloth.Darkened(0.1f), 0.25f).Stroke(Palette.Ink, Lw);
            if (f != Facing.N)
            {
                b.Clear().Ellipse(hx + (side ? 1.4f * d : 0f), hy + 1f, 3.3f, 3.1f, 12).Fill(Palette.Hex(0x0f0e11));
                Color glint = Palette.Hex(0xa89f88);
                if (f == Facing.S)
                {
                    b.Dot(hx - 1.2f, hy + 0.8f, 0.5f, glint);
                    b.Dot(hx + 1.2f, hy + 0.8f, 0.5f, glint);
                }
                else b.Dot(hx + 2.2f * d, hy + 0.8f, 0.5f, glint);
            }
            return;
        }

        // Long hair behind the head when seen from the back or side; a bun for tied hair.
        if (look.HairStyle == HairStyle.Long && f != Facing.S)
        {
            if (f == Facing.N) b.Clear().RoundRect(hx - 5.4f, hy - 2f, 10.8f, 10.5f, 4f).Fill(look.Hair).Stroke(Palette.Ink, 0.7f);
            else b.Clear().RoundRect(hx - 6.2f * d - (d < 0 ? 4.6f : 0f), hy - 3f, 5.2f, 9.5f, 2.5f).Fill(look.Hair).Stroke(Palette.Ink, 0.7f);
        }
        if (look.HairStyle == HairStyle.Tied && f != Facing.S)
        {
            float bx = f == Facing.N ? hx : hx - 5.4f * d;
            b.Clear().Ellipse(bx, hy + (f == Facing.N ? 0.5f : -1f), 2.4f, 2.2f, 10).Fill(look.Hair).Stroke(Palette.Ink, 0.7f);
        }

        // A fae's ears, seen from the front or behind, stand out from behind the head.
        bool fae = look.Has(Kin.Fae);
        if (fae && !side) Ears(b, skin, f, side, d, hx, hy);

        b.Clear().Ellipse(hx, hy, 5.2f, 5.4f, 16).Lit(skin, 0.22f).Stroke(Palette.Ink, Lw);

        Color hair = look.Hair;
        if (look.Ghoul)
        {
            // Ghouls: no hair, sunken eyes.
        }
        else if (f == Facing.N)
        {
            if (look.HairStyle == HairStyle.Bald)
                b.Clear().Ellipse(hx, hy + 2.6f, 5.0f, 2.6f, 12).Fill(hair.WithAlpha(0.9f));
            else
                b.Clear().Ellipse(hx, hy - 0.3f, 5.3f, 5.5f, 16).Lit(hair, 0.18f).Stroke(Palette.Ink, 0.8f);
        }
        else if (f == Facing.S)
        {
            if (look.HairStyle != HairStyle.Bald)
            {
                float lift = look.HairStyle == HairStyle.Cropped ? 1.4f : 0f;
                b.Clear();
                Quad(b, new Vector2(hx - 5.2f, hy + 0.5f - lift), new Vector2(hx - 5f, hy - 7f), new Vector2(hx + 1f, hy - 5.8f), 5, true);
                Quad(b, new Vector2(hx + 1f, hy - 5.8f), new Vector2(hx + 6f, hy - 4.8f), new Vector2(hx + 5.2f, hy + 0.8f - lift), 5, false);
                Quad(b, new Vector2(hx + 5.2f, hy + 0.8f - lift), new Vector2(hx + 3f, hy - 2f - lift), new Vector2(hx, hy - 2.4f - lift), 4, false);
                Quad(b, new Vector2(hx, hy - 2.4f - lift), new Vector2(hx - 3f, hy - 1.5f - lift), new Vector2(hx - 5.2f, hy + 0.5f - lift), 4, false);
                b.Lit(hair, 0.18f).Stroke(Palette.Ink, 0.75f);
            }
            if (look.Beard) Beard(b, hair, hx, hy, 0f);
            for (int k = -1; k <= 1; k += 2) Eye(b, look, skin, hx + 1.9f * k, hy + 0.9f, k * 1.4f);
            if (look.Has(Kin.Dryad))
            {
                // Bark: faint furrows down the brow and cheek.
                b.Line(hx - 2.7f, hy - 3.1f, hx - 3f, hy - 0.6f, skin.Darkened(0.32f), 0.35f);
                b.Line(hx + 2.4f, hy + 2f, hx + 2.8f, hy + 4.1f, skin.Darkened(0.32f), 0.35f);
            }
        }
        else
        {
            if (look.HairStyle != HairStyle.Bald)
            {
                b.Clear();
                Quad(b, new Vector2(hx - 5.2f * d, hy + 2.5f), new Vector2(hx - 5.6f * d, hy - 6f), new Vector2(hx + 1.5f * d, hy - 5.8f), 5, true);
                Quad(b, new Vector2(hx + 1.5f * d, hy - 5.8f), new Vector2(hx + 0.5f * d, hy - 2f), new Vector2(hx - 1.5f * d, hy + 3f), 4, false);
                b.Lit(hair, 0.18f).Stroke(Palette.Ink, 0.75f);
            }
            if (look.Beard) Beard(b, hair, hx, hy, d);
            if (fae) Ears(b, skin, f, side, d, hx, hy);
            Eye(b, look, skin, hx + 2.8f * d, hy + 0.8f, -d * 1.1f);
            b.Clear().Add(hx + 5f * d, hy + 0.8f).Add(hx + 5.9f * d, hy + 2f).Add(hx + 5f * d, hy + 2.6f).StrokeOpen(Palette.Ink, 0.7f);
        }

        if (look.Has(Kin.Dryad) && !look.Ghoul) LeafCrown(b, s, side, d, hx, hy);

        if (look.Mark == Mark.You && f != Facing.N)
        {
            b.Clear();
            Quad(b, new Vector2(hx - 4.6f, hy - 2.6f), new Vector2(hx, hy - 4.6f), new Vector2(hx + 4.6f, hy - 2.6f), 6, true);
            b.StrokeOpen(Palette.YouGold, 1.1f);
        }
    }

    /// <summary>
    /// One eye at (x, y): an ordinary dark eye; a djinn's ember, lit from within; or the undead's sunken
    /// eye, a dark hollow with a faint cold gleam above a fallen cheek (<paramref name="cheek"/> is where
    /// the hollow under the cheekbone runs: outward from the front, back toward the ear from the side).
    /// </summary>
    private static void Eye(Brush b, in FigureLook look, Color skin, float x, float y, float cheek)
    {
        if (look.Has(Kin.Undead))
        {
            // A small dark eye deep in a shadowed hollow under the brow, a faint cold gleam in it.
            Color hollow = skin.Darkened(0.36f);
            b.Clear().Ellipse(x, y - 0.2f, 1.3f, 0.95f, 10).Fill(hollow);
            b.Clear().Ellipse(x, y + 0.1f, 0.5f, 0.58f, 8).Fill(Palette.Eye);
            b.Dot(x - 0.12f, y - 0.02f, 0.2f, ColdGleam);
            b.Line(x + cheek, y + 1.5f, x + cheek * 0.45f, y + 3.1f, hollow.WithAlpha(0.85f), 0.45f);
            return;
        }
        if (look.Has(Kin.Djinn))
        {
            b.Dot(x, y, 1.05f, EmberHalo);
            b.Clear().Ellipse(x, y, 0.68f, 0.62f, 8).Fill(EmberEye);
            return;
        }
        b.Clear().Ellipse(x, y, 0.6f, 0.75f, 8).Fill(Palette.Eye);
    }

    private static void Beard(Brush b, Color hair, float hx, float hy, float d)
    {
        b.Clear();
        if (d == 0f)
            b.Add(hx - 4.7f, hy + 0.6f).Add(hx - 3.6f, hy + 4.2f).Add(hx, hy + 5.8f).Add(hx + 3.6f, hy + 4.2f).Add(hx + 4.7f, hy + 0.6f).Add(hx + 2.8f, hy + 2.7f).Add(hx, hy + 3.3f).Add(hx - 2.8f, hy + 2.7f);
        else
            b.Add(hx - 1.5f * d, hy + 1f).Add(hx + 4.6f * d, hy + 2.4f).Add(hx + 3.2f * d, hy + 5.2f).Add(hx - 0.5f * d, hy + 4.6f);
        b.Fill(hair.Darkened(0.05f)).Stroke(Palette.Ink, 0.6f);
    }

    /// <summary>
    /// Asleep, downed or dead, seen from above at an angle: a long low shape. Sleepers lie still
    /// under a blanket when on a bed; the downed curl around their wound and breathe hard; the dead
    /// lie splayed, their colour gone grey (charred black by the sun, thin from hunger).
    /// </summary>
    private static void Lying(Brush b, in FigureLook look, in FigureState s, ArtTextures tex, float shadowAlpha)
    {
        bool dead = s.Pose == Pose.Dead, downed = s.Pose == Pose.Downed;
        float dir = s.HeadLeft ? -1f : 1f;
        float lift = s.Carried ? -25f : 0f;
        if (!s.Carried && !s.OnBed) Shadow(b, tex, 0f, 0.5f, 15f, 3.8f, shadowAlpha);

        Color cloth = look.Cloth, skin = look.Skin, hair = look.Hair;
        float thin = 1f;
        if (dead)
        {
            switch (s.Death)
            {
                case DeathKind.Sun:
                    cloth = cloth.Lerp(Palette.Hex(0x1b1510), 0.75f);
                    skin = Palette.Hex(0x261b14);
                    hair = Palette.Hex(0x110d0b);
                    break;
                case DeathKind.Wasted:
                    thin = 0.82f;
                    cloth = Desaturate(cloth, 0.45f).Darkened(0.15f);
                    skin = skin.Lerp(Palette.Hex(0x8f9184), 0.5f);
                    break;
                default:
                    cloth = Desaturate(cloth, 0.35f).Darkened(0.12f);
                    skin = skin.Lerp(Palette.Hex(0x8a8c80), 0.45f);
                    break;
            }
            // The undead at rest at last: grave-grey, crumbling to the dust they lie in.
            if (look.Has(Kin.Undead)) skin = skin.Lerp(Palette.GraveGrey.Lightened(0.15f), 0.5f);
        }

        b.Push();
        b.Translate(0f, -2.5f + lift);
        if (s.Pose == Pose.Sleep && s.OnBed)
        {
            // In a bed: lying along it, head on the pillow at the north end, the length foreshortened.
            b.Translate(0f, -9f);
            b.Rotate(-MathF.PI / 2f);
            b.Scale(0.8f, 1f);
        }
        else b.Scale(dir, 0.82f);
        float heave = downed ? MathF.Sin(s.Time * 4.2f + s.Seed) * 0.35f : s.Pose == Pose.Sleep ? MathF.Sin(s.Time * 1.3f + s.Seed) * 0.25f : 0f;

        // Legs.
        Color trousers = look.Hostile ? Palette.Hex(0x1d1c1f) : TrousersOf(look);
        Color boots = BootsOf(look);
        if (dead && s.Death == DeathKind.Sun) trousers = Palette.Hex(0x15110e);
        if (downed)
        {
            LimbPair(b, trousers, boots, new Vector2(-5f, -1.2f), new Vector2(-10f, -5.5f), new Vector2(-15.5f, -2.5f), 1.8f * thin);
            LimbPair(b, trousers, boots, new Vector2(-5f, 1.4f), new Vector2(-10.5f, -2.5f), new Vector2(-15f, 1.5f), 1.8f * thin);
        }
        else if (dead)
        {
            b.Clear().Capsule(new Vector2(-5f, -1.4f), new Vector2(-16f, -4.5f), 1.8f * thin).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Capsule(new Vector2(-5f, 1.4f), new Vector2(-15.5f, 4.2f), 1.8f * thin).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Ellipse(-16.4f, -4.7f, 1.4f, 2.1f, 10).Fill(boots).Stroke(Palette.Ink, 0.6f);
            b.Clear().Ellipse(-15.9f, 4.4f, 1.4f, 2.1f, 10).Fill(boots).Stroke(Palette.Ink, 0.6f);
        }
        else
        {
            b.Clear().Capsule(new Vector2(-5f, -1.2f), new Vector2(-15.5f, -1.4f), 1.8f).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Capsule(new Vector2(-5f, 1.2f), new Vector2(-15.5f, 1.4f), 1.8f).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
        }

        // Torso.
        b.Clear().RoundRect(-6f, -4f * thin - heave, 13f, 8f * thin + heave * 2f, 3f).Lit(cloth, 0.22f).Stroke(Palette.Ink, Lw);
        if (s.Wounds > 0 || (dead && s.Death == DeathKind.Wounds))
        {
            int n = dead ? 4 : s.Wounds + 1;
            Color stain = StainOf(look);
            for (int i = 0; i < n; i++)
                b.Clear().Ellipse(-3.5f + i * 2.4f, -1.5f + (i % 2) * 2.6f, 2.2f, 1.7f, 10).Fill(stain);
        }

        // Arms.
        Color sleeve = cloth.Darkened(0.12f);
        if (dead)
        {
            Limb(b, sleeve, skin, new Vector2(5f, -3.2f), new Vector2(10.5f, -9.5f), 1.6f * thin);
            Limb(b, sleeve, skin, new Vector2(4.5f, 3.4f), new Vector2(2.5f, 10f), 1.6f * thin);
        }
        else if (downed)
        {
            Limb(b, sleeve, skin, new Vector2(5f, -3f), new Vector2(13.5f, -6f), 1.6f);
            Limb(b, sleeve, skin, new Vector2(4.5f, 3f), new Vector2(0f, 0.5f), 1.6f);
        }
        else if (!s.OnBed)
        {
            Limb(b, sleeve, skin, new Vector2(5f, -3.2f), new Vector2(-1.5f, -4.4f), 1.6f);
        }

        // A blanket over a sleeper in a bed.
        if (s.Pose == Pose.Sleep && s.OnBed)
            b.Clear().RoundRect(-16.5f, -4.8f - heave, 21f, 9.6f + heave * 2f, 3f).Lit(Palette.Cloth, 0.2f).Stroke(Palette.Ink, 0.8f);

        // Head and collar.
        if (look.Vampire) b.Clear().Add(6f, -4.5f).Add(8f, -9f).Add(9f, -3.5f).Fill(Palette.Collar).Stroke(Palette.Ink, 0.8f);
        float tilt = dead ? 1.2f : 0f;
        if (look.Hostile && look.Hood)
        {
            b.Clear().Ellipse(10.5f, tilt, 4.8f, 4.4f, 14).Lit(look.Cloth, 0.2f).Stroke(Palette.Ink, Lw);
        }
        else
        {
            b.Clear().Ellipse(10.5f, tilt, 4.6f, 4.2f, 14).Lit(skin, 0.2f).Stroke(Palette.Ink, Lw);
            if (look.HairStyle != HairStyle.Bald)
            {
                b.Clear();
                Quad(b, new Vector2(7.5f, -3.5f + tilt), new Vector2(10.5f, -6.5f + tilt), new Vector2(15f, -2.5f + tilt), 4, true);
                Quad(b, new Vector2(15f, -2.5f + tilt), new Vector2(15.5f, 1.5f + tilt), new Vector2(13.5f, 3.2f + tilt), 3, false);
                Quad(b, new Vector2(13.5f, 3.2f + tilt), new Vector2(12.5f, -1.5f + tilt), new Vector2(9f, -2f + tilt), 3, false);
                b.Fill(hair).Stroke(Palette.Ink, 0.7f);
            }
            if (look.Beard) b.Clear().Ellipse(7.2f, 1.2f + tilt, 1.8f, 2.4f, 10).Fill(hair);
            // Closed eyes (sleep, death) or a pained squint (downed).
            b.Line(9.3f, 0.4f + tilt, 11.4f, 0.1f + tilt, Palette.Ink, 0.7f);
            if (look.Has(Kin.Fae))
                b.Clear().Add(9.6f, -3.6f + tilt).Add(8.6f, -7.6f + tilt).Add(11.4f, -3.9f + tilt).Fill(skin).Stroke(Palette.Ink, 0.55f);
            if (look.Has(Kin.Dryad))
            {
                // A dryad's crown, even asleep: leaves about the crown of the head (dropped and browned in death).
                int leaves = s.Leaves == Leaves.Bare ? 1 : dead ? 3 : 5;
                for (int i = 0; i < leaves; i++)
                {
                    float a = -1.25f + i * (2.5f / Math.Max(1, leaves - 1));
                    var away = new Vector2(MathF.Cos(a), MathF.Sin(a));
                    Leaf(b, new Vector2(10.5f + away.X * 4.3f, tilt + away.Y * 3.9f), away, 3f, 1.2f, LeafColour(s.Leaves, i, dead ? 0.7f : s.Nature * 0.35f));
                }
            }
        }

        if (dead && s.Death == DeathKind.Sun && s.DeadHours < 8f)
        {
            // Embers still glowing in the char.
            for (int i = 0; i < 4; i++)
            {
                float flick = 0.5f + 0.5f * MathF.Sin(s.Time * (5f + i) + i * 1.7f);
                b.Dot(-8f + i * 5f, (i % 2 == 0 ? -1.5f : 1.8f), 0.8f, Palette.Ember.WithAlpha(0.35f + 0.5f * flick));
            }
        }
        b.Pop();
    }

    private static void Limb(Brush b, Color sleeve, Color skin, Vector2 from, Vector2 to, float r)
    {
        b.Clear().Capsule(from, to, r).Lit(sleeve, 0.15f).Stroke(Palette.Ink, 0.75f);
        b.Clear().Ellipse(to.X, to.Y, 1.6f, 1.6f, 10).Fill(skin).Stroke(Palette.Ink, 0.6f);
    }

    private static void LimbPair(Brush b, Color trousers, Color boots, Vector2 hip, Vector2 knee, Vector2 foot, float r)
    {
        b.Clear().Capsule(hip, knee, r).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
        b.Clear().Capsule(knee, foot, r * 0.92f).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
        b.Clear().Ellipse(foot.X - 0.6f, foot.Y, 1.4f, 2.1f, 10).Fill(boots).Stroke(Palette.Ink, 0.6f);
    }

    /// <summary>A soft shadow on the ground (texture, or a flat ellipse without it).</summary>
    /// <summary>
    /// Just the person's ground shadow, for the shadow pass that runs before anything stands up
    /// (Person itself then gets a shadow alpha of 0), so shadows never break the batch of figures.
    /// </summary>
    public static void PersonShadow(Brush b, in FigureLook look, in FigureState s, ArtTextures tex, Vector2 shadow, float alpha)
    {
        if (s.Pose is Pose.Sleep or Pose.Downed or Pose.Dead)
        {
            if (!s.Carried && !s.OnBed) Shadow(b, tex, 0f, 0.5f, 15f, 3.8f, alpha);
            return;
        }
        Shadow(b, tex, shadow.X, shadow.Y, 9.5f * look.Wide + MathF.Abs(shadow.X) * 0.5f, 3.4f, alpha);
    }

    public static void Shadow(Brush b, ArtTextures tex, float x, float y, float rx, float ry, float alpha)
    {
        if (alpha <= 0.01f) return;
        if (tex.Shadow != null) b.Texture(tex.Shadow, x, y, rx * 2.9f, ry * 2.9f, 0f, new Color(0f, 0f, 0f, alpha));
        else b.Clear().Ellipse(x, y, rx, ry, 16).Fill(new Color(0f, 0f, 0f, alpha * 0.8f));
    }

    /// <summary>Appends a quadratic curve to the brush's shape.</summary>
    public static void Quad(Brush b, Vector2 from, Vector2 ctrl, Vector2 to, int segments, bool includeStart)
    {
        for (int i = includeStart ? 0 : 1; i <= segments; i++)
        {
            float t = i / (float)segments, u = 1f - t;
            b.Add(u * u * from + 2f * u * t * ctrl + t * t * to);
        }
    }

    public static Color Desaturate(Color c, float amount)
    {
        float grey = c.R * 0.3f + c.G * 0.59f + c.B * 0.11f;
        return new Color(Mathf.Lerp(c.R, grey, amount), Mathf.Lerp(c.G, grey, amount), Mathf.Lerp(c.B, grey, amount), c.A);
    }

    // ---- monsters for later (kept from the mockup in the same language) ----

    /// <summary>A wolf in side view (wolfB): anchored at the feet, facing east or west.</summary>
    public static void Wolf(Brush b, Facing f, float phase, ArtTextures tex, float shadowAlpha)
    {
        float sw = MathF.Sin(phase * MathF.Tau), d = f == Facing.W ? -1f : 1f;
        Shadow(b, tex, 0f, 0f, 13f, 3f, shadowAlpha);
        Color body = Palette.Hex(0x57524a), dark = Palette.Hex(0x3d3933), head = Palette.Hex(0x5d574e);
        b.Push();
        b.Scale(d, 1f);
        void WolfLeg(float x, float s2, bool back)
        {
            b.Push();
            b.Translate(x, -8f);
            b.Rotate(sw * 0.5f * s2);
            b.Clear().RoundRect(-1.3f, 0f, 2.6f, 8.5f, 1f).Fill(back ? dark.Darkened(0.15f) : dark).Stroke(Palette.Ink, 0.7f);
            b.Pop();
        }
        WolfLeg(6f, -1f, true);
        WolfLeg(-7f, 1f, true);
        b.Clear().Add(-10f, -11f).Add(-14f, -12f + sw * 2f).Add(-18f, -9f + sw * 2f).StrokeOpen(Palette.Ink, 3.4f);
        b.Clear().Add(-10f, -11f).Add(-14f, -12f + sw * 2f).Add(-18f, -9f + sw * 2f).StrokeOpen(Palette.Hex(0x4f4a42), 2.3f);
        b.Clear().Ellipse(0f, -11f, 11f, 4.8f, 16).Lit(body, 0.2f).Stroke(Palette.Ink, 0.9f);
        WolfLeg(7.5f, 1f, false);
        WolfLeg(-5.5f, -1f, false);
        b.Clear().Add(8f, -15f).Add(13f, -16.5f).Add(18f, -13.5f).Add(18.5f, -12f).Add(13f, -11f).Add(9f, -9f).Lit(head, 0.2f).Stroke(Palette.Ink, 0.9f);
        b.Clear().Add(11f, -16f).Add(12f, -20.5f).Add(13.8f, -16.2f).Fill(Palette.Hex(0x4a453e)).Stroke(Palette.Ink, 0.7f);
        b.Dot(14.5f, -14.2f, 0.6f, Palette.Hex(0x1a1714));
        b.Pop();
    }

    /// <summary>The Barrow King (kingB): a person-shaped horror at twice the size, crowned, eyes aglow.</summary>
    public static void King(Brush b, in FigureState s, ArtTextures tex, Vector2 shadow, float shadowAlpha)
    {
        b.Push();
        b.Scale(2f, 2f);
        var look = new FigureLook
        {
            Cloth = Palette.Hex(0x2c2a2f), Hair = Palette.Hex(0x2c2a2f), Skin = Palette.Hex(0xb4b39f),
            Vampire = true, Mark = Mark.None, Wide = 1f, Tall = 1f, HairStyle = HairStyle.Bald,
        };
        Person(b, look, s, tex, shadow, shadowAlpha);
        bool side = s.Facing is Facing.E or Facing.W;
        float d = s.Facing == Facing.W ? -1f : 1f, hx = side ? d : 0f, hy = -33.5f;
        Color crown = Palette.Hex(0x8a8170);
        for (int k = -1; k <= 1; k += 2)
        {
            b.Clear().Add(hx + 2.5f * k, hy - 4.5f).Add(hx + 5f * k, hy - 10f).Add(hx + 8f * k, hy - 11f).StrokeOpen(crown, 1f);
            b.Line(hx + 4f * k, hy - 8f, hx + 3f * k, hy - 12f, crown, 1f);
        }
        if (s.Facing != Facing.N)
        {
            var glow = new Color(0.78f, 0.84f, 0.63f, 0.75f + MathF.Sin(s.Time * 3f) * 0.25f);
            if (s.Facing == Facing.S)
            {
                b.Clear().Ellipse(-1.9f, hy + 0.9f, 0.9f, 0.7f, 8).Fill(glow);
                b.Clear().Ellipse(1.9f, hy + 0.9f, 0.9f, 0.7f, 8).Fill(glow);
            }
            else b.Clear().Ellipse(hx + 2.8f * d, hy + 0.8f, 0.9f, 0.7f, 8).Fill(glow);
        }
        b.Pop();
    }
}
