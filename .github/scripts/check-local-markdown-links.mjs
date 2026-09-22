import { readdir, readFile, stat } from "node:fs/promises";
import path from "node:path";
import process from "node:process";

const root = process.cwd();
const excludedDirectories = new Set([".git", "bin", "node_modules", "obj"]);
const markdownFiles = [];

async function collectMarkdownFiles(directory) {
  for (const entry of await readdir(directory, { withFileTypes: true })) {
    if (entry.isDirectory() && excludedDirectories.has(entry.name)) continue;

    const fullPath = path.join(directory, entry.name);
    if (entry.isDirectory()) {
      await collectMarkdownFiles(fullPath);
    } else if (entry.isFile() && entry.name.toLowerCase().endsWith(".md")) {
      markdownFiles.push(fullPath);
    }
  }
}

function extractTarget(rawTarget) {
  const trimmed = rawTarget.trim();
  if (trimmed.startsWith("<")) {
    const closing = trimmed.indexOf(">");
    return closing >= 0 ? trimmed.slice(1, closing) : trimmed;
  }
  return trimmed.split(/\s+["']/u, 1)[0];
}

async function exists(target) {
  try {
    await stat(target);
    return true;
  } catch {
    return false;
  }
}

await collectMarkdownFiles(root);

const errors = [];
const linkPattern = /!?\[[^\]]*\]\(([^)]+)\)/gu;
for (const file of markdownFiles) {
  const content = await readFile(file, "utf8");
  for (const match of content.matchAll(linkPattern)) {
    let target = extractTarget(match[1]);
    if (!target || target.startsWith("#") || /^[a-z][a-z0-9+.-]*:/iu.test(target)) continue;

    target = target.split("#", 1)[0].split("?", 1)[0];
    try {
      target = decodeURIComponent(target);
    } catch {
      // Keep the original target so an invalid escape is reported as a missing path.
    }

    const resolved = target.startsWith("/")
      ? path.join(root, target.slice(1))
      : path.resolve(path.dirname(file), target);

    if (!(await exists(resolved))) {
      const line = content.slice(0, match.index).split(/\r?\n/u).length;
      errors.push(`${path.relative(root, file)}:${line} -> ${match[1]}`);
    }
  }
}

if (errors.length > 0) {
  console.error("Enlaces Markdown locales inexistentes:");
  for (const error of errors) console.error(`- ${error}`);
  process.exit(1);
}

console.log(`Enlaces Markdown locales válidos en ${markdownFiles.length} archivos.`);
