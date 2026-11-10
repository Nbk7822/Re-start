# Peoples of myth: design

- **Date:** 2026-11-09. **Status:** signed off by the founder (ADR-0025), and revised the same day for that decision's first point: beyond fixed decision 5's archetypes, every people is our own derivative.
- **Governs:** fixed decision 5 (peoples differ in needs, not stats), read with fixed decisions 1, 2, 4, 6, 10 and 12 and the main principle (ADR-0024).
- **Read with:** CLAUDE.md; BIBLE.md §4.4 (the vampire) and §5 (growth); ADR-0004 (determinism), ADR-0006 (events and stories), ADR-0007 (content), ADR-0013 (tuning in code), ADR-0017 to ADR-0021, ADR-0024 and ADR-0025; and ADR-0022 (the first four peoples and the needs model they run on), recorded on the batch 1 branch (`worktree-wf_23280ce9-9be-1`) as accepted for the build. Companion designs in this folder: `2026-11-09-build-design.md` (its grave and the act of laying the dead to rest, §4.8 and slice 8.4, are used here), `2026-11-09-zones-design.md`, `2026-11-09-schedule-design.md` and `2026-11-09-culture-faith-design.md`.
- **Revisions.**
  - *After the builder's questions:* every people runs on batch 1's needs model (ADR-0022), so the first plan (generic satisfier kinds, species.json v2, a vampire migration first) is withdrawn (§5). 2A is one people alone, and the ghoul moves to the slice of the dead (§12.1).
  - *After ADR-0025:* there are no tradition readers. Each people beyond the archetypes is an original derivative with its own name, look and twist (§3); batch 2 is rewritten as derivatives in the same order and slices (§6); the roster goes from 40 entries to 32 (§4). Folding near-duplicate kin into one derivative and dropping two entries are the lead's calls, and any of them can be split back out. §4.4 maps every earlier name, including those the companion designs still use.
- **The sign-off covers:** the content rules (§3); the roster and the build order (§4, §12); 2A's content and schema changes (§9: one optional species field, `arrivesFrom`; the `head_water` need, the brimmet and Teal as entries in batch 1's formats; new thoughts and loc keys). These presuppose that ADR-0022 is accepted when batch 1 merges.

## Summary

1. A people is a body with a need the camp must answer, and a heart shaped by that body and by the culture the person grew up in. Each people adds a need that someone else's choice, labour, food or time must meet. That collision is where unplanned stories start, as with the vampire turned down by all eight humans in the camp (STORIES.md).
2. Beyond the vampire, dryad, djinn, fae and undead of fixed decision 5, every people is our own: an original creation inspired by Earth folklore, with its own name, look and twist (ADR-0025). The Earth-born player may notice a resemblance, which helps where a derivative keeps a motif and misleads where it twists one (§3).
3. Every people runs on batch 1's needs model (ADR-0022): each need is data in `content/needs.json` plus one behaviour named by its id, so a kin people that shares a need is JSON alone, and code grows by need or act, never by species (§8).
4. The roster is 32 peoples: the human, the five archetypes, and 26 derivatives drawn from motifs across Asia, Africa, Europe and the Americas (§4). Batch 2 gets full entries: the brimmet, ghoul, clobbin, listrel, werewolf, tatterlin, knollet and feeplin (§6).
5. The first slice (2A) is the brimmet alone (water in a dish on its crown), on today's map and items. The ghoul (the flesh of the dead, eaten only with leave from the dead's friends) waits for the slice of the dead, because a camp member dies in only a few one-year worlds (§12.1).
6. Peoples bring their own clocks: a dish that dries every day, a moon every ten days, a bowl to fill every night. They are the first pressure the director gets in the middle of the year (ADR-0018).

---

## 1. Intent: why peoples, and the stories they make

Fixed decision 5 says peoples differ in what they need, not in numbers. The vampire shows why that works. Ilex cannot feed without a human saying yes, so every thirsty night puts a question to the camp, and the answer depends on who is hungry, who is weak, who likes Ilex and who was refused last time. One story strangers retold unprompted at the M0 exit check is that question asked eight times and answered "no" eight times.

Every people in this document must be at least that good. Its need must be something the camp can meet only through somebody else: their consent, their labour, their food, their silence, their courtesy, their time. A people whose need it can meet alone is a human in a costume, and is cut.

There are two further reasons.

- **The middle of the year is empty.** ADR-0018 records that most stories land in the last ten days, because the director has no mid-year pressure besides thieves and spoilage. Peoples bring their own clocks: a brimmet's dish dries every day, a ghoul waits for a death, a werewolf's moon comes round every ten days, a tatterlin's bowl must be filled every night. These run all year, and they collide with winter when it comes.
- **The player half-knows them.** The player is Earth-born, and a brimmet calls to mind river folk from stories at home. Where a derivative keeps a motif, that memory helps: a brimmet who is bowed to must bow back and spill its water, and a player who remembers can save a friend with it. Where a derivative twists a motif, memory misleads, and finding out is a story too. Both are Earth knowledge used in play, as fixed decision 6 intends. Everything about the person is the world's own: their culture, faith, language and name.

Three stories the first batches should produce, as they would read in the Chronicle (names follow the M0 cast's style until cultures generate names):

**The bow at the watch post** (brimmet: Social → Needs → Combat → Work → Social)
```
Day 22, 23:10 — Hollis bowed to Teal at the watch post, and Teal bowed back; the water spilled from Teal's head-dish.
Day 22, 23:10 — Teal's head-dish is drying out.
Day 23, 02:40 — Teal spotted a thief.
Day 23, 02:55 — Teal, weak with a drying dish, was struck down by the thief.
Day 23, 03:20 — Bram carried Teal to the river.
Day 23, 03:20 — Teal owes Bram for the rescue.
```

**What was left of Bram** (ghoul, once deaths are common: Combat → Social → Needs → Social)
```
Day 26, 04:10 — Bram died of his wounds.
Day 26, 20:05 — Morrow asked Mira's leave to eat what was left of Bram.
Day 26, 20:05 — Mira refused.
Day 30, 01:00 — Morrow is starving for the dead.
Day 30, 01:30 — Morrow, starving, went to Bram's body in the night against Mira's wishes.
Day 30, 06:10 — Mira found what had been done to Bram.
Day 30, 06:10 — Mira now holds a grudge against Morrow.
```
If the camp has buried Bram by then, the night meal takes its grave form (§8.11): "Morrow, starving, dug up Bram's grave in the night", and whoever buried him finds the grave open too.

**The empty bowl** (tatterlin: World → Needs → Incident → Needs → Social)
```
Day 31, 00:00 — Winter came, and frost took the berries from the bushes.
Day 33, 21:00 — When the camp shared out what little food it had, Pim's bowl at the hearth was left empty.
Day 34, 21:00 — Pim's bowl was left empty again.
Day 35, 02:00 — Pim, slighted, soured 3 berries in the stockpile.
Day 36, 09:00 — Nia is starving.
Day 37, 17:00 — Nia left the camp.
```

In the third story the tatterlin's bowl is its ration, so the ration policy the player chose on day 1 decides whether it is filled. That is how the ration policy already links the player's first decision to an ending weeks later (STORIES.md, "Vale, refused by a starving camp").

## 2. Goals, non-goals, anti-goals

**Goals**
1. Peoples whose differences are needs, harms and feelings, never numbers. Every need is data, and every need touches at least two systems.
2. One needs model for every people, batch 1's (ADR-0022): each need is data plus one behaviour named by its id, so a kin people is a JSON entry and a new need is one behaviour.
3. A wide roster of our own derivatives, chosen with care, built in an order set by story value first and code reuse second.
4. Hostile members of a people take the thief's path through the code. No people is hostile by nature.
5. More stories that end before winter.

**Non-goals (for this document)**
- **Stat differences.** No people is stronger, faster or smarter as a number. Every species keeps `ticksPerTile` 6. Body size matters only where the world must fit a body (beds, doors, stairs), and that comes with Build.
- **Hybrids, conversion and the Ideas Tree.** They are architected for (needs are a list, leanings are numbers, looks are parts) but not built. Conversion means being bitten, cursed or raised.
- **A knowledge layer** (who knows what about whom). Batch 4 and the listrel's gossip need one; batch 2 does not.
- **Generative text.** Every line is a template filled from stored state (fixed decision 10).
- **Aging.** Lifespans stay in this doc's entries until aging exists, and join content then (§9). Fixed decision 12 needs aging before its M1 acceptance (ADR-0020), and ADR-0024 makes lifespans matter more, since the player may roll an ageless people.

**Anti-goals**
- A bestiary. A people ships only with a need that makes stories.
- Humans in costumes. If a people's need does not change what the camp must do, it is cut.
- A tradition's being reproduced under our name; peoples as stand-ins for real ethnic groups, real religions, real dress or real speech.
- Good peoples and evil peoples. Hostility is a situation: a starving ghoul, a slighted tatterlin, a wolf on a moon night. It is never a people's nature.
- A species panel, a species HUD or a spreadsheet of peoples. Peoples appear on the map, in the inspector, in tooltips and in the Chronicle (ADR-0008).

## 3. Rules for choosing and portraying peoples

These rules are part of the sign-off. A builder adding a people later applies them without asking.

1. **Our own derivatives.** The vampire, dryad, djinn, fae and undead of fixed decision 5 are built as they are. Every other people is an original creation inspired by Earth folklore, never a reproduction of a tradition's being (ADR-0025). Each passes four tests:
   - **Its own name** (rule 6).
   - **Its own look:** no tradition's iconic image or dress.
   - **Its own twist:** at least one thing in its need, body or heart that no source tells, chosen for the stories it makes. Each batch 2 entry lists its twists under "Ours".
   - **Motifs, not a being:** it draws on folklore motifs, from more than one tradition where it can, and its entry names them in an "Inspired by" line.
2. **Which motifs.** A derivative may draw on widely shared, non-sacred folk motifs from any region, including regions the roster is thin in. It never draws on a closed or sacred being, on anything prayed to or venerated, or on a god, prophet, saint or angel, and it never uses scripture, prayer or sacred names as a mechanic (no verses as wards, no divine name animating a construct). A being that a living religion also speaks of appears only in its folk form. The djinn are the test case: many Muslims hold that jinn exist, as the Qur'an describes, so Return's djinn are those of folk tales and the Thousand and One Nights (bargains, lamps, rings, contracts), with no verses, no jinn named in scripture, no prayers as wards, and no portrayal of belief in jinn as foolish.
3. **Exclusions.** These are left out, and no derivative may be a thin rename of them. In Return no real group carries a people as a secret identity, and nobody is accused of being a monster because of who they are.

| Why | Left out |
|---|---|
| A source community holds it sacred, or asks outsiders not to use it | the *wendigo*, part of the living teaching of Algonquian peoples (Anishinaabe, Cree and their neighbours) about greed and the taboo it guards, which many ask not to be made into entertainment; *skinwalkers*, whom Diné people avoid speaking of and ask outsiders not to depict; the *thunderbird* and the *katsinam*; the *nāga*, *garuda* and *yaksha*, venerated in Hindu, Buddhist and Jain practice; the *taniwha*, kaitiaki (guardians) to Māori; *Mami Wata*, venerated across West and Central Africa and the diaspora; the *alux* of living Maya practice; the Hawaiian *huakaʻi pō*; the Chamorro *taotao mona*; the ancestral beings of Aboriginal Australia, such as the bunyip and the Rainbow Serpent, which belong to Country and law; Inari's messenger foxes (folk foxes stay) |
| The belief is used to harm real people today | *tokoloshe* and *impundulu* (witchcraft accusations in southern Africa); *bouda* (accusations against artisans and Beta Israel in Ethiopia); *aswang* (a stigma against the people of Capiz, and accusations against women); *àbíkú* and *ọgbanje* (children believed to be spirit children have been mistreated); fox possession, and the "fox-keeping" families of Japan; *changelings*, a belief used to justify violence against disabled children and women (the 1895 killing of Bridget Cleary is the best-known case) |
| It caricatures real peoples | the *kapre*, named from the colonial "cafre" and drawn as a dark-skinned giant; the *saci*, a contested racial caricature; the *abatwa*, whose name is also a name for a real people (the Twa); the *patupaiarehe*, used in pseudo-history that denies Māori first settlement; the *mouras encantadas*, enchanted "Moorish" maidens; the *pishtaco*, a folklore of real colonial violence; the *rakshasa*, epic demons long read as a picture of real South Indian and forest peoples; the stage-Irish *leprechaun* of green suits and pots of gold; dwarves drawn with greed and hoards, which echoes antisemitic tropes; goblins with horns and tiger skins, an image widely held in Korea to be a colonial-era import from Japanese oni |

4. **No people wears a real people's costume.** Cultures, faiths, languages, scripts and dress are invented for each world (culture-faith design). A brimmet lives in one of the world's cultures, never in a Japan-like one, and a ghoul wears no desert-bazaar dress.
5. **The resemblance.** The Earth-born player may notice what a people is like. Hovering the species word shows `species.<id>.resemblance`: "Earth folklore tells of something like them: river folk who keep their strength in a dish of water on the head, and spill it when they bow." The line names motifs, never a tradition, a country or a being's own name, so it points at a memory without claiming the people is that being. The archetypes get the line too ("… the dead who rise to drink the blood of the living"). The world itself knows nothing of Earth.
6. **Names.** A people's name is a coinage that is no word of a real language and no tradition's own term. It may be built from English roots (turnfoot, kilnborn): it is the English string under `species.<id>.name`, and translators coin their own rather than borrow a tradition's term. Batch 2's six coinages were checked against dictionaries and the web on 2026-11-09; the others are checked when their batch is designed. Archetypes as widespread in fantasy as the vampire (the werewolf, ghoul, troll, dwarf and centaur) keep their generic names and still get a twist. A person's name takes its *structure* from the people (what a name is made of, who gives it, when it changes; §6) and its *sounds* from the world's invented cultures, never from a real language's list of names (accepted, ADR-0025). The example names in this document (Teal, Morrow, Pim) follow the M0 cast's style and are placeholders.
7. **No people is evil by nature.** A threat form is one person in a situation (§4.3), never the whole people. A hostile ghoul is a ghoul who broke the ghouls' own code.
8. **Peoples, not individuals; folklore, not cryptids.** Single legendary figures are not peoples: Baba Yaga, La Llorona, Anansi, the Minotaur. Modern cryptids are not folklore peoples: the chupacabra, Mothman, and the sasquatch of the cryptid hunters (the Sts'ailes' Sasq'ets is theirs).

## 4. The roster

### 4.1 Thirty-two peoples

How to read the table:
- **Leanings** are four numbers from −2 to +2 on fixed decision 5's axes, in this order: Immortal/Mortal, Arcane/Mundane, Wild/Civil, Oathbound/Free; positive leans to the first word. They are a people's default, around which each person's own value would be drawn (§10). Batch 1 has none, and they stay in this table until something reads them, since culture-faith already derives each person's ethics from their culture and faith (its §4.6; §9).
- **Touches** names the systems the need runs through. "Culture" means the culture-faith design; "Build", "Zones" and "Schedule" mean the companion designs.
- **Batch:** 0 exists; 1 is built on its branch (§5); 2A to 2E come next (§6, §12), and 2E, the ghoul, comes with the slice of the dead whenever that lands (§12.1); 3 to 6 come later (§12.3). "Kin" shares another people's need (§12.3).

| People | Inspired by | Need, and its twist | Touches | Leanings | Threat form | Batch |
|---|---|---|---|---|---|---|
| Human | Earth, and every world | food, rest, company | — | 0 0 0 0 | the night thief (exists) | 0 |
| Vampire | the blood-drinking dead of Southeastern and Eastern European folklore (strigoi, vrykolakas, upiór) | blood, only given with consent; burns in daylight | Needs, Social, Work, Combat | +2 +1 0 0 | a vampire who feeds without consent (later) | 0 |
| Dryad | the tree nymphs of Greek myth (dryads, hamadryads) | forest close by, and one tree they are bound to | Needs, Work (felling), Mood, Social | +1 +1 +2 +1 | the warden of the wood: a dryad whose own tree was felled (later) | 1 |
| Djinn | folk tales from North Africa to South Asia; older Arabian folklore; the Thousand and One Nights | contracts struck and honoured | Social (debts as terms), Work, Mood | +2 +2 0 +2 | the ifrit: a djinn whose contract was broken | 1 |
| Fae | the Good Folk of Irish, Scottish, Welsh and English fairy lore | cannot lie; their word must stay true | Social (promises, refusals), Mood | +2 +2 +2 +1 | a fair-folk abduction (later) | 1 |
| Undead | the revenants of medieval English and French folklore | a purpose: work, or business left unfinished | Work, Mood, Needs | +2 +1 −1 +2 | the hollow walker: an undead whose purpose is lost (later) | 1 |
| Cairnwake | the barrow-dead of Norse sagas, who guard what was buried with them | a purpose: the watch. *Twist:* it guards the camp's graves as its hoard, so no ghoul eats while it stands watch (with 2E) | Work, Zones, Combat, Social | +2 +1 0 +2 | the barrow lord (the art's Barrow King) | kin of undead |
| Wishwraith | ghosts of unfinished business (Korean and South Asian tales) | a purpose: one wish about someone living, left unfinished at death. *Twist:* only that person can finish it, and then it rests for good, so helping it means losing it | Social, Mood, Growth (wants) | +2 +1 +1 +1 | — | kin of undead |
| Brimmet | river folk with a water-dish crown who must return a bow; healing lake folk (§6.1) | water in the dish on its crown; it returns any bow, and spills. *Twist:* no shell or beak, and its pledges cost it water | Social (the bow), Travel, Combat (weakness), Work (rescue, care), Incident (rain) | 0 +1 +1 +2 | the brimmet at the ford who seizes swimmers | 2A |
| Ghoul | the graveyard ghūl of Arabian folklore; eating to gain what the eaten knew (§6.2) | the flesh of the dead, never the living, eaten only with leave. *Twist:* a code no tale gives it, and the echo of the dead's strongest feeling | Combat and Needs (deaths make bodies), Social (leave, debts, grudges, the echo), Mood (witnesses), Build (graves), Culture (rites) | +1 +1 +1 +1 | the ghoul of the waste, who breaks the code and raids graves | 2E |
| Clobbin | thing-born goblins; night wrestling and tickling contests; the debt repaid every night (§6.3) | contests and play at night; repays a debt over and over. *Twist:* it plays to lose | Social (challenges, pranks), Combat (bouts without wounds), debts (the loop), Work (night foraging for gifts), Needs (set-aside meals) | +1 +2 +1 +2 | none; a slighted clobbin's night of pranks | 2B |
| Listrel | the riddling tale-spirit of the Vetala tales; the moth at the lamp as a visiting soul (§6.4) | new tales heard; rests folded in a standing tree. *Twist:* it hears tales rather than tells them, and the telling reopens old hurts | Social (tales, refusals), Mood (old wounds reopened), Work (felling its tree), Travel, Growth (shared memories) | +2 +2 0 +1 | none | 2B |
| Werewolf | European werewolf tales; the Breton lai of Bisclavret (§6.5) | to run free on moon nights. *Twist:* the wolf carries the heart, going for whoever the person resents and never for a friend | the calendar, Travel, Combat, Social (its grudges choose the target), Mood, Schedule, Zones | −2 +1 +2 −1 | the wolf in the night (the art's wolf) | 2D |
| Tatterlin | household spirits who work unseen for a bowl and leave if paid (§6.6) | a house kept, chores done unseen at night, a bowl filled every night. *Twist:* it wears the household's cloth, and quarrels at its hearth wound it | Needs (rations), Work (night chores), Social (gifts, thanks, quarrels), Incident (spoilage), Build (a house), Schedule (who is awake) | +1 +1 −2 +1 | a tatterlin soured by neglect | 2C |
| Knollet | mound-dwellers who must be asked leave; elf-shot; fairy loans (§6.7) | leave asked when passing its mound. *Twist:* its luck is a loan | Travel (passing), Build and Zones (protected ground), Work (foraging, felling), Social (offence, amends, debts), Needs (the curse), Incident (windfalls) | +1 +2 +1 +1 | the mound's curse; no hostile body | 2C |
| Feeplin | whistling forest folk who take what is left out; the warning not to whistle at night (§6.8) | gifts given; speaks only in whistles. *Twist:* only its go-between understands it, warnings included | Social (whistle-speech, the go-between, debts), Needs (gifts from rations), Combat (warnings), Work (night foraging), Growth (the learned speech) | 0 +1 +2 −1 | small night thieves when gifts lapse | 2C |
| Vellet | seal folk of the northern isles and swan maidens of Eurasian tales, who shed a skin or a feather cloak to walk on land | open water, and its shed skin kept safe. *Twist:* the skin is only ever given, as a pledge to stay (question 6) | Travel, Items, Social (trust, theft), Mood | 0 +1 +2 −2 | none | 3 |
| Fetlow | horse-headed tricksters who lead travellers astray (Philippine tales); the night horse ridden with a bridle of its own hair (Irish tales) | to roam the wild at night, in company. *Twist:* alone, it leads walkers astray; it gives a hair to whoever rides out a night with it, a pledge to come when called | Travel, Items, Social, Combat | +1 +1 +2 +1 | a lone fetlow leading walkers astray | 3 |
| Thrummel | fiddlers of the falls who teach music for a gift (Norwegian and Swedish tales); river singers who draw listeners into the water (European and South American tales) | music by running water, answered. *Twist:* unanswered, it plays the drowning song; answered, it teaches whoever answered, who then lifts the camp at feasts | Social, Culture (festivals), Mood, Travel | +1 +2 +2 0 | the drowning song | 3 |
| Hushtail | shape-changing foxes of East Asian tales, who grow tails with age and keep a pearl; the fox trickster of European fables | to keep its fox nature hidden; a pearl kept safe. *Twist:* a tail grows for every secret it keeps for someone, and falls for every one it betrays | Social, knowledge, Items, Growth (tails) | +1 +2 +1 −1 | a hushtail who trades the secrets it was given | 4 |
| Seldkin | hidden people of stones and hills (Iceland, Scandinavia, the Malay world); the guest of a hidden village who comes home years later | their stones left be; to stay unseen. *Twist:* time runs differently among them, and someone they take may come back after a rewind remembering the timeline it erased | Build, Zones, Social, knowledge, rewinds | +1 +2 0 +1 | someone taken into their village | 4 |
| Turnfoot | backward-footed wild folk whose tracks lead the wrong way (Caribbean, Brazilian and West African tales) | to go untracked: the night, the high forest. *Twist:* it shows its true tracks only to those it trusts, and following one uninvited is the gravest insult | Travel, Combat (pursuit), Social, knowledge | 0 0 +2 −2 | none | 4 |
| Troll | Norwegian, Swedish, Icelandic and Faroese troll tales | to eat like three and sleep under stone. *Twist:* dawn turns it to stone until someone who cares for it calls its name at dusk; uncalled, it stays a landmark | Needs (rations), Build (stone), Map, Mood, Social | +1 +1 +2 −1 | the troll that raids the stores | 5 |
| Dwarf | the master smiths of Norse and Germanic tales | to make one thing with its hands each season. *Twist:* it must give what it makes away, and a made thing kept sours it: the opposite of a hoard | Work (crafting), Build, Items, Social (gifts, debts) | +1 +1 −1 +2 | none | 5 |
| Centaur | the horse-bodied people of Greek myth, wild at feasts | open ground to run; doors, beds and stairs too small for it. *Twist:* it carries no one it has not sworn to, so a carry to safety is an oath | Build, Travel, Social (oaths), Culture (feasts) | 0 0 +1 −1 | a drunken raid | 5 |
| Gorrock | village-tale ogres of Asia, Africa and Europe; the old guest-right, by which whoever has eaten at your fire may not harm your house | to be welcomed: a seat and a share at the fire. *Twist:* fed at a fire, it cannot harm that household; turned away too often, it becomes the raider the tales feared | Social, Culture (welcomes and wards), Combat, Work | +1 +1 +1 −1 | gorrock raiders | 5 |
| Charrow | the fire-dwelling salamanders of Renaissance natural philosophy and older bestiaries | a fire kept burning. *Twist:* its touch chars wood to ash before it catches, so someone else must feed its fire | Work (timber), Build (hearths), Incident (rain), Needs, Social | +1 +2 +1 0 | a wildfire | 5 |
| Rimeling | the snow women of Japanese tales; Slavic and Nordic frost spirits | cold: winter makes it strong, the hearth harms it, and it summers in deep shade. *Twist:* food keeps longer near it, so the camp wants it by the stores while it wants to be by its friends | Needs (seasons), Build (cold rooms), Incident (spoilage), Social | +2 +2 +2 +1 | the freezing breath on winter nights | 5 |
| Knellow | the keening women of Irish and Scottish lore who foretell a death; the washer at the ford | to keen for the dying, whose death it feels coming from the state of the body. *Twist:* a death it keens for can still be turned aside | Mood, Culture (omens), Social, Work (care) | +2 +2 0 +2 | none | 6 |
| Holtward | forest guardians who spare the young and lead greedy hunters astray (Brazilian, Slavic and Baltic lore) | the hunt kept fair. *Twist:* fair hunters find game where it leads them; greedy ones walk in circles until dark | Work (hunting), Zones, Social, Travel | +2 +1 +2 +1 | the warden who leads greedy hunters astray | 6 |
| Kilnborn | clay servants who follow orders to the letter (the Prague tales); the apprentice whose servant would not stop | a task, given by someone, done to the letter. *Twist:* words written on its brow wake it, and whoever wrote them carries the blame for what they said | Work, Social (instructions), Build | +2 +1 −2 +2 | the kilnborn that will not stop | 6 |
| Nightwright | European one-night builders, whose bridge or wall had to stand before cockcrow | a night's building, begun at dusk and done by dawn. *Twist:* it builds for a night's share of food, and what stands unfinished at dawn can never be finished | Build, Schedule, Needs (rations), Social | +1 +1 −1 +2 | none | 6 |

The roster is thin in sub-Saharan Africa and has nothing from Oceania or Indigenous North America. Rule 2 lets a derivative draw on widely shared, non-sacred motifs from any region, so those gaps fill the way any other does, never by reaching for an excluded or closed being.

### 4.2 How each reads at 32–48 px (peoples without a full entry)

ADR-0019's procedural figures are about 39 units tall; every people must read at 32–48 px (BIBLE.md §3). These are the silhouette cues the art builder starts from. Batch 2's are in §6.

| People | Reads as |
|---|---|
| Dryad | a person with a leaf crown and bark-dark arms; the crown thins as their need falls |
| Djinn | a tall figure with no feet: the cloak's hem ends in a curl of smoke; a faint ember glow at night |
| Fae | too tall and too thin, with long fingers and a narrow head; no chest mark, because fae hide what they do |
| Undead and kin | a stiff, slightly forward-leaning stance in grave-grey cloth; a cairnwake broad and swollen, a wishwraith pale with one hand always held out, as if asking |
| Vellet | a person wearing their shed skin as a mantle, spotted grey or white-feathered. Without it they look plainly human, and *that* reads as loss |
| Fetlow | very tall and bony, with a long horse-like head and a mane down the back; knees above the waist when crouched |
| Thrummel | a figure kneeling at running water with a reed fiddle at its chin; seen mostly at night |
| Hushtail | a person whose tails, one for each secret kept, show only when the fox is revealed (batch 4) |
| Seldkin | barely drawn: glimpsed at dusk at the map's edge, gone when looked at |
| Turnfoot | hair to the knees; footprints in the mud that point the wrong way (GroundWear) |
| Troll | twice a person's size (the Barrow King's scale); a lumpy, mossed outline; a stone grey that deepens toward dawn |
| Dwarf | half height and very broad; a long beard; a leather apron, and always something half-made in its hands |
| Centaur | a horse's body under a person's torso, 1.6 tiles long; the only four-legged person |
| Gorrock | huge and stooped, with a heavy brow and long arms; it ducks under every door; muted earth tones |
| Charrow | small and lizard-like on two legs; a glow under the skin that brightens near fire and greys in rain |
| Rimeling | white and still; frost on the ground around its feet; breath that shows even indoors |
| Knellow | a hooded figure at the ford, washing cloth in the colour of the one it keens for |
| Holtward | a small figure in a cloak of living leaves, its face a mask of bark |
| Kilnborn | a squat, kiln-red block with its task written on its brow in the world's own script |
| Nightwright | small and quick, seen only at night beside a half-built wall |

### 4.3 Threat forms

A threat is one person in a situation: a ghoul starving with no one who will give it leave, a tatterlin slighted for a week, a werewolf whose moon night is penned in. It is never the whole people.

- **In code, threats take the thief's path.** `IncidentSystem.SendThief` makes a `CharStatus.Hostile` character with a `Raid` job, and `CombatSystem.Step` handles only `Raid` and `Flee`. So the first threat forms are raid-shaped: they go for a target tile (the stockpile, a body, a grave), are spotted and fought by the watch and the Protective, and flee or fall.
- Threats that are not raids (the brimmet who seizes a swimmer, the fetlow who leads walkers astray) wait for later slices, which extend `CombatSystem` beyond `Raid` and `Flee`.
- **Members of the threat's own people react.** The shame thoughts in §6 are what make a hostile ghoul a story about Morrow, who lives in the camp.
- **Threats can later be talked down** and recruited (§12.4).

Some threats have no people behind them, because the folklore gives them no community life. They are derivatives too, named for what they do:

| Threat | Inspired by |
|---|---|
| the chest-sitter: a weight on a sleeper's chest, and a night's rest lost | the night-mare of Germanic and Slavic lore, and sleep-paralysis tales worldwide |
| the name-caller: a voice at night that calls a sleeper by name, to be followed | Bengali tales of the night voice, and the warning in many lands never to answer it |
| the frost-knocker: waylays walkers on the coldest nights | Balkan and Anatolian midwinter tales |
| the ford-horse: a water horse that carries its riders into the river | Scottish, Scandinavian and Icelandic water-horse tales |
| the canopy-hunter: hangs from forest trees and hooks passers-by | Akan forest tales |
| the false guest: an ogre who passes as a person | Swahili and Kikuyu ogre tales |
| the stand-in seeker: a drowned one who needs someone to take its place | Vietnamese and Chinese tales of the drowned |

### 4.4 Names in earlier drafts

The first draft and the companion designs use tradition names. Each maps here, and the companion designs take the new names when they are next revised. The ghoul, werewolf, troll, dwarf and centaur keep their names and gain a twist.

| Earlier name | Now |
|---|---|
| kappa | brimmet |
| dokkaebi; shurale | clobbin |
| vetala | listrel |
| house spirits (brownie, nisse, tomte, domovoi, kobold, zashiki-warashi); the boggart | tatterlin; a tatterlin soured by neglect |
| duwende, nuno sa punso, duende | knollet |
| mmoatia | feeplin |
| draugr | cairnwake |
| gwisin, bhoot | wishwraith |
| nang tani | dropped: it added nothing beyond the dryad's own bond to one tree |
| selkie, merrow, swan maidens | vellet |
| tikbalang | fetlow |
| nøkk, näcken, fossegrim; iara | thrummel |
| fox spirits (kitsune, huli jing, kumiho) | hushtail |
| huldufólk and hulder; orang bunian | seldkin |
| ciguapa | turnfoot |
| oni | gorrock |
| salamander | charrow |
| yuki-onna | rimeling |
| bean sí | knellow |
| curupira; the leshy and curupira as the dryad's threat | holtward; the dryad's threat is now one of its own |
| golem | kilnborn |
| menehune | nightwright, drawn from European one-night-builder legends instead |
| kalanoro | dropped: it had no design and waited only on readers |
| jiangshi, as the undead's threat | the hollow walker |
| mare, nishi, karakoncolos, kelpie and each-uisge, asanbosam and sasabonsam, zimwi and marimu, ma da and shuǐguǐ | the threat-only table in §4.3 |

## 5. Batch 1: built on its branch

Dryads, djinn, fae and undead are built on the batch 1 branch (`worktree-wf_23280ce9-9be-1`), with one cast member each (Dryope, Qamar, Fionn, Ketil), and recorded in ADR-0022. Batch 1 also built the needs model every later people uses (§8). Batch 2 builds on it and rewrites nothing in batch 1.

| People | Need id and behaviour | What batch 2 uses from it |
|---|---|---|
| Undead | `purpose` (`Natures/Purpose.cs`): restored while doing the one work they are bound to (a Truth); fading harms them | `foodSource: "none"` and `tires: false`; a need bound to a work (`bondWorks`); the player's "never" order as a cause |
| Dryad | `forest` (`Natures/Forest.cs`): living trees within reach feed it, their own tree triple; `JobKind.TendTree` | a need bound to a thing (`NatureNeed.BondId`); the felling hook (`Forest.Felled`), which the listrel's resting tree joins in 2B; `refusesWork`; `EffectKind.BondSevered` |
| Djinn | `contract` (`Natures/Bargains.cs`): restored while a bargain is open | bargains as records (`World.Contracts`; `EffectKind.ContractFormed`, `ContractKept`, `ContractBroken`); consent from state, as for blood; the schedule's bound hours read these records |
| Fae | `courtesy` (`Natures/Courtesy.cs`): moved only by events (words kept or broken, talk, insults) | `cannotLie`, `grudgeDecayEveryDays`, `DebtKind.Welcome` |

- **Common to all four:** `content/needs.json` and its schema (§8.2); `RequiredContent.Needs`, listing each need id with the params its code reads; `arrivalKey`; and cast.json's `arrivalDays` window, drawn after the shuffled wanderers so no other arrival moves.
- **The first draft's plan is withdrawn.** It would have absorbed batch 1 into generic satisfier kinds (species.json v2) and migrated the vampire first, with byte-identical soak logs. Batch 1's model follows ADR-0018 #4's precedent for incidents instead (behaviour named by id, numbers in content, ids checked at load), and neither the rewrite nor the migration would make a new story. Food and blood become data only when something needs them to (hybrids, or the items milestone), and then under §8.3's rule of identical logs.
- **Threats need no extra work.** Batch 1's threat forms (the warden of the wood, the ifrit, the hollow walker, and the cairnwake's barrow lord) are raid-shaped and can follow at any time.
- **At merge** (lead's call). ADR-0022 and a code comment frame the djinn through "Islamic tradition" and call Qamar "the djinn built for Sulayman", and a djinn's arrival line calls it "a being of smokeless fire"; the last two echo scripture. Rule 2 keeps the folk djinn of the tales and the Nights, so these lines change when batch 1 merges. The same review applies rule 6 to batch 1's cast names, which come from real languages (ADR-0025).

## 6. Batch 2: full entries

Eight peoples, chosen for story value first, then code reuse. Every entry has the same parts, ending in hooks for fixed decision 12; the systems each need touches are in §4.1. Thought values are starting points for tuning (ADR-0013), and every new thought uses an existing emote (anger, sweat, hearts, sparkle, alert, gloom, music, none). The brimmet's and the ghoul's code and numbers are in §8 and §9.

Build order (§12): **2A** the brimmet; **2B** clobbin and listrel; **2C** tatterlin, knollet and feeplin (they share the `offering` mechanism, which is deliberate code reuse); **2D** werewolf; **2E** the ghoul, with the slice of the dead (§12.1).

### 6.1 Brimmet (2A)

**Inspired by** river folk who keep their strength as water in a dish on the crown, must return any bow and so spill it, and keep a pledge once fairly bested (Japanese tales); and the lake folk who taught healing to the families that kept faith with them (Welsh tales).

**Ours:** no shell and no beak, but a smooth body mottled like a river stone; a careful, head-level walk, like someone carrying a full bowl; and each pledge sealed by pouring water from its dish into the other's hands (2B), so a promise costs it something before it is kept.

**Body.** Child-sized to small adult, with long webbed hands and a cupped hollow in the crown, rimmed with horn, that must hold water. It lives 80–200 years (data) and eats what the camp eats, berries for now (fish and cress, its delights, come with items). It sleeps at night, beside water when it can, and is sharp-eyed in water, dull in dust. A dry dish, long sun on the dish and fire harm it; it dies of drying out (dish dry, health falls, collapse, death) or of wounds.

**Need: `head_water`**, a need of its nature in batch 1's model.
- **Drain:** steady, and faster in open daylight. A brimmet arrives with a full dish. While critical it loses health (batch 1's rule); at zero it also collapses where it stands, and stays down until someone carries it to water.
- **Met by** standing beside water (the `TendNeed` job: "Wetting their head-dish at the river") and by rain on an uncovered brimmet.
- **Frustrated by:**
  - **a bow.** When someone chats with a brimmet, a Dice draw decides whether they bow. It always bows back, and the dish spills. The bow becomes the cause of the drying that follows.
  - **duties that keep it from the river:** a promise, a patient, the night watch (§8.5 decides which wins).
  - **being downed far from water.**
- **Once the dish is low** it fights at half its hit chance, because the water is its strength, and a rescuer carries it to water rather than to a bed.

**Heart.** It delights in rain on the dish (`dish_rain` +5, sparkle), a full dish at dawn (`dish_full` +3, music), being bowed to, because it loves courtesy even when courtesy costs it (`bowed_to` +2, music), and a fair contest, won or lost. It fears drought and dust, the dish drying in the open (`dish_drying` −6, sweat; `dish_dry` −14, gloom) and fire. It is ashamed to be tricked into spilling by someone who knew (which needs the knowledge layer) and of a broken pledge. It grieves a drowned friend, the more because cultures that fear brimmets blame them for drownings.

**With others.** Courteous to a fault, slow to take offence, and it never forgets a pledge. A species flag, `keepsPledges` (built with the pledge in 2B), puts its pledges ahead of even a critical need: a brimmet keeps its word until its dish runs dry and it falls.

**Story seeds.**
1. The bow at the watch post (§1): Social → Needs → Combat → Work → Social.
2. The pledge (2B, with the contest act):
```
Day 9, 20:15 — Oren beat Teal at wrestling by the river.
Day 9, 20:15 — Teal pledged to tend Oren's wounds whenever he is hurt, and poured water from its head-dish into his hands.
Day 23, 02:55 — Oren was struck down by the thief.
Day 23, 03:05 — Teal left the river, its dish still low, to tend Oren's wounds.
Day 23, 10:35 — Teal's dish ran dry, and Teal collapsed.
Day 23, 11:10 — Mira carried Teal to the river.
Day 23, 11:10 — Teal owes Mira for the rescue.
```
3. The bow as a rescue (the threat form, later), where a player who remembers the Earth tale saves a life:
```
Day 17, 21:05 — Something in the river seized Nia at the ford.
Day 17, 21:06 — Mira bowed to it, and it bowed back; the water spilled from its head-dish.
Day 17, 21:06 — The river brimmet let Nia go and fled.
Day 17, 21:06 — Nia owes Mira for the rescue.
```

**Look and names.** Four-fifths of a person's height and long-armed, its head held level and still while the body moves: the careful walk is the read. The crown's pale disc glints when the dish is full and dulls when it is dry, drawn from the need's value. Mottled olive and slate, darkened, with the role mark on the chest. A brimmet is known by its stretch of water and by a name its elders give after its first fair contest ("Teal of the Deep Bend"), and it signs its pledges with that name.

**Threat form and needs first.** The brimmet at the ford who seizes someone crossing at night needs a combat job beyond `Raid` (later). The people itself needs only batch 1's needs model, since water tiles, rain, rescue and care all exist; the pledge waits for the contest act (2B).

**Growth.** Wants: to be bested fairly; to keep a pledge to the end; to see the river full again. Evolution, on a naming or a kept pledge: *river elder* (its dish holds more, and others bring disputes to it) or *bone-setter* (its care heals more, and when it teaches a human, the human's trait mutation cites the teaching event).

### 6.2 Ghoul (2E, with the slice of the dead)

**Inspired by** the ghūl of Arabian folklore, from pre-Islamic poetry to the Thousand and One Nights and Maghrebi tales: a being of graveyards and lonely roads that eats the dead, which one blow kills and a second revives; and the tales in which whoever eats of a wondrous creature gains what it knew (the salmon of knowledge, Fáfnir's heart). It has no religious framing, no names from a real language and no desert dress (rules 2, 4 and 6).

**Ours:** the code (it never kills to eat, and eats a camp member only with leave from the dead's friends); the echo (its first meal from a member passes it the dead's strongest feeling about someone living); and a second name taken from the first of the dead it was trusted with.

**Body.** Gaunt, long-armed and stooped, with eyes that catch the light at night. It lives 150–300 years (data), and only the flesh of the dead nourishes it. It sleeps by day (the sun does not harm it; it is simply made for the night) and smells death from far off. Hunger, wounds and fire harm it; it dies of wounds, or slowly of hunger, the slowest of any people: days between meals.

**Need: `dead_flesh`.** It replaces food (`foodSource: "none"`, batch 1's rule). §8.11 has the rules in full.
- **Met by** a portion of a body (three to a body) or of an old grave. Two old graves lie by the ruins, placed from Truths; they feed a newcomer for about twelve days, and then the camp's own dead become the question.
- **Leave.** At the first dusk after a death it asks each of the dead's friends (every member whose opinion of the dead was 30 or more, the line `friend_died` already uses; friendship stands in for kin until there is a family graph): "Morrow asked Mira's leave to eat what was left of Bram." A friend answers from state, never from Dice, as with consent to give blood. Granted, the ghoul eats and owes the friend (`DebtKind.Leave`); any refusal closes that body to it. With no living friends no leave is needed ("nobody had loved Hollis enough to say no"), and a dead thief belongs to nobody.
- **The echo.** Its first meal from a member moves its opinion of one living person by half the dead's opinion of them, citing the meal: "Morrow carries Bram's grudge against Nia." Leave given is also a feeling handed on.
- **Frustrated by** refusal while starving. A ghoul who is Desperate (the trait that takes others' meals) goes to the body in the night anyway. Each friend finds out on waking ("Mira found what had been done to Bram") and usually forms a grudge, and the ghoul carries the shame.
- **Burial closes the body** (lead's call; the content line is question 4). A grave waits for the asking, a buried member is never eaten with leave, and digging up a grave at night is the worse offence. A culture that burns its dead leaves the ghoul nothing (culture-faith's death rites).

**Heart.** It delights in leave freely given (`fed_with_leave` +8, hearts, with warmth toward the giver), the quiet of night and being trusted with the dead. It fears fire, being hunted and being seen eating. It is ashamed of eating without leave (`fed_without_leave` −10, gloom, five days) and of being called a scavenger to its face. It grieves its friends like anyone and hungers at the same time, and the inspector shows both, "A friend died (−25)" and "The dead are calling (−6)". That pair is the people.

**With others.** Patient, nocturnal and watchful: good on the night watch, uneasy around fires and feasts. Someone who sees a ghoul eat gets `saw_dead_eaten` (−6, gloom, by default) through their culture's attitude toward ghouls (culture-faith §5.10), and takes it harder in a culture that fears them.

**Story seeds.**
1. What was left of Bram (§1): Combat → Social → Needs → Social.
2. Nobody to say no:
```
Day 33, 10:00 — Nia starved to death.
Day 33, 20:00 — With nobody left who had loved Nia to say no, Morrow ate what was left of her.
Day 33, 20:05 — Sera saw Morrow at Nia's body and was sickened.
Day 34, 09:30 — Sera insulted Morrow.
Day 35, 11:00 — Morrow now holds a grudge against Sera.
```
3. The echo:
```
Day 12, 19:40 — Oren gave Morrow leave to eat what was left of Hollis.
Day 12, 19:40 — Morrow owes Oren for the leave.
Day 12, 20:30 — Morrow ate what was left of Hollis, and carries Hollis's grudge against Nia.
Day 15, 09:10 — Morrow insulted Nia.
Day 15, 09:10 — Morrow now holds a grudge against Nia.
```

**Look and names.** The art's existing ghoul (`FigureLook.Ghoul`): hunched, a ragged pointed hem, grey-green cloth, the head below the line of the shoulders, arms past the knees, and two pale points for eyes at night while it is awake. It eats in the existing crouch over the body, and the view draws the remains smaller as portions go, stylised (a covered shape, then bones), never a close-up. A ghoul takes a second name from the first of the dead it was trusted with ("Morrow, who was trusted with Hollis"). Friends use the first name; the second is spoken at its own death.

**Threat form and needs first.** The ghoul of the waste, who has broken the code, is raid-shaped: it comes at night for a body or a grave and is fought like a thief (later: strike it once, and a second blow revives it). The people needs deaths common enough to feed it, meaning aging and multi-year worlds (§12.1 gives the numbers), and comes as one slice with the build design's graves and laying to rest (its slice 8.4), culture-faith's death rites (its slice 2) and the Zones burial ground, timed once as culture-faith's question 5. A dead `Character` keeps its `Pos` and stays in `World.Characters`, so its core needs no new body state; the old graves are new map features (§8.11).

**Growth.** Wants: to be buried, not eaten, when it dies; to be trusted with the dead. Evolution: *undertaker* (keeps the camp's dead; the grief of the dead's friends eases sooner once it has tended the body) or *the hungry one* (a decline: a ghoul refused too often begins to wait on the dying).

### 6.3 Clobbin (2B)

**Inspired by** household things that wake as goblins after long use (Korean and Japanese tales); goblins who challenge night walkers to wrestling and song (Korean tales) or to tickling contests (Tatar and Bashkir forest tales); and the goblin who came back every night to repay a debt, forgetting he already had (a Korean tale).

**Ours:** it plays to lose (a bout lost meets its need in full and a bout won only in part, so it seeks out the strong and is bored by the weak); and it wears its thing's grain, straw-shaggy if broom-born, wood-grained if pestle-born, clay-rough if pot-born, with no club, horns or tiger skin (rule 3).

**Body.** Broad, squat and very hairy, with big hands and a wide grin; strong for its size. It wakes from an old household thing and lives as long as the thing lasts (100–400 years, data); a dead clobbin falls back into its thing, and a worn broom lies where it fell, a lasting mark on the map. It eats what the camp eats (porridge and strong drink, its delights, come with items), sleeps by day and wakes at dusk. Wounds harm it, and so does fire, because its thing burns.

**Need: `play`.**
- **Drain:** 30 an hour while awake; low below 300, critical below 150. At zero there is no harm, but its mood falls fast and the mischief starts.
- **Met by** the `contest` act at night, a challenge to wrestle, sing or guess. The partner accepts or refuses from state (awake and rested, their mood, their opinion of the clobbin). The bout is a few Dice draws (`DiceId.ContestBout`), with no wounds, only rest spent. A loss restores more than a win (`lossRestore`, `winRestore`). The loser is teased; clobbins lose gladly, a Proud human does not.
- **A clobbin that loses leaves the winner a gift at dawn.** It forages in the night and sets the yield aside in the stockpile for the winner, as a `Reservation`. Nothing appears from nowhere.
- **The repayment loop.** When a clobbin repays any debt, it keeps repaying. Each night a Dice draw (`DiceId.RepayAgain`) decides whether it remembers that it already has; if not, it sets aside another share for the creditor, who grows rich in set-aside meals, which matters in winter.
- **Frustrated by** being refused or ignored too long. It then plays pranks (the act `prank`): scattering the timber stack so it must be hauled again, souring a berry, hiding a bedroll. Each prank cites the refusals before it, so refusals lead to a prank, an insult and a grudge.

**Heart.** It delights in a good bout, best of all one it loses, in a song, in being outwitted cleverly (`outwitted` +4, sparkle) and in a full stomach. It fears its thing thrown away, fire and being forgotten. It is ashamed to lose to a cheat or be caught in its own trick. It would grieve the death of whoever wore its thing smooth (a Truth, for later).

**With others.** Loves company, adores the bold, teases the proud and punishes the greedy: a clobbin that sees someone take another's meal (`event.social.took_meal`) pranks the taker.

**Story seeds.**
1. The endless repayment. It also answers a known seam, debts that nobody returns to (ADR-0018):
```
Day 8, 21:00 — Bram brought Jink a meal.
Day 8, 21:00 — Jink owes Bram for the meal.
Day 9, 05:00 — Jink repaid Bram.
Day 10, 05:00 — Jink, forgetting, repaid Bram again.
… and the same every night, until Day 30.
Day 34, 03:00 — Nia took the meal set aside for Bram.
Day 34, 03:00 — Bram now holds a grudge against Nia.
```
2. Refused all night:
```
Day 20, 23:00 — Jink challenged Oren to wrestle. Oren, exhausted, refused.
Day 21, 00:30 — Jink challenged Hollis to wrestle. Hollis refused.
Day 21, 03:00 — Jink, bored and slighted, scattered the timber stack.
Day 21, 07:10 — Hollis insulted Jink.
Day 21, 07:10 — Jink now holds a grudge against Hollis.
```
3. The broom by the hearth (keeping it is a later beat; a kept broom might wake again as the same clobbin with its memories gone, under fixed decision 12's memory rules):
```
Day 25, 02:20 — Jink fought the thief.
Day 25, 02:31 — Jink was struck down by the thief.
Day 25, 02:40 — Jink died, and fell back into an old broom.
Day 25, 07:00 — Wren kept the broom by the hearth.
```

**Look and names.** A wide, squat, shaggy outline; a big round head with a wide grin; hair or hide showing its thing's grain; muted ochre and earth tones. A clobbin is named for the thing it came from and the house that thing served ("Broom-of-the-Third-House"), and whoever beats it gives it a new use-name.

**Threat form and needs first.** None that is hostile: a slighted clobbin's night of pranks is an incident. It needs the `contest` and `prank` acts, new code running on existing systems (Social, Combat's roll pattern, and the set-aside meals in Rations).

**Growth.** Wants: to find someone who can beat it; to be remembered by the house. Evolution: *gift-bringer* (its gifts come more often, and its pranks are worse) or *house goblin* (settles into one family's service and becomes kin of the tatterlins).

### 6.4 Listrel (2B)

**Inspired by** the spirit of the Indian Vetala tales, who hangs in a tree and trades in tales and riddles; and the belief, found in many lands, that a moth at the lamp is a soul come to visit. Nothing is drawn from the Vetal worshipped as a village god in parts of the Konkan coast, and there are no cremation-ground rites and no king from the tales.

**Ours:** it must hear tales rather than tell them, and telling it an old hurt reopens the hurt; and it has a moth's body and a moth's trouble, drawn every evening to the hearth where tales are told while fire is what it fears. It rests folded among a tree's branches like a cocoon.

**Body.** Tall, light and pale, with long hair falling forward and a dusty mantle like folded wings; cold to the touch. It lives 200–600 years (data) and does not age; it fades when unheard. It eats nothing, rests only folded among the branches of a standing tree and barely rests elsewhere. It hears whispers and remembers every tale exactly. Fire, its tree felled while it rests there and long silence harm it; it dies of fire or wounds.

**Need: `tales`.**
- **Drain:** 25 an hour; low below 300, critical below 120. At zero it fades: no harm, but its mood falls to the floor and it may leave.
- **Met by** the `ask_tale` act. It asks someone to tell what happened to them: a recent event that names them, carries a persistent effect (a grudge, a debt, a death they saw, a departure), and that the listrel has not heard. The partner tells it, or refuses, from state (mood, opinion). A new tale gives +300, and the listrel remembers the event id in a short list on the person (fixed decision 12's `memories[]`).
- **The cost to the teller: telling an old hurt reopens it.** The sim re-adds the matching thought about the same person, citing the original event (Bram's `meal_stolen` about Nia comes back). That can push an opinion past the grudge line, and the grudge then cites both the telling and the theft. Old wounds reopen without any knowledge layer.
- **Rest** is at a place: a standing tree within one tile, the one it last rested in. If someone fells that tree, a reaction to `event.work.felled_tree` gives it grief and a grudge against the feller.

**Heart.** It delights in a new tale (`heard_a_tale` +6, sparkle), a clever listener and a riddle nobody can answer. It fears silence, being forgotten and fire. It is ashamed to retell a tale wrong. When the last person who knew a tale dies, it mourns the tale (`a_tale_lost` −8, gloom).

**With others.** Curious and intrusive. It asks about painful things, so a refusal may come with an insult. Loyal to those who tell it good tales.

**Story seeds.**
1. Old wounds (the chain runs back to the theft and the winter that caused it):
```
Day 21, 22:00 — Ossa asked Bram to tell how his meal was taken.
Day 21, 22:05 — Bram told the tale, and it all came back to him.
Day 21, 22:05 — Bram now holds a grudge against Nia.
```
2. The resting tree:
```
Day 16, 10:30 — Sera felled a tree for timber.
Day 16, 10:30 — Ossa's resting tree is gone.
Day 16, 10:30 — Ossa now holds a grudge against Sera.
```
3. The tale only Hollis told:
```
Day 12, 21:30 — Ossa asked Hollis to tell of the night the thief came. Hollis told the tale.
Day 30, 04:00 — Hollis starved to death.
Day 30, 04:00 — Ossa mourned the tale only Hollis had told.
Day 31, 20:00 — Ossa told Mira the tale of Hollis's last night.
Day 31, 20:05 — Mira, who had loved Hollis, insulted Ossa for telling it.
Day 31, 20:05 — Mira now holds a grudge against Ossa.
```

**Look and names.** Tall, thin and pale, in a mantle like folded wings that catches the firelight from the edge of the hearth; at rest, a pale bundle folded into a tree's crown, a shape no other people has, readable at 32 px. Its eyes are closed while it listens. A listrel is known by its tree and by the tale it keeps best ("Ossa of the river oak, who keeps the tale of the thief in the rain"), and when its tree falls it takes a new name from the next one.

**Threat form and needs first.** None in batch 2. It needs the `ask_tale` act, which reads the event log and renews thoughts; rest gains an optional place, using the brimmet's `TendNeed` job (§8.3), and everyone else keeps resting anywhere.

**Growth.** Wants: to hear the tale no one will tell; to keep the camp's chronicle. Evolution: *chronicler* (a court role; its tales lift the court's mood at the hearth) or *gossip* (a decline: it starts telling tales to the wrong people, which needs the knowledge layer).

### 6.5 Werewolf (2D)

**Inspired by** werewolf tales across Europe, from the Greek lykanthropos to the Slavic vukodlak, in which a wolf-skin, a belt or a curse brings the change in older tales and the full moon in later ones; and the Breton lai of Bisclavret, whose wolf was gentle with its friends and fell on the wife who betrayed it.

**Ours:** the moon is a ten-day clock the whole camp can see; and the wolf carries the heart. Changed in camp, it goes for whoever the person holds a grudge against (else whoever they think least of, else the stockpile); it never strikes someone the person counts a friend, and it fights for them.

**Body.** A person, usually born human and sometimes of a wolf-blooded line, who turns wolf for a night. Human lifespan; eats what the camp eats, and as a wolf it hunts (later, with animals). It sleeps at night, restless before the moon, and has keen smell and hearing even in human shape (later, an early warning of a thief). Being shut in on a moon night harms it, being seen changing shames it, and silver will harm it once items exist. A wolf killed is the person killed.

**Need: `moon_run`.**
- **When it bites:** silent between moons. The calendar gains a moon (`world.json`): a ten-day cycle, so a full moon every season. On a moon night the need drains fast, 200 an hour from dusk.
- **Met by running:** hours spent beyond a set distance from the hearth (the place mechanism, in a `roam` form). Meeting it in full takes a night away; the werewolf comes back at dawn, tired.
- **Frustrated by being kept in camp:** by a duty, a promise, the Schedule choice "keep them in" on moon nights, or a closed room set aside in Zones. Below critical, the change comes where it stands. For the rest of the night the person is a wolf: still a member, but flagged `Changed`, drawn as the art's wolf and given a raid-shaped job on its target. `CombatSystem` treats a changed member as a raider, and the camp fights it as it would a thief. At dawn the flag clears, and every hurt done in the night carries over as thoughts, grudges and debts.

**Heart.** It delights in the run, the moon and the smell of rain (`ran_the_moon` +8, sparkle). It fears being penned, hurting a friend, and being watched while changing (`seen_changing` −8, sweat). It is ashamed of the morning after a wild night (`woke_bloodied` −12, gloom), and grieves anyone it hurt.

**With others.** Pack loyalty: it bonds faster with a chosen few (chats with them warm twice as much) and is wary of strangers. The curse is no secret in batch 2, which has no knowledge layer; it becomes one in batch 4.

**Story seeds.**
1. Kept in. The player's schedule is the first link, as the ration policy is today, and an old grudge chooses the victim:
```
Day 9, 08:30 — Sera insulted Hask.
Day 9, 08:30 — Hask now holds a grudge against Sera.
Day 14, 19:00 — The moon rose full.
Day 14, 19:05 — Hask stayed on watch through the moon night, as the schedule asked.
Day 15, 01:30 — Hask changed, and the wolf went for Sera, whom Hask had not forgiven.
Day 15, 01:31 — Sera was struck down by the wolf.
Day 15, 06:00 — At dawn the wolf was Hask again.
Day 15, 06:00 — Sera now holds a grudge against Hask.
```
2. The wolf and the thief:
```
Day 24, 19:00 — The moon rose full, and Hask ran into the hills.
Day 25, 02:10 — A thief struck Mira down at the stockpile.
Day 25, 02:12 — The wolf that was Hask came out of the dark and fought the thief.
Day 25, 02:20 — The thief fled, wounded.
Day 25, 02:25 — The wolf that was Hask dragged Mira back to the fire.
Day 25, 02:25 — Mira owes Hask for the rescue.
```
3. The winter moon:
```
Day 34, 19:00 — The moon rose full over the frost.
Day 34, 19:10 — Hask, starving, ran into the hills.
Day 35, 06:00 — Hask did not come back at dawn.
Day 35, 12:00 — Hask left the camp.
```

**Look and names.** In human shape, lean and rangy, in a heavy fur mantle whose hood carries two wolf ears. As a wolf, the art's existing wolf at one tile, the coat darkened, with a scrap of the person's cloth colour at the neck so the player knows who it is. A werewolf takes its culture's names, plus a pack name given on its first run and used only by those who run with it.

**Threat form and needs first.** The wild wolf of the moon night (raid-shaped, the art's wolf), whether a stranger or a member who was penned in. The people needs:
- a moon in the calendar (`world.json`, a schema change);
- a `Changed` flag for the night, rather than a status flip. Setting the status to Hostile would drop the wolf from `World.Members()`, so needs, mood, rations and talk would all skip it, and `Lifecycle.Die` would treat a werewolf killed in wolf form as a stranger nobody mourns. The flag keeps the person a member while `CombatSystem` treats it as a raider, choosing its target from the person's opinions. This is new, and it touches Combat and Social;
- the Schedule and Zones seams for the player's levers. The need works without them; the werewolf simply goes.

**Growth.** Wants: to be rid of the wolf, or to master it. Evolution, on a milestone (a moon run shared with a chosen companion, or a founding): *moon-tamed* (the change waits for its will) or *wolf-blooded* (stronger runs and longer nights away; harder to keep in).

### 6.6 Tatterlin (2C)

**Inspired by** household spirits who do chores unseen for a bowl left at the hearth, leave at once if paid, and turn spiteful when neglected (British, Scandinavian, East Slavic and German tales); and the house that falls to ruin when its child-spirit leaves (northern Japan).

**Ours:** it wears the household, a patchwork of scraps of each member's cloth, so its look changes as people come and go; quarrels at its hearth wound it; and its bowl is its ration, so the ration policy decides whether it is filled.

**Body.** Small (two-thirds of a person or less), soot-dark, ember-eyed and long-armed; very strong for its size. It lives as long as its house lasts (200–500 years, data). It eats only what is left for it, a bowl at the hearth each night, and cannot take food from the stockpile itself. It sleeps by day in a corner or behind the hearth, and knows everything that happens in its house. An unkept house, being watched at work, being paid, quarrels at its hearth and a house abandoned all harm it. It does not starve to death: with no food or no house, it leaves.

**Needs.**
- **Food, through the `offering` mechanism:** a member leaves one share of berries in the bowl at the hearth each night, as a new small job ("Filling the bowl at the hearth"). The tatterlin stands in the ration line like anyone else, so the ration policy decides whether its share is set aside (the winter story in §1). An empty bowl counts as "went without", with its own text.
- **`house_kept`:** met while the camp has a roofed shelter with a hearth (today's shelter with at least one sound roof section), and while it can do chores at night unwatched: hauling and roof repair, existing work, done by night. Drained when someone awake watches it work (an awake member within a few tiles), when the shelter is in ruins, and by each insult traded inside the shelter. A night watch set for every night leaves it no unwatched hour, so the player's schedule can be the first link.
- **Payment ends it.** A Generous member who finds work done in the night may leave a gift by the hearth (the act `leave_thanks`). The tatterlin takes it as wages and leaves at once. A kindness ends a relationship, and the log says so plainly.
- **Frustrated by** two empty bowls, a week with no unwatched hour, or a quarrelling house. It then turns spiteful for a night: it sours food (a cause for spoilage), scatters stock, spoils a sleeper's rest or hides a quarreller's things. Each prank cites what slighted it.

**Heart.** It delights in a well-kept house (`house_in_order` +4, music), a full bowl and a sleeping household. It fears being seen, the house falling to ruin, and fire. It is ashamed to be paid as if it were a servant, or watched (`seen_at_work` −6, sweat). It grieves its household dying or leaving (`house_emptied` −20, gloom).

**With others.** Attached first to the house, then to its people. It dislikes idlers and quarrellers and is fond of children (there are none yet). When the camp moves (later), it follows only if embers from its old hearth are carried to the new one.

**Story seeds.**
1. The empty bowl (§1): World → Needs → Incident → Needs → Social.
2. The thank-you:
```
Day 18, 06:00 — Sera found the roof mended in the night.
Day 18, 06:05 — Sera left a gift by the hearth for whoever had mended it.
Day 18, 06:05 — Pim took the gift as wages, and left the camp.
```
3. The quarrel at the hearth:
```
Day 19, 21:10 — In the shelter, Oren insulted Sera.
Day 19, 21:12 — Sera insulted Oren.
Day 20, 02:00 — Pim, troubled by the quarrel at the hearth, hid Oren's bedroll.
Day 20, 07:30 — Oren insulted Pim.
Day 20, 07:30 — Pim now holds a grudge against Oren.
```

**Look and names.** Very small (0.6 of a person) and hunched, soot-dark with two ember points for eyes, wrapped in a patchwork drawn from the members' cloth colours; a scrap fades when its owner leaves or dies. Seen mostly at night, by the hearth. A tatterlin is known by its house ("the one of the river shelter") and rarely says its own name. Being named by the household is fixed decision 12's naming trigger for it.

**Threat form and needs first.** A tatterlin soured by neglect plays pranks; it does not raid. It needs the `offering` mechanism and the bowl job; the `leave_thanks` act; night chores with a check for watchers; and insults counted by where they happen. It is better with Build (a house of its own choosing) and Schedule (who is awake), but it runs on today's shelter.

**Growth.** Wants: a house worth keeping; to be given a name. Evolution, on a naming: *hearth-warden* (the house it keeps holds out against rain and spoilage) or *the soured one* (a decline after neglect).

### 6.7 Knollet (2C)

**Inspired by** small beings of mounds, corners and old trees who must be asked leave and who sicken whoever disturbs their home (Philippine tales of the old one of the mound; the house goblins of Spain, Portugal and Latin America); Irish fairy forts, never dug or built on; elf-shot, the sudden sickness old English charms blamed on offended elves; and fairy loans that must be repaid (Irish and Scottish tales).

**Ours:** its luck is a loan, so a windfall found near its mound is a debt and a debt left unpaid is an offence; it is root and moss rather than cap and beard; and its courtesy is the world's own, asked in the world's words, with no real phrase of asking leave and no real folk healer.

**Body.** Tiny (knee-high), wizened, root-skinned and moss-backed, with a face like a knot of root; it lives inside a mound. It lives 300–600 years (data) and eats a little of what is offered or foraged. It sleeps by day inside its mound and feels every footstep on its ground. Its mound broken, built over or dug harms it, and so does rudeness. It rarely dies; a destroyed home drives it away.

**Need: `leave_asked`.**
- **Drain:** slow, 10 an hour; courtesy keeps it up.
- **Met by** someone asking leave while passing within a tile of its mound (the act `ask_passage`, part of travel; people whose culture keeps the custom do it unprompted, others once a member who keeps it has taught them), and by an offering at the mound (the `offering` mechanism the tatterlin uses, at a different place).
- **Frustrated by** walking past or across without asking, and by foraging, felling or building on or next to the mound. Members who keep the custom refuse to build on a mound (fixed decision 2's refusal).
- **The curse:** each offence can bring it (`DiceId.CurseRoll`). The offender sickens (a `cursed` thought, and a small loss of health and rest every hour) until amends are made: an offering at the mound and an asking of leave, by the offender or by someone on their behalf, who is then owed a debt.
- **The loan:** while the knollet is content, a forager near its mound sometimes finds a bush heavier than it should be (`DiceId.Windfall`). The extra is real forage, and the forager owes the knollet for it (`DebtKind.Luck`). An offering at the mound within ten days repays it; past that, the unpaid debt counts as an offence and cites the windfall.
- **Mounds** are map features placed from Truths: a few in the grass, never on paths the camp needs.

**Heart.** It delights in being greeted (`greeted` +3, music), offerings, a respectful household and a loan repaid. It fears fire, digging and ploughs (later). It admits to no shame. It grieves its mound destroyed (`home_destroyed` −25, gloom).

**With others.** Quick to take offence and quick to forgive after amends; generous to those who keep the courtesy, and a hard creditor to those who do not.

**Story seeds.**
1. The shortcut:
```
Day 6, 10:20 — Hollis cut across Burr's mound without asking leave.
Day 6, 10:20 — Burr cursed Hollis.
Day 7, 08:00 — Hollis is sick with the mound's curse.
Day 8, 14:00 — Hollis was hurt in an accident while working.
Day 9, 19:00 — Bram left an offering at the mound and asked leave for Hollis.
Day 9, 19:00 — Burr lifted the curse.
Day 9, 19:00 — Hollis owes Bram for the amends.
```
2. The wall:
```
Day 28, 09:00 — You marked out a wall across the mound by the stockpile.
Day 28, 09:30 — Mira refused to build on the mound.
Day 29, 15:00 — Oren built the wall.
Day 29, 15:00 — Burr left the camp, and the camp's luck went with him.
```
3. The loan:
```
Day 11, 09:00 — Sera found the bush by Burr's mound heavy with berries.
Day 11, 09:00 — Sera owes Burr for the windfall.
Day 21, 09:00 — Sera never repaid Burr, and Burr cursed her.
Day 23, 09:00 — Sera, sick with the curse, refused to work.
Day 25, 18:00 — Sera insulted Burr.
Day 25, 18:00 — Burr now holds a grudge against Sera.
```

**Look and names.** Tiny (0.45 of a person), root-skinned with a moss-green back and a staff of twisted root. What reads at 32 px is the mound: a low earthen dome with a dark opening, and a lantern-sized glow at dusk while its owner is awake. A knollet is never addressed by its own name, only by an elder's title in the world's language; if it ever gives its true name, that is a sign of great trust (a fixed decision 12 naming trigger).

**Threat form and needs first.** The curse is its only threat; there is no hostile body. It needs mound map features (Truths), the `ask_passage` check in travel, the curse, the loan, and the `offering` mechanism (shared with the tatterlin). It is best with Build and Zones (protected ground, refusals), which the companion designs supply.

**Growth.** Wants: a household that keeps the courtesy; its mound left whole. Evolution: *luck of the house* (moves into the camp's corner; its windfalls come more often and ask less) or *the old one* (a decline: it curses harder after repeated offence).

### 6.8 Feeplin (2C)

**Inspired by** small forest folk who speak in whistles, are seldom seen, and take the fruit and drink left out for them (West African tales); the fairies who shared their lore only with those who kept faith with them (Irish and Welsh tales); and the warning, found in many lands, not to whistle at night because something answers.

**Ours:** only a go-between understands it, and all the camp's dealings with it pass through that one person; its whistles are warnings that only the go-between can read; and it has big ears rather than backward feet (those are the turnfoot's). It teaches no herb lore, so no real healer is drawn.

**Body.** Small (knee-high or less), round-headed and long-armed, with ears like broad leaves. It lives 100–300 years (data) and eats fruit (berries for now). It rests in thick cover by day and forages at dusk and dawn; it is very sharp-eared, and sharp-eyed at night. Capture, sticky things and open, bare ground harm it; it dies of wounds.

**Needs.**
- **`gifts_given`:** drains 12 an hour, and is met by an offering at the forest edge, one share of berries left by a member (the `offering` mechanism). Gifts flow back: a feeplin that is given gifts forages at dusk and sets the yield aside for the giver.
- **Whistle-speech.** Only someone who understands it can meet a feeplin's need for company; everyone else gets "could not understand each other" instead of a chat. Understanding is learned through patient contact, deep talks and gifts with the same feeplin over days: a flag on the person, `understands: [feeplin id]`, earned by experience in the shape of fixed decision 12's mutations. The one who learns it becomes the go-between, and everyone's dealings with the feeplin pass through them: requests, debts, warnings.
- **Warnings.** A feeplin awake at night that spots a raider whistles. If its go-between is within earshot, awake or asleep, they wake and rouse the watch; if not, nobody understands the whistle.
- **Frustrated by** no gift for ten days, when it takes what is not given (a small night theft from the stockpile; whoever's set-aside meal it took forms a grudge), and by losing its go-between to death or departure, when its need for company can no longer be met and it leaves within days.

**Heart.** It delights in a gift (`gift_received` +5, hearts), being understood (`understood` +4, music) and music. It fears capture, sticky things and open ground. It is ashamed to be tricked, or to have whistled a warning nobody understood. It grieves the loss of its go-between (`go_between_lost` −20, gloom).

**With others.** Shy with strangers, devoted to its go-between, mischievous toward the stingy.

**Story seeds.**
1. Nobody left who understood:
```
Day 10, 21:00 — Nia came to understand Lark's whistling.
Day 36, 17:00 — Nia left the camp.
Day 36, 17:00 — Nobody left in the camp could understand Lark.
Day 38, 06:00 — Lark, unheard, left the camp.
```
2. The warning:
```
Day 19, 02:05 — Lark whistled a warning into the dark.
Day 19, 02:06 — Nia understood, and woke Bram on watch.
Day 19, 02:15 — Bram spotted a thief at the stockpile.
Day 19, 02:30 — Bram drove the thief off.
Day 19, 02:30 — Bram owes Lark for the warning.
```
3. The stingy camp:
```
Day 22, 02:00 — Nobody had left Lark a gift in ten days.
Day 22, 02:10 — Lark took the meal set aside for Hollis.
Day 22, 07:00 — Hollis now holds a grudge against Lark.
```

**Look and names.** Very small (0.5 of a person), round-headed and long-armed, with broad leaf-like ears that turn toward sounds. It shows the music-note mark when it whistles to its go-between. Its name is whistled and cannot be spoken; humans use a spoken use-name that the go-between gives it (a fixed decision 12 naming trigger).

**Threat form and needs first.** A small night thief of the stores when gifts lapse (raid-shaped). It needs the `offering` mechanism, shared with the tatterlin and the knollet; a whistle-speech filter in social checks and the go-between flag; and the warning, passed from the feeplin's spotting through the go-between.

**Growth.** Wants: to be understood by more than one person; a gift given without being asked. Evolution: *forest-teacher* (teaches its speech to several people, so the camp has more than one go-between) or *the unseen* (withdraws, and is seen only by its go-between).

## 7. How the player meets them

Peoples add no button to the bottom bar and no panel. They arrive through the incidents that already exist, show their needs on the map and in the inspector, and change the stories in the Chronicle. The player's levers stay intentions: work priorities and the ration policy today; standing orders, schedules, zones and plans later. Never "Morrow, eat that" or "Teal, go to the river" (fixed decision 2).

### 7.1 Someone new walks in

A wanderer of a new people arrives like any wanderer. Where they enter is data: `arrivesFrom` is `edge` by default; a brimmet climbs out of the river onto the bank. They walk to the hearth and are asked to stay. If the camp cannot meet one of their needs right now, the inviter promises it (§8.8).

```
 . . . . . . . . . . . . ≈ ~ ~ ~
 . . H ← ← ← ← ← ← ← ← T ≈ ~ ~ ~      T   Teal climbs out of the river
 . . . . . . . . . . . . ≈ ~ ~ ~      H   the hearth, where Teal is asked to stay
 . . . . . . ♣ . . . . . ≈ ~ ~ ~      ≈   bank tiles: standing here fills the dish
```
```
Day 12, 15:00 — Teal climbed out of the river, hungry and alone.
Day 12, 15:40 — You welcomed Teal into the camp.
```

### 7.2 Clicking someone

The inspector keeps its shape. Its needs come from the species, in the order batch 1's `StateApi` gives them: Food or Blood (none for a people that eats nothing from the stores), then the people's own needs with a one-word stage (the ghoul's reads "Hunger for the dead"), then Rest (none for a people that never tires), Company and Health. Hovering the species word shows the resemblance line (§3, rule 5) and what the people needs, in one plain line each.

```
┌──────────────────────────────────────┐
│ Teal                               × │
│ Brimmet · Lives in the camp          │ ← hover "Brimmet":
│ Now: Wetting their head-dish at the  │     Earth folklore tells of something like
│      river                           │     them: river folk who keep their strength
│ Needs                                │     in a dish of water on the head.
│  Food       ███████░░░  712          │     Needs water in its head-dish every day,
│  Head-dish  ██░░░░░░░░  212 drying   │     and always returns a bow.
│  Rest       █████░░░░░  540          │
│  Company    ██████░░░░  601          │
│  Health     ██████████ 1000          │
│ Mood        ████░░░░░░   41          │
│ Traits  Oathbound, Generous          │
│ Recent thoughts                      │
│  My head-dish is drying (−6)         │
│  Was bowed to (+2)                   │
└──────────────────────────────────────┘
```

### 7.3 Seeing a need on the map

- **Everything shown is drawn from sim state** (ADR-0019): the brimmet's dish glints when full and goes dull when dry; the sweat or gloom mark comes from the need's thoughts; the ghoul crouches at a body at night, its eyes two pale points, and the remains shrink as portions go; the listrel rests folded into its tree; a tatterlin's patchwork shows who is in the household.
- **The People list** shows each person's job in plain words: "Teal — Wetting their head-dish at the river", "Morrow — Asking Mira's leave".
- **Hovering a tile** names what is there: "What was left of Bram", "An old grave, its name long gone (dug into)", and later "A mound (someone lives here)".

### 7.4 The player's levers

- **Today:**
  - **Work priorities.** A brimmet set to keep watch first stands far from the river all night. That choice can be the first link in its story.
  - **The ration policy.** It decides whether a tatterlin's bowl is filled.
- **Later, as standing orders** (a new command, `SetStandingOrder`, in slice 2C). An order appears in the Orders list only while it matters:

```
┌ Orders ─────────────────────────────────────────────────────┐
│ When food runs short                                         │
│  (•) The hungriest eat first   ( ) Promised meals come first │
│  ( ) You eat first                                           │
│ Pim's bowl at the hearth                    (a tatterlin lives here)
│  (•) Every night   ( ) When there is plenty   ( ) Never      │
│ When someone dies                           (a ghoul lives here)
│  (•) Ask their friends first   ( ) Bury them at once         │
│ This only sets what the camp intends. People still choose.   │
└──────────────────────────────────────────────────────────────┘
```
- **"When someone dies"** ships with 2E, the slice of the dead (the build design's slice 8.4), as a layer on its grave plans and on culture-faith's rites (lead's call):
  - *Ask their friends first* holds a member's grave until the ghoul has asked (§8.11); *Bury them at once* lets the grave go ahead, which closes the body to the ghoul.
  - The first draft's third choice, "Leave them to Morrow", is dropped: it read as the player handing a body over, which the ghouls' code and fixed decision 2 both forbid.
- **Later, through the companion designs:** a moon-nights row in Schedule, "leave be" ground and a room to keep someone in from Zones, and a trough, a pond or a hearth from Build (§11).

## 8. What changes in the simulation

Everything here builds on batch 1's needs model (ADR-0022, §5). 2A is the brimmet (§8.10); 2E is the ghoul, with the dead (§8.11). The other slices bring their code to their own sign-offs.

### 8.1 State

- **Needs of a person's nature** are batch 1's `Character.NatureNeeds`: one `NatureNeed` per need the species names, with `Id`, `Value` (0–1000), `Stage` (0 fine, 1 low, 2 critical), `LastEvent`, `CriticalSince` and the bonds (`BondId`, `BondWork`, `BondItem`). The brimmet's dish is one of these. 2A adds no field to it.
- **The existing fields keep their meaning.** Food or blood stays in `Food`, rest in `Rest` and company in `Social`. Batch 1's `foodSource: "none"` and `tires: false` switch off the first two for the peoples that need it.
- **Enums.** Append only; snapshots store names. 2A adds `JobKind.TendNeed`: standing at a place that feeds a need until it is met (the brimmet at the river). Batch 1's `TendTree` stays the dryad's.
- **2E adds** (§8.11):
  - `World.Remains`: `{ Id, CharacterId, GraveId, Portions, DugTick }`. A record is created the first time someone eats from a body or digs a grave; a dead person with no record is whole. Old graves get a record at world creation.
  - `World.PendingDiscoveries`: `{ WitnessId, EventId }`, holding what someone will find when they next wake. If 2C's tatterlins land first ("Sera found the roof mended in the night"), they add it.
  - `JobKind.AskLeave`, `JobKind.FeedOnDead` and `DebtKind.Leave`.
  - The grave itself, `Character.DiedEvent` and `RestingIn` are the build design's (its §4.2 and §4.8), built in the same slice. Old graves are its `grave` structure with `ForId` 0, placed last in world creation on open tiles of the ruins' yard from Truths, so no tree, bush or rock moves.
- **The slices between** add their own state with their sign-offs: for example 2C's `DebtKind.Luck` and the go-between's `understands` list, and 2D's `Changed` flag.

### 8.2 Needs are data

A need in `content/needs.json` (batch 1's schema) gives its name; its start, decay, low, critical and met marks; its low and critical thoughts and events; the health it takes while critical, with the death text; the hours critical before its holder leaves; the works or items it can be bound to; and integer `params` for its behaviour. A species lists its needs in `needs`. That is a **list**, so a hybrid can later take the union of its parents' needs.

The brimmet uses the format as it is. What only the brimmet does is a param of `head_water`, read by its behaviour, so 2A changes nothing in the needs schema (lead's call: the other choice was new typed fields, which would need a schema change). 2B adds `pledgePour` with the pledge.

| Param | Value | What it does |
|---|---|---|
| `waterPerHour` | 500 | restored each hour beside water |
| `rainPerHour` | 150 | restored each hour of rain while uncovered |
| `daylightPerHour` | 40 | drained on top of the decay each hour in open daylight |
| `tendMaxTicks` | 1800 | the longest a `TendNeed` stays at the river |
| `bowPermille` | 120 | the chance that someone chatting with a brimmet bows |
| `spill` | 400 | what the dish loses when the brimmet bows back |
| `weakFromStage` | 1 | from this stage (1, low) the brimmet fights weakened |
| `weakHitPct` | 50 | the share of its hit chance it keeps while weakened |

### 8.3 Code grows by need or act, never by species

- **The rule.** A need's id names its behaviour (`sim/Systems/Natures/`), as an incident's id does (ADR-0018 #4); an act's id names its code. `RequiredContent.Needs` lists each need id with the params its code reads. No species id appears in `sim/` except `human`, the night thief (a test, §12.1). A people that shares a need with another (the "kin" of §4.1) is JSON alone.
- **The shared model asks each need's behaviour** through dispatch points in `NatureNeeds`; batch 1 has `Bind`, `Restore`, `SourceOpen` and `Answer`. 2A adds four. Each answers "nothing" for every need but `head_water`, so batch 1's peoples are untouched:
  - `BreaksJob`: should this need break a running job now? For the brimmet, yes at critical (§8.5).
  - `FightHitPct`: what share of their hit chance does the holder keep? For the brimmet, `weakHitPct` from `weakFromStage` on.
  - `Collapses`: does the need fell its holder now? For the brimmet, yes at zero.
  - `RescueSpot`: where should a rescuer carry the downed holder? For the brimmet, the nearest bank tile.
- **A mechanism is pulled out when a second need uses it** (lead's call). The first need to do a thing writes it in its own behaviour. When a second need does the same, the code moves into one mechanism that both call. If the first user is code that already ships (batch 1's), the second writes the shared form, and the first moves onto it later, as a refactor. What is expected to be shared, and when:

| Mechanism | First user | Shared from | Grows out of |
|---|---|---|---|
| stand at a place until a need is met | dryad (`TendTree`, batch 1) | 2A: the brimmet's `TendNeed` is written as the shared job; the listrel's rest uses it (2B); the dryad moves onto it as a refactor | `Forest.Tend`, `Places.Adjacent` |
| weather on the uncovered | brimmet rain (2A) | the charrow (batch 5) | `IncidentSystem.RainUpkeep` |
| ask, and be answered from state | vampire blood (`BloodJobs`); batch 1's bargains wrote their own | 2B: the listrel's tale; the ghoul's leave (2E) uses it | `BloodJobs.RefusalKey`, `Bargains.RefusalKey` |
| a contest at night | clobbin play (2B) | 2B: the brimmet's pledge | `CombatSystem`'s hit roll, without damage |
| a feeling renewed or handed on from stored state | the listrel's reopened thought (2B) | 2E: the ghoul's echo | the thought and opinion code in `Relations` |
| a share left at a place (`offering`) | tatterlin, knollet, feeplin (2C) | 2C: built once, for all three | `Rations` set-aside meals; the `KeepPromise` job |
| a bond kept or broken | djinn contracts (batch 1) | batch 3's tokens | `World.Contracts`, `Debt` |

- **Refactors keep the logs identical** (lead's call). Pulling a mechanism out of a need that already ships is a refactor, so the soak's `world-N.txt` logs for seeds 1–100 must come out byte-identical. If they cannot, it is not a refactor: it waits, or it ships as a behaviour change in its own commit, with its own soak comparison and a line in the slice's ADR. Identical logs win over tidier code.

### 8.4 Harms

Today the only harm outside the needs is the sun (`daylightBurnPerHour`), and it stays as it is. No people in 2A to 2E adds a harm: the sun on a brimmet's dish is a drain (`daylightPerHour`), its fall at zero is `head_water`'s behaviour, and the listrel's fear of fire is a feeling, not a harm. Harms become a list on the species when batch 5 brings the next one: `petrify` for trolls, `heat` near the hearth for rimelings, `iron` once items exist. Each entry then gives its kind, health per hour, effect, cover, thought, event, death text and the promise it implies for a newcomer. Sleep hours come from the schedule design's `rhythm` field (`diurnal`, `nocturnal`, `sleepless`); this doc adds no sleep field.

### 8.5 The order of concerns

`JobPicker.Choose` keeps batch 1's order (lead's call; changing it would move batch 1's worlds):
1. Cover from the sun.
2. A fae keeping its sworn word (batch 1).
3. Food or blood below hungry: eat or forage; ask a donor.
4. **A need of their nature at critical:** its behaviour's job.
5. Rest; the injured lie down (only for those who tire).
6. **A need of their nature at low:** its behaviour's job.
7. Promises: the Oathbound.
8. Care and rescue.
9. A Generous donor offering blood.
10. Eating when below fed.
11. Prioritised work.
12. Company.
13. Wandering.

In this order, a brimmet whose dish is low goes to the river before it keeps a promise or tends someone, unless it is already busy, because a low need never breaks a running job. The pledge of §6.1 (seed 2) comes in 2B with `keepsPledges`, a species flag that puts that species' pledges ahead of even a critical need.

**Breaking off a job.** `JobPicker.Choose` runs only when someone is idle. `WorkSystem.ShouldInterrupt` breaks a running job for three reasons: the sun, starving and exhaustion.
- 2A adds a fourth: a need whose behaviour says `BreaksJob`, which is the brimmet's dish at critical. Batch 1's needs never say so, so their worlds do not move.
- It does not break a fight, a flight, a rescue or an arrival (these are already exempt), or the need's own job.
- A low need never breaks a job. It waits for the job to end.

So a brimmet on watch with a drying dish stays at its post until the dish is critical, and a fight that starts before then runs to its end (§1).

### 8.6 Reactions: the body's share of feeling

- **Three layers of feeling.** Body: the species and its needs. Culture: customs, in the culture-faith design. Person: traits.
- **Culture landed first, so it owns the shared shape.** Culture-faith's `customs.json` gives each custom a kind implemented in code and params in content. When a people needs a reaction of the body to an event (a thought, a need change, or both), it is a custom scoped to a species, in that shape. This doc adds no reaction record of its own.
- **2A needs none.** The brimmet's delight in rain comes from `head_water`'s own rain restore, which adds `dish_rain`. In 2E, the thought a human has on seeing a ghoul eat comes from their culture's attitude toward ghouls (culture-faith §5.10), not from a species reaction.

### 8.7 Social acts

- **An act that only one need uses lives in that need** (lead's call): its code in the need's behaviour, its numbers in the need's params. 2A's bow belongs to `head_water` (`bowPermille`, `spill`); 2E's `ask_leave` belongs to `dead_flesh`.
- **`content/acts.json` arrives with the first act that two needs share:** the contest in 2B, used by the clobbin's play and the brimmet's pledge. `ContentDb` then rejects an act with no behaviour, as it already rejects an incident with none (ADR-0018 #4).
- **Acts by slice:** `bow` (2A); `contest`, `prank` and `ask_tale` (2B); `leave_thanks`, `ask_passage`, and the whistle-speech filter with its warning (2C); `ask_leave` (2E).
- **Where they hook in:**
  - At chat time in `SocialSystem.Step`: the bow, contest offers, the whistle filter.
  - As jobs from `JobPicker`: `ask_tale`, `ask_leave`.
  - In travel: `ask_passage`, when a path passes a mound.
  - In spotting: a feeplin's warning, passed on through its go-between.

### 8.8 Join promises

- **Unchanged in 2A.** `IncidentSystem.UnmetNeed`, as batch 1 left it: those who burn are promised a roof when no roof section is sound; those who eat from the stores are promised a meal when nothing is free for them; those who eat nothing are promised nothing.
- The brimmet eats berries, so it is promised a meal like anyone when the stores are bare. Nobody promises it the river, which is always there.
- **Promises from data come in 2C,** when tatterlins are promised a bowl with their own text: an optional promise on a need, checked after the sun and the food. That is a schema change for 2C's sign-off.

### 8.9 The vampire migration: withdrawn

Withdrawn with the first draft's plan (§5). Food and blood become data only when something needs them to, and that change must leave the soak logs byte-identical (§8.3).

### 8.10 The brimmet (2A)

- **The need.** `head_water` restores `waterPerHour` while its holder stands on a passable tile next to water, and `rainPerHour` for each hour of rain while it is uncovered, which also adds `dish_rain`. It drains `daylightPerHour` more in open daylight.
- **Going to the river.** Low or critical, its behaviour returns a `TendNeed` job: the nearest passable tile next to water, ties broken by (y, x) as `Places.CoveredSpot` does, so no random draw. The brimmet stays until the need is met, `tendMaxTicks` pass, or something more urgent comes up. The job's text is `job.tend_need.head_water`.
- **The bow.** When a chat in `SocialSystem.Step` involves a brimmet, one `CourtesyBow` draw (Social stream) at `bowPermille` decides whether the other person bows. The brimmet bows back and the dish loses `spill`. The bow event becomes the need's `LastEvent`, so the drying that follows cites it.
- **Weakness.** `CombatSystem`'s hit roll multiplies the chance by `NatureNeeds.FightHitPct`. The downing event then cites the event that put the need at its weakening stage, because it changed the odds (ADR-0018 #1).
- **Collapse.** At zero, `NatureNeeds.Collapses` downs the brimmet where it stands (the existing `event.needs.collapsed`, with a head-dish text). It stays down until its dish is back above critical.
- **Rescue.** The rescue job carries the downed to `NatureNeeds.RescueSpot` when there is one, and to a bed otherwise, as now. For the brimmet that is the nearest bank tile, where the water fills its dish and it gets up.
- **Arrival.** The species field `arrivesFrom: "water"` puts a newcomer on a bank tile instead of at the map's edge. The tile is drawn as the edge tile is (`DiceId.ArrivalEdge`, Incident stream), so no new draw tag is needed. The brimmet's `arrivalKey` reads "{a} climbed out of the river, hungry and alone."
- **Food balance.** The brimmet is one more berry-eater from day 12. As batch 1 did for the fae (ADR-0022 #8), one more small bush cluster (two bushes, keyed after the others so none moves) keeps winter at half rations per head. It lands with Teal in the cast step, not before, so the content step stays byte-identical (§12.1).

### 8.11 The ghoul (2E), and the buried dead

The ghoul comes in the slice of the dead, with the build design's graves and the act of laying the dead to rest (its 8.4), culture-faith's death rites and the Zones burial ground (§12.1 says why). The rules below are designed now so that slice is built once; nothing here is 2A's.

- **Where it finds food, in this order:**
  1. an old grave (there is nobody to ask);
  2. an unburied body with no living friends (a dead thief belongs to nobody);
  3. an unburied body whose friends have all given leave.

  It reads only `World.Remains` and dead characters, never a grave's `ForId`. The build design lets the player mark a grave "for whoever needs it first", which also has `ForId` 0, and an empty grave never feeds a ghoul.
- **Asking leave.**
  - `AskLeave` runs from 19:00 to 22:00 (`dead_flesh`'s `fromHour` and `toHour`), when a nocturnal ghoul and the friends are both awake. The ghoul walks to each friend in turn and emits `asked_leave`, citing its hunger and the death (`Character.DiedEvent`).
  - The answer comes from state, never from Dice. A friend refuses if their mood is below `refuseMoodBelow` or their opinion of the ghoul is below `requiredOpinion`, or if their faith forbids it (culture-faith).
  - **Granted:** the event forms a `DebtKind.Leave` debt.
  - **Refused:** that body is closed to that ghoul. The ghoul gets the thought `leave_refused` about the friend (−6, anger; opinion −10).
- **The echo.** On the first meal from a member's body, with leave or without it (the echo follows the flesh, not the leave, so a stolen meal or a dug grave carries it too), the sim finds the living person the dead held the strongest opinion of (largest absolute value, ties by id) and moves the ghoul's opinion of that person by `echoPercent` of the dead's opinion. It emits `echo`, citing the meal. The existing opinion lines then form a grudge or a friendship. Old graves and dead thieves give none.
- **The grave waits for the asking** (lead's call).
  - While the camp's "When someone dies" order is *Ask their friends first* (§7.4, the default), a grave plan for a member who died waits until the first asking window after the death has closed, or until every friend has answered.
  - The build design's stand-in player marks a grave six hours after a death (its 8.4). It honours the order, and it picks the order by seed, as it does the ration policy (ADR-0017 #2), so soaks see both.
  - A rite whose deadline comes sooner (culture-faith's D1) is never held back.
- **Burial closes the body** (lead's call; the content line is question 4).
  - Once a member is laid to rest (`RestingIn` set), nobody is asked to open the grave, and the ghoul never eats that body with leave. Burial is the camp's answer.
  - Leave given earlier covers only what is eaten before the burial. What is left goes into the grave.
  - Old graves stay open to it, because nobody living loved those dead.
- **The night meal.** A ghoul that is starving (critical), Desperate and shut out of every body takes a `FeedOnDead` job at night anyway.
  - **At an unburied body:** it emits `fed_without_leave`, citing the refusal and the starving event, and adds a pending discovery for each friend. When a friend wakes, the sim emits `found_desecrated` and gives the friend `dead_desecrated` (mood −20, opinion −50 toward the ghoul). The existing opinion line then forms the grudge.
  - **At a grave, the worse offence:** it emits `grave_dug`, citing the refusal if one was given before the burial, else the burial itself (`event.work.buried`, the camp's answer), and the starving event. The grave is left open (`Remains.DugTick`). Each friend, and whoever laid the body to rest, gets a pending discovery. On waking the sim emits `found_grave_dug` and gives them `grave_dug` (mood −25, opinion −60 toward the ghoul), so a grudge is likelier and comes sooner. The body counts as unburied again: the build design's `unburied` grief returns for its friends, citing the digging, until someone lays it to rest a second time.
  - Either way the ghoul carries the shame, `fed_without_leave` (−10, gloom, five days).
- **Witnesses.** Anyone awake within six tiles of any meal gets `saw_dead_eaten` (−6, gloom, by default) through their culture's attitude toward ghouls.
- **Draws.** None. Asking, answering, the echo and digging are all decided from state; only where the old graves lie is a Truth (§10).

### 8.12 Events, causes and endings

**2A: the brimmet**

| Event key | System | Cites | Effect |
|---|---|---|---|
| `event.incident.brimmet_arrives` (the brimmet's `arrivalKey`) | Incident | nothing | — |
| `event.social.bowed` | Social | nothing (courtesy starts chains) | — |
| `event.needs.head_water_low`, `_critical` | Needs | the need's `LastEvent` (the bow, or the last time it was met) | — |
| `event.needs.collapsed` (existing, with a head-dish text) | Needs | the critical event | — |
| `event.combat.downed` (existing) | Combat | the fight, plus the event that weakened the brimmet | — |
| `event.work.rescued` (existing, with a "to the river" text) | Work | the downing or the collapse | Debt formed (existing) |
| `event.needs.died_dry` | Needs | `LastHealthEvent` | Death |

**2E: the ghoul**

| Event key | System | Cites | Effect |
|---|---|---|---|
| `event.needs.dead_flesh_low`, `_critical` | Needs | the last meal, or arrival | — |
| `event.social.asked_leave` | Social | the ghoul's hunger event; the death | — |
| `event.social.leave_granted` | Social | the request | Debt formed (`Leave`) |
| `event.social.leave_refused` | Social | the request | — |
| `event.needs.fed_on_dead` | Needs | the leave given, or the death (no friends), or the hunger (old grave) | — |
| `event.social.echo` | Social | the meal | — (an opinion change; the existing lines may then form a grudge) |
| `event.social.fed_without_leave` | Social | the refusal; the starving event | — |
| `event.social.found_desecrated` | Social | the night meal | Grudge formed (existing event) |
| `event.social.grave_dug` | Social | the refusal, else the burial; the starving event | — |
| `event.social.found_grave_dug` | Social | the digging | Grudge formed (existing event) |
| `event.mood.unburied` (the build design's, again) | Mood | the digging | — |
| `event.needs.died_dead_flesh` | Needs | `LastHealthEvent` | Death |

2A and 2E add no effect kinds; batch 1 added four (ADR-0022). Stories end in grudges, debts, departures and deaths, which the analyzer already counts (ADR-0006). Arc completions join them in fixed decision 12's M1. Every new event has a `_you_` variant wherever the player can fill a placeholder (ADR-0017 #4).

## 9. Content and schema changes

The founder's sign-off approves these for 2A. Each later slice brings its own to its own sign-off.

**Schema change (2A): one.**
- `content/schemas/species.schema.json` gains one optional field, `arrivesFrom`: `"edge"` (the default) or `"water"`.

The needs schema stays batch 1's, unchanged: the brimmet's own numbers are `params`, and Teal's arrival window uses batch 1's `arrivalDays`. The first draft's other species fields wait until something reads them, since ADR-0018 already lists content fields with no effect as a seam (lead's call): `size`, `lifespanYears` and `ethics` come with Build, aging and leanings; sleep is the schedule's `rhythm`; harms come in batch 5 (§8.4); blood stays on `canDonateBlood`; `keepsPledges` comes with the pledge in 2B; reactions are culture customs (§8.6); and loc keys follow a convention, checked at load.

**Content entries (2A), in batch 1's formats**
- **`content/needs.json`:** `head_water` (§8.2). `RequiredContent.Needs` gains it, with its params.
- **`content/species.json`:** `brimmet`.
- **`content/cast.json`:** Teal (brimmet), `"arrivalDays": [12, 20]`. With batch 1's four that makes fifteen; question 2 asks how the cast grows after that.
- **`content/thoughts.json`:** `dish_drying`, `dish_dry`, `dish_full`, `dish_rain`, `bowed_to`.
- **`content/loc/en.peoples.json`** (batch 1's file for the peoples' text): `species.<id>.name`, `.plural`, `.resemblance` and `.summary` for every species, the six that exist and the brimmet (culture-faith asks for `.plural` too); `need.head_water` with `.low` and `.critical`; the events of §8.12 with their `_you_` variants; `job.tend_need.head_water`; the new thoughts.
- **`game/art/looks.json`** is a view asset, not content: a `species` block names the body parts each people's figure draws (the brimmet's crown-dish and its rim, its long webbed hands). This is ADR-0019's pattern, so the sim never reads it.

**Checked at load** (`ContentDb.Validate`), each failure naming the file and the id:
- batch 1's checks: every need a species names exists and has behaviour, every param its code reads is there, and every thought and event key it names exists;
- every species has its `.name`, `.plural`, `.resemblance` and `.summary` loc keys;
- `arrivesFrom` is a known value.

**Examples** (batch 1's formats):
```json
// needs.json: the brimmet's need
{ "id": "head_water", "nameKey": "need.head_water",
  "start": 1000, "decayPerHour": 40,
  "lowBelow": 350, "criticalBelow": 150, "satisfiedAt": 800,
  "lowThought": "dish_drying", "criticalThought": "dish_dry",
  "lowEventKey": "event.needs.head_water_low", "criticalEventKey": "event.needs.head_water_critical",
  "criticalHealthPerHour": 20, "deathKey": "event.needs.died_dry",
  "params": { "waterPerHour": 500, "rainPerHour": 150, "daylightPerHour": 40, "tendMaxTicks": 1800,
              "bowPermille": 120, "spill": 400, "weakFromStage": 1, "weakHitPct": 50 } }
```
```json
// species.json: the brimmet
{ "id": "brimmet", "nameKey": "species.brimmet.name",
  "foodSource": "berries", "foodDecayPerHour": 25,
  "restDecayPerHour": 30, "socialDecayPerHour": 15, "sleepRestorePerHour": 120,
  "daylightBurnPerHour": 0, "ticksPerTile": 6, "canDonateBlood": false,
  "needs": ["head_water"], "arrivalKey": "event.incident.brimmet_arrives", "arrivesFrom": "water" }
```

**2E's content, for that slice's sign-off** (listed so the slice of the dead is designed once):
- the `dead_flesh` need, with params `perPortion` 600, `portions` 3, `friendOpinion` 30, `fromHour` 19, `toHour` 22, `refuseMoodBelow` 30, `requiredOpinion` 0 and `echoPercent` 50;
- the `ghoul` species: `foodSource: "none"`, `needs: ["dead_flesh"]`, the schedule's `"rhythm": "nocturnal"`, and Zones' `"atHomeInDark": true`;
- Morrow in cast.json, with an `arrivalDays` window;
- the thoughts `dead_hunger`, `dead_starving`, `fed_with_leave`, `fed_without_leave`, `leave_refused`, `dead_desecrated` and `grave_dug`;
- the "When someone dies" standing order, and `saw_dead_eaten` as a culture attitude.

## 10. Determinism, snapshots, the text-state API, localization and mod hooks

**Draw ids: batch 2 has its own range** (lead's call). Each parallel track claims a range, so no branch renumbers at merge:
- growth: TruthId 20–29 and DiceId 100–119 (ADR-0021);
- batch 1: TruthId 30–39 and DiceId 120–139 (ADR-0022);
- culture and faith: TruthId 40–49 and DiceId 140–159 (ADR-0023, on its branch);
- **batch 2: TruthId 50–59 and DiceId 160–179.**

A `TruthId` value feeds the Truths hash, so once merged it never changes (ADR-0004). `DiceId` values are tags, and `JobKind` and `DebtKind` are stored by name. Batch 2 adds no `DiceSystem`.

**Truths** (stateless; appended, never renumbered or reused)
- **2A adds none.** This doc first proposed a Truth to vary each person's drain on their people's own needs (`NeedTolerance`). It is folded into question 8, which asks once whether every need should vary per person, food and rest included.
- **Later, in landing order, from 50:** `CastPresence` (2B, question 2); `Mounds` (2C); `MoonPhase` (2D); `OldGraves` (2E); `EthicsLeaning`, per person and per axis, once species leanings change behaviour; `Lifespan`, once aging exists; `WandererSpecies`, the people of a generated wanderer, keyed by arrival slot.
- **Not drawn at all:**
  - A cast member's people is content (`cast.json`).
  - Answers to every ask (blood, leave, a tale, a contest) come from state. The same situation gives the same answer, and a refusal reads as a choice, not a roll. The ghoul's echo, the wolf's target and the feeplin's warning come from state too.

**Dice** (per-system streams)
- **160 `CourtesyBow`** (Social stream, 2A).
- **Later, in landing order:** `ContestBout` and `RepayAgain` (2B); `PrankKind`, `CurseRoll` and `Windfall` (2C); `MoonChange` (2D). Evolution path choices (fixed decision 12) use growth's stream and range (`DiceSystem.Growth`, DiceId 100–119), each with its purpose tag.
- **A people's draws happen only when that people is present.** A world whose cast has no brimmet draws exactly what it drew before. A test enforces this (§12.1).

**Rewinds.** Truths keep each person's people and nature. Dice re-roll their experiences, so in another timeline the same brimmet spills its dish on different nights. The Chronicle of Lost Futures can then say "In another timeline, Teal was carried to the river by Bram." The seldkin (batch 4) is the one people whose twist is built on rewinds themselves.

**Snapshots.**
- 2A adds no state. The dish is a `NatureNeed` (batch 1's), and `JobKind.TendNeed` is appended and stored by name. `World.SchemaVersion` is not bumped for it (ADR-0021 bumps it when saves arrive).
- 2E adds `World.Remains`, `World.PendingDiscoveries` (unless 2C did) and its job and debt kinds. Its snapshot tests restore in the middle of `AskLeave` and `FeedOnDead`, with a grave dug and with a discovery pending.
- Restore stays far under a second at 200 characters.

**Text-state API.**
- `CharacterState.Needs` is built from the species, as batch 1 does: food or blood, the needs of their nature, rest, company, health. 2A adds each nature need's stage word (`need.<id>.low`, `.critical`), so the inspector can say "drying".
- A new `SpeciesState` (id, name, plural, resemblance, summary, needs, who is in camp) comes through `StateApi.Species(w, id)`. The inspector's species tooltip reads it.
- `LocationState.Contents` lists remains and old graves (2E).
- The headless runner's cast lines already show each species. The soak summary gains "stories involving each people".

**Localization.** No English is composed in code (ADR-0007). New key families:
- species: `species.<id>.name`, `.plural`, `.resemblance`, `.summary`;
- needs: `need.<id>`, `need.<id>.low`, `need.<id>.critical`;
- events: those in §8.12, with `_you_` variants;
- jobs: `job.tend_need.<need>`;
- thoughts: `thought.<id>`.

**Mod hooks.**
- A mod can add a people with JSON alone if its needs already exist (a kin people). It supplies the species, thoughts, loc keys and a looks entry, all merged by id (ADR-0007).
- A new need or act needs code. A registration hook on `IModHook` comes later (§12.4).
- Hooks see every new event at the end of its tick.

## 11. Fit with the parallel work

**Batch 1 (its branch, ADR-0022).** Batch 2 builds on batch 1's needs model and rewrites none of batch 1's peoples (§5, §8). 2A starts once batch 1 has merged.

**Character growth (fixed decision 12; ADR-0021, merged).**
- The M0 fields (`Character.Growth`: trait origins, want, arc) are untouched. `Growth.ChangeTrait` is the one door for trait changes, so the peoples' mutations in M1 go through it, each citing its event.
- Each batch 2 entry lists wants and evolution paths for `wants.json` and `evolution_paths.json`, ready for when M1 builds them. ADR-0021's wants already carry species weights (walking in daylight is a vampire's), so a people's own wants are entries in that file, and so is the wishwraith's wish when it comes.
- Peoples supply experiences for `trait_mutations.json`: a dish spilled in front of others, a contest won or lost, whistle-speech learned, a night as the wolf, and in 2E a refused leave. Each mutation cites its event, so nothing grows from grind.
- M1 asks for naming-triggered evolution for two species. Recommended: the tatterlin, whose naming is part of its design, and dryads (batch 1).

**Culture and faith (`2026-11-09-culture-faith-design.md`, being built on its branch).** This design relies on four things from it:
1. **Customs that fire on events and give thoughts.** A species reaction, when one is needed, is a custom scoped to a species (§8.6).
2. **Attitudes toward peoples** (its §5.10): revere, wary, fear, scorn. Its §6.10 adds stances on batch 1's peoples as content, and batch 2's peoples get theirs the same way as each lands. How someone takes seeing a ghoul eat is one of them (§8.11).
3. **Death rites per culture** (its §5.4, its slice 2): grave, pyre or river. They decide the ghoul's world. A culture that burns its dead is a famine for a ghoul; one that buries makes the asking a race with the grave (§8.11). The ghoul comes in the same slice.
4. **Holy days and festivals on the calendar.** They share one moon with the werewolf (2D), and carry the days a culture welcomes or wards off gorrocks (batch 5) and the feasts where the centaur and the thrummel come in.

It also shares these with the culture-faith design:
- **Offerings.** One `offering` mechanism serves the tatterlin, the knollet, the feeplin and culture's own offerings to spirits.
- **Courtesy.** A culture that greets with a bow makes brimmet spills common (a greeting custom that raises the bow's chance among its members). Asking leave of a mound is a custom people learn from one another. The player starts with no culture (ADR-0025), so learning these courtesies is part of the player's own story, and what they learn they keep across rewinds.
- **Guest-right.** The join promise (§8.8) is the hook for cultures that owe guests more, and the gorrock's need runs on it.
- **Oaths.** Djinn contracts, brimmet pledges and centaur carries are oaths, for the cultures that weigh oaths.

Every person of every people belongs to one of the world's invented cultures (rule 4); culture-faith's seeding gives the peoples their cultures through its `"*"` weights. A brimmet raised in a human culture is a story too.

**Build, Zones and Schedule (companion designs).** What peoples ask of each:
- **Build:**
  - graves and the act of laying the dead to rest, for the ghoul (build §4.8 and slice 8.4, with 2E);
  - a trough or pond, for the brimmet;
  - a house with a hearth, for the tatterlin (build slice 2's hearths);
  - stone rooms (troll), wide doors (centaur), fire and fuel (charrow), cold rooms (rimeling) and night building (nightwright), in batches 5 and 6;
  - refusing to build on a mound (knollet), through build's existing refusal path.
- **Zones:**
  - "leave be" ground around mounds, old graves and a dryad's grove (zones slice 2);
  - a run ground, and a room to keep a werewolf in;
  - resting trees spared for a listrel;
  - `atHomeInDark`, which the zones design asks this doc to set (lead's call): undead, djinn, fae, the ghoul, the clobbin and the tatterlin are at home in the dark; dryads and the brimmet are not, so a home area can keep them from their trees or the river at night; and the werewolf is not, because the moon run is the point.
- **Schedule:**
  - each species' kind of day, through the schedule's `rhythm` field: the ghoul, the clobbin and the tatterlin are `nocturnal`, as vampires are; the brimmet stays `diurnal`;
  - a moon-nights row, shown only for werewolves (let them run, or keep them in); ADR-0025's compliance rule decides how far "keep them in" carries.

  A tatterlin's unwatched hours are a consequence of the watch schedule, not a setting.

## 12. Build order and acceptance

### 12.1 First slice: 2A, the brimmet (now)

**Why the brimmet alone** (lead's call; the builder asked what the ghoul would eat in a one-year world).
- **The numbers.** In the last soak on main (`out/soak`, 100 worlds × 1 year) a member died in 2 worlds, both vampires burned by the sun; nobody died of wounds or hunger, and a thief was killed in 5 worlds. Batch 1 adds a few deaths, mostly in winter: its undead fades to rest in 3 worlds and its dryad withers in 2 (ADR-0022). The two old graves (four portions of 600, at 8 an hour) last about twelve days, so a ghoul arriving on days 12–20 would run out on days 25–33 and starve or leave near the turn of winter in about 95 of 100 worlds.
- **What that would mean.** Asking leave, the point of the people, would almost never happen, which fails this doc's own test (§1); the ending would land in winter, against goal 5; and the ghoul's acceptance would rest on two to seven worlds. The other ways out are worse: more old graves or a slower drain would feed it alone (a human in costume), more deaths would change every M0 regression world (not this doc's call), and reporting the starvation honestly would ship a people whose story is always the same.
- **So the ghoul waits for the dead, as 2E.** The build, Zones and culture-faith designs hold back their work on the dead for the same numbers, timed once as culture-faith's question 5. If that answer is "rites now", the ghoul moves up with them.
- **No other people takes its place in 2A.** The listrel is worth more in 2B, where it rests on the brimmet's place job and its tale pulls the ask-and-answer mechanism out of blood and bargains (§8.3). A one-people slice is also the quickest way to prove that batch 1's model takes a new need.

**Starts from:** batch 1 merged, with ADR-0022 accepted.

Steps, each a small commit with its tests:
1. **The brimmet in content and code, not yet in the cast:** `head_water` and its behaviour (water and rain, the daylight drain, `TendNeed`); the four dispatch points (§8.3); the bow, weakness, collapse and rescue to water; `arrivesFrom`; thoughts and loc; `SpeciesState` and the stage words. With no brimmet in the cast, the soak logs are byte-identical with batch 1's.
2. **The brimmet in the view:** its parts (the crown-dish with its glint, drawn from the need's value; the long webbed hands; the level-headed walk) and the inspector's species tooltip with the resemblance line. View only.
3. **The cast:** Teal joins with `arrivalDays` [12, 20], and one small bush cluster keyed after the others keeps winter's half rations (§8.10). Every world changes here, so this step sets the new soak baseline.
4. **Soak and record:** the soak, the stranger check, STORIES.md, and an ADR recording the slice.

**Acceptance: tests**
- **`AbsentPeopleChangeNothing`:** with the brimmet defined in content but not in the cast, the soak logs for seeds 1–100 are byte-identical with batch 1's build.
- **Content checks:** each of these fails at load, naming the file and the id: a species naming a missing need, a need with no behaviour, a missing `head_water` param, a missing species loc key (`.resemblance` included), an unknown `arrivesFrom`.
- **`SpeciesIdsNotInCode`:** no species id appears as a literal in `sim/` except `human` (the night thief).
- **Causality:** bow → spill → drying → struck down in the fight → carried to the river → debt, each event citing the one before.
- **The interrupt:** a brimmet on watch with a low dish stays at its post; at critical it leaves for the river, unless it is fighting. No batch 1 need breaks a job.
- **Snapshots:** in the middle of `TendNeed`, and with a brimmet downed at zero, a restored world has the same future as a straight run.
- **Determinism and invariants:** the same seed and commands give the same world with the brimmet present; need values stay within 0–1000; nobody stands on water.
- **Benchmark:** CPU per character per tick stays within ADR-0005's budget. It is measured at the largest camp a world can hold, plus thieves: fifteen people now, and twenty once question 2's presence draw exists (lead's call).

**Acceptance: soak** (100 worlds × 1 year). These are targets for tuning. If one cannot be reached, report the honest number, as ADR-0017 did.
- Stories per world-year are no fewer than in the last soak before the slice (12.13 with batch 1, ADR-0022).
- A brimmet need event lies on a story's chain in at least 40% of worlds.
- The share of stories that end before winter (day 31) rises by at least 5 points over the last soak before the slice (lead's call: the first draft asked 10 points of two peoples).
- No world crashes.

**Acceptance: stranger check.** Four fresh readers each get one raw log from a seed with Teal present. At least two retell a brimmet story unprompted. This is the M0 exit criterion, applied to the new people.

**Acceptance: screenshots**
- **The pawn sheet at 32 and 48 px:** human, vampire, the four batch 1 peoples, and the brimmet with its dish full and with it dry. This is also the sheet that formally accepts ADR-0009.
- **The inspector** open on the brimmet, showing the head-dish bar and the resemblance tooltip.

### 12.2 The rest of batch 2

**How each slice is measured** (lead's call). A slice's acceptance percentages are over the worlds where its people is present, and are judged only on a soak with at least 30 such worlds (the build design's rule for its 8.4); the soak grows past 100 worlds when it must. Until question 2's presence draw exists, every world has every people.

| Slice | Peoples | Builds | Waits for | Acceptance beyond the usual tests |
|---|---|---|---|---|
| 2B | clobbin, listrel | first, the presence draw (question 2); `acts.json` with `contest`; `prank` and `ask_tale`, pulling the ask-and-answer mechanism out of blood and bargains (§8.3); the clobbin's restores for a loss and a win, and its repayment loop; thoughts renewed by tales; rest at a tree, on the brimmet's `TendNeed`; the brimmet's pledge, with `pledgePour` and `keepsPledges` | nothing | a repayment-to-theft chain in at least 20% of worlds with a clobbin; a reopened-wound grudge in at least 25% of worlds with a listrel; fewer stories ending on a debt nobody returns to (ADR-0018) |
| 2C | tatterlin, knollet, feeplin | the `offering` mechanism and the bowl job; join promises from data (the bowl); `SetStandingOrder` with the bowl order; quarrels at the hearth; mounds, `ask_passage`, the curse and the windfall loan; whistle-speech, the go-between and its warnings | best after build slice 1 and zones | an empty-bowl chain in at least 30% of worlds with a tatterlin that reach winter; a go-between departure chain in at least 20% of worlds with a feeplin |
| 2D | werewolf | the moon in `world.json`; the `Changed` flag for one night, with the wolf's target read from the person's opinions; Schedule's moon row | the schedule design | a moon-night chain (ran, kept in, or changed) ending in a persistent effect in at least 40% of worlds with a werewolf |
| 2E | ghoul | `dead_flesh` and its behaviour; `ask_leave` and the `Leave` debt; the echo; the night meal, grave digging and pending discoveries; the old graves; the "When someone dies" order; Morrow in the cast (§8.11) | the slice of the dead: aging and multi-year worlds, the build design's 8.4, culture-faith's death rites (timing: culture-faith's question 5) | of the worlds where a member dies while the ghoul lives in camp (at least 30 of them), at least half produce an ask-leave chain and at least a quarter end in a grudge or debt that names the ghoul; tests for the chain ask → refuse → starving → night meal → discovery → grudge, for its grave form, and for an echo that becomes a grudge; a night-scene screenshot with the ghoul at a body, the gore stylised (ADR-0019) |

### 12.3 Later batches, and what each needs first

| Batch | Peoples | Needs first |
|---|---|---|
| 3: tokens, water, music | vellet, fetlow, thrummel | the items milestone (named personal items that can be stored, hidden and stolen); a combat job beyond `Raid` (the ford, the drowning song); culture's festivals for music |
| 4: secrets and the hidden | hushtail, seldkin, turnfoot | a knowledge layer: who knows which facts about whom (the listrel's gossip and the rival otherworlders need it too); for the seldkin, rewinds in play and fixed decision 9's anomalies |
| 5: bodies, elements, seasons | troll, dwarf, centaur, gorrock, charrow, rimeling | Build's rooms, doors, stone, fuel and cold rooms; seasons that matter beyond winter; culture's holy days and guest customs (gorrock); crafting (dwarf) |
| 6: later | knellow, holtward, kilnborn, nightwright | omens (culture-faith); animals and hunting; constructs; night building |
| Kin of undead | cairnwake, wishwraith | the undead's `purpose` and its code. The cairnwake is data and looks, and its watch over graves comes with 2E. The wishwraith's wish, bound to a want about another person, is a small new behaviour |

### 12.4 Architected for, not built

- **The player's rolled race** (ADR-0024): any people whose needs exist can be the player's race. Its need then runs on the player as on anyone, and the player's levers stay intentions.
- **Hybrids:** the union of the parents' needs, averaged leanings, combined looks, merged customs.
- **Conversion** (bitten, cursed, raised), only through fixed decision 12's evolution machinery (question 7).
- **Threats** who can be talked down, tended and recruited.
- **Generated wanderers** of any people (`WandererSpecies`), biased toward unfilled court roles (fixed decision 12's niche). Question 2's presence draw is the step before it.
- **Earth folklore as ideas** (fixed decision 6): the player teaches what an Earth tale says ("bow to the thing in the river"), and the world reacts, rightly or wrongly depending on how far the derivative strays from the tale.
- **A mod hook** to register new need behaviours and acts.

## 13. Questions for the founder

Most important first, each with a recommended answer and why it matters. ADR-0025 took each recommended answer below unless the founder objects, so the builder builds them; they stay here so the reasons travel with the design. The readers question is gone, because ADR-0025 answered it with the derivative rule (§3).

1. **Approve 2A's schema and determinism changes, on batch 1's needs model.** One optional species field (`arrivesFrom`); the brimmet, its `head_water` need and Teal as entries in batch 1's formats; `JobKind.TendNeed`; `DiceId.CourtesyBow`. No new Truth.
   *Recommended: approve.* The first draft's plan stays withdrawn (§5).
   *Why it matters:* CLAUDE.md asks before schema and determinism changes, and every later people builds on whichever needs schema you accept, ADR-0022's included.
2. **Which peoples live in each world?** Batch 1 brings the cast to fourteen and 2A to fifteen. If every world holds every people, you have twenty companions besides yourself by 2D and twenty-one by 2E, past fixed decision 2's 5–20, and the roster has thirty-two.
   *Recommended:* the M0 ten are in every world. Each later people's cast member is present in a world by a Truth (`TruthId.CastPresence`, an optional cast.json field and a world.json number, four per world to start), so no world holds more than twenty. Built first in 2B, with one new soak baseline. Nobody in the cast is replaced. Like any Truth, who is in a world survives a rewind.
   *Why it matters:* it is a cast schema change and a new Truth that changes every world's arrivals, and it decides how every later slice is measured.
3. **Where is the line for derivatives, and for beings a living religion also speaks of?**
   *Recommended:* rules 2 and 3 as written. A derivative draws only on widely shared, non-sacred folk motifs and is never a thin rename of an excluded being. A being a living religion also speaks of appears only in its folk form, as the djinn of the tales and the Nights do. Nothing prayed to or venerated, and no scripture, prayer or sacred names as mechanics.
   *Why it matters:* your request asked for creatures from every culture and religion, and this is how the roster honours it without using what believers hold sacred. With no readers, these rules are the check, and they also decide batch 1's djinn lines at merge (§5).
4. **Is the ghoul's need acceptable for a companion, and where is the line on the buried dead?** It eats the dead. It never kills, eats only with leave from the dead's friends, carries shame when it breaks that, and carries an echo of each member it eats.
   *Recommended:* yes, with the gore stylised (ADR-0019), and a grave is never opened with leave, so only the night offence reaches a buried friend (§8.11). The ghoul comes with the slice of the dead (2E): if you answer culture-faith's question 5 "rites now", it moves up with it.
   *Why it matters:* it is the roster's clearest monster-as-person and its most sensitive entry, and the burial line decides whether a friend's grave can ever be opened with consent.
5. **How does an Earth resemblance show?** Naming is settled (ADR-0025): a people's name is our coinage, and a person's name has the people's structure and the world's sounds.
   *Recommended:* one line in the species tooltip, "Earth folklore tells of something like them: …", naming motifs, never a tradition, a country or a being's own name. It is the only place Earth appears; the world never names it.
   *Why it matters:* naming the Earth being would make each people read as that being renamed, which ADR-0025 rules out, and naming nothing would waste the Earth knowledge fixed decision 6 wants in play.
6. **How do the token peoples work?** Batch 3's tokens come from captivity stories: the vellet's skin, the fetlow's hair, the hushtail's pearl.
   *Recommended:* a token is only ever given, as a pledge to stay. Taking one is theft: it forms a grudge under the old consent rules and never gives control. The player never benefits from holding one, and giving it back leaves the person free to stay or go.
   *Why it matters:* it decides whether batch 3 can be built without making captivity a mechanic.
7. **Can someone become another people** (bitten, cursed, raised)?
   *Recommended:* not before fixed decision 12's M1. After that, only through its evolution machinery, as a rare, logged beat with a cause, and never something the player can aim at someone.
   *Why it matters:* conversion the player could aim would be control of a person's nature, against fixed decision 2.
8. **Should every need vary from person to person?** ADR-0018 lists the seam: everyone goes hungry on the same clock. This doc first proposed a Truth for the peoples' own needs only (`NeedTolerance`).
   *Recommended:* yes, as one change for every need, food and rest included, after 2A, accepting that every existing world changes.
   *Why it matters:* it is a new Truth that touches every world, so it should be asked and built once.
