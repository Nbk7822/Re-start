# Animals by habitat, snakes and venom, and game prior art (research notes)

Research for Return, 2026-11-09. This is reference material for the designers; it proposes no design. Every venom and poison number carries a source URL. Each animal card ends with its sources. Anything marked **(unverified)** comes from general knowledge and was not confirmed against a fetched source this session. Anything marked **(derived)** is my arithmetic on cited numbers.

Contents
- 0. How to read this, and how it maps onto the current sim
- 1. Temperate habitats: summary table, presence matrix, animal cards
- 2. Other biomes (world layer, later)
- 3. Snakes, venom and other poisons
- 4. Prior art from games (mechanics only)
- 5. Traps and open questions for the designers

---

## 0. How to read this, and how it maps onto the current sim

**The sim today** (from `/Users/nkov/Re-start/content/world.json` and `/Users/nkov/Re-start/sim/State/Map.cs`): a 48 x 32 tile map; terrain is Grass, Soil, Water, Rock (plus built Floor, Wall, Door); trees and berry bushes are objects; 4 seasons of 10 days; dawn at hour 6, dusk at hour 19. There are no animals in content or sim yet.

**Seasons.** Real dates below are Northern Hemisphere temperate. If the four sim seasons are read as meteorological seasons: spring = Mar to May, summer = Jun to Aug, autumn = Sep to Nov, winter = Dec to Feb. Each card's dates can be binned that way.

**Time of day.** Diurnal = active between dawn (6) and dusk (19). Nocturnal = active between dusk and dawn. Crepuscular = activity peaks around dawn and dusk. Several deer are "active around the clock with peaks at dawn and dusk", which is crepuscular with background activity.

**Which habitats the current map can already express**

| Habitat | Expressible now? | How, with today's tiles and objects |
|---|---|---|
| Old forest | Yes | Interior of a dense tree cluster |
| Woodland edge | Yes | Tiles bordering a tree cluster, with grass and berry bushes |
| Meadow / grassland | Yes | Open Grass away from trees |
| Heath / scrub | Partly | Would need a heath or scrub marker; today only approximable as Soil/Grass with berry bushes and Rock |
| Marsh / reed beds | Partly | Grass/Soil tiles adjacent to Water; no reed or wet-ground terrain yet |
| River / lake / pond | Yes | Water tiles (no flow, depth or fresh/salt distinction) |
| Rocky hills and uplands | Partly | Rock tiles; no elevation |
| Coast | No | Needs salt water, sand or shingle, and tides |

**Scale warning.** 48 x 32 tiles is small. Animals with large home ranges (wolf packs, bears, lynx, red deer herds) are best read as passing through the map, not living on it. RimWorld handles this by spawning wild animals at map edges and running "migration" events (section 4.1).

---

## 1. Temperate habitats

### 1a. Habitat summary

| Habitat | Signature animals | Peak danger to an adult person (when) | What people historically took | Signs to read |
|---|---|---|---|---|
| **Old forest** | Red deer, wild boar, wolf, brown bear, lynx, badger, red squirrel, tawny owl, capercaillie (old pine), hornet, wood ant, fire salamander (continental Europe), ticks | Brown bear: about half of attacks are a mother defending cubs, peaking in summer and daylight. Wild boar: rut, Nov to Jan, mostly solitary males. Rabid wolves (historically most wolf attacks). Hornet nests in hollow trees | Venison, pork, furs (squirrel, marten, bear), honey and wax from wild colonies (unverified), hides, antler | Rooting and wallows (boar); roaring (red deer rut); screams and barks at night (fox, badger); owl calls at night |
| **Woodland edge** | Roe deer, fallow deer, red fox, rabbit, hedgehog, wood pigeon, pheasant, adder, grass snake, slow worm, bees, wasps, ticks | Adder bites, summer; wasps, late summer (unverified); ticks, peaking late spring to early summer | Roe venison, rabbits, pigeons, game birds, berries | Roe "barks"; rabbit latrines and burrows; fraying on saplings |
| **Meadow / grassland** | Brown hare, rabbit, grey partridge, wild horse (historically), aurochs and wisent (historically), fox, mole, bees, adder on rough grassland and chalk downs | Low. Historically wild cattle ("could be very aggressive if provoked") and wild horses; adders on rough ground | Hares, rabbits, partridge; horses captured and ridden | Hare "forms"; hares "boxing" in spring daylight; molehills |
| **Heath / scrub** | Adder, smooth snake (Britain: heathland only), lizards, red grouse (heather moorland), rabbit, ticks, bees | Adder, especially spring basking and summer | Grouse, rabbits, heather honey (unverified) | Basking snakes on south-facing banks (unverified) |
| **Marsh / reed beds** | Grey heron, bittern, mallard, greylag goose, mute swan, otter, beaver, grass snake, frogs, toads, pond turtle (continental), eel, pike, mosquitoes, medicinal leech; water hemlock grows here | Historically malaria ("ague") in English marshes until the 19th century; water hemlock eaten by mistake; mute swans hissing at intruders near nests (low) | Wildfowl and eggs, eels, pike, leeches for medicine, beaver fur and castoreum | Frog and toad spawn at traditional ponds (timing unverified); bittern "booming" (unverified); heron colonies |
| **River / lake / pond** | Salmon, brown trout, pike, perch, carp (ponds), eel, crayfish, otter, beaver, dice and grass snakes, frogs, toads | Bad water (cholera, typhoid); a beaver grabbed by a fisherman killed him in 2013 (cornered-animal case) | Fish and eels (weirs and traps), crayfish, otter and beaver fur, castoreum | Otter spraints; beaver-felled trees, dams and lodges |
| **Rocky hills and uplands** | Alpine ibex, chamois, mouflon (wild sheep), red deer on moor and hill, golden eagle, peregrine, raven, adder on rocky hillsides (to about 3,000 m in the Alps), lynx | Low from animals; adders | Ibex and chamois meat and hides; falcons taken for falconry | Chamois and ibex herds on cliffs; raptor nests on crags (unverified) |
| **Coast** | Grey and harbour seals, gannet, puffin, guillemot (murre), eider, gulls, mussels, oysters, crabs, herring, weever fish in sand | Weever stings in sandy shallows (painful, rarely dangerous; season unverified); paralytic shellfish poisoning from mussels after algal blooms (no antidote) | Seabird eggs and young, eider down, seals, shellfish, fish | Seal haul-outs; seabird cliffs; shell middens (unverified) |

Sources for the table are on the animal cards (1c) and in section 3.

### 1b. Presence matrix

● = typical there; ○ = present at the margins, seasonally or passing through. "Hist." = gone from most of Europe now but present in a pre-modern world.

| Animal | Old forest | Woodland edge | Meadow | Heath / scrub | Marsh / reeds | River / lake | Rocky uplands | Coast |
|---|---|---|---|---|---|---|---|---|
| Red deer | ● | ● | ○ | ○ | | | ● (moor and hill) | |
| Roe deer | ○ | ● | ○ (fields) | ○ | | | | |
| Fallow deer | ● | ● | ○ (farmland) | | | | | |
| Moose (elk) | ● (boreal) | ○ | | | ● | ○ | | |
| Wild boar | ● | ● | ○ (crops) | ○ | ● | ○ | | |
| Wild horse (tarpan type), feral horse | | ○ | ● | ● | ○ | | ○ | ○ |
| Aurochs, wisent (hist.) | ● (wisent) | ● | ● (aurochs) | | ○ | | | |
| European rabbit | | ● | ● | ● | | | ○ | ○ (dunes) |
| Brown hare | | ○ | ● | ○ | | | | |
| Red fox | ○ | ● | ● | ● | ○ | | ○ | ○ |
| Grey wolf | ● | ● | ○ | ○ | | | ● | |
| Brown bear | ● | ○ | | | | ○ (salmon runs) | ● | |
| Eurasian lynx | ● | ○ | | | | | ● | |
| European badger | ● | ● | ○ | ○ | | | | |
| Eurasian otter | | | | | ● | ● | | ● |
| Eurasian beaver | ○ | | | | ● | ● | | |
| Red squirrel | ● | ○ | | | | | | |
| Hedgehog | ○ | ● | ● | ○ | | | | |
| Alpine ibex, chamois | | | | | | | ● | |
| Mouflon (wild sheep) | | | ○ | ● | | | ● | |
| European wildcat | ● | ● | | ○ | | | ○ | |
| Grey and harbour seal | | | | | | ○ (estuaries) | | ● |
| Wood pigeon | ● | ● | ● (crops) | | | | | |
| Pheasant, grey partridge | | ● | ● | ○ | ○ | | | |
| Red grouse | | | | ● (heather moor) | | | ○ | |
| Capercaillie | ● (old pine) | | | | | | | |
| Mallard, greylag goose | | | ○ (grazing) | | ● | ● | | ○ |
| Mute swan | | | | | ● | ● | | ○ |
| Grey heron | | | | | ● | ● | | ○ |
| Tawny owl | ● | ● | | | | | | |
| Goshawk, peregrine, golden eagle | ● (goshawk) | | | | | | ● | ○ (peregrine) |
| Raven | ○ | ○ | | | | | ● | ● |
| Seabirds (gannet, puffin, murre, eider, gulls) | | | | | | | | ● |
| Adder | | ● | ● (rough) | ● | ● | | ● | ○ (dunes) |
| Grass snake, dice snake | | ● | ○ | | ● | ● | | |
| Smooth snake | | | | ● | | | | |
| Slow worm, common lizard | | ● | ● | ● | | | | |
| Common frog, common toad | ○ | ● | ○ | | ● | ● | | |
| Fire salamander | ● (damp, continental) | | | | | ○ (streams) | | |
| Salmon, brown trout | | | | | | ● | | ○ (salmon at sea) |
| Pike, perch, carp, eel | | | | | ● | ● | | ○ (eel) |
| Mussels, oysters, crabs, herring, weever | | | | | | | | ● |
| Honey bee, bumblebee | ● (wild colonies) | ● | ● | ● | | | | |
| Wasps, hornets | ● (hornet) | ● | ● | | | | | |
| Wood ants | ● | ○ | | | | | | |
| Ticks | ● | ● | ● (long grass) | ● | | | | |
| Mosquitoes, medicinal leech | | | | | ● | ● | | |

### 1c. Animal cards

**Meat yield rule used below (derived).** Farmed red deer dress out at 53.7 to 65.7% of live weight (Bokor et al. 2023, https://doi.org/10.1016/j.meatsci.2023.109290); farmed fallow deer at 55.2% (https://pmc.ncbi.nlm.nih.gov/articles/PMC7374648/); farmed wild boar carcass yield 65.5% (Skewes et al. 2008, https://doi.org/10.1016/j.meatsci.2008.05.015). Hunters get about 48% of a deer's field-dressed weight as boneless venison (National Deer Association citing Ohio DNR, via https://www.outdoorlife.com/hunting/how-much-venison-from-deer/). Taken together, edible boneless meat is roughly **35 to 45% of live weight** for wild hoofed animals. The per-animal meat figures below are live weight times that range. For comparison, RimWorld uses meat = 140 x body size and leather = 40 x body size (section 4.1).

#### Mammals

**Red deer** (*Cervus elaphus*)
- Where: woodland and forest, but "can adapt to open moor and hills".
- Active: around the clock, peaking at dawn and dusk; uses open ground more at night where disturbed.
- Group: in woodland, solitary or mother and calf; on open ground, large single-sex groups that mix only in the rut; in the Scottish Highlands large groups persist most of the year.
- Seasons: rut late Sep to Nov; calves born mid-May to mid-Jul after an 8-month gestation; antlers cast Mar or Apr, regrown and clean of velvet by Aug or Sep.
- Toward people: flees (unverified). Stags clash in the rut and "sometimes sustain serious injuries"; both sexes kick with the forelegs when attacked.
- Danger to an adult: low; rutting stags Sep to Nov and cornered or wounded animals (unverified).
- Yields: stags 90 to 190 kg, hinds 63 to 120 kg live, so roughly 30 to 85 kg of meat per stag and 20 to 55 kg per hind (derived). Venison is eaten widely; farmed today. Hide, antler and sinew (unverified).
- Taming: not domesticated historically (unverified); farmed now.
- Signs: stags roar and grunt in the rut.
- Sources: https://bds.org.uk/information-advice/about-deer/deer-species/red-deer/ ; https://en.wikipedia.org/wiki/Red_deer

**Roe deer** (*Capreolus capreolus*)
- Where: "particularly associated with the edges of woodlands and forests"; also copses, scrub, hedgerows and nearby fields.
- Active: around the clock, peaking at dawn and dusk.
- Group: solitary; small groups in winter; territories held by sex.
- Seasons: rut mid-Jul to mid-Aug; delayed implantation (the egg implants in January); kids born May to Jun, usually two or three.
- Toward people: both sexes give dog-like barks when startled; flees.
- Danger to an adult: negligible (unverified).
- Yields: 10 to 25 kg live, so about 4 to 11 kg of meat (derived).
- Signs: slots about 4 cm long in soft ground, droppings on worn paths, frayed bark on small trees, scrapes, urine marking.
- Source: https://bds.org.uk/information-advice/about-deer/deer-species/roe-deer/

**Fallow deer** (*Dama dama*)
- Where: mature broadleaf woodland with understorey, open conifer woodland and open farmland.
- Active: around the clock, peaking at dawn and dusk; lies up by day to ruminate.
- Group: in large woods the sexes keep apart and meet only to breed; on open farmland they mix in large herds all year; herd size depends on density.
- Seasons: autumn rut (month unverified) with groaning bucks, rutting stands and, at high density, leks; one fawn in Jun or Jul after 229 days.
- Toward people: does and fawns give short barks when alarmed.
- Yields: bucks 46 to 93 kg, does 35 to 56 kg live, so about 16 to 42 kg and 12 to 25 kg of meat (derived).
- Taming: the Romans kept them in enclosures (vivaria); reintroduced to Britain in the 11th century; today's wild herds descend from medieval deer-park escapees. A park animal, never domesticated (unverified).
- Signs: slots about 6 cm, longer and deeper than roe; groaning in the rut.
- Sources: https://bds.org.uk/information-advice/about-deer/deer-species/fallow-deer/ ; https://en.wikipedia.org/wiki/Fallow_deer

**Moose, called elk in Europe** (*Alces alces*)
- Where: boreal and mixed forest with wetlands (unverified for habitat detail).
- Group: solitary, apart from a calf that stays with its mother until she comes into heat again (about 18 months).
- Seasons: autumn rut with fights between bulls.
- Toward people and danger: "more people in Alaska are injured by moose than by bears each year". Bulls may be aggressive in the rut (late Sep and Oct); cows with young calves attack people who get too close (late spring and summer); hungry, tired moose in deep winter snow. Warning signs: hump hairs raised, ears laid back, lip-licking. A charging moose "won't chase you very far".
- Yields: bulls 380 to 700 kg, cows 200 to 490 kg live, so about 130 to 315 kg and 70 to 220 kg of meat (derived).
- Sources: https://en.wikipedia.org/wiki/Moose ; https://www.adfg.alaska.gov/index.cfm?adfg=livewith.aggressivemoose

**Wild boar** (*Sus scrofa*)
- Where: from taiga to desert; in Europe, deciduous and mixed forest. Needs dense brush for cover and water for drinking and wallowing.
- Active: shifts to night to avoid people ("They avoid human contact through nocturnal lifestyles").
- Group: female-led sounders of barren sows and mothers with young, led by an old matriarch. Males leave at 8 to 15 months; adult males are mostly solitary outside the breeding season. Typical sounder 6 to 20 (unverified).
- Seasons: breeding Nov to Jan; farrowing Mar to May; litters average 4 to 6, at most 10 to 12. Males lose about 20% of body weight in the rut.
- Toward people: piglets "take cover or stand immobile"; frightened boars give loud huffing calls. Attacks are rare but serious: mostly in the rut (Nov to Jan), in farmland bordering forest or on forest paths, by solitary males. The boar charges with its tusks (most wounds to the thigh), steps back and charges again "if the victim is still moving".
- Danger numbers: a compilation of 1825 to 2012 found 412 attacks on at least 665 people, mostly non-hunting and unprovoked; in hunting, wounded animals were the main cause; only 4 fatal attacks in the US, 3 by wounded animals (Mayer 2013). A 2023 follow-up reportedly counts 172 deaths worldwide in 2000 to 2019 (secondary summary, unverified).
- Yields: European males average 75 to 100 kg (largest 200 kg), females 60 to 80 kg live, so about 26 to 45 kg and 21 to 36 kg of pork (derived). Coarse bristles (brush use unverified).
- Domestication: pigs were domesticated from wild boar "as early as 13,000–12,700 BCE in the Near East"; separately in China about 8,000 years ago.
- Signs: rooting (the head "acts as a plough"); mud wallows; huffing.
- Sources: https://en.wikipedia.org/wiki/Wild_boar ; Mayer 2013, https://digitalcommons.unl.edu/icwdm_wdmconfproc/151/ ; follow-up lead: https://digitalcommons.usu.edu/hwi/vol17/iss1/4/

**Wild horse and feral horse** (*Equus ferus*; the extinct European tarpan; Przewalski's horse; feral horses)
- Where: open grassland, steppe and heath (unverified for habitat detail). In the 12th century "large herds were hunted" in Denmark (Albertus Magnus).
- Group: harems led by a dominant mare with other mares, foals and immature horses, usually one herd stallion. Przewalski described troops of 5 to 15. Young stallions and stallions that have lost their harems form bachelor groups.
- Seasons: gestation 11 to 12 months; mares breed from about 3 years old. Foaling in spring (unverified).
- Toward people: a prey animal that runs (unverified). Historical accounts call the tarpan "absolutely untameable", defending itself "harshly against predators".
- Yields: Przewalski's horse about 300 kg, so about 105 to 135 kg of meat (derived); hides; mare's milk (milking is claimed at Botai).
- Domestication: modern domestic horses (the DOM2 lineage) come from a domestication "around 2200 BCE in the Volga–Don region". Earlier Botai sites (3700 to 3500 BCE) show intensive use of wild horses, debated as domestication. Neither the tarpan nor Przewalski's horse is the ancestor of domestic horses. The last tarpan died in captivity in Russia in 1918. Riding and draught after domestication.
- Sources: https://en.wikipedia.org/wiki/Feral_horse ; https://en.wikipedia.org/wiki/Przewalski%27s_horse ; https://en.wikipedia.org/wiki/Tarpan ; https://en.wikipedia.org/wiki/Domestication_of_the_horse

**Aurochs and wisent** (wild cattle and European bison; historical in most of Europe)
- Aurochs: the last one died in Jaktorów forest, Poland, in 1627. Historical descriptions say it "could be very aggressive if provoked, and was not generally fearful of humans". Domesticated twice in the Neolithic, giving cattle.
- Wisent: males 615 to 920 kg, females 424 to 633 kg (about 215 to 415 kg and 150 to 285 kg of meat, derived). Herds lived in the forest until the mid-17th century; hunted out of the wild in the early 20th century and reintroduced from captive animals.
- Sources: https://en.wikipedia.org/wiki/Aurochs ; https://en.wikipedia.org/wiki/European_bison

**European rabbit** (*Oryctolagus cuniculus*)
- Where: native to Iberia and south-west France, spread by people to "at least 800 islands and every continent except Antarctica". Best habitat is "short grasslands with secure refuge"; burrows on slopes and banks.
- Active: dawn, dusk and night (unverified on this page); spends most of its non-feeding time underground.
- Group: warrens of 1 to 5 adult bucks and 1 to 8 does, centred on territorial females.
- Seasons: in its native range breeding runs from autumn to spring, sometimes into summer (the British season differs, unverified); gestation 30 days; 3 to 7 young per litter; 4 to 5 litters a year.
- Toward people: runs and hides; flashes a white tail; thumps the ground to warn others.
- Danger: none. Predators: foxes, wolves, lynxes, badgers, wild cats, mustelids, eagles.
- Yields: 1.5 to 3 kg live (wild Iberian 0.9 to 1.4 kg); fur; bones were used for needles.
- Taming: "the only domesticated species of rabbit"; raised for food "since at least the first century BCE"; Pliny describes hunting rabbits with ferrets. "Coney" comes through Old French and Norman.
- Signs: dung hills ("latrines") marking territory; burrow entrances 10 to 50 cm with bare earth at the mouth.
- Source: https://en.wikipedia.org/wiki/European_rabbit

**Brown hare** (*Lepus europaeus*)
- Where: open fields and grassland (unverified for habitat detail).
- Active: "primarily nocturnal" and shy; in spring hares chase one another in broad daylight.
- Group: thought of as asocial but seen in large and small groups.
- Seasons: breeding Jan to Aug; three or four young per litter, three litters a year, up to ten young per litter; young are born in a surface hollow ("form"), not a burrow, and are active at once. "Boxing" in spring is usually a female fending off a male.
- Toward people: escapes by fast endurance running.
- Yields: 3.5 to 5 kg live, so about 1.2 to 2.3 kg of meat (derived).
- Signs: forms; spring boxing.
- Source: https://en.wikipedia.org/wiki/European_hare

**Red fox** (*Vulpes vulpes*)
- Where: almost everywhere, including settlements, which it has "successfully colonised".
- Active: hunts and scavenges in the late evening and at night.
- Group: pairs or small family groups (a mated pair and young, or a male with related females); young of the previous year help raise kits.
- Seasons: males reach breeding condition in Dec to Feb; gestation 49 to 58 days; litters average 4 to 6 kits (up to 13). Kits born in spring (unverified).
- Toward people: "too small to pose a significant threat to humans". Red foxes are "the most important rabies vector in Europe".
- Danger to an adult: only through rabies.
- Yields: 5 to 10 kg (Scandinavian race); "one of the most important fur-bearing animals harvested for the fur trade"; hunted as a pest for centuries. Takes poultry (unverified).
- Taming: a domesticated silver fox line exists from Russian breeding experiments.
- Signs: a wide vocal range "spanning five octaves"; a scream "commonly heard during the breeding season", thought to be vixens summoning males; dens ("earths", term unverified).
- Source: https://en.wikipedia.org/wiki/Red_fox

**Grey wolf** (*Canis lupus*)
- Active: "Wolves are nocturnal predators."
- Group: packs; average pack 5.5 in Europe and 8 in North America; across Eurasia a family of about eight (two adults, juveniles and yearlings), exceptionally up to 42.
- Seasons: gestation 62 to 75 days, "pups usually being born in the spring months or early summer in very cold places such as on the tundra"; dens with play areas and food remains around them.
- Toward people: attacks are rare "because wolves are relatively few, live away from people, and have developed a fear of humans".
- Danger numbers (NINA report, 2002 to 2020): 489 victims worldwide, of whom 380 (78%) were attacked by rabid wolves, 67 in predatory attacks and 42 in provoked or defensive attacks; 26 attacks were fatal, 14 of them involving rabies. Europe and North America together: 14 victims in 18 years, 2 fatal. Historically most wolf attacks involved rabies, with predatory attacks rarer (secondary summary).
- Yields: pelts; bounties (unverified on this page).
- Taming: the dog is a domesticated subspecies (date unverified).
- Signs: howling (wolves do not howl at the Moon).
- Sources: https://en.wikipedia.org/wiki/Wolf ; report summary https://wolf.org/?p=51272 ; report: Linnell, Kovtun and Rouart 2021, NINA Report 1944, https://hdl.handle.net/11250/2729772 (PDF not retrievable this session)

**Brown bear** (*Ursus arctos*)
- Where: semi-open country with cover to rest in by day.
- Active: day or night; near people, more nocturnal.
- Group: solitary except when mating or raising cubs; gathers at rich food (spawning salmon rivers, dumps).
- Seasons: lightest in spring after hibernation, heaviest in late autumn after hyperphagia. Mating mid-May to early July (later further north). Delayed implantation: the fertilized egg floats free for about six months; cubs are born eight weeks after it implants, "while the mother sleeps" in the winter den.
- Toward people and danger: of 664 attacks in 2000 to 2015 (North America 183, Europe 291, East 190), 14.3% were fatal (Europe 6.6%, North America 13.1%, East 32.0%). The main scenario was a female with cubs (47%). Attacks peak in summer and in daylight, when people are out; 22% of victims were hunting. Attacks rose over time and were commoner where bear density is high and human density low.
- Yields: 80 to 600 kg; fur; fat and meat (unverified).
- Taming: kept in zoos since antiquity, "tamed and trained to perform in circuses".
- Sources: https://en.wikipedia.org/wiki/Brown_bear ; Bombieri et al. 2019, https://pmc.ncbi.nlm.nih.gov/articles/PMC6562097/

**Eurasian lynx** (*Lynx lynx*)
- Where: "rugged country providing plenty of hideouts and stalking opportunities"; in mountains it moves down to the lowlands in winter, following prey and avoiding deep snow.
- Group: solitary (unverified on this page).
- Toward people and danger: no record of threat to people on the page (unverified).
- Yields: 12 to 32 kg; fur (unverified).
- Source: https://en.wikipedia.org/wiki/Eurasian_lynx

**European badger** (*Meles meles*)
- Active: nocturnal and crepuscular; sleeps by day in setts.
- Group: social clans; young stay in the family group.
- Seasons: 7 to 13 kg in spring, 15 to 17 kg in autumn before its "winter sleep"; litters of up to five cubs born in spring.
- Toward people: barks when surprised, screams when frightened; fights between badgers can be fatal; tame badgers "can make affectionate pets".
- Disease: carries bovine tuberculosis, which spreads to cattle.
- Signs: barks, whickers, screams; digs out wasp and bumblebee nests.
- Source: https://en.wikipedia.org/wiki/European_badger

**Eurasian otter** (*Lutra lutra*)
- Where: rivers, lakes, marshes and coasts (unverified for habitat detail).
- Seasons: breeds at any time of year.
- Yields: 7 to 12 kg (old males to 17 kg); fur (unverified).
- Signs: droppings ("spraints") used to mark territory.
- Source: https://en.wikipedia.org/wiki/Eurasian_otter

**Eurasian beaver** (*Castor fiber*)
- Group: monogamous pairs that stay together for several breeding seasons.
- Active: cuts trees at night (historical description).
- Toward people: in 2013 a Belarusian fisherman who "tried to grab" a beaver bled to death after it bit him and severed a leg artery (cornered-animal case).
- Yields: 11 to 30 kg; hunted "to near-extinction for both its fur and castoreum" (about 1,200 left in the early 20th century).
- Signs: felled trees, dams and lodges (unverified on this page).
- Source: https://en.wikipedia.org/wiki/Eurasian_beaver

**Red squirrel** (*Sciurus vulgaris*)
- Active: diurnal, from sunrise to after sunset.
- Group: solitary.
- Seasons: coat paler in winter.
- Yields: fur; Russian squirrel bellies were made into fur clothing whose pattern became the heraldic fur "vair".
- Source: https://en.wikipedia.org/wiki/Red_squirrel

**European hedgehog** (*Erinaceus europaeus*)
- Where: woodland, meadow, pasture, farmland, orchards and settlements.
- Seasons: hibernates in winter; breeds after hibernation.
- Yields: adults over 1,100 g (record 2,000 g).
- Venom: hedgehog blood plasma neutralizes the haemorrhagic activity of adder venom (Toxicon 1987, https://pubmed.ncbi.nlm.nih.gov/3590212/).
- Source: https://en.wikipedia.org/wiki/European_hedgehog

**Alpine ibex and chamois** (*Capra ibex*, *Rupicapra rupicapra*)
- Ibex: males 67 to 117 kg, females 17 to 32 kg; groups based on sex and age.
- Chamois: males 30 to 60 kg, females 25 to 45 kg; females and young in herds of up to 15 to 30, adult males mostly solitary; rut late Nov to early Dec in Europe, with fierce fights between males.
- Yields: about 23 to 53 kg of meat per male ibex, 10 to 27 kg per male chamois (derived).
- Sources: https://en.wikipedia.org/wiki/Alpine_ibex ; https://en.wikipedia.org/wiki/Chamois

**Mouflon** (*Ovis gmelini*): wild sheep "thought to be the ancestor of all modern domestic sheep breeds"; found on Mediterranean islands. Source: https://en.wikipedia.org/wiki/Mouflon

**European wildcat** (*Felis silvestris*): predominantly nocturnal in France and Italy, active by day when undisturbed. Source: https://en.wikipedia.org/wiki/European_wildcat

**Grey and harbour seals**: grey seal pups are born between September and March depending on place; harbour seals pup in spring to summer (April to May in California). Sealing for oil, skins and meat is historical (unverified). Sources: https://en.wikipedia.org/wiki/Grey_seal ; https://en.wikipedia.org/wiki/Harbor_seal

#### Birds

| Bird | Where | Habits that matter | What people took | Source |
|---|---|---|---|---|
| Wood pigeon | Woods, fields | Eats cereals, "considered an agricultural pest" | Food (unverified) | https://en.wikipedia.org/wiki/Common_wood_pigeon |
| Pheasant, grey partridge | Edges, fields | Ground-nesting game birds (unverified) | Game birds (unverified) | https://en.wikipedia.org/wiki/Common_pheasant ; https://en.wikipedia.org/wiki/Grey_partridge |
| Red grouse | "Heather moorland in Great Britain and Ireland" | Lives on heather | Shot as game (unverified) | https://en.wikipedia.org/wiki/Red_grouse |
| Capercaillie | Old pine forest | Males display at a lek (in spring, unverified); hens arrive late in the courting season. Lekking males attacking people (unverified) | Game bird (unverified) | https://en.wikipedia.org/wiki/Western_capercaillie |
| Mallard | Marsh, rivers | Northern birds migrate (unverified for mallard) | "The main ancestor of most breeds of domestic duck" | https://en.wikipedia.org/wiki/Mallard |
| Greylag goose | Marsh, grazing | Northern birds migrate south for winter | Ancestor of most domestic geese, "domesticated at least as early as 1360 BCE"; feathers for arrows (unverified) | https://en.wikipedia.org/wiki/Greylag_goose |
| Mute swan | Lakes, rivers | "Strongly territorial", hisses at intruders; nests on mounds in shallow water in spring; colonies of 100+ pairs where food allows | Food at feasts (unverified) | https://en.wikipedia.org/wiki/Mute_swan |
| Grey heron | Any watery habitat | Colonial nester (unverified) | Hunted with falcons, eaten at feasts (unverified) | https://en.wikipedia.org/wiki/Grey_heron |
| Tawny owl | Woods | "A nocturnal bird of prey"; can strike people near its nest (unverified) | None | https://en.wikipedia.org/wiki/Tawny_owl |
| Peregrine, goshawk, golden eagle | Cliffs, forest | Raptors | Falconry: peregrines prized for "strong hunting ability, high trainability" | https://en.wikipedia.org/wiki/Peregrine_falcon |
| Raven | Uplands, coast | "Unusually intelligent" problem-solver; scavenges (unverified) | Tamed as pets (unverified) | https://en.wikipedia.org/wiki/Common_raven |
| Eider | Coast | Nests lined with down plucked from the female's breast | Eiderdown "long been harvested for filling pillows and quilts" | https://en.wikipedia.org/wiki/Common_eider |
| Gannet, puffin, guillemot (murre) | Sea cliffs, islands | Dense breeding colonies (St Kilda and Sula Sgeir gannetries) | Young and eggs harvested by island communities (unverified) | https://en.wikipedia.org/wiki/Northern_gannet ; https://en.wikipedia.org/wiki/Atlantic_puffin ; https://en.wikipedia.org/wiki/Common_murre |

#### Reptiles and amphibians

(The adder and the other snakes are in section 3.)

| Animal | Where and when | Toward people | Notes | Source |
|---|---|---|---|---|
| Grass snake (and barred grass snake) | Semi-aquatic, often near water; feeds almost only on amphibians; males emerge first in spring to bask | Harmless; plays dead and gives off a foul smell (unverified) | Lays 8 to 40 leathery eggs in Jun to Jul, hatching in about 10 weeks; eggs need at least 21 °C (so warm compost and manure heaps, unverified) | https://en.wikipedia.org/wiki/Grass_snake ; https://en.wikipedia.org/wiki/Barred_grass_snake |
| Dice snake | Water snake; hibernates Oct to Apr in dry holes near water | Harmless | | https://en.wikipedia.org/wiki/Dice_snake |
| Smooth snake | In Britain "restricted to heathland habitats" | Harmless; subdues prey by constriction | Rare, secretive (unverified) | https://en.wikipedia.org/wiki/Smooth_snake |
| Slow worm | Grassland, gardens, edges | Harmless "legless lizard"; sheds its tail | Often taken for a snake (unverified) | https://en.wikipedia.org/wiki/Common_slow_worm |
| Common toad | Solitary, but converges in large numbers on traditional breeding ponds; many killed on roads during annual migrations | Skin glands secrete bufotoxin to deter predators (grass snakes seem unaffected) | Handling then eating is the risk (unverified) | https://en.wikipedia.org/wiki/Common_toad |
| Common frog | Ponds and marsh; mass spawning in early spring; hibernates only in the coldest months | Harmless | Frogs' legs eaten (unverified) | https://en.wikipedia.org/wiki/Common_frog |
| Fire salamander | Damp continental forest; active in the evening and at night, and by day when it rains | Its alkaloid samandarin "causes strong muscle convulsions and hypertension" in all vertebrates | Folklore of salamanders running out of burning logs (unverified) | https://en.wikipedia.org/wiki/Fire_salamander |

#### Fish and shellfish

| Animal | Where and when | Notes | Source |
|---|---|---|---|
| Atlantic salmon | Rivers and sea | Returns from the sea to spawn in rivers; 5 to 10% survive to spawn again; long a target of fishing. Autumn spawning runs, and bears and people fishing them, are well known (timing unverified) | https://en.wikipedia.org/wiki/Atlantic_salmon |
| Brown trout | Clear streams and lakes (unverified) | | https://en.wikipedia.org/wiki/Brown_trout |
| Northern pike | Lakes, slow rivers, marsh | "Typical ambush predators"; average 40 to 55 cm, record 28.4 kg | https://en.wikipedia.org/wiki/Northern_pike |
| Common carp | Ponds | Farmed "since Roman times" in specially built ponds | https://en.wikipedia.org/wiki/Common_carp |
| European eel | Rivers, marsh, coast | Spawns in the Sargasso Sea; glass eels enter estuaries and migrate upstream. Adults run back to sea in autumn (unverified); eels as rent or currency in medieval England (unverified) | https://en.wikipedia.org/wiki/European_eel |
| Mussels, oysters | Coast | Filter feeders: can carry paralytic shellfish toxin after algal blooms (section 3.9) | https://en.wikipedia.org/wiki/Paralytic_shellfish_poisoning |
| Weever fish | Sandy shallows | Buries in sand by day; venomous dorsal spines (section 3.9) | https://en.wikipedia.org/wiki/Weever |

#### Insects and other invertebrates

| Animal | Where and when | Toward people and danger | What people took | Source |
|---|---|---|---|---|
| Honey bee | Colonies of "tens of thousands" in hives or hollow trees (hollow trees unverified); swarms (season unverified). Spring and summer workers live a few weeks; autumn-born workers cluster inside for months | Workers have barbed stingers and defend the hive; numbers in section 3.9 | Honey, wax (unverified on this page) | https://en.wikipedia.org/wiki/Western_honey_bee |
| European hornet | Nests in "hollow tree trunks" | "Stings in response to being stepped on or grabbed, but generally avoids conflict"; defends its nest; aggressive around food | | https://en.wikipedia.org/wiki/European_hornet |
| Common wasp | Ground and cavity nests; the queen founds the colony in April after hibernating, and it collapses around October when she dies | Late-summer stinging around food (unverified) | | https://en.wikipedia.org/wiki/Vespula_vulgaris |
| Red wood ant | Boreal and old forest; mound nests (unverified) | Bites and sprays formic acid (unverified) | | https://en.wikipedia.org/wiki/Formica_rufa |
| Sheep tick | Long grass, woodland, heath | Carries Lyme disease and tick-borne encephalitis; nymphs "quest" on vegetation, peaking in late spring and early summer | | https://en.wikipedia.org/wiki/Ixodes_ricinus |
| Mosquitoes | Marsh and standing water | Malaria was "an important cause of illness and death in several parts of England" from 1564 to the 1730s, declining only in the 19th century | | https://pmc.ncbi.nlm.nih.gov/articles/PMC2627969/ |
| Medicinal leech | Ponds and marsh | Bites painlessly (unverified) | Used in medicine since antiquity; "over-exploitation by leech collectors in the 19th century" | https://en.wikipedia.org/wiki/Hirudo_medicinalis |

---

## 2. Other biomes (world layer, later)

Short reference only. Facts are from the linked pages; anything not in them is marked.

| Biome | Signature animals | Main dangers to people | What people got / domesticated | Seasonal or story hooks | Sources |
|---|---|---|---|---|---|
| **Boreal forest / taiga** | Moose, reindeer, brown bear, wolf, lynx, beaver, capercaillie; sable and marten (unverified) | Moose ("more people in Alaska are injured by moose than by bears"); bears, especially mothers with cubs | Reindeer: "the only successfully semi-domesticated deer on a large scale in the world"; furs (sable, beaver, squirrel) | Bear hibernation; moose rut late Sep to Oct; summer biting-fly swarms (unverified) | https://en.wikipedia.org/wiki/Reindeer ; https://www.adfg.alaska.gov/index.cfm?adfg=livewith.aggressivemoose ; https://pmc.ncbi.nlm.nih.gov/articles/PMC6562097/ |
| **Tundra** | Reindeer and caribou (migratory and sedentary populations), muskox, Arctic fox, polar bear on coasts, summer geese (unverified) | Polar bears: less sea ice means more time on land and "increasing conflicts with humans" | Reindeer herding; muskox wool (unverified) | Long migrations; summer-only abundance (unverified) | https://en.wikipedia.org/wiki/Reindeer ; https://en.wikipedia.org/wiki/Muskox ; https://en.wikipedia.org/wiki/Polar_bear |
| **Steppe** | Wild horses, saiga, Bactrian camel, wolves, marmots (unverified) | Low; wolves | Horses: modern domestic horses come from a domestication "around 2200 BCE in the Volga–Don region"; Bactrian camels, 2 million, "mainly in the domesticated form" | Saiga herds migrate south in winter | https://en.wikipedia.org/wiki/Domestication_of_the_horse ; https://en.wikipedia.org/wiki/Saiga_antelope ; https://en.wikipedia.org/wiki/Bactrian_camel |
| **Desert** | Dromedary, scorpions, saw-scaled viper, fennec and oryx (unverified), locusts (unverified) | Scorpions: over 1.2 million stings and over 3,250 deaths a year, children worst; north-Saharan Africa, the Sahel and the Near and Middle East are high-risk zones. Saw-scaled viper, one of India's "big four" | Dromedary: "probably first domesticated in the Arabian Peninsula about 4,000 years ago"; meat, milk, riding, pack | Night activity of scorpions and vipers (unverified) | https://en.wikipedia.org/wiki/Dromedary ; https://pubmed.ncbi.nlm.nih.gov/18579104/ ; https://en.wikipedia.org/wiki/Echis_carinatus |
| **Savanna** | Elephant, lion, African buffalo, hippo, Nile crocodile, black mamba, puff adder, tsetse fly | Hippo, "among the most dangerous animals in the world due to their aggressive and unpredictable nature"; Nile crocodile, "responsible for hundreds of human deaths every year"; puff adder, "the most snakebite fatalities in Africa"; black mamba deaths peak in its breeding season, Sep to Feb | Few local domesticates (unverified). Tsetse flies carry nagana in "cattle and horses", with "pronounced economic and public health consequences in sub-Saharan Africa" | Great herd migrations (unverified); tsetse belts that exclude cattle and horses | https://en.wikipedia.org/wiki/Hippopotamus ; https://en.wikipedia.org/wiki/Nile_crocodile ; https://en.wikipedia.org/wiki/Puff_adder ; https://en.wikipedia.org/wiki/Tsetse_fly ; https://en.wikipedia.org/wiki/Black_mamba |
| **Tropical forest** | Tiger (including Sundarbans mangroves), fer-de-lance, kraits, cobras, Anopheles mosquitoes, leeches (unverified), red junglefowl (unverified) | Fer-de-lance, "responsible for the most envenomated snakebites within its range, largely due to its proximity to humans, livestock and pets"; common krait bites sleepers at night; malaria | Chicken from red junglefowl (unverified); tamed elephants (unverified) | Monsoon flooding drives snakes into houses (unverified) | https://en.wikipedia.org/wiki/Bothrops_asper ; https://en.wikipedia.org/wiki/Common_krait ; https://en.wikipedia.org/wiki/Anopheles ; https://en.wikipedia.org/wiki/Tiger |
| **High mountains** | Alpine ibex, chamois, snow leopard, golden eagle, marmots (unverified), yak and llama as domesticates | Low from animals; adders reach about 3,000 m in the Alps | Yak (milk, wool, pack) and llama (pack) (unverified details) | Chamois rut late Nov to early Dec; mountain lynx move down in winter | https://en.wikipedia.org/wiki/Chamois ; https://en.wikipedia.org/wiki/Alpine_ibex ; https://en.wikipedia.org/wiki/Snow_leopard ; https://en.wikipedia.org/wiki/Llama |
| **Swamp** | Cottonmouth (North America), water buffalo, crocodiles, leeches, mosquitoes, eels | Cottonmouth, "in or near water... slow-moving and shallow lakes, streams, and marshes", with "a painful and potentially fatal bite"; malaria (endemic in English marshes 1564 to the 1730s) | Water buffalo domesticated for draught (rice-paddy role unverified) | Malaria season in summer (unverified) | https://en.wikipedia.org/wiki/Agkistrodon_piscivorus ; https://en.wikipedia.org/wiki/Water_buffalo ; https://pmc.ncbi.nlm.nih.gov/articles/PMC2627969/ |

---

## 3. Snakes, venom and other poisons

### 3.1 Which snakes live where

**Temperate Europe**
- **Adder** (*Vipera berus*), the only venomous snake across most of northern Europe (unverified). It has "the widest distribution of any other terrestrial snake", from Scotland to Sakhalin and from the Balkans (42°N) to beyond the Arctic Circle (69°N) (https://pmc.ncbi.nlm.nih.gov/articles/PMC6409582/). Habitats include "chalky downs, rocky hillsides, moors, sandy heaths"; "sufficient habitat complexity is crucial"; up to about 3,000 m in the Swiss Alps.
  - Active: "mainly diurnal, especially in the north"; further south also in the evening and on summer nights.
  - Seasons: hibernates about 150 days (British males) or 180 days (females), 8 to 9 months in northern Sweden; basks on mild winter days. Mating in the last week of April (Hungary) to the second week of May (north). Live young born Aug or Sep (July to early October).
  - Toward people: "not aggressive and usually bites only when really provoked, stepped on, or picked up". Most British bites were to men "who foolishly picked up the adder" (https://pubmed.ncbi.nlm.nih.gov/1276841/).
  - Numbers: Sweden records about 1,300 bites a year, about 12% hospitalized. Severe (grade II to III) adder bites across Europe ran at 0.6 per million people a year in 1980 to 2010, "with a peak in the summer". A bite injects about 10 to 18 mg of venom (PMC6409582).
  - Source for habits: https://en.wikipedia.org/wiki/Vipera_berus
- **Asp viper** (*Vipera aspis*), south-western Europe; "approximately 4% of all untreated bites are fatal" (Stemmler 1971, cited at https://en.wikipedia.org/wiki/Vipera_aspis).
- **Nose-horned viper** (*Vipera ammodytes*): northern Italy, the Balkans and Asia Minor (https://en.wikipedia.org/wiki/Vipera_ammodytes).
- **Harmless**: grass snake and barred grass snake (semi-aquatic, near water), dice snake (water), smooth snake (Britain: heathland only), and the slow worm, which is a legless lizard. See the reptile table in 1c.

**Temperate North America**
- **Timber rattlesnake**: woodland. Gravid females favour "open, rocky ledges"; others favour "cooler, denser woodland" (https://en.wikipedia.org/wiki/Timber_rattlesnake).
- **Eastern copperhead**: a habitat generalist that hibernates "in dens or limestone crevices, often together with timber rattlesnakes"; bites "are rarely fatal" (https://en.wikipedia.org/wiki/Eastern_copperhead).
- **Cottonmouth**: in or near water (swamp row in section 2).
- **Harmless garter snakes**: "forests, fields, and prairies to streams, wetlands, meadows, marshes, and ponds" (https://en.wikipedia.org/wiki/Common_garter_snake).

### 3.2 How deadly: untreated versus treated

| Snake | Untreated | Treated / modern | Notes | Source |
|---|---|---|---|---|
| **European adder** | Britain: "only 14 deaths from poisoning" in about 100 years (1876 to 1975). England and Wales 1950 to 1972: **1 adder death against 61 deaths from bee or wasp stings** | Sweden, average annual deaths per 100,000 people: 0.02 (1911 to 1949), 0.003 (1950 to 1979), 0.0008 (1980 to 2009). Europe: fewer than 5 deaths a year. Czech registry 1999 to 2013: 191 bites, no deaths | At about 1,300 bites a year in Sweden today, the modern death rate is roughly 1 in 20,000 bites (derived: 0.0008 per 100,000 x about 9 million people / 1,300). The 1911 to 1949 rate is about 1 in 1,000 if bites were as frequent then (derived, assumption flagged). A 1957 German paper is reported to give 0.4% snakebite mortality for Sweden and Norway in 1946 to 1950 (secondary, unverified; https://www.thieme-connect.de/products/ejournals/pdf/10.1055/s-0028-1116836.pdf, not read) | https://pubmed.ncbi.nlm.nih.gov/1276841/ ; https://pmc.ncbi.nlm.nih.gov/articles/PMC6409582/ ; https://pmr.lf1.cuni.cz/115/3/120/ |
| **Asp viper** | About 4% of untreated bites fatal (Stemmler 1971) | | Higher than the adder | https://en.wikipedia.org/wiki/Vipera_aspis |
| **US pit vipers (rattlesnakes etc.)** | "Before antivenom, estimates of mortality rates ranged from 5-25%" (secondary, page blocked; attributed to Dart 1992) | 7,000 to 8,000 venomous bites a year in the US, "about 5 people die" (about 0.07%, derived) | One clinical reference gives 0.28% with antivenom vs 2.6% without (search snippet, unverified) | https://www.cdc.gov/niosh/outdoor-workers/about/venomous-snakes.html ; http://emedicine.medscape.com/article/771455-overview |
| **Copperhead** | "Rarely fatal" | | | https://en.wikipedia.org/wiki/Eastern_copperhead |
| **Black mamba** | South Africa 1957 to 1979: 75 confirmed bites, 63 systemic, **21 died (28%)**. Before 1962 patients got an antivenom that did not cover mamba venom: **15 of 35 died (43%)** | With mamba-specific (1962) or fully polyvalent (1971) antivenom: **5 of 38 died (13%)**. Rural Zimbabwe 1991 to 1992: 2 of 15 died | Symptoms often within 10 minutes; collapse within 45 minutes; death typically 7 to 15 hours untreated. The often-repeated "100% untreated" is not supported by these data | https://en.wikipedia.org/wiki/Black_mamba (citing Christensen 1981, S Afr Med J) |
| **Common krait** | "Untreated mortality rate as high as 70–80%" (a clinical toxinology source; original study not named, secondhand) | | Painless night bites to people sleeping on the ground; death in about 4 to 8 hours untreated | https://en.wikipedia.org/wiki/Common_krait |
| **All snakes, world** | 5.4 million bites, 1.8 to 2.7 million envenomings, **81,410 to 137,880 deaths** a year, and about three times as many amputations and permanent disabilities | "High-quality snake antivenoms are the most effective treatment" | "Children often suffer more severe effects than adults, due to their smaller body mass." India: about 1.2 million deaths 2000 to 2019 | https://www.who.int/news-room/fact-sheets/detail/snakebite-envenoming |

### 3.3 Dry bites (a bite with no venom)

- Review: asymptomatic (grade 0) bites "account for about 50% of bites globally, and for up to 80% of bites for some snake species". Faulty venom delivery happens in "20–50% of bites". In the authors' compiled data, dry bites were about equally common for vipers (14.7%) and elapids (14.5%). Pucca et al. 2020, https://pmc.ncbi.nlm.nih.gov/articles/PMC7690386/
- Per-study rates from that review: *Vipera berus* 26% (Czech Republic); Pacific rattlesnakes 22%; US rattlesnakes 3 to 10% in several series; Malayan pit viper 50%; common krait 10 to 24%; Indian cobra 20%; Russell's vipers (two species) 2 to 38%.
- Why: "most dry bites result from a (deliberate) decision to conserve venom" (venom metering). Defensive strikes are drier than predatory ones. Adults are more selective than juveniles, which tend to empty their glands. Other causes: recently emptied glands, misjudged strikes, clothing.
- Czech adder registry, 191 bites: 26.7% dry, 47.6% local envenoming only, 25.7% systemic; 12.6% given antivenom; no deaths (https://pmr.lf1.cuni.cz/115/3/120/).
- Consequence: "survived a bite" and "survived envenoming" are different events. A quarter to a half of real survivors received no venom at all.

### 3.4 Time from bite to death or recovery

| Venom | Onset | Worst point | Death (if any) | Recovery | Source |
|---|---|---|---|---|---|
| Adder | Immediate severe pain; swelling and tingling within minutes (up to 30); systemic signs from 5 minutes or hours later | Whole limb swollen and bruised within 24 hours; untreated symptoms may persist or fluctuate for up to 48 hours. Early collapse "often resolves spontaneously"; persistent or recurrent shock is the severe sign | Rare | "Children recover quickly but adults may take weeks or months"; up to a year | https://en.wikipedia.org/wiki/Vipera_berus ; https://pubmed.ncbi.nlm.nih.gov/1276841/ |
| Black mamba | Often within 10 minutes | Collapse within 45 minutes | Typically 7 to 15 hours | | https://en.wikipedia.org/wiki/Black_mamba |
| Common krait | Facial muscles tighten within 1 to 2 hours | | About 4 to 8 hours untreated | | https://en.wikipedia.org/wiki/Common_krait |
| Funnel-web spider | 8 to 175 minutes (median 28) | | Deaths reported within an hour | 97% complete response to antivenom | https://www.mja.com.au/journal/2005/182/8/funnel-web-spider-bite-systematic-review-recorded-clinical-cases |
| Black widow type spiders | Intense local pain 5 to 10 minutes after the bite | Waxes and wanes over 1 to 4 days | Very rare | Typical duration 3 to 6 days | https://en.wikipedia.org/wiki/Latrodectism |

### 3.5 What first aid and treatment change

- **First aid (US CDC):** get to care fast, since antivenom may be needed; have someone else drive; sit or lie with the bite comfortable; remove rings and watches before swelling; wash and cover the bite; mark the edge of swelling and the time. Do not: handle the snake, wait for symptoms, use tourniquets, electric shock or folk remedies, cut the wound, suck out venom, apply ice, or drink alcohol or take aspirin or ibuprofen. https://www.cdc.gov/niosh/outdoor-workers/about/venomous-snakes.html
- **Pressure-immobilisation bandage** (Australian practice for elapids and funnel-webs): in the funnel-web series, the only two patients whose symptoms began after 2 hours both had one applied (MJA link above).
- **Antivenom** is the decisive treatment. Sweden's fall in adder deaths is credited to intensive care plus highly purified antivenoms used since 1995 (PMC6409582).
- **The wrong antivenom is no antivenom:** black mamba victims given an antivenom that did not cover mamba venom died at 43%, against 13% with the right one (3.2).
- **Treatment has its own risk:** antivenom can cause anaphylaxis or delayed serum sickness (https://en.wikipedia.org/wiki/Antivenom). 25% of envenomed Australian snake handlers given antivenom reacted to it, 3% severely (https://pubmed.ncbi.nlm.nih.gov/22875778/). About 4% had side effects in European adder treatment (PMC6409582).
- **For a pre-modern world:** the realistic levers are rest, keeping the bitten limb still, getting help, and supportive care. In Britain, Reid (1976) found "in most cases simple symptomatic treatment is enough", with antivenom kept for persistent shock.

### 3.6 Do repeated bites build tolerance, or dangerous allergy?

In reality, both happen. Which one depends on the venom, the dose, and above all how regular the exposure is.

**Allergy from repeated natural exposure (documented)**
- "Allergic reaction to snake venom after repeated bites of *Vipera aspis*", Clin Exp Allergy 1993, https://pubmed.ncbi.nlm.nih.gov/8472192/
- "Phospholipase A2 triggers anaphylaxis to snake venom by repeated skin sensitization", J Investig Allergol Clin Immunol 2021, https://pubmed.ncbi.nlm.nih.gov/32732180/
- Australian snake handlers (2004 to 2011): 106 bites; 8 systemic hypersensitivity reactions to venom, 5 meeting anaphylaxis criteria, 3 with low blood pressure; the authors note venom anaphylaxis had been poorly recognized. https://pubmed.ncbi.nlm.nih.gov/22875778/
- A handler sensitized by airborne spitting-cobra venom developed IgE to 9 snake venoms; the "life-threatening sensitivity" persisted "after a long period of avoidance". https://pubmed.ncbi.nlm.nih.gov/15945564/

**Protection after prior exposure (documented)**
- Mice: "a single prior exposure" to Russell's viper venom raised survival against a potentially lethal dose, through IgE. Starkl et al. 2016, https://pubmed.ncbi.nlm.nih.gov/26410782/
- Bees: professional beekeepers "receive hundreds of stings yearly without developing systemic reactions". Tolerant beekeepers stung 50 or more times a year had high venom-specific IgG4, IL-10 and regulatory T cells. https://pubmed.ncbi.nlm.nih.gov/34213416/
- Beekeepers' serum antibodies protected mice from lethal bee venom (Schumacher et al. 1996, https://pubmed.ncbi.nlm.nih.gov/8780460/).

**Frequency decides the direction (bees)**
- In a Chinese survey (2021 preprint, not peer reviewed), 34.3% of beekeepers had bee venom allergy. Odds rose with sting frequency for beekeepers: 6.7 times for 2 to 4 stings a year and 42 times for 5 or more, against 1 or fewer. Among people receiving live-bee acupuncture, frequent stinging lowered the odds: 0.17 times for 7 or more stings a week, against 2 or fewer. https://doi.org/10.21203/rs.3.rs-511045/v1
- FAO: "allergies to bee venom can be developed quickly even after long periods of use"; occasional exposure carries a risk of developing allergy. https://www.fao.org/4/w0076e/w0076e18.htm
- Venom immunotherapy (deliberate escalating doses) and natural beekeeper exposure both "induce immune tolerance" (2026 study, https://pubmed.ncbi.nlm.nih.gov/40444662/).

**Surviving a disease (the closest real analogue to "survive it, get tolerance")**
- Cholera: "People who survive an episode of cholera have long-lasting immunity for at least three years (the period tested)." https://en.wikipedia.org/wiki/Cholera
- Typhoid: 19th-century physicians noted that "persons who already had the disease seemed to be protected" (historical observation). https://en.wikipedia.org/wiki/Typhoid_fever

**Narrowness is real**
- Immunity is specific to the venom. Bill Haast built up immunity to cobra venoms, yet a common krait bite in 1954 "eventually did affect him", putting him in hospital for days (https://en.wikipedia.org/wiki/Bill_Haast).
- Antibodies from Tim Friede's blood neutralized elapid neurotoxins but give "no resistance to viper venom" (https://en.wikipedia.org/wiki/Tim_Friede).

### 3.7 The historic practice of deliberate small doses

- **Mithridatism**: "the practice of protecting oneself against a poison by gradually self-administering non-lethal amounts", named after Mithridates VI of Pontus. It is "not effective against all types of poison". "Immunity is generally only possible with biologically complex types which the immune system can respond to." It "can lead to the lethal accumulation of a poison". The liver can be conditioned against some small molecules (heavy drinkers) but not against cyanide. https://en.wikipedia.org/wiki/Mithridatism
- **Styrian arsenic eaters** (Tschudi, 1851): users started with "a small piece of the arsenic, about the size of a lentil" (under half a grain) several times a week and raised the dose as it lost its effect; one man worked up to four grains, enough to kill most people. Stopping reportedly caused anxiety and indigestion. Critics called the evidence hearsay. Roscoe (1862) confirmed a sample was white arsenic. Early 20th-century work suggested the coarse arsenic was "poorly absorbed". Whether the eaters were truly tolerant "remains unproven". Parascandola, Hektoen International, https://hekint.org/2017/01/30/the-arsenic-eaters-of-styria/
- **Antivenom horses**: in February 1894 Calmette, and independently Phisalix and Bertrand, announced serum treatment. Calmette went on to immunize horses with Indian cobra venom, producing the first commercial antivenom. Host animals include horses, donkeys, goats, sheep, rabbits, chickens, llamas and camels. The principle is hyperimmunizing a host animal and transfusing its serum. https://en.wikipedia.org/wiki/Antivenom
- **Self-immunizers**:
  - Bill Haast injected himself "with gradually increasing quantities of venom", starting soon after he opened his serpentarium at the end of 1947. He had been "bitten 172 times by mid-2008" and died aged 100 in 2011. His donated blood treated snakebite victims ("more than twenty of those individuals recovered"). A 2003 Malayan pit viper bite cost him a finger. https://en.wikipedia.org/wiki/Bill_Haast
  - Tim Friede made over 800 self-injections and took about 200 bites in 18 years. A 2001 double cobra bite left him in a coma for four days. https://en.wikipedia.org/wiki/Tim_Friede
  - Two broadly neutralizing antibodies from Friede's B cells, plus the drug varespladib, rescued animals from whole-venom challenge by all 19 elapids in a WHO Category 1 and 2 set, with complete protection against most. Glanville et al. 2025, Cell, https://pubmed.ncbi.nlm.nih.gov/40318633/

### 3.8 Animals that resist venom: genetic, not earned

- **Mongoose**: its acetylcholine receptor "does not bind α-bungarotoxin" (Barchan et al. 1992, PNAS, https://pmc.ncbi.nlm.nih.gov/articles/PMC49782/).
- **Hedgehog**: blood macroglobulins neutralize adder venom's haemorrhagic activity (https://pubmed.ncbi.nlm.nih.gov/3590212/).
- **Opossums**: serum factors neutralize pit viper venom (https://pubmed.ncbi.nlm.nih.gov/8186456/).
- **California ground squirrels**: resistance varies between populations, and local rattlesnake venom is adapted to overcome local squirrel resistance; "rattlesnakes are evolutionarily ahead of their squirrel prey" (Holding et al. 2016, https://pmc.ncbi.nlm.nih.gov/articles/PMC4855376/).
- In every case, a species or population evolved the resistance; no individual animal acquired it by being bitten.

### 3.9 Other venoms and poisons a forager could meet

| Hazard | Where and when | Numbers | Course | Tolerance in reality? | Source |
|---|---|---|---|---|---|
| **Honey bee stings** | Hives, wild colonies; summer (unverified) | Adult LD50 2.8 mg venom per kg; 0.15 to 0.3 mg per full sac; "600 stings could well be lethal" for a 60 kg adult, about 90 for a 10 kg child. A 10 kg child survived about 10 stings per kg of Africanized bees with intensive care | Massive stinging kills by toxicity (kidney failure, muscle breakdown) within about 72 hours (case report); most sting deaths are allergic, not dose-related (unverified here) | Yes for regular heavy exposure (beekeepers); allergy for irregular exposure (3.6) | https://www.fao.org/4/w0076e/w0076e18.htm ; https://pmc.ncbi.nlm.nih.gov/articles/PMC8274777/ |
| **Wasp and hornet stings** | Hornets nest in hollow trees; wasps around food in late summer (unverified) | England and Wales 1950 to 1972: 61 bee or wasp sting deaths against 1 adder death | Hornets sting "in response to being stepped on or grabbed" and defend the nest | As bees (venom immunotherapy) | https://pubmed.ncbi.nlm.nih.gov/1276841/ ; https://en.wikipedia.org/wiki/European_hornet |
| **Spiders (Europe)** | European black widow "associated with fields, and humans come in contact only during harvest"; 1950s about 3 bites a year, epidemics up to 180 | "Death from latrodectism is rare"; series of 11 and 60 patients had no deaths; no US deaths for decades | Pain within 5 to 10 minutes, typically 3 to 6 days | None known (unverified) | https://en.wikipedia.org/wiki/Latrodectism |
| **Funnel-web spiders (Australia)** | | 198 possible bites, 77 severe; 13 deaths, all before the 1981 antivenom, 7 of them children; none confirmed since | Deaths within an hour | | https://www.mja.com.au/journal/2005/182/8/funnel-web-spider-bite-systematic-review-recorded-clinical-cases |
| **Scorpions** | Seven high-risk zones (north-Saharan Africa, Sahel, South Africa, Near and Middle East, South India, Mexico, South America east of the Andes); 2.3 billion people at risk | Over 1.2 million stings and over 3,250 deaths a year (0.27%) | Children more severe; antivenom works best given early, and delay past 1 hour raised risk in a Moroccan series | | https://pubmed.ncbi.nlm.nih.gov/18579104/ ; https://pubmed.ncbi.nlm.nih.gov/10813485/ |
| **Weever fish** | Buries in sand in shallows by day | | Severe pain that eases "considerably within a few hours even if untreated"; "not generally dangerous" | | https://en.wikipedia.org/wiki/Weever |
| **Toads, fire salamanders** | Ponds, damp woods | | Bufotoxin from toad skin glands; samandarin causes convulsions | | https://en.wikipedia.org/wiki/Common_toad ; https://en.wikipedia.org/wiki/Fire_salamander |
| **Water hemlock** (*Cicuta*) | Wet meadows, streambanks, marshes; mistaken for wild parsnip, wild celery or wild carrot | "One of North America's most toxic plants"; exact human toxic dose unknown, "very small amounts may lead to death" | Seizures from about 15 minutes; deaths "within a few hours"; livestock die in 15 minutes | No | https://en.wikipedia.org/wiki/Cicuta |
| **Hemlock, yew, monkshood, deadly nightshade** | Hedges, woods, damp ground (unverified detail) | Hemlock killed Socrates; all of yew except the red aril is poisonous; aconite was used to poison wolves ("wolfsbane"); belladonna berries contain atropine and scopolamine | Delirium and hallucinations (belladonna) | No (unverified) | https://en.wikipedia.org/wiki/Conium_maculatum ; https://en.wikipedia.org/wiki/Taxus_baccata ; https://en.wikipedia.org/wiki/Aconitum ; https://en.wikipedia.org/wiki/Atropa_bella-donna |
| **Death cap mushroom** | Under oaks, also beech and others; summer and autumn; resembles edible species | About 90% of mushroom deaths. Mortality about 60 to 70% until the mid-20th century; 22.4% in a 1971 to 1980 European review; about 10 to 15% by 1995 (other estimate 10 to 30%). Half a cap (30 g) can kill | Symptoms at 6 to 12 hours; apparent recovery for up to 72 hours; liver and kidney failure from day 3 to 6; death at 6 to 16 days | No (unverified) | https://en.wikipedia.org/wiki/Amanita_phalloides |
| **Ergot (on rye)** | Bread from contaminated rye; cold, damp years and poor storage (weakly sourced) | Sologne, France, 1778: over 8,000 deaths; Pont-Saint-Esprit 1951: about 250 affected, 7 deaths | Convulsive and gangrenous forms ("St Anthony's fire"); outbreaks read as divine punishment or witchcraft | No | https://en.wikipedia.org/wiki/Ergotism |
| **Shellfish poisoning (PSP)** | Mussels and clams after algal blooms; toxin kept for weeks after a bloom, up to 2 years in butter clams | No case-fatality figure on the page | Onset 10 to 30 minutes; tingling, then paralysis; "no antidote"; ventilate until it passes; cooking does not destroy it | No | https://en.wikipedia.org/wiki/Paralytic_shellfish_poisoning |
| **Bad water: cholera** | Contaminated water and food | Untreated 50 to 60% mortality; under 1% with proper care; 1.3 to 4.0 million cases and 21,000 to 143,000 deaths a year | Symptoms 12 hours to 5 days after infection; most infected show none; can kill "within hours" | Survivors immune for at least 3 years | https://www.who.int/news-room/fact-sheets/detail/cholera ; https://en.wikipedia.org/wiki/Cholera |
| **Bad water: typhoid** | Contaminated water and food | Untreated up to 20%; treated 1 to 4%; 9 million cases and 110,000 deaths a year | Begins 6 to 30 days after exposure; weeks of illness; healthy carriers exist | Survivors seemed protected (historical) | https://www.who.int/news-room/fact-sheets/detail/typhoid ; https://en.wikipedia.org/wiki/Typhoid_fever |
| **Rabies** (bite of a rabid fox, wolf, dog or bat) | | "Virtually 100% fatal" once symptoms appear; 44,203 deaths a year (2021 estimate); dogs cause 99% of human cases | Incubation typically 2 to 3 months (1 week to 1 year); wash the wound for 15 minutes; post-exposure treatment prevents death | Vaccine only | https://www.who.int/news-room/fact-sheets/detail/rabies |
| **Ticks** | Long grass and woodland; nymphs peak late spring to early summer | | Lyme disease, tick-borne encephalitis | | https://en.wikipedia.org/wiki/Ixodes_ricinus |
| **Marsh malaria ("ague")** | English marshes, 1564 to the 1730s and later | "An important cause of illness and death" | | Partial immunity in endemic adults (unverified) | https://pmc.ncbi.nlm.nih.gov/articles/PMC2627969/ |

### 3.10 The founder's numbers next to reality (comparison only, not a recommendation)

- **A 20% survival baseline (80% death) is deadlier than any real temperate snake.** Untreated death is under 1% for the adder (14 British deaths in 100 years), about 4% for the asp viper, and 5 to 25% for pre-antivenom rattlesnakes. Only the worst elapids come near 80%: the common krait's secondhand 70 to 80%. Black mamba victims given useless antivenom died at 43%.
- **Real survival is several separable variables.** Dry-bite chance (15 to 50%); the victim's body mass (children are worse); species and dose; time to care; first aid; the right antivenom. Each maps to a different system: body or species, healer, travel distance, knowledge.
- **Narrowness is the strongest-supported part of the brief.** Real immunity is specific to the venom (Haast and the krait; Friede's antibodies and vipers; mamba antivenom coverage).
- **Repeated exposure is double-edged.** Irregular repeated stings and bites are documented to cause dangerous allergy, while frequent regular exposure (beekeepers stung 50 or more times a year) produces tolerance. A model in which every survival only ever raises tolerance has no real-world counterpart for snakes.
- **Over-cap ("125%") has a close analogue in Path of Exile.** There, resistance above the cap is a buffer against effects that lower resistance (curses, exposure) but does nothing against penetration (section 4.3). The designers will need to say which kind of "reduction" the overflow absorbs.

---

## 4. Prior art from games (mechanics only)

### 4.1 RimWorld

**Placing animals by biome**
- Each biome lists wild animals with a commonality weight and has an overall animal density.
- Temperate forest: density 3.7. Commonality: hare 1, rat 1, squirrel 1; deer 0.5, wild boar 0.5, ibex 0.5, turkey 0.5, raccoon 0.5, muffalo 0.5, alpaca 0.5; guinea pig 0.45; boomalope 0.4, boomrat 0.4; gazelle 0.3, tortoise 0.3; horse 0.2, donkey 0.2, megasloth 0.2; rhinoceros 0.1; bison, cougar, grizzly bear, lynx, red fox, timber wolf and warg 0.07; yak 0.06. DLC adds badger 0.2, crow 0.5, swan 0.1, wolverine 0.07, and coastal-map-only otter, seal and crab at 0.1. https://rimworldwiki.com/wiki/Temperate_forest
- Wild animals "occasionally spawn at the map edges according to the biome". https://rimworldwiki.com/wiki/Animals
- The exact spawn formula linking density and commonality is not on the wiki pages read (unverified).
- "Predators are especially a problem in biomes with already little other wildlife." https://rimworldwiki.com/wiki/Biomes
- Each biome also has a disease rate (temperate forest: one disease per 50 days on average, weighted list). https://rimworldwiki.com/wiki/Temperate_forest

**Herds and migration**
- Animals carry a herd-animal flag ("some wild animals tend to stay together in a herd").
- The herd migration event spawns a herd of the larger of a random 3 to 5 or 4 divided by body size (rounded up). It is always a species that can spawn in the biome; the herd "won't attack unless provoked" and passes "quickly", so the window to hunt or tame is short. https://rimworldwiki.com/wiki/Events

**Predators**
- "Most carnivores are predators who will hunt smaller species."
- When hungry with no other food nearby they "hunt, kill and consume almost any animal smaller than them", including colonists and children.
- They keep attacking a downed victim, linger about a day near a kill, and avoid explosive animals.
- Source: https://rimworldwiki.com/wiki/Animals

**Revenge and manhunters**
- "Revenge chance on harm" is the chance an animal turns manhunter when harmed by the colony, "three times higher for close-ranged attacks". Examples: timber wolf 100% (and 30% on a failed taming), muffalo 10%, cobra 50% (10% on taming), deer, boar and horse 0%.
- When a pack animal is wounded in a hunt, "its full horde may turn hostile"; manhunter status "will disappear overnight".
- Manhunter packs are disease-maddened animals of a biome species, with 40% more points than a raid; they bash doors and stay 24 to 54 hours.
- "Mad animal" turns a single animal manhunter.
- Sources: https://rimworldwiki.com/wiki/Animals ; https://rimworldwiki.com/wiki/Events ; per-animal pages such as https://rimworldwiki.com/wiki/Timber_wolf

**Taming via wildness**
- "Tame chance = Animals * 2 * (1 - wildness) * Manipulation Offset * Talking Offset * Hearing Offset", where the Animals term is "4% plus 3% per skill level". At 0% wildness the chance doubles; at 100% taming is impossible. https://rimworldwiki.com/wiki/Tame_Animal_Chance
- Wildness: horse 0.35, boar 0.5, warg 0.6, deer 0.75, hare 0.75, cobra 0.75, grizzly 0.8, timber wolf 0.85. Wildness also sets the minimum handling skill (0.35 needs 3, 0.75 needs 8).
- Tamed status decays for animals above 10.1% wildness, over 12 down to 6 days, faster for wilder animals. https://rimworldwiki.com/wiki/Wildness
- Each attempt gives the handler 90 XP, so doing builds the skill.

**Hunting yields**
- Meat amount = 140 x body size, multiplied by the share of body parts remaining, x0.66 if not carefully slaughtered, and x0.4 at full malnutrition, then a post-process curve. A butcher spot works at 70% and a butcher table at 100%.
- Leather amount = 40 x body size.
- Deer (body size 1.2): 168 meat, 48 leather.
- Sources: https://rimworldwiki.com/wiki/Meat_Amount ; https://rimworldwiki.com/wiki/Leather_Amount ; https://rimworldwiki.com/wiki/Deer

**Venom as toxic buildup**
- A cobra's bite is a "toxic bite" of power 12, and cobras have 50% toxic resistance themselves. They do not use venom when hunting, so prey does not rot. https://rimworldwiki.com/wiki/Cobra
- Toxic buildup is a severity from 0 to 100% with stages: 4% (-5% consciousness), 20%, 40% (vomiting; permanent dementia possible), 60%, 80% (unconscious), 100% (death). It falls 8% a day once exposure stops.
- Toxic resistance cuts buildup by its percentage (humans 0%, animals 50%, insects and mechanoids immune) and stacks multiplicatively with environmental resistance. Direct attacks such as bites bypass environmental resistance. https://rimworldwiki.com/wiki/Ailments

**Disease as an immunity race (the closest existing "survive it, become immune" model)**
- Each day severity rises and immunity rises; the disease is beaten if immunity reaches 100% before severity does.
- Base rates per day: flu severity +0.249 against immunity +0.239; plague +0.666 against +0.522; malaria +0.370 against +0.314.
- Tending lowers severity by up to -0.077, -0.362 and -0.232 at 100% tend quality.
- Immunity gain speed depends on food, rest, bed quality and age (it declines past life expectancy), with ±20% random variation per disease.
- Source: https://rimworldwiki.com/wiki/Disease

### 4.2 Dwarf Fortress

**Creature biomes and spawning**
- A creature lists `[BIOME:...]` tokens. There are 48 biome types plus groupings such as `ANY_TEMPERATE_FOREST`, `ANY_TEMPERATE_WETLAND`, `ANY_RIVER`. https://dwarffortresswiki.org/index.php/DF2014:Biome_token
- `[FREQUENCY:n]` is a weight, default 50: with frequencies 10/25/50, the 50 "will appear approximately 58.8% of the time".
- `[POPULATION_NUMBER:min:max]` is the number present per world-map tile.
- `[CLUSTER_NUMBER:min:max]` is the group size; it also drives seasonal migrations of some fish in temperate ocean and river biomes.
- `[LARGE_ROAMING]` lets a creature spawn as wildlife.
- `[LARGE_PREDATOR]` "will attack things that are smaller than it"; only one large-predator group appears per map ("possibly two" on savage maps), and they appear in worldgen rampages and quests.
- `[BENIGN]` creatures run from anything not friendly.
- `[SAVAGE]`, `[GOOD]` and `[EVIL]` restrict a creature to those regions.
- Time-of-day tokens with clock windows (adventure mode): `DIURNAL` (6:00 to 20:00), `NOCTURNAL` (22:05 to 4:30), `CREPUSCULAR` (4:30 to 6:00 and 20:00 to 22:05), `MATUTINAL`, `VESPERTINE`, `ALL_ACTIVE`.
- Source for spawning tokens: https://dwarffortresswiki.org/index.php/DF2014:Creature_token

**The adder** (a v0.34-era raw reproduced on the wiki; only its birth tags were modified, https://dwarffortresswiki.org/index.php/Fixed_Creature_RAWs)
```
[FREQUENCY:30] [LARGE_ROAMING]
[BIOME:GRASSLAND_TEMPERATE] [BIOME:SAVANNA_TEMPERATE] [BIOME:SHRUBLAND_TEMPERATE]
[BIOME:ANY_TEMPERATE_FOREST] [BIOME:ANY_TEMPERATE_WETLAND]
[POPULATION_NUMBER:10:25] [CLUSTER_NUMBER:1:1] [PET_EXOTIC]
[SYNDROME][SYN_NAME:adder bite][SYN_AFFECTED_CLASS:GENERAL_POISON]
  [SYN_IMMUNE_CREATURE:ADDER:ALL][SYN_INJECTED]
  [CE_NAUSEA:SEV:50:PROB:100:RESISTABLE:SIZE_DILUTES:START:400:PEAK:500:END:1200]
  [CE_PAIN:SEV:75:PROB:100:RESISTABLE:SIZE_DILUTES:LOCALIZED:VASCULAR_ONLY:START:10:PEAK:50:END:2400]
  [CE_SWELLING:SEV:25:PROB:100:RESISTABLE:SIZE_DILUTES:LOCALIZED:VASCULAR_ONLY:START:50:PEAK:500:END:1500]
  [CE_BLISTERS:SEV:50:PROB:100:RESISTABLE:SIZE_DILUTES:LOCALIZED:VASCULAR_ONLY:START:50:PEAK:500:END:1500]
```
- A fortress-mode day is 1,200 ticks, so adder pain lasts 2 days. https://dwarffortresswiki.org/index.php/DF2014:Time
- The wiki's adder page: strong pain, then nausea, blisters and swelling; "not usually fatal", but pain can cause unconsciousness and so death in a fight. Adders can be caged and tamed as exotic pets. https://dwarffortresswiki.org/index.php/DF2014:Adder

**Syndromes: onset, duration, severity, resistance** (https://dwarffortresswiki.org/index.php/DF2014:Syndrome)
- **Transmission**: `SYN_CONTACT`, `SYN_INGESTED`, `SYN_INHALED`, `SYN_INJECTED`. Who is affected: `SYN_AFFECTED_CLASS` / `SYN_IMMUNE_CLASS` / `SYN_IMMUNE_CREATURE` (a snake is immune to its own venom).
- **Each effect** (`CE_PAIN`, `CE_PARALYSIS`, `CE_NECROSIS`...) has:
  - `SEV`: severity. Damage accrues every tick, so even minimal bruising destroys a part given enough time.
  - `PROB`: chance in percent that the effect manifests at all.
  - `START` / `PEAK` / `END`: ticks after exposure.
  - `ABRUPT`: begins at once instead of ramping up.
  - `LOCALIZED`: hits the bitten part.
  - `VASCULAR_ONLY` / `MUSCULAR_ONLY`: which tissue layers.
  - `DWF_STRETCH`: multiplies durations in fortress mode.
- **Resistance**: `RESISTABLE` lets the victim's disease-resistance attribute hinder the effect. The human median is 1000, and "a disease resistance of 5000 does not grant immunities" (https://dwarffortresswiki.org/index.php/DF2014:Attribute). `SIZE_DILUTES` and `SIZE_DELAYS` presumably scale severity and onset by body size against dose ("yet to be extensively tested").
- **Innate per-species resistance**: `SYN_IDENTIFIER` plus the creature token `SYNDROME_DILUTION_FACTOR`. Dwarves have `INEBRIATION:150`, which lowers alcohol severity; 100 is normal and higher dilutes more.
- **Repeated exposure stacks**: a creature holds only one active syndrome per identifier. A second exposure while it is active adds `SYN_CONCENTRATION_ADDED` instead. Alcohol adds 100 per drink up to 1000, "each 100 of <amount> will contribute SEV in general to each effect", and concentration never falls until the syndrome ends.

**Doing counts**: attributes rise through their associated skills ("shield use will raise endurance") and rust without use; gain and rust rates are moddable. https://dwarffortresswiki.org/index.php/DF2014:Attribute

### 4.3 Other games with a strong habitat, venom or tolerance model

- **Path of Exile, resistance caps and overcap.** Default maximum resistance is 75% (from search summaries of the wikis) and the hard cap is 90% (forum thread). On the official forum, players explain that resistance above the maximum is "lowered" first by effects such as elemental weakness, while penetration acts on the capped value: 95% uncapped, 90% maximum and 10% penetration gives 80% effective resistance; "penetration reduces your effective resistance, and does not affect the current and maximum". https://www.pathofexile.com/forum/view-thread/3571271 (community answers, not developer statements). The PoE and PoE2 wikis were blocked by bot protection this session, so caps and order of operations are not checked against a wiki. Another research step likely covers PoE in depth.
- **Skyrim, poison resistance with a hard cap.** Resist Poison 50% "will halve any poison damage". It "is capped at 85%; i.e., you cannot gain poison immunity". Wood elves and Redguards have an innate 50%; a perk gives 50%. https://en.uesp.net/wiki/Skyrim:Resist_Poison
- **Kenshi, toughness from taking damage.** Toughness experience scales with damage taken relative to the toughness level (search summary of https://kenshi.fandom.com/wiki/Toughness; page not fetched, unverified). The closest commercial example of "being hurt makes you harder to hurt".
- **The Long Dark, feats earned by doing.** Each feat unlocks a permanent perk:
  - Cold Fusion: spend 100 days outside; +2 °C core temperature.
  - Efficient Machine: survive 500 days; 10% fewer calories.
  - FireMaster: start 1,000 fires; begin with fire-starting level 3.
  - Snow Walker: travel 1,000 km.
  - A feat earned in one game can be used only in the next playthrough, and difficulty limits how many are active (5, 4, 3, 2). This is a precedent for know-how that carries across runs.
  - Source: https://www.thegamer.com/the-long-dark-difference-feats-challenges/
- **UnReal World, tracks.** Animals leave tracks; the tracking skill finds and follows existing tracks but cannot create them; wounded animals leave blood trails that are easier to follow. https://steamcommunity.com/app/351700/discussions/0/3939020962918603525 (forum, via search summary; unverified detail)

---

## 5. Traps and open questions for the designers

1. **The founder's 20% baseline is not a temperate number.** A real adder kills well under 1% of people it bites, even untreated. If the world uses 20%, it is a deliberately mythic snake (or untreated krait-class venom), and the game should say so in-world.
2. **Dry bites.** 15 to 50% of real bites inject no venom. If "survived a bite" grants a level, a dry bite would grant tolerance to a venom the body never met. Reality says no: there was nothing to respond to.
3. **Tolerance versus allergy.** Repeated natural snakebites are documented to cause anaphylaxis (handlers, a repeatedly bitten asp-viper victim). Tolerance in reality comes from regular deliberate dosing (antivenom horses, self-immunizers, venom immunotherapy) or very frequent stinging (beekeepers). The beekeeper data even shows a direction switch with frequency.
4. **Narrowness is real.** Immunity is specific to the venom; even one snake family does not protect against another (cobras versus krait, elapids versus vipers). This strongly supports the founder's "snake tolerance does nothing against a spider".
5. **Innate resistance is genetic, not earned.** Mongooses, hedgehogs, opossums and ground squirrels resist venom as species or populations. That fits the species and needs layer and the Truths (hybrids could inherit it), not the earned-ability layer.
6. **Surviving a disease gives time-limited protection** (cholera at least 3 years). That is the closest real analogue to "survive it and level up", and it fades.
7. **Over-cap needs a defined target.** In PoE, overflow absorbs curses and exposure but not penetration. "125% applies to any kind of abilities anyone else has that reduces tolerance" has to choose which of those two it is.
8. **Secondhand numbers.** The common krait's 70 to 80%, the rattlesnake's pre-antivenom 5 to 25%, the 0.28% vs 2.6% US figure and the 1957 Scandinavian 0.4% are repeated secondhand. The popular "black mamba 100% untreated" is contradicted by the South African series (28% overall, 43% with useless antivenom).
9. **Danger is situational and seasonal, not constant.** The peaks are:
   - Mothers with young: 47% of bear attacks; moose cows in late spring and summer.
   - The rut: boar Nov to Jan; moose late Sep to Oct; red deer late Sep to Nov.
   - Wounded animals in hunts.
   - Rabies: 78% of wolf-attack victims in 2002 to 2020.
   - Hymenoptera: in England, bees and wasps killed 61 people to the adder's 1 in 1950 to 1972.
10. **Map limits.** Heath and coast cannot be expressed with today's tiles. At 48 x 32, big-range animals should pass through rather than live on the map, as RimWorld's edge spawns and migrations do.
11. **Game numbers are balance, not ecology.** RimWorld wildness and revenge chances, and DF frequencies, are tuned for fun. The DF adder raw is an older-version copy with modified birth tags.
12. **Several card details are marked unverified.** These are mostly signs, some seasonal dates and historical uses. Check them before they become content.
