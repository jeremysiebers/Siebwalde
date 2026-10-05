# Koploper Internal State PoC — static binary analysis 01

Datum: 2026-10-02  
Onderzoek: *alleen* offline/static; `koploper.exe` is niet uitgevoerd, gemodificeerd of gepatcht.  
Bron: door Jeremy aangeleverde `koploper-runtime.zip`.

## 1. Binary en reproduceerbaarheid

| Eigenschap | Resultaat |
|---|---|
| ZIP-inhoud | één bestand: `koploper.exe` |
| EXE-grootte | 4,682,752 bytes |
| SHA-256 | `645b4681c14975f3619eb44968c74f3b7925cb914c5a23302932918d73079d2e` |
| Versiestring in version resource | `9.4.0.9` (komt overeen met Koploper 9.4 build 9) |
| Type | PE32 Windows GUI, Intel i386, native executable (geen .NET CLR-directory) |
| Delphi indicaties | Delphi `TPF0` streamed form resources; class/type records met `TBlok`, `TBlokBaan`, `TLokRij`; Delphi-stringlayout |
| Preferred image base | `0x00400000` |
| CODE section | virtueel `0x00401000` t/m ca. `0x00722608`; ruwe bestands-offset `0x00000400` |
| Resource section | `.rsrc`, bevat o.a. meerdere Delphi forms |

**Adressentip:** voor een raw file offset binnen `CODE` is de *preferred* VA `file_offset + 0x00400C00`; dit geldt niet voor resource-offsets en moet bij herlocatie worden gecorrigeerd met de feitelijke modulebasis. Een string-VA is geen functie-adres.

## 2. Wat aantoonbaar in deze EXE zit

1. Er zijn Delphi type/class markers voor `TBlok` (file-offset `0x2D50CB`), `TBlokBaan` (`0x184399`) en `TLokRij` (`0x2A86EE`). Die namen zijn in structuren aanwezig, niet alleen als losse Nederlandstalige documentatie. De exacte objectlayout, VMT en relatie tussen de drie zijn nog niet gereconstrueerd.
2. De EXE bevat letterlijk `Blok is gereserveerd door locomotief` (`0x2F4B90`) en `Blok is gereserveerd door locomotief komend uit blok` (`0x2F4BEC`), naast claimvarianten. De eerste tekst wordt op `0x006F49D9` als string geïnitialiseerd; dit adres is **niet** bewezen de evaluator van die conditie.
3. Diagnostiek in dezelfde codeomgeving noemt `Kan onvoldoende blokken reserveren!` (`0x2BE504`), `Vervolg blok/wisselstraat bezet!`, ` reeds gereserveerd` en `TBlok.VrijOphef: aanvullen!` (`0x2DB058`). Dit biedt gerichte cross-references.
4. De configuratieteksten `Negeer extra blokken reservering`, `Slechts één vervolg blok: claim dat blok` en de drie aanduidingen voor één, twee en meer dan twee gereserveerde blokken zijn aanwezig. Dit laat zien dat Koploper uitdrukkelijk rekening houdt met *meerdere* gereserveerde vervolgblokken.
5. Het Delphi-formulier `Tf_inst` bevat `cmb_statusbalk` met optie `Blok (reserveringen)` (`0x3EBB8B`). Formulier `Tf_insr` bevat `cmb_snelh` met `Lengte gereserveerde rijweg` (`0x3E65DB`). Deze ingebouwde UI kan mogelijk als onafhankelijke referentie in de PoC dienen.

## 3. Eerste code-cross-references — kandidaten, nog geen semantisch bewijs

- `0x006BE68A`: gebruik van tekst `Kan onvoldoende blokken reserveren!`; aangrenzende code roept rond `0x006BE729` functie `0x006BBFB8` aan en rond `0x006BE76D` functie `0x006DE8B4`. Mogelijk een relevant beslis-/controlepad; in Ghidra call graph, argumenten en effecten uitzoeken.
- `0x006DBBA6`: referentie naar `TBlok.VrijOphef: aanvullen!`; in dezelfde routine staat onder andere een call naar `0x006DF74C` op `0x006DBB79`. Kandidaten voor onderzoeken van vrijgavepad.
- `0x006DE8B4`: functie die door het bovenstaande pad wordt aangeroepen. Er staat onder voorwaarden een byte-write rond `0x006DE9AE` naar een objectveld `+0x18`. Zonder type-/control-flow-analyse betekent dit **niet** dat `+0x18` een reserveringsveld is.
- `0x006F49D9`: wijst naar de logische-conditiestring, maar zit in een reeks stringinitialisaties. Gebruik dit **niet** als directe breakpoint voor een reserveringsbeslissing.

Een compact, semikolon-gescheiden index van de markers staat in `koploper_static_candidates_2026-10-02.csv`.

## 4. Wat nog niet bewezen is

- Geheugenadres, klasse en veld-offset voor: actuele blokbezetting, gereserveerd-door-loc, geclaimd-door-loc en volgorde van de reserveringsketen.
- Of de relevante collectie per `TBlok` dan wel per `TLokRij` is opgeslagen.
- Of een externe read-only `ReadProcessMemory` observer voldoende coherente snapshots kan maken of dat eventinstrumentatie nodig is.
- Het verschil tussen reserveren, claimen, fysiek bezetten en vrijgeven in de runtime-representatie.

## 5. Concreet aanbevolen PoC 02

**Niet op de fysieke baan debuggen**: een debugger kan de hoofdthread onderbreken, waardoor treincommando's en bezetmeldingen stoppen. Neem een kopie van de ovaaltje-database en start Koploper in (Full)Simulation, zonder de fysieke versterkers aan te sturen.

1. Zoek in Koplopers instellingen de optie voor de statusbalk `Blok (reserveringen)` (`Tf_inst`/`cmb_statusbalk`) en zet die aan. De precieze menupadlocatie is niet statisch vastgesteld. Maak screenshots in toestand A (loc in blok 1, nog geen rijweg), B (blok 2/3 vooruit gereserveerd, loc nog in blok 1), C (blok 2 binnengekomen), D (reservering ingetrokken). Dit levert waarneembare ground truth voor de uiteindelijke geheugendecode.
2. Laad deze EXE in **Ghidra als x86 PE32**, preferred base `0x00400000`. Begin bij `0x006BE68A`, de aangrenzende routine en `0x006DE8B4`. Traceer gecontroleerd de calls naar `0x006BBFB8`, `0x006DE8B4` en eventuele writes naar objecten met verwantschap aan de `TBlok`-/`TLokRij`-records. Controleer alternatieve aanknopingspunten bij `0x006DBBA6`.
3. Bepaal eerst via static analysis *welk pointerpad* de reservering bereikt (array/list → blokobject of locobject → reserveringsveld). Pas daarna in een geïsoleerde simulator een debugger gebruiken voor gerichte read/watchpoints bij A/B/C/D. Geen willekeurige massascans of patching.
4. Prototype: `IReadOnlyKoploperStateSource`, dat per blok object-ID, bezet/gereserveerd/geclaimd, loc-ID, volgorde, update-tijd en synchronisatiestatus geeft. Bevestig ieder veld tegen de Koploper UI.
5. Bij observer-uitval of onvoldoende betrouwbare snapshot: geen geërfde reserveringen doorzetten naar PWM. De fysieke occupancy-interlocks en de vereiste vrijgave `399` blijven onafhankelijk beslissend.

## 6. Beslismoment

De statische onderzoeksvraag is geslaagd: **we beschikken over de juiste binary en concrete reserveringsgerichte code-/klasse-aanknopingspunten**. Het eigenlijke doel (een bewezen reserveringslayout + uitleesbare keten) is nog *niet* behaald. Dat vereist de gerichte Ghidra-decompilatie en simulatorcorrelatie uit PoC 02.
