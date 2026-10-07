# FIȘA DORMITOR — set din lemn masiv, 3 niveluri de material (PREMIUM / STANDARD / ESENȚIAL)

- **Iterația 11 (2026-10-07)** · sursa cifrelor: `FURNITURE_CONFIGURATOR` (configurații reale generate, nu estimări de mână)
- **Scop:** toate piesele dormitorului, detaliate până la nivel de piesă debitată, plus regula după care, în variantele mai ieftine, **înlocuim lemnul doar acolo unde nu se vede**, fără să stricăm rezistența, mișcarea lemnului sau onestitatea față de client.
- **Legături:** [FURNITURE_BED_SOLID_WOOD](beds/FURNITURE_BED_SOLID_WOOD.md) · [FURNITURE_FRONTS_DOORS](cabinets/FURNITURE_FRONTS_DOORS.md) · [FURNITURE_CASEWORK_DRAWERS](cabinets/FURNITURE_CASEWORK_DRAWERS.md) · [WOOD_SPECIES_DIN68364_LWF](../05_WOOD_SELECTION/european_species/WOOD_SPECIES_DIN68364_LWF.md) · [JOINT_STRENGTH_DATA_2015_2026](../13_DESIGN_AND_ENGINEERING/structural_logic/JOINT_STRENGTH_DATA_2015_2026.md) · [STANDARDS_REGULATIONS_2026](../99_REFERENCE/sources/STANDARDS_REGULATIONS_2026.md)
- **Etichete:** `[V-TEXT]` sursă oficială citită · `[V-STD]` standard (previzualizare) · `[V-DATA]` tabel oficial / calcul din configurator · `[REF]` secundar · `[PRACTICA]` regulă de atelier · `[ESTIMARE]` propunere de-a noastră.
- **Prețurile** din `db/pricing.json` și `db/materials.json` sunt **exemple** (stejar 6 500 RON/m³, frasin 5 000, pin 2 200; SWP stejar 520 RON/m², frasin 430, molid 190). Procentele de economie de mai jos se schimbă când pui prețurile tale; metoda rămâne.

---

## 1. Răspunsul scurt: e OK pentru lux?

**Da, cu două condiții.** Lemnul secundar în părțile ascunse (laterale de sertar, funduri, șipci, grinzi interioare) este o practică veche a ebenisteriei bune, nu un semn de produs ieftin `[PRACTICA]`. Ce strică imaginea de lux nu e lemnul secundar, ci:
1. **ascunderea lui** — clientul trebuie să afle din ofertă ce specie are fiecare clasă de piese (vezi regula MIX-011);
2. **un lemn secundar care arată ieftin acolo unde se vede la deschidere** — pinul în sertare citește „buget”; frasinul lângă stejar citește „atelier bun” (același lemn cu pori inelari, deschis la culoare) `[PRACTICA]`.

De aceea propunerea este:

| Nivel | Ce vede clientul închis (clasa A) | Ce vede la deschidere (clasa B) | Ce nu vede (clasa C) | Poziționare |
|---|---|---|---|---|
| **PREMIUM — „stejar integral”** | stejar | stejar | stejar | semnătura casei: „stejar masiv peste tot, inclusiv sertarele” |
| **STANDARD — „stejar, interior frasin”** | stejar | **frasin** | pin | lux onest, cel mai bun raport preț/aspect |
| **ESENȚIAL — „stejar la vedere”** | stejar | pin / SWP molid | pin | intrare în gamă; se declară explicit |

**Cât se economisește realmente** (set: pat King + 2 noptiere + comodă 1800, prețuri exemplu) `[V-DATA]` calculat din configurator:

| Nivel | Material | Cost producție (cu regie 10 %) | Preț vânzare (adaos 35 %, TVA 21 %) | față de PREMIUM |
|---|---|---|---|---|
| PREMIUM | 6 192 RON | 13 223 RON | 21 600 RON | — |
| STANDARD | 5 325 RON | 12 269 RON | 20 042 RON | **−7,2 %** |
| ESENȚIAL | 4 125 RON | 10 949 RON | 17 885 RON | **−17,2 %** |

**Concluzia importantă:** materialul e doar ~45 % din costul de producție; manopera, prelucrarea, finisajul și **feroneria** sunt restul. La comodă, cele 8 perechi de glisiere ascunse costă 760 RON — mai mult decât toată economia STANDARD pe comodă (333 RON). Lemnul secundar e o pârghie reală, dar nu singura (vezi §7).

---

## 2. Clasele de vizibilitate (regula de bază)

| Clasă | Definiție | Piese în dormitor | Regulă |
|---|---|---|---|
| **A — exterior** | se vede cu mobila închisă, se atinge zilnic | fronturi, capac/blat, laterale exterioare de corp, picioare, tăblie (panou + ramă), lonjeroni, traverse de capăt ale patului, profile de mâner | **mereu specia de bază (stejar) în toate nivelurile** (MIX-001) |
| **B — interior vizibil** | se vede doar la deschidere | cutii de sertar (laterale, față, spate), polițe, separatoare, fundul corpului, polița nișei | specie secundară „nobilă” (frasin) în STANDARD; ieftină (pin/molid) în ESENȚIAL |
| **C — ascuns** | nu se vede deloc în folosire | șipci de somieră, grindă centrală, picior central, rigle de sprijin, spate, funduri de sertar | cea mai ieftină specie care trece verificarea de rezistență |

Clasificarea se face pe piesă, automat, în scriptul `dormitor/calc.py` (aceleași nume de piese ca în configurator).

---

## 3. Regulile de îmbinare a speciilor (MIX) — cu formule

| ID | Regulă | Formulă / prag | Etichetă |
|---|---|---|---|
| MIX-001 | Clasa A rămâne stejar în toate nivelurile; nu se înlocuiește nimic din ce se vede închis. | — | `[PRACTICA]` |
| MIX-002 | **Nu se amestecă specii în aceeași față vizibilă** (panou încleiat, blat, tăblie). | — | `[PRACTICA]` |
| MIX-003 | Două specii încleiate pe fibră lungă (ex. ramă stejar + lamelă frasin) trebuie să se miște asemănător. | Δ(contragere diferențială tangențială) ≤ 0,05 %/% `[ESTIMARE]`. Valori DIN 68100 `[V-DATA]`: stejar 0,36 · pin 0,36 · frasin 0,38 · molid 0,39 · fag 0,41 · nuc 0,29 · cireș 0,26–0,30 → **stejar+pin, stejar+frasin, stejar+molid: OK; stejar+fag: la limită; stejar+cireș: evită încleierea pe lățime** |
| MIX-004 | Piesa portantă înlocuită se **redimensionează** cu proprietățile noii specii (configuratorul o face singur la `select_beam`). | rezistență: h₂ = h₁·√(f_m1 / f_m2) · rigiditate: h₂ = h₁·(E₁/E₂)^(1/3). Cu valorile din bază (stejar D30: f_m 30, E 11 000; pin C24: f_m 24, E 11 000): h × 1,118 la rezistență, × 1,0 la săgeată. **Verificare reală:** patul King generat integral din pin păstrează aceleași secțiuni (șipci 28×70, grindă 45×70) — trec și în pin `[V-DATA]` | formule = mecanică elementară; clase EN 338 `[V-STD]` |
| MIX-005 | Îmbinarea **ia rezistența lemnului mai slab** din pereche (scobitura în lemnul moale e veriga slabă). | Kasal 2015: M ∝ S^0,42 (S = forfecare ∥ fibră). Pin (6,2 MPa) vs fag (10,3): (6,2/10,3)^0,42 = **0,81**; pin vs stejar (13,8, proxy WH): (6,2/13,8)^0,42 = **0,71 → −29 %** `[V-TEXT]` + calcul | `[V-TEXT]` |
| MIX-006 | Feroneria în lemn moale (molid/pin): șuruburile țin mai puțin; la ușile cu balamale și la glisiere se folosesc **dibluri/inserții** sau se păstrează specia tare în zona de prindere. | fără valori oficiale găsite | `[PRACTICA]` |
| MIX-007 | Buloanele de pat se sprijină pe lemn perpendicular pe fibră: dacă lonjeronul ar fi din pin, șaiba crește. | f_c,90: stejar alb 7,4 MPa vs pin roșu 4,1 MPa (WH 2010, proxy) → aria șaibei × 1,8 `[V-DATA]` | proxy WH |
| MIX-008 | Clasa B se alege **ca aspect**, nu doar ca preț: frasin lângă stejar (pori inelari, culoare apropiată). | — | `[PRACTICA]` |
| MIX-009 | Toate speciile la **aceeași umiditate** la asamblare: mobilier 8 % (6–10 %) USDA; SWP la livrare 8 ± 2 % (EN 13353). | ΔMC sezonier apartament încălzit ≈ 5 % `[ESTIMARE]` | `[V-TEXT]` / `[V-STD]` |
| MIX-010 | Stejarul (taninuri) pătează în contact cu oțel neprotejat + umezeală → feronerie inox / zincată. | — | `[PRACTICA]` |
| MIX-011 | **Declarația de specie** pe ofertă și pe fișa produsului, pe clase (A/B/C). „Stejar masiv” fără calificare = doar PREMIUM. O formulare care induce în eroare asupra materialului intră sub practicile comerciale incorecte (Directiva 2005/29/CE). | text tip în §8 | `[REF]` |
| MIX-012 | Panourile SWP se finisează **pe ambele fețe la fel** (curbarea depinde de simetria umidității); șipcile/grinzile ascunse din masiv pot rămâne nefinisate în ESENȚIAL. | Gereke et al. 2009 | `[REF]` |
| MIX-013 | Îmbinarea de corp **nu se vede** la lux: confirmat cu capac pe lateralele de stejar e interzis în toate nivelurile → dibluri (lipit) sau minifix (demontabil). | `joinery = dibluri` în calcul | `[PRACTICA]` |

---

## 4. Piesele active (generate de configurator)

### 4.1 PAT — King 1600 × 2000 (preset `King 1600 x 2000`)

| Parametru | Valoare | De unde |
|---|---|---|
| Saltea | 1600 × 2000 × 200, cu arcuri | preset |
| Înălțime pat (fața de sus a saltelei sub ea) | 400 mm | preset; ergonomie în [FURNITURE_BED_SOLID_WOOD](beds/FURNITURE_BED_SOLID_WOOD.md) |
| Spațiu sub pat | 150 mm | parametru |
| Tăblie | 1000 mm, ramă + panou masiv 20 mm (panou încleiat, plutitor) | parametru |
| Îmbinare lonjeron–picior | **bulon de pat M8 ascuns + piuliță cilindrică + dop D20** (regula BED-R-030: demontabil pentru transport) | regulă |
| Somieră | 16 șipci 28 × 70, pe rigle 20 × 42 înșurubate | calcul săgeată (`select_beam`) |
| Grindă centrală | 1 × 45 × 70 + picior central 45 × 45 (regula BED-R-001: saltea lată → grindă) | regulă |
| Standard produs | EN 1725:2023 (paturi) `[V-STD]` — încercarea se face pe produs | — |

### Tabel piese: Pat King 1600x2000

| Piesa | Buc | t × l × L (mm) | Clasa | PREMIUM | STANDARD | ESENTIAL | Cost mat. P / S / E (RON) |
|---|---|---|---|---|---|---|---|
| Picior tablie | 2 | 70 × 70 × 1000 | A | stejar masiv | stejar masiv | stejar masiv | 95 / 95 / 95 |
| Picior picioare | 2 | 70 × 70 × 430 | A | stejar masiv | stejar masiv | stejar masiv | 43 / 43 / 43 |
| Traversa capat | 2 | 28 × 120 × 1702 | A | stejar masiv | stejar masiv | stejar masiv | 116 / 116 / 116 |
| Lonjeron | 2 | 28 × 120 × 2046 | A | stejar masiv | stejar masiv | stejar masiv | 139 / 139 / 139 |
| Rigla sprijin | 2 | 20 × 42 × 2010 | C | stejar masiv | pin masiv | pin masiv | 38 / 13 / 13 |
| Sipca | 16 | 28 × 70 × 1604 | C | stejar masiv | pin masiv | pin masiv | 523 / 177 / 177 |
| Grinda centrala | 1 | 45 × 70 × 2040 | C | stejar masiv | pin masiv | pin masiv | 63 / 21 / 21 |
| Picior central | 1 | 45 × 45 × 267 | C | stejar masiv | pin masiv | pin masiv | 6 / 2 / 2 |
| Traversa sus tablie | 1 | 28 × 95 × 1702 | A | stejar masiv | stejar masiv | stejar masiv | 46 / 46 / 46 |
| Panou tablie | 1 | 20 × 486 × 1628 | A | stejar masiv | stejar masiv | stejar masiv | 165 / 165 / 165 |
| **Total material** | | | | | | | **1235 / 818 / 818** |

Feronerie: Patina / talpa reglabila picior pat × 5; Bulon de pat M8 cap cilindric inox × 4; Piulita cilindrica (barrel nut) M8 D12x20 × 4; Dop lemn D20 (acoperire cap bulon) × 4; Surub lemn 4 x 40 (rigle) × 26; Surub lemn 3,5 x 30 (sipci, coltare) × 48; Adeziv PVAc D3 (kg) × 0.2.

**Note:** la pat, STANDARD = ESENȚIAL, pentru că patul nu are piese de clasă B: tot ce nu se vede (șipci, rigle, grindă, picior central = **51 % din costul de stejar al patului**, 417 RON) trece pe pin. Secțiunile rămân aceleași (MIX-004). Lonjeronii rămân stejar (clasa A și sprijinul buloanelor, MIX-007).

### 4.2 NOPTIERĂ — 1 sertar + nișă (× 2 în set)

| Parametru | Valoare |
|---|---|
| Gabarit | corp 500 × 400 × 550 (capac 520 × 410), picioare din stejar 45 × 45 × 150 |
| Corp | SWP 19, îmbinat cu **dibluri** (MIX-013); spate HDF 3 în nut |
| Sertar | front SWP stejar 19 cu **scobitură** pentru deget (fără mâner, iterația 10); cutie din SWP 19 (constanta `drawer_side_t = 19`), fund HDF 3, glisieră ascunsă soft-close |
| Nișă | poliță fixă deasupra sertarului |
| Validare | OK; sub pragurile de stabilitate EN 14749 (H < 900 și m < 35 kg) `[V-STD]` |

### Tabel piese: Noptiera 1 sertar + nisa

| Piesa | Buc | t × l × L (mm) | Clasa | PREMIUM | STANDARD | ESENTIAL | Cost mat. P / S / E (RON) |
|---|---|---|---|---|---|---|---|
| Lateral 381x400 | 2 | 19 × 400 × 381 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 178 / 178 / 178 |
| Fund 462x400 | 1 | 19 × 400 × 462 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 108 / 89 / 39 |
| Capac 520x410 | 1 | 19 × 410 × 520 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 124 / 124 / 124 |
| Spate 477x377 | 1 | 3 × 377 × 477 | C | HDF 3 | HDF 3 | HDF 3 | 3 / 3 / 3 |
| Polita nisa 462x386 | 1 | 19 × 386 × 462 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 104 / 86 / 38 |
| Fata sertar 497x200 | 1 | 19 × 200.5 × 497 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 58 / 58 / 58 |
| Sertar 420 lateral | 2 | 19 × 153.5 × 350 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 63 / 52 / 23 |
| Sertar 420 fata/spate | 1 | 19 × 153.5 × 382 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 34 / 28 / 12 |
| Sertar 420 spate | 1 | 19 × 141.5 × 382 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 31 / 26 / 12 |
| Sertar 420 fund | 1 | 3 × 335 × 394 | B | HDF 3 | HDF 3 | HDF 3 | 2 / 2 / 2 |
| Picior 45x150 | 4 | 45 × 45 × 150 | A | stejar masiv | stejar masiv | stejar masiv | 15 / 15 / 15 |
| **Total material** | | | | | | | **720 / 661 / 504** |

Feronerie: Diblu fag 8 x 35 × 30; Glisiera ascunsa soft-close (pereche) × 1; Surub 4 x 16 (feronerie) × 12; Diblu fag 10 x 40 × 4; Surub lemn 4 x 40 (rigle) × 4; Patina / talpa reglabila picior pat × 4.

### 4.3 COMODĂ — 1800 × 450 × 800

| Parametru | Valoare |
|---|---|
| Coloane | 2 × 4 sertare **gradate** (cele de jos mai înalte: fronturi 197 / 174 / 151 / 128) |
| Corp | SWP 19 cu dibluri, separator central, picioare stejar 45 × 45 × 120 (6 buc., cu picioare intermediare sub separator) |
| Fronturi | SWP stejar 19, scobitură |
| Stabilitate | **~87 kg, H 800 → intră la încercarea de stabilitate EN 14749**: kit anti-basculare inclus + etichetă `[V-STD]` |

### Tabel piese: Comoda 1800x450x800

| Piesa | Buc | t × l × L (mm) | Clasa | PREMIUM | STANDARD | ESENTIAL | Cost mat. P / S / E (RON) |
|---|---|---|---|---|---|---|---|
| Lateral 661x450 | 2 | 19 × 450 × 661 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 346 / 346 / 346 |
| Fund 1762x450 | 1 | 19 × 450 × 1762 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 462 / 382 / 169 |
| Capac 1820x460 | 1 | 19 × 460 × 1820 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 488 / 488 / 488 |
| Separator 642x436 | 1 | 19 × 436 × 642 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 163 / 135 / 60 |
| Spate 1777x657 | 1 | 3 × 657 × 1777 | C | HDF 3 | HDF 3 | HDF 3 | 21 / 21 / 21 |
| Fata sertar 897x197 | 2 | 19 × 197 × 897 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 206 / 206 / 206 |
| Sertar 830 lateral | 4 | 19 × 150 × 400 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 140 / 116 / 51 |
| Sertar 830 fata/spate | 2 | 19 × 150 × 791.5 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 138 / 114 / 51 |
| Sertar 830 spate | 2 | 19 × 138 × 791.5 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 127 / 105 / 46 |
| Sertar 830 fund | 8 | 3 × 385 × 803.5 | B | HDF 3 | HDF 3 | HDF 3 | 44 / 44 / 44 |
| Fata sertar 897x174 | 2 | 19 × 174 × 897 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 182 / 182 / 182 |
| Sertar 830 lateral | 4 | 19 × 134 × 400 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 125 / 103 / 46 |
| Sertar 830 fata/spate | 2 | 19 × 134 × 791.5 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 124 / 102 / 45 |
| Sertar 830 spate | 2 | 19 × 122 × 791.5 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 112 / 93 / 41 |
| Fata sertar 897x151 | 2 | 19 × 151 × 897 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 158 / 158 / 158 |
| Sertar 830 lateral | 4 | 19 × 111 × 400 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 103 / 86 / 38 |
| Sertar 830 fata/spate | 2 | 19 × 111 × 791.5 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 102 / 85 / 37 |
| Sertar 830 spate | 2 | 19 × 99 × 791.5 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 91 / 75 / 33 |
| Fata sertar 897x128 | 2 | 19 × 128 × 897 | A | SWP stejar 19 | SWP stejar 19 | SWP stejar 19 | 134 / 134 / 134 |
| Sertar 830 lateral | 4 | 19 × 88 × 400 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 82 / 68 / 30 |
| Sertar 830 fata/spate | 2 | 19 × 88 × 791.5 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 81 / 67 / 30 |
| Sertar 830 spate | 2 | 19 × 76 × 791.5 | B | SWP stejar 19 | SWP frasin 19 | SWP molid 19 | 70 / 58 / 26 |
| Picior 45x120 | 6 | 45 × 45 × 120 | A | stejar masiv | stejar masiv | stejar masiv | 18 / 18 / 18 |
| **Total material** | | | | | | | **3518 / 3185 / 2299** |

Feronerie: Diblu fag 8 x 35 × 68; Glisiera ascunsa soft-close (pereche) × 8; Surub 4 x 16 (feronerie) × 96; Diblu fag 10 x 40 × 6; Surub lemn 4 x 40 (rigle) × 6; Patina / talpa reglabila picior pat × 6; Kit anti-basculare (fixare de perete) × 1.

**Note:** la comodă economia vine aproape toată din cutiile de sertar (clasa B, 24 de piese). Corpul exterior (laterale, capac, fronturi, picioare) e ~44 % din material și rămâne stejar în toate nivelurile.

---

## 5. Piesele planificate (încă negenerate) — specificație de pornire

| Piesă | Ce propun | Clase A / B / C | Reguli care se aplică | Status |
|---|---|---|---|---|
| **Dulap** (uși batante) | H 2000–2400, adâncime ~600, uși ≤ 600 lățime, corp SWP 19, uși SWP 19 fără mâner (tesitură/scobitură) sau push; bară de haine, polițe, 2 sertare interioare | A: uși, laterale exterioare, capac, plintă/picioare · B: polițe, separatoare, sertare interioare · C: spate, funduri | EN 14749 stabilitate (H > 900 → ancorare obligatorie) `[V-STD]`; balamale: grosime ≤ 26 mm, distanța dintre balamaua de sus și cea de jos > lățimea ușii `[V-TEXT]`; 3–4 balamale la H 2000+ `[PRACTICA]`; sarcină poliță 0,65 kg/dm³ `[V-STD]` | `[ESTIMARE]` — de construit pe modulul `case` |
| **Tăblie contemporană cu îmbinări la vedere** | ramă stejar cu **cep cu pană** prin picior sau coadă de rândunică vizibilă; panou SWP sau masiv cu joc la mișcare | totul A | IT9: îmbinările fără clei nu au date peer-review → probă proprie `[V-TEXT]` | `[ESTIMARE]` |
| **Băncuță** (picior de pat) | 1200–1400 × 380 × 450, cadru cu cepuri, șezut masiv | totul A (se vede din toate părțile) | ergonomie șezut 440–480 `[V-TEXT]` (Mattiazzi); EN 12520 sarcini șezut 1300 N `[V-STD]` | `[ESTIMARE]` |
| **Etajeră** | stâlpi + polițe în îmbinări de locaș / coadă de rândunică glisantă | A: tot; doar spatele (dacă există) C | săgeată poliță cu fluaj (deja în configurator: `shelf_thickness`) | `[ESTIMARE]` |
| **Dressing** | sistem de module pe perete (ca bucătăria): corpuri, bare, sertare | A puțin, B mult → **aici lemnul secundar aduce cea mai mare economie** | idem dulap | `[ESTIMARE]` |

---

## 6. Ce se schimbă în configurator ca să facă asta automat

| # | Schimbare | De ce |
|---|---|---|
| 1 | parametru nou **„Nivel material”** (`PREMIUM / STANDARD / ESENTIAL`) pe pat, noptieră, comodă (și pe produsele noi) | o singură alegere în loc de 4–5 câmpuri de material |
| 2 | **material pe rol/clasă** (A, B, C) în locul unui singur `body_mat`: laterale vs fund/separatoare/polițe, cutie sertar separat | azi corpul are un singur material pentru toate panourile |
| 3 | pat: **material pe grupă** (structură vizibilă vs somieră/grindă) | azi patul are un singur `material` |
| 4 | validare MIX-003 (contragere), MIX-005 (îmbinare în lemnul mai slab), MIX-013 (confirmat la vedere) | reguli din §3 |
| 5 | **„Set dormitor”**: pat + 2 noptiere + comodă cu aceleași alegeri și **o singură debitare** (plăcile SWP 5000 × 2050 se împart pe tot setul) | azi o noptieră singură „cumpără” o placă întreagă de SWP (5 437 RON material), deși folosește ~1,3 m² (720 RON) |
| 6 | declarația de specii (MIX-011) scrisă automat în oferta PDF/XLSX | onestitate + vânzare |

Corecții deja făcute în această iterație: finisajul se calculează acum și pe plăcile SWP cu față de lemn (înainte apărea 11 RON la comodă).

---

## 7. Alte pârghii de cost (în afara lemnului)

| Pârghie | Cât contează (comodă 1800) | Observație |
|---|---|---|
| Glisiere | 8 × 95 = **760 RON** | glisieră pe bile (28 RON) în ESENȚIAL; sau sertare cu ghidaj din lemn (tradițional, fără feronerie) în PREMIUM ca element de lux |
| Prelucrare + manoperă | ~1 100 RON | dibluri vs confirmat: aceeași oră; fronturile fără mâner adaugă o operație CNC |
| Finisaj | ~600 RON | ulei pe ambele fețe la SWP (MIX-012); șipcile ascunse pot rămâne nefinisate în ESENȚIAL |
| Spatele și fundurile | HDF 3 în toate nivelurile azi | **în PREMIUM, HDF-ul nu se potrivește cu „stejar integral”** → propun spate și funduri din placaj/SWP de stejar subțire sau panou în ramă (de adăugat în baza de materiale) |

---

## 8. Text de declarație pentru ofertă (MIX-011)

- **PREMIUM:** „Stejar masiv în toate componentele din lemn: structură, fronturi, interior și sertare. Spate și funduri: [material].”
- **STANDARD:** „Exterior din stejar masiv (fronturi, blat, laterale, picioare, tăblie). Interior și cutiile sertarelor din frasin masiv. Elemente structurale ascunse (somieră, grinzi) din pin masiv.”
- **ESENȚIAL:** „Exterior din stejar masiv. Interior, sertare și structura ascunsă din pin / placă din lemn masiv de molid.”

---

## 9. Lacune
- prețurile sunt exemple → pune prețurile tale în `db/materials.json` și rulează din nou `dormitor/calc.py`;
- pragul MIX-003 (0,05 %/%) e o propunere `[ESTIMARE]`; nu am găsit o sursă oficială pentru compatibilitatea speciilor încleiate;
- forța de smulgere a șuruburilor/confirmatului în molid vs stejar: nicio valoare oficială găsită;
- nu există încă material pentru spate/funduri „de lux” (placaj stejar);
- dulapul, băncuța, etajera, tăblia cu îmbinări la vedere și dressingul nu sunt încă generate.
