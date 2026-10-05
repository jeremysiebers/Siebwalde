# Koploper 9.4 Internal State → Siebwalde C# Integration
## Authoritative Technical Handoff — PoC 01 t/m PoC 06

**Document status:** authoritative handoff voor vervolgontwikkeling  
**Datum:** 2026-10-05  
**Project:** C# Siebwalde  
**Koploper target:** 9.4 build 9 (`9.4.0.9`)  
**Gevalideerde EXE SHA-256:** `645B4681C14975F3619EB44968C74F3B7925CB914C5A23302932918D73079D2E`  
**Architectuur:** native Win32 / PE32 / i386 / Delphi  
**Doelgroep:** ChatGPT, OpenCode/DeepSeek Project Lead, Architect, Developer, Integrator  

---

# 1. Doel van dit document

Dit document bevat de volledige technische overdracht van het onderzoek naar het uitlezen van de **interne runtime-status van Koploper 9.4 zonder sourcecode**.

Het primaire doel voor Siebwalde is niet alleen weten waar een locomotief *nu* is, maar vooral weten welke toekomstige blokken Koploper al aan die locomotief heeft **gereserveerd**. De C#-besturing moet daarmee vooraf weten welke fysieke track-amplifiers bij dezelfde locomotief horen en dus welk locomotiefsetpoint/PWM-doel zij moeten volgen.

De kernvraag was:

> Kan een externe C#-applicatie betrouwbaar en read-only uitlezen welke blokken Koploper voor welke locomotief heeft gereserveerd, zonder Koploper te patchen en zonder 30 locomotieven × 50 blokken = 1500 handmatig geconfigureerde logische acties?

**Antwoord na PoC 01–06:** ja, voor normale reserverings-/bezettingstrajecten is dit met hoge betrouwbaarheid aangetoond. De relevante per-blok owner- en statusvelden zijn gevonden en in Koplopers standaard simulator runtime gevalideerd. Een expliciete intrekking van een reeds bestaande reservering zonder tussentijdse bezetting is nog niet succesvol uitgelokt en blijft een open validatiepunt.

Dit document moet voortaan als **primary technical source** worden gebruikt. De losse PoC-bestanden blijven evidence en detailbron, maar nieuwe agents moeten niet vanaf nul opnieuw reverse-engineeren.

---

# 2. Projectdoel binnen Siebwalde

Koploper blijft eigenaar van onder andere:

- treinplanning;
- rijwegkeuze;
- blokreservering;
- locomotiefadministratie;
- rem-/stopplanning;
- wisselstraatlogica.

Siebwalde C# blijft eigenaar van de vertaling naar de eigen hardware, onder andere:

- track-amplifier-toewijzing;
- PWM/setpoint-distributie;
- richting;
- fysieke occupancy;
- freshness;
- safety-interlocks;
- hardwarediagnostiek.

Gewenst conceptueel gedrag:

```text
Koploper
  Loc X:
    occupied block = 17
    reserved blocks = 19, 20
          |
          v
KoploperReservationObserver (read-only)
          |
          v
Siebwalde logical block ownership
          |
          +---- block 17 -> Loc X
          +---- block 19 -> Loc X
          +---- block 20 -> Loc X
          |
          v
Track/amplifier assignment
          |
          v
bestaande safety/interlocklaag
          |
          v
hardware output
```

**Belangrijk:** `reserved by Koploper` betekent **niet automatisch hardware-output toegestaan**. De reserveringsinformatie is een input voor logisch eigenaarschap en setpointdistributie. Fysieke occupancy, amplifier health, freshness, richting en bestaande safety-interlocks blijven onafhankelijk beslissend.

---

# 3. Waarom de bestaande externe interfaces niet voldoende waren

## 3.1 ECoS-emulator

De bestaande Siebwalde ECoS-emulator ziet onder andere locomotiefcommando's, rijstappen, richting, wisselcommando's en occupancy-communicatie. Dit toont echter niet rechtstreeks Koplopers volledige vooruitkijkende reserveringsadministratie.

## 3.2 Lokale poort 5700

Poort 5700 was reeds in Siebwalde in gebruik om locomotiefpositie te volgen. Packet-analyse bevestigde een leesbaar formaat:

```text
&<loc> ESC <blok> ESC <tijd> ESC <kloktijd> ESC <routetekst> ESC
```

Voorbeeld uit de capture:

```text
&1 ESC 3 ESC 00:50:47 ESC 13:06:29 ESC Route onbekend ESC
```

De events volgden specifieke ECoS-bezetmeldingen met ongeveer 7–16 ms vertraging en zijn zeer bruikbaar voor administratieve **positie-events**. Ze komen echter pas bij de blokovergang en leveren niet de volledige toekomstige reserveringsset.

**Conclusie:** 5700 blijft nuttig als onafhankelijke diagnostische/cross-checkbron, maar is niet de primaire bron voor toekomstige reserveringen.

## 3.3 Koploper netwerkserver / TCP 5000

De netwerkversie is onderzocht omdat meerdere Koploper-instanties via een server baansecties kunnen koppelen. Capture op TCP/5000 liet een protocol met lengte-prefix en periodieke statusframes zien, onder andere:

- server → client: `00 03 66`;
- client → server: 67-byte `00 43 67 ...` statusframe.

Dit verkeer bleek in de uitgevoerde test vooral globale/statistische status te bevatten en niet de complete lokale per-loc reserveringstabel.

De netwerkfunctionaliteit blijft een nuttige reverse-engineeringingang, maar is **niet nodig als primaire runtime-interface** voor Siebwalde.

## 3.4 Koploper logische acties

Koploper ondersteunt onder andere logische condities zoals:

- `Blok is gereserveerd door locomotief`;
- `Blok is gereserveerd door locomotief komend uit blok`;
- `Blok is geclaimd door locomotief`.

Deze kunnen in principe naar virtuele decoder-/wisseluitgangen worden vertaald, maar voor 30 locomotieven × 50 blokken zou dat circa **1500 combinaties** opleveren, exclusief onderhoud en adresmapping.

**Conclusie:** bruikbaar als handmatige ground-truth-test voor één combinatie, niet als productiearchitectuur.

---

# 4. Waarom extern geheugen uitlezen op Windows mogelijk is

De oplossing omzeilt Windows-geheugenbeveiliging niet. De observer gebruikt de normale Windows debug-/diagnose-API:

- `OpenProcess(...)`;
- `ReadProcessMemory(...)`.

Het doelproces blijft in zijn eigen virtuele adresruimte. Windows valideert de access rights en kopieert gevraagde bytes gecontroleerd naar het observerproces.

De uiteindelijke observer moet uitsluitend minimaal benodigde read/queryrechten gebruiken, bijvoorbeeld:

```text
PROCESS_QUERY_LIMITED_INFORMATION
PROCESS_VM_READ
```

Niet gebruiken:

- `WriteProcessMemory`;
- DLL-injectie;
- code hooks;
- runtime patching;
- gemodificeerde `koploper.exe`.

ASLR betekent dat absolute runtime-adressen per processtart kunnen veranderen. Daarom mogen run-specifieke pointers zoals `0x028A981C` nooit hardcoded worden. De oplossing gebruikt een versioned RVA naar de rootpointerlocatie, leest de actuele runtime-pointer en volgt vervolgens de actuele objectpointers.

---

# 5. Binary identity en version gating

De reverse engineering en runtimevalidatie zijn uitgevoerd op:

| Eigenschap | Waarde |
|---|---|
| Product | Koploper |
| Versie | 9.4 build 9 / `9.4.0.9` |
| EXE size | 4,682,752 bytes |
| SHA-256 | `645B4681C14975F3619EB44968C74F3B7925CB914C5A23302932918D73079D2E` |
| Type | PE32 Windows GUI, i386 |
| Runtime | native Delphi |
| Preferred image base | `0x00400000` |

**Productieregel:** een memory-layoutprofiel mag alleen worden geactiveerd wanneer executable identity/version/hash expliciet matcht. Bij mismatch wordt geen bekende offset toegepast en wordt de Koploper state source `UnsupportedVersion/Unknown`.

---

# 6. Reverse-engineeringresultaten — bewezen runtime-layout

## 6.1 Centrale rootpointer

Ghidra preferred VA:

```text
PTR_DAT_007259B0
```

Bij imagebase `0x00400000`:

```text
RVA = 0x003259B0
```

Runtimeconcept:

```text
moduleBase = actual base address of koploper.exe
rootPointerAddress = moduleBase + 0x3259B0
root = ReadU32(rootPointerAddress)
```

Een geobserveerde root tijdens één PoC-run was:

```text
0x028A981C
```

Dit adres is uitsluitend een runtimevoorbeeld en **mag nooit hardcoded worden**.

## 6.2 Centrale objectlijsten

Binnen het rootobject:

| Offset | Betekenis | Status |
|---|---|---|
| `root + 0x5AC` | lijst van `TBlok*` | runtime bevestigd |
| `root + 0x5C8` | lijst van locomotiefobjecten | runtime bevestigd |

Ghidra-functies:

- `FUN_00676330`: lookup block op interne block-ID;
- `FUN_00676380`: lookup locomotive op interne loc-ID.

## 6.3 Delphi `TList` layout

Voor de gevonden eenvoudige lijsten:

```text
+0x00  VMT pointer
+0x04  pointer naar 32-bit item-pointerarray
+0x08  count
+0x0C  capacity
```

Deze layout werd uit Delphi RTL-routines en runtimegedrag afgeleid.

## 6.4 `TBlok` relevante velden

| Offset | Betekenis | Vertrouwen / status |
|---|---|---|
| `+0x14C` | interne block-ID | hoog / runtime bevestigd |
| `+0x15C` | zichtbaar/display bloknummer kandidaat | hoog, maar mapping per database expliciet valideren |
| `+0x1AC` | toegewezen `Loc*` ownerpointer | hoog / runtime bevestigd |
| `+0x1ED` | blokstatus | hoog / statisch + runtime gevalideerd |
| `+0x1EE` | statuswijzigingsflag | statisch aangetoond, nog niet als publieke semantiek nodig |
| `+0x1F0` | update-/tijdteller rond statusupdate | statisch aangetoond; precieze tijdsemantiek nog niet nodig |

### Gevalideerde statusinterpretatie

| Raw status | Gevalideerde interpretatie | Productiebehandeling |
|---:|---|---|
| `0` | vrij / geen owner | Free-kandidaat, mits coherente snapshot |
| `1` | vooraf gereserveerd voor owner-loc | Reserved |
| `2` | administratief bezet door owner-loc | Occupied |
| `9` | transitie-/vrijgave-/resetstatus | **Transition/Unknown**, niet als Free behandelen |
| anders | niet gevalideerd | Unknown |

De normale, herhaald waargenomen cyclus is:

```text
0 -> 1 -> 2 -> 9 -> 0
free   reserved   occupied   transition   free
```

Directe `9 -> 1` herreservering is eveneens waargenomen.

## 6.5 Locomotiefobject relevante velden

| Offset | Betekenis | Status |
|---|---|---|
| `+0x1A8` | interne locomotief-ID | hoog / runtime bevestigd |
| `+0x58` | current/occupied block reference kandidaat | zeer sterk empirisch; 913/915 PoC05-records matchten status-2 blok |
| `+0x54` | vorige/andere block reference kandidaat | niet definitief gedecodeerd |

`Loc+0x58` is nuttig voor coherentiecontrole maar **geen onafhankelijke safety-authority**.

---

# 7. Belangrijke Ghidra-functies

Onderstaande namen zijn Ghidra-namen; de semantische naam is onze onderzoeksnaam, niet de originele Delphi-methodenaam tenzij expliciet bekend.

| Ghidra-functie | Geobserveerd gedrag | Status |
|---|---|---|
| `FUN_00676330` | block lookup via `root+0x5AC`, key `TBlok+0x14C` | bewezen |
| `FUN_00676380` | loc lookup via `root+0x5C8`, key `Loc+0x1A8` | bewezen |
| `FUN_006D8118` | schrijft statusbyte `TBlok+0x1ED`, markeert update | bewezen |
| `FUN_006DF0C8` | zet `TBlok+0x1AC=Loc`, eindigt met status `1` | bewezen codepad, runtime semantiek = reservering sterk bevestigd |
| `FUN_006DEFE0` | zet owner/admin block state en status `2` | bewezen codepad, runtime semantiek = bezet bevestigd |
| `FUN_006DF8B0` | wist owner, zet status `0` | bewezen |
| `FUN_006DF74C` | vrijgave/resetpad, zet status `9` | bewezen codepad, precieze interne naam niet nodig |
| `FUN_006D7A80` | voegt loc toe aan `TBlok+0x16C` keyed collection | bewezen codegedrag; semantiek = claimpad |
| `FUN_006D7AE4` | verwijdert loc uit `TBlok+0x16C`; `-1` wist collectie | bewezen codegedrag |
| `FUN_006DDB14` | claim/collision membership-check | bewezen codegedrag |
| `FUN_006DDB4C` | membership in `+0x16C` | bewezen codegedrag |
| `FUN_006AB81C` | route/kandidaatitems propagatie naar claimcollectie | bewezen codepad, geen productiebron |
| `FUN_006A9498` | getter `*(x+0x58)` | bewezen getter; objectsemantiek afgeleid |
| `FUN_006A949C` | getter `*(x+0x54)` | bewezen getter; semantiek open |

---

# 8. Afgewezen of begrensde hypotheses

Nieuwe agents moeten onderstaande paden **niet opnieuw als primaire reserveringsbron implementeren** tenzij nieuw bewijs daartoe aanleiding geeft.

## 8.1 `TBlok+0x16C`

Dit is een Delphi `TStringList`-achtige keyed collectie waarin locomotiefobjecten kunnen worden toegevoegd/verwijderd. Statische analyse toont duidelijk een claimmechanisme.

Runtime in normale simulator:

- PoC03: 5400 samples, count steeds 0;
- PoC04: 10.800 block samples, count steeds 0.

**Conclusie:** dit is niet de normale langdurige reserveringsset die Siebwalde nodig heeft. Waarschijnlijk claim-/kandidaatlogica of zeer tijdelijke toestand.

## 8.2 `TBlok+0x24C` en `TBlok+0x258`

PoC04:

- altijd NULL.

Geen productiebetekenis aannemen.

## 8.3 `TBlok+0x25C`

PoC04:

- constant `255`.

Niet relevant gebleken voor normale reservering.

## 8.4 `Loc+0x4D8`

Statisch een kandidaat route-itemlijst, maar in PoC04:

- alle 1080 locmetingen: count 0.

Niet gebruiken als primaire reserveringsbron.

## 8.5 Alleen `TBlok+0x1AC`

Een eerdere aanname was dat `+0x1AC` uitsluitend de huidige locpositie was. Dit bleek te beperkt: dezelfde loc kon tegelijkertijd aan meerdere blokken gekoppeld zijn.

Pas de combinatie:

```text
TBlok+0x1AC = owner
TBlok+0x1ED = state
```

maakt onderscheid tussen gereserveerd en bezet.

## 8.6 TCP/5000 als centrale reserverings-API

Niet voldoende bewijs. In huidige capture voornamelijk globale status/statistiek.

## 8.7 1500 logische acties

Functioneel mogelijk als brute-force matrix, architectonisch verworpen vanwege schaal, onderhoud en foutkans.

---

# 9. PoC-geschiedenis en beslissingen

## PoC 01 — Static binary reconnaissance

**Doel:** bevestigen dat de juiste binary analyseerbaar is en reserveringsgerichte codepunten vinden.

Belangrijkste resultaten:

- Koploper 9.4 build 9 bevestigd;
- native Delphi PE32;
- class/type-markers `TBlok`, `TBlokBaan`, `TLokRij`;
- strings zoals:
  - `Blok is gereserveerd door locomotief`;
  - `Kan onvoldoende blokken reserveren!`;
  - `Vervolg blok/wisselstraat bezet!`;
  - `reeds gereserveerd`;
  - `TBlok.VrijOphef: aanvullen!`;
- UI-optie `Blok (reserveringen)` aangetroffen;
- Ghidra-candidate pad rond `0x006BE68A`.

**Beslissing:** voldoende bewijs om volledige Ghidra-export en objectlayout te reconstrueren.

## PoC 02 — Full Ghidra export / object graph

**Doel:** centrale block/loc objecten en mogelijke reserveercollecties reconstrueren.

Belangrijkste resultaten:

- rootpointerlocatie `VA 0x007259B0` / `RVA 0x3259B0`;
- block list `root+0x5AC`;
- loc list `root+0x5C8`;
- block ID `+0x14C`;
- display candidate `+0x15C`;
- loc ID `+0x1A8`;
- owner `TBlok+0x1AC`;
- claimcollectie `+0x16C`;
- claim add/remove/check codepaden gevonden.

**Open hypothese destijds:** `+0x16C` was mogelijk de reserveringslijst.

## PoC 03 — Eerste read-only runtime probe

PowerShell observer via `OpenProcess` / `ReadProcessMemory`.

Een succesvolle `-Mode Once` gaf onder andere:

```text
Koploper PID 1064
root 0x028A981C
Found 30 block objects and 3 locomotive objects.
```

Locomotieven:

```text
InternalLocId 2  -> 0x0291B014
InternalLocId 8  -> 0x02919D6C
InternalLocId 24 -> 0x0291587C
```

Beginbloktoewijzingen:

```text
block 10 -> loc 24
block 15 -> loc 2
block 19 -> loc 8
```

Dit kwam overeen met Koplopers standaard simulator GUI.

Watchresultaat:

- 180 snapshots;
- 30 blocks / 3 locs stabiel;
- `+0x1AC` veranderde tijdens rijden;
- `+0x16C` bleek een `TStringList`, maar alle 5400 samples count=0.

**Beslissing:** runtime reader werkt; `+0x16C` niet als normale reserveerset gebruiken.

## PoC 04 — Alternatieve kandidaten + statusbyte-doorbraak

Nieuwe kandidaten werden gelogd:

- `TBlok+0x24C`;
- `TBlok+0x258`;
- `TBlok+0x25C`;
- `Loc+0x4D8`.

Alle bleken in normale simulatie leeg/constant.

Belangrijk inzicht: één loc kwam tegelijk in meerdere `+0x1AC` blockowners voor. Hierdoor werd opnieuw naar de statische state-setter gekeken.

Doorbraak:

```text
TBlok+0x1ED = state byte
```

Statische codepaden:

```text
FUN_006DF0C8 -> state 1
FUN_006DEFE0 -> state 2
FUN_006DF74C -> state 9
FUN_006DF8B0 -> state 0
```

**Beslissing:** PoC05 moet owner + state gezamenlijk meten.

## PoC 05 — Reservering runtime bewezen

Standaard Koploper simulator, read-only.

Meetdata:

- 305 snapshots;
- 30 blokken;
- 3 locomotieven;
- 9150 block records;
- 915 loc records;
- circa 90,055 s;
- 94 gewijzigde blokevents.

Overgangen:

| Transition | Count |
|---|---:|
| `0 -> 1` | 23 |
| `9 -> 1` | 2 |
| `1 -> 2` | 24 |
| `2 -> 9` | 24 |
| `9 -> 0` | 21 |

Alle status-1 en status-2 records hadden een geldige eigenaar. Alle geobserveerde status-0 en status-9 records hadden geen eigenaar.

### Reservering vóór bezetting

24 reserveringen gingen in dezelfde capture van status 1 naar status 2 met **dezelfde loc-owner**.

Gemeten tijd tussen eerste waargenomen status 1 en eerste status 2:

- minimum: 0,858 s;
- gemiddelde: 4,589 s;
- mediaan: 4,415 s;
- maximum: 9,107 s.

### Meerdere toekomstige blokken tegelijk

Loc-ID 24:

| Tijd/sequence | Occupied (`2`) | Reserved (`1`) |
|---|---:|---|
| seq 115 | 17 | 19, 20 |
| seq 130 | 19 | 20 |
| seq 146 | 20 | 24 |

Dit bewijst dat een loc meerdere toekomstige blokken tegelijk kan bezitten en dat de observer precies het soort data ziet dat Siebwalde nodig heeft.

`Loc+0x58` matchte in 913/915 gevallen het status-2 blok; twee afwijkingen pasten bij een niet-atomaire overgang.

## PoC 06 — Video/GUI cross-validation

Doel: memory state 1 onafhankelijk vergelijken met Koplopers zichtbare current→next informatie.

Video en probe werden op timestamp gesynchroniseerd.

Voorbeelden:

| Video | GUI current → next | Loc-ID | Probe state 2 | Probe state 1 |
|---|---|---:|---:|---|
| ~00:10 | 9 → 10 | 2 | 9 | 10, 11 |
| ~00:20 | 11 → 12 | 2 | 11 | 12 |
| ~00:30 | 1 → 2 | 24 | 1 | 2 |
| ~00:40 | 3 → 6 | 24 | 3 | 6 |
| ~01:30 | 21 → 19 | 8 | 21 | 19, 11 |

Conclusie:

- GUI current block matcht state 2;
- GUI next block matcht een state-1 block;
- probe kan aanvullende toekomstige reserveringen zien die de simpele GUI next-column niet toont.

PoC06 meetresultaat:

- 22 `1 -> 2` transitions;
- alle 22 behielden dezelfde owner;
- mean reserve lead ~4,940 s;
- median ~4,603 s;
- range ~2,031–10,113 s;
- maximaal twee gelijktijdige reserved blocks per loc geobserveerd; **dit is geen veronderstelde systeemlimiet**.

### PoC 06 annuleringstest

Langere 180-s test:

- 597 snapshots;
- 93 nieuwe reserveringsepisodes;
- 92 gingen naar status 2 met dezelfde owner;
- één reservering stond bij einde nog open;
- geen `1 -> 0`;
- geen `1 -> 9`;
- geen owner-swap in state 1.

Uitgevoerde stop-/blokkeerinteracties annuleerden geen reeds bestaande reservering aantoonbaar.

**Belangrijk:** dit bewijst niet dat Koploper een reservering nooit kan intrekken. Alleen dat de gebruikte trigger dat niet deed.

Open acceptance item: een expliciete reserve → cancel zonder occupation moet nog gecontroleerd worden indien zo'n normaal Koploper-bedieningspad kan worden uitgelokt.

---

# 10. Gevalideerde runtime-state model

Het model dat C# moet representeren is primair **per blok**, omdat Koploper dezelfde locomotiefpointer aan meerdere blockobjects kan koppelen.

Aanbevolen domeinmodel:

```csharp
public enum KoploperBlockState
{
    Unknown = -1,
    Free = 0,
    Reserved = 1,
    Occupied = 2,
    Transition = 9
}

public sealed record KoploperBlockSnapshot(
    int InternalBlockId,
    int? DisplayBlockNumber,
    int? OwnerLocomotiveId,
    KoploperBlockState State,
    uint RawState,
    uint? RawUpdateTick);

public sealed record KoploperLocomotiveSnapshot(
    int InternalLocomotiveId,
    int? CurrentBlockInternalId);
```

Observer-level aggregatie:

```csharp
public sealed record KoploperLocomotiveTrajectory(
    int InternalLocomotiveId,
    int? OccupiedBlock,
    IReadOnlyList<int> ReservedBlocks);
```

**Let op:** `ReservedBlocks` is in eerste instantie een set, geen gegarandeerd geordende route.

---

# 11. Interne block-ID versus display blocknummer

PoC06 bevestigde dat interne block-ID's niet altijd gelijk zijn aan de nummers die de gebruiker in de GUI ziet.

Geobserveerde demo-mappings bevatten bijvoorbeeld:

```text
internal 30 -> display 2
internal 22 -> display 25
internal 23 -> display 22
internal 24 -> display 23
internal 25 -> display 24
internal 31 -> display 30
```

Daarom:

1. nooit lijstindex als bloknummer gebruiken;
2. internal ID en display ID afzonderlijk modelleren;
3. voor de fysieke Siebwalde-baan expliciet een gecontroleerde mapping `KoploperBlock -> TrackSection/Amplifier` gebruiken;
4. mapping mismatches zijn configuratiefouten en moeten fail-safe zijn.

---

# 12. Snapshotcoherentie en read semantics

`ReadProcessMemory` leest niet atomair over meerdere objecten. Koploper kan tussen twee reads zijn state wijzigen.

Dit is aangetoond doordat `Loc+0x58` in PoC05 913/915 keer overeenkwam met het status-2 block en twee keer tijdens een overgang achterliep.

De C# observer moet daarom een coherentiealgoritme hebben.

Aanbevolen minimaal patroon:

```text
1. Lees root en list metadata.
2. Lees block pointer list.
3. Lees per block minimaal:
      internalId
      displayCandidate
      ownerPtr
      state
      update marker/tick
4. Lees loc pointer list en loc IDs.
5. Herlees owner/state/update marker voor blocks.
6. Als waarden tijdens snapshot veranderen:
      snapshot inconsistent -> retry.
7. Publiceer alleen coherent snapshot.
8. Als retries mislukken:
      SourceHealth = Degraded/Unknown.
```

Daarnaast:

- pointer moet binnen geldige committed/readable process memory vallen;
- list count moet binnen plausibele grenzen vallen;
- duplicates/missing IDs detecteren;
- unknown raw state nooit naar `Free` coërceren;
- state 1/2 vereist geldige ownerpointer die naar een bekende locomotief resolveert;
- state 0/9 hoort in de geobserveerde versie geen owner te hebben; afwijking = inconsistent/unknown;
- process restart / PID change = volledige state invalidaten en root opnieuw oplossen.

---

# 13. Freshness en fail-safe gedrag

De PowerShell PoCs haalden ondanks een gevraagd 200-ms interval feitelijk vaak circa 295–300 ms per snapshot. Dit is voldoende voor reverse engineering, maar is **geen performancecontract voor de C# implementatie**.

C# moet timestamps en age expliciet modelleren:

```csharp
CapturedAtUtc
ObservedProcessStartTime
Sequence
IsConsistent
SourceHealth
Age
```

Er moet een configureerbare `MaxSnapshotAge` komen. De uiteindelijke numerieke waarde moet uit control-/hardwaretests komen en niet uit de PoC worden gegokt.

Bij:

- read failure;
- unsupported hash/version;
- stale snapshot;
- inconsistent snapshot;
- invalid pointer;
- owner mismatch;
- unknown raw state;
- process exit/restart;

moet Koploper-state naar **Unknown/Unavailable** gaan. Oude reserveringen mogen nooit stilzwijgend als actueel blijven gelden.

---

# 14. Gewenste C# architectuur

Aanbevolen componenten. Namen kunnen binnen de bestaande repositoryconventies worden aangepast, maar de verantwoordelijkheden moeten gescheiden blijven.

```text
KoploperProcessLocator
        |
        v
KoploperExecutableVerifier
        |
        v
KoploperMemoryReader
        |
        v
Koploper94MemoryLayout
        |
        v
KoploperRootResolver
        |
        v
KoploperSnapshotReader
        |
        v
KoploperStateDecoder
        |
        v
KoploperReservationObserver
        |
        +---- full snapshots
        +---- state-change events
        +---- source health / freshness
        |
        v
KoploperTrackAssignmentAdapter
        |
        v
existing TrackControl / safety boundary
        |
        v
track-amplifier backend
```

## 14.1 `IKoploperProcessLocator`

Verantwoordelijk voor:

- `koploper.exe` vinden;
- PID/process lifetime;
- process start time;
- module base;
- restart detectie.

## 14.2 `IKoploperExecutableVerifier`

Verantwoordelijk voor:

- file/version/hash-validatie;
- alleen ondersteunde layoutprofielen toelaten.

## 14.3 `IKoploperMemoryReader`

Dunne Windows abstraction:

```csharp
bool TryReadUInt32(nuint address, out uint value);
bool TryReadByte(nuint address, out byte value);
bool TryReadPointer32(nuint address, out uint value);
bool TryReadBytes(nuint address, Span<byte> destination);
```

Geen domeinsemantiek in deze laag.

## 14.4 `Koploper94MemoryLayout`

Versieprofiel met uitsluitend offsets/RVA's:

```text
RootPointerRva = 0x3259B0
Root_BlockList = 0x5AC
Root_LocoList  = 0x5C8

TList_Items    = 0x04
TList_Count    = 0x08
TList_Capacity = 0x0C

TBlock_InternalId  = 0x14C
TBlock_DisplayId   = 0x15C
TBlock_Owner       = 0x1AC
TBlock_State       = 0x1ED
TBlock_ChangedFlag = 0x1EE
TBlock_UpdateTick  = 0x1F0

TLoc_InternalId    = 0x1A8
TLoc_BlockRef54    = 0x54   // unverified semantic
TLoc_BlockRef58    = 0x58   // strong current-block candidate
```

## 14.5 `KoploperSnapshotReader`

Verantwoordelijk voor raw objectgraph → coherent raw snapshot.

Geen hardwareacties.

## 14.6 `KoploperStateDecoder`

Raw `0/1/2/9` → expliciete domeinstates.

Onbekende codes blijven `Unknown`.

## 14.7 `KoploperReservationObserver`

Verantwoordelijk voor:

- periodieke snapshots;
- freshness;
- source health;
- diff/event generation;
- `ReservedBlocksByLoc`;
- `OccupiedBlockByLoc`;
- volledige snapshot blijft authoritative; events zijn optimalisatie, niet waarheid.

## 14.8 Integratie naar TrackControl

De integratielaag vertaalt:

```text
Koploper block ownership
        +
modelbaan mapping
        ->
logical track/amplifier assignment
```

Niet rechtstreeks:

```text
Koploper state 1 -> Write PWM
```

De bestaande Siebwalde safety-lagen blijven ertussen.

---

# 15. Aanbevolen outputcontract van de Observer

Voorbeeld:

```json
{
  "source": "Koploper94.ReadOnlyMemory",
  "executableHash": "645B4681...79D2E",
  "processId": 1064,
  "sequence": 115,
  "capturedAtUtc": "2026-10-02T13:32:15.515Z",
  "consistent": true,
  "sourceHealth": "Healthy",
  "locomotives": [
    {
      "internalLocomotiveId": 24,
      "occupiedBlock": 17,
      "reservedBlocks": [19, 20]
    }
  ],
  "transitionOrUnknownBlocks": []
}
```

Dit contract is conceptueel. De uiteindelijke types horen strongly typed C# records/classes te zijn, niet JSON als interne interface.

---

# 16. Routevolgorde versus reserveringsset

Bewezen is:

- één loc kan meerdere state-1 blocks tegelijk hebben;
- deze blocks worden vooraf gereserveerd;
- een subset gaat later state `2` worden.

Niet algemeen bewezen is dat de **volgorde** alleen uit de set kan worden afgeleid.

Mogelijke strategieën, in volgorde van voorkeur:

1. baan-topologie + current block + rijrichting + reserveerset gebruiken;
2. reservation timestamps/update ticks gebruiken als aanvullende volgorde-indicatie;
3. bestaande 5700 current-block events als cross-check gebruiken;
4. alleen indien nodig later een specifieke interne route-list reconstrueren.

Voor PWM-distributie kan het mogelijk voldoende zijn dat alle veilig aan dezelfde loc toegewezen reserved blocks hetzelfde target volgen; een totale geordende route is dan niet noodzakelijk voor de eerste integratie. Dit moet door TrackControl-architectuur worden bevestigd.

---

# 17. Safety boundary voor track-amplifiers

De Koploper observer is een **advisory/administrative state source**, niet de fysieke safety authority.

Minimaal behouden:

- fysieke occupancy-input uit track amplifiers;
- occupancy freshness (`Unknown` bij stale data);
- amplifier health;
- direction consistency;
- conflicting ownership detectie;
- bestaande ControlSafetyInterlock-logica;
- bestaande veilige vrijgavegedrag voor verlaten blokken, inclusief de in de PoC-documentatie vastgelegde eis `verlaten blok -> PWM 399`;
- fail-safe bij verlies van Koploper observer.

Ontwerpregel:

```text
Observed Koploper state != Authorized physical output
```

Een reservation mag een amplifier logisch aan een loc koppelen, maar hardware-output wordt pas toegestaan door de complete control/safetylaag.

---

# 18. Acceptance criteria voor de C# read-only Observer

## A. Binary/version identity

- [ ] juiste `koploper.exe` wordt gevonden;
- [ ] versie/hash matcht exact ondersteund profile;
- [ ] mismatch -> `Unsupported`, geen memory decode.

## B. Process lifecycle

- [ ] process start werkt;
- [ ] process exit wordt binnen begrensde tijd gedetecteerd;
- [ ] restart/PID change wist alle oude state;
- [ ] root wordt opnieuw opgelost.

## C. Objectgraph

- [ ] block list en loc list plausibel;
- [ ] IDs zijn uniek;
- [ ] pointers zijn leesbaar/geldig;
- [ ] list mutation tijdens read leidt tot retry/invalid snapshot.

## D. State semantics

- [ ] state 0 = free/no owner voor gevalideerde versie;
- [ ] state 1 = reserved met owner;
- [ ] state 2 = occupied met owner;
- [ ] state 9 = transition/unknown, niet free;
- [ ] unknown raw code = Unknown.

## E. Known simulator regression

Op de standaard simulator moet de C# reader dezelfde klasse resultaten reproduceren als PoC05/06:

- [ ] 30 blockobjects / 3 locobjects voor de gebruikte demo-config;
- [ ] loc IDs 2, 8, 24;
- [ ] current/next GUI cross-checks kloppen;
- [ ] meerdere state-1 blocks per loc kunnen worden gepubliceerd;
- [ ] state 1 -> 2 behoudt owner.

Deze aantallen zijn **regressietestdata voor de demo**, geen productieconstanten.

## F. Snapshot coherency

- [ ] torn snapshot wordt niet als consistent gepubliceerd;
- [ ] retrybeleid begrensd;
- [ ] bij uitputting -> degraded/unknown;
- [ ] geen stale owner/reservation inheritance.

## G. Freshness

- [ ] timestamp per snapshot;
- [ ] configurable max age;
- [ ] stale -> Unknown/Unavailable;
- [ ] consumers kunnen source health zien.

## H. Reservation revoke

- [ ] wanneer state 1 verdwijnt, wordt reservation verwijderd uit de **volgende volledige snapshot**;
- [ ] eventverlies mag niet leiden tot blijvende stale reservation;
- [ ] expliciete reserve→cancel zonder status2 blijft nog een open Koploper scenario-test.

## I. Concurrency

- [ ] meerdere locs met gelijktijdige reserveringen;
- [ ] één block kan in één snapshot niet twee geldige owners hebben;
- [ ] inconsistentie -> invalid snapshot/safety diagnostic.

## J. No-write guarantee

- [ ] process handle heeft geen write/injection rights;
- [ ] code bevat geen `WriteProcessMemory`;
- [ ] geen DLL injection/hooking;
- [ ] unit/static test kan requested access mask controleren.

---

# 19. Open technische vragen

## 19.1 Expliciete reserveringsannulering

Nog open:

```text
state 1 -> no reservation
zonder eerst state 2 te worden
```

De 180-s PoC06-test lokte dit niet uit. Handmatig blokkeren/stoppen annuleerde een reeds bestaande reservering niet aantoonbaar.

Dit hoeft de ontwikkeling van de observer niet te blokkeren: een full-snapshot observer zal een toekomstige intrekking vanzelf correct detecteren zodra Koploper die state wijzigt. Voor live hardware-authority moet het gedrag wel doelgericht worden gevalideerd.

## 19.2 Definitieve display-block mapping

`+0x15C` is een sterke displaycandidate, maar voor de echte Siebwalde-database moet de mapping volledig geverifieerd worden tegen de baanconfiguratie.

## 19.3 Routevolgorde

State-1 levert een set. Indien TrackControl een strikt geordende route nodig heeft, moet topologie/timing of aanvullende runtime state worden gebruikt.

## 19.4 Performance

PowerShell was langzaam. C# `ReadProcessMemory` hoort veel sneller te kunnen zijn, maar een latency/freshness-budget moet empirisch worden vastgesteld.

## 19.5 Koploper binary updates

Iedere nieuwe Koploper binary/hash is een nieuw memory-layoutprofile totdat bewezen is dat offsets identiek zijn.

---

# 20. Aanbevolen development increments

Deze increments zijn bedoeld om door de bestaande Siebwalde Development Workflow te lopen met Project Lead → Architect → Developer → Integrator. Elke increment moet een kleine, reviewbare feature zijn.

## Increment KIS-01 — Read-only Windows process foundation

**Doel:** betrouwbaar en veilig Koploper vinden/verifiëren/lezen zonder domeininterpretatie.

Deliverables:

- `IKoploperProcessLocator`;
- process PID/modulebase/starttime;
- executable hash/version verification;
- `IKoploperMemoryReader`;
- 32-bit pointer helpers;
- geen write rights;
- unit tests met fake memory reader;
- Windows integration smoke test.

Acceptance:

- supported binary → attach/read;
- unsupported hash → fail closed;
- process exit/restart → detected;
- no write access requested.

## Increment KIS-02 — Koploper 9.4 objectgraph decoder

**Doel:** root, block list en loc list consistent decoderen.

Deliverables:

- `Koploper94MemoryLayout`;
- root resolver via modulebase + `0x3259B0`;
- TList reader;
- block/loc raw objects;
- pointer/count sanity checks;
- demo regression fixture.

Acceptance:

- standaard simulator: 30 blockobjects, 3 locs;
- IDs 2/8/24 herkenbaar;
- current demo allocations reproduceerbaar.

## Increment KIS-03 — Block state decoder

**Doel:** owner + raw status naar typed blockstate vertalen.

Deliverables:

- `KoploperBlockState` enum;
- decode `0/1/2/9`;
- unknown codes preserved;
- owner validation;
- internal/display IDs afzonderlijk.

Acceptance:

- status 1/2 vereist bekende owner;
- state 9 nooit als Free gepubliceerd;
- invalid combination -> Unknown/diagnostic.

## Increment KIS-04 — Coherent snapshot + freshness

**Doel:** safe, repeatable full-state snapshots.

Deliverables:

- double-read/coherency policy;
- bounded retries;
- sequence/timestamps;
- source health;
- max-age configuration;
- stale/restart invalidation.

Acceptance:

- simulated torn reads worden geweigerd;
- stale snapshots verdwijnen uit authoritative state;
- process restart geeft geen oude reservations door.

## Increment KIS-05 — Reservation Observer

**Doel:** per loc occupied/reserved sets publiceren.

Deliverables:

- `IKoploperReservationObserver`;
- full snapshots;
- optional diff events;
- `OccupiedBlockByLoc`;
- `ReservedBlocksByLoc`;
- diagnostics.

Acceptance:

- PoC05/06 trace kan als golden testdata worden gereplayed;
- state 1 -> Reserved;
- state 1 -> 2 behoudt owner;
- disappearing reservation wordt door volgende snapshot verwijderd.

## Increment KIS-06 — Simulator cross-validation harness

**Doel:** C# output live vergelijken met Koploper standard simulator en bestaande PoC-evidence.

Deliverables:

- CSV/structured trace logging;
- visible block mapping diagnostics;
- optional port-5700 read-only cross-check;
- repeatable test protocol.

Acceptance:

- huidige/volgende block GUI correlations reproduceren;
- multi-loc concurrent reservations;
- geen dubbele ownership inconsistencies in geldige snapshots.

## Increment KIS-07 — Siebwalde block/amplifier mapping, Shadow only

**Doel:** reservation data naar bestaande modelbaan/amp mapping vertalen zonder hardware-writes.

Deliverables:

- explicit config/map `KoploperBlock -> TrackSection/Amplifier`;
- logical assignment model;
- shadow diagnostics `loc -> occupied/reserved -> amps`;
- geen writes naar hardwarebackend.

Acceptance:

- unmapped/duplicate blocks fail safe;
- reserved amp assignment zichtbaar in logging;
- reservation removal trekt assignment in shadow in.

## Increment KIS-08 — Safety-interlock integration, still no live writes

**Doel:** observer output als input aan de bestaande safetylaag aanbieden.

Deliverables:

- stale/unknown propagation;
- conflict diagnostics;
- occupancy cross-check;
- safe fallback;
- expliciete scheiding desired assignment vs authorized output.

Acceptance:

- loss of Koploper observer kan nooit output laten 'hangen';
- stale reservation wordt niet geautoriseerd;
- state 9/Unknown blokkeert of volgt expliciet veilige policy.

## Increment KIS-09 — Controlled hardware validation

**Precondition:** KIS-01 t/m KIS-08 PASS, plus open revoke/safetytests beoordeeld.

Begin met één loc / beperkt traject / lage snelheid / supervisie.

Doel:

- gereserveerde amps krijgen correct setpoint wanneer safetylaag dit autoriseert;
- occupied/reserved transitions veroorzaken geen onderbreking of verkeerde loc-owner;
- verlaten block volgt bestaande veilige PWM/vrijgaveregel;
- occupancy blijft onafhankelijke authority.

Geen algemene productie-uitrol voordat dit gecontroleerd is.

---

# 21. Unit/integration teststrategie

## Pure unit tests

Gebruik een `FakeKoploperMemoryReader` met geconstrueerde memory maps voor:

- valid root/list;
- invalid pointers;
- count mutation;
- state 0/1/2/9;
- unknown state;
- owner missing;
- loc ID duplicate;
- block ID duplicate;
- process restart generation.

## Golden trace tests

Gebruik PoC05/06 CSV's als behavioral evidence om snapshots/events te vergelijken.

Test onder andere:

```text
reserved owner X -> occupied owner X
multiple reserved blocks for X
concurrent reservations for multiple locs
transition 9 preserved as transition
reservation still open at end
```

## Windows integration test

Alleen op bekende Koploper 9.4 binary/simulator:

- attach read-only;
- verify hash;
- read 30/3 demo objectgraph;
- record short trace;
- no mutation of target process.

---

# 22. Diagnostics en observability

Aanbevolen diagnostics:

```text
KOPLOPER_PROCESS_NOT_FOUND
KOPLOPER_UNSUPPORTED_BINARY
KOPLOPER_ACCESS_DENIED
KOPLOPER_ROOT_INVALID
KOPLOPER_LIST_INVALID
KOPLOPER_POINTER_INVALID
KOPLOPER_SNAPSHOT_INCONSISTENT
KOPLOPER_UNKNOWN_BLOCK_STATE
KOPLOPER_OWNER_NOT_FOUND
KOPLOPER_STALE
KOPLOPER_RESTARTED
KOPLOPER_BLOCK_MAPPING_MISSING
KOPLOPER_BLOCK_MAPPING_CONFLICT
```

Voor iedere snapshot/log:

- PID;
- process start time/generation;
- executable hash/profile;
- sequence;
- captured UTC;
- duration;
- retry count;
- coherent yes/no;
- source health;
- block count / loc count;
- reserved/occupied summary.

---

# 23. Coding constraints voor DeepSeek/OpenCode

Agents moeten de volgende regels als hard constraints behandelen:

1. **Geen writes naar Koploper.**
2. **Geen hooks/injection/patches.**
3. Memory layout is versioned en gekoppeld aan exact hash/version.
4. Geen absolute runtime pointers hardcoden.
5. Geen raw offsetgebruik verspreiden door business code; offsets horen in één layoutprofile.
6. Raw memory read, decode, observer en hardware integration moeten afzonderlijke lagen zijn.
7. Full snapshot is authoritative; diff-events alleen afgeleide convenience.
8. `state 9` nooit stilzwijgend als `Free` behandelen.
9. Unknown/inconsistent/stale => geen geërfde reservation authority.
10. Internal block ID != display block ID != physical amplifier ID.
11. Reserved block set is niet automatisch geordende route.
12. Koploper-state is geen hardware-safety authority.
13. Bestaande occupancy/freshness/safety-interlocks behouden.
14. Geen 1500 handmatig geconfigureerde loc×block-acties bouwen.
15. Afgewezen hypotheses `+0x16C`, `+0x24C`, `+0x258`, `Loc+0x4D8` niet opnieuw als productiebron introduceren zonder nieuw bewijs.

---

# 24. Suggested repository placement

Binnen de huidige Siebwalde-oplossing is een logische plaatsing bijvoorbeeld:

```text
SiebwaldeApp.Core/
  Koploper/
    Process/
    Memory/
    Model/
    Observation/
    Diagnostics/

SiebwaldeApp.Core.Tests/
  Koploper/

SiebwaldeApp.Core.Host/
  // composition/configuration only
```

De exacte repositorystructuur moet door Project Lead/Architect worden afgestemd op de actuele solution layout. De observer hoort niet in `EcosEmu` zelf: ECoS en internal-state-observation zijn twee onafhankelijke bronnen.

---

# 25. Eerste DeepSeek/OpenCode development prompt — aanbevolen scope

Gebruik niet meteen een prompt om de volledige integratie te bouwen. Start met KIS-01 en KIS-02.

Voorbeeldopdracht voor Project Lead:

> Read `docs/koploper-internal-state-integration.md` as the authoritative technical handoff. Do not redo the reverse engineering and do not reinterpret rejected offsets without new evidence. Create a feature increment for a read-only Koploper 9.4 memory source implementing only KIS-01 and KIS-02: process discovery, exact executable hash/version gating, minimal read-only Windows memory access, dynamic root resolution from module base + RVA 0x3259B0, and decoding of the root block/loco TLists. No reservation semantics, no TrackControl integration and no hardware writes in this increment. Use dependency injection so memory access can be fully unit tested. Add tests for unsupported hashes, process restart, invalid pointers/list counts, and a Windows-only integration seam. Architect reviews boundaries, Developer implements, Integrator independently verifies read-only permissions and tests. Produce the normal workflow evidence and stop at MERGE_READY; do not extend scope into state decoding unless explicitly authorized.

Dit houdt de eerste implementatie klein en voorkomt dat de agent meteen reverse engineering, business logic en hardwarecontrol in één PR vermengt.

---

# 26. Bronartefacten / evidence index

Bewaar onderstaande bestanden als onderliggende evidence. Het masterdocument is de handoff; deze bestanden zijn detailbewijs.

## Static/Ghidra

- `koploper_internal_state_poc01_2026-10-02.md`
- `koploper_internal_state_poc02_2026-10-02.md`
- `koploper_internal_state_poc02_bundle.zip`
- `koploper_static_candidates_2026-10-02.csv`
- `koploper_reservation_candidates.csv`
- `koploper_reservation_core_excerpts.txt`
- `Koploper94_Ghidra_Analysis.zip`

## Network background

- `koploper_protocol_analysis_2026-10-02.md`
- `koploper_port5000_decoded.csv`
- `koploper_port5700_events.csv`
- `koploper_network_raw.pcapng`

## Runtime PoCs

- `koploper_internal_state_poc03_watch_analysis_2026-10-02.md`
- `koploper_internal_state_poc04_analysis_2026-10-02.md`
- `Koploper_InternalState_PoC05_Analysis.zip`
- `Koploper_PoC06_validation_bundle.zip`
- `Koploper_PoC06_annuleringstest_2026-10-02.md`

## Probe versions

- `KoploperStateProbe_PoC03.zip`
- `KoploperStateProbe_PoC03_v0.1.1.zip`
- `KoploperStateProbe_PoC04_v0.2.0.zip`
- `KoploperStateProbe_PoC05_v0.3.0.zip`

---

# 27. Current decision record

## Proven enough to implement now

- external read-only memory observation is feasible;
- exact Koploper 9.4 process/root/objectgraph is decodable;
- block and loco registries are readable;
- block owner is readable;
- states 0/1/2/9 are functionally characterized for normal simulator operation;
- state 1 gives future reservations before occupation;
- multiple future blocks can be assigned to one loc;
- multiple locs can have concurrent reservations;
- current/next GUI behavior corroborates the observer;
- no Koploper modification is needed.

## Not proven / not production-authorized yet

- explicit reserve cancellation without occupation;
- formal ordered route source for every topology;
- performance/freshness budget for live physical control;
- physical amplifier behavior driven by observer;
- safety case for production hardware authorization;
- compatibility with other Koploper binaries/hashes.

## Architectural decision

Proceed with a **version-gated, read-only C# Koploper Reservation Observer** based on full coherent snapshots of the internal block registry. Integrate it first in diagnostics/shadow mode. Do **not** implement a modified Koploper binary, injected DLL, network-server dependency or 1500 logical actions.

---

# 28. Short handoff for a new AI session

If context is limited, the new AI should read this document first and retain these essentials:

1. Koploper 9.4 build 9 exact hash is known.
2. Use read-only Windows memory APIs, no patching.
3. Resolve root using modulebase + RVA `0x3259B0`.
4. `root+0x5AC` = block list; `root+0x5C8` = loco list.
5. `TBlok+0x14C` = internal block ID.
6. `TBlok+0x15C` = display block candidate; map explicitly.
7. `TBlok+0x1AC` = loc ownerpointer.
8. `TBlok+0x1ED`: `0 free`, `1 reserved`, `2 occupied`, `9 transition`.
9. `Loc+0x1A8` = internal loc ID.
10. Status 1 is validated against GUI next-block behavior and normally precedes status 2 by several seconds.
11. A loc can have multiple state-1 blocks simultaneously.
12. `+0x16C` is a claim collection but was empty in normal simulator traces; not the primary reservation source.
13. Build coherent full snapshots; events are secondary.
14. Stale/inconsistent/unsupported version -> Unknown and no authority.
15. First C# increments are process/memory foundation, then objectgraph decoder, then state observer, then shadow integration.

---

**End of authoritative handoff.**
