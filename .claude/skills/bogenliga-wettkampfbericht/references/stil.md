# Stilleitfaden Bogenliga-Wettkampfbericht (Verbands- oder Vereinswebseite)

## Tonalität

- Sachlich-sportlich wie ein Lokalsportteil: lebendig, aber nicht reißerisch.
  Keine Superlative ohne Beleg („sensationell“ nur bei echter Überraschung laut Tabelle).
- Neutral gegenüber allen Vereinen – der Verband berichtet für alle Mannschaften seiner Ligen.
- Aktiv formulieren, kurze Sätze, Präteritum für den Wettkampfverlauf, Präsens für die
  Tabellensituation.
- Vereinsnamen beim ersten Nennen vollständig (wie in den Daten); Schützen mit Vor- und Nachname und Verein, danach Ortsname oder
  gängige Kurzform („die Fuldaer“, „Wiesbaden“). Keine Spitznamen erfinden.

## Aufbau (400–600 Wörter, wenn nicht anders gewünscht)

```markdown
# <Titel: „Bogenliga“ prominent vorn + Kernaussage mit Teamname, max. ~80 Zeichen>

**<Teaser, 1–2 Sätze: Liga, Wettkampftag, Ort, wichtigstes Ergebnis.>**

<Absatz 1: Aufmacher – Tagessieger / Tabellenführung und wie es dazu kam.>

<Absatz 2: Das Match des Tages – Verlauf Satz für Satz, Wendepunkt, Entscheidung.>

<Absatz 3: Top-Schützen und weitere Höhepunkte – beste Ringschnitte, 60er-Satz, Überraschungen.>

<Absatz 4: Tabellensituation – je nach Ligaregel Meisterschaft, Relegation oder Aufstieg sowie Abstieg; bei unbekannten Regeln nur Tabellenspitze und -ende.>

<Absatz 5: Dank an den Ausrichter als guten Gastgeber, verbunden mit dem Ausblick auf den nächsten Wettkampftag.>

## Ergebnisse <Liga> – <Wettkampftag>

<Tagestabelle als Markdown-Tabelle: Rang | Mannschaft | Matchpunkte | Satzpunkte>

<Gesamttabelle, falls Vortabelle vorhanden: Rang | Mannschaft | Matchpunkte | Satzpunkte>

## Die nächsten Wettkampftage

<Einladung an Zuschauer, 1–2 Sätze; freier Eintritt nur, wenn laut Regeldatei bekannt.>

- **<Wochentag>, <TT. Monat JJJJ>, ab <HH:MM> Uhr** – <Ausrichter>, <Halle>, <Straße>, <PLZ Ort>
- …
```

Wochentag aus dem Datum berechnen, nicht schätzen. Uhrzeit = Beginn laut bulibow.

Zwischenüberschriften im Fließtext nur bei mehr als ~600 Wörtern.

### Überschrift

Die Überschrift muss auf den ersten Blick zeigen, dass es um **Bogensport** geht – auf den
Webseiten der Schützenverbände stehen auch Berichte aus Gewehr-, Pistolen- und anderen
Disziplinen. Deshalb
„Bogenliga“ (oder „Bogensport“) prominent an den Anfang, z. B. als Dachzeile mit
Doppelpunkt:

- „Bogenliga: Bürstadt startet mit weißer Weste in die Blankbogen-Saison“
- „Bogenliga Hessen: Doppelter Saisonauftakt in Bürstadt“
- „Hessenliga Bogen Compound: Fulda trifft zweimal die 60“
- „Bogenliga Westfalen: Mettingen startet mit sieben Siegen“

Auch Zwischenüberschriften für einzelne Ligen nennen die Bogenklasse
(„Hessenliga Blankbogen“, „Hessenliga Compound“, „Hessenliga Recurve“); heißt eine Liga
ohne Bogenklasse im Namen (z. B. „Westfalenliga“), „Bogen“ ergänzen („Westfalenliga Bogen“).

## Fachbegriffe (Bogenliga, Satzsystem)

| Begriff | Verwendung |
|---|---|
| Wettkampftag | ein Liga-Spieltag (in bulibow ein oder mehrere Fixtures) |
| Match / Begegnung / Duell | zwei Mannschaften auf einem Scheibenpaar |
| Satz | 6 Pfeile einer Mannschaft (3 Schützen × 2 Pfeile), max. 60 Ringe |
| Stechen | Entscheidung per Einzelpfeil bei Gleichstand (falls die Liga es vorsieht) |
| Satzpunkte | 2 für Satzgewinn, je 1 bei Gleichstand; Match gewonnen ab 6 Satzpunkten |
| Matchpunkte (Tabellenpunkte) | 2 für Sieg, je 1 bei 5:5, 0 für Niederlage |
| „Satz-Remis“ / „ringgleich“ | beide Mannschaften gleiche Ringzahl im Satz |
| „Maximum“ / „perfekter Satz“ | 60 Ringe – sechs Zehner |
| Ringschnitt | Ringe pro Pfeil (z. B. 9,25) – anschaulicher als Ringe pro Satz |

Ergebnisse immer als Satzpunkte schreiben („gewann 6:4“), Ringe nur ergänzend
(„… 58:58 im letzten Satz reichte zum 6:4“). Dezimalzahlen mit Komma.

## Formulierungshilfen

- Comeback: „lag nach zwei Sätzen mit 0:4 zurück“, „kämpfte sich zurück“, „drehte die Partie“
- Knapp: „mit einem einzigen Ring Vorsprung“, „ringgleich – beide Teams teilten sich den Satz“
- Dominanz: „ließ nichts anbrennen und siegte glatt mit 6:0“
- Ausrichter: „Ein großes Dankeschön geht an den PBC Bürstadt, der ein hervorragender Gastgeber
  war“, „die Gastgeber sorgten für einen reibungslosen Ablauf“, „bot den Mannschaften
  beste Bedingungen“ (nur allgemein – keine erfundenen Details)
- Einladung: „Zuschauer sind herzlich willkommen – erleben Sie spannenden Bogensport aus
  nächster Nähe und feuern Sie die Mannschaften an!“, „Die Ausrichter freuen sich über jeden
  Besucher in der Halle.“; bei `eintritt_frei: true` immer ergänzen: „der Eintritt ist frei“
  (keine Versprechen zu Eintritt, Verpflegung o. Ä. ohne Quelle)
- Meisterschaft (z. B. Hessenliga): „führt das Rennen um den Hessenmeistertitel an“, „hat die
  Meisterschaft im Blick“, „Titelverteidiger“ (nur wenn bekannt) – nie „Aufstiegsplatz“
- Relegation (z. B. Hessenliga Recurve): „belegt einen der beiden Plätze für die
  Aufstiegsrelegation zur Regionalliga“
- Aufstieg: „steht auf dem Aufstiegsplatz“
- Abstieg bei genau einem Absteiger: „steht auf dem einzigen Abstiegsplatz“, „will den
  letzten Platz schnell wieder verlassen“, „hat zwei Punkte Vorsprung auf den Abstiegsplatz“
- Abstieg bei offener Anzahl: „rangiert auf dem sicheren Abstiegsplatz“, „muss je nach
  Absteigern aus der höheren Liga ebenfalls noch zittern“
- Regeln unbekannt: „übernimmt die Tabellenführung“, „Schlusslicht“, „steht am Tabellenende“
  – keine Begriffe wie Aufstieg, Abstieg, Meister oder Relegation
- Tabelle: „übernimmt die Tabellenspitze“, „verteidigt Platz eins“, „bleibt mit nur einem
  Punkt Rückstand in Schlagdistanz“, „steht weiter auf einem Abstiegsplatz“, „verschafft sich
  Luft im Tabellenkeller“

## Was nicht in den Artikel gehört

- Namen von Schützen, die nicht aus den Daten stammen; Trainer oder Zitate, die der Nutzer nicht geliefert hat.
- Schwache Einzelleistungen namentlich hervorheben – Schützen werden nur positiv genannt.
- Begriffe wie „Einzelwertung“, „Einzelrangliste“, „Einzelsieger“ – es gibt keine offizielle
  Einzelwertung; Einzelleistungen sind nur Statistik („bester Ringschnitt des Tages“).
- Angaben zu Wetter, Halle, Zuschauern, Stimmung ohne Quelle.
- Prognosen zu Auf-/Abstieg als Tatsache, solange rechnerisch noch alles offen ist; Aussagen
  zu Titel, Aufstieg, Relegation oder Abstieg, die nicht durch die Regeldatei gedeckt sind.
- Interne Begriffe aus BuLiBow (Fixture, Target, RoundNo, Spotter, DOS, Satz 99).
