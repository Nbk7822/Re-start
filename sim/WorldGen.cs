using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Systems;

namespace Return.Sim;

/// <summary>
/// What a cast member fundamentally is, derived lazily from Truths (ADR-0004). WantType is the want
/// they are drawn to by nature (fixed decision 12, ADR-0021), "" if nothing has weight for them.
/// </summary>
public sealed record CharacterNature(string SecondaryTrait, string WantType);

/// <summary>
/// Builds a new world. Map, camp layout and every character's nature come from the stateless
/// Truths stream (keyed by tile, feature or cast key), so they can be re-derived at any time and
/// survive rewinds. Only event-like facts (when wanderers arrive) come from Dice.
/// </summary>
public static class WorldGen
{
    private const int TreeTarget = 24;

    /// <summary>
    /// Bushes per cluster (ADR-0013's three clusters of four). The fourth, smaller cluster feeds the one
    /// extra mouth the peoples brought (the fae eat berries; ADR-0022), keeping winter's half rations per
    /// head. It is keyed after the first three, so none of their bushes moves.
    /// </summary>
    private static readonly int[] BushesInCluster = { 4, 4, 4, 2 };
    private const int StartBerriesOnBush = 4;
    private const int LateArrivals = 3;

    /// <summary>
    /// A new world for a seed. Your people and gift are rolled from Truths (ADR-0030) unless <paramref name="choice"/>
    /// chooses them for testing.
    /// </summary>
    public static World Create(ContentDb content, ulong seed, RebirthChoice? choice = null)
    {
        var w = new World { Seed = seed, Rng = Rng.FromSeed(seed), Content = content };
        CreateCast(w, choice);
        w.Map = BuildMap(w);
        foreach (Character c in w.Characters) Systems.NatureNeeds.Init(w, c); // after the map: a dryad is bound to one of its trees
        PlaceStartItems(w);
        PositionStarters(w);
        ScheduleArrivals(w);
        FormStartersWants(w);
        Systems.Culture.CultureGen.Seed(w);
        // The old roof's sections take their ids after everything else is made, so no other id moved (ADR-0027).
        foreach (RoofSection r in w.Map.Roof) r.Id = w.NewId();
        return w;
    }

    /// <summary>
    /// What a cast member is by nature. For you, the otherworlder, that follows the people you are reborn as (a Truth,
    /// ADR-0030), so your natural want matches your people (a vampire can want daylight); <paramref name="species"/>
    /// names a people chosen for testing instead.
    /// </summary>
    public static CharacterNature NatureOf(ContentDb content, ulong seed, string castKey, string? species = null)
    {
        CastDef cast = content.CastByKey(castKey);
        species ??= Rebirth.IsYou(cast) ? Rebirth.PeopleOf(content, seed, castKey) : cast.Species;
        var pool = new List<string>();
        foreach (TraitDef t in content.Traits)
            if (t.Id != cast.SignatureTrait) pool.Add(t.Id);
        var truths = new TruthRng { WorldSeed = seed };
        int pick = truths.Range(TruthId.SecondaryTrait, Fnv1a.Hash64(castKey), 0, 0, pool.Count);
        WantDef? want = Growth.NaturalWant(content, truths, Fnv1a.Hash64(castKey), new[] { cast.SignatureTrait, pool[pick] },
            species, cast.Role, cast.Otherworlder, someoneElse: true);
        return new CharacterNature(pool[pick], want?.Id ?? "");
    }

    /// <summary>Test helper for the snapshot benchmark: clones cast entries until the world holds <paramref name="total"/> characters.</summary>
    internal static void AddSyntheticCharacters(World w, int total)
    {
        int i = 0;
        while (w.Characters.Count < total)
        {
            Character template = w.Characters[i % 10];
            CastDef td = w.Content.CastByKey(template.CastKey);
            var c = NewCharacter(w, td, template.Traits[1], td.Species);
            c.CastKey = template.CastKey + "_" + w.Characters.Count;
            c.Otherworlder = false;
            c.Status = CharStatus.Member;
            c.CultureId = template.CultureId;
            c.FaithId = template.FaithId;
            c.Devotion = template.Devotion;
            Systems.NatureNeeds.Init(w, c);
            c.Pos = FindOpenNear(w.Map, w.Map.Hearth, i);
            w.Characters.Add(c);
            Growth.FormWant(w, c);
            i++;
        }
    }

    /// <summary>
    /// The cast, in file order (so cast ids are stable across timelines). You, the otherworlder who starts in camp,
    /// are reborn as the people rolled for this world, or chosen for testing (ADR-0030); cast.json's species for you
    /// is only the fallback a mod sees.
    /// </summary>
    private static void CreateCast(World w, RebirthChoice? choice)
    {
        foreach (CastDef cd in w.Content.Cast)
        {
            bool you = w.PlayerId == 0 && Rebirth.IsYou(cd);
            string species = you ? Rebirth.PeopleFor(w.Content, w.Seed, cd.Key, choice) : cd.Species;
            Character c = NewCharacter(w, cd, NatureOf(w.Content, w.Seed, cd.Key, species).SecondaryTrait, species);
            c.Status = cd.StartsInCamp ? CharStatus.Member : CharStatus.OffMap;
            w.Characters.Add(c);
            if (!you) continue;
            w.PlayerId = c.Id;
            Rebirth.Begin(w, c, choice);
        }
    }

    private static Character NewCharacter(World w, CastDef cd, string secondaryTrait, string species)
    {
        var c = new Character
        {
            Id = w.NewId(),
            CastKey = cd.Key,
            NameKey = cd.NameKey,
            SpeciesId = species,
            RoleId = cd.Role,
            Otherworlder = cd.Otherworlder,
            Named = true,
            Traits = new List<string> { cd.SignatureTrait, secondaryTrait },
        };
        c.Growth.TraitOrigins = Growth.GeneratedOrigins(c.Traits);
        foreach (RolePriority p in w.Content.RoleById(cd.Role).Priorities)
            c.Priorities.Add(new WorkPriority { WorkId = p.Work, Priority = p.Priority });
        if (w.Content.SpeciesById(species).EatsNothing) c.Food = 1000;
        Rebirth.SetAges(w, c, species, cd.Key);
        return c;
    }

    /// <summary>
    /// The camp starters enter the world with it, so their wants form now (fixed decision 12). Their
    /// beats are logged on the first tick (Growth.Hourly), so mod hooks and the Chronicle see them.
    /// </summary>
    private static void FormStartersWants(World w)
    {
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member) Growth.FormWant(w, c);
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
        for (uint c = 0; c < BushesInCluster.Length; c++)
        {
            int placed = 0, wanted = BushesInCluster[c];
            for (uint attempt = 0; attempt < 40 && placed < wanted; attempt++)
            {
                ulong key = c * 64 + attempt;
                var centre = new Point(t.Range(TruthId.BushPlacement, key, 0, 3, width - 12), t.Range(TruthId.BushPlacement, key, 1, 3, height - 3));
                if (!Open(centre)) continue;
                for (uint k = 0; k < 16 && placed < wanted; k++)
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
        // Each slot keeps its window (ADR-0015's first week, the eve of winter) so a return can re-draw within it (ADR-0030).
        (int First, int Last) firstWeek = (1, 1 + (early - 1) * 3 / 2 + 1), eveOfWinter = (lateFrom, lateFrom + (waiting.Count - early - 1) * 3 + 2);
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
            (int first, int last) = i < early ? firstWeek : eveOfWinter;
            w.Arrivals.Add(new Arrival
            {
                CharacterId = c.Id, Tick = (long)day * def.TicksPerDay + (long)hour * def.TicksPerHour,
                FirstDay = first, LastDay = last, Shuffled = true,
            });
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
            w.Arrivals.Add(new Arrival
            {
                CharacterId = c.Id, Tick = (long)day * def.TicksPerDay + (long)hour * def.TicksPerHour,
                FirstDay = window[0] - 1, LastDay = window[1] - 1,
            });
        }
    }

    /// <summary>
    /// After a return (ADR-0030): ScheduleArrivals fixed every arrival's tick when the world was made, so a reseed
    /// alone would bring the same people at the same hours. For every arrival still to come, re-draw who fills each
    /// remaining wanderer slot (shuffled among those slots) and when, from the reseeded World stream with the same
    /// tags, within each slot's window (or the person's own season) and never before now. The hours keep to each
    /// person's kind: night for those the sun burns. Only if a window has no hour left for someone (a burner moved
    /// onto a day slot on its last day) do they come at the first such hour after it.
    /// </summary>
    public static void RedrawArrivals(World w)
    {
        WorldDef def = w.Content.World;
        DiceStream dice = w.Rng.Dice[DiceSystem.World];
        var slots = new List<Arrival>();
        var people = new List<int>();
        foreach (Arrival a in w.Arrivals)
            if (!a.Done && a.Shuffled) { slots.Add(a); people.Add(a.CharacterId); }
        for (int k = people.Count - 1; k > 0; k--)
        {
            int j = dice.Range(DiceId.ArrivalOrder, 0, k + 1);
            (people[k], people[j]) = (people[j], people[k]);
        }
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].CharacterId = people[i];
            slots[i].Tick = RedrawTick(w, slots[i], DiceId.ArrivalDay, DiceId.ArrivalHour);
        }
        foreach (Arrival a in w.Arrivals)
            if (!a.Done && !a.Shuffled) a.Tick = RedrawTick(w, a, DiceId.PeopleArrivalDay, DiceId.PeopleArrivalHour);
    }

    /// <summary>The hours of the day someone may walk in: from dusk into the small hours for those the sun burns, else 08:00 to 17:00.</summary>
    public static IReadOnlyList<int> ArrivalHours(World w, Character c)
    {
        WorldDef def = w.Content.World;
        var hours = new List<int>();
        if (w.Content.SpeciesById(c.SpeciesId).DaylightBurnPerHour > 0)
            for (int r = 0; r < 6; r++) hours.Add((def.DuskHour + 1 + r) % def.HoursPerDay);
        else
            for (int h = 8; h < 18; h++) hours.Add(h);
        return hours;
    }

    private static long RedrawTick(World w, Arrival a, DiceId dayDraw, DiceId hourDraw)
    {
        WorldDef def = w.Content.World;
        DiceStream dice = w.Rng.Dice[DiceSystem.World];
        Character c = w.Get(a.CharacterId);
        IReadOnlyList<int> hours = ArrivalHours(w, c);
        long At(int day, int hour) => (long)day * def.TicksPerDay + (long)hour * def.TicksPerHour;
        var days = new List<int>();
        for (int d = Math.Max(a.FirstDay, Calendar.Day(w.Tick, def)); d <= a.LastDay; d++)
            foreach (int h in hours)
                if (At(d, h) > w.Tick) { days.Add(d); break; }
        if (days.Count == 0)
        {
            // Nothing left in the window for them: the first hour of theirs after now.
            for (int d = Calendar.Day(w.Tick, def); ; d++)
                foreach (int h in hours)
                    if (At(d, h) > w.Tick) return At(d, h);
        }
        int day = days[dice.Range(dayDraw, 0, days.Count)];
        var open = new List<int>();
        foreach (int h in hours)
            if (At(day, h) > w.Tick) open.Add(h);
        return At(day, open[dice.Range(hourDraw, 0, open.Count)]);
    }
}
