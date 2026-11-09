using Return.Sim.Api;
using Return.Sim.Content;
using Return.Sim.Core;
using Return.Sim.Events;

namespace Return.Sim.Systems.Culture;

/// <summary>
/// Gods are believed in, not proven (ADR-0023). Deeds the gods care about are remembered for a while;
/// when an incident a god is said to speak through follows one, a faithful follower may read it as
/// that god's anger or thanks and think worse or better of whoever did the deed. Readings are Dice
/// (the Culture stream), so they re-roll after a rewind; the gods themselves are Truths.
/// </summary>
public static class Omens
{
    public static void RecordDeed(World w, DeedKind kind, long eventId, int doerId)
    {
        if (eventId == 0 || doerId == 0) return;
        w.Culture.Deeds.Add(new Deed { Kind = kind, EventId = eventId, DoerId = doerId, Tick = w.Tick });
    }

    /// <summary>Forgets deeds older than the deed memory, and readings older than that plus the reader's cooldown.</summary>
    public static void Prune(World w)
    {
        long ticksPerHour = w.Content.World.TicksPerHour;
        long deedHorizon = w.Tick - CultureTuning.DeedMemoryHours * ticksPerHour;
        long readHorizon = w.Tick - (CultureTuning.DeedMemoryHours + CultureTuning.OmenCooldownHours) * ticksPerHour;
        w.Culture.Deeds.RemoveAll(d => d.Tick < deedHorizon);
        w.Culture.OmenReads.RemoveAll(r => r.Tick < readHorizon);
    }

    /// <summary>The snake_case name content uses for a deed ("oath_broken").</summary>
    public static string DeedName(DeedKind kind) => Names.Snake(kind.ToString());

    /// <summary>
    /// A sign happened (rain, a windfall, spoiled food, an accident, a member's death). For every
    /// god whose domain reads this sign, every follower devout enough to read omens for that faith,
    /// and not read one lately, looks for the latest deed (by someone else still in camp) the god
    /// cares about this way; only then is a reading rolled. A reading cites the deed and the sign,
    /// and moves the reader's opinion of the doer; an angry one may set a grudge (M1, M2).
    /// </summary>
    public static void OnSign(World w, string sign, SimEvent signEvent)
    {
        if (w.Culture.Deeds.Count == 0) return;
        ContentDb db = w.Content;
        long ticksPerHour = w.Content.World.TicksPerHour;
        foreach (SeededFaith faith in w.Culture.Faiths)
        {
            FaithDef faithDef = db.FaithById(faith.TemplateId);
            foreach (SeededGod god in faith.Gods)
            {
                DomainDef domain = db.DomainById(god.DomainId);
                foreach (SignDef s in domain.Signs)
                {
                    if (s.Sign != sign) continue;
                    bool anger = s.Reads == "anger";
                    List<string> kinds = anger ? domain.AngeredBy : domain.PleasedBy;
                    foreach (Character reader in w.Characters)
                    {
                        if (reader.Status != CharStatus.Member || reader.Downed || reader.FaithId != faith.TemplateId) continue;
                        if (reader.Devotion < faithDef.OmenDevotion || ReadLately(w, reader.Id, ticksPerHour)) continue;
                        Deed? deed = LatestDeed(w, reader.Id, kinds, ticksPerHour);
                        if (deed == null) continue;
                        Read(w, reader, deed, god, sign, signEvent, anger);
                    }
                }
            }
        }
    }

    private static void Read(World w, Character reader, Deed deed, SeededGod god, string sign, SimEvent signEvent, bool anger)
    {
        Character doer = w.Get(deed.DoerId);
        int odds = reader.Devotion * (anger ? god.Wrath : god.Grace) * CultureTuning.DeedWeightPct(deed.Kind) / CultureTuning.OmenOddsDivisor;
        odds = odds * (100 + CultureTuning.ArcanePerAxisPct * Ways.Axis(w, reader, EthicAxis.ArcaneMundane)) / 100;
        odds = Math.Clamp(odds, 0, CultureTuning.OmenOddsMaxPermille);
        if (!w.Rng.Dice[DiceSystem.Culture].Chance(DiceId.OmenRead, odds)) return;
        SimEvent e = w.Log.Emit(w, SystemTag.Culture, anger ? "event.culture.omen_anger" : "event.culture.omen_blessing",
            new[] { reader.Id, doer.Id }, new[] { deed.EventId, signEvent.Id }, null,
            EventArg.Char("a", reader.Id), EventArg.Char("b", doer.Id), EventArg.Key("sign", "sign." + sign),
            EventArg.Key("god", god.NameKey), EventArg.Key("deed", "deed." + DeedName(deed.Kind)));
        w.Culture.OmenReads.Add(new OmenRead { ReaderId = reader.Id, DeedEvent = deed.EventId, Tick = w.Tick });
        MoodSystem.AddThought(w, reader, anger ? "omen_anger" : "omen_blessing", doer.Id, e.Id);
        if (anger && Relations.Opinion(w, reader.Id, doer.Id) <= SocialSystem.GrudgeAtOpinion && Relations.ActiveGrudge(w, reader.Id, doer.Id) == null)
            Relations.FormGrudge(w, reader, doer, CultureTuning.OmenGrudge, e.Id);
    }

    private static bool ReadLately(World w, int readerId, long ticksPerHour)
    {
        foreach (OmenRead r in w.Culture.OmenReads)
            if (r.ReaderId == readerId && w.Tick - r.Tick < CultureTuning.OmenCooldownHours * ticksPerHour) return true;
        return false;
    }

    /// <summary>The most recent remembered deed of one of these kinds, by a member other than the reader, that the reader has not already read.</summary>
    private static Deed? LatestDeed(World w, int readerId, List<string> kinds, long ticksPerHour)
    {
        for (int i = w.Culture.Deeds.Count - 1; i >= 0; i--)
        {
            Deed d = w.Culture.Deeds[i];
            if (w.Tick - d.Tick > CultureTuning.DeedMemoryHours * ticksPerHour) break;
            if (d.DoerId == readerId || !kinds.Contains(DeedName(d.Kind))) continue;
            if (w.Find(d.DoerId) is not { Status: CharStatus.Member }) continue;
            bool read = false;
            foreach (OmenRead r in w.Culture.OmenReads)
                if (r.ReaderId == readerId && r.DeedEvent == d.EventId) { read = true; break; }
            if (!read) return d;
        }
        return null;
    }
}
