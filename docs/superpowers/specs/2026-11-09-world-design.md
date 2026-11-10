# The world before you, and the path to dominating it: design

- **Date:** 2026-11-09
- **Status:** proposed, for the founder's sign-off. *Revised after the builder's read:* cross-references to the player, loot-economy, peoples and culture-faith designs are brought up to date; questions the project's documents already answer now sit in the text where a builder looks; small calls that don't change what gets built are marked "(lead's call)". §9 holds only what the founder must decide.
- **Covers:** the world that exists when you arrive, and how it is generated, shown, kept alive off-screen and connected to the camp. Also politics, war, what total domination means and how the storyteller paces it, and how rewinds treat the world.
- **Reads with:** CLAUDE.md (main principle; fixed decisions 1–10 and 12); BIBLE.md §2, 4.2, 4.6–4.10, 5; ADR-0004–0009, 0013, 0017–0019, 0021, 0024 and 0025; ADR-0022 and ADR-0023 on their branches; the other specs in this folder, including `2026-11-09-player-design.md` and `2026-11-09-loot-economy-design.md`, written alongside this one (§7 lists the seams). The player design leaves three things to this one: where you wake, which peoples a world holds, and the domination test that ends a run in victory.
- **Signing approves** the first slice, W1 (§8), and nothing later:
  - seven content files with schemas (§5), plus `loc/en.world.json` and new entries in `thoughts.json`, `incidents.json` and `rebirth.json`;
  - `World.Realm`, four character fields, the `Abroad` and `Visiting` statuses, `JobKind.Visit`, `DebtKind.Tribute` and the `AnswerEnvoy` command (§4). `Abroad`, `Visiting` and `JobKind.Visit` are shared with loot-economy's peddler; whichever design lands first adds them;
  - two system tags, six effects and two Dice streams (§4.10, §6);
  - TruthIds 120–129 and DiceIds 300–314 (§6).

  Later slices bring their own lists to their own sign-off. Until a question in §9 is answered, the builder builds its recommended answer.

**In short**
1. **Two grains.** The camp stays a tile map run every tick. The world is a coarse grid of regions whose realms, settlements and about 45 named rulers and courtiers run hourly, daily and seasonally. Everyone else in the world is a number.
2. **Seeded once.** Terrain, 5–7 realms built on culture-faith's cultures and faiths, their rulers' natures and a short history are Truths. What happens after you arrive is Dice.
3. **Politics runs on the existing relationship graph.** Rulers hold opinions, grudges and debts of each other, and swear oaths, which are a new first-class record. A realm's stance toward you is its ruler's opinion of you.
4. **Map-first.** Zoom out past the camp to see the world; the bar does not change. The world reaches the camp only through people and word: envoys, raiders, refugees, news.
5. **A measurable win.** Every standing realm is held by you or sworn to you, at one moment, while you live. Companions act on what you ask, and they can refuse.
6. **First slice, W1.** In W1a the world lives while the camp's logs stay byte-identical. In W1b it reaches the camp.

---

## 1. Why: the stories

Today every story stays inside one camp. The main principle asks for "pre existing civilizations" where politics, culture and war "cause events through butterfly effects". The camp proves the engine (a day-1 ration policy decides who leaves on day 35, STORIES.md); this design gives it a world to reach into.

**The Rushmere tribute** (W1b: World → Politics → Needs → Politics → Social → War → Combat → Work → Social)
```
Day 1, 00:00 — You decided that when food runs short, the hungriest eat first.
Day 9, 16:20 — Kesh, envoy of the Rushmere Crown, came to the camp.
Day 9, 16:25 — Kesh asked you, in Oswy's name, for 4 berries at every turn of the season.
Day 9, 17:05 — You agreed to pay the Rushmere Crown 4 berries each season.
Day 31, 00:00 — Winter came, and frost took the berries from the bushes.
Day 31, 06:00 — Kesh came for the tribute.
Day 31, 07:00 — The hungriest ate first, and nothing was left for the tribute.
Day 31, 19:00 — Kesh left empty-handed. You broke your word to the Rushmere Crown.
Day 31, 19:00 — Oswy now holds a grudge against you.
Day 36, 02:10 — Raiders of the Rushmere Crown crept toward the camp, led by Kesh.
Day 36, 02:40 — Kesh struck Mira down.
Day 36, 03:05 — Hollis tended Mira's wounds.
Day 36, 03:05 — Mira owes Hollis for tending their wounds.
```
Under "promised meals come first" the tribute takes the berries and somebody goes without instead. The same day-1 choice decides whether the camp keeps faith with a king or keeps itself fed. Kesh leads the raid because the envoy who went away unpaid comes back as captain (§4.6).

**The one who came back** (W1b: Social → Needs → Social → War → Combat → Social)
```
Day 2, 14:32 — Sera agreed to stay after you promised them a meal within 2 days.
Day 3, 07:00 — Sera went without so others could eat.
Day 3, 07:00 — You broke your promise to Sera.
Day 3, 07:00 — Sera now holds a grudge against you.
Day 3, 08:00 — Sera left the camp.
Day 3, 08:00 — Sera made for Rushmere.
Day 27, 01:30 — Raiders of the Rushmere Crown crept toward the camp, led by Sera.
Day 27, 01:45 — Vale spotted Sera.
Day 27, 02:20 — Sera was killed at the stockpile.
Day 27, 02:20 — Oswy now holds a grudge against you.
```
The raid cites Sera's departure and the forming of her old grudge, so the chain runs back into the camp (§4.6).

**The heir who would not kneel** (W3: Politics → Social → War; multi-year runs add the year to the time format)
```
Year 6, Day 3 — Oswy of the Rushmere Crown swore loyalty to you.
Year 9, Day 22 — Oswy died. Edric now rules the Rushmere Crown.
Year 9, Day 22 — Edric does not hold to Oswy's oath; the Reed Folk take a promise lightly.
Year 9, Day 22 — With that, the Rushmere Crown no longer bends to you.
Year 9, Day 26 — You asked Oren to bring the Rushmere Crown back under you.
Year 9, Day 26 — Oren, one of the Reed Folk, refused to make war on them.
Year 9, Day 27 — You asked Mira instead.
Year 9, Day 28 — Mira marched on Rushmere with thirty spears.
Year 9, Day 35 — Mira laid siege to Rushmere.
Year 10, Day 4 — Rushmere opened its gates. Edric swore loyalty to you.
```

## 2. Goals, non-goals, anti-goals

**Goals**
1. **A world that is already there:** realms, peoples, faiths, rulers and old grievances exist before tick 0, as Truths (fixed decision 4).
2. **Butterfly effects with causes:** every world event cites what brought it about, reaches the camp through people, touches two or more systems and ends in changes the analyzer counts (fixed decision 1).
3. **Intimacy at world scale:** politics happens between named people with the camp's traits, opinions, grudges and debts; the realm is run by asking companions, who decide (fixed decision 2).
4. **A measurable win** (§4.9).
5. **Cheap off-screen life:** the realm's day under 1 ms, which is under 1% of ADR-0005's 144 ms camp-day budget; restore still under 1 s at 200 characters.

**Not in W1:** everything in §8.3's later slices; tile maps of anywhere but the camp; goods beyond berries and timber (loot-economy).

**Anti-goals (never)**
- **No spreadsheet:** no realm ledger, no diplomacy numbers, no army roster. A realm's strength, food, opinions and armies reach the player only as words; the one count shown is the dominion count.
- **No unit control:** you never move an army or order a battle.
- **No faceless strategy layer:** every realm is a ruler and a court, and every war has named captains.
- **No people is evil by nature:** a raid is a ruler's act in a situation (peoples rule 7).
- **No Earth realms, religions or ethnic stand-ins.**
- **No language model:** the "AI storyteller" is fixed decision 8's rule-based director, and all text derives from stored state (fixed decision 10).

## 3. How the player uses it

**One screen, two scales.** Today the wheel zooms out until the whole camp fits (the `CameraRig.FitZoom` floor).
- Zooming out past that point cross-fades to the world map, centred on the camp's region. Zooming back in over the camp cross-fades back. There is no World button, because the bar is fixed (ADR-0008); the M key also switches scales, since a key adds nothing to the screen (lead's call).
- The bar is unchanged. Build, Zones, Work and Schedule act on the camp and return you there; Orders, People and Chronicle work at both scales (Orders is a lead's call: it holds your promises to realms).
- The world map is a square grid of regions (40 × 26 to start, faint grid lines) in the grounded dark style (ADR-0009, ADR-0019). Olive plains, near-black forest, slate water, worn tracks. Settlements are clusters of roofs; a seat has walls and a banner. Each realm's land is edged in its muted colour, and the camp is drawn as its hearth. Bands are small figures (an envoy with a staff, raiders under a banner), and a burned village smokes. Everything is drawn from sim state.

```
+---------------------------------------------------------------------------------------+
| ~~ [#] Rushmere .. TT .. .. TT TT [#] Greyhollow .. .. ^^ .. .. .. .. .. .. .. .. ..  |
| ~~ .. o .:.:.:.:.: .. TT .. .. .. o .. .. ^^ .. .. .. .. .. .. .. .. .. .. .. .. ..   |
| ~~ .. .. .. (*) the camp .. .. .. .. >> raiders .. .. .. .. .. .. .. .. .. .. .. ..   |
+---------------------------------------------------------------------------------------+
| Build  Orders  Zones  Work  Schedule | People  Chronicle   Day 14, 06:00  || 1x 3x 6x |
```

**Asking about the world.** Hover a region for a tooltip (`StateApi.Region`: terrain, settlement, ruler, bands passing). Click a settlement for its inspector, the same panel a person opens; click its ruler for theirs (traits, opinion of you, grudges, oaths). Opinions are always words: "thinks well of you", "thinks poorly of you", "holds a grudge against you".

```
+--------------------------------------------------+
| Rushmere, seat of the Rushmere Crown             |
| Ruled by Oswy (Proud, Anxious); heir: Edric      |
| Home of the Reed Folk, most of them human        |
| They keep the Old Roots                          |
| A walled town of some four hundred               |
| Oswy thinks poorly of you                        |
| Sworn to friendship with the Moot of Greyhollow  |
| At war with the Stillwater Council               |
| Your people from here: Wren                      |
+--------------------------------------------------+
```

**When the world comes to the camp (W1b).**
- **Envoys.** An envoy walks in from the edge nearest their seat and waits at the hearth with a "?" overhead. Clicking them opens the question.
  - Your answer is a tick-stamped command, `AnswerEnvoy`, queued while paused like any order (`OrderDrafts`). It is your own voice, as the ration policy is: it commits you and orders no one (fixed decision 2; player design §4.5). Whether your word then holds turns on your people (§4.6).
  - An envoy kept waiting past their time leaves offended.
- **Orders** lists the camp's standing promises, under the ration policy: "We pay the Rushmere Crown 4 berries each season (next due day 21)."
- **Raiders** come in the way the night thief does, but more of them, led by a captain in their realm's colour.
- **The Chronicle** shows only what the camp knows: what happens at the camp, and news once it arrives ("Word reached the camp: Rushmere's raiders burned Ashford").

```
+--------------------------------------------------+
| Kesh, envoy of the Rushmere Crown                |
| Asks, in Oswy's name, for 4 berries at every     |
| turn of the season.                              |
|   [ Agree ]   [ Refuse ]       waits until 22:00 |
+--------------------------------------------------+
```

**Later (W2–W3), still within the bar:** a settlement's inspector offers "Ask someone to…" (visit, scout, recruit, then take or defend), showing who would refuse; People lists each companion's task; the Chronicle's header carries the dominion count ("3 of 7 realms bend to you"), and the map shows it in your gold.

## 4. What changes in the simulation

### 4.1 Two grains

| What | Grain | Where |
|---|---|---|
| Camp movement, jobs, combat | every tick | as today |
| Camp needs, mood, social, incidents, growth | hourly | as today |
| Bands crossing regions; clashes; news reaching the camp (W1b) | hourly (a region takes its terrain's `travelHours`) | `RealmSystem.Hourly`, after `Growth.Hourly` |
| Rulers' acts | every 5 days per realm, staggered by slot | `RealmSystem.Daily` (hour 0) |
| Settlement food, deaths, succession | daily | same |
| Tribute due; harvests | each turn of the season | same |

Only the camp has a tile map. A settlement is a record holding counts and the ids of its named people.

### 4.2 Generating the world (Truths only)

`RealmGen.Seed(w)` runs in `WorldGen.Create` after the camp is built and after culture-faith's `CultureGen.Seed`, and before loot-economy's market and peddler seeding when that has landed (lead's call). It reads only `w.Rng.Truths`, keys each draw by a region or a stable string (never an entity id), and emits no events.

1. **Terrain:** lattice noise for elevation and moisture (as `BuildMap` makes soil), banded into `terrain.json` kinds; two or three rivers run to the sea.
2. **Realms (5–7):** each takes a culture from culture-faith's seeded cultures (each used once before repeats, so two realms can share one and be rivals), a government weighted by the culture's axes, the culture's main faith, and a seat at least 7 regions from other seats. The seat is named from the culture's place names (`names.json`), the realm from its government's template ("the {seat} Crown").
3. **Settlements:** 2–4 holdings per realm within 5 regions of its seat; 0–3 free villages; ruins and lairs. Dungeons are the loot design's. Roads are the cheapest paths over terrain from each holding and village to its seat and from each seat to its two nearest seats, found without a draw (ties by region order); bands follow them, and the map draws them as worn tracks (lead's call).
4. **Arrival (where you wake):** a wild region 5–10 regions from the nearest seat. Its local map is today's ruined camp with Mira and Oren, keyed as today, so every M0 camp is byte-identical.
5. **Peoples:** a world holds the peoples the peoples design's presence draw puts in it (`TruthId.CastPresence`, its question 2, settled by ADR-0025). Until its slice 2B builds that draw, every shipped people is present (lead's call), so W1a leaves the player's roll unchanged. You are rolled from the present peoples (player design §4.1), and every species draw below is among them. Populations are counts split by each culture's `peoples` weights.
6. **Notables:** a ruler, heir, marshal and envoy per realm, and a castellan per holding (about 45). Each draws a species from the culture's weights, two traits, an unrepeated name, and faith and devotion as culture-faith §4.5 does.
7. **History:** 8–16 `Legend` records (old wars, truces, marriages, feuds, a burning, a friendship or tribute, at most one war still going). On the first `RealmSystem.Hourly`, each legend's event is emitted, and *then* its grudge, oath or war is formed citing it. `EventLog.Emit` rejects a cause that is not yet an event, so nothing can form at seeding.
8. **Homes:** each cast member's home is a settlement of their culture's realms, or the wilds.

**A realm's axes** follow culture-faith §4.6's faction rule, computed and never stored:
- `base = seatCulture.axis + stateFaith.axis × courtDevotion / 100`, where `courtDevotion` is the mean devotion of the living notables;
- `axis = clamp(base + (ruler.axis − base) / 2, −2, +2)`, with integer division.

Realms name culture-faith template ids, so changing culture-faith's content reshapes realms.

| Axis | In politics |
|---|---|
| Mortal / Immortal | Mortal courts prize heirs. Immortal courts may keep an ageless ruler who never needs a successor (`undying`), and their grudges fade at half speed. |
| Arcane / Mundane | No effect in W1. From W2, Arcane courts read world events as signs (culture-faith's omen rule, M1), drawing on the Politics stream and never culture-faith's `OmenRead`; Mundane courts favour practical friendships. |
| Wild / Civil | Wild courts raid. Civil courts send envoys, demand tribute and build walls. |
| Oathbound / Free | Oath weight is `100 + 25 × axis` % (culture-faith O1). The axis also decides whether an oath outlives the ruler who swore it (§4.5). |

Signs follow culture-faith: positive is Mortal, Arcane, Wild or Oathbound. The peoples doc's leanings read Immortal as positive and must be flipped when they reach content.

### 4.3 State

Everything lives in `World.Realm`, a `RealmBook`. Lists are kept in id order, with no dictionaries. Non-person records take ids from `RealmBook.NextId`, so they never shift a character's id.

| Record | Fields |
|---|---|
| `RealmBook` | `Width`, `Height`, `Terrain` (byte per region), `Rivers`, `Roads`, `Sites`, `Polities`, `Oaths`, `Wars`, `Bands`, `Visits`, `News`, `Legends`, `CampSiteId`, `NextId` |
| `Site` | `Id`, `Key`, `Kind`, `Cell`, `NameKey`, `PolityId`, `Population` (species, count), `Food` (days of supply), `Walls`, `BurnedTick`, `CastellanId` |
| `Polity` | `Id`, `Slot`, `NameKey`, `SeatId`, `GovernmentId`, `CultureId`, `FaithId`, `RulerId`, `HeirId`, `Weariness`, `NextCourtTick`, `KnowsCampSince` |
| `Oath` | `Id`, `KindId`, `SwearerId`, `SwornToId`, their polity ids (0 = personal), `Terms`, `CauseEvent`, `FormedEvent`, `EndsTick`, `Status` (Open, Kept, Broken, Lapsed, Released), `ClosedEvent` |
| `War` | `Id`, `AttackerId`, `DefenderId`, `Goal`, `CauseEvent`, `BeganEvent`, `EndedEvent` |
| `Band` | `Id`, `Kind` (envoy, raid; later expedition, army), `PolityId`, `LeaderId`, `MemberIds`, `Strength`, `Route`, `Progress`, `TargetSiteId`, `CauseEvent`, `WaitUntilTick` |
| `Visit` (W1b) | an envoy at the camp: `Id`, `CharacterId`, `PolityId`, `LeavesTick`, `Ask` (oath kind, terms), `AskEvent`, `Answer`, `AnswerEvent` |
| `NewsItem` (W1b) | `EventId`, `ArrivesTick`, `Delivered` |
| `Legend` | `Id`, `KindId`, `ActorIds`, `SiteIds`, `YearsAgo`, `Event` |

`Oath` is general on purpose. It covers truce, friendship, tribute, alliance and fealty now, and has room for culture-faith's sworn vows (O4) later. Batch 1's djinn bargains (`World.Contracts`) stay separate unless they prove to be the same thing.

### 4.4 Notables are people

- **Characters.** Notables are `Character`s with `Named = true` and a new appended status, `Abroad`. They gain four fields: `PolityId`, `SiteId` (0 = the camp), `HomeSiteId` and `Office`. They have traits, opinions, thoughts, grudges, debts and oaths like anyone else.
- **Keys and natures.** A notable's `CastKey` is `n:<slot>:<office>:<generation>`. `Relations.NatureKey` hashes it, so the chemistry between a ruler and Mira is a Truth, and a given generation's successor has the same nature in every timeline. `RealmGen` draws natures from `NotableNature`, never through `WorldGen.NatureOf` (which calls `CastByKey`).
- **Ids.** Notables are created after the camp in `WorldGen.Create`, so the cast, beds, trees and bushes keep their ids. Ids made later shift, but nothing printed depends on them (W1a's test).
- **Loops.** Systems filter by `Member`, `IsActive` or `Hostile`, which `Abroad` is not. Four readers change:
  - `StoryLog.WriteCast` lists only former and current camp members (rulers get a "The world" section);
  - the story log's counts line counts camp events and stories only, and world stories get a line of their own;
  - `TickCostUnderBudget` divides by characters on the local map, not `w.Characters.Count`, with a separate budget for the realm's day;
  - the 200-character restore test counts notables.
- **Thoughts and wants.** `MoodSystem.Hourly` skips notables, so `RealmSystem.Daily` prunes their thoughts. They have no wants in W1. Rulers' wants come in W2, as `wants.json` entries weighted by office.
- **At the camp.** An envoy is `Visiting`, the status loot-economy's peddler also uses. `IsActive` gains `Visiting`, so they walk; rations read `Member` and guards read `Hostile`, so they are never fed from the stores or attacked; `SocialSystem.CanTalk` admits them, so they chat like anyone. A raid captain is `Hostile`, like the thief.

### 4.5 Off-screen life (W1a)

**Rulers' acts.** Every 5 days each ruler takes at most one act, from two draws on the Politics stream: a `CourtAct` pick over the weights below, then an `ActTarget` pick weighted by grudge or affinity. The weights are `RealmTuning` constants (as ADR-0013 does for tuning). Each act cites its reason.

| Act | Only when | Weight rises with | Result |
|---|---|---|---|
| Offer friendship | not at war; opinion of the target's ruler ≥ 0 | Generous, Practical; Civil; shared culture or faith | an audience |
| Demand tribute | strength ≥ 2 × the target's | Proud; a grudge; Civil | an audience |
| Raid | at war, a grudge ≥ 40, or a Desperate ruler in famine; and no truce, friendship or tribute oath | Wild; Proud, Desperate; the grudge | a raiding band |
| Declare war | grudge ≥ 60; strength ≥ the target's | Proud, Independent; Free | `War` (`WarBegan`) |
| Make peace | at war; weariness ≥ 50 | Anxious, Practical; Oathbound | a truce (`WarEnded`) |
| Break an oath | an open oath to a ruler they hold a grudge against | Free; Independent, Proud | `OathBroken` and the wronged ruler's grudge |
| Nothing | always | base weight (most turns) | — |

**Audiences come from state, not Dice,** so a refusal reads as a choice (the peoples design's "ask, and be answered from state", its §8.3).
- The ruler asked accepts when `opinion + 10 × chemistry + 15 (shared culture) + 15 (shared faith) + 25 × min(strength ratio − 1, 2) + traits ≥ acceptAt`, with `acceptAt` set per oath kind.
- The trait terms: Proud −30 against tribute, Anxious +20 toward it, Independent −20 against any oath.
- Accepting swears the oath. Refusing a tribute demand gives the asker a grudge.

**Raids.** A named captain, usually the marshal, leads an unnamed strength of a tenth of the seat's people across the regions.
- **The clash.** At the target they fight its defenders, a tenth of its people, in up to six hourly rounds on the War stream. Each round, each side loses up to a quarter of the other's strength, and walls halve the defenders' losses.
- **Breaking.** A side breaks at half its strength lost. A Proud leader holds to two thirds; an Anxious one breaks at a third.
- **Named leaders.** Each round, each takes a blow with chance 150‰, for 60–140 damage (`CombatSystem`'s numbers). At 0 they die; below 150 they are carried off and their side breaks.
- **Events.** A raid makes at most four:
  - plunder (food moves to the raiders' seat);
  - driven off;
  - burned, only when Wild-leaning raiders, or raiders holding a grudge of 60 or more, win outright: the population falls as people flee, not by slaughter (`SettlementBurned`; lead's call, matching question 3's recommendation);
  - a captain hurt or killed.

**Deaths and succession.**
- **Death.** Until the player design's aging lands, rulers and heirs die at 1‰ a day ("Oswy died."); others die only in raids.
- **The heir rules** (`RulerChanged`), and the government's law names the next: `heir`, `council` (the courtier who thinks best of the ruler), `strongest` (the marshal) or `undying` (none). A next-generation notable fills the empty office.
- **The dead ruler's oaths** depend on the realm's Oathbound/Free axis:
  - at 1 or more, the heir keeps them;
  - at −1 or less, they lapse (`Lapsed`): opinion falls, but no grudge forms;
  - at 0, the heir keeps an oath only if they think at least neutrally (≥ 0) of the other party.
- **Oaths sworn to a realm**, such as the camp's tribute, stay with the realm.

**Food** is counted in days of supply. Harvests at the turns of summer and autumn add days by terrain, and each day uses one. At zero, the population falls, and a Desperate ruler raids.

### 4.6 Contact with the camp (W1b)

- **The camp as a target.** In rulers' acts the camp counts as a realm whose ruler is You (`w.PlayerId`), whose strength is its members, and whose axes are 0 (the outsider). Each realm learns of it on a Truth-set day (`Polity.KnowsCampSince`), sooner if near, and a cast member's home realm from that member's arrival. Only realms that know of it aim acts at it.
- **Envoys.** A friendship or tribute act aimed at the camp sends the envoy, who enters as `Visiting`, walks to the hearth (`JobKind.Visit`), asks, and waits 6 hours. Agreeing swears the oath (You to their ruler). Refusing tribute gives the ruler a grudge; refusing friendship costs opinion. No answer: "Kesh left, unanswered", and opinion drops. The Proud and Independent think less of you for bowing, the Anxious and Practical better for peace.
  - The command is `AnswerEnvoy(Tick, VisitId, Agree)`, tagged `answerEnvoy` in `sim/Commands.cs`. Leaving an envoy unanswered needs no command, and an answer that reaches an envoy who has gone does nothing (lead's call).
- **Tribute.** Each turn of the season raises a claim (`DebtKind.Tribute`: debtor You, creditor the collecting envoy, due at dusk) that joins the share-out (`Rations.Assign`) as a promise does: first under "promised meals come first", after everyone hungry under "the hungriest eat first", right after You under "you eat first". The Desperate can take its set-aside berries like any set-aside meal. That theft forms no grudge in the collector: the broken oath cites it, and the grudge it brings is Oswy's against You. At dusk it is paid (`DebtRepaid`, opinion rises) or broken (`DebtBroken`, `OathBroken`, the ruler's grudge against You). Whether your word holds turns on your policy and your people: delegation from the first slice.
- **Raids on the camp.** A captain and 1–3 unnamed raiders wait near the camp for up to 3 days, until the director would allow a major incident at night (pressure ≥ 50 and `MinHoursBetweenMajors`). They enter from the edge facing their seat, a tile picked by rule rather than through `EdgeTile`, as `Hostile` with `Raid` jobs, and `CombatSystem` treats them like the thief. Once inside, the thief path draws on the Combat and Incident streams (`Exchange`, `EdgeTile` when fleeing); W1b accepts that, since contact is open. The raid pays `MajorCost`; its loot feeds the realm; a captain's death gives the ruler a grudge against You.
  - **The captain** is a former member who left with a grudge against You, and the raid cites their departure and that grudge's `FormedEvent` without reviving the grudge; else the envoy you refused, left unanswered or failed to pay, and the raid cites that; else the marshal (lead's call for the envoy).
- **Leavers go somewhere.** After `left_camp`, a leaver makes for their home if it stands, else the nearest settlement whose culture neither fears nor scorns their people (culture-faith A1), else they are gone, as now ("Sera made for Rushmere."). Their grudges stay ended (ADR-0018). Former members without a grudge are preferred as envoys.
- **Refugees.** A scheduled wanderer whose home was raided or burned in the last 20 days arrives "from Ashford, fleeing the raid", citing it; timing and mechanics are unchanged. New people from the world come in W2.
- **News.** A ruler's death or succession, a war begun or ended, a burning or a broken oath, within 12 regions or from a realm that knows the camp, arrives after distance × 3 hours as a `World` event citing its source. "My home was burned" (−12 mood, 4 days) touches whoever came from there; "Our ruler is dead" (−4) touches that culture's members.

### 4.7 Politics after W1 (W2)

You ask a companion to carry an offer (friendship, alliance, tribute, recruits, later loyalty). They accept, bend it (an Independent may turn "demand loyalty" into "offer friendship") or refuse, travel as a band, and the audience resolves by §4.5's rule plus culture's hooks: greeting faux pas (G4), kin warmth (G1), the court's stance toward the envoy's people (A1), and gifts. You may swear oaths yourself, including loyalty to a stronger realm for protection: an arc to keep, outgrow or break. Recruits and refugees join under the cast cap of twenty (peoples question 2). A companion vouches for you in the realm they come from; one whose home you raid may leave, and their kin may follow (culture-faith K1).

### 4.8 War (W3)

- **Goals and armies.** Wars are fought for plunder, a holding or an oath (later, holy war). Armies are bands of named captains and unnamed strength; levies cost settlements food, so long wars breed famine.
- **Battles** resolve at the world grain unless your people are there; then attackers enter the local map under today's combat, and the host beyond about twelve bodies stays a number that sends reinforcements. **Sieges** starve a settlement until it surrenders (an oath or the seat), is stormed or relieved, or the besiegers tire.
- **Conquest** (`HoldingTaken`) leaves a population its culture, whose customs your steward may offend. **Every holding needs a companion as steward**, so you cannot hold more towns than you have people you trust; beyond that, realms must be sworn. This is the structural guard against a spreadsheet kingdom.
- **Sworn realms** pay tribute and answer calls to arms (refusal breaks the oath), and rebel when their ruler's opinion of you falls below −40 or an heir lets the oath lapse.
- **Marshals.** Asked to take, defend or raid, a marshal chooses when (an Anxious one waits for two to one, a Proud one goes at even odds), whom (volunteers, by opinion and want) and whether: they may refuse for culture, faith or a grudge against you. You can ride along.

### 4.9 Domination and the storyteller

**What counts.** A realm *bends to you* when it is in one of the end states below. The other routes are roads toward them, not ends in themselves; question 1 asks you to confirm which count.

| Route | Meaning | Counts? (recommended) |
|---|---|---|
| Held | Its seat is your holding, or all its holdings are taken. | Yes |
| Sworn | Its ruler has sworn loyalty to you, or to someone sworn to you, and the oath stands. | Yes |
| Allied under you | It allies with you but has not sworn. | No: a road; it makes swearing easier |
| Converted | Its court or people share your faith. | No: a road; swearing becomes likelier and rebellion rarer |
| Bound by trade | Its markets depend on your goods. | No: a road; it becomes a term in the audience rule (loot-economy §8) |

**The win.** At one moment, while you live, every realm still standing bends to you: "The world bends to you." Realms born later through revolts or rivals count too. Free villages and wild places do not. `StateApi.Dominion` computes the count daily, and the player design ends the run on it (its §6.3).

**Pacing** (lead's call; content numbers in `worldgen.json`, tuned by soak): a 40 × 26 world of 5–7 realms and about 25 settlements, about 10 days to cross by road, paced so that domination is within reach of the player design's life of 40–45 years (27–30 hours at ×6, its §6.2). A faster world-map speed, once you rule more than a camp, is W3's to propose.

**The storyteller at world scale** (fixed decision 8; W3).
- One hidden personality per playthrough, a Truth, sets the world's pressure and incident weights.
- Arc detection follows your path: alone, a band, a village, a realm, a power, dominion. When it stalls, the storyteller seeds a turn: an envoy seeking protection, a ruler's death, a famine, a rival.
- Once a third of the realms bend to you, independent rulers who think poorly of you swear alliance against you.
- The director paces conquest between AI realms like any other pressure: no realm takes another's seat in the first 5 years, then at most one every 10, so the map stays open until you can take it. Raids, oaths and broken oaths stay free (lead's call).

**Fate knots.** The player design ties knots after events whose keys are in `rebirth.json`'s trigger list (its §5.1). W1b adds as content: an envoy's ask, raiders creeping toward the camp, and your oaths sworn or broken. W3 adds wars on you or by you, holdings taken, and the death of a ruler sworn to you, which is the player design's "a known ruler's death" (lead's call).

**In W1** contact goes through the camp's existing pressure meter. A raid is a major incident and an envoy a minor one, so the world cannot drown the camp the way insults once did (ADR-0013).

### 4.10 Events, endings and what Realm touches

**New tags:** `Politics` and `War`. News and envoy answers use the existing `World` tag.

**New effects:** `OathSworn`, `OathBroken`, `RulerChanged`, `WarBegan`, `WarEnded` and `SettlementBurned` in W1, and `HoldingTaken` in W3.

| Event (W1) | Tag | Cites | Effect |
|---|---|---|---|
| Legend (told on the first hourly step) | Politics | — | none; what it sets up cites it |
| Envoy sent; sworn or refused; oath broken or lapsed | Politics | the act's reason; the grudge, unpaid claim or succession | `OathSworn` / `GrudgeFormed`; `OathBroken` |
| War declared; peace made | War / Politics | grudge; weariness | `WarBegan`; `WarEnded` |
| Raid; plunder; driven off; burned | War | the act; a departure | `SettlementBurned` |
| Captain or ruler killed; ruler died; now rules | War (Combat at the camp); Politics | the clash; the death | `Death`; `RulerChanged` |
| Envoy asks; you agree, refuse or leave it unanswered | Politics; World | the act; the command | `OathSworn` / `GrudgeFormed` |
| Tribute due, paid, unpaid | Politics | the oath; the share-out | `DebtFormed` / `DebtRepaid` / `DebtBroken` + `OathBroken` |
| Made for {settlement}; refugee arrives; news | Social; Incident; World | departure; raid; source | —; `Recruitment`; — |

**Legends** follow Growth's precedent (ADR-0021 point 7): records at creation, events on the first `RealmSystem.Hourly` (§4.2), since the Chronicle starts empty and hooks never see creation-time events. Their chains are too short to count as stories.

**Where an event belongs.** Off-screen events take keys under `event.realm.`. The camp's Chronicle and the runner's camp sections leave them out. Events at the camp (envoys, tribute, raids on the camp, news) use other keys and belong to the camp. The byte-identical check in §8.1 rests on this rule.

**The cut rule.**
- **Realm reads:** social (opinions, grudges), culture (cultures, faiths, axes, stances), the director (pacing), rations (claims) and combat (raids).
- **Realm writes:** incidents (envoys, raids, refugees), social (grudges, where leavers go), mood (news), rations and combat.
- BIBLE §4.2 gains a Realm row.

## 5. Content and schema changes (W1)

Each new file gets a schema with `additionalProperties: false`. Each is a top-level array merged by id across mod folders, except `worldgen.json`, which is one object, as `world.json` is.

| File | Holds |
|---|---|
| `worldgen.json` | `width` 40, `height` 26, `realms` {5, 7}, `holdings` {2, 4}, `holdingRadius` 5, `freeVillages` {0, 3}, `seatMinDistance` 7, `arrivalSeatDistance` {5, 10}, `legends` {8, 16}, `courtEveryDays` 5, `newsHoursPerRegion` 3, `newsReach` 12. A separate file leaves `world.json`, which sibling branches edit, untouched. |
| `terrain.json` | `id`, `nameKey`, `travelHours` (0 = impassable), `settleWeight`, `harvestDays`, elevation and moisture bands |
| `sites.json` | `id`, `nameKey`, `population` {min, max}, `walls`, `holding`, `weight` |
| `governments.json` | `id`, `realmNameKey` ("the {seat} Crown"), `succession` (`heir`, `council`, `strongest`, `undying`), axis weights, `offices` |
| `oaths.json` | `id` (`truce`, `friendship`, `tribute`, `alliance`, `fealty`; the id names the behaviour, as incident ids do), `nameKey`, `textKey`, `acceptAt`, `breakGrudge`, `keptOpinion`, integer `params` |
| `legends.json` | `id`, `kind` (`old_war`, `truce`, `marriage`, `feud`, `burning`, `friendship`, `tribute`, `war_ongoing`), `weight`, `textKey`, `params` |
| `names.json` | per culture template id, `people` and `places` lists of loc keys (at least 48 and 24). Names are authored words in `loc/en.world.json`, never composed in code (ADR-0007; peoples rule 6). |

**Also:**
- `loc/en.world.json` holds every string, including `_you_` variants.
- New entries with no new fields:
  - `thoughts.json`: `home_burned`, `ruler_died`, `bowed_to_crown`, `kept_peace`, `kept_waiting`;
  - `incidents.json`: `envoy_arrives`, `raid_on_camp`;
  - `rebirth.json` (W1b): the three knot triggers in §4.9. If the player design has not landed, its slice 1 adds them (whichever lands second writes the seam).
- `RequiredContent` lists the ids the code names.
- `cast.json`, `species.json`, `world.json` and culture-faith's files are not touched.

**Plain words:** a polity is a "realm" ("the Rushmere Crown"); a site is its name, a "town", "village" or "fort", and a capital is a "seat"; the oaths read "swore loyalty to", "pays tribute to", "agreed a truce", "swore friendship" and "swore to stand together"; dominion is "bends to you"; bands are "envoy", "raiders" or "a war band"; a charge is "You asked Mira to…".

## 6. Determinism, snapshots, rewinds, text-state API, localization, mod hooks

**Draw ids.** These are append-only. A `TruthId` value feeds the hash, so once merged it never changes (ADR-0004).

| Track | TruthIds | DiceIds |
|---|---|---|
| M0 | 1–8 | 1–23 |
| Growth (ADR-0021) | 20–29 | 100–119 |
| Peoples batch 1 (ADR-0022) | 30–39 | 120–139 |
| Culture-faith (ADR-0023) | 40–49 | 140–159 |
| Peoples batch 2 | 50–59 | 160–179 |
| Build, zones (left free by schedule §6.1; unclaimed) | 60–69 | 180–199 |
| Schedule | 70–79 | 200–219 |
| Player design | 80–89 | 220–239 |
| **World** | **120–149** | **300–339** |
| Loot-economy | 150–159 | 350–369 |

The player design's §10 and loot-economy's §7 quote earlier world ranges; this table is current. Nothing has merged, so the founder's allocation wins if it differs.

**TruthIds (W1)**
- 120 `WorldTerrain`
- 121 `WorldRiver`
- 122 `PolityNature`: 0 culture, 1 government, 2 seat
- 123 `SitePlacement`
- 124 `SiteNature`: 0 kind, 1 name, 2 population, 3 walls
- 125 `NotableNature`: 0 species, 1–2 traits, 3 name, 4 faith, 5 devotion
- 126 `Legend`
- 127 `HomeSite`
- 128 `KnowsCamp`
- 129 `ArrivalRegion`
- 130–149 are reserved for successors, the rivals' timetable and the storyteller's personality.

**DiceIds (W1)**
- On a new `DiceSystem.Politics`: 300 `CourtAct`, 301 `ActTarget`, 302 `NotableDeath`.
- On a new `DiceSystem.War`: 310 `ClashRound`, 311 `NamedBlow`, 312 `BlowDamage`, 313 `RaidSize`, 314 `RaidHour`.
- Each new system takes the next contiguous value at merge (ADR-0023's `Reserved8` precedent).

**Rules:** `RealmSystem` never draws from another system's stream, so a world without contact leaves the camp's sequences untouched (raids at the camp use the thief path's draws, §4.6). Integers only; no `NewId` before the camp is built; no events while seeding; `StateHash` covers `World.Realm`.

**Snapshots.** `RealmBook` and the notables are plain serialised state: about 1 KB of terrain, 40 sites and 45 characters. Bump `SchemaVersion` when saves arrive (ADR-0021). Tests restore mid-march, mid-clash, with an envoy waiting, and with a tribute claim open.

**Rewinds** (when built):

| Survives (Truths) | Re-rolls (Dice) |
|---|---|
| terrain, rivers, settlements; realms at arrival; notables' natures and every pair's chemistry; legends; homes; the camp's map; successors' natures (by generation); later, rivals' timetable and the storyteller's personality | every act after arrival: envoys, raids, oaths, wars, deaths, successions, battles, news; whether a successor is ever needed |

- A rewind restores the whole `RealmBook` from the fate knot (player design §5.3).
- What you have seen of the world and whom you have met (W2) is player memory, because the player keeps their memories (fixed decision 3), as they keep the customs they learn (ADR-0025 point 3). It joins "what you keep" in `World.Fate` (player design §5.4), carried as culture-faith's `LearnedCustoms` is. From W2, regions you have not seen are dark and news is your only word of distant events; the soak and the text-state API see everything (lead's call).
- Every knot now carries the realm, about 45 more characters. W1a measures snapshot size and restore time against the player design's knot budget (its §8.2).
- The Chronicle of Lost Futures keeps abandoned history as legends ("In another timeline, Rushmere burned in year 6.").

**Text-state API.** New reads: `StateApi.WorldMap`, `Region(x, y)`, `Site`, `Polity`, `Oath`, `War`, `Band`, `Dominion` and `News`, all localised and JSON-serialisable. An envoy's ask and deadline come through `StateApi.Visit`, the read loot-economy defines for its peddler; whichever lands first defines it. `CharacterState` gains `Place` (where they live, what they rule, their office). `Faction(w, "camp")` stays; realms are CLAUDE.md's "factions", read through `Polity`. The runner answers envoys with `AnswerEnvoy`.

**Localization.**
- Every string is a loc key; a missing one renders as `⟦key⟧`.
- Names are authored keys. `EventText.Resolve` reads a `k:` argument with a flat lookup, so realms and settlements get their own argument types: `p:<polityId>` fills the government's template with the seat's name, and `s:<siteId>` gives a settlement's name. This mirrors how `c:` names a character, keeps English out of code, and collides with no prefix the sibling designs add (`t:`, `e:`, `h:`).
- Realm and culture names keep their article and never open a sentence (culture-faith's grammar rule 1), and third person is "their" (its rule 2).

**Mod hooks.**
- Mods add terrain, site kinds, governments, oath kinds, legends and names by id.
- A new succession law, oath kind or legend kind needs code. An unknown one fails at load, naming the file and the id.
- `IModHook.OnEvent` sees Politics and War events at the end of their tick.

## 7. Fit with the other documents and the work in flight

**Merge base.** W1 starts from the milestone-0 tip once culture-faith slice 1 has merged, because realms are seeded from its `CultureBook`. That follows the order in schedule §7.0: growth, batch 1, culture-faith, schedule, zones, 2A.
- W1a is additive and can land on either side of schedule and zones.
- W1b's raids meet the schedule's night watch and the build's walls. Whichever lands second writes that seam (schedule §7's rule).

| Document or track | This design takes | It gives |
|---|---|---|
| Culture-faith (ADR-0023) | cultures, faiths, gods; person and faction axes (§4.6); stances (A1); oath weight (O1); omens (M1); greetings (G4) and kin warmth (G1) in audiences | `Oath` as the home for vows (O4); realms as its "factions"; world-scale faith from W3 |
| Peoples (batches 1, 2) | which peoples exist and its presence draw (its question 2); realms' peoples via culture weights | ageless rulers (`undying`); raids as situations, not natures; the sign note (§4.2); per-culture names in `names.json`, the invented cultures ADR-0025 asks names to come from |
| Growth (ADR-0021) | wants, arcs, `Growth.ChangeTrait` | world events as causes for M1's trait changes; founding and coronation as evolution triggers; rulers' wants (W2) |
| Build, Zones, Schedule | walls, doors, the watch rota, the home area | raids that test them; later delegates (master builder, stewards, watch captain) |
| Player design | your rolled people and gift; your own intentions kept without a roll (its §4.5); aging, lifespans and the soul's span (its §6); fate knots, the return and what you keep (its §5) | where you wake (§4.2); which peoples a world holds; the domination test its §6.3 ends a run on; knot triggers; your people read by cultures' stances; rivals drawn from its people and gift tables by rival slot (its §11) |
| Loot-economy | the peddler and its `StateApi.Visit` read; raiders' packs and a captain's weapon (its §5.4) once it has landed; ventures and the barrow (its slice 2), shared with W2's expeditions | `Abroad`, `Visiting` and `JobKind.Visit` for its peddler (§4.4); settlements with an id, name, culture and a distance in days, as its §8 assumes. From W2, not before, because market draws would move the camp's Economy stream: a `Market` for each market settlement (a `market` template in `sites.json`), the nearest sending the peddler, Greyford re-keyed under it, its food stock as the site's `Food`, and its word carried as news; then war blockades and levies as road shocks, and tribute and plunder in its goods |

## 8. Slices and acceptance

### 8.1 W1a: the world exists and lives

**Builds:** the content in §5; `RealmGen`, `RealmBook`, notables and `Abroad`; `RealmSystem` (acts, audiences, raids between realms, deaths, succession, food); legends on the first hourly step; the new tags, effects and streams; StateApi reads; the world map, tooltips and inspectors; the runner's "The world" and "Far away" sections, with world stories counted separately.

The camp knows nothing yet: no contact, no news, and no world events in the Chronicle.

**Tests**
- **Determinism:** same seed and commands, same world (realm included); generating twice gives identical state; reseeding Dice leaves tick 0 unchanged; notables' natures do not depend on creation order.
- **`WorldLeavesTheCampAlone`:** for seeds 1–100, camp maps and the camp sections of story logs are byte-identical to the merge base, and cast, bed, tree and bush ids are unchanged.
- **Creation:** the Chronicle is empty at creation; legends appear on the first hourly step.
- **Causality:** raid → captain killed → ruler's grudge → war declared; death → new ruler → oath lapsed by axis.
- **Snapshots:** restoring mid-march or mid-clash matches a straight run.
- **Content checks** name the file and id: unknown succession, oath or legend kinds; missing name keys; too few names.
- **Budgets:** the realm's day costs under 1 ms at seven realms; the tick-cost test counts only the local map; snapshot bytes and restore time are reported with the realm, for the player design's knot archive.

**Soak** (100 worlds × 1 year): no crashes; realms average at least two acts a year, and fewer than 5% of realm-years pass without one (lead's call: "nothing" is the commonest court turn and a season holds two, so a per-season floor would fail by chance); a succession or a broken oath in at least 30% of worlds; a median of at least 3 world stories per world.

**Screenshots:** the world map at its default zoom; the cross-fade between scales; a seat's inspector and its ruler's.

### 8.2 W1b: the world comes to the camp

**Builds:** knowing the camp; envoys, `AnswerEnvoy`, `Visiting` and `JobKind.Visit` (§4.4); tribute claims in the share-out; raids on the camp, paced by the director; leavers who go somewhere and come back; refugees; news and its thoughts; Orders listing the camp's promises. **Autoplay** answers envoys by seed (`seed / 3 % 3`: agree, refuse or ignore) alongside the ration policy, so the soak covers every answer (ADR-0017 point 2). Today `Autoplay.Begin` enqueues once, at tick 0, so the runner must also watch `Realm.Visits` each hour and enqueue `AnswerEnvoy`.

**Tests**
- the tribute chain under each policy, including a Desperate member taking the tribute berries;
- an unanswered envoy and the ruler's opinion;
- refused demand → grudge → raid → captain killed → ruler's grudge;
- a former member returning as captain, the raid citing their departure and the grudge's `FormedEvent`, with the grudge left inactive;
- a refugee arrival citing the raid;
- news arriving after distance × 3 hours;
- `AnswerEnvoy` replaying identically.

**Soak**, against the merge base's own numbers, measured before the step (batch 1 measured 12.13 stories per world-year):
- camp stories per world-year no fewer than the base;
- a story whose chain holds a Politics or War event and ends on a camp member in at least 50% of worlds;
- the share of stories ending before winter (day 31) up at least 5 points (ADR-0018's mid-year seam);
- at most 2 raids on the camp per world-year on average.

**Stranger check:** of four fresh readers, at least two retell, unprompted, a story that crosses from the world into the camp.

**Screenshots:** an envoy waiting with their inspector open; raiders at night; a news line in the Chronicle.

### 8.3 Later

| Slice | Builds | Needs first |
|---|---|---|
| W2: you go to the world | asking companions (visit, scout, recruit); expeditions on loot-economy's venture machinery; road encounters; audiences with culture's hooks; your own oaths; recruits and refugees as new people; fog and map memory; the dominion count; markets in settlements | the peoples design's presence draw (its slice 2B); loot-economy slice 2 |
| W3: you take the world | your realm, holdings and stewards; marshals; armies, sieges, conquest; sworn realms and rebellion; the win; coalitions; the storyteller's personality and path stages; a faster world-map speed (§4.9) | the player design's aging; multi-year soaks |
| W4 and later | rival otherworlders (player design tables, by rival slot); marriages, births and heirs (hybrids); local maps of visited places (audience halls, sieges, dungeons); holy wars and conversion; Ideas Tree reactions by realm; Distortion at world scale | the player design's slice 2; culture-faith slice 3 |

## 9. Questions for the founder

Most important first. Until a question is answered, the builder builds its recommended answer.

1. **What counts as total domination?**
   *Recommended:* two end states count, held and sworn. Alliance, a shared faith and dependence on your trade are roads that make swearing easier, not ends in themselves (§4.9). Victory comes the moment every standing realm bends to you while you live. It is kept in `Fate`, so no return undoes it, and you may rule on until old age (player design §6.3).
   *Why it matters:* it is the win, and every later slice aims at it.
2. **Approve W1's schema and determinism changes,** as listed under "Signing approves".
   *Recommended:* approve. W1a must prove the camp unchanged before W1b changes it.
   *Why it matters:* CLAUDE.md asks before both kinds of change, and once merged the draw ids feed every world's hash.
3. **How dark may war be?**
   *Recommended:* raids plunder and burn stores and fields, named people die, and bodies stay (ADR-0019). A burning drives people out rather than killing them (§4.5). Captives are ransomed or freed. No massacres of populations, and no slavery, as a player choice or as a mechanic.
   *Why it matters:* it is a content line like the one ADR-0025 drew for faiths, and it decides what W3's sieges and conquest may do to a town.
