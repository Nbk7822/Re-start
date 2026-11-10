# Peoples of myth: design

- **Date:** 2026-11-09, revised the same day after the builder's questions. **Status:** proposed, for the founder's sign-off.
- **Governs:** fixed decision 5 (peoples differ in needs, not stats), read with fixed decisions 1, 2, 4, 10 and 12.
- **Read with:** CLAUDE.md; BIBLE.md §4.4 (the vampire) and §5 (growth); ADR-0004 (determinism), ADR-0006 (events and stories), ADR-0007 (content), ADR-0013 (tuning in code), ADR-0017 to ADR-0021; and ADR-0022 (the first four peoples and the needs model they run on), recorded on the batch 1 branch (`worktree-wf_23280ce9-9be-1`) as "accepted for the build, for founder review". Companion designs in this folder, all on disk: `2026-11-09-build-design.md` (its grave and the act of laying the dead to rest, §4.8 and slice 8.4, are used here), `2026-11-09-zones-design.md`, `2026-11-09-schedule-design.md` and `2026-11-09-culture-faith-design.md`.
- **What the revision changed:** every people runs on batch 1's needs model (ADR-0022), so this doc's first plan (generic satisfier kinds, species.json v2, a vampire migration first) is withdrawn (§5, §8); the first slice is the kappa alone, and the ghoul moves to the slice of the dead, with graves and death rites (§12.1); which peoples live in each world goes to the founder (§13, question 2).
- **Sign-off approves:** the content rules (§3); the roster and the build order (§4, §12); 2A's content and schema changes in §9 (one optional species field, `arrivesFrom`; the `head_water` need, the kappa and Teal as entries in batch 1's formats; new thoughts and loc keys). These presuppose that ADR-0022 is accepted when batch 1 merges.

## Summary

1. A people is a body with a need the camp must answer and a heart shaped by that body and by the culture the person grew up in. Each people adds a need that someone else's choice, labour, food or time must meet. That collision is where unplanned stories start, as with the vampire turned down by all eight humans in the camp (STORIES.md).
2. Every people runs on the needs model batch 1 built (ADR-0022). `content/needs.json` gives each need its numbers, marks, thoughts, events and harm, which shared code runs; how a need is met or wounded is a behaviour named by its id, the way an incident's id names its behaviour. A kin people that shares a need is JSON alone. Code grows by need or act, never by species, and a mechanism two needs share is pulled out when the second arrives (§8).
3. The roster is 40 peoples from the folklore of Asia, Africa, Europe and the Americas. Oceania waits for readers and partners from its traditions (§3, rule 7). Each is listed with its need, the systems it touches, its ethics leanings, its threat form and its batch. Rules decide what is left out and why. Batch 2 gets full entries: kappa, ghoul, dokkaebi, vetala, werewolf, house spirits, duwende and mmoatia.
4. The first slice (2A) is the kappa alone (water in the dish on its head), on today's map and items. The ghoul (the flesh of the dead, eaten only with leave from the dead's friends) waits for the slice of the dead, with graves and death rites, because a camp member dies in only a few one-year worlds (§12.1).
5. Peoples bring their own clocks: a dish that dries every day, a moon every ten days, a bowl to fill every night. They are the first pressure the director gets in the middle of the year (ADR-0018).

---

## 1. Intent: why peoples, and the stories they make

Fixed decision 5 says peoples differ in what they need, not in numbers. The vampire shows why that works. Ilex cannot feed without a human saying yes, so every thirsty night puts a question to the camp, and the answer depends on who is hungry, who is weak, who likes Ilex and who was refused last time. One story strangers retold unprompted at the M0 exit check is that question asked eight times and answered "no" eight times.

Every people in this document must be at least that good. Its need must be something the camp can meet only through somebody else: their consent, their labour, their food, their silence, their courtesy, their time. A people whose need it can meet alone is a human in a costume, and is cut.

There are two further reasons.

- **The middle of the year is empty.** ADR-0018 records that most stories land in the last ten days, because the director has no mid-year pressure besides thieves and spoilage. Peoples bring their own clocks. A kappa's dish dries every day. A ghoul waits for a death. A werewolf's moon comes round every ten days. A house spirit's bowl must be filled every night. These run all year, and they collide with winter when it comes.
- **The player knows them.** The player is Earth-born and recognises a kappa or a ghoul from stories at home. The world honours what those stories say about the body: a kappa who is bowed to must bow back and spill its water. Everything about the person is the world's own: their culture, faith, language and name. A player who remembers the bow can save a friend with it. That is Earth knowledge used in play, the way fixed decision 6 intends.

Three stories the first batches should produce, as they would read in the Chronicle (names follow the M0 cast's style until cultures generate names):

**The bow at the watch post** (kappa: Social → Needs → Combat → Work → Social)
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

**The empty bowl** (house spirit: World → Needs → Incident → Needs → Social)
```
Day 31, 00:00 — Winter came, and frost took the berries from the bushes.
Day 33, 21:00 — When the camp shared out what little food it had, Pim's bowl at the hearth was left empty.
Day 34, 21:00 — Pim's bowl was left empty again.
Day 35, 02:00 — Pim, slighted, soured 3 berries in the stockpile.
Day 36, 09:00 — Nia is starving.
Day 37, 17:00 — Nia left the camp.
```

In the third story the house spirit's bowl is its ration, so the ration policy the player chose on day 1 decides whether it is filled. That is how the ration policy already links the player's first decision to an ending weeks later (STORIES.md, "Vale, refused by a starving camp").

## 2. Goals, non-goals, anti-goals

**Goals**
1. Peoples whose differences are needs, harms and feelings, never numbers. Every need is data, and every need touches at least two systems.
2. One needs model for every people, batch 1's (ADR-0022): each need is data plus one behaviour named by its id, so a kin people is a JSON entry and a new need is one behaviour.
3. A wide roster, chosen with care, built in an order set by story value first and code reuse second.
4. Hostile members of a people take the thief's path through the code. No people is hostile by nature.
5. More stories that end before winter.

**Non-goals (for this document)**
- **Stat differences.** No people is stronger, faster or smarter as a number. Every species keeps `ticksPerTile` 6. Body size matters only where the world must fit a body (beds, doors, stairs), and that comes with Build.
- **Hybrids, conversion and the Ideas Tree.** They are architected for (needs are a list, leanings are numbers, looks are parts) but not built. Conversion means being bitten, cursed or raised.
- **A knowledge layer** (who knows what about whom). The disguise peoples of batch 4 and the vetala's gossip need one. Batch 2 does not.
- **Generative text.** Every line is a template filled from stored state (fixed decision 10).
- **Aging.** Lifespans stay in this doc's entries until aging exists, and join content then (§9). Fixed decision 12 needs aging before its M1 acceptance anyway (ADR-0020).

**Anti-goals**
- A bestiary. A people ships only with a need that makes stories.
- Humans in costumes. If a people's need does not change what the camp must do, it is cut.
- Peoples as stand-ins for real ethnic groups, real religions, real dress or real speech.
- Good peoples and evil peoples. Hostility is a situation: a starving ghoul, a slighted house spirit, a wolf on a moon night. It is never a people's nature.
- A species panel, a species HUD or a spreadsheet of peoples. Peoples appear on the map, in the inspector, in tooltips and in the Chronicle (ADR-0008).

## 3. Rules for choosing and portraying peoples

These rules are part of the sign-off. A builder adding a people later applies them without asking.

1. **Folklore beings only.** A people comes from folk tradition: tales, legends, local belief.
   - A being that a living religion also speaks of may appear in its folk form. Djinn are the test case. Many Muslims hold that jinn exist, as the Qur'an describes. Return's djinn are the djinn of folk tales and the Thousand and One Nights: bargains, lamps, rings, contracts. So: no verses, no jinn named in scripture, no prayers used as wards, and no portrayal of belief in jinn as foolish. Oni likewise appear as the ogres of village tales, not as the wardens of Buddhist hells.
   - Never a god of any pantheon. Never a figure who is prayed to or venerated, or held to be a prophet, saint or angel. Never scripture, prayer or sacred names as a game mechanic: no verses as wards, no divine name animating a golem.
2. **Leave out what a source community holds sacred, or asks outsiders not to use.**
   - The *wendigo* belongs to the living teaching of Algonquian peoples (Anishinaabe, Cree and their neighbours) about greed and the taboo it guards, and many community members ask that it not be made into entertainment.
   - Diné people avoid speaking of *skinwalkers* and ask outsiders not to depict them.
   - Also out: the *thunderbird* and the *katsinam*; the *nāga*, *garuda* and *yaksha*, venerated in Hindu, Buddhist and Jain practice; the *taniwha*, kaitiaki (guardians) to Māori; *Mami Wata*, venerated across West and Central Africa and the diaspora; the *alux* of living Maya practice; the Hawaiian *huakaʻi pō*; the Chamorro *taotao mona*; and the ancestral beings of Aboriginal Australia, such as the bunyip and the Rainbow Serpent, which belong to Country and law and are shared on their custodians' terms.
   - Folk foxes stay. Inari's messenger foxes go.
3. **Leave out beliefs used today to harm real people.**
   - *Tokoloshe* and *impundulu*: witchcraft accusations in southern Africa.
   - *Bouda*: accusations against artisans and Beta Israel in Ethiopia.
   - *Aswang*: a stigma against the people of Capiz, and accusations against women.
   - *Àbíkú* and *ọgbanje*: children believed to be spirit children have been mistreated.
   - Fox possession, and the "fox-keeping" families of Japan.
   - In Return no real group carries a people as a secret identity, and nobody is accused of being a monster because of who they are.
4. **No caricature of real peoples.**
   - Out: the *kapre*, whose name comes from the colonial "cafre" and who is drawn as a dark-skinned giant; the *saci*, a contested racial caricature; the *abatwa*, whose name is also a name for a real people (the Twa); the *patupaiarehe*, used in pseudo-history that denies Māori first settlement; the *mouras encantadas*, enchanted "Moorish" maidens; the *pishtaco*, a folklore of real colonial violence; *changelings*, a belief used to justify violence against disabled children and women (the 1895 killing of Bridget Cleary is the best-known case); the *rakshasa*, epic demons long read as a picture of real South Indian and forest peoples.
   - Some figures are already stereotypes. The *leprechaun* of green suits and pots of gold is a stage-Irish caricature as much as a folk being, so he is left out. Dwarves get no greed-and-hoard caricature, which echoes antisemitic tropes. Dokkaebi get no horns and tiger skins, an image widely held in Korea to be a colonial-era import from Japanese oni.
5. **Peoples come from Earth's myths; cultures and faiths are invented for each world.** A kappa in Return is recognisably the kappa of Japanese tales; the Earth-born player knows it. It does not live in a Japan-like culture. Its culture, faith, language, script and dress are the world's own (culture-faith design). No people wears a real people's costume.
6. **Names: structure from the people, sounds from the world.** Each people has a naming *structure*: what a name is made of, who gives it, and when it changes (§6 lists them). The sounds come from the world's invented cultures. No people ever uses a real language's list of names, so no people becomes a stand-in for a real ethnicity: no Arabic names for ghouls, no Japanese names for kappa. The example names in this document (Teal, Morrow, Pim) follow the M0 cast's style and are placeholders.
7. **Readers from the tradition.**
   - Every people gets a reader from its tradition before it ships. ◆ marks peoples that need one before the design is final, because their risk is specific.
   - Some traditions keep most of their beings sacred: Indigenous North America, Aboriginal Australia and much of Polynesia. There the roster leaves the gap open rather than fill it. Any future entry from those traditions is made with their people, not about them (question 5).
8. **No people is evil by nature.** Threat forms are individuals in a situation (§4.3), never the whole people. A hostile ghoul is a ghoul who broke the ghouls' own code.
9. **Peoples, not individuals; folklore, not cryptids.** Single legendary figures are not peoples: Baba Yaga, La Llorona, Anansi, the Minotaur. Modern cryptids are not folklore peoples: the chupacabra, Mothman, and the sasquatch of the cryptid hunters (the Sts'ailes' Sasq'ets is theirs).

## 4. The roster

### 4.1 Forty peoples

How to read the table:
- **Leanings** are four numbers from −2 to +2, on the axes of fixed decision 5 in this order: Immortal/Mortal, Arcane/Mundane, Wild/Civil, Oathbound/Free. Positive leans to the first word, negative to the second. They are a people's default; each person's own value would be drawn around it (§10). They stay in this table until something reads them: culture-faith already derives each person's ethics from their culture and faith (§5, §9).
- **Touches** names the systems the need runs through. "Culture" means the culture-faith design; "Build", "Zones" and "Schedule" mean the companion designs.
- **Batch:** 0 exists; 1 is built on its branch (§5); 2A to 2E come next (§6, §12), and 2E, the ghoul, comes with the slice of the dead whenever that lands (§12.1); 3 to 6 come later (§12.3); R means readers first, not scheduled. "Kin" means the same need as another people: data only once that people's code exists.
- **◆** means a reader from the tradition is needed before the design is final (§3, rule 7).

| People | From | Distinct need | Touches | Leanings | Threat form | Batch |
|---|---|---|---|---|---|---|
| Human | Earth, and every world | food, rest, company | — | 0 0 0 0 | the night thief (exists) | 0 |
| Vampire (kin: strigoi, vrykolakas, upiór) | Southeastern and Eastern European folklore | blood, only given with consent; burns in daylight | Needs, Social, Work, Combat | +2 +1 0 0 | a vampire who feeds without consent (later) | 0 |
| Dryad | Greek myth (dryads, hamadryads) | forest close by, and one tree they are bound to | Needs, Work (felling), Mood, Social | +1 +1 +2 +1 | the wardens of the wood (leshy, curupira) when the camp fells too much | 1 |
| Nang tani | Thailand | one wild banana tree of her own | Needs, Work, Mood | +1 +1 +1 +1 | — | kin of dryad |
| Djinn ◆ | pre-Islamic Arabian folklore; the Thousand and One Nights; folk tales from North Africa to South Asia | contracts struck and honoured | Social (debts as terms), Work, Mood | +2 +2 0 +2 | the ifrit: a djinn whose contract was broken | 1 |
| Fae | Irish and Scottish aos sí, Welsh tylwyth teg, English fairy lore | cannot lie; their word must stay true | Social (promises, refusals), Mood | +2 +2 +2 +1 | a fair-folk abduction (later) | 1 |
| Undead (kin: revenant) | medieval English and French folklore, and the kin below | a purpose: work, or business left unfinished | Work, Mood, Needs | +2 +1 −1 +2 | the jiangshi (China) | 1 |
| Draugr | Norse | a purpose: the barrow and what lies in it, guarded | Work, Zones, Combat | +2 +1 0 +2 | the barrow lord (the art's Barrow King) | kin of undead |
| Gwisin ◆ | Korea | a purpose: the wish left unfulfilled at death | Social, Mood | +2 +1 0 +1 | — | kin of undead |
| Bhoot ◆ | South Asian folklore | a purpose: business left unfinished | Social, Mood, Travel | +2 +1 +1 +1 | — | kin of undead |
| Kappa | Japan | water in the dish on its head; it must return any bow | Travel, Social, Combat, Work, Incident (rain) | 0 +1 +1 +2 | the kappa at the ford who seizes swimmers | 2A |
| Ghoul ◆ | Arabian and North African folklore (ghūl, ghūla); the Nights | the flesh of the dead, never the living, eaten only with leave | Social, Needs, Combat (the slain), Build (graves), Culture (rites) | +1 +1 +1 +1 | the ghoul of the waste, who breaks the ghouls' code and raids graves | 2E |
| Dokkaebi ◆ | Korea | contests and play at night; repays a debt over and over | Social, Combat (bouts), debts, Work (gifts) | +1 +2 +1 +2 | none; a slighted dokkaebi's night of pranks | 2B |
| Vetala ◆ | India: the Vetala tale cycle (Vetālapañcaviṃśati, retold as Baital Pachisi) | new tales heard; rests hanging from a tree | Social, Mood (old wounds reopened), Work (felling its tree), Travel | +2 +2 0 +1 | none | 2B |
| Werewolf | Europe-wide: Greek lykanthropos, Germanic werwolf, French loup-garou, Slavic vukodlak | to run free on full-moon nights | Calendar, Travel, Combat, Social, Schedule, Zones | −2 +1 +2 −1 | the wolf in the night (the art's wolf) | 2D |
| House spirits (brownie, nisse, tomte, domovoi, kobold, zashiki-warashi) | Scotland and northern England; Scandinavia; East Slavic lands; German lands; Japan | a house kept, chores done unseen at night, a bowl filled every night | Needs (rations), Work, Schedule, Build, Social, Incident | +1 +1 −2 +1 | the boggart: a house spirit spoiled by neglect | 2C |
| Duwende and nuno sa punso (kin: duende) ◆ | the Philippines; the duende of Spain, Portugal and Latin America | leave asked when passing their mound or corner | Travel, Build, Zones, Work, Social, Needs (the curse) | +1 +2 +1 +1 | the mound's curse; no hostile body | 2C |
| Mmoatia ◆ | Akan folklore (Ghana, Côte d'Ivoire) | gifts given; speaks only in whistles | Social (go-between), Needs (gifts from rations), Work (herb lore), Growth | 0 +1 +2 −1 | small night thieves when gifts lapse | 2C |
| Selkie | Orkney, Shetland and the Scottish coast; Ireland; the Faroe Islands; Iceland | the sea, and their sealskin kept safe | Travel, Items, Social (trust, theft), Mood | 0 +1 +2 −2 | none | 3 |
| Merrow | Ireland | the sea, and their red cap kept safe | Travel, Items, Social | 0 +1 +2 −1 | none | kin of selkie |
| Swan maidens | European tales (Norse, German, Slavic) | open sky and water, and their feather cloak kept safe | Travel, Items, Social | 0 +1 +1 −2 | none | kin of selkie |
| Tikbalang ◆ | the Philippines | to roam the wild at night; serves whoever holds its golden hair, given and never taken | Travel, Items, Social, Combat | +1 +1 +2 +1 | leads walkers astray at night | 3 |
| Nøkk, näcken, fossegrim | Norway, Sweden | music made and heard by running water | Social, Culture (festivals), Mood, Travel | +1 +2 +2 0 | the drowning song | 3 |
| Iara ◆ | Brazil (of Tupi origin) | song at the river | Social, Travel, Mood | +1 +2 +2 −1 | the drowning song | kin of nøkk |
| Shurale | Tatar and Bashkir folklore | play: tickling contests in the woods | Social, Combat (bouts) | +1 +1 +2 −1 | tickles a lone walker half to death | kin of dokkaebi |
| Fox spirits (kitsune, huli jing, kumiho) | Japan, China, Korea | to keep their fox nature hidden; a fox pearl kept safe; a tail for every age | Social, knowledge, Items, Growth (tails) | +1 +2 +1 −1 | the predatory fox of many tales | 4 |
| Huldufólk and hulder | Iceland; Norway and Sweden | their stones left be; to stay unseen | Build, Zones, Social, knowledge | +1 +1 −1 +1 | someone taken into the hill | 4 |
| Orang bunian ◆ | the Malay world (Malaysia, Indonesia, Brunei) | to stay unseen; a hidden village nearby | Travel, Social, knowledge | +1 +2 +1 0 | someone taken into their village, back years later | 4 |
| Ciguapa ◆ | the Dominican Republic | to go untracked: feet turned backward, the night, the high forest | Travel, Combat (pursuit), knowledge | 0 0 +2 −2 | none | 4 |
| Troll | Norway, Sweden, Iceland, the Faroe Islands | to eat like three and sleep under stone; dawn turns them to stone, a landmark that stays | Needs (rations), Build (stone), Map, Mood | +1 +1 +2 −1 | the troll that raids the stores | 5 |
| Dwarf (dvergr) | Norse and Germanic | to make something with their hands each season; sunrise turns them to stone | Work (crafting), Build, Items | +1 +1 −1 +2 | none | 5 |
| Centaur | Greek myth | open ground to run; doors, beds and stairs too small for them; wine | Build, Travel, Culture (feasts), Social | 0 0 +1 −1 | a drunken raid | 5 |
| Oni | Japan | to be welcomed; the bean-throwing custom drives them off | Social, Culture (holy days), Combat, Work | +1 +1 +1 −1 | oni raiders | 5 |
| Salamander | Renaissance Europe (Paracelsus's fire elementals) and older bestiaries | a fire kept burning | Work (timber), Build (hearths), Incident (rain), Needs | +1 +2 +1 0 | a wildfire | 5 |
| Yuki-onna | Japan | cold: winter makes her strong, the hearth harms her; a secret kept | Needs (seasons), Build (cold rooms), Social (the secret) | +2 +2 +2 +1 | the freezing breath on winter nights | 5 |
| Bean sí | Ireland | to keen for the dying; she feels a death coming from the state of the body | Mood, Culture (omens), Social | +2 +2 0 +2 | none | 6 |
| Curupira ◆ | Brazil (of Tupi origin), a national folklore figure | the hunt kept fair: the young and the carrying spared | Work (hunting), Zones, Social | +2 +1 +2 +1 | the warden who leads greedy hunters astray | 6 |
| Golem ◆ | Jewish folklore (Prague, Chełm) | a task to do, taken to the letter | Work, Social (instructions), Build | +2 +1 −2 +2 | the golem that will not stop | 6 |
| Menehune ◆ | Hawaiʻi | night building that must be finished by dawn | Build, Schedule | — | — | R |
| Kalanoro ◆ | Madagascar | decided with readers | — | — | — | R |

The table is honest about its gaps. Sub-Saharan Africa has one scheduled people and Oceania none, because rules 2 to 4 exclude most of the beings outsiders know from there. Filling those gaps means paying readers and partners from those traditions (question 5), not reaching for the excluded beings.

### 4.2 How each reads at 32–48 px (peoples without a full entry)

ADR-0019's procedural figures are about 39 units tall; every people must read at 32–48 px (BIBLE.md §3). These are the silhouette cues the art builder starts from. The batch 2 entries in §6 have fuller notes.

| People | Reads as |
|---|---|
| Dryad, nang tani | a person with a leaf crown and bark-dark arms; the crown thins as their need falls |
| Djinn | a tall figure with no feet: the cloak's hem ends in a curl of smoke; a faint ember glow at night |
| Fae | too tall and too thin, with long fingers and a narrow head; no chest mark, because fae hide what they do |
| Undead and kin | a stiff, slightly forward-leaning stance; grave-grey cloth; a draugr is broad and swollen, a gwisin or bhoot pale with hair falling forward |
| Selkie, merrow, swan maidens | a person wearing the token: a spotted grey mantle, a red cap, a white feathered cloak. Without it they look plainly human, and *that* reads as loss |
| Tikbalang | very tall and bony; a long horse head; knees above the waist when crouched |
| Nøkk, iara | a figure kneeling at the water's edge with a fiddle or loose hair; seen mostly at night |
| Shurale | thin, with very long fingers and a single horn |
| Fox spirits | a person with one to nine tails that show only when the fox is revealed (batch 4) |
| Huldufólk, orang bunian, ciguapa | barely drawn: seen at dusk at the map's edge. A ciguapa leaves footprints in the mud that point the wrong way (GroundWear) |
| Troll | twice a person's size (the Barrow King's scale); a lumpy, mossed outline; a stone grey that deepens toward dawn |
| Dwarf | half height and very broad; a long beard; a leather apron |
| Centaur | a horse's body under a person's torso, 1.6 tiles long; the only four-legged person |
| Oni | big, horned, thick-armed; skin in a dark red or slate blue, muted |
| Salamander | small and lizard-like on two legs; a glow under the skin that brightens near fire and greys in rain |
| Yuki-onna | white and still; frost on the ground around her feet; breath that shows even indoors |
| Bean sí | a hooded figure washing cloth at the ford; long grey hair |
| Curupira | a small figure with red hair and feet turned backward |
| Golem | a squat clay-brown block with a mark on the brow in the world's own script |

### 4.3 Threat forms

A threat is one person in a situation: a ghoul starving with no one who will give it leave, a house spirit slighted for a week, a werewolf whose moon night is penned in. It is never the whole people.

- **In code, threats take the thief's path.** `IncidentSystem.SendThief` makes a `CharStatus.Hostile` character with a `Raid` job, and `CombatSystem.Step` handles only `Raid` and `Flee`. So the first threat forms are raid-shaped: they go for a target tile (the stockpile, a body, a grave), are spotted and fought by the watch and the Protective, and flee or fall.
- Threats that are not raids (the kappa who seizes a swimmer, the tikbalang who leads walkers astray) wait for later slices, which extend `CombatSystem` beyond `Raid` and `Flee`.
- **Members of the threat's own people react.** The shame thoughts in §6 are what make a hostile ghoul a story about Morrow, who lives in the camp.
- **Threats can later be talked down.** A beaten raider who is cared for rather than chased off can become a wanderer who asks to stay. That needs the incident library's next pass and is listed in §12.4.

Some beings appear only as threats, because the folklore gives them no community life to build a people on:
- the *mare* (Germanic and Slavic), who sits on a sleeper's chest: rest lost, a bad night;
- the *nishi* (Bengal), the night voice that calls a sleeper by name;
- the *karakoncolos* (Balkans and Anatolia), who waylays people on the coldest nights;
- the *kelpie* and *each-uisge* (Scotland), the water horse at the ford;
- the *asanbosam* and *sasabonsam* (Akan), hunters that hang from forest trees;
- the *zimwi* and *marimu* (Swahili and Kikuyu tales), ogres who pass as people;
- the *ma da* and *shuǐguǐ* (Vietnam and China), drowned dead who seek someone to take their place.

## 5. Batch 1: built on its branch

Dryads, djinn, fae and undead are built on the batch 1 branch (`worktree-wf_23280ce9-9be-1`), with one cast member each (Dryope, Qamar, Fionn, Ketil), and recorded in ADR-0022. Batch 1 also built the needs model every later people uses (§8). Batch 2 builds on it and rewrites nothing in batch 1.

| People | Need id and behaviour | What batch 2 uses from it |
|---|---|---|
| Undead | `purpose` (`Natures/Purpose.cs`): restored while doing the one work they are bound to (a Truth); fading harms them | `foodSource: "none"` and `tires: false`; a need bound to a work (`bondWorks`); the player's "never" order as a cause |
| Dryad | `forest` (`Natures/Forest.cs`): living trees within reach feed it, their own tree triple; `JobKind.TendTree` | a need bound to a thing (`NatureNeed.BondId`); the felling hook (`Forest.Felled`), which the vetala's hanging tree joins in 2B; `refusesWork`; `EffectKind.BondSevered` |
| Djinn | `contract` (`Natures/Bargains.cs`): restored while a bargain is open | bargains as records (`World.Contracts`; `EffectKind.ContractFormed`, `ContractKept`, `ContractBroken`); consent from state, as for blood; the schedule's bound hours read these records |
| Fae | `courtesy` (`Natures/Courtesy.cs`): moved only by events (words kept or broken, talk, insults) | `cannotLie`, `grudgeDecayEveryDays`, `DebtKind.Welcome` |

- **Common to all four:** `content/needs.json` and its schema (start, decay, marks, thoughts, events, harm while critical, hours critical before they leave, bonds, integer params); `RequiredContent.Needs`, which lists each need id with the params its code reads; `arrivalKey`; and cast.json's `arrivalDays` window, drawn after the shuffled wanderers so no other arrival moves.
- **The first draft's joining rule is withdrawn.** It planned to absorb batch 1 into generic satisfier kinds with byte-identical soak logs. Batch 1 landed first, as that rule allowed, and its model follows the precedent ADR-0018 #4 already set for incidents: behaviour named by id, numbers in content, ids checked at load. Rewriting four working, tested peoples would make no new story, so the absorption does not happen, and neither does the choice between identical logs and the old rule if it failed. §8.3 keeps identical logs as the test for later refactors.
- **The leanings in §4.1 stay in this doc.** Batch 1 has no species leanings, and culture-faith derives each person's ethics from their culture and faith (its §4.6). Species leanings join content when something reads them (§9).
- **Threats need no extra work.** The batch 1 threat forms (warden, ifrit, barrow lord) are raid-shaped and can follow at any time.
- **For the djinn's reader (◆).** ADR-0022 and a code comment frame the djinn through "Islamic tradition" and call Qamar "the djinn built for Sulayman", and the arrival line calls a djinn "a being of smokeless fire". Rule 1 keeps the folk djinn of the tales and the Nights. The reader checks these lines against it before the djinn's design is final.

## 6. Batch 2: full entries

Eight peoples, chosen for story value first, then code reuse, then reach across regions (Japan, the Arab world and North Africa, Korea, India, Europe, the British Isles, Scandinavia and the Slavic lands, the Philippines, West Africa). Each entry gives: where the people comes from; the body; the need, with how the sim meets and frustrates it and which systems it runs through; the heart; how they are with others; three story seeds as Chronicle lines; the silhouette; names; the threat form; what must exist first; and hooks for fixed decision 12. Thought values are starting points for tuning (ADR-0013). Every new thought uses one of the existing emotes: anger, sweat, hearts, sparkle, alert, gloom, music, none.

Build order inside batch 2 (§12): **2A** the kappa; **2B** dokkaebi and vetala; **2C** house spirits, duwende and mmoatia (they share the `offering` mechanism; that is deliberate code reuse); **2D** werewolf; **2E** the ghoul, with the slice of the dead (§12.1).

### 6.1 Kappa — Japan (2A)

**From.** Japanese folklore, where river creatures go by many local names (kawatarō, gawappa, kawako and more). In the tales a dish of water on the crown carries a kappa's strength. A kappa must return any bow, and so spills it. A bested kappa keeps its pledge. One kappa, who had lost an arm, traded the secret of bone-setting to get it back.

**Body.**
- **Build:** child-sized to small adult, with a beaked face, a shell on the back, webbed hands and feet, and a shallow dish in the crown that must hold water.
- **Lifespan:** long; data 80–200 years.
- **Food:** eats what the camp eats. That is berries for now; fish and cucumbers, its delights, come when items exist.
- **Sleep and senses:** sleeps at night, beside water when it can. Sharp-eyed in water, dull in dust.
- **What harms it:** a dry dish, long sun on the dish, fire.
- **How it dies:** of drying out (dish dry, health falls, collapse, death) or of wounds.

**Need: `head_water`** (a need of its nature, in batch 1's model; §8.10).
- **Drain:** 40 an hour, 80 in open daylight. A kappa arrives with a full dish (1000). Low below 350, critical below 150. While critical it loses 20 health an hour (batch 1's rule: harm runs while a need is critical). At zero it also collapses where it stands, and stays down until someone carries it to water.
- **Met by:**
  - standing beside water: any passable tile next to a water tile, +500 an hour. The `TendNeed` job reads "Wetting their head-dish at the river".
  - rain on an uncovered kappa: +150 an hour.
- **Frustrated by:**
  - **A bow** (the act `bow`). When someone chats with a kappa, a Dice draw (Social stream, `DiceId.CourtesyBow`) decides whether they bow. The kappa always bows back, and the dish spills (−400). The bow event becomes the cause of the drying that follows.
  - **Duties that keep it from the river:** a promise, a patient, the night watch. The order of concerns and the interrupt rule decide which wins (§8.5).
  - **Being downed far from water.**
- **Once the dish is low:** it fights at half its hit chance, because the water is its strength. A rescuer carries a downed kappa to water, not to a bed. Both are `head_water`'s behaviour, tuned by its params (§8.2, §8.3).
- **Touches:** Social (the bow), Travel (to the river), Combat (weakness), Work (rescue, care), Incident (rain).
- **Later:** Build (a trough or pond lets a kappa live away from the river) and Culture (where a culture greets with a bow, spills are common).

**Heart.**
- **Delights:** rain on the dish (`kappa_rain` +5, sparkle); a full dish at dawn (`dish_full` +3, music); being bowed to, because it loves courtesy even when courtesy costs it (`bowed_to` +2, music); a fair contest, won or lost (2B).
- **Fears:** drought and dust; the dish drying in the open (`dish_drying` −6, sweat; `dish_dry` −14, gloom); fire.
- **Shames:** being tricked into spilling by someone who knew (needs the knowledge layer); breaking a pledge, which it almost never does.
- **Griefs:** a drowned friend. The tales blame kappa for drownings, and each drowning weighs on it.

**With others.** Courteous to a fault, slow to take offence, and it never forgets a pledge. A species flag, `keepsPledges` (built with the pledge, in 2B), puts its pledges ahead of its own need, even a critical one: a kappa keeps its word until its dish runs dry and it falls. Cultures may fear kappa for the drownings (culture-faith design). Leanings 0 +1 +1 +2.

**Story seeds.**
1. The bow at the watch post (§1): Social → Needs → Combat → Work → Social.
2. The pledge (2B, needs the contest act):
```
Day 9, 20:15 — Oren beat Teal at wrestling by the river.
Day 9, 20:15 — Teal pledged to tend Oren's wounds whenever he is hurt.
Day 23, 02:55 — Oren was struck down by the thief.
Day 23, 03:05 — Teal left the river, its dish still low, to tend Oren's wounds.
Day 23, 10:35 — Teal's dish ran dry, and Teal collapsed.
Day 23, 11:10 — Mira carried Teal to the river.
Day 23, 11:10 — Teal owes Mira for the rescue.
```
3. The bow as a rescue (the threat form, later):
```
Day 17, 21:05 — Something in the river seized Nia at the ford.
Day 17, 21:06 — Mira bowed to it, and it bowed back; the water spilled from its head-dish.
Day 17, 21:06 — The river kappa let Nia go and fled.
Day 17, 21:06 — Nia owes Mira for the rescue.
```

**Silhouette.**
- Four-fifths of a person's height, hunched. A domed shell breaks the line of the back where others have a cloak.
- A flat crown with a pale disc that glints when the dish is full and goes dull when it is dry, drawn from the need's value.
- A short beak in profile and splayed webbed feet, in olive and slate, darkened. The role mark sits on the plastron.

**Names.** A kappa is known by its stretch of water and by a name its elders give after its first fair contest ("Teal of the Deep Bend"). It signs its pledges with that name.

**Threat form.** The river kappa at the ford, who seizes someone crossing at night. This needs a combat job beyond `Raid` (later).

**Needs first.** Batch 1's needs model (§8). Beyond it, nothing: water tiles, rain, rescue and care all exist. The pledge waits for the contest act (2B).

**Growth (fixed decision 12).**
- **Wants:** to be bested fairly; to keep a pledge to the end; to see the river full again.
- **Evolution, on a naming or a kept pledge:** *river elder* (its dish holds more, and others bring disputes to it) or *bone-setter* (its care heals more, and when it teaches a human, the human's trait mutation cites the teaching event).

### 6.2 Ghoul — the Arab world and North Africa (2E, with the slice of the dead) ◆

**From.** Arabian folklore: the ghūl (female ghūla), from pre-Islamic poetry to the Thousand and One Nights, and the ghoula of Maghrebi tales. It is a desert being that haunts graveyards and lonely roads and eats the dead. Old Arabic lore says one blow kills a ghūl and a second brings it back. Under rule 1 this is the folk ghoul, with no religious framing. Under rule 6 there are no Arabic names and no desert-bazaar dress: ghouls live in the world's own cultures.

**Body.**
- **Build:** gaunt, long-armed and stooped, with eyes that catch the light at night.
- **Lifespan:** long; data 150–300 years.
- **Food:** the flesh of the dead. Ordinary food does not nourish it.
- **Sleep and senses:** sleeps by day. The sun does not harm it; it is simply made for the night. It smells death from far off and knows when a body lies near.
- **What harms it:** hunger, wounds, fire.
- **How it dies:** of wounds, or slowly of hunger. Its hunger is the slowest of any people: days between meals.

**Need: `dead_flesh`.** It replaces food. The ghoul eats nothing from the stores (`foodSource: "none"`, batch 1's rule), and its hunger is a need of its nature.
- **Drain:** 8 an hour. Hungry (low) below 300, starving (critical) below 150. While starving it loses 10 health an hour.
- **Met by** eating a portion of a body (three to a body, +600 each) or of an old grave. Two old graves lie by the ruins, two portions each, placed from Truths (§8.11). They feed a newcomer for about twelve days, and then the camp's own dead become the question.
- **The ghouls' code:** every ghoul in camp keeps it. A ghoul never kills to eat, and eats a camp member's body only with leave.
  - **Who gives leave:** the dead's friends, meaning every member whose opinion of the dead was 30 or more. That is the line `friend_died` already uses. The sim has no family graph yet, so friendship stands in for kin.
  - **When it asks:** the act `ask_leave`, at the first dusk after the death (19:00–22:00, while the ghoul and the friend are both awake): "Morrow asked Mira's leave to eat what was left of Bram."
- **How a friend answers:** from state, never from Dice, as with consent to give blood. A friend refuses if their opinion of the ghoul is below the required level, or if their mood is low, or (later) if their faith forbids it. Otherwise they grant leave.
  - **Leave granted:** the ghoul eats and owes the friend (`DebtKind.Leave`).
  - **Any refusal:** the ghoul must not eat that body.
  - **No living friends:** no leave is needed ("nobody had loved Hollis enough to say no"). A dead thief belongs to nobody.
- **Frustrated by** refusal while starving. A ghoul who is Desperate (the trait that takes others' meals) goes to the body in the night anyway.
  - Each friend learns of it when they next wake ("Mira found what had been done to Bram"). They take a heavy opinion blow and usually form a grudge.
  - The ghoul carries the shame (`fed_without_leave`, a long thought).
- **Burial closes the body** (lead's call; the content line is question 4). The ghoul comes in the same slice as the build design's graves and act of laying the dead to rest (its §4.8 and slice 8.4), so the two are built together (§8.11):
  - The ghoul asks at the first dusk after a death. While the camp's "When someone dies" order is *Ask their friends first* (the default), a grave for that member waits until the asking is over (§7.4).
  - Nobody is asked to open a grave, and a buried member is never eaten with leave: burial is the camp's answer. Old graves are the exception, because nobody living loved those dead.
  - A starving, Desperate ghoul may still dig at night. That is the worse offence: the dead's friends and whoever buried them find the grave open, the blow to their opinion is heavier, and the body lies unburied again until someone lays it to rest.
  - Whether a culture buries, burns or leaves its dead belongs to culture-faith's death rites (§11). A culture that burns its dead leaves the ghoul nothing.
- **Touches:** Combat and Needs (deaths make bodies), Social (leave, refusals, debts, grudges), Mood (witnesses), Build (graves), Culture (rites).
- **Later:** Work (the ghoul as the camp's undertaker, carrying the dead and digging graves).

**Heart.**
- **Delights:** leave freely given (`fed_with_leave` +8, hearts, with warmth toward the giver); the quiet of night; being trusted with the dead.
- **Fears:** fire (a people that burns its dead means famine); being hunted; being seen eating.
- **Shames:** eating without leave (`fed_without_leave` −10, gloom, five days); being called a scavenger to its face.
- **Griefs:** a ghoul grieves its friends like anyone, and hungers at the same time. The inspector shows both: "A friend died (−25)" and "The dead are calling (−6)". That pair is the people.

**With others.** Patient, nocturnal and watchful: good on the night watch, uneasy around fires and feasts. How humans take a ghoul belongs to their cultures: someone who sees a ghoul eat gets `saw_dead_eaten` (−6, gloom, by default) through their culture's attitude toward ghouls (culture-faith §5.10), and takes it harder in a culture that fears them. Leanings +1 +1 +1 +1.

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
3. The debt of leave:
```
Day 12, 19:40 — Oren gave Morrow leave to eat what was left of Hollis.
Day 12, 19:40 — Morrow owes Oren for the leave.
Day 29, 02:55 — Oren was struck down by the thief.
Day 29, 03:10 — Morrow carried Oren to safety.
Day 29, 03:10 — Morrow repaid Oren.
```

**Silhouette.** The art's existing ghoul (`FigureLook.Ghoul`): hunched, a ragged pointed hem, grey-green cloth, the head below the line of the shoulders, arms past the knees. Two pale points for eyes at night while it is awake. It eats in the existing crouch pose over the body. The view draws the remains smaller as portions go, stylised: a covered shape, then bones, never a close-up.

**Names.** A ghoul takes a second name from the first of the dead it was trusted with ("Morrow, who was trusted with Hollis"). Friends use the first name. The second is spoken at its own death.

**Threat form.** The ghoul of the waste, who has broken the code. It is raid-shaped: it comes at night for a body or a grave and is spotted and fought like a thief. Later: strike it once; a second blow revives it.

**Needs first.** Deaths common enough to feed it: aging and multi-year worlds, which ADR-0020 needs before M1 anyway (§12.1 gives the numbers). It comes as one slice with the build design's graves and act of laying the dead to rest (its slice 8.4), culture-faith's death rites (its slice 2) and the Zones burial ground, whose timing is answered once, as culture-faith's question 5. Its core needs no new body state, because a dead `Character` keeps its `Pos` and stays in `World.Characters`; the old graves are new map features (§8.11).

**Growth.**
- **Wants:** to be buried, not eaten, when it dies; to be trusted with the dead.
- **Evolution:** *undertaker* (keeps the camp's dead; the grief of the dead's friends eases sooner once it has tended the body) or *the hungry one* (a decline: a ghoul refused too often begins to wait on the dying; fixed decision 12's decline).

### 6.3 Dokkaebi — Korea (2B) ◆

**From.** Korean folklore. Dokkaebi are goblins, not demons. They are born from old household things (a broom, a pestle, a pot) that held human blood or long use. They love wrestling (ssireum) and song, reward the good-hearted and fool the greedy, and a magic club (bangmangi) brings them what they wish for. Two tales matter here. In one, dokkaebi took an old man's lump because they thought his song lived in it. In the other, a dokkaebi borrowed money and came back every night to repay it, forgetting that he already had.

**Body.**
- **Build:** broad, squat and very hairy, with big hands and a wide grin; strong for its size.
- **Death:** a dead dokkaebi falls back into the old thing it came from, and a worn broom lies where it fell (a lasting mark on the map).
- **Lifespan:** as long as its thing lasts; data 100–400 years.
- **Food:** eats what the camp eats. That is berries for now; buckwheat jelly and rice wine, its delights, come when items exist.
- **Sleep:** sleeps by day and wakes at dusk.
- **What harms it:** wounds; fire, because its thing burns.

**Need: `play`.**
- **Drain:** 30 an hour while awake. Low below 300, critical below 150. At zero there is no harm, but its mood falls fast and the mischief starts.
- **Met by** the `contest` act at night: a challenge to wrestle, sing or guess.
  - **Acceptance** is decided from state: is the partner awake and rested, what is their mood, what do they think of the dokkaebi.
  - **The bout** is a few Dice draws (`DiceId.ContestBout`). There are no wounds, only rest spent.
  - **Win or lose, the need is met.** The loser is teased. Dokkaebi lose cheerfully; a Proud human does not.
  - **A dokkaebi that loses leaves the winner a gift at dawn.** It forages in the night and leaves the yield in the stockpile, set aside for the winner as a `Reservation`. Nothing appears from nowhere.
- **The repayment quirk.** When a dokkaebi repays any debt, it keeps repaying. Each night a Dice draw (`DiceId.RepayAgain`) decides whether it remembers that it already has. If it does not, it brings the creditor another set-aside share. The creditor grows rich in set-aside meals, which matters in winter (story 1).
- **Frustrated by** being refused or ignored too long. It then plays pranks (the act `prank`): scattering the timber stack so it must be hauled again, souring a berry, hiding a bedroll. Each prank cites the refusals before it, so refusals lead to a prank, an insult and a grudge.
- **Touches:** Social (challenges, refusals, pranks), Combat (bouts reuse the hit roll without damage), debts (repayment loops), Work (foraging at night for gifts), Needs (rations, through set-aside meals).

**Heart.**
- **Delights:** a good bout; a song; being outwitted cleverly (`outwitted` +4, sparkle); a full stomach.
- **Fears:** being thrown away (its thing discarded); fire; being forgotten.
- **Shames:** losing to a cheat; being caught in its own trick.
- **Griefs:** the death of the person whose blood made it. Who that is would be a Truth, for later.

**With others.** Loves company, adores the bold, teases the proud and punishes the greedy. A dokkaebi that sees someone take another's meal (`event.social.took_meal`) pranks the taker; this is a reaction. Leanings +1 +2 +1 +2.

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
3. The broom by the hearth:
```
Day 25, 02:20 — Jink fought the thief.
Day 25, 02:31 — Jink was struck down by the thief.
Day 25, 02:40 — Jink died, and fell back into an old broom.
Day 25, 07:00 — Wren kept the broom by the hearth.
```
Keeping the broom is a later beat. A kept broom might wake again as the same dokkaebi with its memories gone (fixed decision 12's memory rules).

**Silhouette.** A wide, squat, shaggy outline; a big round head with a wide grin; a knobbed club over one shoulder; muted ochre and earth tones. No horns and no tiger skin (rule 4).

**Names.** A dokkaebi is named for the thing it came from and the house that thing served ("Broom-of-the-Third-House"). Whoever beats it at wrestling gives it a new use-name.

**Threat form.** None that is hostile. A slighted dokkaebi's night of pranks is an incident.

**Needs first.** The `contest` and `prank` acts. Both are new code running on existing systems: Social, Combat's roll pattern, and the set-aside meals in Rations.

**Growth.**
- **Wants:** to find a worthy wrestler; to be remembered by the house.
- **Evolution:** *club-bearer* (its gifts come more often, and its pranks are worse) or *house goblin* (settles into one family's service and becomes kin of the house spirits).

### 6.4 Vetala — India (2B) ◆

**From.** India: the Sanskrit tale cycle Vetālapañcaviṃśati ("Twenty-five tales of the vetala"), retold as the Hindi Baital Pachisi and in modern Vikram and Betaal tellings. A vetala is a spirit that inhabits a dead body, hangs upside down from a tree, and tells tales that end in riddles. **Care:** in parts of the Konkan coast a guardian called Vetal is worshipped as a village deity. Return's vetala is the storyteller spirit of the tales, never that god. There are no cremation-ground rites and no king from the tales.

**Body.**
- **Build:** a spirit wearing a borrowed body: grey, light and cold to the touch, with long hair falling forward.
- **Lifespan:** immortal while its body lasts. The body wears out over years. That becomes a later need, sharing the ghoul's way of eating from the dead (§8.3), which is why the ghoul and the vetala will one day want the same dead.
- **Food:** none.
- **Sleep:** it rests only hanging upside down from a standing tree. Elsewhere it barely rests.
- **Senses:** it hears whispers and remembers every tale exactly.
- **What harms it:** fire; its tree felled while it hangs there; long silence.
- **How it dies:** when its body is destroyed, the vetala is gone (a death).

**Need: `tales`.**
- **Drain:** 25 an hour. Low below 300, critical below 120. At zero it fades: no harm, but its mood falls to the floor and it may leave.
- **Met by** the `ask_tale` act. It asks someone to tell what happened to them: a recent event that names them, carries a persistent effect (a grudge, a debt, a death they saw, a departure), and that the vetala has not heard.
  - The partner tells it, or refuses, from state (mood, opinion).
  - A new tale gives +300. The vetala remembers the event id in a short list on the person (fixed decision 12's `memories[]`).
- **The cost to the teller: telling an old hurt reopens it.** The sim re-adds the matching thought about the same person, citing the original event; for example, Bram's `meal_stolen` about Nia comes back. That can push an opinion past the grudge line. The grudge then cites both the telling and the theft. Old wounds reopen without any knowledge layer.
- **Rest** is at a place: a standing tree within one tile. Its tree is the one it last slept at. If someone fells that tree, a reaction to `event.work.felled_tree` gives it grief and a grudge against the feller.
- **Touches:** Social (tales, refusals), Mood (reopened thoughts, grudges), Work (felling), Travel (to its tree), and fixed decision 12's shared memories.

**Heart.**
- **Delights:** a new tale (`heard_a_tale` +6, sparkle); a clever listener; a riddle nobody can answer.
- **Fears:** silence; being forgotten; fire.
- **Shames:** telling a tale wrong.
- **Griefs:** when the last person who knew a tale dies, the vetala mourns the tale (`a_tale_lost` −8, gloom).

**With others.** Curious and intrusive. It asks about painful things, so a refusal may come with an insult. Loyal to those who tell it good stories. Leanings +2 +2 0 +1.

**Story seeds.**
1. Old wounds (the chain runs back to the theft and the winter that caused it):
```
Day 21, 22:00 — Ossa asked Bram to tell how his meal was taken.
Day 21, 22:05 — Bram told the tale, and it all came back to him.
Day 21, 22:05 — Bram now holds a grudge against Nia.
```
2. The hanging tree:
```
Day 16, 10:30 — Sera felled a tree for timber.
Day 16, 10:30 — Ossa's hanging tree is gone.
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

**Silhouette.** Tall, thin and grey, with hair falling forward and a shroud-like wrap. At rest it hangs upside down beneath a tree's canopy. No other people has that pose, and it reads at 32 px. Its eyes are closed while it listens.

**Names.** A vetala is known by its tree and by the tale it tells best ("Ossa of the river oak, who tells of the thief in the rain"). When its tree falls, it takes a new name from the next one.

**Threat form.** None in batch 2.

**Needs first.** The `ask_tale` act, which reads the event log and renews thoughts. Rest gains an optional place, using the kappa's `TendNeed` job (§8.3); everyone else keeps the default of resting anywhere. Nothing else.

**Growth.**
- **Wants:** to hear the tale no one will tell; to keep the camp's chronicle.
- **Evolution:** *chronicler* (a court role; its tales lift the court's mood at the hearth) or *gossip* (a decline: it starts telling tales to the wrong people, which needs the knowledge layer).

### 6.5 Werewolf — Europe (2D)

**From.** Across Europe: the Greek lykanthropos, the Germanic werwolf, the French loup-garou, the Slavic vukodlak and volkolak, the Lithuanian vilkolakis and the Latvian vilkacis. Old tales tie the change to a wolf-skin, a belt or a curse; later tales tie it to the full moon. Return uses the moon because it is a clock the whole camp can see.

**Body.**
- **Build:** a person, usually born human and sometimes of a wolf-blooded line, who turns wolf for a night.
- **Lifespan:** human.
- **Food:** eats what the camp eats; as a wolf, it hunts (later, with animals).
- **Sleep:** at night, restless before the moon.
- **Senses:** keen smell and hearing even in human shape. Later this gives an early warning of a thief.
- **What harms it:** being shut in on a moon night; being seen changing (shame); silver (later, with items).
- **How it dies:** as people die. A wolf killed is the person killed.

**Need: `moon_run`.**
- **When it bites:** silent between moons. The calendar gains a moon (`world.json`): a ten-day cycle, so a full moon every season. On a moon night the need drains fast, 200 an hour from dusk.
- **Met by running:** hours spent beyond a set distance from the hearth (the place mechanism, in a `roam` form). Meeting it in full takes a night away. The werewolf comes back at dawn, tired.
- **Frustrated by being kept in camp:**
  - What keeps it in: a duty, a promise, the Schedule choice "keep them in" on moon nights, or a closed room set aside in Zones.
  - Below critical, the change comes where it stands. For the rest of the night the person is a wolf: still a member, but flagged `Changed`, drawn as the art's wolf and given a raid-shaped job on the stockpile or the nearest person. `CombatSystem` treats a changed member as a raider, and the camp fights it as it would a thief.
  - At dawn the flag clears and the wolf is the person again. Every hurt done in the night carries over as thoughts, grudges and debts.
- **Touches:** the calendar (a new moon field), Travel, Combat, Social, Mood; Schedule and Zones as the player's intentions.

**Heart.**
- **Delights:** the run, the moon, the smell of rain (`ran_the_moon` +8, sparkle).
- **Fears:** being penned; hurting a friend; being watched while changing (`seen_changing` −8, sweat).
- **Shames:** the morning after a wild night (`woke_bloodied` −12, gloom).
- **Griefs:** anyone it hurt.

**With others.** Pack loyalty: it bonds faster with a chosen few (chats with them warm twice as much) and is wary of strangers. The curse is no secret in batch 2, which has no knowledge layer; it becomes one in batch 4. Leanings −2 +1 +2 −1.

**Story seeds.**
1. Kept in. The player's schedule is the first link, as the ration policy is today:
```
Day 14, 19:00 — The moon rose full.
Day 14, 19:05 — Hask stayed on watch through the moon night, as the schedule asked.
Day 15, 01:30 — Hask changed, and the wolf broke loose in the camp.
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

**Silhouette.** In human shape: lean and rangy, in a heavy fur mantle whose hood carries two wolf ears (the old tales' wolf-skin). As a wolf: the art's existing wolf at one tile, the coat darkened, with a scrap of the person's cloth colour at the neck so the player knows who it is.

**Names.** A werewolf takes its culture's names, plus a pack name given on its first run and used only by those who run with it.

**Threat form.** The wild wolf of the moon night (raid-shaped, the art's wolf), whether a stranger or a member who was penned in.

**Needs first.**
- A moon in the calendar (`world.json`, a schema change).
- A `Changed` flag for the night, rather than a status flip. Setting the status to Hostile would drop the wolf from `World.Members()`, so needs, mood, rations and talk would all skip it. `Lifecycle.Die` would then treat a werewolf killed in wolf form as a stranger nobody mourns. The flag keeps the person a member while `CombatSystem` treats it as a raider. This is new, and it touches Combat and Social.
- The Schedule and Zones seams for the player's levers. The need works without them; the werewolf simply goes.

**Growth.**
- **Wants:** to be rid of the wolf, or to master it.
- **Evolution, on a milestone** (a moon run shared with a chosen companion, or a founding): *moon-tamed* (the change waits for its will) or *wolf-blooded* (stronger runs and longer nights away; harder to keep in).

### 6.6 House spirits — brownie, nisse, tomte, domovoi, kobold, zashiki-warashi (2C)

**From.** One people with many names, because the folklore of these traditions agrees on the need:
- the brownie of Scotland and northern England, and the boggart, a brownie gone bad;
- the nisse and tomte of Scandinavian farms;
- the domovoi of East Slavic homes, who lives behind the stove and moves with the family when embers from the old hearth are carried to the new one;
- the German kobold;
- the zashiki-warashi of northern Japan, a child spirit whose leaving brings a house to ruin.

**Body.**
- **Build:** small (two-thirds of a person or less), shaggy and old-looking; very strong for its size.
- **Lifespan:** as long as its house lasts; data 200–500 years.
- **Food:** only what is left for it, a bowl at the hearth each night. It cannot take food from the stockpile itself.
- **Sleep and senses:** sleeps by day in a corner or behind the hearth. It knows everything that happens in its house.
- **What harms it:** an unkept house; being watched at work; being paid; a house abandoned.
- **How it ends:** it does not starve to death. A house spirit with no food or no house leaves.

**Needs.**
- **Food, through the `offering` mechanism:** one share of berries left in the bowl at the hearth each night by a member, as a new small job ("Filling the bowl at the hearth").
  - The bowl is its ration. The house spirit stands in the ration line like anyone else, so the ration policy decides whether its share is set aside (the winter story in §1).
  - An empty bowl counts as "went without", with its own text.
- **Distinct need `house_kept`:**
  - Met while the camp has a roofed shelter with a hearth (today's shelter with at least one sound roof section), and while it can do chores at night unwatched: hauling and roof repair, existing work, done by night.
  - Drained when someone awake watches it work (an awake member within a few tiles), and when the shelter is in ruins.
- **The twist from the folklore: payment ends it.** A Generous member who finds work done in the night may leave a gift by the hearth (the act `leave_thanks`). The house spirit takes it as wages and leaves at once. A kindness ends a relationship, and the log says so plainly.
- **Frustrated by** two empty bowls, or a week with no unwatched hour. It then turns boggart for a night: it sours food (a cause for spoilage), scatters stock, or spoils a sleeper's rest. Each prank cites what slighted it.
- **Touches:** Needs (rations), Work (night chores), Social (gifts, thanks, insults), Incident (spoilage), Build (a house), Schedule (who is awake at night).
- **Later:** the domovoi's moving rite, when the camp moves.

**Heart.**
- **Delights:** a well-kept house (`house_in_order` +4, music); a full bowl; a sleeping household.
- **Fears:** being seen; the house falling to ruin; fire.
- **Shames:** being paid as if it were a servant; being watched (`seen_at_work` −6, sweat).
- **Griefs:** its household dying or leaving (`house_emptied` −20, gloom).

**With others.** Attached first to the house, then to its people. It dislikes idlers and is fond of children (there are none yet). Leanings +1 +1 −2 +1.

**Story seeds.**
1. The empty bowl (§1): World → Needs → Incident → Needs → Social.
2. The thank-you:
```
Day 18, 06:00 — Sera found the roof mended in the night.
Day 18, 06:05 — Sera left a gift by the hearth for whoever had mended it.
Day 18, 06:05 — Pim took the gift as wages, and left the camp.
```
3. No unwatched hour:
```
Day 22, 20:00 — You set a night watch for every night.
Day 28, 03:00 — Pim has had no unwatched hour for a week.
Day 28, 03:30 — Pim, slighted, spoiled Oren's sleep.
Day 29, 09:00 — Oren insulted Pim.
Day 29, 09:00 — Pim now holds a grudge against Oren.
```

**Silhouette.** Very small (0.6 of a person) and hunched, with a great mass of beard or hair, a pointed hood in muted russet (not the bright red of modern Christmas pictures), and big bare feet. Seen mostly at night, by the hearth.

**Names.** A house spirit is known by its house ("the one of the river shelter") and rarely says its own name. Being named by the household is fixed decision 12's naming trigger for it.

**Threat form.** The boggart: pranks, not raids.

**Needs first.** The `offering` mechanism and the bowl job; the `leave_thanks` act; night chores with a check for watchers. It is better with Build (a house of its own choosing) and Schedule (who is awake), but it runs on today's shelter.

**Growth.**
- **Wants:** a house worth keeping; to be given a name.
- **Evolution, on a naming:** *hearth-warden* (the house it keeps holds out against rain and spoilage) or *boggart* (a decline after neglect).

### 6.7 Duwende and nuno sa punso — the Philippines; the duende of Spain, Portugal and Latin America (2C) ◆

**From.** Philippine folk belief: the duwende of house corners, old trees and mounds, and the nuno sa punso ("the old one of the mound"), who lives in an anthill or termite mound. People say "tabi-tabi po" ("by your leave") when passing places where these beings may live. The Spanish duende (from *dueño de casa*, "owner of the house") names related house goblins in Spain, Portugal and Latin America.

**Care.** "Tabi-tabi po" is a living custom. Return keeps its spirit, asking leave, in the world's own words. Duwende are not sorted into good and bad by colour, and there is no depiction of real folk healers.

**Body.**
- **Build:** tiny (knee-high), wizened or child-like. It lives inside a mound, an old tree or a corner.
- **Lifespan:** long; data 300–600 years.
- **Food:** a little of what is offered or foraged.
- **Sleep and senses:** sleeps by day inside its mound and feels every footstep on its ground.
- **What harms it:** its mound broken, built over or dug; rudeness.
- **How it ends:** it rarely dies. A destroyed home drives it away.

**Need: `leave_asked`.**
- **Drain:** slow, 10 an hour; courtesy keeps it up.
- **Met by:**
  - someone asking leave while passing within a tile of its mound (the act `ask_passage`, part of travel). People whose culture keeps the custom do it unprompted; others do it once a member who keeps it has taught them (culture-faith design).
  - an offering at the mound (the same `offering` mechanism the house spirit uses, at a different place).
- **Frustrated by:**
  - walking past or across without asking;
  - foraging, felling or building on or next to the mound. Members who keep the custom refuse to build on a mound (fixed decision 2's refusal).
- **The curse:** each offence can bring it. The offender sickens (a `cursed` thought, and a small loss of health and rest every hour) until amends are made: an offering at the mound and an asking of leave, by the offender or by someone on their behalf, who is then owed a debt.
- **Mounds** are map features placed from Truths: a few in the grass, never on paths the camp needs.
- **Touches:** Travel (passing), Build and Zones (protected ground), Work (foraging and felling near mounds), Social (offence, amends, debts, grudges), Needs (the curse on health and rest), Incident (small windfalls for a camp that keeps the courtesy).

**Heart.**
- **Delights:** being greeted (`greeted` +3, music); offerings; a respectful household.
- **Fears:** fire; digging; ploughs (later).
- **Shames:** none it will admit.
- **Griefs:** its mound destroyed (`home_destroyed` −25, gloom).

**With others.** Quick to take offence and quick to forgive after amends; generous to those who keep the courtesy. Leanings +1 +2 +1 +1.

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
3. The kicked mound:
```
Day 12, 08:10 — Sera kicked the edge of Burr's mound while foraging.
Day 12, 08:10 — Burr cursed Sera.
Day 14, 09:00 — Sera, sick with the curse, refused to work.
Day 16, 18:00 — Sera insulted Burr.
Day 16, 18:00 — Burr now holds a grudge against Sera.
```

**Silhouette.** Tiny (0.45 of a person), with a large head, a peaked cap or long white hair, and a walking stick. What reads at 32 px is the mound: a low earthen dome with a dark opening, and a lantern-sized glow at dusk while its owner is awake.

**Names.** A duwende is never addressed by its own name, only by an elder's title in the world's language. If it ever gives its true name, that is a sign of great trust (a fixed decision 12 naming trigger).

**Threat form.** The curse. There is no hostile body.

**Needs first.** Mound map features (Truths); the `ask_passage` check in travel; the curse; the `offering` mechanism (shared with house spirits). Best with Build and Zones (protected ground, refusals), which the companion designs supply.

**Growth.**
- **Wants:** a household that keeps the courtesy; its mound left whole.
- **Evolution:** *luck of the house* (moves into the camp's corner and brings small windfalls) or *the old one* (a decline: it curses harder after repeated offence).

### 6.8 Mmoatia — the Akan of Ghana and Côte d'Ivoire (2C) ◆

**From.** Akan folklore: the mmoatia, small forest beings whose feet point backward and who speak in whistles. In the Anansi tale, the spider catches "Mmoatia, the fairy whom men never see" with a sticky gum doll. They are said to take the palm wine and bananas left out for them, and to share knowledge of healing plants with some.

**Care.** No jungle exoticism, and no "pygmy" framing of a small people. No depiction of real healers or shrines. Readers check the herb lore before the design is final.

**Body.**
- **Build:** small (knee-high or less), round-headed and long-armed. Its feet are turned backward, so its tracks point the wrong way.
- **Lifespan:** long; data 100–300 years.
- **Food:** fruit (berries for now).
- **Sleep and senses:** rests in thick cover by day and forages at dusk and dawn. Very sharp-eared.
- **What harms it:** capture; sticky things (the gum doll); open, bare ground.
- **How it dies:** of wounds.

**Needs.**
- **`gifts_given`:**
  - Drains 12 an hour.
  - Met by an offering at the forest edge: one share of berries left by a member (the `offering` mechanism).
  - Gifts flow back. A mmoatia that is given gifts gives in return: forage at dusk, or herb lore to its go-between.
- **Whistle-speech:**
  - Only someone who understands whistle-speech can meet a mmoatia's need for company. Everyone else gets "could not understand each other" instead of a chat.
  - Understanding is learned through patient contact: deep talks and gifts with the same mmoatia over days. It is a skill flag on the person, `understands: [mmoatia id]`, earned by experience in the shape of fixed decision 12's mutations.
  - The one who learns it becomes the go-between. Everyone's dealings with the mmoatia pass through them: requests, debts, warnings.
- **Frustrated by:**
  - **No gift for ten days:** it takes what is not given, a small night theft from the stockpile. It slips out and back. Whoever's set-aside meal it took forms a grudge.
  - **Losing its go-between** to death or departure: its need for company can no longer be met, and it leaves within days.
- **Touches:** Social (whistle-speech, the go-between, debts), Needs (gifts come out of rations), Work (herb lore raises the go-between's `care` yield; in M1 this becomes a trait mutation citing the teaching event), Travel (backward tracks shake off a pursuer), Growth.

**Heart.**
- **Delights:** a gift (`gift_received` +5, hearts); being understood (`understood` +4, music); music.
- **Fears:** capture; sticky things; open ground.
- **Shames:** being tricked.
- **Griefs:** losing its go-between (`go_between_lost` −20, gloom).

**With others.** Shy with strangers, devoted to its go-between, mischievous toward the stingy. Leanings 0 +1 +2 −1.

**Story seeds.**
1. Nobody left who understood:
```
Day 10, 21:00 — Nia came to understand Lark's whistling.
Day 36, 17:00 — Nia left the camp.
Day 36, 17:00 — Nobody left in the camp could understand Lark.
Day 38, 06:00 — Lark, unheard, left the camp.
```
2. The herb lore:
```
Day 15, 19:00 — Mira left a share of berries at the forest edge for Lark.
Day 16, 06:00 — Lark showed Mira which leaves close a wound.
Day 27, 03:30 — Mira tended Oren's wounds with Lark's leaves.
Day 27, 03:30 — Oren owes Mira for tending his wounds.
```
3. The stingy camp:
```
Day 22, 02:00 — Nobody had left Lark a gift in ten days.
Day 22, 02:10 — Lark took the meal set aside for Hollis.
Day 22, 07:00 — Hollis now holds a grudge against Lark.
```

**Silhouette.** Very small (0.5 of a person), round-headed and long-armed. Its footprints in the mud point backward (GroundWear). It shows the music-note mark when it whistles to its go-between.

**Names.** A mmoatia's name is whistled and cannot be spoken. Humans use a spoken use-name that the go-between gives it (a fixed decision 12 naming trigger).

**Threat form.** A small night thief of the stores, when gifts lapse (raid-shaped).

**Needs first.**
- The `offering` mechanism, shared with the house spirits and the duwende.
- A whistle-speech filter in social checks, and the go-between flag.
- Herb lore, as a thought now and as a trait mutation in M1.
- Readers, before the design is final.

**Growth.**
- **Wants:** to be understood by more than one person; a gift given without being asked.
- **Evolution:** *forest-teacher* (teaches several people, and all the camp's carers gain) or *the unseen* (withdraws, and is seen only by its go-between).

## 7. How the player meets them

Peoples add no button to the bottom bar and no panel. They arrive through the incidents that already exist, show their needs on the map and in the inspector, and change the stories in the Chronicle. The player's levers stay intentions: work priorities and the ration policy today; standing orders, schedules, zones and plans later. Never "Morrow, eat that" or "Teal, go to the river" (fixed decision 2).

### 7.1 Someone new walks in

A wanderer of a new people arrives like any wanderer. Where they enter is data: `arrivesFrom` is `edge` by default; a kappa climbs out of the river onto the bank. They walk to the hearth and are asked to stay. If the camp cannot meet one of their needs right now, the inviter promises it (§8.8).

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

The inspector is unchanged in shape. Its needs come from the species, in the order batch 1's `StateApi` gives them: Food or Blood (none for a people that eats nothing from the stores), then the people's own needs with a one-word stage (the ghoul's reads "Hunger for the dead"), then Rest (none for a people that never tires), Company and Health. Hovering the species word shows where the people comes from on Earth and what it needs, in one plain line each. The player is Earth-born and would know.

```
┌──────────────────────────────────────┐
│ Teal                               × │
│ Kappa · Lives in the camp            │ ← hover "Kappa":
│ Now: Wetting their head-dish at the  │     From the folklore of Japan.
│      river                           │     Needs water in the dish on its head
│ Needs                                │     every day, and always returns a bow.
│  Food       ███████░░░  712          │
│  Head-dish  ██░░░░░░░░  212 drying   │
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

- **Everything shown is drawn from sim state** (ADR-0019).
  - The kappa's dish glints when full and goes dull when dry.
  - The sweat or gloom mark comes from the need's thoughts.
  - The ghoul crouches at a body at night, its eyes two pale points.
  - The remains shrink as portions go.
  - The vetala hangs from its tree at rest.
- **The People list** shows each person's job in plain words: "Teal — Wetting their head-dish at the river", "Morrow — Asking Mira's leave".
- **Hovering a tile** names what is there: "What was left of Bram", "An old grave, its name long gone (dug into)", and later "A mound (someone lives here)".

### 7.4 The player's levers

- **Today:**
  - **Work priorities.** A kappa set to keep watch first stands far from the river all night. That choice can be the first link in its story.
  - **The ration policy.** It decides whether a house spirit's bowl is filled.
- **Later, as standing orders** (a new command, `SetStandingOrder`, in slice 2C). An order appears in the Orders list only while it matters:

```
┌ Orders ─────────────────────────────────────────────────────┐
│ When food runs short                                         │
│  (•) The hungriest eat first   ( ) Promised meals come first │
│  ( ) You eat first                                           │
│ Pim's bowl at the hearth                    (a house spirit lives here)
│  (•) Every night   ( ) When there is plenty   ( ) Never      │
│ When someone dies                           (a ghoul lives here)
│  (•) Ask their friends first   ( ) Bury them at once         │
│ This only sets what the camp intends. People still choose.   │
└──────────────────────────────────────────────────────────────┘
```
- **"When someone dies"** ships with 2E, the slice of the dead (the build design's slice 8.4), as a layer on its grave plans and on culture-faith's rites (lead's call):
  - *Ask their friends first* holds a member's grave until the ghoul's asking at the first dusk after the death is over. A rite whose deadline comes sooner is never held back.
  - *Bury them at once* lets the grave go ahead, which closes the body to the ghoul (§8.11).
  - The first draft's third choice, "Leave them to Morrow", is dropped. It read as the player handing a body over, which the ghouls' code and fixed decision 2 both forbid.
- **Later, through the companion designs:**
  - **Schedule:** a "moon nights" row, only for werewolves: let them run, or keep them in.
  - **Zones:** "leave be" ground around mounds and old graves; a run ground; a room to keep someone in.
  - **Build:** a trough or pond for a kappa; a house with a hearth.

## 8. What changes in the simulation

Everything here builds on batch 1's needs model (ADR-0022, §5). 2A is the kappa (§8.10); 2E is the ghoul, with the dead (§8.11).

### 8.1 State

- **Needs of a person's nature** are batch 1's `Character.NatureNeeds`: one `NatureNeed` per need the species names, with `Id`, `Value` (0–1000), `Stage` (0 fine, 1 low, 2 critical), `LastEvent`, `CriticalSince` and the bonds (`BondId`, `BondWork`, `BondItem`). The kappa's dish is one of these. 2A adds no field to it.
- **The existing fields keep their meaning.** Food or blood stays in `Food`, rest in `Rest` and company in `Social`. Batch 1's `foodSource: "none"` and `tires: false` switch off the first two for the peoples that need it.
- **Enums.** Append only; snapshots store names. 2A adds `JobKind.TendNeed`: standing at a place that feeds a need until it is met (the kappa at the river). Batch 1's `TendTree` stays the dryad's.
- **2E adds** (§8.11):
  - `World.Remains`: `{ Id, CharacterId, GraveId, Portions, DugTick }`. A record is created the first time someone eats from a body or digs a grave; a dead person with no record is whole. Old graves get a record at world creation.
  - `World.PendingDiscoveries`: `{ WitnessId, EventId }`, holding what someone will find when they next wake. If 2C's house spirits land first, they add it.
  - `JobKind.AskLeave`, `JobKind.FeedOnDead` and `DebtKind.Leave`.
  - The grave itself, `Character.DiedEvent` and `RestingIn` are the build design's (its §4.2 and §4.8), built in the same slice. Old graves are its `grave` structure with `ForId` 0, placed last in world creation on open tiles of the ruins' yard from Truths, so no tree, bush or rock moves.

### 8.2 Needs are data

A need in `content/needs.json` (batch 1's schema) gives its name; its start, decay, low, critical and met marks; its low and critical thoughts and events; the health it takes while critical, with the death text; the hours critical before its holder leaves; the works or items it can be bound to; and integer `params` for its behaviour. A species lists its needs in `needs`. That is a **list**, so a hybrid can later take the union of its parents' needs.

The kappa uses the format as it is. What only the kappa does is a param of `head_water`, read by its behaviour, so 2A changes nothing in the needs schema (lead's call: the other choice was new typed fields, which would need a schema change):

| Param | Value | What it does |
|---|---|---|
| `waterPerHour` | 500 | restored each hour beside water |
| `rainPerHour` | 150 | restored each hour of rain while uncovered |
| `daylightPerHour` | 40 | drained on top of the decay each hour in open daylight |
| `tendMaxTicks` | 1800 | the longest a `TendNeed` stays at the river |
| `bowPermille` | 120 | the chance that someone chatting with a kappa bows |
| `spill` | 400 | what the dish loses when the kappa bows back |
| `weakFromStage` | 1 | from this stage (1, low) the kappa fights weakened |
| `weakHitPct` | 50 | the share of its hit chance it keeps while weakened |

### 8.3 Code grows by need or act, never by species

- **The rule.** A need's id names its behaviour (`sim/Systems/Natures/`), as an incident's id does (ADR-0018 #4); an act's id names its code. `RequiredContent.Needs` lists each need id with the params its code reads. `ContentDb` rejects a need with no behaviour or a missing param, naming the file and the id. No species id appears in `sim/` except `human`, the night thief (a test, §12.1). A people that shares a need with another (the "kin" of §4.1) is JSON alone. This replaces this doc's first rule, "by mechanism, never by people", which assumed generic satisfier kinds. Batch 1's code already keeps the rule in this form.
- **The shared model asks each need's behaviour** through dispatch points in `NatureNeeds`; batch 1 has `Bind`, `Restore`, `SourceOpen` and `Answer`. 2A adds four. Each answers "nothing" for every need but `head_water`, so batch 1's peoples are untouched:
  - `BreaksJob`: should this need break a running job now? For the kappa, yes at critical (§8.5).
  - `FightHitPct`: what share of their hit chance does the holder keep? For the kappa, `weakHitPct` from `weakFromStage` on.
  - `Collapses`: does the need fell its holder now? For the kappa, yes at zero.
  - `RescueSpot`: where should a rescuer carry the downed holder? For the kappa, the nearest bank tile.
- **A mechanism is pulled out when a second need uses it** (lead's call). The first need to do a thing writes it in its own behaviour. When a second need does the same, the code moves into one mechanism that both call. If the first user is code that already ships (batch 1's), the second writes the shared form, and the first moves onto it later, as a refactor. What is expected to be shared, and when:

| Mechanism | First user | Shared from | Grows out of |
|---|---|---|---|
| stand at a place until a need is met | dryad (`TendTree`, batch 1) | 2A: the kappa's `TendNeed` is written as the shared job; the vetala's rest uses it (2B); the dryad moves onto it as a refactor | `Forest.Tend`, `Places.Adjacent` |
| weather on the uncovered | kappa rain (2A) | the salamander (batch 5) | `IncidentSystem.RainUpkeep` |
| ask, and be answered from state | vampire blood (`BloodJobs`); batch 1's bargains wrote their own | 2B: the vetala's tale; the ghoul's leave (2E) uses it | `BloodJobs.RefusalKey`, `Bargains.RefusalKey` |
| a contest at night | dokkaebi play (2B) | 2B: the kappa's pledge | `CombatSystem`'s hit roll, without damage |
| a share left at a place (`offering`) | house spirit, duwende, mmoatia (2C) | 2C: built once, for all three | `Rations` set-aside meals; the `KeepPromise` job |
| a bond kept or broken | djinn contracts (batch 1) | batch 3's tokens | `World.Contracts`, `Debt` |
| eat from the dead | ghoul (2E) | the vetala's worn body (later) | `World.Remains` |

- **Refactors keep the logs identical** (lead's call). Pulling a mechanism out of a need that already ships is a refactor, so the soak's `world-N.txt` logs for seeds 1–100 must come out byte-identical. If they cannot, it is not a refactor. It waits, or it ships as a behaviour change in its own commit, with its own soak comparison and a line in the slice's ADR. Identical logs win over tidier code.

### 8.4 Harms

Today the only harm outside the needs is the sun (`daylightBurnPerHour`), and it stays as it is. No people in 2A to 2E adds a harm. The sun on a kappa's dish is a drain (`daylightPerHour`), and its fall at zero is `head_water`'s behaviour. Harms become a list on the species when batch 5 brings the next one: `petrify` for trolls and dwarves, `heat` near the hearth for the yuki-onna, `iron` once items exist. Each entry then gives its kind, health per hour, effect, cover, thought, event, death text and the promise it implies for a newcomer. Sleep hours come from the schedule design's `rhythm` field (`diurnal`, `nocturnal`, `sleepless`). This doc adds no sleep field.

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

This doc's first draft put promises and care before a low need. In batch 1's order, a kappa whose dish is low goes to the river before it keeps a promise or tends someone, unless it is already busy, because a low need never breaks a running job. The pledge of §6.1 (seed 2) comes in 2B with `keepsPledges`, a species flag that puts that species' pledges ahead of even a critical need.

**Breaking off a job.** `JobPicker.Choose` runs only when someone is idle. `WorkSystem.ShouldInterrupt` breaks a running job for three reasons: the sun, starving and exhaustion.
- 2A adds a fourth: a need whose behaviour says `BreaksJob`, which is the kappa's dish at critical. Batch 1's needs never say so, so their worlds do not move.
- It does not break a fight, a flight, a rescue or an arrival (these are already exempt), or the need's own job.
- A low need never breaks a job. It waits for the job to end.

So a kappa on watch with a drying dish stays at its post until the dish is critical, and a fight that starts before then runs to its end (§1).

### 8.6 Reactions: the body's share of feeling

- **Three layers of feeling.** Body: the species and its needs. Culture: customs, in the culture-faith design. Person: traits.
- **Culture landed first, so it owns the shared shape.** Culture-faith's `customs.json` gives each custom a kind implemented in code and params in content. When a people needs a reaction of the body to an event (a thought, a need change, or both), it is a custom scoped to a species, in that shape. This doc adds no reaction record of its own.
- **2A needs none.** The kappa's delight in rain comes from `head_water`'s own rain restore, which adds `kappa_rain`. In 2E, the thought a human has on seeing a ghoul eat comes from their culture's attitude toward ghouls (culture-faith §5.10), not from a species reaction.

### 8.7 Social acts

- **An act that only one need uses lives in that need** (lead's call): its code in the need's behaviour, its numbers in the need's params. 2A's bow belongs to `head_water` (`bowPermille`, `spill`); 2E's `ask_leave` belongs to `dead_flesh`.
- **`content/acts.json` arrives with the first act that two needs share:** the contest in 2B, used by the dokkaebi's play and the kappa's pledge. `ContentDb` then rejects an act with no behaviour, as it already rejects an incident with none (ADR-0018 #4).
- **Acts by slice:** `bow` (2A); `contest`, `prank` and `ask_tale` (2B); `leave_thanks`, `ask_passage` and the whistle-speech filter (2C); `ask_leave` (2E).
- **Where they hook in:**
  - At chat time in `SocialSystem.Step`: the bow, contest offers, the whistle filter.
  - As jobs from `JobPicker`: `ask_tale`, `ask_leave`.
  - In travel: `ask_passage`, when a path passes a mound.

### 8.8 Join promises

- **Unchanged in 2A.** `IncidentSystem.UnmetNeed`, as batch 1 left it: those who burn are promised a roof when no roof section is sound; those who eat from the stores are promised a meal when nothing is free for them; those who eat nothing are promised nothing.
- The kappa eats berries, so it is promised a meal like anyone when the stores are bare. Nobody promises it the river, which is always there.
- **Promises from data come in 2C,** when house spirits are promised a bowl with their own text: an optional promise on a need, checked after the sun and the food. That is a schema change for 2C's sign-off.

### 8.9 The vampire migration: withdrawn

This doc first moved food and blood onto generic satisfier kinds (species.json v2), with a byte-identical vampire migration as step one. That is withdrawn. No batch 2 people needs it, batch 1 built `foodSource: "none"` for those who eat nothing from the stores, and the migration makes no new story. Food and blood become data only when something needs them to (hybrids, or the items milestone), and that migration must then leave the soak logs byte-identical (§8.3).

### 8.10 The kappa (2A)

- **The need.** `head_water` restores `waterPerHour` while its holder stands on a passable tile next to water, and `rainPerHour` for each hour of rain while it is uncovered, which also adds `kappa_rain`. It drains `daylightPerHour` more in open daylight.
- **Going to the river.** Low or critical, its behaviour returns a `TendNeed` job: the nearest passable tile next to water, ties broken by (y, x) as `Places.CoveredSpot` does, so no random draw. The kappa stays until the need is met, `tendMaxTicks` pass, or something more urgent comes up. The job's text is `job.tend_need.head_water`.
- **The bow.** When a chat in `SocialSystem.Step` involves a kappa, one `CourtesyBow` draw (Social stream) at `bowPermille` decides whether the other person bows. The kappa bows back and the dish loses `spill`. The bow event becomes the need's `LastEvent`, so the drying that follows cites it.
- **Weakness.** `CombatSystem`'s hit roll multiplies the chance by `NatureNeeds.FightHitPct`. The downing event then cites the event that put the need at its weakening stage, because it changed the odds (ADR-0018 #1).
- **Collapse.** At zero, `NatureNeeds.Collapses` downs the kappa where it stands (the existing `event.needs.collapsed`, with a head-dish text). It stays down until its dish is back above critical.
- **Rescue.** The rescue job carries the downed to `NatureNeeds.RescueSpot` when there is one, and to a bed otherwise, as now. For the kappa that is the nearest bank tile, where the water fills its dish and it gets up.
- **Arrival.** The species field `arrivesFrom: "water"` puts a newcomer on a bank tile instead of at the map's edge. The tile is drawn as the edge tile is (`DiceId.ArrivalEdge`, Incident stream), so no new draw tag is needed. The kappa's `arrivalKey` reads "{a} climbed out of the river, hungry and alone."
- **Food balance.** The kappa is one more berry-eater from day 12. As batch 1 did for the fae (ADR-0022 #8), one more small bush cluster (two bushes, keyed after the others so none moves) keeps winter at half rations per head. It lands with Teal in the cast step, not before, so the content step stays byte-identical (§12.1).

### 8.11 The ghoul (2E), and the buried dead

The ghoul comes in the slice of the dead, with the build design's graves and the act of laying the dead to rest (its 8.4), culture-faith's death rites and the Zones burial ground (§12.1 says why). The rules below are designed now so that slice is built once. They answer the builder's question about buried bodies: nothing here is 2A's.

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
- **Draws.** None. Asking, answering and digging are all decided from state; only where the old graves lie is a Truth (§10).

### 8.12 Events, causes and endings

**2A: the kappa**

| Event key | System | Cites | Effect |
|---|---|---|---|
| `event.incident.kappa_arrives` (the kappa's `arrivalKey`) | Incident | nothing | — |
| `event.social.bowed` | Social | nothing (courtesy starts chains) | — |
| `event.needs.head_water_low`, `_critical` | Needs | the need's `LastEvent` (the bow, or the last time it was met) | — |
| `event.needs.collapsed` (existing, with a head-dish text) | Needs | the critical event | — |
| `event.combat.downed` (existing) | Combat | the fight, plus the event that weakened the kappa | — |
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

The needs schema stays batch 1's, unchanged: the kappa's own numbers are `params`. Teal's arrival window uses batch 1's `arrivalDays`. The first draft's other fields are dropped or deferred (lead's call), because nothing reads them yet and ADR-0018 already lists content fields with no effect as a seam:
- `size`, `lifespanYears` and species `ethics`: when Build, aging and leanings read them. Culture-faith already derives each person's ethics (its §4.6).
- `sleep`: the schedule design's `rhythm` field instead.
- `harms`, `gives`, `constraints`, `reactions`: harms in batch 5 (§8.4); blood stays on `canDonateBlood`; `keepsPledges` comes with the pledge in 2B; reactions are culture customs (§8.6).
- `pluralKey`, `fromKey`, `summaryKey`: loc keys by convention instead, checked at load.

**Content entries (2A), in batch 1's formats**
- **`content/needs.json`:** `head_water` (§8.2). `RequiredContent.Needs` gains it, with its params.
- **`content/species.json`:** `kappa`.
- **`content/cast.json`:** Teal (kappa), `"arrivalDays": [12, 20]`. With batch 1's four that makes fifteen; question 2 asks how the cast grows after that.
- **`content/thoughts.json`:** `dish_drying`, `dish_dry`, `dish_full`, `kappa_rain`, `bowed_to`.
- **`content/loc/en.peoples.json`** (batch 1's file for the peoples' text): `species.<id>.name`, `.plural`, `.from` and `.summary` for every species, the six that exist and the kappa (culture-faith asks for `.plural` too); `need.head_water` with `.low` and `.critical`; the events of §8.12 with their `_you_` variants; `job.tend_need.head_water`; the new thoughts.
- **`game/art/looks.json`** is a view asset, not content: a `species` block names the body parts each people's figure draws (shell, beak, dish). This is ADR-0019's pattern, so the sim never reads it.

**Checked at load** (`ContentDb.Validate`), each failure naming the file and the id:
- batch 1's checks: every need a species names exists and has behaviour, every param its code reads is there, and every thought and event key it names exists;
- every species has its `.name`, `.plural`, `.from` and `.summary` loc keys;
- `arrivesFrom` is a known value.

**Examples** (batch 1's formats):
```json
// needs.json: the kappa's need
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
// species.json: the kappa
{ "id": "kappa", "nameKey": "species.kappa.name",
  "foodSource": "berries", "foodDecayPerHour": 25,
  "restDecayPerHour": 30, "socialDecayPerHour": 15, "sleepRestorePerHour": 120,
  "daylightBurnPerHour": 0, "ticksPerTile": 6, "canDonateBlood": false,
  "needs": ["head_water"], "arrivalKey": "event.incident.kappa_arrives", "arrivesFrom": "water" }
```

**2E's content, for that slice's sign-off** (listed so the slice of the dead is designed once):
- the `dead_flesh` need, with params `perPortion` 600, `portions` 3, `friendOpinion` 30, `fromHour` 19, `toHour` 22, `refuseMoodBelow` 30 and `requiredOpinion` 0;
- the `ghoul` species: `foodSource: "none"`, `needs: ["dead_flesh"]`, the schedule's `"rhythm": "nocturnal"`, and Zones' `"atHomeInDark": true`;
- Morrow in cast.json, with an `arrivalDays` window;
- the thoughts `dead_hunger`, `dead_starving`, `fed_with_leave`, `fed_without_leave`, `leave_refused`, `dead_desecrated` and `grave_dug`;
- the "When someone dies" standing order, and `saw_dead_eaten` as a culture attitude.

## 10. Determinism, snapshots, the text-state API, localization and mod hooks

**Draw ids: batch 2 has its own range** (lead's call). Each parallel track now claims a range, so no branch renumbers at merge:
- growth: TruthId 20–29 and DiceId 100–119 (ADR-0021);
- batch 1: TruthId 30–39 and DiceId 120–139 (ADR-0022);
- culture and faith: TruthId 40–49 and DiceId 140–159 (ADR-0023, on its branch);
- **batch 2: TruthId 50–59 and DiceId 160–179.**

A `TruthId` value feeds the Truths hash, so once merged it never changes (ADR-0004). `DiceId` values are tags, and `JobKind` and `DebtKind` are stored by name. Batch 2 adds no `DiceSystem`.

**Truths** (stateless; appended, never renumbered or reused)
- **2A adds none.** This doc first proposed a Truth to vary each person's drain on their people's own needs (`NeedTolerance`). It is folded into question 9, which asks once whether every need should vary per person, food and rest included.
- **Later, in landing order, from 50:** `CastPresence` (2B, if question 2 is answered yes); `Mounds` (2C); `MoonPhase` (2D); `OldGraves` (2E); `EthicsLeaning`, per person and per axis, once species leanings change behaviour; `Lifespan`, once aging exists; `WandererSpecies`, the people of a generated wanderer, keyed by arrival slot.
- **Not drawn at all:**
  - A cast member's people is content (`cast.json`).
  - Answers to every ask (blood, leave, a tale, a contest) come from state. The same situation gives the same answer, and a refusal reads as a choice, not a roll.

**Dice** (per-system streams)
- **160 `CourtesyBow`** (Social stream, 2A).
- **Later, in landing order:** `ContestBout` and `RepayAgain` (2B); `PrankKind` and `CurseRoll` (2C); `MoonChange` (2D). Evolution path choices (fixed decision 12) use growth's stream and range (`DiceSystem.Growth`, DiceId 100–119), each with its purpose tag.
- **A people's draws happen only when that people is present.** A world whose cast has no kappa draws exactly what it drew before. A test enforces this (§12.1).

**Rewinds.** Truths keep each person's people and nature. Dice re-roll their experiences, so in another timeline the same kappa spills its dish on different nights. The Chronicle of Lost Futures can then say "In another timeline, Teal was carried to the river by Bram."

**Snapshots.**
- 2A adds no state. The dish is a `NatureNeed` (batch 1's), and `JobKind.TendNeed` is appended and stored by name. `World.SchemaVersion` is not bumped for it (ADR-0021 bumps it when saves arrive).
- 2A's snapshot tests restore in the middle of `TendNeed`, and with a kappa downed at zero, and compare with a straight run.
- 2E adds `World.Remains`, `World.PendingDiscoveries` (unless 2C did) and its job and debt kinds. Its snapshot tests restore in the middle of `AskLeave` and `FeedOnDead`, with a grave dug and with a discovery pending.
- Restore stays far under a second at 200 characters.

**Text-state API.**
- `CharacterState.Needs` is built from the species, as batch 1 does: food or blood, the needs of their nature, rest, company, health. 2A adds each nature need's stage word (`need.<id>.low`, `.critical`), so the inspector can say "drying".
- A new `SpeciesState` (id, name, plural, from, summary, needs, who is in camp) comes through `StateApi.Species(w, id)`. The inspector's species tooltip reads it.
- `LocationState.Contents` lists remains and old graves (2E).
- The headless runner's cast lines already show each species. The soak summary gains "stories involving each people".

**Localization.** No English is composed in code (ADR-0007). New key families:
- species: `species.<id>.name`, `.plural`, `.from`, `.summary`;
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
- Each batch 2 entry lists wants and evolution paths for `wants.json` and `evolution_paths.json`, ready for when M1 builds them. ADR-0021's wants already carry species weights (walking in daylight is a vampire's), so a people's own wants are entries in that file.
- Peoples supply experiences for `trait_mutations.json`: a dish spilled in front of others, a won contest, whistle-speech learned, a night as the wolf, and in 2E a refused leave. Each mutation cites its event, so nothing grows from grind.
- M1 asks for naming-triggered evolution for two species. Recommended: house spirits, where naming is already in the folklore, and dryads (batch 1).
- A fox's tails, a ghoul's second name, a dokkaebi's new use-name: these are where peoples visibly change, as fixed decision 12 asks.

**Culture and faith (`2026-11-09-culture-faith-design.md`, being built on its branch).** This design relies on four things from it:
1. **Customs that fire on events and give thoughts.** A species reaction, when one is needed, is a custom scoped to a species (§8.6).
2. **Attitudes toward peoples** (its §5.10): revere, wary, fear, scorn. Its §6.10 adds stances on batch 1's peoples as content, and batch 2's peoples get theirs the same way as each lands. How someone takes seeing a ghoul eat is one of them (§8.11).
3. **Death rites per culture** (its §5.4, its slice 2): grave, pyre or river. They decide the ghoul's world. A culture that burns its dead is a famine for a ghoul; one that buries makes the asking a race with the grave (§8.11). The ghoul comes in the same slice.
4. **Holy days and festivals on the calendar.** They share one moon with the werewolf (2D), and carry the oni's bean-throwing day (batch 5) and the feasts where the centaur's wine and the nøkk's music come in.

It also shares these with the culture-faith design:
- **Offerings.** One `offering` mechanism serves the house spirits, the duwende, the mmoatia and culture's own offerings to spirits.
- **Courtesy.** A culture that greets with a bow makes kappa spills common (a greeting custom that raises the bow's chance among its members). Asking leave of a mound is a custom people learn from one another.
- **Guest-right.** The join promise (§8.8) is the hook for cultures that owe guests more.
- **Oaths.** Djinn contracts and kappa pledges are oaths, for the cultures that weigh oaths.
- **Taboos.** What a body cannot eat (species) and what a culture will not eat (custom) both work through customs.
- **Regard.** How a faith sees each people (welcoming dryads, abhorring ghouls) belongs to the culture-faith design, not this one.

Every person of every people belongs to one of the world's invented cultures (rule 5); culture-faith's seeding gives the peoples their cultures through its `"*"` weights. A kappa raised in a human culture is a story too.

**Build, Zones and Schedule (companion designs).** What peoples ask of each:
- **Build:**
  - graves and the act of laying the dead to rest, for the ghoul (build §4.8 and slice 8.4, with 2E). Where this doc once said "the build design's slice 1c" for graves, it means that slice;
  - a trough or pond, for the kappa;
  - a house with a hearth, for house spirits (build slice 2's hearths);
  - stone rooms (troll), wide doors (centaur), and fire and fuel (salamander) in batch 5;
  - refusing to build on a mound (duwende), through build's existing refusal path.
- **Zones:**
  - "leave be" ground around mounds, old graves and a dryad's grove (zones slice 2);
  - a run ground, and a room to keep a werewolf in;
  - hanging trees spared for a vetala;
  - `atHomeInDark`, which the zones design asks this doc to set (lead's call): undead, djinn, fae, the ghoul, the dokkaebi and house spirits are at home in the dark; dryads and the kappa are not, so a home area can keep them from their trees or the river at night, and the werewolf is not, because the moon run is the point.
- **Schedule:**
  - each species' kind of day, through the schedule's `rhythm` field: the ghoul, the dokkaebi and house spirits are `nocturnal`, as vampires are; the kappa stays `diurnal`;
  - a moon-nights row, shown only for werewolves.

  A house spirit's unwatched hours are a consequence of the watch schedule, not a setting.

## 12. Build order and acceptance

### 12.1 First slice: 2A, the kappa (now)

**Why the kappa alone** (lead's call; the builder asked what the ghoul would eat in a one-year world).
- **The numbers.** In the last soak on main (`out/soak`, 100 worlds × 1 year) a member died in 2 worlds, both vampires burned by the sun. Nobody died of wounds or hunger, and a thief was killed in 5 worlds. Batch 1 adds a few deaths, still rare and mostly in winter: its undead fades to rest in 3 worlds and its dryad withers in 2 (ADR-0022). The two old graves (four portions of 600, at 8 an hour) last about twelve days. A ghoul arriving on days 12–20 would run out on days 25–33, and starve or leave near the turn of winter in about 95 of 100 worlds.
- **What that would mean.** Asking leave, the point of the people, would almost never happen. The camp could not meet the need at all, which fails this doc's own test (§1). The ending would land in winter, against goal 5. And the ghoul's acceptance would rest on two to seven worlds.
- **The other ways out are worse.**
  - More old graves or a slower drain would feed it alone: a human in costume.
  - More deaths would change every M0 regression world and its stories, which is not this doc's call.
  - Reporting the starvation honestly would ship a people whose story is always the same.
- **So the ghoul waits for the dead, as 2E.** The build, Zones and culture-faith designs hold back their work on the dead for the same numbers, and their timing is answered once, as culture-faith's question 5. If that answer is "rites now", the ghoul moves up with them.
- **No other people takes its place in 2A.** Most of batch 2 is ◆ and would wait for a reader. The vetala is worth more in 2B, where it rests on the kappa's place job and its tale pulls the ask-and-answer mechanism out of blood and bargains (§8.3). A one-people slice is also the quickest way to prove that batch 1's model takes a new need.

**Starts from:** batch 1 merged, with ADR-0022 accepted.

Steps, each a small commit with its tests:
1. **The kappa in content and code, not yet in the cast:** `head_water` and its behaviour (water and rain, the daylight drain, `TendNeed`); the four dispatch points (§8.3); the bow, weakness, collapse and rescue to water; `arrivesFrom`; thoughts and loc; `SpeciesState` and the stage words. With no kappa in the cast, the soak logs are byte-identical with batch 1's.
2. **The kappa in the view:** its parts (shell, beak, and the dish with its glint, drawn from the need's value) and the inspector's species tooltip. View only.
3. **The cast:** Teal joins with `arrivalDays` [12, 20], and one small bush cluster keyed after the others keeps winter's half rations (§8.10). Every world changes here, so this step sets the new soak baseline.
4. **Soak and record:** the soak, the stranger check, STORIES.md, and an ADR recording the slice.

The kappa is not ◆. It still gets a reader from its tradition before it ships (rule 7).

**Acceptance: tests**
- **`AbsentPeopleChangeNothing`:** with the kappa defined in content but not in the cast, the soak logs for seeds 1–100 are byte-identical with batch 1's build.
- **Content checks:** each of these fails at load, naming the file and the id: a species naming a missing need, a need with no behaviour, a missing `head_water` param, a missing species loc key, an unknown `arrivesFrom`.
- **`SpeciesIdsNotInCode`:** no species id appears as a literal in `sim/` except `human` (the night thief).
- **Causality:** bow → spill → drying → struck down in the fight → carried to the river → debt, each event citing the one before.
- **The interrupt:** a kappa on watch with a low dish stays at its post; at critical it leaves for the river, unless it is fighting. No batch 1 need breaks a job.
- **Snapshots:** in the middle of `TendNeed`, and with a kappa downed at zero, a restored world has the same future as a straight run.
- **Determinism and invariants:** the same seed and commands give the same world with the kappa present; need values stay within 0–1000; nobody stands on water.
- **Benchmark:** CPU per character per tick stays within ADR-0005's budget. It is measured at the largest camp a world can hold, plus thieves: fifteen people now, and twenty once question 2's presence draw exists (lead's call).

**Acceptance: soak** (100 worlds × 1 year). These are targets for tuning. If one cannot be reached, report the honest number, as ADR-0017 did.
- Stories per world-year are no fewer than in the last soak before the slice (12.13 with batch 1, ADR-0022).
- A kappa need event lies on a story's chain in at least 40% of worlds.
- The share of stories that end before winter (day 31) rises by at least 5 points over the last soak before the slice (lead's call: the first draft asked 10 points of two peoples).
- No world crashes.

**Acceptance: stranger check.** Four fresh readers each get one raw log from a seed with Teal present. At least two retell a kappa story unprompted. This is the M0 exit criterion, applied to the new people.

**Acceptance: screenshots**
- **The pawn sheet at 32 and 48 px:** human, vampire, the four batch 1 peoples, and the kappa with its dish full and with it dry. This is also the sheet that formally accepts ADR-0009.
- **The inspector** open on the kappa, showing the head-dish bar and the species tooltip.

### 12.2 The rest of batch 2

**How each slice is measured** (lead's call). A slice's acceptance percentages are over the worlds where its people is present, and are judged only on a soak with at least 30 such worlds (the build design's rule for its 8.4); the soak grows past 100 worlds when it must. Until question 2's presence draw exists, every world has every people.

| Slice | Peoples | Builds | Waits for | Acceptance beyond the usual tests |
|---|---|---|---|---|
| 2B | dokkaebi, vetala | first, the presence draw, if question 2 is answered yes; `acts.json` with `contest`; `prank` and `ask_tale`, pulling the ask-and-answer mechanism out of blood and bargains (§8.3); the repayment loop; thoughts renewed by tales; rest at a tree, on the kappa's `TendNeed`; the kappa's pledge and `keepsPledges` | nothing | a repayment-to-theft chain in at least 20% of worlds with a dokkaebi; a reopened-wound grudge in at least 25% of worlds with a vetala; fewer stories ending on a debt nobody returns to (ADR-0018) |
| 2C | house spirits, duwende, mmoatia | the `offering` mechanism and the bowl job; join promises from data (the bowl); `SetStandingOrder` with the bowl order; mounds and `ask_passage`; the curse; whistle-speech and the go-between | best after build slice 1 and zones; readers for the duwende and mmoatia | an empty-bowl chain in at least 30% of worlds with a house spirit that reach winter; a go-between departure chain in at least 20% of worlds with a mmoatia |
| 2D | werewolf | the moon in `world.json`; the `Changed` flag for one night; Schedule's moon row | the schedule design | a moon-night chain (ran, kept in, or changed) ending in a persistent effect in at least 40% of worlds with a werewolf |
| 2E | ghoul | `dead_flesh` and its behaviour; `ask_leave` and the `Leave` debt; the night meal, grave digging and pending discoveries; the old graves; the "When someone dies" order; Morrow in the cast (§8.11) | the slice of the dead: aging and multi-year worlds, the build design's 8.4, culture-faith's death rites (timing: culture-faith's question 5); a reader (◆) | of the worlds where a member dies while the ghoul lives in camp (at least 30 of them), at least half produce an ask-leave chain and at least a quarter end in a grudge or debt that names the ghoul; tests for the chain ask → refuse → starving → night meal → discovery → grudge, and for its grave form; a night-scene screenshot with the ghoul at a body, the gore stylised (ADR-0019) |

### 12.3 Later batches, and what each needs first

| Batch | Peoples | Needs first |
|---|---|---|
| 3: tokens, water, music | selkie, merrow, swan maidens, tikbalang, nøkk, iara, shurale | the items milestone (named personal items that can be stored, hidden and stolen); a combat job beyond `Raid` (the ford, the drowning song); culture's festivals for music. Shurale can come with 2B. |
| 4: secrets and the hidden | fox spirits, huldufólk and hulder, orang bunian, ciguapa | a knowledge layer: who knows which facts about whom. The vetala's gossip and the rival otherworlders need it too. |
| 5: bodies, elements, seasons | troll, dwarf, centaur, oni, salamander, yuki-onna | Build's rooms, doors, stone and fuel; seasons that matter beyond winter; culture's holy days (oni); crafting (dwarf) |
| 6: later | bean sí, curupira, golem | omens (culture-faith); animals and hunting; constructs; readers |
| Kin | nang tani; draugr, gwisin, bhoot; merrow and swan maidens; iara; shurale | their parent's need and code, then data and looks only |
| Readers first | menehune, kalanoro | partners from the tradition |

### 12.4 Architected for, not built

- **Hybrids:** the union of the parents' needs, averaged leanings, combined looks, merged customs.
- **Conversion** (bitten, cursed, raised), only through fixed decision 12's evolution machinery (question 8).
- **Threats** who can be talked down, tended and recruited.
- **Generated wanderers** of any people (`WandererSpecies`), biased toward unfilled court roles (fixed decision 12's niche). Question 2's presence draw is the step before it.
- **Earth folklore as ideas** (fixed decision 6): the player teaches "bow to the river kappa", and the world reacts.
- **A mod hook** to register new need behaviours and acts.

## 13. Questions for the founder

Most important first, each with a recommended answer and why it matters. The builder builds the recommended answer to each.

1. **Approve 2A's schema and determinism changes, on batch 1's needs model.** One optional species field (`arrivesFrom`); the kappa, its `head_water` need and Teal as entries in batch 1's formats; `JobKind.TendNeed`; `DiceId.CourtesyBow`. No new Truth.
   *Recommended: approve.* This doc's first plan (generic satisfier kinds, species.json v2 and a vampire migration first) is withdrawn.
   *Why it matters:* CLAUDE.md asks before schema and determinism changes, and every later people builds on whichever needs schema you accept, ADR-0022's included.
2. **Which peoples live in each world?** Batch 1 brings the cast to fourteen and 2A to fifteen. If every world holds every people, you have twenty companions besides yourself by 2D and twenty-one by 2E, past fixed decision 2's 5–20, and the roster has forty.
   *Recommended:* the M0 ten are in every world. Each later people's cast member is present in a world by a Truth (`TruthId.CastPresence`, an optional cast.json field and a world.json number, four per world to start), so no world holds more than twenty. Built first in 2B, with one new soak baseline. Nobody in the cast is replaced. Like any Truth, who is in a world survives a rewind.
   *Why it matters:* it is a cast schema change and a new Truth that changes every world's arrivals, and it decides how every later slice is measured.
3. **Where exactly is the line for beings that a living religion also speaks of?**
   *Recommended:* rule 1 as written. Folk forms are allowed: djinn as in the Nights, oni as in village tales. Anything prayed to, venerated, or held to be a prophet, saint or angel is not, and neither are scripture, prayer or sacred names as mechanics.
   *Why it matters:* your request asked for creatures from every culture and religion, and this is how the roster honours it without using what believers hold sacred. The djinn's reader also checks batch 1's framing against it (§5).
4. **Is the ghoul's need acceptable for a companion, and where is the line on the buried dead?** It eats the dead. It never kills, eats only with leave from the dead's friends, and carries shame when it breaks that.
   *Recommended:* yes, with the gore stylised (ADR-0019), and a grave is never opened with leave, so only the night offence reaches a buried friend (§8.11). The ghoul comes with the slice of the dead (2E): if you answer culture-faith's question 5 "rites now", it moves up with it.
   *Why it matters:* it is the roster's clearest monster-as-person and its most sensitive entry, and the burial line decides whether a friend's grave can ever be opened with consent.
5. **Will you fund readers from each tradition?**
   *Recommended: yes.* Every people gets a paid reader from its tradition before it ships, and each ◆ people before its design is final. The gaps for Indigenous North America, Aboriginal Australia and most of Polynesia stay open until partners from those traditions want to make something with us.
   *Why it matters:* the ghoul, the djinn and most of batch 2 are ◆, so their slices wait on it.
6. **How do Earth origins show?** Names take their structure from the people and their sounds from the world's invented cultures, never from a real language's list of names. The species tooltip says plainly "From the folklore of …", with no exotic adjectives.
   *Recommended: yes to both.*
   *Why it matters:* no people stands in for a real ethnicity (the cost: a kappa's name will not sound Japanese), while the Earth-born player's knowledge still becomes part of play for the price of one line.
7. **How do the token peoples work?** Batch 3's tokens come from captivity stories: the selkie's skin, the swan maiden's cloak, the tikbalang's hair, the fox's pearl.
   *Recommended:* a token is only ever given, as a pledge to stay. Taking one is theft: it forms a grudge under the old consent rules and never gives control. The player never benefits from holding one, and giving it back leaves the person free to stay or go.
   *Why it matters:* it decides whether batch 3 can be built without making captivity a mechanic.
8. **Can someone become another people** (bitten, cursed, raised)?
   *Recommended:* not before fixed decision 12's M1. After that, only through its evolution machinery, as a rare, logged beat with a cause, and never something the player can aim at someone.
   *Why it matters:* conversion the player could aim would be control of a person's nature, against fixed decision 2.
9. **Should every need vary from person to person?** ADR-0018 lists the seam: everyone goes hungry on the same clock. This doc first proposed a Truth for the peoples' own needs only (`NeedTolerance`).
   *Recommended:* yes, as one change for every need, food and rest included, after 2A, accepting that every existing world changes.
   *Why it matters:* it is a new Truth that touches every world, so it should be asked and built once.
