# FURNITURE — BED (pat din lemn masiv) — ghid de proiectare complet

Raspunde la: "Vreau sa construiesc un pat din lemn masiv. Ce sectiuni folosesc? Cum imbin cadrul? Ce imbinare e mai rezistenta? Ce toleranta las? Ce adeziv? Cu freza / manual? Alternativa japoneza? Ce lemn? Stejar vs brad?"

Niveluri: majoritatea valorilor sunt `[PRACTICA]`/`[ESTIMARE]` (consens de atelier + verificare simpla). Surse de sprijin: Fairham `[V-TEXT]` (imbinari), Joyce *Encyclopedia of Furniture Making* (sectiuni tipice de mobilier) `[REF]`, Wood Handbook cap. 4–5 `[REF]`, EN 338 `[V-STD]`, standardele de mobilier EN 1725 (paturi — cerinte de siguranta si metode de incercare) `[V-STD]`.

## 1. Dimensiuni de gabarit (saltele europene)
Saltea 90/140/160/180 × 200 cm. Cadru interior = saltea + 5–10 mm pe fiecare directie (joc de montaj). Inaltimea suprafetei de dormit (top saltea) ~45–55 cm (ergonomie: sezut usor = genunchi la ~90°). `[PRACTICA]`

## 2. Sectiuni recomandate `[ESTIMARE]` (lemn uscat 8–10 %, sarcina: 2 persoane ~200 kg + dinamica)
| Element | Stejar / fag / frasin | Brad / molid / pin | Observatii |
|---|---|---|---|
| Picioare | 70×70 – 80×80 mm | 80×80 – 90×90 mm | la buloane de pat + cepuri, piciorul trebuie sa permita scobitura + gaura bulonului fara a fi slabit sub ~1/3 din sectiune pe fiecare perete |
| Lonjeroane laterale (L ≈ 2 m) | 28–32 × 140–160 mm | 35–40 × 160–180 mm | criteriu: sageata; vezi calculul de mai jos |
| Traverse cap/picioare (L 0,9–1,8 m) | 28–32 × 140–160 mm | 35–40 × 160–180 mm | |
| Sipca de sprijin a somierei (cleat) pe lonjeron | 25×40 mm, lipit + surub la 150–200 mm | 30×45 mm | sau falt in lonjeron |
| Grinda centrala (pat ≥ 140 cm) | 40×80 – 45×100 mm | 45×100 – 50×120 mm | **obligatoriu** cu 1–2 picioare centrale reglabile |
| Lamele (daca nu e somiera) | 18–22 × 70–90 mm, interspatiu ≤ 50–70 mm | 20–25 × 80–100 mm | lamele fixe pe grinda centrala cu un surub la capat (nu blocate rigid la ambele capete — lasa miscare); verifica interspatiul recomandat de producatorul saltelei |
| Tablie | rama + panou plutitor sau scanduri cu lamba-uluc | idem | |

### Verificare rapida a lonjeronului `[ESTIMARE]`
Model: grinda simplu rezemata, L = 2,0 m, sarcina pe un lonjeron ~1/4 din (200 kg persoane + 40 kg saltea/somiera) → ≈ 60 kg ≈ 0,6 kN distribuit, x 2 factor dinamic ≈ 1,2 kN.
Pentru 30×150 mm stejar: W = b·h²/6 = 30·150²/6 = 112 500 mm³; M = qL²/8 = (1,2 kN/2 m)·2²/8 = 0,3 kNm → σ = 0,3e6/112 500 ≈ **2,7 MPa** ≪ f_m,k D30 = 30 MPa. Sageata: I = 30·150³/12 = 8,44e6 mm⁴; w = 5qL⁴/(384EI) = 5·0,6·2000⁴/(384·11000·8,44e6) ≈ **1,3 mm** → OK (L/1500).
**Concluzie**: sectiunile sunt dictate de **imbinari si rigiditatea nodurilor** (si estetica), nu de incovoierea lonjeronului. Partea critica = nodul picior–lonjeron (moment de rasucire/racking + smulgerea cepului la miscari laterale). Pentru elemente portante neconventionale (pat suspendat, supraetajat) → calcul Eurocod 5 + EN 747 (paturi supraetajate) `[V-STD]`.

## 2b. Verificare cu surse istorice (iteratia 3) `[V-TEXT]`
| Element | Recomandarea din aceasta fisa | Sofa-pat stejar alb — *Mission Furniture* Part 2 (1910) | Concluzie |
|---|---|---|---|
| Picioare | 70–80 mm (stejar) | **76 × 76 mm** (3 in) | ✔ identic |
| Lonjeroane (≈ 2 m) | 28–32 × 140–160 mm | **22 × 203 mm** × 2083 mm (cu cepuri) | MF: mai subtiri dar mai adanci — rigiditate I = 15,3·10⁶ mm⁴ vs 8,4·10⁶ la 30×150 → varianta MF e **mai rigida**; ambele sunt OK (sageata ≈ 1,3 mm la 30×150 — §2) |
| Suport saltea | sipca 25×40 pe lonjeron | sipci (cleats) 22 × 51 mm insurubate pe interior, **muchia de sus la 51 mm sub muchia lonjeronului**; 12 lamele 19 × 127 mm pe ~640 mm | ✔ aceeasi solutie; marginea de 51 mm tine salteaua pe loc |
| Colturi | — | **coltare din stejar cu fibra pe diagonala** + suruburi; echer verificat prin diagonale | adauga coltare la paturile fara bulon |
| Imbinare picior–lonjeron (demontabil) | varianta A: bulon + cep scurt | Wells & Hooper (1922), patul "francez": **cep scurt + bulon, piulita ingropata in picior, golul astupat cu lemn**; settee MF: cuie de lemn scoase, fara adeziv | ✔ **varianta A confirmata** de o sursa de ebenisterie profesionala |
Detalii: `../furniture_construction/FURNITURE_PROPORTIONS_HISTORICAL.md` (BED-WH-001…005) si `../chairs/FURNITURE_PROJECTS_SEATING.md` (PRJ-P2_COUCH).

## 3. Imbinarea cadrului (picior–lonjeron) — optiuni
| Varianta | Rezistenta | Demontabil | Dificultate | Cand |
|---|---|---|---|---|
| **A. Bulon de pat (bed bolt) + cep orb scurt de aliniere (stub tenon 15–20 mm)** | foarte mare | DA | medie | **recomandat standard** — paturile trebuie sa iasa pe usa |
| B. Cep trecut + pana transversala (tusk tenon) — Fairham Fig. 152–155 | mare | DA | mare | aspect "craftsman"/rustic, fara metal |
| C. Cep lung + hanasen (cui prin capatul cepului) / sao + shachi-sen — varianta japoneza | mare | DA | mare | fara metal, stil japonez |
| D. Feronerie de pat cu carlige (bed rail fasteners, hook-plates) ingropata | medie–mare | DA | mica | rapid; necesita frezare precisa |
| E. Cep orb lipit (M&T clasic) | foarte mare | NU | medie | pat construit pe loc / dimensiuni mici |
| F. Dibluri + surub (confirmat) | medie | partial | mica | mobilier ieftin; slabeste in timp |

**Cea mai rezistenta (ca rigiditate pe termen lung)**: E (M&T lipit) > A (bulon + cep) ≈ B (tusk) > C > D > F. Pentru pat, **A** este compromisul optim (rezistenta + demontare + re-strangere in timp). `[PRACTICA]` (consens; nu exista test numeric unic verificat aici)

### Detaliu A (bulon + cep de aliniere) `[PRACTICA]`
- Bulon M8–M10 × 150–180 mm (bulon de pat cu cap cilindric) sau tija filetata + piulita cilindrica (barrel nut Ø 12–15 mm) in lonjeron, la ≥ 60–80 mm de capatul lonjeronului.
- Cep(uri) scurt(e) de aliniere (15–20 mm) pe capatul lonjeronului, grosime ~1/3 din grosimea lonjeronului (regula Fairham), latime cat inaltimea minus haunch-uri; NU se lipeste.
- Doua picioare la acelasi colt nu au scobituri la acelasi nivel (lonjeron si traversa) → decaleaza pe inaltime 20–30 mm ca sa nu slabesti piciorul in acelasi plan (sau cepuri care se intalnesc mitred — Fairham Fig. 167). `[V-TEXT]` + `[PRACTICA]`
- Capac decorativ peste capul bulonului.
- Toleranta: cep de aliniere cu joc 0,2–0,3 mm pe grosime (se monteaza/demonteaza fara forta); umerii perfect in echer (toata strangerea se face pe umeri).

## 4. Tolerante si miscare
- Cadrul nu are piese late fixate incrucisat → miscarea e mica; **tablia** (panouri) da probleme: panou plutitor in nut, joc estimat: panou stejar flatsawn 400 mm, ΔUM 4 % → 400 × 0,00376 × 4 ≈ 6,0 mm total (WH 2021 Tab. 13–5, coeficient white oak) → lasa ~3 mm pe fiecare parte (mai putin daca montezi in sezonul umed). Vezi `05_WOOD_SELECTION/moisture/WOOD_MOISTURE_MOVEMENT.md`.
- Lamelele: prinse cu un singur surub sau in locase, nu lipite.

## 5. Adeziv
- Tablie (rama + panou) si picioare compuse: PVAc D2/D3.
- Imbinarile demontabile: **fara adeziv**.
- Stejar: suruburi/buloane zincate pot pata (taninuri) → inox sau ascunse/neatinse de umezeala.

## 6. Executie — cu utilaje vs manual
| Operatie | Cu utilaje | Manual |
|---|---|---|
| Dimensionare | abricht → grosime → circular | rindele #7/#5, gramil (Fairham) |
| Scobituri in picioare | masina de scobit / freza de mana cu ghidaj (spirala ascendenta Ø 10–12 mm) / Domino XL | burghiu + dalta de scobit, din ambele parti (Fairham) |
| Cepuri | circular cu tenoning jig sau masa de frezat | ferastrau de cepuri + rindea de umar |
| Gaura bulonului | masina de gaurit cu coloana (piciorul) + gabarit; gaura lunga in lonjeron: burghiu lung cu ghidaj | coarba cu burghiu lung + echer de ghidaj |
| Locasul piulitei cilindrice | burghiu Forstner | coarba |

## 7. Alternative japoneze
- Picior–lonjeron: **cep lung + hanasen** sau **sao-tsugi cu shachi-sen** (tragere prin chei). Vezi `11_JAPANESE_WOODWORKING/joinery/JP_HANASEN_WEDGED_TENON.md`, `JP_SAO_TSUGI_SHACHISEN.md`.
- Rigidizare: **nuki** la nivelul de jos al picioarelor (traversa care strabate picioarele + pene).
- Pat japonez traditional = futon pe tatami (fara pat); "pat-platforma" in stil japonez = cadru jos + lamele/scanduri.

## 8. Ce lemn
Stejar (premium, durabil, greu), frasin (tenace, mai ieftin), fag (bun, dar misca mult — evita panouri late), nuc/cires (lux), pin/brad/molid (economic — sectiuni mai mari, atentie la strivire la buloane: **saibe mari / piulite cilindrice**), zambru (traditie alpina, parfum). Vezi `05_WOOD_SELECTION/european_species/WOOD_SPECIES_EUROPEAN.md`.

## 9. Stejar → brad: modificari de design
1. Picioare +10 mm pe latura; lonjeroane +5–8 mm grosime, +20 mm inaltime.
2. Buloane: saibe de Ø 30–40 mm sau piulite cilindrice Ø 15 mm (suprafata de strivire); bulon M10.
3. Cepuri mai groase (spre 1/2 din grosimea lonjeronului) si umeri lati.
4. Colturi rotunjite/tesite (se ciobesc mai usor).
5. Finisaj: sealer inainte de bait (blotching) / lac mai dur (brad se zgarie).
6. Greutate mai mica → usor de mutat; rigiditate similara (vezi calcul E).

## 10. Defecte frecvente si cum le eviti
- Pat care scartaie → umeri care nu se aseaza plan, lamele frecate, buloane slabite → umeri in echer, banda de feltru/teflon sub lamele, re-strangere dupa 1–2 luni.
- Lonjeron rotit (twist) → lemn neuscat / ne-aclimatizat; foloseste lemn quartersawn sau rift pentru lonjeroane.
- Picior crapat la gaura bulonului → bulon prea aproape de capatul piciorului / scobituri suprapuse.
- Grinda centrala lasata → picior central lipsa.
