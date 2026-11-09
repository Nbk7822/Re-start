# Decision log

ADR style. Newest at the bottom. Status is one of: proposed, accepted, superseded.

---

## ADR-0001 — CLAUDE.md is the source of truth; repo layout as specified
- **Status:** accepted (2026-11-08)
- **Context:** The founder's brief defines vision, fixed decisions, tech constraints and Milestone 0.
- **Decision:** Save the brief as `CLAUDE.md`. Use its layout: `/sim`, `/game`, `/content`, `/tools`, `/tests`, plus `BIBLE.md`, `DECISIONS.md`, `STORIES.md`.
- **Consequences:** Changes to fixed decisions are proposed here first.

## ADR-0002 — Godot 4.7.2 with .NET 8; Return.Sim targets net8.0
- **Status:** accepted (2026-11-08)
- **Context:** Godot 4.x with C# is mandated. Current stable is 4.7.2; GodotSharp 4.7.2 targets net8.0; the machine has the .NET 8.0.408 SDK.
- **Decision:** `Return.Sim`, tests and tools target net8.0. `Return.Game` uses `Godot.NET.Sdk/4.7.2`.
- **Consequences:** Upgrading Godot means bumping the SDK version in one csproj.

## ADR-0003 — Dependencies approved for M0
- **Status:** accepted (2026-11-08, approved by the founder)
- **Decision:** xUnit 2.9.3 + xunit.runner.visualstudio + Microsoft.NET.Test.Sdk (tests only); JsonSchema.Net (tests only, validates `/content`); Godot.NET.Sdk 4.7.2 (game only). `Return.Sim` has no package references; JSON uses the built-in System.Text.Json.
- **Consequences:** Any further package needs a new ADR and the founder's approval.

## ADR-0004 — Determinism rules
- **Status:** accepted with changes (2026-11-08, founder sign-off)
- **Decision:**
  - **Truths is stateless and hash-derived:** `value = hash(worldSeed, truthId, entityId, index)`. There is no sequence and no locking. Any truth can be queried lazily at any time (a character created later, an arriving rival, a secret resolved in year 20), in any order, and it survives rewinds by construction. The hash is a SplitMix64-style 64-bit mix over the inputs, specified in `sim/Core/TruthRng.cs`.
  - **Dice is stateful:** PCG32 substreams, one per system, each seeded from `hash(diceSeed, systemId)`. A rewind reseeds `diceSeed` and therefore every substream. Generator states and draw counters are serialisable and part of every snapshot.
  - Draw tags are enums with fixed numeric values (`TruthId`, `DiceId`, `DiceSystem`), never strings. Truth ids feed the hash: never renumber or reuse one.
  - Simulation state uses integers. No floating point in state or decisions.
  - Never `System.Random`; never wall-clock time in the sim.
  - Never `string.GetHashCode()` (it is randomised per process). Stable string hashing is FNV-1a 64 (or xxHash).
  - Never iterate a `Dictionary` or `HashSet` where order affects an outcome; use lists ordered by stable ids.
  - Player input is a tick-stamped command queue. Pending commands and the log of applied commands are persisted in saves and snapshots. Same seed + same commands = same world; tests compare state hashes, including snapshot → restore → continue against a straight run.
- **Consequences:** Floats are allowed in the Godot view only. Adding a draw to one system cannot shift another system's sequence.

## ADR-0005 — Time model
- **Status:** accepted (2026-11-08, founder sign-off)
- **Decision:**
  - Calendar: 300 ticks per in-game hour, 24 hours per day, 10 days per season, 4 seasons per year (7,200 ticks per day, 288,000 per year), set in `content/world.json`. Needs, mood and memory decay run hourly; movement and job progress per tick.
  - Base tick rate: 20 ticks per real second at 1× (speeds ×1, ×3, ×6 = 20, 60, 120 ticks/s). A day lasts 6 real minutes at 1× (target 5–8).
  - CPU budget: on average at most 2 µs per character per tick, amortised over a day (hourly and job-start work included), target 1 µs. A benchmark test fails above budget. At budget, a 100-world, 1-year, 10-character soak costs at most 576 CPU-seconds (about 1.2 minutes on 8 cores).
- **Consequences:** Worlds run in parallel on separate threads (each world is single-threaded and self-contained).

## ADR-0006 — Event record and story detection
- **Status:** accepted (2026-11-08)
- **Decision:** Every event carries: id, tick, system tag, localisation key and arguments, actor ids, cause event ids, and a list of persistent effects (grudge formed/ended, debt formed/repaid, death, departure, recruitment, opinion crossing a threshold). The story analyzer walks cause links backwards from each event with persistent effects and counts a story when the chain has ≥3 events, ≥2 distinct systems and a named character. Each terminal event counts once.
- **Consequences:** Systems must record causes faithfully; a missing cause link silently shortens stories, so tests check causal chains for key interactions.

## ADR-0007 — Content, schemas and localisation
- **Status:** accepted (2026-11-08)
- **Decision:** Species, traits, needs, incidents, items, work types and the calendar live in `/content/*.json`, each with a JSON Schema in `/content/schemas`. Player-facing text is a key plus arguments, resolved from `/content/loc/en.json`; neither the sim nor the view composes English in code. Mods supply extra content folders merged by id, and can subscribe to sim events through `IModHook`.
- **Consequences:** Changing a schema needs the founder's approval (CLAUDE.md "ask before").

## ADR-0008 — UX: map-first, one bottom menu bar, square grid
- **Status:** accepted (2026-11-08, founder feedback during the art brainstorm)
- **Decision:** The map fills the screen. One menu bar along the bottom (Build, Orders, Zones, Work, Schedule, People, Chronicle, clock, pause and speeds). Panels only on demand (inspector on click, tooltip on hover). Square tiles with faint grid lines by default.
- **Consequences:** No persistent side panels or HUD bars without a new decision.

## ADR-0009 — Target art direction: the demo's people, darker and grounded (post-M0)
- **Status:** direction chosen by the founder (2026-11-09: option B of the art mockup, after seeing the pawn sheet at 32 px); formal acceptance against the founder's criterion (a pawn sheet readable at 32 px that satisfies fixed decision 11) to confirm. Not an M0 item. Replaces the 2026-11-08 "Cut Ink, anime-flavoured" proposal.
- **Context:** Fixed decision 11 says "very basic and functional in the RimWorld spirit, with our own silhouette language". The founder asked to take characters, species and monsters from the archived demo (top-down cloaks, round heads, per-character cloth and hair colours, role marks; vampires with pale faces, spiked collars and a diamond mark) and make the environment and everything, characters included, a bit darker and somewhat more realistic, keeping the style simple and clean, with simple animations for life.
- **Decision:** Grounded three-quarter view (mockup option B): figures about 1.3 tiles tall with head-to-body near 1:3.8, legs and arms, two-tone shading with light from the upper left, thin warm-black outlines, soft ground shadows; trees with trunks and layered canopies, walls with a top and a front face, depth sorted by y so things in front hide things behind. A darker, muted palette (olive grass, brown soil, slate river). Animation: walk cycles with swinging arms and legs, four facings (turn toward movement or a conversation partner), idle breathing, work and crouch poses, lying asleep; trees sway, grass stirs, water flows, fire flickers and lights the night. Monsters in the same language: thief, wolf, ghoul, and bosses at twice a person's size (the Barrow King). Reference: `docs/art/art-direction-2026-11-09.html` (opens in any browser; toggles for A/B and day, dusk, night).
- **Consequences:** The paper-doll pawn system (body, head, hair, limbs, facing, species parts, poses) and the three-quarter environment kit become post-M0 tasks. M0 stays rectangles and text.

## ADR-0010 — Archive the earlier browser demo
- **Status:** accepted (2026-11-08, founder's choice)
- **Decision:** Move the OpenAI-built `return-demo/` to `archive/return-demo/` and commit it for reference. No code depends on it; its design does (ADR-0011).

## ADR-0011 — M0 ports the demo's design into a real-time grid sim
- **Status:** accepted (2026-11-08, founder: "Return is based on the demo")
- **Context:** The founder was unhappy with how the demo played (turn-based shifts, an intention button, a dashboard), not with what it was about.
- **Decision:** M0 keeps the demo's eight cast members, its eight traits and their behaviours, hunger/fatigue/health, vampires' blood need and daylight limit, consent for blood donation, directed trust (now opinion), concrete debts, promises with due dates, the ration policy and its incident set. It replaces shifts with a fixed tick, abstract locations with a tile map and pathfinding, and the intention button with autonomous work driven by needs, traits and priorities. Two new humans bring the cast to ten. The six incidents become: wanderer arrives, rain, food spoils, work accident, night thief, windfall.
- **Consequences:** The demo's behaviour (e.g. Nia taking a reserved meal, a broken meal promise driving someone away, consensual feeding creating a debt) is a regression target: the soak should be able to produce those stories.

## ADR-0012 — Small M0 implementation calls
- **Status:** accepted for M0 (2026-11-08); item 2 flagged for founder review.
- **Decision:**
  1. Work priorities are keyed by content work id (`"forage"`), not by the `JobKind` enum, so they stay data-driven. `SetWorkPriority` takes a work id.
  2. A character's secondary trait is drawn from every trait except their signature, instead of the demo's fixed pool of four. Keeping the pool would need a new field in `traits.schema.json` (for example `"secondaryPool": true`), and schema changes need founder approval. If soaks show the wider pool hurts (say, too many Desperate secondaries), propose that field.
  3. `content/roles.json` (role → default work priorities) was added in Task 3 alongside the other schemas.

## ADR-0013 — M0 tuning constants live in code (for now)
- **Status:** accepted for M0 (2026-11-08); proposal for founder review. Values updated 2026-11-09 after soak probing.
- **Context:** Some numbers are tuning, not content. Putting them in `/content` needs new schema fields, which needs approval.
- **Decision:** Keep them as named constants (`NeedsSystem`, `SocialSystem`, `Places`, `BloodJobs`, `CombatSystem`) during M0 so soak tuning can move fast. Proposal: after the M0 soak settles the values, add a `tuning` block to `world.json` and its schema.
- **Current values:**
  - Conversation: start 25‰ per 30-tick check (400‰ when seeking company); insult 30‰ base, +250‰ in a low mood (< 25), +300‰ toward a grudge target; deep talk 300‰. (Started at 60‰ / 50‰; probes logged ~400 insults per 40 days, which drowned every other story.)
  - Needs: hungry < 300, starving < 150, fed ≥ 450, collapse < 150, recover ≥ 300. Starvation costs 10 health per hour, so starving lasts days, not hours, and leaves room for rescue, theft and departure.
  - Departure: opinion of You ≤ −45 with a grudge or hunger; or starving for 24 h with mood < 20; or two active grudges with mood < 20.
  - Food: 12 berry bushes (3 clusters of 4), at most 6 berries each, regrowing 2 per day, none in winter. Winter opens with an event that becomes the cause of the hunger that follows. (9 bushes starved every camp; 16 bushes at 2 per day meant nothing ever happened.)
  - Sun: "sun danger" runs from one hour before dawn until dusk. A vampire outside is pulled off any job to seek cover; a vampire on watch stops at sun danger.
  - Blood: after a refusal a vampire waits 2 h before asking anyone else. Donors refuse while hungry, weak (health below max(600, their risky-work threshold + 100)), in a low mood (< 35), or when their opinion of the vampire is below their consent bias. Each reason has its own log line and cites its cause.
  - Incidents: night thief 12‰ per night hour × pressure / 50, from day 3; food spoils 120‰ at 06:00 × pressure / 50 when the stockpile holds ≥ 4 berries; majors at least 6 h apart and cost 30 pressure.
  - Added 2026-11-09 (ADR-0014–0017): insults +4‰ per point of the speaker's dislike of the listener and +60‰ per point of bad chemistry; chats warm opinion by (100 + 50 × chemistry)%; newcomers (first 7 days after arriving) leave at an opinion of You 30 points higher (−15); wanderers arrive on days 1–6 (four) and 29–37 (three, shuffled with Dice); winter frost strips the bushes, then one berry per bush every 2 days.

## ADR-0014 — Promises only when fulfilment is uncertain
- **Status:** accepted for M0 (2026-11-09), lead's call; for founder review.
- **Context:** Every wanderer was promised a meal on joining and ate within the hour, so 700 of the first soak's 1,428 "stories" were the same instant promise-kept chain. A debt opened and closed in an hour is not a story, and it hid that real stories ran at ~7 per world.
- **Decision:** A newcomer is promised only what the camp cannot give right now: a meal when nothing in the stockpile is free for them to eat (amended 2026-11-09 after review: "less than a meal per mouth" still let promises close within minutes); for vampires, a sound roof against the sun when no roof section is mended. Otherwise they are simply welcomed (a Recruitment, not a story by itself). A promised meal is a first claim on food: if food is shared out and the creditor, hungry, is left out, the promise breaks at once (as in the demo, the ration policy decides whether you keep your word). A promised roof is kept when anyone mends a section in time, and repairs made while such a promise is open cite it. The newcomer's food cause becomes the promise, so the hunger that follows chains back to it.
- **Consequences:** "Broken promise → departure" appears in 38% of soak worlds. Kept promises stay open 1 to 40 hours (median about 15); none closes within the hour. Promised rest is no longer kept by sleeping in the ruins. The promise's length comes from world.json (`promiseDueHours`) and reaches the text as "within {n} days" / "within a day".

## ADR-0015 — Rationing that bites, and a winter squeeze instead of a cliff
- **Status:** accepted for M0 (2026-11-09), lead's call; for founder review. Pacing change: the last three wanderers now arrive near winter.
- **Context:** Reservations existed in 39 of 57,600 world-hours: food delivered between hourly checks went to whoever walked past first, so the ration policy (the player's main lever) almost never mattered, and meal theft (a demo regression story) happened in 1 of 100 worlds. Winter was a cliff: bushes kept their berries into winter, famine hit in the last three days, and "went without so others could eat" fired even when nobody ate.
- **Decision:** Food reaching the stockpile is shared out at once by the policy. With empty stores nobody "goes without". The Desperate, left out while critically hungry, go straight for a set-aside meal (the theft itself still happens at the stockpile, if the meal is still there). Winter frost strips the bushes, which then trickle one berry every two days. Four wanderers arrive in the first week; the last three arrive on days 29–37 in an order shuffled with Dice (new draw tag `DiceId.ArrivalOrder`).
- **Consequences:** Winter is a ten-day half-rations squeeze in which the policy decides who eats. Meal theft → grudge appears in 67% of soak worlds.

## ADR-0016 — Pair chemistry is a Truth; dislike feeds insults
- **Status:** implemented provisionally (2026-11-09); **needs founder approval** because it adds a Truth draw (`TruthId.PairChemistry`), which touches the determinism surface (additively: no existing draw changes).
- **Context:** Insults landed on whoever was nearest and chats nudged every opinion up, so opinions converged on mild liking and grudges formed in 6 of 100 worlds.
- **Decision:** Each pair has a chemistry from −2 to +2 (10/20/40/20/10%), a stateless Truth keyed by the pair's cast keys: symmetric, order-independent, rewind-proof, so a player who dies learns who clashes with whom. Bad chemistry adds 60‰ per point to insult odds; chemistry scales the warmth of a chat by (100 + 50 × chemistry)%. Insult odds also rise 4‰ per point of the speaker's dislike of the listener, so feuds feed themselves. Newcomers in their first week leave at an opinion 30 points higher.
- **Consequences:** Grudges form in 88% of worlds. Of 227 grudges in a 100-world soak, 115 came from meal theft, 49 from broken promises, 7 from abandonment in a fight, 2 from refusals and 54 from insults.

## ADR-0017 — Counting and telling stories honestly
- **Status:** accepted for M0 (2026-11-09).
- **Decision:**
  1. One arc is one story: a story whose ending lies inside another story's chain (a broken promise inside "...broke the promise, held a grudge, left") is not counted again.
  2. The headless runner has a stand-in player that sets one of the three ration policies on the first tick, chosen by seed, so a soak exercises every policy's stories. `run` and `soak` share it.
  3. The story log folds a run of identical steps that differ only in one person ("… and the same with Bram, Nia, Mira, until Day 40, 04:00"), and uses singular item names for one.
  4. The player speaks in second person: `World.PlayerId` names the player, and event text picks `<key>_you_<arg>` templates when the player fills that placeholder ("You were hurt", "Mira tended your wounds"). Rival otherworlders, later, are not the player.
- **Consequences:** The honest count is 8.5 stories per world-year after the ADR-0018 cause fixes (it was 9.1 before them, 10.3 with nested sub-stories), from 14 distinct system sets; every world has at least 4. This is below the M0 plan's target of 10, and stays reported as such.

## ADR-0018 — M0 exit check: cause honesty, hooks, and content the code relies on
- **Status:** accepted (2026-11-09).
- **Context:** The exit check gave four fresh readers one raw story log each (all four retold a specific causal story unprompted) and had three reviewers audit determinism (no findings: 12,880 snapshot-restore checks, serial and parallel soaks byte-identical), architecture and story counting.
- **Decision (fixes made):**
  1. An event cites only what brought it about. An insult cites a grudge, else (only when the speaker's mood is low enough to count) their worst thought, else their worst thought about the target, else nothing. "Went without" cites the ration policy only when hungriest-first would have fed that person. A sunburn cites earlier harm only if it is still going on (starving now, or burnt in the last hour). A departure cites food only if the leaver is hungry. Winter is the food cause only for people who actually get hungry.
  2. Someone who leaves or dies takes their grudges with them, silently: no "let go of their grudge" endings for people who are gone.
  3. Mod hooks see each event at the end of the tick it happened in, after all systems have run, so a hook may change the world (it used to crash a system mid-loop).
  4. `RequiredContent` lists the content ids and incident params the code names; `ContentDb` checks them at load, and a test keeps the list in step with the code. An incident with no behaviour is rejected at load.
  5. Food is berries-only until the items milestone: a species that eats another item is rejected at load with a pointer here, instead of starving in play.
  6. Calendar facts come from world.json (late arrivals on the eve of winter; humans sleep from three hours after dusk to dawn). `work_accident` is labelled a minor incident, which is how it has always been paced.
- **Known seams, not yet fixed (from the stranger readers and the architecture review):**
  - Hunger runs on fixed clocks (hungry to starving in exactly six hours; departures exactly a day after starving begins). Per-person appetite and temperament would vary them, but that is a new Truth draw: proposal for the founder.
  - The log's "What happened" shows only notable events, so a refusal "hungry themselves" can appear before any visible hunger line.
  - Blood debts are seldom repaid, so many stories end on a debt nobody returns to.
  - Two near-identical stories (two vampires' first feeding) are told separately, and a story shows one causal path, so a character's earlier deeds (Ilex fighting off thieves before being refused) are left out. Arc detection (fixed decision 8) is the place for this.
  - A spotted thief who reaches the berries before anyone reaches them still steals; it reads oddly when it happens three times in a year.
  - Most stories land in the last ten days (the winter squeeze); the director has no mid-year pressure besides thieves and spoilage.
  - Content fields with no effect yet: items `stackLimit`, incidents `kind` (pacing is in code), traits `sharesFood` (only speeds grudge decay; no food is shared), and work fields for ids whose code does not read them.
