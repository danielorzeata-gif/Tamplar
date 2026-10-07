# Changelog

## 1.2.0
- **Fără fantome**: „Generează” dintr-o cameră scrie acum toată camera (înainte scria doar piesa activă, iar restul rămânea ca previzualizare dublată); la generare și la o piesă/cameră nouă se șterg obiectele rămase de la proiectele vechi (Ctrl+Z le readuce); comandă nouă `WoodClean`.
- **Dulap** (`casework.wardrobe`): uși suprapuse cu balamale cu cupă 35 (2/3/4 după înălțimea ușii), bară de haine la 1520–1770 mm, polițe fixe deasupra barei, spate HDF, picioare reglabile, kit anti-basculare; mâner vertical, push-to-open sau canal pe muchia liberă.
- **Etajeră** (`casework.shelving`): montanți + polițe în canale (adâncime 1/3, max 12 mm), spate HDF; verificare de săgeată a rafturilor (L/300 sub 0,65 kg/dm³).
- **Băncuță** (`casework.bench`): construcția mesei cu proporții de bancă (înălțime 400–500, implicit 450).
- Reguli noi: `SHELF_DEFLECTION` (FUR-SHF), `SPECIES_MIX` (MIX-003) și MIX-001/005/011 în catalog; avertismentul de stabilitate devine informare când kitul anti-basculare e inclus; ușile pe balamale nu mai primesc „panou fără fixări”.
- Camera de dormitor poate include și un dulap (cu rotație 270°, fața spre pat).
- **Comodă** (`casework.dresser`): coloane × sertare parametrice (implicit 2×4), fronturi gradate (197/174/151/128), separatoare, 6 picioare (colțuri + sub separator), glisiere soft-close, spate și funduri HDF, kit anti-basculare adăugat automat când se depășesc pragurile EN 14749 (R10).
- **Pat** (`casework.bed`): picioare tăblie/capăt, traverse, lonjeroane demontabile cu bulon M8 ascuns (piuliță cilindrică + dop Ø20, găuri generate), tăblie cu panou plutitor în canale, rigle, șipci pe cant (număr după lungimea saltelei), grindă și picior central; structura ascunsă (clasa C) din pin la STANDARD/ECONOMA, stejar la PREMIUM.
- **Deschiderea sertarelor** (set Aspect al camerei): scobitură, mâner 128, push-to-open, falț J; aceeași în toată camera.
- **Cameră de dormitor** (`WoodBedroom`, tab Cameră): pat + 2 noptiere + comodă; o variantă, o deschidere de sertare și o esență pentru toate piesele; piesele sunt așezate în cameră (rotații în multipli de 90°); debitare comună cu rezerva aplicată o dată; declarația de specii pe clase (A/B/C) în ofertă; camera se salvează în documentul Rhino.
- Fișa tehnică: lista de materiale se strânge automat la piesele cu multe familii.
- **Piesă nouă: Noptieră** (`casework.nightstand`, din fișa dormitor): 4 picioare, 2 laterale, fund, poliță de nișă, capac, sertar cu față suprapusă și cutie, spate și fund de sertar din HDF (preț pe m²). Panourile late se încleiază din scânduri cu lamele; îmbinările de corp sunt dibluri (sau lamele), picioarele pe dibluri Ø10; glisieră soft-close și patine reglabile în BOM.
- **Clase de vizibilitate A/B/C**: piesele vizibile închis (față, laterale, capac, picioare) rămân din esența proiectului; interiorul (fund, poliță, cutia sertarului) urmează varianta: PREMIUM stejar, STANDARD frasin, ECONOMA molid (alegere „Esență interior”, modificabilă).
- `WoodNew` / tab Proiect → „Piesă nouă”: alegi tipul (masă / noptieră).
- Verificările de fibră: avertismentul „fibre încrucișate” apare doar când o piesă se mișcă pe linia de lipire iar cealaltă nu; „panou fără fixări” nu mai apare la panourile prinse prin îmbinări.
- **Fixări blat pe zargile scurte**: numărul crește cu dimensiunea mesei (aceeași regulă de pas ca la zargile lungi), mereu impar, cu un șurub pe axa centrală, distribuite simetric; șuruburile îndepărtate de centru primesc gaură alungită mai lungă (`TOP-SLOTSCREW-L`, cursă 26 mm).
- Soluții aplicabile și pentru avertismentul „cursa fixării” (schimbă fixarea blatului cu una care permite mișcarea), nu doar pentru mortaze.
- Baza de cunoștințe: `03_FURNITURE/bedroom/FISA_DORMITOR.md` (clase de vizibilitate A/B/C, reguli MIX, niveluri PREMIUM/STANDARD/ESENȚIAL) și secțiunile de fronturi de comodă.
- **Blatul se vede din scânduri**: în modul Normal și peste, blatul încleiat e desenat ca scânduri separate (fiecare solid propriu, cu fibra pe scândură).
- **Lamele (biscuiți) între scânduri**: #20 (56×23×4) de la 20 mm grosime, #10 sub; centrate în grosime (2 rânduri de la 45 mm); primele la 60 mm de capete, distribuite uniform la cel mult „Pas lamele” (200 mm implicit, parametru nou). Numărul și poziția se recalculează la orice schimbare de lungime/lățime/grosime/pas. Canalele sunt decupate boolean în ambele scânduri, lamelele apar ca solide ovale 3D (Inginerie+), intră în BOM/cost și în notele de montaj.
- Exportul de piese: fiecare scândură de blat e o piesă separată; lamelele au layerul `Debitare::04 Lamele`.
- Avertismentul „mortaze care se intersectează” are acum **soluții aplicabile**: fiecare variantă e încercată pe proiect, ordonată după strategia de optimizare (cost / achiziție / deșeu / nr. bare) și apoi după rezistență; prima e marcată ★ Recomandat, cu buton „Aplică soluția”.
- Piesele sunt **decupate boolean în 3D** (mortaze, găuri, obrajii și umerii cepurilor) în modurile Normal și peste.
- `WoodCutParts` / buton „Exportă piese de debitare (3D)”: layere `Debitare::01 Piesă brută`, `02 După rindeluire`, `03 Debitare` (piesa cu găuri + deșeul în roșu), așezate plat lângă model.
- `WoodStart`: fereastră cu taburi independente. Primul tab **Configurare** = interfața Atelier; celelalte (Proiect, Afișare, Optimizare, Suprascrieri, Fișă tehnică, Documente, Verificări, Setări, Materiale, Despre) înlocuiesc comenzile din linia de comandă (care rămân disponibile).

## 1.1.0 (unreleased)
- Selectable joints per connection (mortise-tenon, loose tenon, dowel, bridle, Japanese kusabi, biscuit, pocket screw) and tabletop
  fixing (Z-clip, figure-8, wooden button); rail lengths, tenons, holes and hardware follow the choice; choices persist.
- `JointInfo` (strength, difficulty, pros/cons, source) for every joint; THROUGH_CONFLICT validation.
- `PreviewEngine` (2D oblique preview from the Core model); 83 tests.
- Room workspace: `Workspace/Room/PieceEntry/StyleSet` (Aspect + Structure sets per room), field provenance (standard / set / piece override), "only here" vs "whole set" edits, re-link, conflicts reported instead of silently applied; one global optimization per room (never worse than separate plans) and workspace persistence.
- Optimizer: reserve is now part of candidate comparison and is covered by the cheapest commercial option (fixes tighter plans being penalised).
- `RuleCatalog` R1-R19 with confidence levels (safe / provisional / conditional / typology-only / forbidden) + tested helpers (EN 14749/12521 stability, finger traps, panel movement, chair capacities, tenon equation E1 with domain check); stability validation rule.
- Species data replaced by verified DIN 68364/68100 values (`DataLabel` [V-DATA]/[UNVERIFIED]); Janka removed (R17); default moisture swing 5 points.
- `knowledge/Wood/13_DESIGN_AND_ENGINEERING/RULES_EN_PEER_REVIEW_2026.md` added; `docs/UI_SPEC.md`.
- PDF sheets in the Atelier style (Romanian, diacritics): technical sheet (live preview), joint dimensions, cutting plan + cut list, order & cost, assembly notes; SALE mode (offer only, no internal costs/waste/stock codes); `WoodPdf`, `WoodSheet` commands and a "Fișă tehnică" panel.
- Atelier panel (Eto): tokens, controls and the full panel with ECONOMA/STANDARD/PREMIUM variants, draft + viewport preview before generation; modal new-project dialog removed; DEPOZIT_LEMN replaced by "Necesar lucrare".
- `knowledge/Wood`: imported knowledge base + iteration 9 research on doors, windows, furniture matrix, hardware; `docs/ROADMAP_V2.md`.

## 1.0.0
First complete release (roadmap V1: core + parametric table + material optimization).

- Project model, dependency graph (pull-based, early cut-off, per-node evaluation counters), numeric manual overrides
  (Add/Replace, Recalculate / Keep / Convert-to-custom resolutions).
- Materials: 10 species + custom species, commercial stock catalogue, rule-driven manufacturing allowances.
- Part families (identical parts are one procurement problem), rough pieces, glued panels (top strips).
- Joinery: mortise & tenon (mitred when mortises collide), loose tenon, dowel, biscuit, dovetail, finger, box, dado, rabbet,
  half-lap, bridle, scarf, Japanese wedged through tenon (kusabi), custom template joints.
- Holes/features as first-class data; hardware installers that generate holes/slots/pockets (Z-clips, slotted-hole screws).
- Global stock optimizer (multi-policy packing + merge/empty/retype improvement, 6 strategies), remnant reuse, kerf, reserve
  applied once after optimization, purchase lines, explanation text.
- Manufacturing operations, full traceability (part → purchase → board → cut → operations → joinery/hardware → assembly).
- BOM, cut list, procurement list, cost engine, validation (INFO/WARNING/ERROR), documentation export (CSV, SVG, HTML, MD).
- Persistence (JSON project file + single string in the .3dm), project reopening with snapshot verification.
- Rhino 8 plugin: commands, lightweight geometry sync with LOD display modes, manual-edit protection, dockable panel.
