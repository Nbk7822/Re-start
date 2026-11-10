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

## 2026-11-09 — Culture and faith, slice 1: fresh-eyes review (soak of 100 worlds × 1 year, build bfff5b8)

After the review fixes recorded in ADR-0023, culture stories are 347 of 1,038 (33.4%), in 87 of 100 worlds, and eight of the design's nine families start stories (greetings start none). The five culture stories above come from the build before the review (4effb4e); all five still happen in this soak.

### Bram's own meal (seed 6, soak; policy "the hungriest eat first")
Nia wandered in on day 37, hungry and alone, and stayed after you promised her a meal within two days. A quarter of an hour later the camp shared out what food it had and Nia went without so others could eat. Within the hour Bram, of the Folk of the Low Water, gave up his own meal for her, as his people do for a guest: your promise was kept, and Nia owed Bram a meal. By noon Bram was hungry and by evening starving. That night the vampire Ilex came to him for blood, and Bram, hungry himself, refused. Ilex went to Nia instead, and Nia gave it.
- Chains: arrival (Incident) → promise on joining (Social) → went without (Needs) → guest-bread from the host's own share (Culture) → promise kept → owes a meal (Social), 6 steps; and from the same guest-bread → Bram hungry, starving (Needs) → refuses Ilex (Social) → Nia gives blood (Work) → Ilex owes Nia (Social), 10 steps.
- Why it matters: the first guest-bread in a soak paid from the host's own share, and its cost travels: the host goes hungry for his guest, and the guest feeds the camp's vampire in his place. Only a share given away becomes the host's hunger cause; the review stopped berries picked for a guest doing the same.

### Sera, a guest wronged (seed 18, soak; policy "the hungriest eat first")
Sera wandered in on day 34, hungry and alone, and stayed after you promised her a meal within two days. By evening she was hungry and by two in the morning starving. At the 02:40 share-out she went without, and your promise to her broke there and then. She had come as a guest, and the Hearthfolk do not forgive a broken promise to a guest: she held a grudge against you, heavier than a broken promise alone leaves, and twenty minutes later she had left the camp.
- Chain: arrival (Incident) → promise on joining (Social) → hungry, starving, went without (Needs) → promise broken (Social) → wronged guest (Culture) → grudge → left (Social). 9 steps.
- Why it matters: M0's "Hollis's four hours" with a people's custom on the path. The player's promise and the share-out are the same as ever; Sera's people are why the break weighs what it does.

## 2026-11-09 — The four peoples: dryads, djinn, fae, undead (ADR-0022; soak 100 worlds × 1 year, build 2fba765)

1,213 stories (12.13 per world; 8.47 before this track). Stories in which each people's own need appears: undead 76 in 23 worlds, dryad 50 in 27, djinn 197 in all 100, fae 100 in 73.

### The djinn's fire and the dryad's tree (seed 13, soak; policy "promised meals come first")
On Day 11 Cindral the djinn, restless without a bargain, offered to gather berries for the camp in return for timber for the fire each dawn. You and Mira turned it down; Wren struck the bargain, and Cindral served it out to the turn of the season. On Day 22 Mira struck the next one. The fire wanted wood, and on Day 28 Cindral itself felled the tree Thessil was bound to. Thessil cried out as it came down, and now holds a grudge against Cindral. She grew faint away from the trees, was withering by Day 30, and Wren, Mira, you and Bram each tended her in turn; on Day 39 Thessil withered away, far from the living trees.
- Chain: restless (Needs) → offered, turned down twice, struck (Social) → served out, struck again (Social) → felled the bound tree (Work, citing the bargain) → cried out → faint → withering → withered away (Needs, Death). Systems: Needs → Social → Work.
- Why it matters: two peoples' needs in one chain. The djinn's price is fire, the fire needs wood, and the wood was the dryad's life; nobody in it is a villain.

### Sennet's grudge, and Sennet's word (seed 15, soak; policy "the hungriest eat first")
Sennet, one of the Good Folk, came out of the hill mist on Day 19 and swore to repay you for the welcome. In winter Wren wandered in hungry and stayed after you promised a meal within two days; at 02:29 on Day 37 Wren went without, you broke your promise, the broken word stung Sennet like cold iron, and Sennet now holds a grudge against you. Fourteen minutes later, with you starving, Sennet brought you a meal and kept their word to you.
- Chains: arrival (Incident) → promised a meal (Social) → hungry → starving → went without (Needs) → promise broken → stung → grudge (Social); and winter (World) → hungry → starving (Needs) → brought a meal (Work) → word kept (Social).
- Why it matters: a fae cannot forgive the broken word and cannot break their own, in the same quarter hour.

### Varrow and the bare bushes (seed 47, soak)
Varrow walked in out of the barrows, cold and sleepless, bound by their nature to forage. Winter came on Day 31 and the frost took the berries from the bushes. With nothing left to pick, by Day 32 Varrow was losing the purpose that kept them here, by Day 35 fading, and on Day 39 Varrow lay down, their purpose gone, and was at rest at last.
- Chain: winter (World) → purpose slipping → fading → at rest (Needs, Death). The same chain, headed by "Your priority for Forage is now never", is the player-caused version (scenario test `TakingAnUndeadsWorkAwayFadesThemToRest`).
- Why it matters: the undead's need ties them to a work and a season, and the player can take it away.

### Cindral's winter (seed 2, soak; policy "you eat first")
From Day 8 Cindral kept the night watch for a share of the berries, in bargains Mira struck for the camp, and both kept their word at the turn of two seasons. On Day 36 the camp had no berries at dawn and the bargain broke. Restless, Cindral came to you; you would not strike an unsafe bargain. Burning with nothing to bind it, and wronged, Cindral let the fire loose: 2 timber burned in the stockpile. An hour later Bram struck a new bargain; at the next dawn there were no berries again, and Cindral now holds a grudge against Bram.
- Chain: restless (Needs) → offered, refused, struck, kept, struck again, kept, struck again → broken → refused as unsafe (Social) → unbound (Needs) → fire loosed → struck → broken → grudge (Social). 25 steps.
- Why it matters: the djinn's bargains are a year-long relationship that winter breaks, and the Proud trait's "refuses an unsafe bargain" finally does something.

## 2026-11-09 — The four peoples' look (view only; ADR-0022 update)

No new stories. This track changes only the view, and all 100 soak logs are byte-identical to the build before it (1,213 stories, 12.13 per world). What it adds is that the peoples' stories now show on the map. Two playtest views follow; the game sets no ration policy, so its histories differ from the soak's.

### Varrow's dust (seed 10, playtest)
At 02:00 on Day 25 Varrow, bound by their nature to keep watch, meets the night thief near the watch post. Where Varrow is struck, the ground takes a pale drift of grave dust, a step from the thief's red blood.
- Chain on the map: night thief (Incident) → spotted, fought (Combat) → a blow to Varrow, shown as dust, not blood (view). Scenario test: `TheUndeadOnWatchGiveDustWhereTheThiefStrikesThem`.

### Thessil by the lone pine (seed 13, playtest)
Thessil's bound tree comes down early on Day 28. By 10:00 she stands by a lone pine among the stumps, with a stain of amber sap on her dress where the fall wounded her, and her autumn crown browning as the forest she needs runs low.
- Chain on the map: felled bound tree (Work) → cried out, wounded (Needs) → forest low (Needs), shown as a sap stain and a wilting crown (view).

## 2026-11-09 — The four peoples, reviewed (ADR-0022 review update; soak 100 worlds × 1 year)

1,161 stories (11.61 per world; 12.13 before this review, 8.47 at c818511). Stories each people appears in: Varrow 70 in 38 worlds, Thessil 78 in 47, Cindral 194 in 100, Sennet 154 in 83. The fall is sibling stories: four people tending one withering dryad used to be four stories, and the dryad no longer withers to death in her bed.

### The camp's axe and Thessil's tree (seed 13, soak; policy "promised meals come first")
On Day 11 Cindral the djinn, restless without a bargain, offered to gather berries for the camp for timber for the fire each dawn. You and Mira turned it down; Wren struck it, and Cindral served it out to the turn of the season. On Day 22 Wren struck the next one. The camp kept timber in store for the djinn's fire, and on Day 25 Oren felled the tree Thessil was bound to. Thessil cried out as it came down, and now holds a grudge against Oren.
- Chain: restless (Needs) → offered, turned down twice, struck, kept, struck again (Social) → Oren felled the bound tree, the felling citing the bargain (Work) → cried out (Needs) → grudge (Social).
- Why it matters: before this review the djinn felled the tree itself, to pay its own price (in 7 of the 27 worlds where her tree fell). Now the camp pays the djinn, and the axe is a neighbour's. Traced hour by hour, Thessil then withers beside a lone tree near (33, 25) from Day 32 and lives to the year's end, sleeping in the shelter and getting up to stand by the tree whenever the withering starts to harm her. Before the fix she slept until she died, with eight trees standing.

### Varrow and the bare bushes, again (seed 74, soak)
Winter came on Day 31 and the frost took the berries from the bushes. Varrow, bound by their nature to forage, was losing the purpose that kept them here by Day 32 and fading by Day 34, and on Day 40 lay down and was at rest at last.
- Chain: winter (World) → purpose slipping → fading → at rest (Needs, Death).
- Why it matters: the undead no longer lie down to sleep when wounded, so a fading undead goes on until they fall and is carried in from where they fell, not tended in bed.

The earlier playtest entries describe the build before this review, and the histories have moved. Seed 10's Varrow no longer meets the thief; their grave dust beside the thief's blood now shows in seed 11, on Day 33 at 19:00, near (20, 28). Seed 13's bound tree falls on Day 25, to Oren. The djinn now reads as living flame: a low warmth and wavering flame-light, with no smoke.

## 2026-11-09 — Zones slice 1: stockpiles and the meeting place (ADR-0028; soak 100 worlds × 1 year)

With no zones marked, 1,423 stories (14.23 per world; 1,426 at the merge base). With the stand-in's `--zones dry` plan (the stockpile moved into the covered ruins on the first morning), 1,313: nothing spoils in the damp, but the ruins sit in the middle of the camp, where the watch and the Protective see most thieves coming, so thefts fall too and the move rarely heads a story (4 stories in 3 worlds). People kept away from the fire in 98 worlds.

### The dry stores (seed 91, soak `--zones dry`; policy "promised meals come first")
On the first morning you marked a stockpile in the covered ruins and took away the one by the hearth; nothing spoiled in the damp all year. Late on Day 35 a thief crept in and made off with 3 berries from the stockpile, out of sight of Ilex on watch. By the next evening Nia and Wren were hungry, by midnight starving, and at the 02:18 share-out on Day 37 both went without so others could eat. At 02:31 Wren took the meal set aside for Bram, and at 02:51 Nia did the same. Bram held a grudge against them both, and that evening he left the camp.
- Chains: stockpile marked (World) → theft out of the watch's sight (Incident) → hungry, starving, went without (Needs) → took the meal → grudge (Social), once for each thief of a meal; Wren's runs on to Bram leaving.
- Why it matters: the zones design's story 1, arising unprompted. The player's tidy-up of the stores is the first link of a feud five weeks later, and nobody in it is a villain. It is rare (see ADR-0028 on why the ruins are safer than they look).

### Out of your sight (seed 30, soak `--zones dry`; policy "the hungriest eat first")
You moved the stores into the ruins on the first morning. On Day 20, while you kept the night watch, a thief made off with 3 berries from the stockpile, out of your sight. By six you were getting hungry, and at 06:38 Sennet, one of the Good Folk, brought you a meal: the welcome they had sworn to repay.
- Chain: stockpile marked (World) → theft out of the watch's sight (Incident) → hungry (Needs) → brought a meal (Work) → word kept (Social).

### The fire they would not share (seed 27, soak, no zones marked; policy "the hungriest eat first")
On the first night Oren felled a tree for timber: the old tree sacred to the Old Roots. Mira, who follows the Old Roots, held a grudge against him from that hour, and from then on the two never sat at the fire together. Day after day one of them came to the fire, found the other there and went to sit alone by the shelter: "Mira kept away from the fire while Oren sat there", and, once she had insulted him on Day 6 and he had taken a grudge of his own, "Oren kept away from the fire while Mira sat there", 27 times between them in sixteen days. On Day 17 you insulted Mira, and within the hour, holding two grudges and sour from sitting apart, she left the camp. You took her place: you had held a grudge against Oren since Day 9, and for ten more days you kept away from the fire while he sat there, until on Day 27 he held a grudge against you too and left at dusk, the evening before the Old Roots lit their harvest fire.
- Chain the analyzer found: felled → desecrated (Culture) → grudge → Mira left (Social); and felled → desecrated → grudge → insults → grudges, to Mira's grudge against you.
- Off the chain, on the map: 49 "kept away" lines in the year. They are the feud made visible, not causes: the next insult cites the grudge itself. Mira's sitting apart (−3 each time) is part of the black mood her two grudges took her away in.
- Why it matters: the culture slice's soak told this world as a feud the harvest fire set down. With the camp gathering at the fire, the same first night now shows as a fire two people take turns at, and the feud runs out of people before the festival comes.

### Mira apart at dusk (seed 27, playtest view)
Day 8, 18:18: the camp sits round the fire (Hollis, Wren, Bram, Sera, Thessil and you), and Mira stands alone by the shelter's west wall with the gloom mark over her head, nineteen minutes after she kept away from the fire while Oren sat there. The screenshot is ADR-0028's.


## 2026-11-09 — Schedule slice 1: the camp's day and the watch in turns (ADR-0029; soak 100 worlds × 1 year)

With the camp's default day and watch, 1,417 stories (1,423 at the merge base; 6,989 against 6,860 on 500 seeds), 170 of them through the watch or overwork, in 89 worlds. Humans now sleep 22:00–06:00 (a median 7.6 h, from 4.7) and the night is kept in four-hour turns, handed from person to person. Seeds are from `tools/soak --worlds 100 --years 1 --days <plan>`; the nightwatcher plan puts Mira on nights.

### The walk to wake Sera (seed 92, soak `--days nightwatcher`; policy "you eat first")
On the night of Day 38 Vale kept the first turn and Varrow the second. At 03:30 Varrow left the post to wake Sera for the last turn, and two minutes later, while he walked, a thief made off with 3 berries; nobody was on watch. By morning Nia was hungry, and at 12:27 she took the meal set aside for Mira. Mira held a grudge against Nia, was starving by mid-afternoon, and on Day 40 left the camp.
- Chain: took the watch, took the watch, went to wake (Work) → the theft, citing the walk (Incident) → hungry (Needs) → took the meal → grudge (Social); a second story runs on through Mira's hunger to her leaving.
- Why it matters: the schedule design's first story, arising unprompted. The empty post during a handover is a real window on the map, and the theft says why the post was empty.

### Bram would not get up (seed 77, soak; policy "you eat first")
Bram had held a grudge against Varrow since Day 25, and every morning kept away from the fire while Varrow sat there. Three nights running, at 23:29, Varrow walked to wake him for his turn on watch, and three times Bram would not get up for Varrow. The third time, Varrow held a grudge against Bram. The next morning Varrow felled the tree Thessil was bound to, and on Day 38 he left the camp.
- Chain the analyzer found: took the watch, went to wake (Work) → would not get up → grudge → left (Social). Off the chain, in what happened: the mornings at the fire and the other two nights.
- Why it matters: a grudge now shows at night as well as at the fire. The fair rota sent Varrow to the person he liked least among those equally due, and a refusal from the same person three nights over is what turned him.

### Sera's watch (seed 32, soak; policy "you eat first")
At 03:00 on Day 40 a thief crept in; Ilex spotted it, but it made off with 3 berries. By 09:00 Sera was starving. That night, when Vale's turn ended, Vale woke her: Sera, hungry, would not take the watch. Vale went to wake you instead, and at 23:56 you kept the watch in Sera's place. Sera owes you for a watch kept in their place.
- Chain: night thief (Incident) → spotted (Combat) → theft → starving (Needs) → refused, hungry (Social) → kept in her place (Work) → owes (Social). Five systems.
- Why it matters: a debt that comes from the watch, and the theft the night before is why. ADR-0018 noted that most debts are never repaid; a watch kept in someone's place can close an old one or open a new one, and here it opens a debt from Sera to you.

### The Longest Night, and after (seed 67, soak `--days nightwatcher`; policy "promised meals come first")
At dusk on Day 38 the River-Born began to keep the Longest Night by the hearth, and nobody kept watch. After the fire, at 23:19, Vale took the watch. At 03:24 Vale woke Mira, who told Vale to find someone else; Sera was too tired; Vale left the watch with nobody to take it. At 04:53 Varrow came to the empty post and kept the watch in Sera's place, and Sera owes Varrow for it.
- Chain: the festival (Culture) → took the watch → went to wake → told to find someone else, too tired (Social) → left the watch → kept in Sera's place (Work) → owes (Social).
- Why it matters: the festival leaves the post empty on purpose and is the first link, and an Independent's own way and a tired refusal make the rest of the night.

### Sennet's sworn watch (seed 3, soak; policy "the hungriest eat first")
On Day 34 two berries spoiled in the damp; by 19:00 Sennet, one of the Good Folk, was starving. At 23:28 Varrow woke Sennet for the watch, and Sennet, hungry, would not take it. Varrow kept the watch in Sennet's place, and Sennet swore to repay him.
- Chain: food spoils (Incident) → hungry, starving (Needs) → refused, hungry (Social) → kept in Sennet's place (Work) → swore to repay (Social).
- Why it matters: a fae cannot owe without swearing. A watch debt is a debt like any other, so the Good Folk's oath comes with it.

### Your night on watch (seed 1, playtest view)
Day 2, 09:00. Last night Oren kept the watch until 23:00, then woke you; you kept it until 04:00 and woke Mira. Your inspector's lived strip shows the five hours on watch, each notched where your planned day said sleep, and a nap from 18:00 to 21:00 the evening before, notched again. The Schedule list beside it reads "Last night: Oren 19:00–23:00 · You 23:00–04:00 · Mira 04:00–06:00". On Day 22 at midnight the lantern burns over Vale at the post; the night folk keep most watches once they have come.
