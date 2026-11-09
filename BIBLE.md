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

- **Current build (ADR-0009, ADR-0019):** the demo's people in a grounded three-quarter view, dark and grungy. Cloaks in each person's muted cloth colour, round heads, hair and skin from `game/art/looks.json`, the demo's chest marks (your gold return arrow, a cross for builders and carers, a leaf for foragers, the vampire's diamond under a spiked collar). Thin warm-black outlines, light from the upper left, soft shadows. Walls with a top and a front face, roof sections (the broken one shows its rafters), trees with trunks, a river that flows, grass that stirs, a hearth whose fire lights the night. Grime everywhere: mottled ground, mud where people walk, ash round the fire, frost in winter, a vignette. Blood where people are hurt, pools under the downed and the dead, bodies that stay, flies, a vampire caught by the sun glowing and burning, then smoking. Nights are dark but playable: the fire is the warm centre, a lantern hangs over the watch, and people catch the moonlight so they read against the ground.
- Everything drawn is derived from sim state; the view never changes the world.
- Silhouette language per species: humans plain; vampires high collar points and a diamond mark; dryads leaf crowns (later). Every species and hybrid must read at 32–48 px.

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

The first four peoples beyond human and vampire (fixed decision 5, ADR-0022) are full characters who join the camp like anyone else; each adds a need of their nature (content/needs.json), shown as a bar like the others:

- **Undead** (the revenants and draugr of European folklore): no food, no sleep; **purpose**. Each is bound to the work they came back for (keep watch or forage, a Truth). Doing it restores them; without it they fade and at last lie down at rest. Setting that work to "never" takes their purpose away, and the story that follows is the player's. The living are uneasy around them for their first days and warm with time.
- **Dryad** (the tree nymphs of Greek myth): no food; **forest**. Living trees feed them and each day they go and stand by the one tree they are bound to (a Truth). They never fell a tree. Every felling grieves them; felling their own wounds them, cuts what feeds them and is never forgiven, unless the feller thought well enough of them to leave it standing. Cut off from all trees, they wither.
- **Djinn** (beings of smokeless fire in Arabian folklore and Islamic tradition, with free will and their own ethics): no food; **a bargain**. Unbound, a djinn grows restless and offers one: a service for a price each dawn until the season turns (both Truths). Whoever it asks decides by their own nature; the player never strikes it. Paid to the end, it is kept; a dawn with nothing to pay breaks it and the djinn holds the patron to account. Wronged and unbound, it lets the fire loose; unbound long enough, it leaves.
- **Fae** (the Good Folk of Celtic and wider European folklore): eat berries; **courtesy**. They cannot lie: where others insult, they say the hard truth aloud, and those near hear it. Any broken promise or bargain in camp stings them like cold iron, and a broken promise is held against the oath-breaker; kept words and talk restore them. What they owe they have sworn to repay, first, even from their own share. Slights are remembered long.

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

Each character has a signature trait plus one secondary trait drawn from the other seven (Truths keyed by cast key, never a duplicate). The demo drew from a fixed pool of four (Practical, Generous, Anxious, Independent); restoring that needs a schema field, see ADR-0012. Effects live in `content/traits.json` as modifiers on needs, mood, social odds, work preference and combat choices.

### 4.6 Relationships

- **Opinion:** −100…100 per directed pair (the demo's "trust"), moved by remembered interactions.
- **Grudge:** a first-class record (holder, target, cause event, intensity, decay). Formed by theft of a reserved meal, a broken promise, a refused plea in a crisis, abandonment in a fight, or repeated insults. Grudges drive insults, refusals and departures.
- **Debt:** a first-class record (debtor, creditor, kind, cause event, weight). Formed by care, a shared meal, donated blood, a rescue. Debtors help, defend and repay their creditors; repayment closes the debt.
- **Promise:** a debt with a due date (from the demo). When a wanderer is invited to stay and the camp cannot give them what they need right now, the inviter promises it within two days: a meal when nothing in the stockpile is free for them to eat, or (for vampires) a sound roof against the sun when no roof section is mended. A promised meal is a first claim on food: if food is shared out and the hungry newcomer is left out, the promise breaks then and there. A promised roof is kept when anyone mends a section in time. Kept: the debt closes and opinion rises. Broken: opinion drops sharply and a grudge forms; newcomers, with no loyalty yet, leave soonest (ADR-0014).
- **Chemistry:** every pair has a fixed chemistry from -2 to +2, part of their nature (a Truth, so it survives rewinds). Pairs who grate on each other trade insults; pairs who click warm to each other faster. Dislike feeds itself: the more someone dislikes you, the likelier they insult you (ADR-0016).

### 4.7 Incidents (6)

Wanderer arrives (most in the first week, the last three as autumn turns to winter, when the camp may not be able to feed them); rain (soaks anyone without a sound roof); food spoils; work accident (injury while chopping, foraging or repairing); night thief (steals from the stockpile unless someone keeps watch, in which case they fight); windfall (late berries or river driftwood). The director keeps a pressure meter so incidents arrive in waves with quiet stretches between. Everything an incident rolls after it fires is Dice.

### 4.8 Characters

Ten people. Eight come from the demo: You (the otherworlder, human, Practical), Mira (human forager, Protective), Ilex (vampire, Proud), Oren (human builder, Oathbound), Bram (human carer, Generous), Nia (human forager, Desperate), Sera (human builder, Independent), Vale (vampire watcher, Anxious). Two are new humans to reach ten. Who they are is generated once from the Truths stream and survives a rewind; when the wanderers arrive is Dice. You, Mira and Oren start in the camp; four more arrive in the first week and the last three around the turn of winter (who comes when is Dice).

Four more come in their own season (ADR-0022), each on a Dice day within a window set in cast.json: Dryope (dryad, Protective, forager, Days 3–7), Qamar (djinn, Independent, builder, Days 7–12), Ketil (undead, Oathbound, watcher, Days 11–17) and Fionn (fae, Proud, carer, Days 14–20). Fourteen people in all.

### 4.9 Player input in M0

Only intentions, never orders: a ration policy for short food (hungriest first, promises first, or yourself first) and per-person work priorities. Both are tick-stamped commands, so they are part of the determinism test.

### 4.9a Food and winter

Fourteen berry bushes (three clusters of four, and a cluster of two for the fae's extra mouth, ADR-0022) regrow two berries a day for three seasons. Winter opens with frost that strips the bushes; through winter each bush puts out one berry every two days, so the camp lives on half rations and the ration policy decides who eats. Food reaching the stockpile is shared out at once by that policy; anyone left out "goes without", and the Desperate, left out while critically hungry, go for someone else's share. With nothing in the stores, nobody goes without "so others could eat"; they are simply hungry (ADR-0015).

### 4.10 Map

A small grid (48 × 32) generated from Truths: a river, rocks, trees, berry bushes, and the demo's ruined camp: a shelter whose roof has three broken sections (repairs cost timber), covered ruins where vampires shelter by day, a hearth, and a stockpile.

## 5. Character growth and arcs (fixed decision 12)

The founder's addendum of 2026-11-09, recorded as given (ADR-0020).

Companions must visibly change over a lifetime: body, role, traits and heart. Static RimWorld-style pawns are a failure state. Growth is event-driven, never grind-driven, and every change is a logged story beat with a cause.

### 5.1 Mechanics

1. **Arc state per character:** want -> obstacle -> turning point -> resolution (or failed/abandoned). The director seeds incidents to advance stalled arcs. The want is generated at creation and may change after a resolution.
2. **Trait mutation through experience:** traits are not fixed. Experiences (near-death, betrayal, command, grief, mercy received or given) roll on trait-change tables. Every change logs cause and tick.
3. **Evolution:** a rare transformation triggered by naming, oaths, or player milestones (founding, coronation). Offers 2-3 paths; the character's want weights the choice. Changes subspecies, role, abilities and sprite parts.
4. **Niche:** recruit generation is biased toward unfilled court roles. The director tracks redundancy; a redundant character gets a niche, a departure arc, or a spotlight.
5. **Shared memory:** each character stores references to significant shared events (with the player and with each other) that feed templated thoughts and dialogue. Memories are wiped by rewind and survive death only in others' memories.
6. **Underdog payoff:** the director flags low-power, high-exposure characters and reserves a crisis where their want aligns with the moment.
7. **Decline:** aging, grief, trauma and resentment. Growth can be negative. Powerful lieutenants may develop ambitions that conflict with the player's.
8. **Rewind interaction:** Truths keep a character's nature; Dice re-roll their experiences, so the same person grows differently across timelines. The Chronicle records prior-timeline versions.

### 5.2 Data model

- Character: traits[] with origin (generated | mutated{cause, tick}); want{type, target, state}; arc{stage, stalledSinceTick}; role; evolutionTier; memories[] (eventId, salience, valence); relationships via the existing graph.
- Content (JSON, schema-validated): trait_mutations.json (trigger -> from -> to, probability), evolution_paths.json (species, tier, trigger, options), wants.json, arc_incidents.json.
- Story log: emit a beat for every trait change, evolution and arc stage transition. The story detector counts arc completions as stories.

### 5.3 Scope

- **M0:** data fields and logging only (want, arc stage, trait origin). No director logic.
- **M1:** trait mutation tables, arc stage advancement, naming-triggered evolution for 2 species.
- **Later:** underdog payoff, decline, rewind-variant chronicle, generative memory text.

### 5.4 Acceptance

- Soak 100 worlds x 20 years: at least 70% of companions alive 5+ years have at least one trait mutation and one arc transition; at least 30% complete an arc.
- The story log can answer "how did X become who they are" as a causal chain of incident ids.
- No growth from grind: every trait change references an incident id.
- All mutation and evolution rolls use the Dice stream with purpose tags.
