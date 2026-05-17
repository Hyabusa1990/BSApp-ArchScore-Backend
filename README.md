# ArchScore — Backend

> _[Kurze Beschreibung: Was macht dieses Backend? Welche Rolle spielt es im Gesamtsystem?]_  
> Beispiel: Receives arrow scores from spotters and serves current match state via REST API to the display frontend.

---

## Tech Stack

| Component | Technology |
|---|---|
| Language | C# |
| Framework | _[z.B. ASP.NET Core, Minimal API, …]_ |
| Datenbank | _[z.B. PostgreSQL, SQLite, MSSQL, …]_ |
| Auth | _[z.B. JWT, API Key, …]_ |

---

## Projektstruktur

```
archscore-backend/
├── _[Ordner]_/       # [Beschreibung]
├── _[Ordner]_/       # [Beschreibung]
└── ...
```

---

## API-Übersicht

_[Basis-URL, z.B. `http://localhost:PORT/api`]_

| Method | Endpoint | Beschreibung |
|---|---|---|
| `GET` | `/api/...` | |
| `POST` | `/api/...` | |

### Polling-Hinweis

Das Frontend ruft den aktuellen Match-Zustand im Sekundentakt per `GET` ab.  
Der entsprechende Endpoint sollte daher performant und zustandslos sein — kein WebSocket oder Push-Mechanismus erforderlich.

---

## Konfiguration

_[Wie wird das Backend konfiguriert? appsettings.json, Umgebungsvariablen, beides?]_

### Beispiel `appsettings.json` / `.env`

```
# Datenbankverbindung
CONNECTION_STRING=...

# Port
PORT=...

# CORS — erlaubte Frontend-Origin
CORS_ALLOWED_ORIGINS=http://localhost:5173

# Auth
# ...
```

---

## Getting Started

```bash
# 1. Repository klonen
git clone https://github.com/your-org/archscore-backend.git

# 2. Abhaengigkeiten installieren
# [Befehl einfuegen]

# 3. Konfiguration anlegen
# [Befehl / Datei einfuegen]

# 4. Datenbank migrieren
# [Befehl einfuegen, falls noetig]

# 5. Server starten
# [Befehl einfuegen]
```

_[URL zur API-Doku, falls vorhanden — z.B. Swagger unter `http://localhost:PORT/swagger`]_

---

## Voraussetzungen

- _[z.B. .NET 8 SDK]_
- _[z.B. PostgreSQL 15+]_
- _[weitere Abhaengigkeiten]_