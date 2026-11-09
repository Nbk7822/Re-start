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

## ADR-0009 — Target art direction: Cut Ink, anime-flavoured (post-M0)
- **Status:** proposed (2026-11-08). Founder: stays proposed until an art-direction pass shows a pawn sheet readable at 32 px that satisfies fixed decision 11. Not an M0 item.
- **Context:** Fixed decision 11 says "very basic and functional in the RimWorld spirit, with our own silhouette language". In the brainstorm the founder rejected the first demo's look as AI-generated and chose a linocut "Cut Ink" style with anime, full-chibi people, thinner outlines, animated limbs and four facings.
- **Decision (proposed):** Adopt that look as the target for the vertical slice. M0 stays rectangles and text. The look is still simple shapes with outlines, so it can be read as a refinement of decision 11 rather than a replacement, but because chibi faces and limb animation go beyond "very basic", it needs the founder's explicit sign-off.
- **Consequences:** If accepted, the paper-doll pawn system (body, head, hair, limbs, facing) becomes a vertical-slice task, and species silhouettes are specified per section 3 of BIBLE.md.

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
