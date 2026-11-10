# The Build menu: design

- **Date:** 2026-11-09
- **Status:** proposed, for the founder's sign-off
- **Covers:** the Build button on the bottom bar: plans (blueprints) on the grid, delivering timber, building, repairing and taking down; beds and who owns them; graves and laying the dead to rest (designed here, built later with culture-faith's death rites); and the hooks that stores, shrines, storms and fire will use later.
- **Written alongside:** `2026-11-09-zones-design.md`, `2026-11-09-schedule-design.md`, `2026-11-09-peoples-design.md`, and `2026-11-09-culture-faith-design.md` (being built).
- **Sources:** CLAUDE.md; BIBLE.md sections 2, 4.2, 4.6, 4.10 and 5; ADR-0004, 0006, 0008 and 0012–0020; the last soak (`out/soak`, 100 worlds × 1 year); and the code named in each section.

**What signing approves.** The new content file `content/buildings.json` and its schema. New entries, but no new fields, in `work.json`, `roles.json`, `traits.json` and `thoughts.json`. The new commands, enum values and snapshot fields in section 4. The runner's `--plan` flag in section 8. Section 4.8 as the only design for laying the dead to rest; that slice's code and content are signed off when it is planned (8.4). It does not approve the items asked in section 9.

**In short**
- The player marks out plans on the grid. People choose, through their work priorities, who carries the timber and who builds. They can refuse, for example to finish a wall that would shut someone in.
- The first slice adds walls, doors, roof sections and beds. Envy follows (1c). Graves and laying the dead to rest wait until deaths are common, then come as one slice with culture-faith's death rites (4.8, 8.4). Stores, watch posts, hearths, shrines, storms and fire come in slice 2.
- Stories come from scarce timber, things that belong to people and, once deaths are common, the dead who lie where they fell. They end in grudges, debts, broken promises, departures and deaths. These are the effects the story analyzer already counts. Finishing a building is never a story ending by itself.
- Slice 1 is additive. It keeps the demo's roof and bed records and extends them, adds a structure list for walls and doors (graves join it later), and adds no random draws.
- Player-facing words are *plan*, *marked out*, *take down* and *call off*. Code says Blueprint, TakeDown and CancelBlueprints.

---

## 1. Why build: the stories

The demo camp already has one building story. The shelter has three broken roof sections, a vampire is promised a sound roof, and the story turns on whether someone mends it in time (`CampJobs.RepairRoof`, ADR-0014). The Build menu makes that general. The player marks what the camp should have, and the people decide who builds it, when, and with what. What gets built then changes who sleeps where, who stays dry, who owes whom, and who resents whom.

Building makes stories for three reasons:
- **Timber is scarce and shared.** Every plan draws on one stock from 24 trees that do not grow back yet. When the last timber goes into one thing, something someone else was counting on does not get built.
- **What is built belongs to someone.** A bed is somebody's bed. The person who built it, the person who sleeps in it and the person sleeping on the floor beside it all feel something about it.
- **The dead lie where they fell.** Bodies stay on the map (ADR-0019) until someone lays them to rest, and how and by whom matters to the living. This waits until deaths are common (4.8).

Three stories this design makes, in the in-game Chronicle's format:

**The last of the timber** (slice 1a)
```
Day 2, 21:10 — Ilex agreed to stay after you promised them a sound roof against the sun within 2 days.
Day 3, 08:30 — You marked out a bed.
Day 3, 15:05 — Sera used the last of the timber on a bed.
Day 3, 17:40 — Sera built a bed.
Day 3, 23:15 — Sera claimed the bed they built.
Day 4, 22:00 — You broke your promise to Ilex.
Day 4, 22:00 — Ilex now holds a grudge against you.
Day 4, 22:00 — Ilex left the camp.
```
The chain runs: arrival (Incident) → promise (Social) → your plan (World) → the last of the timber (Work) → promise broken → grudge → left (Social). Nobody is a villain. Sera had no bed, you asked for one, and the roof could wait, until it couldn't.

**A bed of one's own** (slice 1c)
```
Day 5, 11:20 — Hollis built a bed.
Day 5, 22:45 — Hollis claimed the bed they built.
Day 6, 06:10 — Nia has no bed and resents Hollis for having one.
Day 7, 12:06 — Nia insulted Hollis.
Day 8, 19:30 — Nia insulted Hollis.
Day 9, 08:48 — Nia insulted Hollis.
Day 9, 08:48 — Hollis now holds a grudge against Nia.
```
The chain runs: built (Work) → claimed (Social) → resentment (Mood) → insults → grudge (Social). Each insult cites Nia's resentment, because that is her worst thought about Hollis.

**Laid to rest** (later: the laying-to-rest slice, 8.4)
```
Day 27, 04:12 — Mira died of their wounds.
Day 27, 17:00 — Wren cannot bear that Mira lies unburied.
Day 27, 17:20 — You marked out a grave for Mira.
Day 28, 10:30 — Bram buried Mira.
Day 28, 10:30 — Wren owes Bram for burying Mira.
```
The chain runs: thief (Incident) → fight (Combat) → death → unburied (Mood) → plan (World) → burial (Work) → debt (Social). Culture-faith's rites come in the same slice, so the same burial can go wrong: "Sera, who keeps the rites of the pyre, now holds a grudge against Bram."

None of these needs a new kind of effect. Finishing a building is deliberately **not** a persistent effect. That way "planned → built" can never count as a story on its own, which is the trap ADR-0014 fell into with promises that were kept within the hour.

## 2. Goals, non-goals, anti-goals

**Goals**
1. The player marks intentions on the grid: plans, things to take down, plans to call off. People decide who builds what and when, through their work priorities, and they can refuse (fixed decision 2).
2. Every building has an effect in the sim that reaches at least two other systems, and a story it exists to make (table in 4.1). Nothing is built only to look nice.
3. Who built something, whose it is and who lies where are stored in state. The player reads them in the tooltip and the Chronicle; tools read them through the text-state API.
4. Plans compete for scarce timber, so plans have costs other people feel.
5. The first slice adds no random draws and no new kinds of story ending, and every existing content field keeps its current meaning.

**Non-goals** (other documents, or later)
- Painted areas: stockpiles, groves to spare, a home area, burial grounds. These belong to the Zones doc.
- When people work, sleep or rest, and holy days. These belong to the Schedule and culture-faith docs.
- Temperature, insulation, room quality scores, floors, furniture that spans several tiles, crafting stations, farming, traps, and buildings with more than one storey.
- The rites themselves: what form a burial must take, who performs it, and the taboos. These belong to culture-faith, as a layer on this doc's grave and act of laying the dead to rest. All of it is built later, in one slice (4.8, 7.2).

**Anti-goals**
- A colony builder with elves. There are no production chains and no catalogue that grows for its own sake. The menu stays near ten entries through slice 2, and every entry has a story.
- A spreadsheet. There is no building manager screen and no list of every structure. The map is the list.
- Direct control. The game never says "Oren, build this". The player cannot choose who builds, only what is planned and how high building ranks in each person's priorities.
- Building that is free or instant, or a building whose only effect is a number nobody ever hears about.

## 3. How the player uses it

### 3.1 What the camp has and lacks today
Everything here comes from `WorldGen.BuildMap` and is placed from the Truths stream:
- an 8×5 shelter of old stone walls with one door on its south side;
- an 18-tile interior under three 2×3 roof sections, all broken;
- four bedrolls along the shelter's north wall;
- the covered ruins (3×3), where the vampires shelter by day;
- a hearth, a 4×3 stockpile and a watch post;
- 24 trees and 12 berry bushes.

Up to ten people live here, and there are four beds. So what the camp lacks, in order: beds first (six short by the end of the first week), dry space as it grows, and a place for the dead. Later it will want a dry store, a second watch post and shrines.

### 3.2 The Build list
Build on the bottom bar becomes a toggle like Orders and Work (`BottomBar.Toggle` and `ListPopup`). Pressing it opens a list above the bar:

```
            ┌ Build ──────────────────────────────┐
            │ Shelter                             │
            │   Wall ............. 1 timber each  │
            │   Door ............. 2 timber       │
            │   Roof ............. 1 per 2 tiles  │
            │ Rest                                │
            │   Bed .............. 2 timber       │
            │ ─────────────────────────────────── │
            │   Take down       Call off plans    │
            │   Timber in store: 7                │
            └─────────────────────────────────────┘
[Build][Orders][Zones][Work][Schedule] | [People][Chronicle]   Day 3, 14:20 · Spring | Pause ×1 ×3 ×6
```

- The entries come from `StateApi.Buildings`, which the sim provides, so buildings added by mods appear too.
- Hovering an entry shows its description. An entry the camp cannot build yet is greyed out, with the reason (for example "Comes with faiths" for shrines in slice 2).
- **Grave** joins the list, under Rites, when the dead can be laid to rest (4.8, 8.4). If the peoples doc's old graves reach the map first, it is listed greyed until then (lead's call, 4.8).
- Picking an entry closes the list and starts placing. No panel stays open.

### 3.3 Placing
- **Wall, door, bed, grave.** A click places one. A drag places a straight line along the drag's longer axis. Shift-drag places the outline of a rectangle, so a room takes one gesture.
- **Roof.** Drag a rectangle. The view splits anything larger than 4×4 into sections of at most 4×4, and each section is its own piece.
- **One release is one plan.** It sends one `PlaceBlueprint` command and adds one Chronicle line ("You marked out 13 walls."). It adds one more line when the plan is finished.
- **The ghost** under the cursor shows each tile as valid (pale ochre dashed outline, faint silhouette) or invalid (a red ✕). A hint by the cursor gives the reason and the cost:

```
  .  ▒  ▒  ▒  ▒  ▒  .         ▒  planned wall (pale dashed ghost)
  .  ▒  .  .  .  ▒  .         D  planned door
  .  ▒  .  .  .  ✕  .         ✕  can't go here: a tree stands on it
  .  ▒  ▒  D  ▒  ▒  .
                 ↖ Wall · 13 marked · 13 timber · 7 in store; people will fell trees for the rest
```

- Right-click or Esc stops placing.
- The grid lines darken while a Build tool is active. This is BIBLE section 2's "only while building" option, used as emphasis.
- While the game is paused, a plan is a draft, as orders are today (`SimHost.Order` → `OrderDrafts`). It is drawn paler, with "Takes effect when time moves". Calling it off while still paused leaves no trace.

### 3.4 Watching it happen
Plans sit on the map as ghosts. Timber appears on the site as it is delivered, and walls rise as the work goes on. The builder swings a hammer: `Poses` already maps `RepairRoof` work to `Pose.Swing` with `HeldTool.Hammer`. Hover any tile to see who built it and whose it is:

```
 planned        timber here       half built        built
 ┌╌╌╌╌┐          ┌╌╌╌╌┐            ┌────┐           ████
 ╎ ░░ ╎          ╎ ░░ ╎ ≡          │▓▓▓▓│           ████
 └╌╌╌╌┘          └╌╌╌╌┘            └────┘

 Tooltip lines:  Bed — Mira's · built by Oren
                 Wall · built by Sera
                 Planned roof · 2 of 3 timber here · waiting for walls
                 Grave of Bram
                 To be taken down
```

### 3.5 Taking down and calling off
**Take down.** Drag over built things to mark them; they get a red dashed outline. Click a marked thing again to unmark it. Later, someone whose priorities include building takes it down, and half its timber comes back as a loose stack.

Some things cannot be taken down yet:
- the old stone walls and the old roof, until stone exists (slice 3);
- graves, because desecration is a faith matter (with culture-faith's rites, 8.4).

**Call off plans.** Drag over ghosts to call them off. Any timber already delivered drops on the site as a loose stack for hauling.

### 3.6 Graves (later: 8.4)
Choose Build → Grave and click a tile. A small list asks who the grave is for. The grave keeps the name of whoever lies there for the rest of the world's life.

```
 ┌ For whom? ──────────────────┐
 │  Mira (died day 27)         │
 │  A thief (died day 19)      │
 │  Whoever needs it first     │
 └─────────────────────────────┘
```

### 3.7 Priorities
A new kind of work, **Build**, appears among the inspector's priorities (1–4, or –) and in the Work list, through `JobPicker.PrioritizedWork` as the existing kinds do. Haul now also covers carrying timber to plans.

### 3.8 What the view needs (slice 1b)
- **`BottomBar`.** Build becomes a `Toggle` with a `ListPopup`. `RETURN_OPEN=build` opens it.
- **`game/scripts/Logic/BuildTool.cs`.** New and Godot-free, compiled into the tests like `OrderDrafts`. It holds the tool modes, the line, outline and roof-splitting logic, and the draft keys.
- **`OrderDrafts`.**
  - Today `SameSlot` keys a draft by the thing it changes. A placement is its own slot, with a local draft id.
  - Calling off a draft removes it. Calling off a placed plan sends `CancelBlueprints`.
  - Marking and unmarking the same thing for taking down replace each other.
  - `Flush` sends any placement that still has a valid tile, and the sim checks again when it applies.
- **`MapView` ghosts.** Drawn from three sources: `World.Blueprints`, `World.PendingCommands` (placed this tick, not yet applied) and the drafts.
- **`MapView` and `WorldArt`: built pieces.**
  - **New timber walls** are drawn as split logs, so they read differently from the old masonry. `WorldArt.DrawWall` chooses by whether a wall structure exists at the tile.
  - **Doors.**
  - **Roof sections.** `WorldArt.RoofRect` and `RoofSortY` assume the shelter's rectangle today. Instead, stretch each section to the wall centres it touches and sort it just in front of the wall below it. `MapView._roofAlpha` is a 16-slot array indexed by section; key it by roof id instead.
  - **Beds.** Today `WorldArt.DrawGround` bakes the bedrolls into the ground. Beds leave the bake and become drawables. The blanket shows the owner's cloth colour, or straw when unclaimed, so whose bed it is reads at a glance.
- **Refresh on change.** `WorldArt` builds its wall map, grass tufts and ground wear once, in its constructor. They must refresh whenever `Map.Revision` changes (4.2). The ground bake only needs redoing if the floors change, and slice 1 never changes floors.
- **`TileTooltip`.** Its lines come from `StateApi.Location`, which is extended (6.4).
- **`SimHost`.** `RETURN_PRESS` gains `build:<id>:x,y[,w,h]`, which places through the real tool path, and `takedown:x,y`.
- **Laying-to-rest additions (later: 8.4).** The grave (a low mound and a wooden marker tied with a scrap of the dead person's cloth colour), the "For whom?" list, a body carried to its grave (`MapView.Bearer` handles only the downed today), and buried bodies no longer drawn.

## 4. What changes in the simulation

### 4.1 Buildings, what they do and the stories they are for

| Building (kind) | Slice | What it does in the sim | Stories it is for | Systems touched |
|---|---|---|---|---|
| Wall | 1a | Blocks movement; holds up roofs | Rooms; who was shut in or out | Travel, Work |
| Door | 1a | A walkable gap in a wall; holds up roofs | Enabler now; barred and iron doors later | Travel |
| Roof section | 1a | Covers from rain and sun, like the old sections | A vampire's promised shelter; who stays dry; thanks to whoever raised it | Needs (sun), Incident (rain), Social (promises), Mood |
| Bed | 1a | A sleep spot that restores rest faster; one person claims it | A place of one's own; the builder's right; envy | Needs (rest), Mood, Social |
| Grave | later (8.4) | Holds a body and a name | Grief for the unburied; debts for laying the dead to rest; rites (with faith) | Mood, Social, Work |
| Store | 2 | Holds goods that count as stock and stay dry | Spoilage averted or not; what thieves go for | Incident, Needs, rations |
| Watch post | 2 | A second post that sees thieves sooner | Lonely night watches; thieves met at the edge | Combat, Work |
| Hearth | 2 | A second gathering place, and a fire risk | The camp splitting around two fires | Social, Incident |
| Shrine | 2 | A place for a faith; culture-faith gives it meaning | Refusals to build it; offence; desecration | Faith, Social, Mood |

Later: stone walls and cairns, iron-bound doors, a vampire's coffin, and a seat by the fire someone calls their own (section 8).

### 4.2 State (additive in slice 1)

The demo's roof and beds are read in many places:
- `CampJobs.RepairRoof` indexes `Map.Roof[j.TargetId]`;
- `IncidentSystem.UnmetNeed`;
- `Places.SleepSpot` and `TakenBed`;
- `JobPicker.OnBed`;
- `StateApi.Faction` and `Location`;
- `WorldArt` and `GroundWear`.

The peoples branch is touching some of these right now. So slice 1 adds to the model rather than replacing it. A single list of structures is the right end state, and it comes after the parallel branches merge (question 1).

**Changed and new records**
- **`RoofSection`** gains `Id`, `BuiltBy` (a list of character ids; empty means the old camp), `BuiltEvent`, `MendedBy` (the last person to repair it), `MarkedEvent` (0 unless it is marked for taking down) and `Removed`.
  - New sections are appended to `Map.Roof`.
  - A section that is taken down stays in the list with `Sound = false`, so `Index` remains its position and `RepairRoof` keeps working.
  - Every reader that treats an unsound section as broken must skip `Removed` ones, or a removed section gets repaired straight back from stock. These are `JobPicker.RoofJob`, `Places.TimberNeeded`, `StateApi.Faction` (`RoofTotal`) and `Location`, and the art.
  - `Map.Covered` is unchanged: sound sections give cover.
- **`Bed`** already has an unused `OwnerId`. It gains `BuiltBy`, `BuiltEvent`, `ClaimEvent`, `PrevOwnerId` and `MarkedEvent`. New beds are appended to `Map.Beds`; a bed that is taken down is removed.
- **`Structure`** (new) holds the kinds that have no record yet: walls and doors built in this playthrough, and graves. Fields:
  - `Id`, `DefId` (the id in buildings.json), `Pos`;
  - `Ground` (the terrain it replaced);
  - `BuiltBy`, `BuiltEvent`, `PlanEvent`;
  - `ForId` (for graves: who lies here);
  - `MarkedEvent` (0 unless it is marked for taking down).

  They live in `Map.Structures`, in id order.

**Walls and doors write through to terrain.** A finished wall sets `TerrainKind.Wall` at its tile and a door sets `TerrainKind.Door`. Taking one down restores `Ground`. Passability, pathfinding, the wall art and every check that reads terrain keep working unchanged. The invariant: every Wall or Door tile is either part of the old camp (no structure) or has a standing wall or door structure.

**More new state**
- **`Map.Revision`** is a number that goes up whenever terrain, roofs, beds or structures change. The view watches it, and blocked plans check again when it changes.
- **`Blueprint`** (new; `World.Blueprints`, in id order). Fields:
  - `Id`, `DefId`;
  - `Area`: 1×1, except a roof section;
  - `PlanEvent`: the placement event, shared by every piece of one plan;
  - `Delivered`: timber on the site, committed to it;
  - `Progress`: work ticks done. It is kept when a builder is interrupted, so anyone can continue;
  - `Contributors`: who delivered timber or worked on it;
  - `ForId`: graves;
  - `Blocked`: none, waiting for walls, would shut people in, or someone is in the way;
  - `BlockedRevision`.
- **`Character`** gains `DiedEvent` and `RestingIn` (the grave's id) when the dead can be laid to rest (4.8). Another slice may add `DiedEvent` sooner, as defined there.
- **Enums.** Append only; snapshots store names. `JobKind.Build`, `Deliver` and `TakeDown`. Laying the dead to rest adds `JobKind.LayToRest` and `DebtKind.LaidToRest` later, and they are the only kinds for that act (4.8).
- **`Job`** has a single integer `TargetId` today. Deliver and take-down jobs must also know what kind of thing they are aimed at (a plan, a roof section, a bed or a structure), so `Job` gains `TargetKind`.
- **Schema version.** `World.SchemaVersion` goes to 2. There are no shipped saves to break.

### 4.3 Commands
All three are tick-stamped records in `Commands.cs` with `JsonDerivedType` registrations. They are recorded and replayable like the ration policy (ADR-0004). Each one checks itself again in `Apply`, because a draft made while paused applies a tick or more later.

- **`PlaceBlueprint(Tick, BuildingId, Pieces, ForId = 0)`.** `Pieces` is a list of rectangles: 1×1 for tile buildings, up to 4×4 for roof sections. One command is one plan.
  - `Apply` drops every piece that is not valid now. A roof section is dropped whole if any of its tiles is invalid.
  - If pieces survive, it emits `event.world.planned` with how many. If nothing survives, it emits `event.world.plan_failed` ("Your plan for a bed could not be placed: something is in the way.").
- **`CancelBlueprints(Tick, BlueprintIds)`.** Removes them. Delivered timber drops as a loose stack on the site. It emits an event only if something from that plan had been delivered or built ("You called off the walls.").
- **`MarkTakeDown(Tick, Targets, Marked)`.** Marks or unmarks built things. Targets are pairs of kind and id: a roof section, a bed, or a structure. Marking emits `event.world.marked_take_down`, once per gesture. Unmarking emits nothing.

### 4.4 Placement rules
One function in `Return.Sim`, `Building.Check(World, buildingId, pieces)`, answers for each tile with a reason key. The view's ghost (through `StateApi.CanPlace`), each command's `Apply` and the stand-in player all use it.

**Open ground.** A tile is open for building when all of these hold:
- it is in bounds;
- it is grass, soil or floor (graves: grass or soil only);
- it is not the hearth, the watch post, the stockpile (stockpiles are zones) or the covered ruins;
- no tree or bush stands there and no body lies there;
- no other plan on it would overlap. A roof section is overhead, so it never overlaps a bed or grave plan, whichever is placed first. The `camp` plan relies on this: it places a hut's roof before the beds inside it.

**Then, by kind:**
- **Wall.** No bed, grave, roof section or door on the tile.
- **Door.** As for a wall, and there must be a wall or planned wall on two opposite sides.
- **Roof section.** Rules:
  - a rectangle of at most 4×4;
  - no wall or door tile inside it;
  - no sound or planned roof already over any of its tiles;
  - every tile within 3 tiles (Chebyshev distance) of a wall or door, standing or planned.

  The old 2×3 sections meet this rule, and a room up to 6 tiles across can be roofed.
- **Bed.** No wall, door or grave on the tile.
- **Grave.** No wall, door or bed on the tile, and not inside the shelter.

**Checks made at build time, not at placement**
- A roof section is built only once every tile is within 3 of a *standing* wall or door. Until then it is "waiting for walls".
- A wall or door is never finished while anyone active (a member or a hostile) or a body is on its tile; until then it is "someone is in the way". Without this, `YearInvariantTests` would fail, because it asserts that no member or hostile stands on a tile they cannot pass. Loose items on the tile are moved aside without an event.
- The enclosure rule in 4.5.

### 4.5 Work: delivering, building, taking down
People do three things, chosen through their priorities like any other work (`JobPicker.WorkJob`):
- **Deliver** (under `haul`). When no loose stack needs hauling, carry timber from the stock to the oldest plan still short of it. Delivered timber is committed: it leaves the stock count and lies on the site.
  - A delivery never brings more than the plan still lacks.
  - Two people never deliver to the same plan at once (`Places.Targeted`, as for hauling today).
  - While a roof promise is open, deliverers leave enough timber in stock to mend or raise one roof section (3). Haulers carry timber where it is needed; they do not decide what goes without. Builders can still take that timber for a plan they choose, and that is how someone gets passed over (4.7).
- **Build** (new `build` work).
  1. Pick a plan.
  2. Fetch any timber it still lacks, as `RepairRoof` does today.
  3. Go to it: beside a wall or door tile, onto a bed or grave tile, or under a roof section.
  4. Work for the building's `workTicks`.
  5. Finishing does the following:
     - creates the roof section, bed or structure;
     - raises `Map.Revision`;
     - rebuilds the passability caches;
     - emits the event (4.10) and adds the thoughts (4.6);
     - rolls for a work accident with `build`'s odds, through `JobRunner.RollAccident` (the existing Work Dice draw).
- **Take down** (under `build`). Walk to a marked thing, work for half its `workTicks`, remove it, and return `takeDownPct` (50%) of its timber, rounded down, as a loose stack.

**Where building sits among the other work.** `build` goes right after `repair_roof` in `JobPicker.WorkOrder`: care, repair_roof, build, gather_timber, forage, haul, keep_watch. When scores tie, `WorkJob` keeps the earlier kind, so at equal priority a builder mends the old roof before building anything, with one exception.

**The builder's right.** Someone without a bed, who is not Generous, scores a bed plan as if their `build` priority were one step higher (a score 100 lower). So a bedless builder with both at 1 builds a bed before repairing the roof. That is the choice the first story in section 1 turns on.

**Which plan a builder picks**, in this order:
1. An Oathbound person who owes a promised roof builds or mends a roof first. This extends `JobPicker.PromiseJob`, which runs before `WorkJob`, from repairs to roof plans.
2. A bed plan, for someone with the builder's right.
3. Otherwise, the oldest plan (lowest id) they can reach and supply, ties broken by distance. A Generous person skips a plan that would pass someone over (4.7).

Sun, hunger, exhaustion and wounds interrupt building as they interrupt other work. Vampires build outdoors only at night, and Anxious people skip building when they are fragile, because `build` is risky work.

**Refusing to shut people in** (fixed decision 2 in action).
- Before finishing a wall piece, the builder runs one flood fill from the hearth with that tile blocked. Every member and the stockpile must still be reachable.
- If not, they refuse: `event.work.refused_to_shut_in` ("Oren would not finish the wall: it would shut people in.").
- The plan is marked blocked, and nobody tries it again until `Map.Revision` changes. A door is the answer.

**Felling follows demand.** Today `Places.TimberNeeded` counts only broken roof sections. It will count the timber still owed to plans and to broken roofs, minus the stock and what people are carrying, so builders fell trees only when plans need them.

### 4.6 Beds, comfort, pride and thanks
**Claiming.** Lying down in an unclaimed bed claims it, if you have no bed. On the first night the three starters claim three of the four bedrolls. Events:
- `claimed_bed`: "Mira claimed a bed."
- `claimed_bed_built`: "Nia claimed the bed Oren built."
- `claimed_own_bed`: "Sera claimed the bed they built."
- `claimed_bed_of`: "Nia claimed the bed that was Mira's.", when its last owner left or died.

A claim on a bed built in this timeline cites the bed's built event.

**Where people sleep** (`Places.SleepSpot`), in this order:
1. their own bed;
2. an unclaimed bed they built themselves;
3. an unclaimed bed under a roof;
4. any unclaimed bed;
5. today's fallbacks: covered floor, the shelter floor, or where they stand.

Nobody sleeps in someone else's bed. Vampires still put cover first, and use their own bed only if it is covered. When an owner leaves or dies, their bed becomes unclaimed without an event; `PrevOwnerId` remembers whose it was.

**Comfort.** Sleeping in a bed restores rest `restBonusPct` (20%) faster. On waking, people get `slept_in_own_bed` (+3) in their own bed, or `no_bed` (−2) if they slept without one. Both stack with the existing `slept_in_shelter` and `slept_outside`.

**Pride.** Finishing a bed, a roof section or a whole plan gives the builder `built_something` (+3 for 24 hours).

**Thanks: who built what.** Waking under a roof section someone else raised or mended, or in a bed someone else built, gives `sheltered_by` (+1 mood, +2 opinion) about that person, at most once a day. When Oren mends the roof, everyone who sleeps under it thinks a little better of him.

### 4.7 Materials and disputes
**Prices** (in buildings.json):

| Building | Timber |
|---|---|
| Wall | 1 per tile |
| Door | 2 |
| Roof section | 1 per 2 tiles, rounded up (a 2×3 section costs 3, as the repair does today) |
| Bed | 2 |
| Grave | 1 |

**Budget.**
- **Supply.** 24 trees × 3 timber (`gather_timber`'s yield) = 72. Add `startTimber` of 2 and river windfalls of about 4 timber a year.
- **The old roof** takes 9.
- **The stand-in player's `camp` plan** takes 28: three beds in the shelter, a 5×4 hut with a door, its roof, and two beds inside. Graves, when they come (8.4), add 1 each.
- **In total** that is about 40 timber, over half the forest, within the plan's first weeks.

Trees do not grow back until slice 2, so a long plan or a second year will strip the map. That is a real story (dryads, later) and a soak hazard now, so the soak reports the trees left in each world (6.4).

**Passing someone over.** Someone has a *stake* in a plan Y in either case:
- it would keep a promise owed to them: an open roof promise, and any roof plan or broken roof section;
- they have no bed and Y is a bed.

When a builder or deliverer commits timber to a plan X, check whether the stock left can still cover every plan someone has a stake in. If it cannot cover some plan Y, and X does not meet the same stake, then three things happen:
- **One event**, `event.work.used_last_materials` ("Sera used the last of the timber on a bed."). Its actors are the taker and everyone passed over; its cause is X's plan event.
- **A thought.** Each person passed over gets `passed_over` (−4 mood, −8 opinion) about the taker, caused by that event, so a later insult can cite it.
- **The broken promise cites it.** The creditor of a roof promise gets that event as their `LastRestEvent`. `SocialSystem.CheckPromises` already cites `LastRestEvent` when a roof promise breaks, so the first story in section 1 needs no new plumbing.

**A known seam,** in ADR-0018's style. `IncidentSystem.RainUpkeep` sets `LastRestEvent` to the rain for anyone it soaks. If it rains on the creditor between the last-timber event and the promise's due time, the break cites the rain instead. That is honest, because the rain is also why they want a roof, but it shortens the chain. If soaks show it often, keep the passing-over event on the promise itself (a cause field on `Debt`) and have `Relations.Break` cite both.

### 4.8 Laying the dead to rest (later: one slice with culture-faith's rites)

**Not now.** Graves, the act of laying the dead to rest, grief for the unburied and the debt that follows all wait until deaths are common. Then they come as one slice with culture-faith's death rites and the Zones burial ground (8.4).
- **Why.** A feature exists only if it makes players retell unplanned stories (fixed decision 1), and the early slices build the smallest set of systems that does (CLAUDE.md). In the last soak (`out/soak`, 100 worlds × 1 year) a member died in 2 worlds, both times the vampire Vale burning in the sun. The burial bar this doc set, a burial in at least half of the planned worlds where a member dies, would be judged on one or two worlds, and "Laid to rest" would almost never be told. The peoples branch adds new ways to die, and they stay rare: by that branch's soak, its undead faded to rest in 3 of 100 one-year worlds.
- **When.** With aging and multi-year soaks, which ADR-0020 already requires before M1 can be accepted.
- **The other docs wait too.** Culture-faith (its question 5) and Zones (its question 2) put off their parts of the same act for the same reason. The founder answers the timing once, as culture-faith's question 5. If the answer is "rites now", this whole slice moves up and is still built once, from this section.
- **Built once.** Enums are append only, because snapshots store their names. If this doc's job and debt were built now and culture-faith's later, one act would have two job kinds and two debt kinds for good.

**One act, owned here** (lead's call). Build owns things made from materials by work (7.4), and the Zones and peoples docs already build on this doc's grave and act.
- **Build owns** the grave and other `grave`-kind buildings (a pyre is one), the body at rest, the act, the general grief (`unburied`, `laid_to_rest`) and the debt.
- **Culture-faith owns** what the act means, as a layer on top. Section 7.2 lists the contract.
- **One job kind and one debt kind,** with names that fit every rite: `JobKind.LayToRest` and `DebtKind.LaidToRest` (lead's call). Neither this draft's `Bury` and `Burial` nor culture-faith's `PerformRite` and `Rite` is added.
- **The grave is sim state, not only something drawn.** The body moves onto the grave tile and keeps it as its `Pos`, `RestingIn` names the grave, and the grave's `ForId` names the dead. A dead person stays in `World.Characters`, so the peoples doc's ghoul (which can eat a buried body only by digging it up) and its raids on "a body, a grave" have real targets.

**Its own work** (lead's call). The act is a work of its own in the priorities, **Lay the dead to rest** (`lay_to_rest`), right after `care` in `JobPicker.WorkOrder`, not part of `build`. The dead should not wait behind beds and roofs. Its own priority also lets the player choose who tends the dead: a carer, the peoples doc's ghoul as undertaker, or an undead bound to that work. That is delegation as fixed decision 2 means it. This takes culture-faith's placement for its rite work.

**The starting design.** Its numbers are tuned in that slice.
- **Unburied.**
  - Every hour, the sim looks at each named dead member who has not been laid to rest and has been dead for 12 hours or more.
  - Each member whose opinion of the dead person is 30 or more gets `unburied` (−6, refreshed hourly) about them.
  - The first time, it emits `event.mood.unburied` ("Wren cannot bear that Mira lies unburied."), caused by the death (`Character.DiedEvent`, which `Lifecycle.Die` sets).
- **What starts it.** A plan for the dead: one the player marks (3.6), or one the Zones burial ground marks on its own. Culture-faith may have a due rite mark its own form.
- **Laying to rest** (`LayToRest`).
  - The person goes to the body: the plan's `ForId`, or else the named member who died first and has not been laid to rest.
  - They carry it, as `Rescue` carries the downed, work for the grave's `workTicks`, and lay the body to rest. The body's `RestingIn` is set, it moves to the grave tile, and the grave structure records `ForId`.
  - Timber for the marker is delivered or fetched like any plan's.
  - Strangers, such as a dead thief, are laid to rest only in a grave marked for them (lead's call; this was question 10). Friends grieve for their own unburied dead, not for thieves.
- **Afterwards.**
  - The sim emits `event.work.buried` ("Bram buried Mira."), caused by the death, the plan and the unburied event if there was one.
  - The dead person's friends lose `unburied` and get `laid_to_rest` (+4 for 48 hours).
  - The dead person's closest friend owes whoever laid them to rest: the member with the highest opinion of the dead, provided it is 30 or more and they did not do it themselves. This is a `DebtKind.LaidToRest` debt ("Wren owes Bram for burying Mira."), repaid the way other debts are (`CareJobs.SettleOrOwe`). `Relations.FormDebt` emits only `a` and `b` today, so it needs an `owes_laid_to_rest` case in its key switch and an optional third argument, `c`, for the dead.
  - The death's own story lies inside the burial's chain, so it is counted once, as the longer story (ADR-0017).

**How culture-faith's D1 to D4 sit on this** (its section 5.4):
- **D1, a rite due:** at death, a form for the dead person (grave, pyre or river) and a deadline. `unburied` runs anyway; it is the grief underneath.
- **D2, the rite as work:** the same `LayToRest` job. Culture-faith decides the form and who may do it, through the check in 7.2, and kin of the dead's culture score `lay_to_rest` one priority higher. A pyre fetches its timber like any plan.
- **D3, a neglected rite:** when the deadline passes, culture-faith's event and the kin's grudge against the leader come on top of `unburied`.
- **D4, the mourner's debt:** `DebtKind.LaidToRest`, with the rite's own words ("Wren owes Bram for burning Mira's body.").

**The grave before the act** (lead's call).
- The buildings schema approved with this doc keeps `grave` among the kinds and `rites` among the categories, so the grave needs no schema change later.
- The grave's entry joins `buildings.json` with the first slice that puts a grave on the map: the peoples doc's old graves (its slice 2A) or this one.
- Until the act exists, a grave only holds a name or none (`ForId` 0), is drawn and named in the tooltip, keeps plans off its tile (4.4), and cannot be taken down (3.5).
- The Build list shows it greyed until then, with "Not available in this milestone" (`ui.bar.not_yet`), the words the bottom bar already uses for its disabled buttons.
- Whichever slice needs `Character.DiedEvent` first adds it as defined here. The ghoul's chains cite the death.

### 4.9 Envy (slice 1c)
- **When.** A person wakes from a night without a bed, and there was no unclaimed bed they could have taken.
- **Whom.** They resent the bed owner they like least, provided their opinion of that owner is below 20.
- **The thought.** `envies_bed` (−3 mood, −2 opinion), with `AboutId` set to the owner, refreshed nightly. Opinion effects apply on every refresh, which is why the numbers are small.
- **The event.** The first night of a stretch emits `event.mood.resents_bed` ("Nia has no bed and resents Hollis for having one."), caused by the owner's claim.
- **The link to insults.** Because the thought is about the owner, `SocialSystem.Insult` cites it at an ordinary mood: it cites the worst thought about the person insulted.

This is the first event tagged `SystemTag.Mood`; nothing emits one today. `event.mood.unburied` joins it when the dead can be laid to rest (4.8).

### 4.10 Events

| Key | System | Causes | Effect | Notes |
|---|---|---|---|---|
| `event.world.planned` | World | — | — | The player is the actor. One per command: "You marked out 13 walls." |
| `event.world.plan_failed` | World | — | — | Nothing in the command could be placed. |
| `event.world.plan_called_off` | World | plan | — | Only if something was delivered or built. |
| `event.world.marked_take_down` | World | — | — | One per gesture. |
| `event.work.built` | Work | plan; passing over; promise | — | One per bed, roof section or grave, because claims and thanks cite them. |
| `event.work.plan_finished` | Work | plan | — | Walls and doors: one when the plan's last piece is done. |
| `event.work.roof_repaired` | Work | promise | — | Unchanged. |
| `event.work.used_last_materials` | Work | plan | — | 4.7. |
| `event.work.refused_to_shut_in` | Work | plan | — | 4.5. |
| `event.work.took_down` | Work | mark | — | One per bed or roof section; one per gesture for walls. |
| `event.social.claimed_bed` (and variants) | Social | built | — | 4.6. |
| `event.social.promise_kept` | Social | built | DebtRepaid | A new roof section keeps a roof promise, as a repair does today. |
| `event.mood.resents_bed` | Mood | claim | — | 1c. |
| `event.mood.unburied` | Mood | death | — | Later (4.8). |
| `event.work.buried` | Work | death; plan; unburied | — | Later (4.8). The grave's form; culture-faith adds a key for each other form. |
| `event.social.owes_laid_to_rest` | Social | buried | DebtFormed | Later (4.8). |

**Why walls and doors report once per plan.** The in-game Chronicle lists the last 30 events with no filter (`ViewText.Chronicle`), so a wall run reported tile by tile would push everything else out.

**The runner's story log** shows only notable events: those with an effect, those from Incident or Combat, and those on a story's path (`StoryLog.Analyze`). Placing and finishing appear there only when they are part of a story.

### 4.11 Taking down things people own or built
- **A bed.** Its owner gets `lost_bed` (−6 mood, −10 opinion of whoever marked it, which is the player).
- **Anything someone built.** Its builder gets `work_torn_down` (−3 mood, −3 opinion of the player).

If either opinion reaches the grudge threshold, a grudge forms, as it does after insults (question 5).

### 4.12 How Build touches the other systems
A proposed new row for BIBLE 4.2:

| System | Reads from | Writes to |
|---|---|---|
| Build (plans, structures, beds; graves later) | work, map, items, needs, social | needs (rest, cover from sun and rain), mood (comfort, pride, thanks, envy; grief later), social (claims, passing over, roof promises kept and broken; debts for laying the dead to rest later), travel (walls), incidents (work accidents) |

## 5. Content and schema changes

### 5.1 New: `content/buildings.json` and `content/schemas/buildings.schema.json`

| Field | Type | Meaning |
|---|---|---|
| `id` | id | |
| `nameKey`, `descKey` | loc key | Menu name and hover text. Event text also uses `building.<id>.a` ("a bed") and `building.<id>.plural` ("beds"), checked at load. |
| `kind` | enum: `wall`, `door`, `roof`, `bed`, `grave` | The behaviour the sim implements. A mod can add a building of an existing kind, such as a plank bed, without code. An unknown kind fails the schema; a kind with no behaviour fails at load, as incidents do. `grave` is in the enum from the start, so its entry can join later (4.8) without a schema change. Slice 2 adds `store`, `watch_post`, `hearth` and `shrine`. |
| `category` | enum: `shelter`, `rest`, `rites` | How the menu groups it. Slice 2 adds `storage`, `watch` and `faith`. |
| `materials` | list of {`item`, `count`, `perTiles`?} | The cost. `perTiles` is for roofs (1 per 2 tiles). Items must exist in items.json. |
| `workTicks` | integer ≥ 1 | Work to finish; per tile for roofs. |
| `takeDownPct` | 0–100 | Share of the materials returned when it is taken down. |
| `placeOn` | list of terrain | `grass`, `soil`, `floor`. |
| `maxSide`, `supportRange` | integer | Roof only. |
| `restBonusPct` | 0–100 | Bed only. |
| `tags` | list of ids | Free tags for other systems: `wood`, `stone`, `iron`, `grave`, `holy`. A coordination point with peoples and culture-faith (section 7); nothing in slice 1 reads them. |

The file in slice 1a (the grave joins later, with the first slice that puts one on the map: 4.8):
```json
[
  { "id": "wall", "nameKey": "building.wall.name", "descKey": "building.wall.desc", "kind": "wall", "category": "shelter",
    "materials": [{ "item": "timber", "count": 1 }], "workTicks": 300, "takeDownPct": 50,
    "placeOn": ["grass", "soil", "floor"], "tags": ["wood"] },
  { "id": "door", "nameKey": "building.door.name", "descKey": "building.door.desc", "kind": "door", "category": "shelter",
    "materials": [{ "item": "timber", "count": 2 }], "workTicks": 450, "takeDownPct": 50,
    "placeOn": ["grass", "soil", "floor"], "tags": ["wood"] },
  { "id": "roof", "nameKey": "building.roof.name", "descKey": "building.roof.desc", "kind": "roof", "category": "shelter",
    "materials": [{ "item": "timber", "count": 1, "perTiles": 2 }], "workTicks": 100, "takeDownPct": 50,
    "maxSide": 4, "supportRange": 3, "placeOn": ["grass", "soil", "floor"], "tags": ["wood"] },
  { "id": "bed", "nameKey": "building.bed.name", "descKey": "building.bed.desc", "kind": "bed", "category": "rest",
    "materials": [{ "item": "timber", "count": 2 }], "workTicks": 450, "takeDownPct": 50, "restBonusPct": 20,
    "placeOn": ["grass", "soil", "floor"], "tags": ["wood"] }
]
```
When the grave joins (4.8):
```json
  { "id": "grave", "nameKey": "building.grave.name", "descKey": "building.grave.desc", "kind": "grave", "category": "rites",
    "materials": [{ "item": "timber", "count": 1 }], "workTicks": 600, "takeDownPct": 0,
    "placeOn": ["grass", "soil"], "tags": ["wood", "grave"] }
```
A roof takes 100 ticks per tile, so a 2×3 section takes 600 ticks, the same as today's repair.

`ContentDb` loads the file like the others (`ReadMerged`, so mods merge by id). It checks the materials, the fields each kind needs and the loc keys. `RequiredContent.Buildings` names the ids the code relies on: wall, door, roof and bed, plus grave once it joins. The old camp and the stand-in player use them. A test keeps the list in step with the code, as it does for work and thoughts.

### 5.2 New entries in existing files (no new fields)
- **`work.json`.**
  - `build`: `restCostPerHour` 25, `accidentPermille` 20, `risky`. Its `durationTicks` is 0, because each building carries its own work time.
  - Entries that are not prioritised but give the new jobs a rest cost: `deliver` (10) and `take_down` (25, accident 15). `NeedsSystem.JobRestCost` finds work by the job kind's snake-case name, so these names must match `JobKind`.
  - Laying the dead to rest brings its own prioritised entry, `lay_to_rest`, later (4.8).
- **`roles.json`.** `build` at 1 for builders and 3 for everyone else.
- **`traits.json`.** Practical gets `{ "work": "build", "add": 1 }` next to its repair bias.
- **`thoughts.json`.** All numbers are starting points for soak tuning, as in ADR-0013. `RequiredContent.Thoughts` gains every one.

  | Thought | Slice | Mood | Opinion | Lasts | Emote |
  |---|---|---|---|---|---|
  | `built_something` | 1a | +3 | — | 24 h | sparkle |
  | `slept_in_own_bed` | 1a | +3 | — | 12 h | — |
  | `no_bed` | 1a | −2 | — | 12 h | — |
  | `sheltered_by` | 1a | +1 | +2 | 24 h | — |
  | `passed_over` | 1a | −4 | −8 | 48 h | anger |
  | `lost_bed` | 1a | −6 | −10 | 72 h | anger |
  | `work_torn_down` | 1a | −3 | −3 | 48 h | gloom |
  | `envies_bed` | 1c | −3 | −2 | 24 h | anger |
  | `unburied` | later (4.8) | −6 | — | 2 h, refreshed hourly | gloom |
  | `laid_to_rest` | later (4.8) | +4 | — | 48 h | music |

- **`repair_roof` stays as it is in slice 1:** the same id, name and `timberCost` of 3. Until storms exist, nothing but a broken roof section can need repair. Renaming it to `repair` waits for slice 2 (question 2).
- **Loc.**
  - `en.json`: building names and their article and plural forms, events, thoughts, tooltip lines, `job.build`, `job.deliver`, `job.take_down`, `work.build.name` (and the other new work names `ContentDb` requires), and reason keys under `build.reason.*`. The keys for laying the dead to rest come with that slice (4.8).
  - `en.ui.json`: the Build list and its hints. `GameViewTests` keeps UI strings in this file.
  - `en.log.json`: the camp summary's beds and plans.
- **Unchanged in slice 1:** cast, species, items, incidents and world.json.

## 6. Determinism, snapshots, text-state API, localization, mod hooks

### 6.1 Truths and Dice
- **Truths.** Slice 1 adds none. The old camp is still placed from the Truths stream (`CampPlacement`); its roof sections and beds keep their places, and their new fields start empty (an empty `BuiltBy` means the old camp). Later truths about building, such as a culture's way of burying or a dryad's bond tree, belong to the documents that own those facts, appended to `TruthId`.
- **Dice.** Slice 1 adds no draws. The only random roll on the building path is the existing work-accident roll on the Work stream. Everything else follows from state:
  - plans are taken in id order, ties broken by distance, then by (y, x);
  - beds are claimed by whoever lies down first, in `Step` order;
  - the enclosure flood fill always gives the same answer for the same map.
- **Existing seeds change.** People have more jobs to choose from and beds are claimed, so existing seeds tell different stories. The determinism test is not affected; the soak baseline is measured again (section 8).
- **Slice 2.** Storms and fire get a new Dice substream, `DiceSystem.Build = 8`, appended at the end because `DiceRng` requires the values to be contiguous, with new `DiceId`s appended too. That way a burning hut cannot shift the incident stream that times the thieves.

### 6.2 Commands and order
- The new commands go into the replay log. The same seed with the same commands gives the same world, including plans that apply only in part because the world changed while the game was paused.
- Blueprints and structures take their ids from `World.NewId`, like everything else.
- Every list is walked in id order. No dictionary decides an outcome.

### 6.3 Snapshots
- Blueprints, structures, the new fields and `Map.Revision` are plain serialised state.
- `Map.RebuildCaches` already runs on load and reads terrain, so walls need nothing new there.
- Add a cached cover array to `Map.RebuildCaches`. Today `Places.CoveredSpot` scans the whole map calling `Map.Covered`, which loops over every roof section, so more sections would make it slow.
- Snapshots grow by a few hundred small records at most. The target of a 1-second restore at 200 characters is unaffected.
- A rewind restores the camp as built at its fate knot. Plans and buildings made after it are gone with that timeline; the player remembers them. Later, the Chronicle of Lost Futures can cite `BuiltEvent`s from lost timelines ("In a future that never came, Bram was buried by the river.").

### 6.4 Text-state API
All of these are reads in `StateApi` and change nothing.
- **`Buildings(w)`:** what the Build list shows: id, name, description, category, cost text, whether it is available, and the reason if not.
- **`CanPlace(w, buildingId, pieces)`:** a verdict and reason for each tile, the total cost, and the timber in store. It is the same `Building.Check` the commands use.
- **`Blueprints(w)`:** for each plan:
  - id, building, area;
  - status: waiting for timber, waiting for walls, ready, being built, would shut people in, or someone is in the way;
  - timber here out of timber needed, and progress as a percentage;
  - contributors;
  - `pending` for placements queued this tick.
- **`Structure(w, kind, id)`:** building, area, who built it and when, its owner (beds), who rests there (graves), who mended it (roofs), and whether it is marked for taking down.
- **`Location(w, x, y)`** gains lines like those in 3.4.
- **`Character(w, id)`** gains `Bed` ("the bed Oren built", or none).
- **`Faction(w)`** gains beds, beds claimed, open plans and trees standing. The soak uses trees standing to report deforestation.

### 6.5 Localization
- Every new string the player reads is a loc key with arguments; the sim composes no English.
- Event text follows `EventText`'s existing rules:
  - `_you_a` (and the other placeholder variants) where the player fills a placeholder: "You built a bed.", "You claimed the bed you built.", "Oren took down your bed."
  - `_one` where a count is one: "You marked out a bed." against "You marked out 13 walls."
- Building names come in three forms: `building.<id>.name` (Bed), `.a` (a bed) and `.plural` (beds).

### 6.6 Mod hooks
- `buildings.json` merges by id from mod folders like every other content file, so a mod can add a building by reusing a kind.
- Build events reach `IModHook.OnEvent` at the end of the tick, as all events do.
- `RequiredContent` stops a mod from deleting a building the code relies on, and says so at load.

## 7. Fit with the parallel work

### 7.1 Peoples
Dryads, djinn, fae and undead: the peoples doc owns their data. Build leaves the following hooks, and this document approves no species fields.
- **Felling.** `CampJobs.GatherTimber` already emits `event.work.felled_tree` with the feller. If the peoples doc needs the tree's id too, adding it is a one-line change.
  - Building is now what drives felling, because `TimberNeeded` counts plans.
  - A dryad's need for forest, a dryad refusing to fell trees or to build with timber, and a bond tree whose felling is a death are all theirs to define.
  - If they need a "spare these trees" mark, that is a zone, and `TimberNeeded` and `Places.StandingTree` must respect it.
- **Materials.** The `materials` and `tags` in buildings.json say what a building is made of. A people that shuns a material reads the tags: the fae and cold iron might refuse an iron-fitted bed and keep away from an iron-bound door. Iron arrives in slice 3.
- **Labour.**
  - An undead person's need for purpose can be met by open plans. `Blueprints` is the queue of work that needs doing, and `event.work.built` and `plan_finished` are the moments it is met.
  - A djinn who works only under contract needs a way to refuse `build` without one. The hook is a refusal of a work type at the level of a people, emitting a refusal event like `refused_to_shut_in`.
- **Vampires already shape building.** They build outdoors only at night and put cover first, and a roof section is what keeps the promise made to them.
- **The dead.** A dead person stays in `World.Characters` with their `Pos`, so the peoples doc's ghoul can find bodies today. Once the dead can be laid to rest (4.8), a buried body lies at a real grave, so the sim can tell when one is dug up or raided. The peoples doc's old graves may reach the map first (4.8). Where that doc says "the build design's slice 1c" for graves, burial and its "When someone dies" order, it now means 8.4.
- **A suggestion for the peoples doc:** a body left unburied is a natural precondition for the restless dead.

### 7.2 Culture and faith
The culture-faith doc owns meaning. For the dead, this is the contract, with one job kind and one debt kind for the act whatever the rite (4.8). Both sides are built later, in one slice (8.4).
- **Build provides:**
  - the grave and other `grave`-kind buildings: a pyre is one, told apart by its materials and tags. A river rite, if culture-faith adds one, needs no building;
  - the body at rest, in the sim: `RestingIn`, `DiedEvent`, the body's `Pos` on the grave, and the grave's `ForId`;
  - the act: `JobKind.LayToRest`, under the `lay_to_rest` work, with who, whom, where and when all as events;
  - the general thoughts `unburied` and `laid_to_rest`;
  - the one debt, `DebtKind.LaidToRest`.
- **Culture-faith supplies, as a layer on top:**
  - the form due for this dead person (bury, burn, river, cairn or sky) and its deadline (its D1);
  - the check the job makes before it starts: may this person lay that one to rest, this way? Where no custom says otherwise, anyone may, and the form is a grave;
  - who should do it: kin score higher (D2);
  - the neglected rite (D3), and what the faithful think when it is done wrong;
  - the rite's own words for the debt (D4);
  - taboos on materials and places, read from `tags`.
- **What this means for culture-faith's draft:** when its slice 2 is planned, D2 to D4 become this layer. It adds no `JobKind.PerformRite` and no `DebtKind.Rite`, and the grave is sim state, not something only drawn.
- **Shrines (slice 2)** are a `shrine` kind. Its faith is chosen at placement from the faiths held in the camp. What a shrine does, who refuses to build one and what taking it down means all belong to culture-faith.
- **Holy days** that stop work stop building, through the schedule.

### 7.3 Character growth (fixed decision 12)
Build gives character arcs their beats:
- a want for a place of one's own resolves when someone claims a bed in a room they built;
- laying a friend to rest (4.8, later) is a grief experience for the trait-change tables;
- a refusal (to shut people in, or to raise a rival faith's shrine) can be a turning point.

Events carry structure ids and owners, so M1's arc logic can cite them. Suggested wants for wants.json: `home`, `honour_the_dead` and `raise_a_shrine`.

### 7.4 Zones
- **Zones owns painted areas:** stockpiles (where loose stacks go, and where `Stock` counts and takes timber), groves, and any burial ground.
- **Build owns things made from materials by work.**
- This line is settled (lead's call; it was question 4). The Zones doc draws it the same way.
- In slice 1, Build reads timber through `Stock` and never places anything on the stockpile rectangle. When Zones replaces `Map.Stockpile` with zones, Build follows `Stock`.
- A store (slice 2) is a structure that counts as storage for `Stock` and keeps food dry.
- **The burial ground** (Zones' slice 2) marks graves on its own. It waits for laying the dead to rest (8.4) and starts the act defined in 4.8. Where the Zones doc says "Build slice 1c (graves)", it now means 8.4.

### 7.5 Schedule
Building, delivering, taking down and, later, laying the dead to rest are work. They happen in work hours and stop for sleep and holy days as the schedule says. Nothing here defines blocks of time.

## 8. Slices and acceptance

### 8.1 Slice 1a: plans, building and beds (sim and headless)
**Scope:**
- buildings.json and its schema;
- `Blueprint`, `Structure` and the new fields;
- the three commands and `Building.Check`;
- the deliver, build and take-down jobs;
- new walls, doors, roof sections and beds;
- claims, comfort, pride and thanks;
- passing over, and `TimberNeeded` counting plans;
- the `StateApi` reads;
- the stand-in player's plans.

**The stand-in player** (`tools/Return.Runner/Autoplay.cs`).
- `run` and `soak` gain `--plan none|beds|camp|mixed`. The ration policy pick (seed % 3) is unchanged.
- **`beds`.** On day 1 at 08:00, three beds on free shelter floor, keeping the tile just inside the door clear. (Graves for the dead join this plan with 8.4.)
- **`camp`.** Everything in `beds`, plus, on day 2 at 08:00:
  - a 5×4 ring of walls, with its door facing the hearth, at the nearest valid site found by a fixed search outward from the stockpile;
  - a roof over its inside;
  - two beds in it.
- **`mixed`.** Chosen by (seed / 3) % 3, so plans and ration policies vary independently.
- **Valid on every map.** Plans go through `Building.Check`.
- **The runner advances an hour at a time,** instead of one `Advance` for the whole year, so the stand-in player can react. The logs stay a function of seed and flag.
- **Logs change.** The M0 exit-check logs (seeds 1–4) change even with `--plan none`, because beds are now claimed. That is expected and recorded.
- **Nightly CI** moves to `--plan mixed`.
- **The soak summary** gains two tallies: stories whose chain contains a Build event, and trees left, by plan.

**Tests**
- **ContentTests.** buildings.json validates against its schema; every building has its four loc keys; `RequiredContent.Buildings` matches the code.
- **DeterminismTests.** `Scripted` also places a bed and a wall run: the same hash twice, and a different hash for a different plan. `TickCostUnderBudget` (ADR-0005) still passes with a `camp` plan active.
- **SnapshotTests.** Snapshot with a plan half built, timber on the site and a builder carrying timber. Restoring and continuing equals the straight run.
- **BuildTests (new).**
  - A builder builds a bed from a plan: stock goes down by 2, and the built event cites the plan.
  - A deliverer with `build` at 0 carries the timber and a builder finishes; both are recorded.
  - A wall never finishes under a standing person.
  - A ring of walls with no door is refused, and stays blocked until the map changes.
  - A roof waits for walls, then gives cover: `Map.Covered` is true, a vampire seeks cover under it, and rain does not soak anyone beneath it.
  - A plan made while paused is checked again when applied: two plans for one tile in the same tick, and the second is dropped.
  - Calling off a plan drops its delivered timber on the site.
  - Taking down returns half the timber.
  - A builder claims the bed they built.
  - Nobody sleeps in someone else's bed.
- **CausalityTests.** The last timber is cited when the roof promise breaks, and `StoryAnalyzer` finds that chain with Work among its systems.
- **NeedsMoodTests.** A bed restores rest faster; the own-bed and no-bed thoughts appear.
- **YearInvariantTests,** also run on seeds with a `camp` plan:
  - nobody stands on a tile they cannot pass;
  - every Wall or Door tile is old camp or a standing structure;
  - no two plans overlap;
  - no plan holds more timber than it costs.
- **RunnerTests.** The `--plan` choices work, and `mixed` is independent of the ration policy.

**Soak** (100 worlds × 1 year, `--plan mixed`)
- No failures.
- Stories per world-year at least the current 8.5 overall, and worlds with `--plan none` within 10% of it.
- At least 30% of worlds with a plan have a story whose chain contains a Build event.
- Build stories span at least three distinct sets of systems.
- Under `--plan camp`, roof promises are kept about as often and as fast as under `--plan none` (ADR-0014's baseline: a median of about 15 hours). Plans must not starve the old roof.
- Trees left are reported by plan.
- Notable Build stories are added to STORIES.md.

### 8.2 Slice 1b: the view
**Scope:** everything in 3.8 except the grave work: the Build list, the placement tool, ghosts and drafts, drawing new walls, doors, roof sections, beds and plans, the tooltip lines, and refreshing on `Map.Revision`. It can start as soon as the 1a commands and `StateApi` contracts exist, and it touches only `game/`.

**Tests** (`GameViewTests`, Godot-free)
- `BuildTool` lines, Shift outlines, and roofs split into sections of at most 4×4.
- Placing and calling off while paused leaves no trace.
- Every building has a list entry and a hint.
- Every UI key exists.
- Poses: building swings a hammer.

**Screenshots** (`RETURN_SCREENSHOT`)
- The Build list open (`RETURN_OPEN=build`).
- A paused wall ghost with one red tile (`RETURN_PAUSE=1 RETURN_PRESS=build:wall:x,y,6,1`).
- A built hut at night with a claimed bed in its owner's colour (`RETURN_TICKS`, `RETURN_FOCUS`).

**Frame time.** The night scene with 40 plans at ×6 stays within 10% of today's frame time (`RETURN_FPS`).

### 8.3 Slice 1c: envy
**Scope:** envy (4.9). Graves and laying the dead to rest moved to 8.4.

**Tests**
- Envy is cited by the insult, and the grudge that follows counts as a story with Mood among its systems.

**Soak**
- Envy grudges in at least 15% of worlds with a plan.
- At least 50% of worlds with a plan have a story containing a Build event.
- The stranger check again, as in ADR-0018: two fresh readers get one `--plan camp` log each, and at least one retells a Build story unprompted.

**Screenshot:** the inspector open on someone who resents a bed owner, with the anger mark over their head (lead's call; it replaces the grave).

### 8.4 Laying the dead to rest (later, with culture-faith and Zones)
**When:** once aging and multi-year soaks exist, which ADR-0020 needs before M1 can be accepted. It is planned as one slice with culture-faith's death rites (its slice 2, as the layer in 7.2) and the Zones burial ground (its slice 2). Where those docs or the peoples doc say "the build design's slice 1c" for graves and burial, they mean this slice.

**Scope** (Build's part, as 4.8 gives it):
- grave plans and the "For whom?" list (3.6);
- the `lay_to_rest` work, the `LayToRest` job, carrying the body, and the body at rest;
- `unburied`, `laid_to_rest` and the `LaidToRest` debt;
- the laying-to-rest additions in 3.8;
- the stand-in player's graves: under `beds` and `camp`, a grave for each member who dies, six hours later, on the nearest valid tile at least three tiles from the hearth.

**Tests**
- A grave for the dead: the body is carried and laid to rest, and the event cites the death.
- The debt forms, and later care repays it.
- An unburied friend grieves, and stops once the body is laid to rest.
- A dead thief is laid to rest only in a grave marked for them.
- Where no custom says otherwise, anyone may lay the dead to rest, and the form is a grave.

**Soak** (multi-year worlds)
- A burial in at least half of the planned worlds where a member dies. It is judged only on a soak with at least 30 such worlds, so it never rests on one or two (lead's call).

**Screenshot:** a grave beside the camp.

### 8.5 Slice 2
A separate design pass. It covers:
- **Store:** a structure `Stock` counts as storage; food on it does not spoil in the damp.
- A second watch post and a second hearth.
- **Shrine,** together with culture-faith.
- **Storms,** which damage roofs and walls. `Condition` arrives then, repair widens, and `repair_roof` becomes `repair`.
- **Fire:** hearth sparks and lightning, spreading, firefighting work, burns.
- Trees growing back from stumps.
- A bed given as a gift, which can be declined.
- Companions asking for buildings.
- Beauty.

Slice 1 leaves these hooks for it: kinds as an enum in buildings.json, `tags`, `Map.Revision`, the blocked reasons, one event per piece for things people care about, and `DiceSystem.Build` reserved.

### 8.6 Slice 3 and later
- **Stone:** quarrying the rock outcrops, stone walls that do not burn, cairns, and taking down the old stone walls.
- **Salvaging the ruins** for stone and old iron.
- **Iron-bound doors** that bar thieves at night and keep the fae out.
- **A vampire's coffin.**
- **One structure list** for everything, including roofs, beds, the hearth and the watch post, once the kingdom layer needs halls.
- **A master builder** in the court who turns your wishes into plans: delegation at the scale of a kingdom (fixed decision 2).

## 9. Questions for the founder
Most important first, each with my recommendation.

Not asked again here: whether laying the dead to rest waits for multi-year worlds (4.8). It is answered once, as culture-faith's question 5, for this doc, culture-faith and Zones together. *My recommendation is the same: wait,* so the act is built once, when deaths are common enough to tell stories. Two earlier questions are now settled in the body: stockpiles are zones and stores are buildings (7.4), and strangers are laid to rest only in a grave marked for them (4.8).

1. **Additive now, unified later?** Slice 1 extends the old roof and bed records and adds a structure list for walls and doors (graves join it later), instead of moving everything into one list now.
   *Recommendation: yes.* It keeps slice 1 out of the code the peoples branch is changing. Unify once the parallel branches have merged.
2. **Keep `repair_roof` as it is until storms?** Renaming it to `repair` touches roles, traits, `RequiredContent`, the job picker and tests, and only the old roof can break until slice 2.
   *Recommendation: keep it now and rename it in slice 2.*
3. **Plans for the stand-in player, and a new soak baseline.** This adds `--plan` to `run` and `soak`, runs nightly CI with `mixed`, and measures the 8.5 stories per world-year and the logs for seeds 1–4 again.
   *Recommendation: yes.* Without it, no soak ever builds anything.
4. **Who owns a bed?** People claim beds themselves (first to lie down, with the builder's right), or you assign them.
   *Recommendation: claims,* with a gift that can be declined in slice 2. Assigning beds is the closest Build comes to ordering people about, and claims make better stories.
5. **Who is blamed when something someone owns or built is taken down?**
   *Recommendation: you, the one who marked it.* It is your plan, just as the ration policy is your rule.
6. **Should companions ask for buildings?** For example, "Mira asks for a shrine." The request shows in the Chronicle and is marked in the Build list. Granting it warms them; ignoring it for days cools them.
   *Recommendation: yes, in slice 2.* It is delegation running the other way.
7. **Should newcomers be promised a bed?**
   *Recommendation: no.* It would repeat the meal-promise story the soak already tells most often. Beds matter through rest, comfort and envy instead. Revisit if beds do not show up in soak stories.
8. **Should felled trees grow back?**
   *Recommendation: yes, from the stump after one season, in slice 2.* Dryads can speed it up. Until then the forest is finite.
9. **Fire, and arson.**
   *Recommendation:* fire from hearth sparks and storms in slice 2. Arson by someone holding a grudge only once arc detection exists, and only with your go-ahead.
10. **May the old ruins be torn down?** In slice 3, for stone and old iron, at the cost of the vampires' day shelter.
    *Recommendation: yes.* It is a real choice.
