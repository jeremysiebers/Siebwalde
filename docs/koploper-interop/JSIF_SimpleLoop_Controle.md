# Koploper — Simple Loop met vier prototype-amplifiers

## Bron en resultaat

- Bron: de aangeleverde `JSIF.zip` (Koploper-datadirectory `JSIF/`).
- Resultaat: `JSIF_SimpleLoop_4Amp.zip`, met dezelfde directory-root `JSIF/`, dezelfde tekstbestandsconventies (tabvelden, CRLF, Windows-1252 waar nodig) en opnieuw berekende ZIP-CRC's.
- Het aangeleverde archief was **niet versleuteld**; de aangepaste versie is eveneens niet versleuteld.
- Het oorspronkelijke bronarchief is niet aangepast.

## Voorgestelde mapping

| Koploper-blok | Eén bezetmelder | Intern sensornummer | Logische Simple Loop-sectie | Fysieke amplifier (latere Real-binding) | Volgend blok |
|---|---|---:|---:|---:|---:|
| 1 | 1.01 | 101 | 1 | 1 | 2 |
| 2 | 1.02 | 102 | 2 | 3 | 3 |
| 3 | 1.03 | 103 | 3 | 4 | 4 |
| 4 | 1.04 | 104 | 4 | 6 | 1 |

**Belangrijk:** de fysieke amplifier-adressen 1/3/4/6 staan hier als aansluit-/C#-bindingsspecificatie. Koploper stuurt in deze configuratie op ECoS-loc- en feedbackadressen, niet direct op Modbus-slave-adressen. De Siebwalde C# Simple Loop-binding moet deze tabel dus overnemen voordat een echte hardwaretest begint. De mapping van de huidige C#-profielbestanden is niet met dit ZIP-bestand mee gewijzigd.

### Baan

```text
                Blok 4  (1.04)
          +-----------------------+
          |                       |
 Blok 3   |                       |  Blok 1
 (1.03)   |                       |  (1.01)
          |                       |
          +-----------------------+
                Blok 2  (1.02)

         Rijvolgorde: 1 → 2 → 3 → 4 → 1
```

- Geen wissels of passerende tak; oorspronkelijke wisselobjecten 1/2 en bijbehorende signaalplaatsingen uit de tekening verwijderd.
- Eén indicator (`INBL`), één feedbackadres (`BZWL`) en één routegebonden feedbackdefinitie (`INBV`) per blok.
- De volledige `LIJN`-geometrie vormt één aaneengesloten, gesloten ovaal.
- Oorspronkelijke locomotieven behouden: decoderadressen **1** en **2**, bestaande ECoS-objectidentificaties **1000** en **1001**. De actuele, opgeslagen locplaatsing (`locr.dba`) is aangepast naar loc 1 in blok 1 en loc 2 in blok 3.

## Consistent doorgevoerde wijzigingen

| Bestand | Wijziging |
|---|---|
| `baan.dba` | Eén ovaal met 8 aangesloten lijnsegmenten; 4 blokken; geen wissels, passeertak of seinplaatsingen; vier feedbackindicatoren/relaties. |
| `blok.dba` | Blok 5 verwijderd; vier voorganger-/route-relaties voor de gesloten lus; één melder per blok in `BLOK` en `BLVN`; oude deadlock-/passeerlusinstellingen verwijderd. |
| `kopd.dba` | `BLAV5` verwijderd, `BLAV1` verwijst alleen nog naar voorganger 4. Locomotief- en algemene definities behouden. |
| `locr.dba` | Bestaande loco 1 en 2 naar geldige blokken 1 en 3 verplaatst. |
| `kopl.ini` | Laatst gebruikte blok-ID aangepast van 5 naar 4; overige ECoS-/installatie-instellingen behouden. |
| `snel.dba` | Oude, baan- en sensorafhankelijke snelheidskalibraties gereset (leeg bestand); opnieuw kalibreren voor deze testbaan. |
| `save_oud.txt` | Niet meegeleverd: historische runtime-snapshot bevatte blok 5, oude wisselstatus en feedbackadressen. |
| Overige bestanden | Ongewijzigd behouden. |

## Uitgevoerde controles

1. Exact vier `BLOK`, `BLAV` en `PBLK`-definities, plus één bezetmelder per `BLOK`/`BLVN`.
2. `BLVN`/`BLRI`/`BLAV` volgen gesloten 1→2→3→4→1, zonder verwijzing naar blok 5.
3. Vier `INBL`, vier `INBV`, vier `BZWL`; feedback-ID's 101, 102, 103, 104 zijn bij alle bijbehorende records gelijk.
4. Acht resterende `LIJN`-segmenten vormen geometrisch één gesloten verbinding (alle eindpunten sluiten aan; nergens een losse tak).
5. Geen overgebleven `WISS` of `WSTR`-records in het baanbeeld.
6. Beide locs blijven gedefinieerd, starten in bestaande blokken en behouden hun decoderadressen.
7. ZIP is opnieuw opgebouwd; Python-ZIP-CRC-check en `unzip -t` beide PASS; ongewijzigde bronbestanden zijn byte-identiek gebleven.

**Beperkingsgrens:** dit is een structurele/format- en verwijzingscontrole. De daadwerkelijke Koploper-applicatie is in deze omgeving niet beschikbaar. Ik kan daarom niet claimen dat het nieuwe databestand al daadwerkelijk in Koploper is geopend of dat automatisch rijden met één melder per blok zonder verdere Koploper-stop-/remconfiguratie veilig functioneert.

## Eerste import-/operatorcheck

1. Sluit Koploper volledig. Bewaar de oorspronkelijke `JSIF.zip` en, indien van toepassing, de oorspronkelijke `JSIF`-directory.
2. Pak het nieuwe ZIP-archief uit op een schone testlocatie. Het bevat opnieuw een rootmap `JSIF/`. Vervang niet simpelweg losse bestanden bovenop de oude map: dan kunnen verwijderde oude bestanden (zoals `save_oud.txt`) achterblijven.
3. Laat Koploper de **nieuwe testkopie** van de `JSIF`-gegevens openen via dezelfde directory-/restorewerkwijze waarmee het aangeleverde origineel gebruikt wordt. Dit archief is een ZIP van de Koploper-datadirectory, geen extra zelfbedachte `.bck`-container.
4. Controleer op de baanpagina: ovaal zonder wissels; blokken 1 t/m 4; vier bezetmelders (1.01–1.04); geen blok 5, melders 1.05–1.10 of oude passeerlus.
5. Controleer in de blokgegevens iedere relatie en bevestig dat Koploper het gebruik van één melder per blok met de gewenste stop-/remmethode accepteert. Oude snelheidsmetingen zijn bewust verwijderd.
6. Pas de C# `simple-loop.json`/Real-binding aan dezelfde tabel aan voordat je hardware aansluit. Voer nog geen fysieke rit uit op basis van alleen de geslaagde ZIP-structuurcontrole.

SHA-256 van het resultaat: `4eaeb131128670eee86bac7421a9bc7fdcbee93a07ea66828a9e22a110132a3a`.
