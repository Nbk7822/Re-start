using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems;

/// <summary>
/// Conversations between neighbours (every few in-game minutes) and the hourly upkeep of promises,
/// grudges and departures. Tuning constants live here for M0 (ADR-0013).
/// </summary>
public static class SocialSystem
{
    public const int InitiatePermille = 25;
    public const int SeekingCompanyPermille = 400;
    public const int NeighbourRange = 6;
    public const int BaseInsultPermille = 30;
    public const int LowMoodInsultPermille = 250;
    public const int LowMoodBelow = 25;
    public const int GrudgeInsultPermille = 300;
    public const int DislikeInsultPermillePerPoint = 4;
    public const int ClashInsultPermille = 60;
    public const int DeepTalkPermille = 300;
    public const int GrudgeAtOpinion = -40;
    public const int LeaveAtOpinion = -45;
    public const int StarvingLeaveHours = 24;
    public const int NewcomerDays = 7;
    public const int NewcomerLeaveSlack = 30;
    public const int StarvingLeaveMood = 20;

    public static void Step(World w)
    {
        foreach (Character c in w.Characters)
        {
            if (!CanTalk(c)) continue;
            DiceStream dice = w.Rng.Dice[DiceSystem.Social];
            int initiate = c.Job?.Kind == JobKind.Socialize ? SeekingCompanyPermille : InitiatePermille;
            if (!dice.Chance(DiceId.SocialInitiate, initiate)) continue;
            Character? partner = Nearest(w, c);
            if (partner == null) continue;
            if (Culture.Greetings.FirstMeeting(w, c, partner)) continue; // a breached greeting ends the talk (ADR-0023)

            Grudge? grudge = Relations.ActiveGrudge(w, c.Id, partner.Id);
            int chemistry = Relations.Chemistry(w, c, partner);
            int dislike = Math.Max(0, -Relations.Opinion(w, c.Id, partner.Id));
            int insult = BaseInsultPermille + Traits.Sum(w, c, t => t.InsultPermilleAdd) + dislike * DislikeInsultPermillePerPoint
                + Math.Max(0, -chemistry) * ClashInsultPermille
                + (c.Mood < LowMoodBelow ? LowMoodInsultPermille : 0) + (grudge != null ? GrudgeInsultPermille : 0)
                + Culture.Ways.StanceInsult(w, c, partner);
            if (dice.Chance(DiceId.SocialInsult, Math.Clamp(insult, 0, 1000)))
            {
                if (Places.Species(w, c).CannotLie) Courtesy.HardTruth(w, c, partner); // the fae cannot sneer; they say it plainly (ADR-0022)
                else Insult(w, c, partner);
                continue;
            }
            bool close = Relations.Opinion(w, c.Id, partner.Id) >= 30 && c.Mood >= 50 && partner.Mood >= 50;
            if (close && dice.Chance(DiceId.SocialDeepTalk, DeepTalkPermille))
            {
                MoodSystem.AddThought(w, c, "deep_talk", partner.Id, 0);
                MoodSystem.AddThought(w, partner, "deep_talk", c.Id, 0);
                c.Social = Math.Min(1000, c.Social + 250);
                partner.Social = Math.Min(1000, partner.Social + 250);
                Courtesy.Courteous(w, c, deep: true);
                Courtesy.Courteous(w, partner, deep: true);
            }
            else
            {
                // Warmth is directional: kinship, strangeness and how each one's people see the other's (ADR-0023),
                // dimmed while the living are still uneasy around the dead (ADR-0022).
                int fromC = Unease.WarmthPct(w, c, partner), fromP = Unease.WarmthPct(w, partner, c);
                MoodSystem.AddThought(w, c, "chatted", partner.Id, 0, Culture.Greetings.Warmth(w, c, partner, chemistry) * fromC / 100);
                MoodSystem.AddThought(w, partner, "chatted", c.Id, 0, Culture.Greetings.Warmth(w, partner, c, chemistry) * fromP / 100);
                Unease.Feel(w, c, partner, fromC);
                Unease.Feel(w, partner, c, fromP);
                Courtesy.Courteous(w, c, deep: false);
                Courtesy.Courteous(w, partner, deep: false);
                c.Social = Math.Min(1000, c.Social + 150);
                partner.Social = Math.Min(1000, partner.Social + 150);
            }
        }
    }

    /// <summary>
    /// actor insults target. The event cites only what made the insult likelier: a grudge actor holds
    /// against target; else, if actor's mood is low enough to count, the event behind their worst
    /// thought; else the event behind their worst thought about target. Otherwise it cites nothing.
    /// If target's opinion falls far enough, target forms a grudge.
    /// </summary>
    public static SimEvent Insult(World w, Character actor, Character target)
    {
        Grudge? grudge = Relations.ActiveGrudge(w, actor.Id, target.Id);
        long cause = grudge?.FormedEvent ?? WorstThoughtCause(w, actor, actor.Mood < LowMoodBelow ? 0 : target.Id);
        SimEvent e = w.Log.Emit(w, SystemTag.Social, "event.social.insulted", new[] { actor.Id, target.Id }, new[] { cause }, null,
            EventArg.Char("a", actor.Id), EventArg.Char("b", target.Id));
        MoodSystem.AddThought(w, target, "insulted", actor.Id, e.Id, Traits.ScalePct(w, target, t => t.InsultOpinionScalePct));
        target.Social = Math.Max(0, target.Social - 50);
        Courtesy.Slighted(w, target, e);
        if (Relations.Opinion(w, target.Id, actor.Id) <= GrudgeAtOpinion && Relations.ActiveGrudge(w, target.Id, actor.Id) == null)
            Relations.FormGrudge(w, target, actor, 30, e.Id);
        return e;
    }

    public static void Hourly(World w)
    {
        WorldDef def = w.Content.World;
        CheckPromises(w);
        if (Calendar.IsDayStart(w.Tick, def)) DailyDecay(w);
        CheckDepartures(w);
    }

    private static void CheckPromises(World w)
    {
        foreach (Debt d in w.Debts)
        {
            if (d.Status != DebtStatus.Open || !d.IsPromise || d.DueTick == 0 || d.DueTick > w.Tick) continue;
            Character? creditor = w.Find(d.CreditorId), debtor = w.Find(d.DebtorId);
            if (creditor == null || debtor == null || creditor.Status != CharStatus.Member || debtor.Status != CharStatus.Member)
            {
                d.Status = DebtStatus.Broken;
                continue;
            }
            long reason = d.Kind == DebtKind.PromiseMeal ? creditor.LastFoodEvent : creditor.LastRestEvent;
            Relations.Break(w, d, reason);
        }
    }

    private static void DailyDecay(World w)
    {
        foreach (Grudge g in w.Grudges)
        {
            if (!g.Active) continue;
            Character? holder = w.Find(g.HolderId);
            if (holder == null || holder.Status != CharStatus.Member) continue;
            int every = Places.Species(w, holder).GrudgeDecayEveryDays; // the fae remember slights long (ADR-0022)
            if (every > 1 && Calendar.Day(w.Tick, w.Content.World) % every != 0) continue;
            int decay = holder != null && Traits.Any(w, holder, t => t.SharesFood) ? 2 : 1;
            g.Intensity -= decay;
            if (g.Intensity <= 0) Relations.EndGrudge(w, g);
        }
        foreach (Character c in w.Characters)
            foreach (Opinion o in c.Opinions)
                if (o.Value > 0) o.Value--;
                else if (o.Value < 0) o.Value++;
    }

    private static void CheckDepartures(World w)
    {
        Character? leader = w.Find(w.PlayerId) is { Status: CharStatus.Member } p ? p : null;

        foreach (Character c in w.Characters)
        {
            if (c.Status != CharStatus.Member || c.Id == w.PlayerId || c.Downed) continue;
            long cause = 0;
            bool newcomer = c.ArrivedTick > 0 && w.Tick - c.ArrivedTick < (long)NewcomerDays * w.Content.World.TicksPerDay;
            int leaveAt = LeaveAtOpinion + (newcomer ? NewcomerLeaveSlack : 0);
            if (leader != null && Relations.Opinion(w, c.Id, leader.Id) <= leaveAt)
            {
                Grudge? grudge = Relations.ActiveGrudge(w, c.Id, leader.Id);
                if (grudge != null) cause = grudge.FormedEvent;
                else if (c.HungerStage >= 1) cause = c.LastFoodEvent;
            }
            if (cause == 0 && c.HungerStage >= 2 && c.Mood < StarvingLeaveMood
                && w.Tick - c.StarvingSince >= (long)StarvingLeaveHours * w.Content.World.TicksPerHour)
                cause = c.LastFoodEvent;
            if (cause == 0 && c.Mood < 20)
            {
                Grudge? first = null;
                int count = 0;
                foreach (Grudge g in w.Grudges)
                    if (g.Active && g.HolderId == c.Id) { count++; first ??= g; }
                if (count >= 2 && first != null) cause = first.FormedEvent;
            }
            if (cause == 0 && c.NatureNeeds.Count > 0) cause = NatureNeeds.DepartureCause(w, c);
            if (cause != 0) Leave(w, c, cause);
        }
    }

    public static SimEvent Leave(World w, Character c, long cause)
    {
        c.Status = CharStatus.Left;
        c.Path.Clear();
        c.Job = null;
        c.Asleep = false;
        c.Carrying = null;
        w.Reservations.RemoveAll(r => r.CharacterId == c.Id);
        Relations.DropGrudgesHeldBy(w, c);
        Building.LetGo(w, c); // their bed is left unclaimed (ADR-0027)
        return w.Log.Emit(w, SystemTag.Social, "event.social.left_camp", new[] { c.Id }, new[] { cause, c.HungerStage >= 1 ? c.LastFoodEvent : 0 },
            new[] { new Effect { Kind = EffectKind.Departure, A = c.Id } }, EventArg.Char("a", c.Id));
    }

    private static bool CanTalk(Character c) => c.Status == CharStatus.Member && !c.Asleep && !c.Downed;

    private static Character? Nearest(World w, Character c)
    {
        Character? best = null;
        int bestDist = int.MaxValue;
        foreach (Character o in w.Characters)
        {
            if (o.Id == c.Id || !CanTalk(o)) continue;
            int d = c.Pos.ChebyshevTo(o.Pos);
            if (d > NeighbourRange || d >= bestDist) continue;
            best = o;
            bestDist = d;
        }
        return best;
    }

    /// <summary>The event behind c's most negative thought (only thoughts about aboutId, unless it is 0); 0 if none.</summary>
    private static long WorstThoughtCause(World w, Character c, int aboutId)
    {
        int worst = 0;
        long cause = 0;
        foreach (Thought t in c.Thoughts)
        {
            if (aboutId != 0 && t.AboutId != aboutId) continue;
            int mood = w.Content.ThoughtById(t.DefId).Mood;
            if (mood < worst && t.CauseEvent != 0)
            {
                worst = mood;
                cause = t.CauseEvent;
            }
        }
        return cause;
    }
}
