# Koploper Internal State — PoC 06, annuleringstest

Datum: 2026-10-02 (Europe/Amsterdam)  
Bronnen: `output_v03(2).zip` (uitsluitend de nieuwe `*_20261002_160210.*` reeks) en `Recording 2026-10-02 160548.mp4`.

## Meetdekking

- Probe: 16:02:10.584–16:05:10.739; 597 snapshots / 30 blokken / 3 locomotieven.
- Video: metadata-start 16:02:10; duur 176.367 s.
- Interval gevraagd 200 ms, feitelijke mediaan 295 ms, piek 580 ms. Kortere tijdelijke toestanden kunnen worden gemist.
- 30 beginrecords plus 359 veranderingsevents.

## Waargenomen toestandovergangen

| Overgang | Aantal | Interpretatie |
|---|---:|---|
| 0 → 1 | 84 | nieuwe reserveringskandidaat |
| 9 → 1 | 9 | nieuwe reserveringskandidaat |
| 1 → 2 | 92 | reservering naar administratief bezet |
| 2 → 9 | 92 | bezetting naar tussen-/vrijgavestatus |
| 9 → 0 | 82 | terug naar vrije status |
| 1 → 0 | 0 | directe reserveringsannulering niet waargenomen |
| 1 → 9 | 0 | reserveringsannulering via tussenstatus niet waargenomen |
| 1 → 1 met andere eigenaar | 0 | eigenaarwissel tijdens reservering niet waargenomen |

- Van de 93 nieuwe reserveringsepisodes gingen er 92 over naar status 2 en in alle 92 gevallen bleef de eigenaar gelijk.
- Reservering-tot-bezetting tijd: mediaan 4.41 s, gemiddelde 4.25 s (min 0.30, max 9.12 s).
- Eindsnapshot: blok 11 status 1, eigenaar loc-ID 8, sinds 16:05:03.031. Dit is **niet** als geannuleerd te tellen.
- Daarnaast bezet: blok 13 (loc 2), 18 (loc 24), 19 (loc 8). Blok 21 staat op 9, zonder eigenaar.

## Video-observatie

- De video toont actief rijgedrag, stop-/wachtmeldingen en contextmenu-interacties in het baanoverzicht.
- Omstreeks +115 s is `Handmatig geblokkeerd` zichtbaar als menuoptie; omstreeks +137 s is dit in een ander zichtbaar contextmenu aangevinkt.
- In de bijbehorende eventstroom, ook na die interactie, blijft de overgang `1 → 2` de enige waargenomen uitgang uit status 1.
- Blokkeren of stoppen heeft in deze proef dus **niet aantoonbaar een reeds vastgelegde reservering geannuleerd**. Dit is niet hetzelfde als bewijs dat Koploper nooit reserveeracties kan terugdraaien.

## Validatiestatus

- A. Vooraf reserveren met juiste loc: **PASS** (samen met eerdere PoC06-visuele controle).
- B. Status 1 → 2 met behoud van eigenaar: **PASS** (herhaald).
- C. Bestaande reservering intrekken zonder tussentijdse bezetting: **OPEN**.
- D. Gelijktijdige locreserveringen: **ondersteund door meetbeelden, geen volledige concurrency- of safety-garantie**.

## Aanbevolen laatste test

Gebruik een **kopie van de standaard simulatiebaan**. Beperk dit tot één rijdende loc. Maak een op status `1` al zichtbare reservering tot expliciete preconditie, en annuleer/verander daarna met een normale Koploper-bedieningsactie de automatische rit of rijweg. Alleen een handmatige blokkade vóórdat een reservering gemaakt is, test een geweigerde aanvraag in plaats van de intrekking van een bestaande reservering.

Log daarbij de actie met lokale timestamp en neem weer video op. Accepteer test C alleen bij een waarneembare overgang van de concrete blok-/loccombinatie vanuit status 1 naar toestand zonder geldige reservering, zonder voorafgaande status 2, met bevestiging in Koploper.

Deze read-only PoC is **geen autorisatie voor live PWM-bediening**. De toekomstige Observer moet coherente snapshots, freshness, restart-detectie, geldige ID-mapping en de bestaande fysieke safety-interlocks respecteren.
