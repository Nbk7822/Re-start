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
- Silhouette language per species, taken from each species' nature in content (what it drinks, the needs of its nature), so a hybrid wears both looks: humans plain; vampires high collar points and a diamond mark; dryads a wreath of leaves that keeps the season, bark skin, bare feet and a hem of leaves, glowing faintly green by their tree at night; djinn ember-bronze with ember eyes, lit from within by a low warmth, thin tongues of flame-light wavering up behind the head and shoulders and never smoke (they are made of living flame); fae slight and pale with pointed ears, translucent wings and a glimmer at night; undead grave-grey, hollow-eyed and ragged, with a stiff gait. A nature's need shows on the body: a dryad's crown wilts away from the trees, a restless djinn burns brighter and an unbound one sparks, a fae's glimmer dims among broken words, the undead fade greyer as their purpose slips. Wounds give what a nature gives: blood, a dryad's sap, a djinn's ash, the undead's grave dust (ADR-0022, updates). Every species and hybrid must read at 32–48 px.

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
| Growth (wants, arcs, trait origins; section 5) | traits, species, roles, social (chemistry), incidents (arrivals) | in M0 only the story log, the inspector and the text-state API; from M1, traits (and through them needs, mood, social, work, combat) |
| Culture and faith (customs, gods, omens; section 6) | social, needs, incidents, work, map | mood, social (warmth, consent, grudges ended), work (feasts, guest-bread, repayments), rations (who goes without) |
| Build (plans, structures, beds; section 4.11) | work, map, items, needs, social | needs (rest, cover from sun and rain), mood (comfort, pride, thanks; envy with slice 1c), social (claims, passing over, roof promises kept and broken), travel (walls), incidents (work accidents), and through felling the dryads and the sacred trees |

Every system touches at least two others; that is the cut rule from CLAUDE.md. Growth is the one exception for now: by the founder's M0 scope for fixed decision 12 (section 5.3) it reads three systems (traits, social chemistry, incidents) but writes back only beats, and M1's trait changes close the loop.

### 4.3 Lineage: Return grows out of the first-camp demo

The archived browser demo (`archive/return-demo/`) is the design seed. Its cast, traits, needs, debts, promises, consent rules, ration policy and incidents carry into M0 unchanged in spirit. What changes is the feel: real time on a tile grid with pathfinding and autonomous work, instead of twelve turn-based shifts with an intention button.

### 4.4 Species

- **Human:** needs food, rest, social.
- **Vampire:** needs blood instead of food, and cannot work in the open in daylight; daytime is spent under cover. Blood comes only from a consenting human donor (the donor loses health and gains fatigue; the vampire owes the donor a debt). A non-consenting human cannot be fed on: refusal is a valid answer and is remembered. A vampire whose blood need goes unmet weakens and can die. This is the "distinct need" for M0.

The first four peoples beyond human and vampire (fixed decision 5, ADR-0022) are full characters who join the camp like anyone else; each adds a need of their nature (content/needs.json), shown as a bar like the others:

- **Undead** (the revenants and draugr of European folklore): no food, no sleep; **purpose**. Each is bound to the work they came back for (keep watch or forage, a Truth). Doing it restores them; without it they fade and at last lie down at rest. Setting that work to "never" takes their purpose away, and the story that follows is the player's. Wounded, they mend on their feet; they never lie down to sleep. The living are uneasy around them at first: a chat costs a little regard in the first days, nothing after the first week, and warms as usual once twelve days are out.
- **Dryad** (the tree nymphs of Greek myth): no food; **forest**. Living trees feed them and each day they go and stand by the one tree they are bound to (a Truth). They never fell a tree. Every felling grieves them; felling their own wounds them, cuts what feeds them and is never forgiven, unless the feller thought well enough of them to leave it standing. A lone tree feeds them only a little faster than they fade, so once the camp has thinned the forest to lone trees, a dryad whose own tree is gone withers and lives on beside one, getting up even from sleep to go to it when the withering starts to harm her. Cut off from every tree, she withers away.
- **Djinn** (fire spirits of Arabian folklore, as the Nights tell them, with free will and their own ethics): no food; **a bargain**. Unbound, a djinn grows restless and offers one: a service for a price each dawn until the season turns (both Truths). Whoever it asks decides by their own nature; the player never strikes it. The camp pays the price; the djinn never gathers its own. Paid to the end, it is kept; a dawn with nothing to pay breaks it and the djinn holds the patron to account. Wronged and unbound, it lets the fire loose; unbound long enough, it leaves.
- **Fae** (the Good Folk of Celtic and wider European folklore): eat berries; **courtesy**. They cannot lie: where others insult, they say the hard truth aloud, and those near hear it. Any broken promise or bargain in camp stings them like cold iron (it never fells them), and a broken promise is held against the oath-breaker; kept words and talk restore them. What they owe they have sworn to repay, first, even from their own share. Slights are remembered long.

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

Four more come in their own season (ADR-0022), each on a Dice day within a window set in cast.json: Thessil (dryad, Protective, forager, Days 3–7), Cindral (djinn, Independent, builder, Days 7–12), Varrow (undead, Oathbound, watcher, Days 11–17) and Sennet (fae, Proud, carer, Days 14–20). Fourteen people in all.

### 4.9 Player input in M0

Only intentions, never orders: a ration policy for short food (hungriest first, promises first, or yourself first) and per-person work priorities. Both are tick-stamped commands, so they are part of the determinism test.

### 4.9a Food and winter

Fourteen berry bushes (three clusters of four, and a cluster of two for the fae's extra mouth, ADR-0022) regrow two berries a day for three seasons. Winter opens with frost that strips the bushes; through winter each bush puts out one berry every two days, so the camp lives on half rations and the ration policy decides who eats. Food reaching the stockpile is shared out at once by that policy; anyone left out "goes without", and the Desperate, left out while critically hungry, go for someone else's share. With nothing in the stores, nobody goes without "so others could eat"; they are simply hungry (ADR-0015).

### 4.10 Map

A small grid (48 × 32) generated from Truths: a river, rocks, trees, berry bushes, and the demo's ruined camp: a shelter whose roof has three broken sections (repairs cost timber), covered ruins where vampires shelter by day, a hearth, and a stockpile.

### 4.11 Building (slice 1a of the build design, ADR-0027)

- **Plans are intentions.** The player marks out walls, doors, roof sections (up to 4×4, within 3 tiles of a wall) and beds on the grid; one gesture is one plan. People choose, through their priorities, who carries the timber (under haul) and who builds (a work of its own, after mending the roof); the player never says who. A wall is never finished on someone's tile, and a wall that would shut people in is refused until the map changes.
- **Timber is scarce and shared.** Every plan draws on one stock from 24 trees that do not grow back yet, and felling now follows what plans lack, so a plan can cost a dryad her tree or a faith its sacred tree. Whoever commits the last of the timber someone was counting on (a bedless sleeper's bed, a promised roof) passes them over, and they think less of the taker; a broken roof promise cites it.
- **Beds belong to someone.** Lying down in an unclaimed bed claims it, if you have none; nobody sleeps in someone else's bed. A bed restores rest faster; waking in your own bed is a comfort and waking with none is felt; waking under a roof someone mended, or in a bed someone built, warms you to them. Someone without a bed builds a bed before mending the roof, unless they are Generous. Whoever leaves or dies leaves their bed unclaimed. Vampires take a bed only under cover, and by day only if the way there is under cover.
- **Taking down** returns half the timber; a bed's owner and a thing's builder blame the player who marked it.
- **Finishing a building is never a story's ending by itself.** Building stories end where the systems it touches end them: grudges, departures, debts, broken promises.
- The headless stand-in player builds with `--plan beds|camp|mixed`: three beds in the shelter, then a 5×4 hut with a door, a roof and two beds, each on the first morning the camp has outgrown its beds.

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

### 5.5 What M0 built (ADR-0021)

- **Trait origins:** every trait records where it came from, parallel to the trait list: generated (born with it) or mutated {cause event, tick}. `Growth.ChangeTrait` is the only way to change a trait; it refuses a change without the event that caused it and logs a beat ("Mira, once Anxious, became Protective.").
- **Wants:** content/wants.json holds fourteen camp wants (to protect someone, to keep their word, never to go hungry again, to find a way home to Earth, to walk in daylight again, ...), each with a target kind (person, place, item or none) and weights by trait, species, role and otherworlder. A person's first want is their nature, drawn from Truths by cast key, so it survives rewinds; who a want is about is whoever is in the camp when it forms (best or worst chemistry). Someone who finds the camp empty keeps their natural want unless it is about another person; then they want something that needs no one, drawn again from Truths. It forms when they enter the world (camp starters at creation, newcomers on arrival) and is logged as a beat: "Mira wants to protect you." / "You want to find a way home to Earth."
- **Arcs:** every want starts an arc at "want". `Growth.AdvanceArc` moves it one stage at a time (obstacle, turning point, resolution) or ends it (failed, abandoned), refuses a move without a cause, and logs a beat that cites the cause and the arc's previous beat.
- **Not yet:** no system mutates traits or advances arcs, and beats are not stories by themselves (they carry no persistent effect). StateApi exposes all of it; the inspector shows each trait's origin and a Wants line.

## 6. Culture and faith

Slice 1 of the culture-and-faith design (ADR-0023). Culture has no screen of its own: it changes what people do at moments the player already watches, and every custom that acts emits a Culture event that sits between its cause and its consequence, so stories pass through it.

### 6.1 What a world holds

- **Four cultures** (the Hearthfolk, the Reed Folk, the Ashen, the Nightborn; each world picks one of three names for each), **three faiths** (the Sworn Word, the Old Roots, the Ember; again one of three names) and **four gods** (two for the Sworn Word, one each for the others). Content: `content/cultures.json`, `faiths.json`, `customs.json`, `domains.json`, text in `content/loc/en.culture.json`.
- All of it is seeded once per playthrough from Truths (fixed decision 4): names, each culture's main faith and customs (always-held ones plus weighted picks, at most one of each kind), each god's name, domain (oaths, hearth, vein, grove), epithet (two gods of one faith never share one while their domain has another) and temperament (wrath and grace, shown only as stern, kindly, fickle or distant), festival days, and the sacred tree (one of the six trees nearest the hearth). The same seed gives the same peoples and gods in every timeline, so a player who dies can learn them.
- **Everyone but you belongs.** Each person's culture, faith and devotion are Truths keyed by cast key. Cultures cross the lines between peoples (a vampire can be raised among the Hearthfolk); a people no culture names gets one through the `"*"` weights. Devotion is never shown as a number: lapsed (under 30), faithful, devout (70 and over). Culture customs bind every member; faith customs bind the faithful and the devout.
- **You are from another world**: no culture, no faith, no devotion. You breach greetings three times as often as anyone else until you learn them, you read no omens, and every culture judges you by its own rules.
- The ethics axes of fixed decision 5 (Mortal/Immortal, Arcane/Mundane, Wild/Civil, Oathbound/Free) are on cultures and faiths, and a person's value is their culture's plus their faith's scaled by devotion. Slice 1 reads two: Oathbound/Free (how heavily someone weighs a promise, 50% to 150%) and Arcane/Mundane (how readily they see signs). The other two are carried for factions and the Ideas Tree and do nothing yet.

### 6.2 Customs (seven kinds, twelve customs)

| Kind | Customs | What it does |
|---|---|---|
| Greeting | the clasped arm, no touch at meeting, the bow of greeting, the lowered eyes | At a first meeting, someone who neither shares nor has learned the listener's greeting may breach it: a slight (the listener thinks less of them), the talk ends, the custom is learned. Kin of one culture greet each other warmly instead. |
| Blood stance | the blood taboo, the blood-gift | A taboo makes a donor refuse; the refusal names the custom when it was decisive, and stings at 40%. A blood-gift makes a donor consent more readily and offer unasked to a vampire weak with thirst. |
| Guest-right | guest-bread | A keeper who is neither starving nor injured feeds a newcomer the camp promised a meal and cannot give one: their own share (and goes hungry for it), or berries they go and pick. The promise is kept and the guest owes the host. A guest-right keeper wronged in their first week holds a heavier grudge. |
| A leader eats last | the leader eats last | Under "you eat first", a keeper left out while you are fed takes offence; when you go without, keepers who ate see how a leader is known. |
| Debt of honour | debts of honour | A keeper who has owed someone for two days brings them food when they go hungry, settling the debt (most blood debts used to stay open); a vampire waits for dark. |
| Festival | the harvest fire (autumn), the Longest Night (winter) | On its day, at dusk, if a keeper is in camp: a feast from what nobody has a claim on, or a lean fire. Everyone gathers round the fire, one to a tile, and talks more warmly; afterwards each keeper who came sets down (or eases) their slightest grudge against another who came. |
| Sacred tree | the sacred tree | Its keepers never fell it. Anyone else may; if a keeper sees it, it is a desecration, and the faithful think bitterly of the feller. The tree wears cloth strips on the map; its stump keeps one. |

Beside the customs: chat warmth leans toward kin and away from strangers while either is new; each culture's stance toward other peoples (revere, wary, fear, scorn) shifts warmth, insult odds and blood consent; and among a people who hold a promise heavy, word of a broken one spreads to their kin.

### 6.3 Gods and omens

Gods are believed in, not proven: in slice 1 they act only through the people who read signs. A god's domain says which deeds it hates and loves (a broken or kept promise, a stolen or shared meal, a wronged or honoured guest, blood refused or given, a felled tree, the sacred tree felled) and which incidents it speaks through (rain, a windfall, spoiled food, an accident, a member's death). When a sign follows such a deed within two days, a follower devout enough for their faith may read it (a Dice roll scaled by devotion, the god's wrath or grace, the deed and the reader's Arcane axis): "To Oren, the rain was Ostrel's anger at you for a broken promise." The reading moves the reader's opinion of the doer, and an angry one past the grudge line holds a grudge. A reader then waits five days before reading again.

### 6.4 Where it shows

- The inspector's line under the name: "Of the Hearthfolk · follows the Sworn Word (devout)", or "From another world"; its tooltip lists their ways, their gods and the ways they have learned here. No numbers.
- The Chronicle and the story log carry every Culture event; the log's cast line says where each person belongs, and a "Peoples and faiths" section names each people's customs and stances and each faith's gods.
- The tile tooltip names the sacred tree ("Tree (sacred to the Old Roots)", or its stump) and a hearth during a festival.
- `StateApi.Character(...).Belonging`, `Cultures`, `Culture(id)`, `Faiths` and `Faith(id)` give the same in structured form; the soak counts culture stories, their share, the worlds that have them, how culture entered them, and departures and deaths by species.

### 6.5 Not yet

Death rites, holy days and fasting, kin following kin, devotion that drifts and conversion, gods who stir, shrines, food taboos, the four peoples' customs and stances (a content follow-up once they merge), and any player lever of culture's own (the culture-and-faith design, sections 5 and 9).
