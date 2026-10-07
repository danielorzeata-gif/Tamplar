# HARTA ROMANIEI — specii, imbinari si acoperisuri in arhitectura populara de lemn (ETNOMON)

Iteratia 6 (2026-10-05).

## Sursa si licenta
**Date**: ETNOMON — *Muzeul virtual al monumentelor etnografice in aer liber din Romania*, baza de date intretinuta si administrata de **Institutul National al Patrimoniului** (CIMEC), https://monumente-etnografice.cimec.ro — licenta **CC BY-SA 4.0** (https://creativecommons.org/licenses/by-sa/4.0/deed.ro). Ultima actualizare a bazei la momentul descarcarii: 12.05.2026.
**Ce am facut**: am descarcat cu un crawler politicos (o cerere la ~0,6 s) **1.662 din 1.663 de fise** (o fisa nu a raspuns), am pastrat cele **1.047 de constructii**, am extras campurile structurate (pereti: MATERIAL / TEHNICA / IMBINARE; temelie; sarpanta; invelitoare; zona etnografica; judet de provenienta) si le-am clasificat automat (scripturi in `99_REFERENCE/sources/etnomon/`).
**Licenta acestei fise**: fiind o lucrare derivata din date CC BY-SA 4.0, **aceasta fisa si setul de date derivat `99_REFERENCE/sources/etnomon/ETNOMON_constructii_clasificate.json` sunt distribuite tot sub CC BY-SA 4.0**, cu atribuire: „Date: ETNOMON, Institutul National al Patrimoniului, CC BY-SA 4.0; prelucrare: Wood Knowledge Base".
Nivel de incredere: `[V-DATA]` — numaratori din date reale, cu limitele de mai jos.

## Limite (citeste inainte de a folosi cifrele)
1. **Esantion muzeal, nu recensamant**: ASTRA Sibiu (305 fise) si Muzeul Satului Bucuresti (236) domina; Maramures e supra-reprezentat (208 constructii). Cifrele arata **ce s-a pastrat in muzee**, nu frecventa reala in sate.
2. **Fisele sunt completate de autori diferiti** (1998–2020+), cu vocabular diferit: 261 de constructii spun doar "cununi orizontale / blockbau" fara tipul de colt.
3. Campul IMBINARE de la sarpanta si invelitoare **repeta de obicei valoarea de la pereti** (artefact de completare) — am folosit doar campul peretilor.
4. Clasificarea e automata (cuvinte-cheie); categoriile rare (< 5 fise) sunt orientative.

## Rezultate principale `[V-DATA]`
**ETN-001 — Specii in pereti**: brad 567 fise, stejar 415, fag 96, "rasinoase" nespecificat 44, gorun 22, salcam 16, molid 11, carpen 8, salcie 8 (o fisa poate avea mai multe specii). **Bradul si stejarul acopera marea majoritate**; molidul apare rar ca denumire (posibil inclus sub "brad"/"rasinoase" in limbajul fiselor — vezi C-17).
**ETN-002 — Talpile** (unde sunt mentionate): stejar 45, brad 6, gorun 4, fag 3, frasin 2 → **talpa e aproape intotdeauna din stejar**, chiar cand peretii sunt din brad. Confirma RO-1 (Bartha 2019) si regula generala: elementul in contact cu temelia/umezeala = specia cea mai durabila.
**ETN-003 — Barne**: cioplite in 4 fete 212, cioplite nespecificat 162, rotunde 83, cioplite in 2 fete 67.
**ETN-004 — Acoperis**: in 4 ape 656 (≈ 71 % din cele cu tip cunoscut), in 2 ape 237, intr-o apa 23, in 3 ape 9. Exceptii regionale cu 2 ape dominant: Marginimea Sibiului, Banat (Timis, Caras-Severin), Dobrogea (Tulcea), Arad.
**ETN-005 — Invelitoare**: sindrila 235, draniţa 229, sita 229, paie 97, tigla 70, stuf 28. Regional: **dranita** — Maramures, Bucovina; **sita** — Gorj, Valcea, Arges, Muscel; **sindrila** — Vrancea, Bran, Tinutul Secuiesc, Valcea; **paie** — Zarand, Lapus, Chioar, Alba; **stuf** — Dobrogea; **tigla** — Banat, Marginimea Sibiului.

**ETN-006 — Tipuri de imbinari si unde apar** (din tabelul "Pe tip de imbinare"):
| Tip (termen din fise) | Ce este `[ESTIMARE]` pe baza descrierilor | Unde (zone dominante) | Specii / barne |
|---|---|---|---|
| **cheotori drepte** (91) | colt cu crestatura dreapta, la barne cioplite; cel mai frecvent tip numit | Maramures, Gorj, Valcea, Alba, Salaj | brad, stejar; barne cioplite in 4 fete |
| **cheotori rotunde** (59) | crestatura rotunda la barne rotunde (saddle notch) | Zarand (Hunedoara), Tinutul Secuiesc, Marginimea Sibiului | **brad**; barne **rotunde** (asociere clara) |
| **cheotori in coada de randunica / nemtesti** (32) | colt trapezoidal | sudul Transilvaniei (Hunedoara, Sibiu, Alba), Gorj, Arad | brad, stejar |
| **muc** (25) | **cep** in stalp ("imbinare tip muc in orificiul sapat in stalpi"; "scheletul prins in mucuri") | Bucovina (Suceava), Maramures, Oas | brad; barne cioplite in 4 fete |
| **limba si uluc / nut si feder** (24) | lamba-uluc — **mai ales la montanti** ("montanti de legatura cu limba si uluc") | **Valcea** (18 din 24) | brad, stejar |
| **soşi / şeşi** (19) | **stalpi verticali intermediari, santuiti**, in care intra capetele barnelor scurte — "cununi de barne orizontale, cu soşi intermediari santuiti"; "soşi verticali la mijlocul laturii mari"; "lodbe prinse in soşi" | Campia Transilvaniei, Dealurile Clujului, Bistrita, Zarand, Arad, Chioar | **stejar** |
| **amnare** (17) | acelasi principiu, termenul bucovinean: "barne rotunde intre stalpi verticali; incheiere in amnar"; "imbinare in amnare a barnelor scurte"; "amnarii dispusi vertical" | **Bucovina** (Suceava: Radauti, Campulung Moldovenesc, Humor) | brad / rasinoase |
| **căţei** (13) | in Maramures folosit ca sinonim pentru soşi ("Căţei (Şosi verticali la mijlocul laturii mari)"); in Banat — tip de imbinare a barnelor de stejar ("cheotori in căţei") | **Banat** (Timis, Caras-Severin), Bihor, Mures | **stejar** |
| in crestez / crestat (5) | colt crestat | Tinutul Secuiesc, Arad | brad |
| in cleste (2) | — | Alba | — |
| stalpi / schelet (86) | sistem pe stalpi (Fachwerk / stalpi + umplutura de scanduri, nuiele, lut) | Marginimea Sibiului, Valcea, Dobrogea, Alba | stejar, brad, salcam |

**ETN-007 — Doua sisteme constructive coexista**: (a) **cununi orizontale imbinate la colturi** (blockbau) — 250 de fise; (b) **stalpi + umplutura** (scanduri, nuiele, lut, barne scurte in stalpi santuiti) — 204 + sistemele soşi/amnare/căţei. Confirma RO-4 (Bucovina: Blockbau si Fachwerk). Sistemul cu **stalpi santuiti si barne scurte** (soşi / amnare / căţei) permite pereti lungi din **barne scurte** — util cand lemnul lung lipseste (in special stejar).
**ETN-008 — Asocieri** (orientative):
- barne **rotunde** ↔ **cheotori rotunde** ↔ **brad** (zone montane si submontane: Secuime, Zarand, Marginime, Lapus);
- barne **cioplite in 4 fete** ↔ **cheotori drepte / coada de randunica / muc** (Maramures, Gorj, Bucovina);
- **stejar** ↔ sisteme cu **stalpi santuiti** (soşi, căţei) in zonele de campie/deal din vestul si centrul Transilvaniei si Banat.

## Tabele complete (generate automat)
Fise ETNOMON descarcate: **1662**; constructii analizate: **1047**.

### Totaluri

- **Imbinari pereti (lemn)**: cununi, imbinare de colt nespecificata 261, cheotori drepte 108, stalpi / schelet 86, cheotori rotunde 59, muc (cep in stalp) 35, cheotori in coada de randunica / nemtesti 32, limba si uluc / nut si feder 25, cuie 22, soşi (stalpi intermediari santuiti) 20, amnare (stalpi santuiti, Bucovina) 17, căţei 16, cheotori (tip nespecificat) 16, in crestez / crestat 5, cep 4
- **Specii pereti**: brad 567, stejar 415, fag 96, rasinoase (nespec.) 44, gorun 22, salcam 16, molid 11, carpen 8, salcie 8, anin/arin 6, frasin 5, plop 4, ulm 3, pin 1
- **Barne**: cioplite 4 fete 212, cioplite (nespec.) 162, rotunde 83, cioplite 2 fete 67
- **Specii talpi**: stejar 45, brad 6, gorun 4, fag 3, frasin 2
- **Acoperis**: 4 ape 656, 2 ape 237, ? 122, 1 apa 23, 3 ape 9
- **Invelitoare**: sindrila 235, dranit 229, sita 229, ? 123, paie 97, tigla 70, stuf 28, scandur 19, trestie 12, tabla 3, olane 2

### Pe judet de provenienta (judete cu ≥ 5 constructii)

| Grup | N | Specii pereti (nr. fise) | Imbinari pereti | Barne | Acoperis | Invelitoare |
|---|---|---|---|---|---|---|
| Maramureş | 208 | brad 118, stejar 108, fag 26, frasin 3 | cununi, imbinare de colt nespecificata 89, cheotori drepte 23, stalpi / schelet 6, cheotori rotunde 5 | cioplite 4 fete 53, cioplite (nespec.) 49, rotunde 28, cioplite 2 fete 15 | 4 ape 171, 2 ape 20, 1 apa 1, 3 ape 1 | dranit 107, sita 33, paie 26, sindrila 8 |
| Gorj | 88 | stejar 66, brad 59, fag 6, salcie 4 | cununi, imbinare de colt nespecificata 25, cheotori drepte 18, stalpi / schelet 5, cheotori in coada de randunica / nemtesti 3 | cioplite 4 fete 32, cioplite (nespec.) 9, rotunde 3, cioplite 2 fete 2 | 4 ape 75, 2 ape 11, 3 ape 1 | sita 53, sindrila 24, dranit 2, tigla 1 |
| Suceava | 76 | brad 40, rasinoase (nespec.) 29, plop 1, carpen 1 | cununi, imbinare de colt nespecificata 33, amnare (stalpi santuiti, Bucovina) 12, muc (cep in stalp) 12, cheotori rotunde 5 | cioplite 4 fete 37, rotunde 6, cioplite 2 fete 5, cioplite (nespec.) 4 | 4 ape 54, 2 ape 17, 1 apa 2 | dranit 61, sindrila 7, sita 4, scandur 1 |
| Vâlcea | 72 | brad 55, stejar 50, fag 8, plop 1 | limba si uluc / nut si feder 19, cheotori drepte 12, stalpi / schelet 8, cununi, imbinare de colt nespecificata 7 | cioplite 4 fete 19, cioplite (nespec.) 17, cioplite 2 fete 16, rotunde 2 | 4 ape 57, 2 ape 7, 1 apa 2, 3 ape 1 | sindrila 38, sita 28, scandur 1 |
| Sibiu | 58 | brad 42, stejar 17, fag 7, anin/arin 1 | stalpi / schelet 16, cheotori drepte 9, cheotori rotunde 8, cheotori in coada de randunica / nemtesti 5 | cioplite 4 fete 4, cioplite (nespec.) 4, rotunde 3, cioplite 2 fete 1 | 2 ape 42, 4 ape 12, 1 apa 3, 3 ape 1 | sita 22, tigla 21, sindrila 9, paie 4 |
| Hunedoara | 57 | brad 28, stejar 17, fag 13, gorun 7 | cununi, imbinare de colt nespecificata 15, cheotori rotunde 11, cheotori in coada de randunica / nemtesti 6, cheotori drepte 5 | cioplite (nespec.) 12, rotunde 8, cioplite 4 fete 7, cioplite 2 fete 3 | 4 ape 36, 2 ape 10, 1 apa 2 | paie 22, sita 10, sindrila 7, tigla 6 |
| Alba | 54 | brad 35, stejar 11, fag 8, gorun 2 | cheotori drepte 12, cununi, imbinare de colt nespecificata 12, stalpi / schelet 8, cheotori in coada de randunica / nemtesti 4 | cioplite (nespec.) 11, cioplite 4 fete 8, rotunde 4, cioplite 2 fete 1 | 4 ape 35, 2 ape 12, 1 apa 1 | paie 15, dranit 14, sita 11, sindrila 9 |
| Harghita | 46 | brad 27, stejar 4, molid 2 | cununi, imbinare de colt nespecificata 9, cheotori rotunde 8, stalpi / schelet 2, cheotori drepte 1 | rotunde 8, cioplite (nespec.) 6, cioplite 4 fete 1 | 4 ape 27, 2 ape 13 | sindrila 21, dranit 12, sita 4, scandur 1 |
| Vrancea | 34 | brad 12, stejar 8, anin/arin 1, fag 1 | cununi, imbinare de colt nespecificata 15, cheotori drepte 3, cheotori rotunde 2, cheotori (tip nespecificat) 1 | cioplite (nespec.) 5, rotunde 3, cioplite 4 fete 2 | 4 ape 31, 2 ape 3 | sindrila 30, sita 3, tabla 1 |
| Argeş | 33 | brad 19, stejar 13, fag 3, anin/arin 1 | cununi, imbinare de colt nespecificata 6, stalpi / schelet 5, cuie 2, cheotori drepte 2 | cioplite 4 fete 7, cioplite (nespec.) 6, cioplite 2 fete 1, rotunde 1 | 4 ape 23, 2 ape 6, 3 ape 1 | sita 18, sindrila 10, scandur 2 |
| Braşov | 27 | brad 12, rasinoase (nespec.) 11, molid 2, stejar 1 | cununi, imbinare de colt nespecificata 15, cheotori rotunde 3, cheotori drepte 3, cheotori (tip nespecificat) 2 | cioplite 2 fete 12, cioplite 4 fete 5 | 4 ape 21, 2 ape 6 | sindrila 15, sita 7, tigla 3 |
| Buzău | 26 | brad 14, stejar 5 | stalpi / schelet 4, cheotori in coada de randunica / nemtesti 2, cheotori rotunde 1, cununi, imbinare de colt nespecificata 1 | cioplite (nespec.) 4, rotunde 1 | 4 ape 5, 2 ape 5, 1 apa 3 | sindrila 9, sita 3, paie 2, tabla 1 |
| Timiş | 26 | brad 10, stejar 5, gorun 3, fag 1 | cuie 5, căţei 5, cheotori drepte 3, muc (cep in stalp) 2 | cioplite (nespec.) 5, rotunde 3, cioplite 4 fete 1 | 2 ape 14, 4 ape 4, 1 apa 3, 3 ape 2 | tigla 15, sindrila 5, stuf 1, scandur 1 |
| Tulcea | 24 | brad 12, stejar 5, salcam 4, frasin 1 | stalpi / schelet 7, cuie 1 | cioplite (nespec.) 2, cioplite 2 fete 1 | 2 ape 16, 4 ape 3, 1 apa 3 | stuf 16, scandur 3, sita 2, paie 1 |
| Cluj | 20 | stejar 14, brad 12, carpen 1, plop 1 | soşi (stalpi intermediari santuiti) 5, cununi, imbinare de colt nespecificata 4, muc (cep in stalp) 3, stalpi / schelet 2 | cioplite (nespec.) 6, cioplite 4 fete 4, cioplite 2 fete 3, rotunde 1 | 4 ape 14, 2 ape 2, 3 ape 1 | paie 6, trestie 6, sindrila 5, sita 2 |
| Arad | 17 | stejar 11, brad 2, plop 1, carpen 1 | soşi (stalpi intermediari santuiti) 3, cheotori in coada de randunica / nemtesti 3, cununi, imbinare de colt nespecificata 2, in crestez / crestat 2 | cioplite (nespec.) 3, cioplite 4 fete 3, rotunde 2, cioplite 2 fete 1 | 2 ape 11, 4 ape 2, 1 apa 1 | tigla 6, sita 4, stuf 3, sindrila 2 |
| Bihor | 16 | stejar 13, brad 11 | stalpi / schelet 1, cheotori in coada de randunica / nemtesti 1, cheotori drepte 1, soşi (stalpi intermediari santuiti) 1 | cioplite (nespec.) 4, cioplite 4 fete 3 | 4 ape 9, 2 ape 4 | sindrila 6, dranit 4, sita 2, tigla 2 |
| Neamţ | 14 | brad 10 | cununi, imbinare de colt nespecificata 6, stalpi / schelet 1 | cioplite (nespec.) 2, cioplite 4 fete 2, cioplite 2 fete 1 | 4 ape 6, 2 ape 3, 1 apa 1 | sindrila 5, dranit 4, sita 1 |
| Mureş | 14 | stejar 11, brad 10, fag 6, pin 1 | cheotori drepte 5, cheotori rotunde 2, stalpi / schelet 2, cheotori (tip nespecificat) 2 | cioplite (nespec.) 6, cioplite 2 fete 1 | 4 ape 9, 2 ape 5 | dranit 5, sita 4, tigla 2, paie 1 |
| Dolj | 14 | stejar 7, salcam 4 | cuie 2, stalpi / schelet 1, căţei 1 | cioplite 4 fete 1 | 4 ape 3, 2 ape 2 | paie 3, tigla 2, scandur 1, stuf 1 |
| Bistriţa Năsăud | 13 | brad 11, stejar 3, molid 1, frasin 1 | soşi (stalpi intermediari santuiti) 2, muc (cep in stalp) 2, căţei 2, cheotori (tip nespecificat) 2 | cioplite 4 fete 5, cioplite 2 fete 3, cioplite (nespec.) 1 | 4 ape 8, 2 ape 5 | dranit 8, sindrila 3, tigla 2 |
| Covasna | 12 | brad 6, stejar 1, mesteacan 1 | cununi, imbinare de colt nespecificata 2, in crestez / crestat 2 | rotunde 2 | 4 ape 4, 2 ape 1 | sindrila 6 |
| Satu Mare | 12 | stejar 10, fag 2 | cununi, imbinare de colt nespecificata 6, cheotori drepte 1, soşi (stalpi intermediari santuiti) 1, muc (cep in stalp) 1 | cioplite 4 fete 7, rotunde 2, cioplite 2 fete 1, cioplite (nespec.) 1 | 4 ape 10 | sindrila 4, paie 3, trestie 3 |
| Caraş Severin | 11 | stejar 6, brad 4, fag 2 | căţei 3, cheotori drepte 2, cununi, imbinare de colt nespecificata 2, cheotori (tip nespecificat) 1 | rotunde 3, cioplite (nespec.) 1, cioplite 4 fete 1 | 2 ape 7, 4 ape 3, 1 apa 1 | tigla 6, sindrila 2, dranit 1, sita 1 |
| Sălaj | 10 | gorun 5, stejar 3, fag 2, brad 1 | cheotori drepte 5, cununi, imbinare de colt nespecificata 2, stalpi / schelet 1, cheotori in coada de randunica / nemtesti 1 | cioplite (nespec.) 2, cioplite 4 fete 1, rotunde 1 | 4 ape 10 | paie 6, tigla 1, sita 1, tabla 1 |
| Prahova | 10 | brad 7, stejar 5, fag 4 | stalpi / schelet 2, cheotori rotunde 1, cununi, imbinare de colt nespecificata 1 | cioplite 4 fete 2, cioplite (nespec.) 1, rotunde 1 | 4 ape 7, 2 ape 1 | sindrila 5, sita 4, dranit 1 |
| Constanţa | 7 | stejar 3, fag 3, brad 2, salcam 2 | stalpi / schelet 2, cununi, imbinare de colt nespecificata 1 | — | 4 ape 2, 2 ape 1, 3 ape 1 | scandur 2, paie 2, stuf 1, olane 1 |
| Olt | 6 | stejar 6, salcam 2 | soşi (stalpi intermediari santuiti) 1, stalpi / schelet 1 | — | 4 ape 3, 2 ape 1 | sita 3, paie 1, scandur 1 |
| Vaslui | 6 | stejar 2, carpen 1, salcam 1 | stalpi / schelet 1, cununi, imbinare de colt nespecificata 1 | — | 4 ape 4 | stuf 4, paie 1 |
| Dâmboviţa | 5 | stejar 3, brad 2, fag 2 | stalpi / schelet 3, soşi (stalpi intermediari santuiti) 1 | — | 4 ape 3, 2 ape 1 | sita 4 |
| Ilfov | 5 | — | — | — | 2 ape 1 | sindrila 1 |

### Pe zona etnografica (zone cu ≥ 8 constructii)

| Grup | N | Specii pereti (nr. fise) | Imbinari pereti | Barne | Acoperis | Invelitoare |
|---|---|---|---|---|---|---|
| Maramureş | 160 | brad 109, stejar 81, fag 12, frasin 3 | cununi, imbinare de colt nespecificata 58, cheotori drepte 23, stalpi / schelet 6, limba si uluc / nut si feder 4 | cioplite (nespec.) 45, cioplite 4 fete 34, cioplite 2 fete 13, rotunde 9 | 4 ape 127, 2 ape 17, 3 ape 1 | dranit 97, sita 27, paie 4, sindrila 3 |
| Gorj | 86 | stejar 65, brad 59, fag 6, salcie 4 | cununi, imbinare de colt nespecificata 25, cheotori drepte 17, stalpi / schelet 5, cheotori in coada de randunica / nemtesti 3 | cioplite 4 fete 32, cioplite (nespec.) 9, rotunde 3, cioplite 2 fete 2 | 4 ape 72, 2 ape 11, 3 ape 1 | sita 52, sindrila 24, dranit 2, tigla 1 |
| Vâlcea | 68 | brad 53, stejar 46, fag 8, plop 1 | limba si uluc / nut si feder 17, cheotori drepte 12, stalpi / schelet 8, cununi, imbinare de colt nespecificata 5 | cioplite (nespec.) 17, cioplite 4 fete 15, cioplite 2 fete 14, rotunde 2 | 4 ape 53, 2 ape 7, 1 apa 2, 3 ape 1 | sindrila 38, sita 24, scandur 1 |
| Ţinutul Secuiesc | 51 | brad 28, stejar 4, molid 2, mesteacan 1 | cununi, imbinare de colt nespecificata 11, cheotori rotunde 4, in crestez / crestat 3, cuie 1 | rotunde 8, cioplite (nespec.) 6, cioplite 4 fete 1 | 4 ape 29, 2 ape 8 | sindrila 24, dranit 11, scandur 1 |
| Mărginimea Sibiului | 37 | brad 32, stejar 8, fag 3, anin/arin 1 | stalpi / schelet 10, cheotori rotunde 6, cheotori drepte 4, cheotori in coada de randunica / nemtesti 4 | cioplite 4 fete 3, rotunde 2, cioplite 2 fete 1, cioplite (nespec.) 1 | 2 ape 25, 4 ape 9, 1 apa 3 | sita 17, tigla 10, sindrila 9, scandur 1 |
| Munţii Apuseni | 29 | brad 24, fag 5, stejar 3, molid 1 | cununi, imbinare de colt nespecificata 11, cheotori drepte 4, stalpi / schelet 4, cuie 3 | cioplite (nespec.) 9, cioplite 4 fete 7, cioplite 2 fete 3, rotunde 3 | 4 ape 18, 2 ape 8, 3 ape 1, 1 apa 1 | sindrila 10, dranit 10, paie 8, sita 1 |
| Vrancea | 28 | brad 9, stejar 8, anin/arin 1 | cununi, imbinare de colt nespecificata 11, cheotori drepte 3, cheotori rotunde 2, cheotori (tip nespecificat) 1 | cioplite (nespec.) 4, rotunde 2, cioplite 4 fete 2 | 4 ape 25, 2 ape 3 | sindrila 24, sita 3, tabla 1 |
| Zarand | 21 | stejar 13, gorun 6, fag 4, brad 2 | cheotori rotunde 8, cununi, imbinare de colt nespecificata 4, cheotori in coada de randunica / nemtesti 2, soşi (stalpi intermediari santuiti) 2 | cioplite (nespec.) 6, rotunde 1, cioplite 4 fete 1 | 4 ape 13, 2 ape 5 | paie 13, tigla 4, sita 2, scandur 1 |
| Buzău | 21 | brad 10, stejar 4 | cheotori in coada de randunica / nemtesti 2, cheotori rotunde 1, cununi, imbinare de colt nespecificata 1, cuie 1 | cioplite (nespec.) 2, rotunde 1 | 4 ape 4, 2 ape 4, 1 apa 2 | sindrila 8, sita 3, tabla 1 |
| Bran | 19 | rasinoase (nespec.) 11, brad 6, molid 2, stejar 1 | cununi, imbinare de colt nespecificata 13, cheotori rotunde 3, cheotori drepte 2 | cioplite 2 fete 11, cioplite 4 fete 2 | 4 ape 16, 2 ape 3 | sindrila 14, sita 4, tigla 1 |
| Argeş | 18 | stejar 10, brad 9, fag 2, anin/arin 1 | stalpi / schelet 5, limba si uluc / nut si feder 1, cheotori drepte 1, cununi, imbinare de colt nespecificata 1 | cioplite 4 fete 5, cioplite (nespec.) 4, cioplite 2 fete 1, rotunde 1 | 4 ape 13, 2 ape 3 | sindrila 8, sita 7, scandur 2 |
| Dobrogea de nord | 17 | brad 8, stejar 4, salcam 3 | stalpi / schelet 6 | cioplite (nespec.) 2 | 2 ape 10, 1 apa 3, 4 ape 2 | stuf 9, scandur 3, sita 2, paie 1 |
| Ţara Haţegului | 16 | brad 11, fag 4, stejar 2, anin/arin 1 | cununi, imbinare de colt nespecificata 6, cheotori drepte 3, stalpi / schelet 2, cheotori in coada de randunica / nemtesti 2 | cioplite (nespec.) 3, cioplite 4 fete 2, rotunde 1, cioplite 2 fete 1 | 2 ape 7, 4 ape 7, 1 apa 1 | tigla 5, sindrila 3, paie 3, sita 2 |
| Lăpuş | 16 | stejar 7, fag 6, brad 2 | cununi, imbinare de colt nespecificata 5, cheotori rotunde 2 | rotunde 7, cioplite 4 fete 6, cioplite (nespec.) 1 | 4 ape 13, 2 ape 2, 1 apa 1 | paie 12, dranit 4 |
| Ţara Chioarului | 16 | stejar 14, brad 1, gorun 1, fag 1 | cununi, imbinare de colt nespecificata 10, soşi (stalpi intermediari santuiti) 2, cuie 1, muc (cep in stalp) 1 | cioplite 4 fete 6, rotunde 5, cioplite (nespec.) 1 | 4 ape 15 | paie 7, sita 4, trestie 1, dranit 1 |
| Banat | 16 | brad 10, stejar 1, fag 1 | cheotori drepte 3, cuie 2, cununi, imbinare de colt nespecificata 2, amnare (stalpi santuiti, Bucovina) 1 | cioplite (nespec.) 4, rotunde 2, cioplite 4 fete 1 | 2 ape 9, 1 apa 3, 3 ape 1, 4 ape 1 | tigla 9, scandur 2, sindrila 2 |
| Muscel | 15 | brad 10, stejar 3, salcam 1, rasinoase (nespec.) 1 | cununi, imbinare de colt nespecificata 5, cuie 2, amnare (stalpi santuiti, Bucovina) 2, cheotori (tip nespecificat) 1 | cioplite 4 fete 2, cioplite (nespec.) 2 | 4 ape 10, 2 ape 3, 3 ape 1 | sita 11, sindrila 2 |
| Câmpulung Moldovenesc | 15 | rasinoase (nespec.) 7, brad 7, molid 1 | cununi, imbinare de colt nespecificata 6, muc (cep in stalp) 3, amnare (stalpi santuiti, Bucovina) 2, cuie 2 | cioplite 4 fete 11 | 4 ape 13, 2 ape 2 | dranit 15 |
| Rădăuţi | 14 | brad 7, rasinoase (nespec.) 5, plop 1 | cununi, imbinare de colt nespecificata 7, cheotori rotunde 3, amnare (stalpi santuiti, Bucovina) 2, muc (cep in stalp) 1 | cioplite 4 fete 5, rotunde 2, cioplite 2 fete 1 | 4 ape 8, 2 ape 5 | dranit 11, sindrila 1, scandur 1 |
| Alba | 13 | brad 6, stejar 4, fag 3, gorun 2 | cheotori drepte 3, in cleste 2, stalpi / schelet 2, cheotori in coada de randunica / nemtesti 2 | cioplite (nespec.) 3, cioplite 4 fete 2 | 4 ape 12, 2 ape 1 | paie 6, dranit 4, sita 3 |
| Suceava | 13 | brad 9, rasinoase (nespec.) 1 | cununi, imbinare de colt nespecificata 5, amnare (stalpi santuiti, Bucovina) 2, muc (cep in stalp) 2 | cioplite 4 fete 4, cioplite (nespec.) 1, rotunde 1 | 4 ape 9, 1 apa 2, 2 ape 1 | dranit 11, sindrila 1 |
| Neamţ | 13 | brad 9 | cununi, imbinare de colt nespecificata 6 | cioplite (nespec.) 2, cioplite 4 fete 2, cioplite 2 fete 1 | 4 ape 6, 2 ape 2, 1 apa 1 | sindrila 4, dranit 4, sita 1 |
| Ţara Oaşului | 12 | stejar 10, fag 2, molid 1 | muc (cep in stalp) 4, cununi, imbinare de colt nespecificata 4, soşi (stalpi intermediari santuiti) 2, cheotori drepte 1 | cioplite 4 fete 6, rotunde 3, cioplite 2 fete 1, cioplite (nespec.) 1 | 4 ape 8 | sindrila 5, paie 4 |
| Valea Jiului | 11 | brad 7, fag 2, ulm 1 | cununi, imbinare de colt nespecificata 6, cheotori ciobanesti / stâneste 1 | rotunde 4, cioplite 2 fete 1, cioplite (nespec.) 1 | 4 ape 6, 2 ape 2, 1 apa 1 | sita 4, dranit 3 |
| Arad | 11 | stejar 6, brad 2, carpen 1, salcam 1 | cheotori in coada de randunica / nemtesti 3, in crestez / crestat 2, soşi (stalpi intermediari santuiti) 1 | cioplite 4 fete 3, cioplite (nespec.) 2, rotunde 1 | 2 ape 5, 4 ape 2, 1 apa 1 | sita 4, stuf 3, tigla 1, sindrila 1 |
| Romanaţi | 11 | stejar 8, salcam 6 | cuie 2, stalpi / schelet 2 | cioplite 4 fete 1 | 2 ape 2, 4 ape 1 | paie 3, scandur 2, stuf 1 |
| Sălaj | 10 | gorun 5, stejar 3, fag 2, brad 1 | cheotori drepte 5, cununi, imbinare de colt nespecificata 2, stalpi / schelet 1, cheotori in coada de randunica / nemtesti 1 | cioplite (nespec.) 2, cioplite 4 fete 1, rotunde 1 | 4 ape 10 | paie 6, tigla 1, sita 1, tabla 1 |
| Bihor | 10 | stejar 10, brad 8 | cheotori in coada de randunica / nemtesti 1, cheotori drepte 1, soşi (stalpi intermediari santuiti) 1, cununi, imbinare de colt nespecificata 1 | cioplite 4 fete 3, cioplite (nespec.) 2 | 4 ape 6, 2 ape 2 | dranit 4, sindrila 3, sita 2 |
| Bucovina | 9 | brad 8 | cununi, imbinare de colt nespecificata 3, cheotori drepte 2, cheotori rotunde 1, cheotori in coada de randunica / nemtesti 1 | cioplite 2 fete 2, rotunde 1, cioplite (nespec.) 1 | 4 ape 5, 2 ape 3 | dranit 3, sindrila 3, sita 2 |
| Ţara Oltului | 9 | brad 6, fag 4 | cheotori (tip nespecificat) 2, cheotori rotunde 2, cheotori drepte 1, stalpi / schelet 1 | cioplite 4 fete 1 | 2 ape 7, 4 ape 2 | tigla 5, sita 4 |

### Pe specie dominanta a peretilor → tip de imbinare si barne

| Grup | N | Imbinari | Barne | Judete |
|---|---|---|---|---|
| brad | 312 | cununi, imbinare de colt nespecificata 98, cheotori rotunde 31, cheotori drepte 27, stalpi / schelet 19 | cioplite (nespec.) 48, cioplite 4 fete 48, rotunde 31, cioplite 2 fete 20 | Maramureş 55, Suceava 39, Alba 29, Sibiu 27 |
| mixt | 299 | cununi, imbinare de colt nespecificata 65, cheotori drepte 43, stalpi / schelet 41, limba si uluc / nut si feder 18 | cioplite 4 fete 78, cioplite (nespec.) 64, cioplite 2 fete 25, rotunde 18 | Maramureş 70, Gorj 46, Vâlcea 44, Hunedoara 19 |
| stejar | 167 | cununi, imbinare de colt nespecificata 41, cheotori drepte 22, stalpi / schelet 14, soşi (stalpi intermediari santuiti) 11 | cioplite 4 fete 42, cioplite (nespec.) 41, rotunde 16, cioplite 2 fete 5 | Maramureş 50, Gorj 22, Vâlcea 9, Argeş 9 |
| rasinoase (nespec.) | 43 | cununi, imbinare de colt nespecificata 29, amnare (stalpi santuiti, Bucovina) 7, cuie 3, cheotori drepte 1 | cioplite 4 fete 26, cioplite 2 fete 12, rotunde 3 | Suceava 29, Braşov 10, ? 3, Argeş 1 |
| fag | 27 | cheotori drepte 6, cununi, imbinare de colt nespecificata 5, cheotori rotunde 3, stalpi / schelet 3 | rotunde 9, cioplite 4 fete 4, cioplite (nespec.) 2, cioplite 2 fete 2 | Maramureş 10, Alba 4, Sibiu 3, Hunedoara 2 |
| gorun | 16 | cheotori drepte 5, cununi, imbinare de colt nespecificata 4, cheotori rotunde 1, cheotori (tip nespecificat) 1 | cioplite 4 fete 2, rotunde 1 | Hunedoara 6, Sălaj 5, Timiş 2, Maramureş 1 |
| molid | 4 | cununi, imbinare de colt nespecificata 3 | rotunde 1, cioplite 2 fete 1, cioplite 4 fete 1 | Maramureş 2, Bistriţa Năsăud 1, Braşov 1 |
| salcam | 4 | căţei 1 | — | Tulcea 2, Dolj 1, Timiş 1 |

### Pe tip de imbinare → specii, barne, judete

| Grup | N | Specii | Barne | Judete | Zone |
|---|---|---|---|---|---|
| cheotori drepte | 91 | brad 55, stejar 51, fag 15, gorun 5 | cioplite 4 fete 18, cioplite (nespec.) 16, cioplite 2 fete 8, rotunde 6 | Maramureş 21, Gorj 17, Alba 10, Vâlcea 9 | Maramureş 21, Gorj 16, Vâlcea 9, Sălaj 4 |
| stalpi / schelet | 86 | stejar 53, brad 49, fag 11, salcam 4 | cioplite (nespec.) 14, cioplite 4 fete 8, cioplite 2 fete 2 | Sibiu 16, Vâlcea 8, Alba 8, Tulcea 7 | Mărginimea Sibiului 10, Vâlcea 8, Dobrogea de nord 6, Maramureş 6 |
| cheotori rotunde | 59 | brad 40, stejar 21, fag 10, gorun 2 | rotunde 15, cioplite (nespec.) 8, cioplite 4 fete 5, cioplite 2 fete 2 | Hunedoara 11, Harghita 8, Sibiu 8, Suceava 5 | Zarand 8, Mărginimea Sibiului 6, Ţinutul Secuiesc 4, Vâlcea 4 |
| cheotori in coada de randunica / nemtesti | 32 | brad 22, stejar 19, fag 6 | cioplite (nespec.) 10, cioplite 2 fete 4, cioplite 4 fete 4 | Hunedoara 6, Sibiu 5, Alba 4, Gorj 3 | Mărginimea Sibiului 4, Gorj 3, Arad 3, Maramureş 2 |
| muc (cep in stalp) | 25 | brad 19, stejar 8, molid 3, fag 1 | cioplite 4 fete 22, rotunde 2, cioplite (nespec.) 2, cioplite 2 fete 1 | Suceava 10, Maramureş 5, Vâlcea 2, ? 1 | Maramureş 4, zona etnografică Humor 3, Câmpulung Moldovenesc 3, Ţara Oaşului 2 |
| limba si uluc / nut si feder | 24 | brad 20, stejar 18, fag 5, anin/arin 1 | cioplite 4 fete 9, cioplite 2 fete 9, cioplite (nespec.) 7, rotunde 1 | Vâlcea 18, Maramureş 4, Sibiu 1, Argeş 1 | Vâlcea 16, Maramureş 4, Mărginimea Sibiului 1, Argeş 1 |
| soşi (stalpi intermediari santuiti) | 19 | stejar 16, brad 7, plop 1 | cioplite 4 fete 7, cioplite (nespec.) 3, cioplite 2 fete 2, rotunde 1 | Cluj 5, Maramureş 4, Arad 3, Bistriţa Năsăud 2 | Câmpia Transilvaniei 3, Zarand 2, Bistriţa 2, Dealurile Clujului 2 |
| amnare (stalpi santuiti, Bucovina) | 17 | brad 9, rasinoase (nespec.) 7, plop 1, fag 1 | cioplite 4 fete 7, rotunde 2 | Suceava 12, Argeş 2, Caraş Severin 1, Vâlcea 1 | Rădăuţi 2, Suceava 2, zona etnografică Câmpulung Moldovenesc 2, zona etnografică Rădăuţi 2 |
| căţei | 13 | stejar 8, brad 3, fag 1, pin 1 | cioplite 4 fete 2, cioplite 2 fete 1, rotunde 1, cioplite (nespec.) 1 | Timiş 5, Caraş Severin 3, Mureş 1, Bihor 1 | Caraş 3, Câmpia Timişului 3, Târnava Mare 1, Crişul Negru 1 |
| cheotori (tip nespecificat) | 12 | brad 10, stejar 2, gorun 1, frasin 1 | cioplite 2 fete 2, cioplite 4 fete 1, cioplite (nespec.) 1, rotunde 1 | Braşov 2, Sibiu 2, Mureş 2, Hunedoara 1 | Ţara Oltului 2, Bran (Branul de Jos) 1, Zarand 1, Muscel 1 |
| in crestez / crestat | 5 | brad 3, stejar 2, mesteacan 1 | cioplite 4 fete 3, rotunde 1 | Covasna 2, Arad 2, Harghita 1 | Ţinutul Secuiesc 3, Arad 2 |
