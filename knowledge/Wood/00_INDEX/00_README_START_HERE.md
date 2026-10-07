# WOOD KNOWLEDGE BASE — START HERE

Versiune: 1.0 (2026-10-05) — prima iteratie (Fazele 1–9 parcurse o data, la nivel de "fundatie").

## WOOD ENGINE (iteratia 8)
Motorul parametric (reguli → imbinari → piese → Rhino cu deseu rosu → debitare/operatii/scule) este in `WOOD_ENGINE/` — vezi `WOOD_ENGINE/README.md`. Pilot: `WOOD_ENGINE/projects/pat_montessori/`.

## Cum se foloseste
1. Intrebare de proiectare (ex. "pat din lemn masiv") → incepe cu `03_FURNITURE/<tip>/` (sectiuni + imbinari recomandate).
2. Imbinare → `02_WOOD_JOINERY/<tip>/JOINERY_*.md` (fisa cu cele 19 campuri).
3. Specie → `05_WOOD_SELECTION/european_species/WOOD_SPECIES_EUROPEAN.md`.
4. Miscarea lemnului / tolerante → `05_WOOD_SELECTION/moisture/` + `09_CONSTRUCTION/tolerances/`.
5. Lanturi decizionale (specie → imbinare → adeziv → finisaj) → `00_INDEX/KNOWLEDGE_GRAPH.md`.
6. Contradictii intre surse → `00_INDEX/SOURCE_CONFLICTS.md`.

## Conventii de sursa (OBLIGATORIU de citit)
Fiecare unitate de cunostinte are camp `SOURCE` si un nivel de verificare:

| Eticheta | Inseamna |
|---|---|
| `[V-TEXT]` | Extras direct de mine din textul integral al sursei (domeniu public, citit in aceasta sesiune). Locatia = capitol + nr. figura din carte. Gutenberg nu pastreaza numerele de pagina; pagina de start a capitolului este data din cuprinsul original. |
| `[V-STD]` | Standard european / norma (EN 204, EN 350, EN 338, EN 1995 etc.). Valori de verificat in textul standardului oficial (acces platit). |
| `[REF]` | Informatie consacrata in literatura de specialitate; cartea citata o trateaza, dar **pagina NU a fost verificata** (cartea nu e accesibila legal gratuit). De verificat inainte de citare formala. |
| `[PRACTICA]` | Regula de atelier larg acceptata, fara o sursa unica. Tratata ca euristica. |
| `[V-DATA]` | Numaratori / asocieri calculate de agent din date structurate reale (ex. baza ETNOMON, 1.047 constructii). Arata ce exista in esantion, nu o regula universala. |
| `[V-FOTO]` | Observat de agent in fotografii (ex. ETNOMON, ~750 × 500 px) — doar forma exterioara vizibila. |
| `[ESTIMARE]` | Calcul / dimensionare orientativa facuta de agent. NU inlocuieste calculul structural (Eurocod 5) pentru elemente portante. |

## Despre Scribd
Scribd a fost folosit **doar pentru descoperire** (titluri, autori, subiecte). Majoritatea rezultatelor relevante sunt
incarcari de utilizatori ale unor carti comerciale (unele marcate explicit "Z-Library", "Anna's Archive", "PDFDrive"),
deci fara drept clar de distributie. Conform regulii 12 din brief, continutul lor NU a fost extras; s-au cautat in schimb
versiuni legale (Project Gutenberg, USDA Forest Products Lab, Internet Archive). Lista descoperirilor: `99_REFERENCE/sources/SCRIBD_DISCOVERY_LOG.md`.

## Conventie de denumire
- Fise carte: `YYYY_AUTHOR_SHORTTITLE_CATEGORY.md` in `99_REFERENCE/books/`
- Fise cunostinte: `JOINERY_*.md`, `WOOD_*.md`, `TOOL_*.md`, `FINISHING_*.md`, `FURNITURE_*.md`, `CONSTRUCTION_*.md`
- ID-uri unitati: `JOINERY-MT-001`, `WOOD-MOIST-003`, `FIN-OIL-002` etc. (unice in toata baza).
