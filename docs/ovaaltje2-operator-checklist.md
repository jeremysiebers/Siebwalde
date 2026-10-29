# Ovaaltje2 — Operator-checklist (fysieke acceptatie, niet-beladen)

**Doel:** de normale Siebwalde C#-applicatie niet-beladen valideren (koude start, vier-amp initialisatie, global-stop-latch-detectie en expliciet operator-herstel). **Geen locomotief, geen firmware-flash, geen H-brug-activatie.**

> Deze checklist is de operator-in-the-loop-overdracht. De applicatie heeft **geen** software-leesbare fysieke stop-sensor: "physical stop inputs clear" is altijd een **bevestiging van de operator**, nooit een gesimuleerde of hardcoded waarde.

## Stap 1 — Fysieke voorinspectie (operator, vóór alles)

| # | Actie | Vereist resultaat |
|---|-------|-------------------|
| 1.1 | Bevestig dat er **geen locomotief** op het testspoor staat | Geen locomotief aanwezig |
| 1.2 | Bevestig de **veilige elektrische conditie**: baanspanning geïsoleerd / H-brug-uitgangen fysiek gede-energiseerd (bijv. voeding uit, of een fysieke scheidingsschakelaar) | Uitgangen kunnen **niet** bekrachtigen — niet alleen "geen locomotief" |
| 1.3 | Bevestig dat de **noodstop** bereikbaar en functioneel is | Noodstop binnen handbereik |

**STOP als 1.1–1.3 niet alle drie bevestigd zijn.** Zonder 1.2 mag er geen actieve initialisatie plaatsvinden.

## Stap 2 — Koude start van de normale applicatie

| # | Actie | Wat de applicatie moet tonen / wat PASS bevestigt |
|---|-------|---------------------------------------------------|
| 2.1 | Start de normale WPF-app; de init-pagina detecteert de track-host | Host aanwezig, geen fout |
| 2.2 | Start de track-runtime (Real mode) | Runtime bereikt **Running**; heartbeat (110) zichtbaar; PIC32 verbonden |
| 2.3 | Observeer de initiële global-stop-status | **Disarmed/Armed** (niet gelatcht) → init loopt door |
| 2.4 | Vier-amp initialisatie (1/3/4/6) | Alle vier **CONFIG_OK** (configVerified); identiteit/compatibiliteit geverifieerd; geen flash |
| 2.5 | Controleer uitgangen | **Geen** niet-neutraal HR0; PWM blijft 399; geen onbedoelde H-brug-activatie |

**PASS-bewijs 2.x:** host gedetecteerd, Running, CONFIG_OK ×4, geen niet-neutraal commando.

## Stap 3 — Gelatchte global-stop + herstel

| # | Actie | Vereist resultaat |
|---|-------|-------------------|
| 3.1 | Veroorzaak/observeer een gelatchte global-stop (bijv. host-verlies uit een eerdere sessie) | Command 111 meldt **gelatcht (1 of 2)** |
| 3.2 | Bekijk de init-pagina | UI toont **gelatchte** status + reden |
| 3.3 | Controleer de herstelknop | Knop alleen actief bij gelatchte status |
| 3.4 | Operator klikt herstel en **bevestigt** dat de fysieke stop-ingangen vrij zijn | Modal bevestiging vereist; herstel geweigerd zonder OK |
| 3.5 | Controleer command 112 + 111 re-read | 112 verstuurd; 111 = **Armed** |
| 3.6 | Controleer C#-state | In-memory `GlobalStopController` = **Armed** (consistent) |
| 3.7 | Controleer bewegings-autorisatie | **Geen** automatische bewegings-autorisatie na re-arm |

## Stap 4 — Ongeldig herstel (fail-closed)

| # | Scenario | Vereist resultaat |
|---|----------|-------------------|
| 4.1 | Geen operator-autorisatie | Herstel blijft geblokkeerd, geen 112 |
| 4.2 | Physical-stop-clear **niet** bevestigd | Herstel geweigerd (fail-closed), geen 112 |
| 4.3 | Herstel mislukt/time-out | Geen voortgang, geen output-activatie |
| 4.4 | Communicatie onderbroken | Geen onbedoelde activatie; veilige toestand behouden |

**Elk 4.x-geval moet fail-closed eindigen en mag de uitgangen NIET activeren.**

## Stap 5 — Wanneer stoppen

Stop onmiddellijk de test en meld bij de Project Lead als:
- een uitgang onbedoeld activeert (niet-neutraal HR0 / fysieke H-brug-activiteit), of
- 112 niet leidt tot Armed, of
- de applicatie geen gelatchte status toont terwijl 111 gelatcht meldt, of
- enige fysieke veiligheidsconditie uit Stap 1 wegvalt.

## Stap 6 — Bewijsregistratie

Noteer per uitgevoerde stap: commando-niveau bewijs (verzonden/ACK) én, waar fysiek, de door de operator waargenomen toestand. Claim **nooit** een fysieke stop of fysieke de-energisatie uit een firmware-register (V4-grens). Gebruik de **normale productie-applicatie**, niet alleen de Diagnostics MCP-harness. De HR8 logische enable-active bit is **geen** bewijs van fysieke H-brug-toestand.

## Gereedheids-oordeel

Na Stap 1–4 met alle PASS-resultaten en de operator-bevestigingen: **READY** voor de eerste gesuperviseerde test. Deel E (beladen locomotief) vereist een **aparte PO GO**.
