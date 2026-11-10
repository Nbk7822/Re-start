# What Return's design documents already say: notes for the "Abilities and stats" and "Wildlife" designs

Research only, not design. Compiled 2026-11-09 from the repo at `/Users/nkov/Re-start`. Nothing in the repo was changed.

## How to read this

**Read in full on `milestone-0` at e505293:** CLAUDE.md, BIBLE.md, DECISIONS.md (ADR-0001 to 0026), STORIES.md, and all eight specs in `docs/superpowers/specs/`. Each spec is byte-identical on every worktree (checked with `cmp`).

**Code and content, read where a document points to them:** `sim/Core/DrawIds.cs`, `sim/Events/SimEvent.cs`, `sim/Systems/Traits.cs`, `CombatSystem.cs`, `IncidentSystem.cs`, `NeedsSystem.cs`, `Culture/Omens.cs`, `Culture/CultureTuning.cs`, `sim/Content/RequiredContent.cs`, `content/schemas/domains.schema.json` and `content/*.json`.

**Worktrees** (from `git -C /Users/nkov/Re-start worktree list`). `main` holds only "Initial commit" (7cb2d11), so `milestone-0` is the trunk.

| Short name | Path | Branch @ head | Track | State |
|---|---|---|---|---|
| WT-build | `/Users/nkov/Re-start/.claude/worktrees/wf_c234c89e-1a0-1` | `worktree-wf_c234c89e-1a0-1` @ 7b18eed | Build slice 1a and 1b (ADR-0027) | committed, apart from one view file (`game/scripts/Art/WorldArt.cs`) |
| WT-zones | `/Users/nkov/Re-start/.claude/worktrees/wf_3991bcec-5e6-1` | `worktree-wf_3991bcec-5e6-1` @ 0b9ffb8 | Zones steps 1 and 2 (ADR-0028), then the Schedule (ADR-0029) | the Schedule is partly uncommitted: `Commands.cs`, `SimEvent.cs`, `Compliance.cs`, `EventText.cs` and `OrderDrafts.cs` are modified; `ScheduleCommands.cs`, `ScheduleSystem.Commands.cs` and `ScheduleCommandTests.cs` are new |
| WT-player | `/Users/nkov/Re-start/.claude/worktrees/wf_bafd34a6-140-1` | `worktree-wf_bafd34a6-140-1` @ 2f96984 | Player design slice 1 (ADR-0030) | clean |

**Citations.** A citation is `file:line`.
- Specs are cited by short name: `loot:` (loot-economy-design), `player:`, `world:`, `build:`, `zones:`, `schedule:`, `peoples:`, and `culture:` (culture-faith-design).
- `BIBLE:`, `DECISIONS:`, `STORIES:` and `CLAUDE:` mean the milestone-0 copies.
- Code paths are relative to the repo root on milestone-0 unless a worktree short name comes first.
- "Brief" means the founder statements quoted in this research task.

**Sibling notes in this folder:**
- `poe-offense.md` and `poe-defense.md` hold the Path of Exile math. Section 1.3 below gives only the mapping onto this repo.
- `repo-systems.md` holds the code-level hook points.
- `animals.md` holds the zoology.

---

## 0. The findings that matter most

1. **The repo already has a small modifier core shaped like Path of Exile's, with two of its three stages.**
   - In `sim/Systems/Traits.cs:8-35`: `Traits.Sum` is PoE's "added" stage. `Traits.ScalePct` is "more": it multiplies one value after another and truncates to an integer at each step, so the result depends on the order. `Traits.Max` means only the highest applies, and `Traits.Any` is a flag.
   - There is no "increased" stage.
   - No damage type and no resistance exists anywhere in code, content or specs.
2. **The loot design already routes items into that core.**
   - "A quality may name any `traits.json` field that sums or is a flag ... `Traits.Sum`, `Any` and `ScalePct` read the kit after the traits" (loot:185).
   - It adds a closed set of seven "thing modifiers", each read in exactly one place (loot:187-197).
   - An ability that writes the same fields would share the core with items.
3. **The player design's gifts are the existing template for "levels by counted events."**
   - Stages come at counts. Each growth cites the event that caused it and ends a story with `EffectKind.GiftGrew`.
   - A return sets progress back, "but a tier you reached before needs half the count" (player:155).
   - WT-player builds this as `Gifts.Needed` = `(at + 1) / 2`, with `World.Fate.GiftMastery` kept across returns.
4. **Venom's nearest precedent is WT-player's bleeding wound.** `Character.Bleed` costs health every hour equal to 225% of the blow's damage. It has its own death line, tending sets it to 0, and it needs no new draw (ADR-0030 #11).
5. **No spec, ADR, bible line, story or content file mentions poison, venom or snakes** (section 3.1).
   - Animals appear only as things deferred until later: grazing "after animals exist" (zones:127, 199); the werewolf "hunts (later, with animals)" (peoples:460); peoples batch 6 "needs first ... animals and hunting" (peoples:1108).
   - A wolf is drawn in the view but unused (DECISIONS:159; `game/scripts/Art/Figures.cs:1029`).
6. **Several of the founder's words already mean something else in the repo** (section 5, traps 1 to 4).
   - The founder's "zones (eg; forests)" are habitats. In the repo, Zones are areas the player paints, and `graze` is already a planned Zones behaviour (zones:513).
   - "Ability" is the player's rolled gift in CLAUDE.md and ADR-0024.
   - "Tolerance" is the peoples design's per-person variation in how fast needs drain (`NeedTolerance`, peoples:972 and 1146-1148). ADR-0025 #5 accepted it.
   - "Level", "tier", "stage" and "depth" already mean four different things.
7. **The founder's survival curve, read literally, almost never pays out.**
   - The literal reading is 20% at level 0, then 5 points more per level up to 100%.
   - That gives 0.27 expected survived bites before death, and about 1 chance in 32,000 of surviving the 16 bites needed for full tolerance.
   - Set that against "you die every few years" (ADR-0026 #2) and the cap of at most 2 companion deaths per world-year (player:581). ADR-0030 measured 1.87 overall and 2.25 in human worlds.
8. **Hunting for meat collides with the berries-only food rule.** ADR-0018 #5 keeps food berries-only until the items milestone, and a species that eats anything else is rejected at load (DECISIONS:133). The loot design keeps it that way until its slice 3 (loot:8).
9. **Fixed decision 5 forbids differences in stats between peoples** (CLAUDE:16; peoples:79). A people born immune to venom would be one.
10. **Proposed free numbers** (section 2.8):
    - TruthIds 160-169 for abilities and 170-179 for wildlife.
    - DiceIds 260-279 and 280-299.
    - Streams named `Ability` and `Wildlife`. Their numbers can only be fixed at merge, because stream numbers must run without gaps. The Abilities design may not need a stream at all.
    - SystemTags `Ability` and `Wildlife`.
    - ADR-0031 onward.
    - Loc files `en.abilities.json` and `en.wildlife.json`.

---

## 1. (a) Vocabulary and mechanisms to reuse

### 1.1 The modifier core that exists today: `Traits`

`sim/Systems/Traits.cs` reads character modifiers from content, in integer arithmetic only (line 5).

| Helper | Code | What it does | Path of Exile analogue |
|---|---|---|---|
| `Traits.Sum` | :8-13 | adds one field across every trait held | "added" or flat (BASE in Path of Building) |
| `Traits.Max` | :15-20 | takes the highest value of a field across traits (floor 0) | "only the highest applies"; a threshold |
| `Traits.Any` | :22-27 | true if any trait has the flag | a keystone-style flag |
| `Traits.ScalePct` | :29-35 | starts at 100 and applies `pct = pct * pick / 100` for each trait | "more" and "less" (multiplicative), truncated at each step |
| `Traits.WorkBias` | :37-44 | adds up biases per kind of work | an additive weight per kind of work |

Every trait field, how it is read, and who holds it (call sites from grepping `Traits.`; values from `content/traits.json`; fields from `TraitDef` at `sim/Content/Defs.cs:91-113`):

| Field | Read with | Read in | Held by |
|---|---|---|---|
| careUrgency | Sum | JobPicker.cs:79 | protective 3, generous 2 |
| insultPermilleAdd | Sum | SocialSystem.cs:45 | protective −10, proud 15, independent 10 |
| insultOpinionScalePct (default 100) | ScalePct | SocialSystem.cs:94, BloodJobs.cs:153 | proud 160 |
| consentBias | Sum | BloodJobs.cs:61, Natures/Bargains.cs:81 | proud −20, generous 20, independent −10 |
| fightsThieves | Any | CombatSystem.cs:68 | protective |
| fleeHealthBelow | Max | CombatSystem.cs:117, JobPicker.cs:305 | anxious 700 |
| stealFoodBelow | Max | Rations.cs:80, JobPicker.cs:137 | desperate 220 |
| offersBlood | Any | JobPicker.cs:121, BloodJobs.cs:105 | generous |
| sharesFood | Any | SocialSystem.cs:135 (grudge decay) | generous |
| restThresholdAdd | Sum | JobPicker.cs:70 | oathbound −80 |
| riskyWorkHealthBelow | Max | JobPicker.cs:241, BloodJobs.cs:59 | anxious 600 |
| priorityRespectPct (default 100) | ScalePct | JobPicker.cs:240 | independent 50 |
| promisePriority | Sum | JobPicker.cs:155 | oathbound 3 |
| refusesUnsafeBargain | Any | Natures/Bargains.cs:80 | proud |
| workBias | WorkBias | JobPicker.cs:251 | practical (repair_roof 2, forage 1) |

**What follows for a shared core:**
- **No "increased" stage exists.** PoE's middle stage, which adds up percentages and applies the total once, would be new.
- **`ScalePct` truncates after every multiplication.** With two or more modifiers the result depends on the order of the trait list. That order is deterministic, but the result is not order-free.
- ADR-0004: "Simulation state uses integers. No floating point in state or decisions." (DECISIONS:30). Path of Building's own "more" product rounds to two decimals (section 1.3), so an integer port has to choose its rounding.
- **Other multipliers in code or specs each have a formula of their own:**
  - oath weight `100 + 25 × axis` percent, "50% to 150%" (BIBLE:168; `CultureTuning.OathWeightPerAxisPct` = 25);
  - omen odds × `(100 + 25 × Arcane axis) / 100` (culture:739; Omens.cs:75);
  - `NatureNeeds.FightHitPct`, the share of its hit chance a weakened person keeps (peoples:762, 841; the brimmet's `weakHitPct` is 50, peoples:755);
  - the black ring: "the wearer's accident threshold doubles" (loot:262);
  - chemistry scales a chat's warmth by `(100 + 50 × chemistry)%` (DECISIONS:113).
- **Nothing is a resistance, a damage type or a defence.** Harm comes in two kinds:
  - instant harm through `CombatSystem.Hurt`;
  - hourly harm added up in `NeedsSystem.Hourly`: starvation 10 an hour, harm from a nature need, the vampire's sun burn of 60 an hour, and the bleed on WT-player.
- **The nearest thing to PoE's damage types is a planned list of "harms" on each species:** "Harms become a list on the species when batch 5 brings the next one: `petrify` for trolls, `heat` near the hearth for rimelings, `iron` once items exist. Each entry then gives its kind, health per hour, effect, cover, thought, event, death text and the promise it implies for a newcomer." (peoples:781)

### 1.2 The loot design's things: qualities, modifiers, kit and tiers

Status: proposed, then signed off by ADR-0026 #5: "Every other question in the three documents takes the document's recommended answer. These include the schema and determinism changes of each first slice" (DECISIONS:331). Not built.

**Vocabulary** (loot:95-105):
- Goods are counted in heaps; a "thing" is one object (`ItemInstance`). A purse holds a person's own money and remedies.
- **Kit:** "one thing in hand (a tool or weapon), one on the body (a cloak), one at the belt (a lantern) and one keepsake. Only kit takes effect." (loot:99)
- **Quality:** "A rolled property, such as 'keen' (strikes harder). Path of Exile's affix." (loot:100)
- **Rarity:** plain, fine, rare or storied (loot:101).
- **Depth:** "Deeper finds roll rarer things and stronger qualities, as Path of Exile's item level does." (loot:102)

**How kit is chosen and read** (loot:183-199):
- "When a job starts, people take the best thing they own for it, else a free camp thing from the stores ... else nothing. Nobody needs a thing to work" (loot:183).
- **Trait modifiers:** "A quality may name any `traits.json` field that sums or is a flag (`insultPermilleAdd`, `careUrgency`, `fightsThieves`, …), with its meaning there; `Traits.Sum`, `Any` and `ScalePct` read the kit after the traits. A thing is then a trait that can be put down, sold or stolen, and peoples still differ only in needs (fixed decision 5). Fields read with `Max` (`fleeHealthBelow` and the like) are closed to things: a thing adds behaviour and never cancels a trait's." (loot:185)
- **Thing modifiers, "a closed set, each read at one place"** (loot:187-197):

| Modifier | Read in | Effect |
|---|---|---|
| `workDurationPct`, `workAccidentPermille` (per work) | JobRunner | faster work; fewer accidents (or more) |
| `combatDamageAdd`, `combatHitPermille` | CombatSystem.Exchange, both sides | harder or surer blows |
| `nightSightAdd` | CombatSystem.Spot | the watch sees thieves farther off at night |
| `rainShelter` | IncidentSystem.RainUpkeep | no `soaked` thought and no rest lost |
| `sleepRestorePct` | NeedsSystem | rest returns faster |
| `careHealAdd` | CareJobs | tending heals more |
| `chatWarmthAdd` | SocialSystem | talk warms faster |

- "A new modifier, relic kind, shock kind or loot source needs code, and an unknown one fails at load (ADR-0018)." (loot:402)
- **Kit cache:** "a `[JsonIgnore]` array on `Character`, rebuilt when an instance moves, keeps modifier reads within ADR-0005's budget." (loot:177)
- "Thieves and raiders use kit too" (loot:199).

**How things are generated.** Every roll is Dice on the Economy stream, in Path of Exile's order (loot:202-208):
1. Rarity, weighted by the source and its depth. At depth 1: plain 70, fine 24, rare 5, storied 1.
2. The base item.
3. Qualities: "fine one or two, rare three or four, no repeats, among those whose `tags` the base carries, each at the best tier its depth allows (`minDepth`)."
4. A two-word name for a rare thing.
5. A storied relic, at most once per world.
6. Value: "base value × (100 + 40 per quality) / 100, tripled if storied."

**Qualities, at tiers I / II / III** (loot:222-233):

| Quality | Modifier and tiers |
|---|---|
| keen | `combatDamageAdd` 20 / 35 / 50 |
| steady | `workAccidentPermille` −8 / −12 / −16 |
| swift | `workDurationPct` −15 / −25 / −35 |
| oiled | `rainShelter`, plus `sleepRestorePct` +10 / +15 / +20 |
| bright | `nightSightAdd` +1 / +2 / +3 |
| of the watch | `fightsThieves` |
| of patience | `insultPermilleAdd` −10 / −15 / −20 |
| of wrath | `insultPermilleAdd` +15 / +20 / +25 |
| of kinship | `chatWarmthAdd` +25 / +40 / +50 |
| of mercy | `careUrgency` +1 / +2 / +2 |

**Bases** (loot:212-220):

| Base | Value | What it does |
|---|---|---|
| knife | 30 | fights; a knife remembers its last holder's grudge |
| club | 10 | fights |
| axe | 45 | "fells 20% faster" |
| cloak | 35 | `rainShelter` |
| lantern | 40 | `nightSightAdd` 3 on the watch |
| salve | 12 | care uses one and "heals 150 more" |
| keepsakes | 20-30 | gifts that settle debts; heirlooms |

The qualities schema: `tiers [{minDepth, min, max, weight}]`, plus a `modifier` taken from the closed set (loot:354).

**Story things** (relic kinds, loot:256-265):
- remembers: every blade carries its last holder's grudge;
- cursed: the black ring doubles the wearer's accident threshold;
- heirloom, faith_claim and token.

**Making things comes later.** Slice 3 maps PoE's crafting orbs (chaos, exalted, Vaal, divine) onto people who do the work (loot:346).

**The anti-goal that binds any stat pipeline:** "**A stat treadmill.** No grinding, no damage numbers, no '+3% work speed' lines. Qualities are words ('keen', 'keeps you dry')." (loot:87)

### 1.3 A PoE-style pipeline as the shared core: what maps where

This section describes; it does not design. The full math is in `poe-offense.md` (section 0.1) and `poe-defense.md` (sections 2 and 3). The facts below were checked for this file. All are PoE1.
- **Primary** means the Path of Building Community source, which the community treats as the reference calculator.
- **Secondary** means a guide or forum post.

| Fact | Source | Grade |
|---|---|---|
| A stat's value is BASE × (1 + ΣINC/100) × MORE. `calcLib.val` returns `baseVal * calcLib.mod(modStore, cfg, name)`, and `calcLib.mod` returns `(1 + (modStore:Sum("INC", cfg, ...)) / 100) * modStore:More(cfg, ...)` | [CalcTools.lua](https://raw.githubusercontent.com/PathOfBuildingCommunity/PathOfBuilding/dev/src/Modules/CalcTools.lua) | primary |
| Each MORE multiplies in: `modResult = modResult * (1 + value / 100)`; the product is then rounded (`round(modResult, 2)`; `math.floor(result * modResult * power) / power`) | [ModDB.lua, `MoreInternal`](https://raw.githubusercontent.com/PathOfBuildingCommunity/PathOfBuilding/dev/src/Classes/ModDB.lua) | primary |
| "In math terms, 'increased / decreased' is an additive effect, 'more / less' is multiplicative." Formula given: "Base Damage * (1 + increased damage - decreased damage) * more damage * less damage" | [official forum, thread 320366](https://www.pathofexile.com/forum/view-thread/320366) (player posts, no staff badge) | secondary |
| Resistance: the uncapped `total = base * inc`; `max = m_min(data.misc.MaxResistCap, ...)`; `final = m_max(m_min(total, max), min)` with `min = data.misc.ResistFloor`; `ResistOverCap = m_max(0, total - max)` | [CalcDefence.lua, `calcs.resistances`](https://raw.githubusercontent.com/PathOfBuildingCommunity/PathOfBuilding/dev/src/Modules/CalcDefence.lua) | primary |
| `MaxResistCap = 90`, `ResistFloor = -200` | [Data.lua](https://raw.githubusercontent.com/PathOfBuildingCommunity/PathOfBuilding/dev/src/Modules/Data.lua) | primary |
| "Resistance for each Element is capped at 75%." Excess resistance "would also act as a buffer so that you do not fall below Resistance cap." Chaos resistance is typically around −60%, and the campaign penalises resistance (−30% in Act VI, −60% in the epilogue) | [poe-vault beginner guide](https://poe-vault.com/guides/path-of-exile-beginner-guide-learning-to-loot-learning-resistances) | secondary |
| Curses and exposure lower the uncapped value, so overcap absorbs them. Penetration applies to the capped value, so overcap never helps against it | `poe-defense.md` section 3.5, citing the PoE wiki's Resistance page; the Path of Building excerpt read here only stores `enemyPen` | sibling notes; not re-verified here |
| Poison constants: `PoisonPercentBase = 0.30`, `PoisonDurationBase = 2` | [Data.lua](https://raw.githubusercontent.com/PathOfBuildingCommunity/PathOfBuilding/dev/src/Modules/Data.lua) | primary (what the constants mean: see `poe-offense.md` section 4) |

**How the PoE stages would map onto the repo:**

| PoE stage | In the repo today | Gap |
|---|---|---|
| BASE, or added (flat) | `Traits.Sum` over the `*Add` fields; loot's `combatDamageAdd`, `nightSightAdd`, `careHealAdd`, `chatWarmthAdd` | none |
| INCREASED (add the percentages, apply once) | nothing | a new stage; loot's `workDurationPct` and `sleepRestorePct` could be read either way |
| MORE (multiply) | `Traits.ScalePct` (`insultOpinionScalePct`, `priorityRespectPct`) | truncates at every step; the order is the trait list's |
| Only the highest; thresholds | `Traits.Max` (closed to things, loot:185) | none |
| Flags (keystones) | `Traits.Any`; loot's `fightsThieves`, `rainShelter` | none |
| Resistance with a maximum and overcap | nothing | new; this is the founder's "tolerance" |
| Chance to hit | `CombatSystem.HitPermille` 500 × `NatureNeeds.FightHitPct` (peoples:841), plus loot's `combatHitPermille` | none |
| Damage over time | the hourly harms in `NeedsSystem.Hourly`; WT-player's `Character.Bleed` | a damage type for venom |
| Avoiding or turning a hit | WT-player's `Gifts.TurnHarm`, called before damage lands (CombatSystem.cs:125; IncidentSystem.cs:172) | none |

**The two ways harm enters today.** A tolerance would sit at one or both.
- **Instant harm:** `CombatSystem.Hurt(w, c, damage, cause, deathKey, system)` (milestone-0 CombatSystem.cs:41-60; WT-player adds `bool bleeds`). Its callers on WT-player:
  - thieves' and raiders' blows (CombatSystem.cs:125);
  - work accidents (IncidentSystem.cs:177, 182);
  - a dryad's tree being felled (Natures/Forest.cs:116).
- **Hourly harm:** `NeedsSystem.Hourly` adds into one `damage` total:
  - starvation, 10 an hour (`StarvationDamagePerHour`);
  - harm from a nature need;
  - the bleed (WT-player NeedsSystem.cs:54-59);
  - the sun, 60 an hour for vampires (`daylightBurnPerHour`).
  The death key is whichever of these set it last.
- **Combat numbers:** `HitPermille` 500, `DamageMin` 60, `DamageMax` 140, `BlowEveryTicks` 30 and `SightRange` 8 (CombatSystem.cs:15-19). World raids reuse them: "Each round, each takes a blow with chance 150‰, for 60–140 damage (`CombatSystem`'s numbers)" (world:242).

**The founder's overcap rule maps onto one side of a known PoE fork.** The rule: "when you go to 125% tolerance the remaining 25% applies to any kind of abilities anyone else has that reduces tolerance" (brief).
- In PoE terms this is the *reduction* case. Curses and exposure lower the uncapped total, so overcap acts as a buffer.
- It is not the *penetration* case. Penetration applies after the cap, so overcap gives nothing against it.
- A third PoE case also exists: lowering the *maximum*, which overcap cannot help with (`poe-defense.md`, section 1, item 2).
- The designers have to choose and say so. In integers, the order needs to be spelled out, for example `final = clamp(total − reduction, floor, max)`.

**Venom and the bleed precedent** (WT-player):
- "An accident or a blow bleeds `CombatSystem.BleedSharePct` = 225% of its damage in health an hour until someone tends it; a death of it is the wound's own line and cites it; no draw." (WT-player DECISIONS.md:348)
- In code:
  - `public const int BleedSharePct = 225;` (WT-player sim/Systems/CombatSystem.cs:25);
  - `c.Bleed += Math.Max(1, damage * BleedSharePct / 100);`, with `BleedCause` and `BleedDeath` set beside it (:57-59);
  - taken each hour with the other harm (NeedsSystem.cs:54-59);
  - cleared by care: `patient.Bleed = 0; // tending stops the bleeding (ADR-0030)` (Jobs/CareJobs.cs:33).
- What it means in health: the bleed is set once at the wound and taken whole at every hourly pass.
  - A 100-damage blow leaves 900 health and a bleed of 225 an hour: 675, 450, 225, then death in the fourth untended hour.
  - A thief's blow of 60-140 bleeds 135-315 an hour.
  - A work accident of 150-300 bleeds 337-675 an hour, so death comes within one or two untended hours.
  - This is the lethality a venom design would be tuned against.
- Anyone bleeding counts as injured, so carers come (JobPicker.cs:194).
- A second damage-over-time pattern is planned: the knollet's curse. "The offender sickens (a `cursed` thought, and a small loss of health and rest every hour) until amends are made" (peoples:562).

**The integer scales a design would be written against:**

| Quantity | Value | Source |
|---|---|---|
| Needs and health | 0-1000 (1000 is full) | sim/State/Character.cs:24 |
| Mood | 0-100 | same |
| Opinion | −100 to 100 | BIBLE:87 |
| Odds | per mille (‰) | throughout |
| Downed | health below 150 | NeedsSystem.cs:19 |
| Injured (carers come) | health below 400 | JobPicker.cs:16 |
| Back on their feet after being downed | 300 or more | NeedsSystem.cs:20 |
| A blow in combat | hit 500‰; damage 60-140 | CombatSystem.cs:17-19 |
| A work accident's damage | 150-300 | content/incidents.json, `work_accident` |
| Accident odds per job | forage 15‰, gather timber 25‰, repair roof 30‰ | content/work.json |
| The night thief | health 600; flees below 300 | content/incidents.json |
| Care | 300 ticks; heals 260 (half that without a berry) | content/work.json; CareJobs |
| Bleed | 225% of the damage, every hour until tended; wounds add up | WT-player CombatSystem.cs:25, 57 |
| Starvation; sun | 10 an hour; 60 an hour for vampires | NeedsSystem.cs:21; species.json |
| Blood | bloodPerFeed 700; donorHealthCost 130; donorRestCost 170 | species.json, vampire |
| Time | 300 ticks an hour, 7,200 a day; 10-day seasons; a 40-day year of 288,000 ticks | BIBLE:35 |
| Grudge line; leaving line | opinion −40 or lower; −45 or lower | SocialSystem.cs:23-24 |

### 1.4 The player design's gifts: growth by counted events

Status: designed in the player spec, signed off (ADR-0026), and slice 1 built on WT-player (ADR-0030). Not merged.

**The rules** (player:150-157). The binding ones are quoted:
1. "One gift per run, a Truth (`TruthId.PlayerGift`), drawn by weight from `gifts.json`."
2. "**Never a number.** A gift touches at least two systems, and every event it makes cites its cause." (player:151)
3. "**No one's choice is taken away.** Consent and refusals stand (fixed decision 2)."
4. "**A cost or a reaction:** someone resents it, fears it, leans on it, or pays for it."
5. It awakens at a trigger, or by its `byDay` in a dream. Once awakened it is known in every timeline.
6. "**It grows by events, not grind.** Each gift names the events of its own that count, and tiers come at counts. Growth is world state, so a return sets it back, but a tier you reached before needs half the count." (player:155)
7. "**It is seen.** A gift is hidden, seen or loud; witnesses within 8 tiles react with a thought." (player:156)
8. You steer it only through one standing order of its own, and you never roll compliance against your own order.

**Anti-goals:** "Stat gifts." and "A status screen, a skill tree, or a meter on screen (ADR-0008)." (player:98, 100). Even so, the design's inspector mock-up shows digits: "Gift Bottomless satchel · holds 3 meals" and "(−10)" next to a remembered wrong (player:377, 380). So "never a number" means a gift is never a flat stat bonus, not that the screen shows no digits.

**Tier counts** (player:201, 209, 215; WT-player content/gifts.json):

| Gift | Stage 2 at | Stage 3 at | What grows |
|---|---|---|---|
| Bottomless satchel | 3 shares | 8 shares | capacity 3 / 6 / 10 |
| Fortune's favourite | 3 turned harms | 7 turned harms | reach 1 / 2 / 2; "death passes you by" at stage 3 |
| Familiar faces | 2 | 5 | echo 25% / 40% / 60% |

The gifts.json shape is `tiers: [{at, stageKey, params}]` (player:489-496). WT-player adds `stageKey`, `growsKey` and `dreamKey` (ADR-0030 #2).

**How "half the count" is built** (WT-player `sim/Systems/Gifts/Gifts.cs:57-62`):
```
public static int Needed(World w, GiftDef g, int tier)
{
    int at = g.Tiers[tier].At;
    return tier <= w.Fate.GiftMastery ? (at + 1) / 2 : at;
}
```
- `Count` adds to `World.Gift.Progress`, raises `Tier` and sets `w.Fate.GiftMastery = Math.Max(...)`. It then emits `event.gift.grew` citing the counted event, with `EffectKind.GiftGrew` (:68-80).
- `GiftMastery` lives in `World.Fate`, "The best stage your gift reached in any timeline" (WT-player sim/State/Fate.cs:44-45). That is the one block of the world a return does not restore.
- `World.Gift` (`Tier`, `Progress`, ...) goes back with the world (player:423).
- **Rounding up has a cost.** A stage at count 1 still needs 1, and a stage at count 2 needs 1. Early levels gain little or nothing from mastery.
- Growth ends stories: "Stories still end in grudges, debts, departures and deaths (ADR-0006), and now also in `GiftGrew`." (player:460)

**What a return keeps** (player:274-283, the table verbatim):

| You keep (`World.Fate`) | Goes back with the world |
|---|---|
| your people, gift and soul span; that your gift woke, and its best tier | your body; your gift's growth and contents |
| every lost future; your memories; Familiar faces' echoes | everyone's memories, opinions, grudges, debts and promises (BIBLE 5.1.5) |
| customs you learned; what you have seen of the world and whom you met (world design, question 5) | items, buildings, zones, schedules, priorities, the ration policy |
| every timeline's commands; knots older than the one you returned to | Dice events (arrivals still to come, re-drawn in step 5; incidents, weather, pressure); companions' growth; newer knots |

- "Natures hold because they are Truths: traits, natural wants, pair chemistry, a dryad's tree, an undead's work." (player:283)
- "Companions remember nothing of a lost future, except through anomalies and the gifts that name an exception." (player:312)
- Your memories come back at half strength (player:270). WT-player adds a `minFeeling` of 10 and at most four memories per return (ADR-0030 #9).
- **The founder's rewind rule, from the brief:** "the player's body forgets what it endured but know-how returns twice as fast, and companions lose what they gained after the point returned to."
  - "Your body" and "companions' growth" are already in the goes-back column.
  - "Twice as fast" equals the half count kept in `GiftMastery`.
  - Still open: whether what the body "endured" (a tolerance) gets the half count at all, or only know-how does.
  - Today there is one mastery value for the player's one gift. Abilities would need one per ability, kept for the player only.

**Gifts in the catalogue that overlap earned abilities** (player:161-188):
- Take the wound ("suffering healer"; slice 2; Work, Needs, Social, Fate);
- Red haze (berserker; later);
- Name-giver (naming starts an evolution; growth M1);
- Talent sight (you see each want and its turning point; growth M1);
- Maker's hands ("what you build lasts; those beside you learn"; Build, M1);
- Every tongue (no faux pas; culture-faith);
- Wayfinder (map skill; world W2).

**Wounds that bleed** (player:464-470):
- `CombatSystem.Hurt` adds a share of the damage to `Character.Bleed` (health lost per hour) and keeps the wound as `BleedCause`. `NeedsSystem.Hourly` takes the bleed, citing the wound.
- Tending stops it, and the tended person owes the tender.
- "No new draw. Who lives turns on who is near and awake."
- It touches Incident, Combat, Needs, Work and Social.
- It was built with `BleedSharePct` = 225 (section 1.3).

**Death targets:**
- The design's soak targets (player:581): a natural return in 25-35% of worlds, at least 15% for each people, and "Companions' deaths at most 2 a world-year".
- Measured on WT-player (WT-player DECISIONS.md:354-355): natural returns in 76 of 300 worlds (25.3%); companions' deaths 1.87 a world-year (1.94 on 300 seeds). Human worlds read 2.25, "over the bar of 2".
- The founder: "**You die every few years.** About once every three in-game years, through the world's own dangers" (ADR-0026 #2, DECISIONS:328).

### 1.5 BIBLE section 5: growth (fixed decision 12)

- "Growth is event-driven, never grind-driven, and every change is a logged story beat with a cause." (BIBLE:119)
- **Mechanics** (BIBLE:123-130):
  - arcs: want → obstacle → turning point → resolution;
  - "**Trait mutation through experience:** ... Experiences (near-death, betrayal, command, grief, mercy received or given) roll on trait-change tables. Every change logs cause and tick." (:124);
  - "**Evolution:** a rare transformation triggered by naming, oaths, or player milestones (founding, coronation). Offers 2-3 paths; the character's want weights the choice. Changes subspecies, role, abilities and sprite parts." (:125);
  - niche (:126), shared memory (:127), underdog payoff (:128);
  - decline: "Growth can be negative" (:129);
  - "Truths keep a character's nature; Dice re-roll their experiences" (:130).
- **Data model** (BIBLE:134-136):
  - each trait records its origin (generated, or mutated with a cause and a tick); want; arc; role; `evolutionTier`; memories;
  - content files `trait_mutations.json`, `evolution_paths.json`, `wants.json` and `arc_incidents.json`;
  - "emit a beat for every trait change, evolution and arc stage transition".
- **Scope** (BIBLE:140-142):
  - M0: data and logging only;
  - M1: mutation tables, arc advancement, and naming-triggered evolution for two species;
  - later: underdog payoffs, decline, and a chronicle of earlier timelines' versions.
- **Acceptance** (BIBLE:146-149):
  - in a soak of 100 worlds × 20 years, at least 70% of companions alive for 5 or more years have at least one mutation and one arc transition;
  - "No growth from grind: every trait change references an incident id." (:148);
  - "All mutation and evolution rolls use the Dice stream with purpose tags." (:149)
- **What M0 built** (ADR-0021):
  - `Growth.ChangeTrait` is "the only way to change a trait; it refuses a change without the event that caused it" (BIBLE:153; DECISIONS:173).
  - "Growth beats carry `SystemTag.Growth` and no persistent effect, so none ends a story" (DECISIONS:179).
  - `DiceSystem.Growth = 8` is seeded and never drawn, and "DiceId 100–119 stay reserved for M1's mutation and evolution rolls" (DECISIONS:180).
- **For now growth breaks the cut rule.** It reads three systems but writes only beats until M1 closes the loop (BIBLE:52).
- **Experiences other designs have already offered to M1's tables:**
  - peoples:1015: a dish spilled, a contest, whistle-speech learned, a night as the wolf, a refused leave;
  - culture:1154-1157: repeated omen readings "could harden someone toward zealotry"; forgiveness at a festival "could soften a Proud character";
  - zones:652-655: "learned the hard way", mercy received, sitting apart for days;
  - build:649: laying a friend to rest;
  - loot:413: being cheated, a good bargain, "the barrow's terror";
  - world:435: world events as causes; founding and coronation as evolution triggers.
- **One learned skill is already designed:** the feeplin's whistle-speech, which is "learned through patient contact, deep talks and gifts with the same feeplin over days: a flag on the person, `understands: [feeplin id]`, earned by experience in the shape of fixed decision 12's mutations" (peoples:614).
- **Evolutions are already written as changes of ability.** The werewolf becomes *moon-tamed* ("the change waits for its will") or *wolf-blooded* ("stronger runs and longer nights away; harder to keep in") (peoples:507).
- **Conversion goes only through evolution.** Peoples question 7: "only through its evolution machinery, as a rare, logged beat with a cause, and never something the player can aim at someone" (peoples:1143-1145).

### 1.6 Culture-faith: omens, signs and deeds, and how to add one

**Definitions** (culture:86-88):
- "**Deed**: a thing someone did that gods care about, recorded when it happens."
- "**Sign**: an incident a god is said to speak through (rain, a windfall, spoiled food, an accident, a death)."
- "**Omen**: a follower (devotion at least the faith's `omenDevotion`, 40 or 50 in slice 1) reading a sign as a god's verdict on a recent deed."

**The rule** (BIBLE:186): "When a sign follows such a deed within two days, a follower devout enough for their faith may read it (a Dice roll scaled by devotion, the god's wrath or grace, the deed and the reader's Arcane axis) ... The reading moves the reader's opinion of the doer, and an angry one past the grudge line holds a grudge. A reader then waits five days before reading again."

**In code** (sim/Systems/Culture/Omens.cs:34-86; CultureTuning.cs):
- `odds = Devotion × (Wrath or Grace) × DeedWeightPct / 2000`, then × `(100 + 25 × Arcane axis) / 100`, clamped to 0-600‰.
- Deeds are remembered for 48 hours; a reader waits 120 hours; an angry reading forms a grudge of 20.
- The roll is `DiceId.OmenRead`, on the Culture stream. "The Dice roll happens only when a matching deed exists" (culture:748).
- `DeedWeightPct` (CultureTuning.cs:53-63): DebtRepaid 80, MealShared 60, GuestWronged 150, BloodGiven 40, BloodRefused 60, TreeFelled 30, SacredHarmed 200, anything else 100.

**Both lists are closed** (culture:152-153, 349, 404-420):
- Signs: `rain`, `windfall`, `food_spoils`, `work_accident`, `death`.
- Deeds: `oath_kept`, `oath_broken`, `debt_repaid`, `meal_taken`, `meal_shared`, `guest_honoured`, `guest_wronged`, `blood_given`, `blood_refused`, `tree_felled`, `sacred_harmed`.

**Adding a sign** (an animal seen, a snake bite) takes every one of these changes on milestone-0:
1. The `sign` enum at content/schemas/domains.schema.json:21. This is a schema change, so ask first.
2. The `Signs` list at sim/Content/RequiredContent.cs:74.
3. A call to `Culture.Omens.OnSign(w, "<sign>", e)` right after the event, as the existing five do: IncidentSystem.cs:128 (rain), 161 (food spoils), 172 (work accident), 211 (windfall) and Lifecycle.cs:28 (death).
4. A loc key `sign.<sign>`, such as "the rain" (culture:179).
5. Entries in content/domains.json under `signs`, each reading `anger` or `blessing`.

**Adding a deed** (a hunt, a sacred animal killed):
1. The `deed` enum at domains.schema.json:32. This is a schema change.
2. Append to the `DeedKind` enum at sim/State/Culture.cs:71-84.
3. The `Deeds` list at RequiredContent.cs:67-71.
4. A call to `Omens.RecordDeed(w, DeedKind.X, eventId, doerId)` where the deed happens. Today's 11 calls: Relations.cs:131 and 147; CampJobs.cs:64; Promises.cs:23; NeedsJobs.cs:37; BloodJobs.cs:134 and 157; CareJobs.cs:103; Hospitality.cs:81 and 82; SacredTrees.cs:46.
5. `CultureTuning.DeedWeightPct`, if the weight is not 100.
6. A loc key `deed.<deed>`.
7. Entries in domains.json under `pleasedBy` or `angeredBy`.

**Related precedents and plans:**
- **Deeds with a parameter:** "Teaching an idea is a deed ... `idea_taught:<idea>` ... Domains may list `idea:<idea>` in `pleasedBy` or `angeredBy` (the deed enum becomes a pattern then)" (culture:430).
- **The binding rule:** "a Culture event sits between its cause and its consequence" (culture:620).
- **Planned, not built:**
  - M3, gods who stir (culture:503);
  - G6, learning by living: five days among a culture teaches its greeting (culture:448).

### 1.7 Peoples' natures that animals or abilities touch

**Vampire**
- The rule: "Blood comes only from a consenting human donor (the donor loses health and gains fatigue; the vampire owes the donor a debt). A non-consenting human cannot be fed on" (BIBLE:61).
- Why it matters here: animal blood would get round consent, the engine of the stories strangers retold unprompted at the M0 exit check (STORIES:12, 45).
- Its numbers: food source blood; bloodPerFeed 700; donorHealthCost 130; donorRestCost 170; sun burn 60 an hour (species.json).
- Donors refuse when hungry, when weak (health below max(600, their risky-work line + 100)), in a mood below 35, or when their opinion is below their consent bias. After a refusal the vampire waits 2 hours before asking anyone else (BloodJobs.cs:59-61, 163; DECISIONS:94).

**Dryad**
- No food. The forest feeds her: living trees within reach, her own tree counting triple. She never fells a tree, and every felling grieves her.
- Her own tree felled: she is wounded (Dice `TreeWound`), forms a grudge, and the bond ends (`BondSevered`). A lone tree restores 25 an hour against a decay of 20 (BIBLE:66; DECISIONS:203, 240; needs.json, `forest`).
- Why it matters here: the forest is a habitat, and trees never grow back (DECISIONS:211; build:381).

**Undead**
- No food and no sleep. Purpose ties each to one kind of work, a Truth. They "mend on their feet", and "The undead never bleed": wounds give grave dust (BIBLE:65; DECISIONS:202, 222, 229).
- Why it matters here: a body with no blood meets venom. A species-wide immunity would be a stat difference (fixed decision 5).

**Werewolf** (batch 2D)
- "as a wolf it hunts (later, with animals)"; "keen smell and hearing even in human shape (later, an early warning of a thief)"; "silver will harm it once items exist" (peoples:460).
- A changed member is treated as a raider (peoples:465, 504). Its evolutions are moon-tamed or wolf-blooded (peoples:507).
- Why it matters here: hunting, the wolf already drawn, and silver as a kind of damage.

**Holtward** (batch 6)
- "the hunt kept fair. *Twist:* fair hunters find game where it leads them; greedy ones walk in circles until dark" (Work (hunting), Zones, Social, Travel) (peoples:156).
- It looks like "a small figure in a cloak of living leaves, its face a mask of bark" (peoples:185).
- Why it matters here: it needs animals and hunting first (peoples:1108), and "greedy" needs some measure of over-hunting.

**Fetlow** (batch 3)
- A horse-headed trickster: "the night horse ridden with a bridle of its own hair"; "it gives a hair to whoever rides out a night with it, a pledge to come when called" (peoples:144).
- It looks like "a long horse-like head and a mane down the back" (peoples:173). It needs the items milestone and "a combat job beyond `Raid`" (peoples:1105).
- Why it matters here: horses, riding and tokens (peoples question 6).

**Centaur** (batch 5)
- "the only four-legged person" (peoples:180); "it carries no one it has not sworn to, so a carry to safety is an oath" (peoples:151).
- Why it matters here: horses that carry anyone would sit beside this twist.

**Beings that are only threats**
- "the ford-horse: a water horse that carries its riders into the river"; "the canopy-hunter: hangs from forest trees and hooks passers-by"; also the chest-sitter, the name-caller and the frost-knocker (peoples:200-208).
- Why it matters here: wild dangers by habitat, which must be derivatives (section 4.5).

**Smaller peoples**
- Brimmet (2A): "fish and cress, its delights, come with items"; "sharp-eyed in water, dull in dust" (peoples:270). Fish, later.
- Ghoul (2E): it "fears fire, being hunted" (peoples:326).
- Knollet (2C): its curse drains health and rest every hour until amends are made; `DiceId.CurseRoll` (peoples:562). A precedent for damage over time and its cure.
- Feeplin (2C): whistle-speech is learned by experience (`understands: [id]`) (peoples:614). A precedent for learned know-how.

**Rules for every people**
- "**Stat differences.** No people is stronger, faster or smarter as a number. Every species keeps `ticksPerTile` 6." (peoples:79). This bites on mounts (speed) and on tolerances a people would be born with.
- Harms become a list per species: `petrify`, `heat`, `iron` (peoples:781). This is the vocabulary for kinds of damage.
- Threats: "In code, threats take the thief's path" ... "Threats that are not raids ... wait for later slices, which extend `CombatSystem` beyond `Raid` and `Flee`." (peoples:193-194). A biting animal is not a raid.

---

## 2. (b) Every claimed number

### 2.1 TruthIds

TruthIds feed the Truths hash: never renumber or reuse one (ADR-0004, DECISIONS:29).

| Range | Owner | In use | Status | Source |
|---|---|---|---|---|
| 1-8 | M0 | Terrain 1 … PairChemistry 8 | built, milestone-0 | DrawIds.cs:9-16 |
| 9-19 | next to M0's block | — | unclaimed (world:367 gives M0 as 1-8) | — |
| 20-29 | Growth (ADR-0021) | InitialWant 20, WantTarget 21 | built, milestone-0 | DrawIds.cs:18-22; DECISIONS:176, 193 |
| 30-39 | Peoples batch 1 (ADR-0022) | BoundTree 30, PurposeWork 31, BargainService 32, BargainPrice 33 | built, milestone-0 | DrawIds.cs:23-31 |
| 40-49 | Culture-faith (ADR-0023) | CultureNature 40, FaithNature 41, GodNature 42, PersonBelonging 43, SacredTree 44, FestivalDate 45; 46-49 left in the track | built, milestone-0 | DrawIds.cs:32-42; DECISIONS:247 |
| 50-59 | Peoples batch 2 | planned "in landing order, from 50": CastPresence (2B), Mounds (2C), MoonPhase (2D), OldGraves (2E), EthicsLeaning, WandererSpecies; 2A takes none | claimed in spec | peoples:967, 972-973 |
| 60-69 | "Build, zones (left free by schedule §6.1; unclaimed)" | none: Build and Zones added no Truth | reserved in the table, unused | world:372; build:564; zones:556 |
| 70-79 | Schedule (ADR-0029) | Chronotype 70, only if schedule question 6 is answered yes; the code says "None drawn yet" | claimed; comment on WT-zones | schedule:604, 930; WT-zones DrawIds.cs:43 |
| 80-89 | Player (ADR-0030) | PlayerPeople 80, PlayerGift 81, AdultAge 82, Lifespan 83, SoulSpan 84; 85-89 reserved for later gift details | built on WT-player | WT-player DrawIds.cs:43-53; player:514 |
| 90-99 | — | — | unclaimed | — |
| 100-109 | the loot design's early draft | — | withdrawn | loot:370 |
| 110-119 | — | — | unclaimed | — |
| 120-149 | World | W1 uses 120-129 (WorldTerrain 120 … ArrivalRegion 129); 130-149 reserved for successors, the rivals' timetable and the storyteller's personality | claimed in spec | world:375, 381-391 |
| 150-159 | Loot-economy | Reckoning 150, MarketNature 151, PeddlerNature 152, RuinsCache 153, RelicPast 154, DungeonNature 155, SeekerNature 156 | claimed in spec | loot:370-378 |
| 160 and up | — | — | unclaimed; no document or DrawIds.cs names 90-119 or 160-179 as a TruthId (grep) | — |

**Two Truths already promised with no range assigned:**
- Peoples question 8: per-person variation in how fast needs drain. The design called it `NeedTolerance`; the recommendation is "one change for every need, food and rest included, after 2A", and ADR-0025 #5 accepted it (peoples:972, 1146-1148).
- ADR-0018's seam: per-person appetite and temperament (DECISIONS:136).

### 2.2 DiceIds

DiceIds are tags only: "A `DiceId` is only a tag: `DiceStream.Range` records it for diffing, and it never reaches the generator" (schedule:607).

| Range | Owner | Values | Status | Source |
|---|---|---|---|---|
| 1-23 | M0 | ArrivalDay 1 … ArrivalOrder 23 | built | DrawIds.cs:48-70 |
| 24-99 | — | an early schedule draft took 24 and 25 | unclaimed; the 24 and 25 claim is stale | zones:557 |
| 100-119 | Growth M1 | none yet: "reserved for M1's mutation and evolution rolls"; the peoples' evolution paths use it too | reserved | DECISIONS:180; peoples:980 |
| 120-139 | Peoples batch 1 | PeopleArrivalDay 120, PeopleArrivalHour 121, TreeWound 122 | built | DrawIds.cs:72-77 |
| 140-159 | Culture-faith | FauxPas 140, GuestOffer 141, OmenRead 142 | built | DrawIds.cs:78-81 |
| 160-179 | Peoples batch 2 | CourtesyBow 160 (2A); later ContestBout and RepayAgain (2B), PrankKind, CurseRoll and Windfall (2C), MoonChange (2D) | claimed in spec | peoples:967, 979-980 |
| 180-199 | Zones (ADR-0028) | ZoneDefy 180, on the Work stream; "Nothing draws it yet" | built on WT-zones | WT-zones DrawIds.cs:84-86 |
| 200-219 | Schedule (ADR-0029) | DayChangeDefy 200, WatchDefy 201, on the Work stream | built on WT-zones | WT-zones DrawIds.cs:88-92; schedule:596-604 |
| 220-239 | Player (ADR-0030) | FateKnotTail 220, FateKnotPick 221, LuckVictim 222; 223-229 for anomalies; 230-239 for later gifts | built on WT-player | WT-player DrawIds.cs:93-100 |
| 240-259 | the loot design's early draft | — | withdrawn | loot:370 |
| 260-299 | — | — | unclaimed; nothing names 260-299 (grep) | — |
| 300-339 | World | on Politics: CourtAct 300, ActTarget 301, NotableDeath 302; on War: ClashRound 310, NamedBlow 311, BlowDamage 312, RaidSize 313, RaidHour 314 | claimed in spec | world:393-395 |
| 340-349 | — | — | unclaimed (a narrow gap) | — |
| 350-369 | Loot-economy | 350-358 for packs, rarity, base, qualities, values, names, heirlooms and finds; 359-365 for price drift, shocks, the peddler and seekers; 366 LootKeep and 367 TradeOwnWay, compliance rolls on the Work stream | claimed in spec | loot:379-381 |
| 370 and up | — | ventures "take their ids with their own sign-off" | unclaimed | loot:385 |

### 2.3 DiceSystem streams

Stream numbers must run from 0 with no gaps.
- `DiceRng.SeedStreams` throws "DiceSystem values must be contiguous from 0" (sim/Core/DiceRng.cs:96-97).
- Each stream is seeded from `Mix64.Combine(DiceSeed, (ulong)system)` (:98), so a stream's number is also its seed.

| Value | Stream | Status | Source |
|---|---|---|---|
| 0-7 | World, Needs, Mood, Social, Work, Travel, Incident, Combat | built | DrawIds.cs:87-94 |
| 8 | Growth (seeded, never drawn) | built, milestone-0 | DrawIds.cs:95-96; DECISIONS:180 |
| 9 | Culture | built, milestone-0 | DrawIds.cs:97 |
| 10 | Fate ("The one new stream of this wave"); the design's separate Gift stream is folded into it | built on WT-player | WT-player DrawIds.cs:117-121; WT-player DECISIONS.md:338 |
| next free at merge | Economy | claimed: "takes the next contiguous value at merge" | loot:387 |
| next free at merge | Politics and War | claimed: "Each new system takes the next contiguous value at merge" | world:394-396 |
| next free at merge | Build (storms and fire, in slice 2) | claimed as "`DiceSystem.Build = 8`", which is stale because 8 is Growth | build:570 |
| — | Schedule = 8 | withdrawn; the schedule's rolls use the Work stream | schedule:605-609 |
| — | Gift | folded into Fate | ADR-0030 #1 |

- **Streams cannot be pre-assigned in blocks.** "A `DiceSystem` cannot be pre-assigned in blocks ... its number, and every roll in it, would depend on merge order" (schedule:609).
- **There is a precedent for landing out of order.** ADR-0023 added a seeded, never-drawn `DiceSystem.Reserved8` placeholder: "replace it with that branch's system at merge (if both survive, the contiguity check throws at startup, so a bad merge cannot pass silently)" (DECISIONS:247).
- **WT-player sets the next number:** "A sibling branch that adds a Dice stream must take the next value after `Fate = 10`" (WT-player DECISIONS.md:369).

### 2.4 SystemTag and EffectKind

**SystemTag.** Snapshots store tags by name, so "order only matters to the story analyzer's bitmask, which is order-free" (DECISIONS:278).

| Source | Tags |
|---|---|
| milestone-0 | Needs, Mood, Social, Work, Travel, Incident, Combat, World, Growth, Culture (sim/Events/SimEvent.cs:6-20) |
| WT-player adds | Fate, Gift (WT-player SimEvent.cs:21, 23) |
| WT-zones, WT-build | none added (checked) |
| claimed in specs | Economy (loot:10, 179); Politics and War (world:312) |

**EffectKind.** An event that carries one can end a story (ADR-0006).

| Source | Effects |
|---|---|
| milestone-0 | GrudgeFormed, GrudgeEnded, DebtFormed, DebtRepaid, DebtBroken, Death, Departure, Recruitment, BondSevered, ContractFormed, ContractKept, ContractBroken (SimEvent.cs:23-41) |
| WT-player adds | GiftGrew (WT-player SimEvent.cs:48) |
| claimed in world | OathSworn, OathBroken, RulerChanged, WarBegan, WarEnded, SettlementBurned (W1); HoldingTaken (W3) (world:314) |
| claimed in culture-faith | Converted, in slice 3 (culture:541) |
| loot | "No new `EffectKind`" (loot:334) |

### 2.5 ADR numbers

| ADR | What | Where | Status |
|---|---|---|---|
| 0001-0026 | everything through the founder's sign-off of the World, Player and Loot designs | milestone-0 DECISIONS.md | merged |
| 0027 | Build, slice 1a | WT-build DECISIONS.md:334 | on a branch |
| 0028 | Zones slice 1, steps 1 and 2 | WT-zones DECISIONS.md:334 | on a branch |
| 0029 | the Schedule | only in code comments on WT-zones (DrawIds.cs:43 and 88; ScheduleSystem.cs:8; WorldGen.cs:42; and others); its DECISIONS.md has no heading for it yet | on a branch, in progress |
| 0030 | Player, slice 1 | WT-player DECISIONS.md:334 | on a branch |
| stale | the schedule design calls itself "ADR-0026, the number pre-assigned to this track" (schedule:6, 604) and assumed build and zones would take 0024 and 0025 (schedule:675). Founder ADRs overtook all three numbers | spec | stale |

Pre-assigned ADR numbers have already collided once. ADR numbers do not feed any hash, so a collision costs only edits, unlike a TruthId.

### 2.6 Content files, loc files and SchemaVersion

**Loc files:**
- milestone-0: `en.json`, `en.log.json`, `en.ui.json`, `en.peoples.json`, `en.culture.json`.
- On branches: `en.build.json` (WT-build); `en.zones.json` and `en.schedule.json` (WT-zones); `en.fate.json` (WT-player).
- Claimed in specs: `en.economy.json` (loot:363) and `en.world.json` (world:351).
- Each track keeps its text in a file of its own, "so en.json is untouched for the parallel branches" (DECISIONS:201).

**Content files:**
- milestone-0: cast, cultures, customs, domains, faiths, incidents, items, needs, roles, species, thoughts, traits, wants, work, world.
- On branches: `buildings.json` (WT-build); `zones.json` and `rhythms.json` (WT-zones); `gifts.json` and `rebirth.json` (WT-player).
- Claimed in specs:
  - qualities, relics, loot, markets and shocks (loot:10);
  - worldgen, terrain, sites, governments, oaths, legends and names (world:340-348);
  - `acts.json` (peoples:818);
  - later, `dungeons.json` (loot:339);
  - `trait_mutations.json`, `evolution_paths.json` and `arc_incidents.json` (BIBLE:135).
- No file is named abilities, animals, wildlife or habitats anywhere.

**SchemaVersion:**
- milestone-0 is at 2 (sim/State/World.cs:16). WT-build, WT-zones and WT-player each set 3.
- WT-player: "one that bumps `SchemaVersion` should go to 4" (WT-player DECISIONS.md:369).
- ADR-0028's merge notes: "the second to land raises it again".

### 2.7 Stale or colliding claims found

| Claim | Problem |
|---|---|
| build:570, `DiceSystem.Build = 8` | 8 is Growth on milestone-0 |
| schedule:6 and 604, "ADR-0026" | 0026 is the founder's sign-off; the Schedule became 0029 |
| zones:557, "Schedule's doc takes 24 and 25" | the Schedule took 200-201 |
| culture:589, thirteen TruthId names | ADR-0023 folded them into six (DECISIONS:247) |
| loot:370 says the world and player tables "still show" its withdrawn 100-109 and 240-259 | they no longer do (world:376; player:520) |
| world:378 says the player and loot designs "quote earlier world ranges" | both now quote 120-149 and 300-339 (player:520; loot:370) |
| player:13 and 517, `DiceSystem.Gift` | folded into Fate (ADR-0030 #1) |
| `DiceSystem.Growth = 8` | also claimed as `Build = 8` and `Schedule = 8`; ADR-0021 already logs this (DECISIONS:193) |

### 2.8 Proposed free numbers for the two designs

| Kind | Abilities and stats | Wildlife | Why |
|---|---|---|---|
| TruthIds | 160-169 | 170-179 | Past every claim (the world reserves up to 149 and loot ends at 159), as the loot design did. Nothing names them. Alternatives: 90-99 and 110-119, both unclaimed (100-109 carries a withdrawn claim). |
| DiceIds | 260-279 | 280-299 | An unclaimed gap between the withdrawn 240-259 and the world's 300-339. Nothing names them. 370 and up may go to loot's ventures. |
| Dice stream | `DiceSystem.Ability` | `DiceSystem.Wildlife` | Names only. Each takes the next free number at merge, after Fate = 10 and whichever of Economy, Politics, War and Build land first. If both landed straight after WT-player they would be 11 and 12. If one lands ahead of another stream, use ADR-0023's placeholder pattern. |
| SystemTag | `Ability` | `Wildlife` | Singular, like Gift and Fate. Appended at merge (order does not matter). |
| EffectKind, if any | for example `AbilityGrew` (A the person, RefId the level), after `GiftGrew` | none proposed | See section 5, trap 9: story inflation. |
| ADR | 0031 | 0032 | The next numbers after 0030. If the founder's answers get a sign-off ADR of their own, as in 0025 and 0026, that takes 0031 and the designs take 0032 and 0033. Confirm at merge. |
| Loc file | `content/loc/en.abilities.json` | `content/loc/en.wildlife.json` | Follows the `en.<track>.json` pattern. |
| Content file and schema | `content/abilities.json` + `schemas/abilities.schema.json` | `content/animals.json` + `schemas/animals.schema.json` | Every content file needs its schema and every schema its file (ADR-0021 #12, DECISIONS:182). |

**Two options worth putting on the table:**
- **The Abilities design may need no stream at all.** That holds if a level comes from counting events, as with gifts, and the outcome roll (did you survive the bite?) belongs to the stream of whatever deals the harm. Accident damage is on Incident and blows are on Combat; a bite would be on Wildlife. Then the ability system draws nothing. Three tracks shipped that way:
  - bleeding draws nothing ("no draw", ADR-0030 #11);
  - gift growth draws nothing (only Fortune's favourite's tie-break uses Fate);
  - Build: "no new draw, no new `DiceSystem`" (WT-build DECISIONS.md:352).
- **Or treat ability gains as growth** on `DiceSystem.Growth = 8`, which exists and draws nothing today. The catch: DiceIds 100-119 on it are reserved for M1's mutation and evolution rolls (DECISIONS:180).

---

## 3. (c) Where the repo waits for these two designs

### 3.1 The negative result first

I ran `grep -rn -i -E "\b(poison|poisoned|poisons|venom|venomous|toxin|toxic|snakes?|serpents?|antidote|tolerance)\b"` over BIBLE.md, DECISIONS.md, STORIES.md, CLAUDE.md, `docs/` and `content/`, and over each worktree's BIBLE, DECISIONS, STORIES and content.
- It found nothing but two "snake_case" mentions (build:535, culture:349) and the excluded "Rainbow Serpent" (peoples:106).
- No document waits for poison or venom.
- The word "tolerance" appears only as `NeedTolerance` (peoples:972, 1146).

### 3.2 Animals, hunting and habitats

| Where | What it says |
|---|---|
| zones:30 | "**Growing plots** wait for the items milestone, and **grazing** waits for animals." |
| zones:127 | Grazing zone: "Animals graze here" … "Wolves at the edge of the home area" … "After animals exist" |
| zones:198-199 | "Grazing needs animals, and the sim has none. When animals come, the home area is where they spend the night, and wolves at its edge are the story." |
| zones:513 | the zone `behaviour` enum: "`stores \| gather \| home` in slice 1. Slice 2 appends `keep_out`, `leave_be` and `burial`; later, `grow` and `graze`." |
| zones:818 | "**Later:** growing plots (items milestone, section 3.6), grazing (animals), delegated zones (kingdom layer)." |
| WT-zones BIBLE.md:225-227 | section 7.5, "Not yet": "growing plots (after the items milestone) and grazing (after animals)" |
| schedule:774 | "Hours on zones ('nobody in the forest after dark') are a later joint feature." |
| peoples:460 | the werewolf: "as a wolf it hunts (later, with animals)"; "keen smell and hearing even in human shape (later, an early warning of a thief)"; "silver will harm it once items exist" |
| peoples:156 and 1108 | the holtward ("Work (hunting), Zones, Social, Travel"); batch 6's "Needs first": "omens (culture-faith); animals and hunting; constructs; night building" |
| peoples:144, 1105 | the fetlow (horses, riding): batch 3 needs "the items milestone ...; a combat job beyond `Raid`" |
| peoples:151, 1107 | the centaur: batch 5 needs Build's rooms and doors, among other things |
| peoples:193-194, 307 | "Threats that are not raids ... wait for later slices, which extend `CombatSystem` beyond `Raid` and `Flee`"; the brimmet at the ford "needs a combat job beyond `Raid` (later)" |
| peoples:200-208 | threat-only beings: the ford-horse, the canopy-hunter and others |
| peoples:270 | the brimmet: "fish and cress, its delights, come with items" |
| DECISIONS:63 | ADR-0009: "Monsters in the same language: thief, wolf, ghoul, and bosses at twice a person's size (the Barrow King)." |
| DECISIONS:149, 159 | "People and animals are procedural (... figureB, wolfB and kingB)"; "The wolf and the Barrow King are drawn but unused until the sim has them." The code: `game/scripts/Art/Figures.cs:1029` `Wolf(Brush b, Facing f, float phase, ArtTextures tex, float shadowAlpha)` |
| world:100, 165, 343 | the world map: "Olive plains, near-black forest, slate water, worn tracks"; terrain is banded into `terrain.json` kinds with fields `travelHours`, `settleWeight`, `harvestDays` and elevation and moisture bands. These are hooks for habitats on the world map |
| world:167 | "2–4 holdings per realm ...; 0–3 free villages; ruins and lairs." |
| world:488 | W2 builds "road encounters", among other things |
| world:16 | "Everyone else in the world is a number": off the camp map, animals would most likely be counts |
| build:381; DECISIONS:211; player:593 | trees do not grow back (until build slice 2), so the forest habitat shrinks over a year |
| loot:339, 341 | dungeon guardians "reusing the peoples' threat forms (the barrow lord; the art's Barrow King, drawn and unused), each 60% stronger than the one above"; each level is played "in rounds under the combat rules" |

### 3.3 Abilities, growth and stats

| Where | What it says |
|---|---|
| BIBLE:125 | evolution "Changes subspecies, role, abilities and sprite parts", yet no ability system exists |
| BIBLE:124, 135, 141, 156 | trait-change tables, `trait_mutations.json` and `evolution_paths.json` wait for M1; "Not yet: no system mutates traits or advances arcs" |
| BIBLE:52 | Growth "writes back only beats, and M1's trait changes close the loop" |
| DECISIONS:180 | `DiceSystem.Growth = 8` and DiceIds 100-119 are reserved for M1 |
| DECISIONS:303 | "A unique-ability system is needed (content-defined, one rolled per run). Each ability must make stories by touching at least two systems, not add a flat stat bonus." Partly met by the gifts on WT-player |
| player:166-188 | gifts waiting on later features: Take the wound (slice 2), Red haze (later), Name-giver and Talent sight (growth M1), Maker's hands ("those beside you learn"; Build, M1), Every tongue, Wayfinder (W2) |
| player:333 | decline in slice 2 ("slower rest"), which is BIBLE 5.1.7's negative growth |
| peoples:614 | the feeplin's learned whistle-speech: a know-how earned through experience, waiting for a mechanism |
| peoples:507 and others | evolutions that change what a person can do (moon-tamed, wolf-blooded) |
| peoples:1015; culture:1154; zones:652; build:649; loot:413; world:435 | experiences offered to M1's trait-change tables |
| peoples:972, 1146-1148; DECISIONS:136 | per-person need drain ("tolerance") and per-person appetite: new Truths, accepted but not built |
| schedule:930 | a fixed body clock, `TruthId.Chronotype = 70`: an inborn trait per person, as a Truth |
| culture:448 | G6 "Learning by living" (slice 2): five days beside a culture teaches its greeting without a breach |

### 3.4 Combat, harm, resistance and stat scaling

| Where | What it says |
|---|---|
| peoples:781 | harms as a species list "when batch 5 brings the next one" (`petrify`, `heat`, `iron`) |
| peoples:841 | `NatureNeeds.FightHitPct` multiplies the chance to hit (weakness) |
| peoples:562 | the knollet's curse: harm every hour until amends (2C) |
| player:464-470; ADR-0030 #11 | the bleed, a damage-over-time precedent with no draw |
| player:205-208 | Fortune's favourite turns harm aside before it lands, a hook in the damage path |
| loot:187-197 | the closed list of thing modifiers has nothing for resistance or defence; "Fields read with `Max` ... are closed to things" (loot:185) |
| loot:262 | the black ring doubles the accident threshold, a multiplier on odds |
| loot:269 | silver "wards against shapeshifters" and salt "wards off the restless dead" (slice 3), hooks for vulnerability and warding |
| loot:339 | guardians "each 60% stronger than the one above", a scaling that has no stat model yet |
| loot:84, 288, 458 | plague comes with the world layer |
| world:240-242 | raids: "each side loses up to a quarter of the other's strength, and walls halve the defenders' losses"; a side breaks at half its strength (a Proud leader at two thirds, an Anxious one at a third); named leaders take blows at 150‰ for 60-140 |
| build:783 | "Storms, which damage roofs and walls" (slice 2) |

### 3.5 What blocks hunting for food, and riding

| Where | What it says |
|---|---|
| DECISIONS:133 | ADR-0018 #5: "Food is berries-only until the items milestone: a species that eats another item is rejected at load with a pointer here, instead of starving in play." |
| loot:8 | "Food stays berries-only through slice 1 ... Food that keeps and food that can be bought come in slice 3, under Zones §3.6's balance guard." |
| zones:187-196 | Wait for the items milestone; the balance guard for it keeps hunger-chain stories within ±20%, went-without events above half, and meal theft → grudge in at least 50% of worlds |
| DECISIONS:331 | ADR-0026 #5: "no food sold to the camp before the food milestone" |
| culture:1145 | food taboos (F5) come after the items milestone |
| peoples:79 | "Every species keeps `ticksPerTile` 6"; the closed list of thing modifiers has no travel-speed modifier (loot:187-197) |

---

## 4. (d) Constraints that bind both designs

### 4.1 Fixed decisions (CLAUDE.md)

- **1** (CLAUDE:12): "Story generator first. A feature exists only if it makes players retell unplanned stories."
- **2** (CLAUDE:13): "Intimacy at every scale. The player manages 5–20 named companions who become the court; the kingdom is governed only by delegation through them. Never a spreadsheet view. Never direct unit control."
  - What this means here: hunting, herding and riding go through work priorities, zones and standing orders. Refusals stand, and ADR-0025's compliance rule applies (DECISIONS:314).
- **4** (CLAUDE:15): "Truths vs Dice. World facts seeded once per playthrough (map, hidden truths, characters' natures, the player's race and unique ability, rival otherworlders' arrivals, gods, the civilizations that exist when you arrive) survive rewinds; events re-roll after every rewind."
- **5** (CLAUDE:16): "Species from many mythologies differ in NEEDS, not stats (vampires: blood; dryads: forest; djinn: contracts; fae: cannot lie; undead: purpose). Ethics axes: Mortal/Immortal, Arcane/Mundane, Wild/Civil, Oathbound/Free, on individuals and factions. Intermarriage produces procedural hybrids via trait inheritance."
  - The peoples design restates it: "No people is stronger, faster or smarter as a number." (peoples:79)
- **10** (CLAUDE:21): "Characters: RimWorld-grade needs, mood and traits, plus a relationship graph where grudges, debts, oaths and ambitions are first-class objects. Dialogue is templated in v1; any generative text must derive from stored state."
- **12** (CLAUDE:23): "Character growth and arcs: companions visibly change over a lifetime (body, role, traits, heart); static pawns are a failure state. Growth is event-driven, never grind-driven, and every change is a logged story beat with a cause ... Truths keep a character's nature; Dice re-roll their experiences."
  - With BIBLE:148: "No growth from grind: every trait change references an incident id."
- **The cut rule** (CLAUDE:46): "A system must interact with at least two others or it is cut."

### 4.2 ADR-0024 and the gift rule, beside the founder's percentages

- **ADR-0024** (DECISIONS:303), verbatim: "A unique-ability system is needed (content-defined, one rolled per run). Each ability must make stories by touching at least two systems, not add a flat stat bonus."
- **The gift rule** (player:151), verbatim: "**Never a number.** A gift touches at least two systems, and every event it makes cites its cause."
- **The founder's numbers** (brief): "a mathematical system of probabilities of survivng a snake bite starting at 20% and if you survive you get snake poison tolerance lvl 1 which makes survival rate 25% and then you can get to enough lvls and a 100% surival rate and you can levle past it like when you go to 125% tolerance the remaining 25% applies to any kind of abilities anyone else has that reduces tolerance"; and "take inspiration from all the stats and math path of exile does".

**How the repo already holds numbers in state while showing words:**
- loot:87: "Qualities are words ('keen', 'keeps you dry')"; "no damage numbers, no '+3% work speed' lines".
- culture:41-42: "no stat sheet ... Devotion reads as a word, never a number"; "No culture as a stat bonus ('+10% work speed')".
- BIBLE:166: devotion shows as lapsed (under 30), faithful, or devout (70 and over).
- BIBLE:190: the inspector's belonging line has "No numbers."
- world:88: "A realm's strength, food, opinions and armies reach the player only as words; the one count shown is the dominion count."
- player:303: Distortion (0-1000) "shows in words, never as a bar".
- player:383: "A human sees 'Age 23 · Humans here live to about 70'."
- **But digits do appear where they are counts:** "holds 3 meals" and a remembered feeling's "(−10)" (player:377, 380).

**The rule the repo applies to numbers:** numbers live in content `params` and in state (gift tiers, quality tiers), and every feature must still touch two systems and cite causes. A tolerance that touches only health would be exactly ADR-0024's "flat stat bonus".

### 4.3 Truths and Dice

ADR-0004 (DECISIONS:27-35):
- "**Truths is stateless and hash-derived:** `value = hash(worldSeed, truthId, entityId, index)` ... it survives rewinds by construction."
- "**Dice is stateful:** PCG32 substreams, one per system ... A rewind reseeds `diceSeed`."
- "Draw tags are enums with fixed numeric values (`TruthId`, `DiceId`, `DiceSystem`), never strings. Truth ids feed the hash: never renumber or reuse one."
- "Simulation state uses integers. No floating point in state or decisions."
- "Never iterate a `Dictionary` or `HashSet` where order affects an outcome."
- "Player input is a tick-stamped command queue."
- The consequence it states: "Adding a draw to one system cannot shift another system's sequence."

Practice since then:
- **Key Truths by a stable string.** Use a cast key or a template id, never an entity id (DECISIONS:176, 249; world:163).
- **Create new entities last** in `WorldGen.Create`, "so no id handed out before them moves" (WT-zones ADR-0028 #2; ADR-0027 #3), or give them their own `NextId` (`EconomyBook`, loot:164; `RealmBook`, world:191).
- **Prove the empty step changes nothing.** A first step with no behaviour must leave all 100 soak logs byte-identical (ADR-0022 #9; loot:388; world:450).

For these designs:
- What a person gains by experience is Dice-driven world state, so it goes back with a return.
- A nature (an inborn aptitude, if any) would be a Truth keyed by cast key.
- Facts of the map, such as where a den or nest lies, would be Truths, so a player who dies can learn them. Compare "a black ring lies in the ruins" (loot:391).

### 4.4 The "Ask before" list, and what these designs would trigger

- CLAUDE:48: "Ask before: changing a fixed decision, adding a dependency, changing a content schema, or touching anything that affects determinism."
- ADR-0007: "Changing a schema needs the founder's approval" (DECISIONS:53).
- ADR-0003: "Any further package needs a new ADR and the founder's approval" (DECISIONS:22).

What these designs are likely to touch:
- **New content files with schemas:** abilities, animals.
- **Schema changes:**
  - `domains.schema.json` for a new sign or deed (section 1.6);
  - `species.schema.json`, if harms or innate fields arrive;
  - `traits.schema.json`, if abilities write new trait fields;
  - `items.schema.json`, if remedies or antidotes arrive;
  - `zones.schema.json`, if `graze` arrives (WT-zones).
- **Determinism:**
  - new TruthIds, DiceIds and streams;
  - animal entities on the camp map, which change every world's draws and ids;
  - new SystemTags and EffectKinds, which change story counts;
  - a SchemaVersion bump.
- **Possibly a fixed decision:** fixed decision 5, if any people is given an innate tolerance.
- **Possibly a founder decision:** lifting ADR-0018 #5 (berries-only food) for hunting.
- Every earlier design ended with a founder question for exactly this: "Approve the schema and determinism changes" (culture:1167; loot:464; player:602; world:499; peoples:1125).

### 4.5 Content rules for derivatives (ADR-0025)

- ADR-0025 #1 (DECISIONS:313): "**Peoples are our own derivatives.** Beyond the archetypes fixed decision 5 names (vampire, dryad, djinn, fae, undead), each people is an original creation inspired by Earth folklore, with its own name, look and twist, and never a reproduction of a tradition's being. There are no paid tradition readers. The content rules stay: no derivative of anything sacred or venerated, or of a belief still used to harm real people, and no caricature. The exclusions in the peoples design apply to derivatives too. An Earth-born player may notice a resemblance ('Earth folklore tells of something like them')."
- **The peoples design's rules** (peoples:96-114):
  - its own name, look and twist;
  - motifs, not a being;
  - nothing prayed to or venerated, and no scripture as a mechanic;
  - "**Peoples, not individuals; folklore, not cryptids.**" (rule 8).
- **Exclusions that bear on beasts** (peoples:106-108):
  - "the *thunderbird*"; "the *nāga*, *garuda* and *yaksha*, venerated in Hindu, Buddhist and Jain practice";
  - "the bunyip and the Rainbow Serpent, which belong to Country and law";
  - "Inari's messenger foxes (folk foxes stay)";
  - "fox possession, and the 'fox-keeping' families of Japan".
  - So no great serpent may be a thin rename of these.
- **Common real animals are not folklore beings.** The deer, rabbits and horses the founder named are outside ADR-0025's derivative rule, which governs peoples and beings drawn from myth. That reading is mine and the founder should confirm it. Any mythic beast is a derivative.
- **Dark content limits:**
  - ADR-0025 #4: "Blood-gifts, taboos, curses, grim rites and feared gods are welcome. For now there are no human sacrifice or torture rites, and nothing mirrors an Earth religion" (DECISIONS:316).
  - World: "No Earth realms, religions or ethnic stand-ins" (world:92).
  - Animal sacrifice is not addressed anywhere, so it is a question for the founder, not a settled rule.
- **War limits:** ADR-0026 #5: "war that plunders and burns and kills named people but has no massacres and no slavery" (DECISIONS:331).

### 4.6 Other rules any design here inherits

- **Every event's record** (BIBLE:7-12; ADR-0006, DECISIONS:47): the system, the causing events, the characters and the persistent effects.
- **ADR-0018 #1, cause honesty:** "An event cites only what brought it about" (DECISIONS:129).
- **ADR-0017 #1:** "One arc is one story" (DECISIONS:119).
- **ADR-0018 #4:** `RequiredContent` lists the ids and params the code names, and "An incident with no behaviour is rejected at load" (DECISIONS:132).
  - A new incident (a bite, an animal attack) needs a behaviour in `IncidentSystem.Fire` (IncidentSystem.cs:27-36) and its params in content.
- **ADR-0013:** tuning constants live in code (DECISIONS:87).
- **ADR-0017 #4:** a `_you_` form wherever the player can fill a slot (DECISIONS:122).
- **Text:** every player-facing string is a loc key (ADR-0007, DECISIONS:52; CLAUDE:31).
- **Text-state reads:** new state is readable through `StateApi`, with `CharacterState` fields appended last (pattern: DECISIONS:181, 248).
- **Budgets:** "at most 2 µs per character per tick" (ADR-0005, DECISIONS:42); "restore under 1 s at 200 characters" (CLAUDE:29).
- **Death guards:**
  - companions' deaths at most 2 a world-year (player:581);
  - Zones' cap of 0.1 member deaths a world-year (player:548);
  - culture-faith's vampire guard at 125% of the base (DECISIONS:264);
  - natural returns in 25-35% of worlds (player:581).
- **Story share caps of earlier tracks:** at most 35% of stories through Culture (DECISIONS:262) or through Economy (loot:450).

---

## 5. Traps and contradictions for the designers

1. **"Zones" is taken.** The founder's "type of zones (eg; forests etc)" means habitats or terrain. In the repo, a Zone is a painted player area (`content/zones.json`, with behaviours `stores`, `gather` and `home`; `graze` comes later, zones:513). Use "habitat" or "terrain" (`terrain.json` at the world grain, world:165).
2. **"Ability" is taken.** CLAUDE:4-6 and ADR-0024 say the player's "one unique ability". The player design calls it a "gift" (`gifts.json`, `SystemTag.Gift`, `EffectKind.GiftGrew`). Earned abilities need a word or a relation that keeps the rolled gift (a Truth) apart from earned growth (Dice-driven world state).
3. **"Tolerance" is taken.** Peoples question 8 is per-person variation in how fast needs drain, `NeedTolerance`, accepted but unbuilt (peoples:972, 1146-1148).
4. **"Level", "tier", "stage" and "depth" each already mean something.**
   - gifts: `tiers` with `at` in content, a "stage" in text and in `RefId` (player:492; WT-player gifts.json);
   - loot qualities: tiers I, II and III;
   - loot "depth" means PoE's item level (loot:102);
   - dungeon "levels" (loot:339).
5. **Fixed decision 5 against species immunity.** "Undead have no blood, so venom cannot touch them" would be a stat difference between peoples (CLAUDE:16; peoples:79). The loot design kept things inside the rule by making them "a trait that can be put down" (loot:185).
6. **The survival curve.** Read literally (20% at level 0, 5 points more per level, 100% at level 16):
   - expected bites survived before death: 0.27;
   - P(surviving 16 bites in a row): about 3.1 × 10⁻⁵;
   - P(surviving the first two): 5%.
   Set that against:
   - the founder's death rate for the player (ADR-0026 #2, about once every three years);
   - the companion-death guard (2 a world-year; measured 1.87, human worlds 2.25 on WT-player);
   - Zones' 0.1 member-death cap (player:548).
   Encounter rates and the curve would need tuning together. Tending or a remedy could raise survival, as tending stops a bleed.
7. **What a return does to tolerances.** Read beside player:278, "the body forgets what it endured but know-how returns twice as fast" leaves open whether endurance-type abilities get the half count. `(at + 1) / 2` rounding means stages at counts 1 and 2 gain little. Mastery today is one value, for the player's one gift.
8. **Grind.** "Increase the level ... by doing them more" (brief) sits against fixed decision 12's "never grind-driven" and BIBLE:148. Count discrete, cited, notable events (a life saved, a bite survived), never hours of work. That is what gifts do (player:155).
9. **Story inflation.**
   - If every level-up carries an EffectKind, as `GiftGrew` does, every level-up ends a story.
   - Growth beats today carry no effect on purpose (DECISIONS:179).
   - A new SystemTag also adds one more "system" to any chain it touches, and the analyzer counts stories with 2 or more systems.
   - ADR-0017 #1's "one arc is one story" decides how nested stories count.
10. **Consent and blood.** "Blood comes only from a consenting human donor" (BIBLE:61). Animal blood would undercut the vampire stories strangers retold (STORIES:12, 45; ADR-0018).
11. **Food.** Venison and rabbit are blocked by ADR-0018 #5 (DECISIONS:133) until the items or food milestone (loot:8). Zones' balance guard governs any food that changes winter (zones:187-196).
12. **Animals as `Character`s, or as something lighter.**
    - Today's statuses are OffMap, Member, Left, Dead and Hostile (Character.cs:6-13).
    - "Threats take the thief's path" (peoples:193), and a biting animal is not a raid (peoples:194).
    - Every Character costs budget (DECISIONS:42) and counts toward the 200-character restore test.
    - The analyzer needs a "named character" (CLAUDE:49), so an unnamed deer must not end stories as if it were a person.
    - Entities must be created last, or keep their own ids (section 4.3).
    - The art's wolf exists (Figures.cs:1029).
13. **Stream numbers depend on merge order** (DiceRng.cs:96-98; schedule:609). Fate = 10 is on a branch, and Economy, Politics, War and Build are pending.
14. **ADR numbers have collided before.** The Schedule was pre-assigned 0026 and became 0029 (schedule:6; WT-zones DrawIds.cs:43). SchemaVersion is 2 on milestone-0 and 3 on all three branches, so the next is 4 or higher (WT-player DECISIONS.md:369).
15. **Integer "more".** `ScalePct` truncates at every step (Traits.cs:33), and Path of Building rounds the "more" product to two decimals (ModDB.lua). An integer port needs a defined order and a rounding rule (DECISIONS:30).
16. **The overcap fork.** The founder's 125% matches PoE's reduction semantics: overcap absorbs curses and exposure. PoE also has penetration, applied after the cap, so overcap gives nothing against it, and a lowered maximum, which overcap cannot help with (`poe-defense.md` section 3.5). The design has to say which of these exist in Return.
17. **Signs and deeds are closed enums in a schema** (domains.schema.json:21, 32). A snake bite read as an omen, or a hunt that angers a grove god, is a schema change (section 1.6).
18. **Horses as mounts.**
    - Speed is a stat that peoples are denied (`ticksPerTile` 6, peoples:79), and no thing modifier covers travel (loot:187-197).
    - The centaur's twist is that "it carries no one it has not sworn to" (peoples:151).
    - Riding has to stay a person's own choice (fixed decision 2).
19. **The forest habitat shrinks.** Trees never grow back (DECISIONS:211; build:381). In ADR-0027's soak, the camp plan left about 7 of 24 trees by the year's end (6.7 on average; WT-build DECISIONS.md:354). Forest animals at the camp lose their habitat within a year.
20. **The holtward needs a measure of fairness.** "Greedy hunters walk in circles" (peoples:156) implies over-hunting is tracked, which means animal populations need state.
