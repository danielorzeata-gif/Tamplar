# Prețuri de referință lemn de mobilier — lei/m³

*Întocmit 08.10.2026 · curs folosit: 1 EUR ≈ 5,0 lei (orientativ, verifică cursul BNR)*

## Cum se citește

- **Medie anunțuri RO**: media prețurilor afișate de producători români pe AfaceriLemn.ro (cherestea brută, fără TVA declarat, fără transport). Există doar pentru stejar și fag. La celelalte esențe, producătorii nu afișează prețuri.
- **Valoare de lucru**: cifra de pus în calcul (și în `DEPOZIT_LEMN`) pentru cherestea **uscată 8–10%, tivită, clasa A/B**.
  - Stejar și fag: am pus valoarea puțin peste medie, pentru că anunțurile includ și clasa C sau lemn doar zvântat.
  - Celelalte esențe: **estimare** calculată din prețul german × raportul RO/DE măsurat la stejar și fag (≈ 0,26).
- **Interval RO**: plaja probabilă de preț la producător (raport 0,22–0,30 față de Germania).
- **Referință DE 2026**: lista de prețuri Mühlbauer Holz (Germania), februarie 2026, €/m³ net. Este **plafonul**, adică prețul de comerț din vest.

## Tabel principal

| Specie | Rol în mobilier | Medie anunțuri RO | **Valoare de lucru** | Interval RO estimat | Ref. DE 2026 (€/m³) | Încredere |
|---|---|---|---|---|---|---|
| **Stejar** (european, gorun) | PREMIUM integral, fețe vizibile STANDARD | 2.225 lei (4 prețuri) | **2.800 lei** | 1.800–3.500 lei | 1.790–1.920 | ●●● |
| **Fag** | piese ascunse, sertare, structură; aburit pentru fronturi vopsite | 1.065 lei (3 prețuri) | **1.300 lei** | 1.000–1.500 lei | 920–1.130 | ●●● |
| **Frasin** (alb) | alternativă deschisă la stejar, design contemporan | — | **1.950 lei** | 1.650–2.250 lei | 1.450–1.550 | ●●○ |
| **Paltin** (european) | fețe deschise, blaturi, piese fine | — (1 anunț 1.100 lei, unitate neclară) | **1.450 lei** | 1.200–1.650 lei | 1.050–1.150 | ●●○ |
| **Tei** | interior sertare, spate, piese sculptate | — | **1.400 lei** | 1.200–1.600 lei | 1.050–1.110 | ●○○ |
| **Cireș** (european) | PREMIUM accent, fronturi | — | **1.700 lei** | 1.450–1.950 lei | 1.300 | ●○○ |
| **Nuc** (european, aburit) | PREMIUM accent, blaturi, mânere | — | **3.200 lei** | 2.700–3.700 lei | 2.450–2.700 | ●○○ |
| **Salcâm** | exterior, piese rezistente | — | **1.600 lei** | 1.400–1.900 lei | 1.250 | ●○○ |
| **Ulm** | accent, desen puternic | — | **1.750 lei** | 1.500–2.000 lei | 1.350 | ●○○ |
| **Arin** | piese ascunse, vopsit | — | **1.100 lei** | 950–1.300 lei | 850–890 | ●○○ |
| **Plop** | spate, fund sertare, vopsit | — (cerere: 850 lei) | **900 lei** | 800–1.000 lei | — | ●○○ |
| **Carpen** | piese ascunse, ghidaje, cepuri | — | **800 lei** | 650–900 lei | 600 | ●○○ |
| *Rășinoase (molid/brad)* | *comparație / structură* | *900 lei (depozit București)* | *900 lei* | — | — | ●●○ |

**Încredere:** ●●● = prețuri reale din anunțuri RO · ●●○ = estimare plus un reper RO · ●○○ = doar estimare din raportul RO/DE.

### Datele brute pentru stejar și fag

| Firmă | Produs | Preț |
|---|---|---|
| Gerend (Covasna) | gorun netivit zvântat 25–32 mm | 1.800 lei/m³ |
| DORA (Alba) | stejar uscat 30/55 mm A/B/C | de la 400 EUR ≈ 2.000 lei/m³ |
| DORA (Alba) | stejar tivit uscat 14%, 30–55 mm, A/B/C | 2.100 lei/m³ |
| Teo-Da Wood (Sălaj) | stejar netivit uscat în uscător, A/B/rustic | 3.000 lei/m³ neg. |
| DORA (Alba) | fag AB 28/55 mm | 1.000 lei/m³ |
| Dej (Cluj), 2 anunțuri | fag netivit, diverse clase | 1.100 lei/m³ |

## Preț pe m² de placă (pentru `DEPOZIT_LEMN`)

`lei/m² = lei/m³ × grosime brută (m)`, calculat cu valoarea de lucru.

| Specie | 29 mm brut (→ ~22 finit) | 55 mm brut (→ ~45 finit) |
|---|---|---|
| Stejar | 81 lei/m² | 154 lei/m² |
| Fag | 38 lei/m² | 72 lei/m² |
| Frasin | 57 lei/m² | 107 lei/m² |
| Paltin | 42 lei/m² | 80 lei/m² |
| Tei | 41 lei/m² | 77 lei/m² |
| Cireș | 49 lei/m² | 94 lei/m² |
| Nuc | 93 lei/m² | 176 lei/m² |
| Salcâm | 46 lei/m² | 88 lei/m² |
| Ulm | 51 lei/m² | 96 lei/m² |
| Arin | 32 lei/m² | 61 lei/m² |
| Plop | 26 lei/m² | 50 lei/m² |
| Carpen | 23 lei/m² | 44 lei/m² |

## Cât comanzi: de la necesarul finit la volumul brut

**Volum de comandat = volum finit (din cut list) × factor de randament**

Factorii de mai jos sunt orientativi. Includ rindeluirea pe grosime (29 → 22 mm înseamnă deja ×1,3), fălțuirea marginilor, capetele crăpate, nodurile și alburnul.

| Ce cumperi | Factor |
|---|---|
| Semifabricate / frize calibrate A/B | × 1,15–1,25 |
| Tivit uscat, clasa A | × 1,4–1,5 |
| Tivit uscat, clasa B / AB | × 1,6–1,8 |
| Netivit uscat (A/B) | × 1,8–2,2 |
| Clasa C / rustic | × 2,0–2,5 |

**Exemplu:** 0,50 m³ stejar finit (cut list), cumpărat tivit B:
0,50 × 1,7 = **0,85 m³ brut** × 2.800 lei = **2.380 lei** + transport.
Dacă vânzătorul cubează la grosimea nominală (debitat 29, facturat 26), plătești de fapt cu ~10% mai puțin pe m³ real.

**Costuri de adăugat peste prețul pe m³:**
- **Transport.** Spectrum (Harghita) livrează gratuit. Altfel, ~3–6 lei/km pentru o dubă, sau curse grupate.
- **TVA 21%.** Prețurile din anunțuri sunt de obicei fără TVA.
- **Uscare suplimentară** pentru lemnul „zvântat”: cere oferta de uscător de la vânzător.
- **Comandă minimă.** De regulă 1 palet (~1–1,5 m³) sau 3 m³ (Manuco).

## Reper: cât costă în magazin, ca să compari

| Produs | Preț | ≈ lei/m³ |
|---|---|---|
| Riglă molid rindeluită 18×114×2000 (Dedeman) | 22,68 lei/buc | ~5.500 lei/m³ |
| Plintă masivă stejar 12×60×2000 (Hornbach) | 62 lei/buc | ~43.000 lei/m³ |
| Panouri încleiate în dinți (tei, cireș, paltin, stejar), 18–43 mm (Argeș) | de la 25 EUR/m² | ~6.250 lei/m³ la 20 mm |

Cherestea brută de la producător, uscată și rindeluită de tine, iese de **2–5 ori mai ieftin** pe m³ util decât lemnul gata rindeluit din magazin. Diferența se plătește în timp de lucru și în pierderi.

## Limite

- Doar stejarul și fagul au prețuri românești reale (7 cifre). Restul sunt **estimări derivate**: verifică-le cu 1–2 telefoane înainte să le pui în ofertele de vânzare.
  - Nordik Express (Timiș, 0745 525 203) vinde frasin, paltin, tei, cer și cireș, uscate 8–10%.
- Prețurile germane sunt prețuri de comerț (cu marjă), nu de producător. Raportul 0,26 e calibrat doar pe stejar și fag, deci poate să nu se potrivească la esențele rare (nuc, cireș), unde oferta în RO e mică.
- Clasa și umiditatea pot schimba prețul cu ±30–50% în cadrul aceleiași specii.

## Surse

- [AfaceriLemn.ro — cereri și oferte cherestea stejar](https://afacerilemn.ro/cereri-oferte-cherestea-stejar/p-1) și [fag](https://afacerilemn.ro/cereri-oferte-cherestea-fag/p-1), plus filtrele pe specie ([paltin](https://afacerilemn.ro/cereri-oferte/f-Paltin/p-1), [frasin](https://afacerilemn.ro/cereri-oferte/f-Frasin/p-1), [nuc](https://afacerilemn.ro/cereri-oferte/f-Nuc/p-1), [tei](https://afacerilemn.ro/cereri-oferte/f-Tei/p-1), [cireș](https://afacerilemn.ro/cereri-oferte/f-Cires/p-1))
- [Mühlbauer Holz — listă prețuri Schnittholz, feb. 2026 (€/m³ net)](https://www.muehlbauerholz.com/files/holz_schnittholz.pdf)
- [Drauholz (AT) — listă prețuri brută, 2019 (verificare istorică)](https://www.drauholz.at/media/1704/bruttopreisliste-2021-02-09.pdf)
- [Holzquelle — oferte nuc (DE/AT)](https://holzquelle.com/kaufen-verkaufen/schnittholz/nuss)
- [LemnSuperMarket — cherestea paltin](https://www.lemnsupermarket.ro/cherestea-paltin-6706)
- [Cherestea București — depozit](https://cheresteabucuresti.ro/depozit/cherestea/)
- [Hornbach](https://www.hornbach.ro/s/stejar%20rindeluit) · [Dedeman — cherestea](https://www.dedeman.ro/ro/cherestea/c/405)
