# WOOD — MOISTURE & MOVEMENT (umiditate, contractie, miscarea lemnului)

Sursa principala (CITITA in iteratia 2): USDA Forest Products Laboratory, *Wood Handbook — Wood as an Engineering Material*, **FPL-GTR-282 (2021)** — cap. 4 "Moisture Relations and Physical Properties of Wood" (Glass & Zelinka) si cap. 13 "Drying and Control of Moisture Content and Dimensional Changes" (Bergman). Domeniu public: https://research.fs.usda.gov/treesearch/62243 (cap. 4), /62261 (cap. 13). `[V-TEXT]` — locatia = nr. tabel / ecuatie / pagina din editia 2021.
Surse istorice: J.B. Wagner, *Seasoning of Wood* (1917) `[V-TEXT]`; I.S. Griffith, *Essentials of Woodworking* (1908), cap. X `[V-TEXT]`.

> Iteratia 2: coeficientii de miscare din iteratia 1 erau valori din memorie (editia 2010) si au fost **inlocuiti** cu Tabelul 13–5 din editia 2021. Unele valori difera semnificativ (ex. stejar rosu C_T 0,00304, nu 0,00369) — vezi SOURCE_CONFLICTS #C-10.

## Unitati de cunostinte

**WOOD-MOIST-001 — Punctul de saturatie a fibrei (FSP)**: umiditatea la care peretii celulari sunt saturati, fara apa libera in lumen. Pentru calcule, WH foloseste **30 % UM** daca FSP-ul speciei nu e cunoscut (22 % pentru cativa cedri/redwood). Sub FSP lemnul se contracta/umfla; peste FSP dimensiunile nu se schimba. SOURCE: WH 2021 cap. 4 (Ec. 4–9, "If MCfs is not known, 30% MC can be used"); cap. 13 Tab. 13–5 nota a/b `[V-TEXT]`; Wagner 1917 `[V-TEXT]`.

**WOOD-MOIST-002 — Anizotropie si gradient**: contractia longitudinala normala = **0,1–0,2 %** (verde → anhidru); lemnul de reactie si lemnul juvenil pot ajunge la **2 %** → arcuiri, rasuciri, crapaturi transversale. Contractia tangentiala ≈ 1,5–2,5 × radiala (vezi tabel). In piese mari, suprafata incepe sa se contracte inainte ca media piesei sa scada sub FSP (gradient de umiditate). Coeficientul de variatie al contractiei transversale ≈ **15 %** → contractia unei piese individuale nu se poate prezice exact, doar media. SOURCE: WH 2021 cap. 4, sect. "Longitudinal Shrinkage" si "Relationship between Moisture Content and Shrinkage", p. 4–10 `[V-TEXT]`; Fairham `[V-TEXT]`.

**WOOD-MOIST-003 — Umiditatea de echilibru (EMC)** — extras din WH 2021 Tabel 4–2 `[V-TEXT]` (UM % la temperatura data):
| RH → | 20 % | 30 % | 40 % | 50 % | 60 % | 65 % | 70 % | 80 % | 90 % |
|---|---|---|---|---|---|---|---|---|---|
| 4,4 °C | 4,6 | 6,3 | 7,9 | 9,5 | 11,3 | 12,3 | 13,5 | 16,5 | 21,0 |
| 21,1 °C | 4,5 | 6,2 | 7,7 | 9,2 | 11,0 | 12,0 | 13,1 | 16,0 | 20,5 |
| 26,7 °C | 4,4 | 6,1 | 7,6 | 9,1 | 10,8 | 11,7 | 12,9 | 15,7 | 20,2 |
Note WH: precizia de 0,1 % din tabel e doar ilustrativa; valorile au fost derivate in principal pentru molid Sitka; spatiile climatizate (45–55 % RH) dau ~8–10 % UM; iarna, cu incalzire, poate cobori la ~4 % (nordul SUA). SOURCE: WH 2021 cap. 4 p. 4–5/4–6; cap. 10 p. 10–14 `[V-TEXT]`.
Aplicare Romania `[ESTIMARE]`: locuinta incalzita iarna (RH 30–40 %) → 6–8 %; vara (RH 55–65 %) → 10–12 % → **variatie sezoniera tipica ~4 puncte procentuale** in interior.

**WOOD-MOIST-004 — Umiditatea la montaj (recomandari)**
WH 2021 Tabel 13–2 `[V-TEXT]` (SUA, "most areas"):
| Utilizare | Medie | Piese individuale |
|---|---|---|
| Interior: tamplarie, pardoseli, mobilier, ornamente | 8 % | 6–10 % |
| Exterior: siding, ornamente, invelitori, lemn lamelat | 12 % | 9–14 % |
WH: "Interior wood MC ... is typically 6% to 12%" (cap. 16) `[V-TEXT]`; cherestea "dry" in standardul american = max 19 % `[V-TEXT]`.
Adaptare Romania `[PRACTICA]` + EN 942 `[V-STD]`:
| Utilizare | UM tinta |
|---|---|
| Mobilier interior, locuinta incalzita | 8–10 % |
| Parchet cu incalzire in pardoseala | 6–8 % |
| Tamplarie exterioara | 12–15 % |
| Structuri acoperite, neincalzite | 15–18 % (max ~20 %) |
Regula WH pentru structuri: daca la aplicarea finisajelor interioare UM-ul cadrului nu depaseste cu mai mult de ~5 puncte valoarea de serviciu, defectele de contractie sunt minime. SOURCE: WH 2021 cap. 13, "Framing Lumber in House Construction" `[V-TEXT]`.

**WOOD-MOIST-005 — Calculul miscarii (6–14 % UM)** — WH 2021 Ec. 13–3 `[V-TEXT]`:
`ΔD = D_I × C × (M_F − M_I)` — D_I dimensiunea initiala, C = C_T (tangential, flatsawn) sau C_R (radial, quartersawn), M_F/M_I umiditatea finala/initiala (%). Rezultat negativ = contractie.
Coeficientii se obtin din contractia totala S (Tab. 4–3) cu Ec. 13–2: `C = 1 / (FSP·100/S − FSP + 10)` (FSP = 30). Pentru fibra mixta/necunoscuta WH recomanda valorile tangentiale. Pentru variatii in afara 6–14 %: Ec. 13–4 (de la dimensiunea verde).
**Tabel — WH 2021 Tab. 13–5 (C_T, C_R) + Tab. 4–3 (contractie totala verde→anhidru, %)** `[V-TEXT]`:
| Specie (SUA) | Rudă europeana | C_T | C_R | S_T % | S_R % | S_vol % |
|---|---|---|---|---|---|---|
| White oak | stejar (Q. robur/petraea) — **cea mai apropiata ruda** | 0,00376 | 0,00194 | 10,5 | 5,6 | 16,3 |
| Northern red oak | stejar rosu (plantat in RO) | 0,00304 | 0,00137 | 8,6 | 4,0 | 13,7 |
| American beech | fag | 0,00431 | 0,00190 | 11,9 | 5,5 | 17,2 |
| White ash | frasin | 0,00274 | 0,00169 | 7,8 | 4,9 | 13,3 |
| Sugar maple | paltin / artar | 0,00353 | 0,00165 | 9,9 | 4,8 | 14,7 |
| Black walnut | nuc | 0,00274 | 0,00190 | 7,8 | 5,5 | 12,8 |
| Black cherry | cires | 0,00248 | 0,00126 | 7,1 | 3,7 | 11,5 |
| Yellow birch | mesteacan | 0,00338 | 0,00256 | 9,5 | 7,3 | 16,8 |
| American basswood | tei | 0,00330 | 0,00230 | 9,3 | 6,6 | 15,8 |
| Black locust | salcam (aceeasi specie, Robinia) | 0,00252 | 0,00158 | 7,2 | 4,6 | 10,2 |
| American chestnut | castan | 0,00234 | 0,00116 | 6,7 | 3,4 | 11,6 |
| Red alder | arin | 0,00256 | 0,00151 | 7,3 | 4,4 | 12,6 |
| Red spruce | molid | 0,00274 | 0,00130 | 7,8 | 3,8 | 11,8 |
| White fir | brad | 0,00245 | 0,00112 | 7,0 | 3,3 | 9,8 |
| Balsam fir | brad | 0,00241 | 0,00099 | 6,9 | 2,9 | 11,2 |
| Red pine | pin silvestru (ruda) | 0,00252 | 0,00130 | 7,2 | 3,8 | 11,3 |
| Eastern white pine | zambru / pin moale | 0,00212 | 0,00071 | 6,1 | 2,1 | 8,2 |
| Western larch | larice | 0,00323 | 0,00155 | 9,1 | 4,5 | 14,0 |
| Douglas-fir (coast) | duglas (aceeasi specie) | 0,00267 | 0,00165 | 7,6 | 4,8 | 12,4 |
Coloana "ruda europeana" = `[ESTIMARE]` a agentului: speciile europene sunt diferite botanic (exceptii: salcam si duglas = aceeasi specie), deci folositi valorile ca **ordin de marime**; pentru proiecte critice — Wagenführ *Holzatlas* / DIN 68100 `[REF]`.

Exemple `[ESTIMARE]` (UM 8 % iarna → 12 % vara, ΔM = 4):
| Piesa | Calcul | ΔD |
|---|---|---|
| Blat stejar flatsawn 800 mm | 800 × 0,00376 × 4 | **12,0 mm** |
| Acelasi, quartersawn | 800 × 0,00194 × 4 | 6,2 mm |
| Blat fag flatsawn 800 mm | 800 × 0,00431 × 4 | 13,8 mm |
| Blat cires flatsawn 800 mm | 800 × 0,00248 × 4 | 7,9 mm |
| Panou stejar 400 mm (tablie pat) | 400 × 0,00376 × 4 | 6,0 mm |
| Lonjeron brad, inaltime 170 mm | 170 × 0,00245 × 4 | 1,7 mm |
Exemplul oficial WH: scandura de white fir flat-grain 232 mm, 8 → 11 % → +1,7 mm `[V-TEXT]`.

**WOOD-MOIST-006 — Reguli de proiectare**
1. WH 2021 cap. 13 `[V-TEXT]`: elementele mari (grinzi ornamentale, cornise, stalpi de scara, mana curenta) se construiesc **din piese mici**; ancadramentele/plintele late se scobesc pe spate (hollow-backed); panourile masive se monteaza **libere sa se miste transversal**; **latimile inguste sunt preferabile**.
2. WH 2021 cap. 13 `[V-TEXT]`: la structuri grele, contractia transversala a grinzilor pe care reazema stalpi produce tasari — foloseste capete metalice de stalp sau console; la sabotii de grinda, varful grinzii secundare peste varful grinzii principale (dupa contractie se aliniaza).
3. WH 2021 cap. 10 `[V-TEXT]`: tensiunile de umiditate se minimizeaza lipind piese cu **orientare compatibila a fibrei, coeficienti mici de contractie, aceeasi specie, aceeasi umiditate**, la UM-ul de serviciu.
4. `[PRACTICA]`: nu fixa rigid o piesa lata perpendicular pe alta — blat pe zarga cu buttons / figure-8 / gauri ovale; breadboard lipit doar la mijloc.
5. `[PRACTICA]`: aclimatizare 1–3 saptamani; debitare in doua etape.
6. Alternarea inelelor la incleiere — SOURCE_CONFLICTS #C-05.

**WOOD-MOIST-007 — Uscare**: defecte (crapaturi de capat/suprafata, colaps, "case hardening"); capete sigilate. Regimurile de uscator: WH 2021 cap. 13, Tab. 13–3 `[V-TEXT]` (nesintetizate inca). Regula "1 an/inch" = `[PRACTICA]` (WH nu o formuleaza). Wells & Hooper (1922) `[V-TEXT, OCR]`: uscarea naturala e "cea mai buna si sigura, desi nu cea mai rapida"; dupa expertul T. Laslett, un bustean de stejar de 16–20 in (400–500 mm) are nevoie de **~18 luni de "prima uscare"**, iar scandurile rezultate de **cel putin 2 ani in stive** (mai mult daca e posibil) inainte de atelier; uscarea cu aer cald e folosita ca a doua etapa; uscarea in apa curgatoare scoate seva (pentru piloti). Directia contractiei e "aproape intotdeauna circumferentiala"; deformarea e maxima la debitarea tangentiala si minima la cea radiala. SOURCE: Wagner 1917 `[V-TEXT]`.

**WOOD-MOIST-008 — Masurare**: aparate rezistive (corectie de specie si temperatura) sau dielectrice (contact de suprafata); la loturi, WH recomanda masurarea a **cel putin 10 % din piese** (ex. 6 din 60 de grinzi) `[V-TEXT, Tab. 13–2 nota b]`; referinta gravimetrica EN 13183-1 `[V-STD]`.
