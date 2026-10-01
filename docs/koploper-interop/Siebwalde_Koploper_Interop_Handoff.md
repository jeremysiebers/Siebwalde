# Siebwalde — overdracht Koploper JSIF → Simple Loop en toekomstige LayoutProfile-interoperabiliteit

Datum: 2026-10-01  
Doelgroep: Siebwalde Project Lead / Architect / Developer / Integrator  
Status: **referentiegegevens en onderzoeksresultaten; niet een generieke, productierijpe Koploper-parser**.

## 1. Aanleiding en feitelijk operatorresultaat

De Product Owner leverde `JSIF.zip` aan: een bestaande Koploper-testbaan met vijf blokken, passeerlus/wissels en twee bezetmelders per blok. Een afgeleide `JSIF_SimpleLoop_4Amp.zip` is geconstrueerd: vier blokken, geen wissels, één melder per blok en een gesloten keten 1→2→3→4→1. De Product Owner heeft op 2026-10-01 bevestigd dat Koploper deze aangepaste configuratie heeft ingelezen en dat het resultaat goed lijkt. **Dit bewijst een geslaagde eerste operator-import/visuele plausibiliteit, nog geen automatische route-, stop-/rem- of hardwarevalidatie.**

Bewaar origineel en afgeleid profiel beide als regressie-/interoperabiliteitsfixtures. De bestanden zijn in deze bundle meegeleverd, met controleverslag en de eenmalige transformatie-/validatiescripts.

## 2. Inhoud en technische eigenschappen van de bron

- Dit specifieke `JSIF.zip` is een gewone, **niet-versleutelde ZIP** van een Koploper-datadirectory met root `JSIF/`; het is **geen `.bck`-container**. De gewijzigde ZIP heeft dezelfde root.
- Oorspronkelijk 13 ZIP-entries, aangepast 12 (de oude `JSIF/save_oud.txt` is bewust weggelaten omdat deze oude runtime-/layoutreferenties bevat).
- Geobserveerde `.dba`-tekstrecords zijn tabgescheiden; de onderzochte regels gebruiken CRLF en Windows-1252/cp1252. Behoud onbekende recordtypes, velden en bytes waar mogelijk. `kopl.ini` bevat onder meer unieke nummering.
- ZIP-library rekent ZIP-CRC en entrygroottes opnieuw uit. Dat is archiefintegriteit; het is niet vanzelf een verklaring van alle Koploper-applicatie-invarianten.
- Het bewust publieke wachtwoord voor andere Koploper-backuparchieven, elders in `docs/koploper-interface.md`, is **niet nodig om dit specifieke JSIF.zip te openen**.

### Observed record inventory (alleen voor deze dataset bewezen)

| Bestand | Waargenomen records / functie in de uitgevoerde omzetting |
|---|---|
| `baan.dba` | `BAAN` pagina, `LIJN` geometrie, `PBLK`/`LIBL` grafische blokassociaties, `INBL`/`INBV`/`BZWL` feedback in het baanbeeld, `WISS`/`WSTR` wisseltekening. |
| `blok.dba` | `BLOK` blokdefinitie/feedbackvelden; `BLVN` voorganger en routegebonden feedback; `BLRI` rijrelaties; `DLCK` oude passeerlus-/deadlockinstellingen. |
| `kopd.dba` | `BLAV1..5` overige blokrelaties, `LOKO1..2` locomotiefdefinities, veel ongewijzigde algemene records. |
| `locr.dba` | Opgeslagen plaatsing van locomotieven. |
| `kopl.ini` | Onder meer de laatst gebruikte unieke blok-ID. |
| `snel.dba` | Oude baan-/sensorafhankelijke kalibratie; voor het nieuwe profiel bewust leeggemaakt. |
| `save_oud.txt` | Historische runtime-snapshot; niet in nieuwe ZIP opgenomen. |

**Pas op:** de referentiescripts gebruiken geobserveerde positionele veldindices voor deze concrete JSIF-variant. Behandel niet elk indexnummer als universeel of volledig gedocumenteerd Koploper-formaat. Ontwikkel bij een algemene reader eerst een expliciete versie-/variant-/schema-aanpak.

## 3. Uitgevoerde mapping — Simple Loop

| Koploper-blok | Bezetmelder | Intern feedback-ID | Logische sectie | Beoogde fysieke amplifier-binding | Volgend blok |
|---:|---|---:|---:|---:|---:|
| 1 | 1.01 | 101 | 1 | 1 | 2 |
| 2 | 1.02 | 102 | 2 | 3 | 3 |
| 3 | 1.03 | 103 | 3 | 4 | 4 |
| 4 | 1.04 | 104 | 4 | 6 | 1 |

In de gewijzigde Koploper-data zijn de locdecoderadressen **1 en 2** behouden en blijven de bestaande ECoS-object-ID's **1000 en 1001** behouden; de opgeslagen plaatsing is loc 1 → blok 1 en loc 2 → blok 3. **Decoderadres is niet hetzelfde als ECoS-object-ID.** Verifieer de betekenis in de actuele C#-mapping; hergebruik geen 1000/1001 als DCC-decoderadres zonder onderbouwing.

De relatie tot fysieke slaves **1/3/4/6** is een toekomstige **Siebwalde C# Real-binding**, niet iets wat Koploper in zijn eigen bloknummers direct uitdrukt. Het gegenereerde ZIP-bestand heeft `simple-loop.json` in de C# repository niet aangepast.

Wat daadwerkelijk in de afgeleide ZIP veranderde:
- gesloten grafische ovaal met acht `LIJN`-segmenten en vier `PBLK`-blokken;
- twee wissel-/passeerlus-objecten en blok 5 verwijderd;
- één `INBL`, `INBV`, `BZWL` en bijbehorende `BLOK`/`BLVN`-feedback per blok, IDs 101..104;
- vier geldige gesloten `BLVN`/`BLRI`/`BLAV`-relaties;
- twee locdefinities behouden en opgeslagen locaties aangepast;
- oude snelheidsmetingen gereset; oude runtime-snapshot niet meegeleverd;
- alle andere bronbestanden byte-identiek behouden.

## 4. Uitgevoerde validatie en bewijsgrens

Zie `docs/JSIF_SimpleLoop_Controle.md` en `reference_scripts/validate_simple_loop.py` voor volledige referentiecontroles. Gecontroleerd zijn:

1. vier blokken en sluitende relaties 1→2→3→4→1 zonder blok 5;
2. één feedback-ID per blok in de bijbehorende records;
3. grafische `LIJN`-graaf: enkelvoudige, aangesloten gesloten lus, geen wissel-/passeertak;
4. locdefinities bestaan nog, opgeslagen locblokken bestaan;
5. geen overgebleven oude speed-calibration/snapshot;
6. ZIP-test/CRC PASS, bron ongewijzigd, onbekende niet-doelrecords behouden;
7. operator bevestigde dat Koploper het gegenereerde archief inleest en dat het resultaat goed lijkt.

**Niet bewezen:** volledige semantiek van elk `.dba`-veld; algemene compatibiliteit met andere Koploper-dataversies; automatisch rijden/stoppen met één bezetmelder per blok; echte hardwarebezetting en elektrische veiligheid. Bewaar daarom validatie- en operatortestgates voor iedere toekomstige export.

## 5. Wat de Project Lead hieruit moet halen

### A. Nu bruikbaar bij actieve ontwikkelstap 1 (software-only)

- Neem deze twee Koploper-datasets en het controlerapport als **fixtures/referentie** voor de actieve `Simple Loop — C# / Real-mode Readiness` opdracht.
- Vergelijk bestaande `simple-loop.json` en ECoS/Koploper-mapping met de tabel hierboven; maak het onderscheid tussen logische sectie, feedback-ID, DCC-decoderadres, ECoS-object-ID en fysiek amplifieradres expliciet.
- Eén logische Simple Loop blijft dezelfde tussen FullSimulation en latere Real mode; fysieke 1/3/4/6-binding mag afzonderlijk zijn.
- Voeg alleen de mappings-/validatiewijzigingen toe die binnen het geautoriseerde huidige increment nodig zijn. Geen fysieke bediening of firmwareflashing.

### B. Nieuwe feature voor later: Koploper → `LayoutProfile` importer

Ontwerp eerst een **read-only importer** die uit een bekende Koploper-variant een **kandidaat-LayoutProfile** plus diagnose-/provenancerapport afleidt:

1. Archiefingang onderscheiden: gewone datadirectory-ZIP (`JSIF/`) versus eventuele `.bck`-/versleutelde backups. Bouw daarvoor zo nodig verschillende adapters; neem geen gedeeld containerformaat aan.
2. Formaatherkenning, expliciete decoding, behoud van raw/unknown velden; geen stilzwijgende recordverwijdering.
3. Parseer en kruiscontroleer blokken, route-/voorganger-relaties, feedback-ID's, grafische relaties, wisselobjecten, locadressen en relevante metadata.
4. Maak semantische categorieën apart: logische topology, feedbackmapping, wissel-/routecondities, locinformatie, displaygeometry en optionele fysieke bindings.
5. Ondersteun **0..n melders per blok** in het algemene model voor zover de werkelijke eisen dit vragen: één melder/blok is alleen de Simple Loop-testcase, niet de volledige Siebwalde-schemaregel. Een ontoereikende mapping moet worden afgewezen/gemarkeerd; niet stilzwijgend worden gecorrigeerd.
6. Valideer alle onderlinge verwijzingen, duplicaten, onmogelijke/ontbrekende routes, route-/wisselconsistentie en conflicterende feedback-ID's. Geef bewijs per bronbestand/record/veld en markeer onbegrepen velden als unknown.
7. Genereer `LayoutProfile` **als voorstel** en een reviewbare diff ten opzichte van de bestaande C#-config; overschrijf geen productconfig of Real-mode binding zonder expliciete beoordeling/acceptatie.
8. De volledige Siebwalde-Koploper-database kan later als grotere referentiedataset dienen; maak geen fictieve ontbrekende fysieke mapping.

**Source-of-truth:** Koploper beschrijft zijn eigen configured blocks/routes/feedback; C# moet waar mogelijk uit dezelfde gevalideerde bron kunnen initialiseren. Dit betekent niet dat een grafische `LIJN`-tekening op zichzelf elektrische secties of fysieke amplifieradressen bewijst. Die fysieke werkelijkheid blijft expliciet te binden en later hardwarematig te verifiëren.

### C. Daarna: gecontroleerde export/generator voor Koploper

Pas nadat de read-only importer en referentietests betrouwbaar zijn: ontwerp een exporter die een **nieuw, Koploper-inleesbaar kandidaatarchief** produceert vanuit een gevalideerd profiel/template. Wijzig conservatief bekende records, behoud onbekende content/formatvarianten zo veel mogelijk, controleer cross-file-invarianten, reset of migreer afhankelijk runtime-/kalibratiedata expliciet, controleer ZIP-integriteit, valideer opnieuw door dezelfde importer, en voer altijd een operator-open-/GUI-acceptance uit op een **testkopie**. Nooit direct de operationele Koploper-database overschrijven.

**De referentiescripts in deze bundle zijn op maat geschreven voor dit ene JSIF-paar**; zij zijn geschikt als geobserveerde voorbeeldtransformatie en testoracle, niet als generieke exporttool. Ook zijn de bron- en uitvoerpaden hierin nog `/mnt/data/...` en moeten die voor repo-gebruik worden geparametriseerd.

### D. Toekomstig gebruik: volledige Siebwalde

Als de hele fysieke modelbaan in Koploper is gedefinieerd, kunnen blokken, sensoradressen, routes, wissels en nummering als kandidaatconfiguratie worden geïmporteerd. C# init kan dan een versie-/hashgebonden, door de Product Owner goedgekeurd `LayoutProfile` laden. Fysieke sectie→amplifier / backplane- / speciale-I/O-bindingen vereisen een eigen autoritatieve bron en mogen niet worden afgeleid uit alleen Koploper-labels. Valideer op representatieve echte bestanden en behoud audittrail.

## 6. Aanbevolen gefaseerde backlog, geen scope creep

- **Nu:** dataset overdragen, C# Simple Loop-mapping in actieve stap 1 aligneren en deze fixtures als referentie vastleggen.
- **Later, apart geautoriseerd increment:** read-only Koploper-importer, toetsing tegen Simple Loop en Koploper Oval én volledige Siebwalde-dataset zodra beschikbaar.
- **Daarna apart:** exporter/nieuwe Koploper-configuraties, roundtrip- en operatoracceptance.
- Dit versnelt de vierstappen Simple Loop-roadmap; het mag die roadmap niet ongevraagd onderbreken of stap 2/3/4 vervroegd starten.

## 7. Meegeleverde bestanden

- `fixtures/JSIF_original.zip` — oorspronkelijk vijfbloksmateriaal (ongewijzigde bytes).
- `fixtures/JSIF_SimpleLoop_4Amp.zip` — Koploper-geaccepteerde, afgeleide vierblokstestkopie.
- `docs/JSIF_SimpleLoop_Controle.md` — mapping, bestandswijzigingen en structurele checks.
- `reference_scripts/build_simple_loop.py` — eenmalige transformatie, geen algemene encoder.
- `reference_scripts/validate_simple_loop.py` — eenmalige referentietests, geen algemene parser.
- `mapping_reference.json` — expliciete voorbeeld-relatie; **niet** automatisch het productie-`LayoutProfile` JSON-schema.
- `SHA256SUMS.txt` — bestandsintegriteit.

Neem bij repo-overname eerst een Architect/Developer/Integrator-besluit over plaatsing van testfixtures, tooling en documentatie. Bewaar originele fixtures en verzin geen ontbrekende veldsemantiek.
