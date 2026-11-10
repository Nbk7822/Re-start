# Path of Exile offensive math: research notes for Return

Research only, no design. Compiled 2026-11-09 for the ability-levelling and animals designs (founder request: "take inspiration from all the stats and math path of exile does when it calculates damage, resistances, etc").

**Sources and how much to trust them**

| Tag | Source | Use |
|---|---|---|
| [GGG] | Grinding Gear Games patch notes and staff posts (Mark_GGG, Bex_GGG, Rory and others), as cited by the wikis | Primary when a formula was announced |
| [W1] | poewiki.net (official community wiki, PoE1, current to patch 3.28) | Primary community source for PoE1. Raw wikitext read on 2026-11-09 |
| [W2] | poe2wiki.net (PoE2, current to about 0.4/0.5) | Primary community source for PoE2 |
| [PoB1] | Path of Building Community, `PathOfBuildingCommunity/PathOfBuilding`, branch `dev`, fetched 2026-11-09 (HEAD a69391d6daaa) | Second source: the community calculator, which carries constants mined from game data |
| [PoB2] | Path of Building for PoE2, `PathOfBuildingCommunity/PathOfBuilding-PoE2`, branch `dev`, fetched 2026-11-09 (HEAD 601768bd1058) | Second source for PoE2. `src/Data/Misc.lua` is auto-generated from game files |

pathofexile.fandom.com was not needed: poewiki.net had a page for every topic. Every formula below carries a URL. Where two sources disagree, both are given and the disagreement is flagged with **DISAGREEMENT**. Section 10 gathers them all.

---

## 0. Global rules that every later formula uses

### 0.1 Flat, increased, more (the core stacking rule)

- **Flat** stats ("Adds X to Y", "+X to") are summed.
- **Increased/reduced** stats are summed into **one additive pool** per calculation (reduced counts as negative).
- **More/less** stats each multiply **separately**. Internally their stat IDs end in `_final`.

```
Total = (Base + ΣFlat) × (1 + ΣIncreased/100) × Π(1 + More_i/100)
```

Order of application [W1 Stat]: 1. local flat, 2. local increased and more (on the item itself), 3. global and skill flat, 4. global and skill increased, 5. global and skill more. Each stat applies at most once per calculation. Converted damage "count[s] as both the Type converted from and converted to, but can only be affected by a particular stat at most once."

Worked example [W1 Stat]: 1000 base damage, 100% increased from gear and 100% increased from passives. Adding a gem with "100% increased" gives 1000 × (1 + 3.0) = 4000. Adding a gem with "100% more" instead gives 1000 × (1 + 2.0) × (1 + 1.0) = 6000.

Diminishing return [W1 Stat]: each +100% increased is worth less than the last (×2, then ×1.5, ×1.33, ×1.25). Each further 20% reduced is worth more than the last (×0.8 → 0.6 → 0.4 → 0.2 → 0).

- https://www.poewiki.net/wiki/Stat
- [GGG] Stats are always integers: Mark_GGG, https://www.reddit.com/r/pathofexile/comments/7zr0l0/enough_mice_can_kill_a_wolf/duqx1uv/
- [GGG] A modified stat is rounded down: Mark_GGG, https://www.pathofexile.com/forum/view-thread/46934/page/3#p2743595
- [GGG] Local versus global: Mark_GGG, https://www.pathofexile.com/forum/view-thread/84366#p940702

**PoE1 vs PoE2:** the same in both. [W2 Receiving_damage] adds that several "more" values from one source stack additively (e.g. "10% more Damage per Stage" at 5 stages is 50% more, not 1.1^5): https://www.poe2wiki.net/wiki/Receiving_damage

### 0.2 Rounding and integer facts

| Fact | Value | Source |
|---|---|---|
| Stat values | integers, rounded down when modified | [GGG] above; https://www.poewiki.net/wiki/Stat |
| Hit damage | stored as a signed 32-bit integer, so no hit can exceed 2^31 − 1 (about 2.1 billion) | https://www.poewiki.net/wiki/Hit |
| Damage over time | computed per minute internally, so total DoT taken is capped at 35,791,394/s = (2^31 − 1)/60, across all DoT sources combined | https://www.poewiki.net/wiki/Damage_over_time ; [PoB1] `src/Modules/Data.lua` `DotDpsCap = 35791394` |
| Chance to hit | an integer percent, added to the entropy counter (§2). PoB rounds to the nearest whole percent before clamping | https://www.poewiki.net/wiki/Evasion ; [PoB1] `src/Modules/CalcDefence.lua` L58–64 |
| Crit chance | rolled per skill use as a number from 0 to 99.99. PoB: "The game uses an integer permyriad chance for each critical strike roll" (0.01% resolution) | https://www.poewiki.net/wiki/Critical_strike ; [PoB1] `src/Modules/CalcOffence.lua` L3135–3136 |
| Server tick | 33 ms. DoT is taken at the start of each server frame | https://www.poewiki.net/wiki/Damage_over_time |
| Armour rounding | a graph caption says rounding "is usually decided in the monster's favor" | https://www.poewiki.net/wiki/Armour |

---

## 1. Hit damage pipeline

### 1.1 Order of operations, PoE1 (attacker to defender)

From https://www.poewiki.net/wiki/Receiving_damage, condensed to the offensive part plus the mitigation steps that offense interacts with:

1. **Prevent the hit:** projectile avoidance, then **evasion vs accuracy** (§2) for attacks, then spell dodge. "These are the only defensive mechanics that prevent hits entirely."
2. **Before being hit** effects.
3. **Unmitigated damage**, per damage type:
   1. **Flat damage** = base (weapon for attacks, gem for spells) + added. Both are scaled by base and added **damage effectiveness**.
   2. **Damage conversion:** "Gain X% of A as extra B" and "X% of A converted to B".
   3. **Damage multipliers:** all increased/reduced summed; every more/less separate. Converted damage includes modifiers for its original type and every type it passed through. [GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/1676002/page/40#p13418219
   4. **Critical strike:** if the crit roll succeeded, multiply by the crit multiplier (players 150% by default, monsters 130%). Against attack crits, evasion is tested a second time (§2).
   5. **Roll for damage** inside the [min, max] range produced by the multipliers. Each damage type rolls independently. Lucky = roll twice, keep the higher. Unlucky = keep the lower (§6). [GGG] Mark_GGG: http://www.pathofexile.com/forum/view-thread/377182/page/3#p3349323
   6. **Double/triple damage** applies after all other offensive modifiers.
4. "Cannot take damage" effects.
5. **Damage taken as** (the defender shifts a type; this is not conversion and can happen only once).
6. **Mitigation:** cannot-take-X, immunity, avoidance, then **resistance** (with the attacker's **penetration**, §7), then **damage reduction** (armour, physical by default). In PoE1, resistance comes before armour.
7. **Modifying damage taken:** flat ±X taken, then the summed increased/reduced damage taken (Shock, Vulnerability), then each more/less damage taken separately (§8). [GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/924683#p7847614
8. Stun roll, hit combination (Dual Strike and similar), block, final damage, then resources (ward, energy shield, mana, life).

DoT does not hit, so it skips evasion, armour, block, crit and luck. "While damaging ailments are applied by hits, they have a separate base DPS calculation, starting from the flat damage of a hit. Hit-specific offensive modifiers will not apply to this calculation, nor will the defender's mitigation" [W1 Receiving_damage]. Mitigation applies later, when the DoT damage is actually taken.

> Trap: https://www.poewiki.net/wiki/Damage is tagged `{{cleanup}}`. Its one-line formula (`(Base+Added) × Inc × More × HitRate × (CritDmg × CritChance)`) is an average-DPS sketch, not the per-hit order. Use Receiving_damage for the order.

### 1.2 Base damage and damage effectiveness (PoE1)

- **Base damage:** attacks use the weapon after its **local** modifiers. [W1 Stat] example: weapon 75–75, local "Adds 25–25" gives 100–100, local "100% increased Physical" gives 200–200, and only then do global stats apply. Spells use the gem's base damage. Unarmed and minions have their own.
- **Damage effectiveness** has two parts: **base** effectiveness (shown on attack gems as "Attack Damage: X% of Base") multiplies base damage, and **added** effectiveness multiplies all added flat damage. A spell's base effectiveness is always 100%. If a skill lists no added effectiveness, it is 100%. It "does not directly affect 'Gain #% of X damage as extra Y damage'". https://www.poewiki.net/wiki/Damage_effectiveness

```
Flat_t = BaseDamage_t × BaseEffectiveness + AddedDamage_t × AddedEffectiveness     (per damage type t, min and max separately)
```

### 1.3 Conversion and "gain as extra" (PoE1)

https://www.poewiki.net/wiki/Damage_conversion

- **Converted** removes damage from the source type. **Gain as extra** leaves the source intact and adds new damage. [GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/553671/page/1#p5146950
- **When:** "after added damage is added to base damage, but before multiplication by modifiers such as increased/more Damage."
- **Fixed order:** **Physical → Lightning → Cold → Fire → Chaos.** Steps can be skipped. Damage never converts backwards. [GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/1203998/page/1#p10127678
- **Loop rule:** the calculation loops over source types in that order. Within each source type, **gain as extra is calculated before converted**, so gain as extra uses the pre-conversion amount. Damage can therefore chain (phys → cold → fire), and "gain X% of non-chaos as extra chaos" can apply several times to the same damage as it converts.
- **Overconversion:** if "converted" modifiers from one source type sum above 100%, they are scaled to 100%. **Skill conversion takes priority** and is not scaled down. Non-skill conversion (gear, passives) shares whatever is left, proportionally. [GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/173465#p1540982 and https://www.pathofexile.com/forum/view-thread/542347/page/1#p4883216
- The defender mitigates by the **final** type only.

### 1.4 Increased vs more, and the "inherits" rule (PoE1)

- Per damage type: `Damage_t = Flat_t(after conversion) × (1 + ΣInc applicable) × Π(More applicable)`.
- **Inherits rule:** "Damage modifiers such as increased/more Damage are applied to converted damage for any of the damage types that the damage was converted through, but only once" [W1 Damage_conversion]. So damage converted lightning → cold benefits equally from increased lightning, cold, elemental or generic damage. Gained-as-extra damage also counts as its source type. In the wiki's example 1, chaos gained from physical scales with physical modifiers. [GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/1676002/page/40#p13418219
- **Which modifiers apply after conversion:** all global and skill increased/more modifiers, using the history above. **Local weapon modifiers apply before conversion**, because they define the weapon's base damage. Both methods in [W1 Stat] reach the same result (200 physical and 300 lightning).
- The pool also includes source and keyword modifiers (attack, spell, melee, projectile, area), weapon-type modifiers and conditional modifiers ("against Chilled Enemies"). Only hits can check conditions on the target. https://www.poewiki.net/wiki/Damage and https://www.poewiki.net/wiki/Hit

### 1.5 Worked example (PoE1, averages; the game rolls each type inside its range)

Setup: weapon (after local mods) 50–100 physical, average 75. Skill "Attack Damage 120% of base", added effectiveness 120%. Global "Adds 10–20 Fire to Attacks" (average 15). Support "Gain 10% of Physical as Extra Lightning". Gear "40% of Physical Converted to Cold". Increased: 100% physical, 50% elemental, 30% cold, 20% fire, 40% melee. More: 30% more melee physical (support), 25% more damage (generic). Crit multiplier 150% + 100% = 250%. Target: 40% fire, cold and lightning resistance, 2000 armour, monster physical damage reduction cap 75% ([PoB1] `src/Data/Misc.lua` monster `maximum_physical_damage_reduction_% = 75`). The target is shocked for 20%.

1. Flat: physical 75 × 1.2 = **90**, fire 15 × 1.2 = **18**.
2. Gain before convert, physical first: lightning = 10% × 90 = **9**. Then convert 40% × 90 = **36** cold, leaving **54** physical.
3. Multipliers, using each chunk's history:
   - Physical 54: increased 100 + 40 = 140% → ×2.4. More ×1.3 × 1.25. Result **210.6**.
   - Cold (phys → cold) 36: increased 100 + 50 + 30 + 40 = 220% → ×3.2. More ×1.3 × 1.25. Result **187.2**.
   - Lightning (gained from phys) 9: increased 100 + 50 + 40 = 190% → ×2.9. More ×1.3 × 1.25. Result **42.41**.
   - Fire 18: increased 50 + 20 + 40 = 110% → ×2.1. More ×1.25 only (it was never physical). Result **47.25**.
   - Non-crit hit before mitigation: **487.46**. Crit hit: ×2.5 = **1218.66**.
4. Mitigation, non-crit: elemental (187.2 + 42.41 + 47.25) × (1 − 0.40) = 166.12. Armour against the physical hit: DR = 2000 / (2000 + 5 × 210.6) = 65.5% (under the 75% cap), so physical taken = 72.64. Sum 238.76. Shock ×1.2 gives **286.5**.
5. Mitigation, crit: elemental 415.29. Armour DR falls to 43.2% against the bigger physical hit (526.5), so physical taken = 299.19. Sum 714.49. Shock ×1.2 gives **857.4**. The crit is 2.5× before mitigation but about 3× after, because armour (A/(A + 5D), https://www.poewiki.net/wiki/Armour) weakens against larger hits.

### 1.6 PoE1 vs PoE2: the pipeline

| Aspect | PoE1 | PoE2 |
|---|---|---|
| Conversion order | Fixed: Phys → Light → Cold → Fire → Chaos, never backwards | **Any type to any type, no order.** "Damage types cannot be converted to the same damage type" |
| Inherits rule | Converted damage scales with **every** type in its history | **Only the final type**: "if a percentage of physical damage is converted to fire damage, the converted fire damage no longer scales with increased Physical damage." The same applies to min/max-range modifiers such as Heft |
| Priority | Skill conversion first; non-skill conversion scaled to the remainder | **Two sequential steps:** step 1 is skill-inherent conversion and gain, step 2 is everything else (items, buffs, supports, passives) applied to step 1's output. Overconversion is normalised to 100% **within each step** |
| Gain as extra | Calculated per source type before converting | Can gain a type as extra of **the same** type ("Gain 20% of Damage as extra Fire" turns 50 phys + 50 fire into 50 phys + 70 fire). Gain modifiers stack **additively within a step and multiplicatively between steps** (tested in 0.5) |
| Armour vs resistance order | Resistance, then armour | **Armour/damage reduction, then resistance** ("Damage reduction occurs before resistances, unlike in POE1") |
| Armour formula | DR = A / (A + 5D), PDR cap 90% (players), 75% (monsters) | DR = A / (A + 10D), cap 90% for players and 75% for monsters ([PoB2] `src/Data/Misc.lua` `maximum_physical_damage_reduction_%`). Armour cannot reduce a hit below 1 |

PoE2 sources: https://www.poe2wiki.net/wiki/Damage_conversion , https://www.poe2wiki.net/wiki/Receiving_damage (page carries an `{{update}}` banner: "adapted from the sister page on poewiki... may need to be re-tested") , https://www.poe2wiki.net/wiki/Armour. PoE2 two-step example [W2]: 100 phys with skill 80% phys → fire becomes 80 fire + 20 phys. Then Avatar of Fire (75%) turns 15 of the remaining 20 phys into fire, for **95 fire + 5 phys**. The wiki's citation for this is only a link to Mark_GGG's forum profile, so it is weakly sourced.

---

## 2. Accuracy, chance to hit and evasion

### 2.1 PoE1 formula

```
UncappedChanceToHit = 1.25 × Accuracy / (Accuracy + (Evasion × 0.2)^0.9)
ChanceToHit = min(max(UncappedChanceToHit, 5%), 100%)
ChanceToEvade = 1 − ChanceToHit    (so evasion caps at 95%)
```

- Source: https://www.poewiki.net/wiki/Accuracy and https://www.poewiki.net/wiki/Evasion. The current formula dates from **3.16.0** ("Improved the Evasion formula..."). The wiki credits its derivation to u/esqtin, https://www.reddit.com/r/pathofexile/comments/q3cpn3/i_worked_out_the_new_evasion_formula/ (community, not GGG).
- **Second source agrees:** [PoB1] `src/Modules/CalcDefence.lua` L58–64: `rawChance = accuracy / (accuracy + (evasion / 5) ^ 0.9) * 125; return m_max(m_min(round(rawChance), 100), 5)`. PoB rounds to a whole percent.
- **Stale line on the wiki:** the Accuracy page's 3.7.0 history row mentions "a 1.15 multiplier". That was the pre-3.16 formula, superseded by 1.25 in 3.16.0. It is history, not a conflict.
- Players get 2 accuracy per level and 2 per point of Dexterity. Monster base evasion = (50 + 16 × Level) × 1.0212^Level, before monster-type modifiers (https://www.poewiki.net/wiki/Accuracy). Level 70 gives about 5,081.
- Bypasses: "Can't be Evaded" or "Always Hit" set chance to hit to 100%. "Cannot Evade" on the defender does the same. Spells cannot be evaded. Minion attacks always hit (3.27.0).
- **Crit confirmation:** accuracy is checked a second time when an attack hit would crit. If that check fails, the hit lands as a non-crit. Spell crits are exempt. [GGG] Mark_GGG: http://www.pathofexile.com/forum/view-thread/11707/filter-account-type/staff/page/10#p748465. PoB models the effective crit rate as `CritChance × HitChance` (`src/Modules/CalcOffence.lua` L3175).

### 2.2 Entropy: evasion is not rolled independently

https://www.poewiki.net/wiki/Evasion ([GGG] Mark_GGG: http://www.pathofexile.com/forum/view-thread/11707/filter-account-type/staff/page/10#p748465 and http://www.pathofexile.com/forum/view-thread/11707/filter-account-type/staff/page/8#p642245)

1. The first time an entity is attacked, or when more than 100 server ticks (3.33 s) have passed since the last attack, the entropy counter is set to a random value from 0 to 99.
2. Each attack adds the attacker's chance to hit (an integer) to the counter.
3. If the counter is below 100, the attack is **evaded**. At 100 or more it **hits**, and 100 is subtracted.
4. The second evasion check against crits is a separate random roll and does not touch entropy.
5. All attackers share the defender's single counter.

Consequence: "if a player has a 1 in N chance to be hit, then the player will be hit exactly once for every N consecutive attacks." There are no lucky or unlucky streaks, only a random starting phase.

Worked example [W1/W2, same example]: A has a 70% chance to hit; B and C have 45%. Start at 37. A: 107 → hit → 7. B: 52 (evaded). C: 97 (evaded). A: 167 → hit → 67. The PoE1 page's example says the reset happens after ">6 seconds", contradicting its own rule (">100 server ticks (3.33 seconds)"). The PoE2 page says 3.33 s. Treat 3.33 s as correct.

### 2.3 PoE2 versions: there are two formulas

| Direction | Formula | Source |
|---|---|---|
| Player (or ally) attacking a **monster** (unchanged since 0.3.0) | `ChanceToHit = 1.25 × Accuracy / (Accuracy + 0.3 × Evasion)`, clamped to 5–100% (the Amazon "Critical Strike" notable lets it exceed 100%, to at most 125%) | https://www.poe2wiki.net/wiki/Accuracy , https://www.poe2wiki.net/wiki/Evasion (both cite https://www.reddit.com/r/PathOfExile2/comments/1n2tefy/comment/nb8wufb). [PoB2] `src/Modules/CalcDefence.lua` L33–38, commit 6a06297738 ("Update Evasion / Hit Chance formula with 0.3 changes", 2025-08-29) |
| Monster attacking a **player or player minion** (since 0.3.1) | `ChanceToHit = 1 − (0.95 × DefenderEvasion) / (DefenderEvasion + 4 × AttackerAccuracy)`, clamped to 5–100% | [GGG] 0.3.1 patch notes: "When calculating chance to Hit vs Players or their Minions, we now use the following formula: Chance to Hit = 1 - (0.95 * defender_evasion) / (defender_evasion + 4 * attacker_accuracy)… The chance to hit formula vs Monsters is unchanged." https://www.pathofexile.com/forum/view-thread/3862213 . [PoB2] `src/Modules/CalcDefence.lua` L41–46, commit cf4837a682 ("Fix evade chance for 0.3.1", 2025-10-09) |
| Before 0.3.0 (history) | `1.5 × A / (A + E)` | [PoB2] diff of commit 6a06297738 |

- **DISAGREEMENT:** the poe2wiki Accuracy and Evasion pages give only the vs-monster formula and present it as *the* evasion formula. The 0.3.1 patch notes (GGG) and PoB2 use a different formula when a player is the defender. The wiki lags the patch.
- PoE2 accuracy per level and per Dexterity point is 6 each (0.3.0). Player attacks suffer a **distance penalty**: none within 2 m, rising linearly to **90% less accuracy at 9 m or more** (0.3.0). Monsters have no distance penalty. Base player evasion is 7 with no per-level growth (0.3.1). https://www.poe2wiki.net/wiki/Accuracy
- PoE2 keeps entropy and the crit-downgrade second check unchanged (https://www.poe2wiki.net/wiki/Evasion). PoE2 adds **Deflection**, a second, partial roll against accuracy: `ChanceToDeflect = 150% × (1 − Accuracy / (Accuracy + 0.12 × DeflectionRating))`, capped at 95% (https://www.poe2wiki.net/wiki/Deflection ; [PoB2] `calcs.deflectChance` gives the same expression). A deflected hit has **40%** of its damage prevented (`BasePercentDamageDeflected = 40` in [PoB2] `src/Data/Misc.lua`; each Tailwind stack adds 1%). Deflection is applied after damage-taken modifiers. Receiving_damage says it "still applies for ailments" (https://www.poe2wiki.net/wiki/Receiving_damage).

### 2.4 Worked example (accuracy 1,000 vs evasion 10,000)

- PoE1: (10000 × 0.2)^0.9 = 2000^0.9 ≈ 935.0. 1.25 × 1000 / 1935.0 = 0.646, so **65%**.
- PoE2, player attacking a monster: 1.25 × 1000 / (1000 + 3000) = **31.25%**, so 31%.
- PoE2, monster attacking a player: 1 − 0.95 × 10000 / (10000 + 4000) = 1 − 0.6786 = **32.1%**.
- PoE2 before 0.3.0: 1.5 × 1000 / 11000 = 13.6%.

The PoE2 player-defender curve is bounded by construction: at zero evasion the monster always hits, and as evasion grows without limit its chance to hit falls toward 5%. No clamp is needed.

---

## 3. Critical strikes (PoE1) and critical hits (PoE2)

### 3.1 The roll

"When a skill is used or triggers, it performs a critical strike roll. This roll ranges from 0 to 99.99, and determines the critical strike chance a hit must exceed to critically strike." If the roll is 12, every hit from that use needs more than 12.00% crit chance. Usually all hits from one use either crit or don't. Channelled skills and some others roll per hit or stage. https://www.poewiki.net/wiki/Critical_strike . [GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/11707/filter-account-type/staff/page/10#p755973

### 3.2 Crit chance (PoE1)

```
CritChance = (BaseCrit + AdditionalCrit) × (1 + ΣIncreasedCrit/100) × Π(MoreCrit)
```

- Base comes from the weapon for attacks (including the weapon's local increased crit) and from the gem for spells. Unarmed is 0% in PoE1. "Additional" sources (Brittle, Assassin's Mark, Increased Critical Strikes Support) add to base before multipliers. Example: weapon 5% + 1% additional, with 50% increased, gives 6 × 1.5 = **9%**. https://www.poewiki.net/wiki/Critical_strike
- **Caps:** 0% to 100% since **3.6.0**. Before that, crit had a 5% floor and a 95% ceiling: "Removed the minimum and maximum Critical Strike Chance caps" (https://www.poewiki.net/wiki/Critical_strike , version history). "Prevent crits" effects beat "always crit" effects.
- **Second source:** [PoB1] `src/Modules/CalcOffence.lua` L3136: `round((baseCrit + base) * (1 + inc / 100) * more * 100) / 100`, capped by `CritChanceCap` (100).

### 3.3 Crit multiplier (PoE1)

- Players' base is **150%** (+50% extra damage). Monsters' is **130%**. Since 2.2.0 all "increased crit multiplier" became flat "+X% to Critical Strike Multiplier", summed onto the 150. Crit damage = hit × multiplier. https://www.poewiki.net/wiki/Critical_strike , https://www.poewiki.net/wiki/Damage_over_time ("the character's inherent 150% Critical Strike Multiplier"). [GGG] monsters' 130%: Mark_GGG, http://www.pathofexile.com/forum/view-thread/313264/page/2#p2943556
- **Second source:** [PoB1] `src/Data/Misc.lua`: character `base_critical_strike_multiplier = 150`, monster `= 130` (L89, L166).
- **Defender side:** "Take X% reduced Extra Damage from Critical Strikes" scales only the part above 100%, stacks additively, and caps at 100%. Example: 130% monster crit, 30% reduced → 100 + 30 × 0.7 = **121%**. [GGG] Mark_GGG: http://www.pathofexile.com/forum/view-thread/313264#p2725515
- **Expected multiplier per hit:** `1 + c × (M − 1)`, where c is crit chance and M the multiplier.

### 3.4 Lucky crits

Lucky means roll twice and keep the better requirement. `p_lucky = 1 − (1 − p)² = 2p − p²`. Unlucky: `p²`. Unexciting (PoE1 only) keeps the median of 3 rolls: `3p² − 2p³` ([PoB1] `src/Modules/CalcOffence.lua` L3155). Bifurcated crits roll twice; if both succeed, the extra damage applies twice. https://www.poewiki.net/wiki/Luck

### 3.5 Worked example (PoE1)

Weapon 7% + 1% additional, 200% increased: 8 × 3 = **24%**. Lucky: 1 − 0.76² = **42.24%**. Multiplier 150 + 50 = 200%. Expected per-hit multiplier: 1 + 0.24 × 1.0 = **1.24** (lucky 1.4224). Add a 65% chance to hit with the second accuracy check: effective crit per landed hit = 0.24 × 0.65 = 15.6%, and per attack attempt 0.24 × 0.65² = 10.1%.

### 3.6 Crits and ailments (PoE1)

- Crits **always ignite, freeze and shock** (they still need the right damage type and enough magnitude). Crits do **not** automatically poison or bleed.
- Damaging ailments from crits do **not** use the crit multiplier. Instead they get **+50% damage over time multiplier** (monsters and minions +30%), additive with other DoT multipliers. Perfect Agony makes crit multiplier apply instead. Elemental Overload makes ailments never count as crits. [GGG] https://www.pathofexile.com/forum/view-thread/2627116 . [PoB1] `src/Data/Misc.lua` `critical_ailment_dot_multiplier_+ = 50` (character) and `30` (monster).

### 3.7 PoE1 vs PoE2

| Aspect | PoE1 | PoE2 |
|---|---|---|
| Name | critical strike, crit **multiplier** | critical **hit**, crit **damage bonus** |
| Base bonus | +50% (150% total) | **+100% (200% total)** for players |
| How modifiers stack | flat "+X% multiplier" summed onto 150 | flat "+X% to Critical Damage Bonus" is added to the base +100% first, **then** increased/reduced and more/less multiply the bonus: `Mult = 1 + (100 + ΣFlat) × (1 + ΣInc) × ΠMore / 100` |
| Unarmed base chance | 0% | 5% (`unarmed_base_critical_strike_chance = 500` permyriad, [PoB2]) |
| Ailments | crit always ignites, freezes and shocks; +50% DoT multiplier for damaging ailments | crits **no longer guarantee ailments**. They raise ailment chance indirectly by raising hit damage, and PoE2 damaging ailments are based on hit damage (§4.4) |
| Lucky crit | keep the better of 2 | same: "keep the lower requirement" |
| Evasion downgrade | yes | yes, or a reroll with Forced Outcome |

- PoE2 sources: https://www.poe2wiki.net/wiki/Critical_hit. The formula order cites NeverSink, https://x.com/NeverSinkDev/status/1868712499614515415 (community, not GGG). PoE2 example: +50% flat and 40% increased gives (100 + 50) × 1.4 = 210% bonus, a **×3.1** multiplier.
- **DISAGREEMENT (PoE2 monsters and minions):** the wiki says "The default critical damage bonus for players and minions is +100%... Monsters have 40% less bonus critical damage". An editor comment there questions what that means. [PoB2] `src/Data/Misc.lua` gives characters `base_critical_hit_damage_bonus = 100`, monsters `30`, and player minions (`PlayerMinionIntrinsicStats`) `70`. So the auto-generated game data says minions get +70%, not +100%, and monsters a flat +30%.

---

## 4. Damage over time and damaging ailments (attacker side)

### 4.1 Shared PoE1 rules

Sources: https://www.poewiki.net/wiki/Ailment , https://www.poewiki.net/wiki/Damage_over_time , https://www.poewiki.net/wiki/Receiving_damage . [GGG] Bex_GGG, "Damage over Time changes: More Information": https://www.pathofexile.com/forum/view-thread/1894879 . Rory, "Damage Over Time Changes, Part 2": https://www.pathofexile.com/forum/view-thread/1894471#p14456894 . 3.0.0 made ailments a separate calculation ("taken straight from the base and added damage of the skill").

```
AilmentBaseDPS  = k_ailment × Σ_t Flat_t        (flat = base + added, after effectiveness and conversion, BEFORE increased/more; only the damage types that can inflict this ailment)
AilmentDPS      = AilmentBaseDPS × (1 + ΣIncreased) × Π(More) × (1 + ΣDoTMultiplier [+0.50 if the inflicting hit was a crit])
AilmentDuration = BaseDuration × (1 + ΣIncreasedDuration) × Π(MoreDuration) / (1 + Faster)
DamageTaken/s   = AilmentDPS × (1 − DefenderResistance_ofDealtType) × (damage-taken modifiers)
```

- **Applicable increased/more:** generic "damage", "damage over time", the ailment's own name (poison, bleeding, ignite or "burning"), "damage with ailments", the dealt type (chaos for poison, fire for ignite, physical for bleed), **and** the source type for the share that came from it. Poison from base physical scales with physical **and** chaos modifiers; ignite from phys → fire conversion also gets physical modifiers. Minion, trap, mine and totem modifiers apply when the ailment comes from those. **Not applicable:** attack, spell, melee, projectile, area and weapon modifiers, and hit-only effects (penetration, double damage, lucky, crit multiplier).
- **DoT multiplier:** a separate additive pool. It counts only for the type the DoT deals (chaos DoT multiplier for poison, even if the poison came from lightning). [W1 Damage_over_time] cites a community source (https://www.reddit.com/r/pathofexile/comments/a37j9o/) for "additive with each other".
- **Snapshot:** the base DPS is fixed when the ailment is applied, so later changes to the source's stats don't affect it. Changes in the **defender's** mitigation (Shock, Flammability, Immortal Call) do change it live.
- **"Deal damage X% faster":** DPS × (1 + X) and duration ÷ (1 + X). Total damage is unchanged.
- **Chance to inflict:** additive chance, 0% by default except crit → ignite. **Over 100% is wasted.** Poison applies at most one stack per hit ([GGG] Bex_GGG: https://www.pathofexile.com/forum/view-thread/1528287). The target's avoid chance and immunity multiply with it: "100% Avoidance and Immunity will take priority over sources of Always inflict." The hit must deal at least 1 point of an eligible type to the defender.
- **Defender mitigation:** resistance of the **dealt** type, plus non-armour damage reduction. Armour never applies to DoT. Penetration never applies to DoT ([GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/628983/page/1#p5494062).

### 4.2 Per-ailment constants (PoE1)

| Ailment | Can be inflicted by | k (share of flat damage per second) | Base duration | Stacking | Notes |
|---|---|---|---|---|---|
| **Poison** | physical + chaos | **30%** (3.15.0; previously 20%) | **2 s** | **unlimited** stacks, independent durations | deals chaos damage. [GGG] 30% value: Mark_GGG, https://www.pathofexile.com/forum/view-thread/1930909/page/1#p14659848 . Stacking: Bex_GGG, https://www.pathofexile.com/forum/view-thread/1511833 |
| **Bleeding** | physical, **attacks only** | **70%** stationary. Moving or aggravated: "an additional 140% damage per second", **210%** total (×3) | **5 s** | only the **strongest** deals damage (Crimson Dance: 35% each, 8 stacks) | monster-inflicted bleed is 10%. Not reduced by armour |
| **Ignite** | fire (crits always ignite) | **90%** (3.16.0: 125%; 3.17.0: cut 28% to 90%) | **4 s** | only the **strongest** deals damage (Emberwake is the exception) | enemy-inflicted: "200% of base damage over 4 seconds, or 50%/s", tagged `{{confirm\|3.17.0 change}}` on the wiki |

- Pages: https://www.poewiki.net/wiki/Poison , https://www.poewiki.net/wiki/Bleeding , https://www.poewiki.net/wiki/Ignite
- **Second source agrees:** [PoB1] `src/Modules/Data.lua` L262–267: `BleedPercentBase = 70, BleedDurationBase = 5, PoisonPercentBase = 0.30, PoisonDurationBase = 2, IgnitePercentBase = 0.9, IgniteDurationBase = 4`.

### 4.3 Worked example (PoE1 poison)

A hit's flat (pre-modifier) damage averages 150 physical + 30 chaos. Base poison DPS = 0.30 × 180 = **54/s**. Increased: 60% poison, 30% chaos, 20% DoT, 10% damage, for 120% → ×2.2. There are no physical-only modifiers here; any would apply only to the 45/s that came from physical. 20% more poison → ×1.2. +25% chaos DoT multiplier → ×1.25. Non-crit poison: 54 × 2.2 × 1.2 × 1.25 = **178.2/s**. A poison from a crit uses ×1.75 instead: **249.5/s**. Duration 2 × (1 + 0.30 increased poison duration) = 2.6 s, so one non-crit stack does 463 damage before mitigation. Against 20% chaos resistance that is 142.6/s per stack. At 3 poisons per second the steady state is 3 × 2.6 = 7.8 stacks, about **1,112/s**.

Bleed (PoE1): flat physical 150 → 105/s on a stationary target, **315/s** moving, 525 total over 5 s stationary. Ignite (PoE1): flat fire 200 → 180/s, 720 over 4 s.

### 4.4 PoE2: ailments are a share of the hit actually dealt

https://www.poe2wiki.net/wiki/Ailment , https://www.poe2wiki.net/wiki/Poison , https://www.poe2wiki.net/wiki/Bleeding , https://www.poe2wiki.net/wiki/Ignite

```
AilmentDPS = k × (hit damage of eligible types dealt, AFTER the attacker's modifiers including crit, BEFORE defender mitigation and damage-taken modifiers) × (1 + ΣIncreasedMagnitude) × ΠMoreMagnitude
```

| Ailment | k per second | Base duration | Stacking | Chance |
|---|---|---|---|---|
| Bleeding | **15%/s** of physical (75% total over 5 s). +100% against **moving monsters** or when aggravated | **5 s** | strongest only | explicit chance to bleed (any skill that hits can bleed) |
| Poison | **20%/s** of physical + chaos | **2 s** | **default stack limit 1**: only the highest instance(s), up to the limit, deal damage. The limit can be raised | explicit chance to poison |
| Ignite | **20%/s** of fire (80% total over 4 s) | **4 s** | strongest only | from **Flammability**: 1% ignite chance per 5% of ailment threshold dealt, i.e. `20 × D/T` % (§5) |

- "Ailment damage cannot be scaled directly; however, it can be indirectly scaled by modifying the hit damage", plus "magnitude" modifiers, which multiply k. Because the base is the hit actually dealt, crit damage bonus **does** feed PoE2 ailments. Ignite "does not double-dip on enemy resistance": the base is taken before the hit's resistance, then resistance applies to the hit and the ignite separately. Bleed and poison bypass energy shield.
- **Constants are confirmed by game data:** [PoB2] `src/Data/Misc.lua` `gameConstants`: `BleedingHitDamagePercentPerMinute = 900` (15%/s), `PoisonHitDamagePercentPerMinute = 1200` (20%/s), `IgniteHitDamagePercentPerMinute = 1200` (20%/s), `BaseBleedingDuration = 5`, `BasePoisonDuration = 2`, `BaseIgniteDuration = 4`, `IgniteChanceMultiplier = 20`.
- **DISAGREEMENT (wording):** the poe2wiki Poison page says "20% of a hit's combined physical and chaos damage... over 2 seconds", which reads as 20% **total**. The Ailment page's table ("20%... per second... base duration is 2 seconds") and the game constant (1200%/min = 20%/s) both say **per second**. The Bleed (75% over 5 s) and Ignite (80% over 4 s) pages state totals that match 15%/s and 20%/s.
- PoE2 chance to poison or bleed, per [PoB2] `src/Modules/CalcOffence.lua` L5543: `min(100, base × (1 + inc/100) × more)`.
- **Worked example (PoE2):** a hit deals 300 physical + 100 chaos (after the attacker's modifiers). Poison = 0.20 × 400 = 80/s; with 50% increased poison magnitude, **120/s for 2 s**; against 20% chaos resistance, 96/s. With the default stack limit of 1, a second, weaker poison adds nothing until the first expires. Bleed from 600 physical dealt: 90/s, or 180/s while the monster moves, for 5 s. Ignite from 1000 fire dealt: 200/s for 4 s. A hit dealing 50% of the target's ailment threshold has a 10% ignite chance.

### 4.5 PoE1 vs PoE2 summary (damaging ailments)

| | PoE1 | PoE2 |
|---|---|---|
| Base | flat base + added damage (pre-modifier) | hit damage dealt (post-attacker modifiers, pre-mitigation) |
| Scaling | its own increased/more pool plus DoT multiplier | only "magnitude"; otherwise inherits the hit's scaling |
| Crit | +50% DoT multiplier (not the crit multiplier) | crit bonus flows in through hit damage |
| Poison | 30%/s, 2 s, unlimited stacks | 20%/s, 2 s, stack limit 1 by default |
| Bleed | 70%/s (×3 moving), 5 s, attacks only | 15%/s (×2 moving monsters), 5 s, any hit |
| Ignite | 90%/s, 4 s, crit always ignites | 20%/s, 4 s, chance from damage ÷ threshold |

---

## 5. Non-damaging ailments and the ailment threshold

### 5.1 PoE1: magnitude scales with damage ÷ threshold

- **Ailment threshold (T):** "In the great majority of cases, the ailment threshold is the same as the monster's maximum life." It is lower on high-life bosses, and has a maximum of about 800 million. Magnitude depends on the damage the enemy **takes** from the hit, not the hit's base damage. https://www.poewiki.net/wiki/Ailment
- Each ailment has a minimum (below it, the ailment is **discarded**) and a maximum (it is **capped** there). Increased effect raises magnitude before the min/max check, so it lowers the damage needed but cannot exceed the cap.

**Shock and chill** ([GGG] 3.9.0 patch notes: "The formula used to calculate Shock and Chill magnitudes is now: 1/2*[Damage % of Ailment Threshold]^0.4… The minimum Shock and Chill value is now 5%… To reach the minimum, you must deal 0.4% of a monster's Ailment Threshold… A 30% Chill requires a hit of 28%". https://www.pathofexile.com/forum/view-thread/2687400):

```
E = 0.5 × (D / T)^0.4 × (1 + M)        D = damage of the ailment's type taken, T = ailment threshold, M = Σ increased effect
inverse (M = 0): D/T = (2E)^2.5
```

| Ailment | Effect | Minimum (discard below) | Maximum | Base duration | Applied without damage |
|---|---|---|---|---|---|
| **Shock** | increased damage taken (§8) | 5% | **50%** | 2 s | 15% |
| **Chill** | reduced action speed. **Every** cold hit attempts to chill (100% chance) | 5% | **30%** | 2 s | 10% |
| Scorch (alternative fire ailment) | −X% elemental resistances | 0 | 30% | 4 s | 10% |
| Brittle (alternative cold ailment) | +X% base crit chance against the target | 0 | 6% | 4 s | 2% |
| Sap (alternative lightning ailment) | X% less damage dealt | 0 | 20% | 4 s | 6% |

- Thresholds for shock at M = 0 [W1 Shock]: 5% needs 0.32% of T; 10% needs 1.79%; 20% needs 10.12%; 30% needs 27.89%; 50% needs 100%.
- Pages: https://www.poewiki.net/wiki/Shock (its formula is tagged `{{confirm|potentially outdated calculations}}`, but the 3.9.0 notes state it verbatim) , https://www.poewiki.net/wiki/Chill , https://www.poewiki.net/wiki/Scorch , https://www.poewiki.net/wiki/Brittle , https://www.poewiki.net/wiki/Sapped
- **Second source agrees:** [PoB1] `src/Modules/CalcOffence.lua` L5376–5405: chill, shock and scorch `50 * ((damage / enemyThreshold) ^ 0.4) * effectMod`; brittle `10 * (...)^0.4`; sap `(100/3) * (...)^0.4`. `src/Modules/Data.lua` L424–431 lists min, max, default and duration exactly as in the table.
- Shock and chill are applied **after** the hit, so the hit that shocks does not benefit from its own shock.
- Only the strongest instance of each applies. Different ailments coexist without limit.

**Freeze** (https://www.poewiki.net/wiki/Freeze):

```
FreezeDuration = 0.06 s × (100 × D / T) × (1 + ΣIncreasedFreezeOrAilmentDuration)    capped at 3 s (D/T = 50%); discarded below 0.3 s (D/T = 5% at no duration bonus)
```

Crits with cold always freeze; otherwise freeze needs a "chance to freeze". Frozen means 100% less action speed. Increased freeze effect does nothing useful, because action speed cannot go below 0. Temporal Chains doesn't count toward the 0.3 s minimum, but it does lengthen a freeze that lands. [PoB1] `src/Modules/CalcOffence.lua` L5440 uses the same threshold rule ("For freeze to apply for the minimum of 0.3 seconds, target must have no more than baseVal × 20 × durationMod Ailment Threshold").

**Worked example (PoE1):** T = 10,000, lightning damage taken 1,000 (10%). Shock = 0.5 × 0.1^0.4 = 0.5 × 0.398 = **19.9%** (19% once rounded down to an integer stat). With 50% increased shock effect: 29.9%. A cold hit of the same size: chill 19.9% (cap 30%), and if it freezes, 0.06 × 10 = **0.6 s**.

### 5.2 PoE2: chance and buildup replace magnitude scaling

https://www.poe2wiki.net/wiki/Ailment , https://www.poe2wiki.net/wiki/Shock , https://www.poe2wiki.net/wiki/Chill , https://www.poe2wiki.net/wiki/Freeze , https://www.poe2wiki.net/wiki/Electrocute

- **Thresholds:** a player's threshold is **50% of maximum life** (`PlayerAilmentThresholdLifeFactor = 0.5`, [PoB2]). A monster's ailment threshold and its separate **poise** threshold (used by freeze, electrocute, pin and stun) depend on **monster level, not life**: level 1 is 15 (poise 30), level 53 is 4,247 (poise 5,338), with a full table on the Ailment page. They are multiplied by rarity. The values are from game files and were "confirmed with GGG by LocalIdentity": https://www.reddit.com/r/PathOfExile2/comments/1pghfps/comment/olixt0d
- **Shock:** a **fixed 20% increased damage taken** (`BaseShockMagnitude = 20`), raised by magnitude modifiers up to a cap of 100%. **Chance = 25% per 100% of ailment threshold dealt** (`ShockChanceMultiplier = 25`), i.e. `25 × D/T` %, plus any flat chance, × increased × more. It lasts **8 s on non-players** (0.3.0; previously 4 s) and 4 s on players. Crits do not automatically shock. [GGG] Viperesque on the official Discord: https://discord.com/channels/174993814845521922/1118165205025828884/1311050747261227071
- **Ignite chance:** `20 × D/T` % via Flammability (§4.4).
- **Chill:** "All cold damage above a minimum ailment threshold applies chill". Magnitude scales with D/T up to **50%**. **Discarded below 30%** (0.3.0; previously 5%). Lasts 8 s on non-players (0.3.0) and 2 s on players. Rarity's "less slow effect" applies after the cap (unique monsters effectively max at 25%). Chill without damage is 30% (0.3.0, tagged undocumented/confirm). The wiki gives no chill formula. [PoB2] uses a **linear** `ChillEffectMultiplier (100) × D/T × effectMod`, capped at `ChillMaxEffect = 50` (`src/Modules/CalcOffence.lua` L5674). PoB2's minimum check (`threshold / 100`, L5551) predates the 30% discard rule.
- **Freeze, electrocute, pin and heavy stun are buildup meters against poise.** When buildup reaches 100%, the target is frozen (or electrocuted, and so on) for a fixed time, and the meter resets. Bosses' thresholds rise after each freeze and then decay. [PoB2] models per-hit buildup as `DamageScale / PoiseThreshold × (1 + inc) × more × damage`, with `FreezeDamageScale = 2.1`, `ElectrocuteDamageScale = 1.7`, `PinDamageScale = 4.2`, `HeavyStunDamageScale = 0.58`. Freeze lasts `FreezeDuration = 4` s on monsters and 2 s on players; electrocute 5 s and 2 s (`src/Data/Misc.lua`, `src/Modules/CalcOffence.lua` L5569–5585). 0.2.0: "Freeze now builds up roughly 48% slower on enemies." Lightning damage cannot electrocute by default; a source must grant it.
- **Worked example (PoE2):** level-53 monster, T = 4,247. A lightning hit of 2,000 has a 25 × 2000/4247 = **11.8%** chance to shock, applying a flat 20% for 8 s. A cold hit of 2,000 chills at about 100 × 0.471 = **47%** by PoB2's linear model, which is above the 30% discard line and under the 50% cap.
- **DISAGREEMENT (pre- or post-mitigation damage for PoE2 ailment rolls):** the Ailment page says chance uses "damage dealt before mitigation". The Shock, Chill and Freeze pages say "damage taken by the enemy after mitigation". Receiving_damage puts the "elemental ailment chance/buildup" roll **after** damage-taken modifiers. PoB2 uses "unmitigated" damage for ignite and shock chance (`calcMinMaxUnmitigatedAilmentSourceDamage`, L5615). This is unresolved; testing would settle it.
- **PoB2 legacy:** PoB2's display table still carries PoE1's `50 × (D/T)^0.4` curve for Shock (`src/Modules/CalcOffence.lua` L5680), while the wiki says PoE2 shock is a flat 20% with a damage-scaled chance. It is probably dead code left over from PoE1.
- The poe2wiki Ailment summary table still lists shock as "4 seconds" and "based on the lightning damage of the hit". Both are outdated (see 0.3.0).

---

## 6. Lucky and unlucky rolls

https://www.poewiki.net/wiki/Luck , https://www.poe2wiki.net/wiki/Luck

- **Lucky:** roll twice, apply the **best**. **Unlucky:** roll twice, apply the **worst**. **Unexciting** (PoE1 only): roll three times, apply the **middle**. Lucky and unlucky on the same roll cancel out ([GGG] https://www.pathofexile.com/forum/view-thread/3108809/page/1#p24083466). Unexciting usually cancels both.
- **Binary rolls** (crit, block, evade and the like), with p the base chance:

```
Lucky      p' = 1 − (1 − p)² = 2p − p²
Unlucky    p' = p²
Unexciting p' = 3p² − 2p³          ([PoB1] src/Modules/CalcOffence.lua L3155)
n-fold luck (PoB): p' = 1 − (1 − p)^(rolls+1)
```

  Table [W2]: 10% → 19% / 1%; 25% → 43.75% / 6.25%; 50% → 75% / 25%; 75% → 93.75% / 56.25%; 90% → 99% / 81%. At 0% or 100%, luck does nothing.
- **Damage in a range** [min, max] (the integer roll):

```
E[normal]  = (min + max) / 2
E[lucky]   = min/3 + 2·max/3 + (max − min) / (6 · (1 + max − min))   ≈ (min + 2·max) / 3
E[unlucky] ≈ (2·min + max) / 3                                         (by symmetry)
gain from lucky ≈ (max − min) / 6;  at most +33% when min ≈ 0
```

- **Scope:** "Damage is lucky" applies to the **post-multiplier tooltip range** of **hit** damage only. It does not affect accuracy, crit chance or DoT rolls ([GGG] Mark_GGG: http://www.pathofexile.com/forum/view-thread/377182/page/3#p3349323). A separate source must make crit chance lucky.
- **Worked example** [W1]: Spark, 63–1198, average 630.5. Lucky adds (1198 − 63)/6 = 189.2, for 819.7 (+30%). Fireball, 1095–1643: +91.3 (+6.6%). Unlucky Spark: (2 × 63 + 1198)/3 = 441.3.
- PoB1 builds lucky into the average directly: `(min/rolls + (rolls − 1) × max/rolls)` with rolls = 3 for one level of luck ([PoB1] `src/Modules/CalcOffence.lua` L3507–3510). That is the same (min + 2·max)/3.
- **PoE1 vs PoE2:** identical, except PoE2 has no "Unexciting".

---

## 7. Resistance and everything that lowers it: penetration, exposure, curses, overwhelm

### 7.1 PoE1 resistance model

https://www.poewiki.net/wiki/Resistance , https://www.poewiki.net/wiki/Resistance_penetration

```
Uncapped  = Σ(all resistance stats, including act penalties, curses, exposure and other "−X% to resistance")    then × (1 − ΣReducedResistance%), with "reduced" capped at 100%
Capped    = clamp(Uncapped, −200%, MaxRes)                  MaxRes defaults to 75%; "+X% to maximum" can raise it to a hard 90%
Effective = Capped − Penetration                            NO lower bound; penetration may go below −200%
DamageTaken = Damage × (1 − Effective)
```

- Damage ratio at −60% vs 75% resistance: (1 + 0.6) / (1 − 0.75) = **6.4×** [W1].
- **Overcap** = Uncapped − MaxRes. "Only resistances up to a player's maximum resistances will provide damage mitigation but overcapped resistances serve as a buffer against resistance reduction such as curses and exposure." Curses and exposure "directly affect uncapped resistance values."
- **Penetration** applies to hits only, "as damage is dealt, after other resistance calculations. Penetration applies to capped resistance value, so an enemy with 75% capped resistances against a hit with 30% penetration will take damage as if they had 45% resistances." [GGG] Mark_GGG: http://www.pathofexile.com/forum/view-thread/59271/page/2#p751660 . Formula from the penetration page: `R_t = min(r_t, m_t) − p_t`. Penetration is checked against the resistance actually used (Hexblast's chaos resisted by fire resistance is penetrated by fire penetration). "Your Hits treat X Resistance as #% higher" is negative penetration and adds to it.
- **Value of penetration:** `I_t = p_t / (1 − min(r_t, m_t))`, so it is worth more against high resistance.
- **Ignore resistance:** treat the resistance as 0, discarding all reductions, inversions and penetration. It loses damage if the resistance was negative.
- **Inversion:** multiply the capped resistance by −1 (still subject to caps).
- **Second source agrees:** [PoB1] `src/Modules/CalcOffence.lua` L494–507 `calcResistForType`: `max(min(resist, maxResist), ResistFloor = −200)`, where resist = Σ BASE × max(increased/reduced mod, 0). Then L3609–3622: `effMult = (1 + takenInc/100) × takenMore`, `effectiveResist = (resist − pen)`, `effMult × (1 − effectiveResist/100)`. `src/Modules/Data.lua` L245–246: `ResistFloor = -200`, `MaxResistCap = 90`. The −200% floor arrived in 3.16.0 (https://www.poewiki.net/wiki/Version_3.16.0).

**Exposure (PoE1):** one debuff per element; only the strongest exposure of each element applies. Player-applied exposure is **−10%** by default; specific skills override it (Elemental Equilibrium −25%, Scorching Ray −25%, Frost Bomb −15%, Wave of Conviction −15%). Enemy-applied exposure is −25%. A timed exposure defaults to 4 s. Exposure lowers the **uncapped** value. https://www.poewiki.net/wiki/Exposure

**Curses (PoE1, post-3.20.0, gem levels 1–20):**

| Curse | Effect | Notes |
|---|---|---|
| Elemental Weakness | cursed enemies have **−(15–30)%** to elemental resistances (−30% at level 20) | previously −(20–39)%. The skill-interaction text on the page still says "level 1 → −20%", which is stale |
| Vulnerability | cursed enemies take **(15–30)%** increased **physical** damage; attack hits against them have **25%** chance to bleed | this is an increased-damage-taken modifier, so it goes in the damage-taken pool (§8), not resistance. [GGG] Mark_GGG on why it is multiplicative with the attacker's own modifiers: http://www.pathofexile.com/forum/view-thread/891571/page/1#p7610912 |
| Despair | cursed enemies have **−(15–30)%** to chaos resistance | |

- Curse effect stacks the usual way (base × (1 + Σincreased effect) × more). The default limit is **one curse** per target, and applying another replaces the oldest. **3.20.0 removed Doom and the bosses' 33% or 66% less curse effect.** https://www.poewiki.net/wiki/Curse , https://www.poewiki.net/wiki/Elemental_Weakness , https://www.poewiki.net/wiki/Vulnerability , https://www.poewiki.net/wiki/Despair (the level 1–20 ranges come from each page's infobox and 3.20.0 history; the per-level table is rendered from Cargo data, which was not retrieved).
- **Scorch** (§5.1) also lowers elemental resistances, by up to 30%, and is a plain reduction of the same kind.

**What overcapped resistance protects against (PoE1):**

| Mechanic | Acts on | Does overcap protect? |
|---|---|---|
| Curse "−X% to resistance" (Elemental Weakness, Despair) | uncapped value | **Yes**: absorbed until uncapped drops below the maximum |
| Exposure | uncapped value | **Yes** |
| Scorch and other flat "−X% to resistances" | uncapped value | **Yes** |
| "X% reduced resistances" (a percentage) | uncapped value, multiplied | **Partly**: overcap shrinks too, since it multiplies the sum. This follows PoB's model (`calcResistForType`: Σ BASE × max(mod, 0)). The wiki only says "The effects of x% reduced Resistance(s) is capped at 100%" |
| **Penetration** | **capped** value, at hit time | **No**: subtracted after the cap |
| Ignore resistance | treats resistance as 0 | **No** |
| −X% to *maximum* resistance (defender-side, from map mods and the like) | the cap itself | **No**: it lowers the ceiling, though a larger overcap keeps you at the new ceiling |

### 7.2 Worked example (PoE1): curse vs penetration against an overcapped defender

The defender has **110% uncapped fire resistance** and a 75% maximum, so they take 25%.

- Elemental Weakness (−30%): uncapped 80%, capped 75%. Damage taken **unchanged** at 25%; the overcap absorbed the curse.
- 30% fire penetration instead: 75 − 30 = 45% effective. They take **55%**, 2.2× more.
- Elemental Weakness −30% plus exposure −10%: uncapped 70%, so 70% effective, 30% taken.
- All three (curse, exposure and penetration): 70 − 30 = 40% effective, **60% taken**.
- Against a monster at exactly 40% resistance (no overcap): the curse takes it to 10%, exposure to 0%, and 30% penetration to −30%. It takes **130%**.

### 7.3 PoE2 resistance model

https://www.poe2wiki.net/wiki/Resistance , https://www.poe2wiki.net/wiki/Penetration , https://www.poe2wiki.net/wiki/Exposure , https://www.poe2wiki.net/wiki/Curse

- The same 75% default maximum and 90% hard maximum. The endgame elemental penalty is −60%. Curses and exposure lower **uncapped** resistance, so overcap protects: "A player with 150% uncapped fire resistance and a maximum resistance of 75% would have their effective resistance unchanged after being hit with -40%... since the remaining 110% is still enough."
- **Penetration differs:** it is applied last, "to a minimum of 0% resistance by default". It is "only effective against positive resistances; it has no effect once the effective resistance is 0 or negative. In exchange, it affects both maximum resistance and uncapped resistance." So it still ignores overcap. Leopold's Applause lowers the floor (the Exposure page says to −49%). **Second source:** [PoB2] `src/Modules/CalcOffence.lua` L466–468: `calcPenResist(resist, minPen, pen) = resist > minPen and max(resist − pen, minPen) or resist`.
- **Exposure (PoE2):** the Exposure page's lead says it "reduces resistances against all three elemental damage types". But the same page's 0.2.0f history row names separate **Fire Exposure** and **Lightning Exposure** supports, which implies element-specific debuffs still exist. Treat exposure as element-specific unless confirmed. Default **−20% for 4 s** ([W2 Resistance]). Since 0.3.0 it has **15% / 30% / 50% less effect on magic / rare / unique** monsters, and it can't be applied to enemies far above the skill's level.
- **Curses (PoE2, gem levels 1–20, from each page's `stat_text`):** Elemental Weakness **−(40–59)%** elemental resistances. Despair **−(35–49)%** chaos resistance (0.3.0: "−41 to −49% at Gem levels 9–20"). **Vulnerability now lowers armour, −(16–19,210) total armour**, instead of increasing damage taken. Curses apply after a 1.5 s delay, last 6–7.4 s, and have **15% / 30% / 50% less effect on magic / rare / unique** monsters (0.3.0). https://www.poe2wiki.net/wiki/Elemental_Weakness , https://www.poe2wiki.net/wiki/Despair , https://www.poe2wiki.net/wiki/Vulnerability
- **Worked example (PoE2):** a rare monster with 30% fire resistance. Elemental Weakness level 20: −59% × 0.7 (rare) = −41.3%, giving −11.3%. Adding 20% penetration does **nothing**, because resistance is already below 0. They take 111.3%. Without the curse, 20% penetration gives 30 − 20 = 10% (90% taken). 50% penetration gives max(30 − 50, 0) = **0%** (100% taken), not −20%.

### 7.4 Overwhelm (physical damage reduction's version of penetration)

- PoE1's `Overwhelm` wiki page describes the **Slayer notable** of that name, not this mechanic.
- **Base model:** physical damage reduction = armour's A/(A + 5D) (PoE1) or A/(A + 10D) (PoE2), **plus** "additional physical damage reduction" (additive). It is capped at **90%** for players and 75% for monsters ([PoB1]/[PoB2] `src/Data/Misc.lua` `maximum_physical_damage_reduction_%`), with a floor of 0%. https://www.poewiki.net/wiki/Damage_reduction ([GGG] cap: Mark_GGG, https://www.reddit.com/r/pathofexile/comments/h9rlc2/development_manifesto_game_balance_in_path_of/fuykttl/). The wiki's example: armour computing 100% is capped to 90%, and with the "−15% additional PDR" debuff Crushed the result is 85%, not 75%. **Additive reductions therefore subtract before the cap.**
- **Overwhelm in PoE1** is a rename of "Enemies have −X% to **Total** Physical Damage Reduction against your Hits". [GGG] Mark_GGG replied "It's this." to that suggestion: https://devtrackers.gg/pathofexile/p/60e27302-importing-the-new-tree-into-pob-reveals-a-new-stat-for-physical-damage-builds . It cannot push physical damage reduction below 0 ([GGG] Mark_GGG, cited by the Damage_reduction page: https://www.reddit.com/r/pathofexile/comments/fcojvn/question_regarding_enemies_have_to_total_physical/fjcel7v/).
- **Overwhelm in PoE2:** "directly ignores physical damage reduction similar to penetration, but cannot go below zero. Armour break, however, can go below zero." https://www.poe2wiki.net/wiki/Physical . PoE2 also has **Armour Break**, a debuff that lowers armour itself. At 0 armour the target is **Fully Broken** and takes 20% increased physical damage from hits (monsters only). Warbringer can push armour negative, which turns the reduction into an amplification at the same ratio. https://www.poe2wiki.net/wiki/Armour_Break
- **DISAGREEMENT (does overwhelm apply before or after the cap?):** [PoB1] offence (`src/Modules/CalcOffence.lua` L3552) adds overwhelm as a negative `EnemyPhysicalDamageReduction` **before** clamping to the cap. That would mean excess armour absorbs overwhelm, the way overcap absorbs curses. [PoB1] defence (`src/Modules/CalcDefence.lua` L2038–2039) does `max(min(armourDR + flatDR, cap) − overwhelm, 0)`, i.e. **after** the cap, like penetration. The PoE2 wiki's "similar to penetration" wording and PoB1's defence code suggest after the cap. PoB1's offence code does it before the cap. The stat name's "Total" is suggestive at most. (The Crushed example above uses a different stat, "−15% additional PDR", so it doesn't settle Overwhelm.) No GGG statement on the order was found, so this stays **unresolved**.

---

## 8. "Increased damage taken" and "less damage taken" (Shock, Vulnerability and others)

https://www.poewiki.net/wiki/Receiving_damage , https://www.poewiki.net/wiki/Damage_taken . [GGG] Mark_GGG: https://www.pathofexile.com/forum/view-thread/924683#p7847614

```
Taken = (MitigatedDamage + ΣFlat "±X damage taken from hits") × (1 + ΣIncreasedDamageTaken) × Π(1 + MoreOrLessDamageTaken_i)
```

- "Modifiers to 'damage taken'… apply to the value calculated after mitigation by resistances and armour." The order is flat, then the **summed** increased/reduced, then **each** more/less separately.
- **Increased damage taken (one pool, additive with each other):** PoE1 Shock (up to 50%), Vulnerability ((15–30)% increased **physical**), Withered (6% increased **chaos** per stack, up to 15 stacks, 2 s each; https://www.poewiki.net/wiki/Withered), Intimidate, Unnerve. Because the pool sits on the defender and comes after mitigation, it **multiplies** with every attacker increased/more modifier. That is why it is strong.
- **More/less damage taken (each multiplies):** Spell Suppression, Fortification, Arctic Armour, Toughness and so on.
- Type- or source-specific "taken" modifiers apply only to matching damage. For example, Vulnerability counts only for physical. PoB sums `DamageTaken` and `<Type>DamageTaken` into one increased pool per type ([PoB1] `src/Modules/CalcOffence.lua` L3521–3522, L3609).
- **DoTs are affected too:** changes in the defender's damage taken (Shock and the like) change existing DoT damage while it runs ([W1 Damage_over_time]). Shock does not boost the hit that applies it.
- **Worked example (PoE1):** 1,000 physical after resistance and armour. The defender has "−50 physical damage taken from hits", is Shocked for 30% and is cursed with level-20 Vulnerability (30% increased physical taken). It also has 20% less damage taken. Result: (1000 − 50) × (1 + 0.30 + 0.30) × 0.8 = 950 × 1.6 × 0.8 = **1,216**.
- **PoE2:** the same three-step order (https://www.poe2wiki.net/wiki/Receiving_damage). The increased-taken examples there are Shock, Withered and Intimidate. Then **Deflection** (by default 40% of a deflected hit's damage is prevented, §2.3) applies **after** the damage-taken modifiers. The page says it "still applies for ailments" (cited to a GGG staff answer posted as an image, https://imgur.com/a/gtY3b2B). PoE2 Shock is a flat **20%** increased damage taken (§5.2). PoE2 Vulnerability no longer touches damage taken (it lowers armour, §7.3). Fully Broken Armour gives monsters 20% increased physical damage taken from hits.

---

## 9. Attack and cast speed (brief)

- **PoE1:** attacks per second = (1 / base attack time of the weapon) × (1 + ΣIncreasedAttackSpeed) × Π(More). Unarmed base attack time is 0.83 s. Cast speed works the same way from the spell's cast time: 0.8 s base with 50% increased gives 1.25 × 1.5 = 1.88 casts/s, or 0.53 s per cast. **Action speed** (chill, Tailwind, freeze) multiplies the final speed separately. https://www.poewiki.net/wiki/Attack_speed , https://www.poewiki.net/wiki/Cast_speed
- **PoE2:** "skill speed" is the generic stat. Attack, cast, warcry and similar speeds add to it. It is a separate multiplier from action speed, which slows affect. Fixed use-time penalties ("+X seconds to Total Attack Time") are added after speed modifiers and cannot be sped up. https://www.poe2wiki.net/wiki/Skill_speed
- **Hit rate × hit damage = DPS:** expected DPS = average hit × (1 + c(M − 1)) × chance to hit × uses per second × hits per use. https://www.poewiki.net/wiki/Damage. This is an average; the per-hit order is in §1.
- **Example:** 0.8 s base → 1.25/s. 30% increased and 20% more gives 1.25 × 1.3 × 1.2 = 1.95/s. Chilled for 30% (less action speed): 1.365/s.

---

## 10. Disagreements, stale text and open questions (consolidated)

1. **PoE2 chance to hit when a player defends:** the poe2wiki Accuracy and Evasion pages show only `1.25A/(A + 0.3E)`. The 0.3.1 patch notes (GGG) and PoB2 use `1 − 0.95E/(E + 4A)` when a monster attacks a player or player minion, keeping the first formula for attacks on monsters. The wiki lags. (§2.3)
2. **PoE1 entropy reset:** the rule says >100 ticks (3.33 s); the PoE1 example says ">6 seconds". The PoE2 page says 3.33 s. (§2.2)
3. **PoE2 monster and minion crit bonus:** the wiki says minions +100% and monsters "40% less", with an editor's doubt. Game data via PoB2 says minions +70% and monsters +30%. (§3.7)
4. **PoE2 poison wording:** the Poison page reads as 20% total over 2 s. The Ailment table and game constants say 20% per second. (§4.4)
5. **PoE2 ailment roll, before or after mitigation:** the Ailment page says before. The Shock, Chill and Freeze pages say after. Receiving_damage places the roll after damage-taken modifiers. PoB2 uses unmitigated damage for shock and ignite chance. (§5.2)
6. **PoE2 chill formula and minimum:** the wiki gives no formula. PoB2 uses a linear 100 × D/T with a stale minimum check. The 0.3.0 discard rule (<30%) and the 30% default for damage-free chill are tagged undocumented/confirm. (§5.2)
7. **PoB2 shock display table** still uses PoE1's `50 × (D/T)^0.4`. It is probably legacy code; the wiki says shock is a flat 20%. (§5.2)
8. **Overwhelm, before or after the cap:** PoB1's offence and defence code disagree, and GGG has not stated the order. (§7.4)
9. **Stale wiki text:** Elemental Weakness "level 1 gives −20%" (now −15%). The Accuracy 3.7.0 "1.15 multiplier" row is history. The PoE1 enemy-ignite 50%/s is tagged `{{confirm}}`. The PoE1 `Damage` page is tagged `{{cleanup}}` and gives only an average-DPS sketch. The PoE2 Receiving_damage page carries an `{{update}}` banner saying it was adapted from PoE1. The PoE2 Ailment summary table lists an outdated shock duration (4 s; it is 8 s on monsters since 0.3.0).
10. **Community-only derivations** (no GGG statement found): the PoE1 3.16 evasion formula (u/esqtin, matched by PoB1); the PoE2 crit damage bonus order (NeverSink); that DoT multipliers are additive with each other (a Reddit thread).
11. **PoE2 exposure scope:** the lead says it covers "all three elemental damage types", but the page's own history names separate Fire and Lightning Exposure supports. (§7.3)
12. **Not retrieved:** the PoE1 per-level curse tables (only the level 1–20 ranges from the infobox and version history). No poe2wiki page exists for PoE2 "damage effectiveness", so PoE2's added-damage scaling is **not covered here**.

---

## 11. Translation notes for Return (mapping candidates, not decisions)

Each line gives a PoE mechanic, the one sentence that would explain the roll in a story log, and how well it fits a story simulation whose outcomes are dice rolls.

**Strong fits**

- **Overcapped resistance is the founder's "125% tolerance".** In PoE the part above the cap gives no extra protection by itself. It only **absorbs flat reductions** (curses, exposure, Scorch) before protection drops, and it does **nothing against penetration**, which is subtracted after the cap (PoE1). In PoE2, penetration also stops at 0. *"Kael's 125% adder tolerance soaked up the shaman's −20% venom hex, so the bite still could not kill him."* versus *"The basilisk's venom cut straight through his hardened blood."* This is the single most useful mapping. Designers would need to sort every tolerance-reducing effect into one of three classes: **reduction** (overflow absorbs it), **penetration** (ignores overflow; optionally can't go below base, as in PoE2) or **ceiling-lowering** (−maximum; overflow can't help). PoE's precedents: the default cap is 75%, the hard cap 90%, and true immunity is a **separate binary flag** ("Immune to X"), never 100% resistance (§7.1).
- **One increased pool versus separate more multipliers.** *"Her three small venom boons add together (+15%); the shrine's blessing multiplies on top."* This gives designers a vocabulary for stacking many narrow abilities without runaway growth: additive pools give diminishing returns. Note: for the founder's numbers, "+5 points per level from a 20% base" is **identical** to "25% increased per level" (20 × (1 + 0.25n) reaches 100% at n = 16). "25% **more** per level" compounds and reaches 100% at about n = 7.2. (§0.1)
- **Lucky and unlucky.** Lucky means roll twice and keep the better; unlucky, keep the worse; unexciting, roll three times and keep the middle. Binary chances become 2p − p², p² and 3p² − 2p³. *"Fortune favoured Mira: the bite was rolled twice and she kept the kinder outcome."* The effect is largest at mid-range chances (50% becomes 75% or 25%). It costs a fixed number of extra draws on the Dice stream. (§6)
- **Damage ÷ victim threshold.** PoE1 non-damaging ailments scale as 0.5 × (D/T)^0.4, discard below a minimum and cap at a maximum; freeze lasts 0.06 s per 1% of threshold. *"A grass snake's bite barely registers on an ox; the same bite fells a child."* The minimum-discard rule also keeps trivial events out of the log. PoE2 instead turns D/T into a **chance** (shock 25% per 100% of threshold, ignite 20%) or a **buildup meter** that fires at 100% (freeze, electrocute). Buildup maps onto "accumulated exposure", such as cold building toward hypothermia. (§5)
- **Ailment defence ladder** (https://www.poewiki.net/wiki/Ailment): reduced effect → chance to avoid → cannot be inflicted → **unaffected** (inflicted but does nothing) → **immune** (removes it). This is a ready-made rubric for tolerance tiers below and at full immunity. *"At level 5 Tam is 'unaffected' by adder venom: it is in his blood but does nothing."* Avoidance multiplies with the attacker's chance to inflict.
- **DoT snapshot, mitigated once.** A DoT's strength is fixed when applied. The attacker's later buffs don't change it, but the defender's changing mitigation does, live. *"The venom's strength was set when the fangs went in; his new tolerance, gained mid-fever, still eased it."* (§4.1)
- **Strongest-only versus stacking.** PoE1 ignite and bleed apply only the strongest instance, while poison stacks without limit. PoE2 poison has a default stack limit of 1. Strongest-only gives one-sentence explanations ("only the worst bite counts"). Unlimited stacks produce "death by a thousand cuts" stories that are harder to explain one roll at a time. (§4.2, §4.4)
- **Rarity scaling (PoE2):** curses and exposure have 15% / 30% / 50% less effect on magic / rare / unique monsters. *"The named wyrm shrugged off half the hex."* A natural rule for named creatures. (§7.3)
- **Armour against hit size:** A/(A + 5D) (PoE1) or A/(A + 10D) (PoE2). *"Thick hide turns a cat's scratch but not a bear's swipe."* (§1.5, §7.4)

**Usable with care**

- **Entropy.** A deterministic accumulator with a random starting phase: exactly 1 hit per N attempts, no streaks. *"Her third parry had to fail."* It reads well, but it lowers variance, which cuts against the founder's "very unpredictable". It also needs per-defender state that rewinds would have to snapshot. PoE resets it after 3.33 s of quiet. (§2.2)
- **The inherits rule (PoE1) versus final-type-only (PoE2).** In PoE1, converted damage remembers every type it passed through and benefits from each type's modifiers once ("a wound remembers what caused it"). PoE2 dropped this; only the final type counts. For narrow abilities, the question is whether a hybrid threat (a cursed snake's venom) triggers both tolerances, as in PoE1, or only the final classification, as in PoE2. (§1.4, §1.6)
- **Increased damage taken after mitigation** (Shock, Vulnerability): one additive pool on the defender that multiplies every attacker modifier. *"Fevered, she took 20% more from everything."* It is strong, because it stacks multiplicatively with offense. (§8)
- **One crit roll per action:** all hits from one use share it. *"The whole volley flew true or none of it did."* It keeps action outcomes coherent. (§3.1)
- **"Faster" ailments:** DPS × (1 + x) and duration ÷ (1 + x), so total damage is the same. *"The venom worked faster, not harder."* (§4.1)
- **Overflow precedent that converts.** PoE generally **wastes** overflow: chance to inflict above 100% (one poison per hit), crit chance above 100%, hit chance above 100%. Two exceptions: overcapped resistance (a buffer) and PoE2's Amazon "Critical Strike", which lets chance to hit exceed 100% (up to 125%) and converts 25% of the excess into base crit chance (120% gives +5%). https://www.poe2wiki.net/wiki/Critical_Strike . This supports the founder's wish that leveling past 100% should mean something.

**Poor fits**

- **Per-tick DoT and attacks-per-second math:** PoE assumes thousands of small rolls a minute. Return's rolls are few and life-or-death, so variance (and anything that changes it, such as luck and entropy) matters far more than averages.
- **Conversion order tables and per-type damage rolls:** high bookkeeping cost, and the result can't be explained in one sentence.
- **Accuracy distance falloff and per-patch tuned constants** (0.06 s, 0.4 exponent, 1.25 / 0.3 / 0.9): these are tuning, not rules worth copying. Treat them as illustrations of curve shapes (diminishing, capped, floored).
