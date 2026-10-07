# WOOD KNOWLEDGE GRAPH

Lant canonic: **SPECIE → PROPRIETATI → APLICATIE → IMBINARE → SCULE → ADEZIV → ASAMBLARE → FINISAJ**
Format pentru masina (triplete) la final — poate fi incarcat intr-un graf (Neo4j / networkx).

## Lanturi de proiectare (exemple validate pe fisele din baza)

### 1. Stejar → pat / mobilier structural
Stejar → ρ ~700, dur, tanin, C_T ~0,0037 (miscare medie), durabil cl. 2 → picioare/lonjeroane de pat, mese → M&T haunched; la pat bulon + cep de aliniere → dalta de scobit / masina de scobit / freza spirala; circular cu tenoning jig → PVAc D3 (tablie), fara adeziv la imbinarile demontabile; feronerie inox (tanin!) → clemele in echer, verificare diagonale → ulei-ceara dur sau ulei + lac; umplere pori pentru luciu.

### 2. Brad/molid → acelasi pat (economic)
Brad → ρ ~450, moale, strivire usoara, blotching → sectiuni +10–20 %, cepuri mai groase, saibe mari/piulite cilindrice → aceleasi imbinari, colturi tesite → PVAc D2/D3 → sealer inainte de bait; lac dur.

### 3. Fag → scaun
Fag → dur, omogen, miscare mare, se curbeaza cu abur, nedurabil exterior → scaune interioare, piese inguste → M&T lung / interlocking chair joint (Fairham) → circular, freza, dalti → PVAc D2 sau clei animal (reparabilitate) → ulei sau lac pe baza de apa.

### 4. Salcam/larice → mobilier de gradina
Salcam → foarte durabil (EN 350 cl. 1–2), dur → mobilier exterior → M&T + drawbore / bridle + cui de lemn (functioneaza fara adeziv) → pre-gaurire obligatorie → PUR / D4 sau fara adeziv → asamblare cu pene → ulei de exterior / lazura pigmentata (UV).

### 5. Hinoki/sugi (sau molid de rezonanta) → shoji
Fibra dreapta, fara noduri, stabil → shoji → ai-gaki la kumiko, M&T la rama → dozuki, nomi, kanna, jig-uri → fara adeziv (sau minim) → gang-cutting, asamblare prin presare → natural (rindeluit cu kanna).

### 6. Molid C24 → sarpanta
Molid → C24, E ~11 000 MPa → ferme, pane → M&T in pop, tusk, scarf ≈ 4×h langa reazem / conectori metalici → circular portabil, freza cu lant, CNC → fara adeziv (mecanic) → calcul EC5 + P100 → tratament fungicid/insecticid daca e cazul.

## Muchii de tip "cauza → efect" (reguli generalizabile)
| Cauza | Efect | Regula | ID |
|---|---|---|---|
| UM scade sub FSP | contractie, tang ≈ 2× radial | debitare radiala pentru stabilitate | WOOD-MOIST-001/002 |
| Fibra incrucisata fixata rigid | crapare / imbinare desfacuta | permite miscarea (buttons, panou plutitor, gauri ovale) | WOOD-MOIST-006 |
| Tanin + fier + umezeala | pete negre | inox/alama la stejar/castan | WOOD-SP-001 |
| Fibra inclinata | rezistenta redusa | max ~1:10–1:15 la piese portante | WOOD-DEF-001 |
| Cep > 1/3 din grosime | pereti de scobitura slabi | regula 1/3 | JOINERY-MT-001 |
| Dovetail mai abrupt de 1:6 | colturi sfaramate | 1:8 (min 1:6) | JOINERY-DT-001 |
| Scobitura langa capat | crapare | horn 25–75 mm | JOINERY-MT-003 |
| Umar subtaiat | rost vizibil la contractie | pariere verticala | JOINERY_MT §17 |
| Fibra de capat lipita | lipire slaba | armare mecanica | ADH-004 |
| Finisaj pe o singura fata | cupare | finiseaza ambele fete | FIN-003 |
| Lavete cu ulei sicativ | autoaprindere | uscare intinsa / apa | FIN-002 |
| Lipsa cutit despicator | recul | riving knife mereu | MACH-003 |
| Presiune de strangere prea mare | imbinare "infometata" | 0,7 MPa lemn usor … 1,7 MPa lemn dens | ADH-005 |
| Rindeluire/slefuire imediat dupa incleiere | rosturi adancite sub lac | asteapta ~7 zile la temp. camerei | ADH-005c |
| Imbinare cap-la-cap lipita | ~25 % din rezistenta | scarf ≥ 1:12 / finger joint | ADH-004 |
| Surub fara gaura de ghidare in esenta tare | despicare | gaura 90 % din baza filetului (70 % la rasinoase) | FAST-001 |
| Lemn expus la soare inainte de vopsire | exfolierea vopselei | finiseaza in < 1 sapt. (usor) / < 4 sapt. (dens) | FIN-004b |
| Capete nesigilate la exterior | apa intra de 10–100 × mai repede, putrezire | sigileaza toate capetele si gaurile | FIN-004c |

## Triplete (subiect | relatie | obiect)
```
Stejar | are_densitate | 650-760 kg/m3
Stejar | contine | tanin
tanin | reactioneaza_cu | fier
Stejar | potrivit_pentru | mobilier_structural
Stejar | durabilitate_EN350 | 2
Fag | are_miscare | mare
Fag | potrivit_pentru | scaune
Salcam | durabilitate_EN350 | 1-2
Salcam | potrivit_pentru | exterior
Molid | clasa_tipica | C24
mobilier_structural | imbinare_recomandata | mortise_tenon
cadru_de_usa | imbinare_recomandata | mortise_tenon_haunched
colt_de_carcasa | imbinare_recomandata | dovetail
picior_lonjeron_pat | imbinare_recomandata | bed_bolt_plus_stub_tenon
picior_lonjeron_pat | alternativa_JP | hanasen
prelungire_grinda | imbinare_recomandata | scarf
prelungire_grinda | alternativa_JP | okkake_daisen_tsugi
mortise_tenon | regula_grosime | 1/3_din_grosime
mortise_tenon | echivalent_JP | hozo_sashi
drawbore | echivalent_JP | komisen_uchi
fox_wedged_tenon | echivalent_JP | jigoku_hozo
tusk_tenon | echivalent_JP | hanasen
bridle | echivalent_JP | sanmai_gumi
half_lap | echivalent_JP | ai_gaki
dovetail | panta | 1:8_min_1:6
bridle | regula_grosime | treimi
scarf_pana | lungime | 4x_inaltime
mortise_tenon | scula_manuala | dalta_de_scobit
mortise_tenon | scula_utilaj | masina_de_scobit
mortise_tenon | scula_utilaj | freza_de_mana
dovetail | scula_manuala | ferastrau_dovetail
interior_uscat | adeziv | PVAc_D2
bucatarie_baie | adeziv | PVAc_D3
exterior | adeziv | D4_PUR_epoxy
restaurare | adeziv | clei_animal
casa_traditionala_RO | sistem | blockbau_cununi_orizontale
barne_rotunde | imbinare_colt | cheotoare_rotunda
barne_cioplite_4_fete | imbinare_colt | cheotoare_coada_de_randunica
cheotoare_coada_de_randunica | denumire_populara | cheia_nemteasca
talpi | specie | stejar
barne_perete_RO | sectiune | 120-150x200-300_mm
munte | specie_case | molid
deal | specie_case | stejar
taiere_lemn_constructie | perioada | toamna_tarziu_sau_sfarsit_iarna
ciuperci | UM_minima | 20_pct
talpi_RO | specie_dominanta | stejar_45_din_60
barne_rotunde | asociat_cu | cheotori_rotunde
cheotori_rotunde | zone | Zarand_Tinutul_Secuiesc_Marginimea_Sibiului
cheotori_drepte | zone | Maramures_Gorj_Valcea_Alba
cheotori_coada_de_randunica | zone | Hunedoara_Sibiu_Alba_Gorj
sosi | definitie | stalpi_intermediari_santuiti
sosi | zone | Campia_Transilvaniei_Cluj_Arad_Bistrita
amnare | zona | Bucovina
catei | zone | Banat_Maramures
muc | echivalent | cep
dranita | zone | Maramures_Bucovina
sita | zone | Gorj_Valcea_Arges
acoperis_RO | tip_dominant | 4_ape
cheotori_rotunde | echivalent_EN | saddle_notch
cheotori_coada_de_randunica | echivalent_EN | full_dovetail_notch
cheotori_drepte | echivalent_probabil_EN | square_lap_notch_cu_capete_iesite
sosi_amnare_catei | echivalent_EN | post_and_log_infill_piece_sur_piece
saddle_notch | adancime | D_pe_2
saddle_notch | timp_indemanare | minim
full_dovetail_notch | siguranta | maxima
full_dovetail_notch | capete_iesite | nu
square_notch | blocare | nu_necesita_cuie_de_lemn
crestatura_pe_fata_de_sus | efect | aduna_apa
talpa | distanta_minima_sol | 20_cm
epoxid | risc | retine_umezeala_in_lemnul_alaturat
lipitura | interzis | ciment_Portland
perete_barne_10_randuri_verde | tasare | 5-10_cm
Anobium_punctatum | ataca | lemn_uscat
lemn_usor | presiune_lipire | 0.7_MPa
lemn_dens | presiune_lipire | 1.7_MPa
lipire | umiditate_lemn | 6-14_pct
rasinoase | gaura_ghidare_surub | 70pct_baza_filet
foioase | gaura_ghidare_surub | 90pct_baza_filet
fibra_capat | rezistenta_smulgere_surub | 75pct
white_oak | C_T | 0.00376
white_oak | C_R | 0.00194
american_beech | C_T | 0.00431
white_fir | C_T | 0.00245
white_oak | MOE_GPa | 12.3
white_fir | MOE_GPa | 10.3
stejar_pori_inelari | finisaj_lucios_necesita | umplere_pori
cires_artar_pin | risc | blotching
exterior | finisaj | lazura_pigmentata
```
