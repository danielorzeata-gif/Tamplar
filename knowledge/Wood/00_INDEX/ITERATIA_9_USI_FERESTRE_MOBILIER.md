# ITERATIA 9 (2026-10-07) — usi, ferestre, mobilier complet + integrare cu pluginul Rhino Wood

## Ce s-a adaugat
| Fisier | Continut |
|---|---|
| `04_CARPENTRY/doors/DOORS_COMPLETE.md` | tipologie, dimensiuni (DIN 18101), goluri, proportii, panouri plutitoare + formula de joc, feronerie, exterior/standarde, fisa de parametri |
| `04_CARPENTRY/windows/WINDOWS_COMPLETE.md` | **subiect nou**: tipuri, profile IV68/78/92, canal Euro, imbinari, geam, apa/aer/montaj, EN 14351-1, Uw, ghilotina, fisa de parametri, lista de verificari O-01…O-05 |
| `03_FURNITURE/FURNITURE_MASTER_MATRIX.md` | matrice tip × componente × imbinari alegibile × feronerie × capcana de miscare; ergonomie; rafturi (sageata); sertare/balamale |
| `08_MATERIALS/hardware/HARDWARE_COMPLETE.md` | feronerie mobilier/usi/ferestre |
| `99_REFERENCE/sources/SOURCES_WEB_ITER9.md` | 20 grupuri de surse web, cu statutul `[WEB]` |
| `PLUGIN_DATA/joint_roles_and_rules.json` | roluri de imbinare alegibile + reguli numerice (date pentru plugin) |

## Limitari oneste
- Paginile web mari au fost **blocate** pentru descarcare; s-au folosit rezumate de cautare → `[WEB]` = indicativ. Valorile critice (Falzluft, Uw nZEB, EN 350, rezistente imbinari) sunt in lista **O-01…O-05**.
- Standardele (EN 14351-1, EN 942, EN 350, EN 1627, EN 1634, DIN 18101) nu au fost citite in text oficial (acces platit).
- Dimensiunile ghilotinei si ale scarilor nu au fost cercetate in profunzime.
- Nu s-au modificat fisele din iteratiile 1–8 (doar completate).

## Urmatorul pas (integrare plugin)
Vezi `/docs/ROADMAP_V2.md` in repo-ul pluginului: preview inainte de generare, `DoorDefinition`, `WindowDefinition`, extinderea selectiei de imbinari pe roluri, apoi corpuri/rafturi/dulapuri.
