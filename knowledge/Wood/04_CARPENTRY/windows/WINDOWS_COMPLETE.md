# FERESTRE DIN LEMN — cercetare completa (iteratia 9) — SUBIECT NOU (lipsea din baza)

Conventii: vezi `00_INDEX/00_README_START_HERE.md`. `[WEB]` = rezumat de cautare web (pagina integrala neverificata; URL-uri in `99_REFERENCE/sources/SOURCES_WEB_ITER9.md`). `[ESTIMARE]` = calcul propriu. `[REF]`/`[PRACTICA]` = cunostinte de meserie fara pagina verificata.
**Atentie**: o fereastra exterioara este un produs sub marcaj CE (EN 14351-1) — la comercializare se cer incercari/declaratie de performanta; pentru uz propriu valorile de mai jos sunt reguli de proiectare, nu certificare.

## 1. Tipologie

| Tip | Deschidere | Observatii pentru proiectare |
|---|---|---|
| Fixa (fix light) | — | doar toc + geam + bead; stabilitate la vant, fara feronerie |
| Batanta (casement) | 1–2 canate, balamale laterale | simpla; canatul "bate" in toc |
| **Oscilo-batanta (tilt & turn)** | 2 pozitii (batant / rabatabil sus) | standardul european; foloseste feronerie in **canal Euro (16 mm)** |
| Ghilotina (sash, vertical) | culisare verticala, contragreutati/arcuri | cutie de toc; lemn vizibil, restaurari |
| Culisanta (sliding / lift-slide) | orizontal | usa balcon, canate grele (cai) |
| Pivotanta / basculanta (awning, hopper, pivot) | ax orizontal/vertical | luminatoare, mansarde |
| Fereastra de acoperis | pivot | produs integrat (rama + tabla); nu se proiecteaza aici |
| Usa balcon / French | batanta/oscilo-batanta 2 canate | pragul = zona critica la apa |

## 2. Sisteme de profil (sectiune toc/canat)

**WIN-PRF-001 — Sistemul IV ("Isolier-Verglasung") si grosimea profilului**: IV68 → 68 mm (latime vizibila ~80 mm), pentru geam dublu cu strat 14–20 mm argon; **IV78** → 78 mm, pentru geam triplu (2 × 12–16 mm gaz); **IV92** → 92 mm, masiv, geam pana la 48 mm grosime. Specii uzuale: pin, meranti, stejar (si molid/larice). SOURCE: `[WEB]` (producatori de ferestre).
**WIN-PRF-002 — Lamelat in 3 straturi**: profilele moderne se fac din lamele lipite (miez + doua straturi exterioare), cu fibra rotita ca sa anuleze deformarea → stabilitate dimensionala superioara fata de lemn masiv. SOURCE: `[WEB]` + WH 2021 (miscare) — principiul din `05_WOOD_SELECTION/moisture`.
**WIN-PRF-003 — Picurator, etansare triplă**: profilele IV78 au picurator, 3 garnituri si sipca de prindere profilata. SOURCE: `[WEB]`.
**WIN-PRF-004 — Alegerea profilului dupa geam**:
| Geam | Grosime unitate | Profil minim | Ug orientativ |
|---|---|---|---|
| Dublu 4-16-4 (argon, low-e) | 24 mm | IV68 | ~1,0–1,1 W/m²K `[ESTIMARE]` |
| Triplu 4-12-4-12-4 | 36 mm | IV78 | **0,7** W/m²K `[WEB]` |
| Triplu gros / securizat / fonic | 44–48 mm | IV92 | 0,5–0,7 `[ESTIMARE]` |
Distantier "warm edge" (plastic/inox subtire) in loc de aluminiu: reduce puntea termica `[WEB]`.

## 3. Canalul Euro si feronerie (oscilo-batanta)

**WIN-HW-001 — Canalul Euro (Eurofalz) pentru lemn**: canal de **16 mm**, **axa feroneriei 9 mm** (alternativ 13 mm), **adancimea falt 20 mm** (alt. 24 mm), **joc de falt (Falzluft) 12 mm**; in profile lemn tipic upstand 18 mm, backset 15 mm. SOURCE: `[WEB]` (Roto NX, Siegenia Titan AF, Maco).
**WIN-HW-002 — Greutate maxima canat**: pana la 150 kg cu feronerie standard; pana la 300 kg cu balamale grele (Roto NT Power Hinge). SOURCE: `[WEB]`.
**WIN-HW-003 — Elemente**: balama de colt (sus/jos pe toc), foarfeca (stay), mecanism de oscilare, broasca de colt, cremone (inchideri), clanta cu bloc de siguranta; toate se frezeaza in canalul Euro si se aleg din catalogul producatorului dupa **greutate si dimensiune de canat**. SOURCE: `[WEB]`.
**WIN-HW-004 — DE VERIFICAT (O-01)**: interpretarea exacta a "Falzluft 12 mm" pentru dimensionarea canatului (castigul dimensional canat ↔ toc) — se citeste din tabelul de dimensiuni al producatorului de feronerie (FFB/RFB). Pluginul va avea parametru `falzluft` (implicit 12) si NU il va folosi ca cota structurala pana la verificare.
**WIN-HW-005 — Limite practice de canat** `[ESTIMARE]`: latime 400–1400 mm, inaltime 400–2400 mm (limitat de greutate si de catalogul feroneriei); peste ~1,4 m lat se trece la 2 canate cu montant mobil sau fix.

## 4. Imbinari

**WIN-JNT-001 — Colt de canat/toc**: **cep-mortaiza trecut cu haunch** (through haunched M&T); haunch = **1/3 din lungimea cepului** si **1/6 din latimea cepului** in adancime; cep = **1/3 din grosimea** lemnului. SOURCE: `[WEB]` (reguli M&T) + JOINERY_MORTISE_TENON.
**WIN-JNT-002 — Dublu cep**: adancimea unei imbinari de fereastra e mica in comparatie cu o usa → **dublu cep** distribuie sarcina mai bine. SOURCE: `[WEB]`.
**WIN-JNT-003 — Rezistenta**: intr-un studiu pe ferestre, imbinarea cep-mortaiza a atins 344 N·m la tractiune si 325 N·m la compresiune, superior imbinarilor cu dibluri. SOURCE: `[WEB]` (BioResources 12(2), Podlena et al.). Valorile depind de specie/adeziv — DE VERIFICAT inainte de citare.
**WIN-JNT-004 — Profil**: profilul interior (falt de geam, canal Euro, profil sticking/cope) se executa pe frezele de profil; la imbinarea in unghi drept se foloseste **cope & stick** (contraprofil) sau taietura de profil + cep `[REF]`.
**WIN-JNT-005 — Cornuri (horns)**: la canat, montantii depasesc cu 30–50 mm pentru protectie la fasonare si se taie dupa montare (ghilotina: cornurile raman ca element de design) `[PRACTICA]`.
**WIN-JNT-006 — Traversa/montant mobil (mullion/transom)**: imbinare in T cu cep in toc si profil copiat (scribed) pe canat `[REF]`.
**WIN-JNT-007 — Lipire**: adeziv D4 (EN 204) rezistent la apa la exterior; nu se foloseste PVAc D3 la ferestre exterioare (vezi MATERIALS_ADHESIVES) `[V-STD]`.

## 5. Geam si prindere

**WIN-GLZ-001 — Unitati**: dublu 4-16-4 = 24 mm; triplu 4-12-4-12-4 = 36 mm `[WEB]`; argon ~90 % in cavitate; distantier warm edge `[WEB]`.
**WIN-GLZ-002 — Greutate geam** `[ESTIMARE]`: ~2,5 kg/m² pe mm de sticla → dublu 4+4 ≈ 20 kg/m²; triplu 4+4+4 ≈ 30 kg/m²; canat 1,0 × 1,4 m dublu ≈ 28 kg sticla + lemn.
**WIN-GLZ-003 — Falt de geam** `[PRACTICA]`: adancime falt 18–20 mm; joc sticla–falt 3–5 mm perimetral; **calarasi (setting blocks)** pe cantul de jos la 1/4 din latime, nu in colt; geam sprijinit pe jos si pe latura de balama; garnitura/sigilant intre sticla si falt; sipca (bead) 15–20 mm fixata cu cuie fine/suruburi, demontabila.
**WIN-GLZ-004 — Siguranta** `[REF]`: geam securizat/laminat in canate de usa si la geamuri joase (< ~800 mm de la pardoseala, dupa codul national) — EN 12600; verifica reglementarea locala.

## 6. Apa, aer, montaj

**WIN-WTR-001 — Pervazul exterior**: panta **≥ 5–10°**; **picurator** — canal ~**12 mm (1/2")** de muchia exterioara, care rupe firul de apa. SOURCE: `[WEB]`.
**WIN-WTR-002 — Drenaj si garnituri**: orificii de drenaj in falt/prag (2–3 pe latimea ferestrei), garnitura mediana + exterioara; etansare in 3 trepte la IV78 `[WEB]` + `[PRACTICA]`.
**WIN-INS-001 — Montaj**: bandă de etansare la aer pe interior, bandă permeabila la vapori pe exterior, **joc de montaj 10–15 mm** umplut cu spuma/izolatie, fixare mecanica la ≤ ~150 mm de colturi si ≤ ~700 mm intre ele, calarasi sub montanti `[PRACTICA]` (RAL-Montage) — ghidurile producatorilor de benzi (ex. pro clima) confirma banda pe pervaz si 10–15 cm pe laterale `[WEB]`.
**WIN-INS-002 — Montaj "in planul izolatiei"** (cald) reduce puntile termice vs. montaj in grosimea zidului `[REF]`.

## 7. Performanta si standarde

**WIN-STD-001 — EN 14351-1**: standard de produs armonizat; declaratie de performanta cu: permeabilitate la aer (EN 12207, **4 clase**), etanseitate la apa (EN 12208, **9 clase**), rezistenta la vant (EN 12210), transmitanta termica, acustica `[WEB]`.
**WIN-STD-002 — Calcul Uw** `[ESTIMARE]` (EN ISO 10077-1):
```
Uw = (Ag·Ug + Af·Uf + lg·Ψg) / (Ag + Af)
```
Exemplu: fereastra 1200×1400 (Aw = 1,68 m²), ramă 30 % (Af = 0,50 m²), Ug = 0,7, Uf = 1,2, lg = 4,4 m, Ψ = 0,04 → Uw ≈ (0,82 + 0,60 + 0,18)/1,68 = **0,95 W/m²K**.
**WIN-STD-003 — Cerinte nZEB Romania**: valorile maxime Uw/Ur pentru ferestre sunt in C107/2005 (cu modificari) si Ord. 2641/2017 `[WEB: exista; valoarea exacta NU confirmata]` — **DE VERIFICAT (O-02)** inainte de a seta o limita de validare in plugin.
**WIN-STD-004 — Lemn pentru tamplarie**: EN 942 (clase J2…J50 dupa aspect, specii, UM, defecte) `[WEB]`; EN 350 durabilitate: **meranti 3–4**, **molid 4–5**, stejar 2, larice 3–4, pin 3–4 (alburn mai putin) `[WEB]` (meranti/molid) + `[REF]` (restul); pentru clasa de utilizare 3 (exterior neacoperit) se cer specii durabile sau tratament + finisaj `[REF]`. DE VERIFICAT in EN 350 / EN 335.

## 8. Fereastra ghilotina (sash window) — constructie

**WIN-SSH-001**: doua canate culisante vertical in cutie de toc (montanti dubli + contragreutati sau arcuri); piese: stile, traversa de intalnire (meeting rail), bare de geam (glazing bars), cornuri, parazapada, saibe/sfori. Imbinari: cep-mortaiza simplu haunched; traversa de intalnire cu falt de inchidere. SOURCE: `[WEB]` (sinteza) — detalii dimensionale **DE VERIFICAT** (nu au fost citite din sursa primara).

## 9. Fisa de parametri pentru `WindowDefinition` (plugin)

| Parametru | Implicit | Interval | Legatura |
|---|---|---|---|
| latime / inaltime toc (exterior) | 1200 / 1400 | 400–3000 | |
| tip | oscilo-batanta, 1 canat | fixa/batanta/oscilo/2 canate | WIN tip |
| profil | IV78 | IV68/IV78/IV92 | WIN-PRF-001/004 |
| specie | pin (lamelat) | pin/larice/stejar/meranti/molid | WIN-STD-004 |
| geam | triplu 4-12-4-12-4 | dublu/triplu/custom | WIN-GLZ-001 |
| imbinare colt | cep-mortaiza trecut cu haunch | cep dublu, dibluri (limitat), cope&stick | alegere utilizator |
| canal Euro | 16 mm, ax 9 mm, falt 20 mm | | WIN-HW-001 |
| montant mobil / traversa | optional | | WIN-JNT-006 |
| inclinatie pervaz | 10° | 5–15° | WIN-WTR-001 |

Piese generate: toc (2 montanti + sus + jos), canat (2 stile + 2 traverse), geam (primitiva, nu din lemn), sipci (4), pervaz, optional mullion; operatii: profil falt de geam, canal Euro, mortase/cepuri cu haunch, orificii feronerie, drenaj, picurator; BOM: sticla, garnituri, feronerie, sigilant, benzi de montaj. Validari: greutate canat vs feronerie, grosime geam vs profil, dimensiune canat vs limite, Uw calculat (dupa verificarea O-02), clasa durabilitate vs expunere.

## Lista de verificare ramasa (de inchis inainte de productie)
| ID | Subiect | De unde se verifica |
|---|---|---|
| O-01 | Cota canat ↔ toc (Falzluft 12 / ax 9 / falt 20) | catalog feronerie (Roto/Maco/Siegenia) |
| O-02 | Uw/Ur maxime in Romania (nZEB) | C107/2005 + Ord. 2641/2017 |
| O-03 | Clase de durabilitate EN 350 / EN 335 | standarde (acces platit) |
| O-04 | Valori de rezistenta imbinare (BioRes) | articolul integral |
| O-05 | Dimensiuni ghilotina | Fairham / Wells & Hooper (cap. "Sash and frame") |
