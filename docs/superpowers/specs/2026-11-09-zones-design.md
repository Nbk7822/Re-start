# The Zones menu: design

- **Date:** 2026-11-09
- **Status:** proposed, for the founder's sign-off. Revised the same day after a builder's read. Step 3 (the home area) is now built on the Schedule design's day and watch (sections 3.3, 5.5 and 8.2). One shared helper decides who keeps the camp's rules (section 5.10). The soak targets are measured against the real merge base, not against fixed numbers (section 9.2, Appendix A). Smaller calls are marked "(lead's call)" where they are made.
- **Covers:** the Zones button on the bottom bar, which today is a disabled "Not available in this milestone" button. This doc covers painted areas on the grid: the stockpile (which exists today as a fixed rectangle), a meeting place by the hearth, a home area for the night, places to keep out of, groves to leave be, a burial ground, growing plots and grazing. It says which of these to build now, which to build later and which should wait.
- **Written alongside:** `2026-11-09-build-design.md` (I read it; this doc follows its conventions and its boundary: Zones owns painted areas, Build owns things made by work, and graves are Build's), `2026-11-09-schedule-design.md`, `2026-11-09-peoples-design.md` and `2026-11-09-culture-faith-design.md`. The last three were written after this doc's first draft. The revision reads them, and section 8 says where this doc gives way to them and what it asks of them.
- **Sources:** CLAUDE.md; BIBLE.md sections 2, 4 and 5; ADR-0004 to 0008 and 0013 to 0021; the code at `c818511`, rechecked at `17be2d5` (growth merged); and a read-only probe of 100 one-year worlds (Appendix A).

**What signing approves.**
- A new content file, `content/zones.json`, and its schema. No other schema changes. Traits and species gain no fields: section 5.10 reads trait fields that already exist, and the night folk come from Schedule's kinds of day.
- Three new thoughts and the new loc keys.
- The commands, state, snapshot fields and one new draw tag, `DiceId.ZoneDefy`, in section 5. Its number is the next free one when it merges.
- The order of work in section 9.1: steps 1 and 2 start now; step 3 starts after Schedule's slice 1 merges and is built on its day and its watch.
- Three changes that reach every world, whether or not the player marks a zone:
  - idle people gather at the meeting place;
  - work in the dark beyond the firelight goes wrong twice as often (four times for someone also worn out, under Schedule's rule);
  - a cause fix (section 5.5): a forage job chosen because of hunger carries that hunger, and work events cite their job's cause.

  All three move the stories-per-world-year baseline (8.47 at `c818511`, unchanged by the growth merge; ADR-0021), so the soak measures them one at a time against the merge base (section 9.2).
- The runner's `--zones` flag in section 9.
- Two calls of mine:
  - you keep your own zones: the player's character never rolls to defy them;
  - there are no per-person allowed areas (section 2).

It adds no dependency and changes no fixed decision. Signing does not settle the questions in section 10.

**In short**
- A zone is an intention, never an order (fixed decision 2): "keep the food here", "stay inside here after dark", "gather here". Each person weighs it against their needs, their traits and how they feel about you, and later against their people and faith. They may ignore it, and when that matters the Chronicle says who ignored it and why.
- Slice 1 makes three zones. **Stockpile:** the fixed rectangle becomes zones the player can move, add to and filter. **Meeting place:** around the hearth, where idle people gather. **Home area:** where people stay after dark. It is built on Schedule's day once Schedule lands, because Schedule owns the night (section 8.2).
- Slice 2 comes with the peoples and culture-faith work: **keep out** (always or after dark), **leave be** (no felling or picking, for groves), the **burial ground** (where the camp digs graves on its own, using Build's graves), and thieves who waylay people alone in the dark. **Growing plots** wait for the items milestone, and **grazing** waits for animals.
- Stories come from three sources. **Trade-offs you choose and live with:** dry stores or watched stores. **Rules that split the camp:** the rule-keeper who resents the rule-breaker, the free spirit who resents being kept in, the hungry one who stayed in because you asked. **Places that show relationships:** who sits by the fire, and who keeps away from whom.
- Zone events cite only what brought them about (ADR-0018). Your zone is a cause only where someone obeyed it and that changed what happened.

---

## 1. Why zones: the stories

The camp is one shelter, some ruins, a hearth, a stockpile and a watch post on a 48 × 32 grid. Its stories come from hunger, theft, promises and the night (STORIES.md). Zones give the player a way to shape where those things happen without ordering anyone about, and they give companions something new to agree or disagree with. Three stories show the range. The lines are written as the Chronicle would show them, in the existing event templates where they exist.

### Story 1: The dry stores (stockpile)
```
Day 24, 09:10 — You moved the stockpile; it now lies in the covered ruins.
Day 34, 02:10 — A thief crept toward the camp in the dark.
Day 34, 02:31 — A thief made off with 3 berries from the stockpile, out of sight of Vale on watch.
Day 34, 03:00 — Nia went without so others could eat.
Day 34, 03:13 — Nia took the meal set aside for Bram.
Day 34, 03:13 — Bram now holds a grudge against Nia.
```
- **The chain:** stockpile moved (World) → stolen out of sight of the watch (Incident) → went without (Needs) → took the meal → grudge (Social).
- **Why it matters:** moving the food into the ruins kept it dry. Today "spoiled in the damp" fires 6.5 times per world-year, taking 2 to 4 berries each time (Appendix A). But the ruins lie 13 tiles from the watch post, and the watcher sees 8. The player's tidy-up is the first link of a theft and a feud ten days later. Nobody in it is a villain.

### Story 2: Never one to be told (home area, then the meeting place)
```
Day 12, 18:40 — You marked out a home area (230 tiles), asking everyone to keep inside it after dark.
Day 32, 19:20 — Never one to be told, Sera went out after dark anyway.
Day 32, 21:25 — Sera picked 2 berries.
Day 32, 21:25 — Sera was hurt in an accident while working in the dark.
Day 33, 12:05 — Oren insulted Sera.
Day 34, 19:25 — Never one to be told, Sera went out after dark anyway.
Day 35, 10:15 — Oren insulted Sera.
Day 36, 17:40 — Oren insulted Sera.
Day 36, 17:40 — Sera now holds a grudge against Oren.
Day 36, 19:30 — Sera kept away from the fire while Oren sat there.
Day 37, 19:35 — Sera kept away from the fire while Oren sat there.
```
- **The circumstances:** it is winter, the stores are empty, and Sera is hungry but not starving. Under Schedule's day, 19:00 to 22:00 is free time. Nobody starts ordinary work then, but hunger still sends people to the bushes, because needs come first. The others stayed in as you asked. Sera is Independent, and she went to a bush beyond the home area. Oren was sitting at the fire 6 tiles away and saw her go.
- **The chains:**
  - Your rule (World) → Sera's defiance (Work) → Oren, who keeps rules, saw it and thinks less of her → his insults (Social) → her grudge (Social).
  - Separately: her hunger (Needs) → the forage (Work) → the accident in the dark (Incident).
- **Why the accident chain does not start with your rule:** Sera went out for food she needed; the rule did not send her. So the rule is not on the accident's chain (section 5.6).
- **Times are compressed.** With today's tuning, a feud takes several insults to harden into a grudge (ADR-0013, ADR-0016).
- **Why it matters:** the same rule makes two people into opponents. Afterwards the map shows it: Sera sits alone at the edge of the camp each evening while the others sit by the fire.

### Story 3: The watch nobody kept (home area)
```
Day 30, 15:10 — Hollis agreed to stay after you promised them a meal within 2 days.
Day 30, 18:30 — You marked out a home area (190 tiles), asking everyone to keep inside it after dark.
Day 30, 19:00 — Oren would not keep watch outside the home area after dark.
Day 30, 20:00 — Nobody kept the watch.
Day 31, 02:10 — A thief crept toward the camp in the dark.
Day 31, 02:35 — A thief made off with 3 berries; nobody was on watch.
Day 31, 03:00 — Hollis went without so others could eat.
Day 31, 03:00 — You broke your promise to Hollis.
Day 31, 03:00 — Hollis now holds a grudge against you.
Day 31, 03:00 — Hollis left the camp.
```
- **The chain:** home area (World) → Oren obeyed it, so nobody kept the watch (Work) → theft (Incident) → went without (Needs) → promise broken → grudge → left (Social).
- **How it runs:** the watch is Schedule's. Its `WatchSystem` hands out the turns, and the home area is one more reason a person is not picked (section 5.5). "Nobody kept the watch." and "nobody was on watch" are Schedule's lines. The theft cites Oren's line through Schedule's `GapEvent`.
- **The circumstances:** neither vampire had arrived. The night folk are asked first for the watch, and the home area does not bind them, so on a later night one of them would have kept it.
- **Why it matters:** the demo's signature broken-promise story, with an unplanned first link. You drew the home area one row short of the watch post. Oren did exactly what you asked, and it cost the camp a newcomer.

---

## 2. Goals, non-goals, anti-goals

**Goals**
- Turn the fixed stockpile into zones, and add the meeting place and the home area, so that three new kinds of story appear in soaks (section 9 gives the numbers).
- Make "people may refuse" visible and explained. Every time someone ignores a zone and it matters, there is one Chronicle line with the reason: need, helping someone, their own nature, their feelings about you, and later their people's customs.
- Keep zones legible on the map. People walk home at dusk. One person sits apart from the fire. Food lies in a dry corner.
- Build the zone framework once (state, commands, content kinds, overlay, marking tool) so that slice 2's zones are content plus one behaviour each.

**Non-goals (this doc)**
- Buildings: walls, roofs, beds, graves, stores, hearths, watch posts. These belong to Build. Zones read what Build makes, such as cover, light and graves.
- When people work, sleep or rest. This belongs to Schedule. The rule between them: Schedule says when, Zones say where (section 8.2).
- What a burial must look like, which places a faith holds sacred, and which omens a people heeds. These belong to culture-faith. This doc gives them places to attach to.
- Per-person allowed areas (RimWorld's "restrict this pawn to area X"). That is direct control by another name (fixed decision 2) and a spreadsheet in the making. Zones apply to everyone, and people decide.

**Anti-goals**
- Zones as a tidiness minigame. Every zone must change behaviour in a way that can end in a grudge, a debt, a departure, a death or a recruitment.
- Zones that always work. A zone people never ignore is a wall, and walls are Build's.
- Gotchas from the UI. Moving a stockpile must never starve anyone through bookkeeping: food waiting to be carried is still the camp's food (section 5.3).
- A zone management screen. The Zones list is short (kinds, then your zones). It never becomes a table of statistics.

---

## 3. Which zones, and when

| Zone | What it asks | Who may ignore it, and why | Stories it makes | When |
|---|---|---|---|---|
| **Stockpile** | Keep goods here (optionally only some items) | Nobody in slice 1. In slice 2, believers refuse to carry food into a place their faith forbids | Dry stores against watched stores; thefts out of the watch's sight; one big pile or several small ones | Slice 1 |
| **Meeting place** | Gather here when idle | Anyone holding a grudge against someone sitting there keeps away | Who sits with whom; the person who sits apart; later, two fires and two cliques (Build slice 2 adds hearths) | Slice 1 |
| **Home area** | After dark, stay inside | The starving (to find food); helpers (to reach someone hurt); the Independent, people who dislike you, people in a black mood (a nightly roll); never the night folk, for whom the dark is day | Breaches and witnesses; compliance that costs (hunger, a watch not kept); accidents in the dark | Slice 1, step 3, on Schedule's day |
| **Keep out** | Stay away from here, always or after dark | The starving, helpers, defiers; believers whose faith calls them there | The river at night (with culture's omens); the place where the thief came in | Slice 2 |
| **Leave be** | Fell nothing and pick nothing here | The starving pick berries; a builder short of timber fells anyway | The grove that keeps a dryad content, and the night someone took an axe to it | Slice 2 |
| **Burial ground** | Bury our dead here | Those whose rite is not burial (culture-faith); nobody goes out to dig after dark if it lies outside the home area | The body that lay by the fire till dawn; the rite done wrong; burial debts | Slice 2 |
| **Growing plot** | Sow, tend and harvest here | Holy days; food taboos; dryads on forest soil | Seed eaten in a hungry spring; a harvest lost to the frost | After the items milestone |
| **Grazing** | Animals graze here | — | Wolves at the edge of the home area | After animals exist |

### 3.1 Stockpile (slice 1)
- **Today:** a fixed 4 × 3 rectangle west of the hearth (`Map.Stockpile`, set in `WorldGen.BuildMap`). Everything that counts, takes, shares out or steals food asks "is this stack inside that rectangle?" (Appendix B lists every site).
- **After slice 1:** the camp starts with the same rectangle as a stockpile zone. The player can move it, add more stockpiles anywhere passable, and say what each one keeps (berries, timber, or both).
- **Physics that make placement matter** (two of these exist already):
  - **The watch sees 8 tiles** (`CombatSystem.SightRange`). The default berry pile always sits 9 tiles from the watch post: new berries go to the stockpile's top-left tile, and that holds in all 100 seeds (Appendix A). So stockpile placement is already a lever against thieves; this doc names it and makes it visible.
  - **"Spoiled in the damp" spares covered food** (new). `food_spoils` takes only from stores lying in the open. Covered means under a sound roof or in the covered ruins (`Map.Covered`). Today it fires 6.5 times per world-year, taking 2 to 4 berries each time.
  - **A thief takes from the pile it reaches** (new), not from the whole camp's stores at once, as `Stock.Take` allows today. Splitting stores into several piles limits what one theft can take.
- **The knock-on to measure:** spoilage is a major incident that spends 30 director pressure. Dry stores leave that pressure unspent, so thieves come more often, because the thief roll scales with pressure. Dry stores, more thieves, and the ruins out of the watch's sight: that is the story 1 trade-off, and section 9 measures it.

### 3.2 Meeting place (slice 1)
- **Today:** idle people wander within 4 tiles of where they are and drift back to the hearth when more than 8 tiles away (`JobPicker.WanderJob`). In the evening, 82% of awake hours are idle, but only 24% of awake hours are spent within 3 tiles of the hearth (Appendix A).
- **After slice 1:** the camp starts with a meeting place on the 24 tiles around the hearth. Idle people go there.
  - With several meeting places, each person picks the one where the people they like best are sitting.
  - Someone who holds a grudge against a person sitting there keeps away and sits apart.
  - The fire's comfort is Schedule's `evening_by_the_fire`: a free hour spent within 3 tiles of the hearth with company. The default meeting place lies inside that radius, so Zones adds no fire thought of its own (lead's call; section 5.4).
  - Wanderers walk to the meeting place when they arrive, which is where culture-faith's guest-right will apply.
- **Why it earns its place:** it puts the relationship graph on the map. Holders of grudges and their targets were idle near the hearth together on 3.7 days per world-year, in 66 of 100 worlds. With the meeting place, that turns into visible avoidance and a Chronicle line.
- **What it sets up:** it is where Schedule's free time sends people, where culture-faith's festivals and guest-right happen, and, with Build's second hearth in slice 2, where "the camp sat at two fires" comes from.

### 3.3 Home area (slice 1, step 3: built on Schedule's day)
- **Today (at `c818511`, before Schedule):** humans start 109 forage, felling or hauling jobs after dusk per world-year. Of these, 60 target bushes outside the camp clearing, and 34.5 of those 60 are by starving people (Appendix A). Night has one danger, the thief, and it never touches anyone who is not defending the stockpile.
- **Schedule owns the night; Zones own where people are in it.** Both docs draw the line the same way: Schedule says when, Zones say where (section 8.2). Schedule's default day gives diurnal people free time from 19:00 to 22:00 with no ordinary work and keeps them in bed from 22:00 to 06:00. Its `WatchSystem` hands out the watch in turns. Most of today's night work disappears with that day, so the home area is built on it, not on today's night. Step 3 starts after Schedule's slice 1 merges. If Schedule is not signed, step 3 waits. It is never built on today's night, which Schedule removes as an artefact.
- **After step 3:** the player can mark one home area. After dark, every bound person (section 5.5: everyone whose kind of day sleeps in the dark) starts nothing outside it unless excused (below). At dusk, those outside at work or idling walk back in, which is a legible scene on the map. Under Schedule's day, this is what it still touches:
  - **hunger:** someone hungry but not starving would go to a bush beyond the home area in their free time; instead they stay in;
  - **the watch:** a watch post outside the home area gets no bound watcher (story 3);
  - **the walk home at dusk:** a job outside that would run on past dusk is left unfinished;
  - **night hours:** someone the player has put on nights (Schedule's own hours) works inside the home area.
- **Excuses:**
  - starving (they go to find food);
  - helping someone hurt outside (care or rescue);
  - a nightly roll for the Independent, for people who dislike you, and for people in a black mood (section 5.10).
- **Combat is never blocked:** a fight or a flight from a thief is not a breach.
- **What the dark costs** (new physics, independent of the zone; question 2): work done at night more than 8 tiles from the hearth goes wrong twice as often. At `c818511` that meant about one more accident per world-year in a camp with no home area (0.98 of the 2.33 accidents a year happened at night). Under Schedule's day the cost falls on fewer people: jobs that run on past dusk, starving foragers and anyone on night hours. The probe measures it again (section 9.2).
- **Social effects:**
  - Someone who keeps rules (section 5.10) and sees a defiant breach thinks less of the breaker.
  - An Independent who is held back resents you a little.
  - A hungry person who obeys has the zone recorded as the cause of the hunger that follows.
- **No home area at start** (lead's call). A home area is an intention, and intentions are the player's to set (fixed decision 2). The stockpile and the meeting place exist at start only because the demo's camp had them. Without a home area, nothing changes except the dark's accident rate, and default worlds stay close to today's balance.

### 3.4 Keep out and leave be (slice 2)
- **Keep out (always, or after dark):** people do not take targets inside it, and paths avoid it unless there is no other way. In the pathfinder, its tiles cost 5 × as much for those bound by it; this is deterministic and adds no draws. Excuses and witnesses work as for the home area.
- **Leave be:** trees and bushes inside it are skipped by `Places.StandingTree`, `Places.BushWithBerries` and `Places.TimberNeeded`, which the Build doc asks for in its section 7.1. The starving still pick berries there; a builder short of timber may fell there on a defiant night.
- **Why they wait:** in today's sim nothing at the river or in a grove can hurt anyone or matter to anyone.
  - The river's only role is washing up windfalls.
  - Trees matter only for timber (24 trees; they never regrow).
  - A keep-out zone now would be a rule with no stories. With dryads (forest need), culture-faith's omens ("the river takes the careless at night") and Truth-seeded sacred places, both zones carry stories at once. Story 2's rule-keeper and rule-breaker dynamics then extend to groves and riverbanks.

### 3.5 Burial ground (slice 2)
- **What Build provides** (slice 1c): graves, the act of burying, grief for the unburied, `laid_to_rest` and the burial debt. In Build, the player places each grave by hand ("Build → Grave, for whom?").
- **What the burial ground adds:** the camp's standing intention. When a member dies and no grave is planned for them, the camp plans one on its own, on the first free valid tile of the burial ground (in tile order, through Build's `Building.Check`), and Build's burying work takes over.
  - If the burial ground lies outside the home area, nobody digs there after dark. The body lies by the fire till dawn, and Build's `unburied` grief runs.
  - Culture-faith decides whether a grave is the right rite for this person (a pyre is a grave-kind building in Build's terms). Where it is not, the burial ground is not used, and the rite is culture-faith's.
- **Why it waits** (this follows from fixed decision 1, so it is no longer a question for the founder): today members die 0.02 times per world-year (Appendix A). Burial needs multi-year worlds, with aging and the 20-year soaks that fixed decision 12 already requires, before it has anything to do. Culture-faith moves its death rites to its slice 2 for the same reason, and until then Build's hand-placed graves (its slice 1c) cover the deaths there are.

### 3.6 Growing plots: how farming would change hunger stories, and why it should wait
- **The winter squeeze carries the stories.** Most of the year's stories land in the last ten days, the winter squeeze (ADR-0018). Meal theft → grudge appears in 67% of worlds (ADR-0015), and the ration policy, your main lever, matters because winter puts the camp on half rations. Twelve bushes are the tuned balance: nine starved every camp, and sixteen meant nothing happened (ADR-0013).
- **A berry plot would do nothing for winter.** Berries spoil in 5 days (`items.json`), so berries grown in autumn rot before the frost. In the growing seasons the camp already stops foraging at 2 berries per human in store. A berry plot adds work, not stories.
- **A crop that keeps would remove the squeeze.** Grain or roots kept into winter would remove most hunger stories, and with them the ration policy's weight. A crop that keeps also needs species that eat more than berries, which ADR-0018 §5 forbids until the items milestone: rationing, theft and spoilage name `"berries"` in code.
- **Wait for the items milestone.** ADR-0018 §5 settles this (food is berries-only until then), so it is no longer a question for the founder. Building plots sooner would mean lifting that ADR, which would be a new founder decision. When the milestone comes, build plots so that hunger stories move rather than vanish:
  - **Sowing takes seed from the stores.** Eat now or plant: the ration policy can set seed aside, and the Desperate may eat it.
  - **The harvest races the frost.** The first winter frost takes whatever is still in the field, so the existing winter event becomes the cause of a poor harvest.
  - **A granary is a bigger prize.** Stockpile placement and the watch matter more.
  - **Blight and drought** become Dice incidents.
  - **Refusals:** food taboos (culture-faith) and dryads defending forest soil.
- **The balance guard for that milestone:**
  - hunger-chain stories per world-year stay within ±20% of the pre-plot baseline;
  - went-without events stay above half the baseline;
  - meal theft → grudge still appears in at least 50% of worlds.

### 3.7 Grazing
Grazing needs animals, and the sim has none. When animals come, the home area is where they spend the night, and wolves at its edge are the story.

---

## 4. How the player uses it

### 4.1 The Zones list
Zones on the bottom bar becomes a toggle like Orders and Work: `BottomBar.Toggle` with a `ListPopup` titled "Zones". `Open("zones")` and `RETURN_OPEN=zones` work like the other lists.
```
            ┌ Zones ───────────────────────────────────────────┐
            │ Mark a place                                     │
            │   Stockpile       where the camp keeps its goods │
            │   Meeting place   where people gather by the fire│
            │   Home area       where people stay after dark   │
            │   Unmark          clear tiles from any zone      │
            │ Your zones                                       │
            │   Stockpile by the hearth · 12 tiles          ✕  │
            │     Keeps: [✓ berries] [✓ timber]                │
            │   Meeting place by the hearth · 24 tiles      ✕  │
            │ Drag on the map to mark. Right-click or Esc stops.│
            └──────────────────────────────────────────────────┘
[Build][Orders][Zones][Work][Schedule] | [People][Chronicle]   Day 3, 14:20 · Spring | Pause ×1 ×3 ×6
```
- **"Mark a place"** lists the zone kinds this build implements, from `StateApi.ZoneKinds`, so a mod's kind with an existing behaviour appears too. Hovering an entry shows its one-line hint.
- **"Your zones"** lists each zone by kind and place.
  - **Place phrases** come from `EventText.PlacePhrase`:
    - "in the covered ruins" or "in the shelter", when most of the zone's tiles lie inside them;
    - otherwise "by the hearth", "by the watch post" or "by the river", for the nearest of these within 8 tiles of the zone's centre;
    - otherwise "out in the open".
  - **Clicking a row** centres the camera on the zone. A stockpile row shows its item toggles.
  - **✕ removes the zone.**
  - **Guard:** a ✕ or toggle that would leave an item with no stockpile is disabled, with the hint "Another stockpile must keep timber first."
- **Pending changes:** while changes are pending (paused), the list shows the existing "Takes effect when time moves."

### 4.2 Marking on the map
Picking a kind closes the list and starts the marking tool.
```
   .  .  #  #  #  #  #  #  #  #  .  #  #  #  .
   .  .  #  b  .  b  .  b  b  #  .  ░  ░  ░  .        ░  tiles being marked (stockpile colour)
   .  .  #  .  .  .  .  .  .  #  .  ░  ░  ░  .        ✕  can't go here (shown on walls, water, rock, trees,
   .  .  #  .  .  .  .  .  .  #  .  ░  ░  ░  .           bushes, beds, the hearth, buildings and plans)
   .  .  #  #  #  #  D  #  #  #  .  .  .  .  .
                                 ↖ Stockpile · 9 tiles · in the covered ruins · dry
```
- **Drag a rectangle to mark it.**
  - If the drag starts on or next to a zone of the same kind, the tiles join that zone; otherwise they make a new one.
  - There is only one home area, so home tiles always join it.
  - Painting a stockpile or meeting place over the other kind takes those tiles from it.
- **Unmark** is its own entry in the list: drag over tiles to clear them from whatever zone holds them.
- **Stopping:** right-click or Esc stops the tool. Left clicks do not select people while a tool is active. This matches Build's placing rules, so the two tools feel like one.
- **The hint by the cursor** gives the zone, the tile count, the place, and one fact that matters for the kind:
  - **Stockpile:** "dry" or "in the open", and "out of the watch's sight" when more than 8 tiles from the watch post.
  - **Home area:** "leaves the watch post outside", "leaves the beds outside" or "leaves the stockpile outside" when it does. These are the three ways a home area can quietly cost the camp a watch, sleep or food after dark, and the anti-goals rule out UI gotchas.
- **One release is one change:** it sends one command and makes one Chronicle line.
- **While paused,** a change is a draft (`SimHost.Order` → `OrderDrafts`), drawn hatched. Marking then unmarking the same tiles while paused leaves no trace.
- **Grid lines** darken while a zone tool is active, as for Build.

### 4.3 Seeing zones
- **While the list or a tool is open,** every zone shows a faint fill in its kind's colour, a dashed edge and its name at the top-left tile.
- **The rest of the time,** the map stays clean:
  - stockpiles and meeting places show only their dashed edge, as the stockpile does today;
  - the home area shows a faint dashed edge only after dusk, when it matters;
  - no names.
- **Colours** follow ADR-0019: muted and darkened (ochre stockpile, amber meeting place, pale sage home area), with thin lines and no bright fills.
- **Tooltip lines** (`StateApi.Location`): "Stockpile (berries, timber)", "Meeting place", "Home area", "Berries ×3, waiting to be carried to a stockpile", and at night "In the dark" or "By the fire's light".
- **Inspector:** the new thoughts appear under Recent thoughts with their mood ("Kept in after dark (−2)", "Saw someone break the camp's rules", "Sat apart from the others (−3)"). The fire's comfort shows as Schedule's "An evening by the fire".
- **Chronicle:** breaches, compliance and avoidance, each once per person per night (or per day), with runs folded as the story log folds them (section 5.6).

---

## 5. What changes in the simulation

### 5.1 State
- **`Zone`** (new, `sim/State/Zone.cs`), in `Map.Zones` in id order. Fields:
  - `Id` (from `World.NewId`);
  - `Kind` (a `zones.json` id);
  - `Tiles`: a list of `TileRun(Y, X, Length)`, always canonical (sorted by y then x, merged, no overlaps), so equal tile sets serialise identically;
  - `Items`: stockpile only; the item ids it keeps, sorted; never empty;
  - `MadeEvent`: the event that last marked, moved or reshaped it; 0 for the zones the world starts with;
  - `MadeBy`: the character who decided; the player now, a delegate at kingdom scale.
- **Caches:** `Map.UseZoneAt` (the zone id per tile, for the use layer) and `Map.HomeAt` (one byte per tile) are `[JsonIgnore]`. `Zones.RebuildCache` rebuilds them, and it runs from `Map.RebuildCaches` (on load) and after every zone change.
- **Revision:** every zone change bumps `Map.Revision`, which the Build doc introduces. If Zones lands first, Zones adds it.
- **The generated rectangle:** `Map.Stockpile` becomes `Map.StartStockpile`, the rectangle `WorldGen` laid out. It stays only as a landmark for `WorldGen`'s reserved area, `GroundWear`, tests that need "a tile in the starting stockpile", and Build's stand-in search. No stores logic reads it.
- **Stores:** `ItemStack.Stored` (bool) means "put away in a stockpile". Stores are stored stacks wherever they lie. `Carry.Stored` keeps that status while a hauler moves a stack, and a load dropped on the way keeps it too.
- **`Character`** gains per-night zone memory, all ints and bools so snapshots stay plain:
  - `NightRollDay` and `DefiesTonight`;
  - the throttles `LastBreachDay`, `LastStayedInDay`, `LastKeptInDay` and `LastKeptAwayDay`.
  - A night belongs to the day its dusk fell on, so 02:00 on day 21 is night 20 (lead's call). The per-night fields store that number; `LastKeptAwayDay` stores the calendar day.
- **`World`** gains `HomeWatchNight`: the night `stayed_in_watch` last fired, its once-a-night throttle. The event that explains an empty post is Schedule's `World.Watch.GapEvent`; Zones adds no second one.
- **Schema version:** `World.SchemaVersion` goes up by one. Build also raises it; whichever lands second raises it again. There are no shipped saves to break.

### 5.2 Commands
- **The commands:** three tick-stamped records in `Commands.cs`, each with a `JsonDerivedType` registration, recorded and replayable like the ration policy (ADR-0004). Each one checks itself again in `Apply`, because a draft can apply a tick or more later. Tile sets are absolute, not deltas, so a later draft for the same zone simply replaces an earlier one.
  - **`MarkZone(Tick, ZoneId, Kind, Tiles)`.** `ZoneId` 0 makes a new zone; otherwise the zone's tiles become `Tiles`.
    - Tiles that fail `Zones.Check` are dropped. For the use layer (stockpile, meeting place), a tile fails if it is impassable, the hearth, a bush, a bed, or holds a building or a building plan. Roofs overhead are fine.
    - Use-layer tiles taken from another zone leave it. A zone left with no tiles is removed, without a separate event.
    - A second home area merges into the first.
    - If nothing survives, nothing changes and no event is emitted. The view never sends such a command, because it uses the same check.
  - **`RemoveZone(Tick, ZoneId)`.**
  - **`SetZoneItems(Tick, ZoneId, Items)`.**
- **The guard:** a command that would leave any item in `items.json` with no stockpile keeping it is ignored whole, with no event. The UI disables the button that would send it.
- **Drafts:** `OrderDrafts.SameSlot` treats commands for the same zone as one slot. A zone made while paused gets a draft-local negative id, so reshaping it before time moves stays one `MarkZone`. `Flush` skips commands that change nothing.
- **Events** (World system; the actor is `MadeBy`):
  - `event.world.zone_marked`: "You marked a stockpile in the covered ruins (9 tiles)."
  - `event.world.zone_moved`, when fewer than half the old tiles remain: "You moved the stockpile; it now lies in the covered ruins."
  - `event.world.zone_changed`: "You changed the meeting place by the hearth; it now covers 30 tiles."
  - `event.world.zone_removed`.
  - `event.world.zone_items`: "The stockpile by the hearth now keeps berries."
  - The home area has its own lines: "You marked out a home area (230 tiles), asking everyone to keep inside it after dark." and "You took away the home area; people may go where they like after dark."

### 5.3 Stores (the stockpile behaviour)
- **One predicate:** `Zones.Accepts(w, tile, itemId)` is the only way to ask whether goods are kept here. Build's slice 2 store building extends this same predicate.
- **Putting away:** `Stock.Add(at)` sets `Stored` when the tile accepts the item. Loose stacks are not stored: windfalls, and loads dropped on a tile that does not accept them.
- **`Stock.Count`, `Stock.Take`, `Places.FoodTile`** and the stores counts in `StateApi.Faction` read stored stacks, not a rectangle.
- **Taking food happens where the taker stands.**
  - Eating and roof timber take from the stack on the taker's tile first, then from the nearest stored stack.
  - Care's berry keeps today's shortcut.
  - A thief takes only from stored stacks within 1 tile of where it stands.
- **Where things are put down:** `Stock.DropTile(defId, from)` is the accepting tile nearest `from` that already holds that item, else the nearest free accepting tile; ties go to the lower tile index.
- **Hauling** (`JobPicker.Candidate("haul")`) carries two kinds of stack, in this order, each by stack id:
  1. stored stacks lying where no stockpile now keeps them ("waiting to be carried");
  2. loose stacks that some stockpile accepts.
- **Moving a stockpile never starves anyone.** Food waiting to be carried is still stores: it can be eaten, shared out and stolen where it lies.
- **`Rations.AfterDeposit`** shares food out when a load becomes stored, on any accepting tile.
- **The damp rule:** `food_spoils` fires only when the stored berries lying in the open number at least `minBerries`, and it takes only those. Its text is unchanged.
- **The unseen theft:** a thief that reaches stores while a member keeps watch at the post, without ever being spotted, emits `event.incident.stolen_unseen`: "A thief made off with 3 berries from the stockpile, out of sight of Vale on watch."
  - The thief was never spotted, so by construction the pile lay beyond every guard's sight.
  - The event cites the thief's cause and, if not 0, the `MadeEvent` of the zone that held the pile.
  - The generated stockpile has no `MadeEvent`, so a theft from it cites no zone.
  - Schedule's `stolen_unwatched` covers a theft with nobody at the post. `stolen_unseen` needs a watcher at the post, so the two never describe the same theft.

### 5.4 The meeting place behaviour
- **Where idle people go.** When someone would wander (`JobPicker.WanderJob`), they go to a reachable meeting place instead, if one is allowed: after dark, those bound by the home area only go to meeting places inside it.
  - **Which one:** score each meeting place by the sum of the person's opinion of every awake member standing in it. Any place where someone they hold an active grudge against is standing is skipped. Take the highest score; ties go to the lower zone id.
  - **Where in it:** the target tile and the wait reuse the existing `DiceId.WanderTarget` and `WanderWait` draws, inside the zone.
- **One rule with Schedule's free time** (lead's call). Both docs change `WanderJob`. Whichever lands first writes this rule, and the other fits into it. In order:
  1. in free time, a species' own free-time place (Schedule's `FreeTimeSpot`; for example, dryads go among the trees);
  2. else the meeting place chosen above, in any block (`Zones.MeetingPlaceFor`, which `FreeTimeSpot` calls before its hearth default);
  3. else, in free time, around the hearth at radius 2 (Schedule's default);
  4. else as today: within 4 tiles of where they stand, drifting back to the hearth from more than 8 tiles away.

  The default meeting place is the 24 tiles round the hearth, which is the same ground as Schedule's radius 2. So default worlds gather in the same place whichever doc lands first.
- **Keeping away.** If every meeting place is skipped because of a grudge, the person wanders near their sleeping spot instead.
  - **The event:** `event.mood.kept_away` ("Sera kept away from the fire while Oren sat there"; or "…from the meeting place…" when it is not by a hearth), at most once per person per day. It cites the grudge's `FormedEvent`.
  - **The thought:** `sat_apart` (−3, 12 h), caused by that event.
  - **The tag:** Mood, following Build's `resents_bed`: the event records a feeling about someone.
- **No fire thought from Zones** (lead's call). Schedule's `evening_by_the_fire` (+4 for 12 h, for a free hour spent by the hearth with company) is the only fire thought. Two would stack, and both would feed the black-mood term in the defiance odds (section 5.10).
- **Arrivals** walk to the nearest meeting-place tile (today, a tile beside the hearth) and join there as they do now.

### 5.5 The home area behaviour
Built in step 3, on Schedule's slice 1 (section 8.2). It reads Schedule's blocks and kinds of day, and adds one filter to Schedule's `WatchSystem`. It never decides when anyone sleeps, works or keeps watch.

**Definitions**
- **Dark:** not `Calendar.IsDaylight`, from dusk at 19:00 to dawn at 06:00. In Schedule's diurnal day, the dark is free time (19:00–22:00) and sleep (22:00–06:00).
- **Night folk:** people whose species follows a kind of day (Schedule's `rhythms.json`) whose shipped day has no Sleep hours in the dark. Today that means the vampires (`nocturnal`); the peoples branch adds the sleepless undead. The test reads content, not the camp day the player paints, so putting a human on nights does not make them see in the dark. There is no new species field, and no species id in code (lead's call; the peoples design's `SpeciesIdsNotInCode` test keeps species ids out of `sim/`).
- **Bound:** a member is bound when the camp has a home area and they are not night folk.
- **Outside:** a tile not in the home area.

**The nightly roll.** At the dusk hour, after `MoodSystem.Hourly`, each bound member except the player rolls once whether they will go their own way tonight. You keep your own rules; the excuses of need and help still apply to you (see "What signing approves").
- **The odds** are the shared compliance odds in section 5.10: the Independent's nature, how they feel about you, a black mood and, once culture-faith lands, their people's leaning.
- **The draw** is taken only when the odds are above 0. It comes from the compliance substream, with the tag `DiceId.ZoneDefy` (section 7.1).
- **The result** is stored in `DefiesTonight` until dawn. Until tonight's roll has run, nobody counts as defiant. This matters at 19:00, when Schedule's `WatchSystem.Step` runs before the hourly systems (lead's call).

**The curfew** applies to bound members while it is dark. It shapes their own choices; no command sets it off.
- **The walk home at dusk** (in `Zones.Hourly`, right after the roll): a bound member who is neither excused nor defiant tonight leaves their job if it is work or idling with a target outside. That means foraging, felling, hauling, roof repair (and Build's work when it lands), wandering and socialising. Their next idle choice takes them home: to a meeting place inside the home area, or near the nearest home tile. Every other job finishes as it is: sleep, seeking cover, eating, care, promises, blood, fights, flight, rescue, arrival and Schedule's watch jobs.
  - Leaving is the person's own choice at dusk, made after their roll; it is not an order. Marking or changing a home area never pulls anyone off the job in hand, by day or by night. That keeps Schedule's rule that a change of hours never does (Schedule §4.2) (lead's call).
- **Choosing:** `JobPicker` skips candidates whose target lies outside, unless excused. Under Schedule's day, that means foraging or eating out of hunger in free time, any work for someone on night hours, and the targets of wandering and company.
- **The watch** (Schedule's `WatchSystem.PickNext`): while it is dark, a bound candidate who is not defiant tonight is skipped when the watch post lies outside. Skipped means not asked. Nobody is woken, so there is no `woken` thought, no refusal, no cover and no debt. Obeying a standing intention never costs anyone a debt; Schedule's refusal table is for one companion asking another (lead's call).
  - If skipping leaves `PickNext` with nobody, `WatchSystem` emits `event.work.stayed_in_watch`, at most once a night for the camp. It names the first skipped candidate in `PickNext`'s order: "Oren would not keep watch outside the home area after dark." It cites the home area's `MadeEvent`. If the post is empty, the event becomes `World.Watch.GapEvent`. Schedule's lines that cite tonight's last refusal (`watch_unkept`, `left_watch_unrelieved`) cite it too, and a theft while the post stays empty cites it through Schedule's `stolen_unwatched`.
  - A watcher already at the post when the home area is drawn keeps their turn. The skip applies from the next handover.
  - The night folk and tonight's defiers can still take the post. A defier who does has breached like any other, and the breach line is theirs.
- **Excuses, in order:**
  1. **Some jobs are never interrupted or blocked:**
     - fighting, fleeing, rescuing and arriving, as `ShouldInterrupt` already does;
     - also sleeping and seeking cover.

     None of these is a breach. The fight lines already explain fights. Sleep is exempt so that a home area drawn short of the beds never keeps anyone from their bed.
  2. **Starving:** food below 150, or below their `stealFoodBelow` (the Desperate, 220). Schedule uses the same test to get people out of bed for food, so Schedule never wakes someone to find food that the curfew then forbids (lead's call). A food job may go out.
  3. **Helping:** a care or rescue target lies outside.
  4. **`DefiesTonight`.**

**What gets recorded**
- **A breach:** the first time each night that an excused person starts a job with an outside target, or stays outside with one after dusk. It emits `event.work.out_after_dark` with the reason as a variant:
  - `_starving`: "Wren, starving, went out after dark to find food."
  - `_help`: "Mira went out after dark to help Wren."
  - `_own_way`: "Never one to be told, Sera went out after dark anyway."
  - `_dislike`: "Hollis went out after dark anyway, caring little for what you want."
  - `_mood`: "Nia, in a black mood, went out after dark anyway."
  - `_ways`, once culture-faith lands: "Wren went out after dark anyway; the Reed Folk go where they please." (`{ways}` is the culture or faith, passed as culture-faith passes it.)
  - **Which variant:** for a defiant breach, the largest term in the odds picks it. Ties go nature, then feeling (`_dislike`), then mood, then culture (section 5.10).
- **Witnesses** (defiant breaches only; question 3): every awake member within 8 tiles who keeps rules (section 5.10) gets `saw_rule_broken` about the breaker (mood −2, opinion −4, stacks to 3), caused by the breach.
  - **The mood must be negative.** `SocialSystem.WorstThoughtCause` cites only thoughts whose mood is below 0, so a mood-0 thought could never become the cause of an insult, and story 2's chain would break at the first insult. Build's `envies_bed` is negative for the same reason.
- **Kept in:** an Independent (a nature term above 0 in section 5.10) whose best candidate was skipped for the curfew, or who left an outside job at dusk, gets `kept_in` about you (mood −2, opinion −2), at most once a night. It is caused by the home area's `MadeEvent`. This is a thought, not an event. The player never gets it: it would be about themselves.
- **Obeying at a cost**, at most once a night per person:
  - `event.work.stayed_in_hungry`, when a hungry person (below 300) skipped a food job outside: "Mira, hungry, stayed in camp after dark as you asked." It cites the home area's `MadeEvent` and their `LastFoodEvent`, and it becomes their `LastFoodEvent`, so the hunger that follows chains to it.
  - `event.work.stayed_in_watch`, at most once a night for the camp: see the watch, above.

**The dark** (physics; zone or no zone)
- **Lit:** within 8 tiles of a hearth by straight-line distance, tested as dx² + dy² ≤ 64 in integers. The view's firelight (a 9.5-tile falloff, ADR-0019) already fades just beyond that; keep the two in step.
- **Accidents in the dark:** `JobRunner.RollAccident` doubles `accidentPermille` when it is dark, the worker is not night folk, and they are not lit. It is the same single draw on the same stream as today. The accident then uses `event.incident.work_accident_dark`: "Sera was hurt in an accident while working in the dark."
- **With Schedule's worn out** (lead's call): the two doublings multiply, so someone who is worn out and working unlit in the dark rolls at four times the odds. Each doubling is a real cause, and the two rarely meet. The text names both, `event.incident.work_accident_tired_dark` ("Oren, worn out, was hurt in an accident while working in the dark."), and the event cites the work event and `WornOutEvent`, as Schedule's tired accident does.

**The cause fix this slice needs** (it applies to all worlds)
- **The gap today:** `event.work.foraged` and `event.work.felled_tree` cite nothing, and `ForageJob` carries no cause, so no work-to-accident chain reaches back to hunger.
- **The fix:**
  - a forage job chosen because of hunger carries the person's `LastFoodEvent`;
  - work events cite their job's `CauseEvent`.
- **Breaches stay off the physical chain.** A defiant breach is not set as the job's cause, because the rule did not send anyone out: without it, they would have gone just the same. So a breach is never the cause of an accident.
- **Measure it on its own.** This fix changes story counts in every world, so run the soak before and after this one change, so the shift is attributed.

### 5.6 Events, causes and throttles

| Event | System | Cites | Updates | Throttle |
|---|---|---|---|---|
| `event.world.zone_marked`, `_moved`, `_changed`, `_removed`, `_items` | World | — | the zone's `MadeEvent` (marked, moved, changed) | one per command |
| `event.incident.stolen_unseen` | Incident | the thief's cause; the zone's `MadeEvent` if not 0 | berry eaters' `LastFoodEvent` (as `stolen` does today) | — |
| `event.incident.stolen_unwatched` (Schedule's; no Zones change) | Incident | the thief's cause; Schedule's `GapEvent`, which is `stayed_in_watch` when the home area emptied the post | unchanged | — |
| `event.work.out_after_dark_starving` | Work | home `MadeEvent`; `LastFoodEvent` | — (the job's cause stays the hunger) | one per person per night |
| `event.work.out_after_dark_help` | Work | home `MadeEvent`; the helped person's `LastHealthEvent` | — | one per person per night |
| `event.work.out_after_dark_own_way`, `_dislike`, `_mood` (and `_ways`, once culture-faith lands) | Work | home `MadeEvent`; for dislike, a grudge against you, else their worst thought about you; for mood, their worst thought; for ways, nothing more (a leaning is not an event) | witnesses' `saw_rule_broken` | one per person per night |
| `event.work.stayed_in_hungry` | Work | home `MadeEvent`; `LastFoodEvent` | `LastFoodEvent` = this | one per person per night |
| `event.work.stayed_in_watch` (emitted by Schedule's `WatchSystem`) | Work | home `MadeEvent` | `World.Watch.GapEvent` = this, when the post is empty | one per night for the camp (`HomeWatchNight`) |
| `event.mood.kept_away`, `_fire` | Mood | the grudge's `FormedEvent` | `sat_apart` on the person | one per person per day |
| `event.incident.work_accident_dark`, `_tired_dark` | Incident | the work event (as today); for `_tired_dark`, also `WornOutEvent` | as today | — |
| `event.work.foraged`, `felled_tree` (changed) | Work | the job's `CauseEvent` (new) | — | — |

**Why the citations fall this way** (ADR-0018 §1)
- **Obeying changes outcomes,** so compliance events cite the zone and stand on the chain.
- **A breach is still a transgression.** It exists because of the rule, so it cites the zone. Witnesses react to the transgression, so the social consequences chain through it. The physical consequences cite the job's real cause.

**Keeping the Chronicle readable**
- **The volume:** in a winter squeeze, several people may go out starving each night.
- **The throttles above,** stored on the character and serialised, cap it.
- **Folding:** the Chronicle should fold a run of the same line that differs only in one person ("… and the same with Bram and Nia, until Day 37, 03:00"), as `StoryLog` already does. Move that fold into `Return.Sim.Api` so the runner and `ViewText.Chronicle` share it.
- **The story log:** zone and breach events count as notable, as World and Incident events explain behaviour to a stranger reader. The runner's notable filter gains them by key prefix.

### 5.7 How zone stories end
Every zone story ends in an effect the analyzer already counts. Zones add no new `EffectKind`.
- **Grudges:**
  - witnessed defiance → disapproval → insults → grudge;
  - a theft out of the watch's sight → hunger → meal theft → grudge.
- **Debts:** an accident or wound in the dark → care or rescue → owes. Burial debts come in slice 2, through Build.
- **Departures:**
  - a broken promise after a theft the curfew let through;
  - an Independent soured on you by nights kept in, through the existing opinion-of-you departure rule.
- **Deaths:** rare. In slice 2, thieves who waylay people alone in the dark can kill.
- **Recruitment:** a wanderer received at the meeting place (unchanged in slice 1). In slice 2, culture-faith's guest-right can turn a refused guest into a grudge.

### 5.8 How zones touch the other systems

| Zone | Reads from | Writes to |
|---|---|---|
| Stockpile | work (who carries what), map (cover), items | needs (where food is eaten), rations (what can be shared out), incidents (damp; what a thief can take), combat (where the watch must look) |
| Meeting place | social (opinions, grudges), map (hearths), mood, Schedule (free time) | work (idle choices), social (who meets whom), mood (`sat_apart`, `kept_away`), incidents (where arrivals go) |
| Home area | Schedule (blocks, kinds of day, the watch), traits, mood, social (opinion of you), calendar; culture once it lands | work (the walk home at dusk, refused candidates, the watch filter), needs (hunger obeyed into), social (witnesses, `kept_in`), combat (watch coverage), incidents (accidents in the dark) |

Each zone touches at least four systems, so each passes CLAUDE.md's cut rule with room to spare.

### 5.9 Tick order and cost
- **No new per-tick system.**
  - Zone checks sit inside `WorkSystem.Step`, `JobPicker`, `CombatSystem` and the incident code.
  - `Zones.Hourly` runs right after `MoodSystem.Hourly`. It does the dusk roll and the walk home at dusk, and clears the night state at dawn.
  - The watch filter runs inside Schedule's `WatchSystem.Step`, every 30 ticks.
  - Commands apply first in the tick, as now.
- **Cost:**
  - every zone query is one array read;
  - choosing a meeting place costs zones × members, and only when someone goes idle;
  - the per-character tick budget (ADR-0005) is untouched;
  - `TickCostUnderBudget` must pass with a home area active.

### 5.10 Compliance: who keeps the camp's rules
Three designs proposed three ways to decide who defies a rule. This doc's first draft added new trait fields. Schedule uses the Independent's existing `priorityRespectPct`. Culture-faith gives every person a place on the Oathbound–Free axis. They become one helper, `Compliance` (`sim/Systems/Compliance.cs`), so that breaking any of your rules reads the same and has one reason the Chronicle can name (lead's call).

**The odds** that a person goes their own way, in permille, clamped to 0–1000:
- **nature:** 6 for each point by which their `priorityRespectPct` falls below 100. The Independent (50) gets 300, as in the first draft.
- **feeling** (question 1): 5 for each point of opinion of whoever set the rule, either way, and at most 400 either way. Dislike adds and liking takes away. Whoever set the rule is you today, and a delegate at kingdom scale; whoever set a rule never rolls against it. Liking is what lets an Independent be won over, as Schedule's consent already allows.
- **mood:** plus 150 if their mood is below 25.
- **culture,** once culture-faith lands: minus 50 for each point of their `oathboundFree`, which is +100 at the Free end and −100 at the oath end.
- **customs,** in culture-faith's slice 2: a custom can settle the answer outright. On a vigil night the faithful go out whatever the curfew says; believers in a river omen stay away whatever the player allows.

**Who keeps rules** (and judges breakers; question 3): anyone with the Oathbound trait (`promisePriority` above 0, the same test Schedule uses for staying on watch), and, once culture-faith lands, anyone at `oathboundFree` +2. Player text never calls the axis "Oathbound"; that word stays the trait's (culture-faith section 4.6).

**No new trait fields.** Both tests read fields that already exist. Culture-faith's slice 2 already plans a trait-level nudge on the axis, so `keepsRules` and `defiesRulesPermille` would have been a third model from day one.

**How many people that is,** in a cast of ten. Each person's secondary trait is drawn evenly, from Truths, from the seven traits other than their signature, and the vampires are never bound.
- **Defiers by nature:** Sera, plus on average 0.9 more among the six other bound humans (each has a 1 in 7 chance of Independent as their secondary; the player never rolls), so about 1.9 a world. Anyone who dislikes you, or is in a black mood, can defy too.
- **Rule-keepers:** Oren, plus on average 1.3 more among the other nine, so about 2.3 a world.
- **With culture-faith's slice 1 weights:** roughly a third of humans lean Free (−1 or −2, adding up to 100‰ a night each), and about four in ten sit at +2 and keep rules.
- Under Schedule's day, breaches are limited by the chances to go out (section 3.3) more than by the odds, so the culture terms widen witness chains more than they add breaches. The soak reports both by source (section 9.2).

**One stream.** Every compliance roll draws from one Dice substream: the one Schedule adds for its consent rolls, so that no system's draws shift another's (ADR-0004). Its number is set at merge. `DiceSystem.Growth = 8` is already on `milestone-0` (ADR-0021), and Build and Schedule both propose 8, so each takes the next free value when it lands. Each rule has its own tag: Schedule's day-change consent and watch consent, and Zones' `ZoneDefy`.

**Who builds it.** Schedule's slice 1 lands before Zones step 3, so Schedule's builder writes `Compliance`, with Schedule's day-change consent as its first user: a person declines a change to their day with these odds. If Schedule ships with its own formula instead (accept with chance respect plus half their liking for you), step 3 moves that roll behind `Compliance` in its first commit, and the step's ADR records the change. An Independent with no feelings either way then declines 30% of changes rather than 50%. Liking lowers that, and someone who dislikes you or is in a black mood may now decline too. The watch's own-mind refusal (Schedule's reason 5) stays in Schedule's table, because it answers a companion who asks, not a rule you set. It still draws from the same stream.

**Reasons for the Chronicle.** The largest positive term names the reason: nature, feeling, mood or culture, with ties in that order. So whichever rule was broken, its line can say why in the same words.

**Culture's stream stays clean.** `Compliance` reads each person's axis and customs; culture code never calls the compliance roll. That keeps culture-faith's stream test (its test 28) true.

---

## 6. Content and schema changes

**New: `content/zones.json`** (slice 1 contents)
```json
[
  { "id": "stockpile",     "nameKey": "zone.stockpile.name",     "hintKey": "zone.stockpile.hint",     "behaviour": "stores", "layer": "use",  "color": "#c8b07a" },
  { "id": "meeting_place", "nameKey": "zone.meeting_place.name", "hintKey": "zone.meeting_place.hint", "behaviour": "gather", "layer": "use",  "color": "#d39a58" },
  { "id": "home",          "nameKey": "zone.home.name",          "hintKey": "zone.home.hint",          "behaviour": "home",   "layer": "area", "single": true, "color": "#a9b8a2" }
]
```

**New: `content/schemas/zones.schema.json`**
- An array of objects with no additional properties.
- **Required:** `id` (id pattern), `nameKey`, `hintKey` (loc-key pattern), `behaviour` and `layer`.
- **`behaviour`:** an enum, `stores | gather | home` in slice 1. Slice 2 appends `keep_out`, `leave_be` and `burial`; later, `grow` and `graze`.
- **`layer`:** `use | area`. Use zones do not overlap each other; areas overlap anything.
- **`single`** (boolean, default false) and **`color`** (`^#[0-9a-f]{6}$`, a view hint, as in `cast.json`).
- **Loading:** `ContentDb` loads the file with `ReadMerged`, so mods merge by id. It checks the loc keys and rejects a behaviour the code does not implement, as it does for incidents.
- **`RequiredContent.Zones`** = `stockpile` and `meeting_place`, the kinds `WorldGen` creates. A test keeps the list in step with the code.

**Unchanged: `traits.json`, `species.json` and their schemas** (lead's call). The first draft added `keepsRules` and `defiesRulesPermille` to traits and `atHomeInDark` to species. None is needed now. `Compliance` reads `priorityRespectPct` and `promisePriority`, which exist (section 5.10). The night folk follow from Schedule's `rhythm`, a species field the Schedule design adds and its sign-off approves (section 5.5).

**New entries in `thoughts.json`** (no schema change; each added to `RequiredContent.Thoughts`)
```json
{ "id": "kept_in",         "textKey": "thought.kept_in",         "mood": -2, "opinion": -2, "durationHours": 12 },
{ "id": "saw_rule_broken", "textKey": "thought.saw_rule_broken", "mood": -2, "opinion": -4, "durationHours": 48, "emote": "anger", "stackLimit": 3 },
{ "id": "sat_apart",       "textKey": "thought.sat_apart",       "mood": -3, "durationHours": 12, "emote": "gloom" }
```
The first draft's `by_the_fire` is gone; Schedule's `evening_by_the_fire` is the one fire thought (section 5.4).
`MoodSystem.AddThought` applies a thought's opinion effect every time it is added, even when capped, which is why each thought is throttled per night or per day.

**Loc keys** (all through `LocTable`; `_you_a`/`_you_b` variants wherever the player can fill a placeholder, ADR-0017 §4)
- **`en.json`:**
  - `zone.<kind>.name` and `.hint`;
  - `event.world.zone_*` (including the home area's own lines);
  - `event.incident.stolen_unseen`;
  - `event.incident.work_accident_dark` and `_tired_dark`;
  - `event.work.out_after_dark_*` (`_ways` comes with culture-faith);
  - `event.work.stayed_in_*`;
  - `event.mood.kept_away` and `_fire`;
  - `thought.kept_in`, `thought.saw_rule_broken` and `thought.sat_apart`;
  - place phrases `place_phrase.covered_ruins|shelter|hearth|watch_post|river|open`;
  - tooltip lines `place.zone.*`, `place.waiting_to_be_carried`, `place.in_the_dark` and `place.firelit`.
- **`en.ui.json`:** `ui.bar.zones_hint`; `ui.zones.title`, `.mark`, `.yours`, `.unmark`, `.unmark_hint`, `.keeps`, `.remove_hint`, `.needs_another`, `.stop_hint` and `.pending`; and the cursor hints (`ui.zones.cursor.*`, including `dry`, `open`, `out_of_sight` and `post_outside`).

**Unchanged in slice 1:** `world.json`, `work.json`, `items.json`, `incidents.json` (the damp and pile rules change behaviour, not parameters), `cast.json` and `roles.json`. The new tuning numbers stay in code, as ADR-0013 does for M0:
- as named constants in `Zones`: 8 lit tiles, and double odds for accidents in the dark;
- as named constants in `Compliance` (section 5.10): 6‰ per point of `priorityRespectPct` below 100; 5‰ per point of opinion of whoever set the rule, either way, capped at 400; 150‰ for a black mood; 50‰ per point of `oathboundFree`.

---

## 7. Determinism, snapshots, text-state API, localization, mod hooks

### 7.1 Truths and Dice

| What | Stream | Notes |
|---|---|---|
| Zone shapes | none | Player commands. The starting stockpile and meeting place come from the camp layout `WorldGen` already derives from Truths (`TruthId.CampPlacement`), so there are no new Truth draws. |
| Who goes their own way tonight | Dice, the compliance substream (the one Schedule adds; section 5.10) | New `DiceId.ZoneDefy`, appended with the next free number at merge. Schedule's doc takes 24 and 25, and a number is never reused once it is on main (ADR-0004). Drawn only when a home area exists and the odds are above 0. |
| Where an idle person stands in a meeting place | Dice, Work | The existing `WanderTarget` and `WanderWait` tags. |
| Accidents in the dark | Dice, Work | The existing single `AccidentRoll`; only the odds change. |
| Which meeting place; damp; unseen theft; the walk home at dusk; the watch filter | none | Follows from state, with ties broken by id, then (y, x). |
| Slice 2: sacred places, omens | Truths | Owned by culture-faith and peoples, with their own appended `TruthId`s. A grove a faith holds sacred is a world fact that survives rewinds. Your keep-out zone over it is an intention that does not. |

**Existing seeds change** even with no zones marked: idle people gather at the meeting place, which changes how many `WanderTarget` draws are used, and accident odds change in the dark. The determinism test (same seed and same commands give the same world) is unaffected. The nightly roll draws from the compliance substream, not the Work stream, so marking a home area shifts no Work draw. The soak baseline and the logs for seeds 1–4 are measured again and recorded, as Build does.

### 7.2 Snapshots
- **Serialised plainly:** zones (canonical tile runs), `ItemStack.Stored`, `Carry.Stored`, the characters' night memory, `World.HomeWatchNight`, and pending and applied zone commands.
- **Not serialised:** the caches, which are rebuilt on load.
- **Restore time:** zones add a few hundred bytes; the 1-second restore at 200 characters is unaffected.
- **A rewind restores the zones as they were at the fate knot.** The player remembers where the thief came in; the world does not. This follows from fixed decisions 3 and 4 and the rule that a fate knot restores the full world state, so it is no longer a question for the founder. Schedule's section 6.2 says the same of the camp's days. A later rewind UX can offer "lay out the camp as I had it" as one ordinary command (section 8.6).

### 7.3 Text-state API
All of these are reads and change nothing.
- **`StateApi.ZoneKinds(w)`:** id, name, hint, colour. Only behaviours this build implements.
- **`StateApi.Zones(w)`:** for each zone, id, kind, name, place phrase, tile count, bounds, the items kept, and `Pending` (a queued command will change it). Like `WorkPriorities` and `Policy`, it counts pending commands.
- **`StateApi.CanZone(w, kind, tiles)`:** a verdict and reason key per tile, from the same `Zones.Check` the commands use. The view's preview and the stand-in player use it.
- **`StateApi.Location`** gains the zone lines, "waiting to be carried", and lit or dark at night.
- **The runner's story log:** its camp section lists zones ("Zones: a stockpile in the covered ruins (9 tiles); a meeting place by the hearth; a home area of 230 tiles.").

### 7.4 Localization
- Every string the player reads is a loc key with arguments; the sim composes no English (ADR-0007).
- Zone names come from `zones.json`, and place phrases from loc.
- The Chronicle variants follow `EventText`: `_you_a`, for example "You went out after dark to help Wren.", and `_one` where counts are one.

### 7.5 Mod hooks
- `zones.json` merges by id from mod folders. A mod can add a kind with an existing behaviour, such as a "granary" that stores, with its own name and colour, without code.
- Zone events reach `IModHook.OnEvent` at the end of the tick, like all events.
- A hook that changes zones calls `Zones.Mark` and `Zones.Remove`, which keep the caches and `Map.Revision` right. It must never edit `Map.Zones` directly. Note this on `IModHook`.

---

## 8. How it fits the parallel work

### 8.1 Build (`2026-11-09-build-design.md`)
- **The boundary:** Zones owns painted areas, and Build owns things made by work. Build's question 4 and section 7.4 say the same.
- **Graves are Build's.** The burial ground (slice 2) is the camp's standing intention to plan graves there.
- **Shared predicates:**
  - stores (`Zones.Accepts`, which Build's store extends in its slice 2);
  - cover (`Map.Covered`, with Build's cached cover array);
  - light (Build's hearths, and later lanterns, extend the lit test in 5.5);
  - `Map.Revision`, which both raise.
- **Placement checks each other:** `Building.Check` treats every stores tile, not the old rectangle, as not open ground. `Zones.Check` refuses tiles with buildings or plans.
- **Build's enclosure flood fill** must keep at least one tile of every stockpile reachable.
- **One tool feel:** drag on the map; right-click or Esc stops; paused means a draft; the cursor hint gives the reason. A shared Godot-free `MapTool` in `game/scripts/Logic` would serve both.
- **Stand-in plans:** Build's `camp` plan searches outward from the stockpile. It should search from `Map.StartStockpile`, so that `--plan` and `--zones` stay independent.

### 8.2 Schedule (`2026-11-09-schedule-design.md`)
Schedule was written after this doc's first draft. Where the two touch, this doc gives way, because the rule both docs state puts the night on Schedule's side: **Schedule says when; Zones say where.**
- **Schedule owns the night.** Its day (sleep, work, free time) and its watch decide who is up, who works and who keeps the post. The home area adds only a place: where bound people go and work after dark. So step 3 is built on Schedule's slice 1 and starts after it merges (section 9.1). Steps 1 and 2 do not touch the night and start now.
  - Free time sends people to the meeting place.
  - Sleep sends them to beds, wherever the beds are.
  - Work goes anywhere the zones allow.
- **What changed in this doc because of it:**
  - with no `keep_watch` candidate left in `JobPicker`, the home area is a filter in `WatchSystem.PickNext`, outside Schedule's cover and debt rules (section 5.5);
  - an empty post has one cause, Schedule's `GapEvent`. `stayed_in_watch` becomes it, and the first draft's `WatchStayedInEvent` is gone;
  - the night folk come from Schedule's kinds of day, and the first draft's `atHomeInDark` is gone;
  - there is one fire thought, Schedule's `evening_by_the_fire`; the first draft's `by_the_fire` is gone;
  - there is one wander rule (section 5.4);
  - the walk home at dusk is each person's choice, and marking a home area never pulls anyone off a job, as Schedule's rule for changes of hours requires (section 5.5);
  - the dark and worn-out doublings multiply (section 5.5);
  - story 2 is told on Schedule's day.
- **What this doc asks of Schedule's builder:**
  - `ScheduleSystem.FreeTimeSpot` calls `Zones.MeetingPlaceFor` before its hearth default, once Zones step 2 exists. Schedule's section 7.4 already expects this.
  - `WatchSystem.PickNext` takes a candidate filter, which Zones step 3 fills.
  - Lines that cite tonight's last refusal accept `stayed_in_watch` as one.
  - The day-change consent goes through `Compliance` (section 5.10).
- **A night shift works inside the home area.** If the player wants night foraging, they leave the home area off or draw it round the bushes.
- **The home area is not "hours on zones".** Schedule's section 8.3 leaves hours on zones (a zone the player gives its own hours, such as "nobody in the forest after dark") to a later joint feature. The home area's hours are the dark, a fact of the world, not hours the player sets. Slice 2's keep-out after dark is the same kind of thing. Hours that the player sets stay the later joint feature.
- **Reconciling the record.** Schedule's section 7 asks the later of two siblings to record a reconciliation in DECISIONS.md. Step 3's ADR records this section.

### 8.3 Peoples (`2026-11-09-peoples-design.md`)
- **Night folk:** there is no species field for it. The peoples work sets each species' `rhythm` in Schedule's `rhythms.json`, and the night folk follow from that (section 5.5): the vampires now, and the sleepless undead with Schedule's slice 3. The ghoul, dokkaebi and house spirits join them if the peoples doc gives them a day that sleeps in daylight, which its section 11 asks Schedule for.
- **What the peoples doc asks of Zones** (its section 11): leave-be ground round mounds, old graves and a dryad's grove; a run ground, and a room to keep a werewolf in; hanging trees spared for a vetala. All of these are slice 2 or later.
- **Dryads (forest):** leave-be zones (slice 2) are the player's tool for keeping a dryad's forest, and `event.work.felled_tree` with its zone is the breach a dryad hears of.
- **Djinn (contracts):** a possible hook to offer. A zone left unchanged for some days counts as a standing term, and reshaping it counts against a djinn's need. The peoples doc decides.
- **Fae (cannot lie):** a fae who breaks a zone confesses it, which is a variant line. In slice 2, a fae who witnesses a breach tells the camp, so witnesses need not have been present.
- **Undead (purpose):** tending the burial ground can be purposeful work (slice 2, with Build's graves).

### 8.4 Culture and faith (`2026-11-09-culture-faith-design.md`, being built)
- **What this doc offers:**
  - **places to attach to:** the meeting place, for guest-right, festivals and holy days; the burial ground, for death rites; keep-out and leave-be, for sacred places and omens;
  - **reason variants:** `event.work.out_after_dark_custom` and `event.work.refused_place_custom` take the custom's loc key as an argument;
  - **compliance terms:** a slot in the nightly roll and in the keep-out check, so a culture can make a zone easier or harder to keep, or make keeping it impossible. For example, believers in a river omen never go to the riverbank at night, zone or no zone; on a vigil night, the faithful go out whatever the curfew says.
- **What it asks of culture-faith:**
  - express those terms as data in its own files (the schema is theirs);
  - seed sacred places and omens from Truths, with its own appended `TruthId`s;
  - tell the burial ground whether a grave is this person's rite.
- **The Oathbound–Free axis** (fixed decision 5; `oathboundFree` in culture-faith's section 4.6) joins the compliance helper as a term, not as a replacement for the traits (section 5.10, lead's call). Leaning Free adds to a person's odds of going their own way, and leaning toward oaths takes from them. People at the far oath end keep rules and judge breakers, alongside the Oathbound trait. The traits stay the main term, because the founder asked for culture to micro-influence events, not to decide them. Culture-faith's slice 2 trait nudge on the axis then reaches compliance with no change here.
- **The Wild–Civil axis** has no effect in culture-faith's slice 1, and Zones leaves it that way for now. A Wild person bristling at a curfew waits for slice 2.

### 8.5 Character growth (fixed decision 12, BIBLE section 5)
- **M0 needs nothing from Zones.** Zones only emit events with causes.
- **For M1's trait-change tables,** these are natural experiences:
  - hurt in the dark after a defiant night ("learned the hard way");
  - rescued outside the home area (mercy received);
  - sitting apart for days (grief or resentment).
- **Suggested wants for `wants.json`:** `a_place_by_the_fire` (resolved by sitting at the meeting place with someone once at odds) and `see_the_dead_honoured` (with the burial ground).
- Every zone event carries the ids M1 needs to cite.

### 8.6 Not now, but built to allow
- **Kingdom layer, by delegation:** zones carry `MadeBy`, and commands can carry who decided. At kingdom scale, a steward companion lays out an outpost's zones by their own nature, and the Chronicle says "Oren marked a stockpile…". Zones gain a camp id when there is more than one camp.
- **Rewind:** zones are part of the timeline. A later rewind UX might offer "lay out the camp as I had it", sent as one ordinary command.
- **Director:** later incidents can read zones, for example a thief who goes for the least-watched pile, or pressure that notices people out in the dark.

---

## 9. Slices and acceptance

### 9.1 Slice 1, in order
Each step can ship on its own.
- **Steps 1 and 2 start now,** alongside Build and Schedule. They do not touch the night.
- **Step 3 starts after Schedule's slice 1 merges.** It is built on Schedule's blocks, kinds of day and `WatchSystem` (section 8.2), and it extends the `Compliance` helper, or creates it if Schedule shipped without one (section 5.10). If Schedule is not signed, step 3 waits.
- **Before each step,** run the Appendix A probe and the soak on the merge base, and record them in that step's ADR as its baseline (section 9.2).

**Step 1: the zone framework and the stockpile**
- `zones.json` and its schema; `Zone`, the caches and the commands; `Zones.Check`.
- The stores refactor in 5.3, including `Stored`, the damp rule, the pile rule and the unseen theft.
- The `StateApi` reads; snapshot fields; the tests.
- The view: the Zones list, the marking and unmarking tools, the overlay, tooltip lines, and drafts.
- The stand-in's `dry` plan.

**Step 2: the meeting place**
- Gathering and choosing, with the one wander rule in section 5.4; keeping away; arrivals.
- The default meeting place; the overlay.

**Step 3: the home area, on Schedule's day**
- The roll through `Compliance` (section 5.10).
- The curfew: the walk home at dusk and the choosing filter.
- The watch filter in `WatchSystem.PickNext`, with `stayed_in_watch` as `GapEvent`.
- The excuses, breaches, witnesses, kept in, and obeying at a cost.
- The dark (lit test, accident odds, multiplying with Schedule's worn out); the cause fix in 5.5.
- Chronicle folding.
- The stand-in's `home`, `short` and `both` plans.

**The stand-in player** (`tools/Return.Runner/Autoplay.cs`)
- `run` and `soak` gain `--zones none|dry|home|short|both|mixed`.
  - `mixed` picks by `(seed / 9) % 5`, so it varies independently of the ration policy (`seed % 3`) and of the `(seed / 3) % 3` that Build's `--plan mixed` and Schedule's plans use.
  - Commands go out on tick 0, after the policy.
- **The plans:**
  - **`dry`:** mark a stockpile on the covered ruins' floor (`Map.CoveredRuins`), then remove the starting one, in that order, so the guard passes.
  - **`home`:** mark the camp clearing, which is `WorldGen`'s 22 × 19 rectangle round the shelter, as the home area. The clearing holds the watch post.
  - **`short`:** the same, without the rows from the watch post's row down, so the post lies just outside. This is story 3's setup; without it no stand-in world ever exercises the watch filter (lead's call).
  - **`both`:** `dry` and `home` together.
- Plans go through `StateApi.CanZone`, so they are valid on every map.
- Nightly CI moves to `--zones mixed` alongside Build's `--plan mixed`.

### 9.2 Acceptance

**Tests**
- **ContentTests:**
  - `zones.json` validates;
  - an unknown behaviour fails at load, naming the file and id;
  - every kind's loc keys exist;
  - `RequiredContent.Zones` and `.Thoughts` match the code.
- **DeterminismTests:**
  - the scripted run also marks, moves and removes zones: the same hash twice, and a different hash for a different layout;
  - pending zone commands survive a snapshot;
  - `TickCostUnderBudget` passes with a home area.
- **SnapshotTests:** snapshot mid-move (a stored stack waiting, a hauler carrying with `Stored` set), and at 20:00 with defiance already rolled. Restoring and continuing equals the straight run.
- **ZoneCommandTests (new):**
  - make, reshape (absolute tiles), remove;
  - invalid tiles are dropped, and a command that changes nothing emits nothing;
  - use zones take tiles from each other;
  - a second home area merges;
  - the guard refuses removing the last berry stockpile or unticking its berries;
  - drafts: marking and unmarking while paused sends nothing.
- **StoresTests (new):**
  - berries dropped on a stockpile are stores, and loose ones are not;
  - after a move, food waiting to be carried can be eaten and shared out, and haulers move it first;
  - a berries-only stockpile does not take timber;
  - `food_spoils` spares covered stores and does not fire when every stored berry is covered;
  - a thief takes only from the pile it reached;
  - `stolen_unseen` cites a marked zone and does not cite the starting one.
- **HomeAreaTests (new; they run on Schedule's slice 1):**
  - at dusk a human outside at work leaves it and walks in, and a night-folk person (a nocturnal kind of day) does not;
  - marking a home area at 21:00 pulls nobody off the job in hand, and their next choice obeys it;
  - a home area drawn short of the beds never keeps anyone from their bed;
  - a starving human, or a Desperate one below 220, goes out once, and the breach cites hunger and the zone;
  - a defiant Independent goes out, and an Oathbound person within 8 tiles gets `saw_rule_broken` about them, while someone 9 tiles away does not;
  - a hungry human obeys in free time, and `stayed_in_hungry` becomes their `LastFoodEvent`;
  - `kept_in` lands at most once a night;
  - the player never rolls, and nobody counts as defiant before tonight's roll;
  - with the watch post outside the home area, `PickNext` skips bound candidates without waking them (no `woken`, no cover, no debt); `stayed_in_watch` fires once and becomes `GapEvent`; a theft that night is `stolen_unwatched` and cites it; and a night-folk watcher still takes the post;
  - accident odds double in the dark beyond 8 tiles for humans, and not for night folk or within the light; for someone worn out they are four times, with the `_tired_dark` text.
- **MeetingPlaceTests (new):**
  - idle people go there, and pick the place where the people they like are;
  - a grudge holder keeps away while the target is there (once a day; the event cites the grudge);
  - arrivals walk there;
  - with Schedule, free time goes to the meeting place Zones picks; with none, to the hearth at radius 2; a species' own free-time place comes first.
- **ComplianceTests (new):**
  - the odds for a given trait, opinion, mood (and axis, once culture-faith lands) are the same whichever rule asks;
  - the reason follows the largest term, with ties going nature, feeling, mood, culture;
  - every compliance draw comes from the one compliance substream (a static scan, like culture-faith's test 28).
- **CausalityTests:**
  - starving → forage → foraged → accident in the dark → care → owes is found as a story with Needs, Work, Incident and Social;
  - a defiant breach is not on the accident's chain.
- **YearInvariantTests** run with every zone plan:
  - no stored stack is lost or duplicated;
  - nobody stands on an impassable tile;
  - every cause names an earlier event;
  - no tile is in two use zones;
  - no bound member who is neither excused nor defiant starts a job with a target outside the home area in the dark.
- **GameViewTests** (Godot-free):
  - `ZoneTool` rectangles and joining a neighbouring zone;
  - drafts leave no trace;
  - list rows, cursor hints and tooltip lines;
  - the Chronicle fold;
  - every UI key exists.
- **ArchitectureTests:** `Return.Sim` stays free of Godot.

**Soak** (100 worlds × 1 year, `--zones mixed`, alongside Build's `--plan mixed` and Schedule's plans)
- **The baselines come from the merge base, not from this doc** (lead's call). Appendix A was measured at `c818511`, before Schedule, Growth and culture-faith. ADR-0021 measured that same commit at 8.47 stories per world-year, already under the 8.5 floor this doc first set (ADR-0017's 8.5 is that figure rounded), and the growth merge left it unchanged. Before each step, the builder runs the probe and the soak on the merge base and records both in the step's ADR. Every target below is measured against those numbers, or against the `--zones none` worlds of the same run, which share every other branch's changes. The `c818511` figures stay in brackets as the record.
- **No regressions:**
  - no failures;
  - serial and parallel soaks byte-identical;
  - stories per world-year not below the merge base's figure, and worlds with `--zones none` within 10% of it.
- **Demo regression stories hold** (ADR-0011):
  - meal theft → grudge in at least 60% of worlds (67% at `c818511`);
  - broken promise → departure in at least 30% (38% at `c818511`);
  - if the merge base already sits below either floor, that floor becomes 90% of the merge base's figure, and the step's ADR says so.
- **Balance guards:**
  - insults per world-year no more than 1.3 × the `--zones none` figure;
  - accidents per world-year no more than 1.5 × the `--zones none` figure (2.33 at `c818511`, when the ceiling was 3.5);
  - member deaths at most 0.1 per world-year (0.02 at `c818511`).
- **Stockpile** (`dry` worlds):
  - spoilage at most 20% of the `--zones none` figure (6.5 at `c818511`, when the target was 1);
  - thefts reported against `none`, with dry worlds expected higher;
  - at least 30% of dry worlds have a story whose chain contains the stockpile move.
- **Home area** (`home`, `short` and `both` worlds):
  - unexcused starts outside after dark by bound members: none. This is the year invariant above, not a tuning target. It replaces the first draft's "at most 5 not-hungry night starts", which Schedule's day makes moot, since free time and sleep start no ordinary work.
  - reported: breaches by reason, compliance, witness thoughts by source (trait or culture), walks home at dusk, watch-filter nights and `stayed_in_watch`;
  - a story whose chain contains a home-area event (`stayed_in_*` or `out_after_dark_*`) in at least 25% of these worlds together. This floor is provisional. The probe before step 3 measures again the chances the home area acts on (hungry evening starts beyond the clearing, nights with no night folk to keep the watch). If they fall well short of Appendix A's, the step's ADR lowers the floor, but never below 15%.
  - The cut rule (fixed decision 1): below 15%, step 3 ships only the watch filter and the dark, and the rest moves to slice 2 with keep-out.
- **Meeting place** (all worlds):
  - evening idle hours spent inside a meeting place at least 60% (24% near the hearth at `c818511`; Schedule's free time moves this on its own, so compare against the merge base);
  - `kept_away` in at least 40% of worlds.
- **New soak tallies:** the soak summary gains, by zone plan, the zone stories (chains containing a zone, breach, compliance, keeping-away or unseen-theft event), breaches by reason, compliance, keeping away, walks home at dusk, watch-filter nights, witness thoughts by source, thefts, spoilage, and accidents split into dark and lit, and worn out or not.
- **STORIES.md:** notable zone stories are added there.

**Screenshots** (`RETURN_SCREENSHOT`)
- **The list:** `RETURN_OPEN=zones RETURN_PAUSE=1`. The Zones list open over the camp, with your zones listed.
- **A drafted stockpile:** `RETURN_PAUSE=1 RETURN_PRESS=zone:stockpile:x:y:3:3`, with the ruins' coordinates for the seed. A hatched stockpile over the ruins, drafted.
  - `RETURN_PRESS` gains `zone:<kind>:x:y:w:h` and `unzone:x:y:w:h`. They go through the same tool code as the mouse.
- **The home area at night:** `RETURN_TICKS` to 20:00 on a `home` seed, with `RETURN_FOCUS`. The home area's faint edge, one person beyond it, firelight.
- **The meeting place at dusk:** people sitting by the fire, and one sitting apart (a seed the soak shows with `kept_away`).
- **Frame time:** the night scene with every overlay shown stays within 10% of today's frame time (`RETURN_FPS`).

### 9.3 Slice 2 and later
Each has an entry condition.
- **Keep out and leave be:** when the peoples branch has dryads, or culture-faith has sacred places or omens.
  - Path cost in the pathfinder; target filters; witnesses.
  - Culture terms; `refused_place_custom`.
- **Burial ground:** after Build slice 1c (graves) and with culture-faith's rites, and measured in multi-year worlds.
  - Graves planned in the zone on a member's death; nobody digs outside the home area after dark; rites decide the form.
- **Thieves who waylay people alone in the dark:**
  - **Who:** a member who is awake, alone (no other awake member within 6 tiles), unlit and carrying goods.
  - **What happens:** within the thief's sight, they are robbed of the load, with one exchange of blows on the Combat stream (`CombatHit` and `CombatDamage`, plus a new `DiceId` to pick the target).
  - **Why:** it makes the curfew a safety rule as well as a social one. Today it would be rare, because only 2.27 thieves a year come in the evening (Appendix A), and more common once nights are longer or more dangerous.
- **Later:** growing plots (items milestone, section 3.6), grazing (animals), delegated zones (kingdom layer).

---

## 10. Questions for the founder
Most important first, each with my recommendation. The first draft asked seven. Four are answered by decisions already made, so they now sit in the body:
- growing plots wait for the items milestone (ADR-0018 §5; section 3.6);
- the burial ground waits for slice 2 (fixed decision 1; section 3.5);
- zones go back with the timeline on a rewind (fixed decisions 3 and 4; section 7.2);
- the camp starts with no home area (fixed decision 2; section 3.3, lead's call).

1. **Should how people feel about you decide how far your intentions carry?** One helper decides whether someone goes their own way under any rule you set: your zones now, and changes to their day once Schedule's consent uses it (section 5.10). Each point of dislike for whoever set the rule adds 5‰ to the odds, up to 400‰, and each point of liking takes 5‰ away. So an Independent who likes you well enough goes along, as Schedule's consent already allows. The reason is always given in the Chronicle.
   *Recommendation: yes.* It is fixed decision 2 made concrete: you govern through people, and people who resent you do not follow you.
   *Why it matters:* it decides who can defy at all. With it, anyone who dislikes you can; without it, only the Independent, people in a black mood and, later, people whose ways are free.
2. **Should work in the dark beyond the firelight go wrong twice as often, in every world?** Under Schedule's day this falls on jobs that run on past dusk, starving foragers and anyone on night hours. For someone also worn out (Schedule's rule) the two multiply to four times (lead's call).
   *Recommendation: yes.* Without a cost to the dark, the home area is only a social rule, and Build's hearths and lanterns have nothing to give.
   *Why it matters:* it changes accident odds in every world, whether or not anyone marks a zone.
3. **Should people who hold a promise heavy think less of people they see defy the camp's rules?** That means the Oathbound trait now and, once culture-faith lands, people whose culture and faith hold oaths heaviest (section 5.10). It applies to defiance only, never to the starving or to helpers.
   *Recommendation: yes.* It is how a rule splits a camp, and with culture-faith it makes your rules a place where peoples clash.
   *Why it matters:* without it, defiance costs nothing socially and story 2's chain never forms.

---

## Appendix A. Baselines (how measured)
**These were measured at `c818511`, before Schedule, Growth and culture-faith.** They record what this design was drawn from; the targets in section 9.2 are not measured against them. Two things have moved since. ADR-0021 measured the same commit's soak at 8.47 stories per world-year; the 8.5 in ADR-0017 is that figure rounded, and the growth merge (`17be2d5`) left it unchanged. And Schedule's default day removes most of the not-hungry night starts below, because free time and sleep start no ordinary work. Before each step, run this probe again on the merge base and record the new table in the step's ADR (section 9.1).

- **Method:** a throwaway console program in the session scratchpad, not committed.
  - It referenced the Release build of `Return.Sim` at `c818511`; the sim last changed at `05c68ee`.
  - It ran seeds 1 to 100 for one year each, with the stand-in player's ration policy (`seed % 3`).
  - It counted job starts after each tick and sampled positions hourly. It only read state.
- **Night as counted:** "after dusk" means not daylight, 19:00 to 06:00.
- **The camp clearing** is `WorldGen`'s 22 × 19 rectangle round the shelter.

| Measure, per world-year | Value |
|---|---|
| Forage, felling and hauling jobs humans start after dusk | 109.3 |
| … of which target outside the camp clearing | 60.4 (forage 59.6, felling 0.6, hauling 0.1) |
| … of which target more than 8 tiles from the hearth | 82.9 |
| Outside the clearing, started by someone starving / hungry but not starving / not hungry | 34.5 / 1.8 / 24.1 |
| Outside the clearing, started by someone Independent | 18.6 |
| Vampire night jobs more than 8 tiles from the hearth | 13.5 |
| Bushes in the clearing / within 8 tiles of the hearth (of 12) | 4.0 / 2.1 |
| Trees in the clearing (of 24) | 5.6 |
| Watch post to the default berry pile (tiles; the watch sees 8) | 9 in every seed; nearest stockpile tile 7; covered ruins 13 |
| Evening (19:00–22:00) awake member-hours / within 3 tiles of the hearth / idle | 817 / 200 (24%) / 674 (82%) |
| Days a grudge holder was idle while their target sat by the hearth | 3.7, in 66 of 100 worlds |
| Work accidents (of which after dusk) | 2.33 (0.98) |
| Thieves (19:00–22:00) / spotted / thefts | 8.15 (2.27) / 5.0 / 5.0 |
| "Spoiled in the damp" | 6.5 |
| Member deaths / thieves killed / departures | 0.02 / 0.05 / 2.01 |

The camp's layout, relative to the shelter's top-left corner (WorldGen places that corner from Truths). It shows why stockpile placement is already a thief lever:
```
        x: -6 -5 -4 -3 -2 -1  0  1  2  3  4  5  6  7  8  9 10 11 12
 y= 0       .  .  .  .  .  .  #  #  #  #  #  #  #  #  .  #  #  #  .      #  wall      D  door
 y= 1       .  .  .  .  .  .  #  b  .  b  .  b  b  #  .  R  R  R  .      b  bed       R  covered ruins
 y= 2       .  .  .  .  .  .  #  .  .  .  .  .  .  #  .  R  R  R  .      S  stockpile m  meeting place
 y= 3       .  .  .  .  .  .  #  .  .  .  .  .  .  #  .  R  R  R  .      H  hearth    W  watch post
 y= 4       .  .  .  .  .  .  #  #  #  #  D  #  #  #  .  .  .  .  .
 y= 5       .  .  .  .  .  .  .  .  m  m  m  m  m  .  .  .  .  .  .
 y= 6       .  S  S  S  S  .  .  .  m  m  m  m  m  .  .  .  .  .  .
          ┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄┄  the watch at W sees rows 7 and below, columns −4 to 12
 y= 7       .  S  S  S  S  .  .  .  m  m  H  m  m  .  .  .  .  .  .
 y= 8       .  S  S  S  S  .  .  .  m  m  m  m  m  .  .  .  .  .  .
 y= 9       .  .  .  .  .  .  .  .  m  m  m  m  m  .  .  .  .  .  .
 y=15       .  .  .  .  .  .  .  .  .  .  W  .  .  .  .  .  .  .  .
```
New berries go to the stockpile's top-left tile (−5, 6), 9 tiles from the post. The covered ruins are 12 to 14 rows above it.

## Appendix B. Code that reads the fixed stockpile today
These sites change in step 1. Each one moves to `Zones.Accepts`, stored stacks or `Map.StartStockpile`, as noted. Line numbers are at `17be2d5`; the growth merge moved those in `WorldGen`, `IncidentSystem` and `StateApi`.

**`sim/`**
- `WorldGen.cs:184, 188, 226`: makes the starting zone; the reserved area and start items use `StartStockpile`.
- `Stock.cs:13, 38, 60`: stored stacks; drop tile by `Zones.Accepts`.
- `Places.cs:173`: stored stacks.
- `Rations.cs:88`: `AfterDeposit` on any accepting tile.
- `JobPicker.cs:253`: haul candidates (section 5.3).
- `IncidentSystem.cs:179`: the thief's fallback target is the first stockpile tile.
- `StateApi.cs:199, 224`: `Faction` counts stored stacks; `Location` lists zones.

**`game/`**
- `WorldArt.cs:122, 235`: the dashed rectangle moves from the baked ground to the per-frame zone overlay.
- `GroundWear.cs:24`: wear follows the stockpile zones at bake time.

**`tests/`**
- `CausalityTests`, `IncidentCombatTests`, `RationingTests`, `WorkTests`, `NeedsMoodTests` and `WorldGenTests` use `w.Map.Stockpile.Center`. They move to `w.Map.StartStockpile.Center`, which is still a stockpile tile while the starting zone is unchanged.
