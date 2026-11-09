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

## 2026-11-09 — The four peoples: dryads, djinn, fae, undead (ADR-0022; soak 100 worlds × 1 year, build 2fba765)

1,213 stories (12.13 per world; 8.47 before this track). Stories in which each people's own need appears: undead 76 in 23 worlds, dryad 50 in 27, djinn 197 in all 100, fae 100 in 73.

### The djinn's fire and the dryad's tree (seed 13, soak; policy "promised meals come first")
On Day 11 Qamar the djinn, restless without a bargain, offered to gather berries for the camp in return for timber for the fire each dawn. You and Mira turned it down; Wren struck the bargain, and Qamar served it out to the turn of the season. On Day 22 Mira struck the next one. The fire wanted wood, and on Day 28 Qamar itself felled the tree Dryope was bound to. Dryope cried out as it came down, and now holds a grudge against Qamar. She grew faint away from the trees, was withering by Day 30, and Wren, Mira, you and Bram each tended her in turn; on Day 39 Dryope withered away, far from the living trees.
- Chain: restless (Needs) → offered, turned down twice, struck (Social) → served out, struck again (Social) → felled the bound tree (Work, citing the bargain) → cried out → faint → withering → withered away (Needs, Death). Systems: Needs → Social → Work.
- Why it matters: two peoples' needs in one chain. The djinn's price is fire, the fire needs wood, and the wood was the dryad's life; nobody in it is a villain.

### Fionn's grudge, and Fionn's word (seed 15, soak; policy "the hungriest eat first")
Fionn, one of the Good Folk, came out of the hill mist on Day 19 and swore to repay you for the welcome. In winter Wren wandered in hungry and stayed after you promised a meal within two days; at 02:29 on Day 37 Wren went without, you broke your promise, the broken word stung Fionn like cold iron, and Fionn now holds a grudge against you. Fourteen minutes later, with you starving, Fionn brought you a meal and kept their word to you.
- Chains: arrival (Incident) → promised a meal (Social) → hungry → starving → went without (Needs) → promise broken → stung → grudge (Social); and winter (World) → hungry → starving (Needs) → brought a meal (Work) → word kept (Social).
- Why it matters: a fae cannot forgive the broken word and cannot break their own, in the same quarter hour.

### Ketil and the bare bushes (seed 47, soak)
Ketil walked in out of the barrows, cold and sleepless, bound by their nature to forage. Winter came on Day 31 and the frost took the berries from the bushes. With nothing left to pick, by Day 32 Ketil was losing the purpose that kept them here, by Day 35 fading, and on Day 39 Ketil lay down, their purpose gone, and was at rest at last.
- Chain: winter (World) → purpose slipping → fading → at rest (Needs, Death). The same chain, headed by "Your priority for Forage is now never", is the player-caused version (scenario test `TakingAnUndeadsWorkAwayFadesThemToRest`).
- Why it matters: the undead's need ties them to a work and a season, and the player can take it away.

### Qamar's winter (seed 2, soak; policy "you eat first")
From Day 8 Qamar kept the night watch for a share of the berries, in bargains Mira struck for the camp, and both kept their word at the turn of two seasons. On Day 36 the camp had no berries at dawn and the bargain broke. Restless, Qamar came to you; you would not strike an unsafe bargain. Burning with nothing to bind it, and wronged, Qamar let the fire loose: 2 timber burned in the stockpile. An hour later Bram struck a new bargain; at the next dawn there were no berries again, and Qamar now holds a grudge against Bram.
- Chain: restless (Needs) → offered, refused, struck, kept, struck again, kept, struck again → broken → refused as unsafe (Social) → unbound (Needs) → fire loosed → struck → broken → grudge (Social). 25 steps.
- Why it matters: the djinn's bargains are a year-long relationship that winter breaks, and the Proud trait's "refuses an unsafe bargain" finally does something.
