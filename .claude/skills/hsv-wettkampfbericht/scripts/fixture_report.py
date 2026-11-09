#!/usr/bin/env python3
"""Holt die Ergebnisse eines oder mehrerer Fixtures (Wettkampftage) aus der
bulibow-API (Bogenligen Hessen) und berechnet die Fakten fuer einen Wettkampfbericht.

Nur Python-Standardbibliothek. Ausgabe: Markdown (--format md) oder JSON.

Benutzte oeffentliche Endpunkte (kein Login noetig):
  GET /api/public/fixture/{id}/info               Liga, Saison, Ausrichter, Ort, Datum
  GET /api/public/fixture/{id}/rounds             Runden > Matches > Scheiben > Saetze > Pfeile
  GET /api/public/fixture/{id}/table              Tabelle NACH diesem Fixture (kumulativ)
  GET /api/public/fixture/{id}/individualranking  Einzelstatistik laut bulibow (keine offizielle Wertung)
  GET /api/public/calendar?seasonId=&leagueId=    alle Fixtures der Liga (Vortabelle, Ausblick)

Mehrere Fixture-IDs derselben Liga (z. B. zwei Durchgaenge an einem Tag) werden zu
einem Wettkampftag zusammengefasst; verschiedene Ligen ergeben je einen eigenen Abschnitt.

Basis-URL: --api-url oder BULIBOW_API_URL (Standard https://api.bogenligen-hessen.org).
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import urllib.error
import urllib.request
from collections import defaultdict

DEFAULT_URL = "https://api.bogenligen-hessen.org"
MAX_SET_SCORE = 60
SHOOT_OFF_SET_NO = 99


# --------------------------------------------------------------------------- API

class Api:
    def __init__(self, base_url: str):
        self.base_url = base_url.rstrip("/")
        self.cache: dict[str, object] = {}

    def get(self, path: str):
        if path in self.cache:
            return self.cache[path]
        req = urllib.request.Request(self.base_url + path, headers={"Accept": "application/json"})
        try:
            with urllib.request.urlopen(req, timeout=60) as resp:
                data = json.loads(resp.read().decode() or "null")
        except urllib.error.HTTPError as e:
            sys.exit(f"API-Fehler {e.code} bei GET {path}: {e.read().decode(errors='replace')[:300]}")
        except urllib.error.URLError as e:
            sys.exit(f"API nicht erreichbar ({self.base_url}): {e.reason}")
        self.cache[path] = data
        return data


def fetch(api: Api, fixture_ids: list[int]) -> dict:
    """Sammelt alle Rohdaten, die die Auswertung braucht."""
    raw = {"fixtures": {}, "calendars": {}, "tables": {}}
    for fid in fixture_ids:
        p = f"/api/public/fixture/{fid}"
        raw["fixtures"][str(fid)] = {
            "info": api.get(p + "/info"),
            "rounds": api.get(p + "/rounds"),
            "individualranking": api.get(p + "/individualranking"),
        }
        raw["tables"][str(fid)] = api.get(p + "/table")
    for fx in raw["fixtures"].values():
        info = fx["info"]
        key = f"{info['seasonId']}-{info['leagueId']}"
        if key not in raw["calendars"]:
            cal = api.get(f"/api/public/calendar?seasonId={info['seasonId']}&leagueId={info['leagueId']}")
            raw["calendars"][key] = cal
    # Vortabelle = Tabelle des letzten Fixtures der Liga vor dem Wettkampftag
    for key, cal in raw["calendars"].items():
        own = [f for f in raw["fixtures"].values() if f"{f['info']['seasonId']}-{f['info']['leagueId']}" == key]
        prev = previous_fixture(cal, own)
        if prev and str(prev["fixtureId"]) not in raw["tables"]:
            raw["tables"][str(prev["fixtureId"])] = api.get(f"/api/public/fixture/{prev['fixtureId']}/table")
    return raw


def calendar_fixtures(cal) -> list[dict]:
    fixtures = cal.get("fixtures") if isinstance(cal, dict) else cal
    return sorted(fixtures or [], key=lambda f: (f["dateTime"], f.get("fixtureNo") or 0, f["fixtureId"]))


def previous_fixture(cal, own: list[dict]):
    own_ids = {f["info"]["fixtureId"] for f in own}
    first = min((f["info"]["dateTime"], f["info"].get("fixtureNo") or 0) for f in own)
    before = [f for f in calendar_fixtures(cal)
              if f["fixtureId"] not in own_ids and (f["dateTime"], f.get("fixtureNo") or 0) < first]
    return before[-1] if before else None


# ---------------------------------------------------------------------- Analyse

def target_sets(t: dict) -> list[dict]:
    return sorted((s for s in (t.get("sets") or []) if s["setNo"] != SHOOT_OFF_SET_NO), key=lambda s: s["setNo"])


def shoot_off(t: dict):
    so = next((s for s in (t.get("sets") or []) if s["setNo"] == SHOOT_OFF_SET_NO), None)
    return so["score"] if so else None


def evaluate_match(fid: int, rnd: dict, match: dict, members: dict) -> dict | None:
    ts = sorted(match.get("targets") or [], key=lambda t: t["targetNo"])
    if len(ts) != 2 or not all(t.get("teamName") for t in ts):
        return None  # Freilos / leere Scheibe
    a, b = ts
    sa_list, sb_list = target_sets(a), target_sets(b)
    sets, progression, sp = [], [], [0, 0]
    run_def = [0, 0]
    lead_changes, leader_prev = 0, None
    for s1, s2 in zip(sa_list, sb_list):
        if s1["score"] is None or s2["score"] is None:
            break
        sets.append((s1["score"], s2["score"]))
        sp[0] += s1["points"] or 0
        sp[1] += s2["points"] or 0
        progression.append(f"{sp[0]}:{sp[1]}")
        leader = 0 if sp[0] > sp[1] else (1 if sp[1] > sp[0] else None)
        if leader is not None and leader_prev is not None and leader != leader_prev:
            lead_changes += 1
        if leader is not None:
            leader_prev = leader
        run_def[0] = max(run_def[0], sp[1] - sp[0])
        run_def[1] = max(run_def[1], sp[0] - sp[1])

    mp = (a.get("matchPoints"), b.get("matchPoints"))
    complete = mp[0] is not None and mp[1] is not None
    names = (a["teamName"], b["teamName"])
    result = None
    if complete:
        result = "draw" if mp[0] == mp[1] else (0 if mp[0] > mp[1] else 1)
    so = (shoot_off(a), shoot_off(b))
    stechen = None
    if so[0] is not None or so[1] is not None:
        stechen = {"pfeile": f"{so[0]}:{so[1]}",
                   "naeher_an_der_mitte": names[0] if a.get("isCloserToCenterInShootOff") else
                   (names[1] if b.get("isCloserToCenterInShootOff") else None)}

    comeback = None
    if result in (0, 1) and run_def[result] >= 2:
        comeback = {"team": names[result], "max_rueckstand_satzpunkte": run_def[result], "ausgang": "Sieg"}
    elif result == "draw":
        side = 0 if run_def[0] >= run_def[1] else 1
        if run_def[side] >= 2:
            comeback = {"team": names[side], "max_rueckstand_satzpunkte": run_def[side], "ausgang": "Unentschieden"}

    def schuetzen(t):
        return [members.get((t["teamId"], p["no"]), f"{p.get('firstNames')} {p.get('surname')}")
                for p in (t.get("participants") or [])]

    tied = sum(1 for x, y in sets if x == y)
    m = {
        "fixture_id": fid,
        "runde": rnd["roundNo"],
        "match_nr": match["matchNo"],
        "team_a": names[0],
        "team_b": names[1],
        "schuetzen_a": schuetzen(a),
        "schuetzen_b": schuetzen(b),
        "saetze": [f"{x}:{y}" for x, y in sets],
        "satzpunkte_verlauf": progression,
        "endstand_satzpunkte": f"{a.get('setPoints')}:{b.get('setPoints')}",
        "matchpunkte": f"{mp[0]}:{mp[1]}" if complete else None,
        "ringe": f"{sum(x for x, _ in sets)}:{sum(y for _, y in sets)}",
        "abgeschlossen": complete,
        "ergebnis": {0: "Sieg " + names[0], 1: "Sieg " + names[1], "draw": "Unentschieden", None: "nicht abgeschlossen"}[result],
        "sieger": names[result] if result in (0, 1) else None,
        "verlierer": names[1 - result] if result in (0, 1) else None,
        "anzahl_saetze": len(sets),
        "stechen": stechen,
        "entscheidung_im_letzten_satz": complete and len(sets) == 5,
        "satz_unentschieden": tied,
        "knappe_saetze_max_1_ring": sum(1 for x, y in sets if abs(x - y) <= 1),
        "fuehrungswechsel": lead_changes,
        "comeback": comeback,
        "glatter_sieg": result in (0, 1) and len(sets) == 3 and tied == 0,
        "_sets": sets,
        "_result": result,
        "_targets": (a, b),
    }
    m["spannung"] = (4 * m["entscheidung_im_letzten_satz"] + 5 * bool(stechen) + 3 * (result == "draw")
                     + 3 * lead_changes + 3 * bool(comeback) + 2 * tied + m["knappe_saetze_max_1_ring"])
    return m


def league_mode(league: str, title_places_override: int | None) -> dict:
    """Bedeutung der Spitzenplaetze je Spielklasse (Bogenligen Hessen).

    Hessenliga = hoechste hessische Spielklasse: Es geht um den Meistertitel, kein Aufstieg;
    in der Hessenliga Recurve qualifizieren sich die ersten beiden fuer die Aufstiegsrelegation
    zur Regionalliga. Darunter (Oberliga, Verbandsliga, Bezirksliga) steigt der Erste auf.
    """
    name = league.casefold()
    if name.startswith("hessenliga") and "recurve" in name:
        # keine feste Anzahl an Absteigern: der Letzte sicher, weitere moeglich
        mode = {"modus": "meisterschaft_mit_relegation", "platz_anzahl": 2, "weitere_moegliche_absteiger": 2,
                "bedeutung": "Platz 1 wird Hessenmeister; Platz 1 und 2 duerfen in der Aufstiegsrelegation zur Regionalliga antreten"}
    elif name.startswith("hessenliga"):
        # Hessenliga Blankbogen/Compound: immer genau ein Absteiger (der Letzte)
        mode = {"modus": "meisterschaft", "platz_anzahl": 1, "weitere_moegliche_absteiger": 0,
                "bedeutung": "Hoechste hessische Spielklasse: Platz 1 wird Hessenmeister, kein Aufstieg; genau ein Absteiger"}
    else:
        mode = {"modus": "aufstieg", "platz_anzahl": 1, "weitere_moegliche_absteiger": 2, "bedeutung": "Platz 1 steigt auf"}
    if title_places_override is not None:
        mode["platz_anzahl"] = title_places_override
    return mode


def table_rows(table) -> list[dict]:
    rows = (table or {}).get("positions") or []
    return [{"rank": p["rank"], "team": p["teamName"], "team_id": p["teamId"],
             "mp_won": p["matchPointsWon"], "mp_lost": p["matchPointsLost"],
             "sp_won": p["setPointsWon"], "sp_lost": p["setPointsLost"],
             "ringe": p.get("totalScore"), "saetze": p.get("setCount")} for p in rows]


def analyse_league(raw: dict, fixtures: list[dict], title_places: int, relegation_places: int,
                   maybe_places: int, min_shots_share: float) -> dict:
    infos = [f["info"] for f in fixtures]
    key = f"{infos[0]['seasonId']}-{infos[0]['leagueId']}"
    cal = calendar_fixtures(raw["calendars"].get(key, {}))

    matches, set_records = [], []
    team_day = defaultdict(lambda: {"team": "", "team_id": 0, "mp_won": 0, "mp_lost": 0, "sp_won": 0, "sp_lost": 0,
                                    "ringe": 0, "saetze": 0, "siege": 0, "unentschieden": 0, "niederlagen": 0,
                                    "matches": []})
    shooters = defaultdict(lambda: {"name": "", "team": "", "ringe": 0, "pfeile": 0, "zehner": 0, "matches": 0})
    shots_total = shots_attributed = 0

    for fx in fixtures:
        fid = fx["info"]["fixtureId"]
        for rnd in sorted(fx["rounds"] or [], key=lambda r: r["roundNo"]):
            for match in sorted(rnd.get("matches") or [], key=lambda m: m["matchNo"]):
                # Namen aller Teammitglieder je Team
                members = {}
                for t in match.get("targets") or []:
                    for p in t.get("allTeamMembers") or []:
                        members[(t["teamId"], p["no"])] = f"{p.get('firstNames') or ''} {p.get('surname') or ''}".strip()
                m = evaluate_match(fid, rnd, match, members)
                if not m:
                    continue
                matches.append(m)
                for idx, t in enumerate(m["_targets"]):
                    opp = m["_targets"][1 - idx]
                    d = team_day[t["teamName"]]
                    d["team"], d["team_id"] = t["teamName"], t["teamId"]
                    my_sets = [s[idx] for s in m["_sets"]]
                    d["ringe"] += sum(my_sets)
                    d["saetze"] += len(my_sets)
                    for sn, score in enumerate(my_sets, 1):
                        set_records.append({"team": t["teamName"], "gegner": opp["teamName"], "runde": m["runde"],
                                            "satz": sn, "ringe": score})
                    # Einzelschuetzen
                    seen = set()
                    for s in target_sets(t):
                        for shot in s.get("shots") or []:
                            if shot.get("score") is None:
                                continue
                            shots_total += 1
                            if shot.get("teamMemberNo") is None:
                                continue
                            shots_attributed += 1
                            k = (t["teamId"], shot["teamMemberNo"])
                            sh = shooters[k]
                            sh["name"] = members.get(k, f"Nr. {shot['teamMemberNo']}")
                            sh["team"] = t["teamName"]
                            sh["ringe"] += shot["score"]
                            sh["pfeile"] += 1
                            sh["zehner"] += shot["score"] == 10
                            seen.add(k)
                    for k in seen:
                        shooters[k]["matches"] += 1
                    if not m["abgeschlossen"]:
                        d["matches"].append(f"R{m['runde']} vs. {opp['teamName']}: offen")
                        continue
                    d["sp_won"] += t.get("setPoints") or 0
                    d["sp_lost"] += opp.get("setPoints") or 0
                    d["mp_won"] += t["matchPoints"]
                    d["mp_lost"] += opp["matchPoints"]
                    if t["matchPoints"] > opp["matchPoints"]:
                        d["siege"] += 1
                    elif t["matchPoints"] < opp["matchPoints"]:
                        d["niederlagen"] += 1
                    else:
                        d["unentschieden"] += 1
                    d["matches"].append(f"R{m['runde']} vs. {opp['teamName']}: {t.get('setPoints')}:{opp.get('setPoints')}")

    for d in team_day.values():
        d["schnitt_pro_satz"] = round(d["ringe"] / d["saetze"], 2) if d["saetze"] else None
        d["schnitt_pro_pfeil"] = round(d["ringe"] / (d["saetze"] * 6), 2) if d["saetze"] else None
    day_table = sorted(team_day.values(), key=lambda r: (-r["mp_won"], -(r["sp_won"] - r["sp_lost"]), -r["sp_won"], r["team"]))
    for i, r in enumerate(day_table, 1):
        r["platz_tag"] = i

    # Einzelschuetzen-Statistik des Tages
    for sh in shooters.values():
        sh["schnitt"] = round(sh["ringe"] / sh["pfeile"], 2) if sh["pfeile"] else None
    max_shots = max((s["pfeile"] for s in shooters.values()), default=0)
    min_shots = int(max_shots * min_shots_share)
    ranked = sorted((s for s in shooters.values() if s["pfeile"] >= min_shots), key=lambda s: (-s["schnitt"], -s["pfeile"]))
    individual = {
        "pfeile_gesamt": shots_total,
        "pfeile_mit_schuetzenzuordnung": shots_attributed,
        "aussagekraeftig": shots_total > 0 and shots_attributed / shots_total >= 0.9,
        "mindestpfeile_fuer_rangliste": min_shots,
        "top_schuetzen": ranked[:5],
        "meiste_zehner": sorted(shooters.values(), key=lambda s: -s["zehner"])[:3],
        "laut_bulibow": [((fx.get("individualranking") or {}).get("individualRankingPositions") or [])[:5]
                         for fx in fixtures],
    }

    # Tabellen: nachher = Tabelle des letzten Fixtures des Tages, vorher = letztes Fixture davor
    last = max(infos, key=lambda i: (i["dateTime"], i.get("fixtureNo") or 0))
    after = table_rows(raw["tables"].get(str(last["fixtureId"])))
    prev = previous_fixture(raw["calendars"].get(key, {}), fixtures)
    before = table_rows(raw["tables"].get(str(prev["fixtureId"]))) if prev else []
    before_by_team = {r["team_id"]: r for r in before}
    erster_wettkampftag = prev is None
    for r in after:
        b = before_by_team.get(r["team_id"])
        r["rang_vorher"] = b["rank"] if b else None
        r["veraenderung"] = (b["rank"] - r["rank"]) if b else None

    # Plausibilitaet: Tageswerte = Tabelle nachher - Tabelle vorher
    abweichungen = []
    for r in after:
        d = next((x for x in day_table if x["team_id"] == r["team_id"]), None)
        b = before_by_team.get(r["team_id"], {"mp_won": 0, "sp_won": 0})
        if d and (r["mp_won"] - b["mp_won"] != d["mp_won"] or r["sp_won"] - b["sp_won"] != d["sp_won"]):
            abweichungen.append(r["team"])

    mode = league_mode(infos[0]["league"] or "", title_places)
    title_places = mode["platz_anzahl"]
    if maybe_places is None:
        maybe_places = mode["weitere_moegliche_absteiger"]
    situation = None
    if after:
        # Absteiger: Der Letzte steigt sicher ab; ob weitere absteigen, steht waehrend der Saison
        # noch nicht fest (haengt u. a. von den Absteigern aus den hoeheren Ligen ab).
        n = len(after)
        leader_pts = after[0]["mp_won"]
        sure = after[n - relegation_places:] if relegation_places else []
        safe = after[n - relegation_places - 1]["mp_won"] if relegation_places and n > relegation_places else None
        maybe = after[max(title_places, n - relegation_places - maybe_places):n - relegation_places] if relegation_places else []
        situation = {
            "ligamodus": mode["modus"],
            "bedeutung_spitzenplaetze": mode["bedeutung"],
            "spitzenplaetze": [r["team"] for r in after[:title_places]],
            "tabellenfuehrer": after[0]["team"],
            "abstand_erster_bis_letzter_spitzenplatz_zum_naechsten": (after[title_places - 1]["mp_won"] - after[title_places]["mp_won"])
                                                                     if n > title_places else None,
            "abstand_platz2_zur_spitze": leader_pts - after[1]["mp_won"] if n > 1 else None,
            "verfolger_innerhalb_4_mp_zur_spitze": [r["team"] for r in after[1:] if leader_pts - r["mp_won"] <= 4],
            "sicherer_abstiegsplatz": [r["team"] for r in sure],
            "punkte_erster_platz_vor_sicherem_abstieg": safe,
            "moeglicherweise_abstiegsgefaehrdet": [r["team"] for r in maybe],
            "innerhalb_4_mp_vor_sicherem_abstieg": [r["team"] for r in after[title_places:n - relegation_places]
                                                    if sure and r["mp_won"] - sure[0]["mp_won"] <= 4],
            "hinweis": (f"{mode['bedeutung']}. Die letzten {relegation_places} steigen sicher ab"
                        + (f"; ob die {len(maybe)} Plaetze davor ebenfalls absteigen, steht waehrend der Saison noch nicht fest."
                           if maybe else "; weitere Absteiger gibt es nicht.")),
        }

    own_ids = {i["fixtureId"] for i in infos}
    later = [f for f in cal if f["fixtureId"] not in own_ids and f["dateTime"] > last["dateTime"]]
    next_day = [f for f in later if f["dateTime"][:10] == later[0]["dateTime"][:10]] if later else []
    remaining_days = len({f["dateTime"][:10] for f in later})
    # Alle kommenden Wettkampftage (je Datum + Ausrichter ein Eintrag) fuer die Terminliste
    upcoming = {}
    for f in later:
        k = (f["dateTime"][:10], f.get("host") or "", f.get("locationCity") or "")
        e = upcoming.setdefault(k, {"datum": f["dateTime"][:10], "beginn": f["dateTime"][11:16],
                                    "wettkampftag_nr": [], "ausrichter": f.get("host"),
                                    "halle": f.get("locationAddressLine1"), "strasse": f.get("locationAddressLine2"),
                                    "plz": f.get("locationPostcode"), "ort": f.get("locationCity"), "fixture_ids": []})
        e["beginn"] = min(e["beginn"], f["dateTime"][11:16])
        e["wettkampftag_nr"].append(f.get("fixtureNo"))
        e["fixture_ids"].append(f["fixtureId"])

    set_records.sort(key=lambda s: -s["ringe"])
    best_set = set_records[0]["ringe"] if set_records else None
    complete = [m for m in matches if m["abgeschlossen"]]
    highlights = {
        "hoechster_satz": [s for s in set_records if s["ringe"] == best_set],
        "perfekte_saetze_60": [s for s in set_records if s["ringe"] == MAX_SET_SCORE],
        "beste_tagesschnitte_teams": [{"team": r["team"], "schnitt_pro_satz": r["schnitt_pro_satz"],
                                       "schnitt_pro_pfeil": r["schnitt_pro_pfeil"]}
                                      for r in sorted(day_table, key=lambda r: -(r["schnitt_pro_satz"] or 0))[:3]],
        "ungeschlagen": [r["team"] for r in day_table if r["niederlagen"] == 0 and r["siege"] + r["unentschieden"] > 0],
        "ohne_sieg": [r["team"] for r in day_table if r["siege"] == 0 and r["niederlagen"] + r["unentschieden"] > 0],
        "spannendste_matches": [strip(m) for m in sorted(complete, key=lambda m: -m["spannung"])[:3]],
        "stechen": [f"R{m['runde']}: {m['team_a']} - {m['team_b']} {m['endstand_satzpunkte']} (Stechen {m['stechen']['pfeile']})"
                    for m in complete if m["stechen"]],
        "glatte_siege_6_0": [f"R{m['runde']}: {m['sieger']} gegen {m['verlierer']}" for m in complete if m["glatter_sieg"]],
        "comebacks": [f"R{m['runde']}: {m['comeback']['team']} ({m['team_a']} - {m['team_b']} {m['endstand_satzpunkte']}, "
                      f"{m['comeback']['ausgang']} nach bis zu {m['comeback']['max_rueckstand_satzpunkte']} Satzpunkten Rueckstand)"
                      for m in complete if m["comeback"]],
        "unentschieden": [f"R{m['runde']}: {m['team_a']} - {m['team_b']}" for m in complete if m["_result"] == "draw"],
    }

    return {
        "liga": infos[0]["league"],
        "saison": infos[0]["season"],
        "fixtures": [{k: i.get(k) for k in ("fixtureId", "fixtureNo", "dateTime", "host", "locationAddressLine1", "locationCity")}
                     for i in sorted(infos, key=lambda i: (i["dateTime"], i.get("fixtureNo") or 0))],
        "erster_wettkampftag_der_saison": erster_wettkampftag,
        "verbleibende_wettkampftage": remaining_days,
        "naechster_wettkampftag": [{k: f.get(k) for k in ("fixtureId", "fixtureNo", "dateTime", "host", "locationCity")} for f in next_day],
        "kommende_wettkampftage": sorted(upcoming.values(), key=lambda e: (e["datum"], e["beginn"])),
        "offene_matches": [f"Fixture {m['fixture_id']} R{m['runde']}: {m['team_a']} - {m['team_b']}" for m in matches if not m["abgeschlossen"]],
        "tabellen_abweichung": abweichungen,
        "highlights": highlights,
        "einzelschuetzen": individual,
        "tagestabelle": day_table,
        "tabelle_nachher": after,
        "tabellensituation": situation,
        "matches": [strip(m) for m in matches],
    }


def strip(m: dict) -> dict:
    return {k: v for k, v in m.items() if not k.startswith("_")}


def analyse(raw: dict, title_places: int, relegation_places: int, maybe_places: int, min_shots_share: float) -> list[dict]:
    by_league = defaultdict(list)
    for fx in raw["fixtures"].values():
        by_league[(fx["info"]["seasonId"], fx["info"]["leagueId"])].append(fx)
    return [analyse_league(raw, fxs, title_places, relegation_places, maybe_places, min_shots_share) for fxs in by_league.values()]


# ---------------------------------------------------------------------- Markdown

def j(v) -> str:
    return json.dumps(v, ensure_ascii=False)


def to_markdown(reports: list[dict]) -> str:
    out = []
    for r in reports:
        f0 = r["fixtures"][0]
        out.append(f"# {r['liga']} {r['saison']} – Wettkampftag {', '.join(str(f['fixtureNo']) for f in r['fixtures'])} "
                   f"({f0['dateTime'][:10]}, {f0['host'] or ''} in {f0['locationCity']})")
        hosts = sorted({f["host"] for f in r["fixtures"] if f.get("host")})
        out.append(f"Ausrichter: {', '.join(hosts) if hosts else 'in bulibow nicht angegeben'} | Halle/Ort: "
                   f"{'; '.join(sorted({(f.get('locationAddressLine1') or '') + ', ' + (f.get('locationCity') or '') for f in r['fixtures']}))}")
        out.append(f"Fixtures: {', '.join(str(f['fixtureId']) for f in r['fixtures'])} | erster Wettkampftag: "
                   f"{r['erster_wettkampftag_der_saison']} | verbleibende Wettkampftage: {r['verbleibende_wettkampftage']} | "
                   f"naechster: {j(r['naechster_wettkampftag'])}")
        out.append("Kommende Wettkampftage:")
        for e in r["kommende_wettkampftage"]:
            nr = ", ".join(str(n) for n in e["wettkampftag_nr"] if n is not None)
            adr = ", ".join(x for x in (e["halle"], e["strasse"], f"{e['plz'] or ''} {e['ort'] or ''}".strip()) if x)
            out.append(f"- {e['datum']} {e['beginn']} Uhr – Wettkampftag {nr} – Ausrichter {e['ausrichter'] or '?'} – {adr}")
        if not r["kommende_wettkampftage"]:
            out.append("- keine (letzter Wettkampftag der Saison)")
        if not any(m["abgeschlossen"] for m in r["matches"]):
            out.append("\n**Achtung: Fuer diesen Wettkampftag liegen noch keine abgeschlossenen Matches vor.**")
        if r["offene_matches"]:
            out.append("\n**Achtung, nicht abgeschlossene Matches:** " + "; ".join(r["offene_matches"]))
        if r["tabellen_abweichung"]:
            out.append("\n**Achtung, Tabelle passt nicht zu den Matchergebnissen bei:** " + ", ".join(r["tabellen_abweichung"]))
        out.append("\n## Highlights")
        for k, v in r["highlights"].items():
            out.append(f"- **{k}**: {j(v)}")
        e = r["einzelschuetzen"]
        out.append(f"\n## Einzelstatistik – keine offizielle Wertung (Pfeile mit Zuordnung: {e['pfeile_mit_schuetzenzuordnung']}/{e['pfeile_gesamt']}, "
                   f"aussagekraeftig: {e['aussagekraeftig']}, Mindestpfeile: {e['mindestpfeile_fuer_rangliste']})")
        if not e["aussagekraeftig"]:
            out.append("- Pfeile sind (fast) keinen Schuetzen zugeordnet: keine Einzelauswertung, im Artikel keine Top-Schuetzen nennen.")
        else:
            for s in e["top_schuetzen"]:
                out.append(f"- {s['name']} ({s['team']}): {s['schnitt']} Ringe/Pfeil, {s['ringe']} Ringe aus {s['pfeile']} Pfeilen, {s['zehner']} Zehner")
            out.append(f"- meiste Zehner: {j([(s['name'], s['team'], s['zehner']) for s in e['meiste_zehner']])}")
            out.append(f"- Einzelstatistik laut bulibow (Top 5 je Fixture): {j(e['laut_bulibow'])}")
            out.append("- Hinweis: keine offizielle Einzelwertung, nur Statistik")
        out.append("\n## Tagestabelle")
        out.append("| Platz | Team | MP | SP | Ringe/Satz | S-U-N |")
        out.append("|---|---|---|---|---|---|")
        for t in r["tagestabelle"]:
            out.append(f"| {t['platz_tag']} | {t['team']} | {t['mp_won']}:{t['mp_lost']} | {t['sp_won']}:{t['sp_lost']} | "
                       f"{t['schnitt_pro_satz']} | {t['siege']}-{t['unentschieden']}-{t['niederlagen']} |")
        out.append("\n## Tabelle nach dem Wettkampftag")
        out.append("| Rang | vorher | Team | MP | SP |")
        out.append("|---|---|---|---|---|")
        for t in r["tabelle_nachher"]:
            out.append(f"| {t['rank']} | {t['rang_vorher'] or '–'} | {t['team']} | {t['mp_won']}:{t['mp_lost']} | {t['sp_won']}:{t['sp_lost']} |")
        out.append(f"\n**Tabellensituation:** {j(r['tabellensituation'])}")
        out.append("\n## Alle Matches")
        for m in r["matches"]:
            extra = f"; Stechen {m['stechen']['pfeile']}" if m["stechen"] else ""
            out.append(f"- F{m['fixture_id']} R{m['runde']} {m['team_a']} – {m['team_b']}: {m['endstand_satzpunkte']} "
                       f"(Saetze {', '.join(m['saetze'])}; Verlauf {' → '.join(m['satzpunkte_verlauf'])}; Ringe {m['ringe']}{extra})"
                       f" | Schuetzen: {', '.join(m['schuetzen_a'])} / {', '.join(m['schuetzen_b'])}")
        out.append("")
    return "\n".join(out)


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("fixture_ids", nargs="*", type=int, help="bulibow-Fixture-IDs (z. B. 220 221)")
    p.add_argument("--api-url", default=os.environ.get("BULIBOW_API_URL", DEFAULT_URL))
    p.add_argument("--raw", help="Rohdaten aus Datei statt API laden (Format von --dump-raw)")
    p.add_argument("--dump-raw", help="Rohdaten zusaetzlich in diese Datei schreiben")
    p.add_argument("--title-places", type=int, default=None,
                   help="Anzahl der Spitzenplaetze (Meister/Relegation/Aufstieg); Standard je nach Liga automatisch")
    p.add_argument("--relegation-places", type=int, default=1, help="sichere Abstiegsplaetze (Standard 1, 0 = keine)")
    p.add_argument("--maybe-relegation-places", type=int, default=None,
                   help="Plaetze vor dem sicheren Abstieg, die ggf. ebenfalls absteigen (Standard je nach Liga: "
                        "Hessenliga Blankbogen/Compound 0, sonst 2)")
    p.add_argument("--min-shots-share", type=float, default=0.5,
                   help="Einzelstatistik: Mindestanteil an Pfeilen des Schuetzen mit den meisten Pfeilen (Standard 0.5)")
    p.add_argument("--format", choices=["json", "md"], default="md")
    a = p.parse_args()

    if a.raw:
        with open(a.raw, encoding="utf-8") as f:
            raw = json.load(f)
        if a.fixture_ids:
            raw["fixtures"] = {k: v for k, v in raw["fixtures"].items() if int(k) in a.fixture_ids}
    else:
        if not a.fixture_ids:
            p.error("Mindestens eine Fixture-ID angeben (oder --raw).")
        raw = fetch(Api(a.api_url), a.fixture_ids)

    if a.dump_raw:
        with open(a.dump_raw, "w", encoding="utf-8") as f:
            json.dump(raw, f, ensure_ascii=False)

    reports = analyse(raw, a.title_places, a.relegation_places, a.maybe_relegation_places, a.min_shots_share)
    print(to_markdown(reports) if a.format == "md" else json.dumps(reports, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
