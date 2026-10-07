# USI DIN LEMN — cercetare completa (iteratia 9)

Extinde `../CARPENTRY_FRAMES_DOORS_ROOFS.md` (CARP-DR-001…004). Conventii de sursa: vezi `00_INDEX/00_README_START_HERE.md`.
Eticheta noua **`[WEB]`** = fapt gasit in rezumatul unei cautari web (URL in `99_REFERENCE/sources/SOURCES_WEB_ITER9.md`), pagina integrala **neverificata** (domeniile mari sunt blocate pentru descarcare in mediul de lucru). `[ESTIMARE]` = calcul propriu din formule/valori ale bazei.

## 1. Tipologie si ce alegem cand

| Tip | Constructie | Cand | Imbinare principala | Risc de miscare |
|---|---|---|---|---|
| **Usa cu panouri (frame & panel)** | montanti + traverse + panouri plutitoare in canal | interior/exterior clasic, dulapuri | M&T haunched; haunch = canalul panoului (DOOR-PRO-004) | panoul se misca pe latime; ramele nu |
| **Usa plina / plata (flush)** | schelet + 2 placi (placaj/MDF) sau fagure | interior economic | dibluri/cuie, lipire | stabil; fara lemn masiv |
| **Usa de lambriu (ledged & braced)** | scanduri T&G pe 2–3 traverse (ledges) + contrafisa | magazii, pivnite, rustic | T&G + suruburi/cuie; contrafisa urca spre balama (CARP-DR-002) | latimea totala se misca: scanduri nelipite |
| **Usa lamelata (laminated / engineered)** | 3 straturi de lamele lipite, miez stabil | exterior, usi mari | profil ca la ferestre | cea mai stabila |
| **Usa cu geam (glazed)** | rama cu falt + sipca de prindere (bead) sau cu mullion | holuri, balcon, French | M&T; falt (rebate) + bead | geamul nu se lipeste in lemn |
| **Usa culisanta / barn** | panou pe sina | spatii inguste | panou M&T/T&G | agatare = greutate pe sina, nu pe cant |
| **Usa dubla (doua canate)** | doua canate + un canat fix cu zavor | deschideri > 1100 mm | idem | falt de inchidere la mijloc |
| **Usa de securitate / foc** | miez + placi, garnituri intumescente | cai de evacuare, apartament | certificat (EN 1634 / EN 1627) | NU se modifica pe santier |

## 2. Dimensiuni si goluri

**DOOR-DIM-001 — Dimensiuni nominale de canat (DIN 18101)**: latimi 610 / 735 / 860 / 985 / 1110 mm; inaltimi 1985 / 2110 mm. SOURCE: `[WEB]` (DIN 18101: usi rezidentiale, dimensiuni canat, pozitia balamalelor si a broastei).
**DOOR-DIM-002 — Piata din Romania**: in listari de retail apar des 700/800/900 × 2000/2050/2100 mm (ex. 80×200, 90×210). Toc reglabil in functie de grosimea peretelui. SOURCE: `[WEB]` (retail). DE VERIFICAT cu furnizorul inainte de comanda.
**DOOR-DIM-003 — Grosimea canatului**: interior 40–44 mm; exterior 44 mm minim, 58–68 mm pentru exterior termoizolat/lamelat. SOURCE: `[WEB]` (44 mm, montant 100 mm in detalii arhitecturale); 58–68 mm `[ESTIMARE]` (similar profilelor de ferestre).
**DOOR-DIM-004 — Jocuri (reveal) in toc**: lateral si sus **3 mm** (acceptabil 2–5 mm); balama: min **3,2 mm (1/8")** intre cantul de balama si falt. SOURCE: `[WEB]`.
**DOOR-DIM-005 — Joc jos**: interior 8–12 mm fata de pardoseala finita (ventilatie / covor) `[PRACTICA]`; exterior = pana la pragul cu garnitura/picurator, ~2 mm fata de varful barei de apa `[WEB]`.
**DOOR-DIM-006 — Gol in zidarie**: latime canat + 2×3 mm joc + 2×grosime toc (≈ 2×30) + 2×(10–15 mm) spatiu de montaj/spuma; inaltime analog; pentru toc cu falt se foloseste cota "gol de zidarie" din fisa producatorului `[ESTIMARE]`.

## 3. Proportii ale cadrului (usa cu panouri)

**DOOR-PRO-001 — Latimea montantilor**: traditional **65–90 mm (2½–3½")**; la usi mari/exterioare **100–120 mm** `[WEB]` (65–90) / `[PRACTICA]` (100–120).
**DOOR-PRO-002 — Traversa de jos mai lata**: cu **12–25 mm** fata de montanti (greutate vizuala + protectie la apa); traversa de sus uneori putin mai ingusta; traversa de mijloc ("lock rail") 150–200 mm pentru broasca `[WEB]` (12–25) / `[PRACTICA]` (lock rail).
**DOOR-PRO-003 — Canalul panoului**: latime 6–10 mm, adancime **10–13 mm (3/8"–1/2")**; la traverse de jos/sus adancimea poate fi 13 mm, la cea de mijloc 6 mm `[WEB]`.
**DOOR-PRO-004 — Cepul**: grosime **1/3** din grosimea lemnului; latimea ≈ latimea traversei minus 12 mm; **haunch = acelasi latime si adancime ca canalul**; cand montantul are canal, canalul "haunch-uieste" cepul (simplifica lucrarea) `[WEB]` + fisa JOINERY_MORTISE_TENON.
**DOOR-PRO-005 — Cornul (horn)**: montantii se taie cu +30–50 mm peste traversa pentru a proteja la fasonare/montaj, apoi se taie la masura `[PRACTICA]`; cepul trecut din traversa iese ~6 mm si se impaneaza `[V-TEXT]` (CARP-DR-001).

## 4. Panouri plutitoare si miscarea lemnului

**DOOR-MOV-001 — Dimensiunea panoului**: panou = distanta intre fundurile canalelor **minus 1,5 mm (1/16")** pe fiecare directie (minim); recomandare mai sigura cand montezi iarna la 5–6 % UM: **≥ 3 mm joc** `[WEB]`.
**DOOR-MOV-002 — Formula de proiectare** `[ESTIMARE]` (coeficienti WH 2021 din `05_WOOD_SELECTION/moisture`):
```
Δ_total = Latime_panou × C_T × ΔUM        (C_T stejar alb 0,00376 / rosu 0,00304 per 1 % UM)
joc c    = 0,5 × Δ_total + 1 mm            (pe fiecare latura)
Latime_panou = Gol_vizibil + 2 × (adancime_canal − c)
```
Exemplu: panou de stejar 300 mm, ΔUM 4 % → Δ = 300 × 0,00376 × 4 = **4,5 mm** → c = 3,3 mm; cu canal 10 mm: panou = gol + 13,4 mm.
**DOOR-MOV-003 — Panoul nu se lipeste in canal**; se centreaza cu "spaceballs"/cauciuc; se finiseaza **inainte** de asamblare (altfel apare cantul nefinisat cand lemnul se contracta) `[WEB]` (spaceballs 5/16" in sursa) + `[REF]`.
**DOOR-MOV-004 — Panourile late din lemn masiv** (> 300 mm) se fac din lamele incleiate cu fibra in aceeasi directie; la panouri foarte late se prefera placaj/MDF furniruit `[PRACTICA]`.
**DOOR-MOV-005 — Montantii stabili**: lemn uscat la 10–12 % (interior) / 12–15 % (exterior) la montaj; lamelat 3 straturi cand canatul > 2,2 m sau exterior expus `[ESTIMARE]` (WH 2021 Tab. 13–2 pentru UM la montaj).

## 5. Feronerie

**DOOR-HW-001 — Numar de balamale**: 2 pana la ~1515 mm; **3** intre 1515–2285 mm; 4 intre 2285–3050 mm; si cresterea numarului pentru canate grele/inguste `[WEB]` (tabel UK). Canat de 6–12 kg → 3 balamale optim `[WEB]`.
**DOOR-HW-002 — Pozitii (3 balamale)**: prima la **~150 mm (6")** de sus, a doua la mijloc, a treia la **~225 mm (9")** de jos `[WEB]`; alternativa: 1/6 de sus / centru / 1/6 de jos. Standard DIN 18101 fixeaza pozitia balamalelor si broastei pentru interschimbabilitate `[WEB]`.
**DOOR-HW-003 — Greutate**: stejar 720 kg/m³ → canat 2100×900×40 ≈ **54 kg** (`[ESTIMARE]`: 0,0756 m³ × 720) → minim 3 balamale cu rulment, recomandat 4 peste ~40 kg.
**DOOR-HW-004 — Broasca/clanta**: clanta la **~1000–1050 mm** de la pardoseala finita `[PRACTICA]`; backset 50–60 mm; broasca in traversa de mijloc sau in montant (nu in cep!). Foc/evacuare: yala conform certificatului.
**DOOR-HW-005 — Prag si picurator (exterior)**: baza usii cu picurator fixat si etansat pe canat, ~2 mm deasupra barei de apa; unghiul picuratorului = raza de deschidere; bara de apa tipic 60×40 mm (otel zincat) `[WEB]`. Canalul de picurare in prag ~12 mm de muchie `[WEB]` (fereastra) — acelasi principiu.

## 6. Exterior si performanta

**DOOR-EXT-001 — Specii**: stejar (durabilitate clasa 2), larice/duglas (3–4), meranti (3–4), lamelat; pin/molid doar cu protectie `[REF]` + `[WEB]` (meranti 3–4, molid 4–5).
**DOOR-EXT-002 — Standarde**: EN 942 (calitatea lemnului pentru tamplarie, clase J2…J50, UM, specii) `[WEB]`; EN 14351-1 (produs: usi/ferestre, declaratie de performanta) `[WEB]`; EN 12207 permeabilitate la aer, EN 12208 etanseitate la apa (9 clase), EN 12210 vant `[WEB]`; EN 14221 pentru usi interioare `[WEB]`; securitate EN 1627, foc EN 1634 `[REF]`.
**DOOR-EXT-003 — Etansare**: 2 garnituri (canat + toc), prag cu bara de apa, picurator; adeziv D4 la lamelat (CARP-DR-004).
**DOOR-EXT-004 — Valoare U**: pentru usi exterioare U se ia din certificat; cerinte nZEB in Romania — **DE VERIFICAT** in C107 / Ord. 2641/2017 `[WEB: exista, valoarea nu a fost confirmata]`.

## 7. Parametri pentru plugin (ce intra in `DoorDefinition`)

| Parametru | Implicit | Interval | Regula |
|---|---|---|---|
| latime canat | 860 | 610–1110 | seria DIN 18101 sau libera |
| inaltime canat | 2110 | 1985–2400 | |
| grosime | 44 | 36–68 | |
| tip | cu panouri | panouri/plata/lambriu/lamelata/vitrata | |
| nr. panouri | 2 | 1–6 | imparte golul dupa traverse |
| montant | 100 | 65–140 | DOOR-PRO-001 |
| traversa sus / mijloc / jos | 100 / 160 / 200 | | DOOR-PRO-002 |
| canal panou (lat × adanc) | 8 × 12 | | DOOR-PRO-003 |
| joc canat–toc | 3 | 2–5 | DOOR-DIM-004 |
| balamale | auto (3) | 2–4 | DOOR-HW-001/003 |
| imbinare colt | M&T haunched | M&T/haunched, dibluri, loose tenon, bridle | alegere utilizator |

Piese generate: 2 montanti, 2–3 traverse, N panouri (plutitoare, brut cu miscare), toc, optional sipci de geam; operatii: canal, mortase, cep+haunch, mortase balamale (grosime balama), broasca.
