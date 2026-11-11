#!/usr/bin/env node
/*
 * Wandelt einen Wettkampfbericht (Markdown, Aufbau laut references/stil.md) in ein
 * Word-Dokument (.docx) um.
 *
 *   node bericht_docx.js <bericht.md> [<ausgabe.docx>] [--mit-faktenpruefung]
 *
 * Unterstuetzt die im Bericht verwendeten Elemente: # / ## / ### Ueberschriften,
 * Absaetze, **fett** (auch ganze Absaetze, z. B. der Teaser), Aufzaehlungen mit "- ",
 * Markdown-Tabellen und "---" als Trenner. Alles ab dem "---" vor der Faktenpruefung
 * bleibt standardmaessig weg; mit --mit-faktenpruefung kommt sie auf eine eigene Seite.
 *
 * Benoetigt das npm-Paket "docx" (lokal oder global installiert).
 */
"use strict";

const fs = require("fs");
const path = require("path");

function loadDocx() {
  try {
    return require("docx");
  } catch (e) {
    const { execSync } = require("child_process");
    const globalRoot = execSync("npm root -g").toString().trim();
    return require(path.join(globalRoot, "docx"));
  }
}

const {
  Document, Packer, Paragraph, TextRun, HeadingLevel, Table, TableRow, TableCell,
  WidthType, BorderStyle, ShadingType, AlignmentType, LevelFormat, PageBreak,
} = loadDocx();

const FONT = "Arial";
const CONTENT_WIDTH = 9026; // A4 mit 2,54-cm-Raendern, in DXA
const ACCENT = "1F4E79";

function parseArgs(argv) {
  const args = argv.slice(2);
  const withFactCheck = args.includes("--mit-faktenpruefung");
  const files = args.filter((a) => !a.startsWith("--"));
  if (files.length < 1) {
    console.error("Aufruf: node bericht_docx.js <bericht.md> [<ausgabe.docx>] [--mit-faktenpruefung]");
    process.exit(1);
  }
  const input = files[0];
  const output = files[1] || input.replace(/\.md$/i, "") + ".docx";
  return { input, output, withFactCheck };
}

/** "**fett** und normal" -> TextRuns */
function inlineRuns(text, base = {}) {
  const runs = [];
  const parts = text.split(/(\*\*[^*]+\*\*)/g).filter((p) => p !== "");
  for (const part of parts) {
    const bold = part.startsWith("**") && part.endsWith("**");
    runs.push(new TextRun({ text: bold ? part.slice(2, -2) : part, bold: bold || base.bold, font: FONT, size: base.size, color: base.color }));
  }
  return runs;
}

function splitRow(line) {
  return line.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map((c) => c.trim());
}

function buildTable(lines) {
  const rows = lines.filter((l) => !/^\|\s*:?-{2,}/.test(l.trim())).map(splitRow);
  const nCols = rows[0].length;
  // Spaltenbreiten nach Textlaenge, mindestens 8 %
  const lens = Array.from({ length: nCols }, (_, i) => Math.max(...rows.map((r) => (r[i] || "").length), 3));
  const total = lens.reduce((a, b) => a + b, 0);
  let widths = lens.map((l) => Math.max(Math.round((l / total) * CONTENT_WIDTH), Math.round(CONTENT_WIDTH * 0.08)));
  const diff = CONTENT_WIDTH - widths.reduce((a, b) => a + b, 0);
  widths[widths.indexOf(Math.max(...widths))] += diff;

  const border = { style: BorderStyle.SINGLE, size: 4, color: "BFBFBF" };
  const borders = { top: border, bottom: border, left: border, right: border };
  const numeric = (s) => /^[\d.,:–-]+$/.test(s);

  return new Table({
    width: { size: CONTENT_WIDTH, type: WidthType.DXA },
    columnWidths: widths,
    rows: rows.map((cells, ri) => new TableRow({
      tableHeader: ri === 0,
      cantSplit: true,
      children: cells.map((c, ci) => new TableCell({
        width: { size: widths[ci], type: WidthType.DXA },
        borders,
        shading: ri === 0 ? { fill: ACCENT, type: ShadingType.CLEAR, color: "auto" }
          : (ri % 2 === 0 ? { fill: "F2F2F2", type: ShadingType.CLEAR, color: "auto" } : undefined),
        margins: { top: 60, bottom: 60, left: 100, right: 100 },
        children: [new Paragraph({
          keepNext: ri < rows.length - 1, // Tabelle moeglichst auf einer Seite halten
          alignment: ri > 0 && numeric(c) ? AlignmentType.CENTER : AlignmentType.LEFT,
          children: inlineRuns(c, ri === 0 ? { bold: true, color: "FFFFFF", size: 20 } : { size: 20 }),
        })],
      })),
    })),
  });
}

function convert(markdown, withFactCheck) {
  let lines = markdown.replace(/\r\n/g, "\n").split("\n");
  // Faktenpruefung = alles ab der letzten "---"-Zeile, auf die eine Faktenpruefungs-Ueberschrift folgt
  const sep = lines.findIndex((l, i) => l.trim() === "---" && lines.slice(i + 1).some((x) => /Faktenpr/i.test(x)));
  let factCheck = [];
  if (sep >= 0) {
    factCheck = lines.slice(sep + 1);
    lines = lines.slice(0, sep);
  }

  const children = [];
  const render = (src) => {
    for (let i = 0; i < src.length; i++) {
      const line = src[i];
      const t = line.trim();
      if (!t) continue;
      if (t.startsWith("|")) {
        const block = [];
        while (i < src.length && src[i].trim().startsWith("|")) block.push(src[i++]);
        i--;
        children.push(buildTable(block));
        children.push(new Paragraph({ spacing: { after: 120 }, children: [] }));
        continue;
      }
      let m;
      if ((m = t.match(/^(#{1,3})\s+(.*)$/))) {
        const level = m[1].length;
        const heading = [HeadingLevel.TITLE, HeadingLevel.HEADING_1, HeadingLevel.HEADING_2][level - 1];
        children.push(new Paragraph({ heading, children: [new TextRun({ text: m[2].replace(/\*\*/g, ""), font: FONT })] }));
        continue;
      }
      if (t.startsWith("- ")) {
        const level = /^\s{2,}- /.test(line) ? 1 : 0; // eingerueckte Unterpunkte
        children.push(new Paragraph({ numbering: { reference: "bullets", level }, spacing: { after: 60 },
          keepNext: level === 0 && i + 1 < src.length && /^\s{2,}- /.test(src[i + 1]), children: inlineRuns(t.slice(2)) }));
        continue;
      }
      if (t === "---") {
        children.push(new Paragraph({ border: { bottom: { style: BorderStyle.SINGLE, size: 6, color: "BFBFBF", space: 1 } }, children: [] }));
        continue;
      }
      // Absatz: aufeinanderfolgende Textzeilen zusammenfassen
      const para = [t];
      while (i + 1 < src.length && src[i + 1].trim() && !/^(#|\||- |---)/.test(src[i + 1].trim())) para.push(src[++i].trim());
      const text = para.join(" ");
      const isTeaser = /^\*\*[^*]+\*\*$/.test(text);
      children.push(new Paragraph({
        // fette Zwischenzeilen und Einleitungen mit Doppelpunkt beim Folgenden halten
        keepNext: isTeaser || text.endsWith(":"),
        spacing: { after: 160, line: 300 },
        children: isTeaser ? [new TextRun({ text: text.slice(2, -2), bold: true, font: FONT, size: 24 })] : inlineRuns(text),
      }));
    }
  };

  render(lines);
  if (withFactCheck && factCheck.length) {
    children.push(new Paragraph({ children: [new PageBreak()] }));
    render(factCheck);
  }
  return children;
}

async function main() {
  const { input, output, withFactCheck } = parseArgs(process.argv);
  const markdown = fs.readFileSync(input, "utf8");
  const title = (markdown.match(/^#\s+(.*)$/m) || [, "Wettkampfbericht"])[1];

  const doc = new Document({
    creator: "Bogenliga-Wettkampfbericht",
    title,
    styles: {
      default: { document: { run: { font: FONT, size: 22 } } },
      paragraphStyles: [
        { id: "Title", name: "Title", basedOn: "Normal", next: "Normal", quickFormat: true,
          run: { font: FONT, size: 36, bold: true, color: ACCENT },
          paragraph: { spacing: { before: 0, after: 240 }, keepNext: true } },
        { id: "Heading1", name: "Heading 1", basedOn: "Normal", next: "Normal", quickFormat: true,
          run: { font: FONT, size: 28, bold: true, color: ACCENT },
          paragraph: { spacing: { before: 300, after: 120 }, keepNext: true, outlineLevel: 0 } },
        { id: "Heading2", name: "Heading 2", basedOn: "Normal", next: "Normal", quickFormat: true,
          run: { font: FONT, size: 24, bold: true, color: ACCENT },
          paragraph: { spacing: { before: 240, after: 100 }, keepNext: true, outlineLevel: 1 } },
      ],
    },
    numbering: {
      config: [{ reference: "bullets", levels: [
        { level: 0, format: LevelFormat.BULLET, text: "•", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 720, hanging: 360 } } } },
        { level: 1, format: LevelFormat.BULLET, text: "–", alignment: AlignmentType.LEFT,
          style: { paragraph: { indent: { left: 1440, hanging: 360 } } } }] }],
    },
    sections: [{
      properties: { page: { size: { width: 11906, height: 16838 }, margin: { top: 1440, right: 1440, bottom: 1440, left: 1440 } } },
      children: convert(markdown, withFactCheck),
    }],
  });

  fs.writeFileSync(output, await Packer.toBuffer(doc));
  console.log(output);
}

main().catch((e) => { console.error(e); process.exit(1); });
