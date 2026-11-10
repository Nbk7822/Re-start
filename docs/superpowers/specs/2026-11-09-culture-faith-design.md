# Culture and faith: design

- **Date:** 2026-11-09
- **Status:** draft for founder sign-off. The builder builds slice 1 on a branch from this spec now, and merges to main only after the founder answers question 1 in section 10 (schema and determinism approval, which CLAUDE.md requires).
- **Founder's ask:** "add into the game variables like religion, culture, customs, traditions and things that microinfluence events."
- **Reads with:** CLAUDE.md (fixed decisions 1, 2, 4, 5, 6, 10, 12), BIBLE.md sections 1, 4 and 5, DECISIONS.md ADR-0004, 0006, 0007, 0013, 0014, 0016, 0017 and 0018. The peoples-roster, Build, Zones and Schedule specs are being written in parallel in this folder.

---

## 0. In one page

Every world seeds, once, from authored templates in content JSON: four **cultures**, three **faiths**, and the **gods** of those faiths, each with a name, a domain and a temperament. Every person's culture, faith and devotion are Truths keyed by cast key, so they survive rewinds. The cultures and faiths are invented, never Earth religions. The peoples come from Earth's myths, and a culture can hold several peoples.

Culture does nothing on a screen of its own. It changes what people do at moments the player already watches: when they meet and talk, when food runs short, when a stranger arrives hungry, when a vampire asks for blood, when a promise is kept or broken, when a tree is felled, and when a season turns. Each of these is a **custom** with a hook into existing code. Each hook emits a Culture event that sits on the causal chain between its cause and its consequence, so the story analyzer counts it.

Faith adds **gods who are believed in**. A god cares about certain deeds (a kept promise, a refused blood-gift, a felled sacred tree) and "speaks" through certain incidents (rain, a windfall, spoiled food). When a sign follows a deed, a faithful follower may read it as the god's anger or thanks, and from then on think worse or better of the person who did it. In slice 1 the gods act only through belief.

The player is **from another world**, with no culture and no faith. They stumble over customs (faux pas) until they learn them, and every culture judges them by its own rules, including their ration policy.

**Slice 1** builds four cultures, three faiths with seeded gods, seven kinds of custom (12 custom entries), attitudes toward peoples, oath weight from the Oathbound/Free axis, and omens. Death rites, holy days and fasting come in slice 2. The reason is in the data: member deaths happened in 2 of 100 one-year worlds in the last soak, and holy days belong with the Schedule spec. Section 6.8 sets a soak bar: culture stories in at least 60% of worlds, but no more than 35% of all stories.

---

## 1. Goals, non-goals, anti-goals

**Goals**
1. Culture and faith change behaviour at moments that already produce stories (talk, food, blood, promises, arrivals, timber, seasons). The result is chains a player retells: "Wren wouldn't give Ilex blood because the Reed Folk forbid it, and when it rained Ilex took it as a sign."
2. Every hook touches at least two systems and can end in a persistent change: a grudge formed or ended, a debt formed or repaid, a departure (fixed decision 1).
3. Cultures, faiths, gods, membership and devotion are Truths seeded once per playthrough (fixed decision 4). Readings and reactions are Dice and re-roll after a rewind. The player can learn a world's customs and gods across deaths.
4. Everything is data-driven: templates, customs, god domains and all text live in `/content`, and modders add cultures, faiths and customs by id.
5. The design is ready for what comes next: the four peoples arriving on another branch, the Ideas Tree (fixed decision 6), factions and the kingdom (fixed decision 5's axes on factions), rewinds, and growth arcs (fixed decision 12).

**Non-goals for slice 1**
- Gods that act on the world (section 10, question 3).
- Death rites, holy days, fasting, food taboos, shrines, conversion and drift (section 9).
- Any new player command. Culture reacts to the levers the player already has: the ration policy and work priorities.
- Faction or kingdom religion, state faiths and holy war.

**Anti-goals (never)**
- No Earth religions, deities, saints, symbols or parodies of them. Faiths are invented and seeded from templates.
- No culture or religion screen, no piety bars, no stat sheet, no "faith points" (fixed decision 2). Devotion reads as a word, never a number.
- No culture as a stat bonus ("+10% work speed"). Peoples differ in needs; cultures differ in what people expect of each other.
- No direct orders. The player never says "perform the rite now" or "pray". People decide and can refuse.
- No cosmetic customs. A custom without a hook that changes an outcome is not shipped.
- No invented culture modelled on a real ethnic group.

---

## 2. What exists today and what this builds on

Grounded in the code at `c818511` and the last 100-world soak (`out/soak/summary.json`).

| Existing mechanic | Code | What culture adds |
|---|---|---|
| Chats, deep talks and insults between neighbours every 30 ticks; chat warmth `100 + 50 × chemistry` | `SocialSystem.Step` | Warmth between kin, wariness of strangers, stances toward peoples, first-meeting faux pas |
| Blood needs consent; refusals are remembered | `BloodJobs.RefusalKey`, `Refuse`, `Give` | Blood taboo and blood-gift customs; stances shift consent. At least 649 refusals per 100 worlds today: a refusal carries no persistent effect, so the logs show only those inside stories |
| A newcomer who can't be fed is promised a meal or a roof | `IncidentSystem.Join`, `UnmetNeed` | Guest-right: a host gives up their own share; a wronged guest's grudge is heavier |
| Who eats when food is short is the player's policy | `Rations.Assign` | "A leader eats last": the player's policy becomes a cultural statement |
| Grudges, debts and promises are first-class | `Relations.FormGrudge`, `FormDebt`, `Repay`, `Break` | Oath weight per culture, word spreading among kin, debts of honour that get repaid |
| Felling trees for timber, nearest first | `Places.StandingTree`, `CampJobs.GatherTimber` | A sacred tree that the faithful won't fell |
| Rain, windfall, spoilage, accidents, deaths | `IncidentSystem`, `Lifecycle.Die` | Signs read through faith (omens) |
| Seasons turn; winter squeeze | `NeedsSystem.SeasonTurns` | Seasonal festivals that bring mid-year beats and can set down grudges |

**Known seams this addresses** (ADR-0018):
- "Blood debts are seldom repaid": `owes_blood` is 467 of 847 story endings. The debt-of-honour custom gives these arcs a repayment.
- "Most stories land in the last ten days": festivals and omens add beats across the year.
- "The director has no mid-year pressure besides thieves and spoilage": same fix.

**Data that sets scope:**
- Member deaths: 98 of 100 one-year worlds have none. Death rites would almost never fire in a soak, so they move to slice 2.
- Meal thefts: 118 per 100 worlds.
- Promises made on joining: 80 per 100 worlds; 45 of them broken, in 38 worlds.
- Grudges: 223 per 100 worlds (ADR-0016 counted 227).

These are counted from the "What happened" section of each log, with folded repeats expanded, so no event is counted twice.

---

## 3. The ideas in plain words

- **Culture**: the people someone was raised among, which decides how they greet, host, eat, mourn, keep promises and celebrate. Customs from a culture are habits, so they apply to every member.
- **Faith**: what someone believes. Customs from a faith apply only to followers whose devotion is at least 30 ("faithful" or "devout").
- **Devotion**: how seriously someone takes their faith. It is a Truth from 0 to 100, and the player only ever sees a word: *lapsed* (under 30), *faithful* (30 to 69), *devout* (70 and over).
- **Custom**: one rule with one hook, such as "will not give blood" or "gives a guest their own meal". Each custom has a **kind** implemented in code and **params** in content.
- **God**: a named being in a faith's pantheon. A god has a **domain** (what it cares about) and a **temperament**, which comes from two numbers: wrath (how readily misfortune is read as anger) and grace (how readily fortune is read as thanks).
- **Deed**: a thing someone did that gods care about, recorded when it happens (a promise broken, blood given, a sacred tree felled).
- **Sign**: an incident a god is said to speak through (rain, a windfall, spoiled food, an accident, a death).
- **Omen**: a follower (devotion at least the faith's `omenDevotion`, 40 or 50 in slice 1) reading a sign as a god's verdict on a recent deed.
- **Attitude**: how a culture feels about a people (species): revere, wary, fear or scorn.
- **Ethics axes** (fixed decision 5): Mortal/Immortal, Arcane/Mundane, Wild/Civil and Oathbound/Free, each from −2 to +2, carried by cultures and faiths and derived for each person (section 4.6). Player text never uses the word "Oathbound" for the axis, because that is already a trait name (section 4.6).
- **The outsider**: the otherworlder. They have no culture, no faith and devotion 0. They learn customs by breaching them.

---

## 4. Data model

### 4.1 Content files (all new; each needs a schema in `/content/schemas`)

All four files are top-level arrays merged by id across mod folders (`ContentDb.ReadMerged`). Ids use the usual pattern `^[a-z][a-z0-9_]*$`. Unknown fields are rejected (`UnmappedMemberHandling.Disallow` and `additionalProperties: false`).

**`cultures.json`: culture templates**

| Field | Type | Meaning |
|---|---|---|
| `id` | id | Template id. It also keys this culture's Truths, so renaming it changes every world. |
| `nameKeys` | loc key[] (≥ 1) | Names to choose from per world, written as mid-sentence noun phrases with the article ("the Reed Folk"). |
| `peoples` | `{species, weight}`[] (≥ 1) | How likely a person of that species is to belong. `species` is a species id or `"*"` (any species not listed). `weight` is 0 to 100. |
| `ethics` | `{mortalImmortal, arcaneMundane, wildCivil, oathboundFree}` | Integers from −2 to +2. Positive means the first-named pole (Mortal, Arcane, Wild, Oathbound) and negative means the second. |
| `faiths` | `{faith, weight}`[] (≥ 1) | Candidates for this culture's main faith in a world. |
| `otherFaithPermille` | 0 to 1000 | Chance that a member follows another faith instead. |
| `devotion` | `{min, max}` (0 to 100) | Range for members' devotion. |
| `customs` | `{custom, always?, weight?}`[] | `always: true` customs are always held. The rest form a weighted pool. |
| `customPicks` | 0 to 4 | How many distinct customs to draw from the pool per world. |
| `attitudes` | `{species, stance}`[] | `stance` is one of `revere`, `wary`, `fear`, `scorn`. |

**`faiths.json`: faith templates**

| Field | Type | Meaning |
|---|---|---|
| `id`, `nameKeys`, `ethics`, `customs`, `customPicks` | as in cultures | |
| `gods` | god slot[] (1 to 3) | One god is seeded per slot. |
| `gods[].nameKeys` | loc key[] (≥ 1) | Names for this slot. |
| `gods[].domains` | `{domain, weight}`[] (≥ 1) | Candidate domains. |
| `gods[].wrath`, `gods[].grace` | `{min, max}` (0 to 100) | Ranges for the god's temperament numbers. |
| `omenDevotion` | 0 to 100 | Minimum devotion to read omens for this faith. |

**`customs.json`: customs**

| Field | Type | Meaning |
|---|---|---|
| `id` | id | Also the stem of the custom's loc keys (section 4.2). |
| `kind` | enum: `greeting`, `blood_stance`, `guest_right`, `leader_last`, `debt_of_honour`, `festival`, `sacred_tree` | The behaviour, implemented in code. A custom of an unknown kind fails at load, the same way incidents do (ADR-0018). |
| `params` | object of integers (camelCase names) | Per kind. `RequiredContent.CustomKinds` lists the required params and `ContentDb` checks them at load. |

| Kind | Required params | Event moments (section 4.2) |
|---|---|---|
| `greeting` | `breachPermille` | `breach` |
| `blood_stance` | `consentBias` (−100 to 100), `offers` (0 or 1) | `refused` if `consentBias < 0`; `offered` if `offers = 1` |
| `guest_right` | `offerPermille`, `wrongedGrudgePct` | `shared`, `wronged` |
| `leader_last` | none | `offended`, `honoured` |
| `debt_of_honour` | `minDebtAgeHours` | `repaid` |
| `festival` | `season` (0-based), `hours`, `feastPerHead`, `forgive` | `begins`, `forgave`, plus `begins_lean` if `feastPerHead > 0` |
| `sacred_tree` | `nearestTrees` | `desecrated` |

**`domains.json`: god domains**

| Field | Type | Meaning |
|---|---|---|
| `id` | id | |
| `epithetKeys` | loc key[] (≥ 1) | "Keeper of Oaths" and so on. One is picked per god. |
| `caresKey` | loc key | Plain words: "hates a broken promise and loves a kept one". |
| `signs` | `{sign, reads}`[] | `sign` is one of `rain`, `windfall`, `food_spoils`, `work_accident`, `death`. `reads` is `anger` or `blessing`. |
| `pleasedBy`, `angeredBy` | deed id[] | Deeds from the closed set in section 4.7. |

**Validation at load** (each failure names the file and the id, like `BadTraitIdNamesFileAndId`):
- References resolve: species (or `"*"`), faiths, customs, domains.
- Every `nameKeys` and `epithetKeys` entry exists in loc. These are arrays, and `ContentTests.EveryLocKeyExists` only reflects over `string` properties, so `ContentDb.Validate` must check them explicitly.
- `min ≤ max` for every range.
- `customPicks` is no more than the number of non-`always` pool entries.
- A culture or faith holds at most one custom of each kind. Two `always` entries of the same kind are an error, and draws skip kinds already held.
- `greeting` customs appear only in cultures, because greetings are cultural.
- Every custom's convention loc keys exist.
- Every species named in `attitudes` has a `species.<id>.plural` loc key.

**Other content changes (no schema change):**
- `thoughts.json` gains 14 entries (section 6.4).
- A new `content/loc/en.culture.json` holds all new strings. The loader already reads `en.*.json` files in ordinal order.
- `en.log.json` and `en.ui.json` gain a few keys.
- `cast.json`, `species.json` and `world.json` are **not touched**, so this branch does not conflict with the four-peoples branch.

### 4.2 Loc key conventions

| What | Key |
|---|---|
| A custom's short name, used in lists and the story log | `custom.<id>.name` ("guest-bread") |
| A custom as a person's rule, used in the inspector | `custom.<id>.rule` ("Gives a guest their own meal if the camp has none to spare.") |
| A custom's events | `event.culture.<id>.<moment>`, plus `_you_a` / `_you_b` variants wherever the player's slot takes a possessive ("their" or "'s") |
| Generic culture events | `event.culture.omen_anger`, `event.culture.omen_blessing`, `event.culture.word_spread` |
| Sign and deed phrases | `sign.<sign>` ("the rain"), `deed.<deed>` ("a broken promise") |
| Words | `devotion.lapsed`, `devotion.faithful`, `devotion.devout`; `temperament.stern`, `temperament.kindly`, `temperament.fickle`, `temperament.distant`; `stance.<stance>` ("Fears {people}.") and `stance.<stance>.many` ("fear {people}") |
| Axis lines (shown when the person's axis value from section 4.6 is ≥ 1 or ≤ −1) | `ethic.oath_heavy` ("Holds a promise heavy.", `oathboundFree` ≥ 1), `ethic.oath_light` ("Takes a promise lightly.", ≤ −1), `ethic.signs_many` ("Sees signs everywhere.", `arcaneMundane` ≥ 1), `ethic.signs_few` ("Has little time for signs.", ≤ −1) |
| Species plurals | `species.<id>.plural` ("vampires") |

**Grammar rules for writers:**
1. A culture or faith name never opens a sentence. Names are stored with the article in lower case ("the Hearthfolk") and every template is written around that ("Among the Hearthfolk: …", "Followers of the Sworn Word honour …").
2. Third person uses "their".
3. A character placeholder that doesn't open the sentence already resolves to "you" through the `_mid` name variant (`EventText.Describe`). Only possessives need `_you_` variants.
4. Event args: the culture or faith holding the custom is passed as `{ways}` (`EventArg.Key("ways", nameKey)`).

### 4.3 Slice 1 content

**Cultures** (all four are seeded in every world):

| id | Names (one per world) | Peoples | Ethics (M/I, A/M, W/C, O/F) | Main faith | Devotion | Customs (always + 2 picks) | Attitudes |
|---|---|---|---|---|---|---|---|
| `hearth` | the Hearthfolk; the Longtable Kin; the Folk of the Kept Fire | human 5, `*` 1 | +1, −1, −2, +2 | sworn 3, roots 1; other 100‰ | 20 to 80 | **clasp_greeting**; guest_bread 3, leader_last 3, harvest_fire 2, debt_of_honour 1 | vampire: wary |
| `reed` | the Reed Folk; the River-Born; the Folk of the Low Water | human 4, `*` 1 | +1, 0, +2, −2 | roots 3, sworn 1; other 150‰ | 10 to 70 | **no_touch_greeting**; blood_taboo 3, longest_night 2, guest_bread 1 | vampire: fear |
| `ashen` | the Ashen; the Grey Folk; the Keepers of the Ash | human 3, vampire 3, `*` 2 | −1, +1, 0, +1 | ember 3, sworn 1; other 100‰ | 30 to 95 | **bow_greeting**; debt_of_honour 3, blood_gift 2, longest_night 2, leader_last 1 | vampire: revere |
| `night` | the Nightborn; the Folk of the Long Dark; the Lantern-Shy | vampire 5, `*` 3, human 1 | −2, +2, +1, 0 | ember 3, roots 1; other 150‰ | 20 to 90 | **veiled_greeting**; debt_of_honour 2, longest_night 2, guest_bread 1, blood_gift 1 | none |

With today's cast (8 humans, 2 vampires), the human weights 5 : 4 : 3 : 1 put about 31% of humans among the Reed Folk (usually blood-taboo) and about 23% among the Ashen (often blood-gift). Vampires mostly land among the Nightborn and the Ashen. Most worlds therefore get a cross-cultural tension around blood without every human refusing (see the vampire guard in section 6.8).

**Faiths:**

| id | Names | Ethics | God slots | Customs | omenDevotion |
|---|---|---|---|---|---|
| `sworn` | the Sworn Word; the Faith of the Witness; the Kept Word | 0, −1, −1, +2 | A: names Ostrel, Hesk, Vaunor; domains oaths 3, hearth 1; wrath 40–90; grace 20–60. B: names Tamra, Ennet, Corran; domains hearth 3, oaths 1; wrath 20–60; grace 40–90. | pick 1 of guest_bread 2, debt_of_honour 2 | 50 |
| `roots` | the Old Roots; the Green Way; the Faith of the Deep Root | +1, +1, +2, 0 | A: names Mossa, Ilvenne, Gorrow; domains grove 3, hearth 1; wrath 30–80; grace 30–80 | **sacred_grove**; pick 1 of blood_taboo 1, harvest_fire 1 | 40 |
| `ember` | the Ember; the Undying Flame; the Long Watch | −2, +1, 0, +1 | A: names Saccar, Wenlow, Iyre; domains vein 3, oaths 1; wrath 40–90; grace 30–70 | **blood_gift**; pick 1 of debt_of_honour 1, longest_night 1 | 50 |

God names are invented. Before shipping, the builder checks each name against lists of real deities and saints and replaces any match.

**Customs:**

| id | kind | params | Rule (inspector) |
|---|---|---|---|
| `clasp_greeting` | greeting | breachPermille 120 | Clasps forearms in greeting, and is slighted by anyone who will not. |
| `no_touch_greeting` | greeting | breachPermille 120 | Does not touch someone they have just met. |
| `bow_greeting` | greeting | breachPermille 120 | Bows before speaking to someone new. |
| `veiled_greeting` | greeting | breachPermille 120 | Never meets a stranger's eyes. |
| `blood_taboo` | blood_stance | consentBias −40, offers 0 | Will not give blood. |
| `blood_gift` | blood_stance | consentBias 30, offers 1 | Gives blood to the undying gladly. |
| `guest_bread` | guest_right | offerPermille 700, wrongedGrudgePct 150 | Gives a guest their own meal if the camp has none to spare. |
| `leader_last` | leader_last | none | Expects a leader to eat last. |
| `debt_of_honour` | debt_of_honour | minDebtAgeHours 24 | Repays what they owe with food when their creditor is hungry. |
| `harvest_fire` | festival | season 2, hours 3, feastPerHead 1, forgive 30 | Lights the harvest fire in autumn, and sets down a grudge there. |
| `longest_night` | festival | season 3, hours 4, feastPerHead 0, forgive 40 | Keeps the Longest Night in winter, and sets down a grudge there. |
| `sacred_grove` | sacred_tree | nearestTrees 6 | Will not fell the old tree sacred to their faith. |

**Domains:**

| id | Epithets | Cares (plain words) | Signs | Pleased by | Angered by |
|---|---|---|---|---|---|
| `oaths` | Keeper of Oaths; the Witness | hates a broken promise and loves a kept one | rain: anger; windfall: blessing | oath_kept, debt_repaid | oath_broken |
| `hearth` | Mother of the Hearth; the Host | hates a stolen meal or a wronged guest, and loves food shared | food_spoils: anger; windfall: blessing | meal_shared, guest_honoured | meal_taken, guest_wronged |
| `vein` | the Undying; Lord of the Vein | hates blood refused and loves blood given | rain: anger; work_accident: anger; death: anger; windfall: blessing | blood_given | blood_refused |
| `grove` | the Green Mother; the Old Root | hates a felled tree and loves food shared | food_spoils: anger; rain: blessing | meal_shared, guest_honoured | tree_felled, sacred_harmed, meal_taken |

**Example of each file's shape** (the builder writes all entries from the tables above). The `//` comments are labels for this document only. Content files carry no comments, because `ContentTests` parses them as strict JSON for schema validation.

```json
// cultures.json (one entry)
{
  "id": "reed",
  "nameKeys": ["culture.reed.name.0", "culture.reed.name.1", "culture.reed.name.2"],
  "peoples": [ { "species": "human", "weight": 4 }, { "species": "*", "weight": 1 } ],
  "ethics": { "mortalImmortal": 1, "arcaneMundane": 0, "wildCivil": 2, "oathboundFree": -2 },
  "faiths": [ { "faith": "roots", "weight": 3 }, { "faith": "sworn", "weight": 1 } ],
  "otherFaithPermille": 150,
  "devotion": { "min": 10, "max": 70 },
  "customs": [
    { "custom": "no_touch_greeting", "always": true },
    { "custom": "blood_taboo", "weight": 3 },
    { "custom": "longest_night", "weight": 2 },
    { "custom": "guest_bread", "weight": 1 }
  ],
  "customPicks": 2,
  "attitudes": [ { "species": "vampire", "stance": "fear" } ]
}
```

```json
// faiths.json (one entry)
{
  "id": "ember",
  "nameKeys": ["faith.ember.name.0", "faith.ember.name.1", "faith.ember.name.2"],
  "ethics": { "mortalImmortal": -2, "arcaneMundane": 1, "wildCivil": 0, "oathboundFree": 1 },
  "gods": [
    {
      "nameKeys": ["god.ember.a.0", "god.ember.a.1", "god.ember.a.2"],
      "domains": [ { "domain": "vein", "weight": 3 }, { "domain": "oaths", "weight": 1 } ],
      "wrath": { "min": 40, "max": 90 },
      "grace": { "min": 30, "max": 70 }
    }
  ],
  "customs": [
    { "custom": "blood_gift", "always": true },
    { "custom": "debt_of_honour", "weight": 1 },
    { "custom": "longest_night", "weight": 1 }
  ],
  "customPicks": 1,
  "omenDevotion": 50
}
```

```json
// customs.json (one entry)
{ "id": "guest_bread", "kind": "guest_right", "params": { "offerPermille": 700, "wrongedGrudgePct": 150 } }

// domains.json (one entry)
{
  "id": "oaths",
  "epithetKeys": ["god.epithet.oaths.0", "god.epithet.oaths.1"],
  "caresKey": "god.cares.oaths",
  "signs": [ { "sign": "rain", "reads": "anger" }, { "sign": "windfall", "reads": "blessing" } ],
  "pleasedBy": ["oath_kept", "debt_repaid"],
  "angeredBy": ["oath_broken"]
}
```

### 4.4 World and character state

All of it is plain serialised state, so snapshots carry it with no extra code. Lists are kept in stable order (template order, id order or event order), and there are no dictionaries in state.

**`Character`: new fields** (append them; the growth branch adds its own fields beside these):

```csharp
public string CultureId { get; set; } = "";   // culture template id; "" = none (the otherworlder)
public string FaithId { get; set; } = "";     // faith template id; "" = none
public int Devotion { get; set; }             // 0..100, a Truth at creation
public List<string> LearnedCustoms { get; set; } = new(); // customs of other cultures learned by breaching them; ordinal order
public List<int> Met { get; set; } = new();   // ids of people this person has spoken with; ascending
```

`Met` is needed because opinion entries can't stand in for "have met". A chat whose warmth is below 100 changes opinion by `1 × warmth / 100 = 0` in integer arithmetic, and `Relations.ChangeOpinion` returns early on a zero delta, so many pairs never get an opinion entry.

**`World`: one new property** (and bump `SchemaVersion` to 2):

```csharp
public CultureBook Culture { get; set; } = new();
```

**`sim/State/Culture.cs` (new):**

```csharp
public sealed class CultureBook
{
    public List<SeededCulture> Cultures { get; set; } = new();      // template order
    public List<SeededFaith> Faiths { get; set; } = new();          // template order
    public List<FestivalDate> FestivalDates { get; set; } = new();  // seeding order
    public List<SacredPlace> SacredPlaces { get; set; } = new();    // seeding order
    public List<Deed> Deeds { get; set; } = new();                  // event order; pruned after DeedMemoryHours
    public List<OmenRead> OmenReads { get; set; } = new();          // pruned with deeds
    public List<GuestOffer> GuestOffers { get; set; } = new();      // pruned when the promise closes
    public List<FestivalRun> Festivals { get; set; } = new();       // festivals happening now
}
public sealed class SeededCulture { public string TemplateId { get; set; } = ""; public string NameKey { get; set; } = ""; public string FaithId { get; set; } = ""; public List<string> CustomIds { get; set; } = new(); }
public sealed class SeededFaith   { public string TemplateId { get; set; } = ""; public string NameKey { get; set; } = ""; public List<string> CustomIds { get; set; } = new(); public List<SeededGod> Gods { get; set; } = new(); }
public sealed class SeededGod     { public int Slot { get; set; } public string NameKey { get; set; } = ""; public string DomainId { get; set; } = ""; public string EpithetKey { get; set; } = ""; public int Wrath { get; set; } public int Grace { get; set; } }
public sealed class FestivalDate  { public string CustomId { get; set; } = ""; public string HolderId { get; set; } = ""; public bool HolderIsFaith { get; set; } public int Season { get; set; } public int Day { get; set; } }   // starts at dusk
public sealed class SacredPlace   { public int TreeId { get; set; } public string HolderId { get; set; } = ""; public bool HolderIsFaith { get; set; } public string CustomId { get; set; } = ""; public bool Desecrated { get; set; } public long DesecratedEvent { get; set; } }
public enum DeedKind { OathKept, OathBroken, DebtRepaid, MealTaken, MealShared, GuestHonoured, GuestWronged, BloodGiven, BloodRefused, TreeFelled, SacredHarmed }
public sealed class Deed          { public DeedKind Kind { get; set; } public long EventId { get; set; } public int DoerId { get; set; } public long Tick { get; set; } }
public sealed class OmenRead      { public int ReaderId { get; set; } public long DeedEvent { get; set; } public long Tick { get; set; } }
public sealed class GuestOffer    { public int HostId { get; set; } public int DebtId { get; set; } public bool Accepted { get; set; } }
public sealed class FestivalRun   { public string CustomId { get; set; } = ""; public string HolderId { get; set; } = ""; public bool HolderIsFaith { get; set; } public long StartEvent { get; set; } public long EndsTick { get; set; } public bool Feasted { get; set; } public List<int> Attendees { get; set; } = new(); }
```

Content refers to deeds in snake_case (`oath_broken`). The code maps them with `Names.Snake(kind.ToString())`, and `RequiredContent.Deeds` lists them all.

### 4.5 Seeding a world (Truths only)

`CultureGen.Seed(World w)` (new, `sim/Systems/Culture/CultureGen.cs`) runs at the **end** of `WorldGen.Create`, after the map, items and arrivals exist. It draws only from `w.Rng.Truths`, calls `w.NewId()` never (so no later id shifts), and **emits no events**. `GameViewTests` asserts an empty Chronicle at start.

Weighted picks use one Truth draw over the total weight. "Distinct picks" draw index `k = 0, 1, …` over the remaining entries.

1. **Faiths** (template order). For each faith, `key = Fnv1a.Hash64("faith:" + id)`.
   - Name: `TruthId.FaithName`.
   - Customs: `always` entries, plus `customPicks` distinct weighted picks with `TruthId.FaithCustoms` at indexes k = 0, 1, …, skipping kinds already held.
   - Gods: for each slot s, `gkey = Mix64.Combine(key, s)`. Domain by `TruthId.GodDomain`. Name by `TruthId.GodName`, index 0. Epithet from the domain's list by `TruthId.GodName`, index 1. Wrath and grace by `TruthId.GodTemper`, indexes 0 and 1, inclusive of max.
2. **Cultures** (template order). For each culture, `key = Fnv1a.Hash64("culture:" + id)`.
   - Name: `TruthId.CultureName`.
   - Main faith: weighted by `TruthId.CultureFaith`.
   - Customs: as for faiths, with `TruthId.CultureCustoms`.
3. **Festival dates.** For every held custom of kind `festival` (on a culture or a faith), `day = Truth(TruthId.FestivalDate, Mix64.Combine(Fnv1a(holderId), Fnv1a(customId)), 0)` in `[1, DaysPerSeason − 1)`. The festival starts at `DuskHour`.
4. **Sacred places.** For every held `sacred_tree` custom: take the `nearestTrees` standing trees nearest the hearth (Chebyshev distance, ties by tree id) and pick one with `TruthId.SacredTree`. If there are no trees, skip.
5. **People** (id order). This covers every character with a non-empty `CastKey` and `Named` set.
   - **The otherworlder** gets `CultureId = ""`, `FaithId = ""` and `Devotion = 0`.
   - **Everyone else** uses `pk = Fnv1a.Hash64(c.CastKey)`, the same key as `WorldGen.NatureOf` and `Relations.NatureKey`.
     - **Culture weights.** For each culture, the weight is the `peoples` entry whose `species == c.SpeciesId`, else the `"*"` entry, else 0. If every weight is 0, all weights become 1, so any species always gets a culture. The pick uses `TruthId.PersonCulture`.
     - **Faith.** If `Truth(PersonFaith, pk, 0)` in `[0, 1000)` is below `otherFaithPermille` and there is more than one faith, the person takes another faith, drawn uniformly by `Truth(PersonFaith, pk, 1)` among the faiths other than the culture's main one. Otherwise they take the main faith.
     - **Devotion.** `Truth(PersonDevotion, pk, 0)` in `[min, max]` of the culture's range.
   - **Read `c.SpeciesId`, never `CastByKey(c.CastKey)`.** `WorldGen.AddSyntheticCharacters` makes keys like `mira_11` that aren't in `cast.json`, and the 200-character restore benchmark would throw. `AddSyntheticCharacters` copies `CultureId`, `FaithId` and `Devotion` from the template character it clones.
6. **Later-created people** (recruits, rivals, children, later) call `Ways.Assign(w, c)`. It uses the same rule, keyed by `CastKey`, or by `Id` when there is no cast key, like `Relations.NatureKey`.

This is why the **four peoples work after the merge with no code change**: their new cast members have a species and a cast key, so step 5 assigns them a culture through the `"*"` weights. Section 6.10 lists the content follow-up that gives them proper homes.

### 4.6 Ethics axes on people (and factions later)

Fixed decision 5 puts the four axes on individuals and factions. In slice 1, a person's value on each axis is:

```
axis(person) = clamp( culture.axis + (faith.axis × Devotion) / 100 , −2, +2 )   // integer division truncates toward zero
```

The otherworlder is 0 on every axis.

- **Slice 1 reads two axes.**
  - `oathboundFree`: oath weight `= 100 + 25 × axis` percent, from 50% to 150% (section 6.3, O1). Word spreads when the *culture's* value is at least 1.
  - `arcaneMundane`: omen odds are multiplied by `(100 + 25 × axis)%`.
- **The other two axes** (`mortalImmortal`, `wildCivil`) are carried in the data because fixed decision 5 puts them on factions and the Ideas Tree reads them (section 4.8). They have no effect in slice 1. The builder records this in the ADR as a known no-effect field, so it doesn't join ADR-0018's list by surprise.
- **Slice 2:** traits get an optional `ethics` nudge (for example the Oathbound trait +1 on `oathboundFree`, Independent −1). That is a traits schema change, asked for then.
- **The kingdom layer:** a faction's axes are the culture of its seat, plus its state faith weighted by the court's devotion, plus a pull toward its ruler. These are computed, not stored, so they follow the court.
- **Naming:** the axis is always `oathboundFree` in code and content, and player text says "Holds a promise heavy" or "Takes a promise lightly". The word "Oathbound" on screen stays the trait (`promisePriority`), so the inspector never shows the same word with two meanings.

### 4.7 Gods: how they are seeded and what they care about

- **Seeding (Truths):** name, domain, epithet, wrath and grace per god slot (section 4.5, step 1). The same seed gives the same gods in every timeline, so a player who dies learns that Ostrel, Keeper of Oaths, is stern and hates broken promises.
- **Temperament as a word:**
  - wrath ≥ 50 and grace < 50: **stern**
  - wrath < 50 and grace ≥ 50: **kindly**
  - both ≥ 50: **fickle**
  - both < 50: **distant**
- **What pleases and angers a god** comes from its domain (section 4.3). Deeds are a closed set in code, recorded where they happen:

| Deed | Recorded in | Doer |
|---|---|---|
| `oath_kept` | `Relations.Repay` (promise) | debtor |
| `oath_broken` | `Relations.Break` | debtor |
| `debt_repaid` | `Relations.Repay` (not a promise) | debtor |
| `meal_taken` | `NeedsJobs.TakeReservedMeal` | the one who took it |
| `meal_shared` | `CareJobs.KeepPromise` (both modes) | the one who carried the food |
| `guest_honoured` | guest-bread delivery | host |
| `guest_wronged` | `Relations.Break` for a guest-right newcomer | debtor |
| `blood_given` | `BloodJobs.Transfer` | donor |
| `blood_refused` | `BloodJobs.Refuse`, only for a plain or custom refusal, not hunger, weakness or mood | donor |
| `tree_felled` | `CampJobs.GatherTimber` | feller |
| `sacred_harmed` | desecration (section 6.3, P2) | feller |

- **Signs** are called from `IncidentSystem.StartRain`, `Windfall`, `Spoil`, `Accident` and `Lifecycle.Die` (members only). Section 6.3, M1, has the reading rule.
- **Gods are believed in, not proven.** In slice 1 a god changes nothing except through the people who read signs (section 10, question 3).

### 4.8 Architecting for the Ideas Tree (fixed decision 6: "the world reacts per species and faith")

Slice 1 builds the pipes the Ideas Tree will use, without any idea content:

1. **Ideas carry axis leanings.** A future `ideas.json` entry has `ethics` leanings. Medicine might be `mortalImmortal +1` (it serves mortal life); a printing press might be `arcaneMundane −2`.
2. **Default reactions come from axes.** A person's stance toward an idea is the sign of the dot product of their four axes (section 4.6) and the idea's leanings: embrace, neutral or shun.
3. **Explicit stances override.** Faith and culture templates gain an optional `ideaStances: [{ idea, stance: "embrace" | "shun" | "forbid" }]` (a schema change, asked for then). Species reactions use the same field on species, for the four peoples.
4. **Teaching an idea is a deed.** When a champion introduces an idea, the code records a parameterized deed `idea_taught:<idea>` with the champion as doer. Domains may list `idea:<idea>` in `pleasedBy` or `angeredBy` (the deed enum becomes a pattern then). The omen pipeline is reused unchanged: the next rain may become "Ostrel's anger at you for the printing press".
5. **Reactions are events on the path.** "Mira refused to learn letters; the Old Roots forbid it" is a Culture event citing the teaching, exactly like the blood taboo refusal in slice 1.

---

## 5. Catalogue of micro-influences

Each hook is listed with its trigger, its effect, the systems it touches and the story it can create. "Culture" means the new `SystemTag.Culture` event. **1** = slice 1, **2** = slice 2, **L** = later.

### 5.1 Greetings and conversation

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| G1 Kin warmth | Two members of the same culture chat | Chat warmth +25 points | Social, Mood | Kin drift together; cross-culture friendships are slower, so they mean more | 1 |
| G2 Kin greeting | First meeting of two people of the same culture | Both get "Met one of my own people" (+3 mood, +6 opinion). No event. | Social, Mood | "Wren and Nia, both River-Born, were friends from the first evening" | 1 |
| G3 Wariness of strangers | A chat across cultures while either is a newcomer (first 7 days, `SocialSystem.NewcomerDays`) | Warmth −25 points | Social, Mood, Incident (arrivals) | Late arrivals stay outsiders longer, so a broken promise in their first week costs more | 1 |
| G4 Faux pas | First meeting (`Met`); the listener's culture holds a greeting custom the speaker neither shares nor has learned | Dice `FauxPas` at `breachPermille`, ×3 for a speaker with no culture. On a hit: a breach event (Culture), the listener's "slighted" thought (−6 opinion), and the speaker learns the custom | Social, Mood, Culture | "You took Wren by the hand at your first meeting; the Reed Folk do not touch a stranger." The otherworlder stumbles through week one | 1 |
| G5 Name of the dead | Someone speaks to a mourner of a culture with that custom, within its mourning season | A faux pas against the mourner (greater offence) | Lifecycle, Social, Mood | "Hollis said Mira's name at the fire; Wren walked away" | 2 |
| G6 Learning by living | 5 days in camp beside members of a culture | Its greeting is learned without a breach | Social | | 2 |

### 5.2 Hospitality and guest-right

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| H1 Guest-bread | A newcomer holds an open meal promise (`IncidentSystem.Join` → `PromiseMeal`); a guest-right keeper (not the debtor) holds a ration share | One Dice `GuestOffer` roll per host and promise. On a hit the host carries their **own** share to the guest. This emits a Culture event, keeps the promise, makes the guest owe the host a Meal debt, and makes the host's next hunger cite the gift | Incident, Rations, Work, Travel, Needs, Social | Bram goes hungry for a stranger, and later the stranger repays (H1 with O3) or doesn't | 1 |
| H2 Wronged guest | A guest-right newcomer's promise breaks (`Relations.Break`) | Culture event; grudge ×1.5; "My guest-right was broken" (−40) replaces "A promise to me was broken" | Social, Needs, Rations | With the newcomer slack (ADR-0016), they leave within the hour, now with a sharper grudge | 1 |
| H3 Bread and salt | A guest who ate a host's bread later insults or steals from that host | A double opinion hit, and word spreads among the host's kin | Social | | 2 |

### 5.3 Food customs

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| F1 A leader eats last (offended) | `Rations.Assign` leaves a keeper out while the leader (the player) holds a share under "you eat first" | Culture event citing the went-without event and the policy event; "Our leader ate while I went without" (−15); grudge at −40, then departure | Rations (player policy), Needs, Social | The player's day-1 policy becomes a political statement | 1 |
| F2 A leader who went without (honoured) | The leader is left out while keepers hold shares | Culture event; "Our leader went without so we could eat" (+10) | Rations, Social | Under "the hungriest eat first", the otherworlder's hunger earns loyalty | 1 |
| F3 Feast days | A festival with `feastPerHead > 0` starts and `Rations.Unreserved(berries)` is at least the number of keepers present | Surplus berries go to the hungriest members; "Celebrated a festival" | Stock, Needs, Mood | The harvest fire turns surplus into goodwill before winter; without surplus it is a lean fire | 1 (inside S1) |
| F4 Fasting | A faith's holy day with a fast | The devout are left out of `Rations.Assign` from dawn to dusk. A starving faster eats anyway (`NeedsSystem.Eat`), which is a "broke the fast" Culture event and lowers co-religionists' opinion | Rations, Needs, Mood, Social | "Oren kept the fast while the stores were empty, and collapsed by dusk" | 2 (with holy days) |
| F5 Food taboos | A culture won't eat item X | Rations skip X for them; the Desperate break the taboo | Rations, Needs, Social | Needs foods other than berries (ADR-0018 item 5) | L |
| F6 Other table orders | "Guests first", "elders first" | The same shape as F1 against other policies | Rations, Social | | L |

### 5.4 Death rites and mourning

**Deferred to slice 2.** In the last soak, 98 of 100 one-year worlds had no member death, so rites would almost never fire. Fixed decision 12's 20-year soaks and aging will make deaths common; rites then move to the front of slice 2. The design is complete now so slice 2 can start without a new spec:

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| D1 Rite due | `Lifecycle.Die` for a member whose culture or faith holds a `death_rite` custom (params `deadlineHours`, `timberCost`, `workTicks`, `rite`: pyre, earth or river) | Adds `RiteDue { DeadId, CustomId, DueTick, DeathEvent }` to `CultureBook` | Lifecycle, Culture | | 2 |
| D2 Rite as work | A rite is due | New prioritized work `perform_rite` ("Lay the dead to rest") in `work.json` and in `JobPicker.WorkOrder` after `care`, plus `JobKind.PerformRite`. Kin of the dead's culture score as one priority higher. A pyre fetches `timberCost` timber, like `RepairRoof`. The job walks to the body, works `workTicks`, then emits a Culture event; the body becomes ash or a grave (view), and mourners get "The rite was done" | Work, Stock (timber against the roof), Travel, Mood | Timber for Mira's pyre is timber not spent on the roof promised to Vale | 2 |
| D3 Neglected rite | `DueTick` passes | Culture event citing the death; kin of the dead get "The dead were not honoured" about the leader (−15), and a grudge at −40 | Social, Mood | "Nobody burned Mira's body before the next dusk. Wren blames you." | 2 |
| D4 Mourner's debt | Rite performed | The dead's closest kin (highest opinion of the dead) owes the performer: `DebtKind.Rite` | Social | "Wren owes Bram for burning Mira's body" | 2 |
| D5 Graves | Earth rite done | The grave is a sacred place (P1 rules); building over it (Build) is desecration | Map, Build, Social | | L |

### 5.5 Holy days and rest

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| R1 Holy day | A seeded day in the faith's calendar (custom kind `holy_day`) | The devout (devotion ≥ 70) skip prioritized work from dawn to dusk and pray at the hearth or a holy place. The Schedule shows it as rest for them | Work, Schedule, Mood | Fewer hands on the holy day; a holy night with no watch invites a thief | 2 (with the Schedule spec) |
| R2 Refusal | A priority-1 job on a holy day | The devout won't do it; the inspector says "keeping the holy day" | Work | | 2 |

### 5.6 Oaths, promises and debts

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| O1 Oath weight | A promise kept or broken (`Relations.Repay` / `Break`) | The opinion effect of "A promise to me was kept / broken" and the broken-promise grudge are scaled by the creditor's oath weight (50% to 150%) | Social, Mood | The Reed Folk shrug off a broken promise; the Hearthfolk don't | 1 |
| O2 Word spreads | A promise to a member of a culture with `oathboundFree ≥ 1` breaks | Culture event citing the break; the creditor's kin get "Heard that someone broke their word" (−8); a grudge at −40 | Social, Mood | One broken promise costs you a whole people's trust | 1 |
| O3 Debt of honour | A keeper owes a non-promise debt (care, blood, meal, rescue) at least `minDebtAgeHours` old, and the creditor is hungry | A `RepayDebt` job forages a berry from a bush, or fetches one free (or own-share) berry, and hands it over. Culture event, then `debt_repaid` with DebtRepaid | Work, Travel, Needs, Social | "Ilex picked berries in the dark and brought them to Bram, to settle the blood debt." Closes owes-blood arcs | 1 |
| O4 Sworn vows | Lasting oaths (to protect, to avenge) as first-class objects (fixed decision 10) | Vows join grudges and debts in the graph | Social, Combat | | L |

### 5.7 Omens (incidents read through faith)

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| M1 Anger | A sign that reads "anger" for a god, within 48 h of a deed in its `angeredBy` list, done by a member; a follower of that god's faith with devotion ≥ its `omenDevotion` | Dice `OmenRead`. On a hit: Culture event citing **the deed and the sign**; the reader's "Saw a sign of the gods' anger at someone" (−12 opinion of the doer); a grudge at −40 | Incident, Social, Mood | "To Oren, the rain was Ostrel's anger at you for a broken promise." | 1 |
| M2 Blessing | A sign that reads "blessing", after a deed in `pleasedBy` | +10 opinion of the doer | Incident, Social, Mood | "To Ilex, the windfall was Saccar's thanks to Bram for a gift of blood." | 1 |
| M3 Gods who stir | Each god has a hidden Truth: whether it stirs. A stirring god angered by a recent deed nudges its signs' odds | The director reads deeds | Incident, Director | Across rewinds the player learns which gods answer | L (question 3) |

### 5.8 Seasonal festivals

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| S1 Festival | The seeded day of the custom's season, at dusk, with at least one keeper in camp | Culture event (a feast or a lean fire, F3). Every eligible member takes a `Feast` job at the hearth, ranked above ordinary work, for `hours`. Chats between attendees get +50 warmth. At the end, attendees get "Celebrated a festival" (+6), or +2 if a feast went lean | Work, Stock, Mood, Social | Mid-year story beats; on festival nights nobody keeps watch, and thieves roll as usual | 1 |
| S2 Setting down a grudge | Festival end; a keeper who attended holds a grudge against another attendee | **Cap: one grudge per keeper per festival**, the weakest (ties: lowest id). Its intensity drops by `forgive`; at 0 it ends with a Culture event that carries GrudgeEnded and cites the grudge's formation and the festival | Social, Mood | "On the Longest Night, Bram set down their grudge against Nia." | 1 |
| S3 Feasts and the stores | The player's Orders say "feasts may not use the stores" | Lean festivals, with keepers offended | Rations, Social | | 2, if playtests want it (question 7) |

### 5.9 Sacred places on the map

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| P1 Sacred tree | Seeded among the 6 trees nearest the hearth | Followers with devotion ≥ 30 never fell it (`Places.StandingTree` skips it). Ribbons hang on it on the map; the tooltip says "sacred to the Old Roots" | Map, Work, view | | 1 |
| P2 Desecration | Anyone else fells it, including a lapsed follower | If a faithful follower (devotion ≥ 30) is in camp: a Culture event citing the felling, and each such follower gets "Someone felled our sacred tree" (−45), with a grudge at −40. Otherwise it is marked silently. The stump keeps a torn ribbon | Work, Social, Mood, view | "Sera felled the old tree sacred to the Old Roots." "Hollis, who had long stopped praying, felled it anyway." | 1 |
| P3 Shrines | Build a shrine (Build spec) | A prayer spot, festivals there, and desecration rules | Build, Work | | L |
| P4 The river | A river faith's rites at the water's edge; river burial | | Travel, Map | | 2 |

### 5.10 Attitudes toward peoples

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| A1 Stance | A chat, insult roll or blood request between a person and someone of a people their culture has a stance on | revere / wary / fear / scorn shift warmth, insult odds and blood consent (section 6.5) | Social, Needs (blood), Mood | The Hearthfolk are wary of Ilex, the Reed Folk fear vampires, and the Ashen revere them | 1 |
| A2 The four peoples | The merge (section 6.10) | Content only: stances on dryads, djinn, fae and undead | Social | "The Hearthfolk never trusted the djinn's contracts" | 1 follow-up |
| A3 Default stances from axes | No explicit stance; the people's own axes against the culture's | A derived stance (requires species axes) | Social | | L |

### 5.11 Kin

| Hook | Trigger | Effect | Systems | Story | Slice |
|---|---|---|---|---|---|
| K1 Kin follow kin | A member leaves; a kinsman with opinion ≥ 40 of the leaver and ≤ 0 of the leader | That kinsman may follow within a day (a Culture event, then a departure). Capped at one follower per departure | Social | "Mira left. Two days later Wren, River-Born like her, followed." | 2 (watch for cascades) |

### 5.12 Conversion and drift (later)

| Hook | Trigger | Effect | Systems | Slice |
|---|---|---|---|---|
| C1 Devotion drift | An omen read (+), a festival kept (+), a sacrilege unpunished (−), long deep talks with a devout friend of the same faith (+) | Devotion changes by Dice; crossing 30 or 70 changes which faith customs apply | Social, Mood, Culture | 3 |
| C2 Conversion | A lapsed person, a devout close friend of another faith, a crisis "answered" by an omen | `FaithId` changes (a Culture event and a persistent effect, `Converted`) | Social | 3 |
| C3 Cultural drift and children | Years among another culture; children of mixed couples (hybrids, fixed decision 5) | Customs learned; a child's culture is inherited from both parents | Social, Growth | L |

---

## 6. Slice 1: exactly what to build

### 6.1 Scope

- **Content:** 4 cultures, 3 faiths (4 god slots), 12 customs of 7 kinds, 4 domains, 14 thoughts, and all strings (section 4.3).
- **Seeding and state:** `CultureBook`, the new character fields, `CultureGen`, `Ways` (queries), and the StateApi reads.
- **Hooks:**
  - G1–G4 (conversation and faux pas)
  - A1 (stances)
  - blood stance (taboo and gift, inside A1's consent and in `BloodJobs`)
  - H1–H2 (guest-right)
  - O1–O3 (oath weight, word spreads, debt of honour)
  - F1–F3 (leader eats last; feasts)
  - M1–M2 (omens)
  - S1–S2 (festivals)
  - P1–P2 (sacred tree)
- **Engine:** `SystemTag.Culture`, `DiceSystem.Culture`, and the deed log.
- **Surfaces:** inspector line and tooltip, tile tooltip, map ribbons, story log, soak counts.
- **Not in slice 1:** sections 5.4, 5.5, F4–F6, G5–G6, H3, K1, S3, P3–P4, C1–C3, M3, and any new command.

### 6.2 Changes, file by file

**New files**
- `content/cultures.json`, `faiths.json`, `customs.json`, `domains.json`, and their four schemas.
- `content/loc/en.culture.json`.
- `sim/State/Culture.cs` (section 4.4).
- `sim/Content/CultureDefs.cs`: `CultureDef`, `FaithDef`, `GodSlotDef`, `CustomDef` (with `Param(name)` like `IncidentDef`), `DomainDef`, `SignDef`, `EthicsDef`, `RangeDef`, `WeightedRef`, `PeopleWeight`, `CustomRef`, `AttitudeDef`.
- `sim/Systems/Culture/CultureGen.cs`: seeding (section 4.5).
- `sim/Systems/Culture/Ways.cs`: queries.
  - `KeptCustoms(w, c)` yields `(CustomDef, holderNameKey)`: culture customs first, then faith customs if `Devotion ≥ 30`, deduplicated by id.
  - `Keeps(w, c, kind)`, `Culture(w, c)`, `Faith(w, c)`, `Axis(w, c, axis)`, `OathWeightPct`, `Stance(w, c, other)`, `BloodBias(w, c)`, `OffersBlood(w, c)` (true when the summed `consentBias` of kept blood customs is above 0 and one of them has `offers = 1`), `DevotionWord`, `Assign`.
- `sim/Systems/Culture/CultureTuning.cs`: named constants in the ADR-0013 style (section 6.5).
- `sim/Systems/Culture/Greetings.cs`: G2 and G4.
- `sim/Systems/Culture/Omens.cs`: `RecordDeed`, `OnSign`, `Prune`.
- `sim/Systems/Culture/Festivals.cs`: start, feast, end, forgiveness.
- `sim/Systems/Culture/CultureSystem.cs`: `Hourly(w)`, which calls `Festivals.Hourly` and `Omens.Prune`.
- `sim/Systems/Jobs/CultureJobs.cs`: `Feast` and `RepayDebt`.
- Tests (section 6.7).

**Changed files**

| File | Change |
|---|---|
| `sim/Core/DrawIds.cs` | Append TruthIds `CultureName`, `CultureCustoms`, `CultureFaith`, `FaithName`, `FaithCustoms`, `GodDomain`, `GodName`, `GodTemper`, `PersonCulture`, `PersonFaith`, `PersonDevotion`, `SacredTree`, `FestivalDate`; DiceIds `FauxPas`, `GuestOffer`, `OmenRead`; `DiceSystem.Culture` (next contiguous value). Take the next free numbers on this branch. If another branch reaches main first with the same numbers, renumber this branch's values at merge (they have never shipped). The ADR records the final values, and nothing reuses a value once it is on main (ADR-0004). |
| `sim/Events/SimEvent.cs` | Append `SystemTag.Culture`. |
| `sim/State/Character.cs` | Five fields (section 4.4). |
| `sim/State/Job.cs` | Append `JobKind.Feast` and `JobKind.RepayDebt`. |
| `sim/State/World.cs` | `CultureBook Culture`; `SchemaVersion = 2`. |
| `sim/Content/ContentDb.cs` | Load, index and validate the four files (section 4.1); add `CultureById`, `FaithById`, `CustomById`, `DomainById` and the lists. |
| `sim/Content/RequiredContent.cs` | Add the 14 thoughts, `CustomKinds` (kind → required params and moments), `Deeds` and `Signs`. |
| `sim/WorldGen.cs` | Call `CultureGen.Seed(w)` last in `Create`; `AddSyntheticCharacters` copies the culture fields. |
| `sim/Simulation.cs` | In the hourly block, call `CultureSystem.Hourly(w)` after `SocialSystem.Hourly` and before `IncidentSystem.Hourly`. |
| `sim/Systems/SocialSystem.cs` | First meeting (G2 and G4) before the insult roll; warmth per direction; stance insult; festival warmth (section 6.3). |
| `sim/Systems/Relations.cs` | `Meet(w, a, b)` returns true on the first meeting and records both people in sorted order. `Break`: oath weight, guest wronged, word spreads, deeds. `Repay`: oath weight on promise kept, deeds. |
| `sim/Systems/Jobs/BloodJobs.cs` | Consent includes `BloodBias` and stance; custom refusal; custom offer key; deeds. |
| `sim/Systems/JobPicker.cs` | Blood-offer condition; `GuestJob`; `FeastJob`; `RepayJob`; pass the feller to `StandingTree`. |
| `sim/Systems/Places.cs` | `StandingTree(w, from, Character? feller = null)` skips a sacred tree for a follower with devotion ≥ 30. |
| `sim/Systems/Jobs/CareJobs.cs` | `KeepPromise` host mode (H1); `meal_shared` deed. |
| `sim/Systems/Jobs/CampJobs.cs` | `GatherTimber`: `tree_felled` deed, desecration (P2). |
| `sim/Systems/Jobs/NeedsJobs.cs` | `TakeReservedMeal`: `meal_taken` deed. |
| `sim/Systems/Jobs/JobRunner.cs` | Dispatch `Feast` and `RepayDebt`. |
| `sim/Systems/IncidentSystem.cs` | `Omens.OnSign(...)` after the event in `StartRain`, `Windfall`, `Spoil` and `Accident`. |
| `sim/Systems/Lifecycle.cs` | `Omens.OnSign(w, "death", e)` when the dead was a member. |
| `sim/Systems/Rations.cs` | F1 and F2 after the share-out loop (section 6.3). |
| `sim/Api/StateApi.cs` | `BelongingState` appended to `CharacterState` (built only in `StateApi.Character`); `Cultures`, `Culture`, `Faiths`, `Faith`; sacred-tree lines in `Location`. |
| `content/thoughts.json`, `content/loc/en.log.json`, `content/loc/en.ui.json`, `content/loc/en.json` | Entries and keys from sections 6.4 and 6.6. `log.system.culture` is required: `RunnerTests` enumerates every `SystemTag` and checks its loc key. |
| `tools/Return.Runner/StoryLog.cs` | Cast line with belonging; "Peoples and faiths" section; Culture events count as notable (like Incident and Combat). |
| `tools/Return.Runner/Soak.cs` | Culture story counts (section 6.8). |
| `game/scripts/InspectorPanel.cs`, `game/scripts/Logic/ViewText.cs` | Belonging line with tooltip (section 6.6). |
| `game/scripts/Art/WorldArt.cs`, `game/scripts/MapView.cs` | Ribbons on a sacred tree; a torn ribbon on its stump (`DrawStump`). Optional: hearth light ×1.3 during a festival (`Lighting`). |
| `DECISIONS.md`, `STORIES.md`, `BIBLE.md` | An ADR for this slice (final TruthId and DiceId values, founder answers, soak results); notable soak stories; a new bible section "6. Culture and faith" and a Culture row in the section 4.2 table (reads: social, needs, incidents, work, map; writes: mood, social, work, rations). |

### 6.3 Hook specifications

The rule for every hook: **a Culture event sits between its cause and its consequence.** It cites what triggered it (the arrival, the deed, the went-without, the grudge) and is cited by what it brings about (the grudge, the debt, the thought). The story analyzer follows a single backward path (ADR-0006), so a culture event hung off to the side never appears in a story.

**G1, G3, A1, S1: warmth and insults** (`SocialSystem.Step`)
- Warmth becomes directional:

  ```
  warmth(c→p) = max(0, 100 + 50 × chemistry + kin + stranger + stanceWarmth(c→p) + festival)
  ```

  where:
  - `kin` = +25 if both have the same non-empty culture
  - `stranger` = −25 if their cultures differ and either is within `NewcomerDays` of arriving
  - `festival` = +50 if both are attendees of a running festival

  Apply `warmth(c→p)` to c's "chatted" thought and `warmth(p→c)` to p's.
- Insult odds gain `stanceInsult(c→p)`.

**G2, G4: first meeting** (start of the per-initiator loop, after a partner is found and before the insult roll)
- If `Relations.Meet(w, c, p)` returns false (they have met before), skip.
- **Same non-empty culture:** both get `kin_greeting` about the other. Then the conversation continues as usual.
- **Otherwise**, if p has a culture whose greeting custom G is held, and c is not of that culture and has not learned G: roll `dice[Culture].Chance(FauxPas, min(1000, G.breachPermille × (c.CultureId == "" ? OutsiderBreachMultiplier : 1)))`. On a hit:
  - emit `event.culture.<G>.breach` (Culture), actors `[c, p]`, no causes, args `a = c`, `b = p`, `ways = p's culture name`;
  - give p `faux_pas` about c, citing the breach;
  - insert G into c's `LearnedCustoms`;
  - `continue`: no chat this time.

  On a miss, nothing happens.

**Blood stance and A1 consent** (`BloodJobs.RefusalKey` and `Refuse`)
- Keep the order: hungry, weak, low mood. Then:

  ```
  required = −(Σ trait ConsentBias + BloodBias(donor) + StanceConsent(donor → vampire))
  ```

  The donor consents if their opinion of the vampire is at least `required`.
- A refusal uses the **custom key** `event.culture.<id>.refused` (Culture, with `{ways}`) only if the custom was decisive: the opinion would meet the requirement without `BloodBias`, and the donor keeps a negative blood custom (`<id>` is the first one). Otherwise it uses the existing `event.social.blood_refused`.
- For a custom refusal, the vampire's `was_refused` thought uses `opinionScalePct = CustomRefusalOpinionPct` (40): it is not personal. Keep the thought id, because `RecentlyRebuffed` and `RefusedRecently` read it.
- Record `blood_refused` for plain and custom refusals.
- **Folding note:** `StoryLog.Repetition` folds runs only across identical keys. A run of refusals mixing plain and custom reasons now folds in shorter pieces. That is accepted, because "… and the same with Nia, Mira" would be false if their reasons differed.

**Blood gift** (`JobPicker`, `BloodJobs.Give`)
- The offer condition becomes `Traits.Any(OffersBlood) || Ways.OffersBlood(w, c)`.
- The offer event uses `event.culture.<id>.offered` (Culture) only when the donor lacks the OffersBlood trait, so Generous offers stay `event.social.blood_offered`.
- Record `blood_given` in `Transfer`.

**H1 Guest-bread** (`JobPicker.GuestJob`, after the Oathbound promise check; `CareJobs.KeepPromise` host mode)
- **Candidate:** c keeps `guest_right`; there is an open `PromiseMeal` debt d whose creditor is within `NewcomerDays` of arriving, has `Food < FedAt` and has no reservation; `c.Id ≠ d.DebtorId`; `Rations.HasReservation(c)`; `c.HungerStage < 2`; no one else is targeting d with `KeepPromise`.
- **Decide once** per (c, d): if `GuestOffers` has no entry, roll `GuestOffer` at `offerPermille` and record it. Only an accepted entry yields `Job { Kind = KeepPromise, TargetId = d.Id }`. (Re-rolling at every job pick would make the effective odds 100%.)
- **Host mode** is when `c.Id ≠ debt.DebtorId`:
  - At the stockpile, require `Rations.HasReservation(c)`, take one berry and call `Rations.Consume(c.Id)`.
  - At delivery:
    1. feed the creditor (as now);
    2. emit `event.culture.<id>.shared` (Culture), actors `[c, creditor]`, causes `[debt.FormedEvent, creditor.LastFoodEvent]`;
    3. give the creditor `guest_bread_received` about c;
    4. call `Relations.Repay(debt, shared)`: the debtor's promise is kept;
    5. call `Relations.FormDebt(creditor, c, DebtKind.Meal, 1, shared)`;
    6. set `c.LastFoodEvent = shared` (the host's coming hunger cites the gift);
    7. record deeds `guest_honoured` and `meal_shared`.
- Remove `GuestOffers` entries when their promise closes.

**H2 Wronged guest, O1 oath weight, O2 word spreads** (`Relations.Break`)
- After emitting `promise_broken` e, with the creditor still a member:
  - `pct = OathWeightPct(creditor)`.
  - **If** the creditor keeps `guest_right` and is within `NewcomerDays` of arriving:
    - emit `event.culture.<id>.wronged` (Culture), actors `[debtor, creditor]`, causes `[e]` → event g;
    - give `guest_right_broken` (creditor about debtor, cause g, `opinionScalePct = pct`);
    - grudge intensity `40 × wrongedGrudgePct / 100 × pct / 100`, cause g;
    - record deed `guest_wronged` (event g).
  - **Else:** `promise_broken` with `opinionScalePct = pct`; grudge `40 × pct / 100`, cause e.
  - Always record deed `oath_broken` (event e).
  - **Word spreads** if the creditor's culture template has `oathboundFree ≥ WordSpreadsFromAxis`:
    - listeners are the members of that culture other than the creditor and the debtor;
    - if there are any, emit `event.culture.word_spread` (Culture), actors `[debtor, …listeners]`, causes `[e]`;
    - each listener gets `heard_oath_broken` about the debtor;
    - a listener whose opinion is now ≤ `SocialSystem.GrudgeAtOpinion` and who has no grudge forms one (`HearsayGrudge`, cause: word spread).
- `Relations.Repay`:
  - for a promise: `promise_kept` with `opinionScalePct = OathWeightPct(creditor)`, plus deed `oath_kept`;
  - for a non-promise debt: deed `debt_repaid`.

**F1 and F2 A leader eats last** (`Rations.Assign`)
- During the loop, collect `(character, wentWithoutEvent)` for every `went_without` emitted in this call.
- After the loop, find the leader: the member whose id is `w.PlayerId`.
- **Offended:** for each collected c ≠ leader that keeps `leader_last`, when `w.Policy == SelfFirst` and `Rations.HasReservation(leader)`:
  - emit `event.culture.<id>.offended` (Culture), actors `[c, leader]`, causes `[wentWithout, w.PolicyEvent]`;
  - give c `leader_ate_first` about the leader;
  - grudge `LeaderLastGrudge` if c's opinion of the leader is ≤ −40 and there is no grudge.
- **Honoured:** if the leader is in the collected list, every member ≠ leader who keeps `leader_last`, holds a reservation and doesn't already have `leader_ate_last`:
  - emit `event.culture.<id>.honoured` (Culture), actors `[m, leader]`, causes `[leader's wentWithout]`;
  - give m `leader_ate_last`.
- `went_without` fires once per hunger episode (`SkippedRation`), so offence is naturally bounded.

**O3 Debt of honour** (`JobPicker.RepayJob`, after care and rescue, before blood offers; `CultureJobs.RepayDebt`)
- **Candidate:** c keeps `debt_of_honour`; there is an open debt d that is not a promise, with `DebtorId == c.Id`; the creditor is a member with `HungerStage ≥ 1`; `w.Tick − d.FormedTick ≥ minDebtAgeHours × TicksPerHour`; `c.HungerStage < 2`; no other `RepayDebt` targets d; and a berry is obtainable. Obtainable means one of:
  - a bush with berries, if c is not sun-blocked;
  - unreserved stock;
  - c's own reservation.
- **Job stages:**
  - **Stage 0:** go to the bush and work the forage duration (`work.json` `forage`), taking one berry. There is no accident roll: `JobRunner.RollAccident` draws from the Work stream, and culture draws only from its own. Or go to the stockpile and take one berry (calling `Rations.Consume` if c used their own share).
  - **Stage 1:** walk to the creditor (`JobRunner.Near`).
  - **Delivery:**
    1. add the berry's nutrition to the creditor's food (capped at 1000) and call `AfterFed`;
    2. emit `event.culture.<id>.repaid` (Culture), actors `[c, creditor]`, causes `[d.FormedEvent, creditor.LastFoodEvent]`;
    3. call `Relations.Repay(d, repaid)`, which emits `debt_repaid` with DebtRepaid;
    4. give the creditor `debt_honoured` about c;
    5. record deed `debt_repaid` (this comes through `Repay`).
- **Inflation guard:** the creditor must be hungry and the debt at least a day old. If soaks show more than 1.5 repayment stories per world-year, raise `minDebtAgeHours` rather than change the design. ADR-0014 shows what happens when a debt opens and closes too fast.

**M1 and M2 Omens** (`Omens.OnSign(w, sign, signEvent)`)

```
for each faith F (book order), each god G (slot order), each sign entry s in domain(G).Signs where s.sign == sign:
  anger = s.reads == "anger"; kinds = anger ? domain.AngeredBy : domain.PleasedBy
  for each member R (id order): R.FaithId == F.TemplateId, R.Devotion >= faith.OmenDevotion, !R.Downed,
                                no OmenRead by R within OmenCooldownHours:
    d = most recent Deed with Kind in kinds, Tick within DeedMemoryHours, DoerId != R.Id,
        doer is a member, and no OmenRead (R, d.EventId)
    if none: continue
    odds = R.Devotion × (anger ? G.Wrath : G.Grace) × DeedWeightPct[d.Kind] / OmenOddsDivisor
    odds = odds × (100 + ArcanePerAxisPct × Axis(R, arcaneMundane)) / 100, clamped to 0..OmenOddsMaxPermille
    if !dice[Culture].Chance(OmenRead, odds): continue
    e = emit (Culture) anger ? "event.culture.omen_anger" : "event.culture.omen_blessing",
        actors [R, doer], causes [d.EventId, signEvent.Id],
        args a=R, b=doer, sign=k:"sign.<sign>", god=k:G.NameKey, deed=k:"deed.<kind>"
    add OmenRead(R, d.EventId, now); thought omen_anger / omen_blessing (R about doer, cause e)
    if anger and Opinion(R→doer) <= GrudgeAtOpinion and no grudge: FormGrudge(R, doer, OmenGrudge, e)
```

The Dice roll happens only when a matching deed exists, so draws stay bounded. `Omens.Prune` (hourly) drops deeds older than `DeedMemoryHours` and reads older than `DeedMemoryHours + OmenCooldownHours`.

**S1 and S2 Festivals** (`Festivals.Hourly`)
- **End** any run with `w.Tick ≥ EndsTick`:
  - every attendee who is still a member gets `festival` (or `festival_lean` if a feast went lean);
  - if `forgive > 0`: for each keeper attendee, take the active grudge they hold against another attendee with the lowest intensity (ties: lowest id) and subtract `forgive`;
  - if that reaches 0 or below, set `g.Active = false` and emit `event.culture.<id>.forgave` (Culture), actors `[holder, target]`, causes `[g.FormedEvent, run.StartEvent]`, effects `[GrudgeEnded A = holder, B = target, RefId = g.Id]`; the holder gets `grudge_set_down`;
  - smaller reductions are silent;
  - then remove the run.
- **Start** at the start of an hour when `Calendar.Season == Season`, `DayOfSeason == Day`, `Hour == DuskHour`, and at least one keeper is a member. Keepers are members of the holder culture, or followers of the holder faith with devotion ≥ 30.
  - **Feast:** if `feastPerHead > 0` and `Rations.Unreserved(berries) ≥ keepersPresent`, take `n = min(feastPerHead × membersAwake, Unreserved)` berries and feed them one each to the n hungriest members (Food ascending, then id; capped at 1000; call `AfterFed`). Emit `begins` with `{n}` and `{item}`, and set `Feasted = true`.
  - **Lean:** if the custom has a feast but there is no surplus, emit `begins_lean`. Festivals with no feast emit `begins`.
  - Actors: the keepers present. Causes: none (the calendar).
  - Record `FestivalRun { EndsTick = now + hours × TicksPerHour }`.
- **The Feast job:** in `JobPicker`, after "eat when not fed fully" and before prioritized work, if a run is active and the member is not starving, downed or sun-blocked, choose `Feast` with a deterministic target beside the hearth: `Places.Adjacent(w, w.Map.Hearth, c.Pos)`, or `c.Pos` if there is none. Never use `Places.Near`, which draws `DiceId.WanderTarget` from the Work stream. The job adds the member to `run.Attendees` (sorted) when they arrive, and finishes when the run ends. The usual interrupts (starving, sun, exhaustion) still apply.

**P1 and P2 Sacred tree** (`Places.StandingTree`, `CampJobs.GatherTimber`)
- `StandingTree` skips a non-desecrated sacred tree when the feller follows its holder faith with devotion ≥ 30 (or belongs to its holder culture).
- After `felled_tree` e: record deed `tree_felled`. If the tree is sacred and not desecrated:
  - mark it `Desecrated`;
  - if any faithful follower (devotion ≥ 30) other than the feller is a member: emit `event.culture.<id>.desecrated` (Culture), actors `[feller]`, causes `[e]`, `ways` = holder name, and store it as `DesecratedEvent`;
  - record deed `sacred_harmed`;
  - each such follower gets `sacred_tree_felled` about the feller, and forms a grudge (`SacrilegeGrudge`, cause: desecration) if their opinion is ≤ −40.

### 6.4 New events, thoughts and effects

**Events** (all `SystemTag.Culture`; English in `en.culture.json`)

| Key | English | Causes | Persistent effect |
|---|---|---|---|
| `event.culture.clasp_greeting.breach` | {a} would not clasp {b}'s arm in greeting; to {ways}, that is a slight. (`_you_b`: …clasp your arm…) | none | none |
| `event.culture.no_touch_greeting.breach` | {a} took {b} by the hand at their first meeting; {ways} do not touch a stranger. | none | none |
| `event.culture.bow_greeting.breach` | {a} spoke to {b} without first bowing; {ways} take that as rudeness. | none | none |
| `event.culture.veiled_greeting.breach` | {a} looked {b} full in the eyes at their first meeting; to {ways}, that is a challenge. | none | none |
| `event.culture.blood_taboo.refused` | {b} refused to give {a} blood; {ways} forbid it. | ask | none (a grudge may follow, as now) |
| `event.culture.blood_gift.offered` | {a} offered {b} their blood, as {ways} teach. (`_you_a`: …your blood…) | vampire's food cause | none |
| `event.culture.guest_bread.shared` | {a} gave up their own meal for {b}, as {ways} do for a guest. (`_you_a`: …your own meal…) | promise formed, creditor's food cause | none (Repay and FormDebt follow) |
| `event.culture.guest_bread.wronged` | {b} had come as a guest; {ways} do not forgive a broken promise to a guest. | promise broken | none (a grudge follows) |
| `event.culture.word_spread` | Word that {a} broke a promise spread among {ways}. | promise broken | none |
| `event.culture.leader_last.offended` | {a} went without while {b} ate; among {ways}, a leader eats last. | went without, ration policy | none |
| `event.culture.leader_last.honoured` | {a} saw {b} go without so others could eat; among {ways}, that is how a leader is known. | leader's went without | none |
| `event.culture.debt_of_honour.repaid` | {a} brought {b} food to settle what they owed, as {ways} do. (`_you_a`: …what you owed…) | debt formed, creditor's food cause | none (`debt_repaid` follows) |
| `event.culture.harvest_fire.begins` | {ways} lit the harvest fire and shared {n} {item} among the camp. | none | none |
| `event.culture.harvest_fire.begins_lean` | {ways} lit the harvest fire, though there was nothing to spare for a feast. | none | none |
| `event.culture.harvest_fire.forgave` | At the harvest fire, {a} set down their grudge against {b}. (`_you_a`: …your grudge…) | grudge formed, festival | GrudgeEnded |
| `event.culture.longest_night.begins` | {ways} kept the Longest Night by the hearth. | none | none |
| `event.culture.longest_night.forgave` | On the Longest Night, {a} set down their grudge against {b}. (`_you_a`) | grudge formed, festival | GrudgeEnded |
| `event.culture.sacred_grove.desecrated` | {a} felled the old tree sacred to {ways}. | tree felled | none (grudges follow) |
| `event.culture.omen_anger` | To {a}, {sign} was {god}'s anger at {b} for {deed}. | deed, sign | none (a grudge may follow) |
| `event.culture.omen_blessing` | To {a}, {sign} was {god}'s thanks to {b} for {deed}. | deed, sign | none |

Sign phrases: `sign.rain` "the rain", `sign.windfall` "the windfall", `sign.food_spoils` "the spoiled food", `sign.work_accident` "the accident", `sign.death` "the death".

Deed phrases: `deed.oath_kept` "a promise kept", `deed.oath_broken` "a broken promise", `deed.debt_repaid` "a debt repaid", `deed.meal_taken` "a stolen meal", `deed.meal_shared` "a shared meal", `deed.guest_honoured` "kindness to a guest", `deed.guest_wronged` "a wronged guest", `deed.blood_given` "a gift of blood", `deed.blood_refused` "refusing blood", `deed.tree_felled` "a felled tree", `deed.sacred_harmed` "the felled sacred tree".

**Thoughts** (`thoughts.json`; add every id to `RequiredContent.Thoughts`)

| id | Text | Mood | Opinion | Hours | Emote | Stack |
|---|---|---|---|---|---|---|
| `faux_pas` | Was slighted by someone who did not know our ways | −3 | −6 | 48 | anger | 2 |
| `kin_greeting` | Met one of my own people | 3 | 6 | 24 | hearts | 1 |
| `guest_bread_received` | Was given a host's own meal | 8 | 16 | 72 | hearts | 1 |
| `guest_right_broken` | My guest-right was broken | −20 | −40 | 168 | anger | 1 |
| `heard_oath_broken` | Heard that someone broke their word | −2 | −8 | 120 | anger | 2 |
| `leader_ate_first` | Our leader ate while I went without | −6 | −15 | 120 | anger | 2 |
| `leader_ate_last` | Our leader went without so we could eat | 4 | 10 | 120 | sparkle | 1 |
| `debt_honoured` | Was repaid what I was owed | 4 | 10 | 72 | sparkle | 1 |
| `festival` | Celebrated a festival | 6 | 0 | 48 | music | 1 |
| `festival_lean` | Kept a lean festival | 2 | 0 | 24 | music | 1 |
| `grudge_set_down` | Set down an old grudge | 5 | 0 | 48 | sparkle | 1 |
| `omen_anger` | Saw a sign of the gods' anger at someone | −4 | −12 | 96 | alert | 2 |
| `omen_blessing` | Saw a sign of the gods' favour for someone | 5 | 10 | 96 | sparkle | 2 |
| `sacred_tree_felled` | Someone felled our sacred tree | −10 | −45 | 240 | anger | 1 |

**No new `EffectKind`, `DebtKind` or `RationPolicy`.** Guest-bread uses the existing `DebtKind.Meal`, whose `owes_meal` text exists but which nothing creates today.

### 6.5 Tuning constants (`CultureTuning.cs`, named as in ADR-0013; move to a `world.json` tuning block with the rest later)

| Constant | Value | Used by |
|---|---|---|
| `FaithCustomDevotion` | 30 | Faith customs apply at or above this |
| `LapsedBelow`, `DevoutFrom` | 30, 70 | Devotion words |
| `KinWarmth`, `StrangerWarmth`, `FestivalWarmth` | +25, −25, +50 | G1, G3, S1 |
| `OutsiderBreachMultiplier` | 3 | G4 |
| Stances: warmth, insult (‰), blood consent | revere +50 / −20 / +30; wary −25 / +20 / −10; fear −50 / +40 / −30; scorn −25 / +60 / −20 | A1 |
| `CustomRefusalOpinionPct` | 40 | Blood taboo refusal |
| `OathWeightPerAxisPct` | 25 | O1 (weight = 100 + 25 × axis) |
| `WordSpreadsFromAxis`, `HearsayGrudge` | 1, 15 | O2 |
| `LeaderLastGrudge` | 25 | F1 |
| `DeedMemoryHours`, `OmenCooldownHours` | 48, 120 | M1, M2 |
| `OmenOddsDivisor`, `OmenOddsMaxPermille`, `ArcanePerAxisPct`, `OmenGrudge` | 2000, 600, 25, 20 | M1 |
| `DeedWeightPct` | oath_kept 100, oath_broken 100, debt_repaid 80, meal_taken 100, meal_shared 60, guest_honoured 100, guest_wronged 150, blood_given 40, blood_refused 60, tree_felled 30, sacred_harmed 200 | M1, M2 |
| `SacrilegeGrudge` | 40 | P2 |

For a sense of scale: a devout reader (80) of a stern god (wrath 80), after an ordinary deed (100%), reads about 320‰ of matching signs; after a felled tree (30%), about 96‰.

### 6.6 What the player sees (plain words, no spreadsheet)

**The first minutes.** You start with Mira and Oren. On your first chat with Mira the Chronicle may say: "You would not clasp Mira's arm in greeting; to the Hearthfolk, that is a slight." When the vampire comes: "Wren refused to give Ilex blood; the Reed Folk forbid it." Nothing new is on screen permanently; culture shows in what people do and in the Chronicle's words.

**Inspector** (`InspectorPanel`; a label under the subtitle)
- Member: **"Of the Hearthfolk · follows the Sworn Word (devout)"**. With no faith: "Of the Hearthfolk · follows no faith". The otherworlder: **"From another world"**.
- **Tooltip on that line** (set `MouseFilter = Stop` and `TooltipText`), plain lines in this order:
  - "Their ways": the person's custom rules (culture first, then faith), axis lines ("Holds a promise heavy."), and stance lines ("Is wary of vampires.").
  - "Their gods": "Ostrel, Keeper of Oaths (stern): hates a broken promise and loves a kept one."
  - For anyone with learned customs, "Ways learned here": "The way of the Reed Folk: no touch at meeting."
  - For the otherworlder with nothing learned: "Knows none of the ways of this world yet."
- No numbers anywhere. Devotion is a word, temperament is a word, and axes appear only as their lines.
- Thoughts, opinions, grudges and debts already list the new thoughts and their causes.

**Chronicle.** Unchanged mechanism: Culture events appear as lines like any event (section 6.4).

**Tile tooltip** (`StateApi.Location` contents):
- "Tree (sacred to the Old Roots)".
- After desecration: "Stump (once sacred to the Old Roots)".
- During a festival, the hearth: "Hearth (the harvest fire is lit)".

**Map.**
- Two or three pale cloth strips hang from the sacred tree and sway with it.
- Its stump keeps one torn strip.
- Festival-goers walk to the hearth, and the festival thought shows the existing music emote.
- Optional: the hearth light is brighter during a festival.
- Everything is drawn from `w.Culture`; the view never changes the world (BIBLE section 3).

**Story log** (runner)
- Cast line: "Mira (Human, of the Hearthfolk, the Sworn Word): Protective, Oathbound. Lives in the camp." The otherworlder's line reads "from another world".
- A new **"Peoples and faiths"** section after the cast:
  - "Among the Hearthfolk: the clasped arm, guest-bread, the harvest fire (day 4 of autumn). Most follow the Sworn Word. They are wary of vampires."
  - "Followers of the Sworn Word honour Ostrel, Keeper of Oaths (stern), and Tamra, the Host (kindly)."
- Culture events count as notable, so they appear in "What happened".
- Loc keys:
  - `log.cast.line` becomes "{name} ({species}, {belongs}): {traits}. {status}."
  - `log.cast.belongs` = "of {culture}, {faith}"; `log.cast.belongs_no_faith` = "of {culture}"; `log.cast.outsider` = "from another world"
  - `log.peoples.heading` = "Peoples and faiths"
  - `log.peoples.culture` = "Among {culture}: {customs}. Most follow {faith}."
  - `log.peoples.stances` = "They {stances}." (using `stance.<s>.many`)
  - `log.peoples.dated` = "{custom} (day {day} of {season})"
  - `log.peoples.faith` = "Followers of {faith} honour {gods}."
  - `log.peoples.god` = "{god}, {epithet} ({temperament})"
  - `log.system.culture` = "Culture"
  - `log.soak.culture` = "Stories with culture or faith in them: {stories} ({share}% of all), in {worlds} of {total} worlds."

**Other loc:**
- `job.feast` "At the festival", `job.repay_debt` "Settling a debt".
- `species.human.plural` "humans", `species.vampire.plural` "vampires".
- `ui.inspector.belongs` "Of {culture} · follows {faith} ({devotion})", `ui.inspector.belongs_no_faith`, `ui.inspector.outsider`, `ui.inspector.ways` "Their ways", `ui.inspector.gods` "Their gods", `ui.inspector.god` "{god}, {epithet} ({temperament}): {cares}.", `ui.inspector.learned` "Ways learned here", `ui.inspector.learned_line` "The way of {culture}: {custom}.", `ui.inspector.no_ways`.

**Text-state API records:**

```csharp
public sealed record GodState(string Name, string Epithet, string Temperament, string Cares);
public sealed record BelongingState(string Line, string Culture, string Faith, string Devotion,
    IReadOnlyList<string> Ways, IReadOnlyList<string> Gods, IReadOnlyList<string> Learned);
public sealed record CultureState(string Id, string Name, IReadOnlyList<int> Members, string Faith,
    IReadOnlyList<string> Ways, IReadOnlyList<string> Stances, IReadOnlyList<string> Festivals);
public sealed record FaithState(string Id, string Name, IReadOnlyList<int> Followers,
    IReadOnlyList<GodState> Gods, IReadOnlyList<string> Ways);
```

`CharacterState` gains `BelongingState Belonging` as its last positional parameter. It is constructed only in `StateApi.Character`, so no caller breaks. `BelongingState.Gods` holds the formatted god lines.

### 6.7 Tests

New tests live in `tests/CultureContentTests.cs`, `CultureSeedTests.cs`, `CultureHookTests.cs` and `OmenTests.cs`, written in the style of `CausalityTests`: build a world, set state directly, call the system, and assert keys, causes and effects.

**Test seams:**
- Culture fields and god temperaments are public state; set them directly.
- Force odds with a test mod folder that overrides a custom by id (for example `breachPermille: 1000`, `offerPermille: 1000`) through `ContentDb.Load(dir, mods)`, the way `ModOverridesById` does.

**Content**
1. `CultureContentShape`: 4 cultures, 3 faiths, 12 customs, 4 domains; every custom's kind is in `RequiredContent.CustomKinds`.
2. `UnknownCustomKindFailsAtLoad`, `MissingCustomParamFailsAtLoad`, `UnknownDeedFailsAtLoad`, `MissingCustomEventTextFailsAtLoad`, `TwoGreetingsInOneCultureFailAtLoad`. Each names the file and the id.
3. `EveryCultureLocKeyExists`: name lists, epithets, cares, rules, convention keys, sign and deed phrases, plurals for attitude species.

**Seeding**

4. `CultureSeedingIsATruth`:
   - two worlds with the same seed have byte-equal `World.Culture` and equal person fields;
   - `CultureGen.Seed` changes no Dice stream's `Draws`;
   - reseeding Dice (`Rng.Dice.Reseed`) and seeding again changes nothing.
5. `CultureSeedsVary`: across seeds 1 to 50, every culture template has members in some world, every god slot shows at least two names, and at least three distinct (culture name, custom set) combinations appear.
6. `EveryoneButTheOtherworlderBelongs`: every named cast member has a culture and devotion within their culture's range; "you" has none and devotion 0.
7. `APeopleNoCultureNamesStillGetsOne`: a test mod adds species `dryad` (a copy of human) and a cast entry using it; after `WorldGen.Create` the dryad has a culture. This is the post-merge path.
8. `SyntheticCharactersCarryTheirCulture`: `AddSyntheticCharacters` copies the fields, and `RestoreUnderOneSecondAt200Characters` still passes.
9. `SeedingEmitsNoEvents`: the log is empty after `Create`.

**Hooks** (each asserts the event key, `SystemTag.Culture`, its causes and its consequences)

10. `BloodTabooRefusesAndSaysWhy`: a taboo donor with opinion 20 refuses with the custom key, citing the ask, and the vampire's `was_refused` opinion is 40% of normal. The same donor without the custom consents. A donor who would refuse anyway uses the plain key.
11. `BloodGiftOffersUnasked`: a faithful follower of a blood-gift faith offers to a thirsting vampire, with the custom offer key. A Generous donor uses the plain key.
12. `GuestBreadKeepsTheCampsPromise`: host mode emits `shared`. `promise_kept` cites `shared`; `owes_meal` (guest to host) cites `shared`; the host's reservation is consumed; `host.LastFoodEvent == shared`. With `offerPermille` 0, nothing happens, and the roll is made once per host and promise.
13. `AWrongedGuestHoldsAHeavierGrudge`: `wronged` cites `promise_broken`; the grudge is 60 at oath weight 100%; the thought is `guest_right_broken`.
14. `PromisesWeighByCulture`: creditor axis +2 gives a grudge of 60, axis −2 gives 20. `word_spread` fires only for a creditor culture at +1 or more, and listeners' thoughts cite it.
15. `ALeaderWhoEatsFirstOffends`: under SelfFirst with scarce food and the player holding a share, `offended` cites `[went_without, PolicyEvent]`. Under Hungriest with the player left out, `honoured` is given to keepers holding shares.
16. `OmensNeedADeedASignAndAFaithfulReader`:
    - a broken promise by "you", then rain within 48 h, then a devout reader whose god has the oaths domain and wrath forced to 100 gives `omen_anger` citing `[promise_broken, rain]`, plus the thought about "you";
    - a second rain within 120 h gives no second omen for that reader;
    - no deed means no omen and no Dice draw;
    - a lapsed reader gets no omen.
17. `FestivalsBeginOnTheirDayAndForgiveOneGrudgeEach`:
    - the run starts at dusk on the seeded day;
    - a feast happens only when `Unreserved ≥ keepers`, otherwise `begins_lean`;
    - a keeper with two grudges against attendees sets down only the weaker one;
    - `forgave` carries GrudgeEnded and cites `[grudge formed, begins]`.
18. `TheFaithfulSpareTheSacredTreeAndMournIt`: `StandingTree` never returns it for a follower (devotion ≥ 30); a lapsed follower fells it; `desecrated` cites the felling; faithful followers' grudges cite `desecrated`.
19. `StrangersStumbleOnceAndLearn`: with breach odds forced, a first meeting gives a faux pas and learning; the next first meeting with another member of that culture gives none; a same-culture first meeting gives `kin_greeting` to both and no event.
20. `DebtsOfHonourAreRepaidWhenTheCreditorIsHungry`: a keeper vampire with a blood debt over 24 h old, a hungry creditor and a bush with berries gives `RepayDebt`, then `repaid`, then `debt_repaid` with DebtRepaid citing `repaid`. Nothing happens before 24 h, or while the creditor is fed.
21. `StancesShiftWarmthInsultAndConsent`: the helper values for each stance.

**State, API and view**

22. `BelongingReadsInPlainWords`: the line for a member and for "you"; devotion is one of the three words; no digits in `Line`, `Ways` or `Gods` (regex).
23. `CultureAndFaithReads`: names, members and gods with temperament words.
24. `SacredTreeShowsInTheTooltip`.
25. In `GameViewTests`: `InspectorBelongingLine` (ViewText) and `ChronicleShowsCultureEvents`.

**Runner**

26. `StoryLogListsPeoplesAndFaiths`.
27. `SoakCountsCultureStories`: a synthetic chain containing a Culture event is counted, and `summary.json` has the new fields.

**Determinism and invariants**

28. `CultureCodeDrawsOnlyFromTheCultureStream`: a static scan of `sim/Systems/Culture/*.cs` and `CultureJobs.cs` finds only `DiceSystem.Culture`, and the three culture DiceIds appear nowhere else. The scan also fails on calls to helpers that draw indirectly from other streams: `Places.Near` (Work, `WanderTarget`), `JobRunner.RollAccident` (Work, `AccidentRoll`) and `IncidentSystem.EdgeTile` (Incident).
29. `SnapshotMidFestivalRestoresExactly`.
30. Existing tests must pass unchanged in intent: `SameSeedAndCommandsSameHash`, `SnapshotRestoreContinueMatchesStraightRun`, `MidActivitySnapshotsRestoreExactly`, `RestoreUnderOneSecondAt200Characters`, `TickCostUnderBudget` (≤ 2 µs per character per tick), `FullYearKeepsInvariants`, `SoakIsThreadCountIndependent`.

**Existing tests that must be updated:**
- `ContentTests.AllContentValidatesAgainstSchemas`: `Assert.Equal(9, files.Count)` becomes 13.
- `ContentTests.EveryLocKeyExists`: add the new defs and their list-valued key fields.
- `ContentTests.M0ContentShape`: unchanged unless the four-peoples merge changes it first.
- `RunnerTests`: the cast-line assertion `"  Mira (Human): Protective, "` becomes a prefix match on `"  Mira (Human, "`, because the culture varies by seed.
- `RunnerTests.LocCoversEveryKeyTheRunnerUses` checks both directions. Every `SystemTag` needs its `log.system.*` key, so add `log.system.culture`. It also **fails on any key in `en.log.json` that the runner code doesn't spell out as a full string literal**. Write each new `log.*` key as a literal in `StoryLog.cs` or `Soak.cs`, never assembled from parts. Keys the runner builds dynamically (`stance.<s>.many`, `custom.<id>.name`) belong in `en.culture.json`, which this test doesn't scan.
- Tests that pin exact seeded event sequences can shift because culture adds events and changes who refuses or eats. If one breaks, update its fixture or seed; don't weaken the behaviour. The ones checked so far (`GameViewTests.ChronicleIsNewestFirstAndCapped` and the runner's fold test, which builds its events by hand) should hold.

### 6.8 Soak acceptance

Before writing hook code, run `tools/soak --worlds 100 --years 1` on the merge base and keep `summary.json` as the **baseline**: stories per world, vampire departures plus deaths per world, and per-seed counts. Then, after slice 1, for seeds 1 to 100:

1. **Reach:** at least 60 of 100 worlds have one or more **culture stories** (a story whose chain contains a Culture event).
2. **Ceiling:** culture stories are at most 35% of all stories. This is the lesson of ADR-0013 (insults drowned everything) and ADR-0014 (instant promise chains).
3. **Variety:** at least 5 distinct culture links appear in stories across the soak. Links are counted by the first Culture event's family: greeting, blood stance, guest-right, leader-last, debt of honour, festival, sacred tree, omen, word spread.
4. **No crowding out:** non-culture stories per world are at least 85% of the baseline.
5. **Vampire guard:** vampire departures plus deaths per world are at most 125% of the baseline. Blood taboos must not starve the vampires out of most camps.
6. **Debt-of-honour guard:** repayment stories are at most 1.5 per world-year (section 6.3, O3).
7. **Stability:** no crashed worlds; serial and parallel soaks are byte-identical; a 10-world, 5-year soak finishes, festivals start in every year their keepers are present, and omens per world-year in year 5 are within ±50% of year 1.
8. **Strangers:** four fresh readers get the raw logs for seeds 1 to 4 (the ADR-0018 method). At least two retell, unprompted, a story with a custom, faith or god in it.

**Soak output:** `summary.json` gains `cultureStories`, `cultureStoryShare`, `cultureWorlds` and `byCultureLink` (a tally keyed by custom id, or by `omen_anger`, `omen_blessing` or `word_spread`). The printed report gains the `log.soak.culture` line.

**If 1, 3 or 5 fails,** tune the constants in section 6.5 and the custom params, not the design, and re-run. The ADR records the results honestly, as ADR-0017 did with the stories-per-world target.

### 6.9 Build order and stop points

Each step ends with all tests green and a commit. Steps 1 and 2 change no behaviour.

1. **Content and loading:** the four files, schemas, defs, `ContentDb` validation, loc, thoughts, and test updates. *Stop point A: content loads.*
2. **State and seeding:** `CultureBook`, the character fields, `CultureGen`, `Ways`, StateApi reads, inspector line and tooltip, story log cast line and peoples section. *Stop point B: every world shows its peoples, faiths and gods, and nothing behaves differently.*
3. **Engine:** `SystemTag.Culture`, `DiceSystem.Culture`, the deed log, and soak counting. **Measure the baseline on the merge base now** (section 6.8).
4. **Conversation:** `Met`, G1–G4, A1 warmth and insults.
5. **Blood:** taboo, gift and stance consent.
6. **Promises and guests:** O1, O2, H2, then H1 guest-bread.
7. **A leader eats last:** F1 and F2.
8. **Gods and omens:** M1 and M2. *Stop point C: run the soak. Every faith now has an effect, so this slice is mergeable here if time runs out, with the rest moved to slice 1b.*
9. **Festivals:** S1, S2 and F3 (optional hearth light).
10. **Sacred tree:** P1 and P2, with ribbons and stump, and the tooltip.
11. **Debt of honour:** O3, the biggest new job and the one most likely to need tuning after a soak.
12. **Finish:** full soak, tuning, the stranger check, STORIES.md, the ADR, and the bible section.

### 6.10 When the four peoples merge

The other branch adds species `dryad`, `djinn`, `fae` and `undead` with new cast members. This branch does not touch `cast.json` or `species.json`, so the merge is textual at most (`Character.cs`, `DrawIds.cs`, `RequiredContent.cs`).

- **It works with no change:** their cast members get cultures through the `"*"` weights (test 7).
- **Content follow-up**, one commit, no code, with suggested values:

| Culture | Add to `peoples` | Add to `attitudes` |
|---|---|---|
| hearth | djinn 2, fae 1, dryad 1 | undead: fear; fae: wary |
| reed | dryad 5, fae 3 | dryad: revere; djinn: wary |
| ashen | undead 4, djinn 3 | undead: revere; dryad: wary |
| night | undead 5, fae 2, djinn 1 | fae: revere |

  Add `species.<id>.plural` for each of the four.
- **Interplay to design together, not in slice 1:**
  - Djinn need contracts, and oath weight and debts of honour are the natural place to satisfy and threaten them.
  - Fae cannot lie, which interacts with breaking promises and greetings.
  - Undead need purpose; devotion and customs (rites, vigils) are candidates for that purpose.
  - Dryads need forest, which overlaps with the sacred grove and the grove god's hatred of felled trees. Keep the species need and the faith's reaction as separate hooks so they stack rather than double-count.

---

## 7. Example stories (as Chronicle lines)

These are written from the templates above. Times and people are illustrative, not from a soak.

**A blood taboo, and a sign in the rain** (Social → Culture → Incident → Culture → Social)

> Day 9, 20:05 — Wren insulted Ilex.
> Day 12, 22:40 — Ilex asked Wren for blood.
> Day 12, 22:40 — Wren refused to give Ilex blood; the Reed Folk forbid it.
> Day 13, 08:00 — Rain began to fall.
> Day 13, 08:00 — To Ilex, the rain was Saccar's anger at Wren for refusing blood.
> Day 13, 08:00 — Ilex now holds a grudge against Wren.

Ilex already disliked Wren (bad chemistry and the earlier insult, which hurts the Proud more). The custom refusal itself cost little, at 40% of a plain refusal. The omen's −12 is what crossed the −40 grudge line.

**Guest-bread and a debt of honour** (Incident → Social → Culture → Social → Needs → Culture → Social)

> Day 31, 14:10 — Hollis wandered in from the wilds, hungry and alone.
> Day 31, 14:32 — Hollis agreed to stay after you promised them a meal within 2 days.
> Day 31, 15:40 — Bram gave up their own meal for Hollis, as the Hearthfolk do for a guest.
> Day 31, 15:40 — You kept your promise to Hollis.
> Day 31, 15:40 — Hollis owes Bram for a shared meal.
> Day 32, 07:00 — Bram went without so others could eat.
> Day 32, 13:00 — Bram is getting hungry.
> Day 33, 18:20 — Hollis brought Bram food to settle what they owed, as the Ashen do.
> Day 33, 18:20 — Hollis repaid what they owed Bram.

**A leader eats last** (World → Needs → Culture → Social)

> Day 1, 00:00 — You decided that when food runs short, you eat first.
> Day 31, 07:00 — Mira went without so others could eat.
> Day 31, 07:00 — Mira went without while you ate; among the Hearthfolk, a leader eats last.
> Day 33, 21:40 — You insulted Mira.
> Day 34, 07:00 — Mira went without so others could eat.
> Day 34, 07:00 — Mira went without while you ate; among the Hearthfolk, a leader eats last.
> Day 34, 07:00 — Mira now holds a grudge against you.

Each offence costs 15 points of Mira's opinion of you and the insult 6. The grudge forms only once her opinion reaches −40, so this assumes it was already cool. From there one more slight takes her past the leaving line (−45 with a grudge, ADR-0013), and her departure cites this grudge.

---

## 8. Determinism, snapshots, text-state API, localization, mod hooks

**Truths (seeded once, survive rewinds).** Everything uses stateless hash draws keyed by stable strings:
- culture and faith names, customs, main faiths and festival dates (keyed by template id);
- gods' domains, names, epithets, wrath and grace (keyed by faith id and slot);
- the sacred tree (keyed by holder and custom);
- every person's culture, faith and devotion (keyed by cast key).

The same seed gives the same peoples, faiths and gods in every timeline.

**Dice (re-roll after a rewind).** All three draws use the new `DiceSystem.Culture` stream, so no culture roll shifts another system's sequence (ADR-0004):
- `FauxPas` (G4)
- `GuestOffer` (H1, once per host and promise)
- `OmenRead` (M1 and M2)

Everything else in this slice is a deterministic rule over state. Culture code never calls a helper that draws from another stream (test 28). Culture's effects still change what other systems draw next: a faux pas skips a chat's insult roll, and a feast changes who forages. That is ordinary behaviour change, not a breach of the stream rule.

**Rules kept:**
- integers only;
- lists in stable order;
- no dictionary iteration (the `ContentDb` dictionaries are lookups);
- no `w.NewId()` in seeding;
- no events during seeding;
- culture state lives in `World`, so `StateHash` covers it.

Determinism (two runs with the same seed) and snapshot tests cover the new state automatically; tests 4, 28 and 29 add culture-specific checks. Every world's hash changes, because behaviour changes; there are no golden hashes to update.

**Rewinds (when built).** Restoring a fate-knot snapshot restores culture state with everything else. One exception belongs to the rewind work: the otherworlder's `LearnedCustoms` is *player memory* (fixed decision 3: the player keeps their memories). The rewind code should carry it forward into the restored world. Then a player who died after offending the Reed Folk greets them correctly next time.

**Snapshots.** `CultureBook` and five character fields are serialised as they are. Bump `SchemaVersion` to 2 (there are no saves to migrate). Restore at 200 characters must stay under 1 s; the per-person additions are a few strings and two short lists.

**Text-state API.**
- `StateApi.Character(...).Belonging`, `StateApi.Cultures`, `Culture(id)`, `Faiths`, `Faith(id)` (records in section 6.6).
- `StateApi.Location` lists sacred trees and festival hearths.
- Everything is localised and JSON-serialisable through `StateApi.Json`, for the view, the runner, mods and agents.

**Localization.** Every player-facing string is a loc key in `en.culture.json` (plus a few in `en.log.json`, `en.ui.json` and `en.json`). Grammar stays in templates (section 4.2). `ContentDb` checks every key at load, and a missing key renders as `⟦key⟧`.

**Mod hooks.**
- Mods add or override cultures, faiths, customs and domains by id (merged like every other content file).
- A new custom of an existing kind is pure content. A new **kind** needs code, so an unknown kind fails at load with the file and the id.
- `IModHook.OnEvent` sees every Culture event at the end of its tick, like any other event.
- Later: a mod-registered custom kind through `IModHook`, if modders ask.

---

## 9. Later slices

**Slice 1b** (if step 8's stop point is used): steps 9 to 11.

**Slice 2: death, rest and kin** (after or alongside aging and multi-year soaks)
- Death rites D1 to D4: `perform_rite` work and `JobKind.PerformRite`, pyre timber against the roof, neglected rites as grievances, `DebtKind.Rite`, the view's ash and graves.
- Holy days R1 and R2 and fasting F4, designed with the Schedule system.
- Mourning silence G5; learning by living G6; bread and salt H3.
- Kin follow kin K1 (with a cascade cap and a soak guard on departures).
- The river rite P4.
- An Orders toggle for feasts S3, only if playtests want one.
- The four peoples' customs. Trait `ethics` nudges (a schema change).

**Slice 3: belief that moves:** devotion drift C1 and conversion C2 (a new `Converted` effect), and devotion shown changing in the inspector ("has grown devout").

**Later**
- Gods who stir M3 (a hidden Truth per god; question 3).
- Shrines and graves through Build and Zones (P3, D5).
- Food taboos F5 (after the items milestone).
- Other table orders F6; sworn vows O4.
- Default stances from axes A3.
- The **Ideas Tree** reactions (section 4.8).
- **Factions** with seats, state faiths and computed axes (kingdom layer).
- **Rival otherworlders** as prophets or heretics who found faiths (fixed decision 7).
- **Distortion anomalies** read as omens (fixed decision 9).
- The **Chronicle of Lost Futures** recording, for example, "in another timeline, the Hearthfolk forgave you".

**Growth tie-ins (fixed decision 12, M1).** Culture events are experiences with incident ids, which is what trait-mutation tables need. Examples:
- repeated `omen_anger` readings could harden someone toward zealotry;
- `forgave` at a festival could soften a Proud character;
- a desecration witnessed could seed a want ("see the grove avenged").

Shared memories should store Culture events with high salience (festivals, forgiveness, desecration).

---

## 10. Open questions for the founder (most important first)

The builder builds the recommended answer to each of these now.

1. **Approve the schema and determinism changes.**
   - Four new content files with schemas (`cultures`, `faiths`, `customs`, `domains`).
   - Fourteen new thoughts.
   - A new `SystemTag.Culture` and `DiceSystem.Culture`.
   - Thirteen TruthIds and three DiceIds.
   - Five character fields and `World.Culture`; snapshot `SchemaVersion` 2.

   CLAUDE.md requires asking before schema and determinism changes. *Recommendation: approve as specified. The builder works on a branch and does not merge to main until you do.*

2. **The player starts as an outsider with no culture and no faith.** They commit faux pas until they learn each people's ways, can't read omens, and are judged by every culture's rules (including "a leader eats last"). Once rewinds exist, they keep what they learned across deaths. *Recommendation: yes. It is the isekai premise made mechanical, and it makes Return by Death teach customs.*

3. **Are the gods real?** In slice 1 the gods act only through belief: people read signs and act on what they read. *Recommendation: keep it so for now. Later, give each god a hidden Truth (stirs or silent). A stirring god nudges the odds of its signs after a deed it hates, and only a player who keeps memories across rewinds can find out which gods answer.* Deciding the direction now affects how omen text is written: as perception ("To Oren, the rain was…") rather than fact.

4. **Cultures cross the lines between peoples.** A human can be raised Nightborn and a vampire can be Hearthfolk, with each culture leaning toward certain peoples. *Recommendation: yes. One culture per people would be simpler but would turn culture into a second species label, and lose stories like "Ilex, a vampire raised among the Hearthfolk, who still won't take blood from a guest".*

5. **Death rites, holy days and fasting wait for slice 2.** Member deaths happened in 2 of 100 one-year worlds, so rites would rarely fire until aging and 20-year soaks exist. Holy days belong with the Schedule spec. *Recommendation: accept. The full design is in sections 5.4 and 5.5.*

6. **How dark may invented faiths be?** The art is "dark and grungy and gorey". *Recommendation: dark folk religion is welcome (blood-gifts, taboos, curses, grim rites, gods who are feared), but no human sacrifice or torture rites in slice 1, and nothing that mirrors, names or parodies an Earth religion.*

7. **Does culture get its own player lever?** For example an Orders toggle, "feasts may use the stores". *Recommendation: not in slice 1. Culture reacts to the levers you already have (the ration policy, work priorities, who fells which tree). Add the toggle in slice 2 only if playtests show players want to say no to a feast.*
