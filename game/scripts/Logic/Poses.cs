using System;
using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Systems;
using SimPoint = Return.Sim.Core.Point;

namespace Return.Game.Logic;

/// <summary>How a figure is drawn this frame. Every pose comes from sim state, never decoration.</summary>
public enum Pose
{
    Stand,
    Walk,
    Crouch,
    Swing,
    Sit,
    Talk,
    Sleep,
    Downed,
    Dead,
}

/// <summary>What a working figure holds.</summary>
public enum HeldTool
{
    None,
    Axe,
    Hammer,
    Club,
    Knife,
}

/// <summary>The sim facts a pose depends on, so pose choice is testable without a world.</summary>
public readonly record struct PoseInput(
    CharStatus Status,
    bool Downed,
    bool Asleep,
    JobKind? Job,
    int Stage,
    bool Moving,
    bool NearHearth);

/// <summary>
/// Chooses a pose, a facing and a held tool from sim state (ADR-0009): walking while moving; a
/// crouch to forage, tend the hurt or feed; a swinging arm to fell timber, mend the roof or fight;
/// sitting to eat, to give blood, or idle by the hearth; lying when asleep, downed or dead. People
/// turn to face where they go (Character.Facing) or whom they are talking to.
/// </summary>
public static class Poses
{
    /// <summary>How long after a chat, a deep talk or an insult the two keep facing each other.</summary>
    public const int TalkTurnTicks = 90;

    /// <summary>Within this many tiles of the hearth an idle person sits down by the fire.</summary>
    public const int HearthSitRange = 2;

    public static Pose For(in PoseInput p)
    {
        if (p.Status == CharStatus.Dead) return Pose.Dead;
        if (p.Downed) return Pose.Downed;
        if (p.Asleep) return Pose.Sleep;
        if (p.Moving) return Pose.Walk;
        switch (p.Job)
        {
            case JobKind.Forage when p.Stage == 1:
            case JobKind.Care when p.Stage >= 1:
            case JobKind.RequestBlood when p.Stage >= 1:
                return Pose.Crouch;
            case JobKind.GatherTimber when p.Stage == 1:
            case JobKind.RepairRoof when p.Stage >= 2:
            case JobKind.Fight:
                return Pose.Swing;
            case JobKind.Eat:
            case JobKind.GiveBlood when p.Stage >= 1:
                return Pose.Sit;
            case JobKind.Socialize when p.Stage >= 1:
                return Pose.Talk;
            case null:
            case JobKind.Idle:
            case JobKind.Wander:
                return p.NearHearth && p.Status == CharStatus.Member ? Pose.Sit : Pose.Stand;
            default:
                return Pose.Stand;
        }
    }

    public static Pose For(World w, Character c, bool moving)
    {
        Job? j = c.Job;
        bool nearHearth = w.Map.InBounds(c.Pos) && c.Pos.ChebyshevTo(w.Map.Hearth) <= HearthSitRange;
        Pose pose = For(new PoseInput(c.Status, c.Downed, c.Asleep, j?.Kind, j?.Stage ?? 0, moving, nearHearth));
        if (pose == Pose.Stand && c.Status == CharStatus.Hostile && Attacker(w, c) != 0) return Pose.Swing;
        return pose;
    }

    /// <summary>A member fighting this person from an adjacent tile, or 0.</summary>
    public static int Attacker(World w, Character target)
    {
        foreach (Character m in w.Characters)
            if (m.Status == CharStatus.Member && !m.Downed && m.Job is { Kind: JobKind.Fight } fj && fj.TargetId == target.Id && m.Pos.ChebyshevTo(target.Pos) <= 1)
                return m.Id;
        return 0;
    }

    /// <summary>The tool in hand for a pose: an axe to fell, a hammer to mend, a club against thieves (a thief carries a knife).</summary>
    public static HeldTool ToolFor(Character c, Pose pose)
    {
        if (c.Status == CharStatus.Hostile) return pose is Pose.Dead or Pose.Downed ? HeldTool.None : HeldTool.Knife;
        if (pose != Pose.Swing) return HeldTool.None;
        return c.Job?.Kind switch
        {
            JobKind.GatherTimber => HeldTool.Axe,
            JobKind.RepairRoof => HeldTool.Hammer,
            JobKind.Fight => HeldTool.Club,
            _ => HeldTool.None,
        };
    }

    /// <summary>Seconds per blow of a swing (felling, mending, fighting).</summary>
    public const float SwingSeconds = 0.9f;

    /// <summary>
    /// How far the near arm reaches forward in side view, in radians from hanging straight down
    /// (positive is toward the facing; the hand is in front of the body anywhere between 0 and π).
    /// Walking swings it to and fro; a swing raises the tool high in front and brings it down fast
    /// onto what is in front (a trunk, a roof beam, a thief); a crouch reaches down to the ground;
    /// sitting rests it on the knees; talking gestures; carrying holds it out under the load.
    /// </summary>
    public static float SideArmReach(Pose pose, float phase, float time, float seed, bool carrying)
    {
        if (carrying) return 0.9f;
        switch (pose)
        {
            case Pose.Walk:
                return MathF.Sin(phase * MathF.Tau) * 0.5f;
            case Pose.Swing:
            {
                // Raised slowly over three quarters of the blow, struck down in the last quarter.
                float k = time / SwingSeconds + seed * 0.13f;
                k -= MathF.Floor(k);
                return k < 0.75f ? 0.45f + 1.65f * Ease(k / 0.75f) : 2.1f - 1.65f * Ease((k - 0.75f) / 0.25f);
            }
            case Pose.Crouch:
                return 0.6f + MathF.Sin(time * 3f + seed) * 0.3f;
            case Pose.Sit:
                return 0.5f;
            case Pose.Talk:
                return 0.7f + MathF.Sin(time * 2.4f + seed) * 0.35f;
            default:
                return 0f;
        }
    }

    /// <summary>The near arm's rotation in side view (the drawing rotates "down" toward -x for a positive angle).</summary>
    public static float SideArmAngle(float reach, Facing facing) => facing == Facing.W ? reach : -reach;

    /// <summary>Where the hand of an arm of <paramref name="length"/> rotated by <paramref name="angle"/> ends up across the body (+x is east).</summary>
    public static float HandX(float angle, float length) => -MathF.Sin(angle) * length;

    private static float Ease(float t) => t * t * (3f - 2f * t);

    /// <summary>
    /// Which way to draw someone: toward whoever they are talking to (or tending, feeding, or
    /// fighting) while they stand still (two on one tile stand side by side in id order, so they
    /// turn to each other); else, sitting by the hearth, toward the fire; otherwise the sim's facing
    /// (the way they last stepped).
    /// </summary>
    public static Facing FacingOf(World w, Character c, bool moving)
    {
        if (moving || c.Status != CharStatus.Member && c.Status != CharStatus.Hostile) return c.Facing;
        int partner = TalkPartner(w, c);
        Character? other = partner == 0 ? null : w.Find(partner);
        if (other != null && other.IsActive)
            return other.Pos != c.Pos ? Toward(c.Pos, other.Pos) : c.Id < other.Id ? Facing.E : Facing.W;
        SimPoint hearth = w.Map.Hearth;
        if (c.Pos != hearth && c.Pos.ChebyshevTo(hearth) <= HearthSitRange && For(w, c, false) == Pose.Sit) return Toward(c.Pos, hearth);
        return c.Facing;
    }

    /// <summary>The same rule the sim uses when someone steps (TravelSystem.FacingFor).</summary>
    public static Facing Toward(SimPoint from, SimPoint to) => TravelSystem.FacingFor(from, to);

    /// <summary>
    /// The person this one is turned toward: the target of a conversation, a feeding or a fight
    /// once they have reached them, else whoever they chatted with, talked deeply with, were
    /// insulted or refused by in the last <see cref="TalkTurnTicks"/> ticks (from their thoughts),
    /// if that person is still close. 0 when none.
    /// </summary>
    public static int TalkPartner(World w, Character c)
    {
        Job? j = c.Job;
        if (c.Status == CharStatus.Hostile) return Attacker(w, c);
        if (j != null && j.TargetId != 0)
        {
            switch (j.Kind)
            {
                case JobKind.Socialize when j.Stage >= 1:
                case JobKind.RequestBlood when j.Stage >= 1:
                case JobKind.GiveBlood when j.Stage >= 1:
                case JobKind.Care when j.Stage >= 1:
                case JobKind.Fight:
                    return j.TargetId;
            }
        }

        WorldDef def = w.Content.World;
        long best = long.MinValue;
        int partner = 0;
        foreach (Thought t in c.Thoughts)
        {
            if (t.AboutId == 0 || !IsTalk(t.DefId)) continue;
            ThoughtDef td = w.Content.ThoughtById(t.DefId);
            long started = t.ExpiresTick - (long)td.DurationHours * def.TicksPerHour;
            if (w.Tick - started > TalkTurnTicks || started < best) continue;
            best = started;
            partner = t.AboutId;
        }
        if (partner == 0) return 0;
        Character? other = w.Find(partner);
        return other != null && other.IsActive && other.Pos.ChebyshevTo(c.Pos) <= SocialSystem.NeighbourRange ? partner : 0;
    }

    private static bool IsTalk(string thoughtId) =>
        thoughtId is "chatted" or "deep_talk" or "insulted" or "was_refused" or "refused_someone";
}
