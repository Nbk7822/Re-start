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

## 2026-11-09 — Culture and faith, slice 1 (soak of 100 worlds × 1 year, build 4effb4e, ADR-0023)

Culture stories (a Culture event on the chain) are 356 of 1,020 (34.9%), in 89 of 100 worlds; nine distinct ways in by custom id (seven of the design's families). Seeds and lines are from `tools/soak --worlds 100 --years 1` on this branch.

### The rain was Wenlow's anger (seed 4, soak; policy "promised meals come first")
In the last days of winter the vampire Vale went from person to person for blood. Sera and Bram and Wren were too hungry themselves; you, Mira and Hollis simply said no. On the morning of day 38 it rained, and Vale, of the Nightborn and a follower of the Long Watch, took the rain as Wenlow's anger at Hollis for refusing blood (Wenlow, Lord of the Vein, is that world's fickle god of blood). Vale now held a grudge against Hollis, and two days later left the camp.
- Chain: went without (Needs) → hungry, starving → refusals (Social) → the rain read as an omen (Culture) → grudge → left (Social). 14 steps.
- Why it matters: the design's own example, arising unprompted. The god did nothing; a believer read the weather, and that reading is what crossed the grudge line.

### Vale's debt of honour (seed 3, soak; policy "the hungriest eat first")
On day 7 the vampire Vale, thirsting, asked Bram for blood; Bram refused, because the Old Roots forbid it. Oren refused, and so did Nia and Mira. By morning Vale was weak with blood hunger. That afternoon you, the stranger with no people and no faith, gave Vale your blood. Twenty-seven days later, in the winter famine, Vale brought you food to settle what was owed, as the Lantern-Shy do, and the debt was closed.
- Chain: thirst (Needs) → refusals, one for the taboo (Social, Culture) → gave blood (Work) → owes (Social) → repaid with food (Culture) → debt repaid (Social). 11 steps.
- Why it matters: before this slice most blood debts were never repaid (ADR-0018's seam). Here a vampire repays an outsider in berries when the outsider is starving, and the taboo is why the outsider was asked at all.

### The sacred tree and the harvest fire (seed 27, soak; policy "the hungriest eat first")
On the first night Oren, of the Longtable Kin, felled a tree for timber: the old tree sacred to the Old Roots. Mira, who follows the Old Roots, held a grudge against him from that hour, and on day 11 insulted him, so Oren took a grudge of his own. At the harvest fire on day 28 (the Old Roots keep it on day 8 of autumn) Mira set down her grudge against Oren. On day 36, in the winter hunger, Oren insulted her, and she took it up again.
- Chains: felled (Work) → desecrated (Culture) → grudge (Social) → set down at the harvest fire (Culture, GrudgeEnded); and felled → desecrated → grudge → insults → Oren's grudge → insult → Mira's grudge again.
- Why it matters: a feud with a cause on day 1, a truce the calendar brings, and a relapse the winter brings. Two systems the player never touched (a builder's nearest tree, a faith's festival day) frame it.

### A leader eats last (seed 8, soak; policy "you eat first")
Winter came on day 31. On day 39, at the small-hours share-out, Oren went without while you ate; among the Folk of the Kept Fire, a leader eats last. He held a grudge against you from that moment, and within the hour he had left the camp.
- Chain: winter (World) → hungry, starving, went without (Needs) → offended (Culture) → grudge → left (Social).
- Why it matters: the day-1 ration policy is read by a people's custom, not only by hunger.

### The vampire who fed a guest (seed 11, soak; policy "you eat first")
Bram wandered in on day 36, hungry and alone, and stayed after you promised him a meal within two days. By evening he was starving, and at the 02:20 share-out he went without. Half an hour later Ilex, a vampire of the Folk of the Long Dark, went out in the dark and found him something to eat, as the Folk of the Long Dark do for a guest. Your promise was kept, Bram owed Ilex a meal, and on day 38 Bram, who is Generous, repaid it.
- Chain: arrival (Incident) → promise on joining (Social) → hungry, starving, went without (Needs) → guest-bread (Culture) → promise kept → owes a meal → repaid (Social).

## 2026-11-09 — Culture and faith, slice 1: fresh-eyes review (soak of 100 worlds × 1 year, build ec20d10)

After the review fixes recorded in ADR-0023, culture stories are 347 of 1,022 (34.0%), in 89 of 100 worlds, and seven of the design's nine families start stories (greetings and word spread start none). The five culture stories above come from the build before the review (4effb4e); all five still happen in this soak.

### Bram's own meal (seed 6, soak; policy "the hungriest eat first")
Nia wandered in on day 37, hungry and alone, and stayed after you promised her a meal within two days. Twenty minutes later the camp shared out what food it had and Nia went without so others could eat. An hour after that Bram, of the Folk of the Low Water, gave up his own meal for her, as his people do for a guest. Your promise was kept, and Nia owed Bram a meal.
- Chain: arrival (Incident) → promise on joining (Social) → went without (Needs) → guest-bread from the host's own share (Culture) → promise kept → owes a meal (Social). 6 steps.
- Why it matters: the first guest-bread in a soak paid from the host's own share; the other eleven were berries picked in the night, mostly by vampires. Since the review only a share given away, not berries picked, becomes the cause of the host's own coming hunger.

### Sera, a guest wronged (seed 18, soak; policy "the hungriest eat first")
Sera wandered in on day 34, hungry and alone, and stayed after you promised her a meal within two days. By evening she was hungry and by two in the morning starving. At the 02:40 share-out she went without, and your promise to her broke there and then. She had come as a guest, and the Hearthfolk do not forgive a broken promise to a guest: she held a grudge against you, heavier than a broken promise alone leaves, and twenty minutes later she had left the camp.
- Chain: arrival (Incident) → promise on joining (Social) → hungry, starving, went without (Needs) → promise broken (Social) → wronged guest (Culture) → grudge → left (Social). 9 steps.
- Why it matters: M0's "Hollis's four hours" with a people's custom on the path. The player's promise and the share-out are the same as ever; Sera's people are why the break weighs what it does.
