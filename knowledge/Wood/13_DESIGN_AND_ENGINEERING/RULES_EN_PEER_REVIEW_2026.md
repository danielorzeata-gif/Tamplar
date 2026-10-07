# Testele EN și studiile peer-review fixează regulile configuratorului

**Concluzia, pe scurt.** Există o bază serioasă și recentă pentru reguli de validare. Dar ea este **inegală**. Partea cea mai solidă vine din:

- **sarcinile de produs** din EN 12520:2024 (scaune) și EN 14749:2016+A1:2022 (corpuri de depozitare);
- **ecuațiile de capacitate pentru cep-scobitură (M&T)**, publicate cu domenii de valabilitate (Kasal 2015; Hu & Liu 2020; Hu & Chen 2021);
- **ajustajele CNC măsurate pe fag**: 0,2 mm joc pe grosime, 0,1–0,2 mm strângere pe lățime;
- **regulile de frezare** de la EPFL IBOIS: raza interioară = D/2, raportul diametru/consolă de 1:7,5, îmbinări 1DOF cu un singur vector de inserție;
- **datele de material DIN 68364 / DIN 68100 pentru specii europene**, reproduse identic în mai multe broșuri LWF Bavaria.

Partea slabă: **niciun standard CEN nu prescrie geometria îmbinărilor**. Conformitatea se demonstrează prin încercarea produsului sau prin calcul prin analogie. Ecuațiile acoperă aproape numai fag și pin, cu clei. Pentru îmbinări fără clei (pană, cep cu pană, tusk), bridle și semilemn nu există date peer-review 2015–2026. Mai multe cifre-cheie trebuie reverificate în textul plătit sau în PDF înainte de a fi codificate:

- amendamentul **EN 12520:2024/A1 (ian. 2026)**;
- **unitatea coeficientului de joc din ecuația Eckelman** (țoli, nu mm);
- **coloana de duritate Wood Handbook**, decalată la extracție;
- **ecuația de încovoiere Fb a lui Hu & Chen**, care nu reproduce optimul raportat.

Regulamentele contează imediat pentru un producător mic din România:

| Regulament | Ce cere | Din ce dată |
|---|---|---|
| REACH, formaldehidă | ≤0,062 mg/m³ | 6 aug. 2026 (deja în vigoare) |
| EUDR | păstrarea numerelor DDS | 30 dec. 2026 (peste ~12 săptămâni) |
| GPSR | dosar tehnic păstrat 10 ani | în vigoare |

Raportul separă explicit valorile **sigure de codificat** de cele **condiționate** și de cele **interzise până la verificare**.

## Cum se citesc etichetele de verificare

Etichetele urmează convenția bazei de cunoștințe a lui dd. Ele reflectă nivelul de citire raportat de cercetătorii acestei sesiuni, nu o recitire separată:

| Etichetă | Semnificație în acest raport | Regulă de codificare |
|---|---|---|
| **[V-TEXT]** | Text integral (articol open-access, pagină oficială de producător sau muzeu) citit în sesiune | Se poate codifica în domeniul de valabilitate declarat |
| **[V-STD]** | Valoare dintr-un standard EN/ISO, văzută în previzualizarea oficială (iTeh/BSI/EVS) | Se poate codifica provizoriu. Trebuie confirmată în textul plătit (ASRO) |
| **[V-DATA]** | Set de date tabelar dintr-o sursă oficială (DIN 68364 via LWF, USDA Wood Handbook, ETA) | Se poate codifica, cu câmpul `sursa` completat |
| **[REF]** | Citat, dar doar abstract, snippet sau sursă secundară | Nu se codifică drept prag. Doar drept notă sau tendință |
| **[ESTIMARE]** | Calcul derivat sau inferență a raportului | Se codifică doar ca valoare implicită, marcată „derivat” |

## Standardele dau sarcini de încercare, nu geometrii de îmbinare

Nucleul normativ CEN/TC 207 a fost aproape integral revizuit în 2023–2026. Ediții în vigoare: EN 12520:2024, EN 12521:2023, EN 16139:2025, EN 15372:2023, EN 1022:2023, EN 1725:2023, EN 747-1/-2:2024 și EN 581-2/-3:2026. **EN 14749:2016+A1:2022 rămâne standardul pentru depozitare domestică și bucătării** ([iTeh CEN/TC 207](https://standards.iteh.ai/catalog/tc/cen/694dab0e-b6a2-4407-83df-b5cdb8780487/cen-tc-207)).

Standardele de metodă rămân în edițiile din 2012: EN 1728:2012 pentru scaune ([BSI](https://knowledge.bsigroup.com/products/furniture-seating-test-methods-for-the-determination-of-strength-and-durability)), EN 1730:2012 pentru mese ([EVS](https://www.evs.ee/en/evs-en-1730-2012)) și EN 16122:2012+AC:2015 pentru depozitare ([EVS](https://evs.ee/en/evs-en-16122-2012)). **EN 14074:2004 este retras din 1 oct. 2024** ([EVS](https://www.evs.ee/en/evs-en-14074-2004)) și nu trebuie citat în dosare noi.

Pentru designul circular există **EN 17902:2023**, care evaluează capacitatea de demontare și remontare ([iTeh CEN/TC 207](https://standards.iteh.ai/catalog/tc/cen/694dab0e-b6a2-4407-83df-b5cdb8780487/cen-tc-207)). Este cel mai probabil suport tehnic pentru viitoarele cerințe ESPR [ESTIMARE].

Esențial pentru configurator: aceste standarde sunt **încercări de performanță ale produsului finit**. Un picior cu cep de 40×50 mm nu este „conform” sau „neconform” în sine. Configuratorul poate doar să **dimensioneze prin analogie** cu literatura, apoi să marcheze ce trebuie încercat.

### Tabel 1 — Sarcini și praguri normative (candidate pentru reguli)

| Standard | Cerință | Valoare | Etichetă | Codificabil? |
|---|---|---|---|---|
| EN 12520:2024 | Masa utilizatorului de calcul | **110 kg** | [V-STD] | Da (provizoriu, vezi A1) |
| EN 12520:2024 | Sarcină statică pe șezut | **1300 N × 10 cicluri** | [V-STD] | Da (provizoriu) |
| EN 12520:2024 | Sarcină statică pe muchia frontală a șezutului | **1300 N** | [V-STD] | Da (provizoriu) |
| EN 12520:2024 | Sarcină statică pe spătar | **450 N** (preview-ul arată și „min 410 N”) | [V-STD] | Condiționat: 410 vs 450 neclar |
| EN 12520:2024 | Sarcină pe locurile neîncercate (banchete) | **750 N** | [V-STD] | Da (provizoriu) |
| EN 12520 / EN 12521 | Prindere degete: găuri accesibile | interzis Ø **7–12 mm**, dacă adâncimea este ≥10 mm | [V-STD] | Da |
| EN 12521:2023 | Puncte de forfecare: mecanisme acționate / uz normal | fără goluri de **8–25 mm** / **8–18 mm** | [V-STD] | Da |
| EN 12521:2023 | Masă „delicată” (verificare de stabilitate) | blat ≤**0,30 m²**, H ≥**600 mm**, masă >**10 kg** | [V-STD] | Da |
| EN 14749 | Sarcină pe raft | **0,65 kg/dm³** din volumul de depozitare | [V-STD] | Da (unitatea de confirmat) |
| EN 14749 | Sarcină în sertare și coșuri | **0,2 kg/dm³** | [V-STD] | Da |
| EN 14749 | Bară de haine | **4,0 kg/dm³** (unitate suspectă) | [V-STD] | Nu, până la verificare |
| EN 14749 | Reținerea raftului: vertical / orizontal | **100 N** / **50 %** din greutatea raftului | [V-STD] | Da |
| EN 14749 | Ușă pivotantă: vertical / orizontal | **30 kg** / **60 N** | [V-STD] | Da |
| EN 14749 | Rezistența sertarului; clapetă rabatabilă | **200 N**; **200 N** | [V-STD] | Da |
| EN 14749 | Blat de bucătărie (H ≤1000, adâncime ≥250 mm) / alte suprafețe superioare | **1000 N** / **750 N** | [V-STD] | Da |
| EN 14749 | Declanșarea testelor de stabilitate | H >**900 mm** și m ≥**10 kg**, sau H >**350 mm** și m ≥**35 kg** | [V-STD] | Da |
| EN 14749 | Sertar >10 kg | **200 N** rezistență la extragere, sau informare a utilizatorului | [V-STD] | Da |
| EN 14749 | Capace (≤1000 mm de la sol, ≥0,25 kg) | cădere ≤**12 mm** pe arcul de **60°** | [V-STD] | Da |

Sursa tabelului: previzualizările oficiale pentru [EN 12520:2024](https://standards.iteh.ai/catalog/standards/cen/9ca01315-a874-43ab-b41a-27c0e48c6291/en-12520-2024), [SIST EN 12521:2024](https://standards.iteh.ai/catalog/standards/sist/4e0071c2-e32d-4304-af7d-9449884e893d/sist-en-12521-2024) și [EN 14749+A1](https://standards.iteh.ai/catalog/standards/cen/b42a9e8c-05d9-4e56-8d24-d8fce51c2a05/en-14749-2016a1-2022).

Lipsesc nivelurile de oboseală (cicluri șezut/spătar), nivelurile EN 16139 și EN 15372, precum și forțele de stabilitate din EN 16122. Ele nu sunt vizibile în previzualizări. **Configuratorul nu are, deci, o țintă normativă de oboseală.** Singurul substitut publicat este factorul static→ciclic din literatură (secțiunea următoare).

Pentru răsturnarea mobilierului înalt, UE nu are un regulament dedicat. Mecanismele sunt pragurile EN 14749, dispozitivele de prindere în perete EN 15939:2019 și obligația generală GPSR. SUA aplică obligatoriu **ASTM F2057-23 din 1 sept. 2023**: 234 de decese între 2000 și 2022, dintre care 199 copii ([CPSC](https://cpsc.gov/Newsroom/News-Releases/2023/CPSC-Adopts-Final-Consumer-Product-Safety-Standard-to-Prevent-Tip-overs-of-Dressers-and-Other-Clothing-Storage-Units)). Regula combinată recomandată are trei părți [ESTIMARE]:

- orice corp peste pragurile EN 14749 primește automat kit de ancorare și etichetă de avertizare;
- se calculează marja la răsturnare cu sertarele deschise și încărcate la 0,2 kg/dm³;
- aceeași regulă acoperă și așteptarea americană.

### Calendarul regulamentar

**GPSR (Reg. (UE) 2023/988)** se aplică din **13 dec. 2024**. Cere ([FPS Economy BE](https://economie.fgov.be/en/themes/quality-and-safety/safety-products-and-services/regulations/general-regulations-safety)):

- analiză internă de risc;
- documentație tehnică păstrată **10 ani**;
- marcarea produsului cu nume, adresă poștală **și** electronică, tip și lot;
- instrucțiuni în limba pieței, deci română.

**REACH, anexa XVII, intrarea 77 (Reg. (UE) 2023/1464)** limitează formaldehida la **0,062 mg/m³** pentru mobilier și articole pe bază de lemn, **din 6 aug. 2026**. Condițiile de cameră sunt 23 °C, 45 % RH, încărcare 1 m²/m³ și 1 schimb de aer/h ([SGS](https://www.sgs.com/en-us/news/2023/07/safeguards-9123-eu-regulates-formaldehyde-in-articles-under-reach); [eco-INSTITUT](https://www.eco-institut.de/en/2023/09/eu-new-regulations-on-formaldehyde-emissions-from-products-from-august-2026/)). Plăcile clasate doar „E1” nu mai sunt suficiente [ESTIMARE].

**EUDR, modificat prin Reg. (UE) 2025/2650**, se aplică de la **30 dec. 2026** operatorilor mari și mijlocii și tuturor comercianților și operatorilor din aval. Micro-operatorii primesc termen până la **30 iun. 2027**, cu excepția celor pentru produse deja acoperite de EUTR, cum e lemnul, care intră tot la 30 dec. 2026. Operatorii din aval nu mai depun declarație proprie. Ei păstrează **5 ani** numerele de referință DDS din amonte ([CE Access2Markets](https://trade.ec.europa.eu/access-to-markets/en/news/delay-until-december-2026-and-other-developments-implementation-eudr-regulation)).

**Ecolabel mobilier (Decizia 2016/1332)** a fost prelungit prin Decizia (UE) 2026/66 **până la 31 dec. 2029** ([EUR-Lex](https://eur-lex.europa.eu/eli/dec/2026/66/oj); [eu-ecolabel.de](https://eu-ecolabel.de/en/the-eu-ecolabel/news/announcement/eu-ecolabel-verlaengerungen-wurden-angenommen-1)).

**ESPR (Reg. (UE) 2024/1781)** numește explicit „furniture, including mattresses” ca grup prioritar ([PE OEIL](https://oeil.europarl.europa.eu/oeil/en/document-summary?id=1785880)) [V-TEXT]. Anul indicativ 2028 pentru actul delegat provine doar din surse secundare [REF].

Eurocodul 5 nu se aplică mobilierului. Pentru piese structurale încastrate (mezanin, scară), prima generație se retrage la **30 mar. 2028** ([JRC](https://eurocodes.jrc.ec.europa.eu/news/second-generation-eurocodes-milestones-achieved)).

## Lungimea cepului dă rezistența, lățimea dă rigiditatea

Literatura peer-review 2015–2026 converge pentru M&T încleiat în fag și pin. **Lungimea cepului controlează momentul ultim**, iar **lățimea controlează rigiditatea**. Cifrele:

- PU dă ~15 % mai mult moment decât PVAc;
- fagul bate pinul cu ~30 %, prin rezistența la forfecare (10,3 vs 6,2 N/mm²);
- un umăr complet așezat adaugă **+54 %** la moment ([Kasal et al. 2015](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2016/06/BioRes_10_4_7009_Kasal_EHEY_Bending_Moment_Capacity_L_shaped_Mortise_7410.pdf) [V-TEXT]; [Likos et al. 2012](https://avesis.omu.edu.tr/yayin/1142002b-f667-4d71-8e5a-0a20544c53fa/effect-of-tenon-geometry-grain-orientation-and-shoulder-on-bending-moment-capacity-and-moment-rotation-characteristics-of-mortise-and-tenon-joints) [REF]).

Un studiu FEM spune contrariul: lățimea contează mai mult ([Hu & Liu 2020, BioResources](https://bioresources.cnr.ncsu.edu/?p=31033) [V-TEXT]). Explicația plauzibilă: a modelat lungimi de până la 60 mm, unde lungimea s-a saturat deja. Contradicția se rezolvă prin respectarea strictă a domeniilor de valabilitate.

Efectul adezivului depinde de tipul îmbinării. PVAc dă **+42 %** rigiditate față de PUR la M&T de fag ([Záborský 2017](https://bioresources.cnr.ncsu.edu/resources/effect-of-wood-species-adhesive-type-and-annual-ring-directions-on-the-stiffness-of-rail-to-leg-mortise-and-tenon-furniture-joints/) [REF]). PVAc bate PU și la îmbinările cu dinți drepți ([Demirci 2020](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2020/03/BioRes_15_2_3136_Demirei_DKE_Effect_Wood_Species_Teeth_Adhesieve_Type_Strength_Box_Joint_17052.pdf) [V-TEXT]). Configuratorul nu trebuie deci să aplice un bonus universal pentru PU.

### Tabel 2 — Ecuații de dimensionare și statutul lor

| Cod | Ecuație (M în N·m, F în N, dimensiuni în mm) | Domeniu de valabilitate | Etichetă | Statut |
|---|---|---|---|---|
| **E1** Kasal 2015, M&T în L | `M = 0.00227·(W·L)·(0.229·W + d)·S^0.42·k1·k2`; k1 = 1,0 (întindere) / 1,066 (compresiune); k2 = 1,0 (PU) / 0,85 (PVAc) | W, L = 30–50; t = 7; S = 6,2–10,3 N/mm²; R² = 0,616, SD 23 % | [V-TEXT] | **Sigur în domeniu**, cu factor 1,5–2. Definiția lui *d* (lățimea umărului) de confirmat în PDF |
| **E2** Hu & Liu 2020, M&T în T, fag | `M = 39.072 + 4.627A − 6.4384B + 0.876AB − 0.434A² − 0.015A²B + 0.008A³` (A = lungime, B = lățime) | A = 10–40; B = 15–25; t = 10; ajustaj 0,2; PVAc; numai fag | [V-TEXT] | **Sigur în domeniu.** Recalculat la optim (28,8×25): ~162 N·m față de 160,2 raportat [ESTIMARE] |
| **E3a** Hu & Chen 2021, extragere | `Fw = 4386.5 − 228.5l + 344.8w − 265.4t + 15.5lw + 15.2lt − 16.6wt − 0.84l² − 14.3w² + 25.7t²` | l = 20–40; w = 15–25; t = 7,5–15; fag; picior 40×40, traversă 30×30 | [V-TEXT] | **Sigur în domeniu.** Recalculat la 40×25×15: 13 782 N față de 13 799,8 raportat |
| **E3b** Hu & Chen 2021, încovoiere | `Fb = 143 + 10.5l − 18.8w + 72.9t + 0.4lw + 0.17lt + 3.5wt − 0.26l² + 0.26w² − 3.9t²` | idem; Fb este forță, nu moment | [V-TEXT] | **NU se codifică.** Recalculat la optim: ~1870 N față de 1591,6 raportat (+17 %). Probabil eroare de transcriere |
| **E4** Hajdarevic 2020, analitic | `M_R = M1 + M2 + M3`; `M3 = nβhl²τ`; `β = 1/[3 + 2.6/(0.45 + h/l)]` | simbolurile nu sunt definite în extras | [V-TEXT] | **Nu se codifică.** Supraestimează testele, eroarea crescând cu lungimea |
| **E6** Eckelman, dibluri, citat de Chen 2019 | `F_cap = 0.834·D·L^0.89·(S1+S2)·a·b·c`; `F_lat = 0.834·D·L^0.89·(0.95·S1+S2)·a·b·c`; a = 0,9 (PVAc <60 % substanță uscată); **b = 1 − 17.1·d (d în țoli!)**; c = 0,9 (striat) | S1 + S2 > 25 MPa | [V-TEXT] (sursă originală 1969 [REF]) | **Condiționat**: d se convertește din mm în țoli. Pentru 0,08 mm → b ≈ 0,946 |
| **E7** Chen 2019 | aceeași formă cu `L^1.00` (cap) / `L^0.99` (lateral) | S1 + S2 < 25 MPa | [V-TEXT] | Sigur; capacitatea crește ~liniar cu adâncimea de încastrare |
| **E8** Podlena 2018 | `σ = Fmax/(π·d·h)` | dibluri de 8 mm, încastrare 21 mm, fag | [V-TEXT] | Sigur ca normalizare |
| **E9** Georgescu 2019, 2 dibluri în L | `Mc = 105.75 + 36.31X1 + 22.69X2 + 17.55X3 + …`; `Mt = 192.06 + 63.18X1 + …` (factori codificați) | frasin termotratat; L = 30–70; D = 6–10; 250–450 g/m² | [V-TEXT] | Condiționat: ordinea X1/X2/X3 de confirmat |

Surse: [Hu & Liu 2020](https://www.mdpi.com/1999-4907/11/5/501), [Hu & Chen 2021](https://www.mdpi.com/1999-4907/12/4/478), [Hajdarevic 2020](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2020/09/BioRes_15_4_8249_Hajdarevic_OMM_Strength_Stiffness_Analys_Standard_Double_Mortise_Tenon_Joints_17584.pdf), [Chen 2019](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2019/10/BioRes_14_4_9214_Chen_XTQZ_Withdrawal_Force_Capacity_Wood_Dowels_Furniture_Appl_16052.pdf), [Podlena 2018](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2018/05/BioRes_13_3_5179_Podlena_HPBB_Axial_Loading_SinglePin_Dowels_Withdrawal_Strength_13746.pdf), [Georgescu 2019](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2019/06/BioRes_14_3_6619_Georgescu_VRB_Effect_Dowel_Length_Diam_Adhesive_Joint_Bending_Capac_15608.pdf).

### Exemplu de calcul cu E1

Fag, PVAc, întindere, W = L = 40 mm, d = 10 mm presupus. Rezultă **~158 N·m**, coerent cu media de 165 N·m pentru fag în întindere raportată de Kasal [ESTIMARE].

Pentru proiectare se folosește **limita inferioară de toleranță, nu media**. La M&T cu capăt rotunjit din stejar alb, LTL 0,95/0,95 = 231 N·m la o medie de 341 N·m, adică **≈0,68 × media** ([Uysal et al. 2025](https://acikerisim.btu.edu.tr/items/b371a704-9378-45a0-952c-e9f89bdba579/full) [REF]).

### Verificarea scaunului întreg și ce mai spun datele

Scaunul are o verificare publicată. În Kılıç et al. 2018 [V-TEXT], îmbinările picior–traversă laterală preiau ~73 % din moment la încărcare față-spate ([Kılıç 2018](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2017/11/BioRes_13_1_0256_Kilic_KKAE_Tenon_Size_Static_Front_Loading_Perform_Wood_Chairs_Design_12049.pdf)). Capacitatea ciclică este ≈**0,56 × capacitatea statică**. Nivelurile ALA de acceptare sunt 1335 N (domestic), 1557 N (mediu) și 2002 N (greu).

Regula de configurator: **capacitate statică necesară ≥ nivel / 0,56**, adică **2384 / 2780 / 3575 N** [ESTIMARE]. În pin silvestru cu PVAc, numai cepurile de 40×50 (domestic) și 50×50 (mediu) au trecut. Mediile pe scaun: 30×30 → 1409 N; 40×50 → 2496 N; 50×50 → 3127 N. Factorul ciclic pe forma cepului: **0,565 (dreptunghiular), 0,668 (rotund), 0,692 (romb)** ([Likos 2013](https://wfs.swst.org/index.php/wfs/article/view/41) [REF]).

**Nu există o corespondență publicată între nivelurile ALA și ciclurile EN 12520/EN 1728.** Verificarea ALA rămâne un proxy, nu o dovadă de conformitate EN.

Pentru diblu, **adâncimea de încastrare domină**: sarcina se dublează de la 12,7 la 38,1 mm ([Chen 2019](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2019/10/BioRes_14_4_9214_Chen_XTQZ_Withdrawal_Force_Capacity_Wood_Dowels_Furniture_Appl_16052.pdf)). Diblurile cu mai multe caneluri elicoidale ating **7,9 MPa** tensiune de aderență, față de 4,9 MPa pentru cele cu o singură canelură spiralată ([Podlena 2018](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2018/05/BioRes_13_3_5179_Podlena_HPBB_Axial_Loading_SinglePin_Dowels_Withdrawal_Strength_13746.pdf)). Chen nu a găsit însă diferențe de textură, deci este o contradicție de semnalat.

Alte date utilizabile:

| Îmbinare | Rezultat | Sursă |
|---|---|---|
| Cep liber încleiat (Domino 8×22×40, fag) | **7738 N** la extragere, ~2× un conector KV | [Bas 2024](https://www.mdpi.com/1999-4907/15/2/343/htm) [V-TEXT] |
| Coadă de rândunică simplă | optim la **84°** (pantă ~1:9,5) | [FPJ 69(2)](https://fpj.kglmeridian.com/view/journals/fpro/69/2/article-p131.xml) [REF] (DOI suspect) |
| Îmbinare cu dinți drepți | 12 dinți > 8 ≈ 4. Toate cedările în linia de clei, la frezare ±0,01 mm | [Demirci 2020](https://bioresources.cnr.ncsu.edu/wp-content/uploads/2020/03/BioRes_15_2_3136_Demirei_DKE_Effect_Wood_Species_Teeth_Adhesieve_Type_Strength_Box_Joint_17052.pdf) [V-TEXT] |
| Diblu de fag sudat prin rotație, fără clei | **1,9–2,6 kN** la 20 mm, dar COV până la **36 %** și cedare fragilă | [Zhao & Jin 2026](https://www.mdpi.com/1999-4907/17/7/800) [V-TEXT] |

Diblul sudat se acceptă doar în grupuri de mai multe dibluri.

## Ajustajul CNC măsurat: joc 0,2 mm pe grosime, strângere 0,1–0,2 mm pe lățime

Grupul Hu Wengang (Nanjing) dă singurele toleranțe numerice măsurate pentru M&T frezat în lemn masiv. Pe fag de 0,63–0,69 g/cm³ cu umiditate 10,8–11,2 %, standardul FEM validat este:

- **0,2 mm joc pe grosimea cepului** (fețele încleiate);
- **0,2 mm strângere pe lățime/înălțime** ([Hu & Guan 2019, J Wood Sci](https://jwoodscience.springeropen.com/track/pdf/10.1186/s10086-019-1794-4) [V-TEXT]).

Valori măsurate în același studiu: coeficient de frecare lemn-lemn **0,54**, linie de clei 54,5 µm, extragere 5133 N (COV 4,35 %).

Forța de contact crește **exponențial** cu strângerea între 0,1 și 0,5 mm. Cepul cu fibra radială este mai rigid decât cel tangențial: 5780 vs 3497 N/mm ([Hu, Liu, Guan 2019](https://bioresources.cnr.ncsu.edu/?p=27370) [V-TEXT]). Trecerea de la 0 la 0,2 mm strângere crește semnificativ capacitatea la întindere ([Hu, Guan, Zhang 2018](https://wfs.swst.org/index.php/wfs/article/view/2623) [REF]).

Nu s-a găsit niciun prag de despicare. Limita de ~0,3 mm este o inferență din curba exponențială [ESTIMARE]. **Toate aceste valori sunt pentru fag.** Pentru specii moi (pin, tei, plop) nu există date.

Regulile de frezare vin de la EPFL IBOIS ([Robeller & Weinand, WCTE 2016](https://infoscience.epfl.ch/record/222484/files/WCTE2016_Robeller.pdf?version=1) [V-TEXT]):

- raza colțului concav = **D/2**, cu crestătură tangențială pe bisectoare, care pierde cea mai puțină suprafață de contact;
- freze Ø10–20 mm, raport **diametru : consolă = 1:7,5**;
- 2/3/4 treceri pentru plăci de 21/27/39 mm;
- unghiuri diedre de **40–140°** la înclinarea sculei de 50° (limita la frezare: 60°).

Placa dublă IBOIS adaugă alte reguli ([Robeller et al., AAG 2016](https://adg.csail.mit.edu/wp-content/uploads/2022/12/2016AAG_DoubleLayerTimber.pdf) [V-TEXT]):

- **rotația lamelei cepului străpuns de 10–25° dă forfecarea optimă** (până la 40° acceptabil);
- aria de contact crește de la 662 la 1422 cm²;
- un singur vector de asamblare per segment;
- rândurile alternative oglindite (herringbone) blochează căile de scăpare.

Pentru plăci din placaj frezat CNC, coada de rândunică și urechea de degajare „Mickey Mouse” (MME) au randament de fabricație de **78 %**. Placajul este cu ≥50 % mai rezistent la compresiune decât MDF-ul ([Uysal et al. 2019](https://bioresources.cnr.ncsu.edu/?p=22335) [V-TEXT]).

### Tabel 3 — Reguli de fabricație și ajustaj

| Regulă | Valoare | Etichetă | Statut |
|---|---|---|---|
| Grosime cep = lățime scobitură − joc | **0,2 mm** (fag, încleiat) | [V-TEXT] | Sigur pentru fag. Implicit pentru alte specii, marcat „netestat” |
| Lățime cep = lungime scobitură + strângere | **0,1–0,2 mm** (fag) | [V-TEXT] | Sigur pentru fag |
| Strângere maximă înainte de risc de despicare | ~0,3 mm | [ESTIMARE] | Avertisment, nu eroare |
| Orientarea fibrei cepului | radial preferat (+65 % rigiditate față de tangențial) | [V-TEXT] | Sigur ca preferință |
| Raza colțului interior | r = D/2 (+0,1–0,2 mm pentru degajare: [ESTIMARE]) | [V-TEXT] | Sigur |
| Diametru minim al frezei | Ø ≥ adâncimea de tăiere / 7,5 | [V-TEXT] | Sigur |
| Unghi diedru la 3 axe | numai 90°. La 5 axe: 40–140° (înclinare ≤50–60°) | [V-TEXT] | Sigur |
| Lungimea cepului față de lățime | 1 × w ≤ l ≤ 2 × w (mai ales la cepuri subțiri) | [V-TEXT] | Sigur în domeniul E3 |
| Grosimea cepului | 1/3–1/2 din traversă. 1/2 dă +28 % rigiditate | [REF] | Implicit |
| Umăr | obligatoriu, complet așezat (+54 % moment) | [REF] / E1 [V-TEXT] | Sigur ca regulă |
| Unghi coadă de rândunică | 84° (81° cel mai bun în fag) | [REF] | Implicit, nu prag |
| Rotația lamelei la cep străpuns (plăci) | 10–25° optim, ≤40° | [V-TEXT] | Sigur |
| Ajustaj la îmbinarea cu dinți | ~0 mm (frezare ±0,01 mm) | [V-TEXT] | Doar ca țintă de precizie |
| Toleranțe Tsugite (0,1–0,3 mm) și rezoluția voxelilor | — | neverificate | **Nu se codifică** |

### Design for disassembly

DfD se traduce în logică de secvență, nu în cifre. Principiile IBOIS (îmbinare 1DOF, un singur vector de inserție per piesă) și modelul „backward/multi-key interlocking” ([Song et al., SIGGRAPH Asia 2017](https://history.siggraph.org/?p=210689) [REF]) se reduc la un algoritm:

1. Se construiește un **graf de blocare direcțională**.
2. Ansamblul este interblocat dacă exact o piesă-cheie este demontabilă și toate celelalte rămân blocate până la scoaterea ei.
3. Demontarea urmează ordinea inversă.

Sinteza a 71 de articole despre DfD în lemn ([Chiletto et al. 2024](https://documentserver.uhasselt.be/bitstream/1942/44396/1/Design%20for%20Disassembly%20and%20Reuse_A%20Synthesis%20for%20Timber%20Construction.pdf) [V-TEXT]) dă principiile: îmbinări reversibile, puține tipuri de conectori, standardizare, stratificare și acces. **Nu dă o metrică de timp de demontare.**

Studiul pregătitor ESPR pentru DG Environment urmărește „durability, reparability, disassemblability, component interchangeability and upgradeability” ([Öko-Institut](https://www.oeko.de/en/projects/detail/espr-preparatory-study-impact-assessment-and-eu-ecolabel-criteria-revision-on-furniture/) [REF]). Câmpuri propuse pentru fiecare îmbinare, gândite pentru un viitor pașaport digital [ESTIMARE]:

| Câmp | Valori |
|---|---|
| `reversibil` | da/nu |
| `vector_insertie` | vector |
| `piesa_cheie` | id piesă |
| `demontare_fara_scule` | da/nu |
| `clei` | da/nu |
| `nr_tipuri_conectori` | număr |

## Materialul european: fagul se mișcă dublu față de castan

Seria LWF Wissen a institutului de stat bavarez reproduce valorile **DIN 68364:2003** (densitate, rezistențe, duritate Brinell) și **DIN 68100** (contragere). Valorile sunt identice în 4–5 broșuri independente pentru stejar, fag, frasin, molid, pin și paltin. De aceea au **încredere ridicată** [V-DATA]. Baza de umiditate este climatul normal 20 °C / 65 % RH (≈12 %). Valorile sunt **medii de lemn fără defecte, nu valori caracteristice EN 338**.

### Tabel 4 — Proprietăți specii europene (DIN 68364 via LWF)

| Specie | ρ medie kg/m³ | E∥ MPa | MOR MPa | σc∥ MPa | Brinell ∥ / ⊥ N/mm² | Etichetă |
|---|---|---|---|---|---|---|
| Stejar (Q. robur/petraea) | 710 | 13 000 | 95 | 52 | 50–65 / 23–42 | [V-DATA] |
| Fag (F. sylvatica) | 710–720 | 14 000 | 120 | 60 | 70 / 28–40 | [V-DATA] |
| Frasin (F. excelsior) | 700 | 13 000 | 105 | 50 | 64 / 28–40 | [V-DATA] |
| Paltin (A. pseudoplatanus) | 630 | 10 500 | 95 | 50 | 48–62 / 26–34 | [V-DATA] |
| Castan (C. sativa) | n/r | 9 000 | 80 | 49 | 32–39 / 15–23 | [V-DATA] |
| Salcâm (Robinia) | 740 | 13 600 | **150** | 73 | 64–78 / 40–57 | [V-DATA] |
| Ulm | 650 | 11 000 | 81 | 51 | 58–63 / 27–37 | [V-DATA] |
| Mesteacăn | 650 | 14 000–16 500 | 120–147 | 43–60 | 49 / 23 | [V-DATA] |
| Anin negru | 550 | 7 700–11 760 | 85–97 | 47–55 | 33–38 / 16–17 | [V-DATA] |
| Plop negru | 450 | 8 800 | 55–65 | 30–35 | 30 / 10 | [V-DATA] |
| Tei | 530 | 7 400 | 90–106 | 44–52 | 37–41 / 13–20 | [V-DATA] |
| Molid | 460 | 11 000 | 80 | 45 | 32 / 12 | [V-DATA] |
| Pin silvestru | 520 | 11 000 | 85 | 47 | 40 / 19 | [V-DATA] |
| Nuc european | 680 | 12 500 | (?) | (?) | (?) | ρ, E [V-DATA]; restul extracție coruptă |
| Cireș (P. avium) | 600–630 | 10 000 | (?) | (?) | (?) | ρ, E [V-DATA]; restul extracție coruptă |
| Larice european | — | — | — | — | — | negăsit |

Surse: [LWF W75 Stejar](https://lfl.bayern.de/mam/cms04/forsttechnik-holz/dateien/w75_das_holz_der_eiche-eigenschaften_und_verwendung_bf_gesch.pdf), [W84 Salcâm](https://lwg.bayern.de/mam/cms04/forsttechnik-holz/dateien/w84_das_holz_der_robinie.pdf), [W78 Tei](https://lfl.bayern.de/mam/cms04/waldbau-bergwald/bilder/w78-holz-der-winterlinde.pdf), [W83 Ulm](https://lfl.bayern.de/mam/cms04/forsttechnik-holz/dateien/w83_flatterulme_holz_eigenschaften_verwendung.pdf), [W42 Anin](https://lwg.bayern.de/mam/cms04/forsttechnik-holz/dateien/w42_das_holz_der_schwarzerle-eigenschaften_verwendung.pdf), [W60 Nuc](https://lfl.bayern.de/mam/cms04/forsttechnik/dateien/w60-das-holz-der-walnuss.pdf).

Problema de duritate trebuie tratată explicit. Coloana Janka din Wood Handbook (Tabel 5-3b, specii nord-americane) a ieșit **decalată la extracție**: plopul american apare la 4000 N, peste cireș, iar salcâmul sub frasin ([USFS cap. 5](https://www.precisebits.com/PDF/USFS_mechanical_properties_of_wood.pdf)). **Duritatea Janka nu se încarcă din acea extracție.** Se folosește Brinell ⊥ din DIN 68364, iar unitățile nu se amestecă (Janka în N, Brinell în N/mm²). Brinell ⊥ este analogul cel mai apropiat de EN 1534, dar nu este identic cu acesta.

Coloanele SG, MOR și MOE din Wood Handbook sunt coerente și rămân utile ca proxy pentru specii lipsă, cu marcajul `proxy_source` [V-DATA]. Exemple: larice vestic pentru Larix decidua (SG 0,52; MOR 90; E 12 900).

### Tabel 5 — Contragere diferențială DIN 68100 (%/% umiditate, sub punctul de saturație al fibrei)

| Specie | diff_R | diff_T | T/R | Mișcare panou tangențial 600 mm, ΔMC = 5 % [ESTIMARE] |
|---|---|---|---|---|
| Fag | 0,20 | **0,41** | 2,1 | **12,3 mm** |
| Mesteacăn | 0,29 | 0,41 | 1,4 | 12,3 mm |
| Molid | 0,19 | 0,39 | 2,1 | 11,7 mm |
| Frasin | 0,21 | 0,38 | 1,8 | 11,4 mm |
| Stejar | 0,16 | 0,36 | 2,2 | 10,8 mm |
| Pin silvestru | 0,19 | 0,36 | 1,9 | 10,8 mm |
| Salcâm | 0,20–0,26 | 0,32–0,38 | ~1,6 | 9,6–11,4 mm |
| Plop negru | 0,13 | 0,31 | 2,4 | 9,3 mm |
| Nuc european | 0,18 | 0,29 | ~1,6 | 8,7 mm |
| Cireș | 0,16–0,18 | 0,26–0,30 | ~1,7 | 7,8–9,0 mm |
| Paltin | 0,10–0,20 | 0,22–0,30 | ~1,8 | 6,6–9,0 mm |
| Castan | 0,14 | **0,21–0,26** | ~1,7 | **6,3–7,8 mm** |

Toate valorile de contragere sunt [V-DATA], din aceleași broșuri LWF.

Formula este cea din Wood Handbook: `ΔD = D·C·(MF − MI)`, valabilă pentru 6–14 % umiditate ([WH 1999 cap. 12](https://blog.spib.org/wp-content/uploads/2021/07/ch12.pdf) [V-DATA]). Fagul american dă 12,9 mm pentru același caz, ceea ce confirmă valorile europene.

Umiditatea de echilibru la 21 °C ([Wood Handbook 2021, Tabel 4-2](https://www.fpl.fs.usda.gov/documnts/fplgtr/fplgtr282/chapter_04_fpl_gtr282.pdf) [V-DATA]):

| RH | 30 % | 40 % | 50 % | 60 % | 65 % |
|---|---|---|---|---|---|
| Umiditate de echilibru | 6,2 % | 7,7 % | 9,2 % | 11,0 % | 12,0 % |

Recomandarea americană pentru mobilier de interior este 8 % (6–10 %). Pentru un apartament încălzit din București, oscilația sezonieră de ~5 puncte (5–7 % iarna, 10–12 % vara) este o valoare de calcul rezonabilă, dar derivată [ESTIMARE]. Ținta EN 942:2007 nu a fost citită.

### Durabilitate, produse inginerești și adezivi

Durabilitatea naturală oficială e confirmată doar parțial: stejar **DC 2**, disputat de testele de teren; salcâm **DC 1–2** (EN 350:2016); ulm **4**; mesteacăn **5**; nuc „moderat durabil” ≈ 3 [V-DATA]. Clasele din Anexa B a EN 350:2016 pentru fag, frasin, paltin, cireș, anin, plop, molid, pin, larice și castan **nu sunt confirmate** ([iTeh EN 350](https://iteh.es/catalog/standards/cen/b02d18a7-87ce-4a20-84c7-c0de641a2780/en-350-2016)).

Produse inginerești:

| Produs | Valori | Sursă | Etichetă |
|---|---|---|---|
| BauBuche GL75 (ETA-18/1018) | ρmean 800 kg/m³; E0,mean 16 800 MPa; fm,k 75 MPa; umiditate la livrare **6 ± 2 %** | [HASSLACHER](https://www.hasslacher.com/data/_dateimanager/broschuere/HNT_News_BauBuche_EN_Web.pdf) | [V-DATA] |
| Accoya | contragere R/T 0,7/1,5 %; C22; Brinell 24 N/mm² | [Accsys](https://internationaltimber.com/wp-content/uploads/2020/11/Accoya_WoodInfoGuide.pdf) (document de producător) | [V-DATA] |
| Panouri SWP (EN 13353:2022) | SWP/1/2/3 corespund claselor de serviciu 1/2/3 | [EVS](https://www.evs.ee/en/evs-en-13353-2022) | [V-STD] |

Adezivi: EN 204:2016 și EN 12765:2016 sunt în vigoare ([LVS](https://www.lvs.lv/en/products/committeeSearch/315)). Pragurile D3 (≥10 N/mm² uscat; ≥2 N/mm² după 4 zile în apă rece) provin dintr-o fișă de producător anterioară ediției 2016 ([Den Braven](https://www.denbraven.com/documents/62/if-11_colles_classification.pdf) [REF]). **Nu sunt praguri sigure** până la confruntarea cu EN 204:2016.

## Designul contemporan reduce numărul de îmbinări în loc să le expună

Colecțiile instituționale confirmă un tipar contrar intuiției „îmbinare-ornament”. Piesele de referință sunt **monolite sculptate CNC sau robotic, cu puține îmbinări simple**:

- **Branca** (Industrial Facility, Mattiazzi 2010): 7 piese din frasin, „joined with wooden dowels and some glue”, frezate de un robot cu 8 axe ([V&A O1224758](https://api.vam.ac.uk/v2/object/O1224758) [V-TEXT]). Piciorul spate „supports the critical joints of the armrest, the seat and the back” ([Mattiazzi MC2](https://www.mattiazzi.eu/product/mc2-branca/) [V-TEXT]). Corecție de atribuire: Branca nu este de Grcic. Grcic a proiectat Medici.
- **Hiroshima** (Fukasawa, Maruni): 10 componente, CNC cu 5 axe plus finisare manuală, „construction remains fully visible” ([V&A O1420563](https://collections.vam.ac.uk/item/O1420563) [V-TEXT]).
- **Radice**: „no screws or metal fittings”, construită numai din lemn ([MC7](https://www.mattiazzi.eu/product/mc7-radice/) [V-TEXT]).
- **Chiaro**: structura de sub șezut s-a născut din intenția de a „simplify the joinery while reducing the total number of parts” ([MC8](https://www.mattiazzi.eu/product/mc8-chiaro/) [V-TEXT]).

Excepțiile documentate, cu îmbinare asumată ca expresie:

- **MC13 Facile** (Lambl/Homburger): „the elegant dovetail expresses a sturdy simplicity” ([Mattiazzi](https://www.mattiazzi.eu/product/mc13-facile/) [V-TEXT]);
- **BIGFOOT** (e15): picioare care traversează blatul și arată capătul fibrei, cu fisurile acceptate ca un caracter ([e15](https://www.e15.com/cms/en/ads-2/the-bigfoot-table/) [V-TEXT]);
- **Ponte** (e15): „a distinct corner joint detail” [V-TEXT, platformă];
- **Clifton** (Benchmark): „traditional exposed jointing”, masă pliabilă [V-TEXT, platformă];
- **Red Loop Windsor**: noduri sferice strunjite ([Wood Awards](https://www.woodawards.com/portfolio/the-red-loop-windsor/) [V-TEXT]).

Atelierele britanice premiate arată un model replicabil la scară mică. **Levity** (Wood Awards 2025) folosește CNC cu 5 axe pentru componente și „fixing joints”, curbare la abur și asamblare manuală ([Wood Awards](https://www.woodawards.com/portfolio/levity-collection/) [V-TEXT]). **Aran** (Morgan) este gândit explicit pentru dezasamblare. **A Forest Datum** (AA) este „demountable and assembled without permanent fixings” [V-TEXT].

Uncino, Forcina, Fronda, T chair, Cord și Tableau sunt hibride metal-lemn sau lipite cu rășină. **Nu sunt exemple lemn-lemn.**

Ergonomia are o singură ancoră normativă citită: **EN 1335-1, scaun de birou reglabil, înălțimea șezutului 400–510 mm, adâncimea 400–420 mm, lățimea ≥400 mm** ([INSST NTP 1129](https://www.insst.es/documentacion/colecciones-tecnicas/ntp-notas-tecnicas-de-prevencion/32-serie-ntp-numeros-1101-a-1135-ano-2018/ntp-1.129-criterios-ergonomicos-para-la-seleccion-de-sillas-de-oficina) [V-TEXT]). Tabelele EN 1729-1 și EN 527-1 nu au fost accesibile.

Baza empirică vine din 17 serii Mattiazzi cu cote oficiale [V-TEXT]:

| Tip | Înălțime șezut | Înălțime masă |
|---|---|---|
| Dining | 450–480 mm (grupat la 450–475) | 725–765 mm |
| Counter | 645–660 mm | — |
| Bar | 745–775 mm | — |
| Lounge | 390–450 mm | — |

Diferența masă–șezut iese **270–300 mm** [ESTIMARE]. **Nicio secțiune de picior sau traversă și nicio grosime de blat nu a fost confirmată oficial.** Fișele PDF Mattiazzi nu au putut fi descărcate.

## Setul de reguli: ce se codifică acum și ce nu

| # | Regulă de configurator | Formulă / prag | Bază | Nivel |
|---|---|---|---|---|
| R1 | Sarcină de calcul pentru scaun | șezut 1300 N, spătar 450 N, utilizator 110 kg | EN 12520:2024 [V-STD] | **SIGUR provizoriu** (recheck A1:2026) |
| R2 | Capacitatea statică a îmbinărilor de scaun | ≥ 2384 N (domestic) / 2780 N (mediu), la încărcare față-spate | Kılıç 2018 [V-TEXT] + [ESTIMARE] | SIGUR ca proxy |
| R3 | Moment M&T | E1 cu factor 1,5–2 sau 0,68×medie; E2/E3a pentru optimizare în fag | [V-TEXT] | SIGUR **numai în domeniu**. În afara lui: avertisment |
| R4 | Factor ciclic | 0,56 (cep dreptunghiular) / 0,67 (rotund) | [V-TEXT]/[REF] | SIGUR |
| R5 | Geometria cepului | w ≤ l ≤ 2w; t = 1/3–1/2 din traversă; umăr obligatoriu | [V-TEXT]/[REF] | SIGUR (t: implicit) |
| R6 | Ajustaj CNC (fag) | t: −0,2 mm; w: +0,1…0,2 mm; avertisment >0,3 | [V-TEXT]/[ESTIMARE] | SIGUR pentru fag, CONDIȚIONAT pentru restul |
| R7 | Sculă și colțuri | r_int = D/2; Ø ≥ adâncime/7,5; 90° la 3 axe | IBOIS [V-TEXT] | SIGUR |
| R8 | Diblu | E6/E7 cu b în țoli; adâncimea de încastrare maximizată; mai multe dibluri | [V-TEXT] | CONDIȚIONAT (unități) |
| R9 | Joc la mișcarea panoului | `gap = W·diff_T/100·ΔMC`, ΔMC = 5 | DIN 68100 [V-DATA] + [ESTIMARE] | SIGUR |
| R10 | Stabilitate depozitare | H >900 & m ≥10 kg sau H >350 & m ≥35 kg → ancoră + etichetă + calcul de răsturnare | EN 14749 [V-STD] | SIGUR |
| R11 | Masă delicată | blat ≤0,30 m², H ≥600 mm, m >10 kg → verificare de stabilitate | EN 12521 [V-STD] | SIGUR |
| R12 | Prindere degete / forfecare | fără Ø 7–12 mm cu adâncime ≥10 mm; fără goluri de 8–25 mm la piese mobile | [V-STD] | SIGUR |
| R13 | Încărcarea rafturilor | 0,65 kg/dm³ × volumul nișei → săgeată și îmbinare | EN 14749 [V-STD] | SIGUR (unitate de confirmat) |
| R14 | Panouri / formaldehidă | ≤0,062 mg/m³ ca flag de conformitate | REACH 2023/1464 [V-TEXT] | SIGUR |
| R15 | Dosar GPSR / EUDR | câmpuri obligatorii: marcare, lot, DDS, retenție 10/5 ani | [V-TEXT] | SIGUR |
| R16 | Secvența de asamblare | graf de blocare; 1DOF; piesă-cheie | IBOIS/Song [V-TEXT]/[REF] | SIGUR ca logică |
| R17 | Duritate | numai Brinell DIN 68364 | [V-DATA] | SIGUR. Janka WH: **INTERZIS** |
| R18 | Încovoiere Fb (Hu & Chen) | — | nu reproduce optimul | **INTERZIS** până la verificare în PDF |
| R19 | Îmbinări fără clei (pană, tusk, pană în cep orb) | — | fără date peer-review | **Numai tipologic**, fără capacitate numerică |

## Lacune / de verificat

Cele mai importante, în ordinea impactului asupra configuratorului:

| # | Lacună | De ce contează | Ce trebuie făcut |
|---|---|---|---|
| 1 | **EN 12520:2024/A1**, publicat de BSI la 15 ian. 2026, cu conținut necunoscut ([BSI](https://knowledge.bsigroup.com/products/bs-en-12520-2024-a1-furniture-safety-strength-and-durability-requirements-for-domestic-seating)) | Valorile 1300/450/110 au fost citite din preview-ul neamendat. Ambiguitatea 410 vs 450 N la spătar rămâne | Reverificare în textul consolidat SR EN la ASRO |
| 2 | **Coeficientul de joc al diblului**: b = 1 − 17,1·d | Cu d în mm, b iese negativ. Interpretarea în țoli (b ≈ 0,946 la 0,08 mm) e o deducție | Confirmare în Eckelman 1969 / Chen 2019 PDF |
| 3 | **Coloana de duritate din Wood Handbook** | Decalată la extracție | Recitire din PDF-ul original GTR-190/282, Tabel 5-3b |
| 4 | **Ecuația Fb Hu & Chen 2021** | Diferență de 17 % la punctul optim | Confruntare cu PDF-ul. Același lucru pentru simbolurile E4 și ordinea factorilor E9 |
| 5 | **Unitatea „kg/dm³” la bara de haine** (EN 14749) | Suspectă | De confirmat în text. Nivelurile de oboseală EN 12520, EN 16139 L1/L2, EN 15372 și forțele EN 16122 lipsesc |
| 6 | **Specii fără date oficiale** | Lipsesc: larice european; MOR/duritate nuc și cireș; densitate castan; clase EN 350 Anexa B pentru 10 specii; date românești Pro Ligno / INCD | Clasele „din memorie” (fag 5, castan 2 etc.) **nu se încarcă** |
| 7 | **Îmbinări fără clei, bridle, semilemn** | Zero date peer-review 2015–2026 | Pentru cep cu pană, tusk și coadă de rândunică glisantă cu pană, singura cale este încercarea proprie după EN 1728/1730 |
| 8 | **Umiditate și cicluri de umiditate** | Ținta EN 942:2007, pierderea de rezistență la cicluri RH și ajustajele pentru specii moi sunt necunoscute | De căutat surse |
| 9 | **Ergonomie și secțiuni** | Tabelele EN 1729-1 și EN 527-1 nu sunt accesibile. Secțiunile de picior/traversă ale pieselor contemporane nu sunt documentate oficial | De căutat fișele tehnice |
| 10 | **Regulament** | Nesigure: anul ESPR pentru mobilier (2028, sursă secundară), edițiile SR EN la ASRO, autoritatea și sancțiunile GPSR în România (ANPC neconfirmat oficial), pragurile EN 204:2016 | De confirmat la sursele oficiale |

## Concluzie

Constatarea principală: **standardele EN nu validează îmbinări, ci produse**. Configuratorul trebuie deci construit pe două straturi.

- **Stratul normativ** (EN 12520/12521/14749, GPSR, REACH, EUDR) produce *praguri de flag*: stabilitate, prindere degete, sarcini, documentație. Aceste praguri sunt sigure acum.
- **Stratul de rezistență** (E1–E3a, E6/E7, factorul ciclic 0,56, ajustajele Hu) produce *dimensionare prin analogie*. Ea e valabilă doar în ferestrele testate: fag și pin, cepuri de 10–50 mm, clei PVAc/PU.

Orice configurație în afara acestor ferestre ar trebui să genereze automat statutul „necesită încercare EN 1728/1730”, nu o valoare extrapolată.

A doua constatare privește designul contemporan documentat de muzee și producători. Acesta se mișcă spre **mai puține îmbinări, ascunse în piese sculptate CNC**, iar demontabilitatea revine pe agenda ESPR. Paradoxul este că exact îmbinările expresive și fără clei, care ar servi designul pentru dezasamblare, sunt cele **fără date de rezistență publicate**. Pentru dd, o serie mică de încercări proprii ar umple un gol real din literatură, nu doar o cerință de conformitate. Ea ar trebui să copieze sarcinile EN 12520 pe cep cu pană și pe coadă de rândunică glisantă, în fag și stejar românesc.
