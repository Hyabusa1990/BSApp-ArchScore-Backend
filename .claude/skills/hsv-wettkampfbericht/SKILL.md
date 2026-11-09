---
name: hsv-wettkampfbericht
description: Schreibt aus den Ergebnissen eines oder mehrerer bulibow-Fixtures (Wettkampftage der Bogenligen Hessen, api.bogenligen-hessen.org) einen Wettkampfbericht für die Webseite des Hessischen Schützenverbandes (HSV). Verwenden, wenn der Nutzer Fixture-IDs nennt und einen Artikel, Bericht, Spielbericht, Pressetext oder News-Beitrag zu einem Wettkampftag, Ligatag oder Liga-Ergebnis haben möchte – auch wenn nur "schreib was zum Wettkampftag 195" gesagt wird.
---

# HSV-Wettkampfbericht aus bulibow-Fixtures

Ziel: ein veröffentlichungsfertiger deutscher Artikel für die HSV-Webseite, der die Highlights
eines Liga-Wettkampftags erzählt – Tagessieger, Top-Schützen, ein spannender Matchverlauf,
Meister-, Aufstiegs- und Abstiegskampf, Dank an den Ausrichter, kommende Termine mit Einladung an Zuschauer. Jede Zahl und jeder Name im Artikel muss aus den Daten stammen.

## 1. Eingaben klären

- **Fixture-ID(s)** (Pflicht) aus bulibow. Mehrere IDs derselben Liga (z. B. zwei Durchgänge
  an einem Tag, wie in der Bezirksliga) fasst das Skript zu einem Wettkampftag zusammen.
  IDs verschiedener Ligen ergeben je einen Abschnitt; frage dann, ob ein gemeinsamer Artikel
  oder je Liga ein eigener gewünscht ist.
  Kennt der Nutzer die ID nicht, finde sie über den Kalender:
  `curl -s "https://api.bogenligen-hessen.org/api/public/calendar?seasonId=<id>"`
  (Saisons: `/api/public/seasons`), Felder `fixtureId`, `league`, `dateTime`, `locationCity`.
- **Ligamodus** – das Skript erkennt ihn am Liganamen (`ligamodus` in `tabellensituation`):
  - **Hessenliga** (höchste hessische Spielklasse, `meisterschaft`): Es geht um den
    **Meistertitel** (Hessenmeister), es gibt keinen Aufstieg. Nie „Aufstiegsplatz“ schreiben.
  - **Hessenliga Recurve** (`meisterschaft_mit_relegation`): Platz 1 wird Hessenmeister;
    die **ersten beiden** dürfen in der **Aufstiegsrelegation zur Regionalliga** antreten.
  - **Oberliga, Verbandsliga, Bezirksliga** (`aufstieg`): Der Erste steigt auf.
  - **Abstieg** (alle Ligen): Der Letzte steigt sicher ab. Ob weitere absteigen, steht während
    der Saison nicht fest – in den unteren Ligen hängt es von den Absteigern aus den höheren
    Ligen ab. Das Skript markiert die zwei Plätze vor dem Letzten als „möglicherweise
    abstiegsgefährdet“ (`--maybe-relegation-places`).
  Übersteuern nur bei abweichenden Regeln (`--title-places`, `--relegation-places`).

Mehr braucht es nicht: Die öffentlichen Endpunkte erfordern keinen Login, Vortabelle,
verbleibende Wettkampftage und nächster Termin kommen aus dem Ligakalender.

## 2. Daten holen und auswerten

```bash
python3 .claude/skills/hsv-wettkampfbericht/scripts/fixture_report.py <ID> [<ID> ...] \
  --dump-raw <scratchpad>/rohdaten.json
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
   Wichtig: Es gibt in den Bogenligen **keine offizielle Einzelwertung** – die Zahlen sind
   nur eine Statistik. Sie dürfen gern in den Artikel, aber als Statistik formuliert
   („den besten Ringschnitt des Tages erzielte …“, „statistisch stärkster Schütze“), nie als
   Wertung, Rangliste, Sieg oder Platzierung („gewann die Einzelwertung“, „Platz 1 der
   Einzelrangliste“). Bei Abweichungen gelten die Zahlen der bulibow-Statistik.
4. **Weitere Höhepunkte** – `perfekte_saetze_60` (immer erwähnen), `hoechster_satz`,
   beste Mannschaftsschnitte, Comebacks, glatte Siege des Underdogs.
5. **Tabellensituation** – je nach `ligamodus`: Kampf um den Meistertitel (Hessenliga), um
   die Relegationsplätze zur Regionalliga (Hessenliga Recurve, Plätze 1–2) oder um den
   Aufstiegsplatz (darunter); Abstand, Verfolger, Rangänderungen (`rang_vorher`). Unten: Der
   Letzte steht auf dem sicheren Abstiegsplatz; die Plätze davor nur als „möglicherweise
   gefährdet“ beschreiben („ob auch Platz sieben absteigen muss, steht erst am Saisonende
   fest“; in unteren Ligen: „… entscheidet sich mit den Absteigern aus den höheren Ligen“) –
   nie als feststehend. Mit `verbleibende_wettkampftage` einordnen („drei Wettkampftage vor
   Schluss“). Am ersten Wettkampftag der Saison: nur „erste Tabellenführung“ etc.
6. **Ausrichter würdigen** – jeder Artikel hebt den Ausrichter (`Ausrichter`, Halle/Ort) als
   guten Gastgeber hervor und dankt ihm, z. B. für die Ausrichtung und die gute Organisation.
   Allgemein und herzlich formulieren; keine Details erfinden (Verpflegung, Helferzahl,
   Hallenausstattung), außer der Nutzer nennt sie. Ist kein Ausrichter angegeben, nach ihm
   fragen oder den Ort als Gastgeber nennen.
7. **Ausblick und Termine** – im Fließtext kurz der nächste Wettkampftag. Am Ende des
   Artikels (nach den Tabellen) **immer** der Abschnitt „Die nächsten Wettkampftage“ mit allen
   Einträgen aus `kommende_wettkampftage` (Datum mit Wochentag, Beginn, Ausrichter, Halle mit
   Adresse) und einer herzlichen Einladung an Zuschauer: Bogensport live erleben, die
   Mannschaften anfeuern. Der Eintritt ist bei allen Wettkampftagen der hessischen Ligen
   frei – das gehört immer in die Einladung. Darüber hinaus nichts versprechen, was nicht
   sicher ist (z. B. Verpflegung). Beim letzten Wettkampftag der Saison
   entfällt die Liste; stattdessen Dank an alle Ausrichter und Zuschauer der Saison.

Nicht jedes Team muss namentlich vorkommen, aber kein Team darf falsch dargestellt werden.

## 4. Artikel liefern

Gib den Artikel als Markdown aus (Struktur siehe `references/stil.md`) und speichere ihn
zusätzlich als Datei im Arbeitsverzeichnis, z. B. `bericht_<liga>_<wettkampftag>.md`.
Darunter, klar abgesetzt, eine kurze **Faktenprüfung für die Redaktion**: genannte Zahlen
und Namen mit Herkunft (Runde/Match, Einzelstatistik) sowie offene Punkte.

Selbstkontrolle vor der Ausgabe:
- Stimmt jede Zahl (Satzpunkte, Ringe, Matchpunkte, Schnitte) mit der Skriptausgabe überein?
- Ist Sieger/Verlierer bei jedem genannten Match korrekt (Team A/B nicht vertauscht)?
- Sind Namen korrekt geschrieben und dem richtigen Verein zugeordnet?
- Keine erfundenen Zitate, Wetterberichte oder Zuschauerzahlen?
- Steht „Bogenliga“ (bzw. „Bogensport“) prominent in der Überschrift?
- Werden Einzelleistungen als Statistik und nicht als Einzelwertung dargestellt?
