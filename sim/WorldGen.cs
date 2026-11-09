using Return.Sim.Content;
using Return.Sim.Core;

namespace Return.Sim;

/// <summary>What a cast member fundamentally is, derived lazily from Truths (ADR-0004).</summary>
public sealed record CharacterNature(string SecondaryTrait);

/// <summary>
/// Builds a new world. Map, camp layout and every character's nature come from the stateless
/// Truths stream (keyed by tile, feature or cast key), so they can be re-derived at any time and
/// survive rewinds. Only event-like facts (when wanderers arrive) come from Dice.
/// </summary>
public static class WorldGen
{
    private const int TreeTarget = 24;
    private const int BushClusters = 3;
    private const int BushesPerCluster = 4;
    private const int StartBerriesOnBush = 4;
    private const int LateArrivals = 3;

    public static World Create(ContentDb content, ulong seed)
    {
        var w = new World { Seed = seed, Rng = Rng.FromSeed(seed), Content = content };
        CreateCast(w);
        w.Map = BuildMap(w);
        foreach (Character c in w.Characters) Systems.NatureNeeds.Init(w, c); // after the map: a dryad is bound to one of its trees
        PlaceStartItems(w);
        PositionStarters(w);
        ScheduleArrivals(w);
        return w;
    }

    public static CharacterNature NatureOf(ContentDb content, ulong seed, string castKey)
    {
        CastDef cast = content.CastByKey(castKey);
        var pool = new List<string>();
        foreach (TraitDef t in content.Traits)
            if (t.Id != cast.SignatureTrait) pool.Add(t.Id);
        var truths = new TruthRng { WorldSeed = seed };
        int pick = truths.Range(TruthId.SecondaryTrait, Fnv1a.Hash64(castKey), 0, 0, pool.Count);
        return new CharacterNature(pool[pick]);
    }

    /// <summary>Test helper for the snapshot benchmark: clones cast entries until the world holds <paramref name="total"/> characters.</summary>
    internal static void AddSyntheticCharacters(World w, int total)
    {
        int i = 0;
        while (w.Characters.Count < total)
        {
            Character template = w.Characters[i % 10];
            var c = NewCharacter(w, w.Content.CastByKey(template.CastKey), template.Traits[1]);
            c.CastKey = template.CastKey + "_" + w.Characters.Count;
            c.Otherworlder = false;
            c.Status = CharStatus.Member;
            Systems.NatureNeeds.Init(w, c);
            c.Pos = FindOpenNear(w.Map, w.Map.Hearth, i);
            w.Characters.Add(c);
            i++;
        }
    }

    private static void CreateCast(World w)
    {
        foreach (CastDef cd in w.Content.Cast)
        {
            Character c = NewCharacter(w, cd, NatureOf(w.Content, w.Seed, cd.Key).SecondaryTrait);
            c.Status = cd.StartsInCamp ? CharStatus.Member : CharStatus.OffMap;
            if (w.PlayerId == 0 && cd.Otherworlder && cd.StartsInCamp) w.PlayerId = c.Id;
            w.Characters.Add(c);
        }
    }

    private static Character NewCharacter(World w, CastDef cd, string secondaryTrait)
    {
        var c = new Character
        {
            Id = w.NewId(),
            CastKey = cd.Key,
            NameKey = cd.NameKey,
            SpeciesId = cd.Species,
            RoleId = cd.Role,
            Otherworlder = cd.Otherworlder,
            Named = true,
            Traits = new List<string> { cd.SignatureTrait, secondaryTrait },
        };
        foreach (RolePriority p in w.Content.RoleById(cd.Role).Priorities)
            c.Priorities.Add(new WorkPriority { WorkId = p.Work, Priority = p.Priority });
        if (w.Content.SpeciesById(cd.Species).EatsNothing) c.Food = 1000;
        return c;
    }

    private static Map BuildMap(World w)
    {
        WorldDef def = w.Content.World;
        TruthRng t = w.Rng.Truths;
        int width = def.MapWidth, height = def.MapHeight;
        var m = new Map { Width = width, Height = height, Terrain = new byte[width * height] };

        // Soil patches: value noise on a coarse lattice with integer bilinear interpolation.
        const int cell = 6;
        int latticeW = width / cell + 2;
        int Lattice(int gx, int gy) => t.Range(TruthId.Terrain, (ulong)(gy * latticeW + gx), 0, 0, 1000);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int gx = x / cell, gy = y / cell, fx = x % cell, fy = y % cell;
                int top = Lattice(gx, gy) * (cell - fx) + Lattice(gx + 1, gy) * fx;
                int bottom = Lattice(gx, gy + 1) * (cell - fx) + Lattice(gx + 1, gy + 1) * fx;
                int v = (top * (cell - fy) + bottom * fy) / (cell * cell);
                if (v > 640) m.Set(new Point(x, y), TerrainKind.Soil);
            }
        }

        // River along the east edge; the bank wanders at most one tile per row.
        int bank = width - 6 - t.Range(TruthId.RiverPath, 0, 0, 0, 2);
        for (int y = 0; y < height; y++)
        {
            if (y > 0) bank = Math.Clamp(bank + t.Range(TruthId.RiverPath, (ulong)y, 0, -1, 2), width - 8, width - 4);
            for (int x = bank; x < width; x++) m.Set(new Point(x, y), TerrainKind.Water);
        }

        // Rock outcrops in the north-west.
        for (uint i = 0; i < 3; i++)
        {
            int cx = t.Range(TruthId.RockPlacement, i, 0, 1, 9);
            int cy = t.Range(TruthId.RockPlacement, i, 1, 1, 7);
            int r = t.Range(TruthId.RockPlacement, i, 2, 1, 4);
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    var p = new Point(x, y);
                    if (m.InBounds(p) && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r + 1) m.Set(p, TerrainKind.Rock);
                }
        }

        // The ruined camp: shelter (8x5, three roof sections), covered ruins, hearth, stockpile, watch post.
        int sx = 16 + t.Range(TruthId.CampPlacement, 0, 0, 0, 6);
        int sy = 9 + t.Range(TruthId.CampPlacement, 0, 1, 0, 4);
        var clearing = new Rect(sx - 7, sy - 2, 22, 19);
        for (int y = clearing.Y; y < clearing.Y + clearing.H; y++)
            for (int x = clearing.X; x < clearing.X + clearing.W; x++)
            {
                var p = new Point(x, y);
                if (m.InBounds(p) && m.At(p) is TerrainKind.Rock or TerrainKind.Water) m.Set(p, TerrainKind.Grass);
            }

        m.Shelter = new Rect(sx, sy, 8, 5);
        for (int y = sy; y < sy + 5; y++)
            for (int x = sx; x < sx + 8; x++)
            {
                bool edge = x == sx || x == sx + 7 || y == sy || y == sy + 4;
                m.Set(new Point(x, y), edge ? TerrainKind.Wall : TerrainKind.Floor);
            }
        m.Set(new Point(sx + 4, sy + 4), TerrainKind.Door);
        for (int i = 0; i < 3; i++)
            m.Roof.Add(new RoofSection { Index = i, Area = new Rect(sx + 1 + 2 * i, sy + 1, 2, 3) });
        foreach (int bx in new[] { sx + 1, sx + 3, sx + 5, sx + 6 })
            m.Beds.Add(new Bed { Id = w.NewId(), Pos = new Point(bx, sy + 1) });

        m.CoveredRuins = new Rect(sx + 9, sy + 1, 3, 3);
        for (int y = sy + 1; y < sy + 4; y++)
            for (int x = sx + 9; x < sx + 12; x++) m.Set(new Point(x, y), TerrainKind.Floor);
        for (int x = sx + 9; x < sx + 12; x++) m.Set(new Point(x, sy), TerrainKind.Wall);

        m.Hearth = new Point(sx + 4, sy + 7);
        m.Stockpile = new Rect(sx - 5, sy + 6, 4, 3);
        m.WatchPost = new Point(sx + 4, Math.Min(height - 3, sy + 15));

        bool Reserved(Point p) =>
            m.Shelter.Contains(p) || m.CoveredRuins.Contains(p) || m.Stockpile.Contains(p) ||
            p.ChebyshevTo(m.Hearth) <= 2 || p == m.WatchPost ||
            (p.X >= sx - 2 && p.X <= sx + 13 && p.Y >= sy - 1 && p.Y <= sy + 9);
        bool Open(Point p) => m.InBounds(p) && m.At(p) is TerrainKind.Grass or TerrainKind.Soil && !Reserved(p) && m.TreeAt(p) == null && m.BushAt(p) == null;

        // Trees by rejection sampling, keyed by attempt number.
        for (uint i = 0; m.Trees.Count < TreeTarget && i < 800; i++)
        {
            var p = new Point(t.Range(TruthId.TreePlacement, i, 0, 1, width - 1), t.Range(TruthId.TreePlacement, i, 1, 1, height - 1));
            if (Open(p)) m.Trees.Add(new Tree { Id = w.NewId(), Pos = p });
        }

        // Berry bushes in small clusters; each cluster centre is itself rejection-sampled onto open ground.
        for (uint c = 0; c < BushClusters; c++)
        {
            int placed = 0;
            for (uint attempt = 0; attempt < 40 && placed < BushesPerCluster; attempt++)
            {
                ulong key = c * 64 + attempt;
                var centre = new Point(t.Range(TruthId.BushPlacement, key, 0, 3, width - 12), t.Range(TruthId.BushPlacement, key, 1, 3, height - 3));
                if (!Open(centre)) continue;
                for (uint k = 0; k < 16 && placed < BushesPerCluster; k++)
                {
                    var p = new Point(centre.X + t.Range(TruthId.BushPlacement, key, 2 + 2 * k, -2, 3), centre.Y + t.Range(TruthId.BushPlacement, key, 3 + 2 * k, -2, 3));
                    if (!Open(p)) continue;
                    m.Bushes.Add(new Bush { Id = w.NewId(), Pos = p, Berries = StartBerriesOnBush });
                    placed++;
                }
            }
        }

        m.RebuildCaches();
        return m;
    }

    private static void PlaceStartItems(World w)
    {
        WorldDef def = w.Content.World;
        Rect s = w.Map.Stockpile;
        ItemDef berries = w.Content.ItemById("berries");
        if (def.StartBerries > 0)
            w.Items.Add(new ItemStack { Id = w.NewId(), DefId = berries.Id, Count = def.StartBerries, Pos = new Point(s.X, s.Y), SpoilTick = (long)berries.SpoilDays * def.TicksPerDay });
        if (def.StartTimber > 0)
            w.Items.Add(new ItemStack { Id = w.NewId(), DefId = "timber", Count = def.StartTimber, Pos = new Point(s.X + 1, s.Y) });
    }

    private static void PositionStarters(World w)
    {
        int i = 0;
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member) c.Pos = FindOpenNear(w.Map, w.Map.Hearth, i++);
    }

    private static Point FindOpenNear(Map m, Point centre, int skip)
    {
        for (int r = 1; r < 8; r++)
            for (int dy = -r; dy <= r; dy++)
                for (int dx = -r; dx <= r; dx++)
                {
                    if (Math.Max(Math.Abs(dx), Math.Abs(dy)) != r) continue;
                    var p = new Point(centre.X + dx, centre.Y + dy);
                    if (!m.Passable(p)) continue;
                    if (skip-- <= 0) return p;
                }
        return centre;
    }

    /// <summary>
    /// Most wanderers turn up in the first week; the last few come as autumn turns to winter, when the
    /// camp may not be able to feed them (ADR-0014). Who comes when is shuffled with Dice.
    /// </summary>
    private static void ScheduleArrivals(World w)
    {
        WorldDef def = w.Content.World;
        DiceStream dice = w.Rng.Dice[DiceSystem.World];
        var waiting = new List<Character>();
        var ownSeason = new List<Character>();
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.OffMap) continue;
            if (w.Content.CastByKey(c.CastKey).ArrivalDays.Length == 2) ownSeason.Add(c);
            else waiting.Add(c);
        }
        for (int k = waiting.Count - 1; k > 0; k--)
        {
            int j = dice.Range(DiceId.ArrivalOrder, 0, k + 1);
            (waiting[k], waiting[j]) = (waiting[j], waiting[k]);
        }
        int early = Math.Max(0, waiting.Count - LateArrivals);
        int lateFrom = (def.SeasonsPerYear - 1) * def.DaysPerSeason - 1; // the eve of winter
        for (int i = 0; i < waiting.Count; i++)
        {
            Character c = waiting[i];
            int day = i < early
                ? 1 + i * 3 / 2 + dice.Range(DiceId.ArrivalDay, 0, 2)
                : lateFrom + (i - early) * 3 + dice.Range(DiceId.ArrivalDay, 0, 3);
            bool burns = w.Content.SpeciesById(c.SpeciesId).DaylightBurnPerHour > 0;
            int hour = burns
                ? (def.DuskHour + 1 + dice.Range(DiceId.ArrivalHour, 0, 6)) % def.HoursPerDay
                : dice.Range(DiceId.ArrivalHour, 8, 18);
            w.Arrivals.Add(new Arrival { CharacterId = c.Id, Tick = (long)day * def.TicksPerDay + (long)hour * def.TicksPerHour });
        }

        // Those who come in their own season (cast.json arrivalDays, ADR-0022) draw after the shuffled
        // wanderers, so adding one never moves anyone else's arrival.
        foreach (Character c in ownSeason)
        {
            int[] window = w.Content.CastByKey(c.CastKey).ArrivalDays;
            int day = window[0] - 1 + dice.Range(DiceId.PeopleArrivalDay, 0, window[1] - window[0] + 1);
            bool burns = w.Content.SpeciesById(c.SpeciesId).DaylightBurnPerHour > 0;
            int hour = burns
                ? (def.DuskHour + 1 + dice.Range(DiceId.PeopleArrivalHour, 0, 6)) % def.HoursPerDay
                : dice.Range(DiceId.PeopleArrivalHour, 8, 18);
            w.Arrivals.Add(new Arrival { CharacterId = c.Id, Tick = (long)day * def.TicksPerDay + (long)hour * def.TicksPerHour });
        }
    }
}
