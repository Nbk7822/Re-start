# The Schedule menu: design

- **Date:** 2026-11-09. **Status:** proposed, for the founder's sign-off.
- **Covers:** the Schedule button on the bottom bar. That means the camp's day (sleep, work, free time), one person's own hours, the night watch kept in turns, overwork and what it costs, and how people bend or refuse all of it. It also covers the hooks that culture and faith (holy days, festivals, rites, daily customs) and the four new peoples will use.
- **Written alongside:** `2026-11-09-build-design.md`, `2026-11-09-zones-design.md`, `2026-11-09-peoples-design.md`, and `2026-11-09-culture-faith-design.md` (being built). Growth M0 is merged and peoples batch 1 is on its branch. Where this document needs something from one of them, §7 names the interface, not their content, and §7.0 sets where slice 1 starts and the merge order.
- **Sign-off approves:** the content and schema changes in §5; the draw ids and the numbers pre-assigned to this track in §6.1 (there is no new Dice substream); the three commands in §4.7; and the changes to sleep and the watch in §4.2–4.3. Those changes move the soak baselines, so acceptance compares against the merge base, re-measured (§8.2), not against c818511 or ADR-0017's 8.5. The builder records the decision as ADR-0026, the number pre-assigned to this track.
- **Revised after the builder's questions** on merge order, the undead, festivals and the watch, one compliance rule, and draw ids. The answers sit where a builder looks for them (§4.1, §4.3, §4.4, §6.1, §7.0–7.4, §8.1–8.2). Calls that don't change what gets built are marked "(lead's call)". §9 holds what only the founder can decide.

---

## 1. Intent

### 1.1 What the camp's day looks like today

I measured it with a read-only probe. It links the Release `Return.Sim.dll` of build c818511 and runs 30 worlds for 1 year (40 days) each. Each world sets a ration policy by seed, as `Autoplay` does, and every member is sampled every 6 in-game minutes. A second run over the same 30 worlds counted events by key, for the insult and volume figures. Nothing in the repo was changed.

These are c818511's figures. They are the evidence for the design, not the acceptance bar. The base has moved since: peoples batch 1 alone adds an undead who is awake all night. So §8.2 re-measures them on the merge base before slice 1's first sim commit.

| | Humans | Vampires |
|---|---|---|
| Asleep | **4.8 h a day.** In bed at 22:00 (`Places.BedtimeAfterDusk` = dusk + 3), up at about 03:00 when rest reaches 950 (`NeedsJobs.Sleep`). | 4.4 h a day, 06:00 to 10:00. |
| Working | 2.8 h a day. | 3.5 h a day, all of it at night. |
| Idle (`Wander` near the hearth) | **15.2 h a day.** Half of all humans are pacing the dark at 04:00–05:00. | 15.1 h a day, mostly 11:00–18:00 under cover. |
| Rest below 150 | 1.8% of the time. | 0.6%. |

- **The watch.** Someone stands at the post in 87% of night samples. 72% of that watching is done by humans who happen to be awake. A stint lasts a median 3.6 h, but the 90th percentile is 9.4 h: one person all night.
- **Thieves.** 243 came (8.1 a world-year). 159 were spotted, 93 of them by a Protective member, often one awake at 3 a.m. only because they woke early. 143 stole something.
- **Insults** peak 16:00–20:00, when the camp idles at the hearth.
- **Wear.** 1 collapse and no deaths in 30 world-years, and 2.2 accidents a world-year. Nobody is ever overworked.
- **Volume.** About 800 events a world-year (20 a day), 288 of them "ate".

What this says: the camp already has plenty of free time. Work is done on demand, because `JobPicker` only offers work that is needed. So the Schedule's lever is not how much gets done. It is **when people are awake, and with whom**: who is up when the thief comes, who has to be woken for the watch and by whom, who sits at the fire together, and who gives up their sleep and pays for it the next afternoon. Those are the stories.

### 1.2 The stories it creates

Three Chronicle excerpts, as the Chronicle and the story log would print them. Every line is an event from §4.8 or an existing event.

**The night nobody got up.** This ends in a grudge.

```
Day 33, 03:00 — Wren took the watch.
Day 33, 19:00 — Hollis took the watch.
Day 33, 23:00 — Hollis went to wake Wren for their turn on watch.
Day 33, 23:02 — Wren was too tired to take the watch from Hollis.
Day 33, 23:05 — Hollis went to wake Sera for their turn on watch.
Day 33, 23:07 — Sera told Hollis to find someone else.
Day 33, 23:08 — Hollis left the watch; nobody came to take it.
Day 34, 01:40 — A thief crept toward the camp in the dark.
Day 34, 01:52 — A thief made off with 3 berries; nobody was on watch.
Day 34, 06:00 — Nia went without so others could eat.
Day 34, 06:13 — Nia took the meal set aside for Bram.
Day 34, 06:13 — Bram now holds a grudge against Nia.
```
Wren had watched the small hours the night before, so she was spent (her refusal cites that watch). Sera is Independent and simply wouldn't. Hollis is neither Oathbound nor Protective, so he went to bed. The chain runs: Work → Social → Social → Work → Incident → Needs → Social. A reader retells it as "Sera wouldn't get up, so a thief took the last berries, and by morning Nia was so hungry she stole Bram's breakfast."

**Wren pays Mira back.** This ends in a repaid debt.

```
Day 12, 10:40 — Wren was hurt in an accident while working.
Day 12, 11:05 — Mira tended Wren's wounds.
Day 12, 11:05 — Wren owes Mira for tending their wounds.
Day 16, 20:40 — Mira fought the thief.
Day 16, 20:42 — The thief fled, wounded.
Day 16, 23:00 — Oren went to wake Mira for their turn on watch.
Day 16, 23:02 — Mira, still hurt, would not take the watch from Oren.
Day 16, 23:05 — Oren went to wake Wren for their turn on watch.
Day 16, 23:08 — Wren kept the watch in Mira's place.
Day 16, 23:08 — Wren repaid what they owed Mira.
```
Taking a turn that someone refused for a good reason counts as keeping it in their place. It goes through `CareJobs.SettleOrOwe`, so it repays an old debt or opens a new one. ADR-0018 lists "blood debts are seldom repaid" as a known seam, and this gives every debt a nightly chance to close.

**Oren's word.** This ends in a new debt, through overwork.

```
Day 20, 19:00 — Oren took the watch.
Day 20, 23:00 — Oren went to wake Sera for their turn on watch.
Day 20, 23:02 — Sera told Oren to find someone else.
Day 20, 23:05 — Oren went to wake Hollis for their turn on watch.
Day 20, 23:07 — Hollis would not get up for Oren.
Day 20, 23:08 — Oren stayed on watch when nobody came to take it.
Day 21, 03:00 — Oren went to wake Nia for their turn on watch.
Day 21, 03:03 — Nia took the watch.
Day 21, 16:20 — Oren is worn out from long hours.
Day 21, 16:45 — Oren, worn out, was hurt in an accident while working.
Day 21, 17:05 — Bram tended Oren's wounds.
Day 21, 17:05 — Oren owes Bram for tending their wounds.
```
Hollis holds a grudge against Oren, so he would not get up for him. Oathbound people stay on when nobody relieves them, for one extra turn at most; then they try the handover again. Being worn out doubles the odds of an accident at risky work. The chain runs: Work → Social → Work → Needs → Incident → Work → Social. Two systems the player never connected, the watch and the woodpile, meet in Oren's afternoon.

### 1.3 Why this design

- **It obeys fixed decision 2.** The player states intentions: the camp's day, when the camp keeps a watch, one person's own hours. People do the rest. They take the watch in turns, wake each other, refuse, cover and bend, and every one of those acts is between two named people, not between the player and a unit.
- **It obeys the cut rule.** It touches Work, Needs, Mood, Social, Incident and Combat (§4.9).
- **It retires two seams.** Humans no longer wake at 03:00 and pace the dark, which was an artefact of "wake at rest 950". Vampires no longer wake sleeping humans for blood at no cost (`BloodJobs.PickDonor` does not look at sleep).
- **It gives open debts a way to close** (see the second story).
- **It descends from the demo.** The demo already refused with "I need sleep before I can do that." and "The sun is still up. I need covered work." (`archive/return-demo/dist/sim.js`), and it already promised people time to rest. Here those become state, not lines.

---

## 2. Goals, non-goals, anti-goals

**Goals**
1. One camp-wide day (sleep, work, free time) that the player paints in seconds, with exceptions set on the person they belong to.
2. A night watch the camp keeps in turns and hands from person to person. Refusals, covering and empty posts land in the Chronicle with reasons and causes.
3. Overwork that costs something visible (mood, sleep, mistakes, accidents) and always has a cause.
4. Species live by their bodies: night folk by night, those who burn under cover by day, and those who never tire (the undead) without sleep.
5. One place where culture and faith write holy days, festivals, rites and daily customs into people's hours.
6. Legibility: every hour of one person's day is visible in their inspector, both as planned and as lived.

**Non-goals** (handled elsewhere or later)
- Meal times and shared meals belong to culture-faith. Owned beds and a second watch post belong to build. Gathering areas belong to zones.
- How much work exists is set by build and zones. The Schedule only says when.
- A watch captain, a companion who keeps the rota, comes with the court layer (§8.3).
- No precision finer than the hour.

**Anti-goals** (never)
- **A people-by-hours grid.** A strip is always one day: the camp's, the night folk's, or one person's. There is never a table of names against hours.
- Ordering a person to a post now, or drafting anyone. A change of hours never pulls someone off the job in hand (§4.2).
- **A schedule that kills.** Needs always beat the schedule. Overwork hurts through what tired people do, never through a meter running out (§9, question 4).
- Schedule clutter on the map. There is no clock of blocks, and no persistent bar or badge.
- Growth from grind. Hours worked never improve anyone (fixed decision 12).

---

## 3. How the player uses it

### 3.1 The bar

Schedule becomes a live button, like Work and Orders. It opens one popup above the bar, and Esc or a second click closes it. The other bar buttons are unchanged.

```
[Build][Orders][Zones][Work][Schedule] | [People][Chronicle]      Day 3, 21:40 · Spring | Pause ×1 ×3 ×6
```

### 3.2 The Schedule popup

This uses BottomBar's `ListPopup`, about 440 px wide. Hours are cells in a 24-cell strip. Below each cell is drawn as a letter: S = sleep, W = work, F = free time. On screen the cells are muted colours from `Palette` (sleep slate, work ochre, free time olive), with the letter shown on hover for colour-blind players.

```
┌ The camp's day ────────────────────────────────────────────────────┐
│ hour   0     3     6     9     12    15    18    21                 │
│        S S S S S S W W W W W W W W W W W W W F F F S S              │
│ Paint: [Sleep] [Work] [Free time]       click or drag across hours  │
│ Sleep 22:00–06:00 · Work 06:00–19:00 · Free time 19:00–22:00        │
│                                                                      │
│ The night folk's day — Ilex, Vale                                    │
│        W W W W W W S S S S S S S S F F F F F W W W W W              │
│                                                                      │
│ The watch                                                            │
│        * * * * * * . . . . . . . . . . . . . * * * * *              │
│ Kept from 19:00 to 06:00, in turns of 4 hours.   [No watch]         │
│ On watch now: Vale (since 19:00).                                    │
│ Last night: Vale 19:00–05:00 · nobody 05:00–06:00                    │
│ Who is asked first is set in Work (Keep watch: 1 first … – never).  │
│                                                                      │
│ Different days                                                       │
│ Mira — sleeps 07:00–15:00, works through the night                   │
│ Sera — kept to the camp's old hours                                  │
│ (click a name to open their day)                                     │
│                                                                      │
│ Takes effect when time moves.                       (only if paused) │
└──────────────────────────────────────────────────────────────────────┘
```

- **The camp's day** is always shown. It is the day for everyone whose people live by day.
- **The night folk's day** appears only while someone who lives by night is in the camp. The day of those who never sleep (§4.1) appears the same way while an undead is in the camp, with its Sleep brush disabled.
- **The watch** is a single range of hours. Drag along its row to set the start and end; `[No watch]` clears it. The line under it says, in plain words, who is on now and who kept last night's watch. That recap is derived from the lived hours (§4.5) and needs no extra state. The hint points at the Work priorities that already exist.
- **Different days** lists, in Work-popup style, each person whose day differs from their kind's camp day, with a short summary. Clicking a name selects that person, centres the camera on them and opens their inspector, exactly as the Work list does today.
- **While paused**, choices are drafts (`OrderDrafts`), drawn as chosen, with the "Takes effect when time moves." note. The same rules apply as for the ration policy.

### 3.3 The inspector's Day section

The Day section sits below "Work priorities". It holds two strips, a paint row and one note.

```
┌ Mira ───────────────────────────────── × ┐
│ Human · Lives in the camp                 │
│ Now: Keeping watch                        │
│ … needs, traits, work priorities …        │
│ Day                                       │
│  planned  W W W W W W W S S S S S S S S W W W W F F F W W │   own hours bright, the camp's dim
│  lived    * * * * S S S S S S S S W W W W W W F F F F * * │   * = on watch
│  Paint: [Sleep] [Work] [Free time]   [Same as the camp]   │
│  Has their own hours.                     │
│  Latest: Mira went to wake Oren for their turn on watch (Day 9, 03:00). │
└───────────────────────────────────────────┘
```

- **Planned** is the person's resolved day (§4.1). Hours inherited from the camp's day are dim and hours that are their own are bright. For anyone who burns in daylight, hours of sun danger are hatched, and the note says "The sun keeps Ilex under cover by day."
- **Lived** is what they mostly did in each of the last 24 clock hours: slept, worked, kept watch, free time, or needs (eating, seeking cover, downed). An hour where lived differs from planned gets a small notch, so bending is visible at a glance. Hovering a cell shows "23:00 — on watch (planned: sleep)".
- **Painting** here changes only this person (a `SetPersonDay` command). `[Same as the camp]` clears their own hours.
- **The note** is the plain-words source of their day ("Keeps the camp's day." / "Has their own hours." / "Keeps the night folk's day."). Under it is the most recent schedule event about them, from the log.
- **When someone declines** (§4.4), the note says "{a} would not change their day." It shows whenever their latest `declined_day_*` or `kept_old_hours_*` event is newer than their `OwnHoursEvent`. A painted change shows as pending until time moves, then snaps back when they decline, and the panel itself says why.
- **For someone who never tires** (the undead), the Sleep brush is disabled and the note reads "Ketil never sleeps." (§4.1).
- For anyone who is not a member (gone, dead, hostile), the section hides, as Work priorities do.

### 3.4 On the map

Nothing new is drawn permanently. What the schedule changes shows up in the people:
- **At the fire.** People gather and sit by the hearth in free time; the sit pose already exists for idlers by the hearth.
- **At night** the camp is asleep, with the zzz mark, except the watcher and the night folk.
- **A handover can be watched.** The watcher leaves the post, walks to a sleeper, the sleeper sits up, and one of them walks to the post.
- **The lantern** on the watch pole goes dark when the watcher falls asleep at the post. Today it lights for any `KeepWatch` job at stage 1; it becomes "and awake". This is a view-only change in `MapView`, derived from `Asleep`.
- **The sweat drop** already exists for exhaustion (BIBLE §2: "sweat drop = overworked"). It now also comes from the new overworked thought.
- **The watch-post tooltip** says who keeps the watch, or "nobody on watch" during watch hours (`StateApi.Location`).

### 3.5 Flows

1. **Go to bed earlier.** Schedule → Sleep brush → drag over 21:00. Running, it applies on the next tick; paused, it is a draft. People already busy finish what they are doing first. The Chronicle reads: "You changed the camp's day: sleep 9 hours, work 13, free time 2."
2. **Stop keeping a watch.** Schedule → `[No watch]` → "You decided the camp keeps no watch." Everyone sleeps through the night. The first thief who comes steals unopposed, and the watch-post tooltip already said nobody was on watch. An undead who came back to keep the watch begins to fade, and the fading cites your decision (§4.3).
3. **Put Mira on nights.** Click Mira on the map → Day → paint Sleep 07:00–15:00 and Work from 22:00 to 07:00. Mira is awake at night, and awake people are asked first (§4.3), so she keeps the watch turns and nobody has to be woken. Her lived strip shows it the next morning.
4. **Find out why the post was empty.** Hover the watch post, or open the Chronicle. The refusal, the reason and the cause are there. Click the refuser: their lived strip shows the watch they kept the night before.
5. **Live alongside the night folk.** Open Ilex: daylight is hatched and the note explains it. To give Ilex company, shift someone's day toward the evening. The camp's free time (19:00–22:00) is also when Ilex finds people awake to ask for blood (§4.6).

---

## 4. What changes in the simulation

### 4.1 Days, blocks and kinds of day

**Blocks** (code enum `DayBlock`, so they are not content):

| Block | Means |
|---|---|
| `Sleep` | Go to bed and stay there until the block ends. People get up early only for hunger, the sun, or being woken (§4.2). |
| `Work` | As today: needs first, then prioritised work. Idle when there is none, and idle time counts as free time in the lived record. |
| `Free` | Needs first. Urgent help still happens: care, rescue, Oathbound promises, Generous blood offers. No ordinary work. Nap if rest is below 400; otherwise seek company and the fire. |

**Kinds of day** (content, `rhythms.json`). Each species may name one. Each kind of day has a default camp day, and the world keeps one camp day per kind, which the player edits. Slice 1 ships three:
- `diurnal`: sleep 22:00–06:00, the same window as today's `BedtimeAfterDusk`. It is the default for a species that tires.
- `nocturnal`, for vampires.
- `sleepless`: work 06:00–22:00, free time 22:00–06:00. It is the default for a species that does not tire. It ships now, not in a later slice, because the undead (peoples batch 1) land before slice 1 (§7.0).

**Who sleeps is the body's call, and one flag says it (lead's call).**
- **The flag** is the species' `tires`. Peoples batch 1 already added and tests it: with `tires: false`, rest stays full and the person is never asleep.
- **Kinds of day carry no flag of their own.** The earlier `"sleeps": false` is dropped, so the two can never disagree. A kind of day either has Sleep hours or has none.
- **Load checks.** `ContentDb` rejects a species that tires naming a kind of day with no Sleep hours, and one that does not tire naming a kind with them.
- **A stray Sleep hour** can still reach someone who does not tire (a mod's own hours, a stale draft). `BlockAt` reads it as Free, because the body sits above the schedule.

**Resolving one person's block at hour h** (`ScheduleSystem.BlockAt(w, c, h)`), first match wins:
1. An observance overlay that applies to this person at h (culture-faith, slice 2, §7.2).
2. The person's own hour, if set (`Character.OwnHours`).
3. The camp day of their kind of day.

The body's limits sit above the schedule and are applied by the job picker, not by the resolver. Anyone who burns stays under cover during sun danger whatever the block says (`JobPicker` already does this through `sunBlocked`). Such hours are hatched in the strip, so the player sees why. The schedule never overrides hunger, injury, exhaustion or the sun.

**State.** All of it is plain public properties, serialised by the existing snapshot code. Lists are fixed length or kept in id order.
- `World.CampDays: List<CampDay>`, where `CampDay { string Rhythm; List<DayBlock> Hours /*24*/; long SetEvent }`. It is initialised from content in `WorldGen`.
- `Character.OwnHours: List<HourBlock>` (`{ int Hour; DayBlock Block }`, sorted by hour; empty means the camp's day), plus `Character.OwnHoursEvent: long`.

### 4.2 Sleep, work and free time: what changes in job choice

These are the exact sites. Today's behaviour keeps working wherever this table is silent.

| Site | Today | With the Schedule |
|---|---|---|
| `Places.SleepHours` / `BedtimeAfterDusk` | night for humans (dusk + 3 to dawn), daylight for those who burn; never for those who do not tire (batch 1's early return) | replaced by `ScheduleSystem.IsSleepTime(w, c)` (block at the current hour is `Sleep`; never for those who do not tire, §4.1). The constant is deleted; the hours live in `rhythms.json`. |
| `JobPicker.Choose`: `if (c.Rest < restAt \|\| (SleepHours && c.Rest < 700)) return SleepJob`. Batch 1 wraps it in `sp.Tires && (…)`. | sleep in sleep hours only if rest < 700 | `if (sp.Tires && (c.Rest < restAt \|\| block == Sleep)) return SleepJob`. In the Sleep block everyone who tires goes to bed. Keep batch 1's guard. |
| `JobPicker.Choose`: work | `WorkJob` whenever reached | `WorkJob` only in a `Work` block. In a `Free` block: nap if rest < 400 (`FreeTimeNapBelow`), then the existing socialise rule (social < 400, unchanged on purpose, §8.2), then `WanderJob` centred on the hearth at radius 2 (radius 4 elsewhere, as today). Batch 1's nature-need steps sit above this line, so they run in Work and Free blocks alike, because needs come first. A critical one also runs ahead of the sleep line. |
| `JobPicker.PromiseJob` | any time before work | unchanged. When an Oathbound person starts one outside their Work block, it emits `worked_late` once a day and sets `LastDutyEvent` (§4.5). |
| `JobPicker.Candidate("keep_watch")` | taken by whoever is free at night. On batch 1, an undead whose purpose seeks the watch also goes when someone already holds the post (`Purpose.Seeks`). | returns null, batch 1's exception included. The watch is handed out by `WatchSystem` (§4.3), which gives that undead first pick instead. `keep_watch` stays in `PrioritizedWork`, because its priority now says who is asked first. Batch 1's `Purpose.Answer` reaches the post through `JobPicker.WorkCandidate`, so it no longer sends anyone there either. That is intended. |
| `NeedsJobs.Sleep` wake rule | wake at starving, or rest ≥ 950, or (outside sleep hours and rest ≥ 600) | wake at starving (Desperate: food below `stealFoodBelow`), or when the block is no longer Sleep and rest ≥ 600. **Nobody gets up at rest 950 during their Sleep block.** The injured rule (health and rest ≥ 600) and the burner-in-sun rule are unchanged. |
| `WorkSystem.ShouldInterrupt` | sun, starving, rest < 60 | unchanged. A block change never interrupts a job in hand; the next choice follows the new block. That is the anti-direct-control rule, and the builder should say so in the method comment. Sleep is the one case running the other way: a sleeper gets up when rested and it is no longer their sleeping time. |

`Places.SleepHours` has exactly two callers, the `JobPicker.Choose` line and the `NeedsJobs.Sleep` wake rule above. That is still true on the peoples branch at fc9af62. Everything else that cares about sleep reads `Character.Asleep` directly and is unaffected: `Places.TakenBed`, `IncidentSystem.RainUpkeep`, `CombatSystem.Spot`, `SocialSystem.CanTalk`, `TravelSystem` and `Rations`. Batch 1's other rest guards stay as they are: `NeedsSystem` holds rest at 1000, and `WorkSystem.ShouldInterrupt` checks `Places.Tires`.

Effect: humans sleep 22:00–06:00. Rest tops out at 1000 and stays there, and nobody wakes at 03:00. Without a watch turn, a default day ends with rest around 450–550, so the default day alone overworks nobody. Watches and long work days are what cost.

### 4.3 The night watch, kept in turns

The camp sets **watch hours**: one range, defaulting to 19:00–06:00 from `world.json`, or none. Inside them, `WatchSystem` keeps one person at the post at a time, in turns of `watchTurnHours` (4). People decide who goes, by fixed rules. The only Dice is the one compliance roll in §4.4.

**State** (`World.Watch`, a `WatchState`):
- `WatcherId` is the person at the post (0 if nobody), with `TurnEndsTick` and `TurnEvent`, the event that began the turn.
- `WakerId` and `WakeTargetId` are set while the watcher is walking to wake someone, with `Tries`.
- `RefusedIds` lists who has refused since this turn ended. It is cleared when a turn begins and at the end of watch hours.
- `LastRefusal` is an event id; `LastRefusalSympathetic` is a bool.
- `GapEvent` is the event that explains an empty post right now, or 0.
- `World.WatchFrom` and `WatchTo` hold the range (equal means no watch), and `World.WatchEvent` holds the decision that set it.
- `Character.LastWatchEndTick` records when each person last kept watch, for fairness.

**Who is asked next** (`PickNext(w, asker)`). A candidate must meet all of these:
- a member who is not downed;
- keep_watch priority above 0;
- not in `RefusedIds`, and not the asker;
- does not burn while the sun is up;
- not keeping an observance that is running (observances, below);
- not kept in by the home area (§7.4, once zones has landed).

Each must be either *awake and idle* (no job, `Wander` or `Socialize`) or *asleep* in a `Sleep` job. The injured can be woken, because the waker cannot see how badly someone is hurt. They refuse, and that refusal is sympathetic (§4.4). Only the downed are never asked. Candidates are ordered by:
1. Awake before asleep. Waking someone costs their sleep, so night folk and people whose own day is set to nights are asked first.
2. **Called to the watch by their nature (lead's call).** That is `NatureNeeds.WorkBias(w, c, "keep_watch") > 0`. On the peoples branch it means an undead whose purpose is the watch and is short of met, or a djinn whose bargain is to keep the watch. This replaces batch 1's rule that sent the undead to the post even when someone held it. The post still has one watcher.
3. Keep_watch priority, 1 first.
4. Longest since their last watch (`LastWatchEndTick`, oldest first). This is fairness.
5. The asker's opinion of them, lowest first. It comes after fairness on purpose: a watcher cannot send someone they dislike to every late turn, only choose among people who are equally due.
6. Id.

**The flow** (`WatchSystem.Step` every 30 ticks, just before `WorkSystem.Step`):

```
watch hour, post empty, nobody walking ──► PickNext(awake only, no asker)
      │ found: they take the post ("took the watch"; cites WatchEvent, or the open GapEvent)
      │ none, for a whole watch hour: "Nobody kept the watch." → GapEvent
      ▼
turn over (unless renewed: the undead, below), or watcher's rest < 120 (not Oathbound) ──► PickNext(asker = watcher)
      │ awake: they walk over and take the post; the watcher leaves when they arrive (no gap)
      │ asleep: the watcher leaves the post and walks to them ("went to wake"; GapEvent = this)
      │         on arrival the sleeper wakes, gets `woken` (−3) and decides (§4.4):
      │            accepts → walks to the post → "took the watch", or "kept the watch in X's place"
      │                       after a sympathetic refusal (cover, below)
      │            refuses → reason event; sleeper back to bed; Tries+1; ask the next (up to wakeTries = 2)
      ▼ nobody left to ask
   Oathbound (promisePriority > 0)  → "stayed on watch when nobody came to take it", for one more turn at most
   Protective (fightsThieves), last refusal sympathetic → "kept the watch in X's place", for one more turn at most
   anyone else                      → "left the watch; nobody came to take it" → GapEvent
```

- **After staying on.** A stay extends the current turn rather than starting a new one, so `RefusedIds` is kept. When the extra turn ends, the handover is tried again from the top, and nobody who refused earlier that night is woken twice. If nobody takes it then, the stayer leaves like anyone else ("left the watch; nobody came to take it"), or falls asleep at the post first if their rest has run out.
- **The empty post during a handover is deliberate.** At 6 ticks a tile, a walk across the camp takes a few in-game minutes. It is a real window, and the player can see it on the map. A thief who reaches the stockpile in it steals, and the theft cites the "went to wake" event through `GapEvent`. Nothing papers over it.
- **The first turn of the night** goes to the most eligible awake, idle person whose refusal check (reasons 1–4 in §4.4) passes. Nobody asks them, so no refusal event and no compliance roll happen. At 19:00 on the default day everyone is in free time and awake. If nobody is idle (everyone is eating or finishing a job), the step simply waits and tries again 30 ticks later. "Nobody kept the watch." fires only after a whole watch hour with nobody at the post. No Dice break these ties; the order above does.
- **The end of watch hours** ends the turn quietly. With less than one hour of watch left there is no handover. Night folk going under cover at 05:00 simply stop, and no gap event fires.
- **Leaving mid-turn.**
  - Hunger: starving, or a Desperate person with food below `stealFoodBelow`. The watcher emits "left the watch to look for food" (cites `LastFoodEvent`) and sets `GapEvent`.
  - Sun, for those who burn: as above.
  - Rest below `asleepOnWatchRestBelow` (60; Oathbound 30): the watcher falls asleep at the post ("fell asleep at the watch post", Needs). Their job becomes a `Sleep` job on that tile, `WatcherId` becomes 0 and `GapEvent` is set. They lie there and the lantern goes dark.
  - A change to their keep_watch priority, or to their day, does not end a turn in hand. It decides only whether they are asked next time.
- **The undead and the watch (lead's call).** Peoples batch 1 binds some undead to keep_watch by Truth (`needs.json` `bondWorks`). Keeping it is the only thing that restores their purpose. `Purpose.Doing` looks for a `KeepWatch` job, so the post is always held as a `KeepWatch` job. Three rules give that purpose a real place in the rota.
  - **First pick.** While their purpose seeks the watch, they come before fairness (step 2 above). They never sleep, so they are always among the awake.
  - **Renewal.** When their turn ends and `Purpose.Seeks(w, c, "keep_watch")` still holds, they keep the post for another turn. This is quiet: no handover and no event. Once purpose reaches 600 (`satisfiedAt`), the normal handover follows. Purpose restores 70 an hour against a decay of 8, so one or two turns a night keep it met. A djinn bound to the watch gets first pick but no renewal, because its need fills from the bargain, not from the work.
  - **The player's levers are causes.** Setting keep_watch to "never" already takes the purpose away (`Purpose.OnPriority`). A `SetWatchHours` that leaves no watch hours does the same. Its Apply calls `Purpose.OnPriority(w, c, "keep_watch", 0, e.Id)` for each member bound to the watch, so "You decided the camp keeps no watch." becomes the first link of their fading, not a gap in the chain.

  Without these rules, the only way that need is met would run through the candidate §4.2 removes. Their purpose would then fall to `fading` (20 health an hour) and `died_purpose`, with no cause.
- **Observances (lead's call, following culture-faith's design).** Culture-faith slice 1 makes festival nights watchless on purpose. At dusk every member takes a `Feast` job at the hearth, and "on festival nights nobody keeps watch, and thieves roll as usual". The schedule keeps that outcome, and takes it out of the hands of the tick order:
  - While a festival runs, `WatchSystem` asks nobody. A watcher ends the turn quietly when it begins. Everyone is at the fire, so the post stands empty for the festival's hours.
  - The festival's `begins` event becomes `GapEvent` on the first `WatchSystem.Step` after it is logged, so a theft cites the festival. "Nobody kept the watch." does not fire for those hours.
  - When it ends, the first-turn rule asks the awake, as it always does. Both slice-1 festivals end at or after 22:00 (harvest fire 19:00–22:00, Longest Night 19:00–23:00), inside the diurnal Sleep block. So the night folk take the post if any are in camp, and a camp of day folk sleeps through to dawn, as after any abandoned turn. That is the price of the festival, and the Chronicle shows it.
  - `WatchSystem` decides this from culture-faith's calendar, not from the `FestivalRun` record: the festival's season and day, `DuskHour`, `hours`, and whether a keeper is a member. `WatchSystem.Step` runs before the hourly block that starts the festival (§4.10), so a record would arrive too late, and who happened to be idle on that tick would decide.
  - An observance kept by only some (a holy day, culture-faith slice 2) excuses only its keepers. The others keep the watch.
- **Covering.** When someone refuses for a *sympathetic* reason (hurt, tired or hungry), whoever ends up keeping that turn keeps it in the refuser's place. That is "{a} kept the watch in {b}'s place" (Work), then `CareJobs.SettleOrOwe(helper: keeper, helped: refuser, DebtKind.Watch, coverEvent)`. If the keeper owed the refuser anything, that debt is repaid; otherwise the refuser now owes the keeper. `DebtKind.Watch` is a lead's call: an appended enum value, stored by name in snapshots, so it needs no schema or determinism change. It gives debts the nightly way to close that ADR-0018 says they lack. The refuser gets `watch_taken_for_me` (+4 mood, +8 opinion of the keeper). After an *unsympathetic* refusal (a grudge, or going their own way), whoever keeps the turn gets `covered_for` (−2 mood, −8 opinion of the refuser). If that pushes opinion to `SocialSystem.GrudgeAtOpinion` (−40), a grudge forms, as `BloodJobs.Refuse` already does for refused vampires.
- **Thieves.** `CombatSystem.Spot` is unchanged. Whoever has a `KeepWatch` job and is awake spots thieves, and so do Protective people who are awake. `CombatSystem.Steal` cites `World.Watch.GapEvent` when it is set during watch hours, and uses the `event.incident.stolen_unwatched` text. With no watch hours set, a theft cites only the thief. That keeps causes honest (ADR-0018): "the camp keeps no watch" is not an event that brought this particular theft about.
- **Volume.** On a night kept by humans: one "took the watch" and two handovers of two events each, so about 5 events. On a night kept by night folk: 1. That is against about 800 events a world-year today. Acceptance is never logged separately; the next "took the watch" cites the wake event.

### 4.4 Bending and refusing

Refusals reuse the shape of `BloodJobs.RefusalKey`. `WatchSystem.RefusalKey(w, c, waker)` returns null, or the key for the first reason that applies, and each reason cites its own cause. Reasons 1–4 are pure. Reason 5 draws (§6.1), so it is checked last, and only on a real waking: never by the first-turn rule or by a read.

| # | Reason | Who and when | Chronicle | Cause cited | Sympathetic |
|---|---|---|---|---|---|
| 1 | hurt | health below max(400, `fleeHealthBelow`, `riskyWorkHealthBelow`); Anxious refuse below 700 | "{b}, still hurt, would not take the watch from {a}." | `LastHealthEvent` | yes |
| 2 | tired | rest below max(150, `riskyWorkRestBelow`); Anxious refuse below 300 (new field) | "{b} was too tired to take the watch from {a}." | `WornOutEvent`, else `LastDutyEvent`, else `LastRestEvent` | yes |
| 3 | hungry | starving, or food below `stealFoodBelow` (Desperate, 220) | "{b}, hungry, would not take the watch from {a}." | `LastFoodEvent` | yes |
| 4 | grudge | holds an active grudge against the waker | "{b} would not get up for {a}." | the grudge's `FormedEvent` | no |
| 5 | their own way | the shared compliance roll (below): `Compliance.Defies(w, c, DiceId.WatchDefy)`. That is 30% for an Independent who neither dislikes you nor is in a black mood. | `_own_way`: "{b} told {a} to find someone else." `_dislike`: "Caring little for what you want, {b} told {a} to find someone else." `_mood`: "{b}, in a black mood, told {a} to find someone else." | the wake event. For dislike, add a grudge against you, else their worst thought about you; for mood, their worst thought (as zones cites). | no |

The ask event is always the first cause. A refusal adds `refused_someone` (an existing thought) to the refuser, about the waker. An unsympathetic refusal also gives the waker the new `watch_refused` (−4 mood, −8 opinion). That is a new thought on purpose: reusing `was_refused` would trip `BloodJobs.RecentlyRebuffed` for a vampire waker. A refusal for their own way (reason 5) also has witnesses, by the shared rule below. Every awake member within 8 tiles who `keepsRules` (the Oathbound) gets `saw_rule_broken` about the refuser, caused by the refusal. The refuser is not a witness, and neither is the waker, who already has `watch_refused`. At night that is usually nobody but the night folk.

**One compliance rule (lead's call).** Going one's own way should read the same whatever rule is broken. So the schedule uses the helper zones asks for (zones §8.2), not a formula of its own. Whichever of zones and the schedule is built first builds `Compliance` (`sim/Systems/Compliance.cs`) to these terms:
- **`Odds(w, c)`, in permille.** Add up:
  - the sum of their traits' `defiesRulesPermille` (Independent 300);
  - 5 per point of dislike of you, at most 400;
  - 150 if their mood is below 25;
  - later, culture and people terms.

  Clamp to 0–1000. The player's odds are always 0. The largest term names the reason: `own_way`, `dislike` or `mood` (ties go trait, then dislike, then mood).
- **`Defies(w, c, tag)`:** one draw on `w.Rng.Dice[DiceSystem.Work]` with the caller's `DiceId`, taken only when the odds are above 0. It returns the reason, or null.
- **`Witness(w, breaker, breach)`:** every other awake member within 8 tiles who `keepsRules` gets `saw_rule_broken` (mood −2, opinion −4, stacks to 3), caused by the breach.

Zones rolls it once a night at dusk, for its curfew. The schedule rolls it at its own two moments: when a day change is applied, and when someone is woken for the watch. This retires the doc's earlier odds: accepting a day change with 50% plus half one's liking for you, and refusing a waking 25% of the time, both read from `priorityRespectPct`. That field keeps scaling work priorities, as today. Whether people may go their own way over the schedule at all is §9, question 2. If the founder says no, both schedule rolls and their events go, and zones still gets the helper.

**The other bends**, by trait. Every number here already exists unless marked new.

| Trait | Bend | Logged as |
|---|---|---|
| Independent (`defiesRulesPermille` 300), and anyone who dislikes you or is in a black mood | When you change their own day, or the camp's day they follow, they take the compliance roll once (`DiceId.DayChangeDefy`, in `ScheduleSystem.DayChangeRoll`). If they go their own way over their own change, `OwnHours` is untouched. If they go their own way over a camp change, the old camp hours become their own hours for the hours that changed. The player's own character never rolls. There are no witnesses: a declined plan happens in nobody's sight. | `event.social.declined_day_<reason>` ("Never one to be told, {a} would not change their day.") / `event.social.kept_old_hours_<reason>` ("{a} kept to the camp's old hours, caring little for what you want."). Reasons as above; Social; cites the command's event. |
| Desperate (`stealFoodBelow` 220) | Gets out of bed when food falls below 220, and leaves the watch for food below 220. | "left the watch to look for food" (only when it empties the post) |
| Anxious (`fleeHealthBelow` 700; `riskyWorkRestBelow` 300, **new**) | Refuses the watch when hurt or tired. With `keep_watch` marked `risky` (§5), also skips any risky work when rest is below 300, which is what "Avoids risky work when wounded or exhausted" already promises. | refusal reasons 1–2 |
| Oathbound (`promisePriority` 3, `restThresholdAdd` −80) | Stays on when nobody takes the watch, for one extra turn at most. Doesn't hand over early when tired. Falls asleep at the post only below rest 30. Keeps working past their hours on an open promise. | "stayed on watch when nobody came to take it" / "kept working past dark to keep their promise to {b}" |
| Protective (`fightsThieves`) | Keeps the watch in place of someone who refused for a sympathetic reason, when nobody else will. | "kept the watch in {b}'s place" |

**Bends from needs**, which apply to everyone: hunger gets people up, exhaustion makes them nap in any block (rest below `200 + restThresholdAdd`, as today), and the injured lie down. These leave no Chronicle line unless they empty the post. The inspector's lived strip shows them as notches.

### 4.5 Overwork

**Lived hours.**
- `Character.Lived: List<LivedKind>` has 24 entries, one per clock hour, each overwritten daily. It records what the person mostly did in the most recent occurrence of that hour. `LivedKind` is `Slept`, `Worked`, `Watched`, `Free` or `Needs`.
- `ScheduleSystem.Sample` adds one tally sample ten times an hour, never on the hour boundary. `ScheduleSystem.Hourly` writes the hour that just ended from the tally (`Character.HourTally`, 5 counts plus a fire count). Ties break in the order Watched, Worked, Slept, Free, Needs.
- Mapping: asleep → `Slept`. `KeepWatch` or `WakeForWatch` → `Watched`. Forage, timber, haul, roof, care, rescue, keep promise, give blood, fight and flee → `Worked`. Wander, socialise or no job → `Free`. Eat, seek cover, ask for blood, arrive or downed → `Needs`.
- A newcomer's `Lived` starts as their planned day, so they never arrive overworked.

From `Lived`: `duty24` = hours Worked plus hours Watched; `free24` = hours Free; `awake24` = 24 minus hours Slept.

**Costs**

| Cost | When | Effect | Chronicle |
|---|---|---|---|
| `overworked` thought (−6, sweat drop) | `duty24` ≥ `overworkHours` (12) | mood | (shown in the inspector) |
| `no_time_to_myself` (−4) | `awake24` ≥ 14 and `free24` = 0 | mood | (shown in the inspector) |
| **worn out** | rest below 150 while `duty24` ≥ `wornOutDutyHours` (8); once per stretch; cleared at rest 600 | sets `Character.WornOutEvent`; doubles accident odds at risky work (`tiredAccidentPct` 200); becomes the cause of what follows | "{a} is worn out from long hours." (Needs; cites `LastDutyEvent`) |
| accident while worn out | `JobRunner.RollAccident`, same draw (`AccidentRoll`), permille × 2 | injury, then care, then debt | "{a}, worn out, was hurt in an accident while working." (cites the work event and `WornOutEvent`) |
| handing over early | watcher's rest below 120 (not Oathbound) | the handover flow | handover events |
| asleep at the post | rest below 60 (Oathbound 30) | empty post, lantern dark | "{a} fell asleep at the watch post." |
| no healing | rest below 200 (exists in `NeedsSystem`) | health doesn't recover | — |
| `evening_by_the_fire` (+4, music note, 12 h) | half or more of a Free hour spent awake within 3 tiles of the hearth with someone else there | mood | — |

**`LastDutyEvent`** names the cause of overwork honestly. It is set:
- when a person takes the watch, keeps it in someone's place or stays on (the event that began that duty);
- on `worked_late`;
- when a command adds Work hours to that person's day (the command's event, like `World.PolicyEvent` for rations).

There is no direct health drain and no death from exhaustion (§9, question 4). Deaths keep story causes: an accident, a thief.

**Those who never tire get none of these costs (lead's call).** An undead's rest never falls, so they never wear out. `overworked` and `no_time_to_myself` also skip anyone whose species does not tire. Work is what they came back for, and their purpose need already speaks for it.

### 4.6 Night folk, the sun and blood

- **Vampires** take `"rhythm": "nocturnal"`. The default night folk's day is sleep 06:00–14:00, free time 14:00–19:00 (under cover, because sun danger already keeps them there) and work 19:00–06:00. Today they sleep 06:00–10:00 and idle under cover until dusk. At night they are awake and idle between jobs, so the watch goes to them first and the humans sleep. Vale, the watcher with keep_watch priority 1, keeps it most nights. Once Ketil arrives, she shares it with him: he is a priority-1 watcher who never sleeps, and comes first while his purpose calls him to the post (§4.3).
- **Asking for blood at night.** `BloodJobs.PickDonor` changes in three ways:
  - It prefers awake donors over sleepers, then opinion, then distance as today.
  - It never picks the person on watch.
  - A sleeper who is asked is woken: the request walks to them as today, and on arrival they get the shared `woken` thought, the same one as for the watch. The new first refusal reason is: asleep when asked and rest below 700 → "{b}, woken from sleep, refused to give {a} blood." (cites the ask).

  The result is that feeding drifts to the camp's evening free time, when both kinds of people are awake, and asking at 23:00 gets a tired "no". STORIES.md's "Vale, refused by a starving camp" went door to door through the night; in this design every one of those doors is a woken sleeper with a reason.
- **Company.** Night folk are awake while the camp sleeps, so their social need (12 an hour) has few partners. Putting a human on nights (flow 3) gives them a watch-mate. In Free hours, `SocialSystem` does the rest: chats, deep talks, opinion, and later perhaps consent to give blood.

### 4.7 Commands and drafts

These mirror `SetRationPolicy` and `SetWorkPriority` exactly: a `JsonDerivedType` registration, an `Apply` that emits a World event, `OrderDrafts.SameSlot`/`Flush`, and `StateApi` merging `PendingCommands`.

| Command | Fields | Apply | Event (World) |
|---|---|---|---|
| `SetCampDay` | `Tick, string Rhythm, DayBlock[] Hours` (24) | replaces that kind's camp day and sets `CampDay.SetEvent`; runs the day-change roll (§4.4) for members of that kind with no own hours there; updates `LastDutyEvent` for anyone who gains Work hours | `event.world.camp_day`: "You changed {day}: sleep {sleep} hours, work {work}, free time {free}." (`day` is a key arg: "the camp's day" or "the night folk's day") |
| `SetWatchHours` | `Tick, int From, int To` (equal means none) | sets the range and `World.WatchEvent`; a watcher outside the new hours ends their turn quietly. When no watch hours are left, it makes this event the cause of the fading of anyone whose purpose is the watch (§4.3). The event carries `EventArg.Hour("from")`, `EventArg.Hour("to")` **and `EventArg.Num("n", hours)`**, where `hours` is the range's length (0 when From equals To). `EventText.Describe` picks the `_zero` text only from an `n` arg of 0, so without it "keeps no watch" never appears. | `event.world.watch_hours`: "You decided the camp keeps a watch from {from} to {to}." `_zero` variant (n = 0 hours): "You decided the camp keeps no watch." |
| `SetPersonDay` | `Tick, int CharacterId, DayBlock?[] Hours` (24; null = the camp's) | runs the day-change roll (§4.4; the player never rolls); then replaces `OwnHours` and sets `OwnHoursEvent` and, if Work hours grew, `LastDutyEvent` | `event.world.person_day`: "You changed {a}'s day: sleep {sleep} hours, work {work}, free time {free}." `_you_a`: "You changed your own day: …". All-null gives `event.world.person_day_reset`: "{a} now keeps {day}." `_you_a`: "You now keep {day}." |

**Drafts.**
- Slots are: one per kind of day for `SetCampDay`, one for `SetWatchHours`, and one per character for `SetPersonDay`.
- A later draft for the same slot replaces the earlier one.
- `Flush` skips any draft equal to the current state, compared element by element.
- `OrderDrafts` gains `CampDay(rhythm)`, `WatchHours()` and `PersonDay(id)` getters, so the view can draw drafts.

One command carries a whole day, so one decision is one Chronicle line, never eight. The declines it causes (§4.4) are lines of their own. In a sour winter a camp change can bring several, and they fold as the story log folds runs ("… and the same with Bram and Nia"), the fold zones §5.6 moves into `Return.Sim.Api`.

### 4.8 New events

Times inside events use a new `EventArg.Hour(name, h)` (prefix `h:`), resolved through the loc key `time.hour` ("{h}:00"). That way the format is the loc file's, not code's.

| Key | System | Actors | Causes | Effects |
|---|---|---|---|---|
| `event.world.camp_day`, `.watch_hours`, `.person_day`, `.person_day_reset` | World | the player, the person | — | — |
| `event.social.declined_day_own_way` / `_dislike` / `_mood`, and `.kept_old_hours_own_way` / `_dislike` / `_mood` | Social | the person | the command's event, plus the reason's cause (§4.4) | — |
| `event.work.took_watch` | Work | watcher | `WatchEvent` on the night's first turn, else the wake event, else `GapEvent` | — |
| `event.work.went_to_wake` | Work | watcher, sleeper | `TurnEvent` (or the last refusal) | — |
| `event.social.watch_refused_hurt` / `_tired` / `_hungry` / `_grudge` / `_own_way` / `_dislike` / `_mood` | Social | waker (a), refuser (b) | the wake event + reason cause (§4.4) | a grudge, if opinion crosses −40 |
| `event.work.covered_watch` | Work | keeper (a), refuser (b) | the refusal | via `SettleOrOwe`: `DebtFormed` (new `event.social.owes_watch`: "{a} owes {b} for keeping watch in their place.") or `DebtRepaid` |
| `event.work.stayed_on_watch` | Work | watcher | the last refusal, or `TurnEvent` | — |
| `event.work.left_watch_unrelieved` | Work | watcher | the last refusal, or `TurnEvent` | — |
| `event.work.left_watch_hungry` | Work | watcher | `LastFoodEvent` | — |
| `event.work.watch_unkept`: "Nobody kept the watch." | Work | — | the last refusal or leave event, if any | — |
| `event.needs.fell_asleep_on_watch` | Needs | watcher | `WornOutEvent`, else `LastDutyEvent` | — |
| `event.needs.worn_out` | Needs | person | `LastDutyEvent` | — |
| `event.work.worked_late` | Work | person (a), creditor (b) | the promise's `FormedEvent` | — |
| `event.incident.work_accident_tired` | Incident | person | the work event + `WornOutEvent` | (as `work_accident`) |
| `event.incident.stolen_unwatched` | Incident | thief | the thief's cause + `GapEvent` | — |
| `event.social.blood_refused_woken` | Social | vampire (a), donor (b) | the ask | — |

Every event that names the player has `_you_` variants, as ADR-0017 requires. No new `EffectKind` is needed. Stories end in what already ends them: debts formed and repaid, grudges, departures through mood, and deaths through accidents and thieves.

### 4.9 How it touches other systems

| System | The Schedule reads | The Schedule writes |
|---|---|---|
| Work (`JobPicker`, jobs) | keep_watch priority, job in hand | job choice by block; watch turns; the new `WakeForWatch` job |
| Needs | rest, food, health | stay-in-bed and wake rules; naps; worn out; asleep at the post |
| Mood | — | `overworked`, `no_time_to_myself`, `evening_by_the_fire`, `woken`, `watch_refused`, `covered_for`, `watch_taken_for_me` |
| Social | grudges, opinions, debts, opinion of you | refusals; covers that form or repay debts; resentment that forms grudges; declines; witnesses' disapproval (§4.4) |
| Peoples' natures (Needs) | an undead's purpose, a djinn's bargain | first pick and renewal at the post; the cause when the watch is taken away (§4.3) |
| Culture (festivals) | whether an observance runs | an empty post, and its cause (§4.3) |
| Incident | — | thefts during empty posts (cause); accidents doubled when worn out |
| Combat | — | who stands at the post to spot (the existing `Spot`) |
| Travel | — | walks to wake, to the post, to the fire |
| Blood (Social and Work) | asleep or awake | donor preference; the woken refusal |

### 4.10 Tick order and code sites

`Simulation.Step` becomes:

```
ApplyDueCommands → Travel → Combat → (every 30 ticks: WatchSystem.Step)
                 → Work → (offset 15 of each 30: ScheduleSystem.Sample) → Social…
hourly: Needs → ScheduleSystem.Hourly → Rations → Mood → Social → Incident
```

`ScheduleSystem.Hourly` runs after Needs (and after any hourly step a sibling adds directly after Needs, such as batch 1's `Bargains.Hourly`), so rest and food are current, and before Mood, so this hour's thoughts count.

`WatchSystem.Step` runs before the hourly block. Anything it must know about the hour, such as whether a festival runs now, it reads from calendars and content. It never reads records that an hourly system creates later in the same tick (§4.3, observances).

**New files:**
- `sim/State/Schedule.cs`: `DayBlock`, `LivedKind`, `HourBlock`, `CampDay`, `WatchState`.
- `sim/Systems/ScheduleSystem.cs`: resolving, sampling, the hourly costs, the day-change roll.
- `sim/Systems/WatchSystem.cs`.
- `sim/Systems/Jobs/WatchJobs.cs`: the `WakeForWatch` job, and `KeepWatch` moved out of `CampJobs`.
- `sim/Systems/Compliance.cs`, unless zones has already built it (§4.4).

**Changed files:**
- `Character`, `World`; `Job.cs` (append `JobKind.WakeForWatch`); `Social.cs` (append `DebtKind.Watch`).
- `Places`, `JobPicker`, `NeedsJobs`, `JobRunner` (dispatch, plus the tired odds in `RollAccident`), `IncidentSystem.Accident` (tired variant and cause), `CombatSystem.Steal`, `BloodJobs`, `Relations.FormDebt` (a key for `DebtKind.Watch`: its switch otherwise falls through to `owes_care`), `Commands`, `DrawIds` (DiceIds 200–201), `Defs`, `ContentDb`, `RequiredContent`, `StateApi`, `EventText`, `WorldGen`.
- From the siblings, when they have landed (§7.0): batch 1's `Purpose.OnPriority`, called by `SetWatchHours`; culture-faith's festival calendar, as a pure query (`Festivals.RunsAt(w, tick)`) that `WatchSystem` reads; zones' curfew test, read by `PickNext`.
- View: `BottomBar`, `InspectorPanel`, `Logic/OrderDrafts`, `Logic/ViewText`, `MapView` (lantern), `SimHost` (check hooks), plus a Godot-free `Logic/DayStrip.cs` (hour from x, painting a range, labels) and a thin `DayStripControl` that draws it.
- Runner: `Autoplay`, `Soak`, `StoryLog`.

---

## 5. Content and schema changes

The founder's sign-off of this document approves these (CLAUDE.md "ask before changing a content schema").

**New `content/rhythms.json`** with `content/schemas/rhythms.schema.json`:

```json
[
  { "id": "diurnal", "nameKey": "rhythm.diurnal.name", "campDayKey": "rhythm.diurnal.camp_day",
    "day": [ { "from": 22, "to": 6, "block": "sleep" },
             { "from": 6, "to": 19, "block": "work" },
             { "from": 19, "to": 22, "block": "free" } ] },
  { "id": "nocturnal", "nameKey": "rhythm.nocturnal.name", "campDayKey": "rhythm.nocturnal.camp_day",
    "day": [ { "from": 6, "to": 14, "block": "sleep" },
             { "from": 14, "to": 19, "block": "free" },
             { "from": 19, "to": 6, "block": "work" } ] },
  { "id": "sleepless", "nameKey": "rhythm.sleepless.name", "campDayKey": "rhythm.sleepless.camp_day",
    "day": [ { "from": 6, "to": 22, "block": "work" },
             { "from": 22, "to": 6, "block": "free" } ] }
]
```

- Schema: `id`; `nameKey` and `campDayKey` (locKey); and `day`, a list of `{ from 0–23, to 0–23, block: "sleep" | "work" | "free" }`. When `to` is less than `from` the range wraps past midnight, and `to` never equals `from`. There is no `sleeps` field: who sleeps is the species' `tires` (§4.1).
- `ContentDb.Validate` checks: every hour is covered exactly once; the loc keys exist; `diurnal` and `sleepless` exist (`RequiredContent.Rhythms`).
- Mods merge by id, like every other list.

**`species.json` and its schema:** optional `"rhythm"`, an id. When it is absent, a species that tires gets `diurnal` and one that does not (batch 1's `"tires": false`) gets `sleepless`. Vampire gets `"rhythm": "nocturnal"`, and the undead need no field. The field is optional, so the peoples branch's species load without it. `ContentDb` rejects an unknown rhythm id, and a mismatch between `tires` and whether the kind of day has Sleep hours (§4.1). Peoples 2A's other night folk (the ghoul, the dokkaebi, house spirits) take `"nocturnal"` as content.

**`world.json` and its schema:** a required `schedule` block:

```json
"schedule": {
  "watch": { "from": 19, "to": 6 },
  "watchTurnHours": 4,
  "wakeTries": 2,
  "overworkHours": 12,
  "wornOutDutyHours": 8,
  "noFreeTimeAwakeHours": 14,
  "tiredAccidentPct": 200,
  "asleepOnWatchRestBelow": 60,
  "freeTimeNapBelow": 400
}
```

The first three are design. The rest are tuning, put in content now rather than in code, in the spirit of ADR-0013's proposed tuning block.

**`traits.json` and its schema:**
- New optional `riskyWorkRestBelow` (integer 0–1000). Anxious gets `"riskyWorkRestBelow": 300`.
- The compliance fields zones defines (zones §5): `keepsRules` (boolean; Oathbound `true`) and `defiesRulesPermille` (integer 0–1000; Independent `300`). Whichever of zones and the schedule lands first adds them, and either sign-off approves them.
- Every other bend reuses an existing field (§4.4).

**`work.json`:** `keep_watch` gains `"risky": true`. No schema change.

**`thoughts.json`** gains seven entries; the schema is unchanged:

| id | mood | opinion | hours | emote | stack |
|---|---|---|---|---|---|
| `overworked` | −6 | | 1 | sweat | 1 |
| `no_time_to_myself` | −4 | | 1 | none | 1 |
| `evening_by_the_fire` | +4 | | 12 | music | 1 |
| `woken` | −3 | | 8 | none | 1 |
| `watch_refused` | −4 | −8 | 24 | anger | 2 |
| `covered_for` | −2 | −8 | 48 | anger | 2 |
| `watch_taken_for_me` | +4 | +8 | 48 | hearts | 1 |

All seven are added to `RequiredContent.Thoughts`. `overworked` and `no_time_to_myself` are re-added each hour while their condition holds, like `hungry`. If zones has not landed, slice 1 also adds zones' `saw_rule_broken` (mood −2, opinion −4, stacks to 3), which `Compliance.Witness` gives.

**Loc** (`en.json`, `en.ui.json`):
- the event keys in §4.8 with their `_you_` variants;
- the thought texts ("Worked too long", "No time to myself", "An evening by the fire", "Woken in the night", "Turned down in the night", "Had to keep {name}'s watch"…);
- `rhythm.*`, `block.sleep|work|free` with hints, `job.wake_for_watch` ("Waking someone for the watch"), `debt.watch` ("a watch kept for them") and `time.hour`;
- `ui.schedule.*`: the title, strip labels, brushes, watch lines, "Different days", summaries, pending note and empty states;
- `ui.inspector.day*`: the section title, planned and lived labels, notes, latest line and `[Same as the camp]`;
- `ui.bar.schedule_hint` ("When the camp sleeps, works and keeps watch").

---

## 6. Determinism, snapshots, the text-state API, localisation and mod hooks

### 6.1 Determinism

| Draw | Source | Id | When |
|---|---|---|---|
| Going their own way over a change to their day | Dice, `DiceSystem.Work` (existing), through `Compliance.Defies` | `DiceId.DayChangeDefy = 200` | once per affected member whose odds are above 0, per applied command; never the player |
| Going their own way when woken for the watch | Dice, `DiceSystem.Work`, through `Compliance.Defies` | `DiceId.WatchDefy = 201` | once per waking, only when reasons 1–4 did not apply and the odds are above 0 |
| An accident while worn out | Dice, `DiceSystem.Work` (existing) | `DiceId.AccidentRoll = 8` (existing) | the same single draw; only the threshold changes |
| Turn order, renewal, refusals 1–4, covers, observances, resolving blocks | none (fixed rules, ties broken by id) | — | — |

If the founder answers no to §9 question 2, the first two rows go, and slice 1 adds no draw at all.

- **Numbers pre-assigned to this track (lead's call; the sign-off approves them).**
  - **This track holds:** ADR-0026, DiceIds 200–219 and TruthIds 70–79. Slice 1 uses DiceIds 200 and 201 and no TruthId. The body clock (§9, question 6) would take TruthId 70.
  - **These continue the blocks the other tracks already hold:** growth ADR-0021 (TruthIds 20–29, DiceIds 100–119, `DiceSystem.Growth = 8`; merged at 17be2d5), peoples ADR-0022 (TruthIds 30–39, DiceIds 120–139) and culture-faith ADR-0023 (TruthIds 40–49, on its branch).
  - **What they leave free:** ADR-0024 and 0025, TruthIds 50–69 and DiceIds 140–199, for culture-faith's draws, build and zones. This assumes the order the founder named the menus in: build, zones, schedule.
  - **If the founder's allocation says otherwise, it wins, at no cost.** A `DiceId` is only a tag: `DiceStream.Range` records it for diffing, and it never reaches the generator. Slice 1 adds no `TruthId` or `DiceSystem`. So renumbering moves no roll and does not reopen acceptance.
- **No new substream (lead's call).** The doc earlier claimed `DiceSystem.Schedule = 8`.
  - **Why not:** growth now holds 8 on milestone-0 (its ADR-0021 lists this doc's claim as a collision). Build reserves 8 for its slice 2, and culture-faith appends `DiceSystem.Culture` next. A `DiceSystem` cannot be pre-assigned in blocks. Values must stay contiguous from 0 (`DiceRng.SeedStreams` throws on a gap), and each value seeds its own stream. So its number, and every roll in it, would depend on merge order.
  - **Instead,** both schedule rolls are Work-system draws, as zones' `ZoneDefy` is. The schedule governs job choice, and its watch turns are Work events.
  - **The cost is real and accepted.** A schedule roll advances the Work stream, so the wander and accident draws after it shift. Slice 1 changes those sequences anyway: job choice changes which wander draws happen at all. Its baseline is re-measured (§8.2), and no other system's stream moves, which is what ADR-0004 protects.
- **There is no new Truth.** A fixed body clock (early riser or night owl) would be one; it is §9, question 6.
- **Commands are tick-stamped and recorded.** Choices made while paused are drafts until time moves. All of it is part of the determinism test (§8.1).
- Everything is integers, and no dictionary iteration decides anything. Order comes from `World.Characters` (id order).

### 6.2 Snapshots

- **New state, all public and serialised by the existing `SnapshotSerializer`:** `World.CampDays`, `WatchFrom`, `WatchTo`, `WatchEvent` and `Watch`; `Character.OwnHours`, `OwnHoursEvent`, `Lived`, `HourTally`, `LastWatchEndTick`, `LastDutyEvent` and `WornOutEvent`.
- The change is additive, with no `SchemaVersion` bump. On load, `ScheduleSystem.EnsureDefaults(w)` fills empty camp days, watch hours and lived strips from content, so older snapshots and test fixtures still load.
- At 200 characters the new state is about 200 × (24 + 24 + 6) small values, well inside the 1-second restore budget.
- **Rewinds.** A fate knot restores the days and watch hours as they were then: they are state, and the player re-states intentions in the new timeline. Lived hours and wear go back with it. Nothing here is a Truth, so nothing survives a rewind except what the player remembers.

### 6.3 The text-state API

All reads are pure and merge pending commands, as `WorkPriorities` and `Policy` do. A pending `SetPersonDay` is merged like any other, even for someone who may go their own way: the read cannot know the compliance roll, and must not try to pre-roll it. The decline arrives as an event when the command applies (§3.3).

```csharp
public sealed record HourState(int Hour, string Block, string Name, string Source, string Note);
// Source: "camp" | "own" | "observance" | "sun"; Note: e.g. "under cover", "planned: sleep"
public sealed record DayState(string Rhythm, string Title, IReadOnlyList<HourState> Hours, IReadOnlyList<int> Members, bool Pending);
public sealed record WatchSpan(int CharacterId, string Name, int From, int To);
public sealed record WatchState(int From, int To, int TurnHours, int WatcherId, string Watcher, long Since,
    IReadOnlyList<WatchSpan> LastNight, bool Pending, string Text);
public sealed record ScheduleState(IReadOnlyList<DayState> CampDays, WatchState Watch,
    IReadOnlyList<PersonDaySummary> Different);
public sealed record PersonDaySummary(int Id, string Name, string Summary);
public sealed record PersonDayState(int Id, IReadOnlyList<HourState> Planned, IReadOnlyList<HourState> Lived,
    string Note, string Latest, bool SameAsCamp, bool Pending, int DutyHours, int FreeHours);

StateApi.Schedule(World w) → ScheduleState
StateApi.Day(World w, int characterId) → PersonDayState
StateApi.Location(...)  // the watch post line: "Watch post (Vale keeps watch)" / "(nobody on watch)"
```

**The headless runner:**
- The story log's cast section prints each person's day when it differs from their kind's ("Mira: sleeps 07:00–15:00, works through the night").
- The soak summary gains a Days block (§8.2).
- `CharacterState` is unchanged; the new reads are separate calls, so nothing downstream breaks.

### 6.4 Localisation and mod hooks

- **Text.** Every string the player reads is a key (§5), and no English is composed in C#. Summaries such as "sleeps 07:00–15:00, works through the night" are built in `ViewText` from per-range templates (`ui.schedule.own.sleep`: "sleeps {from}–{to}") joined with the existing `ui.list_separator`, as `JoinList` already does. Times go through `time.hour`.
- **Mods.**
  - They can add kinds of day (`rhythms.json` merges by id).
  - They can change the default watch and the tuning (a mod's `world.json` replaces the base one, as today).
  - They can add thoughts.
  - They see every schedule event through `IModHook` at the end of the tick.
  - Block kinds and the watch logic are code. A mod that wants a new duty asks for a hook; it does not edit the enum.

---

## 7. How it fits the parallel work

All four sibling documents are now in this folder, and three tracks are being built or already merged. This section says where slice 1 starts and in what order the tracks merge (§7.0), then what the Schedule needs from each sibling and offers it. One rule holds throughout: **whichever of two tracks lands second writes the seam between them**, to the terms stated here, and records it in its ADR.

### 7.0 Where slice 1 starts, the merge order, and what it absorbs (lead's call)

**The tracks as of 2026-11-09:**

| Track | Branch | State | Holds |
|---|---|---|---|
| Growth M0 | merged into milestone-0 | merged at 17be2d5 | ADR-0021; TruthIds 20–29, DiceIds 100–119, `DiceSystem.Growth = 8` |
| Peoples batch 1 | `worktree-wf_23280ce9-9be-1` | undead, dryad and djinn committed (fc9af62); the fae in progress | ADR-0022; TruthIds 30–39, DiceIds 120–139 |
| Culture-faith slice 1 | `worktree-wf_17f53b69-9af-2` | content loaded (1336f74), hooks being built; merges after the founder answers its question 1 | ADR-0023; TruthIds 40–49 |
| Build, zones, schedule | not yet started | designs awaiting sign-off | schedule ADR-0026; build and zones assumed to take 0024 and 0025 (§6.1) |

**Where slice 1 starts.** From the milestone-0 tip once peoples batch 1 has merged on top of growth M0, not from c818511.
- Batch 1 is committed and ahead in the queue. It already rewrites three lines §4.2 replaces: the `JobPicker` sleep line, `Places.SleepHours` and the keep_watch candidate. It also adds Ketil, an undead whose need runs through the watch.
- Landing first from c818511 would make batch 1 re-absorb all of that into code it did not design.
- View-only work (`DayStrip`, the popup's drawing) can start from any base.

**Merge order:** growth M0 (done) → peoples batch 1 → culture-faith slice 1 (whenever its founder question is answered) → **schedule slice 1** → zones slice 1 → peoples 2A (the need model). Build slice 1 can land before or after the schedule.
- **Schedule before zones.** Zones writes its watch skip (`stayed_in_watch`) into the keep_watch candidate that §4.2 deletes. It also measures "evening idle hours at the meeting place" on evenings this design changes. If the schedule lands first, zones writes the skip once, into `PickNext`, and measures on the final shape of the day.
- **Build is orthogonal.** Owned beds live in `Places.SleepSpot`, which the Sleep job already calls. Building is work, so it follows the blocks with no code here.
- **If a track lands ahead of this order,** slice 1 rebases onto it and writes the seam (table below).

**What slice 1 absorbs, depending on what has landed:**

| Sibling behaviour | If it landed before slice 1 | If it lands after |
|---|---|---|
| The undead (batch 1) | Always, since batch 1 lands first: `sleepless` (§4.1); first pick, renewal and causes (§4.3); no overwork costs (§4.5) | — |
| A djinn bound to the watch (batch 1) | first pick (§4.3) | — |
| Dryads and the fae (batch 1) | Nothing. Dryads tire and keep the camp's day. Their forest need takes them to their tree in any waking block, and a critical need does so even in the Sleep block. Every bend here is already spoken, as the fae need. | — |
| Festivals (culture-faith slice 1) | the observance rule in `WatchSystem` (§4.3) and test 17 | culture-faith writes the same rule and test into `WatchSystem` |
| The home area's curfew (zones) | the skip and `stayed_in_watch` in `PickNext` (§7.4) | zones writes them into `PickNext` |
| The meeting place (zones) | `FreeTimeSpot` returns it (§7.4) | zones changes `FreeTimeSpot` |
| `Compliance` (zones) | slice 1 calls it | slice 1 builds it, with zones' fields (§4.4, §5) |
| Owned beds (build) | nothing | nothing |
| Growth M0 | Nothing: the schedule adds no growth fields (§7.3). | — |

**Blood, and the peoples migration.** The byte-identical migration in peoples §8.9 belongs to 2A (the need model, for the kappa and the ghoul), not to batch 1. Batch 1 already changed `cast.json`.
- In this order 2A comes after slice 1. So 2A rewrites slice 1's `BloodJobs` lines into its data questions, and it owns passing its own byte-identical check.
- Slice 1's blood changes (§4.6) ask no new species question; they read `Asleep`, `Rest` and the watcher. So that rewrite touches only the lines §8.9 already lists.
- If 2A lands first, slice 1 writes §4.6 against 2A's questions instead.

**Every landing moves the logs:** batch 1's new cast, culture-faith's customs, build's bed claims, zones' meeting place. So slice 1 measures its baselines on whatever base it lands on (§8.2).

### 7.1 Peoples (dryads, djinn, fae, undead), `2026-11-09-peoples-design.md` and batch 1

- **The undead do not sleep.** That is the species' `tires: false` (§4.1).
  - They keep the `sleepless` day.
  - They are asked first for the watch while their purpose seeks it, and they keep the post past a turn's end until it is met (§4.3).
  - They get no overwork costs (§4.5).
  - Batch 1 restores purpose per hour at the bound work (`Purpose.Restore`), so the duty-hours read this document once offered is not needed and is dropped.
  - The story it opens is the player's. Set the undead's keep_watch to never, or keep no watch, and their purpose falls, citing that decision.
- **Dryads need forest.** Their need already sends them to their tree when it runs low (batch 1's tend job). Nature needs sit above work in `JobPicker`, and a critical one sits above sleep, so free time needs no special place for them. A `freeTimeAt` field stays a later option, if playtests want dryads absent from the evening fire.
- **Djinn keep contracts.** A djinn's bargain binds a service (keep the watch, or forage), not hours. A djinn bound to the watch is asked first (§4.3). *Bound hours* (hours `SetPersonDay` cannot change, drawn locked in the strip) wait until a bargain fixes hours.
- **The fae cannot lie.** Every bend in this design is spoken: a refusal or a decline is an event. That already fits the fae. If a later slice adds silent shirking (someone accepts the watch and then slips away), the fae are exempt.
- **Vampires** are covered in §4.6.

### 7.2 Culture and faith, `2026-11-09-culture-faith-design.md`

The Schedule offers **overlays**: hours that override a person's planned block for one day, for the people they apply to (the first resolution step in §4.1). Built in slice 2.

**What the Schedule needs from culture-faith**, for each observance:
- an id and name key;
- who observes it (a faith or culture membership the Schedule can test per person);
- when: a calendar day (season and day), or a trigger (a death, the first frost);
- when in the day: a start hour and a length in hours, which is how culture-faith stores festivals (`DuskHour` plus the custom's `hours`), and a kind;
- the event that announces it, which refusals and gatherings cite;
- thoughts for keeping it and breaking it.

**Kinds the Schedule implements:**
- `rest`: Free time with no ordinary work. Building stops too, as the build doc expects in its §7.5.
- `gather`: how the strips show a festival, and how `BlockAt` resolves its hours for its keepers (Free, Source "observance"). **Culture-faith's `Feast` job stays what people do (lead's call).** Slice 2 builds around it and does not replace it, so two systems never both send people to the fire. A festival packs everyone into `SocialSystem`'s range, for chats, deep talks and insults between people who rarely meet. A festival that runs into the Sleep block keeps those already at the fire, because a block change never pulls anyone off the job in hand. Anyone choosing a job after the Sleep block begins goes to bed.
- `vigil`: awake through the Sleep hours at the body or the hearth.

A vigil is where the systems compound. The friends of the dead (opinion 30 or more) skip sleep, so the next day they are worn out and accidents follow. And because they are awake, they are asked first for the watch that night.

**Observances and the watch are settled now,** in slice 1 (§4.3). While a festival runs nobody keeps watch, as culture-faith designed, and a theft then cites the festival. An observance kept by only some excuses only those who keep it.

**The player's choice:** the popup gains "Coming days" (the next ten days' observances, each with who keeps it) and a per-occurrence choice, *Keep it* or *Work through it* (a command `SetObservance(Tick, ObservanceId, Day, bool Keep)`). If the camp works through it:
- observers refuse work ("{a} would not work on {day}.", citing the decision and the observance) and keep it anyway;
- non-observers work;
- anyone who keeps an observer's watch or does their share gets the culture's resentment thought.

This is §9, question 5.

**Daily customs** (dawn prayers, a midday rest) are overlays that recur every day. They show in that person's planned strip, marked as custom. If the player paints over them, the person complies at the culture's mood cost ("kept from the noon rest"). That is the "microinfluence" the founder asked for: small, per person, retold.

### 7.3 Character growth and arcs (fixed decision 12, BIBLE §5)

M0 (ADR-0021, merged at 17be2d5) added the `want`, arc-stage and trait-origin fields, with no director logic. The Schedule adds no fields. It supplies beats with event ids, which M1's trait-mutation tables can name as triggers:
- *mercy given*: `covered_watch` after a sympathetic refusal;
- *mercy received*: being covered;
- *betrayal*: `watch_refused_grudge`, or being covered for out of spite;
- *near-death*: a fight while on watch (the existing combat events);
- *grief*: a vigil (slice 2);
- *command*: the watch captain (§8.3).

Every one is an event, so "every trait change references an incident id" holds.

### 7.4 Build, zones and work

- **Build** (`2026-11-09-build-design.md`):
  - Beds become owned and restore rest 20% faster. The Sleep block keeps people in their own bed all night, which is where that comfort is felt.
  - Building is risky work, so it happens in Work hours, stops for sleep and holy days (build's §7.5), and is doubled-risk when worn out.
  - Build's slice-2 second watch post ("lonely night watches") becomes "one watcher per post per turn". `WatchState` grows from one watcher to one per post. Nothing in slice 1 assumes a single post beyond `Map.WatchPost`.
  - The schedule's soak plans get their own flag, `--days`, beside build's `--plan` and zones' `--zones`, and vary independently of both (§8.1).
- **Zones** (`2026-11-09-zones-design.md`). The schedule says when; zones say where. Whichever lands second writes these three seams (§7.0):
  - **The meeting place.** `FreeTimeSpot` returns the meeting place that zones' §5.4 would choose (the best-liked, skipping any with a grudge target in it), instead of the hearth at radius 2. Zones' `WanderJob` change and this are the same call, and the second track merges them.
  - **The curfew and the watch.** A candidate the home area keeps in after dark is not asked: bound, the post outside, not excused, and not going their own way tonight. The first such skip of the night emits zones' `event.work.stayed_in_watch` ("Oren would not keep watch outside the home area after dark."), once a night for the camp. If the post is empty, that event becomes `GapEvent`, so a theft cites it as zones designs. Species that are `atHomeInDark` (vampires, and the undead if peoples sets it) are never kept in.
  - **One compliance rule** (§4.4). Zones' nightly roll and the schedule's two rolls go through the same `Compliance` helper, so going one's own way reads alike everywhere.
  - Hours on zones ("nobody in the forest after dark") are a later joint feature.
- **Work:** keep_watch priority now means "asked first for the watch", and never means "never asked". The Work popup's hint stays true.

---

## 8. What to build now, later, and how to accept it

### 8.1 Slice 1 (build now)

**Sim:**
- blocks, kinds of day, resolving;
- the three commands and their drafts;
- the job-choice changes in §4.2;
- `WatchSystem` (turns, waking, refusals, covers, gaps, Oathbound and Protective staying, leaving for food, falling asleep at the post);
- the undead at the watch: the `sleepless` day, first pick, renewal, and `[No watch]` as a cause (§4.1, §4.3);
- the observance rule, if culture-faith has landed (§4.3, §7.0);
- lived hours and the overwork costs;
- the day-change and waking rolls through `Compliance`, building the helper if zones has not (§4.4);
- the blood changes in §4.6;
- the stolen-while-unwatched cause.

**Content:** §5 (`rhythms.json` with `diurnal`, `nocturnal` and `sleepless`).

**View:**
- the Schedule popup (both strips, the watch row, last night, different days);
- the inspector's Day section (planned, lived, painting, reset, note);
- the lantern dark when the watcher is asleep;
- `RETURN_OPEN=schedule`;
- `RETURN_PRESS` forms that use numeric block codes (0 sleep, 1 work, 2 free time), so they fit `PressForCheck`'s number parser: `camp:B:F:T`, `night:B:F:T`, `person:B:F:T` (for the open inspector) and `watch:F:T`.

**Runner:**
- schedule plans for the stand-in player, on a flag of their own (lead's call): `run` and `soak` gain `--days default|nowatch|nightwatcher|mixed`. The commands go out on tick 0, after the ration policy.
  - `default` (content as shipped);
  - `nowatch` (`SetWatchHours` 0–0);
  - `nightwatcher` (Mira's own day: sleep 07:00–15:00, work 15:00–19:00 and 22:00–07:00).

  `mixed` picks by (seed / 36) % 3. That keeps it independent of the ration policy (seed % 3), build's `--plan mixed` ((seed / 3) % 3) and zones' `--zones mixed` ((seed / 9) % 4). The doc's earlier (seed / 3) % 3 would have moved in lockstep with build's plans. `run` and `soak` share the plans, as ADR-0017 requires, and nightly CI adds `--days mixed`.
- the soak's Days block;
- differing days in the story log.

**Tests** (xUnit, in new `ScheduleTests` and `WatchTests` unless noted):
1. The default days load from content; vampires resolve to the night folk's day; resolving wraps past midnight; own hours override the camp's and reset restores it.
2. **The Sleep block keeps people in bed.** A rested human in bed at 23:00 is still asleep at 05:00 and up after 06:00. A starving one gets up.
3. **Free time skips ordinary work.** At 20:00, with berries to forage, `JobPicker` does not pick forage, but does pick care for a downed companion.
4. **`WatchAtNight` (WorkTests) is rewritten against `WatchSystem`, not deleted.** It keeps the same intent: at a watch hour, one awake, willing person takes the post within one `WatchSystem.Step`. `JobPicker.Choose` no longer returns `KeepWatch`. Batch 1's watch-driven tests are rewritten the same way and keep their intent: priority 0 takes the turns away, priority 3 gives them back. These are `TakingAnUndeadsWorkAwayFadesThemToRest` and `LettingThemWorkAgainBringsThemBack`.
5. **A handover.** Two humans, watch 22:00–06:00. At 02:00 the watcher emits `went_to_wake`, `GapEvent` is set during the walk, the sleeper takes the post (`took_watch` cites the wake event), and the old watcher goes back to bed.
6. **Each refusal reason fires with its cause:**
   - Anxious at health 650 → hurt, citing `LastHealthEvent`;
   - tired → citing `LastDutyEvent`;
   - a grudge against the waker → citing the grudge;
   - their own way → the `WatchDefy` draw through `Compliance`, with the same result for the same seed, and an Oathbound witness within 8 tiles gets `saw_rule_broken`.
7. **Covers.** A refusal for a good reason, when the keeper owed the refuser care, gives `debt_repaid`. With no debt, `owes_watch` carries a `DebtFormed` effect. An unsympathetic refusal gives `covered_for`, and a grudge at −40.
8. **Oathbound stays one turn, then leaves.** Desperate leaves for food at 210 (`left_watch_hungry`, `GapEvent`). A watcher at rest 50 falls asleep, the thief is not spotted, and `stolen_unwatched` cites `fell_asleep_on_watch`.
9. **`worn_out`** cites `LastDutyEvent`. `JobRunner.RollAccident` uses double odds while worn out, and the accident cites `worn_out`. To test it, pass a `WorkDef` with `AccidentPermille` 300 to `RollAccident` and count accidents over a few hundred seeded calls, for a worn-out character and a rested one: the ratio is about 2, and only the worn-out accidents use the `_tired` text. No stub or new seam is needed.
10. **A story through the watch:** scripted refusal → empty post → theft → went without → took meal → grudge. `StoryAnalyzer` finds one story containing the refusal, with Social, Incident and Needs.
11. **Going their own way over a day change.** For some seed, an Independent's `SetPersonDay` gives `declined_day_own_way`, citing the command, and the strip is unchanged. For some seed, someone who is not Independent, at opinion −60 of you, declines with the `_dislike` text. The player's own character never rolls.
12. **Determinism (DeterminismTests):**
    - same seed plus schedule commands gives the same hash;
    - a snapshot taken mid-handover (a `WakeForWatch` walk in progress) restores to an identical future;
    - serial and parallel soak outputs stay byte-identical.
13. **Content:** schemas validate `rhythms.json`. `ContentDb` rejects an hour covered twice or not at all, a species naming an unknown kind of day, and a species whose `tires` disagrees with its kind of day.
14. **Year invariants:** `Lived` and `CampDay.Hours` always have 24 entries, and blocks are valid.
15. **GameViewTests:**
    - `DayStrip` maps x to hour and paints ranges, including wrapping;
    - `ViewText` summaries read as the sketches do;
    - `OrderDrafts` keeps one draft per slot and skips unchanged drafts;
    - the lantern is lit only for an awake watcher.
16. **The undead at the watch** (WatchTests, with Ketil).
    - He resolves to the sleepless day and is never put to bed.
    - Bound to the watch with purpose below 600, he is asked before an awake priority-1 human who has waited longer.
    - He keeps the post past the turn's end until his purpose reaches 600, then hands over.
    - `SetWatchHours` 0–0 becomes the first cause on the chain to his `purpose_low`.
17. **A festival night** (only once culture-faith has landed).
    - At dusk on a festival day nobody is put on watch, in that tick or for the festival's hours. The test also runs `WatchSystem.Step` after the hourly block, to prove the tick order cannot change it.
    - A thief in that window steals, and `stolen_unwatched` cites the festival's `begins` event.
    - When the festival ends, a camp with a vampire puts the vampire on watch. A camp of humans only goes to bed and leaves the post empty until dawn.

### 8.2 Soak measures (`tools/soak`, 100 worlds × 1 year, the plans above)

The soak summary prints a Days block.

**The baseline is the merge base, re-measured (lead's call).**
- **The c818511 column** is §1.1's: the design's evidence, not the bar.
- **Measuring the base.** Before slice 1's first sim commit, the builder runs the §1.1 probe and `tools/soak --worlds 100 --years 1` on the merge base, with the flags nightly CI uses there. The numbers go into ADR-0026 as the *base*.
- **Which targets compare against it.** Targets marked *base* compare against it. The rest are absolute and stand as written.
- **The base has already moved.** Growth M0 reads 8.47 stories per world-year (ADR-0021). Peoples batch 1 reads 10.84 at fc9af62. So ADR-0017's 8.5 is no longer the bar, and the 87% coverage and 9.4 h turn were measured without an undead.
- **Like with like.** Insults and stories compare the base with slice 1 run with `--days default` on the same seeds, so no plan's own effect counts as drift. The mixed run is reported beside it.

| Measure | c818511 (§1.1) | Target |
|---|---|---|
| Humans' median hours asleep a night | 4.8 | 7 to 9 |
| Human samples awake 01:00–05:00, not on watch | 76% awake at 04:00; half of all humans idle | under 5% |
| Watch hours with someone at the post (`default` and `nightwatcher` plans; festival hours aside) | 87% of night samples | 80% or more |
| Thefts per thief, `nowatch` ÷ watched plans | n/a | 1.5 or more (the decision visibly matters) |
| Watch turn length, 90th percentile | 9.4 h | 5 h or less, with Oathbound stays and purpose renewals aside |
| Worlds with at least one refusal | n/a | 20–70% (see below) |
| Worlds where covering repays a debt | n/a | 15% or more (see below) |
| Worlds with a `worn_out` | n/a (no overwork exists) | 10–50%: a storm, not the weather |
| Insults per world-year | 83.7 | within ±20% of *base*. Free time must not repeat ADR-0013's insult flood. |
| Stories per world-year | 8.5 (ADR-0017) | at least *base* |
| Worlds with a story whose chain includes a watch or overwork event | n/a | 15% or more |

If insults rise past the band, tune socialising before anything else (the 400 threshold and the radius-2 gathering), not the fire thought.

**The night folk will keep most watches, and that is the design working.** Vampires live by night. Ketil (batch 1) is in every cast, arriving on days 11–17. He is awake all night and, when bound to the watch, asked first. So once he arrives fewer humans are woken, and refusals, covers and watch debts thin out.
- Report the refusal and cover rows twice: over all worlds, and over worlds whose undead is bound to forage (about half, by Truth). Acceptance reads the second.
- If even that falls short, report the honest number, as ADR-0017 did, rather than weakening the night folk's claim on the watch.

### 8.3 Later slices

- **Slice 2, when culture-faith lands:** overlays (rest, gather, vigil), "Coming days", *Work through it*, daily customs. Festivals stay culture-faith's `Feast` job; the `gather` overlay only shows and resolves their hours (§7.2).
- **With peoples 2A and later:**
  - `nocturnal` for the ghoul, the dokkaebi and house spirits (content only);
  - the werewolf's moon-nights row (2D);
  - djinn bound hours, if a bargain ever fixes hours;
  - a free-time place for dryads, if playtests want one.

  `sleepless` moved into slice 1 (§4.1), and the duty-hours read for purpose was dropped (§7.1).
- **Later:**
  - a fixed body clock (question 6);
  - a watch captain: the court layer, a companion who keeps the rota, whose likes and grudges decide the late turns (fixed decision 2 at kingdom scale);
  - a choice of turn length;
  - a watcher's shout that wakes sleepers to fight;
  - one watcher per post when build adds posts;
  - "a promised day of rest" as a recruitment promise (the demo had "time to rest");
  - hours on zones.

### 8.4 Screenshots

- `RETURN_OPEN=schedule RETURN_PAUSE=1`: the popup sits clear of the inspector (BottomBar's `Place` already does this) and the strips read at 1280×800.
- `RETURN_SELECT=ilex`: daylight is hatched and the note reads in plain words.
- `RETURN_SELECT=mira` with the `nightwatcher` plan, a day in (`RETURN_TICKS`): her lived strip shows the night watch.
- A night at the post with the watcher asleep (`RETURN_FOCUS` on the watch post): the lantern is dark and the zzz mark shows.
- `RETURN_PRESS=camp:0:21:6` paints bedtime through the real signals, and the screenshot shows the draft and "Takes effect when time moves."

---

## 9. Questions for the founder

Most important first, each with my recommendation and why it matters. Everything a builder could settle without changing what gets built is decided in the body and marked "(lead's call)". That covers the merge order and baselines, the undead, festivals and the watch, the shared compliance rule, the draw ids and ADR number, and `DebtKind.Watch`.

1. **A per-person day, painted in that person's inspector: does it sit right with fixed decision 2?** It is an intention for one named person. They can decline it, and needs and the body always override it. A people-by-hours grid never appears.
   *Recommendation: yes. It is the same kind of intention as a work priority, set in the same place.* Why it matters: without it there is no night shift, and flow 3 and the `nightwatcher` plan go.

2. **May people go their own way over the schedule?** That means declining a change to their day, or refusing to be woken for the watch for no reason but their own. The rule would be the one zones uses (§4.4): an Independent 30% of the time, more if they dislike you or are in a black mood, never the player, and always spoken in the Chronicle. This merges the earlier question on Independents declining. How they decline is settled as one rule shared with zones.
   *Recommendation: yes. It is what "needs a better reason to accept someone else's plan" means in hours.* Why it matters: a no removes both schedule rolls, their events and the witnesses, and leaves the watch's refusals to need and grudges alone.

3. **Should the camp keep a night watch by default** (19:00–06:00 in four-hour turns), or nobody until the player says so?
   *Recommendation: on, with "No watch" one click away. New players aren't robbed blind in the first week, and the watch's costs still teach themselves.* Why it matters: it sets the shipped content and the `default` soak plan.

4. **Needs always beat the schedule, and overwork hurts only indirectly:** doubled accident odds, sleep at the post, no healing, worse mood. It is never a health drain or a death from exhaustion. Is that the tone?
   *Recommendation: yes. A death should have a story cause, not a meter that ran out.* Why it matters: a direct drain would add a death path with no other system in its chain.

5. **Holy days (slice 2): may the player ask the camp to work through one, and do observers then refuse openly rather than work slower?**
   *Recommendation: yes and yes. The faithful refuse and keep the day; others work and may resent covering for them. The player's power ends where a person's faith begins.* Why it matters: it decides whether slice 2 adds `SetObservance` and its refusals.

6. **A fixed body clock (early riser or night owl), in a later slice?** It would be a new Truth, `TruthId.Chronotype = 70` from this track's block, keyed by cast key.
   *Recommendation: yes, after culture-faith and peoples land.* Why it matters: it touches the determinism surface, and because it survives rewinds, a player who has died learns who suits the late watch.
