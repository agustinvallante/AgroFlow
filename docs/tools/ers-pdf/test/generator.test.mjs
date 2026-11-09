import assert from "node:assert/strict";
import fs from "node:fs/promises";
import os from "node:os";
import path from "node:path";
import test from "node:test";
import { chromium } from "playwright";
import {
  applyNetworkPolicy,
  assertSupportedMarkdown,
  buildPdf,
  parseArguments,
  printCss,
  renderHtml,
} from "../src/generator.mjs";

const temporaryDirectory = () => fs.mkdtemp(path.join(os.tmpdir(), "ers-pdf-test-"));

test("rejects unknown arguments and missing option values", () => {
  assert.throws(() => parseArguments(["--input"]), /Falta un valor/);
  assert.throws(() => parseArguments(["--other", "x"]), /no reconocido/);
});

test("rejects missing input and invalid output without launching a browser", async () => {
  const directory = await temporaryDirectory();
  const output = path.join(directory, "new.pdf");
  await assert.rejects(buildPdf({ input: path.join(directory, "missing.md"), output }), /no existe/);
  const input = path.join(directory, "input.md");
  await fs.writeFile(input, "# Prueba");
  await assert.rejects(buildPdf({ input, output: path.join(directory, "output.txt") }), /extensión .pdf/);
});

test("refuses to overwrite an existing output", async () => {
  const directory = await temporaryDirectory();
  const input = path.join(directory, "input.md");
  const output = path.join(directory, "existing.pdf");
  await fs.writeFile(input, "# Prueba");
  await fs.writeFile(output, "preserve-me");
  await assert.rejects(buildPdf({ input, output }), /no se sobrescribe/);
  assert.equal(await fs.readFile(output, "utf8"), "preserve-me");
});

test("rejects inline and reference-style images instead of silently omitting them", () => {
  assert.throws(() => assertSupportedMarkdown("![remota](https://example.invalid/a.png)"), /imágenes Markdown/);
  assert.throws(() => assertSupportedMarkdown("![referencia][img]\n\n[img]: ./local.png"), /imágenes Markdown/);
});

test("raw HTML is escaped and cannot execute or create active assets", () => {
  const html = renderHtml('<script>alert("x")</script>\n\n<img src="https://example.invalid/a.png">');
  assert.match(html, /&lt;script&gt;/);
  assert.match(html, /&lt;img/);
  assert.doesNotMatch(html, /<script>|<img\s/);
});

test("renders Spanish characters and the visible incomplete-draft label", () => {
  const html = renderHtml("# Requisitos de recepción\n\nDescripción: caña, gestión y operación.");
  assert.match(html, /BORRADOR · ERS INCOMPLETA/);
  assert.match(html, /recepción/);
  assert.match(html, /caña, gestión y operación/);
});

test("configures A4 print layout and page-number placeholders", async () => {
  assert.match(printCss, /@page\s*\{\s*size:\s*A4/);
  const directory = await temporaryDirectory();
  const input = path.join(directory, "input.md");
  const output = path.join(directory, "numbered.pdf");
  await fs.writeFile(input, "# Paginación");
  await buildPdf({ input, output });
  const pdf = await fs.readFile(output);
  assert.ok(pdf.length > 100, "expected a nonempty PDF");
  assert.equal(pdf.subarray(0, 5).toString(), "%PDF-");
});

test("blocks all browser network requests", async () => {
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await browser.newPage();
    await applyNetworkPolicy(page);
    const failures = new Map();
    page.on("requestfailed", (request) => failures.set(request.url(), request.failure()?.errorText));
    await page.goto("https://example.invalid/").catch(() => {});
    assert.equal(failures.get("https://example.invalid/"), "net::ERR_BLOCKED_BY_CLIENT");
  } finally {
    await browser.close();
  }
});
