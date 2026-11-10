# Path of Exile: defensive and mitigation math (PoE1 and PoE2)

Research notes for the Return ability and animal designs. Research only: no design decisions are made here, and the closing section only translates PoE terms into the founder's vocabulary.

- Fetched 2026-11-09. Game versions in the sources: the PoE1 wiki shows patch history through 3.29.0, the PoE2 wiki through 0.5.1.
- Unless stated otherwise, "PoE1" means current Path of Exile 1 and "PoE2" means current Path of Exile 2 Early Access.

## 0. Sources and how to read the citations

1. **poewiki.net** (PoE1) and **poe2wiki.net** (PoE2) are the community wikis. Their normal pages sit behind a bot challenge, so every page was read as raw wikitext through the MediaWiki API (`/w/api.php?action=query&prop=revisions`). The text is the same as the rendered page; only templates such as cargo tables are unexpanded. Citations use the normal page URL plus a section anchor.
2. **GGG patch notes** are cited by their official forum thread. The wiki's `Version_x.y.z` pages reproduce the notes verbatim and link the thread. Patch notes are the only first-party statements of formulas.
3. **Path of Building (PoB)** is the second, independent source. PoB-Community (PoE1) and PoB-PoE2 are open-source calculators. Their `src/Data/Misc.lua` holds constants datamined from the game files (`Metadata/Characters/Character.ot` and others), and `src/Modules/CalcDefence.lua` holds the defence math. Where wiki prose and a datamined constant disagree, these notes trust the constant and flag the disagreement. Files were read from the `dev` branch on 2026-11-09, so line numbers may drift; function names are stable.
   - PoE1: <https://github.com/PathOfBuildingCommunity/PathOfBuilding/blob/dev/src/Modules/CalcDefence.lua>, <https://github.com/PathOfBuildingCommunity/PathOfBuilding/blob/dev/src/Data/Misc.lua>, <https://github.com/PathOfBuildingCommunity/PathOfBuilding/blob/dev/src/Modules/Data.lua>
   - PoE2: <https://github.com/PathOfBuildingCommunity/PathOfBuilding-PoE2/blob/dev/src/Modules/CalcDefence.lua>, <https://github.com/PathOfBuildingCommunity/PathOfBuilding-PoE2/blob/dev/src/Data/Misc.lua>, <https://github.com/PathOfBuildingCommunity/PathOfBuilding-PoE2/blob/dev/src/Modules/Data.lua>
4. Fandom (pathofexile.fandom.com) was not needed; poewiki answered everything.

Notation: `D` is incoming damage, `A` is armour, `E` is evasion rating, `Acc` is the attacker's accuracy, and `R` is resistance as a fraction. "Uncapped" resistance is the sum of all resistance stats. "Effective" resistance is the value actually used in the damage formula.

All worked-example numbers come from a script (`scripts/examples.py` in this folder) so the arithmetic is exact.

---

## 1. The brief got these wrong (read first)

1. **Endurance charges no longer grant elemental resistance in PoE1.** Patch 3.25.0: "Endurance Charges on Players and Minions no longer grant 4% to all Elemental Resistances per Endurance Charge. They now instead grant 4% additional Elemental Damage Reduction per Endurance Charge." Each charge now gives +4% additional physical damage reduction and +4% additional elemental damage reduction.
   - Sources: [3.25.0 patch notes](https://www.pathofexile.com/forum/view-thread/3531661) (wiki copy: <https://www.poewiki.net/wiki/Version_3.25.0>), [Endurance charge, version history](https://www.poewiki.net/wiki/Endurance_charge#Version_history). PoB datamined `physical_damage_reduction_%_per_endurance_charge = 4` and `elemental_damage_reduction_%_per_endurance_charge = 4` (PoE1 `Misc.lua`, `data.characterConstants`).
   - Monsters (not minions) still get 15% physical damage reduction and +15% to all elemental resistances per charge. PoB lists 4 + 11 PDR and `resist_all_elements_%_per_endurance_charge_if_not_player_minion = 15`.
   - In PoE2, endurance charges grant **nothing by themselves**: skills consume them ([poe2wiki Endurance charge](https://www.poe2wiki.net/wiki/Endurance_charge)).
2. **Overcapping does not protect against "reduced maximum resistance" map mods.**
   - Overcap is resistance above your maximum. It buffers **flat subtractions from resistance**: curses, exposure, Scorch, and the act penalty, which is already counted inside "uncapped".
   - A map mod that lowers *maximum* resistance lowers the cap itself. With 120% uncapped and the maximum dropped to 65%, you sit at 65% whatever your overcap.
   - The only buffer against maximum-resistance reduction is pushing *maximum* resistance past the 90% hard cap, which is a different stat. The wiki's example: +20% maximum gives 95, capped to 90, which absorbs up to 5 points of maximum-resistance reduction.
   - Penetration is never buffered by overcap (section 3.5).
3. **The PoE2 wiki's evasion formula is stale on the defender side.** Since PoE2 0.3.1, hits against players use `ChanceToHit = 1 − 0.95·E / (E + 4·Acc)`. The wiki's `1.25·Acc / (Acc + 0.3·E)` is now only the formula for hits against monsters (section 5.1).
4. **Spell suppression no longer "halves" spell damage.** It prevented 50% from 3.16.0 to 3.26; since 3.27.0 it prevents **40%** ([3.27.0 patch notes](https://www.pathofexile.com/forum/view-thread/3869068)).
5. **"Chaos damage bypasses energy shield" is PoE1 only.** In PoE2, chaos damage hits energy shield normally but removes **twice** as much of it. In PoE2 it is bleeding and poison damage over time that bypass energy shield (section 6.2).
6. **Dodge is legacy.** Attack dodge was removed in 3.16.0. Spell dodge survives only through the Acrobatics keystone (section 5.4).
7. **"Immunity is a 100% maximum" is not how PoE works.** Maximum resistance can never exceed 90%. Immunity (Chaos Inoculation, "Immune to X") is a separate yes-or-no stat that ignores both resistance reduction and penetration. See section 13.

---

## 2. Order of operations (the defensive pipeline)

### PoE1 ([Receiving damage, Order of operations](https://www.poewiki.net/wiki/Receiving_damage#Order_of_operations))

1. **Preventing the hit.** Projectile avoidance, then evasion (attacks only), then spell dodge. "These are the only defensive mechanics that prevent hits entirely."
2. **Before-being-hit effects.**
3. **Unmitigated damage** on the attacker's side: flat damage, then conversion, then increased/more multipliers, then the critical strike. Against an attack crit, evasion is rolled a second time and a success downgrades the crit to a normal hit. Then the damage roll, then double or triple damage.
4. **"Cannot take X damage."**
5. **Damage taken as** (shift between damage types).
6. **Mitigation:**
   1. "Cannot take X damage" again.
   2. Immunity, such as Chaos Inoculation.
   3. Damage avoidance (chance to avoid damage from hits).
   4. **Resistances**, with the attacker's penetration.
   5. **Damage reduction**: armour plus "additional" damage reduction, calculated per damage type. "Damage reduction occurs after resistances."
7. **Damage-taken modifiers**, in this order: flat `±X damage taken`, then the **sum** of increased/reduced damage taken, then each more/less multiplier **multiplicatively**. Spell suppression, Fortification and Toughness live in the more/less step.
8. **Stun** is rolled here.
9. Damage from multi-part skills is combined.
10. **Block** rolls last, against all damage, and prevents most on-hit effects.
11. **Final damage taken.** Recoup and "prevented damage" are measured here.
12. **Resource loss, in order:**
    1. Damage redirected to allies, guard buffs, aegises.
    2. Ward.
    3. Energy shield. Chaos damage bypasses it by default.
    4. Mana before life (Mind over Matter).
    5. Life.
    6. Life-loss prevention effects.

### PoE2 ([poe2wiki Receiving damage](https://www.poe2wiki.net/wiki/Receiving_damage#Order_of_operations))

The page carries an `{{update}}` banner: "adapted from the sister page on poewiki. Mechanics may need to be re-tested."

1. **Avoiding the hit:** movement, dodge-roll invulnerability frames, projectile avoidance, evasion. Evasion is checked again to downgrade crits.
2. Cannot take damage, then damage taken as (shift).
3. **Mitigation:** cannot take X, then immunity, then damage avoidance, then **damage reduction (armour and additional physical damage reduction)**, then **resistances**. "Damage reduction occurs before resistances, unlike in POE1." Penetration applies to the final resistance and "cannot penetrate below 0%".
4. Damage-taken modifiers: flat, then increased/reduced (summed), then more/less (multiplied).
5. **Deflection** ("reduces damage after damage taken modifiers, but still applies for ailments").
6. **Stun, pin and ailment buildup** are rolled.
7. **Block.** "Block does not prevent stun or freeze."
8. Final damage, then resources: redirection, guard, **energy shield** (chaos removes double; bleeding and poison bypass it), mana before life, life-loss prevention, life.

PoB-PoE2 adopted armour-before-resistances in commit [`4c809619`](https://github.com/PathOfBuildingCommunity/PathOfBuilding-PoE2/commit/4c809619e1b48796c160285fb82c39ffe2be34fe) ("Apply armour before resistances (#57)"). PoB-PoE1 computes armour against the post-resistance hit: `calcs.armourReductionF(effectiveAppliedArmour, damage * totalResistMult)`.

### Damage over time follows the same path minus everything hit-only

DoT skips evasion, armour, block, ward and penetration. Damaging ailments compute their base from the hit's **pre-mitigation** flat damage, then mitigation applies to the DoT itself. The defender's resistance therefore applies once to the hit and once to the DoT, never twice to the same damage ([Receiving damage, Damage over time](https://www.poewiki.net/wiki/Receiving_damage#Damage_over_time)).

### Why the order matters

Armour's percentage depends on hit size (section 4).
- PoE1 applies resistances first. Where armour also covers elemental damage, it sees a smaller hit and reduces it by a larger percentage.
- PoE2 applies armour to the raw hit.
- Both games put flat "damage taken" first, increased/reduced next, and more/less last.

---

## 3. Resistances (fire, cold, lightning, chaos)

### 3.1 Core formula

```
damage_taken = D × (1 − R_effective)
R_effective  = min(R_uncapped, R_max) − penetration          (penetration: hits only)
```

The formula is given at [Resistance, Damage mitigation](https://www.poewiki.net/wiki/Resistance#Damage_mitigation) and [Resistance penetration](https://www.poewiki.net/wiki/Resistance_penetration#Mechanics). The PoE2 version, `Final Damage = D × (100 − R)/100`, is at [poe2wiki Resistance, Calculation](https://www.poe2wiki.net/wiki/Resistance#Calculation).

- Resistance applies to hits **and** damage over time ([Resistance, Mechanics](https://www.poewiki.net/wiki/Resistance#Mechanics)). Physical damage has no resistance; it uses damage reduction instead.
- Negative resistance increases damage taken. The wiki's own ratio: −60% against 75% takes `1.6 / 0.25 = 6.4×` the damage.

### 3.2 Caps

| Rule | PoE1 | PoE2 |
|---|---|---|
| Default maximum | 75% ([Resistance](https://www.poewiki.net/wiki/Resistance#Mechanics); PoB `base_maximum_all_resistances_% = 75`) | 75%, "soft capped" ([poe2wiki](https://www.poe2wiki.net/wiki/Resistance); PoB same) |
| Raising the maximum | `+X% to maximum [type] resistance`: Purity of Fire/Ice/Lightning, Ruby/Sapphire/Topaz flasks, passives, items ([Resistance, Maximum resistances](https://www.poewiki.net/wiki/Resistance#Maximum_resistances)) | same idea |
| Hard cap on the maximum | **90%**. Added in 3.6.0: "You can no longer have your Maximum Resistances (and therefore your Resistances) raised above 90% from any source" ([3.6.0 notes](https://www.pathofexile.com/forum/view-thread/2333648)). PoB `MaxResistCap = 90`. | 90% ([poe2wiki](https://www.poe2wiki.net/wiki/Resistance)); PoB `MaxResistCap = 90` |
| Floor | **−200%**. 3.16.0: "Resistances can no longer be lowered to below −200%" ([3.16.0 notes](https://www.pathofexile.com/forum/view-thread/3186390), section "Negative Resistances"). PoB `ResistFloor = −200`. | PoB `ResistFloor = −200`; the wiki prose is silent |
| `X% reduced resistances` | Capped at 100% reduced (3.17.0), which pins resistance at 0 ([Resistance, Increased/Reduced](https://www.poewiki.net/wiki/Resistance#Increased/Reduced_resistances)) | not documented |

Excess maximum resistance above 90 still counts as a buffer: "a character with +20% maximum resistances from items will effectively go from 75% to 90% max res, and can ignore up to 5% max res reduction from debuffs and/or map modifiers before the max res value is reduced below 90%" ([Resistance, Maximum resistances](https://www.poewiki.net/wiki/Resistance#Maximum_resistances)).

### 3.3 Campaign penalties

**PoE1** ([Resistance, Resistance penalties](https://www.poewiki.net/wiki/Resistance#Resistance_penalties)):
- "A permanent penalty to all resistances (**including Chaos Resistance**)": −30% on completing Part One (Act 5), −30% more on completing Part Two (Act 10), **−60% total**.
- Moved to Act 5 and Act 10 completion in 3.0.0; previously tied to the Cruel and Merciless difficulties ([3.0.0 notes](https://www.pathofexile.com/forum/view-thread/1930316)).
- The penalty is part of "uncapped" resistance, so gear must supply 135% to sit at 75% after the −60%.

**PoE2** ([poe2wiki Resistance, Elemental resistance penalties](https://www.poe2wiki.net/wiki/Resistance#Elemental_resistance_penalties)): **elemental only, chaos is not penalised**.

| Progression | Penalty |
|---|---|
| Act 2 | −10% |
| Act 3 | −20% |
| Act 4 | −30% |
| Interlude, area level 54–59 | −40% |
| Interlude, area level 60–64 | −50% |
| Endgame, area level 65+ | −60% |

0.3.0 changed post-Act 4 penalties to scale by area level. 0.1.0e removed the extra map-tier penalties.

### 3.4 Uncapped and overcapped

- **Uncapped** is the sum of all resistance stats, including the act penalty. It is "the value Resistance would have if ignoring Maximum Resistance" (in-game alt text).
- **Overcapped** is "the amount by which Uncapped Resistance is higher than Maximum Resistance" (in-game alt text, quoted at [Resistance, Overcapped resistances](https://www.poewiki.net/wiki/Resistance#Overcapped_resistances)).
- "Overcapped resistances serve as a buffer against resistance reduction such as curses and exposure before they can reduce your resistances below your maximum resistances."
- "Resistance reductions such as curses and exposure directly affect uncapped resistance values" ([Resistance, Uncapped resistances](https://www.poewiki.net/wiki/Resistance#Uncapped_resistances)).
- PoE2 says the same: "debuffs from both exposure and curses affect uncapped resistance, regardless of maximum resistance." Its example: 150% uncapped fire against a 75% maximum, hit by −40% from a curse, still has 110%, so effective resistance is unchanged ([poe2wiki Resistance, Curses](https://www.poe2wiki.net/wiki/Resistance#Curses)).

### 3.5 The four ways resistance is lowered, and what overcap does against each

| Mechanism | How it applies | Buffered by overcap? |
|---|---|---|
| **Flat `−X% to resistance`** (curses, exposure, Scorch, the act penalty) | Subtracted from **uncapped** | **Yes, fully** |
| **`X% reduced resistances`** (some uniques) | Multiplies uncapped. Increased and reduced sum additively with each other; "less" multiplies. Capped at 100% reduced ([Resistance, Increased/Reduced](https://www.poewiki.net/wiki/Resistance#Increased/Reduced_resistances)). | **Partly**: 50% reduced turns 150 into 75, still capped, but turns 100 into 50 |
| **`−X% to maximum resistance`** (map mods, some debuffs) | Lowers the cap | **No.** Only maximum resistance above 90 buffers it. |
| **Penetration** (attacker stat, hits only) | `min(R_uncapped, R_max) − pen`, applied to the **capped** value "as damage is dealt, after other resistance calculations" | **No, never** ([Resistance, Resistance penetration](https://www.poewiki.net/wiki/Resistance#Resistance_penetration), citing [Mark_GGG 2012](http://www.pathofexile.com/forum/view-thread/59271/page/2#p751660)) |

Penetration details:
- **PoE1:** effective resistance has **no floor**. Penetration can push past −200% because it does not modify the stat ([Resistance penetration](https://www.poewiki.net/wiki/Resistance_penetration#Mechanics)). Penetration cannot apply to DoT. It is worth more against high resistance: damage multiplier `= 1 + p / (1 − min(r, m))`, so 10% penetration means ×1.10 against 0%, ×1.20 against 50% and ×1.40 against 75%.
- **PoE2:** penetration "cannot penetrate below 0%. If the resistance is at 0% or negative, penetration has no effect" ([poe2wiki Receiving damage](https://www.poe2wiki.net/wiki/Receiving_damage#Order_of_operations); [poe2wiki Penetration](https://www.poe2wiki.net/wiki/Penetration): "to a minimum of 0% resistance by default", with a `{{confirm}}` tag on the ordering sentence).

Other resistance modifiers:
- **Ignore resistance:** treat resistance as 0% and discard all modifications ([Resistance, Ignore resistances](https://www.poewiki.net/wiki/Resistance#Ignore_resistances)).
- **Invert:** multiply the capped resistance by −1, still subject to caps ([Resistance, Resistance inversion](https://www.poewiki.net/wiki/Resistance#Resistance_inversion)).
- **Immunity** is not resistance at all. Chaos Inoculation's chaos immunity ignores chaos resistance reduction **and** penetration ([Chaos Inoculation](https://www.poewiki.net/wiki/Chaos_Inoculation), citing [Dan_GGG 2016](https://www.pathofexile.com/forum/view-thread/1586022#p12817748)).

Typical sizes of resistance reductions:
- **Exposure, PoE1:** −10% when inflicted by players and −25% when inflicted by enemies. Only the strongest per element applies. It lasts 4 seconds by default ([Exposure](https://www.poewiki.net/wiki/Exposure#Mechanics)).
- **Exposure, PoE2:** −20% for 4 seconds by default ([poe2wiki Resistance](https://www.poe2wiki.net/wiki/Resistance#Elemental_Exposure)). It has 15%, 30% and 50% less effect on magic, rare and unique monsters ([poe2wiki Exposure](https://www.poe2wiki.net/wiki/Exposure)).
- **PoE2 curses:** Elemental Weakness inflicts −40% to −59% elemental resistance by gem level ([poe2wiki Elemental Weakness](https://www.poe2wiki.net/wiki/Elemental_Weakness)). Despair inflicts −41% to −49% chaos resistance at gem levels 9–20 ([0.3.0 notes](https://www.pathofexile.com/forum/view-thread/3826682)).

### 3.6 Worked example (PoE1, a 1,000 fire hit)

| Defender | Effective resistance | Damage taken |
|---|---|---|
| 75% uncapped, 75% maximum | 75% | 250 |
| 120% uncapped (45 overcap), enemy fire exposure −25 makes uncapped 95 | 75% | **250**: the overcap absorbed the exposure |
| same, and the hit has 30% fire penetration | 75 − 30 = 45% | **550**: overcap does nothing against penetration |
| 75% uncapped with no overcap, exposure −25 | 50% | 500 |
| same, plus 30% penetration | 20% | 800 |
| 120% uncapped, maximum raised to 80% | 80% | 200 |
| 120% uncapped, maximum raised to 90% | 90% | 100 |
| 120% uncapped, map mod `−10% maximum` (maximum 65) | 65% | **350**: overcap does not help |
| +20% maximum (95, capped 90), then `−10% maximum` | 85% | 150: excess maximum is the buffer |
| −60% | −60% | 1,600 |
| −200% (floor) | −200% | 3,000 |

The leverage near the cap: moving maximum resistance from 75 to 80 cuts damage taken by 20% (250 to 200), and 75 to 90 cuts it by 60%.

---

## 4. Armour and physical damage reduction

### 4.1 PoE1 formula

```
DR(A, D)  = A / (A + 5·D)              per hit; D = raw physical damage of that hit
A needed  = DR · 5·D / (1 − DR)
D_net     = 5·D² / (A + 5·D)
prevented = A·D / (A + 5·D)  →  approaches A/5 as D → ∞
```

All four forms are on [Armour, Formulas](https://www.poewiki.net/wiki/Armour#Formulas), derived on [Guide:Armour calculations](https://www.poewiki.net/wiki/Guide:Armour_calculations).
- Second source: PoB-PoE1 `calcs.armourReductionF` returns `armour / (armour + raw * 5) * 100` (CalcDefence.lua, line 67).
- First-party: the [3.16.0 notes](https://www.pathofexile.com/forum/view-thread/3186390) say "if you want to get 50% mitigation against a physical damage hit, you need 5 times the damage as Armour where previously it required 10 times the damage. As an example, if you want to mitigate a 5,000 hit down to 2,500, you used to need 50,000 Armour to do this. Now you only need 25,000 Armour."
- History: 0.9.3 introduced the ratio form, `A / (A + 25·D)`. The formula immediately before 3.16.0 was `A / (A + 10·D)`, per the 3.16.0 notes; when 25 became 10 was not traced. 3.16.0 onward uses `A / (A + 5·D)`.

Rule of thumb ([Armour, Rule of thumb](https://www.poewiki.net/wiki/Armour#Rule_of_thumb)): armour equal to 2.5× the hit blocks 33%, 5× blocks 50%, 10× blocks 66%, 15× blocks 75%, and 45× blocks 90%.

### 4.2 Why big hits are reduced less

DR depends on the ratio `A / D`, so for fixed armour the percentage falls as the hit grows, and the absolute amount prevented approaches `A/5`. "Armour is more effective against many smaller hits than fewer larger hits, even if both scenarios represent the same total damage" ([Armour, Mechanics](https://www.poewiki.net/wiki/Armour#Mechanics)).

### 4.3 Caps and other sources of physical damage reduction (PoE1)

- **Cap 90%** for players: [Damage reduction](https://www.poewiki.net/wiki/Damage_reduction#Mechanics), citing Mark_GGG; PoB datamined `maximum_physical_damage_reduction_% = 90`. The floor is 0%.
- **Monsters cap at 75%**: PoB `data.monsterConstants`, `maximum_physical_damage_reduction_% = 75`.
- **Additional physical damage reduction is additive with armour's DR**, and the cap applies **after** all modifiers. The example: armour computes 100%, which caps to 90; with Crushed (−15% PDR) the result is `min(100 − 15, 90) = 85%`, not 75% ([Damage reduction](https://www.poewiki.net/wiki/Damage_reduction#Mechanics); [Crushed](https://www.poewiki.net/wiki/Crushed)). Excess DR therefore buffers debuffs exactly like resistance overcap.
- Sources of additional PDR:
  - Endurance charges, 4% each.
  - Strength body-armour suffixes.
  - Others listed on [Damage reduction](https://www.poewiki.net/wiki/Damage_reduction). PoE1 also has a chance to *ignore* PDR on hits.
- **Armour applies only to hits**, never to DoT. **Additional PDR does apply to physical DoT** such as bleeding ([Armour, Mechanics](https://www.poewiki.net/wiki/Armour#Mechanics); [Damage reduction](https://www.poewiki.net/wiki/Damage_reduction#Mechanics): "Damage reduction can mitigate damage over time, though it can only be gained from armour against hits").
- Armour can be made to cover other damage types (Transcendence, Doryani's Prototype and others). It then computes **separately per damage type** against that type's portion of the hit.

### 4.4 PoE2

```
DR(A, D) = A / (A + 10·D)      cap 90%; prevented approaches A/10
```

- Source: [poe2wiki Armour, Armour formulas](https://www.poe2wiki.net/wiki/Armour#Armour_formulas). The wiki's own citation is a YouTube video.
- Second source: PoB-PoE2 `calcs.armourReductionF` uses `armour / (armour + raw * data.misc.ArmourRatio)` with **`ArmourRatio = 10`** (Data.lua, line 261). PoB datamined `maximum_physical_damage_reduction_% = 90`.
- Rule of thumb: 5× blocks 33%, 10× blocks 50%, 20× blocks 66%, 30× blocks 75%, 90× blocks 90%.
- 0.1.1 made armour "more effective, by approximately 15%" ([0.1.1 notes](https://www.pathofexile.com/forum/view-thread/3696353)).
- Armour cannot reduce a hit below 1 damage (0.4.0).
- **Armour Break** subtracts armour. At 0 the target has "Fully Broken Armour", under which **monsters** take 20% increased physical damage from hits; this does not apply to players. Warbringer's Imploding Impacts can break armour below 0, turning DR into a damage multiplier ([poe2wiki Armour Break](https://www.poe2wiki.net/wiki/Armour_Break)).
- **Overwhelm** "directly ignores physical damage reduction similar to penetration, but cannot go below zero" ([poe2wiki Physical](https://www.poe2wiki.net/wiki/Physical)). For PoE1, PoB's maximum-hit code also subtracts the enemy's overwhelm from the *capped* DR: `(DamageReductionMax − enemyOverwhelmPercent)`.
- Bleeding is reduced by additional PDR but not by armour.

### 4.5 Worked example (10,000 armour)

| Raw physical hit | PoE1 DR (A/(A+5D)) | PoE1 taken | PoE2 DR (A/(A+10D)) | PoE2 taken |
|---|---|---|---|---|
| 100 | 95.2%, capped to 90% | 10 | 90.9%, capped to 90% | 10 |
| 1,000 | 66.7% | 333 | 50.0% | 500 |
| 5,000 | 28.6% | 3,571 | 16.7% | 4,167 |
| 10,000 | 16.7% | 8,333 | 9.1% | 9,091 |
| 50,000 | 3.8% (prevents 1,923; limit 2,000) | 48,077 | 2.0% (prevents 980; limit 1,000) | 49,020 |

A PoE1 hit of 1,000 against 3 endurance charges (+12%) gives `66.7 + 12 = 78.7%` DR, so 213 damage is taken.

---

## 5. Hit avoidance and chance layers

### 5.1 Evasion (defender's side)

**PoE1** ([Evasion, Mechanics](https://www.poewiki.net/wiki/Evasion#Mechanics); [Accuracy](https://www.poewiki.net/wiki/Accuracy#Mechanics)):

```
ChanceToHit = clamp( 1.25·Acc / (Acc + (E/5)^0.9) , 5%, 100% )      → evade cap 95%
```

- The formula is community-derived (u/esqtin, 2021, after the 3.16.0 evasion buff), and the wiki cites it.
- Second source: PoB-PoE1 `calcs.hitChance` computes `accuracy / (accuracy + (evasion / 5) ^ 0.9) * 125`, clamped to 5–100 (CalcDefence.lua, line 58). PoB `EvadeChanceCap = 95`.
- **Spells cannot be evaded** in PoE1. Only attacks can.
- An attack crit that lands must pass a second, independent evasion roll, or it is downgraded to a normal hit.
- **Entropy, not dice:**
  1. On the first attack, or after 100 server ticks (3.33 s) without one, entropy is seeded randomly in 0–99.
  2. Each attack adds the attacker's hit chance, as an integer, to entropy.
  3. Entropy of 100 or more is a hit and has 100 subtracted; below 100 is an evade.
  - So "if a player has a 1 in N chance to be hit, then the player will be hit exactly once for every N consecutive attacks". All attackers share one counter.
  - The crit re-roll is a separate random roll and does not touch entropy.
- Worked example: a level-84 monster has 538 accuracy (PoB `monsterAccuracyTable`, matching the wiki). Against it, 5,000 evasion evades 35.3%, 20,000 evades 70.5%, and 40,000 evades 82.3%.

**PoE2:**
- **Hits against players, since 0.3.1:** `ChanceToHit = 1 − 0.95·E / (E + 4·Acc)`. "The chance to hit formula vs Monsters is unchanged."
  - Sources: [0.3.1 notes](https://www.pathofexile.com/forum/view-thread/3862213) (wiki copy: <https://www.poe2wiki.net/wiki/Version_0.3.1>).
  - Second source: PoB-PoE2 `calcs.monsterHitChance` (CalcDefence.lua, line 41), added in [PR #1485](https://github.com/PathOfBuildingCommunity/PathOfBuilding-PoE2/pull/1485), commit `cf4837a682`.
  - As E grows, chance to hit approaches 5%, so evade approaches 95% (PoB `DefaultMaxEvadeChancePercent = 95`).
- **Hits against monsters:** `ChanceToHit = 1.25·Acc / (Acc + 0.3·E)`, clamped at a minimum of 5% ([poe2wiki Evasion](https://www.poe2wiki.net/wiki/Evasion#Mechanics); PoB `calcs.hitChance`, line 33). **The wiki presents this as the general formula; it is stale for the defender side** (section 12).
- Player base evasion is 7 since 0.3.1 (it was 27 and scaled with level).
- Player evasion "works on all hits except boss skills that have a red flash" since 0.3.0, which now includes spells.
- The same entropy system and crit re-roll apply.
- Worked example (hits against players): E = 4,000 against Acc = 1,000 gives a 52.5% chance to hit (47.5% evade). E = 12,000 against Acc = 1,000 gives 28.75% (71.25% evade).

### 5.2 Block

**PoE1** ([Blocking](https://www.poewiki.net/wiki/Blocking#Chance_to_block)):
- Attack block and spell block are separate stats.
- Each has a default maximum of **75%** (datamined `maximum_block_% = 75`, `base_maximum_spell_block_% = 75`).
- The **hard cap is 90%** (3.24.0: "Maximum chance to block Attack Damage or Spell Damage can no longer be raised above 90%", [3.24.0 notes](https://www.pathofexile.com/forum/view-thread/3496784); PoB `BlockChanceCap = 90`).
- Block is **an independent random roll per hit, not entropy**. It rolls **after** all other mitigation and after stun.
- A block prevents all damage and most on-hit effects, including ailments, unless the defender has "You take X% of Damage from Blocked Hits". Glancing Blows doubles block chance and takes 65% of blocked damage ([Glancing Blows](https://www.poewiki.net/wiki/Glancing_Blows)).
- Block does nothing against DoT.

**PoE2** ([poe2wiki Block](https://www.poe2wiki.net/wiki/Block)):
- One block stat covers every hit that is not unblockable (red flash) since 0.3.0.
- The default maximum is **50%** since 0.3.0 (was 75%; datamined `object_inherent_base_maximum_block_%_from_ot = 50`). Turtle Charm sets the maximum to 75% and makes you take 30% of blocked damage.
- PoB carries `BlockChanceCap = 90`; the PoE2 wiki states no hard cap.
- Block does **not** prevent stun or freeze buildup and is disabled while stunned or frozen.
- Active block (Raise Shield, Parry) is directional and guaranteed, and builds heavy-stun.

Worked example: against blockable hits, average damage taken is `(1 − block)`. That is 0.60 at 40%, 0.25 at 75% and 0.10 at 90%, so raising the cap from 75 to 90 cuts average blockable damage by 60%. With Glancing Blows, 30% doubles to 60% and blocked hits deal 65%, for an average of `0.4 + 0.6 × 0.65 = 0.79`.

### 5.3 Spell suppression (PoE1 only)

Source: [Spell suppression](https://www.poewiki.net/wiki/Spell_suppression).
- A chance per **spell hit**, which can stack to **100%** (PoB `SuppressionChanceCap = 100`).
- A suppressed hit has **40%** of its damage prevented since 3.27.0 (50% from 3.16.0 to 3.26). `Prevent +X% of Suppressed Spell Damage` raises the amount.
- The same reduction applies to ignite and poison inflicted by the suppressed hit. Bleeding cannot come from spells.
- Ordering: "after resistances and damage reduction, but before stun and block". It is a more/less damage-taken multiplier.
- Introduced in 3.16.0 as the Dexterity answer to spells, replacing dodge.
- Example: a 1,000 fire spell hit against 75% resistance and 100% suppression deals `250 × 0.6 = 150` (125 before 3.27).

### 5.4 Legacy dodge (PoE1)

Source: [Dodge](https://www.poewiki.net/wiki/Dodge); [3.16.0 notes](https://www.pathofexile.com/forum/view-thread/3186390).
- Before 3.16.0 there were attack dodge and spell dodge. Each was capped at 75%, rolled randomly (not entropy), ignored accuracy, and prevented the hit entirely.
- 3.16.0 removed attack dodge. Spell dodge now exists only through the Acrobatics keystone, which converts spell-suppression modifiers into spell dodge at 50% of their value, still capped at 75%.
- Example (pre-3.16): 40% dodge means 60% of hits land, with no smoothing.

### 5.5 Deflection (PoE2 only)

Source: [poe2wiki Deflection](https://www.poe2wiki.net/wiki/Deflection); [0.5.0 notes](https://www.pathofexile.com/forum/view-thread/3932540).

```
ChanceToDeflect = min( 150% × (1 − Acc / (Acc + 0.12·Deflection)) , 95% )      (0.5.0 formula)
```

- A deflected hit takes **40% less** damage. Datamined `BasePercentDamageDeflected = 40`; Tailwind raises it to 50% at 10 stacks.
- Deflection works against red-flash boss hits, unlike evasion. It applies after damage-taken modifiers but still reduces ailments.
- Second source: PoB-PoE2 `calcs.deflectChance`, line 49.
- Example: Acc = 2,000 against 10,000 deflection rating gives a 56.25% chance, for an average damage multiplier of `1 − 0.5625 × 0.40 = 0.775`.

### 5.6 Damage avoidance (PoE1)

Source: [Avoidance](https://www.poewiki.net/wiki/Avoidance#Damage_avoidance).
- `X% chance to Avoid [type] Damage from Hits` reduces that portion to 0 but does **not** undo the hit, so on-hit effects still happen.
- **Hard cap 75%** (PoB `AvoidChanceCap = 75`).
- Since 3.29.0, avoid-all and avoid-type chances add together.

---

## 6. Pools and recovery

### 6.1 Life

PoE1 ([Life, Calculation](https://www.poewiki.net/wiki/Life#Calculation)):

```
Life = (38 + level × 12 + Str/2 + flat) × (1 + Σincreased − Σreduced) × Π(1 + more) × Π(1 − less)
```

- 12 per level is datamined `life_per_level = 12`.
- Low life means 50% or less in PoE1 and 35% or less in PoE2.
- PoE2: 28 life at level 1, +12 per level, +2 per Strength ([poe2wiki Life](https://www.poe2wiki.net/wiki/Life)).

### 6.2 Energy shield

**PoE1** ([Energy shield](https://www.poewiki.net/wiki/Energy_shield#Mechanics)):
- An extra pool depleted before life. Mitigation is applied before damage reaches energy shield.
- **Chaos damage bypasses energy shield** by default; listed items and masteries change this.
- Recharge starts after **2 s** with no damage. The delay is `200 / (100 + faster_start%)` seconds.
- Recharge runs at **33% of maximum per second** (3.16.0; was 20%; datamined 2000%/min).
- While any energy shield remains, a separate 50% roll ignores stuns.

**PoE2** ([poe2wiki Energy Shield](https://www.poe2wiki.net/wiki/Energy_Shield); [poe2wiki Receiving damage](https://www.poe2wiki.net/wiki/Receiving_damage)):
- **Chaos damage removes twice as much energy shield** but does not change the damage dealt. When one instance spans energy shield and life, the life portion is not doubled. Confirmed by GGG staff (Viperesque) on Discord, as cited by the wiki.
- **Bleeding and poison bypass energy shield.**
- Recharge runs at **12.5% per second** after **4 s**. The delay is `400 / (100 + r)` seconds (datamined 750%/min; PoB `EnergyShieldRechargeDelay = 4`).

### 6.3 Mana before life (Mind over Matter)

- PoE1: 40% of damage is taken from mana before life (3.19.0; was 30%). It applies to DoT since 3.0.0 ([Mind Over Matter](https://www.poewiki.net/wiki/Mind_Over_Matter)).
- PoE2: same step in the pipeline. Bleeding "cannot be redirected to another resource, such as to Mana with Mind Over Matter" ([poe2wiki Bleeding](https://www.poe2wiki.net/wiki/Bleeding)).

### 6.4 Regeneration, leech, recoup, flasks (brief)

- **Regeneration:** a constant per-second recovery.
  - Base mana regeneration is 1.75% per second in PoE1 (datamined `mana_regeneration_rate_per_minute_% = 105`) and 4% per second in PoE2 ([poe2wiki Regeneration](https://www.poe2wiki.net/wiki/Regeneration)).
  - Life regeneration comes only from sources.
  - Over-time recovery happens each server frame (33 ms) **before** DoT is taken ([Damage over time](https://www.poewiki.net/wiki/Damage_over_time#Mechanics)).
- **Leech, PoE1** ([Leech, Mechanics](https://www.poewiki.net/wiki/Leech#Mechanics)):
  - Each hit creates an instance worth `damage × leech%`.
  - Each instance is capped at **10%** of maximum (3.6.0) and recovers **2% of maximum per second**.
  - All instances together are capped at **20% per second** for life and mana and 10% for energy shield (datamined `maximum_life_leech_rate_%_per_minute = 1200`).
  - Example: 5,000 life gives at most 500 per instance, 100/s per instance, and 1,000/s in total.
- **Leech, PoE2:** since 0.5.0 only the single highest-rate instance per resource recovers at a time, and hits above 40,000 damage count as 40,000 ([poe2wiki Leech](https://www.poe2wiki.net/wiki/Leech)).
- **Recoup:** a share of hit damage taken **after mitigation**, recovered over **4 s** in PoE1 or **8 s** in PoE2. Instances are independent and continue at full resource ([Recoup](https://www.poewiki.net/wiki/Recoup); [poe2wiki Recoup](https://www.poe2wiki.net/wiki/Recoup)).
- **Flasks, PoE1:** Ruby, Sapphire and Topaz flasks raise maximum resistance during their effect ([Resistance, Maximum resistances](https://www.poewiki.net/wiki/Resistance#Maximum_resistances)). Flask suffixes grant immunity to bleeding, poison, curses, ignite, chill, freeze or shock while active, and remove the ailment on use ([Immunity, Items](https://www.poewiki.net/wiki/Immunity#Items)). PoE2 flasks and charms were not researched in depth.

---

## 7. Shifts, damage-taken modifiers, charges, Fortify

### 7.1 "Damage taken as" (defensive shift)

Sources: [Damage taken as](https://www.poewiki.net/wiki/Damage_taken_as#Mechanics); [poe2wiki Receiving damage](https://www.poe2wiki.net/wiki/Receiving_damage).
- Happens **before any mitigation**, and all shifts apply **simultaneously**, so damage can be shifted only once.
- The shifted part is mitigated **only by its final type**: physical taken as fire ignores armour.
- More than 100% total shift of one type makes you take *more* base damage.
- Shifting does not count as preventing damage.
- Most shift modifiers are hits-only. Rarer ones also shift DoT, in which case a bleed's DoT is split too.
- Shifted cold or lightning can chill or shock you.
- Example 1 (PoE1): a 1,000 physical hit against 5,000 armour deals 500. With 20% taken as fire and 75% fire resistance: physical 800 gives DR `5000 / (5000 + 4000) = 55.6%`, so 355.6, plus fire `200 × 0.25 = 50`, for **405.6**. The shift also shrinks the physical hit, which raises armour's percentage.
- Example 2 (from the wiki): a 1,000 cold hit with 25% cold penetration against 75/75 resistance deals 500. With 50% cold taken as fire it deals `500 × 0.25 + 500 × 0.50 = 375`. Shifting away from a penetrated element dodges the penetration.

### 7.2 "Increased/reduced" versus "more/less" damage taken

- After mitigation: flat `±X damage taken` first, then the **sum** of all increased and reduced damage taken, then **each** more/less multiplier in turn ([Receiving damage, Modifying damage taken](https://www.poewiki.net/wiki/Receiving_damage#Order_of_operations), citing [Mark_GGG 2014](https://www.pathofexile.com/forum/view-thread/924683#p7847614); [Damage taken](https://www.poewiki.net/wiki/Damage_taken)). PoE2 is identical.
- "Damage reduction" (armour, additional PDR, endurance) is a different, earlier step from "reduced damage taken". The wiki warns not to confuse them ([Damage reduction](https://www.poewiki.net/wiki/Damage_reduction)).
- Shock is an "increased damage taken" debuff:
  - PoE1: up to 50% by default, from the formula in section 8.3. Shocks applied without damage default to 15%.
  - PoE2: 20% by default, additive with other damage-taken modifiers ([poe2wiki Shock](https://www.poe2wiki.net/wiki/Shock)).
- PoE2 Withered: 5% increased chaos damage taken per stack, up to 10 stacks ([poe2wiki Withered](https://www.poe2wiki.net/wiki/Withered)).
- Example: 400 damage after mitigation, with shock (+20% increased) and "10% reduced damage taken", sums to +10% for 440. Then 20 Fortification (20% less) gives **352**.

### 7.3 Endurance charges

- **PoE1:** 3 by default. Each gives **+4% additional physical damage reduction** (additive with armour, inside the 90% cap) and **+4% additional elemental damage reduction** (since 3.25.0; was +4% elemental resistance). Sources are in section 1.
  - Example: a 1,000 fire hit against 75% resistance and 3 charges deals `250 × 0.88 = 220`.
  - Under the pre-3.25 rule, a defender already at the cap gained nothing: the +12% went to uncapped resistance, where it only acted as overcap.
- **Monsters:** 15% PDR and +15% elemental resistance per charge.
- **PoE2:** no inherent effect.

### 7.4 Fortify (PoE1; absent from PoE2)

Source: [Fortification](https://www.poewiki.net/wiki/Fortification); [3.16.0 notes](https://www.pathofexile.com/forum/view-thread/3186390).
- A stacking buff: **1% less damage taken from hits per stack**, **20 stacks maximum** by default, **6 s** duration (3.19.0; 5 s in 3.16).
- Melee hits generate stacks based on the damage dealt relative to the *enemy's* ailment threshold, with a bonus against rare and unique monsters.
- It is a more/less multiplier, calculated "after armour, resistances, and endurance charges". It does nothing against DoT, for example bleeding ([Bleeding, Defensive modifiers](https://www.poewiki.net/wiki/Bleeding#Defensive_modifiers)).
- From 2.0.0 to 3.15, Fortify was a single buff; 3.9.0 made its reduction multiplicative.
- At the default 20 stacks: 1,000 after mitigation becomes 800.

---

## 8. Ailments and stun on the defender

### 8.1 The defence vocabulary for an ailment (PoE1)

Source: [Ailment, Ailment defences](https://www.poewiki.net/wiki/Ailment#Ailment_defences); [Immunity](https://www.poewiki.net/wiki/Immunity#Mechanics); [Avoidance, Mechanic avoidance](https://www.poewiki.net/wiki/Avoidance#Mechanic_avoidance).

| Wording | Meaning |
|---|---|
| **Immune to X** | Cannot be inflicted, **and existing X is removed** |
| **Cannot be X'd** | Cannot be inflicted; existing X persists |
| **Unaffected by X** | X *is* applied and counts for "while X'd" conditions, but its debuff or damage has no effect |
| **X% chance to avoid X** | Rolled multiplicatively against the attacker's chance to inflict. **100% avoid equals "cannot be X'd"**, beats "always inflict", and does not remove existing stacks. 100% avoid of a non-damaging ailment also blocks the matching ground effect. |
| **X% reduced effect of X on you** | Scales magnitude, or for hit-based ailments shifts the damage thresholds. 100% means unaffected. Enemy ailment effect is multiplicative since 3.28.0f, and the wiki tags this with `{{confirm}}`. |
| **X% reduced duration of X on you** | 100% normally zeroes the duration, but monster duration bonuses add to it and ground effects ignore it |
| **Cannot inflict X** (debuff on the enemy) | That enemy cannot apply X |

Chaos Inoculation (chaos immunity) stops chaos damage from poisoning you, but physical damage still can. The resulting poison deals chaos damage, so a CI character is "effectively unaffected by poison" ([Chaos Inoculation](https://www.poewiki.net/wiki/Chaos_Inoculation#Being_Poisoned); [Poison](https://www.poewiki.net/wiki/Poison#Removal,_immunity,_and_unaffected_by)).

Example of avoidance: a defender with 60% chance to avoid being poisoned, bitten by an attacker with 100% chance to poison, is poisoned 40% of the time (`1.00 × (1 − 0.60)`). At 50% inflict chance it would be 20%. At 100% avoid it is never poisoned, even against "always poisons".

An ailment lands only if all of these hold: the hit contains a type that can inflict it; the inflict chance succeeds; the magnitude or duration reaches the minimum; the target fails to avoid it; and the target is not immune ([Ailment, Application by damaging hits](https://www.poewiki.net/wiki/Ailment#Application_by_damaging_hits)).

### 8.2 Ailment threshold (non-damaging ailments)

**PoE1** ([Ailment, Non-damaging ailments and ailment threshold](https://www.poewiki.net/wiki/Ailment#Non-damaging_ailments_and_ailment_threshold); [Chill](https://www.poewiki.net/wiki/Chill#Mechanics); [Shock, Effect calculation](https://www.poewiki.net/wiki/Shock#Effect_calculation); [Freeze, Freeze duration](https://www.poewiki.net/wiki/Freeze#Freeze_duration); [3.9.0 notes](https://www.pathofexile.com/forum/view-thread/2687400)):

```
Chill or Shock effect E = 1/2 × (D / T)^0.4 × (1 + increased effect)
    D = damage of the matching type *taken*; T = ailment threshold
    discarded below 5%; capped at 30% (chill) or 50% (shock)
Freeze duration = 0.06 s per 1% of T dealt; discarded below 0.3 s (5% of T); maximum 3 s (50% of T)
```

- For monsters, T equals maximum life in most cases, lower for high-life bosses. The ailment threshold was introduced in 3.9.0 and rebalanced in 3.15.0.
- For **players**, the threshold is maximum life. This is **inferred**, not stated on the Ailment page: the CI page says stun and elemental-ailment checks use "your maximum life **before** CI takes effect", and PoB labels the stun base "Life before Chaos Inoculation".
- Ailments use damage **taken**, so resistances shrink them.
- Example: threshold 5,000.
  - 500 cold gives `0.5 × 0.1^0.4 = 19.9%` chill.
  - 1,395 cold reaches the 30% chill cap.
  - 500 lightning gives a 19.9% shock.
  - 2,500 lightning gives a 37.9% shock.
  - Freeze from 500 cold lasts 0.6 s.

**PoE2** ([poe2wiki Ailment](https://www.poe2wiki.net/wiki/Ailment#Ailment_and_Poise_Thresholds); [poe2wiki Shock](https://www.poe2wiki.net/wiki/Shock); [poe2wiki Freeze](https://www.poe2wiki.net/wiki/Freeze)):
- The player's threshold is **half of maximum life** (0.2.0; was all of it). Datamined `PlayerAilmentThresholdLifeFactor = 0.5`.
- Monster thresholds come from a level table and are independent of monster life. Immobilising effects (freeze, electrocute, pin, stun) use a separate "poise" value.
- Shock chance is 25% per 100% of the threshold dealt; Viperesque confirmed this on Discord, as cited by the wiki. Crits no longer guarantee ailments.
- A hit too weak to freeze adds to a freeze-buildup meter instead.
- Example: 5,000 life gives a 2,500 threshold. A 500 lightning hit is 20% of it, so the shock chance is 5%.
- Poison and bleed chances are flat inflict chances and do not use the threshold, in both games.

### 8.3 Stun

**PoE1** ([Stun](https://www.poewiki.net/wiki/Stun#Stun_chance)):

```
StunChance% = 200 × StunningDamage / EffectiveMaxLife
    ignored if 20% or less (the hit must exceed 10% of life); certain at 50% of life or more
StunningDamage = 1.25 × melee physical + melee non-physical + non-melee physical + 0.75 × non-melee non-physical
StunDuration ms = 350 × (100 + attacker's increased stun duration) / (100 + defender's increased stun recovery)
```

- Second source: PoB constants `MinStunChanceNeeded = 20`, `StunBaseMult = 200`, `StunNotMeleeDamageMult = 0.75`, `StunBaseDuration = 0.35`.
- The stun threshold is effective maximum life (pre-CI life for CI characters). It can be based on energy shield instead (Valyrium). "Reduced stun threshold" reduces it, with diminishing returns past 75%.
- DoT, reflected damage and self-damage cannot stun.
- Avoidance sources multiply: `% avoid stun`, `% ignore stuns while casting`, and energy shield's separate 50%.
- The wiki notes that 3.21's nerf to non-melee stun is "not reflected in the above formula".
- Example (5,000 life):
  - A non-melee physical 1,000 hit: 40%.
  - A melee physical 1,000 hit: 50%.
  - A non-melee fire 1,000 hit: 30%.
  - A non-melee fire 400 hit: 12%, which is at or below 20%, so ignored.
  - The 40% case with energy shield and 30% avoid: `40 × 0.5 × 0.7 = 14%`.

**PoE2** ([poe2wiki Stun](https://www.poe2wiki.net/wiki/Stun)):
- Light stun chance scales with hit damage against the stun threshold, reaching 100% at 100% of it. Chances below 15% are discarded (0.3.0).
- The player's base stun threshold is **maximum life**, and players get +50% light-stun threshold for each light stun in the past 4 s.
- Multipliers:
  - Monster melee: 33% more.
  - Monster physical: 100% more, multiplicative with melee, so ×2.66 for a melee physical hit.
  - Hits the player deals: physical and melee 50% more each (datamined character constants). These matter when a player stuns monsters, not when a player is stunned.
- Heavy stun is a buildup meter. Cold builds freeze instead. Players cannot be heavy-stunned except while actively blocking, mounted or sprinting.
- Example: 5,000 life hit by a monster's 1,000 melee physical hit gives `20% × 2.66 ≈ 53%` light stun. A 500 ranged fire hit is 10%, below 15%, so discarded. The linear shape is inferred from the wiki's "scales up to 100%" wording.

---

## 9. Damage over time mitigation

### 9.1 Rules common to both games

- DoT does not hit. **Resistances apply; armour, penetration, evasion and block do not.** Additional damage reduction (non-armour PDR, endurance charges) and damage-taken modifiers do apply. Changes to the defender's mitigation (shock, curses, Immortal Call) update existing DoTs live.
  - Sources: [Damage over time](https://www.poewiki.net/wiki/Damage_over_time#Damage_calculation); [Ailment, Damage with ailments](https://www.poewiki.net/wiki/Ailment#Damage_with_ailments): "Resistances and non-armour sources of damage reduction apply to damage of the corresponding types dealt by ailments"; [poe2wiki Damage over time](https://www.poe2wiki.net/wiki/Damage_over_time).
- Damaging ailments snapshot their base when applied, from the hit's **pre-mitigation** flat damage. The defender's mitigation applies afterwards, to the DoT.
- An ailment's damage is mitigated by the ailment's **own** damage type. A poison made from physical damage is mitigated by chaos resistance only.
- Total DoT damage is capped at about 35.8 million per second (PoB `DotDpsCap`).

### 9.2 Poison

| | PoE1 ([Poison](https://www.poewiki.net/wiki/Poison#Damage)) | PoE2 ([poe2wiki Poison](https://www.poe2wiki.net/wiki/Poison), [Ailment](https://www.poe2wiki.net/wiki/Ailment#List_of_ailments)) |
|---|---|---|
| Base | **30% per second** of the hit's flat physical + chaos damage (PoB `PoisonPercentBase = 0.30`) | **20% per second** of physical + chaos (datamined `PoisonHitDamagePercentPerMinute = 1200`) |
| Duration | 2 s, so 60% total per stack | 2 s, so 40% total |
| Stacking | unlimited independent stacks | stack limit 1 by default; the highest instances deal damage |
| Damage type | chaos DoT: chaos resistance applies, **bypasses energy shield** (all chaos does in PoE1) | chaos DoT: chaos resistance applies, **bypasses energy shield** explicitly |
| Infliction | an explicit chance to poison; crits do not grant it | an explicit chance to poison |

### 9.3 Bleeding

| | PoE1 ([Bleeding](https://www.poewiki.net/wiki/Bleeding#Damage)) | PoE2 ([poe2wiki Bleeding](https://www.poe2wiki.net/wiki/Bleeding)) |
|---|---|---|
| Base | **70% per second** of the attack's base physical damage (PoB `BleedPercentBase = 70`). Physical **attacks** only. | **15% per second** of the hit's physical damage (datamined 900%/min). Any hit. |
| Duration | 5 s | 5 s, so 75% total |
| Moving or aggravated | ×3 (210% per second); the status text says "higher while you are moving" | monsters take +100%; **players no longer take extra while moving** (0.5.0) |
| Stacking | only the strongest applies (Crimson Dance changes this) | only the strongest applies |
| Mitigated by | **not armour, not Fortify**; additional PDR such as endurance charges does apply | not armour; additional PDR does apply; **bypasses energy shield**; cannot go to mana through Mind over Matter |

Ignite, briefly: PoE1 deals 90% per second for 4 s (PoB `IgnitePercentBase = 0.9`). PoE2 deals 20% per second for 4 s, 80% total (datamined 1200%/min). Fire resistance applies to it.

### 9.4 Worked example: a snake bite

A bite deals 100 physical and 100 chaos with 100% chance to poison. The defender has 1,000 armour, 30% chaos resistance and some energy shield.

**PoE1:**
- Hit, physical part: `1000 / (1000 + 500)` gives 66.7% DR, so 33.3.
- Hit, chaos part: `100 × 0.7` = 70, which bypasses energy shield straight to life.
- Hit total: 103.3.
- Poison: base `0.30 × 200 = 60/s` for 2 s, 120 total before mitigation. After chaos resistance, **42/s, 84 total**, also bypassing energy shield. Armour does nothing to the poison.

**PoE2:**
- Hit: physical `1000 / (1000 + 1000)` gives 50% DR, so 50, plus chaos 70. In PoE2 the 70 chaos removes 140 energy shield.
- Poison: `0.20 × 200 = 40/s` for 2 s, **56 total** after chaos resistance, bypassing energy shield.

**Defences that end the poison part:**
- "Cannot be poisoned" or 100% avoid stops the poison.
- "Immune to poison" also clears existing stacks.
- Chaos Inoculation still lets the physical part poison, but the poison then deals nothing.

---

## 10. Effective hit points and maximum hit

- poewiki has **no EHP page**: "Effective hit points" redirects to Receiving damage. The working definitions come from PoB-PoE1 `CalcDefence.lua`.
- **Maximum hit taken** is "calculated in reverse, from health pools, via damage reductions, to the max hit". Without armour it is `pool / conversion / resistance multiplier / taken multiplier` (line 3261). With armour, PoB iterates because armour's DR depends on the hit size. The result is deterministic: it ignores evasion, block and other chance-based layers.
- **Total EHP** = `(mitigated hits to die) / (1 − chance not to be hit) × hit size` (lines 2906 and 2954). It folds the chance layers (evasion, block, suppression, deflection) into an average.
- Single-type sketch: `MaxHit = pool / ((1 − R) × (1 − DR) × taken_multiplier)`. For 5,000 life against fire:
  - 75% resistance: 20,000.
  - 75% resistance plus 3 endurance charges (12% elemental DR): 22,727.
  - 80% maximum: 25,000.
  - 90% maximum: 50,000.
  - Adding 50% evasion doubles average EHP against attacks but leaves the maximum hit unchanged.

---

## 11. PoE1 versus PoE2 at a glance

| Topic | PoE1 | PoE2 |
|---|---|---|
| Max res default / hard cap / floor | 75 / 90 / −200 | 75 / 90 / −200 (PoB) |
| Campaign penalty | −30 after Act 5, −60 after Act 10, **includes chaos** | −10 per act to −30, then −40/−50 by area level, −60 at area level 65+, **elemental only** |
| Penetration floor | none, effective resistance can go negative | cannot penetrate below 0% |
| Armour formula | `A / (A + 5D)`, cap 90 (monsters 75) | `A / (A + 10D)`, cap 90 (monsters 75) |
| Mitigation order | resistances, then damage reduction | damage reduction, then resistances |
| Evasion | `1.25·Acc / (Acc + (E/5)^0.9)`, attacks only, entropy, 95% cap | vs players `1 − 0.95E / (E + 4Acc)`, all non-red-flash hits, entropy, 95% cap |
| Block | attack and spell separate, 75 default, 90 hard cap, rolls last | single block, 50 default (Turtle Charm 75), does not stop stun or freeze |
| Spell layer | suppression: 40% prevented, chance cap 100 | none; deflection instead (40% less, chance cap 95) |
| Energy shield | chaos **bypasses**; 33%/s after 2 s | chaos removes **2×**; bleed and poison bypass; 12.5%/s after 4 s |
| Endurance charge | +4% PDR and +4% elemental DR each | no inherent effect |
| Fortify | 1% less per stack, 20 maximum | absent |
| Player ailment threshold | maximum life (inferred) | half of maximum life |
| Stun | `200 × dmg / life`, ignored at 20% or less | light-stun chance proportional to dmg / life, discarded below 15%; heavy-stun meter |
| Poison | 30%/s × 2 s, unlimited stacks | 20%/s × 2 s, limit 1 |
| Bleed | 70%/s × 5 s, ×3 moving | 15%/s × 5 s, no moving bonus on players |
| Recoup | 4 s | 8 s |
| Leech | 2%/s per instance, 10% per instance, 20%/s total | single instance; hits above 40,000 count as 40,000 |

---

## 12. Disagreements, unverified claims and traps (all in one place)

1. **PoE2 evasion formula.** The poe2wiki Evasion page gives `1.25·Acc / (Acc + 0.3·E)` with no side qualifier. GGG's 0.3.1 notes and PoB both use `1 − 0.95E / (E + 4Acc)` for hits against players. The wiki is stale for defenders; its formula is the one against monsters.
2. **PoE2 poison base.** The poe2wiki Ailment table says "20% … per second", but the Poison page says "20% … over 2 seconds", which is ambiguous. The datamined `PoisonHitDamagePercentPerMinute = 1200` settles it at 20% per second, 40% total. Bleed (900%/min × 5 s = 75%) and ignite (1200%/min × 4 s = 80%) agree with their wiki pages.
3. **PoE2 order of operations.** The poe2wiki Receiving damage page is flagged "may need to be re-tested". Armour before resistances rests on a stream clip, a Reddit comment and the PoB commit, not on patch notes.
4. **PoE2 penetration floor of 0%** is stated on two poe2wiki pages, but the Penetration page carries `{{confirm}}` on its ordering sentence. PoE1 has no floor.
5. **PoE2 block hard cap.** PoB carries `BlockChanceCap = 90`; the poe2wiki Block page states only the 50% default and Turtle Charm's 75%. Unconfirmed.
6. **PoE1 evasion formula** is reverse-engineered by the community (u/esqtin, 2021), not published by GGG. PoB uses the same formula.
7. **PoE1 player ailment threshold of maximum life** is inferred from the CI page and PoB, not stated on the Ailment page.
8. **PoE1 bleed "reduced to 10% if an enemy monster applied it"** appears on the Bleeding page without a source. Not asserted here.
9. **PoE1 stun:** the wiki says 3.21's non-melee stun nerf is not reflected in its formula, so non-melee stun chance is about 30% lower in practice.
10. **PoE1 overcap prose.** The Resistance page's sentence on computing overcap has the subtraction backwards; use the in-game alt text, "amount by which Uncapped Resistance is higher than Maximum Resistance".
11. **PoB-PoE2 stun constants** (`MinStunChanceNeeded = 20`, `StunBaseMult = 200`) look inherited from PoE1 and disagree with PoE2's 0.3.0 rule (below 15% discarded). Trust the patch note.
12. **PoE2 ES stun avoidance.** PoB-PoE2 still applies PoE1's "energy shield gives a 50% chance to avoid stun"; no PoE2 wiki page states it. Unverified for PoE2.
13. **Name collision.** PoE1 already has a **"Tolerance"** notable passive. Since 3.16.0 it grants 30% chance to avoid poison and 30% chance to avoid bleeding; 3.21.0 replaced its stun avoidance with +19% chaos resistance ([Tolerance](https://www.poewiki.net/wiki/Tolerance)). Borrowing the word is fine, but PoE players will read "Tolerance" as avoidance plus chaos resistance.

---

## 13. Translation notes (PoE terms into the founder's words; not recommendations)

The founder's example: survival chance starts at 20%; each survived bite raises snake-poison tolerance, giving 25%, then eventually 100%; levelling past that to "125%" makes the extra 25% absorb abilities that reduce tolerance. PoE offers **two different readings**, and they diverge exactly at and above 100%.

### Reading A: tolerance is a resistance

- "Survival chance" maps to `1 − damage_taken` against a lethal dose. Resistance is **deterministic**: it scales damage and rolls no dice. A probability would have to come from elsewhere, such as dose variance or a check against a threshold.
- Each level adds to **uncapped** resistance. **"100% tolerance" is a maximum of 100**, which PoE never allows: the default maximum is 75 and the hard cap 90. Full immunity in PoE is a separate binary stat.
- **"125%" is overcap.** It absorbs flat reductions (curse- or exposure-style, "−X% to tolerance") one for one. It does **not** absorb penetration-style effects ("this venom treats tolerance as X lower"), which act on the capped value. Nor does it absorb effects that lower the **maximum**; only maximum above the hard cap buffers those.
- **Every ability that "reduces tolerance" must therefore be classed** as flat (buffered), percentage-reduced (partly buffered, since it multiplies uncapped) or penetration (never buffered). This is the main design fork PoE exposes.
- A **species weakness** maps naturally to a **lowered maximum** (for example a race capped at 50 against a venom) or a **negative starting value**, since PoE resistance can go to −200%. Penetration-style venoms hit low-maximum species hardest relative to their cap.
- The act penalty is a world-imposed flat −X that grows with progression. It is the PoE precedent for "the world gets harsher; your overcap is what keeps you capped".

### Reading B: tolerance is an avoidance

- PoE literally has "**X% chance to avoid being poisoned**". It is multiplicative with the attacker's chance to inflict. **100% equals "cannot be poisoned"**: absolute, it beats "always inflicts", and it leaves existing poison in place, whereas "Immune" also cures.
- This reading *is* probabilistic. But **PoE has no overcap for avoidance** and no attacker stat that lowers your avoidance, so the founder's "125% that protects against reduction" has **no PoE analogue** here.

### Other PoE structures that map onto the brief

- **Immunity** (Chaos Inoculation; "Immune to X") is binary. It ignores both reduction and penetration, so "immunity plus a buffer" is not a PoE pattern. In PoE, "impervious" means a separate binary stat, not 100% of a scale.
- **Deflection** (PoE2) is the closest hybrid of chance and damage: a *chance* to take 40% less. It sits between "survival chance" and "damage reduction".
- **Endurance-charge-style additional reduction** is a second multiplicative layer after resistance, with its own cap. It is a precedent for tolerance stacking with another system, such as a healer's treatment or a camp's antivenom, without stacking additively.
- **Armour's hit-size curve** has a direct venom analogue: the same tolerance shrugs off small doses and fails against large ones. The prevented amount saturates at `A/5`, so a big snake overwhelms the same tolerance that stops a small one.
- **Ailment threshold relative to maximum life** means effects scale with dose against body size: small creatures are chilled and shocked harder by the same hit. PoE2 halves the player's threshold relative to life.
- **DoT routing:** poison bypasses energy shield in both games and armour never touches it. PoE's precedent is that some threats skip some defensive layers entirely, so which layer a "tolerance" sits in decides what it stops.
- **Entropy evasion** is deterministic smoothing: exactly one hit per N attempts, with a randomly seeded counter. It prevents streaks of luck or misfortune while keeping a random phase, which bears on Return's Dice stream and determinism test if any tolerance check is a repeated roll.
- **Caps as levers:** in PoE the premium stat is raising the **maximum** (75 to 90), because damage taken is proportional to `1 − R`, so each point near the cap is worth more than the last. 75 to 80 is 20% less damage; 75 to 90 is 60% less.
