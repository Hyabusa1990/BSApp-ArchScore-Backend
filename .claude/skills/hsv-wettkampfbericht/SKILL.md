---
name: hsv-wettkampfbericht
description: Schreibt aus den Ergebnissen eines oder mehrerer bulibow-Fixtures (Wettkampftage der Bogenligen Hessen, api.bogenligen-hessen.org) einen Wettkampfbericht für die Webseite des Hessischen Schützenverbandes (HSV). Verwenden, wenn der Nutzer Fixture-IDs nennt und einen Artikel, Bericht, Spielbericht, Pressetext oder News-Beitrag zu einem Wettkampftag, Ligatag oder Liga-Ergebnis haben möchte – auch wenn nur "schreib was zum Wettkampftag 195" gesagt wird.
---

# HSV-Wettkampfbericht aus bulibow-Fixtures

Ziel: ein veröffentlichungsfertiger deutscher Artikel für die HSV-Webseite, der die Highlights
eines Liga-Wettkampftags erzählt – Tagessieger, Top-Schützen, ein spannender Matchverlauf,
Titel- und Abstiegskampf, Ausblick. Jede Zahl und jeder Name im Artikel muss aus den Daten stammen.

## 1. Eingaben klären

- **Fixture-ID(s)** (Pflicht) aus bulibow. Mehrere IDs derselben Liga (z. B. zwei Durchgänge
  an einem Tag, wie in der Bezirksliga) fasst das Skript zu einem Wettkampftag zusammen.
  IDs verschiedener Ligen ergeben je einen Abschnitt; frage dann, ob ein gemeinsamer Artikel
  oder je Liga ein eigener gewünscht ist.
  Kennt der Nutzer die ID nicht, finde sie über den Kalender:
  `curl -s "https://api.bogenligen-hessen.org/api/public/calendar?seasonId=<id>"`
  (Saisons: `/api/public/seasons`), Felder `fixtureId`, `league`, `dateTime`, `locationCity`.
- **Ligamodus** (optional): wie viele Plätze um Titel/Aufstieg spielen und wie viele
  absteigen (Standard 1 bzw. 2). Unbekannt → vorsichtig formulieren („Tabellenende“ statt
  „Abstiegsplatz“).

Mehr braucht es nicht: Die öffentlichen Endpunkte erfordern keinen Login, Vortabelle,
verbleibende Wettkampftage und nächster Termin kommen aus dem Ligakalender.

## 2. Daten holen und auswerten

```bash
python3 .claude/skills/hsv-wettkampfbericht/scripts/fixture_report.py <ID> [<ID> ...] \
  --title-places 1 --relegation-places 2 --dump-raw <scratchpad>/rohdaten.json
```

- Ausgabe standardmäßig Markdown; `--format json` liefert dieselben Fakten maschinenlesbar.
- `--raw <datei.json>` wertet zuvor gespeicherte Rohdaten ohne API aus.
- Andere API-Instanz: `--api-url` oder `BULIBOW_API_URL`.
- Das Skript nutzt `/api/public/fixture/{id}/info|rounds|table|individualranking` und
  `/api/public/calendar`. Satzpunkte und Matchpunkte übernimmt es aus bulibow, den Verlauf
  rechnet es Satz für Satz nach; ein Stechen steht in den Daten als Satz 99.
- Die Tabelle eines Fixtures ist der Stand **nach** diesem Fixture; die Vortabelle ist die
  Tabelle des letzten Fixtures der Liga davor.

Prüfe die Ausgabe, bevor du schreibst:

- **„keine abgeschlossenen Matches“** oder **`offene_matches`** → Wettkampf noch nicht
  (vollständig) geschossen. Nutzer informieren, keinen Ergebnisbericht ohne Rückfrage.
- **`tabellen_abweichung`** → Tabelle und Matchergebnisse passen nicht zusammen (z. B.
  nachträgliche Korrektur). Nutzer darauf hinweisen, Tabellenzahlen aus bulibow verwenden.
- **Einzelschützen „aussagekraeftig: False“** → Pfeile sind keinen Schützen zugeordnet; dann
  keine Top-Schützen nennen, nur Mannschaftsleistungen.

## 3. Geschichte auswählen

Lies `references/stil.md` für Aufbau und Tonalität. Wähle dann aus den Fakten:

1. **Aufmacher** – die wichtigste Nachricht: Tabellenführung übernommen/verteidigt,
   ungeschlagener Tagessieger, Entscheidung im Titel- oder Abstiegskampf, Überraschung
   (Tabellenletzter schlägt Spitzenteam, Favorit verliert glatt 0:6).
2. **Ein spannendes Match ausführlich** – aus `spannendste_matches` das mit der besten
   Geschichte (Stechen, Comeback, Entscheidung im 5. Satz, Duell der Tabellennachbarn).
   Erzähle den Verlauf mit `satzpunkte_verlauf` und `saetze`.
3. **Top-Schützen** – aus `top_schuetzen` (Ringschnitt pro Pfeil, nur Schützen mit
   genügend Pfeilen) und `meiste_zehner`; ein bis drei Namen mit Verein und Schnitt.
   Bei Abweichungen gilt die Einzelrangliste laut bulibow.
4. **Weitere Höhepunkte** – `perfekte_saetze_60` (immer erwähnen), `hoechster_satz`,
   beste Mannschaftsschnitte, Comebacks, glatte Siege des Underdogs.
5. **Tabellensituation** – Spitze und Abstand, Verfolger, Tabellenende, Rangänderungen
   (`rang_vorher`). Mit `verbleibende_wettkampftage` einordnen („drei Wettkampftage vor
   Schluss“). Am ersten Wettkampftag der Saison: nur „erste Tabellenführung“ etc.
6. **Ausblick** – `naechster_wettkampftag` (Datum, Ausrichter, Ort).

Nicht jedes Team muss namentlich vorkommen, aber kein Team darf falsch dargestellt werden.

## 4. Artikel liefern

Gib den Artikel als Markdown aus (Struktur siehe `references/stil.md`) und speichere ihn
zusätzlich als Datei im Arbeitsverzeichnis, z. B. `bericht_<liga>_<wettkampftag>.md`.
Darunter, klar abgesetzt, eine kurze **Faktenprüfung für die Redaktion**: genannte Zahlen
und Namen mit Herkunft (Runde/Match, Einzelauswertung) sowie offene Punkte.

Selbstkontrolle vor der Ausgabe:
- Stimmt jede Zahl (Satzpunkte, Ringe, Matchpunkte, Schnitte) mit der Skriptausgabe überein?
- Ist Sieger/Verlierer bei jedem genannten Match korrekt (Team A/B nicht vertauscht)?
- Sind Namen korrekt geschrieben und dem richtigen Verein zugeordnet?
- Keine erfundenen Zitate, Wetterberichte oder Zuschauerzahlen?
