using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;
using Return.Sim.Systems.Jobs;

namespace Return.Sim.Systems;

/// <summary>
/// The night watch, kept in turns (ADR-0029; Schedule design 4.3–4.4). The player sets only the hours; people
/// do the rest by fixed rules. One person holds the post at a time. When a turn ends the watcher asks the next:
/// someone awake walks over and takes it; a sleeper must be walked to and woken, and decides. They may refuse for
/// their body (hurt, tired, hungry), a grudge against whoever woke them, or by the one compliance rule (their own
/// way; DiceId.WatchDefy, never the player). Whoever keeps a turn someone refused for a good reason keeps it in
/// their place, which repays or opens a debt. With nobody left to ask, the Oathbound stay on and the Protective
/// cover for the hurt or weary, once; anyone else leaves the post empty. An empty post always has a reason
/// (GapEvent), and a thief who finds it cites it. The undead whose purpose is the watch are asked first and keep
/// the post until their purpose is met; while a festival runs nobody keeps watch, and the festival is the reason.
/// The only draw is the compliance roll. Runs every 30 ticks, before WorkSystem, which never ends a watch itself.
/// </summary>
public static class WatchSystem
{
    public const int StepEveryTicks = 30;

    /// <summary>Below this rest a watcher hands over before the turn ends (not the Oathbound, nor during an extra turn).</summary>
    public const int HandOverRestBelow = 120;

    /// <summary>The grudge a refusal or a forced cover leaves once opinion sinks to SocialSystem.GrudgeAtOpinion.</summary>
    public const int SourGrudgeIntensity = 25;

    public const string WorkId = "keep_watch";

    public static void Step(World w)
    {
        WatchState s = w.Watch;
        bool inHours = ScheduleSystem.InWatchHours(w, Calendar.Hour(w.Tick, w.Content.World));
        Reconcile(w, s, inHours);
        if (!inHours)
        {
            if (s.Active) EndNight(w, s);
            return;
        }
        if (!s.Active) BeginNight(w, s);
        if (Culture.Festivals.RunsAt(w, w.Tick))
        {
            Festival(w, s);
            return;
        }
        if (s.WatcherId != 0) Keep(w, s, w.Get(s.WatcherId));
        if (s.WatcherId == 0 && s.IncomingId == 0 && s.WakerId == 0) EmptyPost(w, s);
    }

    /// <summary>Is this person on watch duty: at the post, on the way to it, or walking to wake the next?</summary>
    public static bool OnDuty(World w, Character c) =>
        c.Id != 0 && (c.Id == w.Watch.WatcherId || c.Id == w.Watch.IncomingId || c.Id == w.Watch.WakerId
                      || c.Job is { Kind: JobKind.KeepWatch or JobKind.WakeForWatch });

    /// <summary>A KeepWatch job reached the post: the person on the way takes the turn (or keeps it in someone's place).</summary>
    public static void ReachedPost(World w, Character c)
    {
        WatchState s = w.Watch;
        if (s.IncomingId != c.Id) return; // back at the post after staying on or a fight
        if (s.WatcherId != 0 && s.WatcherId != c.Id && w.Find(s.WatcherId) is { } old) OffDuty(w, old);
        WatchRefusal? owed = Owed(w, s);
        SimEvent e = owed != null
            ? Cover(w, c, owed, s.IncomingCause)
            : w.Log.Emit(w, SystemTag.Work, "event.work.took_watch", new[] { c.Id }, new[] { s.IncomingCause }, null, EventArg.Char("a", c.Id));
        CoveredFor(w, s, c);
        s.WatcherId = c.Id;
        s.IncomingId = 0;
        s.IncomingCause = 0;
        s.TurnEvent = e.Id;
        s.TurnEndsTick = w.Tick + TurnTicks(w);
        s.Extended = false;
        s.Refusals.Clear();
        s.Tries = 0;
        s.GapEvent = 0;
        s.EmptySinceTick = -1;
        s.UnkeptSaid = false;
        s.FirstTurnTaken = true;
        c.LastDutyEvent = e.Id;
    }

    /// <summary>A watcher reached the sleeper they went to wake: wake them and hear their answer, then ask the next if they refuse.</summary>
    public static void Ask(World w, Character waker, Character sleeper)
    {
        WatchState s = w.Watch;
        if (s.WakerId != waker.Id || s.WakeTargetId != sleeper.Id)
        {
            OffDuty(w, waker);
            return;
        }
        bool asleep = sleeper.Asleep && sleeper.Job is { Kind: JobKind.Sleep };
        if (!asleep && !Idle(sleeper))
        {
            // Up and busy by the time the waker came: not asked, and no try spent on them.
            s.Tries--;
            AskNext(w, s, waker);
            return;
        }
        if (asleep)
        {
            WorkSystem.Drop(w, sleeper);
            MoodSystem.AddThought(w, sleeper, "woken", 0, s.WakeEvent);
        }
        Reason? no = Refusal(w, sleeper, waker, draw: true);
        if (no == null)
        {
            s.WakerId = 0;
            s.WakeTargetId = 0;
            SendToPost(w, s, sleeper, s.WakeEvent);
            OffDuty(w, waker);
            return;
        }
        Refuse(w, s, waker, sleeper, no.Value);
        AskNext(w, s, waker);
    }

    /// <summary>Why someone will not take the watch: its line, the event it cites, and whether it is a reason to sympathise with.</summary>
    public readonly record struct Reason(string Key, long Cause, bool Sympathetic);

    /// <summary>
    /// Null if they will take the watch; otherwise the first reason that applies (design 4.4): hurt, tired, hungry,
    /// a grudge against whoever asks, and, only on a real waking (<paramref name="draw"/>), their own way by the one
    /// compliance rule. The first four are pure; the last draws once on the Work stream.
    /// </summary>
    public static Reason? Refusal(World w, Character c, Character? waker, bool draw)
    {
        int hurtBelow = Math.Max(400, Math.Max(Traits.Max(w, c, t => t.FleeHealthBelow), Traits.Max(w, c, t => t.RiskyWorkHealthBelow)));
        if (c.Health < hurtBelow) return new Reason("event.social.watch_refused_hurt", c.LastHealthEvent, true);
        if (Places.Tires(w, c) && c.Rest < Math.Max(150, Traits.Max(w, c, t => t.RiskyWorkRestBelow)))
            return new Reason("event.social.watch_refused_tired", c.WornOutEvent != 0 ? c.WornOutEvent : c.LastDutyEvent != 0 ? c.LastDutyEvent : c.LastRestEvent, true);
        if (!Places.Species(w, c).EatsNothing && (c.Food < NeedsSystem.StarvingBelow || c.Food < Traits.Max(w, c, t => t.StealFoodBelow)))
            return new Reason("event.social.watch_refused_hungry", c.LastFoodEvent, true);
        if (waker != null && Relations.ActiveGrudge(w, c.Id, waker.Id) is { } grudge)
            return new Reason("event.social.watch_refused_grudge", grudge.FormedEvent, false);
        if (!draw) return null;
        string? why = Compliance.Defies(w, c, w.PlayerId, DiceId.WatchDefy);
        return why == null ? null : new Reason(OwnWayKey(why), Compliance.ReasonCause(w, c, w.PlayerId, why), false);
    }

    /// <summary>The line for refusing a waking by one's own way, by Compliance's reason.</summary>
    public static string OwnWayKey(string why) => why switch
    {
        Compliance.Dislike => "event.social.watch_refused_dislike",
        Compliance.Mood => "event.social.watch_refused_mood",
        Compliance.Ways => "event.social.watch_refused_ways",
        _ => "event.social.watch_refused_own_way",
    };

    private static bool IsOwnWay(string key) => key is "event.social.watch_refused_own_way" or "event.social.watch_refused_dislike"
        or "event.social.watch_refused_mood" or "event.social.watch_refused_ways";

    // ---- The night ----

    /// <summary>The watch hours begin. Someone already at the post (a restored world, a mod) keeps it; otherwise the post starts empty.</summary>
    private static void BeginNight(World w, WatchState s)
    {
        if (s.WatcherId == 0) Reset(s);
        s.Active = true;
        if (s.WatcherId == 0) s.EmptySinceTick = w.Tick;
    }

    /// <summary>The watch hours are over: the turn ends quietly, and the night's refusals and gaps are forgotten.</summary>
    private static void EndNight(World w, WatchState s)
    {
        foreach (int id in new[] { s.WatcherId, s.IncomingId, s.WakerId })
            if (id != 0 && w.Find(id) is { } c) OffDuty(w, c);
        Reset(s);
        s.Active = false;
    }

    private static void Reset(WatchState s)
    {
        s.WatcherId = s.IncomingId = s.WakerId = s.WakeTargetId = s.Tries = 0;
        s.IncomingCause = s.WakeEvent = s.TurnEvent = s.TurnEndsTick = s.GapEvent = 0;
        s.Refusals.Clear();
        s.EmptySinceTick = -1;
        s.UnkeptSaid = s.FirstTurnTaken = s.Extended = false;
    }

    /// <summary>
    /// While a festival runs nobody keeps watch: everyone is at the fire. A watcher ends the turn quietly, nobody is
    /// asked, and the festival's beginning is why the post stands empty (culture-faith design; Schedule design 4.3).
    /// </summary>
    private static void Festival(World w, WatchState s)
    {
        foreach (int id in new[] { s.WatcherId, s.IncomingId, s.WakerId })
            if (id != 0 && w.Find(id) is { } c) OffDuty(w, c);
        s.WatcherId = s.IncomingId = s.WakerId = s.WakeTargetId = s.Tries = 0;
        s.Refusals.Clear();
        s.Extended = false;
        if (w.Culture.Festivals.Count > 0) s.GapEvent = w.Culture.Festivals[0].StartEvent;
        s.EmptySinceTick = w.Tick;
    }

    /// <summary>
    /// Keeps the record in step with what people are doing: a watcher who fell, left or went off is gone from the post
    /// (one drawn into a fight comes back to it); someone on the way who no longer is, is not; and someone holding the
    /// post the system does not know of (a test, a mod) is taken as the watcher, or let go outside the watch hours.
    /// </summary>
    private static void Reconcile(World w, WatchState s, bool inHours)
    {
        if (s.WatcherId != 0)
        {
            Character? c = w.Find(s.WatcherId);
            if (c is not { Status: CharStatus.Member } || c.Downed || (c.Health < JobPicker.InjuredBelow && c.Job is not { Kind: JobKind.Fight or JobKind.Flee }))
            {
                // Struck down or wounded at the post: that is why it is empty, and they leave it to lie down.
                if (c is { Status: CharStatus.Member })
                {
                    s.GapEvent = c.LastHealthEvent;
                    OffDuty(w, c);
                }
                if (c != null) c.LastWatchEndTick = w.Tick;
                Vacate(w, s);
            }
            else if (c.Job is { Kind: JobKind.KeepWatch or JobKind.Fight or JobKind.Flee }) { }
            else if (c.Job is { Kind: JobKind.Care or JobKind.Rescue } help)
            {
                // Gone from the fight to someone who fell: the fallen are why the post is empty.
                s.GapEvent = help.CauseEvent;
                c.LastWatchEndTick = w.Tick;
                Vacate(w, s);
            }
            else if (inHours && !c.Asleep && c.Job is not { Kind: JobKind.Eat or JobKind.RequestBlood or JobKind.SeekCover })
                Post(w, c, s.TurnEvent); // back from a fight with the turn not done: to the post again, quietly
            else
            {
                c.LastWatchEndTick = w.Tick;
                Vacate(w, s);
            }
        }
        if (s.IncomingId != 0 && w.Find(s.IncomingId) is not { Status: CharStatus.Member, Downed: false, Job.Kind: JobKind.KeepWatch })
        {
            s.IncomingId = 0;
            s.IncomingCause = 0;
        }
        if (s.WakerId != 0 && w.Find(s.WakerId) is not { Status: CharStatus.Member, Downed: false, Job.Kind: JobKind.WakeForWatch })
        {
            s.WakerId = 0;
            s.WakeTargetId = 0;
        }
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member || c.Job is not { Kind: JobKind.KeepWatch } || c.Id == s.WatcherId || c.Id == s.IncomingId) continue;
            if (!inHours)
            {
                OffDuty(w, c);
                continue;
            }
            if (s.WatcherId != 0) continue;
            s.WatcherId = c.Id;
            s.TurnEndsTick = w.Tick + TurnTicks(w);
            s.TurnEvent = c.Job.CauseEvent;
            s.EmptySinceTick = -1;
        }
    }

    private static void Vacate(World w, WatchState s)
    {
        s.WatcherId = 0;
        if (s.EmptySinceTick < 0) s.EmptySinceTick = w.Tick;
    }

    // ---- The watcher at the post ----

    private static void Keep(World w, WatchState s, Character watcher)
    {
        if (watcher.Job is not { Kind: JobKind.KeepWatch }) return; // drawn into a fight: nothing to decide until they are back
        if (Hungry(w, watcher))
        {
            SimEvent e = w.Log.Emit(w, SystemTag.Work, "event.work.left_watch_hungry", new[] { watcher.Id }, new[] { watcher.LastFoodEvent }, null,
                EventArg.Char("a", watcher.Id));
            s.GapEvent = e.Id;
            OffDuty(w, watcher);
            Vacate(w, s);
            return;
        }
        if (Places.BurnsInDaylight(w, watcher) && Places.SunDanger(w))
        {
            // Those who burn go under cover before the sun; quietly, as at the end of the watch hours (lead's call).
            OffDuty(w, watcher);
            Vacate(w, s);
            return;
        }
        if (Places.Tires(w, watcher) && watcher.Rest < AsleepBelow(w, watcher))
        {
            FallAsleep(w, s, watcher);
            return;
        }
        bool over = w.Tick >= s.TurnEndsTick;
        bool weary = !s.Extended && Places.Tires(w, watcher) && watcher.Rest < HandOverRestBelow && !Oathbound(w, watcher);
        if (!over && !weary) return;
        if (over && Purpose.Seeks(w, watcher, WorkId))
        {
            // Their purpose is the watch and it is not yet met: they keep the post, quietly.
            s.TurnEndsTick = w.Tick + TurnTicks(w);
            return;
        }
        if (TicksLeft(w) < w.Content.World.TicksPerHour || s.IncomingId != 0 || s.WakerId != 0) return; // no handover in the last hour
        s.Tries = 0;
        AskNext(w, s, watcher);
    }

    /// <summary>Asks the next person for the watch: someone awake comes, a sleeper is walked to; with nobody left, the asker stays or leaves.</summary>
    private static void AskNext(World w, WatchState s, Character asker)
    {
        foreach (Character next in Candidates(w, s, asker, awakeOnly: false))
        {
            if (!next.Asleep)
            {
                if (!SendToPost(w, s, next, s.Refusals.Count > 0 ? s.Refusals[^1].Event : s.TurnEvent)) continue;
                if (s.WatcherId != asker.Id)
                {
                    // A waker who found someone awake is done; a watcher at the post waits until they arrive.
                    s.WakerId = 0;
                    s.WakeTargetId = 0;
                    OffDuty(w, asker);
                }
                return;
            }
            if (s.Tries >= w.Content.World.Schedule.WakeTries) break;
            WalkToWake(w, s, asker, next);
            return;
        }
        NobodyLeft(w, s, asker);
    }

    private static void WalkToWake(World w, WatchState s, Character asker, Character sleeper)
    {
        SimEvent e = w.Log.Emit(w, SystemTag.Work, "event.work.went_to_wake", new[] { asker.Id, sleeper.Id },
            new[] { s.Refusals.Count > 0 ? s.Refusals[^1].Event : s.TurnEvent }, null,
            EventArg.Char("a", asker.Id), EventArg.Char("b", sleeper.Id));
        if (s.WatcherId == asker.Id) Vacate(w, s);
        s.GapEvent = e.Id; // the post stands empty while they walk: a thief who comes now cites this
        s.WakeEvent = e.Id;
        s.WakerId = asker.Id;
        s.WakeTargetId = sleeper.Id;
        s.Tries++;
        asker.Path.Clear();
        asker.MoveTicks = 0;
        asker.Job = new Job { Kind = JobKind.WakeForWatch, TargetId = sleeper.Id, Target = sleeper.Pos, StartedTick = w.Tick, CauseEvent = e.Id };
    }

    private static void Refuse(World w, WatchState s, Character waker, Character refuser, Reason no)
    {
        SimEvent e = w.Log.Emit(w, SystemTag.Social, no.Key, new[] { waker.Id, refuser.Id }, new[] { s.WakeEvent, no.Cause }, null,
            EventArg.Char("a", waker.Id), EventArg.Char("b", refuser.Id));
        MoodSystem.AddThought(w, refuser, "refused_someone", waker.Id, e.Id);
        if (!no.Sympathetic)
        {
            MoodSystem.AddThought(w, waker, "watch_refused", refuser.Id, e.Id);
            Sour(w, waker, refuser, e.Id);
        }
        if (IsOwnWay(no.Key)) Compliance.Witness(w, refuser, e, waker.Id);
        s.Refusals.Add(new WatchRefusal { RefuserId = refuser.Id, Event = e.Id, Sympathetic = no.Sympathetic });
    }

    /// <summary>Nobody left to ask: the Oathbound stay on, and the Protective keep a hurt or weary refuser's turn, once; anyone else leaves.</summary>
    private static void NobodyLeft(World w, WatchState s, Character asker)
    {
        WatchRefusal? owed = Owed(w, s);
        bool stays = !s.Extended && (Oathbound(w, asker) || (owed != null && Traits.Any(w, asker, t => t.FightsThieves)));
        s.WakerId = 0;
        s.WakeTargetId = 0;
        if (!stays)
        {
            SimEvent left = w.Log.Emit(w, SystemTag.Work, "event.work.left_watch_unrelieved", new[] { asker.Id },
                new[] { s.Refusals.Count > 0 ? s.Refusals[^1].Event : s.TurnEvent }, null, EventArg.Char("a", asker.Id));
            s.GapEvent = left.Id;
            OffDuty(w, asker);
            if (s.WatcherId == asker.Id) Vacate(w, s);
            return;
        }
        SimEvent e = owed != null
            ? Cover(w, asker, owed, 0)
            : w.Log.Emit(w, SystemTag.Work, "event.work.stayed_on_watch", new[] { asker.Id },
                new[] { s.Refusals.Count > 0 ? s.Refusals[^1].Event : s.TurnEvent }, null, EventArg.Char("a", asker.Id));
        CoveredFor(w, s, asker);
        // An extra turn, not a new one: those who refused tonight are not woken twice.
        s.Extended = true;
        s.TurnEndsTick = w.Tick + TurnTicks(w);
        s.TurnEvent = e.Id;
        s.Tries = 0;
        s.GapEvent = 0;
        s.WatcherId = asker.Id;
        s.EmptySinceTick = -1;
        asker.LastDutyEvent = e.Id;
        if (asker.Job is not { Kind: JobKind.KeepWatch }) Post(w, asker, e.Id);
    }

    /// <summary>"{a} kept the watch in {b}'s place": repays what the keeper owed the refuser, or the refuser now owes the keeper (DebtKind.Watch).</summary>
    private static SimEvent Cover(World w, Character keeper, WatchRefusal owed, long alsoCause)
    {
        Character refuser = w.Get(owed.RefuserId);
        SimEvent e = w.Log.Emit(w, SystemTag.Work, "event.work.covered_watch", new[] { keeper.Id, refuser.Id }, new[] { owed.Event, alsoCause }, null,
            EventArg.Char("a", keeper.Id), EventArg.Char("b", refuser.Id));
        owed.Settled = true;
        CareJobs.SettleOrOwe(w, helper: keeper, helped: refuser, DebtKind.Watch, e.Id);
        MoodSystem.AddThought(w, refuser, "watch_taken_for_me", keeper.Id, e.Id);
        return e;
    }

    /// <summary>Whoever keeps a turn someone refused for no good reason holds it against them (covered_for), and a grudge forms once opinion sinks low enough.</summary>
    private static void CoveredFor(World w, WatchState s, Character keeper)
    {
        foreach (WatchRefusal r in s.Refusals)
        {
            if (r.Sympathetic || r.Settled || r.RefuserId == keeper.Id || w.Find(r.RefuserId) is not { } refuser) continue;
            r.Settled = true;
            MoodSystem.AddThought(w, keeper, "covered_for", refuser.Id, r.Event);
            Sour(w, keeper, refuser, r.Event);
        }
    }

    /// <summary>The first refusal tonight for a reason to sympathise with, whose refuser is still here and not yet covered for.</summary>
    private static WatchRefusal? Owed(World w, WatchState s)
    {
        foreach (WatchRefusal r in s.Refusals)
            if (r.Sympathetic && !r.Settled && w.Find(r.RefuserId) is { Status: CharStatus.Member }) return r;
        return null;
    }

    private static void Sour(World w, Character holder, Character target, long cause)
    {
        if (Relations.Opinion(w, holder.Id, target.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, holder.Id, target.Id) == null)
            Relations.FormGrudge(w, holder, target, SourGrudgeIntensity, cause);
    }

    private static void FallAsleep(World w, WatchState s, Character watcher)
    {
        SimEvent e = w.Log.Emit(w, SystemTag.Needs, "event.needs.fell_asleep_on_watch", new[] { watcher.Id },
            new[] { watcher.WornOutEvent != 0 ? watcher.WornOutEvent : watcher.LastDutyEvent }, null, EventArg.Char("a", watcher.Id));
        s.GapEvent = e.Id;
        watcher.LastWatchEndTick = w.Tick;
        watcher.LastRestEvent = e.Id;
        watcher.Path.Clear();
        watcher.Job = new Job { Kind = JobKind.Sleep, Target = watcher.Pos, Stage = 1, StartedTick = w.Tick, CauseEvent = e.Id };
        watcher.Asleep = true;
        Vacate(w, s);
    }

    // ---- The empty post ----

    /// <summary>
    /// The post stands empty and nobody is on the way: the most eligible awake, idle person takes it, unasked (no
    /// refusal, no roll), but never someone who already kept a turn tonight (lead's call: a watcher who left an
    /// unrelieved post is not sent back to it). After a whole watch hour with nobody there and nothing yet to explain
    /// it: "Nobody kept the watch."
    /// </summary>
    private static void EmptyPost(World w, WatchState s)
    {
        long cause = !s.FirstTurnTaken && w.WatchEvent != 0 ? w.WatchEvent : s.GapEvent;
        foreach (Character c in Candidates(w, s, null, awakeOnly: true, offWatchSince: NightStart(w)))
            if (SendToPost(w, s, c, cause)) return;
        if (s.GapEvent != 0 || s.UnkeptSaid || s.EmptySinceTick < 0 || w.Tick - s.EmptySinceTick < w.Content.World.TicksPerHour) return;
        SimEvent e = w.Log.Emit(w, SystemTag.Work, "event.work.watch_unkept", Array.Empty<int>(),
            new[] { s.Refusals.Count > 0 ? s.Refusals[^1].Event : 0 }, null);
        s.GapEvent = e.Id;
        s.UnkeptSaid = true;
    }

    /// <summary>
    /// Who may be asked, in order (design 4.3): awake before asleep; those their nature calls to the watch; keep_watch
    /// priority, 1 first; longest since their last watch; whoever the asker thinks least of (only among the equally
    /// due); id. Never the downed, anyone who refused tonight, the asker, those the sun keeps in, or those who never
    /// keep watch. An awake person in no state for it (hurt, tired, hungry, or bearing the asker a grudge) is not sent.
    /// </summary>
    private static List<Character> Candidates(World w, WatchState s, Character? asker, bool awakeOnly, long offWatchSince = long.MaxValue)
    {
        var found = new List<(Character C, int Asleep, int Nature, int Priority, long Since, int Opinion)>();
        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member || c.Downed || c.Id == asker?.Id || c.Id == s.WatcherId || c.Id == s.IncomingId || c.Id == s.WakerId) continue;
            if (s.Refusals.Exists(r => r.RefuserId == c.Id) || (c.LastWatchEndTick != 0 && c.LastWatchEndTick >= offWatchSince)) continue;
            int priority = JobPicker.PriorityOf(c, WorkId);
            if (priority <= 0 || Places.Species(w, c).RefusesWork.Contains(WorkId)) continue;
            if (Places.BurnsInDaylight(w, c) && Places.SunDanger(w)) continue;
            bool asleep = c.Asleep && c.Job is { Kind: JobKind.Sleep };
            bool awake = !c.Asleep && Idle(c);
            if (awake ? Refusal(w, c, asker, draw: false) != null : !asleep || awakeOnly) continue;
            found.Add((c, asleep ? 1 : 0, NatureNeeds.WorkBias(w, c, WorkId) > 0 ? 0 : 1, priority, c.LastWatchEndTick,
                asker == null ? 0 : Relations.Opinion(w, asker.Id, c.Id)));
        }
        found.Sort((a, b) =>
        {
            int k = a.Asleep.CompareTo(b.Asleep);
            if (k == 0) k = a.Nature.CompareTo(b.Nature);
            if (k == 0) k = a.Priority.CompareTo(b.Priority);
            if (k == 0) k = a.Since.CompareTo(b.Since);
            if (k == 0) k = a.Opinion.CompareTo(b.Opinion);
            return k != 0 ? k : a.C.Id.CompareTo(b.C.Id);
        });
        var list = new List<Character>(found.Count);
        foreach (var f in found) list.Add(f.C);
        return list;
    }

    /// <summary>Sends someone to take the next turn (they leave whatever idle thing they were doing); false if they cannot reach the post.</summary>
    private static bool SendToPost(World w, WatchState s, Character c, long cause)
    {
        if (!Post(w, c, cause)) return false;
        s.IncomingId = c.Id;
        s.IncomingCause = cause;
        return true;
    }

    /// <summary>Gives someone the KeepWatch job and starts them toward the post.</summary>
    private static bool Post(World w, Character c, long cause)
    {
        Point post = w.Map.Passable(w.Map.WatchPost) ? w.Map.WatchPost : Places.Adjacent(w, w.Map.WatchPost, c.Pos);
        if (post == Point.None) return false;
        if (c.Job != null) WorkSystem.Drop(w, c);
        c.Job = new Job { Kind = JobKind.KeepWatch, Target = post, StartedTick = w.Tick, CauseEvent = cause };
        if (TravelSystem.Start(w, c, post)) return true;
        c.Job = null;
        return false;
    }

    /// <summary>Off watch duty: the job ends (they go to bed or about their day as JobPicker chooses) and the fair rota notes when.</summary>
    private static void OffDuty(World w, Character c)
    {
        if (c.Job is { Kind: JobKind.KeepWatch or JobKind.WakeForWatch }) WorkSystem.Drop(w, c);
        c.LastWatchEndTick = w.Tick;
    }

    private static bool Idle(Character c) => c.Job == null || c.Job.Kind is JobKind.Wander or JobKind.Socialize;

    private static bool Oathbound(World w, Character c) => Traits.Sum(w, c, t => t.PromisePriority) > 0;

    /// <summary>Starving, or a Desperate person below the mark at which they would take someone's meal; never one who eats nothing.</summary>
    private static bool Hungry(World w, Character c) =>
        !Places.Species(w, c).EatsNothing && (c.Food < NeedsSystem.StarvingBelow || c.Food < Traits.Max(w, c, t => t.StealFoodBelow));

    /// <summary>A watcher falls asleep at the post below this rest; the Oathbound hold out to half of it (lead's call: no new field).</summary>
    private static int AsleepBelow(World w, Character c)
    {
        int below = w.Content.World.Schedule.AsleepOnWatchRestBelow;
        return Oathbound(w, c) ? below / 2 : below;
    }

    private static long TurnTicks(World w) => (long)w.Content.World.Schedule.WatchTurnHours * w.Content.World.TicksPerHour;

    /// <summary>The tick tonight's watch hours began.</summary>
    private static long NightStart(World w)
    {
        WorldDef def = w.Content.World;
        long start = w.Tick / def.TicksPerDay * def.TicksPerDay + (long)w.WatchFrom * def.TicksPerHour;
        return start > w.Tick ? start - def.TicksPerDay : start;
    }

    /// <summary>Ticks until the watch hours end.</summary>
    private static long TicksLeft(World w)
    {
        WorldDef def = w.Content.World;
        int hours = (w.WatchTo - Calendar.Hour(w.Tick, def) + ScheduleSystem.HoursPerDay) % ScheduleSystem.HoursPerDay;
        return (long)hours * def.TicksPerHour - w.Tick % def.TicksPerHour;
    }
}
