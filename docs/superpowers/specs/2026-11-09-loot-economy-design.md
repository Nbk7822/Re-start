# Loot and the economy: design

- **Date:** 2026-11-09. **Status:** proposed, for the founder's sign-off. Revised the same day after a builder's read. The answers sit where a builder looks for them: how far the loot and trade orders carry (ADR-0025's compliance rule; 5.4, 5.8), where slice 1 lands and what it borrows (9.0), and goods a person owns (5.4). Smaller calls are marked "(lead's call)".
- **Founder's ask:** "the loot system and in-game economy system of path of exile"; "coming upon new items after defeating someone and these items either enhancing the player and his people or affecting the story in a way or whatever and an in-game economy that exists where prices and trade are determined by and unpredictable economic events and actions can cause butterfly effects. In general, expanding the amount of variables"; "items can also be found in treasure chests, dungeons etc"; on dungeons, "they can raid and explore it for treasures but every level they have to kill a tougher boss"; on rewinds, "after a rewind the economy just goes back to the state at that point and does whatever it is its own machine".
- **Covers:** things with a past (items, kit, qualities, rarity, relics); where loot comes from and who gets it; money; markets, prices and shocks; the peddler and trading; how a distant market reaches the camp. Ventures into dungeons and making things are designed for later slices.
- **Reads with:** CLAUDE.md (main principle; fixed decisions 1, 2, 4, 5, 8, 10, 12); BIBLE.md §2, 4, 5; ADR-0004–0007, 0013–0019, 0021, 0024, 0025, and 0022–0023 on their branches; the soak in `out/soak`; the code at `7849ab8`; the other seven designs in this folder (section 8).

**This is the items milestone** that ADR-0018 §5, Zones §3.6, culture-faith F5 and peoples batch 3 wait for, for *things* only. Food stays berries-only through slice 1: `ContentDb`'s berries-only check, `Rations.Assign` and the winter squeeze are untouched. Food that keeps and food that can be bought come in slice 3, under Zones §3.6's balance guard.

**What signing approves:** five new content files with schemas (`qualities`, `relics`, `loot`, `markets`, `shocks`); new fields in `items.json`; new entries, without new fields, in `incidents.json`, `work.json`, `roles.json` and `thoughts.json`; `en.economy.json`; the state, commands and enum values in section 5, with `SystemTag.Economy`, `DiceSystem.Economy` and each person's purse; TruthIds 150–159 and DiceIds 350–369, two of them tags for compliance rolls; one cause fix that reaches every world, the shortage memory (5.9), measured as its own step; the runner flags `--loot` and `--trade`. Where this design lands first, also the world design's `names.json` with its schema, `CharStatus.Abroad` and `JobKind.Visit`. No dependency, no fixed decision changed. Ventures, making and the section 10 questions are not approved.

**In short**
- **Finds.** Things drop from thieves and raiders, wash up on the river and lie under the ruins. They roll rarity and qualities as in Path of Exile, but every quality is something the sim already models, and every thing remembers its past.
- **Story things.** Blades remember their last holder's grudge, a black ring brings misfortune, and a stolen heirloom brings its owner looking for it.
- **Money is goods.** Each world counts value in silver or salt (a Truth), and there is no coin until a civilization mints one.
- **A market of its own.** The nearest settlement's market moves its prices each dawn and suffers shocks, and a recurring peddler brings its goods and news to the camp.
- **Delegation.** Standing orders say who keeps loot and what may be sold, and how people feel about you decides how far those orders carry, as for every rule you set. The companion who ranks Trade highest deals by their own nature.
- **Endings.** Stories end in grudges, debts, deaths, departures and recruitments. A thing changing hands never ends a story by itself.

---

## 1. Why: the stories

Path of Exile's loot works because any kill might change everything; its economy works because its money is things players use up. Return points both at people: a thing matters for who carries it and what it has done, and a price matters because someone in the camp acts on it.

**The knife that remembered** (slice 1a; loot goes to the stores)
```
Day 12, 01:30 — A thief crept toward the camp in the dark, a knife at its belt.
Day 12, 01:41 — Hollis spotted a thief.
Day 12, 01:44 — Hollis fought the thief.
Day 12, 02:05 — The thief fled, wounded, and dropped its knife.
Day 12, 02:20 — Hollis carried the knife to the stores.
Day 13, 22:00 — Hollis took the camp's knife for the watch.
Day 19, 23:10 — Nia took the meal set aside for Hollis.
Day 19, 23:10 — Hollis now holds a grudge against Nia.
Day 20, 06:00 — Hollis left the camp, leaving the camp's knife behind.
Day 20, 06:00 — The camp's knife carries Hollis's grudge against Nia.
Day 20, 22:00 — Oren took the camp's knife for the watch.
Day 21, 13:05 — Oren insulted Nia.
Day 22, 18:40 — Oren insulted Nia.
Day 22, 18:40 — Nia now holds a grudge against Oren.
```
The chain runs: thief (Incident) → fight (Combat) → the knife (Economy) → meal theft, grudge and departure (Needs, Social) → the knife changes hands (Economy) → insults → grudge (Social). Oren never quarrelled with Nia; he inherited the quarrel.

**Her mother's locket** (slice 1b)
```
Day 15, 03:05 — A thief crept toward the camp in the dark.
Day 15, 03:14 — Vale fought the thief.
Day 15, 03:30 — The thief fled, wounded, and dropped its pack.
Day 15, 07:10 — Hollis took a silver locket from the thief's pack.
Day 18, 14:20 — A stranger, Liss, came up the road asking after her mother's locket.
Day 18, 14:45 — Hollis would not give up the locket he found.
Day 18, 14:45 — Liss now holds a grudge against Hollis.
Day 20, 02:30 — Liss crept back into the camp in the dark for the locket.
Day 20, 02:41 — Ilex spotted Liss.
Day 20, 02:44 — Ilex fought Liss.
Day 20, 03:02 — Liss died of her wounds.
```
The chain runs: thief (Incident) → fight (Combat) → found (Economy) → the stranger (Incident) → refusal and grudge (Social) → her return (Incident) → death (Combat). Hollis is Proud; a Generous finder would have given it back, and Liss might have stayed.

**A famine three days away** (slice 1b; the trade order is "leave it to the trader")
```
Day 27, 11:00 — Odo the peddler came up the road with his cart.
Day 27, 11:00 — Word from Greyford: the harvest has failed there, and food is very dear.
Day 27, 11:40 — Bram sold 5 berries to Odo, for the hungry of Greyford.
Day 28, 02:10 — A thief crept toward the camp in the dark, one of the hungry from Greyford.
Day 31, 00:00 — Winter came, and frost took the berries from the bushes.
Day 31, 19:00 — Nia is hungry.
Day 31, 19:00 — Nia blames Bram for selling the camp's food.
Day 32, 08:00 — When the camp shared out what little food it had, Nia went without.
Day 32, 08:00 — Nia now holds a grudge against Bram.
```
The chain runs: famine (Economy) → sale (Economy) → winter and the sale leave the stores short → hunger (Needs) → blame (Social) → went without (Needs) → grudge (Social). Bram sold food to starving strangers at a fair price, and the winter took the rest.

## 2. Goals, non-goals, anti-goals

**Goals**
1. Every thing does something the sim models; some change the story.
2. Every thing keeps its past in state, told in plain words by the Chronicle, the inspector and the text-state API.
3. The market runs as its own machine and reaches the camp through people (the peddler, thieves, the trader) and the stores.
4. Delegation only (fixed decision 2): companions pick up, claim, give, sell, buy and refuse.
5. Every draw names its stream; a rewind restores the economy and lets it run again; all content lives in `/content`.

**Non-goals for slice 1:** new or bought food and making (slice 3); ventures and raids (slice 2); caravans, wars, plague and minted coin (world layer); storage buildings (Build slice 2).

**Anti-goals (never)**
- **A stat treadmill.** No grinding, no damage numbers, no "+3% work speed" lines. Qualities are words ("keen", "keeps you dry").
- **A spreadsheet.** No inventory screen, no stash, no price ticker. Prices appear only in the peddler's panel while he is in camp.
- **Direct control.** The game never says "give Mira the knife" or "sell the axe".
- **Money that exists only to be spent.**
- **Production chains, crafting stations, or a quality that names no story.**

## 3. The ideas in plain words

| Word | Meaning |
|---|---|
| goods; a thing | Goods are counted in heaps and stack as `ItemStack` does (berries, timber, silver, salt, salve). A thing is one object with an owner, a holder and a past: a knife, a cloak, a ring (`ItemInstance`, new). |
| purse | The goods a person owns, carried on them: money and remedies (5.4). |
| kit | What a person uses: one thing in hand (a tool or weapon), one on the body (a cloak), one at the belt (a lantern) and one keepsake. Only kit takes effect. |
| quality | A rolled property, such as "keen" (strikes harder). Path of Exile's affix. |
| rarity | *Plain* (no quality), *fine* (one or two), *rare* (three or four, and a name) or *storied* (an authored relic with a power and a past). |
| depth | How deep the source lay. A thief's pack is 1; the barrow's third level is 4. Deeper finds roll rarer things and stronger qualities, as Path of Exile's item level does. |
| reckoning good | What this world's markets count value in: silver or salt, a Truth. |
| shock and word | A market event (a failed harvest, a timber glut, a mine collapse, bandits on the road), and the news of it, which reaches the camp only with a visitor. |
| the peddler and the trader | A recurring trader with a cart, whose name and temperament are Truths, and the companion who ranks Trade highest and deals with him. |

## 4. How the player uses it

Nothing new stays on screen (ADR-0008). Things appear on the map, Orders gains rows, Work gains a column, and the peddler's panel opens only when you click him.

**On the map,** things lie on tiles: a dropped pack is a small bundle, and a blade, a ring and a chest each have their own small shape ("The thief's pack: a knife, 4 silver" on hover). Kit is drawn on people: ADR-0019's `HeldTool` reads the thing in hand, falling back to today's tools; a cloak shows over the cloth; a carried lantern lights a small circle at night (view only). The peddler stands at the meeting place with his handcart for a few daylight hours.

```
 . . . , , , , , . . . . . .    H hearth      , meeting place
 . . , , [H] , , . . . . . .    O Odo         c his cart
 . . , , O c , , . . B . . .    B Bram, walking over to trade
 . # # # . . . . . . . x . .    # stockpile   x a pack a thief dropped
```

**Orders** gains three rows and a wish list under the ration policy. Each choice becomes a tick-stamped command, and stays a draft while the game is paused.
```
┌ Orders ───────────────────────────────────────────────────────┐
│ When food runs short  (•) the hungriest first  ( ) promised … │
│ Loot                  (•) finders keep it   ( ) to the stores │
│ When a trader comes   ( ) sell nothing  (•) sell what we can  │
│                       spare  ( ) leave it to the trader       │
│ Food                  ( ) never sell  (•) keep two days' food │
│                       ( ) sell freely                         │
│ We want               axe ×   cloak ×   + add                 │
└───────────────────────────────────────────────────────────────┘
```

**Work** gains a **Trade** column ("Dealing with traders"). Slice 2 adds **Venture** ("Going into the barrow"), where 0 means never.

**The peddler's panel**, only while he is in camp:
```
┌ Odo the peddler · drives a hard bargain · leaves at 16:00 ─────┐
│ Sells                         │ Buys                           │
│ a steady axe     58 silver    │ timber       5 silver          │
│ a cloak          45 silver    │ berries     19 silver · v.dear │
│ a lantern        52 silver    │ the camp's club  6 silver      │
│ Word from Greyford: the harvest has failed there.              │
│ Bram means to sell 5 berries and buy the cloak.        [want]  │
└────────────────────────────────────────────────────────────────┘
```
Prices are in the reckoning good, with a word where one stands out. *Want* adds a wish (`SetTradeWishes`); the trader may still say no, and the Chronicle says why. When you are the trader, the line reads "You mean to …" (5.8). When Odo leaves, no price remains on screen.

**The inspector** gains a Kit line, which also shows the purse when it holds anything. Hovering a thing there, or on its tile, shows its card:
```
Kit   In hand: the camp's knife (keen)    Body: a cloak (Bram's own)
      ┌ the camp's knife ─────────────────────────────────┐
      │ Keen: strikes harder.                              │
      │ Dropped by a thief Hollis drove off on day 12.     │
      │ Hollis carried it on the watch until day 20.       │
      │ It carries Hollis's grudge against Nia.            │
      └────────────────────────────────────────────────────┘
```

**The Chronicle** carries finds, gifts, sales, refusals, word, and anyone who went their own way under an order. A price moves only as word ("food is very dear in Greyford"), never as a number.

## 5. What changes in the simulation

### 5.1 State
The economy's own records live in one new property, `World.Economy` (an `EconomyBook`, like the culture and world books), and take ids from its own `NextId`, so no character's id shifts (world design §4.3; ADR-0021). Purses and visits, below, live elsewhere.

| Record | Fields |
|---|---|
| `EconomyBook` | `NextId`, `Instances`, `Markets`, `Peddler`, `FoodDrains`, `ReckoningGood`, `Wishes` |
| `ItemInstance` | `Id`, `DefId` (base), `Rarity` (Plain, Fine, Rare, Storied), `RelicId`, `Depth`, `Qualities` [{`QualityId`, `Value`}], `NameKeys` (a rare thing's two words); `OwnerId` (0 = the camp), `Place` (Ground, Stores, Carried, Satchel, Cart, Market, Gone), `HolderId`, `Pos`, `Slot` (None, Hand, Body, Belt, Keepsake); `History` [{`Kind`, `CharacterId`, `EventId`, `Tick`}] (at most 12, older entries folded into a count), `Echo` {holder, target, event} (5.5), `KnownEvent` (when a relic's power became known), `SeekerId` |
| `Market` | `Id`, `SiteId` (0 for the stand-in), `TemplateId`, `NameKey`, `Fragilities`, `Goods` [{`ItemId`, `Base`, `Target`, `Stock`, `Price`}], `Shocks` [{`ShockId`, `Event`, `EndsTick`, `Heard`}] |
| `PeddlerState` | `CharacterId`, `NextVisitTick`, `Cart` (goods and instance ids), `LastWishes`, `Deal` {`TraderId`, `OwnWay` (none, or the compliance reason), `Event`} |
| `FoodDrain` | `ItemId`, `Count`, `Event`, `Tick`, `ByCharacterId` |
| `Visit` | `CharacterId`, `Event`, `LeavesTick`: one per visitor in camp (Odo, a seeker); W1b adds its envoy fields to it. In `World.Visits` if this design lands before W1b, else in W1b's `RealmBook.Visits`. |

- **Purse:** `Character.Purse` [{`ItemId`, `Count`}], in id order (5.4). Odo's money is his purse.
- **Invariant,** tested hourly: each instance is in exactly one place; a carried one has a holder; no holder has two in one slot; `Gone` ones stay for their history and every reader skips them.
- **Kit cache:** a `[JsonIgnore]` array on `Character`, rebuilt when an instance moves, keeps modifier reads within ADR-0005's budget.
- **Orders** use the player design's `SetStandingOrder(Tick, OrderId, ChoiceId)`: `loot` (`keep`, `stores`), `trade.sell` (`nothing`, `spare`, `trader`), `trade.food` (`never`, `two_days`, `freely`). The choices live in whatever store that command defines (whichever of the player, peoples or this design lands first defines it: {`OrderId`, `ChoiceId`, `Event`} on `World`). Wishes get `SetTradeWishes(Tick, ItemIds)`, at most three. Each order emits one World event, which claims and deals cite.
- **Enums, appended:** `SystemTag.Economy`; `JobKind.PickUp`, `Trade`, `Give`; `DebtKind.Credit`; `CharStatus.Visitor` (in camp: never fed from the stores, never a guard's target, chats like anyone). Between visits Odo is the world design's `CharStatus.Abroad`, and visitors walk in with its `JobKind.Visit`. The world design names `Visitor` and `Visit` as this design's, and builds `Abroad` in W1a and `JobKind.Visit` in W1b; whichever design lands first adds each of the four.
- **`EventArg.Thing(name, id)`,** prefix `t:`, resolves at display time to the thing's name from its stored keys (`EventArg` has only `c:`, `i:`, `k:`, `n:` today). `World.SchemaVersion` goes up by one.

### 5.2 Kit, and what things do
When a job starts, people take the best thing they own for it, else a free camp thing from the stores (claimed like a bed, until they put it down; fetching it is a detour stage, as `RepairRoof` fetches timber), else nothing. Nobody needs a thing to work: without one they use the camp's old tools, so M0's balance holds where nothing is found.

**Trait modifiers.** A quality may name any `traits.json` field that sums or is a flag (`insultPermilleAdd`, `careUrgency`, `fightsThieves`, …), with its meaning there; `Traits.Sum`, `Any` and `ScalePct` read the kit after the traits. A thing is then a trait that can be put down, sold or stolen, and peoples still differ only in needs (fixed decision 5). Fields read with `Max` (`fleeHealthBelow` and the like) are closed to things: a thing adds behaviour and never cancels a trait's.

**Thing modifiers,** a closed set, each read at one place:

| Modifier | Read in | Effect |
|---|---|---|
| `workDurationPct`, `workAccidentPermille` (per work) | `JobRunner` | faster work; fewer accidents (or more) |
| `combatDamageAdd`, `combatHitPermille` | `CombatSystem.Exchange`, both sides | harder or surer blows |
| `nightSightAdd` | `CombatSystem.Spot` | the watch sees thieves farther off at night |
| `rainShelter` | `IncidentSystem.RainUpkeep` | no `soaked` thought and no rest lost |
| `sleepRestorePct` | `NeedsSystem` | rest returns faster |
| `careHealAdd` | `CareJobs` | tending heals more |
| `chatWarmthAdd` | `SocialSystem` | talk warms faster |

Thieves and raiders use kit too, so loot shows as a threat: "a thief crept toward the camp in the dark, a knife at its belt."

### 5.3 Generating things
Every roll is Dice on the Economy stream, in Path of Exile's order:
1. **Rarity** from the source's weights at its depth (`loot.json`; at depth 1: plain 70, fine 24, rare 5, storied 1).
2. **Base** from the source's weighted list.
3. **Qualities:** fine one or two, rare three or four, no repeats, among those whose `tags` the base carries, each at the best tier its depth allows (`minDepth`).
4. **A rare thing's name:** two words from loc lists ("Ash" + "bite"), stored as keys.
5. **Storied:** a relic from `relics.json` by weight, allowed at that depth and not yet in this world. Each relic exists once per world; its past is the point.
6. **Value** = base value × (100 + 40 per quality) / 100, tripled if storied.

**Slice 1 content** (every number is tuning, measured by soak):

| Base (kind, slot) | Value | What it does |
|---|---|---|
| knife, club (weapon, hand) | 30, 10 | fights; a knife remembers (5.5) |
| axe (tool, hand) | 45 | fells 20% faster, so the forest goes sooner and dryads grieve |
| cloak (wear, body) | 35 | `rainShelter`; envied in the rain |
| lantern (light, belt) | 40 | `nightSightAdd` 3 on the watch; a thief's prize |
| salve (remedy, goods) | 12 | care uses one and heals 150 more |
| ring, locket, trinket (keepsake) | 20–30 | gifts that settle debts; heirlooms |
| silver, salt (money, goods) | 1 as the reckoning good | gifts, savings |

| Quality | On | Modifier, tiers I / II / III | Story it is for |
|---|---|---|---|
| keen | weapon | `combatDamageAdd` 20 / 35 / 50 | the blade that cut deeper |
| steady | tool | `workAccidentPermille` −8 / −12 / −16 | the axe that never slipped |
| swift | tool | `workDurationPct` −15 / −25 / −35 | the forest gone by autumn |
| oiled | wear | `rainShelter`; `sleepRestorePct` +10 / +15 / +20 | dry while others shiver |
| bright | light | `nightSightAdd` +1 / +2 / +3 | the thief seen at the treeline |
| of the watch | weapon, light | `fightsThieves` | the cautious one who now runs at thieves |
| of patience | keepsake, weapon | `insultPermilleAdd` −10 / −15 / −20 | the ring that kept the peace |
| of wrath | weapon | `insultPermilleAdd` +15 / +20 / +25 | the quarrelsome blade |
| of kinship | keepsake | `chatWarmthAdd` +25 / +40 / +50 | the newcomer everyone warmed to |
| of mercy | keepsake, remedy | `careUrgency` +1 / +2 / +2 | the one who tends the hurt first |

Slice 1 authors three relics: **the black ring** (relic kind `cursed`), **the Watchman's lantern** (bright III, of the watch) and **Greyhook**, a knife (keen III, of wrath). Heirlooms are generated (5.5).

### 5.4 Where loot comes from and who gets it

| Source | When | Depth | Slice |
|---|---|---|---|
| A thief's pack | rolled when the thief appears (`SendThief`); dropped if it flees wounded (238 times in 100 world-years today) or dies (about 5); 60% goods (2–6 silver, 1–3 berries), 40% one thing | 1 | 1a |
| The river | "A chest washed up on the riverbank" (`river_find`, 15‰ a day) | 1 | 1a |
| The covered ruins | one thing or none, which and where a Truth; found by someone sheltering or working there (Dice per hour; vampires shelter there by day) | 2 | 1a |
| The dead | keep their kit and purse until someone takes them, as a find | — | 1a |
| The peddler | one or two fine or rare curiosities, now and then a relic whose past he does not know | 2 | 1b |
| Raiders | packs like a thief's; a named captain's fine or rare weapon (world design W1b) | 2 | W1b |
| Dungeon levels | each level's hoard, its guardian's kit, a relic at the bottom (a Truth) | level + 1 | 2 |

- **Picking up.** The nearest free member collects a dropped pack (a short `PickUp` job, ranked with hauling and taken first by whoever fought there). Under *finders keep it* the finder owns it: a thing that does not fit their kit goes to the stores, still theirs, and a pack's goods go as below. It is the default (lead's call: both choices ship, and `--loot mixed` soaks both). Under *to the stores* it is camp property, claimed by whoever first starts the work it serves. An event cites the order only when the order decided who got the thing (ADR-0018).
- **Going their own way.** *To the stores* is a rule you set, so ADR-0025's rule decides how far it carries, as it does for zones, the schedule and the peoples design's moon-nights order. At pick-up the finder takes the shared compliance roll once (`Compliance.Defies`, tag `LootKeep`, only when their odds are above 0). Going their own way, they keep the find, its goods included: "Never one to be told, Sera kept the knife she found." (`economy.kept_find_<reason>`, 5.10). Rule-keepers who see it react as to any broken rule (`Compliance.Witness`). *Finders keep it* has no rule to break, so it takes no roll. Your own character never rolls (5.8).
- **Own goods** (lead's call). Money and remedies a person owns ride in their purse. Under *finders keep it* a pack's silver, salt and salve go to the finder's purse. Its berries always go to the stores, so food stays in the stores and under the ration policy through slice 1. A purse pays for gifts (below), and a carer uses their own salve before the camp's. Thieves at the stores and the trader never reach a purse.
- **Envy of kit** (lead's call). Build's envy rule (its 4.9) is about beds and comes in its slice 1c, so this is a rule of its own in the same shape, and it needs nothing from Build. Soaked beside someone dry in a cloak, or hurt beside someone with a steady axe, a person whose opinion of the holder is below 20 gets `envies_kit` about them, at most daily, which an insult can cite.
- **Gifts settle debts.** An Oathbound or Generous debtor, or one whose opinion of the creditor is at least 30, gives a keepsake they do not use, or enough of the reckoning good from their purse, when they talk: `social.gift_settles`, then `Relations.Repay` (DebtRepaid). The starting prices (care 10, meal 6, blood 20, rescue 30) are tuning, which the 1a soak sets against its floor (9.1). This answers ADR-0018's seam that blood debts are seldom repaid (`owes_blood` ends 467 of 847 stories), alongside culture-faith's debt of honour.
- **Other hands.** Leavers take their kit and purse. Anything else they own becomes the camp's, and camp things stay where they stand (`left_behind`). A thief at the stores takes the most valuable thing within reach if it is worth more than the berries it could carry. The trader may sell camp things, even claimed ones ("Bram sold the knife Oren carried on the watch": Oren gets `sold_my_kit`). What you put in the satchel (a player-design gift) is beyond every thief and trader.

### 5.5 Things that change the story
A relic kind is code with its numbers in content. An unknown kind fails at load.

| Kind | Rule | Systems | Slice |
|---|---|---|---|
| **remembers** (every blade) | When a blade changes hands (left behind, taken from the dead, sold, given, stolen), it records its last holder's strongest active grudge (`economy.remembers`, citing the grudge and the hand-over). The record is taken before `Lifecycle.Die` or a departure calls `Relations.DropGrudgesHeldBy`, which only marks grudges inactive, so the grudge's `FormedEvent` can still be cited. Its next holder gets `blade_remembers` about that target: −2 opinion a day while carried, a thought an insult can cite. It fades when their opinion of the target reaches 30 or they put the blade down. | Economy, Mood, Social | 1a |
| **cursed** (the black ring) | The wearer's accident threshold doubles, and they get `ring_unease` (−3 mood). They will not give, sell or put the ring down. After a second accident while it is worn, the curse becomes known ("Word went round that the black ring brings misfortune"), and others refuse it. | Incident, Mood, Social | 1a |
| **heirloom** (30% of keepsakes in packs) | It belongs to a generated, named stranger (`OffMap`, cast key `seeker:<k>` for the k-th in this world, so `Relations.NatureKey` and culture seeding key on it, after the world design's `n:<slot>:…`; their name, people and traits are Truths, 156, with the name taken as in 5.8), who comes asking 2–6 days later (Dice) if it is still in camp. The Generous, Oathbound and Protective give it back (the stranger owes them and may ask to stay, by `Join`'s rules: the first Recruitment of a non-cast person, a precedent the world design's W2 shares); others refuse (a grudge). A refused stranger has an even chance of returning within three nights as a thief aimed at that thing. Sold to the peddler, it is gone. | Incident, Social, Combat | 1b |
| **faith_claim** | A relic a faith wants back: its faithful want it (`wants.json`'s item target), giving it pleases their gods, selling it angers them, and their temple sends a seeker. | Culture, Social, Growth | after culture-faith |
| **token** | A people's pledge, such as a vellet's skin: only ever given, as a pledge to stay. Taking one is theft and a grudge, and holding one never gives control (peoples question 6, accepted in ADR-0025). | Social, Needs | peoples batch 3 |

### 5.6 Money
Path of Exile's money is its crafting orbs, which players use up, so demand never stops. Return keeps the principle and drops the orbs.
- **Every good has a use.** Berries are eaten, timber built with and burned, salve spent on the wounded. Silver and salt are gifts and settle debts in slice 1; in slice 3 silver inlays qualities and wards against shapeshifters, and salt keeps food and wards off the restless dead.
- **The reckoning good is a Truth per world:** prices are integers in silver or in salt. A player who has died knows that here, salt is money.
- **No coin until a civilization mints one** (world layer); a coin is then one more good, worth what its minter's fortunes make it.

### 5.7 The market
**One market in slice 1:** Greyford, a stand-in for the nearest settlement (section 8), seeded from `markets.json` by Truths: template, culture, fragilities, and a −20% to +20% bias on each good's `Base` (the item's value); its name comes as in 5.8. **Each dawn, for each good,** in integers:
1. `Stock += produce[season] × p% − consume[season] × c%` (running shocks set p and c; never below 0);
2. `want = Base × Target / max(Stock, Target / 4)`, from `Base / 4` to `4 × Base`;
3. `Price` moves a quarter of the way to `want`, drifts −3% to +3% (Dice), and stays within `[Base / 4, 4 × Base]`.

**Shocks** (`shocks.json`; kinds supply, demand, road): each dawn, every idle shock rolls its `dailyPermille` in its seasons (`fragilePermille` in a market with that fragility), runs 5–15 days and emits a market event; the camp hears of it only through word (5.8). **The economy is its own machine,** in the founder's words (the ask above), and it reaches the camp as the main principle's butterfly effects: the storyteller never fires a shock, only reacts to one (thieves in a famine). Camp incidents stay the director's.

| Shock | In town | Reaches the camp as |
|---|---|---|
| harvest_failed | berries produce 0% | food very dear; thieves ×2; Odo pays dear for berries |
| timber_glut | timber produce 300% | timber cheap; Odo buys little |
| mine_collapse | tools, weapons and lanterns produce 0% | none on the cart; the camp's axe worth three times as much |
| bandits_on_road | prices +50% | Odo does not come; thieves ×1.5 |

Plague, war blockades and levies come with the world layer.

### 5.8 The peddler and trading
**Who he is** (Truths): name, people and temperament; his culture comes from culture-faith's `Ways.Assign`, keyed by his cast key. Honest, he sells at 120% of the town price and buys at 80%; shrewd, 135/70; grasping, 150/60. Odo is the same man in every timeline: a `Character` with cast key `peddler:0` (so his chemistry with each trader is a Truth), `Abroad` between visits and a `Visitor` in camp. When he comes is Dice.

**Names** (lead's call). ADR-0025 asks for names from the world's invented cultures, so this design authors no name lists to re-key later. Odo's and each seeker's name comes from their culture's `people` list in the world design's `names.json`, and the stand-in market's from its culture's `places` list. Whichever of W1 and slice 1b lands first adds that file and its schema, as the world design defines them.

**Visits** (`peddler_visits`): every 8–12 days, never while bandits hold the road; he arrives at a map edge between 10:00 and 13:00 and stays five hours at the meeting place (by the hearth if the camp has none). His cart is goods and things bought from Greyford's stock (Dice), leaning toward what is cheap there and what the camp last wished for. He chats like anyone, so opinions of him form. He brings word of every shock begun since his last visit (`economy.word`, citing the shock).

**The deal.** The free member who ranks Trade highest (not asleep, downed or on watch; ties by id) walks over. Everything follows from state, apart from one compliance roll when they set off:
1. **What may go:** *sell nothing*; *sell what we can spare* (timber beyond open plans, roof repairs and any djinn bargain's reserve; berries beyond the food order; unclaimed camp things); *leave it to the trader* (anything the camp owns but the djinn's reserve, judged by the trader's nature).
2. **What to get:** the wishes in order, then what the trader thinks the camp lacks (after a thief, a Protective trader wants a lantern or knife; anyone soaked this week, a cloak), bought for the camp. No food is sold to the camp in slice 1.
3. **Price:** the town price × his temperament %, less 1% per 10 points of his opinion of the trader and 3% per point of chemistry. An *Anxious* trader pays 10% more.
4. **Natures inside the orders,** rules over state like the schedule's bends: *Practical* never sells below two days' food; *Proud* refuses any price above 140% of the town price; *Oathbound* pays any credit first. *Generous*, in a famine, sells food beyond the food order (never under *sell nothing*) "for the hungry of Greyford", citing the word: a reason of help, as Zones excuses helpers.
5. **Their own way** (ADR-0025 #2). The orders and wishes are rules you set, so the trader takes the shared compliance roll once when they set off (`Compliance.Defies`, tag `TradeOwnWay`, only when their odds are above 0). The result is stored in `Deal.OwnWay`, so a deal restored mid-way ends the same. Going their own way, they deal as if you had left it all to them: both orders, the wishes and the djinn's reserve. One whose reason is dislike also buys for themself first, with camp goods ("Sera bought herself a cloak with the camp's timber"). The Chronicle gives the reason once (`economy.own_way_<reason>`), and rule-keepers within 8 tiles react as to any broken rule. A trader's own hunger reaches a deal only this way, through the roll's black-mood term.
6. **Credit:** a wish beyond the trader's means, when Odo's opinion of them is at least 20, goes on credit until his next visit (`DebtKind.Credit`, a promise with a due tick). Unpaid then, it breaks: DebtBroken, his grudge, his worst price ever after. Bandits on the road can make the camp break its word through no fault of its own.
7. **Execution:** one event per good or thing (`sold`, `bought`, `refused_price`, `sold_beyond_order`), citing the order where it decided, the word, and the own-way event. Greyford's stock moves too; the trader gets `good_bargain` or `paid_dear`.

**When you are the trader or the finder** (ADR-0025 #2; player design §4.2 rule 8 and §4.5; the detail is the lead's call). You never take the compliance roll, and no nature overrides an order or a wish you set: a Proud you still buys the axe you wished for, at any price you can pay. Your nature decides only what you left open: what goes under *leave it to the trader*, what to buy beyond your wishes, and whether a Practical you keeps two days' food under *sell freely*. Prices still follow chemistry, Odo's opinion of you and the Anxious rule. So the deal has no separate path for you, only those two skips.

**Stakes.** Selling timber someone has a stake in (an open roof promise, a bed plan for someone without a bed) emits Build's `used_last_materials` with the sale as the use, and those passed over get `passed_over` (Build §4.7). Whichever of this design and Build slice 1a lands second writes this (9.0). It never bites under *sell what we can spare*, which keeps back open plans and roof repairs.

### 5.9 How the market reaches the camp

| From | To | How | Cited |
|---|---|---|---|
| famine, bandits | night thieves | the thief roll's threshold × the shock's `thiefPct` | the shock, only if the roll fell in the band it added. `Chance` returns only a bool, so the thief roll (`Director`) and the accident roll (`JobRunner`) are read with `Range(0, 1000)` to make the band test exact. |
| a food sale | hunger | the shortage memory, below | the sale |
| a timber sale | a broken roof promise; felling | Build's stake rule; dryad fellings cite why the timber was wanted (ADR-0022) | the sale |
| selling a djinn's price goods | a bargain broken at dawn | the reserve is spoken for; only a trader going their own way sells it | the sale |
| the black ring | accidents | the threshold doubles | the curse, only inside its band |
| a mine collapse | temptation | the axe's price triples | the word |

**The shortage memory.** `FoodDrains` keeps each event that took food from the stores (a sale, a theft, spoilage) for that food's `spoilDays` (5 for berries): food sold six days ago would have rotted anyway. Today eating clears `LastFoodEvent`, so no drain ever reaches a winter hunger. Now, when someone grows hungry with no more recent cause, `NeedsSystem.UpdateHunger` cites winter (as today) and the latest drain, but only if the drain bit: they hold no reservation and `Rations.Unreserved(food)` is 0 (ADR-0018 #1's counterfactual rule). If that drain was a sale, they blame the seller (`social.blames_seller`, a thought worth −15), and going without while blaming forms a grudge. This also closes ADR-0018's seam that thefts and spoilage never appear in winter-hunger stories.

### 5.10 Events and endings

| Events | System | Cause | Ending |
|---|---|---|---|
| `incident.night_thief_armed`, `peddler_arrives`, `river_find`, `seeker_arrives`, `seeker_returns` | Incident | —; the find; the refusal | — |
| `economy.dropped`, `found`, `took`, `stored`, `claimed`, `left_behind`, `remembers`, `curse_known`, `word` | Economy | the flight, death or find (and the order, when it decided); the grudge and hand-over; the second accident; the shock | — |
| `economy.kept_find_<reason>`, `own_way_<reason>` (reasons `own_way`, `dislike`, `mood`, as `Compliance` names them) | Economy | the order or orders set aside; for dislike, a grudge against you, else their worst thought about you; for mood, their worst thought (as Zones cites) | — (rule-keepers nearby get `saw_rule_broken`) |
| `economy.sold`, `bought`, `refused_price`, `sold_beyond_order` | Economy | the order, the word, the own-way event | — |
| `social.gift_settles`; `credit`, `credit_paid`, `credit_broken` | Social | the debt; the deal | DebtRepaid; DebtFormed, DebtRepaid, DebtBroken + GrudgeFormed |
| `social.blames_seller`; `heirloom_returned`, `heirloom_refused` | Social | the hungry event; the ask | —; DebtFormed (+ Recruitment if they stay), GrudgeFormed |

No new `EffectKind`; text has `_you_` variants (ADR-0017).

**Systems touched.** Every mechanic reaches at least two (fixed decision 1): Work (tools, the Trade job), Combat (weapons, armed thieves, lanterns, drops), Needs (cloak, salve, shortage memory), Social (blame, envy, a blade's memory, gifts, credit, heirlooms, going their own way; opinions set prices), Incident (shock-raised thieves, the peddler, seekers, finds, curses) and Mood (the new thoughts).

### 5.11 Later: ventures and dungeons (slice 2)
- **Dungeons are placed by this design** (world design §4.2). The first lies under the camp's covered ruins, behind a stair; with the world layer, ruins and lairs hold others. Depth (3–6 levels), each level's guardian and the relic at the bottom are Truths, so a player who dies can come back for it. Guardians come from a boss list in `dungeons.json` reusing the peoples' threat forms (the barrow lord; the art's Barrow King, drawn and unused), each 60% stronger than the one above.
- **A venture is volunteers.** Clicking the stair opens a card: how deep (the next level, or as deep as they dare) and when to turn back (at the first wound, when someone falls, never). Members with a Venture priority above 0, healthy and fed, decide by nature (the Anxious stay unless someone they love goes; the Proud and those who want respect go first; the Protective follow friends), and two to four go. Your character can always go, without a roll (player design, "What you control of yourself"), and a death below rewinds like any other. The party leaves the map at the stair (`OffMap`), or travels to a far dungeon as the world design's expedition `Band`.
- **Below,** each level plays out in rounds under the combat rules (hits, damage, kit, flight, rescue, abandonment), with events as they happen: "In the barrow's second level, Mira fought the Barrow Wight." The party turns back by its orders and state; cleared levels stay cleared; the weapon that kills a guardian records it and becomes storied.
- **Raids** use the same machinery: clearing the bandits ends `bandits_on_road`, an economic problem answered by force.

### 5.12 Later: staples and making (slice 3, the food milestone)
- **Staples** get their full uses: tools wear and are mended with iron, cloaks are made from cloth, salt keeps food, silver inlays and wards.
- **Making is people, not stations,** each kind one of Path of Exile's orbs: a smith reforges a tool's qualities with iron (chaos); silver inlays one more quality (exalted); a djinn works a thing for a bargain, unpredictably and for good (Vaal), which feeds its need; a blessing at a shrine rerolls a quality's value (divine).
- **Food** that keeps, bought food and growing plots come here, under Zones §3.6's guard.

## 6. Content and schema changes

| File | Change |
|---|---|
| `items.json` and schema | New fields: `kind` (food, material, money, remedy, tool, weapon, wear, light, keepsake), `slot`, `value`, `work` (tools), `remembers` (blades), `reckoning`, `qualityTags`, `modifiers` (implicit ones, such as the axe's speed). New entries are listed in 5.3. Existing fields keep their meanings. |
| `qualities.json` (new) | `id`, `place` (prefix or suffix), `wordKey`, `textKey`, `tags`, `tiers` [{`minDepth`, `min`, `max`, `weight`}], `modifier` (from 5.2's closed set), `work` |
| `relics.json` (new) | `id`, `nameKey`, `loreKey`, `base`, `qualities` [{`id`, `value`}], `kind`, `params`, `minDepth`, `weight`, `cache` ("ruins" or none) |
| `loot.json` (new) | for each source: `depth`, `packPermille`, `rarityWeights`, `bases`, `goods` [{`item`, `min`, `max`, `permille`}], `heirloomPermille` |
| `markets.json` (new) | for each template: `goods` [{`item`, `target`, `produce` and `consume` by season}], `fragilities`, `peddler` {`temperaments`, `cart`}; no names (5.8) |
| `shocks.json` (new) | `id`, `kind`, `goods`, `pct`, `days`, `seasons`, `dailyPermille`, `fragilePermille`, `thiefPct`, `wordKey` |
| `names.json` and schema | the world design's, added here only if this design lands first (5.8) |
| `incidents.json` | Entries only: `peddler_visits` (days between visits, arrival hours, hours stayed), `river_find`, `seeker_arrives`. Their behaviours go in `IncidentSystem.Fire` and their params in `RequiredContent`. |
| `work.json`, `roles.json` | `trade`; in slice 2, `venture` |
| `thoughts.json` | `blade_remembers`, `ring_unease`, `envies_kit`, `blames_seller`, `sold_my_kit`, `good_bargain`, `paid_dear`, `gift_received`, `heirloom_returned` (`saw_rule_broken` is Zones') |
| `loc/en.economy.json` (new) | every new string |
| Untouched | `species`, `cast`, `traits`, `world`, `wants` |

**Code that names content:** `RequiredContent` gains silver, salt, salve and the closed sets (modifiers, relic kinds, shock kinds, loot sources). `StateApi.Faction`'s hard-coded berries and timber gain a full goods list beside them. With fifteen item ids, the Zones list ticks them by category (food, materials, things, from `kind`); `Zone.Items` and Zones' guard stay per id, as Zones built them (lead's call). Tuning not in content (deal percentages, blame, envy, find odds) stays in named constants (ADR-0013).

## 7. Determinism, snapshots, text-state API, localization, mod hooks

**Draw ids (the lead's call).** TruthIds 150–159 and DiceIds 350–369, past every claim (world 120–149 and 300–339; player 80–89 and 220–239). An early draft's 100–109 and 240–259 are withdrawn, though the world and player designs' tables still show them. The founder's allocation wins if it differs; nothing has merged.

| Draw | Source | Id |
|---|---|---|
| the reckoning good; a market's template, culture, name, fragilities, price bias (index names the fact) | Truth | 150 `Reckoning`, 151 `MarketNature` |
| the peddler's name, temperament, people | Truth | 152 `PeddlerNature` |
| the ruins' relic (or none) and tile; a seeded relic's past | Truth | 153 `RuinsCache`, 154 `RelicPast` |
| a dungeon's depth, guardians, bottom relic (slice 2) | Truth | 155 `DungeonNature` |
| a seeker's name, people and traits, keyed by `seeker:<k>` (`WorldGen.NatureOf` reads cast.json, so a non-cast key needs its own draw) | Truth | 156 `SeekerNature` |
| packs, rarity, base, qualities, values, names, heirlooms; river and ruins finds | Dice, Economy | 350–358 |
| price drift, shocks, their length; the peddler's day and wares; a seeker's day and return | Dice, Economy | 359–365 |
| a finder under *to the stores*; the trader, once per deal; each only when the odds are above 0 | Dice, through `Compliance.Defies`, on its stream | 366 `LootKeep`, 367 `TradeOwnWay` |
| the rest of every claim and deal, gifts, a blade's memory, envy, answers to seekers | no draw: rules over state | — |
| cursed accidents, shock-raised thieves | existing rolls, thresholds changed | — |

Ventures (slice 2) take their ids with their own sign-off.

- **One new substream:** `DiceSystem.Economy` takes the next contiguous value at merge (schedule §6.1). Market drift draws every dawn; on the Incident stream it would move every thief. Economy code draws only from it. The two compliance rolls are the one exception: they go through the shared helper and draw where it draws (Schedule §6.1: the Work stream), so each shifts the later draws on that stream, a cost the schedule accepts for its own rolls.
- **No existing stream moves until behaviour does:** slice 1a's first step, with empty loot tables, leaves all 100 soak logs byte-identical (ADR-0022's test). With no finds and no peddler, no compliance roll is taken.
- Integers only; lists in id order; no dictionary decides anything; the market and the peddler are seeded at the end of `WorldGen.Create`, without events.

**Snapshots and rewinds.** All of it is plain serialised state (a few dozen instances, the purses, one market); restore stays far under a second at 200 characters. A rewind restores the economy as it stood at the fate knot (stock, prices, running shocks, the peddler's next visit, every thing and its past, every purse, the satchel's contents too, as the player design's "What you keep and what goes back" says), and reseeded Dice then re-roll shocks, visits and finds: "it is its own machine". Truths survive: Greyford and its fragilities, the reckoning good, Odo's temperament, what lies in the ruins, each dungeon's depth and guardians. A player who has died knows that Greyford's harvest is fragile, Odo is grasping and a black ring lies in the ruins. The Chronicle of Lost Futures can cite things: "In a future that never came, Oren carried Hollis's knife, and Hollis's grudge."

**Text-state API.**
- `StateApi.Item` and `Items` return `ItemState`: name, rarity word, qualities in plain words, owner, holder, place, history lines, notes ("carries Hollis's grudge against Nia") and value.
- `CharacterState.Kit` and `.Purse` are appended; `LocationState.Contents` lists things; `FactionState.Goods` lists every good.
- `StateApi.Market` returns prices with words, and shocks with `Heard`. The view shows only what the camp has heard; tools see everything.
- `StateApi.Visit` returns the peddler's wares, his prices for the trader, the trader's intended deal and when he leaves. The deal is read from state and `Deal.OwnWay`; until the trader sets off, it is the deal under your orders, since a read never pre-rolls compliance (schedule §6.3).
- The story log lists notable things at the end; the soak summary gains section 9's counts.

**Localization.** No English in code (ADR-0007). A thing's name is composed at display time from its stored keys by loc templates, so each language orders its own words: `item.name.fine` ("{prefix} {base} {suffix}"), `item.name.rare` ("{first}{second}, {base_a}"), `item.name.owned` ("{owner}'s {thing}"), a relic's `relic.<id>.name`. This is fixed decision 10's rule. New families: `item.*`, `quality.*`, `relic.*`, `names.item.*`, `market.*`, `shock.*`, `history.*`, `event.economy.*` (with `_you_` variants), `ui.orders.*`, `ui.trade.*`. `ContentDb` checks every key at load, arrays included.

**Mod hooks.** Every new file merges by id, so bases, qualities, relics, loot sources, markets and shocks are JSON alone. A new modifier, relic kind, shock kind or loot source needs code, and an unknown one fails at load (ADR-0018). Economy events reach `IModHook.OnEvent` like all events.

## 8. Fit with the other work

| Work | Fit |
|---|---|
| **World design** | It owns settlements, roads, realms, war and domination, and leaves dungeons to this design; this doc owns their markets. Until its W2 builds markets in settlements (its 8.3), the one market is the stand-in, with its own stock. If W1 has landed, the stand-in takes the nearest market settlement's `SiteId`, name and culture, so the panel and the world map agree; whichever of W1 and slice 1b lands second writes this (lead's call). With W2, every market settlement gets a `Market` from its `sites.json` template, the nearest sends the peddler, Greyford's Truths re-key under that site, and a market's food stock becomes the site's `Food` (days of supply), with the world's harvests in place of `produce`, so a famine is one number in both designs. Word becomes one kind of world news (with a visitor or after the world's delay, whichever is first). The two designs share `Visitor`, `Visit`, `Abroad` and `JobKind.Visit` (5.1) and its `names.json` (5.8); raiders carry packs; tribute and plunder move goods once goods exist (its W2). |
| **Player design** | Your character goes on ventures without a roll, and never rolls compliance against your own orders (its 4.2 rule 8 and 4.5; 5.8 here). Its standing orders carry the loot and trade orders. The satchel keeps things beyond thieves and traders. Merchant's eye reads the town price as the fair price (`StateApi.Market`) and waits for slice 1b. Things go back on a return like the rest of the world (its "What you keep and what goes back"). |
| **Build, Zones, Schedule** | Build's `materials` and `tags` (iron, stone) become priced goods; the stake rule (5.8) is Build's, and the later of this design and Build slice 1a writes a sale into it; its slice 2 Store keeps goods dry (not designed here). Zones' stockpiles keep things, ticked by category, and a thief takes from the pile it reaches; the meeting place hosts the cart. Schedule's `Compliance` helper decides how far the loot and trade orders carry. The peddler comes by day, and the trader must be awake and off watch. |
| **Peoples** | Djinn bargain prices are goods and their reserve is spoken for; a djinn's work on a thing is slice 3's gamble. Felling for a sale grieves dryads. A fae trader cannot lie, so never haggles a price down, and Odo trusts them (+10 opinion). Batch 3's tokens are a relic kind, only ever given (5.5); dwarf crafting is slice 3's making. |
| **Culture and faith** | Guest-right extends to visitors; food taboos (F5) wait for the food milestone; relics a faith wants back are `faith_claim`; selling food in a famine and returning an heirloom are deeds gods can care about; sacred trees are never felled for a sale (P1). Its cultures name Odo, the seekers and the market (5.8). |
| **Growth** | Item wants use `wants.json`'s existing item target ("to own something fine", "to get back what was taken" are M1 entries); being cheated, a good bargain and the barrow's terror are experiences for M1's trait changes. |
| **Domination** | The world design's win is every realm held by you or sworn to you. The economy is a means: a realm short of food or iron that buys from your markets weighs that in an audience (from W2), and your village grows into a market of its own, run through a steward in the court (fixed decision 2), never a trade screen. |

## 9. Slices and acceptance

### 9.0 Where slice 1 lands (lead's call)
- **Base:** the milestone-0 tip once this design is signed and schedule slice 1 and zones steps 1 and 2 have merged. By schedule §7.0's order, peoples batch 1 and culture-faith slice 1 are in by then. So the shared files this design touches (`Character`, `World`, `Social`, `Job`, `SimEvent`, `ContentDb`, `RequiredContent`, `DrawIds`, `WorldGen`) have already been rewritten. `Compliance`, the stores refactor, the stockpile filters and the meeting place already exist, and slice 1 builds none of them.
- **Baselines** are measured on that base before step 1 and recorded in the 1a ADR, and every guard below compares with them, not with 8.47 at `7849ab8` (peoples batch 1 measured 12.13 stories per world-year on its branch).
- **Tracks that may land on either side.** Whichever lands second writes the seam between them, to the terms here (schedule §7's rule), and a shared piece is added by whichever lands first:
  - Build slice 1a: a timber sale as a use in the stake rule (5.8), written by the later. Envy of kit needs nothing from Build (5.4).
  - The player design's slice 1: `SetStandingOrder`, defined by whichever of it, peoples 2C and this design lands first, and the satchel (5.4).
  - The world design: the stand-in's site, name and culture (section 8), written by the later of W1 and slice 1b; `names.json` (5.8) and `Abroad` (both W1a), and `Visitor`, `Visit` and `JobKind.Visit` (W1b), each added by whichever lands first.
  - Zones step 3 and peoples 2A share nothing with slice 1.
- **1b follows 1a.**

### Slice 1a: things and loot (sim and headless)

**Steps, in order:**
1. Instances, kit, modifiers through `Traits.*`, the Loot order, `EventArg.Thing` and the API. With empty loot tables, all 100 soak logs stay byte-identical to the merge base.
2. Thieves' packs, armed thieves, drops, pick-ups and claims, with the compliance roll under *to the stores*; the river; the ruins.
3. Purses and gifts, envy of kit, leaving and dying with things, and thieves taking things.
4. A blade's memory and the black ring.

**Tests:** same seed and commands, same hash; snapshots with a pack on the ground and with the ring worn restore to the same hash; the instance invariant every hour of two seeds' years; causes (found cites dropped cites fled; a claim cites the order only when it decided; `remembers` cites the grudge; a cursed accident cites the curse only inside its band); going their own way (a finder under *to the stores* keeps the find only through `Compliance.Defies`, the event cites the order and the reason, an Oathbound member within 8 tiles gets `saw_rule_broken`, and your character never rolls); purses (a found pack's silver reaches the finder's purse and its berries the stores; thieves at the stores never take from a purse; a leaver takes theirs); modifiers (`fightsThieves` sends its holder at a thief, a cloak stops `soaked`, a lantern extends sight); content (schemas, kinds, references, loc forms); Economy code draws only from the Economy stream, and compliance only through `Compliance.Defies` (a static scan, as culture-faith's test 28).

**Soak** (100 worlds × 1 year, `--loot keep|stores|mixed`, `mixed` nightly): an item event inside a story chain in at least 40% of worlds; a debt settled by a gift in at least 25% (the gift prices are tuned to it, and a miss is reported, as ADR-0017 does); `owes_blood` endings, `remembers` and black-ring stories, and finds kept against the order by reason, reported. **Guards** against the base's figures (9.0; Zones §3.6's measures): stories per world-year no lower; hunger-chain stories within ±20%; went-without events at least half; meal theft → grudge in at least 50% of worlds.

### Slice 1b: the peddler and the market (sim and headless)

**Steps, in order:**
1. The shortage memory alone, measured against 1a.
2. Visitors; Greyford and its dawn prices; shocks and word.
3. The peddler, the Trade job, the orders and their compliance roll, credit and blame; stakes, if Build slice 1a has landed (9.0).
4. Thieves raised by shocks, and heirloom seekers.

**Tests:** snapshots during a visit and mid-deal restore to the same hash; the same state and the same compliance result give the same deal; a trader going their own way sets the orders and wishes aside, and one whose reason is dislike buys for themself first with camp goods; your character as the trader keeps your orders and wishes and never rolls (a Proud you buys a wish above 140%); over a year prices stay in bounds and stock never goes negative; word cites its shock and arrives only with a visitor; a sale six days before hunger is not cited, nor a sale while spare food remains; a thief cites the famine only inside its band; a refused seeker returns as a thief aimed at that thing; credit breaks when bandits keep Odo away; with Build landed, a timber sale under *leave it to the trader* passes over a roof promise's creditor.

**Soak** (`--trade nothing|spare|trader|mixed`): Odo visits 3–5 times a world-year; a trade event inside a story chain in at least 50% of worlds; famine → thief chains in at least 20% of worlds with a famine; blame → grudge in at least 10% of worlds that sold food within five days of someone's hunger; Economy events in at most 35% of stories (culture-faith's cap), so the economy colours stories without drowning them; deals gone their own way reported by reason; the 1a guards hold.

### Slice 1c: the view
Things on tiles and figures, the lantern's light, the cart, tooltips, the Kit line, the Orders rows, the Trade column, the peddler's panel, word in the Chronicle. **Screenshots:** a dropped pack beside the blood after a night fight; the night watch with a carried lantern; the cart with the panel open; the Kit line with a thing's past; the Orders list.

### Later
- **Slice 2, ventures:** the barrow, volunteers, levels and guardians; bandits on the road as the first raid. Accepted when, in worlds with venture orders, a venture chain ends in a persistent effect in at least 40% and the second guardian falls in fewer worlds than the first.
- **Slice 3:** staples, making and the food milestone, under Zones §3.6's guard.
- **With the world layer:** a market per settlement, caravans, blockades, plague, coin, `faith_claim` relics, the steward.

## 10. Questions for the founder

Most important first, each with my recommendation. The builder builds the recommendations on a branch and merges once question 1 is answered. Two earlier questions are settled in the body: the economy runs as its own machine, in your words (5.7), and finders keep by default, since both choices ship (5.4, lead's call).

1. **Approve the schema and determinism changes:** five content files with schemas and the new `items.json` fields; `SystemTag.Economy`, `DiceSystem.Economy`, `DebtKind.Credit`, `CharStatus.Visitor`, the job kinds and each person's purse; TruthIds 150–159 and DiceIds 350–369, two of them tags for compliance rolls drawn on the shared helper's stream; the snapshot fields; the shortage-memory cause fix; and, where this design lands first, the world design's `names.json`, `Abroad` and `JobKind.Visit`. *Recommendation: approve.*
   *Why it matters:* CLAUDE.md asks before both kinds of change, and every world draws on them from slice 1a.
2. **Is money goods?** Each world counts value in silver or salt (a Truth), every good has a use, and coin comes only when a civilization mints one. *Recommendation: yes.* A gold coin from day one reads more easily, but it is money that exists only to be spent, which is what Path of Exile avoids.
   *Why it matters:* it sets what every price, purse, gift and debt is counted in, in every world.
3. **May things have a will of their own?** Blades remember their last holder's grudge; a ring brings misfortune. *Recommendation: yes, as this world's lore,* kept to blades and a few authored relics and always explained in the Chronicle. Plain things stay plain.
   *Why it matters:* without it, the black ring and every blade's memory go, and with them the first story in section 1.
4. **Food in slice 1:** the trader may sell berries, but nobody sells food to the camp until slice 3. *Recommendation: yes.* It keeps the winter squeeze whole while letting a sale bite.
   *Why it matters:* a food sale can now cause winter hunger in any world that trades.
