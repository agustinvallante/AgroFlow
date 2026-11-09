import fs from "node:fs/promises";
import path from "node:path";
import { fileURLToPath } from "node:url";
import MarkdownIt from "markdown-it";
import { chromium } from "playwright";

const packageDirectory = path.dirname(fileURLToPath(import.meta.url));
export const repositoryRoot = path.resolve(packageDirectory, "../../../..");
export const defaultInput = path.join(repositoryRoot, "docs/product/ers/ieee830-ers.md");
export const defaultOutput = path.join(repositoryRoot, "artifacts/ers-tfi-parte2.pdf");

const markdown = new MarkdownIt({ html: false, linkify: false, typographer: false });

export const printCss = `
  @page { size: A4; margin: 22mm 18mm 20mm; }
  html { color: #17212b; font: 11pt/1.5 Arial, "Liberation Sans", sans-serif; }
  body { margin: 0; }
  h1, h2, h3 { color: #123b52; line-height: 1.2; break-after: avoid; }
  h1 { font-size: 22pt; }
  h2 { margin-top: 1.6em; font-size: 16pt; }
  h3 { margin-top: 1.2em; font-size: 12pt; }
  p, li { orphans: 3; widows: 3; }
  table { width: 100%; border-collapse: collapse; break-inside: avoid; }
  th, td { border: 1px solid #748493; padding: 5px 7px; text-align: left; vertical-align: top; }
  th { background: #e8eff3; }
  blockquote { border-left: 4px solid #b37b18; margin: 1em 0; padding: .3em 1em; background: #fff8e8; }
  code { font-family: "Courier New", monospace; overflow-wrap: anywhere; }
  .draft-warning { border: 2px solid #a42323; color: #8d1717; padding: 9px 12px; font-weight: bold; }
`;

export function parseArguments(args) {
  let input;
  let output;
  for (let index = 0; index < args.length; index += 1) {
    const argument = args[index];
    if (argument === "--input" || argument === "--output") {
      const value = args[index + 1];
      if (!value || value.startsWith("--")) throw new Error(`Falta un valor para ${argument}.`);
      if (argument === "--input") input = value;
      else output = value;
      index += 1;
    } else {
      throw new Error(`Argumento no reconocido: ${argument}`);
    }
  }
  return { input, output };
}

export function assertSupportedMarkdown(source) {
  if (/!\[[^\]]*\]/.test(source)) {
    throw new Error("Las imágenes Markdown no están admitidas; no se omiten ni se descargan recursos externos.");
  }
  return source;
}

export async function applyNetworkPolicy(page) {
  await page.route("**/*", (route) => route.abort("blockedbyclient"));
}

export function renderHtml(source) {
  assertSupportedMarkdown(source);
  const body = markdown.render(source);
  return `<!doctype html>
<html lang="es"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1">
<style>${printCss}</style></head><body><div class="draft-warning">BORRADOR · ERS INCOMPLETA</div>
<main>${body}</main></body></html>`;
}

async function resolveInput(value) {
  const candidate = path.resolve(process.cwd(), value);
  const stat = await fs.stat(candidate).catch(() => null);
  if (!stat?.isFile()) throw new Error(`El archivo de entrada no existe o no es un archivo: ${candidate}`);
  return candidate;
}

async function resolveOutput(value) {
  const candidate = path.resolve(process.cwd(), value);
  if (path.extname(candidate).toLowerCase() !== ".pdf") throw new Error("La salida debe tener extensión .pdf.");
  if (await fs.stat(candidate).then(() => true, () => false)) {
    throw new Error(`La salida ya existe; no se sobrescribe: ${candidate}`);
  }
  return candidate;
}

export async function buildPdf({ input = defaultInput, output = defaultOutput, browserType = chromium } = {}) {
  const inputPath = await resolveInput(input);
  const outputPath = await resolveOutput(output);
  const source = await fs.readFile(inputPath, "utf8");
  const html = renderHtml(source);
  await fs.mkdir(path.dirname(outputPath), { recursive: true });

  let browser;
  try {
    browser = await browserType.launch({ headless: true });
    const page = await browser.newPage({ locale: "es-AR" });
    await applyNetworkPolicy(page);
    await page.setContent(html, { waitUntil: "load" });
    await page.pdf({
      path: outputPath,
      format: "A4",
      printBackground: true,
      displayHeaderFooter: true,
      headerTemplate: "<div></div>",
      footerTemplate: '<div style="font-size:8px;width:100%;text-align:center;color:#53616c"><span class="pageNumber"></span> / <span class="totalPages"></span></div>',
      margin: { top: "22mm", right: "18mm", bottom: "20mm", left: "18mm" },
    });
  } finally {
    if (browser) await browser.close();
  }
  return outputPath;
}

export async function main(args = process.argv.slice(2)) {
  const options = parseArguments(args);
  const outputPath = await buildPdf(options);
  console.log(`PDF generado: ${path.relative(process.cwd(), outputPath)}`);
}
