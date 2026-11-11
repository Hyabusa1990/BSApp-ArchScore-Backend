---
name: bogenliga-wettkampfbericht
description: Schreibt aus den Ergebnissen eines oder mehrerer BuLiBow-Fixtures (Wettkampftage von Bogenligen im Satzsystem, z. B. Hessen und Westfalen, API api.bogenligen-hessen.org) einen Wettkampfbericht – oder vor dem Wettkampf eine Vorschau – für die Webseite des zuständigen Landesverbands oder Vereins. Verwenden, wenn der Nutzer Fixture-IDs nennt und einen Artikel, Bericht, Spielbericht, Pressetext, News-Beitrag, Vorbericht oder eine Vorschau zu einem Bogenliga-Wettkampftag, Ligatag, Saisonstart oder Liga-Ergebnis haben möchte – auch wenn nur "schreib was zum Wettkampftag 195" gesagt wird. Kann den Bericht auch als Word-Dokument (.docx) ausgeben oder als Entwurf im News-Bereich von BuLiBow anlegen.
---

# Bogenliga-Wettkampfbericht aus BuLiBow-Fixtures

`<SKILL_DIR>` ist das Verzeichnis dieses Skills (dort, wo diese SKILL.md liegt), z. B.
`.claude/skills/bogenliga-wettkampfbericht` im Repository oder
`~/.claude/skills/bogenliga-wettkampfbericht`.

Ziel: ein veröffentlichungsfertiger deutscher Artikel für die Webseite des zuständigen
Landesverbands (oder eines Vereins). Zwei Artikelarten:

- **Wettkampfbericht** (Wettkampftag bereits geschossen, Abschnitte 1–5): erzählt die
  Highlights – Tagessieger, Top-Schützen, spannende Matchverläufe bis zu einzelnen
  entscheidenden Pfeilen, erstmalige Einsätze, Revanchen, Kampf um Titel/Aufstieg und gegen
  den Abstieg, Dank an den Ausrichter, kommende Termine mit Einladung an Zuschauer.
- **Vorschau** (Wettkampftag steht bevor, Abschnitt 6): macht Lust auf den Wettkampf –
  Ausgangslage, Favoriten, Formkurve; zum Saisonstart Titelverteidiger, Auf- und Absteiger,
  Kaderveränderungen und offene Rechnungen aus der Vorsaison.

Ob Bericht oder Vorschau, ergibt sich aus der Anfrage oder dem Datum: liegt der Wettkampf in
der Zukunft bzw. hat er noch keine Ergebnisse, ist es eine Vorschau. Jede Zahl und jeder Name
im Artikel muss aus den Daten stammen.

**Schnitte – immer so angeben:**
- **Schützen: Ringe pro Pfeil** (z. B. „9,71 Ringe pro Pfeil“) – nie pro Satz.
- **Mannschaften: Ringe pro Satz** (= 6 Pfeile, z. B. „57,71 Ringe pro Satz“) – nie pro Pfeil.

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
   Erzähle den Verlauf mit `satzpunkte_verlauf` und `saetze`. **Einzelne entscheidende Pfeile
   dürfen und sollen vorkommen**: Abschnitt „Spannendste Matches – Satz für Satz“ zeigt die
   Pfeile jedes Satzes je Schütze, `schluesselpfeile` die knappen und den entscheidenden Satz:
   `zehner_satzsieger` (wer im Satz die Zehn traf), `schwaechster_pfeil_satzverlierer` und
   `besserer_pfeil_haette_gereicht_fuer` (eine Zehn statt dieses Pfeils hätte den Ausgleich
   bzw. den Satzgewinn gebracht). Beispiele: „Im Entscheidungssatz trafen Melanie Kratz und
   Wolfgang Roser jeweils die Zehn“, „ein Fehlschuss im letzten Satz kostete den Ausgleich“.
   Die Pfeile sind in BuLiBow je Schütze erfasst, nicht in Schussreihenfolge – also nie
   „der letzte Pfeil“, „zum Schluss“ o. Ä. behaupten; Fehlschüsse (0) und schwache Pfeile
   sachlich und ohne Häme benennen, nicht als Schuldzuweisung.
3. **Top-Schützen** – aus `top_schuetzen` (Ringschnitt pro Pfeil, nur Schützen mit
   genügend Pfeilen) und `meiste_zehner`; ein bis drei Namen mit Verein und Schnitt.
   Wichtig: Es gibt in den Bogenligen **keine offizielle Einzelwertung** – die Zahlen sind
   nur eine Statistik. Sie dürfen gern in den Artikel, aber als Statistik formuliert
   („den besten Ringschnitt des Tages erzielte …“, „statistisch stärkster Schütze“), nie als
   Wertung, Rangliste, Sieg oder Platzierung („gewann die Einzelwertung“, „Platz 1 der
   Einzelrangliste“). Bei Abweichungen gelten die Zahlen der BuLiBow-Statistik.
4. **Weitere Höhepunkte** – `perfekte_saetze_60` (immer erwähnen), `hoechster_satz`,
   beste Mannschaftsschnitte (pro Satz), Comebacks, glatte Siege des Underdogs.
   Ab dem zweiten Wettkampftag zusätzlich:
   - **Erstmalige Einsätze** (`erstmalige_einsaetze`, nur wenn `auswertbar`): Schützen, die in
     dieser Saison zum ersten Mal schossen – erwähnenswert, wenn sie stark trafen oder in
     knappen/entscheidenden Matches dabei waren (`einsaetze` mit Ergebnis). Nicht jeden nennen.
   - **Revanchen** (`revanchen`): Begegnungen, die es an früheren Wettkampftagen schon gab.
     `revanche_gelungen` = der frühere Verlierer gewann diesmal – vor allem bei klaren
     Umkehrungen (0:6 → 7:1) oder Spitzenduellen erwähnen; `erneuter_sieg` nur, wenn es eine
     Serie unterstreicht.
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
- Schützenschnitte pro Pfeil, Mannschaftsschnitte pro Satz?
- Einzelne Pfeile korrekt dem Schützen und Satz zugeordnet, ohne Behauptung zur
  Schussreihenfolge?

## 5. Als Entwurf in BuLiBow anlegen

Nur wenn der Nutzer das möchte (z. B. „leg das in BuLiBow an“, „als News-Entwurf“) und erst
nach bestandener Selbstkontrolle. Der Entwurf wird mit allen Fixture-IDs des Berichts
verknüpft, die Faktenprüfung wird nicht übertragen:

```bash
node <SKILL_DIR>/scripts/bericht_entwurf.js bericht_<...>.md --fixture <ID> [--fixture <ID> ...]
```

- Mit `--dry-run` zuerst prüfen, ob Titel und Teaser richtig erkannt werden. Erwartet wird der
  Aufbau aus `references/stil.md`: `# Titel`, direkt darunter der fett gesetzte Teaser.
- Jeder Aufruf legt einen neuen Entwurf an, also nicht ohne Anlass wiederholen. Soll ein
  Bericht überarbeitet werden, den vorhandenen Entwurf im Editor ändern oder löschen.
- Der API-Schlüssel kommt aus der Umgebungsvariablen `BULIBOW_NEWS_API_KEY`. Fehlt sie, den
  Nutzer darum bitten, sie zu setzen, und den Schlüssel nie ausgeben, im Bericht speichern
  oder in Befehle schreiben. Andere BuLiBow-Instanz: `--api-url` oder `BULIBOW_API_URL`.
- Der Schlüssel kann nur Entwürfe anlegen. Dem Nutzer den ausgegebenen Link zum Editor nennen:
  Fotos und Bildunterschriften ergänzt er dort und veröffentlicht den Beitrag selbst.

## 6. Vorschau auf einen kommenden Wettkampftag

Wenn der Nutzer eine Vorschau/einen Vorbericht möchte oder die Fixtures noch nicht geschossen
sind. Die Abschnitte 1 (Eingaben, Ligaregeln), 4 (Liefern, Word, Selbstkontrolle) und 5
(Entwurf) gelten genauso; die Daten kommen aus dem Vorschau-Skript:

```bash
python3 <SKILL_DIR>/scripts/fixture_preview.py <ID> [<ID> ...] --dump-raw <scratchpad>/vorschau.json
```

Es nutzt zusätzlich `/api/public/seasons` und `/api/public/team/{teamId}` (Kader mit
Einsätzen und Pfeilwerten je Schütze) und ordnet Mannschaften saisonübergreifend über den
Namen zu (Team-IDs wechseln je Saison). Die Ausgabe unterscheidet zwei Fälle:

**Laufende Saison (ab dem 2. Wettkampftag):**
- `tabelle_vor_dem_wettkampftag` mit Mannschaftsschnitt pro Satz, Ausbeute des letzten
  Wettkampftags (`mp_letzter_wettkampftag`) und Rang davor; `tabellensituation` wie im Bericht.
- `top_schuetzen_saison` (Statistik, Ringe pro Pfeil) und `punktverluste_der_top3`
  (wer hat die Spitze schon geschlagen? – offene Rechnungen für Revanchen).
- Geschichte: Ausgangslage an der Spitze und am Tabellenende, Formkurve, wer etwas gutzumachen
  hat, Schützen in Form; bei `letzter_der_saison` die möglichen Entscheidungen (nur was die
  Ligaregeln hergeben – nicht ausrechnen, was rechnerisch noch geht, außer es ist eindeutig).

**Saisonstart (1. Wettkampftag):**
- `abschlusstabelle_vorsaison`, `vorjahresmeister` (und ob er als **Titelverteidiger** wieder
  dabei ist), `mannschaften_status` je Team: `titelverteidiger`, `verbleib`, `aufsteiger`,
  `absteiger`, `wechsel_aus_anderer_liga`, `neu_ohne_vorsaison_in_bulibow` – neue Mannschaften
  (Auf- und Absteiger) immer erwähnen.
- `nicht_mehr_dabei`: Teams der Vorsaison, die fehlen, mit Verbleib (abgestiegen nach …).
  Steht dort „nicht in BuLiBow gefunden“, nur vorsichtig formulieren („ist in dieser Saison
  nicht mehr in der Liga vertreten“) – nicht behaupten, das Team sei aufgestiegen.
- `knappe_entscheidungen_vorsaison`: knappe Titel-, Relegations- oder Abstiegsentscheidungen –
  Mannschaften, die deshalb besonders motiviert sein dürften (als Einschätzung formulieren:
  „dürfte auf Wiedergutmachung brennen“).
- `kaderveraenderungen`: `abgaenge` mit Vorsaisonleistung (Ringe pro Pfeil, Pfeile,
  Einsatztage, `leistungstraeger` = großer Anteil an den Pfeilen oder Top 3 des Teams) – vor
  allem Abgänge von Leistungsträgern erwähnen, ggf. mit neuem Team (`jetzt_bei`);
  `zugaenge` (mit Vorsaisonleistung, wenn sie in derselben Liga für ein anderes Team
  schossen). `schreibvarianten` sind dieselbe Person – weder Ab- noch Zugang.
  `kader_weitgehend_neu` deutet eher auf Umnummerierung der Mannschaften eines Vereins hin –
  dann nicht von einem Aderlass schreiben, sondern neutral oder gar nicht.
  **Ab- und Zugänge sind nur aus der vorläufigen Mannschaftsmeldung in BuLiBow abgeleitet**
  (Kader können sich bis zum Wettkampf noch ändern; wer tatsächlich schießt, zeigt erst der
  Wettkampftag). Das muss im Artikel klar erkennbar sein – z. B. „Laut vorläufiger
  Mannschaftsmeldung fehlen …“, „… stehen nach vorläufiger Meldung nicht mehr im Kader“ – und
  nie als feststehender Abgang („hat den Verein verlassen“, „hört auf“) formuliert werden.

Aufbau und Ton einer Vorschau: siehe „Vorschau“ in `references/stil.md`. Immer mit Termin,
Ausrichter, Halle und Adresse sowie der Einladung an Zuschauer (freier Eintritt laut
`eintritt_frei`). **Eine Vorschau behandelt nur den beschriebenen Wettkampftag**: keine
Liste „Die nächsten Wettkampftage“ und keine späteren Termine im Text – die folgen erst im
Nachbericht. (Beschreibt die Vorschau mehrere Ligen am selben Tag, gehören deren Zeiten an
diesem Tag natürlich dazu.) Dateiname z. B. `vorschau_<liga>_<wettkampftag>.md`.
