# Koploper 9.4 — Internal Reservation State PoC 02
**Datum:** 2026-10-02  
**Scope:** statische analyse van user-supplied Ghidra exports; Koploper is niet uitgevoerd, geïnjecteerd of gepatcht.  
**Archief SHA256:** `a4db7392eafb170f76fe18dec5fc7d4263c0d2e5f2f10af1d41d294a71bec352`

## 1. Input en observatiegrens
De ZIP bevat `koploper.exe.gzf` (~18.8 MB), `koploper.exe.txt` (~274.6 MB; ASCII listing), en `koploper_decompiled.c` (~12.3 MB). C-export bevat 7,212 herkende functiedefinities. Ghidra C is *pseudocode*, en kan Delphi register-conventions, nested procedures, exception-handling en return types onjuist weergeven. Beweringen over betekenis zijn hypotheses tenzij aangegeven als direct geobserveerde read/write.

## 2. Belangrijkste vondst: objecten en eigenaarcollectie
Dit is aanzienlijk concreter dan het eerdere vertrekpunt `FUN_006BBFB8` (reserverings-foutbericht).

### Globale registers
- `PTR_DAT_007259B0`: pointerlocatie naar een centraal rootobject (VA vanuit Ghidra; RVA = `0x3259B0` bij imagebase 0x00400000). De ASCII-export toont op `DATA:007259B0` een pointerwaarde `0x007287E4` (BSS-locatie) in het originele image. Controleer bij runtime de werkelijke modulebase en pointerwaarde; een tweede, ongedocumenteerde dereferentie niet aannemen.
- `FUN_00676330`: zoekt een **blok** op in de collectie op `root + 0x5AC`, met ID/key `TBlok + 0x14C`.
- `FUN_00676380`: zoekt een **locomotief** op in `root + 0x5C8`, met ID/key `loc + 0x1A8`.
- `FUN_0040F05C` en `FUN_0040F2AC`: tonen de elementaire `TList`-layout: pointerarray op `list + 4`, count op `list + 8`, capaciteit op `list + 12` (de VMT-pointer staat op `list + 0`). Hierdoor is een externe read-only snapshot conceptueel mogelijk zonder DLL-injectie. De keyed collection op een blok is een ander type en moet apart worden gereconstrueerd.

### TBlok velden (VA van `TBlok`-VMT: circa `0x006D5C00`; Delphi classname nabij `0x006D5CCA`)
| Object-offset | Directe observatie | Interpretatie / grens |
|---|---|---|
| `+0x14C` | `FUN_00676330` vergelijkt met gevraagde key | Interne blokidentifier (hoog vertrouwen). |
| `+0x15C` | Word in diverse meldingen `Blok <nummer>` geformatteerd | Getoond bloknummer; verschil met +0x14C bij runtime controleren. |
| `+0x16C` | Keyed collectie van locomotiefobjecten; zie hieronder | Sterke kandidaat voor voorafgaande claim-/reserveringstoewijzing; exacte semantiek valideren. |
| `+0x1AC` | Door `FUN_006DF0C8` / `FUN_006DEFE0` gezet/gewist; logtekst 'bezet door' / 'vrij van' | Administratieve actuele loc-toewijzing; NIET automatisch alle vooruit gereserveerde blokken. |
| `+0x1C4` | Collectie doorlopen in `FUN_006DB6C0`, met typechecks voor o.a. TBlok, TBezetmelder en TVrijgevenWisselstraat | Gemengde actieve baanitems/vrijgavepad, geen simpele per-loc reserveringstabel. |
| `+0x24C` / `+0x258` | Afzonderlijke verwijzingen, in cleanup op basis van loc-key gewist | Nader onderzoeken; kan aanvullende claim-/routeadministratie zijn. Nog NIET als veldbetekenis vastleggen. |

### Het +0x16C-pad — bijzonder sterk bewijs
- **Add:** `FUN_006D7A80(TBlok*, Loc*)` leest `Loc + 0x1A8`, zet dit om naar een string-key, en roept een virtuele insert van de collectie `TBlok + 0x16C` aan met het locomotiefobject als value. Daarna volgt `FUN_006DFC04` (update).
- **Remove:** `FUN_006D7AE4(TBlok*, LocId)` zoekt de string-key en verwijdert het element; een identifier `-1` wist de collectie.
- **Collision/membership:** `FUN_006DDB14(TBlok*, Loc*)` test of de lijst niet-leeg is en of een opgegeven loc niet reeds in de lijst voorkomt. `FUN_006DDB4C` test expliciet lidmaatschap.
- **Routekoppeling:** `FUN_006AB81C` loopt een lijst van route-/kandidaatitems bij de locomotief (`loc + 0x4D8`) langs, herkent de `TBlok`-klasse en roept **`FUN_006D7A80(Blok, Loc)`** aan; `FUN_0069F228` heeft een ander pad naar dezelfde add-routine. De complete betekenis, geldigheidsduur en ordening van de route-items zijn nog niet bewezen.
- **Overgang tot actuele eigenaar:** `FUN_006DF0C8(TBlok*, Loc*)` schrijft `TBlok + 0x1AC = Loc` en verwijdert dezelfde loc zo nodig uit `TBlok + 0x16C`. `FUN_006DF4C4` produceert de strings **'bezet door'** en **'vrij van'**. Dit ondersteunt de scheiding tussen een vooruitgekeken claim/reservering en een administratief bezet blok.
- **Reeds-geclaimd-controle:** in `FUN_006BBFB8`, bij VA `0x006BCD61`, wordt `FUN_006DDB14` gebruikt vóór de melding **'is al geclaimd'**; ook de melding **'reeds gereserveerd'** komt voor. Die twee termen niet voortijdig samenvoegen.

### Gerelateerde getters
`FUN_006A9498(x) = *(x + 0x58)` en `FUN_006A949C(x) = *(x + 0x54)`; hun waarden worden elders met blokobjecten vergeleken. Dit zijn interessante referenties voor huidige/volgende blokken, maar welk veld welke betekenis heeft staat nog niet vast.

## 3. Voorlopige objectgrafiek (NIET een definitieve C-structdefinitie)
```text
Ghidra VA 0x007259B0 -> ROOT*
ROOT + 0x5AC -> TList* met TBlok* entries
ROOT + 0x5C8 -> TList* met Loc* entries

TList [read-only, in code aangetroffen]:
  +0x00  vtable*
  +0x04  items*   -> 32-bit pointer array
  +0x08  count
  +0x0C  capacity

TBlok*:
  +0x14C  internal block ID
  +0x15C  displayed block number candidate
  +0x16C  keyed list (loc ID string -> Loc*)  <-- claim/reservation PoC focus
  +0x1AC  current administrative Loc* (separate!)
  +0x1C4  list of active track objects (not same as +0x16C)

Loc*:
  +0x1A8  identity used by global loco lookup and keyed block+0x16C list
  +0x4D8  candidate ordered route item list (validate retention and semantics)
  +0x54/+0x58  block references (precise semantics unverified)
```

## 4. Gerichte runtimevalidatie (bij voorkeur FullSimulation, los van fysieke versterkers)
1. Gebruik een *kopie* van de ovaaltje-database en schakel iedere fysieke write/trackoutput uit. De debugger kan Koploper stilzetten; NIET tijdens automatische fysieke ritten attachen.
2. Verifieer onder de debugger de modulebase en de pointer `ROOT = ReadU32(base + 0x3259B0)`; die moet een leesbaar object zijn. Inspecteer root+`0x5AC` en root+`0x5C8`, count en hun pointerarrays; match blok-/loc-ID's met de bekende configuratie. Houd rekening met transient data bij live snapshots.
3. Observeer `FUN_006D7A80` (Add), `FUN_006D7AE4` (Remove) en `FUN_006DF0C8` (current owner assign), eerst in de simulator. Controleer bij Add de blokkey `block+0x14C` en loc-key `loc+0x1A8`, en het aantal entries vóór/na.
4. Test loc 1: current block 1, vervolg 2 en 3. Controleer of de keyed collections van 2/3 vóór de fysieke blokovergang loc 1 bevatten en of zij worden verwijderd bij routewijziging/stop. Vergelijk met Koplopers GUI-logische condities 'geclaimd' versus 'gereserveerd'. **Dit onderscheid is de beslissende open vraag.**
5. Test twee locs met tegenoverliggende reserveringswensen en een ingetrokken rijweg. Verifieer dat geen dubbele eigenaar voor dezelfde versterker ontstaat; analyseer daarbij ook +0x24C/+0x258 en de routevolgorde in loc+0x4D8.
6. Pas daarna een extern read-only C# prototype maken via `OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION | PROCESS_VM_READ)` en `ReadProcessMemory`; geen DLL-injectie of writes. Voor consistente multiveldsnapshots is een herhaal-/versiecontrole nodig; behandel onleesbare velden of staleness als `Unknown`.

## 5. Beslissingsgrens voor Siebwalde
De code bevat **direct identificeerbare rootobjecten en een per-blok collectie van locomotiefobjecten**. Daardoor is een generieke observer zonder 1.500 Koploper-logische acties nu realistisch als PoC. Wat **nog niet bewezen** is: of `TBlok+0x16C` exact de door Koploper bevestigde reservering is of een voorbereidende claim. De test mag dat niet aannemen. Zelfs een bewezen reservering mag nooit op zichzelf een amplifier energizen: fysieke occupancy, block ownership, direction en safety-interlocks blijven bepalend; fail-safe bij verouderde/tegenstrijdige snapshots.

## 6. Ghidra methoden om voorlopig te hernoemen (suggesties, niet bewezen originele namen)
| Origineel | Onderzoeksnaam |
|---|---|
| `FUN_00676330` | `Root_FindBlockByInternalId_CANDIDATE` |
| `FUN_00676380` | `Root_FindLocomotiveById_CANDIDATE` |
| `FUN_006D7A80` | `TBlok_AddClaimEntry_CANDIDATE` |
| `FUN_006D7AE4` | `TBlok_RemoveClaimEntry_CANDIDATE` |
| `FUN_006DDB14` | `TBlok_HasOtherClaim_CANDIDATE` |
| `FUN_006DDB4C` | `TBlok_ContainsLocInClaimList_CANDIDATE` |
| `FUN_006AB81C` | `Loc_PropagateRouteEntriesToBlocks_CANDIDATE` |
| `FUN_006DF0C8` | `TBlok_AssignCurrentLoc_CANDIDATE` |
| `FUN_006DF4C4` | `TBlok_LogOccupiedOrFree_CANDIDATE` |

## 7. Bijgevoegde machineleesbare onderzoeksinput
`koploper_reservation_candidates.csv` (prioriteiten en VA's) en `koploper_reservation_core_excerpts.txt` (gemarkeerde originele C-exportfragmenten met Ghidra-regelnummers). De volledige oorspronkelijke GZF en ASCII-export blijven als bron nodig indien statische twijfel ontstaat.
