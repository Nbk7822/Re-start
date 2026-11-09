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
}

/// <summary>
/// People in the grounded three-quarter view (ADR-0009 option B), ported from figureB in
/// docs/art/art-direction-2026-11-09.html: anchored at the feet, about 39 units tall with a head
/// to body of about 1:3.8, legs and arms that swing with the walk cycle, four facings, thin warm
/// ink outlines, and a soft two-tone shading with light from the upper left. Added for the
/// darker, more realistic pass: builds and hair styles, belts and hem grime, tools in hand, goods
/// carried, blood soaking the clothes of the badly wounded, skin reddening in the sun, and the
/// lying poses (asleep, downed, dead: splayed, greyed, charred by the sun or wasted by hunger).
/// Units are figure units; the caller sets the transform (feet position and scale).
/// </summary>
public static class Figures
{
    /// <summary>Height of a standing figure in figure units (feet to crown).</summary>
    public const float Height = 39f;

    private const float Lw = 0.95f;

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
        float sw = walking ? MathF.Sin(s.Phase * MathF.Tau) : 0f;
        float breathe = MathF.Sin(s.Time * 2.1f + s.Seed) * 0.35f;
        bool sit = s.Pose == Pose.Sit, crouch = s.Pose == Pose.Crouch;
        float drop = sit ? 9f : crouch ? 5f : 0f;
        float W = look.Wide, T = look.Tall;
        Color cloth = look.Cloth;
        Color skin = s.Burning && look.Vampire ? look.Skin.Lerp(new Color(0.78f, 0.32f, 0.22f), 0.45f) : look.Skin;
        float hunch = look.Ghoul ? 3f * d : 0f;

        // Soft contact shadow, stretched away from the sun.
        Shadow(b, tex, shadow.X, shadow.Y, 9.5f * W + MathF.Abs(shadow.X) * 0.5f, 3.4f, shadowAlpha);

        float bob = walking ? -MathF.Abs(MathF.Cos(s.Phase * MathF.Tau)) * 1.2f : 0f;
        b.Push();
        b.Translate(0f, bob + drop);

        // Legs.
        if (!sit)
        {
            if (side)
            {
                Leg(b, look, 0f, 0f, -sw * 0.5f * d, T, true);
                Leg(b, look, 0f, 0f, sw * 0.5f * d, T, false);
            }
            else
            {
                float lift = crouch ? 4f : 0f;
                Leg(b, look, -2.6f * W, walking ? MathF.Max(0f, sw) * 2.5f : lift, 0f, T, false);
                Leg(b, look, 2.6f * W, walking ? MathF.Max(0f, -sw) * 2.5f : lift, 0f, T, false);
            }
        }
        else SeatedLegs(b, look, side, d, W);

        // Long hair falls behind the shoulders, seen from the front.
        if (f == Facing.S && look.HairStyle == HairStyle.Long && !look.Hood)
            b.Clear().Ellipse(0f, -31.5f * T, 6.4f, 6.8f, 12).Fill(look.Hair).Stroke(Palette.Ink, 0.7f);

        // Cloak or torso.
        BodyShape(b, look, side, d, hunch, W, T);
        b.Lit(cloth, 0.26f).Stroke(Palette.Ink, Lw);
        if (!look.Vampire && !look.Ghoul)
        {
            // Hem grime and a belt.
            float hw = side ? 5.6f : 9.0f * W;
            b.Clear().Add(-hw * 0.97f, -13.6f * T).Add(hw * 0.97f, -13.6f * T).Add(hw, -11f * T).Add(-hw, -11f * T);
            if (side) b.Offset(d * 0.2f, 0f);
            b.Fill(cloth.Darkened(0.45f).WithAlpha(0.55f));
            float bw = side ? 4.9f : 7.7f * W;
            b.Line(-bw, -18.2f * T, bw, -18.2f * T, Palette.Hex(0x261d16), 1.3f);
        }
        Folds(b, cloth, side, d, T);
        if (s.Wounds > 0) Stains(b, s, side, W, T);

        if (f == Facing.S) ChestMark(b, look, T);

        // Carried goods are held in front of the chest (or on a shoulder for timber).
        bool armsBusy = s.Carry != Carried.None;
        float swing = walking ? sw * 0.5f
            : s.Pose == Pose.Swing ? MathF.Sin(s.Time * 7f + s.Seed) * 0.9f - 0.6f
            : crouch ? 0.6f + MathF.Sin(s.Time * 3f + s.Seed) * 0.3f
            : sit ? 0.5f
            : 0f;
        if (armsBusy) swing *= 0.25f;

        if (side)
        {
            float ang = -d * swing;
            if (s.Pose == Pose.Talk) ang = -d * (0.7f + MathF.Sin(s.Time * 2.4f + s.Seed) * 0.35f);
            if (armsBusy) ang = -d * 0.9f;
            Arm(b, look, skin, hunch * 0.7f, ang, true, s.Tool, d, T);
        }
        else
        {
            bool front = f == Facing.S;
            float left = 0.1f + (front ? swing * 0.3f : -swing * 0.3f);
            float right = -0.1f - (s.Pose == Pose.Swing ? MathF.Abs(swing) * 1.6f : front ? swing * 0.3f : -swing * 0.3f);
            if (s.Pose == Pose.Talk) right = -0.35f - (0.45f + MathF.Sin(s.Time * 2.4f + s.Seed) * 0.3f);
            if (armsBusy) { left = -0.55f; right = 0.55f; }
            Arm(b, look, skin, -7.6f * W, left, front, HeldTool.None, 1f, T);
            Arm(b, look, skin, 7.6f * W, right, front, s.Tool, 1f, T);
        }

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
    }

    private static void BodyShape(Brush b, in FigureLook look, bool side, float d, float hunch, float W, float T)
    {
        b.Clear();
        if (side)
        {
            if (look.Vampire)
                b.Add((-4.5f * d + hunch), -29f * T).Add((4.5f * d + hunch), -29f * T).Add(7f * d, -10f * T).Add(3f * d, -12.5f * T).Add(-1f * d, -9.5f * T).Add(-6f * d, -11f * T);
            else
                b.Add((-4f * d + hunch), -28.5f * T).Add((4.5f * d + hunch), -28.5f * T).Add(6f * d, -11f * T).Add(-5.5f * d, -11f * T);
            return;
        }
        if (look.Ghoul)
            b.Add(-7f * W, -27.5f * T).Add(7f * W, -27.5f * T).Add(9.5f * W, -11f * T).Add(6f * W, -13.5f * T).Add(3.5f * W, -10f * T).Add(0f, -13f * T).Add(-3.5f * W, -10f * T).Add(-6f * W, -13.5f * T).Add(-9.5f * W, -11f * T);
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

    private static void Stains(Brush b, in FigureState s, bool side, float W, float T)
    {
        int n = s.Wounds switch { 1 => 1, 2 => 3, _ => 6 };
        float r = s.Wounds switch { 1 => 1.6f, 2 => 2.0f, _ => 2.5f };
        for (int i = 0; i < n; i++)
        {
            int k = (int)((uint)(s.Seed * 97f + i * 31f) % (uint)StainSpots.Length);
            Vector2 p = StainSpots[k];
            float x = side ? p.X * 0.55f : p.X * W;
            float rr = r * (0.8f + 0.4f * ((k * 37 + i * 11) % 7) / 6f);
            b.Clear().Ellipse(x, p.Y * T, rr * 1.1f, rr, 10).Fill(Palette.Stain);
        }
        if (s.Wounds >= 3) b.Clear().Ellipse(side ? 0.5f : -0.5f, -19.5f * T, 3.6f, 4.6f, 12).Fill(Palette.Stain.WithAlpha(0.75f));
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
        Color trousers = look.Ghoul ? Palette.Hex(0x3a3d33) : look.Hostile ? Palette.Hex(0x1d1c1f) : Palette.Trousers;
        if (back) trousers = trousers.Darkened(0.2f);
        b.Clear().RoundRect(-1.8f, 0f, 3.6f, len, 1.4f).Lit(trousers, 0.2f).Stroke(Palette.Ink, 0.8f);
        b.Clear().Ellipse(0.3f, len, 2.3f, 1.35f, 10).Fill(Palette.Boots).Stroke(Palette.Ink, 0.7f);
        b.Pop();
    }

    private static void SeatedLegs(Brush b, in FigureLook look, bool side, float d, float W)
    {
        Color trousers = look.Hostile ? Palette.Hex(0x1d1c1f) : Palette.Trousers;
        if (side)
        {
            b.Clear().Capsule(new Vector2(-1f * d, -3f), new Vector2(6f * d, -4.5f), 1.9f).Lit(trousers, 0.2f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Capsule(new Vector2(6f * d, -4.5f), new Vector2(8f * d, 0f), 1.7f).Lit(trousers, 0.2f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Ellipse(8.8f * d, 0.4f, 2.3f, 1.3f, 10).Fill(Palette.Boots).Stroke(Palette.Ink, 0.7f);
            return;
        }
        for (int k = -1; k <= 1; k += 2)
        {
            b.Clear().Capsule(new Vector2(2.4f * k * W, -3f), new Vector2(3.2f * k * W, 1.2f), 1.9f).Lit(trousers, 0.2f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Ellipse(3.3f * k * W, 2f, 2.2f, 1.4f, 10).Fill(Palette.Boots).Stroke(Palette.Ink, 0.7f);
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
            Color eye = Palette.Eye;
            b.Clear().Ellipse(hx - 1.9f, hy + 0.9f, 0.6f, 0.75f, 8).Fill(eye);
            b.Clear().Ellipse(hx + 1.9f, hy + 0.9f, 0.6f, 0.75f, 8).Fill(eye);
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
            b.Clear().Ellipse(hx + 2.8f * d, hy + 0.8f, 0.6f, 0.75f, 8).Fill(Palette.Eye);
            b.Clear().Add(hx + 5f * d, hy + 0.8f).Add(hx + 5.9f * d, hy + 2f).Add(hx + 5f * d, hy + 2.6f).StrokeOpen(Palette.Ink, 0.7f);
        }

        if (look.Mark == Mark.You && f != Facing.N)
        {
            b.Clear();
            Quad(b, new Vector2(hx - 4.6f, hy - 2.6f), new Vector2(hx, hy - 4.6f), new Vector2(hx + 4.6f, hy - 2.6f), 6, true);
            b.StrokeOpen(Palette.YouGold, 1.1f);
        }
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
        if (!s.Carried) Shadow(b, tex, 0f, 0.5f, 15f, 3.8f, shadowAlpha);

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
        }

        b.Push();
        b.Translate(0f, -2.5f + lift);
        b.Scale(dir, 0.82f);
        float heave = downed ? MathF.Sin(s.Time * 4.2f + s.Seed) * 0.35f : s.Pose == Pose.Sleep ? MathF.Sin(s.Time * 1.3f + s.Seed) * 0.25f : 0f;

        // Legs.
        Color trousers = look.Hostile ? Palette.Hex(0x1d1c1f) : Palette.Trousers;
        if (dead && s.Death == DeathKind.Sun) trousers = Palette.Hex(0x15110e);
        if (downed)
        {
            LimbPair(b, trousers, new Vector2(-5f, -1.2f), new Vector2(-10f, -5.5f), new Vector2(-15.5f, -2.5f), 1.8f * thin);
            LimbPair(b, trousers, new Vector2(-5f, 1.4f), new Vector2(-10.5f, -2.5f), new Vector2(-15f, 1.5f), 1.8f * thin);
        }
        else if (dead)
        {
            b.Clear().Capsule(new Vector2(-5f, -1.4f), new Vector2(-16f, -4.5f), 1.8f * thin).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Capsule(new Vector2(-5f, 1.4f), new Vector2(-15.5f, 4.2f), 1.8f * thin).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
            b.Clear().Ellipse(-16.4f, -4.7f, 1.4f, 2.1f, 10).Fill(Palette.Boots).Stroke(Palette.Ink, 0.6f);
            b.Clear().Ellipse(-15.9f, 4.4f, 1.4f, 2.1f, 10).Fill(Palette.Boots).Stroke(Palette.Ink, 0.6f);
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
            for (int i = 0; i < n; i++)
                b.Clear().Ellipse(-3.5f + i * 2.4f, -1.5f + (i % 2) * 2.6f, 2.2f, 1.7f, 10).Fill(Palette.Stain);
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

    private static void LimbPair(Brush b, Color trousers, Vector2 hip, Vector2 knee, Vector2 foot, float r)
    {
        b.Clear().Capsule(hip, knee, r).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
        b.Clear().Capsule(knee, foot, r * 0.92f).Lit(trousers, 0.15f).Stroke(Palette.Ink, 0.8f);
        b.Clear().Ellipse(foot.X - 0.6f, foot.Y, 1.4f, 2.1f, 10).Fill(Palette.Boots).Stroke(Palette.Ink, 0.6f);
    }

    /// <summary>A soft shadow on the ground (texture, or a flat ellipse without it).</summary>
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
