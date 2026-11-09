---
name: bogenliga-wettkampfbericht
description: Schreibt aus den Ergebnissen eines oder mehrerer BuLiBow-Fixtures (Wettkampftage von Bogenligen im Satzsystem, z. B. Hessen und Westfalen, API api.bogenligen-hessen.org) einen Wettkampfbericht für die Webseite des zuständigen Landesverbands oder Vereins. Verwenden, wenn der Nutzer Fixture-IDs nennt und einen Artikel, Bericht, Spielbericht, Pressetext oder News-Beitrag zu einem Bogenliga-Wettkampftag, Ligatag oder Liga-Ergebnis haben möchte – auch wenn nur "schreib was zum Wettkampftag 195" gesagt wird. Kann den Bericht auch als Word-Dokument (.docx) ausgeben.
---

# Bogenliga-Wettkampfbericht aus BuLiBow-Fixtures

`<SKILL_DIR>` ist das Verzeichnis dieses Skills (dort, wo diese SKILL.md liegt), z. B.
`.claude/skills/bogenliga-wettkampfbericht` im Repository oder
`~/.claude/skills/bogenliga-wettkampfbericht`.

Ziel: ein veröffentlichungsfertiger deutscher Artikel für die Webseite des zuständigen
Landesverbands (oder eines Vereins), der die Highlights eines Liga-Wettkampftags erzählt –
Tagessieger, Top-Schützen, ein spannender Matchverlauf, Kampf um Titel/Aufstieg und gegen den
Abstieg, Dank an den Ausrichter, kommende Termine mit Einladung an Zuschauer. Jede Zahl und
jeder Name im Artikel muss aus den Daten stammen.

## 1. Eingaben klären

- **Fixture-ID(s)** (Pflicht) aus BuLiBow. Mehrere IDs derselben Liga (z. B. zwei Durchgänge
  an einem Tag) fasst das Skript zu einem Wettkampftag zusammen. IDs verschiedener Ligen
  ergeben je einen Abschnitt; ein gemeinsamer Artikel bietet sich an, wenn sie am selben Tag
  beim selben Ausrichter stattfanden – sonst fragen, ob gemeinsam oder getrennt.
  Kennt der Nutzer die ID nicht, finde sie über den Kalender:
  `curl -s "https://api.bogenligen-hessen.org/api/public/calendar?seasonId=<id>"`
  (Saisons: `/api/public/seasons`, Ligen mit Region: `/api/public/seasoninfo?seasonId=<id>`).
- **Region und Ligaregeln** – das Skript ermittelt die Region der Liga (z. B. `HS` Hessen,
  `WF` Westfalen) aus BuLiBow und liest die Regeln aus `references/regionen.json`:
  Bedeutung der Spitzenplätze (Meistertitel, Aufstieg, Relegation), Abstiegsregel, Verband,
  ob der Eintritt frei ist. Siehe `ligamodus`, `bedeutung_spitzenplaetze`, `abstieg_regel`
  in `tabellensituation` sowie `region`, `verband`, `eintritt_frei` im Bericht.
  - **Regeln bekannt** → genau danach formulieren (z. B. Hessen: In der Hessenliga geht es um
    den Hessenmeistertitel, nie „Aufstiegsplatz“ schreiben; Hessenliga Recurve: Plätze 1–2 zur
    Aufstiegsrelegation in die Regionalliga; Ober-/Verbands-/Bezirksliga: Erster steigt auf).
  - **Regeln unbekannt** (`ligaregeln_bekannt: false`, z. B. Westfalen oder Regionalliga
    West) → keine Aussagen zu Meistertitel, Aufstieg, Relegation oder Abstieg; nur
    „Tabellenführer“, „Verfolger“, „Tabellenende“. Einmal beim Nutzer nachfragen, ob er die
    Regeln nennen möchte – dann in `references/regionen.json` ergänzen (gleiches Schema wie
    `HS`), damit sie künftig gelten.
  - Einzelne Abweichungen: `--title-places`, `--relegation-places`, `--maybe-relegation-places`.
- **Zielmedium** (optional): Für welche Webseite/welchen Verband wird geschrieben? Standard
  ist der `verband` aus der Regeldatei; ist er unbekannt, neutral formulieren und nicht raten.

Die öffentlichen Endpunkte erfordern keinen Login. Vortabelle, verbleibende Wettkampftage und
Termine kommen aus dem Ligakalender.

## 2. Daten holen und auswerten

```bash
python3 <SKILL_DIR>/scripts/fixture_report.py <ID> [<ID> ...] \
  --dump-raw <scratchpad>/rohdaten.json
```

- Ausgabe standardmäßig Markdown; `--format json` liefert dieselben Fakten maschinenlesbar.
- `--raw <datei.json>` wertet zuvor gespeicherte Rohdaten ohne API aus.
- Andere BuLiBow-Instanz: `--api-url` oder `BULIBOW_API_URL`; andere Regeldatei: `--regeln`.
- Das Skript nutzt `/api/public/fixture/{id}/info|rounds|table|individualranking`,
  `/api/public/calendar` und `/api/public/seasoninfo`. Satzpunkte und Matchpunkte übernimmt
  es aus BuLiBow, den Verlauf rechnet es Satz für Satz nach; ein Stechen steht in den Daten
  als Satz 99.
- Die Tabelle eines Fixtures ist der Stand **nach** diesem Fixture; die Vortabelle ist die
  Tabelle des letzten Fixtures der Liga davor.

Prüfe die Ausgabe, bevor du schreibst:

- **„keine abgeschlossenen Matches“** oder **`offene_matches`** → Wettkampf noch nicht
  (vollständig) geschossen. Nutzer informieren, keinen Ergebnisbericht ohne Rückfrage.
- **`tabellen_abweichung`** → Tabelle und Matchergebnisse passen nicht zusammen (z. B.
  nachträgliche Korrektur). Nutzer darauf hinweisen, Tabellenzahlen aus BuLiBow verwenden.
- **Einzelstatistik „aussagekraeftig: False“** → Pfeile sind keinen Schützen zugeordnet; dann
  keine Top-Schützen nennen, nur Mannschaftsleistungen.
- **„Ligaregeln bekannt: NEIN“** → siehe oben, vorsichtig formulieren.

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
   Einzelrangliste“). Bei Abweichungen gelten die Zahlen der BuLiBow-Statistik.
4. **Weitere Höhepunkte** – `perfekte_saetze_60` (immer erwähnen), `hoechster_satz`,
   beste Mannschaftsschnitte, Comebacks, glatte Siege des Underdogs.
5. **Tabellensituation** – je nach `ligamodus` und `bedeutung_spitzenplaetze`: Kampf um den
   Meistertitel, um Relegationsplätze oder um den Aufstiegsplatz; Abstand, Verfolger,
   Rangänderungen (`rang_vorher`). Unten nach `abstieg_regel`: Gibt es genau einen Absteiger,
   vom „einzigen Abstiegsplatz“ sprechen, ohne über weitere zu spekulieren; sonst steht der
   Letzte auf dem sicheren Abstiegsplatz und die Plätze aus
   `moeglicherweise_abstiegsgefaehrdet` werden nur als „möglicherweise gefährdet“ beschrieben
   – nie als feststehend. Bei `ligamodus: unbekannt` nur Tabellenführung und Tabellenende.
   Mit `verbleibende_wettkampftage` einordnen („drei Wettkampftage vor Schluss“). Am ersten
   Wettkampftag der Saison: nur „erste Tabellenführung“ etc.
6. **Ausrichter würdigen** – jeder Artikel hebt den Ausrichter (`Ausrichter`, Halle/Ort) als
   guten Gastgeber hervor und dankt ihm, z. B. für die Ausrichtung und die gute Organisation.
   Allgemein und herzlich formulieren; keine Details erfinden (Verpflegung, Helferzahl,
   Hallenausstattung), außer der Nutzer nennt sie. Ist kein Ausrichter angegeben, nach ihm
   fragen oder den Ort als Gastgeber nennen.
7. **Ausblick und Termine** – im Fließtext kurz der nächste Wettkampftag. Am Ende des
   Artikels (nach den Tabellen) **immer** der Abschnitt „Die nächsten Wettkampftage“ mit allen
   Einträgen aus `kommende_wettkampftage` (Datum mit Wochentag, Beginn, Ausrichter, Halle mit
   Adresse) und einer herzlichen Einladung an Zuschauer: Bogensport live erleben, die
   Mannschaften anfeuern. **Freier Eintritt** nur nennen, wenn `eintritt_frei` true ist (dann
   immer); bei unbekannt nichts zum Eintritt sagen. Nichts versprechen, was nicht sicher ist
   (z. B. Verpflegung). Beim letzten Wettkampftag der Saison entfällt die Liste; stattdessen
   Dank an alle Ausrichter und Zuschauer der Saison.

Nicht jedes Team muss namentlich vorkommen, aber kein Team darf falsch dargestellt werden.

## 4. Artikel liefern

Gib den Artikel als Markdown aus (Struktur siehe `references/stil.md`) und speichere ihn
zusätzlich als Datei im Arbeitsverzeichnis, z. B. `bericht_<liga>_<wettkampftag>.md`.
Darunter, klar abgesetzt, eine kurze **Faktenprüfung für die Redaktion**: genannte Zahlen
und Namen mit Herkunft (Runde/Match, Einzelstatistik) sowie offene Punkte (z. B. unbekannte
Ligaregeln).

**Word-Dokument (.docx)** – wenn der Nutzer Word/.docx möchte (oder den Bericht weitergeben
will), nach der Selbstkontrolle aus der Markdown-Datei erzeugen:

```bash
node <SKILL_DIR>/scripts/bericht_docx.js bericht_<...>.md [ausgabe.docx] [--mit-faktenpruefung]
```

- Standard: nur der Artikel (Überschrift, Teaser, Text, Tabellen, Termine) – zur
  Veröffentlichung bzw. Weitergabe. `--mit-faktenpruefung` hängt die Faktenprüfung auf einer
  eigenen Seite an (für die interne Abstimmung).
- A4, Arial, Tabellen mit Kopfzeile; Tabellen und Termin-Zwischenzeilen werden nicht über
  Seitenumbrüche zerrissen. Braucht das npm-Paket `docx` (lokal oder global).
- Vor der Abgabe ins PDF rendern und ansehen (z. B. `soffice --headless --convert-to pdf`,
  `pdftoppm -jpeg`), ob Umbrüche und Tabellen passen.

Selbstkontrolle vor der Ausgabe:
- Stimmt jede Zahl (Satzpunkte, Ringe, Matchpunkte, Schnitte) mit der Skriptausgabe überein?
- Ist Sieger/Verlierer bei jedem genannten Match korrekt (Team A/B nicht vertauscht)?
- Sind Namen korrekt geschrieben und dem richtigen Verein zugeordnet?
- Keine erfundenen Zitate, Wetterberichte oder Zuschauerzahlen?
- Steht „Bogenliga“ (bzw. „Bogensport“) prominent in der Überschrift?
- Werden Einzelleistungen als Statistik und nicht als Einzelwertung dargestellt?
- Passen Aussagen zu Titel, Aufstieg, Relegation, Abstieg und Eintritt zur Regeldatei – und
  fehlen sie, wo die Regeln unbekannt sind?
