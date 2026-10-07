# SOURCE CONFLICTS (Faza 8 — comparare surse, informatii contradictorii)

Regula: nu combinam orbeste; aratam ce spune fiecare, de ce difera, ce recomandam.

## C-01 — Grosimea cepului
| Sursa | Spune |
|---|---|
| Fairham `[V-TEXT]` | "in practically all cases" 1/3 din grosimea lemnului |
| Rogowski / Joyce `[REF]` | 1/3 ca punct de pornire; cand montantul e mai gros decat traversa, cepul poate fi mai gros (spre 1/2 din traversa) |
| Practica de atelier | se rotunjeste la latimea daltei/frezei |
**Diferenta provine din**: dimensiunile relative ale pieselor (Fairham presupune piese de aceeasi grosime, tipic rame de usi). **Recomandare**: 1/3 cand piesele au aceeasi grosime; altfel maximizeaza cepul pastrand ≥ ~1/3 din grosimea montantului in fiecare perete. Mai bine argumentat: ambele — conditii diferite.

## C-02 — Panta cozii de randunica
| Sursa | Spune |
|---|---|
| Fairham `[V-TEXT]` | 1:8 "considered correct"; niciodata mai abrupt decat 1:6; fara regula rigida |
| Joyce, Rogowski si multi autori moderni `[REF]` | 1:6 pentru rasinoase, 1:8 pentru esente tari |
| Unii autori (ex. traditia lui Sellers) `[REF]` | 1:6 universal / chiar 1:5 la rasinoase moi — DE VERIFICAT |
| Wells & Hooper 1922 `[V-TEXT]` (iteratia 4) | **1:8 la imbinari fine si vizibile (sertare, cutii), 1:6 la piese grele (corpuri, socluri, cufere)** — criteriul e tipul piesei, nu specia |
**Diferenta provine din**: specia (rasinoasele moi se strivesc → au nevoie de panta mai mare pentru blocare; esentele tari dense au risc de rupere a colturilor scurte la panta mare). **Recomandare (actualizata in iteratia 4)**: toate trei sursele verificate stau in intervalul 1:6–1:8. Doua criterii diferite, ambele argumentate: **specia** (1:6 rasinoase, 1:8 esente tari) si **tipul piesei** (1:8 sertare/cutii fine, 1:6 corpuri grele). Cand se suprapun (ex. cufar greu din pin) → 1:6; sertar din stejar → 1:8.

## C-03 — Subtaierea (undercut) la fundul cozilor
| Sursa | Spune |
|---|---|
| Fairham `[V-TEXT]` | NU — dalta aproape verticala; subtaierea slabeste lipirea si poate sparge fata |
| Multi practicieni moderni `[PRACTICA]` | un usor undercut (1–2°) in zona *interioara* ajuta imbinarea sa se inchida pe linia vizibila |
**Diferenta**: obiectiv diferit (rezistenta lipirii vs. aspect rapid). **Recomandare**: fund vertical; un undercut minim doar la interior, niciodata pe fete vizibile; nu la imbinari solicitate.

## C-04 — Ordinea: tails-first vs pins-first
Fairham si scoala clasica: pins/pini marcati de pe cozi sau invers — ambele descrise; literatura moderna: dezbatere de preferinta. **Recomandare**: indiferent; important e transferul cu cutitul si taierea pe deseu. Nu e un conflict tehnic real.

## C-05 — Orientarea inelelor la incleierea scandurilor
| Sursa | Spune |
|---|---|
| Traditie larga `[PRACTICA]` | alterneaza inimile (sus/jos) ca cuparile sa se compenseze ("valuri" mici) |
| Hoadley si alti autori `[REF]` | cu lemn bine uscat, alege dupa aspect (potrivirea fibrei); alternarea produce suprafata ondulata; prinderea corecta tine blatul plan |
| Fairham `[V-TEXT]` | atentie la contractia circumferentiala; nu asocia piese "salbatice" cu piese cu fibra blanda; fibra in aceeasi directie pentru rindeluire |
**Recomandare**: prioritate aspectului si directiei fibrei (Fairham + Hoadley), cu lemn uscat corect si latimi rezonabile ale scandurilor (< ~150 mm la fag/flatsawn).

## C-06 — Lungimea scarf-ului
Fairham da 4× inaltimea (scarf de pana cu chei) si 5× grosimea (tabled scarf) — **nu e contradictie**, sunt tipuri diferite. Scarf-ul plan lipit (EN 14080, constructii navale) 1:8–1:12 — alta familie (lipire, nu mecanic). Imbinarile japoneze echivalente au proportii proprii (de verificat in Nakahara).

## C-07 — Siguranta alimentara a finisajelor
| Sursa | Spune |
|---|---|
| Flexner `[REF]` | toate finisajele comune, complet intarite, sunt sigure |
| Producatori / reglementari UE `[V-STD]` | conformitate pe produs (EN 71-3 jucarii, Reg. 1935/2004 materiale in contact cu alimente) |
**Recomandare**: pentru suprafete in contact cu alimente foloseste produse cu declaratie de conformitate; ulei mineral/ceara la tocatoare.

## C-08 — Lipirea cu PVAc in rasinoase vs esente tari (presiune)
Valorile de presiune variaza intre producatori; Wood Handbook da intervale pe densitate. **Recomandare**: fisa tehnica a adezivului.

## C-09 — Tradiție europeana vs japoneza: rigiditate vs ductilitate
Nu e o contradictie de fapt, ci de filosofie (triangulare vs nuki). Vezi JOINERY_JAPANESE_OVERVIEW. Pentru constructii moderne in Romania decide calculul seismic (P100) — nu traditia.

## C-10 — Wood Handbook 2010 vs 2021: coeficientii de miscare (adaugat in iteratia 2)
| Sursa | Stejar rosu (N. red oak) C_T / C_R | Stejar alb C_T / C_R |
|---|---|---|
| Valori folosite in iteratia 1 (din memorie, atribuite ed. 2010 — NEVERIFICATE) | 0,00369 / 0,00158 | 0,00365 / 0,00180 |
| WH 2021 Tab. 13–5 `[V-TEXT]` | **0,00304 / 0,00137** | **0,00376 / 0,00194** |
**Explicatie**: in 2021, Tab. 13–5 este calculat consecvent din contractiile totale din Tab. 4–3 cu Ec. 13–2 (FSP = 30 %, baza 10 % UM). Verificare: stejar rosu S_T = 8,6 % → 1/(30·100/8,6 − 30 + 10) = 0,00304 ✓. **Recomandare**: se folosesc exclusiv valorile 2021; toate exemplele de calcul din baza au fost recalculate. Lectie: valorile din memorie nu inlocuiesc citirea sursei.

## C-11 — Lamba-uluc vs rost plan la lipire (adaugat in iteratia 2)
| Sursa | Spune |
|---|---|
| Fairham `[V-TEXT]` | la colturi in unghi, mitre cu lamba e "considerably stronger" decat un rost lipit simplu; cross tongues mai rezistente decat feather tongues |
| WH 2021 cap. 10 `[V-TEXT]` | la imbinari **pe cant (edge-grain)**, T&G si profilele **nu dau de regula rezistenta mai mare** decat rostul plan — avantajul e alinierea |
**Diferenta provine din**: tipul imbinarii. La mitre (fibra de capat pe fibra de capat), lamba adauga suprafata de lipire pe fibra lunga → castig real (consistent cu WH: imbinarile capat-cant au nevoie de suprafete care se intrepatrund). La incleierea blaturilor (fibra lunga pe fibra lunga) rostul plan e deja la fel de rezistent ca lemnul. **Recomandare**: la blaturi — rost plan bine rindeluit (lamele/dibluri doar pentru aliniere); la mitre — lamba/chei.

## C-12 — Presiunea de strangere (inlocuieste nota din C-08)
WH 2021 cap. 10 `[V-TEXT]`: ~0,7 MPa lemn usor → pana la ~1,7 MPa cele mai dense specii; suprafete mici, plane, bine rindeluite — si mai putin. Conflictul C-08 este **rezolvat** (fisele producatorilor raman relevante pentru adezivul concret).

## C-13 — Dibluri vs. cep-scobitura la scaune (iteratia 3)
| Sursa | Spune |
|---|---|
| Wells & Hooper 1922 `[V-TEXT]` | diblurile au inlocuit aproape complet cepurile in productie, dar M&T e folosit la lucrarile de calitate, unde se cer rezistenta si precizie |
| *Mission Furniture* `[V-TEXT]` | M&T peste tot; diblurile ca alternativa ("trebuie stranse foarte tare") |
| Fairham `[V-TEXT]` | M&T "cea mai importanta imbinare"; scaunele cer imbinarea interlocking la punctul cel mai slab |
**Diferenta provine din**: productie de serie (cost, masini de diblat) vs. durabilitate. **Recomandare**: M&T la scaune (cea mai solicitata piesa); diblurile acceptabile la mese si corpuri.

## C-14 — Inaltimea meselor si scaunelor: 1910 vs. azi (iteratia 3)
*Mission Furniture*: picioare de masa 768–778 mm, picior de scaun 470 mm + tapiterie `[V-TEXT]`; ergonomie moderna (Panero & Zelnik `[REF]`): masa 730–760 mm, sezut 430–460 mm. **Nu e o eroare a sursei** — stilul si populatia difera. **Recomandare**: foloseste proportiile constructive (sectiuni, imbinari) din sursele istorice, dar inaltimile din ergonomia actuala.

## C-15 — Rost lipit drept vs. usor concav ("sprung joint") (iteratia 4)
| Sursa | Spune |
|---|---|
| Fairham `[V-TEXT]` | piesele planate ca sa se atinga "la fiecare punct" |
| Wells & Hooper `[V-TEXT, OCR]` | rosturile stranse cu clema se rindeluiesc **usor concav** pe lungime |
**Diferenta provine din**: metoda de lipire. Fairham descrie si rostul frecat (rubbed joint, fara clema), unde contactul perfect e obligatoriu; la rosturile cu clema, concavitatea mica pune capetele (care se usuca si se contracta primele) sub presiune. **Recomandare**: rost frecat → perfect drept; rost cu clema → drept sau usor concav (fractiuni de mm), niciodata convex. (WH 2021 cere suprafete plane si paralele pentru film uniform — compatibil cu o concavitate foarte mica.)

## C-16 — "Cheia batraneasca" vs. "cheia nemteasca" (iteratia 5)
| Sursa | Spune |
|---|---|
| Muzeul Golesti (RO-3) `[V-TEXT]` | barnele "imbinate la capete in cheotori, cunoscute, in functie de tehnica de imbinare, ca «cheia batraneasca» sau «cheotoare in coada de randunica», zisa si «cheia nemteasca»" (ghilimelele din original nu sunt inchise — formularea e ambigua) |
| Analele Bucovinei (RO-4) `[V-TEXT]` | cheotori "simple, rotunde sau ciobanesti, «nemtesti» sau trapezoidale (in coada de randunica)"; barne rotunde la casele vechi, cioplite pe 2–4 fete la cele noi |
| Rezumate web (sursa primara neidentificata) | "cheia batraneasca" = barne rotunde, imbinare veche; "cheia nemteasca" = barne cioplite pe 4 fete, coada de randunica |
**Interpretare `[ESTIMARE]`**: doua familii — (1) imbinarea veche la barne rotunde/necioplite ("cheia batraneasca", cheotori rotunde/ciobanesti, "stâneste"), cu capete iesite; (2) imbinarea trapezoidala in coada de randunica la barne cioplite in patru fete ("cheia nemteasca"), cu colt drept. Ambele surse primare sunt compatibile cu aceasta lectura, dar **niciuna nu descrie geometria**. Necesita verificare in N. Cojocaru, *Casa veche de lemn din Bucovina* sau in alta monografie etnografica. **Iteratia 6**: in 1.047 de fise ETNOMON termenul "batraneasca" **nu apare** ca valoare de imbinare; apar in schimb "cheotori drepte" (91), "rotunde" (59), "coada de randunica / nemtesti" (32) — deci "cheia batraneasca" nu e terminologia muzeala curenta.

## C-17 — Termeni regionali cu sensuri diferite (iteratia 6, ETNOMON)
- **"Căţei"**: in Maramures fisele il dau ca sinonim pentru **soşi** (stalpi verticali intermediari santuiti); in Banat apare ca "cheotori in căţei" la barne de stejar. Analele Bucovinei (RO-4) mentioneaza "catei drepti" la primele case. **Interpretare `[ESTIMARE]`**: termenul desemneaza, in majoritatea zonelor, sistemul cu stalpi santuiti; in Banat poate desemna si un tip de colt — **DE VERIFICAT** pe fotografiile fiselor ETNOMON sau la muzeu.
- **Brad vs. molid**: fisele ETNOMON numesc "brad" de 567 de ori si "molid" doar de 11 ori, iar RO-3 (Muzeul Golesti) spune ca la munte s-a folosit "cel mai mult" molidul. In limbajul popular "brad" desemneaza adesea orice rasinoasa (inclusiv molidul). **Recomandare**: tratati "brad" din fisele etnografice ca "rasinoasa (brad sau molid)", nu ca *Abies alba* sigur.

## C-18 — "Cheotori drepte": imbinare care blocheaza sau nu? (iteratia 7)
- USDA/NPS `[V-TEXT]`: square notch **nu blocheaza** barnele → trebuie cuie de lemn / tije.
- ETNOMON `[V-DATA]`: "cheotori drepte" e cel mai frecvent tip numit (91), iar constructiile au rezistat sute de ani; RO-4: cuie de lemn folosite curent.
**Interpretare `[ESTIMARE]`**: fie "cheotori drepte" desemneaza o crestatura dreptunghiulara **cu pinten de blocare** (tip central-european), fie e un square notch **asigurat cu cuie de lemn**. Fotografiile (750 px) arata doar capetele dreptunghiulare iesite. **DE VERIFICAT pe teren** (ASTRA Sibiu are cele mai multe exemple).
