# Who you are and how a run works: design

- **Date:** 2026-11-09. **Status:** proposed, for the founder's sign-off.
- **Revised after the builder's questions** on how often you die and on being the djinn or the fae. The djinn and fae are answered in §4.1. How often you die is question 1; until it is answered the builder builds its recommendation (§8.5, §12.1). Calls that don't change what gets built are marked "(lead's call)".
- **Governs:** the main principle's "a member of a random race with one unique ability; when you die you come back to life at a random point in your past", and fixed decisions 3, 4, 8 (fate-knot marking) and 9. Read with fixed decisions 1, 2, 5, 10 and 12.
- **Read with:** CLAUDE.md; BIBLE.md §4–5; ADR-0004, 0006, 0007, 0017, 0018, 0021, 0022 (batch 1, on its branch), 0024 and 0025; the peoples, culture-faith, build, zones, schedule, world and loot-economy designs in this folder.
- **Sign-off approves:**
  - new files `rebirth.json`, `gifts.json` and `loc/en.fate.json`, with schemas;
  - species fields `adultAtYears`, `lifespanYears` and `ageless`, and new thoughts;
  - a `deathKey` on batch 1's `contract` and `courtesy` needs, which only you can die of (§4.1);
  - wounds that bleed until tended, with no new draw (§8.5), if question 1 takes its recommended answer;
  - `SetStandingOrder`, and the shape of `SetPersonalIntent` (built with the world design's W2);
  - TruthIds 80–89, DiceIds 220–239, and Dice streams `Fate` and `Gift`;
  - `SystemTag.Fate`, `SystemTag.Gift`, `EffectKind.GiftGrew`;
  - `World.Fate`, the one block of the world that a return does not restore.

## Summary

1. Your people, your gift and the length of your life are rolled once per world from Truths. Deaths never change them, so across deaths you learn what you are.
2. Your people decides what your first days are about: a vampire needs someone who will let them drink, and a dryad will not fell the trees the roof needs.
3. Your gift is one of 26 isekai abilities defined in content. Each touches at least two systems, has a cost, awakens and grows through events, and is judged by whoever sees it. None is a number, and none takes away anyone's choice.
4. Any death but the end of your lifetime sends the world back to a fate knot the director tied, usually hours or days back, sometimes far. You keep the lost future, your feelings about people, the customs you learned and your gift's know-how. The world keeps nothing, and stories grow in that gap: you are cold to Hollis for a theft he never committed, and he leaves.
5. Every soul gets about one lifetime, whatever its body. Slice 1 builds a rolled people, three gifts, and death that returns you to a knot and writes the lost future into the Chronicle. Today you almost never die on your own, so slice 1 also lets a wound that nobody tends kill (question 1). Distortion comes next.

---

## 1. Intent

The main principle promises a random body, one gift, and death that sends you back. Fixed decision 1 asks what stories each makes.
- **Your people.** Peoples differ in needs, not numbers (fixed decision 5). As the player, your people's need becomes the question your early game turns on. Culture-faith's stances toward peoples apply to you as to anyone.
- **Your gift** is something this world has never seen. People must lean on it, resent it or fear it; otherwise it is only a stat.
- **Return by Death.** You remember and the world does not. Stories come from what you do with what you know, and from what your remembered feelings do on their own.
- **One lifetime.** Without an end, deaths cost nothing and the goal never presses.

Today "you" is cast.json's human otherworlder; Truths, Dice and snapshots exist (ADR-0004), but nothing returns. In the 100-world soak in `out/soak` you are hurt 44 times and fight 40 thieves, yet never die. Rolled as each people on batch 1's branch (at 4db9970: 100 one-year worlds each, no gift, nothing returning), you die on your own in 3 of 600 worlds. All three were undead bound to foraging, and they faded after winter's frost took the berries:

| As | Human | Vampire | Undead | Dryad | Djinn | Fae |
|---|---|---|---|---|---|---|
| Stories | 1,213 | 1,268 | 778 | 1,002 | 1,412 | 1,219 |
| Worlds where you died | 0 | 0 | 3 | 0 | 0 | 0 |

So peoples alone do not make life risky. The loop needs danger (question 1, §8.5), the world's raids later, and a harness that can kill you on demand (§8.3).

Three stories, as the Chronicle prints them. Bracketed lines are lost; they show only in the Lost futures tab.

**Who will let you drink** (Rebirth, Social, Needs, Combat, Fate)
```
Day 1, 00:00 — You died on Earth, and woke here as a vampire.
[Day 2, 19:10 — Oren, hungry themselves, refused to give you blood.]
[Day 2, 21:30 — Mira was in no state to give you blood.]
[Day 4, 02:40 — You were struck down by the thief.]
[Day 4, 07:20 — You died of your wounds.]
Day 1, 20:00 — You came back, remembering how you died.
Day 1, 20:00 — You remembered Mira turning you away, in a future that never came.
Day 2, 19:40 — Oren gave blood to you.
Day 2, 19:40 — You owe Oren for the blood.
```
The analyzer counts two stories here, the lost one and the debt; the player retells one. Whether Oren is hungry is Dice, and Dice re-roll after a return. The death needs §8.5: today a vampire you never dies in a year.

**The theft that never happened** (Fate, Mood, Social)
```
[Day 30, 02:10 — Hollis took the meal set aside for you.]
[Day 34, 03:10 — You died of your wounds.]
Day 21, 08:00 — You remembered Hollis taking your meal, in a future that never came.
Day 22, 13:20 — You insulted Hollis.
Day 24, 18:05 — Hollis now holds a grudge against you.
Day 26, 07:00 — Hollis left the camp.
```
Today's code already makes this a chain: an insult cites the speaker's worst thought about the listener (ADR-0018 #1).

**Fortune's favourite** (Gift, Incident, Work, Combat, Social)
```
Day 6, 10:15 — The axe slipped; the harm meant for you found Mira instead.
Day 6, 10:15 — You found your gift: harm meant for you finds someone near you.
Day 6, 10:50 — Bram tended Mira's wounds.
Day 19, 02:44 — The thief's blow, meant for you, fell on Oren.
Day 21, 18:00 — Mira insulted you.
Day 21, 18:00 — Mira now holds a grudge against you.
```

## 2. Goals, non-goals, anti-goals

**Goals:**
1. Every run feels different in its first hour.
2. Death is a story, not a reload: each return leaves a lost future to read, and feelings that act on the new timeline.
3. There are Truths to learn across deaths, while events re-roll.
4. Every roll gets one lifetime.
5. Runs are deterministic, and can be replayed across timelines.

**Non-goals:**
- The world design's: domination, where you wake, which peoples a world holds, and rivals.
- Choosing your people or your gift: the main principle rolls a random race, and the gift with it. A "chosen rebirth" setting, marked in the Chronicle, can come later (lead's call).
- Saves (the format is given in §10).
- Distortion in slice 1, and decline before multi-year worlds.

**Anti-goals:**
- Save-scumming: no picking a knot, no rewinding at will, no dying on command.
- Stat gifts.
- Gifts that override a refusal, convert someone, or command a companion (fixed decision 2).
- A status screen, a skill tree, or a meter on screen (ADR-0008).
- A return that costs nothing.

## 3. The shape of a run

```
 You die on Earth
      ▼
 Day 1: you wake here, as a vampire, with a gift you have not found yet
      │      (your people, gift and the length of your life are Truths)
      ▼
 ┌─► live: the camp, then the world ── the director ties knots as stories turn
 │    ├── you die ──► a knot is picked ──► the world goes back; you remember
 │    │              (the lost future goes into the Chronicle; Dice re-roll)
 └────┘
      ├── old age, or your soul's span runs out ──► no return: the run ends
      └── the world bends to you (world design) ──► victory
```

## 4. Who you are

### 4.1 Your people

- **The roll.** `Rebirth.PeopleOf` draws your people by weight from `rebirth.json`, with `TruthId.PlayerPeople` keyed by your cast key.
  - `WorldGen.CreateCast` applies it to the otherworlder who starts in camp, before `NewCharacter`.
  - `WorldGen.NatureOf` uses it instead of `cast.Species` for that key, so your natural want matches your people: a vampire can want daylight (ADR-0021).
  - cast.json's `"species": "human"` on `you` stays as the fallback.
- **Who can be rolled.** Only peoples whose needs have shipped (peoples §12.4). Once peoples question 2 settles which peoples a world holds, you are drawn from those present (world design §4.2).
- **Weights.** Human 30, each other people 14. With batch 1 merged, that is human 30% and each other people 14%; before it, human 68% and vampire 32%.

| People | You need | Your first days |
|---|---|---|
| Human | food, rest, company | forage and share; the ration policy decides whether you eat |
| Vampire | blood given with consent; the sun burns | in the ruins by day, asking Mira or Oren at dusk; you live on goodwill, and Ilex and Vale want the same donors |
| Undead | the work you came back for (a Truth) | no food or sleep, but you fade without that work; the living stay uneasy for 12 days (ADR-0022 #3) |
| Dryad | forest, and your own tree (a Truth) | you never fell a tree, so the roof's timber waits on others |
| Djinn | an open bargain | you serve whoever takes your bargain (the watch or foraging) for a price from the stores each dawn; a dawn with nothing to pay sets you against them |
| Fae | courtesy; you cannot lie | every broken promise in camp stings you, your own included; where others would insult, you say the hard truth |

Deaths teach the Truths behind each row: who says no, which work binds you, which tree is yours.

**Edges.**
- **Rations:** "you eat first" skips a player who eats nothing from the stores.
- **Food balance:** ADR-0022 #8 set bushes per berry-eater. A non-eating player eases winter, and a vampire player bleeds donors harder. With an undead or dryad you, the camp makes a third and a sixth fewer stories; with a djinn, a sixth more (§1). Slice 1 reports this per people without retuning.
- **Text:** lines that only named others (thirsting, burning, batch 1's needs) need `_you_a` forms; a load check enforces them. As the djinn, today's logs print "You is restless without a bargain." 1,527 times in 100 worlds.
- **Your bargains.** Your need runs on you as on anyone (ADR-0022 #5; peoples §12.4), so as the djinn you are the one bound, and batch 1's `Bargains` is reused unchanged. You offer; the one you think best of agrees or refuses by their own nature; the camp's stores pay at dawn. A dawn with nothing to pay gives you a grudge against your patron, and wronged and unbound you loose the fire once: your nature acting without your say (§4.5). `Bargains.Offeree` already skips the djinn itself, so its tie-break for the player never comes into play when you are the djinn. Others swearing service to you waits for the world design's oaths (W2).
- **When anyone else would leave.** An unmet need weakens and can kill, as BIBLE 4.4 has it for blood, but you never leave (`SocialSystem.CheckDepartures` skips you). Hunger and thirst keep harming you where anyone else would go, and undead and dryads take harm already, but at critical the bargain and courtesy needs only make people leave. So for you alone, once such a need's leaving clock has run (30 critical hours for a bargain, 24 for courtesy), it harms you while it stays critical. The harm cites what last explained the need, and it can kill you, which returns you. The rate is hunger's (`NeedsSystem.StarvationDamagePerHour`, 10 an hour), so one rate serves every need that can kill you (lead's call). `needs.json` gives `contract` and `courtesy` a `deathKey` ("You burned out, bound to no one." / "You faded, where no word was kept."); the field exists, so the schema is unchanged. A load check requires a `deathKey` on every need that drives people away.

### 4.2 Your gift: the rules

1. **One gift per run**, a Truth (`TruthId.PlayerGift`), drawn by weight from `gifts.json`. A gift's weight may be 0 for a people it cannot serve.
2. **Never a number.** A gift touches at least two systems, and every event it makes cites its cause.
3. **No one's choice is taken away.** Consent and refusals stand (fixed decision 2).
4. **A cost or a reaction:** someone resents it, fears it, leans on it, or pays for it.
5. **It awakens** (lead's call). Until its trigger, you know only that "something came with you". If the trigger has not come by the gift's `byDay`, it arrives in a dream. Once awakened, it is known in every timeline. The awakening is the run's first story beat; a setting can name the gift at the start for players who want to plan.
6. **It grows by events, not grind.** Each gift names the events of its own that count, and tiers come at counts. Growth is world state, so a return sets it back, but a tier you reached before needs half the count.
7. **It is seen.** A gift is hidden, seen or loud; witnesses within 8 tiles react with a thought. Later, culture-faith scales that reaction by each culture's attitude to gifts, as it already does for peoples.
8. **You steer it through Orders.** A gift may add one standing order, shown only while you hold it and sent with `SetStandingOrder`. You never roll compliance against your own order (schedule design). The soak's stand-in player picks the order by seed (ADR-0017 #2).

### 4.3 The catalogue

| Gift (isekai root) | What it does | Touches | Seen | When |
|---|---|---|---|---|
| Bottomless satchel (item box) | your hidden store: never spoils, never stolen | Items, Rations, Needs, Social | uncanny | slice 1 |
| Fortune's favourite (lucky hero) | harm meant for you finds someone near you | Incident, Combat, Social | feared | slice 1 |
| Familiar faces ("have we met?") | those close to you in a lost future feel they know you | Fate, Social, Incident | unnoticed | slice 1 |
| Take the wound (suffering healer) | you draw a wound into yourself; dying for someone becomes a lost future | Work, Needs, Social, Fate | revered | slice 2 |
| Green hand (farming isekai) | bushes refruit, trees regrow; winter fruit is an omen | Work, Needs, Map, Culture | blessed or witch | slice 2 |
| Anchor (save point) | once a season you tie a knot; each thins the world | Fate, Director | hidden | slice 2 |
| Rain-caller (weather magic) | once a season you call rain | Incident, Needs, Culture | loud | later |
| Dreams of what comes (foresight) | you dream the storyteller's next blow, and sleep badly | Director, Needs, Mood | hidden | a director lookahead |
| Hear the unsaid (empathy) | you see grudges coming and feel others' despair | Social, Mood | hidden | later |
| Binding word (pact magic) | broken promises burn the breaker; kept ones bind | Social, Needs, Culture | feared by the free | oath weight |
| Every tongue (language gift) | no faux pas; raiders talked down before blades | Culture, Incident, Combat | uncanny | culture-faith |
| Red haze (berserker) | you cannot stop in a fight, and may strike a friend | Combat, Social, Mood | feared | later |
| Hearth voice (bard) | songs warm the hearth; a song of a lost future reopens grief | Mood, Social, Fate | loved | festivals |
| Mark of rule (king's aura) | your rules carry further; resentment runs hotter | Compliance, Social, Growth | rivals fear it | compliance helper |
| Thread-puller (fated meetings) | those you need find you, and so do those fated to cross you | Director, Growth, Social | unnoticed | generated wanderers |
| Speaker for the dead | for a day the dead name their wrongers and leave a last word | Lifecycle, Social, Culture | feared or revered | common deaths |
| Name-giver (naming evolves) | a name, if taken, can start an evolution and binds them to you | Growth, Social, Needs | revered | growth M1 |
| Talent sight (appraisal) | you see each want and the turning point it needs | Growth, Director | hidden | growth M1 |
| Maker's hands (Earth craftsman) | what you build lasts; those beside you learn | Build, Growth, Social | admired | Build, M1 |
| Earth memory (perfect recall) | ideas you champion take root; you keep every lost line | Ideas Tree, Fate | hidden | Ideas Tree |
| Wayfinder (map skill) | you always know the way and find hidden places | Travel, world map | unnoticed | world W2 |
| Merchant's eye (trader) | you know the fair price; those who learn it distrust you | Economy, Social | distrusted | loot-economy |
| Shared return (someone remembers) | your closest wakes with a piece of the lost future | Fate, Social, Growth | uncanny | later |
| Rift-eater | you close anomalies; each closed erases a lost future | Distortion, memory | hidden | anomalies |
| Second skin (mimicry) | you wear another people's look; discovery is betrayal | Culture, Social | betrayal | knowledge layer |
| Favoured of a god (the chosen) | a god (a Truth) favours you; your deeds read as its signs | Faith, Social, Incident | revered or hated | culture's gods |

### 4.4 The first three gifts

They run on today's systems. Numbers are params in `gifts.json`.

**Bottomless satchel** (`satchel`, seen). It holds 3 berries (6 at tier 2; 10 at tier 3, timber included). Nothing in it spoils, thieves and spoilage never reach it, and the ration policy does not count it.
- **Your order, "Your satchel":** *Keep a reserve for yourself* (the default), *Share it when someone is starving*, or *Keep nothing back*.
- **Filling and eating:** you fill it when you forage (`stashed`, citing the forage), except under *Keep nothing back*. When hungry you eat from it first, so the policy cannot leave you out.
- **Sharing:** under *Share*, you take a starving member a meal from it (`shared`, citing their starving), and they owe you for it.
- **Seen:**
  - Someone hungry, or who went without today, who sees you eat from it gets `ate_from_nowhere` (mood −6, opinion −15), citing both events. Repeated sightings form a grudge through the existing opinion line.
  - Anyone else's first sighting in a timeline gives `saw_vanish` (mood −2, opinion −3).
- **Awakens** on your first forage, by Day 2 at the latest. **Grows** with each share that lifts someone out of starving: tier 2 at 3 shares, tier 3 at 8.
- **Its story:** you stash berries in summer; in winter Nia goes without, sees you eat from nowhere, and holds a grudge.

**Fortune's favourite** (`fortunes_favourite`, feared).
- **Harm turns:** an accident that would hurt you, or a hostile's blow meant for you, lands on the nearest member within reach (1 tile; 2 from tier 2), for the same damage. `turned_accident` and `turned_blow` cite the roll or the fight, and the victim's events cite them.
- **The draw:** the turn always happens when someone is in reach; `DiceId.LuckVictim` (Gift stream) breaks only ties. Keeping people near you is your choice, and so is its price.
- **Seen:** the victim gets `ill_luck` (mood −4, opinion −8, stacking to 3, for ten days), and witnesses get half the opinion. Dislike feeds insults (ADR-0016), so the chain runs on to a grudge.
- **Tier 3, "Death passes you by":** once a season, a blow or accident that would down or kill you goes to the nearest person in reach; a bleed you let run stays yours (§8.5). If it kills them, they die in your place, and the world does not go back (lead's call: within the limits ADR-0025 sets for dark faiths, drawn like any death, ADR-0019, and never overriding consent).
- **Awakens** at the first turned harm. **Grows** with each one: tier 2 at 3, tier 3 at 7.

**Familiar faces** (`familiar_faces`, hidden).
- **Echoes:** each return stores every cast member's last opinion of you as an echo (newest wins).
- **After a return:** anyone in camp, or arriving, with an echo of 30 or more gets `familiar`. Their opinion of you rises by 25% of the echo (40% at tier 2, 60% at tier 3), and their mood by 2. An echo of −30 or less gives `uneasy_familiar` instead.
- **The line:** `knew_you` ("Hollis looked at you as if he had known you for years.") cites the return, and the arrival if they have just come.
- **Awakens** at the first `familiar`. **Grows** with each familiar person who becomes your friend, or does you a kindness, within ten days of a return: tier 2 at 2, tier 3 at 5.

### 4.5 What you control of yourself

The world design leaves "you can always go yourself" to this doc. The line runs between you and everyone else:
- You live by your people's needs and act on your own when you have no intention set.
- Intentions you set for yourself are kept without a compliance roll, though needs, the sun and danger still break in.
- Everyone else is only ever asked (fixed decision 2).
- `SetPersonalIntent(Tick, Kind, Target)`, whose kinds are go to, stay at, seek out and none, comes with W2, when there is somewhere to go (question 4).

## 5. Return by Death

### 5.1 Fate knots

A knot is a full snapshot of the world kept for a return. The director ties knots (fixed decision 8); you never do, except later through Anchor.
- **The arrival knot** is tied on the first tick. It is never dropped and is always eligible.
- **Turning knots** are tied after a tick holding an event whose key is on `rebirth.json`'s trigger list:
  - an arrival, or a joining promise;
  - someone starving, or weak with blood hunger;
  - a thief spotted, or a grudge formed;
  - winter;
  - from the world design, as content: oaths, wars, raids, a known ruler's death, holdings taken.

  A knot tied at a turn lets a return change how the turn ends.
- **Quiet knots:** at dawn, if no knot has been tied for 12 hours, one is tied.
- **Safe knots only.** No knot while you are downed, bleeding, below 400 health, at a critical need, fighting, fleeing or in the sun, because a knot inside a death already under way would loop.
- **Spacing:** at least 2 hours between knots, and at most 6 a day.
- **Not listed** (lead's call): no Chronicle line marks a knot, since listed knots invite gaming the pick. At most, the map's edges shimmer for a moment.

### 5.2 Death and the pick

Any death of yours except the end of your lifetime (§6) leads to a pick:
1. **Eligible knots** are those at least 2 hours before the death; the arrival knot always qualifies. Knots from the last 3 days are *recent*, and the rest are *old*.
2. **Recent or old:** a `FateKnotTail` chance at 150‰ picks the old ones. If only one kind is eligible, it is used.
3. **Weights:** old knots weigh the same, which gives the fat tail. Recent knots weigh by rank, the latest heaviest, which gives the recent bias. Each earlier use halves a knot's weight, so a knot that leads straight back to death frays.
4. **The pick:** a `FateKnotPick` draw over those weights. Both draws use the dying world's new Fate stream, so replay covers them.

### 5.3 The return, in order

The return runs at the end of `Simulation.Step`, after `DeliverToHooks` and `w.Tick++`, if you died during the tick. It never runs inside a system.
1. Pick the knot, and build the lost future (§5.5).
2. Carry the dying world's `World.Fate` forward, adding:
   - the lost future, the death and the timeline number;
   - the knot's use count;
   - your memories;
   - this timeline's applied commands, for replay;
   - Distortion, from slice 2.
3. Drop every knot tied after the chosen one, from `Fate.Knots` and from the archive.
4. Restore the knot's snapshot and overwrite its `Fate` with the carried block; the knot's own copy is dead on arrival. Copy `LearnedCustoms` from the dying player to the restored one (culture-faith §8 assigns this to the rewind).
5. Reseed with `Rng.Dice.Reseed(Mix64.Combine((ulong)timeline, (ulong)deathTick))`. Then re-draw the arrivals: `WorldGen.ScheduleArrivals` fixed every arrival's tick at creation in `World.Arrivals`, so a reseed alone would bring the same people at the same hours. For every arrival not yet done, re-draw who fills each remaining slot and at what hour. Use the reseeded World stream and the existing tags (`ArrivalOrder`, `ArrivalDay`, `ArrivalHour`), and keep each slot's window: ADR-0015's first week and eve of winter, batch 1's `arrivalDays`, and night hours for those the sun burns.
6. Emit `event.fate.returned`, citing the knot's trigger (nothing for a quiet knot or the arrival knot). Then emit each memory as a thought with `event.fate.remembered`, then each gift's return hook.
7. **Facade:** `Simulation.World` becomes privately settable and is swapped. Mod hooks are re-attached without a second `OnWorldCreated`, and delivery resumes where the restored log stood before step 6. Then `IFateHook.OnReturned` runs and `Simulation.Returned` is raised.
8. **View:** it pauses and `ViewStepper.Run` stops its batch. Drafts and pending commands are dropped, since `Enqueue` rejects past ticks. GoreLog, GroundWear and placement reset from the restored world.

**Memories** (lead's call). They make the strongest stories that cross a death, though they act without your say, as everything your character feels does (§4.5). For each cast member, take the strongest thing between you after the knot: a grudge you formed against them, else their death while you were friends, else a kindness they did you.
- It becomes `remembered_wrong`, `remembered_death` or `remembered_kindness`, at half the original's opinion weight, for ten days, with at most four per return.
- Memories gather across returns, one per person, and the newest wins.
- A memory is re-applied only where the restored world lacks it, because `MoodSystem.AddThought` re-applies the opinion change every time it is called.

### 5.4 What you keep and what goes back

| You keep (`World.Fate`) | Goes back with the world |
|---|---|
| your people, gift and soul span; that your gift woke, and its best tier | your body; your gift's growth and contents |
| every lost future; your memories; Familiar faces' echoes | everyone's memories, opinions, grudges, debts and promises (BIBLE 5.1.5) |
| customs you learned; what you have seen of the world and whom you met (world design, question 5) | items, buildings, zones, schedules, priorities, the ration policy |
| every timeline's commands; knots older than the one you returned to | Dice events (arrivals still to come, re-drawn in step 5; incidents, weather, pressure); companions' growth; newer knots |

Natures hold because they are Truths: traits, natural wants, pair chemistry, a dryad's tree, an undead's work. A want's type holds, but its target forms again from circumstance (ADR-0021 #6). Mira wants to protect someone; return to before Hollis arrives, and that someone may be you.

### 5.5 The Chronicle of Lost Futures

**What it keeps.** From the dying world's log:
- the stories ending after the knot (the eight longest, then those touching most systems), world stories included ("In another timeline, Rushmere burned in year 6.");
- companions' growth beats after the knot (BIBLE 5.1.8);
- your death.

**How lines are frozen.** Each event is stored once, as a loc key plus arguments (fixed decision 10).
- Cast ids are stable, because `CreateCast` runs in file order, so they stay as they are.
- Anyone made during play, such as a thief, freezes to a name key.
- A new argument type, `e:<future>:<line>`, lets one line quote another without composing English.

**In the Chronicle.** The popup gains tabs: *This life* and *Lost futures*. Each lost future is headed by its death and span, then its lines, then growth lines ("In that future, Mira became Protective."). The 30-line *This life* list (`ViewText.ChronicleLength`) is unchanged.

**As legends.** In slice 1, this is the record's register, as fixed decision 9 puts it. From slice 2, lost futures also leak into the world as tales and dreams, always built from stored lines (lead's call).

### 5.6 Distortion and anomalies (slice 2)

**Distortion** (0–1000) is run memory. A return adds 100, plus 4 per day sent back (at most 200), plus 50 if the knot was used before; it falls by 2 a day while you live. It shows in words, never as a bar ("The world feels thin"), and on the map colour drains a little with each band (view only). The bands are calm, uneasy (150), thin (350), fraying (600) and tearing (850).

**Anomalies.** The director rolls hourly on the Fate stream, at 0, 2, 6, 12 or 25‰ by band, at most one a day. Each is built from a stored lost future:
- **Echo dream:** a companion wakes thinking better or worse of someone in a lost future (Mood, Social).
- **Ghost of the lost:** someone who died in a lost future, and lives now, is seen where they died; nobody rests well there (Mood, Needs, Map).
- **Déjà vu:** a lost future's incident happens again at the same hour (Incident).
- **Told as a legend:** a companion tells a lost future at the hearth, and those in it take it to heart (Social, Mood).
- **The unremembered** (tearing only): someone looks at you as at a stranger; their opinion of you resets and their debts to you are forgotten (Social).

Companions remember nothing of a lost future, except through anomalies and the gifts that name an exception.

## 6. The lifetime clock

### 6.1 Everyone ages

Build, zones, culture-faith and the ghoul all wait for aging. The peoples doc deferred `lifespanYears` until something reads it, and this design is that reader.
- **Fields:** a species gives `adultAtYears`, plus either `lifespanYears` or `ageless: true`. A species without them ages as a human.

  | People | Adult at (years) | Lifespan (years) |
  |---|---|---|
  | Human | 18–22 | 60–78 |
  | Vampire | 18–30 | ageless |
  | Undead | 20–60 | ageless |
  | Dryad | 20–40 | 200–400 |
  | Djinn | 30–90 | 300–700 |
  | Fae | 20–100 | ageless |

  Later peoples take the peoples doc's figures.
- **Truths and state:** `TruthId.AdultAge` and `TruthId.Lifespan` are keyed by cast key. They set `Character.BirthTick` (negative for anyone older than the world) and `Character.OldAgeTick` (0 for the ageless) at creation. The peoples doc's planned `Lifespan` Truth is this one.
- **Death:** at dawn, anyone past `OldAgeTick` dies of old age (`event.growth.died_old_age`, Death effect). For a companion it is an ordinary death, with its grief.
- **Decline** comes in slice 2, once worlds run for years: thoughts first ("The cold gets into your bones"), then slower rest. This is fixed decision 12's decline.

### 6.2 The ageless

| Option | How | Verdict |
|---|---|---|
| **A. The soul's span** | your soul came with one lifetime (`TruthId.SoulSpan`, 40–50 years from arrival); the run ends at the body's old age or the soul's end, whichever is first ("Your soul's thread ran out") | **Recommended:** one human life for every roll, from one Truth; a longer clock would be a stat (fixed decision 5) |
| B. Each people wanes | the vampire's thirst deepens, the fae fade | code per species; hard to balance |
| C. Deaths cost years | each return takes a season off your soul | harsh; until a playtest says otherwise, a death costs the lost progress and Distortion (§5.6) (lead's call) |
| D. No clock | — | the lifetime means nothing (ADR-0024) |

**Play time.** A 40-day year is 4 hours at ×1 or 40 minutes at ×6 (ADR-0005). A human arriving at 18–22 with 60–78 years has 38–60 left, and the soul allows 40–50. So a typical life of 40–45 years is 27–30 hours at ×6, the pacing the world design assumes (its question 4).

### 6.3 How a run ends

- **The end of your lifetime:** old age, or your soul's end, is the only death with no return.
  - `Fate.Ended` is set, and from then on `Step` does nothing.
  - The Chronicle closes on lines from stored state: your people and gift, years lived, lives lost, how far you rose, and who outlived you.
- **The world bends to you:** the world design's daily dominion test ends the run in victory. The victory is held in `Fate`, so no return can undo it. If you rule on until old age (its question 1), each death still sends the world back.
- **Nothing else ends a run.** An empty camp is a story.

## 7. How the player meets it

The bottom bar is unchanged (Build, Orders, Zones, Work, Schedule | People, Chronicle | clock, pause, ×1 ×3 ×6).

**The opening.** A card over the map on the first tick:
```
┌──────────────────────────────────────────────────┐
│ You died on Earth.                               │
│ You woke here, in the ruins of a camp, as a      │
│ vampire. The sun will burn you. Blood will feed  │
│ you, if someone lets you drink.                  │
│ Something came with you that was not yours on    │
│ Earth. You have not found it yet.     [ Begin ]  │
└──────────────────────────────────────────────────┘
```

**Your inspector.** These lines sit above the usual needs, mood, traits and relationships:
```
┌──────────────────────────────────────────┐
│ You                                    × │
│ Vampire · reborn here · age 24           │ ← hover: what vampires need
│ Your body does not age. Your soul came   │
│ with one lifetime.                       │
│ Gift  Bottomless satchel · holds 3 meals │ ← hover: what it does, how it grows
│ Lives lost  2                            │
│ Remembers  What Hollis did, in a future  │
│            that never came (−10)         │
└──────────────────────────────────────────┘
```
A human sees "Age 23 · Humans here live to about 70". Late in life, a thought stands in for any number: "Your soul feels thin."

**Orders.** Your gift's order appears only while you hold that gift:
```
┌ Orders ────────────────────────────────────────────┐
│ When food runs short                                │
│  (•) The hungriest eat first  ( ) Promised meals    │
│  ( ) You eat first                     come first   │
│ Your satchel                            (your gift) │
│  (•) Keep a reserve for yourself                    │
│  ( ) Share it when someone is starving              │
│  ( ) Keep nothing back                              │
└─────────────────────────────────────────────────────┘
```

**Dying and coming back.** About four seconds; a click skips it:
```
1. Time stops; the map darkens.    ┌──────────────────────────────────┐
                                   │ You died of your wounds.         │
                                   └──────────────────────────────────┘
2. A short, colourless fade: the world folds back.
3. The map returns, paused.        ┌──────────────────────────────────┐
                                   │ Day 1, 20:00. You came back.     │
                                   │ You remember everything.         │
                                   │ [ Read what was lost ] [ Go on ] │
                                   └──────────────────────────────────┘
```
"Read what was lost" opens the Chronicle on its Lost futures tab. A run that ends shows one last card ("You died of old age, at 71. There is no return from this."), with *Read the Chronicle* and *A new world*.

## 8. What changes in the simulation

### 8.1 State

- **`World.Fate`** (`sim/State/Fate.cs`) is run memory. It holds:
  - your Truths: `PeopleId`, `GiftId` and `SoulEndTick`;
  - your gift's memory: `GiftAwakened` and `GiftMastery`;
  - the timelines: `Timeline`, `Returns` and `Knots` (metadata only: tick, trigger, reason, uses);
  - what you carry: `LostFutures`, `Memories`, `Echoes` and `TimelineCommands`;
  - the ending: `Ended`, `EndKind` and `EndTick`;
  - from slice 2, `Distortion`.
- **`World.Gift`** goes back on a return. It holds `Tier`, `Progress`, `Held` (the satchel), `LastDeathTurnedSeason` and `SeenBy`.
- **`Character.BirthTick`** and **`Character.OldAgeTick`**; **`Character.Bleed`** and **`BleedCause`** (§8.5).
- **`Simulation.Knots`** maps each knot to its gzipped snapshot. It lives outside `World`; inside, every snapshot would contain the earlier ones, and a run would grow quadratically.

### 8.2 Where it runs, and its cost

`Fate.EndOfTick` runs between ticks and checks three things in order: the end of your lifetime, a return, a knot. Your death sets `Fate.PendingDeath`; the rest of that tick runs as usual, and the return then discards it.

**Measured** at day 40 with ten people (Release build): a snapshot is 224 KiB in 2.2 ms, gzips to 25 KiB in 0.6 ms, and unzips and restores in 4 ms. Six knots a day cost about 17 ms. ADR-0005's budget is 144 ms a day for ten people, and a simulated day now takes about 13 ms. The cost grows with the event log; slice 2's storage removes that growth (§10).

### 8.3 Commands

- **`SetStandingOrder(Tick, OrderId, ChoiceId)`** carries a gift's order (`gift.satchel`: `keep`, `share`, `empty`). The peoples doc (its slice 2C) and the loot-economy design use the same command; whichever lands first defines it.
- **No command** can rewind, tie a knot or kill you: commands are replayable player intentions.
- **Tests** call the internal `Fate.ForceDeath`, through the existing `InternalsVisibleTo`. It logs "(soak) You fell dead."

### 8.4 Events, causes and endings

All have `_you_` forms (ADR-0017 #4).

| Event | System | Cites | Effect |
|---|---|---|---|
| `event.fate.woke` | Fate | — | — |
| `event.gift.awakened` | Gift | the trigger | — |
| `event.fate.returned` | Fate | the knot's trigger | — |
| `event.fate.remembered` | Mood | the return | — |
| `event.gift.satchel.stashed`, `.ate` | Gift, Needs | the forage; your hunger | — |
| `event.gift.satchel.seen` | Social | your meal; their going without | — |
| `event.gift.satchel.shared` | Gift | their starving | Debt formed |
| `event.gift.luck.turned_*` | Gift | the roll, fight or killing harm | — |
| `event.gift.familiar.knew_you` | Social | the return; the arrival | — |
| `event.gift.grew` | Gift | the event that counted | GiftGrew |
| `event.growth.died_old_age` | Growth | — | Death |
| `event.fate.soul_spent` ("Your soul's thread ran out.") | Fate | — | Death |
| `event.needs.died_contract`, `.died_courtesy` (you only, §4.1) | Needs | what last explained the need | Death |
| `event.fate.anomaly.<id>` (slice 2) | Fate | the return | per anomaly |

Stories still end in grudges, debts, departures and deaths (ADR-0006), and now also in `GiftGrew`. Lost futures' stories are counted separately. Nothing counts twice: the final log holds the shared past, and each lost future holds only what came after its knot.

**What it touches:** Needs, Combat and Work (the satchel, the luck, wounds and their tending); Social and Mood (memories, familiarity, resentment, debts); Incident and the Director (knots, anomalies); Growth (wants, prior versions, aging); Culture (stances, carried customs).

### 8.5 Wounds that bleed (question 1)

Step 4 builds this unless question 1 is answered otherwise. Harm the camp already deals (work accidents, the thief's blows, later raiders') can kill when nobody tends it. Nothing new is aimed at you.
- **A wound bleeds.** `CombatSystem.Hurt` adds a share of the damage to `Character.Bleed` (health lost per hour) and keeps the wound's event as `BleedCause`. `NeedsSystem.Hourly` takes the bleed with the other harm, citing the wound, and a death uses the wound's own line ("You died of your wounds.", "… of your injuries.").
- **Tending stops it.** Anyone bleeding counts as injured, so carers come. The existing care job sets `Bleed` to 0, and the tended owes the tender, as now.
- **No new draw.** Who lives turns on who is near and awake, so the only change to determinism is that worlds part ways after the first untended wound. The share is a code constant (ADR-0013), tuned to question 1's target.
- **It touches** Incident and Combat (the wound), Needs (health), Work (care) and Social (the debt; grief at a death).

## 9. Content and schema changes

**New files**, each with a schema (the ADR-0021 #12 test):
```json
// rebirth.json
{ "peoples": [ { "species": "human",   "weight": 30, "openingKey": "rebirth.opening.human" },
               { "species": "vampire", "weight": 14, "openingKey": "rebirth.opening.vampire" } ],
  "soulSpanYears": [40, 50],
  "knots": { "triggers": ["event.incident.wanderer_arrives", "event.social.joined_promised_meal",
                          "event.social.joined_promised_rest", "event.needs.starving", "event.needs.blood_starved",
                          "event.combat.thief_spotted", "event.social.grudge_formed", "event.world.winter_begins"],
             "quietAfterHours": 12, "minHoursBetween": 2, "maxPerDay": 6, "safeHealthAtLeast": 400 },
  "return": { "recentDays": 3, "tailPermille": 150, "minHoursBeforeDeath": 2 },
  "memory": { "strengthPct": 50, "days": 10, "maxPerReturn": 4 } }
```
```json
// gifts.json, one entry
{ "id": "satchel", "nameKey": "gift.satchel.name", "summaryKey": "gift.satchel.summary",
  "weight": 10, "peoples": { }, "seen": "seen",
  "awakening": { "byDay": 2, "textKey": "gift.satchel.awakened" },
  "tiers": [ { "at": 0, "params": { "capacity": 3 } }, { "at": 3, "params": { "capacity": 6 } },
             { "at": 8, "params": { "capacity": 10, "holdsTimber": 1 } } ],
  "order": { "id": "gift.satchel", "choices": ["keep", "share", "empty"], "default": "keep" },
  "params": { "sight": 8, "resentOpinion": -15 } }
```
`loc/en.fate.json` holds all this design's text, in its own file as batch 1 and culture-faith did.

**Changed:**
- `species.schema.json`: `adultAtYears` and `lifespanYears` (pairs, min ≤ max) and `ageless`, which excludes `lifespanYears`.
- `thoughts.json`: `remembered_wrong`, `remembered_kindness`, `remembered_death`, `saw_vanish`, `ate_from_nowhere`, `ill_luck`, `familiar` and `uneasy_familiar`, each with an existing emote.
- batch 1's `needs.json`: a `deathKey` on `contract` and `courtesy` (an existing field), with its text in `en.fate.json`.
- `cast.json` and `world.json`: unchanged.

**Checked at load**, each failure naming the file and the id:
- rollable peoples exist, with a positive weight and opening keys, and every one of their need and sun lines has its `_you_a` form;
- every gift has a behaviour (`RequiredContent.Gifts`, as for incidents and needs), loc keys, ascending tiers, order choices, and a `byDay` within the first season;
- knot triggers are known event keys;
- age fields are well formed;
- every need that drives people away has a `deathKey`, with its `_you_a` form.

## 10. Determinism, snapshots, text-state API, localization, mod hooks

**Truths** (appended): 80 `PlayerPeople`, 81 `PlayerGift` and 84 `SoulSpan`, keyed by your cast key; 82 `AdultAge` and 83 `Lifespan`, keyed by cast key; 85–89 reserved (later gift details, such as which god favours you).

**Dice:**
- **Streams:** `DiceSystem.Fate` (knots and anomalies) and `DiceSystem.Gift` take the next contiguous values at merge. Appending a stream moves no other.
- **Ids:** 220 `FateKnotTail`, 221 `FateKnotPick`, 222 `LuckVictim`, 223–229 for anomalies, 230–239 for later gifts.
- **Isolation:** a gift draws only from its own stream, and only when it acts. Wounds that bleed draw nothing (§8.5).
- **No overlap:** this block is clear of world (120–149, 300–339), loot-economy (150–159, 350–369), peoples batch 2 (50–59, 160–179) and schedule (70–79, 200–219).

**Replay.** The same seed and the same commands in each timeline give the same run. `Simulation.Replay(content, seed, Fate.TimelineCommands)` re-runs a whole life: deaths fall on the same ticks, and the picks repeat.

**Snapshots.**
- `World.Fate`, `World.Gift`, `Character.Bleed` and `BleedCause` round-trip and count in the hash, so a restore mid-bleed keeps bleeding. A later save is the world snapshot plus the archive, with `SchemaVersion` bumped (ADR-0021).
- **Slice 2's storage:** a knot omits the event log and keeps its length. The log only grows within a timeline, and every surviving knot is an ancestor of the current one, so a restore cuts the current log back to that length.
- **Thinning:** keep every knot from the last 10 days, then one a day for a season, then one a season.

**Text-state API.**
- `StateApi.Player(w)`: your people, age line, gift (name, text, awake, stage), lives lost, timeline and memories; from slice 2, Distortion's word.
- `StateApi.LostFutures(w)` and `StateApi.Gifts(content)`.
- **Runner:** the cast line reads "You (Vampire, reborn here; gift: Bottomless satchel)". A return prints as a divider, and a "Lost futures" section follows the log.
  - `--fates` forces one death per world, on a day from 10 to 35, hashed from the seed outside the sim's streams.
  - `--people` and `--gift` are marked "chosen for testing".
- **Soak summary** adds: natural deaths by cause, people and gift; companions' deaths; returns, days sent back (and the deep-past share), stories per timeline, stories crossing a return (a Fate or Gift event in the chain), lost stories, gift events on chains, stories per people, and death loops (three deaths in three days).

**Localization** (`en.fate.json`): `rebirth.opening.<species>`; `gift.<id>.*`; `event.fate.*` and `event.gift.*` with `_you_` forms; `life.*`; the Chronicle tabs.

**Mod hooks.** An optional `IFateHook` (`OnReturned`, `OnLifeEnded`) leaves `IModHook` mods unbroken, and hooks see the return's events at the end of the tick (ADR-0018 #3). Mods add rollable peoples by `rebirth.json` entries merged by species id, and gifts as JSON plus a behaviour named by id; registering behaviours waits for the peoples doc's hook (its §12.4).

## 11. Fit with the other documents

- **Peoples:** you can be any shipped people, and its need runs on you as on anyone, except that a need which only drives people away harms you instead (§4.1); `lifespanYears` and the `Lifespan` Truth live here, not in batch 2's range; the brimmet becomes rollable when 2A ships; its question 2 decides which peoples a world holds.
- **Culture-faith:** you stay the outsider with no culture or faith; `LearnedCustoms` carries across a return; stances toward your people apply to you; "gods who stir" become learnable across deaths; reactions to gifts become culture attitudes, in content.
- **Growth:** your want is drawn with your people; BIBLE 5.1.5 and 5.1.8 are carried out here, and aging starts here; evolution on player milestones waits for M1.
- **Build, zones, schedule:** each restores at the knot, as all three say. Their "lay out the camp as I had it" is slice 2's `Restate`: one click re-sends your last timeline's zones, days, priorities and orders.
- **World:** you wake in its wild arrival region, whose map is today's camp. Its fate-knot candidates become triggers, its map memory is kept, and its dominion test means victory. Its raiders strike as the thief does, so their wounds bleed too, and W1b measures against the baseline step 4 sets. Rivals should draw from these tables by rival slot, as Truths to learn; Return by Death stays yours alone.
- **Death guards elsewhere:** zones' cap of 0.1 member deaths a world-year and culture-faith's vampire guard (125% of the base) guard their own changes. Once step 4 lands it moves the base they measure from, and zones reads its cap against that base, as it does its floors. Step 4 answers to its own guard (§12.1).
- **Loot-economy:** the satchel holds whatever items exist; Merchant's eye waits for its economy.

## 12. Build order and acceptance

### 12.1 Slice 1: reborn, and returned (now)

**Steps**, each a small commit with tests:
1. **Rebirth:** `rebirth.json` and the roll in `CreateCast` and `NatureOf`; aging and the end of a life; the opening card, inspector lines and `_you_a` forms; the runner's header, `--people` and `--gift`.
2. **The return:** `World.Fate`, knots, the archive, the pick and §5.3; memories and lost futures; the facade swap; the view's pause, sequence, resets and tabs; the runner's divider, Lost futures section and `--fates`.
3. **Gifts:** `gifts.json`, the roll, awakening, tiers, mastery and the Gift stream; the three gifts; `SetStandingOrder` and its Orders row, with the stand-in player picking the satchel's order by seed.
4. **Danger** (question 1): wounds that bleed until tended (§8.5), tuned with `--people` to the natural-return target. Every world changes here, so the step's ADR records the new soak baseline.
5. **Record:** the soak, the stranger check, STORIES.md, and an ADR with the final ids.

**Tests:**
- `RebirthIsATruth`: people, gift, ages and spans survive reseeded Dice and reversed arrivals; over 1,000 seeds, peoples land within 3 points of their weights.
- `RebirthChangesNothingWhenHumanAndGiftless`: through step 3, forced human with no gift, all 100 soak logs are byte-identical with the merge base apart from the header and the opening line. From step 4 it compares with step 4's baseline, and checks its three guarantees directly: the rebirth Truths draw no Dice, the Fate and Gift streams leave every other stream's sequence unchanged, and a world that ties knots hashes the same, `Fate` aside, as one that ties none.
- `UntendedWoundsKill` and `TendingStopsTheBleeding` (§8.5): a wound nobody tends kills, and the death cites it; a tended one stops, and the tended owes the tender.
- `YourNeedWearsYouDown` (§4.1): as the djinn or the fae, once the leaving clock has run you take hunger's harm, and can die and return; anyone else with the need still leaves.
- `EveryRollablePeopleCanBeYou`: a year each, no exceptions or missing text.
- `YourDeathReturns`: back to a knot at least 2 hours earlier; the lost future holds the death; later knots gone; Dice reseeded.
- `ArrivalsReroll`: across seeds, arrivals still to come after a return change who comes or when, and none falls outside its window.
- `KnotsAreSafe` (§5.1) and `PickHonoursTheShape`: over 10,000 picks the deep-past share is 150‰ ± 15, and a used knot weighs half.
- `ReturnIsDeterministic`: same seed, commands and forced deaths give the same hash, and `Simulation.Replay` matches.
- `WhatYouKeep` (§5.4), `MemoriesNeverStack` (no doubled opinion), `LostFutureLinesRender`.
- `OldAgeEndsTheRun` and `TheSoulRunsOut` (which ends with `event.fate.soul_spent`), on short-lived test content.
- The gift chains of §4.4, and `GiftDrawsStayInTheirStream`.
- A snapshot after a return restores to the same future; `ReturnUnderOneSecondAt200Characters`.
- ADR-0019's watched-equals-unwatched test, across a return.
- Knot cost within ADR-0005's budget at the largest camp.

**Soak** (100 worlds × 1 year; targets, with misses reported honestly as in ADR-0017):
- **Every run:** no crashes or missing text; serial and parallel runs byte-identical; no world dies four times in three days.
- **Ordinary:** stories per world-year in human worlds at or above the merge base, each other people reported. Natural returns (question 1) in 25–35% of worlds, and in at least 15% for each people, measured with `--people` at 100 worlds a people and reported by gift. Companions' deaths at most 2 a world-year (lead's call; 0.06 today).
- **`--fates`:** every world returns; in at least half, a final-log story carries a Fate or Gift event; at least 60% of lost futures hold a story.
- **Gifts** (at least 30 worlds each): a satchel event on a story chain in 40% of satchel worlds; a turned harm in 25% of Fortune's favourite worlds; familiarity in 40% of Familiar faces worlds under `--fates`. Familiar faces keeps its weight, since natural returns give it deaths to work on.

**Stranger check.** Four readers each get one raw log from the ordinary soak in which you died on your own and came back (the lowest such seeds). At least two retell, unprompted, a story that crosses the return.

**Screenshots:** a vampire's opening card, the return's three frames, the Lost futures tab, your inspector, and Orders with the satchel row.

### 12.2 Slice 2, and what waits

- **Slice 2:** Distortion and anomalies; knots stored without the log, and thinning; decline, once soaks span years; Take the wound, Green hand and Anchor; `Restate`.
- **Later:** the rest of the catalogue as its "When" column allows; rivals; victory; saves; `SetPersonalIntent` with W2.
- **Aging needs multi-year worlds:** `WorldGen.ScheduleArrivals` schedules only year one, so an aging camp empties, and trees never regrow (ADR-0022).

## 13. Questions for the founder

Most important first, each with a recommended answer. Until a question is answered, the builder builds the recommendation. The first draft's questions on memories, gift darkness, legends, knowing your gift, choosing your people, the cost of death and feeling a knot are now answered or lead's calls where the builder looks (§2, §4.2, §4.4, §5.1, §5.3, §5.5, §6.2).

1. **How often should you die on your own, and may slice 1 make the camp deadlier to get there?** Measured, you die on your own in 3 of 600 one-year worlds across the six peoples (§1), and two of the three slice-1 gifts make you safer still.
   *Recommended:* about once every three in-game years, roughly once per two hours of play at ×6. That is some 12–20 returns in a 40–45-year life, 2–3 of them sent back further than three days (§5.2). Slice 1 gets there with wounds that bleed until tended (§8.5, step 4), aiming at a natural return in 25–35% of one-year worlds and at least 15% for every people. If you would rather keep deaths rare until raiders reach the camp (world design W1b), step 4 is dropped, the stranger check reads forced deaths, and Familiar faces waits until deaths are common.
   *Why it matters:* without it, Return by Death almost never happens in play. With it, every world's logs change and companions die more often too (0.06 a world-year today).
2. **Approve the schema and determinism changes** listed under "Sign-off approves". The bleeding adds no draw, and the `deathKey`s fill a field batch 1 already has.
   *Recommended:* approve. CLAUDE.md asks before both kinds of change, and every rolled world draws on them from its first tick.
3. **What clock do the ageless keep, and how long is a life?** ADR-0024 left this open.
   *Recommended:* option A (§6.2), a soul's span of 40–50 years from arrival, whatever the body, with human bodies living 60–78 years. That gives a life of 27–30 hours at ×6, the world design's pacing.
   *Why it matters:* a lifespan that varied by people would be a stat (fixed decision 5), and these are Truths, so changing them later re-draws every world.
4. **May you direct your own moves (go to, stay at, seek out)?**
   *Recommended:* yes, as intentions for yourself only, which your needs can still break into; everyone else is only asked. The main principle says you adventure, fixed decision 2 forbids controlling anyone else, and ADR-0025 already spares you compliance rolls.
   *Why it matters:* it draws fixed decision 2's line at your own body. It comes with world W2, and the loot-economy design's ventures already assume it.
