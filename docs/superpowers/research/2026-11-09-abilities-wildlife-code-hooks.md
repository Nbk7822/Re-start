# Return: where an ability system and wildlife would hook into the code

These are research notes for the designers, not a design. They map every place in the code that (A) an ability system with Path-of-Exile-style stats and (B) wildlife would touch. For each place they give the file and line, quote the current numbers, and say what would have to change.

- **Snapshot.** `/Users/nkov/Re-start`, branch `milestone-0` at `e505293` ("Merge the four peoples", 2026-11-09 19:50). The working tree was clean.
- **Worktrees.** Read on 2026-11-09 between 21:55 and 22:10. One of them was still committing while I read it (§13).
- **Line numbers** are on `milestone-0` unless a worktree is named.
- **Nothing in the repo was changed.** I did not run `dotnet` either, because building writes into the repo's `bin/` and `obj/` folders.
- **Sources** are the code, the tests, `CLAUDE.md`, `BIBLE.md`, `DECISIONS.md` (ADR-0004 to ADR-0026), `docs/superpowers/specs/*.md` and the three worktree diffs.

Words used in these notes:
- **Truths** are the stateless random draws for world facts. They survive a rewind.
- **Dice** are the per-system random streams for events. A rewind re-rolls them.
- **"Stream"** means a Dice substream (`DiceSystem`). **"Tag"** means a `DiceId`. **‰** means per mille (parts per thousand), the unit every chance in the code uses.
- **A fate knot** is a saved snapshot that a death can rewind to.
- **ADR-0030** is the player and fate design. It is not on `milestone-0`; it lives on worktree `wf_bafd34a6`.
- **"Gift"** is the code's name for the player's one rolled unique ability (§3.6).

---

## 0. Summary

1. **Three different code paths reduce health.**
   - `CombatSystem.Hurt` handles a thief's blow on a member, work accidents and a dryad's tree wound.
   - `NeedsSystem.Hourly` subtracts starvation, harm from a critical nature need, sunburn and, on the fate worktree, bleeding.
   - Three places subtract health directly: blows on the thief (`CombatSystem.cs:104`), the fae's sting (`Courtesy.cs:44`) and the blood donor's cost (`BloodJobs.cs:124`).

   So no single function would let a damage-type and resistance pipeline catch every kind of harm.
2. **Survival is not rolled today.**
   - Death is deterministic: health reaches 0 or below (`CombatSystem.cs:47`, `NeedsSystem.cs:68`).
   - Only three things are random: hit chance (500‰), the size of the damage, and whether an accident happens.
   - A "20% to survive a bite" rule would be a new Dice draw. The in-flight bleed mechanic deliberately adds no draw (ADR-0030 #11).
3. **Everything is integers.**
   - `ArchitectureTests` (`tests/ArchitectureTests.cs:18-39`) fails any line in `sim/` that contains `float `, `double ` or `decimal `, including comments (§1).
   - Chances are in ‰: `Chance(tag, permille)` is `Range(0,1000) < permille` (`sim/Core/DiceRng.cs:51`). So a value of 1000‰ or more always succeeds, and an "overcap" above 1000 does nothing more unless code reads it separately.
4. **Modifier helpers already exist.**
   - `Traits.Sum` adds values, like PoE's "increased" (`sim/Systems/Traits.cs:8-13`).
   - `Traits.ScalePct` chains percentages as `pct = pct * x / 100`, the shape of PoE's "more" (lines 30-35).
5. **"Know-how returns twice as fast" is already built for the player's gift on the fate worktree.**
   - A stage reached in any earlier timeline needs half the count: `tier <= w.Fate.GiftMastery ? (at + 1) / 2 : at` (`sim/Systems/Gifts/Gifts.cs:57-62` there).
   - `World.Fate` is the one block of the world that a rewind keeps.
   - "Companions lose what they gained after the point returned to" costs nothing to build, because restoring the knot's snapshot does it.
6. **The fate worktree already has bleeding.** It adds `Character.Bleed`, `BleedCause` and `BleedDeath`, and gives `Hurt` a `bleeds` flag that adds 225% of the damage as health lost per hour. Tending stops it.
7. **The map has no habitats.**
   - The map is 48×32.
   - `TerrainKind` has seven values: Grass, Soil, Water, Rock, Floor, Wall and Door.
   - The 24 trees are scattered by uniform rejection sampling; they are not clustered. Only the berry bushes come in clusters.
   - The founder's "zones (forests etc.)" would have to be derived from state. "Zones" is also the name of the in-flight menu for player-painted stockpiles (§13.2).
8. **The only things that move are Characters.**
   - The night thief is a `Character` with `Status = Hostile`, `Named = false` and species "human".
   - Almost every loop filters on `Member`, on `IsActive` (Member or Hostile), or on `Named`/`CastKey`.
   - Making animals Hostile would wake the thief code: they would steal berries, and the director would stop sending thieves. Animals stored as Characters at world creation would break `WorldGenTests.CastShape` (§4.5).
9. **Hunting for food is blocked.**
   - `ContentDb.Validate` rejects any species that eats something other than berries (`sim/Content/ContentDb.cs:119-120`, ADR-0018 #5).
   - Code that touches food uses the "berries" item id directly throughout.
10. **Nothing in the draw-id plan is allocated to abilities or wildlife, and merge order matters.**
    - The design documents claim a range of draw ids for every other track. The ranges still unclaimed are listed in §6.2.
    - Three branches all set `SchemaVersion = 3`.
    - The fate branch takes `DiceSystem.Fate = 10`.

---

## 1. Rules any design here must follow, and the tests that enforce them

| Rule | Where it is written | What enforces it |
|---|---|---|
| Integer state only; no floating point | ADR-0004 ("Simulation state uses integers. No floating point in state or decisions.") | `tests/ArchitectureTests.cs:18-39` scans every line of `sim/**/*.cs` for `System.Random`, `new Random(`, `Random.Shared`, `DateTime.`, `Stopwatch`, `Environment.TickCount`, `.GetHashCode(`, `HashCode.Combine`, `Guid.NewGuid`, `float `, `double `, `decimal ` and `Godot`. It matches raw text, so a comment like "double the damage" or "logs float downriver" fails the build. |
| Every draw names its stream and tag; Truth ids feed the hash and are never renumbered | ADR-0004; `sim/Core/DrawIds.cs:3-6,45,84` | `DiceRng.SeedStreams` throws if `DiceSystem` values are not contiguous from 0 (`sim/Core/DiceRng.cs:96-97`). `tests/RngTests.cs` covers stream independence and reseeding. |
| Ask the founder before changing a schema, adding a dependency, or touching determinism | CLAUDE.md "How we work"; ADR-0007 consequences | Process only |
| Growth is event-driven, never grind-driven; every change cites its cause | Fixed decision 12; BIBLE §5 and §5.4 ("No growth from grind: every trait change references an incident id"); ADR-0020; ADR-0021 #3 | `Growth.RequireCause` throws for a cause of 0 or an id that is not in the log (`sim/Systems/Growth.cs:311-317`) |
| A system must touch at least two others; the founder adds that each ability must too | CLAUDE.md; BIBLE §4.2 table | Review only |
| A story is 3 or more events, across 2 or more systems, with a named character, ending in a persistent effect | CLAUDE.md; ADR-0006 | `StoryAnalyzer` (§5). ADR-0014 shows the danger: 700 of 1,428 "stories" were one instant promise chain, so an effect on a frequent event floods the counts. |
| Content lives in `/content/*.json` with a schema; every player-facing string is a loc key | CLAUDE.md; ADR-0007 | The schema and loc tests in §7 |
| Mod hooks and localisation from day one | CLAUDE.md | `IModHook` (`sim/Modding/IModHook.cs:11-16`); the loc tests in §5 and §7 |

**Tension to settle.** The founder asked for abilities that "increase the level of these abilities by doing them more". Fixed decision 12 says growth is "never grind-driven", and that is a fixed decision. The in-flight gift design reconciles the two by counting discrete logged events, each with a cause: "It grows by events, not grind. Each gift names the events of its own that count, and tiers come at counts" (player design §4.2 rule 6). The founder also asked that "every level gained is a logged story beat with its cause". The designers must decide whether ability levels follow that rule. If they need a new reading of fixed decision 12, it has to be proposed in DECISIONS.md.

---

## 2. Harm, healing and death today: the core of (A)

### 2.1 The health model
- `Character.Health` runs from 0 to 1000 ("Needs and health are 0..1000 (1000 = fully satisfied)", `sim/State/Character.cs:24-25,49`). The character also has `Downed` (50) and `LastHealthEvent` (65), which points at the event to cite as the cause of the next health event.
- **Thresholds:**
  - `NeedsSystem.DownedBelow = 150` and `RecoverAt = 300` (`sim/Systems/NeedsSystem.cs:19-20`).
  - `JobPicker.InjuredBelow = 400` (`sim/Systems/JobPicker.cs:16`).
  - Below 400, people who tire lie down to sleep (69) and are candidates for care (180-195). They do not care for others themselves (182) and do not rescue (200).
- **Natural healing** (`NeedsSystem.cs:66`), and only in an hour with no damage:
  `else if (c.Food >= HungryBelow && c.Rest >= 200 && c.Health < 1000) c.Health = Math.Min(1000, c.Health + (c.Asleep ? 25 : 10));`
  `HungryBelow` is 300.
- **Downed:**
  - The person's job is dropped every tick (`sim/Systems/WorkSystem.cs:21-25`), and they do not travel (`sim/Systems/TravelSystem.cs:34`).
  - Others carry them to a bed (`JobPicker.RescueJob` 198-213, `IncidentJobs.Rescue` 27-48) or tend them (`CareJobs`).
  - They stop being downed at 300 or more (`NeedsSystem.cs:81-84`; `CareJobs.cs:33`).

### 2.2 Every place health goes down

| # | Site | Amount (current content) | Goes through `Hurt`? | Random draw | Death line / system |
|---|---|---|---|---|---|
| 1 | Thief strikes a member: `CombatSystem.Exchange`, `sim/Systems/CombatSystem.cs:107-109` | Hits 500‰ of the time for `Range(60, 141)`; one exchange every 30 ticks | yes | Combat stream, tags `CombatHit` and `CombatDamage` | `event.combat.died_wounds` / Combat |
| 2 | Members strike the thief: same function, line 104 | Each adjacent defender hits 500‰ of the time for 60-140 | **no**: `thief.Health -= dice.Range(...)` | Combat stream | The thief dies at 0 or below (`Lifecycle.Die(thief, Combat, "event.combat.thief_killed")`, 122-126) and flees below the incident's `fleeHealthBelow` of 300 (127-130). The thief starts at 600 (`content/incidents.json:6`). |
| 3 | Work accident: `IncidentSystem.Accident`, `sim/Systems/IncidentSystem.cs:165-175` | `Range(AccidentDamage, 150, 301)` (incidents.json `damageMin` 150, `damageMax` 300) | yes | Incident stream, tag `AccidentDamage`; whether it happens is a separate Work-stream roll (§2.6) | `event.incident.died_accident` / Incident |
| 4 | A dryad's bound tree is felled: `Forest.Wound`, `sim/Systems/Natures/Forest.cs:109-118` | `Range(TreeWound, 250, 451)` (needs.json `woundMin` and `woundMax`) | yes | **Work** stream, tag `TreeWound` (111) | `event.needs.died_with_tree` / Needs. First it emits `event.needs.tree_wound` carrying `EffectKind.BondSevered`. |
| 5 | Hourly upkeep: `NeedsSystem.Hourly`, `NeedsSystem.cs:45-65` | Starving, 10 an hour (`StarvationDamagePerHour = 10`, line 21). A critical nature need: purpose 20 an hour, forest 15 an hour (`content/needs.json` `criticalHealthPerHour`; `NatureNeeds.cs:48-53`). Vampire sunburn: 60 an hour (`species.json` `daylightBurnPerHour`) in daylight on uncovered ground (54-63) | **no**: summed, then `if (damage > 0) c.Health -= damage;` (65) | none | The last harm sets `deathKey` (`event.needs.died_starving`, `died_blood`, the need's `deathKey`, `died_sun`). Death at 68-72 through `Lifecycle.Die(..., SystemTag.Needs, deathKey, c.LastHealthEvent)`. Downed at 73-80 with `event.needs.collapsed`. |
| 6 | A fae hears a broken promise: `Courtesy.WordBroken`, `sim/Systems/Natures/Courtesy.cs:44` | `brokenWordHurt` 50, never pushing them below 150: `fae.Health = Math.Max(Math.Min(fae.Health, NeedsSystem.DownedBelow), fae.Health - def.Param("brokenWordHurt"));` | no | none | never kills |
| 7 | Giving blood: `BloodJobs.Transfer`, `sim/Systems/Jobs/BloodJobs.cs:121-126` | `donor.Health = Math.Max(1, donor.Health - vsp.DonorHealthCost);` (130) and `donor.Rest = Math.Max(0, donor.Rest - vsp.DonorRestCost);` (170). Neither cost has a downing check | no | none | never kills |

On the fate worktree, `NeedsSystem.Hourly` also adds `c.Bleed` to the hourly damage (§3.4).

### 2.3 `CombatSystem.Hurt`: the closest thing to a single choke point
`sim/Systems/CombatSystem.cs:41-60`. The comment reads "Damages a member; downs them below 150 health and kills them at 0." In order, it:
1. Applies the damage, `c.Health -= damage;`, and sets `c.LastHealthEvent = cause;`.
2. Adds the thought `wounded` (mood −10 for 48 hours, `content/thoughts.json:24`).
3. If health is 0 or below, calls `Lifecycle.Die(w, c, system, deathKey, cause)`.
4. Otherwise, the first time health drops below 150, sets `Downed`, clears `Asleep` and the path, and emits `event.needs.collapsed`. That is a **Needs** event, cited by `LastHealthEvent`.

Exchange then emits `event.combat.downed` ("{a} was struck down by the thief.", `content/loc/en.json:190`). That text names the thief, so an animal attack needs its own keys.

Callers: `CombatSystem.Exchange` (109), `IncidentSystem.Accident` (173) and `Forest.Wound` (116).

On the fate worktree the signature becomes `Hurt(..., SystemTag system, bool bleeds = false)`. `Gifts.TurnHarm(...)` runs before `Hurt` in both Exchange and Accident; it is Fortune's favourite redirecting harm meant for the player. So a resistance or tolerance step would come after any redirect, and its order relative to the bleed must be chosen.

### 2.4 How a death is emitted: `Lifecycle.Die`
`sim/Systems/Lifecycle.cs:8-30`. It is the only code that emits `EffectKind.Death` (20-21).
- It sets the status to Dead and health to 0, and clears the path, job, Downed, Asleep, Carrying and any ration reservations.
- `Relations.DropGrudgesHeldBy` (19) drops the grudges the dead person held.
- The event uses the caller's `SystemTag` and key, cites the caller's cause, and carries `Effect { Kind = Death, A = c.Id }`.
- A thief, or anyone else who was not a member, gets no grief (`if (!wasMember) return e;`, 22).
- For a member, every other member gets `saw_death` (mood −18 for 144 hours), anyone whose opinion was 30 or more gets `friend_died`, and the death is offered to the gods with `Culture.Omens.OnSign(w, "death", e)` (28).

Departures are separate.
- `SocialSystem.Leave` (`sim/Systems/SocialSystem.cs:177-188`) emits `event.social.left_camp` with `EffectKind.Departure`.
- A thief leaves by `thief.Status = CharStatus.Left` (`CombatSystem.cs:161`) with no event, and stays in `w.Characters` for good.

Death keys in use:
- `event.combat.died_wounds` and `event.combat.thief_killed`
- `event.incident.died_accident`
- `event.needs.died` (the default), `died_starving`, `died_blood`, `died_sun`
- `event.needs.died_purpose` and `died_forest` (needs.json)
- `event.needs.died_with_tree`

The fate worktree adds `event.growth.died_old_age`, `event.fate.soul_spent` and needs `deathKey`s for `contract` and `courtesy`.

**View hook.** `GoreLog.DeathKindOf` (`game/scripts/Logic/GoreLog.cs:161-166`) maps only three keys to blood:
- `event.combat.died_wounds`, `event.combat.thief_killed` and `event.incident.died_accident` draw as Wounds.
- `event.needs.died_sun` draws as Sun.
- Everything else falls to Wasted, which draws no blood.

New keys for venom or a mauling need adding there, or they draw as Wasted.

### 2.5 Healing by others: `CareJobs.Care`
`sim/Systems/Jobs/CareJobs.cs:9-39`. Current content: `work.json` `care` takes 300 ticks (1 hour), yields 260 and costs 1 berry (`content/work.json:7`).
- The heal (30-33): `bool supplied = def.BerryCost > 0 && Stock.Take(w, "berries", def.BerryCost) == def.BerryCost; int heal = supplied ? def.Yield : def.Yield / 2;`, giving 260 with a berry and 130 without. Health is capped at 1000, and the patient stops being downed at 300 or more.
- Then (34-37) it emits `event.work.cared_for`, citing `patient.LastHealthEvent`. The patient gets the thought `cared_for` (mood +8, opinion +12), and `SettleOrOwe` repays a debt the carer owed or opens a `DebtKind.Care` debt (42-54).
- **Who cares.**
  - `JobPicker.cs:79-85` lets a person look for rescue or care work if they have `CareUrgency` above 0 (Protective 3, Generous 2) or care at priority 1.
  - `CareJob` (180-195) picks the nearest downed member, or the nearest member below 400.
  - Care is also a prioritised work type: it is first in `WorkOrder` (18), and its candidate is at 270-271.
- **Hook for healing abilities.**
  - The heal amount is set at lines 30-32.
  - The event that could count as "doing" is the emit at line 34. It cites what wounded the patient, so "saved a life" can be judged from the patient's state: downed, or bleeding on the fate worktree.
  - On the fate worktree, care also sets `patient.Bleed = 0`, and a patient who is bleeding counts as injured.

### 2.6 The accident roll: `JobRunner.RollAccident`
`sim/Systems/Jobs/JobRunner.cs:94-98`:
```csharp
if (def.AccidentPermille > 0 && w.Rng.Dice[DiceSystem.Work].Chance(DiceId.AccidentRoll, def.AccidentPermille))
    IncidentSystem.Fire(w, "work_accident", cause, c.Id);
```
It is called once, on completion, after each of these work events:
- a forage that picked berries: `CampJobs.cs:29`, 15‰
- a tree felled: `CampJobs.cs:67`, 25‰
- a roof section repaired: `CampJobs.cs:130`, 30‰

No other work rolls. Haul, the watch and care have no accident chance. A host picking a guest's meal deliberately skips the roll (`CareJobs.cs:70`, "no accident roll; culture draws only its own stream").

Two other designs reach this spot:
- The schedule design plans a "worn out" variant that changes only the threshold and keeps the same draw (`schedule-design.md:598`).
- The culture isolation test bans culture code from calling `RollAccident` (§4.6).

**This is the natural spot for "a snake bites someone foraging or felling".** The forage event cites nothing (`Array.Empty<long>()`, `CampJobs.cs:27`). The felling cites `j.CauseEvent`: why the timber was wanted, a promised roof or a bargain's price (60-63).

### 2.7 How incidents are chosen: `Director` and `IncidentSystem`
**The director** (`sim/Systems/Director.cs:13-49`) runs hourly on the Incident stream.
- Pressure rises by 1 an hour to a cap of 100 (`world.json` `director.pressurePerHour`).
- **Rain:** if it is not raining, a 15‰ chance each hour (incidents.json `startPermillePerHour`, tag `RainStart`).
- **Windfall:** at hour 10, a 100‰ chance (`WindfallRoll`).
- **Majors** are spoilage and thieves. They never come within 6 hours of each other, and each costs 30 pressure (`Major`, 44-49).
  - Spoilage rolls at hour 6, only with 4 or more berries stored: `dailyPermille * Pressure / 50`, that is 120‰ × pressure / 50.
  - A thief rolls in each night hour from day 3, only if no Hostile is present (`HostilePresent`, 51-56): `nightPermillePerHour * Pressure / 50`, that is 12‰ × pressure / 50.

**`IncidentSystem.Fire`** (`IncidentSystem.cs:28-37`) dispatches on the incident's id. An unknown id throws `ContentException`.
- `RequiredContent.Incidents` (`sim/Content/RequiredContent.cs:33-41`) lists each incident with the params its code reads.
- `ContentDb.Validate` rejects an incident that has no behaviour (`sim/Content/ContentDb.cs:158-163`).
- ADR-0018 records that the `kind` field in incidents.json (major, minor, good) has no effect; pacing lives in code.
- `work_accident` is "minor" and is fired from work (§2.6), not from the director.

A bite could follow either precedent: fired from work like an accident, or rolled by the director like a thief. Either way it is a new id in incidents.json, which breaks `M0ContentShape`'s count of 6 incidents (§7.4).

---

## 3. Where (A) would hook: stats, tolerances, ailments, survival rolls and growth

### 3.1 Modifier helpers that already exist
- **Trait helpers** in `sim/Systems/Traits.cs`:
  - `Sum` adds (8-13); `Max` takes the largest (15-20); `Any` is true if any trait has it (22-27).
  - `ScalePct` (29-35) multiplies percentages: `pct = pct * pick(...) / 100`, each defaulting to 100. It truncates at every step, so the order of multiplication matters.
  - `WorkBias` (37-44) adds per kind of work.
  - Each trait is a `TraitDef` read from traits.json (`sim/Content/Defs.cs:91-113`).
- **Chained percentages in culture.** `Omens.Read` (`sim/Systems/Culture/Omens.cs:74-76`) computes `odds = devotion × wrath × DeedWeightPct / divisor`, then `odds * (100 + ArcanePerAxisPct * axis) / 100`, then clamps to the ‰ cap.
- **Additive weights floored at zero.** `Growth.Weight` (`sim/Systems/Growth.cs:53-62`).
- C# integer division truncates toward zero, negative numbers included. A flat → increased → more pipeline would need its rounding stated explicitly. Today's helpers only show the shapes.

### 3.2 Where each stat would hook

| Stat | Where | Current formula or source |
|---|---|---|
| Damage taken (resistance or tolerance) | `CombatSystem.Hurt` 42-60 covers blows, accidents and the tree wound | `c.Health -= damage`. The other paths in §2.2 (2, 5, 6, 7) bypass it. |
| Hit chance and damage dealt | `CombatSystem` constants 15-19 (`SightRange = 8`, `BlowEveryTicks = 30`, `HitPermille = 500`, `DamageMin = 60`, `DamageMax = 140`); Exchange 103-109 | The same for everyone; no attacker or defender stats |
| When a defender flees | `CombatSystem.cs:117-118` | `Traits.Max(FleeHealthBelow)`: Anxious flees below 700. Others then hold a grudge of 20 (`Abandon`, 133-142). |
| Accident chance | `JobRunner.RollAccident` 94-98 | `WorkDef.AccidentPermille` |
| Work speed and yield | `JobRunner.Work` 71-75 (`Progress++ >= duration`); forage yield `Math.Min(def.Yield, bush.Berries)` (`CampJobs.cs:23`); timber `def.Yield` (57) | work.json |
| Healing given | `CareJobs.cs:30-32` | 260, or 130 without a berry |
| Cost of giving blood | `BloodJobs.cs:124-125`; the "too weak" refusal at 59: `donor.Health < Math.Max(600, RiskyWorkHealthBelow + 100)` | species.json `donorHealthCost` 130, `donorRestCost` 170 |
| Avoiding risky work | `JobPicker.cs:241,250` | `fragile = c.Health < Traits.Max(RiskyWorkHealthBelow)` skips work marked `risky` (forage, gather_timber, repair_roof; keep_watch on the schedule worktree) |
| How fast needs fall | `NeedsSystem.cs:38-42` | species.json rates per hour |
| Sun, starvation and nature-need harm | `NeedsSystem.cs:47-63`; `NatureNeeds.cs:48-53` | §2.2 #5 |

### 3.3 Survival rolls
None exist. Dice are drawn for:
- whether a blow hits (500‰), how much damage it does (60-140), and how much damage an accident does (150-300);
- whether an accident happens at all (15, 25 or 30‰).

Death then follows deterministically from health. A survival chance would be a new `Chance` draw on some stream with its own tag. Two precedents show how harm can work without a survival roll:
- **The fate bleed** has no draw. Who lives depends on who is near and awake to tend them (ADR-0030 #11).
- **Nature-need harm** is a per-hour drain (`NatureNeeds.Hourly`).

### 3.4 Ailments: what exists for venom, bleeding and burns
- **Venom:** nothing yet.
- **Bleeding**, on the fate worktree `wf_bafd34a6` only:
  - New fields: `Character.Bleed` (health per hour), `BleedCause` and `BleedDeath` (worktree `sim/State/Character.cs`).
  - `CombatSystem.BleedSharePct = 225`, and in `Hurt`: `if (bleeds && c.Health > 0) { c.Bleed += Math.Max(1, damage * BleedSharePct / 100); c.BleedCause = cause; c.BleedDeath = deathKey; }`.
  - Accidents and blows pass `bleeds: true`.
  - `NeedsSystem.Hourly` adds `c.Bleed` to the damage, cites `BleedCause`, and dies with `BleedDeath`.
  - `CareJobs` sets `Bleed = 0`.
  - `JobPicker.CareJob` treats `Bleed > 0` as injured.
  - `WorkSystem.AwaitsTending` makes someone who is bleeding wait for a carer who is on the way.
  - `Fate.Safe` refuses to tie a knot while the player is bleeding. The rule is "a knot inside a death already under way would loop" (worktree `sim/Systems/Fate/Fate.cs:77-85`). Any new ailment that is killing someone must join that check.
  - ADR-0030's reason for the 225% figure: it was tuned against 300 rolled one-year worlds. The player died and came back on their own in 76 of the 300 (25.3%, 0.34 a world-year). Companions died at 1.87 a world-year (1.94 on 300 seeds), against a bar of 2.
- **Burns:**
  - Vampire sunburn is the only burn damage: 60 an hour, `event.needs.sun_burn` every hour, the thought `burned_by_sun`, death line `event.needs.died_sun` (`NeedsSystem.cs:54-63`).
  - The djinn's "fire loosed" burns timber in the stockpile and hurts nobody (`sim/Systems/Natures/Bargains.cs:213-231`).

**Calibration from ADR-0030 (fate worktree).** Companion deaths run at 1.87 a world-year and the player dies on their own about once every three years. A bite at the founder's 20% base survival is lethal 80% of the time, so how often bites happen settles the death rate. Measure it against these numbers.

### 3.5 Events that could count toward an ability, by doing or by surviving

| Event (key) | Emitted at | Cites | Notes |
|---|---|---|---|
| `event.work.foraged` | `CampJobs.cs:27` | nothing | then the accident roll (29) |
| `event.work.felled_tree` / `felled_bound_tree` | `CampJobs.cs:60-63` | why the timber was wanted; the feller's grudge against the dryad | then `Omens.RecordDeed`, `SacredTrees.Felled`, `Forest.Felled`, the accident roll |
| `event.work.roof_repaired` | `CampJobs.cs:125` | `j.CauseEvent` | |
| `event.work.cared_for` | `CareJobs.cs:34` | `patient.LastHealthEvent` | the healing hook |
| `event.work.rescued` | `IncidentJobs.cs:43` | `patient.LastHealthEvent` | |
| `event.work.gave_blood` | `BloodJobs.cs:128` | the request or the offer | |
| `event.combat.fight` | `CombatSystem.cs:98` | the thief being spotted | logged **once per fight**; each blow lands silently (GoreLog infers blows from health drops of 40 or more) |
| `event.combat.downed`, `event.needs.collapsed` | `CombatSystem.cs:111`; `CombatSystem.cs:57` / `NeedsSystem.cs:78` | | |
| `event.combat.thief_killed`, `thief_fled` | 124, 146 | | |
| `event.incident.work_accident` | `IncidentSystem.cs:171` | the work event | "survived" means still alive after `Hurt` (173) |
| `event.needs.sun_burn` | `NeedsSystem.cs:58` | harm still under way | once an hour |
| `event.needs.starving` / `blood_starved` | `NeedsSystem.cs:138` | `LastFoodEvent` | |
| The watch ending, haul | none | | the watch emits nothing when it ends (`CampJobs.KeepWatch` 136-148; moving to `WatchJobs` in the uncommitted schedule work) |

### 3.6 Growth as the home for ability levels
- **One home for growth state.** `CharacterGrowth` (`sim/State/Growth.cs:105-115`) holds `TraitOrigins`, `Want` and `Arc`. ADR-0021 #1: "later growth fields (evolution tier, memories) need no more", so `Character.cs` changes by one line at most.
- **One door per change.**
  - `Growth.ChangeTrait` (`sim/Systems/Growth.cs:163-210`) refuses a change with no cause and runs every check before changing anything. It records a mutated origin {cause, tick} and logs a `SystemTag.Growth` beat (`event.growth.trait_gained`, `trait_lost` or `trait_changed`) that cites the cause.
  - `Growth.AdvanceArc` (251-282) works the same way. Its beat cites both the cause and the arc's previous beat, so an arc reads as one chain.
- **Beats do not end stories.** ADR-0021 #9: growth beats carry no persistent effect. `StoryLog.Analyze` still marks every Growth event as notable (`tools/Return.Runner/StoryLog.cs:56`), so beats appear under "What happened".
- **Precedent for growth that ends a story.** On the fate worktree, gift growth emits `event.gift.grew` carrying `EffectKind.GiftGrew` (worktree `Gifts.cs:66-82`). Whether ability levels end stories is an open question for the designers (§14).
- **Reserved draws.** `DiceSystem.Growth = 8` exists but nothing draws from it (`sim/Core/DrawIds.cs:95-96`). DiceIds 100-119 are held for mutation and evolution rolls (ADR-0021 #10; BIBLE §5.4: "All mutation and evolution rolls use the Dice stream with purpose tags").
- **The bible already mentions abilities.** BIBLE §5.1.3 says evolution "Changes subspecies, role, abilities and sprite parts". §5.2 plans `trait_mutations.json` and `evolution_paths.json`, neither built.
- **Name clash.** The player's rolled "unique ability" is called a **gift** in the code and content: `gifts.json`, `SystemTag.Gift`, `EffectKind.GiftGrew`, `StateApi.Gifts`, the inspector row `life.gift` (worktree `wf_bafd34a6`). Earned abilities need a distinct name and must not reuse gift identifiers.

### 3.7 How Return by Death interacts with abilities
- **On `milestone-0` nothing rewinds.** There is only `DiceRng.Reseed` (`sim/Core/DiceRng.cs:84-88`), and a snapshot is the whole `World`.
- **On the fate worktree** (ADR-0030, and player design §5.3-5.4):
  - A rewind restores the knot's snapshot, then overwrites its copy of `World.Fate` (`FateBook`, worktree `sim/State/Fate.cs:22-87`) with the dying world's.
  - The "you keep" column of the design's table: "your people, gift and soul span; that your gift woke, and its best tier".
  - The "goes back" column: "your body; your gift's growth and contents", and "companions' growth".
  - `LearnedCustoms` is copied from the dying player to the restored one, which is the precedent for carrying a per-player list across.
  - The founder's rule maps directly. "The body forgets what it endured" means body-state abilities live in the world and go back. "Know-how returns twice as fast" is the `GiftMastery` and `Needed` half-count rule. "Companions lose what they gained" follows from the restore.

---

## 4. Where (B), wildlife, would hook

### 4.1 Map, terrain, trees and bushes
- **`TerrainKind`** (`sim/State/Map.cs:6-15`) is a byte: Grass 0, Soil 1, Water 2, Rock 3, Floor 4, Wall 5, Door 6. It is stored in `byte[] Terrain` (58), so a snapshot holds raw byte values. Appending a value keeps old snapshots readable. Each value needs a loc key `terrain.<snake>` (§5.4).
- **`Tree`** {Id, Pos, Felled} (17-22) and **`Bush`** {Id, Pos, Berries, RegrowTick} (24-30) are lists on `Map`, ordered by id. `TreeAt` and `BushAt` are linear scans (89-101).
- **Passability** (`RebuildCaches`, 103-117). Blocked tiles: Water, Rock, Wall, every standing tree and the hearth. Bushes can be walked through at double cost. The caches are private fields, so they are not serialised; `IJsonOnDeserialized` rebuilds them after a load (51, 119). Felling a tree calls `w.Map.RebuildCaches()` (`CampJobs.cs:56`).
- **Bushes regrow** 2 berries a day up to 6, outside winter (`Places.cs:9-14`; `WorkSystem.RegrowBushes` 81-89). Winter's frost strips them (`NeedsSystem.SeasonTurns` 154-164), then each puts out one berry every 2 days.

### 4.2 How `WorldGen` lays out the map
- **Size** comes from `content/world.json:11-12`: `mapWidth` 48, `mapHeight` 32. `WorldGenTests.CampIsUsable` asserts 48×32 (`tests/WorldGenTests.cs:125-126`).
- **Order in `Create`** (`sim/WorldGen.cs:31-43`):
  1. `CreateCast`: ids 1-14, in cast.json order.
  2. `BuildMap`: beds, trees and bushes, each taking an id from `NewId`.
  3. `NatureNeeds.Init` for **every** character.
  4. `PlaceStartItems`, then `PositionStarters`.
  5. `ScheduleArrivals`, on the World Dice stream.
  6. `FormStartersWants`, then `CultureGen.Seed`.
- **Soil.** Value noise on a lattice of 6-tile cells, `TruthId.Terrain`. A value above 640 makes the tile Soil (128-142).
- **River** along the east edge (144-150). The bank starts at `width-6-{0,1}` and wanders ±1 a row, clamped to `[width-8, width-4]` (x from 40 to 44). Water runs from the bank to the east edge, 4 to 8 tiles wide.
- **Rock**: 3 outcrops in the north-west (152-164). Centres are at x 1-8 and y 1-6, with radius 1-3. A tile is rock if (dx² + dy²) ≤ r² + 1. Placed by `TruthId.RockPlacement`.
- **The camp** (166-197):
  - Its corner is at `sx = 16 + [0,6)` and `sy = 9 + [0,4)`.
  - A clearing `Rect(sx-7, sy-2, 22, 19)` has its rock and water turned to grass.
  - The shelter is 8×5 with walls, a floor, a door and 3 roof sections.
  - 4 beds; the covered ruins at `Rect(sx+9, sy+1, 3, 3)`; the hearth at `(sx+4, sy+7)`.
  - The stockpile is `Rect(sx-5, sy+6, 4, 3)`, renamed `StartStockpile` on the zones worktree.
  - The watch post is at `(sx+4, min(h-3, sy+15))`.
- **Placement tests** (199-203):
  - `Reserved(p)` covers the camp's rectangle (x from sx-2 to sx+13, y from sy-1 to sy+9), the shelter, the ruins, the stockpile, anything within 2 of the hearth, and the watch post.
  - `Open(p)` is Grass or Soil, not reserved, and with no tree or bush on it.
- **Trees.** `TreeTarget = 24` (line 20), placed by **uniform rejection sampling**: x in 1-46, y in 1-30, up to 800 attempts, `TruthId.TreePlacement` keyed by attempt number (205-210). They are **not clustered**. The tests assert at least 20 trees.
- **Bushes.** Clusters of {4, 4, 4, 2} (27). Each cluster centre is rejection-sampled at x 3-35, y 3-28, and its members land within ±2. `TruthId.BushPlacement` is keyed by `c*64+attempt`, and each bush starts with 4 berries (212-229). The tests assert at least 6 bushes. The fourth cluster is "keyed after the first three, so none of their bushes moves" (22-26). That is the convention for adding to Truth-placed content.
- **No biome or habitat exists.** The only notion of density is `Forest.Grove` (`sim/Systems/Natures/Forest.cs:136-151`: the standing tree with the most trees within `treeRange` 3) and `Forest.Restore`'s count of trees in range (40-48).
- **Habitat signals that can be derived from state:** next to water (the riverbank), next to rock, Soil versus Grass, tree density, distance from the hearth, and the west and south edges (where arrivals come in).
- **Fixed decision 4 puts the map among the Truths.** Animal populations placed at world creation would follow the tree and bush precedent (Truths, surviving a rewind). Animal movements and encounters would follow the arrivals precedent (Dice, re-rolled).

### 4.3 Movement and pathfinding
- **`TravelSystem`** (`sim/Systems/TravelSystem.cs`).
  - `Start` (12-28) asks `Pathfinder` for a path.
  - `Step` (30-49) moves only `IsActive` characters (Member or Hostile) that are not downed or asleep, one tile at a time. If the next tile has become blocked, the path is dropped.
  - `TicksFor` (51-57) uses the species' `ticksPerTile` (6 for every species today), ×1.4 on a diagonal and ×2 into a bush.
- **`Pathfinder.Find`** (`sim/Systems/Pathfinder.cs:16-59`) is 8-way A*. A straight step costs 10, a diagonal 14, and entering a bush doubles the cost. It never cuts a corner (45). Ties break deterministically on f, then h, then y, then x (67). Each call allocates three arrays of W×H. It returns null when the target is impassable.
- **What moves.** Only characters. The **night thief** is a `Character` (`IncidentSystem.SendThief`, 177-191):
  - `Id = w.NewId(), NameKey = "npc.thief.name", SpeciesId = "human", Named = false, Status = Hostile`, health 600 from content.
  - It enters at `EdgeTile` with a `Raid` job aimed at the oldest food.
  - It is never removed from `w.Characters`; it ends as `Left` or `Dead`.
  - `RequiredContent.Species = { "human" }` says "Night thieves are human" (`RequiredContent.cs:12-13`).

### 4.4 `Places` and `IncidentSystem.EdgeTile`
- **`EdgeTile(w, draw)`** (`IncidentSystem.cs:215-229`).
  - Picks the **west** edge (x = 0, y from 1 to H-2) or the **south** edge (y = H-1, x from 1 to W-11), with up to 20 attempts.
  - It needs a path from the tile to beside the hearth. The comment explains why: "a dryad withered in a corner on her fourth day, seed 3". Otherwise it falls back to beside the hearth.
  - It always draws from **the Incident stream**, with the caller's tag (`ArrivalEdge`, or `ThiefEdge` even when combat calls it).
- **`Places.Near`** (`sim/Systems/Places.cs:144-154`) draws from **the Work stream** (`WanderTarget`), and `WanderJob` also draws `WanderWait` (`JobPicker.cs:336`). A wildlife system that reused these helpers would shift the Work or Incident sequences. The culture isolation test lists these helpers as cross-stream (§4.6).
- **Loops that count `IsActive` characters:** `Places.Targeted` (157-162), `TakenBed` (133-142), `TimberNeeded` (195-205), `StateApi.Location`'s list of people on a tile (`sim/Api/StateApi.cs:303-304`) and `Hospitality` (`sim/Systems/Culture/Hospitality.cs:44-45`).

### 4.5 The entity model: could animals be Characters?
- **What `World` holds** (`sim/State/World.cs:13-44`): `Characters`, `Items`, `Reservations`, `Grudges`, `Debts`, `Contracts`, `Arrivals`, `Log`, the commands, `Culture`, `Map`, `Rng`, `Director` and `Weather`.
- **One id counter, `NewId()` (52),** serves characters, trees, bushes, beds, item stacks, debts, grudges and contracts.
  - Anything that takes an id at creation shifts every id after it.
  - The in-flight branches therefore hand out creation-time ids last. Zones: `Zones.CreateStart(w); // last, so no id handed out before it moves (ADR-0028)`. Build: roof section ids are taken "after everything else is made, so no other id moved (ADR-0027)".
- **`World.Get` and `Find`** (54-66) are linear scans over `Characters`, and hot paths call them constantly (job targets, event text, relations).
- **Which loops would see an animal stored as a Character.** I checked every `foreach` over `w.Characters` in `sim/`:
  - **Filter on `Status == Member`:** NeedsSystem 33-35, MoodSystem 38-40, WorkSystem 18-20, Growth.Hourly 144-145, SocialSystem departures 149-151, JobPicker 185, 203 and 318, BloodJobs 18 and 38, Bargains, Forest, Courtesy, Omens 58-60, and Rations, which uses `Members()`.
  - **Filter on `IsActive`:** TravelSystem 32-34, the `Places` loops in §4.4, StateApi.Location 303 and Hospitality 44.
  - **Filter on `Named` / `CastKey`:** `CultureGen` 93-94 (`person.CastKey.Length > 0 && person.Named`) and the culture reads in `sim/Api/StateApi.Culture.cs:88,109`.
  - **No filter:** `WorldGen.cs:36` calls `NatureNeeds.Init` for every character at creation, and the opinion decay at `SocialSystem.cs:139-142` touches everyone (harmless).
- **Traps if animals are `Hostile`:**
  - `Director.HostilePresent` (51-56) stops night thieves while any Hostile character exists.
  - `CombatSystem.Step` (21-39) treats every Hostile with a job as a thief. Guards spot it (`Spot` needs a watch job or the `FightsThieves` trait, within 8 tiles), exchanges happen, and `if (t.Pos == t.Job.Target) Steal(w, t)` **steals berries**. Fleeing uses `EdgeTile`.
- **Traps if animals are `Named = false`.**
  - They count in no story: the story analyzer needs a named actor (`StoryLog.cs:50`, `id => w.Find(id)?.Named == true`).
  - They count in no soak losses (`Soak.cs:308`).
  - But each `Lifecycle.Die` carries `EffectKind.Death`. Every animal killed is therefore a possible story ending when a named hunter is in the chain.
- **Tests that count characters:**
  - **`WorldGenTests.CastShape` would break** if animals were Characters created with the world. It asserts `Db.Cast.Count == w.Characters.Count` (`tests/WorldGenTests.cs:48`).
  - **`RestoreUnderOneSecondAt200Characters` would not break from animals created with the world.** `AddSyntheticCharacters` fills up to the total with `while (w.Characters.Count < total)` and copies `w.Characters[i % 10]`, which are still the first ten cast members (`WorldGen.cs:59-78`). It would just add fewer synthetic people. It **would break if Characters were added during play** in the two days it advances, because it asserts exactly 200 after the restore (`tests/DeterminismTests.cs:136,146`). Thieves come only from day 3, so today nothing is added in that window.
  - `PerformanceTests.TickCostUnderBudget` divides the time by `w.Characters.Count` (161-166; at most 2,000 ns per character per tick, ADR-0005). Animals stored as characters would enlarge that denominator; animals stored anywhere else add cost with no denominator.
- **`species.json` is the list of peoples.**
  - `GameViewTests.KinComesFromTheSpeciesNature` pins the exact set of species ids (`tests/GameViewTests.cs:750-757`).
  - `ContentDb` requires anyone who eats an item to eat berries (117-120).
  - Cultures weigh and regard peoples by species id (`ContentDb.Culture.cs:104,116`).
  - The fate worktree rolls the player's people from `rebirth.json`.
  - Animals placed in `species.json` would collide with all of these.
- **A new `CharStatus`** needs the loc key `status.<snake>` (§5.4). `IsActive` (`Character.cs:81-82`) decides who travels and who the view draws.
- **The view.**
  - `MapView` draws every character that is `IsActive` or Dead as a person (`game/scripts/MapView.cs:284`; `Figures.Person` at 697; the hostile look at 762).
  - A wolf figure already exists and is unused: `Figures.Wolf` (`game/scripts/Art/Figures.cs:1028-1029`). ADR-0019 says "The wolf and the Barrow King are drawn but unused until the sim has them".
  - A click selects only character ids (`MapView.CharacterUnderMouse`, 217-223). The inspector reads `StateApi.Character` → `w.Get(id)`, which throws for an id that is not a character.

### 4.6 Hunting, predators, snakes, horses and omens
- **Hunting as work.** Adding a kind of work touches:
  - `work.json` and `RequiredContent.Work`;
  - `JobPicker.WorkOrder` (18) and the `Candidate` switch (266-312);
  - the `JobRunner.Tick` dispatch (11-36) and `JobKind` (`sim/State/Job.cs:5-38`);
  - role priorities (`content/roles.json`);
  - `NeedsSystem.JobRestCost`, which finds the work by `Names.Snake(JobKind)` (169-174), so the work id must match the job kind's snake-case name.
- **Hunting for food.**
  - `ContentDb.cs:119-120`: `'{s.Id}' eats '{s.FoodSource}', but M0 only grows, rations, steals and spoils berries (ADR-0018)`.
  - The test `FoodOtherThanBerriesIsRejectedUntilFoodIsGeneral` (`tests/ContentTests.cs:128-139`) enforces it.
  - "berries" is written directly into combat, care, incidents, the director, festivals, honour and jobs (more than 30 places).
  - ADR-0026 adds "no food sold to the camp before the food milestone".
- **Predators.** `CombatSystem` is built for thieves:
  - `Steal`, and the night thief's params (`fleeHealthBelow`, `stealMax`);
  - text that names the thief: "{a} fought the thief.", "{a} was struck down by the thief." (`en.json:189-191`);
  - `Spot` only reacts to someone keeping watch or holding the Protective trait, within 8 tiles, so a forager attacked far from camp would get no defenders.
- **Snakes biting foragers and fellers.** They would sit beside the `RollAccident` calls (`CampJobs.cs:29,67`). Dryads never fell trees (species `refusesWork`). A host picking a guest's meal deliberately skips the roll.
- **Horses to tame.**
  - There is no mount, carry or riding system. `Character.Carrying` holds a single `Carry`, and travel speed is per species.
  - The relationship graph is keyed by character ids: each `Character` has an `Opinions` list, and `Grudge` and `Debt` hold character ids (`sim/State/Social.cs`). An animal that is not a Character cannot hold an opinion or a debt without new structures.
- **Animals as omens.**
  - `Omens.OnSign(w, sign, signEvent)` (`Omens.cs:42-69`) reads `RequiredContent.Signs = { rain, windfall, food_spoils, work_accident, death }` (`RequiredContent.cs:74`). These map through `domains.json` "signs" entries and need loc keys `sign.<id>`.
  - The deeds the gods care about are `DeedKind` (`sim/State/Culture.cs:71-84`) together with `RequiredContent.Deeds` (67-71), the domains' `pleasedBy` and `angeredBy`, and loc keys `deed.<id>`.
  - Omens draw only from the Culture stream (`Omens.cs:77`).
  - **`CultureEngineTests.CultureCodeDrawsOnlyFromTheCultureStream`** (`tests/CultureEngineTests.cs:23-49`) enforces two things. Code in `sim/Systems/Culture/*.cs` and `CultureJobs.cs` may not name any other `DiceSystem`, nor call `Places.Near(`, `JobRunner.RollAccident(` or `IncidentSystem.EdgeTile(`. And no other file may use `DiceSystem.Culture`, `DiceId.FauxPas`, `GuestOffer` or `OmenRead`.
- **What other designs already say about animals:**
  - Zones: "Grazing: Animals graze here; Wolves at the edge of the home area; After animals exist" (`zones-design.md:127,199`).
  - Peoples: the werewolf "as a wolf it hunts (later, with animals)" (460); the holtward, about hunting, in batch 6 "animals and hunting" (156, 1108); the fetlow and the centaur are horse-like peoples.
  - World: the world map has "Olive plains, near-black forest, slate water" (`world-design.md:100`), with world-scale terrain as `TruthId` 120 `WorldTerrain`. That is separate from the camp map.

---

## 5. The event log, causes, effects and story counting

### 5.1 Records
- **`SimEvent`** {Id, Tick, System, Key, Args, Actors, Causes, Effects} (`sim/Events/SimEvent.cs:69-79`).
- **`EventLog.Emit`** (`sim/Events/EventLog.cs:14-38`). A cause must be an earlier id or it throws `InvalidOperationException`; 0 is dropped and duplicates are removed. Ids run 1, 2, 3… with no gaps, and `Get(id)` indexes `All[id-1]` (12). The whole log is part of every snapshot.
- **`EventArg`** prefixes are `c:` character id, `i:` item id, `k:` loc key and `n:` number (SimEvent.cs:57-66). `EventText.Resolve` turns `c:<id>` into "someone" when `w.Find` fails (`sim/Api/EventText.cs:71-95`), so an animal that is not a Character cannot be named through `c:`. The zones worktree adds `h:` (hour); the fate worktree adds `e:<future>:<line>`.
- **Second person.** Text switches to `<key>_you_<arg>` when the player fills that placeholder (`EventText.cs:49-60`). `StoryAnalyzerTests.SecondPersonVariantsMatchTheirBaseKeys` (`tests/StoryAnalyzerTests.cs:240-259`) checks the variants.

### 5.2 Persistent effects: `EffectKind` (SimEvent.cs:23-41)
- `GrudgeFormed`, `GrudgeEnded`
- `DebtFormed`, `DebtRepaid`, `DebtBroken`
- `Death`, `Departure`, `Recruitment`
- `BondSevered`
- `ContractFormed`, `ContractKept`, `ContractBroken`
- The fate worktree appends `GiftGrew`.

Effects are stored by name in snapshots. Only an event with an effect can end a story.

### 5.3 `StoryAnalyzer` (`sim/Stories/StoryAnalyzer.cs`)
- `MinEvents = 3` and `MinSystems = 2` (21-22).
- **System mask.** Each event contributes the bit `1 << (int)e.System` to an **int** mask (37), so at most 31 system tags fit. There are 10 today; fate adds 2 and loot-economy plans 1 more.
- It keeps the longest chain per set of systems and builds a story only at an event that has effects (46).
- **Choosing the story.** At most one story per ending event: the longest chain first, then the one touching more systems, then the lowest mask (48-64).
- **No nesting.** `Maximal` drops a story whose ending lies inside a longer story's chain (69-78, ADR-0017 #1).
- **A named character is required** (`Build`, 116-122) through the callback `id => w.Find(id)?.Named == true` (`tools/Return.Runner/StoryLog.cs:50`).

### 5.4 Adding a `SystemTag`, enum value or loc key
- **`SystemTag`** (`SimEvent.cs:6-20`). Append the value; it is stored by name, so appending is safe.
  - **You must add `log.system.<snake>`** to `content/loc/en.log.json`. `RunnerTests.LocCoversEveryKeyTheRunnerUses` (`tests/RunnerTests.cs:359-378`) adds every `SystemTag` at line 370, and it also fails on any **unused** key in `en.log.json`.
  - Decide whether the tag is always notable in the log. `StoryLog.Analyze` line 56 lists Incident, Combat, Growth and Culture today.
  - The soak's `SystemSetKey` uses enum names (`Soak.cs:118-129`).
- **`StoryAnalyzerTests.LocCoversKeysUsedInCode`** (`tests/StoryAnalyzerTests.cs:261-277`):
  - Every string literal in `sim/` that starts with `event.`, `job.`, `need.`, `status.`, `terrain.`, `debt.`, `debt_status.`, `place.`, `time.`, `name.`, `policy.`, `faction.` or `log.` must exist in the loc files.
  - Every value of `JobKind`, `CharStatus`, `TerrainKind`, `DebtKind` and `DebtStatus` must have its loc key.
  - New prefixes, such as `ability.` or `animal.`, are not covered by this test unless it is extended. `ContentDb.RequireLoc` (280-284) only checks the keys named in content files.

---

## 6. `sim/Core/DrawIds.cs`: every id in use, and the contiguity rule

### 6.1 What is on `milestone-0` (`sim/Core/DrawIds.cs`)
- **`TruthId`** (7-43). These values feed the Truths hash `Combine(Combine(Combine(WorldSeed, id), entity), index)` (`sim/Core/TruthRng.cs:13-14`), so they must never be renumbered.
  - 1 `Terrain`, 2 `RiverPath`, 3 `RockPlacement`, 4 `TreePlacement`, 5 `BushPlacement`, 6 `CampPlacement`, 7 `SecondaryTrait`, 8 `PairChemistry`
  - 20 `InitialWant`, 21 `WantTarget`
  - 30 `BoundTree`, 31 `PurposeWork`, 32 `BargainService`, 33 `BargainPrice`
  - 40 `CultureNature`, 41 `FaithNature`, 42 `GodNature`, 43 `PersonBelonging`, 44 `SacredTree`, 45 `FestivalDate`
- **`DiceId`** (46-82). These are only tags: `Note()` records them for diffing, and they never reach the generator (`DiceRng.cs:60-65`).
  - 1 `ArrivalDay`, 2 `ArrivalHour`, 3 `SocialInitiate`, 4 `SocialInsult`, 5 `SocialDeepTalk`, 6 `WanderTarget`, 7 `WanderWait`, 8 `AccidentRoll`
  - 9 `ArrivalEdge`, 10 `ArrivalFood`, 11 `RainStart`, 12 `RainLength`, 13 `WindfallRoll`, 14 `WindfallKind`, 15 `WindfallRow`, 16 `SpoilRoll`
  - 17 `SpoilLoss`, 18 `ThiefRoll`, 19 `ThiefEdge`, 20 `AccidentDamage`, 21 `CombatHit`, 22 `CombatDamage`, 23 `ArrivalOrder`
  - 120 `PeopleArrivalDay`, 121 `PeopleArrivalHour`, 122 `TreeWound`
  - 140 `FauxPas`, 141 `GuestOffer`, 142 `OmenRead`
- **`DiceSystem`** (85-98):
  - World 0, Needs 1, Mood 2, Social 3, Work 4, Travel 5, Incident 6, Combat 7
  - Growth 8: reserved, nothing draws from it
  - Culture 9
- **The contiguity rule.** `DiceRng.SeedStreams` (`sim/Core/DiceRng.cs:90-102`) throws `"DiceSystem values must be contiguous from 0"`. Each stream is seeded with `Mix64.Combine(DiceSeed, (ulong)system)` (98), so **a stream's number is part of its seed**: renumbering a `DiceSystem` changes every draw on that stream.
- **Which stream each existing draw uses:**
  - Combat: combat draws.
  - Incident: incident draws, `EdgeTile` and the director.
  - Work: the accident roll, wandering (`WanderTarget`, `WanderWait`) and the dryad's tree wound (`TreeWound`).
  - World: arrivals.
  - Social: conversation.
  - Culture: culture draws.
  - Needs, Mood and Travel draw nothing today.

### 6.2 Ranges already claimed

| Track | TruthIds | DiceIds | DiceSystem | Status and source |
|---|---|---|---|---|
| M0 | 1-8 | 1-23 | 0-7 | merged |
| Growth (ADR-0021) | 20-29 | 100-119 | Growth = 8 | merged; DiceIds reserved for M1 |
| Peoples batch 1 (ADR-0022) | 30-39 | 120-139 | none | merged (30-33; 120-122) |
| Culture-faith (ADR-0023) | 40-49 | 140-159 | Culture = 9 | merged (40-45; 140-142) |
| Peoples batch 2 | 50-59 | 160-179 | none | design (`peoples-design.md:964-980`) |
| Build, zones | 60-69 (free) | 180-199 | Build: next value, slice 2 | zones worktree: `ZoneDefy = 180`; build worktree: none (`world-design.md:365-378`; `build-design.md:570`) |
| Schedule (ADR-0029) | 70-79 | 200-219 | none; uses the Work stream | zones/schedule worktree: `DayChangeDefy = 200`, `WatchDefy = 201`; TruthId 70 later for a body clock |
| Player and fate (ADR-0030) | 80-89 | 220-239 | **Fate = 10** (the design's Gift stream folded into it) | fate worktree: Truth 80-84 (`PlayerPeople`, `PlayerGift`, `AdultAge`, `Lifespan`, `SoulSpan`), 85-89 reserved; Dice 220 `FateKnotTail`, 221 `FateKnotPick`, 222 `LuckVictim`, 223-239 reserved |
| World | 120-149 | 300-339 | Politics and War: next values | design (`world-design.md:363-397`) |
| Loot-economy | 150-159 | 350-369 | Economy: next value | design (`loot-economy-design.md:370,387`) |

- **Unclaimed now** (my reading; not an allocation): TruthIds 9-19, 90-119 and 160 up; DiceIds 24-99, 240-299, 340-349 and 370 up.
- **What the designs say about these blocks:** the world design's table "is current. Nothing has merged, so the founder's allocation wins if it differs", and the loot design marks its early draft ranges as "withdrawn".
- **A new stream** takes the next contiguous value at merge: 11 if the fate branch merges first.
- **A shared stream** is possible too. The schedule's compliance rolls use the Work stream, and the schedule design accepts that "each shifts the later draws on that stream".

---

## 7. Content loading, schemas, loc files and the content tests

### 7.1 `ContentDb` (`sim/Content/ContentDb.cs`)
- **JSON options** (12-18): camelCase, comments and trailing commas allowed, and **`UnmappedMemberHandling.Disallow`** (17). So any new JSON field needs a matching `Def` property, or loading fails.
- **`Load`** (57-90) reads a fixed list of files: world, species, traits, items, work, roles, thoughts, incidents, cast, wants, needs, the loc files, and culture through `LoadCulture` in the partial `ContentDb.Culture.cs`.
- **Partial-class pattern.** The worktrees add `ContentDb.Fate.cs`, `ContentDb.Build.cs`, `ContentDb.Zones.cs` and `ContentDb.Schedule.cs`, each a couple of lines in `ContentDb.cs` plus its own file.
- **`ReadMerged`** (309-335):
  - The file **must exist in `/content`** ("not found", 316-319).
  - Mod folders override entries by id.
  - A duplicate id within one file is an error.
- **`ReadLoc`** (337-356) reads `loc/en.json`, then every `loc/en.*.json` in ordinal name order, with later keys winning. A new file such as `en.abilities.json` is picked up automatically. Its keys must match `^[a-z][a-z0-9_]*(\.[a-z0-9_]+)+$` (`content/schemas/loc.schema.json`).
- **`Validate`** (105-186):
  - `RequireLoc` checks the loc keys each def names.
  - It checks the `RequiredContent` lists.
  - Like an incident, a need whose id has no behaviour is rejected (`ValidateNeeds`, 246-278). ADR-0022 #1 states the pattern: "The need's id names its behaviour, as an incident's does".

### 7.2 `RequiredContent` (`sim/Content/RequiredContent.cs:8-111`)
It lists items (10), species (13), work (15-18), thoughts (20-30), incidents with their params (33-41), culture thoughts (44-48), custom kinds (55-64), deeds (67-71), signs (74) and needs with their params (92-110). `ContentDb` checks every one at load.

### 7.3 Adding a new content file: the checklist the code implies
1. Add `content/<x>.json` and `content/schemas/<x>.schema.json` (JSON Schema draft 2020-12, evaluated with JsonSchema.Net).
   - `ContentTests.AllContentValidatesAgainstSchemas` (`tests/ContentTests.cs:12-37`) needs a one-to-one match: every schema except loc's has its file, and every file has its schema.
   - The conventions: id `^[a-z][a-z0-9_]*$`, loc keys as above, `params` as maps of integers, and levels from 0 to 1000.
2. Add a `Def` class, in `Defs.cs` or a new `<X>Defs.cs`, a `ReadMerged` call, an index and validation.
3. Add a `loc/en.<x>.json`. Each worktree puts its text in its own file: `en.fate.json`, `en.build.json`, `en.zones.json`, `en.schedule.json`.
4. Add the ids the code names to `RequiredContent`.
5. Update the tests:
   - `EveryLocKeyExists` (`ContentTests.cs:51-70`) needs the new def list added to its `defs`.
   - `RequiredContentCoversIdsNamedInCode` (155-189) needs a new kind entry.
   - **Accessor names are scanned by pattern.** `Param("x")` is checked against the incident and need params (the pattern at 161, the check at 183-184). `Arg("x")` is checked against custom params (172, 185-186). On the fate worktree, `(?<!EventArg)\.Num\("x"` is checked against gift params. So a new def's param accessor must use another name, or the test must learn it.
  - **String literals are scanned too.** Any literal in `sim/` matching `"[a-z][a-z_]*"` that equals an item, species, work, thought, incident or need id must be listed in `RequiredContent` (160, 179-182).
6. ADR-0007 and CLAUDE.md: a schema change needs the founder's approval.

### 7.4 Content tests that pin counts
- `ContentTests.M0ContentShape` (`tests/ContentTests.cs:39-49`) asserts 14 cast, 8 traits and **6 incidents**, plus the exact set of species in the cast. None of the three worktrees changes it.
- `GameViewTests.KinComesFromTheSpeciesNature` (750-757) pins the set of species ids.

---

## 8. Snapshots

- **`SnapshotSerializer`** (`sim/Snapshots/SnapshotSerializer.cs:12-31`) serialises the **whole `World`** with System.Text.Json: enums as names, not indented. The state hash is SHA-256 of those bytes. Load re-attaches `Content`.
- **What is serialised:** every public get/set property of `World` and its children. That includes:
  - `Map` (terrain bytes, trees, bushes, roof, beds);
  - every `Character` (path, thoughts, opinions, job, priorities, growth, nature needs);
  - items, debts, grudges, contracts, arrivals and the director;
  - the **entire event log**;
  - pending and applied commands, `Culture`, and the RNG (the PCG32 state and draw count of each stream).
- **`[JsonIgnore]`, all six:**
  - `DiceStream.Recent` (`DiceRng.cs:17`)
  - `Rect.Center` (`Point.cs:24`)
  - `Debt.IsPromise` (`Social.cs:62`)
  - `World.Content` (`World.cs:46`) and `World.Hooks` (49)
  - `Character.IsActive` (`Character.cs:81`)

  `Map`'s private caches `_blocked` and `_bush` are not serialised and are rebuilt in `OnDeserialized` (`Map.cs:51-54,119`). The zones worktree adds internal fields `UseZone` and `ZonesDirty` the same way.
- **`SchemaVersion = 2`** (`World.cs:16`): "Bump when the snapshot shape changes incompatibly." All three worktrees set it to **3**.
- **The tests** (`tests/DeterminismTests.cs`):
  - `SameSeedAndCommandsSameHash` (20-27) and `DifferentCommandsDifferentHash` (29-36) run 20,000 ticks each.
  - `HookSeesEvents` and `EmitRejectsForwardCause`.
  - `SnapshotRestoreContinueMatchesStraightRun` (71-83).
  - `PendingCommandsSurviveSnapshot` (86-103).
  - `MidActivitySnapshotsRestoreExactly` (107-129): 12 snapshots across 3 days.
  - `RestoreUnderOneSecondAt200Characters` (132-151).
  - `PerformanceTests.TickCostUnderBudget` (157-167).
- **Cost.** The event log is in every snapshot. The fate worktree measured a snapshot of 224 KiB at day 40, and its archive keeps every knot, about 0.6 to 1.2 MB a year (ADR-0030 results).

---

## 9. `StateApi.CharacterState` and the inspector: where ability lines would show

- **`CharacterState`** is a **positional record** (`sim/Api/StateApi.cs:15-19`): Id, Name, Species, Status, X, Y, Job, Needs, Mood, Health, Traits, Opinions, Grudges, Debts, RecentThoughts, Growth, Belonging. It is constructed in one place only (145-146), so appending a field is a two-line change.
  - The build worktree appends `string Bed`.
  - Separate reads exist too: `NatureNeeds(w, id)` (205-217) and `Contracts(w, id)` (220-232).
  - The fate worktree adds `StateApi.Player`, `LostFutures` and `Gifts` in a partial file, `StateApi.Fate.cs`.
- **Precedent for "since day N, because of X" text:** `StateApi.Growth` (154-199) builds trait-origin lines "since day 12: <cause>" through `DescribeEvent` (317-321) and the loc keys `trait_origin.*`.
- **The inspector** is `game/scripts/InspectorPanel.cs`. `Build` (88-158) lays it out in this order:
  1. Header: name, subtitle "{species} · {status}", belonging, job.
  2. **Needs**: a grid of 3 columns of bars, one per `NeedState`, plus mood.
  3. **Nature**: a section hidden unless it has lines (128-135, `Visible = lines.Count > 0` at 208). This is the precedent for a section that appears only when needed.
  4. **Traits**: `ViewText.TraitsText` (`game/scripts/Logic/ViewText.cs:210`).
  5. **Wants**: `ViewText.WantLine` (219).
  6. **Work priorities**.
  7. **Opinions**, **Grudges**, **Debts**, **Recent thoughts**.

  `Refresh` (186-229) fills it. Its loc keys are the `ui.inspector.*` keys in `content/loc/en.ui.json:45-78`. Godot-free view text lives in `game/scripts/Logic/` and is tested in `tests/GameViewTests.cs`.
- **The fate worktree** adds rows for the player under the subtitle: life, gift, lives lost, remembers.

---

## 10. Soak summary, runner, determinism and thread-count tests

- **`SoakSummary`** (`tools/Return.Runner/Soak.cs:36-68`) fields:
  - Worlds, Years, FirstSeed, Parallel
  - Stories, StoriesPerWorld, StoriesPerWorldYear
  - BySystemSet (keys like "Incident+Needs+Social"), TopTerminalKinds
  - PerWorld {Seed, Events, Stories}, Failures {Seed, Tick, Error}, ElapsedSeconds, OutDir
  - CultureStories, CultureStoryShare, CultureWorlds, ByCultureLink
  - DeparturesAndDeathsBySpecies

  `WriteSummaryJson` (195 onward) writes them in that order.
- **`RunWorld`** (278-317) records a culture link per story (the first Culture event on the chain), which is the precedent for an "ability link" or "wildlife link" tally. Losses count only `Named` characters (308).
- **The story log's "What happened" section** is any event with an effect, any Incident, Combat, Growth or Culture event, and any event on a story chain (`StoryLog.cs:47-58`).
- **`RunnerTests.SoakIsThreadCountIndependent`** (`tests/RunnerTests.cs:275-310`):
  - It soaks 3 worlds with `--parallel 1` and again with 3 threads, and requires the log bytes to be identical.
  - World 1's file must equal `run --seed 1 --years 1`.
  - It checks that summary.json has the fields worlds, years, stories, storiesPerWorld, bySystemSet, topTerminalKinds and elapsedSeconds.
  - ADR-0005 explains why this holds: each world runs on one thread and is self-contained.
- **The CI soak** runs `tools/soak` nightly. Each worktree adds its own soak fields: zones counts zone stories, build counts plans, and fate counts returns, lost futures, companion deaths and gift chains.

---

## 11. Integer and probability units at a glance
- Health and needs: 0-1000. Mood: 0-100. Opinion: −100 to 100.
- Chances: ‰ (`Chance`). Percentages: `*Pct` fields default to 100 (`TraitDef.InsultOpinionScalePct`, `PriorityRespectPct`).
- The founder's figures in these units: 20% is 200‰, 25% is 250‰, 100% is 1000‰ (always succeeds), and 125% is 1250‰ (saturates unless the code reads the part above 1000 separately).
- Time: 300 ticks an hour, 7,200 a day, 72,000 a season, 288,000 a year (`world.json`; ADR-0005).

---

## 12. Mod hooks
- `IModHook` (`OnWorldCreated`, `OnEvent`) sees each event at the end of its tick (`Simulation.DeliverToHooks`, `sim/Simulation.cs:76-85`).
- Hooks are not in snapshots, and a hook must draw only from `w.Rng` (`IModHook.cs:5-10`).
- Mods add content by id through extra folders (`ContentDb.Load(contentDir, modDirs)`).
- The zones worktree extends `IModHook` (4 lines). The fate worktree adds an optional `IFateHook` (`OnReturned`, `OnLifeEnded`).

---

## 13. Work in flight: three worktrees

Commands run for each: `git -C <wt> log --oneline milestone-0..HEAD`, `git -C <wt> diff --stat milestone-0...HEAD` and `git -C <wt> status --short`.

### 13.1 `.claude/worktrees/wf_bafd34a6-140-1`: player and Return by Death (ADR-0030)
- **State.** HEAD `2f96984`, 8 commits ahead, 78 files changed (+5,471/−105), working tree clean.
- **Files that (A) or (B) would also touch:**
  - `sim/Systems/CombatSystem.cs`: `BleedSharePct = 225`, `Hurt(..., bool bleeds = false)`, `Gifts.TurnHarm` before `Hurt`.
  - `sim/Systems/NeedsSystem.cs`: the bleed in the hourly damage.
  - `sim/Systems/Jobs/CareJobs.cs`: `Bleed = 0`; a bleeding patient counts as injured.
  - `sim/State/Character.cs`: `BirthTick`, `OldAgeTick`, `Bleed`, `BleedCause`, `BleedDeath`.
  - `sim/Systems/IncidentSystem.cs`: `Accident` redirects through `TurnHarm`, bleeds, and calls `Gifts.OnArrival` from `Arrive`.
  - `sim/Systems/JobPicker.cs`: waiting for tending; the gift's job; bleeding counts as injured.
  - `sim/Systems/WorkSystem.cs`: `AwaitsTending`.
  - `sim/Systems/Lifecycle.cs`: `Fate.OnPlayerDied`.
  - `sim/Systems/Growth.cs`: `OldAge` and `Fate.SoulSpan` in `Hourly`.
  - `sim/Systems/Jobs/CampJobs.cs`: `Gifts.Foraged` before the forage's accident roll.
  - `sim/Systems/Jobs/JobRunner.cs`: `JobKind.GiftShare`.
  - `sim/WorldGen.cs`: the people roll in `CreateCast`, ages, arrival windows, `RedrawArrivals`.
  - `sim/Simulation.cs` and a new `Simulation.Fate.cs`: `World` becomes privately settable, plus `Fate.Wake`, `Gifts.Hourly` and `EndOfTick`.
  - `sim/State/World.cs`: `Fate`, `Gift`, `Remembered`, `StandingOrders`, `SchemaVersion = 3`.
  - `sim/Events/SimEvent.cs`: `SystemTag.Fate`, `SystemTag.Gift`, `EffectKind.GiftGrew`.
  - `sim/Core/DrawIds.cs`: Truth 80-84, Dice 220-222, `DiceSystem.Fate = 10`.
  - `sim/Content/ContentDb.cs`, `Defs.cs` and `RequiredContent.cs` (`Gifts`, `FateThoughts`).
  - `sim/Api/EventText.cs`: the `e:` argument.
  - `game/scripts/InspectorPanel.cs` (+43), `MapView.cs` and `SimHost.cs`.
  - `tests/ContentTests.cs` (the `.Num("` gift-param pattern) and `WorldGenTests.cs`.
  - The soak and story log.
- **New mechanics (A) must fit around:**
  - bleeding;
  - harm redirected by Fortune's favourite;
  - `Fate.Safe` (no knot while downed, bleeding, below 400, at a critical need, fighting, fleeing or in the sun);
  - gift growth with the half count;
  - `FateBook` as the only state a rewind keeps.

### 13.2 `.claude/worktrees/wf_3991bcec-5e6-1`: zones slice 1 (ADR-0028) and schedule (ADR-0029, in progress)
- **State.** 15 commits ahead. HEAD was `0b9ffb8` when the worktree list was taken and `7c1aee4` minutes later. About 101 files changed (+5,059/−180). **Uncommitted edits** while I read: `content/work.json` (`keep_watch` becomes `risky`), `sim/Simulation.cs` (`WatchSystem.Step` added between Combat and Work), `sim/State/Schedule.cs`, `CombatSystem.cs` (`stolen_unwatched`), `Festivals.cs`, `JobPicker.cs`, `CampJobs.cs` (`KeepWatch` moves to `WatchJobs`), `JobRunner.cs` (`JobKind.WakeForWatch`), `Places.cs` and `WorkSystem.cs`.
- **Files that (A) or (B) would also touch:**
  - `sim/State/Map.cs`: `Stockpile` renamed `StartStockpile`; new `Zones` list, `long Revision`, internal `UseZone` and `ZonesDirty`; `RebuildCaches` sets `ZonesDirty`.
  - `sim/WorldGen.cs`: `ScheduleSystem.EnsureDefaults`, then `Zones.CreateStart(w)` **last**, so no id is handed out before it moves.
  - `sim/State/Character.cs`: `LastKeptAwayDay`, `OwnHours`, `OwnHoursEvent`, `Lived`, `HourTally`, `LastWatchEndTick`, `LastDutyEvent`, `WornOutEvent`.
  - `sim/Systems/CombatSystem.cs`: `Steal` takes only the pile in reach (`ThiefReach = 1`); `stolen_unseen` and `stolen_unwatched` keys.
  - `sim/Systems/IncidentSystem.cs`: arrivals go to `Zones.ArrivalSpot`; spoilage reaches only stores in the open; the raid target comes from zones.
  - `sim/Systems/NeedsSystem.cs`: `Stock.Take(..., c.Pos)`.
  - `sim/Systems/Jobs/CareJobs.cs`: the same `Stock.Take` change.
  - `JobPicker.cs`: schedule blocks; `Places.SleepHours` is **deleted**; the wander rule becomes the meeting place; haul uses zones.
  - `Places.cs`, `Stock.cs` (+136) and `Rations.cs`.
  - `sim/State/Items.cs`: `ItemStack.Stored`, `Carry.Stored`.
  - `Social.cs`: `DebtKind.Watch`. `Relations.cs`: its key.
  - `sim/State/World.cs`: `CampDays`, `WatchFrom`, `WatchTo`, `WatchEvent`, `Watch`, `SchemaVersion = 3`.
  - `sim/Events/SimEvent.cs`: the `EventArg.Hour` `h:` prefix.
  - `sim/Core/DrawIds.cs`: `ZoneDefy = 180`, `DayChangeDefy = 200`, `WatchDefy = 201`; a TruthId 70-79 comment.
  - `StateApi.cs`: Faction and Location only; `CharacterState` is unchanged.
  - Content: `zones.json`, `rhythms.json` and their schemas; the species and traits schemas gain `rhythm` and `riskyWorkRestBelow`; `world.json` gains a schedule block.
  - `ContentTests.cs` adds `Zones` and `Rhythms` to `EveryLocKeyExists`.
- **The name clash.** "Zones" here means player-painted areas (stockpile, meeting place, planned grazing). The founder's "zones (eg; forests etc)" means habitats.

### 13.3 `.claude/worktrees/wf_c234c89e-1a0-1`: Build menu slice 1a (ADR-0027)
- **State.** HEAD `7b18eed`, 14 commits ahead, 60 files changed (+4,931/−86). **Uncommitted view work:** `game/scripts/Art/Figures.cs`, `WorldArt.cs`, `BottomBar.cs`, `MapView.cs`, `SimHost.cs`, plus new files `WorldArt.Build.cs` and `MapView.Build.cs`.
- **Files that (A) or (B) would also touch:**
  - `sim/State/Map.cs`: `RoofSection` and `Bed` gain build fields; new `Structures`, **`int Revision`** (the zones branch has `long Revision`) and `Changed()`.
  - `sim/WorldGen.cs`: roof section ids assigned last.
  - `sim/Systems/NeedsSystem.cs`: `Building.RestBonus` added to sleep.
  - `sim/Systems/Lifecycle.cs`: `Building.LetGo`.
  - `JobRunner.cs` and `Job.cs`: `Build`, `Deliver`, `TakeDown`, `Job.TargetKind`.
  - `JobPicker.cs`, `CampJobs.cs` (`MendedBy`), `Places.cs` (+117: a cover array, `SleepSpot` reworked, `CoveredWay` as a breadth-first search) and `SocialSystem.cs`.
  - `sim/Api/StateApi.cs`: **`CharacterState` gains `string Bed`**; `FactionState` gains beds, beds claimed, open plans and **trees standing**; `Location` gets build lines.
  - `sim/State/World.cs`: `Blueprints`, `SchemaVersion = 3`.
  - `RequiredContent` (`Buildings`, `BuildThoughts`) and `buildings.json` with its schema; `content/traits.json` and `work.json` (`deliver`, `take_down`).
  - `tests/ContentTests.cs`, `DeterminismTests.cs`, `RunnerTests.cs` and `YearInvariantTests.cs`.
- **DrawIds:** no change. Slice 2 plans a `DiceSystem.Build` for storms and fire.

### 13.4 Overlaps between the worktrees, and with the new work
- **`SchemaVersion = 3`** is set by all three.
- **`Map.Revision`** is `long` on zones and `int` on build.
- **`CharacterState`** gains `Bed` on build. (A) would append after it.
- **Inspector:** fate adds rows. Ability lines would be a new section; the Nature section is the precedent.
- **Creation order:** zones, build and fate all hand out creation-time ids at the end of `WorldGen.Create`, as wildlife spawned at creation would need to.
- **Streams:** fate takes `DiceSystem` 10.
- **`CombatSystem.cs`** is edited by fate (`Hurt`) and by zones/schedule (`Steal`).
- **`CampJobs.cs`** is edited by all three, at the very lines next to `RollAccident`.

---

## 14. Risks and traps (also in the summary output)
1. **Harm has no single entry point.** A tolerance placed in `Hurt` misses the blows on the thief, hourly harm, the fae's sting and the blood donor's cost (§2.2).
2. **Bleeding already exists on the fate branch**, with its own fields, a `Hurt` flag, care that clears it and a check that blocks knots. A typed-ailment design must extend or replace it, not run beside it.
3. **The integer-only test scans comment text too.** Words like "double " or "float " in prose fail the build.
4. **‰ chances saturate at 1000.** Tolerance beyond 100% does nothing unless code reads the part above 1000 explicitly.
5. **Growth "never grind" (fixed decision 12)** against the founder's "level by doing them more". The gift design's "counts of logged events, each with a cause" is the precedent. Changing the reading needs a DECISIONS.md proposal.
6. **Level-ups as plain Growth beats or as a new `EffectKind`** (the `GiftGrew` precedent). Frequent effects flood the story counts (ADR-0014). Plain Growth beats end no story, but are always shown in the log.
7. **The founder's "unique ability" is called a gift in the code.** Earned abilities need a distinct name and identifiers.
8. **The know-how half-count rule exists for gifts only** (`Gifts.Needed`, `FateBook.GiftMastery`). Applying it per ability needs a per-ability record in `FateBook`, which is the only state a rewind keeps.
9. **No habitats exist, and trees are not clustered.** Habitat would have to be derived from terrain, water, rock and tree density. "Zones" clashes with the zones menu.
10. **Animals as Characters** trigger the thief code if Hostile: they steal berries and stop thieves coming. They break `WorldGenTests.CastShape` if created with the world, and the 200-character restore test only if added during play. They inflate the per-character performance denominator, get `NatureNeeds.Init`, and every death carries `EffectKind.Death`. Animals elsewhere need their own mover, renderer, selection and event-argument type, because `c:` resolves to "someone".
11. **Hunting for meat is blocked by the berries-only load check (ADR-0018 #5)** and by the "berries" literals throughout the code.
12. **Reusing `EdgeTile`, `Places.Near` or `RollAccident` draws from the Incident or Work stream** and shifts those sequences. A new stream's number depends on merge order (Fate takes 10).
13. **A new incident such as a snake bite breaks `M0ContentShape`'s count of 6.**
14. **New def param accessors must not be named `Param`, `Arg` or `Num`** without extending `RequiredContentCoversIdsNamedInCode`.
15. **A new `SystemTag` needs a `log.system.*` key**, and en.log.json fails on unused keys. New `JobKind`, `CharStatus`, `TerrainKind` and `DebtKind` values need loc keys.
16. **Calibration.** The fate branch measures companion deaths at 1.87 a world-year (the bar is 2). A bite that kills 80% of the time must be rare.
17. **Merge pressure.** `SchemaVersion` 3 is taken three times, `Map.Revision` has two types, `CharacterState` gains `Bed`, `Places.SleepHours` is deleted, `Map.Stockpile` is renamed, and the `CampJobs` lines next to `RollAccident` are touched by every branch.
