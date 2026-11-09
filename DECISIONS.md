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
- **Status:** accepted (2026-11-08)
- **Decision:**
  - Two RNG streams, `Truths` and `Dice`, each a PCG32 generator implemented in `Return.Sim` (never `System.Random`).
  - Every draw passes a purpose tag (for diffing divergent runs). Each stream counts its draws; both states are part of the snapshot.
  - `Truths` is locked after world generation; a later draw throws.
  - Simulation state uses integers (fixed-point where fractions are needed). No floating point in state or in decisions; no `Math.Sin/Exp` in sim paths.
  - No iteration over `Dictionary`/`HashSet` where order can affect state; use lists ordered by stable ids.
  - No `string.GetHashCode()` or other per-process randomised hashing in the sim.
  - Player input is a tick-stamped command queue; same seed + same commands = same world, checked by a test that compares state hashes, including a snapshot → restore → continue run against a straight run.
- **Consequences:** Floats are allowed in the Godot view only. Cross-platform float differences cannot affect the sim because it has none.

## ADR-0005 — Time model
- **Status:** accepted (2026-11-08)
- **Decision:** 300 ticks per in-game hour, 24 hours per day, 10 days per season, 4 seasons per year (288,000 ticks per year), set in `content/world.json`. Needs, mood and memory decay run hourly; movement and job progress per tick. The view runs 30 ticks per second at 1×.
- **Consequences:** A 100-world, 1-year soak is about 29 million ticks; worlds run in parallel on separate threads (each world is single-threaded and self-contained).

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
- **Status:** proposed (2026-11-08)
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
