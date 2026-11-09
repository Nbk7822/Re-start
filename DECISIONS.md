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
- **Update (2026-11-09, founder):** "Also add in the animations and the graphics… Like the demo but dark and grungy and gorey and more realistic. Maybe use Inkscape to generate these." The art pass moves into the current build, ahead of the M0 plan (fixed decision 11's "prototype art is coloured rectangles and text" is superseded by the founder for this build). Additions to the direction: darker and grungier (dirt, noise, stains, worn colours, vignette), gory but stylised (blood where people are hurt, pools under the downed and the dead, corpses that stay, stained clothes, flies, smoke from sunburn), more realistic (proportions, shading, soft shadows, light falloff). How: people and animals are drawn procedurally in the view so they animate freely; textures and decals are authored as SVG in tools/art/src and rendered to PNG with Inkscape (a development tool only: the PNGs are committed, so building the game does not need it); night and firelight use Godot's 2D lighting. Appearance the sim does not model (hair, skin, build) lives in a view asset, game/art/looks.json. Everything stays view-only: the sim and its content formats are unchanged.

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
  - From the second round of readers (post-fix logs): thefts and spoilage drain the stores but never appear in the winter-hunger stories, because a shortage has no causal memory of what emptied it; when "promised meals come first" leaves someone out, the story cites the policy but not the newcomer's promise that outranked them (Nia's meal and Mira's departure read as unrelated); a refusal can appear without the request before it when only the refusal lies on a story's path.

## ADR-0019 — The art pass: how the dark, grounded look is built (view only)
- **Status:** accepted (2026-11-09), lead's call implementing the founder's ADR-0009 update; for founder review.
- **Context:** The founder asked for the demo's people in option B, "dark and grungy and gorey and more realistic", with Inkscape if possible. The sim and its content formats must not change; the view must stay at 60 fps.
- **Decision:**
  1. **People and animals are procedural** (game/scripts/Art/Figures.cs, ported from the mockup's figureB, wolfB and kingB) so walking, turning and poses animate freely. Pose, facing and held tool come from sim state in Godot-free code (Logic/Poses.cs): walking while moving; a crouch to forage, tend or feed; a swing to fell, mend or fight; sitting to eat, give blood or idle by the hearth; lying when asleep (along the bed, if in one), downed or dead. People turn toward whoever they talk to, from their recent chat, deep-talk, insult or refusal thoughts, and those sitting by the hearth face the fire. Seen from the side a blow raises the tool high in front and strikes down onto what is in front, the blade leading (a test keeps the hand in front of the body). The walk cycle advances with the distance actually drawn, so feet do not slide.
  2. **Textures and decals are SVG sources rendered with Inkscape** (tools/art/src, tools/art/build: ground grunge, grain, dirt, frost, water, planks, masonry, soft blobs, stains, four blood splatters, drops, a pool, scorch, smoke, a soft shadow, the firelight falloff, a vignette). The PNGs in game/art are committed and read from disk at runtime (Image.LoadFromFile on the globalised res:// path), so building or running needs neither Inkscape nor a Godot import step; a missing file is drawn around.
  3. **Appearance the sim does not model** (hair, skin, build, hair style, beard, a cloth override) lives in game/art/looks.json, a view asset keyed by cast key and seeded from the demo's CLOTH and hair tables. Cloth is cast.json's colour unless overridden, darkened and muted at runtime. Vampires are the species that drink blood (collar points, the diamond mark).
  4. **Gore is derived from the sim and never touches it** (Logic/GoreLog.cs). It reads new World.Log events (downings, collapses, work accidents, deaths, bites, sunburn) and, because a fight is logged once while each blow lands silently, it also treats a health drop of 40 or more while fighting as a blow. Only wounds bleed: a collapse or death from hunger or the sun leaves no blood (the sun leaves scorch and a charred, smouldering body). Rotation and choice of splatter come from a hash of the event id (or of the person and tick for a blow), never from the sim's RNG; a test proves a watched world hashes the same as an unwatched one. Marks fade over three in-game days (three times faster in rain); a pool under a body dries but stays. Bodies stay where they died; the badly wounded wear blood-stained clothes and drip as they walk; flies gather by day.
  5. **Every tick the view runs is witnessed by the map** (Logic/ViewStepper.cs, the same loop as Sim.Advance): the warm-up (RETURN_TICKS, tools/play's start day) and the live loop, which runs up to 400 ticks in a long frame. Blows are health drops between looks and marks land where people stand when looked at, so a log that looked once a frame would leave blood that depends on frame timing; a test steps in frames of 1 to 400 ticks and gets the same marks as looking every tick. Stepping through the helper is the only change to SimHost.
  6. **Light:** a CanvasModulate tints the map by time of day (Logic/Daylight.cs, from world.json's dawn and dusk hours, eased; cooler in winter). The night tint (0.34, 0.37, 0.52) is lighter than the mockup's multiply because the grounded palette under it is darker: a night at the camp measures a mean luma of about 25 and a 90th percentile of about 35 (the mockup's night 22.5 and 42). Rain greys the day, but its grey and the wet-ground darkening fade out as night falls, so a rainy dusk is never darker than a dry midnight (a test pins it). At night people catch the moonlight: their cloth, hair and skin are lifted toward a cool light in proportion to the dark (thieves half as much), because a tint that multiplies everything alike cannot make a dark cloak read against the ground. A flickering PointLight2D at the hearth (0.3 + 1.6 × night, 9.5 tiles) matters in proportion to the dark, and the lantern on the watch pole lights while someone keeps watch at night. A vampire burning in daylight glows orange behind the body, with small flames off the shoulders and head. Names, mood marks, rain and a flat vignette are drawn on an unshaded overlay layer above the lit map and below the menu bar.
  7. **Speed:** the Brush triangulates every shape on the CPU and sends one triangle array per batch, with hand-made one-pixel feathered outlines (the Compatibility renderer ignores 2D MSAA); shadows draw in their own pass; the static ground is baked once into a mipmapped texture through an offscreen viewport. Before batching the camp ran at 29 fps; after, about 127 fps with vsync off at night with ten people at ×6 on the founder's M2 (RETURN_FPS=n measures it). After the review fixes the same night scene measures about 106 fps against 112 for the commit before them on the same machine and day (about 0.4 ms more map drawing).
  8. New check hooks for screenshots: RETURN_FOCUS=x,y and RETURN_ZOOM=z (CameraRig), RETURN_FPS=n (FrameProbe).
  9. **Placement, view only:** people sharing a tile stand side by side (0.3 tile apart, in id order, eased so a walker passing through causes a small shuffle); the dead and the downed stay on their blood, a carried patient goes with the bearer. Someone sitting by the hearth sits back from its stones. Someone lying on the ground (not in a bed) lies across the tile's centre, clear of a wall top in front. Everything attached to a person (name, mood mark, hit box, glints, smoke, flies) follows the drawn feet. Names keep off every other figure, stack counts and mood marks: under the feet, else beside them, else one line lower, else wherever overlaps least; the mood mark sits at the upper right of the head and the sleep mark at the upper left.
  10. **Water:** the river is dark teal with a faint, slow tonal drift under crisp ripple marks, drawn in its own layer with mirrored texture repeat (the Inkscape water texture does not tile seamlessly, even with stitched turbulence); the shallows lie in the water and pale toward the bank.
- **Consequences:** Fixed decision 11's "coloured rectangles and text" is superseded for this build by the founder (ADR-0009 update). The wolf and the Barrow King are drawn but unused until the sim has them. Not yet done: a paper-doll cache for figures (each figure is re-triangulated every frame, about 0.3 ms each in a Debug build), the 32 px pawn-sheet check that formally accepts ADR-0009, and fifteen-person frame times (the sim has ten people and the odd thief). Known seams: only people on the same sim tile are spread apart, so two people passing mid-step overlap for a moment, and in a crowded doorway the least-overlap fallback can still put a name on a neighbour's legs; fire, grass and smoke keep moving while the game is paused (the builder's call, so a paused camp still looks alive); tools/art/src/water.svg could be made to tile at source, which would let the river use the texture at full strength.

## ADR-0020 — Fixed decision 12: character growth and arcs (founder addendum)
- **Status:** accepted (2026-11-09), the founder's decision.
- **Context:** The founder supplied a design addendum, "Character Growth and Arcs (fixed decision 12)": companions must visibly change over a lifetime, and static pawns are a failure state.
- **Decision:** Added to CLAUDE.md as fixed decision 12; the full addendum (mechanics, data model, scope, acceptance) is recorded verbatim in BIBLE.md section 5. M0 builds only the data fields and logging (want, arc stage, trait origin), with no director logic; M1 adds trait mutation tables, arc advancement and naming-triggered evolution for two species.
- **Consequences:** Every trait change must reference an incident id and every mutation or evolution roll must use a Dice stream with a purpose tag. The acceptance soak runs 20-year worlds, so aging and multi-year soaks must exist before M1 can be accepted. The story detector will count arc completions as stories once arcs advance (M1).
