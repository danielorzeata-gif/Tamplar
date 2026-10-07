# MOBILIER DIN LEMN MASIV — matrice completa (iteratia 9)

Integreaza fisele existente (`tables/`, `chairs/`, `beds/`, `cabinets/`, `furniture_construction/`) cu usile si ferestrele (`../04_CARPENTRY/`) si cu feroneria (`../08_MATERIALS/hardware/HARDWARE_COMPLETE.md`). Fiecare rand = o "componenta" reutilizabila in plugin; coloana "Imbinari" = lista din care utilizatorul alege (prima = implicit).
Etichete: `[WEB]` rezumat cautare (neverificat integral), `[V-TEXT]`/`[REF]`/`[PRACTICA]`/`[ESTIMARE]` ca in baza. Dimensiuni in mm.

## 1. Reguli transversale (valabile pentru orice piesa de mobilier)

**FUR-GEN-001 — Fibra si miscarea**: lemnul se misca doar pe latime/grosime (≈ 0,3–0,5 % per 1 % UM tangential), nu pe lungime. Orice piesa lata, fixata rigid pe ceva ce se misca altfel, crapa. Calcul: `Δ = L × C_T × ΔUM` (WH 2021; stejar 0,00376 → 900 mm, ΔUM 4 % = 13,5 mm). SOURCE: WH 2021 `[V-TEXT]`; `[WEB]` (blat: fara lipire/insurubare rigida).
**FUR-GEN-002 — Mobil liber vs. mobil incastrat**: mobilierul liber (mese, scaune, paturi, dulapuri) are nevoie de imbinari mai puternice decat cel sprijinit pe pereti/podea. SOURCE: `[WEB]`.
**FUR-GEN-003 — Ierarhia de rezistenta la rame (picior–zarga)**: cep-mortaiza > cep liber / dibluri > suruburi in buzunar > lamele (biscuit). Rezultatele testelor difera (forfecare vs. racking); cepul ramane prima alegere. SOURCE: `[WEB]` (Wood Magazine: 1005 lbf cep-mortaiza; alte teste contradictorii) + SOURCE_CONFLICTS C-13.
**FUR-GEN-004 — Panouri plutitoare** (usi, spate de dulap, laturi): niciodata lipite in canal; joc ≥ 1,5–3 mm/latura; finisate inainte de asamblare (DOOR-MOV-001…003).
**FUR-GEN-005 — Cadru + panou vs. placa masiva**: latimi > ~300 mm la o singura bucata = risc; lamele incleiate cu rost plan (C-11) sau rama + panou.
**FUR-GEN-006 — Aspect**: fata cea mai frumoasa in sus/in fata; fibra continua pe usi si sertare vecine; fete de sertar taiate in ordine din aceeasi scandura `[PRACTICA]`.

## 2. Matricea pe tipuri

| Tip | Componente principale | Imbinari (implicit → alternative) | Feronerie | Capcana de miscare | Fisa baza |
|---|---|---|---|---|---|
| **Masa dining** | blat (lamele), 4 picioare, 4 zarge | zarga–picior: M&T → cep liber, dibluri, bridle, kusabi (traversa 1), buzunar | clipsuri Z / figure-8 / butoni; surub in gaura alungita | blatul se misca pe latime (13,5 mm / 900 mm stejar) | tables/ |
| **Masa cu extensie / rotunda** | blat, picior central, ghidaje | M&T, glisiere de lemn | glisiere | doua jumatati + inserturi: aceeasi directie a fibrei | tables/ |
| **Birou (desk)** | blat, laturi/picioare, sertare, panou frontal | M&T, dado, coada de randunica la sertar | glisiere, maner | blat pe lateral rigid → jumatate fixa | tables + casework |
| **Banca / taburet** | sezut, picioare, traversa, contrafise | M&T, tusk tenon (demontabil), dibluri | — | sezut fix doar la centru | chairs/ |
| **Scaun** | 4 picioare (curbate sau drepte), sezut, spatar, zarge, traverse | M&T stub cu cepuri mitred, dibluri (productie), cep + pana | — | picioare cu fibra dreapta; spatar curbat = lamelat | chairs/ |
| **Pat** | 4 picioare/stalpi, lonjeroane, capete, sipci, grinda centrala | **bulon de pat + cep de aliniere** (demontabil) → M&T tusk, cep + pana | suport lamele, bolturi, pante | capete late = lamele; bascula la lonjeroane | beds/ |
| **Noptiera / comoda** | corp (4 laturi + blat), soclu, sertare, spate | coada de randunica (colt), dado (raft), M&T (rama spate) | glisiere, manere | laturi masive late: blat fixat cu butoni; spate panou plutitor | cabinets/ |
| **Dulap / garderoba** | corp (2 laturi, plafon, fund), soclu, cornisa, usi, raft | dado + suruburi/dibluri, coada de randunica coborata la corpuri mari, M&T la usi | balamale cu cupa 35, sine de haine, glisiere | usi din panouri; spate din placaj/placi in canal | cabinets/ |
| **Biblioteca / raft** | 2 laturi, rafturi, plinta, spate | dado (rafturi), coada de randunica glisanta, dibluri | bolturi de raft 5 mm sistem 32 | raftul masiv se misca pe latime: nu in dado strans pe toata adancimea | cabinets/ |
| **Comoda cu sertare** | corp + rama (face frame) + sertare | M&T la rama, coada de randunica la sertar, dust board in canal | glisiere cu bile 12,7 mm/latura | fete de sertar lipite pe fibra; rama fata nu se lipeste de lateral pe toata lungimea | cabinets/DRAWERS |
| **Bucatarie (module)** | corp inferior 720×560, superior, blat, socluri, usi | dado/dibluri/Confirmat; usi M&T | cupa 35, glisiere, sine, bolturi | blat masiv: fixare in gauri alungite | cabinets/ + kitchens skills |
| **Usa** | vezi `DOORS_COMPLETE.md` | M&T haunched | balamale, broasca | panou plutitor | doors/ |
| **Fereastra** | vezi `WINDOWS_COMPLETE.md` | M&T trecut haunched, dublu cep | feronerie Euro | profil lamelat | windows/ |
| **Scara (optional)** | trepte, contratrepte, limoane, balustrada | housing/dado, wedged tenon, bolturi | — | trepte pe lungime: nu se lipesc de limon pe toata latimea | de adaugat |

## 3. Dimensiuni ergonomice (valori de pornire; verifica si Panero/Dreyfuss din `13_DESIGN_AND_ENGINEERING`)

| Parametru | Valoare | Sursa |
|---|---|---|
| Inaltime masa dining | 720–760 | `[WEB]` (scaun 450–480 pentru mese 720–760) |
| Inaltime scaun | 405–460 (general); 440–500 dining | `[WEB]` |
| Adancime scaun | 380–460 (general); 435–485 dining | `[WEB]` |
| Spatar: inclinare | ≤ 5° (dining formal), inaltime 305–508 deasupra sezutului | `[WEB]` |
| Latime per loc la masa | ≥ 610 (24") coate libere | `[WEB]` |
| Inaltime blat bucatarie | 870 corp + 40 blat = 910 (≈ la nivelul incheieturii) | `[WEB]` |
| Corp bucatarie | 720 inalt × 560 adanc; blat 600; soclu 100–150 (retras 50–75) | `[WEB]` |
| Liber intre blat si corp superior | 450–600 | `[WEB]` |
| Garderoba: adancime | ~600 (interior min. 500) | `[WEB]` |
| Garderoba: bara de haine | 1520–1770 de la podea; 35–55 libere dupa umeras | `[WEB]` |
| Spatiu haine scurte / lungi | 940 / 1880 | `[WEB]` |
| Raftul cel mai de sus accesibil | ≤ 1750 | `[WEB]` |
| Pat: standard de siguranta | EN 1725 (incercari pana la 110 kg) | `[WEB]` |
| Sipci de pat: distanta | 63–100 mm (2,5–4"), recomandat ≤ 70 pentru saltele cu spuma | `[WEB]` + `[PRACTICA]` |

## 4. Rafturi: sageata si deschidere

**FUR-SHF-001 — Grosime vs. deschidere (carti)**: **18–19 mm → max ≈ 750 mm** (pana la 900 mm cu 19 mm doar sarcina usoara); **25 mm → 900 mm**; 32 mm pentru > 900 mm sau sarcini mari. SOURCE: `[WEB]`.
**FUR-SHF-002 — Limita de sageata**: ochiul percepe ~1/32" pe picior (≈ 0,8 mm/300 mm ≈ L/380); tinta de proiectare ≈ 0,02"/picior; lemnul "curge" cu +50 % in timp peste sageata initiala. SOURCE: `[WEB]`.
**FUR-SHF-003 — Fizica**: sageata creste cu **L³** (L⁴ / E·I la sarcina distribuita): dublarea deschiderii ≈ 8× sageata (pe sursa se spune "exponential") → scurteaza deschiderea inainte sa ingrosi. `[ESTIMARE]`: δ = 5·w·L⁴ / (384·E·I).
**FUR-SHF-004 — Remedii**: cant aplicat (lip) pe fata, spate structural, sine/traversa sub raft, raft cu "front rail". SOURCE: `[REF]`.
**Verificare in plugin (regula de validare)**: raft masiv de grosime t, deschidere L, sarcina q → avertisment cand δ > L/300 (E din materials.json, ex. stejar ≈ 12 GPa la 12 % UM, WH 2021 Tab. 5–3a).

## 5. Sertare si usi de corp (vezi si FURNITURE_CASEWORK_DRAWERS.md)

**FUR-DRW-001 — Glisiere cu bile**: joc **12,7 mm pe fiecare latura** (latura 12,7 mm = 1/2") intre corp si sertar; sertar mai ingust cu 25,4 mm decat golul `[WEB: sistem 32 mm / cupe 35 mm; valoarea 12,7 mm din fise producatori — DE VERIFICAT pe model]`.
**FUR-DRW-002 — Cupa de balama**: gaura Ø35 mm; **distanta de gaurire (boring) 3 mm** la usi cu face-frame/overlay complet si **5 mm** la corp fara rama (frameless) si la usi incastrate; trei reglaje (lateral/inaltime/adancime). SOURCE: `[WEB]` (producatori: Blum, Salice, Grass, Hettich).
**FUR-DRW-003 — Numar de balamale** dupa inaltimea/greutatea usii (ex. Blum: tabel pe inaltime si greutate); pentru usa mare de corp: 2 pana la ~900 mm, 3 pana la ~1600, 4 peste, aprox. `[ESTIMARE]` — DE VERIFICAT cu tabelul producatorului.
**FUR-DRW-004 — Sistemul 32 mm**: randuri de gauri Ø5 mm la **32 mm** distanta, la 37 mm de fata; compatibil bolturi de raft si cupe; baza pentru productie CNC. SOURCE: `[PRACTICA]` (industrie europeana).

## 6. Ce lipseste inca (de cercetat in iteratiile urmatoare)
1. Scari (trepte/limoane, dimensiuni EN/NP, balustrade).
2. Mobilier curbat (lamelat/aburit): raze minime, lamele.
3. Lacuri/finisaje per componenta exterioara (usi/ferestre) — FIN-ext.
4. Tabele de sectiuni pentru mobilier mare (dulapuri > 2,4 m, mese > 3 m) cu verificare structurala.
5. Bucatarie lemn masiv: legatura cu skill-ul local `bucatarie-lemn-masiv` / `configurator-bucatarie`.
