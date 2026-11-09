#!/usr/bin/env python3
"""Holt die Ergebnisse eines oder mehrerer Fixtures aus der ArchScore-API (Fawkes)
und berechnet daraus die Fakten, die ein Wettkampfbericht braucht.

Nur Python-Standardbibliothek. Ausgabe: JSON (Standard) oder Markdown (--format md).

Datenquellen
  * API:   --api-url + Zugangsdaten (Umgebungsvariablen ARCHSCORE_API_URL,
           ARCHSCORE_EMAIL, ARCHSCORE_PASSWORD oder ARCHSCORE_TOKEN)
  * Datei: --raw <datei.json> mit zuvor per --dump-raw gespeicherten Rohdaten

Optional: --table <datei.json|csv> mit der Tabelle VOR dem Wettkampftag
(Spalten/Felder: team, rank, mp_won, mp_lost, sp_won, sp_lost). Ohne sie gibt es
nur die Tagestabelle; Titel- und Abstiegskampf lassen sich dann nicht bewerten.

Regeln (wie Fawkes RuleSet DEFAULT): 5 Sätze à 6 Pfeile (max. 60 Ringe),
Satzsieg 2 Satzpunkte, Satz-Unentschieden je 1, Matchgewinn ab 6 Satzpunkten
(2 Matchpunkte), 5:5 nach fünf Sätzen = Unentschieden (je 1 Matchpunkt).
Ein Match sind die Scheiben (2k-1, 2k) einer Runde.
"""

from __future__ import annotations

import argparse
import csv
import json
import os
import sys
import urllib.error
import urllib.request
from collections import defaultdict

MAX_SETS = 5
WIN_SET_POINTS = 6
MAX_SET_SCORE = 60


# --------------------------------------------------------------------------- API

class Api:
    def __init__(self, base_url: str, token: str | None, email: str | None, password: str | None):
        self.base_url = base_url.rstrip("/")
        self.token = token
        if not self.token:
            if not (email and password):
                sys.exit("Keine Zugangsdaten: ARCHSCORE_TOKEN oder ARCHSCORE_EMAIL + ARCHSCORE_PASSWORD setzen.")
            self.token = self._request("POST", "/auth/login", {"email": email, "password": password}, auth=False)["accessToken"]

    def _request(self, method: str, path: str, body=None, auth: bool = True):
        data = json.dumps(body).encode() if body is not None else None
        req = urllib.request.Request(self.base_url + path, data=data, method=method)
        req.add_header("Accept", "application/json")
        if data is not None:
            req.add_header("Content-Type", "application/json")
        if auth:
            req.add_header("Authorization", f"Bearer {self.token}")
        try:
            with urllib.request.urlopen(req, timeout=30) as resp:
                return json.loads(resp.read().decode() or "null")
        except urllib.error.HTTPError as e:
            detail = e.read().decode(errors="replace")[:500]
            sys.exit(f"API-Fehler {e.code} bei {method} {path}: {detail}")
        except urllib.error.URLError as e:
            sys.exit(f"API nicht erreichbar ({self.base_url}): {e.reason}")

    def get(self, path: str):
        return self._request("GET", path)

    def fetch_fixture(self, fixture_id: int) -> dict:
        fixture = self.get(f"/fixtures/{fixture_id}")
        phase = self.get(f"/fixtures/{fixture_id}/phase")
        rounds = [self.get(f"/fixtures/{fixture_id}/rounds/{n}") for n in range(1, phase["roundCount"] + 1)]
        return {"fixture": fixture, "rounds": rounds}


# ---------------------------------------------------------------------- Analyse

def evaluate_match(a: dict, b: dict) -> dict:
    """Spielt ein Match Satz für Satz nach dem Satzsystem nach."""
    sp = [0, 0]
    progression = []
    sets = []
    result = None
    for i in range(MAX_SETS):
        sa = a["setScores"][i] if i < len(a["setScores"]) else None
        sb = b["setScores"][i] if i < len(b["setScores"]) else None
        if sa is None or sb is None:
            break
        if sa > sb:
            sp[0] += 2
        elif sb > sa:
            sp[1] += 2
        else:
            sp[0] += 1
            sp[1] += 1
        sets.append((sa, sb))
        progression.append(f"{sp[0]}:{sp[1]}")
        if sp[0] >= WIN_SET_POINTS or sp[1] >= WIN_SET_POINTS:
            result = "A" if sp[0] > sp[1] else "B"
            break
    if result is None and len(sets) == MAX_SETS:
        result = "draw"

    winner = loser = None
    if result == "A":
        winner, loser = a["teamName"], b["teamName"]
    elif result == "B":
        winner, loser = b["teamName"], a["teamName"]

    # Spannungsindikatoren
    comeback = None
    lead_changes = 0
    leader_prev = None
    max_deficit = [0, 0]  # groesster Rueckstand von A bzw. B im Verlauf
    run = [0, 0]
    for sa, sb in sets:
        run[0] += 2 if sa > sb else (1 if sa == sb else 0)
        run[1] += 2 if sb > sa else (1 if sa == sb else 0)
        leader = "A" if run[0] > run[1] else ("B" if run[1] > run[0] else None)
        if leader and leader_prev and leader != leader_prev:
            lead_changes += 1
        if leader:
            leader_prev = leader
        max_deficit[0] = max(max_deficit[0], run[1] - run[0])
        max_deficit[1] = max(max_deficit[1], run[0] - run[1])
    names = (a["teamName"], b["teamName"])
    if result in ("A", "B"):
        w = 0 if result == "A" else 1
        if max_deficit[w] >= 2:
            comeback = {"team": names[w], "max_rueckstand_satzpunkte": max_deficit[w], "ausgang": "Sieg"}
    elif result == "draw":
        side = 0 if max_deficit[0] >= max_deficit[1] else 1
        if max_deficit[side] >= 2:
            comeback = {"team": names[side], "max_rueckstand_satzpunkte": max_deficit[side], "ausgang": "Unentschieden"}

    tied_sets = sum(1 for sa, sb in sets if sa == sb)
    close_sets = sum(1 for sa, sb in sets if abs(sa - sb) <= 1)
    total_a = sum(s[0] for s in sets)
    total_b = sum(s[1] for s in sets)

    return {
        "scheiben": [a["targetNo"], b["targetNo"]],
        "team_a": a["teamName"],
        "team_b": b["teamName"],
        "saetze": [f"{sa}:{sb}" for sa, sb in sets],
        "satzpunkte_verlauf": progression,
        "endstand_satzpunkte": f"{sp[0]}:{sp[1]}",
        "ringe": f"{total_a}:{total_b}",
        "abgeschlossen": result is not None,
        "ergebnis": {"A": "Sieg " + a["teamName"], "B": "Sieg " + b["teamName"], "draw": "Unentschieden", None: "nicht abgeschlossen"}[result],
        "sieger": winner,
        "verlierer": loser,
        "anzahl_saetze": len(sets),
        "entscheidung_im_letzten_satz": result is not None and len(sets) == MAX_SETS,
        "satz_unentschieden": tied_sets,
        "knappe_saetze_max_1_ring": close_sets,
        "fuehrungswechsel": lead_changes,
        "comeback": comeback,
        "glatter_sieg_3_0": result in ("A", "B") and len(sets) == 3 and tied_sets == 0,
        "_raw": {"a": a, "b": b, "sets": sets, "result": result, "sp": sp},
    }


def spannung(m: dict) -> int:
    """Grober Score, um die spannendsten Matches vorzuschlagen."""
    s = 0
    s += 4 if m["entscheidung_im_letzten_satz"] else 0
    s += 3 if m["ergebnis"] == "Unentschieden" else 0
    s += 3 * m["fuehrungswechsel"]
    s += 3 if m["comeback"] else 0
    s += 2 * m["satz_unentschieden"]
    s += m["knappe_saetze_max_1_ring"]
    return s


def load_table(path: str) -> list[dict]:
    if path.lower().endswith(".csv"):
        with open(path, newline="", encoding="utf-8-sig") as f:
            sample = f.readline()
            f.seek(0)
            rows = list(csv.DictReader(f, delimiter=";" if ";" in sample else ","))
    else:
        with open(path, encoding="utf-8") as f:
            rows = json.load(f)
    table = []
    for r in rows:
        table.append({
            "team": str(r["team"]).strip(),
            "rank": int(r.get("rank") or 0),
            "mp_won": int(r.get("mp_won") or 0),
            "mp_lost": int(r.get("mp_lost") or 0),
            "sp_won": int(r.get("sp_won") or 0),
            "sp_lost": int(r.get("sp_lost") or 0),
        })
    return table


def rank(rows: list[dict]) -> list[dict]:
    rows.sort(key=lambda r: (-r["mp_won"], -(r["sp_won"] - r["sp_lost"]), -r["sp_won"], r["team"]))
    prev = None
    for i, r in enumerate(rows, 1):
        key = (r["mp_won"], r["sp_won"] - r["sp_lost"])
        r["rank"] = i if key != prev else rows[i - 2]["rank"]
        prev = key
    return rows


def analyse(raw: list[dict], table_before: list[dict] | None, title_places: int, relegation_places: int) -> dict:
    fixtures_out = []
    all_matches = []
    team_day = defaultdict(lambda: {"team": "", "mp_won": 0, "mp_lost": 0, "sp_won": 0, "sp_lost": 0,
                                    "ringe": 0, "saetze": 0, "siege": 0, "unentschieden": 0, "niederlagen": 0,
                                    "matches": []})
    set_records = []

    for item in raw:
        fx = item["fixture"]
        fixtures_out.append({k: fx.get(k) for k in ("id", "leagueName", "fixtureName", "location", "date")})
        for rnd in item["rounds"]:
            targets = sorted(rnd["targets"], key=lambda t: t["targetNo"])
            by_match = defaultdict(list)
            for t in targets:
                by_match[(t["targetNo"] + 1) // 2].append(t)
            for match_no, pair in sorted(by_match.items()):
                if len(pair) != 2 or not all(p.get("teamName") for p in pair):
                    continue  # Freilos / leere Scheibe
                m = evaluate_match(pair[0], pair[1])
                m["fixture_id"] = fx.get("id")
                m["runde"] = rnd["roundNo"]
                m["match_nr"] = match_no
                m["spannung"] = spannung(m)
                all_matches.append(m)

                res = m["_raw"]["result"]
                for idx, (team, opp) in enumerate(((pair[0], pair[1]), (pair[1], pair[0]))):
                    name = team["teamName"]
                    d = team_day[name]
                    d["team"] = name
                    my_sets = [s[idx] for s in m["_raw"]["sets"]]
                    d["ringe"] += sum(my_sets)
                    d["saetze"] += len(my_sets)
                    for sn, score in enumerate(my_sets, 1):
                        set_records.append({"team": name, "gegner": opp["teamName"], "runde": rnd["roundNo"],
                                            "satz": sn, "ringe": score})
                    if res is None:
                        d["matches"].append(f"R{rnd['roundNo']} vs. {opp['teamName']}: offen")
                        continue
                    my_sp, opp_sp = m["_raw"]["sp"][idx], m["_raw"]["sp"][1 - idx]
                    d["sp_won"] += my_sp
                    d["sp_lost"] += opp_sp
                    if res == "draw":
                        d["mp_won"] += 1; d["mp_lost"] += 1; d["unentschieden"] += 1
                    elif (res == "A") == (idx == 0):
                        d["mp_won"] += 2; d["siege"] += 1
                    else:
                        d["mp_lost"] += 2; d["niederlagen"] += 1
                    d["matches"].append(f"R{rnd['roundNo']} vs. {opp['teamName']}: {my_sp}:{opp_sp}")

    for d in team_day.values():
        d["schnitt_pro_satz"] = round(d["ringe"] / d["saetze"], 2) if d["saetze"] else None
        d["schnitt_pro_pfeil"] = round(d["ringe"] / (d["saetze"] * 6), 2) if d["saetze"] else None

    day_table = rank([dict(v) for v in team_day.values()])

    # Gesamttabelle
    overall = None
    situation = None
    if table_before:
        before = {r["team"].casefold(): r for r in table_before}
        rows = []
        unknown = [t for t in team_day if t.casefold() not in before]
        for r in table_before:
            d = team_day.get(next((t for t in team_day if t.casefold() == r["team"].casefold()), ""), None)
            rows.append({
                "team": r["team"],
                "rang_vorher": r["rank"],
                "mp_won": r["mp_won"] + (d["mp_won"] if d else 0),
                "mp_lost": r["mp_lost"] + (d["mp_lost"] if d else 0),
                "sp_won": r["sp_won"] + (d["sp_won"] if d else 0),
                "sp_lost": r["sp_lost"] + (d["sp_lost"] if d else 0),
            })
        overall = rank(rows)
        for r in overall:
            r["veraenderung"] = (r["rang_vorher"] - r["rank"]) if r["rang_vorher"] else None
        n = len(overall)
        leader_pts = overall[0]["mp_won"]
        safe_line = overall[n - relegation_places - 1]["mp_won"] if n > relegation_places else None
        situation = {
            "spitze": [r["team"] for r in overall if r["rank"] <= title_places],
            "abstand_platz2_zur_spitze": leader_pts - overall[1]["mp_won"] if n > 1 else None,
            "verfolger_innerhalb_4_mp": [r["team"] for r in overall[1:] if leader_pts - r["mp_won"] <= 4],
            "abstiegsplaetze": [r["team"] for r in overall[n - relegation_places:]] if relegation_places else [],
            "punkte_erster_nichtabstiegsplatz": safe_line,
            "gefaehrdet_innerhalb_4_mp": [r["team"] for r in overall[:n - relegation_places]
                                          if safe_line is not None and r["mp_won"] - safe_line <= 4
                                          and r["rank"] > title_places] if relegation_places else [],
            "nicht_in_vortabelle": unknown,
            "hinweis": "Platzierungen fuer Titel/Abstieg sind Annahmen (--title-places/--relegation-places); "
                       "verbleibende Wettkampftage kennt das Skript nicht.",
        }

    # Highlights
    set_records.sort(key=lambda s: -s["ringe"])
    best_set = set_records[0]["ringe"] if set_records else None
    complete = [m for m in all_matches if m["abgeschlossen"]]
    best_match_avg = sorted(
        ({"team": m[f"team_{side}"], "gegner": m["team_b" if side == "a" else "team_a"], "runde": m["runde"],
          "schnitt_pro_satz": round(sum(s[0 if side == "a" else 1] for s in m["_raw"]["sets"]) / len(m["_raw"]["sets"]), 2)}
         for m in complete for side in ("a", "b") if m["_raw"]["sets"]),
        key=lambda x: -x["schnitt_pro_satz"])[:3]
    highlights = {
        "hoechster_satz": [s for s in set_records if s["ringe"] == best_set],
        "perfekte_saetze_60": [s for s in set_records if s["ringe"] == MAX_SET_SCORE],
        "beste_tagesschnitte": [{"team": r["team"], "schnitt_pro_satz": r["schnitt_pro_satz"],
                                 "schnitt_pro_pfeil": r["schnitt_pro_pfeil"]} for r in
                                sorted(day_table, key=lambda r: -(r["schnitt_pro_satz"] or 0))[:3]],
        "beste_matchschnitte": best_match_avg,
        "ungeschlagen": [r["team"] for r in day_table if r["niederlagen"] == 0 and r["siege"] + r["unentschieden"] > 0],
        "ohne_sieg": [r["team"] for r in day_table if r["siege"] == 0 and r["niederlagen"] + r["unentschieden"] > 0],
        "spannendste_matches": [strip(m) for m in sorted(complete, key=lambda m: -m["spannung"])[:3]],
        "glatte_siege_3_0": [f"R{m['runde']}: {m['sieger']} gegen {m['verlierer']}" for m in complete if m["glatter_sieg_3_0"]],
        "comebacks": [f"R{m['runde']}: {m['comeback']['team']} ({m['team_a']} - {m['team_b']} {m['endstand_satzpunkte']}, "
                      f"{m['comeback']['ausgang']} nach bis zu {m['comeback']['max_rueckstand_satzpunkte']} Satzpunkten Rueckstand)"
                      for m in complete if m["comeback"]],
        "unentschieden": [f"R{m['runde']}: {m['team_a']} - {m['team_b']}" for m in complete if m["ergebnis"] == "Unentschieden"],
    }

    return {
        "fixtures": fixtures_out,
        "regeln": "5 Saetze a 6 Pfeile, Satzsieg 2 SP, Satz-Remis 1 SP, Matchsieg ab 6 SP (2 MP), 5:5 = Remis (1 MP)",
        "offene_matches": [f"Fixture {m['fixture_id']} R{m['runde']}: {m['team_a']} - {m['team_b']}" for m in all_matches if not m["abgeschlossen"]],
        "highlights": highlights,
        "tagestabelle": [{k: v for k, v in r.items()} for r in day_table],
        "gesamttabelle": overall,
        "tabellensituation": situation,
        "matches": [strip(m) for m in sorted(all_matches, key=lambda m: (m["fixture_id"] or 0, m["runde"], m["match_nr"]))],
    }


def strip(m: dict) -> dict:
    return {k: v for k, v in m.items() if not k.startswith("_")}


# ---------------------------------------------------------------------- Markdown

def to_markdown(r: dict) -> str:
    out = []
    for f in r["fixtures"]:
        out.append(f"## {f.get('leagueName')} – {f.get('fixtureName')} ({f.get('location')}, {str(f.get('date'))[:10]})")
    if r["offene_matches"]:
        out.append("\n**Achtung, nicht abgeschlossene Matches:** " + "; ".join(r["offene_matches"]))
    out.append("\n### Highlights")
    for k, v in r["highlights"].items():
        out.append(f"- **{k}**: {json.dumps(v, ensure_ascii=False)}")
    out.append("\n### Tagestabelle")
    out.append("| Rang | Team | MP | SP | Ringe/Satz | S-U-N |")
    out.append("|---|---|---|---|---|---|")
    for t in r["tagestabelle"]:
        out.append(f"| {t['rank']} | {t['team']} | {t['mp_won']}:{t['mp_lost']} | {t['sp_won']}:{t['sp_lost']} | "
                   f"{t['schnitt_pro_satz']} | {t['siege']}-{t['unentschieden']}-{t['niederlagen']} |")
    if r["gesamttabelle"]:
        out.append("\n### Gesamttabelle")
        out.append("| Rang | (vorher) | Team | MP | SP |")
        out.append("|---|---|---|---|---|")
        for t in r["gesamttabelle"]:
            out.append(f"| {t['rank']} | {t['rang_vorher']} | {t['team']} | {t['mp_won']}:{t['mp_lost']} | {t['sp_won']}:{t['sp_lost']} |")
        out.append("\n**Tabellensituation:** " + json.dumps(r["tabellensituation"], ensure_ascii=False))
    out.append("\n### Alle Matches")
    for m in r["matches"]:
        out.append(f"- R{m['runde']} {m['team_a']} – {m['team_b']}: {m['endstand_satzpunkte']} "
                   f"(Sätze {', '.join(m['saetze'])}; Verlauf {' → '.join(m['satzpunkte_verlauf'])}; Ringe {m['ringe']})")
    return "\n".join(out)


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("fixture_ids", nargs="*", type=int, help="IDs der Fixtures (z. B. 12 13)")
    p.add_argument("--api-url", default=os.environ.get("ARCHSCORE_API_URL"))
    p.add_argument("--raw", help="Rohdaten aus Datei statt API laden")
    p.add_argument("--dump-raw", help="Rohdaten zusaetzlich in diese Datei schreiben")
    p.add_argument("--table", help="Tabelle vor dem Wettkampftag (JSON oder CSV)")
    p.add_argument("--title-places", type=int, default=1, help="Plaetze, die um den Titel spielen (Standard 1)")
    p.add_argument("--relegation-places", type=int, default=2, help="Abstiegsplaetze (Standard 2, 0 = keine)")
    p.add_argument("--format", choices=["json", "md"], default="json")
    a = p.parse_args()

    if a.raw:
        with open(a.raw, encoding="utf-8") as f:
            raw = json.load(f)
        if a.fixture_ids:
            raw = [x for x in raw if x["fixture"]["id"] in a.fixture_ids]
    else:
        if not a.fixture_ids:
            p.error("Mindestens eine Fixture-ID angeben (oder --raw).")
        if not a.api_url:
            p.error("API-URL fehlt: --api-url oder ARCHSCORE_API_URL setzen.")
        api = Api(a.api_url, os.environ.get("ARCHSCORE_TOKEN"), os.environ.get("ARCHSCORE_EMAIL"), os.environ.get("ARCHSCORE_PASSWORD"))
        raw = [api.fetch_fixture(i) for i in a.fixture_ids]

    if a.dump_raw:
        with open(a.dump_raw, "w", encoding="utf-8") as f:
            json.dump(raw, f, ensure_ascii=False, indent=2)

    result = analyse(raw, load_table(a.table) if a.table else None, a.title_places, a.relegation_places)
    print(to_markdown(result) if a.format == "md" else json.dumps(result, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
