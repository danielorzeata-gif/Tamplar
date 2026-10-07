# PROJECTS_INDEX

| Proiect | Fisa principala | Imbinari | Specii sugerate |
|---|---|---|---|
| Pat din lemn masiv (demontabil) | [FURNITURE_BED_SOLID_WOOD](../03_FURNITURE/beds/FURNITURE_BED_SOLID_WOOD.md) | bulon + cep de aliniere, tusk, hanasen, M&T tablie | stejar, frasin, pin/brad (sectiuni marite) |
| Masa de dining | [FURNITURE_TABLES_CHAIRS_CASES](../03_FURNITURE/furniture_construction/FURNITURE_TABLES_CHAIRS_CASES.md) | M&T haunched, cepuri mitred, buttons, breadboard | stejar, nuc, frasin |
| Scaun | idem | M&T lung, interlocking chair joint (Fairham Fig. 189) | frasin, fag, stejar |
| Dulap / carcasa | idem | dovetail, dovetail housing, rama + panou | stejar, cires |
| Sertar | idem + JOINERY_DOVETAIL | half-blind + through dovetail | stejar (laterale), fata din specia mobilierului |
| Biblioteca / rafturi | idem (FURN-SH) + JOINERY_HOUSING | stopped dado/dovetail housing | stejar, frasin, placaj |
| Usa cu panouri | [CARPENTRY_FRAMES_DOORS_ROOFS](../04_CARPENTRY/CARPENTRY_FRAMES_DOORS_ROOFS.md) | M&T haunched, pene | stejar, larice, molid |
| Usa de lambriu cu contrafise | idem | T&G, contrafise | molid, pin |
| Banc de tamplar | JOINERY_MORTISE_TENON (Fairham Fig. 157) | cep coada de randunica + pana, drawbore | fag, frasin, molid laminat |
| Ferma de acoperis cu pop | CARPENTRY (CARP-RF-001) | M&T, tusk, scarf | molid/brad C24 (calcul EC5!) |
| Rama de tablou / oglinda | JOINERY_SPLINE_MITRE, JOINERY_LAP | mitre cu chei, mitred halving | orice |
| Cutie | JOINERY_DOVETAIL, JOINERY_FINGER_BOX | dovetail, box, tongued mitre | nuc, cires, kiri |
| Shoji | JP_SHOJI_KUMIKO_SASHIMONO | ai-gaki, M&T | hinoki/molid de rezonanta |

## Proiecte istorice cu liste de materiale verificate (iteratia 3) — *Mission Furniture* 1909–1912 `[V-TEXT]`
| ID | Proiect | Fisa | Imbinari cheie |
|---|---|---|---|
| PRJ-P1_LIBRARY_TABLE | Masa de biblioteca cu raft impanat | [tables](../03_FURNITURE/tables/FURNITURE_PROJECTS_TABLES.md) | keyed tenons cu joc 3 mm/parte, butoni |
| PRJ-P3_LIBRARY_TABLE | Masa de biblioteca cu sertare | idem | M&T, blat diblat |
| PRJ-P3_OAK_TABLE | Masa patrata de stejar | idem | M&T sau dibluri (traverse −51 mm) |
| PRJ-P3_EXTENSION_TABLE | Masa rotunda extensibila | idem | picioare furniruite, glisiere artar |
| PRJ-P3_PIANO_BENCH | Banca (nuc) | idem | sipci ingropate, finisaj ulei+shellac+ceara |
| PRJ-P3_SIDE_CHAIR | Scaun de dining | [chairs](../03_FURNITURE/chairs/FURNITURE_PROJECTS_SEATING.md) | M&T, sipci ingropate |
| PRJ-P3_ARM_DINING_CHAIR | Scaun cu brate | idem | M&T, coltare |
| PRJ-P1_MORRIS_CHAIR | Fotoliu Morris (spatar reglabil) | idem | cepuri trecute 5 mm |
| PRJ-P3_SETTEE | Canapea 2,2 m | idem | cuie de lemn demontabile, fara adeziv |
| PRJ-P2_COUCH | Sofa-pat 2,1 m — **analog de pat** | idem | coltare diagonale, sipci pe cleats |
| PRJ-P3_WARDROBE | Dulap de haine | [cabinets](../03_FURNITURE/cabinets/FURNITURE_PROJECTS_CASEWORK.md) | M&T + panouri in nut |
| PRJ-P2_BUFFET | Bufet cu oglinda | idem | rame+panou, dibluri, cuie de lemn |
| PRJ-P3_SIDEBOARD | Servanta | idem | sertare din plop |
Reguli de ebenisterie pentru corpuri si sertare (Wells & Hooper): [FURNITURE_CASEWORK_DRAWERS](../03_FURNITURE/cabinets/FURNITURE_CASEWORK_DRAWERS.md) · reguli pentru mese extensibile: TBL-WH-001…009 in [tables](../03_FURNITURE/tables/FURNITURE_PROJECTS_TABLES.md)
Sinteza proportiilor: [FURNITURE_PROPORTIONS_HISTORICAL](../03_FURNITURE/furniture_construction/FURNITURE_PROPORTIONS_HISTORICAL.md)

## Proiecte parametrice (WOOD_ENGINE)
| Proiect | Script | Imbinari | Rezultate |
|---|---|---|---|
| Pat Montessori casuta 1400x2000, stejar | [pat_montessori.py](../WOOD_ENGINE/projects/pat_montessori/pat_montessori.py) | KD-BB-01, ST-LOOSE-01, HL-PEG-01, RS-PEG-01 | [out/](../WOOD_ENGINE/projects/pat_montessori/out/) |
| Masa de dining 1800x900x750, stejar | [masa_dining.py](../WOOD_ENGINE/projects/masa_dining/masa_dining.py) | MT-GL-01, BTN-01 | [out/](../WOOD_ENGINE/projects/masa_dining/out/) |
