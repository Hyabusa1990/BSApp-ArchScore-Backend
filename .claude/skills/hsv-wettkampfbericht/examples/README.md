# Beispieldaten

Fiktive Daten zum Testen des Skills ohne API (Vereinsnamen und Ergebnisse sind erfunden).

```bash
python3 .claude/skills/hsv-wettkampfbericht/scripts/fixture_report.py \
  --raw .claude/skills/hsv-wettkampfbericht/examples/beispiel_rohdaten.json \
  --table .claude/skills/hsv-wettkampfbericht/examples/tabelle_vorher.csv --format md
```

- `beispiel_rohdaten.json`: Format von `--dump-raw` (je Fixture die Antworten von `GET /fixtures/{id}` und `GET /fixtures/{id}/rounds/{n}`)
- `tabelle_vorher.csv`: Tabelle vor dem Wettkampftag
