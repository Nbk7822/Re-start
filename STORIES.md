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


## 2026-11-09 — Who you are and Return by Death, slice 1 (ADR-0030; soaks of 100 worlds × 1 year)

You are now reborn as a rolled people with one rolled gift, wounds bleed until someone tends them, and any death but the end of your lifetime sends the world back to a fate knot. Ordinary soak: 1,803 stories (18.03 per world-year; 14.26 at the merge base); you died on your own and came back in 23 of 100 worlds (76 of 300 on seeds 1 to 300). Under `--fates` every world returns and 63% of lost futures hold a story. Lines below are from the soak logs (`tools/soak`, the flags given) and from the game.

### The refusals that came back (seed 3, ordinary soak; policy "the hungriest eat first")
You woke here as a vampire, with a bottomless satchel. In a future that never came, from Day 7 to Day 35, the humans of the camp turned you away again and again: Nia, Oren, Mira, and Bram, whose Old Roots forbid it. You came to hold grudges against Nia, Oren and Mira; on Day 14 you insulted Oren, and on Day 17 he left. At two in the morning of Day 35 you died of your wounds. The world went back twenty-eight days, to six in the morning of Day 7, and you came back remembering every refusal:
```
  ~~ You died on Day 35, 02:00, and the world went back to Day 7, 06:00 ~~
  Day 7, 06:00 — You came back, remembering how you died.
  Day 7, 06:00 — You remembered a wrong from a future that never came: Nia refused to give you blood.
  Day 7, 06:00 — You now hold a grudge against Nia.
  Day 7, 06:00 — You remembered a wrong from a future that never came: Mira refused to give you blood.
  Day 7, 06:00 — You now hold a grudge against Mira.
  Day 7, 06:00 — You remembered a wrong from a future that never came: Oren refused to give you blood.
  Day 7, 06:00 — You now hold a grudge against Oren.
  Day 7, 06:00 — You remembered a wrong from a future that never came: Bram insulted you.
```
Three days later you insulted Oren. Oren now held a grudge against you, and on Day 14 he left the camp, three days sooner than he had in the future you lost. You did the same to Mira on Day 19 and to Nia on Day 22, and each of them left too, Mira four days and Nia fifteen hours sooner than before.
- Chains (final log, Stories 7, 8 and 9, one each for Oren, Mira and Nia): returned (Fate) → remembered a wrong (Mood) → your grudge (Social) → you insulted them → their grudge against you → they left (Social). 6 steps each.
- Why it matters: the design's "theft that never happened", with refusals in place of a theft, three times over. The world forgot and you did not: grudges from a lost future, at half their strength, drove away the people you blamed before they had refused you in this timeline, and the camp emptied faster than it had the first time.

### A future that never came (the same seed's Lost futures section)
The log ends with what was lost: 28 days, eleven stories, every line frozen as it happened. A few of them:
```
  A future that never came (Day 7, 06:00 to Day 35, 02:00). Stories that ended in it: 11.
    Day 8, 19:10 — You now hold a grudge against Nia.
    Day 10, 03:04 — Cindral tended your wounds.
    Day 11, 05:00 — Ilex left the camp.
    Day 14, 20:06 — You insulted Oren.
    Day 17, 16:00 — Oren left the camp.
    Day 23, 06:20 — Sennet tended your wounds.
    Day 23, 06:20 — Sennet kept their word to you.
    Day 24, 01:00 — Nia left the camp.
    Day 35, 02:00 — You died of your wounds.
```
- Why it matters: fixed decision 9's Chronicle of Lost Futures, read in second person in any later world. Set beside the final log it shows what changed and what did not: Vale, Ilex, Oren, Mira and Nia leave in both timelines, but in the one you came back to the last three go because of what you remembered, and sooner.

### Nia saw you eat from nowhere (seed 18, `--gift satchel`; you human; policy "the hungriest eat first"; satchel order "keep")
On the last night of the year, at the midnight share-out, you went without so others could eat. By six in the morning you were hungry, and you ate berries from your satchel. Nia, going hungry herself, saw you eat from nowhere. She now held a grudge against you, and within the hour she had left the camp.
- Chain: went without → hungry → ate from your satchel (Needs) → seen eating from nowhere → grudge → left (Social).
- Why it matters: the design's satchel story, arriving on its own on the year's last day: the gift feeds you when the policy will not, and the camp sees it.

### The blow meant for you (seed 4, `--gift fortunes_favourite`; you a vampire; policy "promised meals come first")
A thief crept toward the camp at seven in the evening of Day 13. Thessil spotted them and Mira fought. The thief's blow, meant for you, fell on Varrow. An hour later you tended Varrow's wounds, and Varrow owes you for it.
- Chain: night thief (Incident) → spotted, fought (Combat) → the blow turned (Gift) → tended (Work) → owes care (Social). Five systems.
- Why it matters: your luck hurts the people beside you, and here you are the one who has to make it right; Varrow, bleeding, waited where they stood for you to come.

### Oren knew you (seed 2, `--gift familiar_faces --fates`; you a dryad; policy "you eat first")
At three in the morning of Day 29 a thief crept toward the camp and Cindral spotted them; the director tied a knot at that turn. You fell dead at dawn the next day (the soak's forced death), and the world went back to the moment the thief was seen. Oren looked at you as if he had known you for years, and within the hour your gift grew: Familiar faces now keeps two fifths of what people felt for you.
- Chain (Story 13): night thief (Incident) → spotted (Combat) → you came back (Fate) → Oren knew you (Social) → your gift grew (Gift). Five systems, across the return.
- Why it matters: a knot tied at a turn lets a return land on the turn; what Oren had come to feel for you survived only in you, and the gift is how it reaches him.

### Bled out while the camp slept (seed 23, playtest)
In the game (no stand-in player), you wake as a vampire with Familiar faces. At two in the morning of Day 10 you die of your wounds in the shelter while the camp sleeps around you and Vale keeps the watch: nobody came to tend them in time. The screen darkens over your body ("You died of your wounds."), folds into a colourless pale, and comes back paused at three in the afternoon of Day 8: "Day 8, 15:00. You came back. You remember everything." You are in the shelter beside Vale, out of the sun; the others are at work.
- Chain on screen: a thief's blow (Combat) → a wound that bleeds (Needs) → nobody near and awake → death → the return (Fate). Screenshots: `return-1-death.png`, `return-2-fold.png`, `return-3-back.png`.
- Why it matters: the founder asked to die through the world's own dangers; this is one, and it reads at a glance.

## 2026-11-09 — Review of who you are and Return by Death, slice 1 (ADR-0030 review; soaks of 100 worlds × 1 year)

The entries in the section above describe the build before this review, and two have moved. Seed 3's refusals still come back word for word; only its first stash moved from Day 1 to Day 2, because a vampire no longer keeps a reserve of berries it cannot eat. In seed 2 under `--gift familiar_faces --fates`, Mira and Oren both know you now (the log shows every gift line), but the gift no longer grows within the hour: they were your friends before they felt it, and the gift grows only when a familiar face becomes your friend or does you a kindness. The review also found that a return seldom reaches the new timeline's stories: 16 stories in 7 of 300 ordinary worlds run through one, most of them a vampire's remembered refusals. Lines below are from the review's soaks and the game.

### Twice dead before breakfast (seed 100, ordinary soak; you a djinn with Fortune's favourite; policy "promised meals come first")
The camp woke at midnight with everyone rested and went to work in the dark. At ten past two you picked berries and hurt yourself; Oren was already asleep and Mira lay down within the hour, you collapsed at three, and by four you had bled to death and the world went back to its first minute. In the timeline that followed everyone slept until dawn, and at half past seven you hurt yourself mending the shelter roof while Mira and Oren worked on the same roof. Neither stopped their work. You died at nine and came back to the first minute again. Your luck turns harm onto whoever stands within a pace of you, and both times nobody did. The Chronicle of Lost Futures keeps both mornings:
```
  A future that never came (Day 1, 00:00 to Day 1, 04:00). Stories that ended in it: 1.
    Day 1, 02:10 — You picked 3 berries.
    Day 1, 02:10 — You were hurt in an accident while working.
    Day 1, 04:00 — You died of your injuries.

  A future that never came (Day 1, 00:00 to Day 1, 09:00). Stories that ended in it: 1.
    Day 1, 07:30 — You repaired a section of the shelter roof (1 of 3 now sound).
    Day 1, 07:30 — You were hurt in an accident while working.
    Day 1, 09:00 — You died of your injuries.
```
- Chain (each lost future's story): the work (Work) → the accident (Incident) → you died of it (Needs).
- Why it matters: two lost futures a reader can follow, and two of the review's findings in one morning. The first night is the deadliest of the year (14 of the 106 natural deaths on seeds 1 to 300 fall on Day 1), because the camp works through its small hours and then sleeps while a wound bleeds; and carers pick up care only when their job ends, so two people at work on the same roof can let you bleed out. Both are design calls (ADR-0030, review).

### The last card (seed 23 in the game, with a one-year soul through `RETURN_MODS`)
You woke as a vampire with Familiar faces, died once of your wounds on the night of Day 10 and came back to the afternoon of Day 8. At dawn on Day 41 your soul's thread ran out. The last card, and the head of the Chronicle's This life tab, read:
```
Your soul's thread ran out, at 26. There is no return from this.
You were reborn here as a vampire, with a gift: Familiar faces.
You lived a year here.
You died once before this, and came back.
Oren, Ilex, Bram, Nia, Sera, Vale, Wren, Hollis, Thessil, Cindral and Sennet outlived you.
```
- Built from stored state: the run memory (your people, gift, returns, when it ended) and who is still alive.
- Why it matters: the design closes a life on who you were and who outlived you (§6.3); the build closed it on one line. Mira and Varrow are not on the list: they died before you did. Screenshots: `final-end-card.png`, `final-end-chronicle.png`.

## 2026-11-09 — The Build menu, slice 1a (ADR-0027; soak 100 worlds × 1 year, `--plan mixed`)

Building stories in 22 of the 66 worlds where the stand-in player made a plan (6 of 33 with beds alone, 16 of 33 with the camp), 35 stories from four sets of systems. Every one is headed by the player's plan and runs through a felling: the timber a plan needs is cut from the trees nearest the camp, and some of those belong to a dryad or a faith. Passing over fires (57 times in 29 of 33 camp worlds) but ends no story within a year, and the design's showcase, "The last of the timber", needs a vampire promised a roof, which no longer happens at this baseline (the old roof is mended on day 1); it is told by the scenario test below.

### Three beds, and Thessil (seed 16, soak `--plan camp`; policy "promised meals come first")
On the fourth morning the camp had outgrown its beds, and you marked out three more in the old shelter. An hour and a half later you went out with the axe yourself and felled a tree for the timber. Thessil, a dryad who had come to the camp that week, grieved for the tree you cut down; she held a grudge against you from that moment, and eighteen minutes later she had left the camp.
- Chain: your plan (World) → you felled a tree for it (Work) → Thessil grieved, grudge, left (Social). 5 steps.
- Why it matters: the felling cites the plan, so a stranger reads a decision about beds turning into a dryad walking out. Nobody is a villain; the beds were needed.

### The sacred tree for the beds (seed 22, soak; plan beds; policy "promised meals come first")
On Day 5 you marked out three beds in the shelter. Before noon you had felled a tree for the timber: the old tree sacred to the Faith of the Deep Root. Bram, who keeps that faith, held a grudge against you, and within the hour he had left the camp.
- Chain: your plan (World) → felled (Work) → the sacred tree desecrated (Culture) → grudge → left (Social). 5 steps.
- The same chain with a neighbour's axe ends in a festival instead (seed 15, plan camp, "the hungriest eat first"): Bram felled the tree sacred to the Green Way for your three beds on Day 4, Sera held a grudge against him, and at the harvest fire on Day 26 she set it down.

### Mira and Thessil's tree (seed 39, soak; plan beds; policy "the hungriest eat first")
On Day 4 you marked out three beds. Mira went to fell timber for them, and the tree she brought down was the one Thessil was bound to. Thessil cried out as it came down, and now holds a grudge against Mira.
- Chain: your plan (World) → Mira felled the bound tree (Work) → Thessil cried out (Needs) → grudge (Social). 4 steps.
- Seen beside it in camp worlds: a dryad whose tree fell for the hut withers, and each person who tends her makes a story of its own (ADR-0022's sibling seam). The camp plan leaves about 7 trees of 24 by the year's end.

### The last of the timber (scenario test `TheLastTimberIsCitedWhenTheRoofPromiseBreaks`)
Ilex, a vampire, agreed to stay after you promised a sound roof against the sun within two days. You had marked out a bed. Sera, who had no bed, built the bed before the roof, and in fetching its timber she used the last of what the roof needed: "Sera used the last of the timber on a bed." Ilex, passed over, thought less of her, and when the two days were out your promise broke, citing that moment.
- Chain: your plan (World) → Sera used the last of the timber (Work) → promise broken → grudge (Social).
- Why it matters: the design's own first story, in the engine. It waits in soaks for a camp whose roof is still broken when a vampire arrives.


## 2026-11-09 — The Build menu, slice 1b: the view (ADR-0027 update; playtest)

The view adds no mechanics, so the soak is unchanged (every one of the 100 `--plan mixed` world logs is byte-identical to slice 1a: 1,398 stories, 13.98 per world-year). What it adds is that a player can now start these chains from the map and watch them run. One playtest through the real tool, kept here because it shows the chain a player follows with their eyes, not because the analyzer counts it.

### The first hut (seed 1, playtest through the Build menu)
Paused on the first morning, you dragged out a 5×4 room east of the fire with Shift held, called off the wall piece facing the hearth and put a door there, dragged a roof over the inside and two beds against the east wall; the plans sat pale and dashed, "Takes effect when time moves". When time moved: "You marked out 13 walls.", a door, a roof section, 2 beds. There were two timber in store. Before noon you and Mira had the axes out, each felling citing the plan, and at 14:32 you were hurt in an accident at the work; the blood stayed where it fell. The walls rose course by course through the evening; Oren finished the thirteen at 21:30, you the door at 22:03, Mira the first bed at 22:12; Oren raised the roof the next morning and you built the second bed. Then the hut stood empty. Nobody in camp lacked a bed: three starters slept in the shelter's bedrolls and the fourth was free. Bram, who came later, claimed the bed Mira built on Day 4, and Sera the one you built on Day 5; on the sixth night both slept there under blankets of their own colours.
- Chain: your plan (World) → fellings that cite it (Work) → an accident (Incident) → walls, door, beds and roof (Work) → claims days later (Social). No grudge, debt, death or departure, so no story by BIBLE section 1, as the design intends: finishing a building ends nothing by itself.
- Why it matters: the founder can now do in play what the stand-in does in soaks, and see on the map what slice 1a measured. Here, as there, the timber was committed before anyone had a stake in it (ADR-0027 item 12). Built a week later, with newcomers sleeping on the floor, the same hut is where "the last of the timber" and slice 1c's envy begin.

## 2026-11-09 — The Build menu, review (ADR-0027 review update; soak `--plan mixed`, 100 and 500 worlds × 1 year)

Every slice 1a entry above still happens at the review's last commit, in the same words: seed 16 (Thessil leaves eighteen minutes after you fell the tree sacred to her faith for three beds), seed 22 (Bram leaves within the hour), seed 15 (Sera sets her grudge against Bram down at the harvest fire on Day 26), seed 39 (Thessil cries out as Mira fells her tree). Over 500 seeds the building stories (246, in 112 of the 333 worlds with a plan) are all still of one shape: the plan heads a felling, and the felling ends in a dryad's or a faith's grudge, sometimes a departure. Nothing built, claimed or taken down sits on any story's path yet, and passing over, now told only of people with no bed to go to, ends none within a year.

### The hut's walls and Thessil's tree (seed 115, soak `--plan mixed` over 500 worlds; plan camp)
On the eighth morning, the camp having outgrown its beds, you marked out the thirteen walls of a hut. Just after noon you had the axe out yourself, and the tree you brought down was the one Thessil was bound to. She cried out as it fell, held a grudge against you, and before the hour was out she had left the camp.
- Chain: your plan (World) → you felled her tree for it (Work) → Thessil cried out (Needs) → grudge → left (Social). 5 steps.
- Why it matters: seed 16's chain with the hut in place of the beds, six days after the design's fixed day 2 would have put it up (ADR-0027 item 12), when there was someone in camp for the felling to hurt.

## 2026-11-09 — The founder plays the return (ADR-0031 play check, first try; milestone-0 at 3f5068d)
The founder played the merged slice of Who you are and Return by Death with `tools/play 23`: seed 23, where you wake as a vampire and, left alone, the build's own run dies of its wounds at 02:00 on Day 10 and comes back to Day 8, 15:00. It was a short session, well under ADR-0031's two hours, and before the opening act. Asked ADR-0031's four questions, the founder answered:
1. Did you die and come back, and did you want to go on? "no"
2. Can you name a companion and say what that person is like? "oren and not much"
3. Did you do anything differently because of what you remembered? "don't know what i remembered"
4. Your run in one line? "meh"
- Lead's reading, for the designs that follow:
  - The return did not land in this sitting: either it never came or it did not make the founder want more. That is ADR-0031 #3's case for the opening act.
  - A companion's name stuck, but not who they are (ADR-0031 #4 and #5).
  - Nothing showed the founder what was remembered (ADR-0031 #5: a memory of what you learned, and lost futures that hurt).
  - The run missed the founder's goal of a story the player can't forget.
- This counts as a miss, and ADR-0031 asks for misses to be recorded. It is not yet the full play check: that needs two hours, the opening act, and two players who have never seen the game.
