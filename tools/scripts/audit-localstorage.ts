#!/usr/bin/env npx tsx
/**
 * B-CD2 (localStorage conversion guard): scan the CD bundle JSX for `localStorage` usage
 * and `useState` initializers that read/write `localStorage` (pseudo-routing) before a
 * `jsx → tsx` port. Read-only: does not modify CD files.
 *
 * @packageDocumentation
 */
import * as fs from 'node:fs';
import * as path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..');
const DEFAULT_SRC = path.join(REPO_ROOT, 'web/design-refs/step-8-console/src');
const DEFAULT_OUT = path.join(
  REPO_ROOT,
  'web/design-refs/step-8-console/.localstorage-audit.md',
);

/** `useState` (or CD aliases uS / useS) on same line as `localStorage` — bundle pseudo-routing. */
function isPseudoRouteLine(line: string): boolean {
  if (!line.includes('localStorage')) return false;
  return /\b(useState|uS|useS)\b/.test(line);
}

type Hit = { line: number; text: string; kind: 'localStorage' | 'pseudo_route' };

function listJsxFiles(dir: string): string[] {
  if (!fs.existsSync(dir)) return [];
  return fs
    .readdirSync(dir)
    .filter((f) => f.endsWith('.jsx'))
    .map((f) => path.join(dir, f))
    .sort();
}

function scanFile(filePath: string): Hit[] {
  const raw = fs.readFileSync(filePath, 'utf8');
  const lines = raw.split(/\r?\n/);
  const hits: Hit[] = [];
  lines.forEach((line, i) => {
    const n = i + 1;
    if (!line.includes('localStorage')) return;
    const kind: Hit['kind'] = isPseudoRouteLine(line) ? 'pseudo_route' : 'localStorage';
    hits.push({ line: n, text: line.trim(), kind });
  });
  // De-dupe same line+kind
  const key = (h: Hit) => `${h.line}:${h.kind}`;
  const seen = new Set<string>();
  return hits.filter((h) => {
    const k = key(h);
    if (seen.has(k)) return false;
    seen.add(k);
    return true;
  });
}

function renderReport(files: { path: string; rel: string; hits: Hit[] }[]): string {
  const lines: string[] = [
    '# CD bundle — localStorage + pseudo-routing audit (B-CD2)',
    '',
    `Generated: ${new Date().toISOString()}`,
    '',
    '## Summary',
    '',
  ];
  const total = files.reduce((a, f) => a + f.hits.length, 0);
  lines.push(
    `- Files scanned: **${files.length}**`,
    `- Findings: **${total}** (localStorage references + useState-backed routing hints)`,
    '',
  );

  for (const f of files) {
    lines.push(`## \`${f.rel}\``, '');
    if (f.hits.length === 0) {
      lines.push('_No `localStorage` / pseudo-routing pattern hits._', '');
      continue;
    }
    lines.push('| line | kind | context |', '| ---: | --- | --- |');
    for (const h of f.hits) {
      const ctx = h.text.replace(/\|/g, '\\|').slice(0, 200);
      lines.push(`| ${h.line} | \`${h.kind}\` | \`${ctx}\` |`);
    }
    lines.push('');
  }

  return lines.join('\n');
}

function parseArgs(argv: string[]) {
  let out = DEFAULT_OUT;
  let src = DEFAULT_SRC;
  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--out' && argv[i + 1]) {
      out = path.resolve(argv[++i]);
    } else if (a === '--src' && argv[i + 1]) {
      src = path.resolve(argv[++i]);
    } else if (a === '-h' || a === '--help') {
      process.stdout.write(`usage: npx tsx tools/scripts/audit-localstorage.ts [--src DIR] [--out FILE]

  Default src: web/design-refs/step-8-console/src
  Default out: web/design-refs/step-8-console/.localstorage-audit.md
`);
      process.exit(0);
    }
  }
  return { out, src };
}

function main() {
  const { out, src } = parseArgs(process.argv);
  const jsxs = listJsxFiles(src);
  const payload = jsxs.map((p) => {
    const rel = path.relative(REPO_ROOT, p);
    return { path: p, rel, hits: scanFile(p) };
  });
  const md = renderReport(payload);
  fs.mkdirSync(path.dirname(out), { recursive: true });
  fs.writeFileSync(out, md, 'utf8');
  process.stdout.write(`audit-localstorage: wrote ${out}\n`);
}

const entry = process.argv[1];
if (entry && import.meta.url === pathToFileURL(path.resolve(entry)).href) {
  main();
}
