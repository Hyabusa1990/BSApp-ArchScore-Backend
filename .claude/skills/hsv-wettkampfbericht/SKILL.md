---
name: hsv-wettkampfbericht
description: Schreibt aus den Ergebnissen eines oder mehrerer ArchScore-Fixtures (Bogen-Liga-Wettkampftage im Satzsystem) einen Wettkampfbericht für die Webseite des Hessischen Schützenverbandes (HSV). Verwenden, wenn der Nutzer Fixture-IDs nennt und einen Artikel, Bericht, Spielbericht, Pressetext oder News-Beitrag zu einem Wettkampftag, Ligatag oder Liga-Ergebnis haben möchte – auch wenn nur "schreib was zum Wettkampftag 12" gesagt wird.
---

# HSV-Wettkampfbericht aus ArchScore-Fixtures

Ziel: ein veröffentlichungsfertiger deutscher Artikel für hsv-ev.de (o. ä.), der die Highlights
eines Liga-Wettkampftags erzählt – Tagessieger, starke Leistungen, ein spannender Matchverlauf,
Titel- und Abstiegskampf. Jede Zahl im Artikel muss aus den Daten stammen.

## 1. Eingaben klären

Vom Nutzer brauchst du:

- **Fixture-ID(s)** (Pflicht). Mehrere IDs = ein Wettkampftag an mehreren Orten oder mehrere
  Ligen; frage nach, ob ein gemeinsamer Artikel oder je Liga ein eigener gewünscht ist, wenn die
  `leagueName`-Werte verschieden sind.
- **Zugang zur API**: `ARCHSCORE_API_URL` plus `ARCHSCORE_TOKEN` oder
  `ARCHSCORE_EMAIL`/`ARCHSCORE_PASSWORD` als Umgebungsvariablen. Passwörter nie in den Chat
  schreiben lassen oder ausgeben. Alternativ eine Rohdaten-Datei (`--raw`).
- **Tabelle vor dem Wettkampftag** (optional, aber nötig für Titel-/Abstiegskampf). Die API
  liefert sie nicht. Format: CSV/JSON mit `team;rank;mp_won;mp_lost;sp_won;sp_lost`
  (siehe `examples/tabelle_vorher.csv`). Fehlt sie, frage einmal danach; ohne sie schreibst du
  nur über den Tag selbst und behauptest nichts über die Gesamttabelle.
- **Ligamodus** (optional): wie viele Plätze um den Titel/Aufstieg spielen und wie viele
  absteigen (Standard 1 bzw. 2), wie viele Wettkampftage noch folgen.
- **Einzelschützen**: ArchScore erfasst nur Mannschaften, keine Namen einzelner Schützen.
  Will der Nutzer „Top-Schützen“ nennen, frage nach den Namen/Einzelleistungen oder schreibe
  über die Top-Mannschaften. Erfinde niemals Personen oder Zitate.

Was sich aus dem Kontext ergibt, nicht erneut fragen. Fehlt nur Optionales, arbeite los und
nenne am Ende, was der Artikel deshalb weglässt.

## 2. Daten holen und auswerten

```bash
python3 .claude/skills/hsv-wettkampfbericht/scripts/fixture_report.py <ID> [<ID> ...] \
  --table tabelle_vorher.csv --relegation-places 2 --title-places 1 \
  --dump-raw <scratchpad>/rohdaten.json --format md
```

- Ohne API (Test, Nachbearbeitung): `--raw <datei.json>` statt IDs.
- `--format json` liefert dieselben Fakten maschinenlesbar, falls du nachrechnen willst.
- Das Skript ruft `GET /fixtures/{id}`, `/fixtures/{id}/phase` und
  `/fixtures/{id}/rounds/{n}` auf und spielt jedes Match Satz für Satz nach den Fawkes-Regeln
  nach (5 Sätze à 6 Pfeile, Satzsieg 2 SP, Satz-Remis 1 SP, Sieg ab 6 SP = 2 MP, 5:5 = Remis).

Prüfe die Ausgabe, bevor du schreibst:

- **`offene_matches` nicht leer** → Wettkampf noch nicht abgeschlossen oder Sätze nicht
  bestätigt. Nutzer darauf hinweisen; kein Endergebnis-Artikel ohne Rückfrage.
- **`nicht_in_vortabelle` nicht leer** → Teamnamen in Vortabelle und ArchScore weichen ab;
  Tabelle korrigieren lassen, statt zu raten.

## 3. Geschichte auswählen

Lies `references/stil.md` für Aufbau und Tonalität. Wähle dann aus den Fakten:

1. **Aufmacher** – die wichtigste Nachricht des Tages, meist eine dieser:
   Tabellenführung gewechselt/verteidigt, ungeschlagener Tagessieger, Entscheidung im
   Titel- oder Abstiegskampf, Überraschung (Tabellenletzter schlägt Spitzenteam).
2. **Ein spannendes Match ausführlich** – aus `spannendste_matches` das mit der besten
   Geschichte (Comeback, Entscheidung im 5. Satz, Satz-Remis, Duell der Tabellennachbarn).
   Erzähle den Verlauf mit `satzpunkte_verlauf` und `saetze`, z. B. „lag nach zwei Sätzen
   0:4 zurück, holte mit 57 und 56 Ringen den Ausgleich und entschied den letzten Satz mit
   einem Ring Vorsprung“.
3. **Starke Leistungen** – `perfekte_saetze_60` (immer erwähnen!), `hoechster_satz`,
   `beste_tagesschnitte` (Ringe pro Pfeil wirken anschaulich), `beste_matchschnitte`.
4. **Tabellensituation** – nur mit Vortabelle: Spitze und Abstand, Verfolger,
   Abstiegsplätze, wer noch gefährdet ist. Formuliere vorsichtig („bleibt in Schlagdistanz“),
   da das Skript nicht weiß, wie viele Wettkampftage folgen – außer der Nutzer hat es gesagt.
5. **Ausblick** – nur wenn bekannt (nächster Wettkampftag, Ort, Datum).

Nicht jedes Team muss namentlich vorkommen, aber kein Team darf falsch dargestellt werden.

## 4. Artikel liefern

Gib den Artikel als Markdown aus (Struktur siehe `references/stil.md`) und speichere ihn
zusätzlich als Datei im Arbeitsverzeichnis, z. B. `bericht_<liga>_<wettkampftag>.md`.
Darunter, klar abgesetzt, eine kurze **Faktenprüfung für die Redaktion**: Liste der
genannten Zahlen mit Herkunft (Runde/Match) sowie offene Punkte (fehlende Vortabelle,
keine Einzelschützen, unbestätigte Sätze).

Selbstkontrolle vor der Ausgabe:
- Stimmt jede Zahl (Satzpunkte, Ringe, Tabellenpunkte) mit der Skriptausgabe überein?
- Ist Sieger/Verlierer bei jedem genannten Match korrekt (Team A/B nicht vertauscht)?
- Keine erfundenen Namen, Zitate, Wetterberichte oder Zuschauerzahlen?
