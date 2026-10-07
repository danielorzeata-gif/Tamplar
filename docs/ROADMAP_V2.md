# Roadmap V2 — de la masa la mobilier complet, usi si ferestre

Baza de cunostinte: `knowledge/Wood/` (importata din arhiva Wood.rar + iteratia 9). Datele numerice pentru plugin: `knowledge/Wood/PLUGIN_DATA/joint_roles_and_rules.json`.

| Etapa | Continut | Stare |
|---|---|---|
| V1.1 | Imbinari si fixari alegibile pe masa (7 tipuri de imbinare, 3 fixari de blat), `JointInfo` cu proprietati din cercetare, motor de preview 2D (`PreviewEngine`) | **facut, testat (83 teste)** |
| V1.2 | **Dialog "Proiect nou" cu preview inainte de generare**: parametri + alegeri, previzualizare oblica in dialog + conduit shaded in viewport, rezumat live (achizitie, avertismente) | de facut (Rhino UI, neverificat in Rhino) |
| V1.3 | Roluri si reguli din JSON (`joint_roles_and_rules.json`) incarcate de Core — mobilier nou fara cod | de facut |
| V2.0 | `DoorDefinition`: canat cu panouri (montanti, traverse, panouri plutitoare cu joc calculat din miscare), toc, balamale (numar/pozitii), mortase de balama, broasca | de facut — vezi `DOORS_COMPLETE.md` §7 |
| V2.1 | `WindowDefinition`: toc + canat, profil IV68/78/92, canal Euro, geam (greutate vs feronerie), picurator/pervaz, Uw | de facut — vezi `WINDOWS_COMPLETE.md` §9; blocat de O-01/O-02 pentru reguli stricte |
| V2.2 | Corpuri: raft/biblioteca, dulap, comoda, sertar (sistem 32, glisiere, cupe de balama), validare sageata raft | de facut — `FURNITURE_MASTER_MATRIX.md` |
| V2.3 | Pat (bulon + cep de aliniere), scaun, banca | de facut — baza are fise (`beds/`, `chairs/`) |
| V2.4 | Import proiecte din `WOOD_ENGINE` (pat Montessori, masa) ca sabloane + comparatie cu motorul C# | de facut |
| V3 | Scari, mobilier curbat, finisaje exterioare | cercetare ramasa |
