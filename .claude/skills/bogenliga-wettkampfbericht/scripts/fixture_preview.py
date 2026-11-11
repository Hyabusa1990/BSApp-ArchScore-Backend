#!/usr/bin/env python3
"""Sammelt die Fakten fuer eine Vorschau auf einen oder mehrere kommende Wettkampftage
(BuLiBow-Fixtures). Gegenstueck zu fixture_report.py, das bereits geschossene Wettkampftage
auswertet.

Nur Python-Standardbibliothek. Ausgabe: Markdown (Standard) oder JSON (--format json).

Je Liga:
  * Termin, Ausrichter, Halle; Stand in der Saison und alle weiteren Termine
  * Ligaregeln (Meister/Aufstieg/Relegation/Abstieg) laut references/regionen.json
  * ab dem 2. Wettkampftag: Tabelle vor dem Tag, Form des letzten Wettkampftags,
    Saisonstatistik der Schuetzen (Ringe/Pfeil) und Mannschaften (Ringe/Satz),
    Niederlagen der Spitzenteams
  * am 1. Wettkampftag: Abschlusstabelle der Vorsaison, Titelverteidiger, Auf- und Absteiger,
    neue und nicht mehr vertretene Mannschaften, knappe Entscheidungen der Vorsaison,
    Kaderveraenderungen (Ab- und Zugaenge) mit der Vorsaisonleistung der Schuetzen

Zusaetzliche oeffentliche Endpunkte: /api/public/seasons, /api/public/team/{teamId}
(Kader mit Einsaetzen je Fixture und Pfeilwerten je Schuetze).

Mannschaften werden saisonuebergreifend ueber den Namen zugeordnet (Team-IDs wechseln je
Saison). Kader laut BuLiBow-Meldung; Schuetzen ueber den Namen.
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
from collections import defaultdict
from datetime import date
from difflib import SequenceMatcher

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fixture_report as fr  # noqa: E402

WEEKDAYS = ["Montag", "Dienstag", "Mittwoch", "Donnerstag", "Freitag", "Samstag", "Sonntag"]


def norm(name: str | None) -> str:
    return re.sub(r"[\s\-]+", " ", (name or "").casefold()).strip()


def norm_base(name: str | None) -> str:
    """Name ohne angehaengte Mannschaftsnummer ("Homburger SG 2" -> "homburger sg")."""
    return re.sub(r"\s+\d+$", "", norm(name))


def person(name: str | None) -> str:
    """'Nachname, Vorname' -> 'Vorname Nachname'."""
    if name and "," in name:
        last, first = [x.strip() for x in name.split(",", 1)]
        return f"{first} {last}".strip()
    return (name or "").strip()


def bow_class(league: str | None) -> str | None:
    m = re.search(r"(Recurve|Compound|Blankbogen|Barebow)", league or "", re.IGNORECASE)
    return m.group(1).casefold() if m else None


def same_person(a: str, b: str) -> bool:
    """Gleiche Person trotz Schreibvariante (z. B. Herrmann/Hermann, Domenic/Dominik)?"""
    return SequenceMatcher(None, norm(a), norm(b)).ratio() >= 0.85


def weekday(iso: str) -> str:
    return WEEKDAYS[date.fromisoformat(iso[:10]).weekday()]


# --------------------------------------------------------------------------- Daten

class Collector:
    """Holt Daten ueber die API oder aus einem Rohdaten-Dump (Pfad -> Antwort)."""

    def __init__(self, api: fr.Api | None, dump: dict | None = None):
        self.api = api
        self.dump = dump or {}

    def get(self, path: str):
        if path not in self.dump:
            if not self.api:
                sys.exit(f"Rohdaten enthalten {path} nicht.")
            self.dump[path] = self.api.get(path)
        return self.dump[path]

    def calendar(self, season_id: int, league_id: int) -> list[dict]:
        return fr.calendar_fixtures(self.get(f"/api/public/calendar?seasonId={season_id}&leagueId={league_id}"))

    def table(self, fixture_id: int) -> list[dict]:
        return fr.table_rows(self.get(f"/api/public/fixture/{fixture_id}/table"))

    def team(self, team_id: int) -> dict:
        return self.get(f"/api/public/team/{team_id}") or {}


def region_of(seasoninfo: dict, league_id: int) -> dict:
    regions = {r["id"]: r for r in seasoninfo.get("regions") or []}
    league = next((l for l in seasoninfo.get("leagues") or [] if l["id"] == league_id), None)
    region = regions.get(league.get("regionId")) if league else None
    return {"code": region["code"], "name": region["name"]} if region else {"code": None, "name": None}


def league_level(league_name: str, region_code: str | None, rules: dict, supra_pattern: str | None):
    if supra_pattern and re.search(supra_pattern, league_name or "", re.IGNORECASE):
        return 0
    for rule in rules.get(region_code or "", {}).get("ligen") or []:
        if re.search(rule["muster"], league_name or "", re.IGNORECASE):
            return rule.get("ebene")
    return None


def final_table(col: Collector, season_id: int, league_id: int) -> tuple[dict | None, list[dict]]:
    """Letztes Fixture der Liga in der Saison und dessen (kumulative) Tabelle."""
    cal = col.calendar(season_id, league_id)
    if not cal:
        return None, []
    last = cal[-1]
    return last, col.table(last["fixtureId"])


def shooter_stats(team_page: dict, fixture_ids: set[int] | None = None) -> dict[int, dict]:
    """Pfeile, Ringe, Zehner und Einsaetze je Schuetze aus der Team-Seite."""
    stats = {}
    members = {m["id"]: m for m in team_page.get("members") or []}
    for sc in team_page.get("shotCounts") or []:
        if fixture_ids is not None and sc["fixtureId"] not in fixture_ids:
            continue
        mid = sc["teamMemberId"]
        st = stats.setdefault(mid, {"name": person(members.get(mid, {}).get("name")), "team": team_page.get("teamName"),
                                    "pfeile": 0, "ringe": 0, "zehner": 0, "fixtures": set()})
        st["pfeile"] += sc["count"]
        st["ringe"] += sc["score"] * sc["count"]
        st["zehner"] += sc["count"] if sc["score"] == 10 else 0
        st["fixtures"].add(sc["fixtureId"])
    for st in stats.values():
        st["schnitt_pro_pfeil"] = round(st["ringe"] / st["pfeile"], 2) if st["pfeile"] else None
        st["einsatz_tage"] = len(st.pop("fixtures"))
    return stats


# ---------------------------------------------------------------------- Analyse

def preview_league(col: Collector, infos: list[dict], rules: dict, supra: str | None, args) -> dict:
    info = min(infos, key=lambda i: (i["dateTime"], i.get("fixtureNo") or 0))
    season_id, league_id, league = info["seasonId"], info["leagueId"], info["league"]
    seasoninfo = col.get(f"/api/public/seasoninfo?seasonId={season_id}") or {}
    region = region_of(seasoninfo, league_id)
    mode = fr.league_mode(league or "", region["code"], rules, args.title_places, args.relegation_places,
                          args.maybe_relegation_places)
    cal = col.calendar(season_id, league_id)
    own_ids = {i["fixtureId"] for i in infos}
    first_key = (info["dateTime"], info.get("fixtureNo") or 0)
    earlier = [f for f in cal if f["fixtureId"] not in own_ids and (f["dateTime"], f.get("fixtureNo") or 0) < first_key]
    later = [f for f in cal if f["fixtureId"] not in own_ids and f["dateTime"][:10] > max(i["dateTime"][:10] for i in infos)]
    day_dates = sorted({f["dateTime"][:10] for f in cal})
    first_day = not earlier

    # Mannschaften der Liga in dieser Saison (Tabelle des Fixtures listet alle Teams)
    current_rows = col.table(info["fixtureId"])
    if not current_rows and earlier:
        current_rows = col.table(earlier[-1]["fixtureId"])
    current_teams = {r["team_id"]: r["team"] for r in current_rows}

    result = {
        "liga": league,
        "saison": info.get("season"),
        "region": region,
        "verband": mode["verband"],
        "eintritt_frei": mode["eintritt_frei"],
        "ligaregeln_bekannt": mode["modus"] != "unbekannt",
        "ligamodus": mode["modus"],
        "bedeutung_spitzenplaetze": mode["bedeutung"],
        "abstieg_regel": mode["abstieg"],
        "wettkampftag": {
            "nr": sorted({i.get("fixtureNo") for i in infos if i.get("fixtureNo") is not None}),
            "von_wettkaempfen": len(cal),
            "termin_nr": day_dates.index(info["dateTime"][:10]) + 1 if info["dateTime"][:10] in day_dates else None,
            "von_terminen": len(day_dates),
            "erster_der_saison": first_day,
            "letzter_der_saison": not later,
            "termine": [{"fixture_id": i["fixtureId"], "datum": i["dateTime"][:10], "wochentag": weekday(i["dateTime"]),
                         "beginn": i["dateTime"][11:16], "ausrichter": i.get("host"),
                         "halle": i.get("locationAddressLine1"), "strasse": i.get("locationAddressLine2"),
                         "plz": i.get("locationPostcode"), "ort": i.get("locationCity")}
                        for i in sorted(infos, key=lambda i: i["dateTime"])],
        },
        "mannschaften": sorted(current_teams.values()),
        "mannschaften_hinweis": None if current_teams else "Mannschaften noch nicht in BuLiBow eingetragen",
        "weitere_termine": [{"datum": f["dateTime"][:10], "wochentag": weekday(f["dateTime"]), "beginn": f["dateTime"][11:16],
                             "wettkampftag": f.get("fixtureNo"), "ausrichter": f.get("host"), "ort": f.get("locationCity")}
                            for f in later],
    }

    if not first_day:
        result.update(preview_running_season(col, earlier, current_teams, mode, args))
    else:
        result.update(preview_season_start(col, info, region, current_teams, rules, supra, mode, args))
    return result


def situation(rows: list[dict], mode: dict) -> dict:
    n = len(rows)
    if not n:
        return {}
    tp, rp, mp = mode["spitzenplaetze"], mode["sichere_absteiger"], mode["weitere_moegliche_absteiger"]
    return {
        "spitzenplaetze": [r["team"] for r in rows[:tp]] if mode["modus"] != "unbekannt" else [],
        "tabellenfuehrer": rows[0]["team"],
        "abstand_platz2_zur_spitze": rows[0]["mp_won"] - rows[1]["mp_won"] if n > 1 else None,
        "sicherer_abstiegsplatz": [r["team"] for r in rows[n - rp:]] if rp else [],
        "moeglicherweise_abstiegsgefaehrdet": [r["team"] for r in rows[max(tp, n - rp - mp):n - rp]] if rp else [],
    }


def preview_running_season(col: Collector, earlier: list[dict], current_teams: dict, mode: dict, args) -> dict:
    last = earlier[-1]
    table = col.table(last["fixtureId"])
    last_date = last["dateTime"][:10]
    before_last = [f for f in earlier if f["dateTime"][:10] < last_date]
    prev_table = {r["team_id"]: r for r in (col.table(before_last[-1]["fixtureId"]) if before_last else [])}
    for r in table:
        p = prev_table.get(r["team_id"], {"mp_won": 0, "mp_lost": 0, "rank": None})
        r["schnitt_pro_satz"] = round(r["ringe"] / r["saetze"], 2) if r.get("saetze") else None
        r["mp_letzter_wettkampftag"] = f"{r['mp_won'] - p['mp_won']}:{r['mp_lost'] - p['mp_lost']}"
        r["rang_vor_letztem_wettkampftag"] = p["rank"]

    # Saisonstatistik der Schuetzen aus den Team-Seiten (nur bereits geschossene Fixtures)
    done = {f["fixtureId"] for f in earlier}
    shooters = []
    for tid in current_teams:
        shooters += [s for s in shooter_stats(col.team(tid), done).values() if s["pfeile"]]
    max_shots = max((s["pfeile"] for s in shooters), default=0)
    top = sorted((s for s in shooters if s["pfeile"] >= max_shots * args.min_shots_share),
                 key=lambda s: (-s["schnitt_pro_pfeil"], -s["pfeile"]))[:5]

    # Niederlagen und Unentschieden der Spitzenteams in dieser Saison
    top_ids = {r["team_id"] for r in table[:3]}
    setbacks = []
    for f in earlier:
        for rnd in col.get(f"/api/public/fixture/{f['fixtureId']}/rounds") or []:
            for match in rnd.get("matches") or []:
                m = fr.evaluate_match(f["fixtureId"], rnd, match, {})
                if not m or not m["abgeschlossen"]:
                    continue
                for idx, t in enumerate(m["_targets"]):
                    opp = m["_targets"][1 - idx]
                    if t["teamId"] in top_ids and t["matchPoints"] <= opp["matchPoints"]:
                        setbacks.append(f"WT{f.get('fixtureNo')} R{m['runde']}: {t['teamName']} – {opp['teamName']} "
                                        f"{t.get('setPoints')}:{opp.get('setPoints')}")
    return {
        "tabelle_vor_dem_wettkampftag": table,
        "tabellensituation": situation(table, mode),
        "top_schuetzen_saison": top,
        "mindestpfeile_top_schuetzen": int(max_shots * args.min_shots_share),
        "punktverluste_der_top3": setbacks,
    }


def preview_season_start(col: Collector, info: dict, region: dict, current_teams: dict, rules: dict, supra: str | None,
                         mode: dict, args) -> dict:
    seasons = sorted(col.get("/api/public/seasons") or [], key=lambda s: s["fromDate"])
    idx = next((i for i, s in enumerate(seasons) if s["id"] == info["seasonId"]), None)
    if not idx:
        return {"vorsaison": None, "hinweis_vorsaison": "Keine Vorsaison in BuLiBow gefunden."}
    prev_season = seasons[idx - 1]
    prev_info = col.get(f"/api/public/seasoninfo?seasonId={prev_season['id']}") or {}
    cur_info = col.get(f"/api/public/seasoninfo?seasonId={info['seasonId']}") or {}
    level_here = league_level(info["league"], region["code"], rules, supra)

    # Abschlusstabelle der Liga in der Vorsaison
    _, prev_rows = final_table(col, prev_season["id"], info["leagueId"])
    prev_mode = fr.league_mode(info["league"] or "", region["code"], rules, None, None, None)

    # Wo standen die Mannschaften in der Vorsaison? (alle Ligen der Vorsaison)
    prev_places = {}
    for lg in prev_info.get("leagues") or []:
        _, rows = final_table(col, prev_season["id"], lg["id"])
        reg = region_of(prev_info, lg["id"])
        for r in rows:
            prev_places.setdefault(norm(r["team"]), []).append(
                {"liga": lg["name"], "liga_id": lg["id"], "platz": r["rank"], "von": len(rows), "team_id": r["team_id"],
                 "team": r["team"], "ebene": league_level(lg["name"], reg["code"], rules, supra),
                 "mp": f"{r['mp_won']}:{r['mp_lost']}"})
    # Wo spielen die Vorsaison-Mannschaften jetzt? (alle Ligen der aktuellen Saison)
    cur_places = {}
    for lg in cur_info.get("leagues") or []:
        cal = col.calendar(info["seasonId"], lg["id"])
        if not cal:
            continue
        reg = region_of(cur_info, lg["id"])
        for r in col.table(cal[0]["fixtureId"]):
            cur_places.setdefault(norm(r["team"]), []).append(
                {"liga": lg["name"], "ebene": league_level(lg["name"], reg["code"], rules, supra)})

    my_class = bow_class(info["league"])

    def by_class(hits):
        # gleiche Bogenklasse zuerst, dann Ligen ohne Klasse im Namen, dann andere Klassen
        return sorted(hits, key=lambda h: (0 if bow_class(h["liga"]) == my_class else (1 if bow_class(h["liga"]) is None else 2)))

    def find_prev(name):
        hits = prev_places.get(norm(name))
        if hits:
            return by_class(hits), False
        base = norm_base(name)
        fuzzy = [h for k, v in prev_places.items() if norm_base(k) == base for h in v]
        fuzzy = [h for h in fuzzy if bow_class(h["liga"]) in (my_class, None)]
        return by_class(fuzzy), bool(fuzzy)

    teams = []
    for tid, name in sorted(current_teams.items(), key=lambda kv: kv[1]):
        hits, fuzzy = find_prev(name)
        same = [h for h in hits if h["liga_id"] == info["leagueId"]]
        entry = {"team": name, "team_id": tid, "name_nur_aehnlich": fuzzy}
        if same:
            h = same[0]
            entry.update({"status": "titelverteidiger" if h["platz"] == 1 else "verbleib",
                          "vorsaison": f"Platz {h['platz']} von {h['von']} ({h['mp']} MP)", "vorsaison_team_id": h["team_id"],
                          "vorsaison_team": h["team"]})
        elif hits:
            h = hits[0]
            if h["ebene"] is not None and level_here is not None and h["ebene"] > level_here:
                status = "aufsteiger"
            elif h["ebene"] is not None and level_here is not None and h["ebene"] < level_here:
                status = "absteiger"
            else:
                status = "wechsel_aus_anderer_liga"
            entry.update({"status": status, "vorsaison": f"{h['liga']}: Platz {h['platz']} von {h['von']} ({h['mp']} MP)",
                          "vorsaison_team_id": h["team_id"], "vorsaison_team": h["team"]})
        else:
            entry.update({"status": "neu_ohne_vorsaison_in_bulibow", "vorsaison": None})
        teams.append(entry)

    present = {norm(t["vorsaison_team"]) for t in teams if t.get("vorsaison_team")}
    departed = []
    for r in prev_rows:
        if norm(r["team"]) in present:
            continue
        now = [x for x in cur_places.get(norm(r["team"])) or [] if bow_class(x["liga"]) in (my_class, None)]
        if now:
            x = now[0]
            if x["ebene"] is not None and level_here is not None:
                wohin = "abgestiegen" if x["ebene"] > level_here else ("aufgestiegen" if x["ebene"] < level_here else "gewechselt")
            else:
                wohin = "andere Liga"
            jetzt = f"{wohin}: {x['liga']}"
        else:
            jetzt = ("nicht in BuLiBow gefunden – evtl. in eine nicht in BuLiBow gefuehrte Liga aufgestiegen "
                     "(z. B. ueber die Relegation) oder abgemeldet; nicht als Tatsache schreiben")
        departed.append({"team": r["team"], "vorsaison_platz": r["rank"], "jetzt": jetzt})

    # Knappe Entscheidungen der Vorsaison
    close = []
    n = len(prev_rows)

    def gap(a, b):
        return prev_rows[a]["mp_won"] - prev_rows[b]["mp_won"]

    if n > 1 and gap(0, 1) <= 2:
        close.append({"art": "spitze", "teams": [prev_rows[0]["team"], prev_rows[1]["team"]], "mp_abstand": gap(0, 1),
                      "entschieden_durch_satzpunkte": gap(0, 1) == 0,
                      "mp": [f"{prev_rows[i]['mp_won']}:{prev_rows[i]['mp_lost']}" for i in (0, 1)],
                      "sp_diff": [prev_rows[i]["sp_won"] - prev_rows[i]["sp_lost"] for i in (0, 1)]})
    tp = prev_mode["spitzenplaetze"]
    if prev_mode["modus"] != "unbekannt" and tp > 1 and n > tp and gap(tp - 1, tp) <= 2:
        close.append({"art": "letzter_spitzenplatz", "teams": [prev_rows[tp - 1]["team"], prev_rows[tp]["team"]],
                      "mp_abstand": gap(tp - 1, tp)})
    rp = prev_mode["sichere_absteiger"]
    if prev_mode["modus"] != "unbekannt" and rp and n > rp and gap(n - rp - 1, n - rp) <= 2:
        close.append({"art": "abstieg", "gerettet": prev_rows[n - rp - 1]["team"], "abgestiegen": prev_rows[n - rp]["team"],
                      "mp_abstand": gap(n - rp - 1, n - rp)})

    # Kaderveraenderungen: Vorsaison-Kader (mit Leistung) gegen aktuellen Kader
    prev_league_team_pages = {r["team_id"]: col.team(r["team_id"]) for r in prev_rows}
    prev_shooters_by_name = {}
    for tp_id, page in prev_league_team_pages.items():
        for st in shooter_stats(page).values():
            prev_shooters_by_name.setdefault(norm(st["name"]), []).append(st)

    roster = []
    for t in teams:
        cur_page = col.team(t["team_id"])
        cur_names = {norm(person(m["name"])): person(m["name"]) for m in cur_page.get("members") or []}
        entry = {"team": t["team"], "kader_aktuell": sorted(cur_names.values()), "abgaenge": [], "zugaenge": []}
        if t.get("vorsaison_team_id"):
            prev_page = prev_league_team_pages.get(t["vorsaison_team_id"]) or col.team(t["vorsaison_team_id"])
            stats = shooter_stats(prev_page)
            team_arrows = sum(s["pfeile"] for s in stats.values()) or 1
            ranked = sorted((s for s in stats.values() if s["pfeile"]), key=lambda s: -(s["schnitt_pro_pfeil"] or 0))
            top3 = {norm(s["name"]) for s in ranked[:3]}
            prev_names = {norm(person(m["name"])): person(m["name"]) for m in prev_page.get("members") or []}
            for key_, name in prev_names.items():
                if key_ in cur_names:
                    continue
                st = next((s for s in stats.values() if norm(s["name"]) == key_), None)
                if not st or not st["pfeile"]:
                    continue  # ohne Einsatz in der Vorsaison nicht relevant
                share = st["pfeile"] / team_arrows
                entry["abgaenge"].append({
                    "name": name, "pfeile_vorsaison": st["pfeile"], "schnitt_pro_pfeil": st["schnitt_pro_pfeil"],
                    "einsatz_tage": st["einsatz_tage"], "anteil_pfeile_team": round(share, 2),
                    "leistungstraeger": share >= 0.25 or key_ in top3,
                    "jetzt_bei": [x for x in (cur_places_by_shooter(col, key_, teams) or [])]})
            for key_, name in cur_names.items():
                if key_ in prev_names:
                    continue
                other = [s for s in prev_shooters_by_name.get(key_, []) if norm(s["team"]) != norm(t.get("vorsaison_team"))]
                entry["zugaenge"].append({"name": name, "vorsaison_in_dieser_liga_bei": [
                    {"team": s["team"], "pfeile": s["pfeile"], "schnitt_pro_pfeil": s["schnitt_pro_pfeil"]} for s in other]})
        # Schreibvarianten: Abgang und Zugang derselben Person zusammenfuehren
        for ab in list(entry["abgaenge"]):
            zu = next((z for z in entry["zugaenge"] if same_person(z["name"], ab["name"])), None)
            if zu:
                entry["abgaenge"].remove(ab)
                entry["zugaenge"].remove(zu)
                entry.setdefault("schreibvarianten", []).append(f"{ab['name']} / {zu['name']} (vermutlich dieselbe Person)")
        regulars = [a for a in entry["abgaenge"] if a["leistungstraeger"]]
        entry["kader_weitgehend_neu"] = len(regulars) >= 3
        roster.append(entry)

    champion = prev_rows[0]["team"] if prev_rows else None
    return {
        "vorsaison": prev_season["name"],
        "vorjahresmeister": {"team": champion,
                             "wieder_dabei": any(t["status"] == "titelverteidiger" for t in teams)} if champion else None,
        "abschlusstabelle_vorsaison": [{k: r[k] for k in ("rank", "team", "mp_won", "mp_lost", "sp_won", "sp_lost")} |
                                       {"schnitt_pro_satz": round(r["ringe"] / r["saetze"], 2) if r.get("saetze") else None}
                                       for r in prev_rows],
        "mannschaften_status": teams,
        "nicht_mehr_dabei": departed,
        "knappe_entscheidungen_vorsaison": close,
        "kaderveraenderungen": roster,
        "hinweis_kader": "Kader laut BuLiBow-Meldung; Zugaenge koennen zum Saisonstart noch fehlen. Schuetzen werden ueber den Namen zugeordnet.",
    }


def cur_places_by_shooter(col: Collector, name_key: str, teams: list[dict]) -> list[str]:
    """Spielt ein Abgang jetzt bei einer anderen Mannschaft dieser Liga?"""
    hits = []
    for t in teams:
        for m in col.team(t["team_id"]).get("members") or []:
            if norm(person(m["name"])) == name_key:
                hits.append(t["team"])
    return hits


# ---------------------------------------------------------------------- Markdown

def to_markdown(previews: list[dict]) -> str:
    j = fr.j
    out = []
    for p in previews:
        wt = p["wettkampftag"]
        out.append(f"# Vorschau {p['liga']} {p['saison']} – Wettkampf {', '.join(map(str, wt['nr']))} von {wt['von_wettkaempfen']} "
                   f"(Termin {wt['termin_nr']} von {wt['von_terminen']})")
        reg = p["region"]
        out.append(f"Region: {reg['name'] or 'unbekannt'} ({reg['code'] or '–'}) | Verband: {p['verband'] or 'unbekannt'} | "
                   f"Eintritt frei: {'ja' if p['eintritt_frei'] else ('nein' if p['eintritt_frei'] is False else 'unbekannt')} | "
                   f"Ligaregeln bekannt: {'ja' if p['ligaregeln_bekannt'] else 'NEIN – keine Aussagen zu Meister/Auf-/Abstieg'}")
        out.append(f"Ligamodus: {p['ligamodus']} – {p['bedeutung_spitzenplaetze']}; Abstieg: {p['abstieg_regel']}")
        out.append(f"Erster Wettkampftag der Saison: {wt['erster_der_saison']} | letzter: {wt['letzter_der_saison']}")
        for t in wt["termine"]:
            out.append(f"- Termin: {t['wochentag']}, {t['datum']}, ab {t['beginn']} Uhr – Ausrichter {t['ausrichter'] or '?'} – "
                       f"{', '.join(x for x in (t['halle'], t['strasse'], f'{t['plz'] or ''} {t['ort'] or ''}'.strip()) if x)}")
        out.append(f"Mannschaften ({len(p['mannschaften'])}): {', '.join(p['mannschaften']) or p['mannschaften_hinweis']}")
        out.append("Weitere Termine: " + ("; ".join(f"{t['wochentag']} {t['datum']} {t['beginn']} WT{t['wettkampftag']} "
                                                  f"{t['ausrichter'] or '?'} ({t['ort']})" for t in p["weitere_termine"]) or "keine"))
        if not wt["erster_der_saison"]:
            out.append("\n## Tabelle vor dem Wettkampftag (Mannschaftsschnitt pro Satz)")
            out.append("| Rang | Team | MP | SP | Ringe/Satz | letzter WT (MP) | Rang davor |")
            out.append("|---|---|---|---|---|---|---|")
            for r in p["tabelle_vor_dem_wettkampftag"]:
                out.append(f"| {r['rank']} | {r['team']} | {r['mp_won']}:{r['mp_lost']} | {r['sp_won']}:{r['sp_lost']} | "
                           f"{r['schnitt_pro_satz']} | {r['mp_letzter_wettkampftag']} | {r['rang_vor_letztem_wettkampftag'] or '–'} |")
            out.append(f"\n**Tabellensituation:** {j(p['tabellensituation'])}")
            out.append(f"\n## Top-Schuetzen der Saison (Statistik, Ringe pro Pfeil, mind. {p['mindestpfeile_top_schuetzen']} Pfeile)")
            for s in p["top_schuetzen_saison"]:
                out.append(f"- {s['name']} ({s['team']}): {s['schnitt_pro_pfeil']} Ringe/Pfeil aus {s['pfeile']} Pfeilen, "
                           f"{s['zehner']} Zehner, {s['einsatz_tage']} Wettkampftag(e)")
            out.append("\n## Punktverluste der Top 3 in dieser Saison")
            out.extend(f"- {x}" for x in p["punktverluste_der_top3"] or ["keine"])
        else:
            out.append(f"\n## Saisonstart – Vorsaison {p.get('vorsaison')}")
            if not p.get("vorsaison"):
                out.append(p.get("hinweis_vorsaison", ""))
                continue
            out.append("| Rang | Team | MP | SP | Ringe/Satz |")
            out.append("|---|---|---|---|---|")
            for r in p["abschlusstabelle_vorsaison"]:
                out.append(f"| {r['rank']} | {r['team']} | {r['mp_won']}:{r['mp_lost']} | {r['sp_won']}:{r['sp_lost']} | {r['schnitt_pro_satz']} |")
            vm = p.get("vorjahresmeister")
            if vm:
                out.append(f"\nVorjahresmeister: {vm['team']} – {'als Titelverteidiger dabei' if vm['wieder_dabei'] else 'NICHT mehr in dieser Liga'}")
            out.append("\n### Mannschaften dieser Saison")
            for t in p["mannschaften_status"]:
                extra = " (Name nur aehnlich – pruefen)" if t["name_nur_aehnlich"] else ""
                out.append(f"- {t['team']}: {t['status']} – Vorsaison: {t['vorsaison'] or 'nicht in BuLiBow'}{extra}")
            out.append("\n### Nicht mehr in dieser Liga")
            out.extend(f"- {d['team']} (Vorsaison Platz {d['vorsaison_platz']}) – jetzt: {d['jetzt']}"
                       for d in p["nicht_mehr_dabei"]) if p["nicht_mehr_dabei"] else out.append("- keine")
            out.append("\n### Knappe Entscheidungen der Vorsaison")
            out.extend(f"- {j(c)}" for c in p["knappe_entscheidungen_vorsaison"]) if p["knappe_entscheidungen_vorsaison"] \
                else out.append("- keine")
            out.append(f"\n### Kaderveraenderungen ({p['hinweis_kader']})")
            for r in p["kaderveraenderungen"]:
                ab = "; ".join(f"{a['name']} ({a['schnitt_pro_pfeil']} Ringe/Pfeil, {a['pfeile']} Pfeile, {a['einsatz_tage']} WT"
                               f"{', LEISTUNGSTRAEGER' if a['leistungstraeger'] else ''}"
                               f"{', jetzt bei ' + ', '.join(a['jetzt_bei']) if a['jetzt_bei'] else ''})"
                               for a in [dict(x, pfeile=x['pfeile_vorsaison']) for x in r["abgaenge"]]) or "keine"
                zu = "; ".join(f"{z['name']}" + (f" (Vorsaison bei {', '.join(f'{v['team']} {v['schnitt_pro_pfeil']} Ringe/Pfeil' for v in z['vorsaison_in_dieser_liga_bei'])})"
                                                  if z["vorsaison_in_dieser_liga_bei"] else "") for z in r["zugaenge"]) or "keine"
                sv = f" | Schreibvarianten: {'; '.join(r['schreibvarianten'])}" if r.get("schreibvarianten") else ""
                neu = " | KADER WEITGEHEND NEU (evtl. Umbenennung/Umnummerierung der Mannschaften – vorsichtig formulieren)" \
                    if r["kader_weitgehend_neu"] else ""
                out.append(f"- **{r['team']}** – Abgaenge: {ab} | Zugaenge: {zu}{sv}{neu}")
        out.append("")
    return "\n".join(out)


def main():
    p = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    p.add_argument("fixture_ids", nargs="*", type=int, help="IDs der kommenden Fixtures")
    p.add_argument("--api-url", default=os.environ.get("BULIBOW_API_URL", fr.DEFAULT_URL))
    p.add_argument("--raw", help="Rohdaten aus Datei statt API laden (Format von --dump-raw)")
    p.add_argument("--dump-raw", help="Rohdaten zusaetzlich in diese Datei schreiben")
    p.add_argument("--regeln", default=fr.DEFAULT_RULES, help="Regeldatei je Region")
    p.add_argument("--title-places", type=int, default=None)
    p.add_argument("--relegation-places", type=int, default=None)
    p.add_argument("--maybe-relegation-places", type=int, default=None)
    p.add_argument("--min-shots-share", type=float, default=0.5)
    p.add_argument("--format", choices=["json", "md"], default="md")
    a = p.parse_args()

    dump = None
    if a.raw:
        with open(a.raw, encoding="utf-8") as f:
            dump = json.load(f)
    col = Collector(None if a.raw else fr.Api(a.api_url), dump)
    ids = a.fixture_ids or sorted({int(m.group(1)) for k in col.dump for m in [re.match(r"/api/public/fixture/(\d+)/info$", k)] if m})
    if not ids:
        p.error("Mindestens eine Fixture-ID angeben.")

    with open(a.regeln, encoding="utf-8") as f:
        supra = json.load(f).get("_ueberregional_muster")
    rules = fr.load_rules(a.regeln)

    infos = [col.get(f"/api/public/fixture/{i}/info") for i in ids]
    by_league = defaultdict(list)
    for i in infos:
        by_league[(i["seasonId"], i["leagueId"])].append(i)
    previews = [preview_league(col, group, rules, supra, a) for group in by_league.values()]

    if a.dump_raw:
        with open(a.dump_raw, "w", encoding="utf-8") as f:
            json.dump(col.dump, f, ensure_ascii=False)
    print(to_markdown(previews) if a.format == "md" else json.dumps(previews, ensure_ascii=False, indent=2, default=str))


if __name__ == "__main__":
    main()
