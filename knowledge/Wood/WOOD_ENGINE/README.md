# WOOD ENGINE — de la idee la debitare, cu deseul vizibil in rosu

**IDEE → REGULI (cu sursa) → IMBINARI (obiecte) → PIESE (brut − scule) → RHINO (ansamblu + piese + deseu rosu) → DEBITARE → OPERATII → SCULE → ASAMBLARE**

## Principiul central
Fiecare imbinare din `data/joints/` este o **functie** care produce **scule de taiere** pe piesele pe care le leaga. Din aceeasi scula rezulta:
| Ce | Cum |
|---|---|
| geometria | piesa finita = brut − uniune(scule) |
| **deseul ROSU** (`DESEURI::Imbinari`) | brut ∩ uniune(scule) — exact ce se scoate |
| **adaosul ROZ** (`DESEURI::Debitare`) | brut de debitare (scandura) − brut — rindeluire + lungime |
| operatia | fiecare scula = o operatie (scobitura, umeri cep, gaura bulon, injumatatire…) |
| unealta | fiecare operatie cere o scula; `data/tools_inventory.json` spune daca o ai |
| "De ce?" | fiecare scula poarta ID-urile regulilor din `data/rules.json` (sursa, pagina/locatie, nivel de incredere) |

## Structura
```
WOOD_ENGINE/
├── data/
│   ├── rules.json            reguli cu provenienta (legate de fisele din baza de cunostinte)
│   ├── materials.json        specii (valori Wood Handbook 2021)
│   ├── tools_inventory.json  INVENTARUL TAU — completeaza status "da"/"nu"
│   └── joints/*.json         imbinari ca obiecte: parametri, reguli, operatii, scule, feronerie
├── engine/
│   ├── geom.py       primitive ca date pure (box, cyl, prism), bbox, punct-in-volum, decupare poligoane
│   ├── model.py      Part (brut + scule cu metadate), Project
│   ├── joints.py     KD-BB-01, ST-LOOSE-01, HL-PEG-01, RS-PEG-01, MT-GL-01, BTN-01 (geometrie + feronerie + verificari)
│   ├── components.py biblioteca de componente: post, rail, board, panel, inclined_member
│   ├── checks.py     coliziuni, conflicte intre imbinari, piese "in aer"
│   ├── reports.py    xlsx: Debitare, Feronerie, Operatii, Scule, Ordine asamblare, De ce (surse), Verificari
│   ├── rhino_out.py  constructia in Rhino 8 (layere, culori, user text)
│   └── preview.py    previzualizare fara Rhino (verificare vizuala)
└── projects/
    ├── pat_montessori/pat_montessori.py   PILOTUL — pat casuta 1400x2000 (demontabil, buloane ascunse)
    ├── masa_dining/masa_dining.py         masa 1800x900x750 (cepuri lipite cu haunch, capete 45°, butoni)
    └── */out/                             xlsx, csv, imagini de previzualizare
```

## Rulare
**Fara Rhino (verificare + rapoarte):**
```bash
python projects/pat_montessori/pat_montessori.py
python projects/masa_dining/masa_dining.py
```
**In Rhino 8:** `ScriptEditor` → deschide `projects/pat_montessori/pat_montessori.py` → Run (Python 3). Scriptul sterge constructia veche de pe `WOOD::<proiect>::*` si o reface — modifici un parametru, rulezi din nou.
In Rhino, selecteaza orice piesa sau deseu → `Properties > Attribute User Text`: `WOOD_piesa`, `WOOD_operatii`, `WOOD_reguli` (= raspunsul la "De ce?").

## Stare (V1–V8 din plan)
| Etapa | Stare |
|---|---|
| V1 Knowledge Database | ✔ baza de cunostinte (~263 reguli cu ID) + `rules.json` (29 reguli folosite de motor) |
| V2 Joinery Database | ◐ 6 imbinari ca obiecte (KD-BB-01, ST-LOOSE-01, HL-PEG-01, RS-PEG-01, MT-GL-01, BTN-01) |
| V3 Component Library | ✔ `engine/components.py` (post, rail, board, panel, inclined_member) — folosita de ambele proiecte; patul rescris pe componente cu 0 diferente fata de versiunea anterioara (test de regresie) |
| V4 Parametric Generator | ✔ pat + masa (Python, testabil fara Rhino) |
| V5 Project System | ◐ doua proiecte, acelasi motor |
| V6 BOM + Cut List | ✔ debitare, feronerie, operatii, scule, ordine de asamblare |
| V7 Rhino Plugin | ✘ (scriptul Rhino exista; bara de butoane = pasul urmator) |
| V8 AI Layer | ✘ |

## Limitari cunoscute
- Masa: cepurile celor doua zargi se intalnesc in picior → capete la 45° cu joc 1 mm (JOINERY-MT-167). Testul fara 45° produce coliziuni zarga×zarga — deci taietura e necesara, nu decorativa.
- Masa: miscarea blatului (stejar, 900 mm, ΔMC 4 %) ≈ 13,5 mm → butonii zargilor lungi au joc de 5 mm fata de zarga si 10 mm angajare in nut (±3,4 mm la fiecare zarga).
- Inventar: `freza_disc_10` si `burghiu_4` sunt marcate "?" — confirma-le.
- **Partea Rhino (`rhino_out.py`) nu a fost rulata in Rhino** — doar compilata; verificarea numerica si previzualizarea au rulat pe aceleasi date. Daca o operatie booleana esueaza, scriptul are fallback (scula cu scula) si raporteaza in consola.
- Nemodelate: rotunjirile R5, gaurile de surub ale sipcilor/lamelelor, filetul, dopurile pe pozitie (apar doar in planul de piese).
- Structural: cadrul "casuta" e rigidizat doar de coama si de cadrul de jos (DES-STR-001) — e decorativ / pentru baldachin, **nu pentru catarat**. Pentru incarcari reale: contrafise sau calcul.
