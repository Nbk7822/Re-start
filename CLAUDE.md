You are lead engineer and designer on Return: a 2D, top-down, pausable simulation game. It is an isekai story generator in the lineage of RimWorld and Dwarf Fortress with the species depth of Stellaris. This file is the source of truth. Read BIBLE.md, DECISIONS.md and STORIES.md if present before any work.

## The game in one paragraph
The player is a person from Earth reincarnated into a procedurally generated fantasy world with nothing. They meet procedurally generated people, make allies, recruit, build, and over a lifetime may found a kingdom. Kingdom-building is how you survive; the goal is to die of old age. Every other death rewinds time to a random earlier "fate knot" in the same playthrough. The player keeps their memories; the world does not.

## Fixed design decisions (do not relitigate; propose changes in DECISIONS.md)
1. Story generator first. A feature exists only if it makes players retell unplanned stories.
2. Intimacy at every scale. The player manages 5–20 named companions who become the court; the kingdom is governed only by delegation through them. Never a spreadsheet view. Never direct unit control.
3. Return by Death. Death rewinds to a weighted-random fate knot (recent-biased, fat tail into the deep past). Old age is the only non-rewinding death and the win condition.
4. Truths vs Dice. World facts seeded once per playthrough (map, hidden truths, characters' natures, rival otherworlders' arrivals, gods) survive rewinds; events re-roll after every rewind.
5. Species from many mythologies differ in NEEDS, not stats (vampires: blood; dryads: forest; djinn: contracts; fae: cannot lie; undead: purpose). Ethics axes: Mortal/Immortal, Arcane/Mundane, Wild/Civil, Oathbound/Free, on individuals and factions. Intermarriage produces procedural hybrids via trait inheritance.
6. Ideas Tree. Earth knowledge is introduced through champion characters, and the world reacts per species and faith.
7. Rival otherworlders arrive over time (a Truth).
8. Storyteller/director: incident library, pressure pacing, one hidden personality per playthrough, arc detection, fate-knot marking.
9. Rewinds generate story: a Distortion meter spawns anomalies; a Chronicle of Lost Futures records abandoned timelines as legends.
10. Characters: RimWorld-grade needs, mood and traits, plus a relationship graph where grudges, debts, oaths and ambitions are first-class objects. Dialogue is templated in v1; any generative text must derive from stored state.
11. Art: very basic and functional in the RimWorld spirit, with our own silhouette language. Prototype art is colored rectangles and text.
12. Character growth and arcs: companions visibly change over a lifetime (body, role, traits, heart); static pawns are a failure state. Growth is event-driven, never grind-driven, and every change is a logged story beat with a cause: an arc per character (want, obstacle, turning point, resolution), trait mutation through experience, rare evolutions, niches, shared memories, underdog payoffs and decline. Truths keep a character's nature; Dice re-roll their experiences. Full addendum, data model, scope and acceptance: BIBLE.md section 5.

## Tech constraints (non-negotiable)
- Godot 4.x with C#/.NET.
- The simulation is `Return.Sim`, a pure .NET class library with zero Godot references. The Godot project `Return.Game` is a view and input layer only. If a sim feature needs Godot, the design is wrong.
- Deterministic: fixed tick; two RNG streams, `Truths` and `Dice`; every random draw names its stream; same seed and inputs produce an identical world. A test enforces this.
- Snapshot and restore of full world state (including character memories) at fate knots; restore under 1 s at 200 characters.
- Data-driven: species, traits, needs, incidents, ideas and items live in `/content/*.json` with schemas. No content in code.
- Text-state API: `Return.Sim` exposes a structured read of any character, faction, location or incident, and a headless runner can drive it. Mod hooks and localization keys for every player-facing string from day one.
- Headless soak: `tools/soak` runs N worlds for M years and writes story logs; CI runs it nightly.

## Repo layout
/sim        Return.Sim (.NET class library)
/game       Godot project (view only)
/content    JSON defs and schemas
/tools      headless runner, soak, story-log analyzer
/tests      determinism, snapshot, system tests
BIBLE.md    design bible
DECISIONS.md  decision log (ADR style)
STORIES.md  notable emergent stories from soaks and playtests

## How we work
- Prototype before architecture. Build the smallest system set that produces stories: needs, mood, traits, relationships, work, incidents, travel, simple combat.
- A system must interact with at least two others or it is cut.
- Each session: state the plan, work in small commits, run tests and a headless soak, append notable stories to STORIES.md, log decisions to DECISIONS.md.
- Ask before: changing a fixed decision, adding a dependency, changing a content schema, or touching anything that affects determinism.
- Definition of a story: a chain of 3 or more causally linked events across 2 or more systems, involving a named character, ending in a persistent state change. Soak output must detect and count these.
- Not now, but architect for: kingdom layer, rewind UX, hybrids, ideas tree, rivals, generative dialogue. Exception: RNG streams and snapshotting are built now because they cannot be retrofitted.

## Milestone 0 (do this first)
1. Scaffold the repo and solution with CI running tests.
2. Return.Sim: world clock, two RNG streams, entity model, 10 characters, 2 species (human plus one with a distinct need), needs and mood, 8 traits, relationship graph (opinion, grudge, debt), 6 incidents, simple work and travel on a small grid map.
3. Snapshot/restore and the determinism test.
4. Headless runner: run one world for one year and print a readable story log; soak 100 worlds and count stories.
5. Godot view: render the sim as rectangles and text, pause and speed controls, click a character to open an inspector panel.
Exit criterion: a stranger reading the story log retells one story unprompted.

## Anti-goals
Not a colony builder with elves. Not a visual novel. Not 3D or isometric. Not a deterministic puzzle loop. No spreadsheet kingdom. No feature that does not produce stories.
