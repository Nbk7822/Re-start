# Return — Design Bible

CLAUDE.md is the source of truth for vision, fixed decisions, tech constraints and the milestone plan. This bible holds the design detail that grows underneath it. When the two disagree, CLAUDE.md wins and the disagreement goes into DECISIONS.md.

## 1. What a story is

A story is a chain of three or more causally linked events, spanning two or more systems, involving a named character, and ending in a persistent state change (a grudge formed, a debt incurred or repaid, a death, a departure, a recruitment). The soak counts these mechanically, so every event the sim emits must carry:

- the system that produced it (needs, mood, social, work, travel, incident, combat);
- the event or events that caused it;
- the characters involved;
- the persistent effects it applied, if any.

A feature that cannot put events into such chains does not ship.

## 2. How it plays (player experience)

- **The map is the screen.** Top-down, square tiles, the camp fills the window. No dashboards.
- **One menu bar** along the bottom: Build, Orders, Zones, Work, Schedule, People, Chronicle, with the clock and time controls (pause, 1×, 2×, 3×) at the right. Nothing else is permanently on screen.
- **Panels appear only when asked for.** Clicking a person opens their inspector (needs, mood, traits, relationships, grudges and debts, recent memories). Hovering a tile shows what is on it.
- **Square grid, readable.** Faint grid lines by default (options: corner ticks, or only while building). Everything sits on a tile: trees, rocks, water, walls, items, beds, people. People walk tile to tile with 8-way pathfinding.
- **Indirect control only.** The player sets intentions (designations, zones, work priorities, schedules); people decide what to do and can refuse. No drafting, no direct orders (fixed decision 2).
- **Moods are legible on the map** through manga-style emotes over heads (anger vein = insulted, sweat drop = overworked, hearts = smitten, sparkles = inspired, ! = danger, ? = unsure, zzz = asleep, gloom = grieving, music note = content). Each emote is driven by a sim state, never decoration.

## 3. Art direction

- **Milestone 0:** coloured rectangles and text (fixed decision 11). The layout rules in section 2 still apply.
- **Target look (proposed, see DECISIONS.md ADR-0009):** "Cut Ink, anime-flavoured". Warm paper ground, thin dark outlines with a slight hand-cut wobble, a small fixed ink palette, flat cel shadows. People are full chibi: big heads and eyes, one signature hairstyle and hair colour each, short limbs that swing when walking, four facings (front, back, left, right) chosen by movement or by who they are talking to. Mockups: `.superpowers/brainstorm/*/content/art-direction-v2.html`, `walk-and-turn.html`, `grid.html` (local, not committed).
- Silhouette language per species: humans plain; vampires high collar points and pointed ears; dryads leaf crowns. Every species and hybrid must read at 32–48 px.

## 4. Milestone 0 simulation

### 4.1 Time

Fixed tick. `content/world.json` sets the calendar (300 ticks per hour, 24 hours per day, 10 days per season, 4 seasons per year: 7,200 ticks per day, 288,000 per year). Movement and job progress run per tick; needs, mood and memory decay run hourly; social checks every few in-game minutes; the director rolls incidents hourly. The view runs 20 ticks per second at 1× (a day is 6 real minutes), 60 at ×3, 120 at ×6.

### 4.2 Systems and how they touch each other

| System | Reads from | Writes to |
|---|---|---|
| Needs (food or blood, rest, social, health) | species, traits, work, incidents | mood, work choice, social (theft, sharing, blood requests), combat (weakness) |
| Mood (thoughts with expiry) | needs, traits, social, incidents, combat | work (refusal, breaks), social (insults when low) |
| Traits (8, data-driven modifiers) | — | needs, mood, social, work, combat |
| Social / relationships (opinion, grudge, debt) | mood, traits, needs, combat, work | mood, work (help or refuse), combat (defend or abandon) |
| Work (forage, gather timber, haul, repair shelter, keep watch, care, rest, eat, donate blood) | needs, mood, traits, social, map | needs, items, social (debts from care and blood, promises kept or broken), incidents (accidents, thieves turned away) |
| Travel (8-way pathfinding on the tile grid) | map, work, combat | work timing, combat (reach, flee), incidents (arrivals at the edge) |
| Incidents (6, director-paced) | all state | needs, mood, combat, social, map |
| Combat (simple melee) | traits, social, travel | needs (health), mood, social (rescue debts, abandonment grudges) |

Every system touches at least two others; that is the cut rule from CLAUDE.md.

### 4.3 Lineage: Return grows out of the first-camp demo

The archived browser demo (`archive/return-demo/`) is the design seed. Its cast, traits, needs, debts, promises, consent rules, ration policy and incidents carry into M0 unchanged in spirit. What changes is the feel: real time on a tile grid with pathfinding and autonomous work, instead of twelve turn-based shifts with an intention button.

### 4.4 Species

- **Human:** needs food, rest, social.
- **Vampire:** needs blood instead of food, and cannot work in the open in daylight; daytime is spent under cover. Blood comes only from a consenting human donor (the donor loses health and gains fatigue; the vampire owes the donor a debt). A non-consenting human cannot be fed on: refusal is a valid answer and is remembered. A vampire whose blood need goes unmet weakens and can die. This is the "distinct need" for M0.

### 4.5 Traits (8, carried from the demo)

| Trait | Behaviour |
|---|---|
| Protective | Puts an injured companion before ordinary work. |
| Oathbound | Tries to finish an accepted promise, even when tired. |
| Proud | Refuses an unsafe bargain and values being asked. |
| Practical | Prefers a workable shelter and a reserve of food. |
| Generous | May volunteer blood or care when someone is in danger. |
| Independent | Needs a better reason to accept someone else's plan. |
| Desperate | May take a reserved meal when critically hungry. |
| Anxious | Avoids risky work when wounded or exhausted. |

Each character has a signature trait plus one secondary trait drawn from Practical, Generous, Anxious or Independent (Truths stream, never a duplicate). Effects live in `content/traits.json` as modifiers on needs, mood, social odds, work preference and combat choices.

### 4.6 Relationships

- **Opinion:** −100…100 per directed pair (the demo's "trust"), moved by remembered interactions.
- **Grudge:** a first-class record (holder, target, cause event, intensity, decay). Formed by theft of a reserved meal, a broken promise, a refused plea in a crisis, abandonment in a fight, or repeated insults. Grudges drive insults, refusals and departures.
- **Debt:** a first-class record (debtor, creditor, kind, cause event, weight). Formed by care, a shared meal, donated blood, a rescue. Debtors help, defend and repay their creditors; repayment closes the debt.
- **Promise:** a debt with a due date (from the demo). When a wanderer is invited to stay, the inviter promises a meal (humans) or protected rest (vampires) within two days. Kept: the debt closes and opinion rises. Broken: opinion drops sharply and a grudge forms; enough distrust and the person leaves.

### 4.7 Incidents (6)

Wanderer arrives; rain (soaks anyone without a sound roof); food spoils; work accident (injury while chopping, foraging or repairing); night thief (steals from the stockpile unless someone keeps watch, in which case they fight); windfall (late berries or river driftwood). The director keeps a pressure meter so incidents arrive in waves with quiet stretches between. Everything an incident rolls after it fires is Dice.

### 4.8 Characters

Ten people. Eight come from the demo: You (the otherworlder, human, Practical), Mira (human forager, Protective), Ilex (vampire, Proud), Oren (human builder, Oathbound), Bram (human carer, Generous), Nia (human forager, Desperate), Sera (human builder, Independent), Vale (vampire watcher, Anxious). Two are new humans to reach ten. Who they are is generated once from the Truths stream and survives a rewind; when the wanderers arrive is Dice. You, Mira and Oren start in the camp; the rest arrive as wanderers over the first season.

### 4.9 Player input in M0

Only intentions, never orders: a ration policy for short food (hungriest first, promises first, or yourself first) and per-person work priorities. Both are tick-stamped commands, so they are part of the determinism test.

### 4.10 Map

A small grid (48 × 32) generated from Truths: a river, rocks, trees, berry bushes, and the demo's ruined camp: a shelter whose roof has three broken sections (repairs cost timber), covered ruins where vampires shelter by day, a hearth, and a stockpile.
