# Stories

Notable emergent stories from soaks and playtests, newest at the bottom. Each entry: date, seed and source (soak, playtest), the story as a player would retell it, and the systems and cause chain the analyzer found.

---

_No entries yet. The first come from the Milestone 0 soak._

## 2026-11-09 — Milestone 0 soak (100 worlds × 1 year, build 6081a5b)

### Vale, refused by a starving camp (seed 85, soak; policy "promised meals come first")
On day 1 you had decided that promised meals come first. On the third day of winter, when the camp shared out what little food it had, Oren went without so others could eat; by the next morning he was starving. When the vampire Vale came to him thirsting, Oren, hungry himself, refused. Vale went from person to person through the night: Nia was hungry, Wren said no, Mira was too weak, Hollis, you and Sera all refused. Before dawn Vale asked Oren again, then Nia, Wren and Mira again, and was turned away by all of them. Vale left the camp.
- Chain: ration policy (World) → Oren went without → hungry → starving (Needs) → eight refusals over two nights → Vale left (Social). 19 steps after folding.
- Why it matters: the player's day-1 policy is the first link of a story that ends 35 days later with someone else leaving, and nobody in it is a villain. (Whether the policy changed who ate is not in the chain: Sera's promised meal had already been served that afternoon.)

### The stolen portion (seed 34, soak; policy "promised meals come first")
Frost came on day 31 and took the berries from the bushes. By day 36 Nia was hungry, then starving. In the small hours of day 37 the camp shared out what little food there was and Nia was left out; thirteen minutes later she took the meal set aside for Bram. Bram now holds a grudge against Nia.
- Chain: winter frost (World) → hungry → starving → went without (Needs) → took the meal → grudge (Social).
- Why it matters: the demo's signature theft story, now arising on its own from winter, rationing and the Desperate trait.

### Hollis's four hours (seed 2, soak; policy "you eat first")
Hollis wandered in from the wilds on day 35, hungry and alone, and agreed to stay after you promised a meal within two days. Four hours later, hungry, Hollis was left out when the camp shared out its food. You broke your promise. Hollis took it as a betrayal and left that same hour.
- Chain: arrival (Incident) → promise on joining (Social) → went without (Needs) → promise broken → grudge → left (Social).
- Why it matters: the demo's broken-promise story, and a newcomer's lack of loyalty, in six lines.

### A thief in the dark, and who tended Mira (seed 34, soak)
Before dawn on day 27 a thief crept toward the camp. Mira spotted them and fought; she drove the thief off but was hurt, and Hollis tended her wounds. Mira now owes Hollis.
- Chain: night thief (Incident) → spotted → fought (Combat) → tended (Work) → owes care (Social).
- Why it matters: four systems in five lines, ending in a debt that can be repaid (or not) later.

## 2026-11-09 — Milestone 0 exit check: strangers retell the logs

Exit criterion: "a stranger reading the story log retells one story unprompted." Fresh readers with no project context each got one raw `world-N.txt` (seeds 1–4, not cherry-picked) and were asked only what they would tell a friend about it.

**Round 1 (build 6081a5b): 4 of 4 retold a causal story unprompted.**
- Seed 1: "Wren got me most: arrived the day the frost hit, was never promised anything, had the meal set aside for them taken by Hollis, and left holding a grudge."
- Seed 2: Hollis, promised a meal at 13:18 and gone by 17:00 the same day, which the reader called "a bug, or a rule the log never explains", and noticed that Hollis "went without so others could eat" under a "you eat first" policy.
- Seed 3: "A rule meant to be kind ends up deciding who starves."
- Seed 4: "Mira starves and walks out on the last day, and the game's story section traces this straight back to the player's day-one rule that promised meals come first. That one really lands."

**Round 2 (build 78e9427, after the ADR-0018 cause fixes): 4 of 4 again.**
- Seed 1: Vale turned down by everyone hungry until Nia, who arrived that morning, says yes; Sera starves and leaves four hours before the newcomer Hollis is fed.
- Seed 2: "That eat-first rule next to who actually went hungry makes a sharp little story": Hollis and Mira each take the meal set aside for you, and you end the year holding two grudges.
- Seed 3: Mira "went without so others could eat," and 54 minutes later "took the meal set aside for Sera."
- Seed 4: "Ilex, the vampire who had fought off thieves for the camp twice, asked all eight humans for blood one after another. Every one of them said no, and Ilex left at three in the morning."

What the readers found mechanical is listed under ADR-0018's known seams.
