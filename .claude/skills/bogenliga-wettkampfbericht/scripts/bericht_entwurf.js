#!/usr/bin/env node
/*
 * Legt einen Wettkampfbericht (Markdown, Aufbau laut references/stil.md) als Entwurf
 * im News-Bereich von BuLiBow an. Veroeffentlicht wird er dort von Hand.
 *
 *   node bericht_entwurf.js <bericht.md> --fixture <ID> [--fixture <ID> ...] [--api-url <url>] [--dry-run]
 *
 * Der API-Schluessel kommt aus der Umgebungsvariablen BULIBOW_NEWS_API_KEY.
 * Basis-URL: --api-url oder BULIBOW_API_URL (Standard https://api.bogenligen-hessen.org).
 * Die Faktenpruefung (alles ab dem "---" davor) wird nicht uebertragen.
 */
"use strict";

const fs = require("fs");

const DEFAULT_URL = "https://api.bogenligen-hessen.org";
const USAGE = "Aufruf: node bericht_entwurf.js <bericht.md> --fixture <ID> [--fixture <ID> ...] [--api-url <url>] [--dry-run]";

function fail(message) {
  console.error(message);
  process.exit(1);
}

function parseArgs(argv) {
  const args = argv.slice(2);
  const result = { input: null, fixtureIds: [], apiUrl: process.env.BULIBOW_API_URL || DEFAULT_URL, dryRun: false };

  for (let i = 0; i < args.length; i++) {
    if (args[i] === "--fixture") {
      const id = Number(args[++i]);
      if (!Number.isInteger(id) || id <= 0) fail(`Ungueltige Fixture-ID: ${args[i]}`);
      result.fixtureIds.push(id);
    } else if (args[i] === "--api-url") {
      result.apiUrl = args[++i];
    } else if (args[i] === "--dry-run") {
      result.dryRun = true;
    } else if (!result.input) {
      result.input = args[i];
    } else {
      fail(USAGE);
    }
  }

  if (!result.input || result.fixtureIds.length === 0) fail(USAGE);
  return result;
}

function parseArticle(markdown) {
  const lines = markdown.replace(/^﻿/, "").split(/\r?\n/);

  // Faktenpruefung = alles ab der "---"-Zeile, auf die eine Faktenpruefungs-Ueberschrift folgt
  const separator = lines.findIndex((l, i) => l.trim() === "---" && lines.slice(i + 1).some((x) => /Faktenpr/i.test(x)));
  const article = separator >= 0 ? lines.slice(0, separator) : lines;

  const titleIndex = article.findIndex((l) => /^#\s+\S/.test(l));
  if (titleIndex < 0) fail("Keine Ueberschrift (# ...) im Bericht gefunden.");
  const title = article[titleIndex].replace(/^#\s+/, "").trim();

  const teaserIndex = article.findIndex((l, i) => i > titleIndex && l.trim() !== "");
  const teaserMatch = teaserIndex >= 0 ? article[teaserIndex].trim().match(/^\*\*(.+)\*\*$/) : null;
  if (!teaserMatch) fail("Kein fett gesetzter Teaser direkt unter der Ueberschrift gefunden.");

  const bodyMarkdown = article.slice(teaserIndex + 1).join("\n").trim();
  if (!bodyMarkdown) fail("Der Bericht hat keinen Text.");

  return { title, teaser: teaserMatch[1].trim(), bodyMarkdown };
}

async function main() {
  const { input, fixtureIds, apiUrl, dryRun } = parseArgs(process.argv);
  const article = parseArticle(fs.readFileSync(input, "utf8"));

  if (dryRun) {
    console.log(`Titel:   ${article.title}`);
    console.log(`Teaser:  ${article.teaser}`);
    console.log(`Text:    ${article.bodyMarkdown.length} Zeichen`);
    console.log(`Fixtures: ${fixtureIds.join(", ")}`);
    return;
  }

  const apiKey = process.env.BULIBOW_NEWS_API_KEY;
  if (!apiKey) fail("Die Umgebungsvariable BULIBOW_NEWS_API_KEY ist nicht gesetzt.");

  const response = await fetch(`${apiUrl.replace(/\/+$/, "")}/api/news-admin/drafts`, {
    method: "POST",
    headers: { "Content-Type": "application/json", "X-Api-Key": apiKey },
    body: JSON.stringify({ ...article, fixtureIds })
  });

  if (response.status === 401) fail("BuLiBow hat den API-Schluessel abgelehnt (401).");
  if (!response.ok) fail(`BuLiBow hat den Entwurf abgelehnt (${response.status}): ${await response.text()}`);

  const draft = await response.json();
  console.log(`Entwurf angelegt (ID ${draft.id}): ${draft.editUrl}`);
}

main().catch((e) => fail(`Fehler beim Anlegen des Entwurfs: ${e.message}`));
