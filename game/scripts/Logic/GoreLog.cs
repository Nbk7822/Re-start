using System;
using System.Collections.Generic;
using Return.Sim;
using Return.Sim.Content;
using Return.Sim.Events;
using SimCalendar = Return.Sim.Core.Calendar;
using SimPoint = Return.Sim.Core.Point;

namespace Return.Game.Logic;

public enum DecalKind
{
    /// <summary>Blood thrown by a blow, a fall or an accident.</summary>
    Splatter,

    /// <summary>A few drops: the badly wounded walking, a bite.</summary>
    Drops,

    /// <summary>A pool under the downed and the dead; it spreads for a while after the fall.</summary>
    Pool,

    /// <summary>Ash and scorch where a vampire burned in the sun.</summary>
    Scorch,

    /// <summary>What the bloodless leave where they are struck or fall: the undead's grave dust, a djinn's ash (Decal.Ichor says which).</summary>
    Dust,
}

/// <summary>How someone died, as far as their body shows it.</summary>
public enum DeathKind
{
    /// <summary>Wounds: a fight or an accident. The body lies in its blood.</summary>
    Wounds,

    /// <summary>Burned by the sun: charred, smouldering by day.</summary>
    Sun,

    /// <summary>Hunger, thirst or failing health: no blood.</summary>
    Wasted,
}

/// <summary>
/// One mark on the ground. X and Y are in tiles (a tile's centre is x + 0.5). Rotation, Variant
/// and the small offset from the tile centre come from a hash of the event id (or of the person
/// and tick, for a blow the log does not record), never from the sim's RNG.
/// </summary>
public struct Decal
{
    public DecalKind Kind;
    public float X;
    public float Y;
    public float Rotation;
    public int Variant;
    public float Scale;
    public long Tick;

    /// <summary>0 fresh … 1 gone. Rain wears blood away faster.</summary>
    public float Wear;

    /// <summary>Under a corpse: it dries and darkens but does not fade while the body lies there.</summary>
    public bool Lasting;

    /// <summary>The event id it came from, or the hash it was keyed by.</summary>
    public long Key;

    /// <summary>Whose wound it was, as the mark shows it: blood or a dryad's sap (splatter, drops, pools), grave dust or ash (dust).</summary>
    public Ichor Ichor;
}

/// <summary>A body: who, how they died, and when (the death event).</summary>
public readonly record struct Remains(int CharacterId, DeathKind Kind, long Tick, long EventId);

/// <summary>
/// The view's memory of violence, derived only from the sim (ADR-0009 update): it reads new events
/// in World.Log since it last looked (blows that downed or killed, work accidents, collapses,
/// deaths, bites, sunburn) and watches health drop between looks for the blows the log does not
/// record one by one (each exchange in a fight). It records where blood fell, keeps the cause of
/// each death for drawing the body, and lets marks wear away over a few in-game days. Wounds give
/// what the wounded's nature gives (Kinship.IchorOf): blood, a dryad's sap, and for those who do
/// not bleed, the undead's grave dust or a djinn's ash; the undead return to dust however they
/// come to rest. It never writes to the world and never draws from its RNG streams.
/// </summary>
public sealed class GoreLog
{
    public const int SplatterVariants = 4;
    public const int DropVariants = 2;
    public const int ScorchVariants = 1;
    public const int PoolVariants = 1;
    public const int DustVariants = 3;

    /// <summary>Marks wear away over this many in-game days (three times faster in rain).</summary>
    public const int FadeDays = 3;

    /// <summary>A drop in health at least this big while fighting is a blow that drew blood.</summary>
    public const int BlowDamage = 40;

    /// <summary>The wounded drip blood as they walk while their health is below this.</summary>
    public const int BleedBelow = 400;

    /// <summary>A pool spreads for this many ticks after someone falls.</summary>
    public const int PoolSpreadTicks = 240;

    /// <summary>At most this many marks are kept; the most worn go first.</summary>
    public const int MaxDecals = 400;

    private readonly List<Decal> _decals = new();
    private readonly Dictionary<int, int> _health = new();
    private readonly Dictionary<int, SimPoint> _lastPos = new();
    private readonly Dictionary<int, Remains> _remains = new();
    private readonly Dictionary<int, long> _pooledAt = new();
    private int _nextEvent;
    private long _lastTick = -1;

    public IReadOnlyList<Decal> Decals => _decals;

    /// <summary>How many log events have been read.</summary>
    public int EventsRead => _nextEvent;

    /// <summary>How someone died, if the log says (null for the living).</summary>
    public Remains? RemainsOf(World w, Character c)
    {
        if (c.Status != CharStatus.Dead) return null;
        if (_remains.TryGetValue(c.Id, out Remains r)) return r;
        // Not seen yet (a world restored or advanced without watching): find the death in the log.
        List<SimEvent> all = w.Log.All;
        for (int i = all.Count - 1; i >= 0; i--)
        {
            SimEvent e = all[i];
            foreach (Effect f in e.Effects)
                if (f.Kind == EffectKind.Death && f.A == c.Id)
                {
                    r = new Remains(c.Id, DeathKindOf(e.Key), e.Tick, e.Id);
                    _remains[c.Id] = r;
                    return r;
                }
        }
        r = new Remains(c.Id, DeathKind.Wasted, w.Tick, 0);
        _remains[c.Id] = r;
        return r;
    }

    /// <summary>
    /// Reads what happened since the last call. Call it after the sim steps (every frame while
    /// playing; every tick while the view fast-forwards, so positions are exact).
    /// </summary>
    public void Observe(World w)
    {
        WorldDef def = w.Content.World;
        long fadeTicks = Math.Max(1L, (long)FadeDays * def.TicksPerDay);
        long elapsed = _lastTick < 0 ? 0 : Math.Max(0, w.Tick - _lastTick);
        bool first = _lastTick < 0;
        _lastTick = w.Tick;

        if (elapsed > 0) Wear(elapsed, fadeTicks, w.Weather.Raining);
        ReadEvents(w, first, fadeTicks);
        WatchBodies(w, first);
        if (_decals.Count > MaxDecals) DropMostWorn();
    }

    /// <summary>The death kind a death event's key shows.</summary>
    public static DeathKind DeathKindOf(string key) => key switch
    {
        "event.combat.died_wounds" or "event.combat.thief_killed" or "event.incident.died_accident" => DeathKind.Wounds,
        "event.needs.died_sun" => DeathKind.Sun,
        _ => DeathKind.Wasted,
    };

    /// <summary>
    /// How bloodied someone's clothes are, 0 (clean) to 3 (soaked): only wounds bleed (members carry
    /// a "wounded" thought after a blow or an accident; a thief is only ever hurt by blows). What soaks
    /// them is the wounded's ichor (Kinship.StainOf): blood, sap, grave dust or ash.
    /// </summary>
    public static int WoundLevel(World w, Character c)
    {
        if (c.Status == CharStatus.Dead) return 0;
        bool wounded = c.Status == CharStatus.Hostile;
        if (!wounded)
            foreach (Thought t in c.Thoughts)
                if (t.DefId == "wounded" && t.ExpiresTick > w.Tick) { wounded = true; break; }
        if (!wounded) return 0;
        int full = c.Status == CharStatus.Hostile ? 600 : 1000;
        int h = c.Health * 1000 / full;
        return c.Downed || h < 300 ? 3 : h < 550 ? 2 : h < 800 ? 1 : 0;
    }

    /// <summary>A vampire caught in daylight outside cover burns (and smokes) until they reach cover.</summary>
    public static bool Burning(World w, Character c)
    {
        if (c.Status != CharStatus.Member || !w.Map.InBounds(c.Pos)) return false;
        SpeciesDef sp = w.Content.SpeciesById(c.SpeciesId);
        return sp.DaylightBurnPerHour > 0 && SimCalendar.IsDaylight(w.Tick, w.Content.World) && !w.Map.Covered(c.Pos);
    }

    private void Wear(long elapsed, long fadeTicks, bool raining)
    {
        float step = elapsed * (raining ? 3f : 1f) / fadeTicks;
        for (int i = _decals.Count - 1; i >= 0; i--)
        {
            Decal d = _decals[i];
            d.Wear = d.Lasting ? Math.Min(0.5f, d.Wear + step) : d.Wear + step;
            if (d.Wear >= 1f) _decals.RemoveAt(i);
            else _decals[i] = d;
        }
    }

    private void ReadEvents(World w, bool first, long fadeTicks)
    {
        List<SimEvent> all = w.Log.All;
        while (_nextEvent < all.Count)
        {
            SimEvent e = all[_nextEvent++];
            // On the first look, events from long ago have worn away; deaths still leave their bodies.
            long age = w.Tick - e.Tick;
            float wear = first ? Math.Min(1f, age / (float)fadeTicks) : 0f;
            bool stale = first && age > 30;

            foreach (Effect f in e.Effects)
                if (f.Kind == EffectKind.Death) Died(w, e, f.A, wear);

            switch (e.Key)
            {
                case "event.combat.downed":
                    if (e.Actors.Count > 0) Fell(w, e, e.Actors[0], wear, stale);
                    break;
                case "event.needs.collapsed":
                    if (e.Actors.Count == 0) break;
                    switch (CauseOf(w, e))
                    {
                        case DeathKind.Wounds: Fell(w, e, e.Actors[0], wear, stale); break;
                        case DeathKind.Sun: Mark(w, e.Actors[0], DecalKind.Scorch, 0.7f, e.Id, e.Tick, wear, stale); break;
                    }
                    break;
                case "event.incident.work_accident":
                    if (e.Actors.Count > 0) Mark(w, e.Actors[0], DecalKind.Splatter, 0.8f, e.Id, e.Tick, wear, stale);
                    break;
                case "event.work.gave_blood":
                    if (e.Actors.Count > 0) Mark(w, e.Actors[0], DecalKind.Drops, 0.5f, e.Id, e.Tick, wear, stale);
                    break;
                case "event.needs.sun_burn":
                    if (e.Actors.Count > 0) Mark(w, e.Actors[0], DecalKind.Scorch, 0.45f, e.Id, e.Tick, wear, stale);
                    break;
            }
        }
    }

    /// <summary>Why someone collapsed: wounds (a fight or an accident), the sun, or neither.</summary>
    private static DeathKind CauseOf(World w, SimEvent collapsed)
    {
        foreach (long id in collapsed.Causes)
        {
            SimEvent? cause = w.Log.Get(id);
            if (cause == null) continue;
            if (cause.Key.StartsWith("event.combat.", StringComparison.Ordinal) || cause.Key == "event.incident.work_accident") return DeathKind.Wounds;
            if (cause.Key == "event.needs.sun_burn") return DeathKind.Sun;
        }
        return DeathKind.Wasted;
    }

    private void Died(World w, SimEvent e, int id, float wear)
    {
        Character? c = w.Find(id);
        DeathKind kind = DeathKindOf(e.Key);
        _remains[id] = new Remains(id, kind, e.Tick, e.Id);
        if (c == null || !w.Map.InBounds(c.Pos)) return;
        // A body does not move, so its position is exact even when read late.
        if (kind == DeathKind.Wounds)
        {
            Wound(w, c, DecalKind.Splatter, c.Pos, 1.15f, ArtHash.Of(e.Id, 1), e.Tick, wear, lasting: false, e.Id);
            Wound(w, c, DecalKind.Pool, c.Pos, 1.3f, ArtHash.Of(e.Id, 2), e.Tick, Math.Min(0.5f, wear), lasting: true, e.Id);
        }
        else if (kind == DeathKind.Sun)
        {
            Add(DecalKind.Scorch, c.Pos, 1.1f, ArtHash.Of(e.Id, 3), e.Tick, Math.Min(0.5f, wear), lasting: true, e.Id);
        }
        // The undead return to dust, however they come to rest.
        if (kind != DeathKind.Wounds && Kinship.IchorOf(w, c) == Ichor.Dust)
            Add(DecalKind.Dust, c.Pos, 1.25f, ArtHash.Of(e.Id, 10), e.Tick, Math.Min(0.5f, wear), lasting: true, e.Id, Ichor.Dust);
    }

    /// <summary>Someone falls to a wound: blood thrown, and a pool where they lie (once per fall).</summary>
    private void Fell(World w, SimEvent e, int id, float wear, bool stale)
    {
        Character? c = w.Find(id);
        if (c == null || !w.Map.InBounds(c.Pos)) return;
        if (stale && !c.Downed && c.Status != CharStatus.Dead) return; // moved on since; don't guess where
        if (_pooledAt.TryGetValue(id, out long at) && Math.Abs(at - e.Tick) <= 30) return;
        _pooledAt[id] = e.Tick;
        Wound(w, c, DecalKind.Splatter, c.Pos, 1f, ArtHash.Of(e.Id, 4), e.Tick, wear, lasting: false, e.Id);
        Wound(w, c, DecalKind.Pool, c.Pos, 0.9f, ArtHash.Of(e.Id, 5), e.Tick, wear, lasting: false, e.Id);
    }

    private void Mark(World w, int id, DecalKind kind, float scale, long eventId, long tick, float wear, bool stale)
    {
        Character? c = w.Find(id);
        if (c == null || !w.Map.InBounds(c.Pos)) return;
        if (stale && !c.Downed && c.Status != CharStatus.Dead) return;
        if (kind == DecalKind.Scorch) Add(kind, c.Pos, scale, ArtHash.Of(eventId, 6), tick, wear, lasting: false, eventId);
        else Wound(w, c, kind, c.Pos, scale, ArtHash.Of(eventId, 6), tick, wear, lasting: false, eventId);
    }

    /// <summary>Blows (health dropping while fighting) and the wounded dripping as they walk.</summary>
    private void WatchBodies(World w, bool first)
    {
        foreach (Character c in w.Characters)
        {
            bool present = c.IsActive || c.Status == CharStatus.Dead;
            if (!present || !w.Map.InBounds(c.Pos))
            {
                _health.Remove(c.Id);
                _lastPos.Remove(c.Id);
                continue;
            }
            if (!first && _health.TryGetValue(c.Id, out int before) && before - c.Health >= BlowDamage && InFight(c))
                Wound(w, c, DecalKind.Splatter, c.Pos, 0.75f, ArtHash.Of(c.Id, w.Tick, 7), w.Tick, 0f, lasting: false, (long)ArtHash.Of(c.Id, w.Tick));
            if (!first && _lastPos.TryGetValue(c.Id, out SimPoint was) && was != c.Pos && c.IsActive && c.Health < BleedBelow
                && WoundLevel(w, c) > 0 && ArtHash.Unit(c.Id, w.Tick, 8) < 0.7f)
                Wound(w, c, DecalKind.Drops, was, 0.55f, ArtHash.Of(c.Id, w.Tick, 9), w.Tick, 0f, lasting: false, (long)ArtHash.Of(c.Id, w.Tick, 9));
            _health[c.Id] = c.Health;
            _lastPos[c.Id] = c.Pos;
        }
    }

    /// <summary>
    /// Only blows bleed: a thief is only ever hurt by blows, a member while fighting one. Hunger,
    /// sunburn and giving blood also lower health but never by a blow's worth in one look while
    /// fighting; a death has its own event.
    /// </summary>
    private static bool InFight(Character c) => c.Status == CharStatus.Hostile || c.Job is { Kind: JobKind.Fight };

    /// <summary>
    /// A wound's mark, as the wounded's nature gives it: blood (or a dryad's amber sap) splatters, drips
    /// and pools; the undead and the djinn do not bleed, and leave grave dust or ash that settles (a
    /// pool becomes a heap, drops a trickle).
    /// </summary>
    private void Wound(World w, Character c, DecalKind kind, SimPoint at, float scale, ulong h, long tick, float wear, bool lasting, long key)
    {
        Ichor ichor = Kinship.IchorOf(w, c);
        if (ichor is Ichor.Dust or Ichor.Ash)
        {
            float settle = kind switch { DecalKind.Pool => 1.1f, DecalKind.Drops => 0.6f, _ => 0.9f };
            Add(DecalKind.Dust, at, scale * settle, h, tick, wear, lasting, key, ichor);
            return;
        }
        Add(kind, at, scale, h, tick, wear, lasting, key, ichor);
    }

    private void Add(DecalKind kind, SimPoint at, float scale, ulong h, long tick, float wear, bool lasting, long key, Ichor ichor = Ichor.Blood)
    {
        if (wear >= 1f) return;
        int variants = kind switch
        {
            DecalKind.Splatter => SplatterVariants,
            DecalKind.Drops => DropVariants,
            DecalKind.Scorch => ScorchVariants,
            DecalKind.Dust => DustVariants,
            _ => PoolVariants,
        };
        float spread = kind == DecalKind.Pool ? 0.08f : 0.22f;
        _decals.Add(new Decal
        {
            Kind = kind,
            X = at.X + 0.5f + (ArtHash.Unit(ArtHash.Mix(h ^ 0xA1)) * 2f - 1f) * spread,
            Y = at.Y + 0.5f + (ArtHash.Unit(ArtHash.Mix(h ^ 0xB2)) * 2f - 1f) * spread,
            Rotation = ArtHash.Unit(ArtHash.Mix(h ^ 0xC3)) * MathF.Tau,
            Variant = (int)(ArtHash.Mix(h ^ 0xD4) % (ulong)variants),
            Scale = scale * (0.85f + ArtHash.Unit(ArtHash.Mix(h ^ 0xE5)) * 0.3f),
            Tick = tick,
            Wear = wear,
            Lasting = lasting,
            Key = key,
            Ichor = ichor,
        });
    }

    private void DropMostWorn()
    {
        _decals.Sort(static (a, b) => a.Wear.CompareTo(b.Wear));
        _decals.RemoveRange(MaxDecals, _decals.Count - MaxDecals);
        _decals.Sort(static (a, b) => a.Tick.CompareTo(b.Tick));
    }
}
