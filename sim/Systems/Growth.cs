using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Character growth (fixed decision 12), M0 scope: the data and the story beats, no director logic
/// (BIBLE.md 5.3, ADR-0021). A person's first want is part of their nature: drawn from Truths by cast
/// key, it survives rewinds, and it forms when they enter the world. Every growth change is a beat
/// that names its cause. Nothing in M0 advances an arc or changes a trait; <see cref="ChangeTrait"/>
/// and <see cref="AdvanceArc"/> are the doors M1's trait-change tables and director will use.
/// </summary>
public static class Growth
{
    /// <summary>
    /// The want a person with these traits, species and role is drawn to by nature: a weighted pick
    /// over every want in wants.json, in file order, rolled with the Truth <see cref="TruthId.InitialWant"/>
    /// (index 0) keyed by <paramref name="natureKey"/> (the cast key's hash). That pick is their nature
    /// (<see cref="CharacterNature.WantType"/>) whoever is around. Only when it is about another person and
    /// there is nobody else (<paramref name="someoneElse"/> false) do they want something that needs no one
    /// instead: a second pick over the wants not about a person, with the same Truth at index 1. Null only
    /// when nothing has any weight.
    /// </summary>
    public static WantDef? NaturalWant(ContentDb content, TruthRng truths, ulong natureKey, IReadOnlyList<string> traits,
        string speciesId, string roleId, bool otherworlder, bool someoneElse)
    {
        WantDef? nature = Pick(content, truths, natureKey, 0, traits, speciesId, roleId, otherworlder, aboutSomeone: true);
        if (nature == null || someoneElse || nature.Target != "person") return nature;
        return Pick(content, truths, natureKey, 1, traits, speciesId, roleId, otherworlder, aboutSomeone: false);
    }

    /// <summary>A weighted pick over wants.json in file order (leaving out wants about a person unless <paramref name="aboutSomeone"/>), rolled with <see cref="TruthId.InitialWant"/> at <paramref name="index"/>.</summary>
    private static WantDef? Pick(ContentDb content, TruthRng truths, ulong natureKey, uint index, IReadOnlyList<string> traits,
        string speciesId, string roleId, bool otherworlder, bool aboutSomeone)
    {
        int total = 0;
        foreach (WantDef d in content.Wants)
            if (aboutSomeone || d.Target != "person") total += Weight(d, traits, speciesId, roleId, otherworlder);
        if (total <= 0) return null;
        int roll = truths.Range(TruthId.InitialWant, natureKey, index, 0, total);
        foreach (WantDef d in content.Wants)
        {
            if (!aboutSomeone && d.Target == "person") continue;
            int weight = Weight(d, traits, speciesId, roleId, otherworlder);
            if (roll < weight) return d;
            roll -= weight;
        }
        return null;
    }

    /// <summary>How strongly someone like this is drawn to a want: the adds sum, and nothing below zero (ADR-0021).</summary>
    public static int Weight(WantDef d, IReadOnlyList<string> traits, string speciesId, string roleId, bool otherworlder)
    {
        WantWeights x = d.Weights;
        int sum = x.Base;
        foreach (string t in traits) sum += x.Traits.GetValueOrDefault(t);
        sum += x.Species.GetValueOrDefault(speciesId);
        sum += x.Roles.GetValueOrDefault(roleId);
        if (otherworlder) sum += x.Otherworlder;
        return Math.Max(0, sum);
    }

    /// <summary>The key a person's natural draws hash: their cast key, or their id if they have none.</summary>
    public static ulong NatureKey(Character c) => c.CastKey.Length > 0 ? Fnv1a.Hash64(c.CastKey) : (ulong)c.Id;

    /// <summary>
    /// Gives a person their natural want if they have none yet, and starts its arc. A want about a
    /// person is about whoever else is in the camp now that they get on with best (or worst) by nature
    /// (pair chemistry, a Truth); a tie is broken by <see cref="TruthId.WantTarget"/>. State only:
    /// <see cref="AnnounceWant"/> logs the beat. Returns their want, or null if nothing has weight.
    /// </summary>
    public static Want? FormWant(World w, Character c)
    {
        CharacterGrowth g = c.Growth;
        if (g.Want != null) return g.Want;
        var others = new List<Character>();
        foreach (Character m in w.Members())
            if (m.Id != c.Id) others.Add(m);
        WantDef? def = NaturalWant(w.Content, w.Rng.Truths, NatureKey(c), c.Traits, c.SpeciesId, c.RoleId, c.Otherworlder, others.Count > 0);
        if (def == null) return null;
        var want = new Want { TypeId = def.Id, TargetKind = TargetKindOf(def), Status = WantStatus.Active, FormedTick = w.Tick };
        switch (want.TargetKind)
        {
            case WantTargetKind.Person:
                want.TargetId = PickPerson(w, c, others, def.Pick == "worst_chemistry");
                break;
            case WantTargetKind.Place:
                want.TargetRef = def.Place;
                break;
            case WantTargetKind.Item:
                want.TargetRef = def.Item;
                break;
        }
        g.Want = want;
        g.Arc = new Arc { Stage = ArcStage.Want, StalledSinceTick = w.Tick };
        return want;
    }

    /// <summary>
    /// Logs the beat for a want that has formed but is not in the log yet ("Mira wants to keep the camp
    /// safe."). The cause is what brought the person into the world (a newcomer's arrival), or 0 for
    /// the camp starters, whose wants formed with the world. Returns null if there is nothing to log.
    /// </summary>
    public static SimEvent? AnnounceWant(World w, Character c, long cause)
    {
        Want? want = c.Growth.Want;
        if (want == null || want.FormedEvent != 0) return null;
        var actors = new List<int> { c.Id };
        var args = new List<EventArg> { EventArg.Char("a", c.Id) };
        switch (want.TargetKind)
        {
            case WantTargetKind.Person:
                actors.Add(want.TargetId);
                args.Add(EventArg.Char("target", want.TargetId));
                break;
            case WantTargetKind.Place:
                args.Add(EventArg.Key("target", "place." + want.TargetRef));
                break;
            case WantTargetKind.Item:
                args.Add(EventArg.Item("target", want.TargetRef));
                break;
        }
        SimEvent e = w.Log.Emit(w, SystemTag.Growth, w.Content.WantById(want.TypeId).BeatKey, actors, new[] { cause }, null, args.ToArray());
        want.FormedEvent = e.Id;
        if (c.Growth.Arc is { LastEvent: 0 } arc) arc.LastEvent = e.Id;
        return e;
    }

    /// <summary>A person enters the world (a newcomer arriving): their want forms, and its beat cites what brought them.</summary>
    public static SimEvent? EnterWorld(World w, Character c, long cause)
    {
        FormWant(w, c);
        return AnnounceWant(w, c, cause);
    }

    /// <summary>
    /// Hourly: anyone living in the camp whose want is not yet in the log gets it now. On the first tick
    /// this logs the camp starters' wants, which formed with the world; later it finds nothing, since
    /// newcomers' wants form and are logged as they arrive.
    /// </summary>
    public static void Hourly(World w)
    {
        foreach (Character c in w.Characters)
            if (c.Status == CharStatus.Member) EnterWorld(w, c, 0);
        OldAge(w);
        Fate.SoulSpan(w);
    }

    /// <summary>
    /// At dawn, anyone past their body's lifespan dies of old age (ADR-0030; the ageless have none). For a companion
    /// it is an ordinary death, with its grief; for you it is the end of the run, the only death with no return.
    /// </summary>
    public static void OldAge(World w)
    {
        WorldDef def = w.Content.World;
        if (w.Tick % def.TicksPerDay != (long)def.DawnHour * def.TicksPerHour) return;
        for (int i = 0; i < w.Characters.Count; i++)
        {
            Character c = w.Characters[i];
            if (c.Status == CharStatus.Member && c.OldAgeTick > 0 && w.Tick >= c.OldAgeTick)
                Lifecycle.Die(w, c, SystemTag.Growth, "event.growth.died_old_age", 0);
        }
    }

    /// <summary>The traits a person is born with: every one generated (their nature).</summary>
    public static List<TraitOrigin> GeneratedOrigins(IReadOnlyList<string> traits)
    {
        var list = new List<TraitOrigin>(traits.Count);
        foreach (string t in traits) list.Add(new TraitOrigin { TraitId = t, Kind = TraitOriginKind.Generated });
        return list;
    }

    /// <summary>
    /// The one way a trait changes after creation: gain one (<paramref name="from"/> null), lose one
    /// (<paramref name="to"/> null) or turn one into another, which keeps its place in the list.
    /// Growth is event-driven, never grind-driven, so this refuses a change without the event that
    /// caused it. It records the new trait's origin (mutated, with that cause and the tick), keeps the
    /// origins parallel to the traits, and logs a Growth beat that cites the cause.
    /// </summary>
    public static SimEvent ChangeTrait(World w, Character c, string? from, string? to, long cause)
    {
        RequireCause(w, cause, "A trait change");
        if (from == null && to == null) throw new ArgumentException("A trait change needs a trait to lose, a trait to gain, or both.");
        List<TraitOrigin> origins = c.Growth.TraitOrigins;
        if (!InStep(c))
            throw new InvalidOperationException($"{Who(c)}'s trait origins are out of step with their traits; every trait change must go through Growth.ChangeTrait.");
        // Every check and lookup comes before anything changes, so a refused change leaves no trace.
        TraitDef? gained = null, lost = null;
        if (to != null)
        {
            gained = w.Content.TraitById(to);
            if (c.Traits.Contains(to)) throw new ArgumentException($"{Who(c)} is already {to}.", nameof(to));
        }
        int at = -1;
        if (from != null)
        {
            at = c.Traits.IndexOf(from);
            if (at < 0) throw new ArgumentException($"{Who(c)} is not {from}.", nameof(from));
            lost = w.Content.TraitById(from);
        }

        if (to == null)
        {
            c.Traits.RemoveAt(at);
            origins.RemoveAt(at);
        }
        else
        {
            var origin = new TraitOrigin { TraitId = to, Kind = TraitOriginKind.Mutated, CauseEvent = cause, Tick = w.Tick };
            if (at >= 0)
            {
                c.Traits[at] = to;
                origins[at] = origin;
            }
            else
            {
                c.Traits.Add(to);
                origins.Add(origin);
            }
        }

        var args = new List<EventArg> { EventArg.Char("a", c.Id) };
        if (gained != null) args.Add(EventArg.Key("trait", gained.NameKey));
        if (lost != null) args.Add(EventArg.Key("from", lost.NameKey));
        string key = from == null ? "event.growth.trait_gained" : to == null ? "event.growth.trait_lost" : "event.growth.trait_changed";
        return w.Log.Emit(w, SystemTag.Growth, key, new[] { c.Id }, new[] { cause }, null, args.ToArray());
    }

    /// <summary>True when the trait origins run parallel to the traits: the same count, the same ids, in order.</summary>
    public static bool InStep(Character c)
    {
        List<TraitOrigin> origins = c.Growth.TraitOrigins;
        if (origins.Count != c.Traits.Count) return false;
        for (int i = 0; i < origins.Count; i++)
            if (origins[i].TraitId != c.Traits[i]) return false;
        return true;
    }

    /// <summary>The arc's endings: it got what it wanted, or it did not.</summary>
    public static bool IsEnding(ArcStage stage) => stage is ArcStage.Resolution or ArcStage.Failed or ArcStage.Abandoned;

    /// <summary>
    /// An arc moves one stage on (want, obstacle, turning point, resolution) or ends without what it
    /// wanted (failed, abandoned) from any open stage. It never skips, goes back, or moves after an ending.
    /// </summary>
    public static bool CanMove(ArcStage from, ArcStage to)
    {
        if (IsEnding(from)) return false;
        if (to is ArcStage.Failed or ArcStage.Abandoned) return true;
        return to == NextStage(from);
    }

    /// <summary>The stage an arc moves on to, spelled out rather than read from the enum's values, so adding a stage cannot silently change which moves are allowed.</summary>
    private static ArcStage? NextStage(ArcStage stage) => stage switch
    {
        ArcStage.Want => ArcStage.Obstacle,
        ArcStage.Obstacle => ArcStage.TurningPoint,
        ArcStage.TurningPoint => ArcStage.Resolution,
        _ => null,
    };

    /// <summary>
    /// The one way an arc moves. Refuses a move without the event that caused it, a person with no
    /// want yet, and any move <see cref="CanMove"/> forbids. Restarts the stall clock; an ending also
    /// closes the want (fulfilled, failed, abandoned). Logs a Growth beat citing the cause and the arc's
    /// previous beat, so an arc reads as one causal chain from the want forming to its end.
    /// </summary>
    public static SimEvent AdvanceArc(World w, Character c, ArcStage to, long cause)
    {
        RequireCause(w, cause, "An arc stage change");
        Want want = c.Growth.Want ?? throw new InvalidOperationException($"{Who(c)} has no want yet, so no arc to move.");
        Arc arc = c.Growth.Arc ?? throw new InvalidOperationException($"{Who(c)} has a want but no arc.");
        if (!CanMove(arc.Stage, to)) throw new ArgumentException($"An arc cannot move from {arc.Stage} to {to}.", nameof(to));
        AnnounceWant(w, c, 0);

        long previous = arc.LastEvent;
        arc.Stage = to;
        arc.StalledSinceTick = w.Tick;
        want.Status = to switch
        {
            ArcStage.Resolution => WantStatus.Fulfilled,
            ArcStage.Failed => WantStatus.Failed,
            ArcStage.Abandoned => WantStatus.Abandoned,
            _ => want.Status,
        };
        string key = to switch
        {
            ArcStage.Obstacle => "event.growth.arc_obstacle",
            ArcStage.TurningPoint => "event.growth.arc_turning_point",
            ArcStage.Resolution => "event.growth.arc_resolution",
            ArcStage.Failed => "event.growth.arc_failed",
            _ => "event.growth.arc_abandoned",
        };
        var actors = new List<int> { c.Id };
        if (want.TargetKind == WantTargetKind.Person) actors.Add(want.TargetId);
        SimEvent e = w.Log.Emit(w, SystemTag.Growth, key, actors, new[] { cause, previous }, null, EventArg.Char("a", c.Id));
        arc.LastEvent = e.Id;
        return e;
    }

    private static WantTargetKind TargetKindOf(WantDef d) => d.Target switch
    {
        "person" => WantTargetKind.Person,
        "place" => WantTargetKind.Place,
        "item" => WantTargetKind.Item,
        _ => WantTargetKind.None,
    };

    /// <summary>Of the others in the camp (in id order), the one with the best or worst chemistry; ties broken by a Truth.</summary>
    private static int PickPerson(World w, Character c, List<Character> others, bool worst)
    {
        int best = int.MinValue;
        var tied = new List<Character>();
        foreach (Character o in others)
        {
            int score = worst ? -Relations.Chemistry(w, c, o) : Relations.Chemistry(w, c, o);
            if (score > best)
            {
                best = score;
                tied.Clear();
            }
            if (score == best) tied.Add(o);
        }
        if (tied.Count == 1) return tied[0].Id;
        return tied[w.Rng.Truths.Range(TruthId.WantTarget, NatureKey(c), 0, 0, tied.Count)].Id;
    }

    private static void RequireCause(World w, long cause, string what)
    {
        if (cause <= 0)
            throw new ArgumentException($"{what} needs the id of the event that caused it: growth is event-driven, never grind-driven (fixed decision 12, ADR-0021).", nameof(cause));
        if (w.Log.Get(cause) == null)
            throw new ArgumentException($"{what} cites event {cause}, which is not in the log.", nameof(cause));
    }

    private static string Who(Character c) => c.CastKey.Length > 0 ? c.CastKey : "character " + c.Id;
}
