#!/usr/bin/env npx tsx
/**
 * CD bundle → interactions.json (Stage 12 Step 14 P5.a — locked 2026-04-30).
 *
 * Walks `archetypes.jsx` + `archetypes-extension.jsx` via @babel/parser to
 * recover component name, default-prop bag, and BEM class-root for each
 * interactive archetype (Knob/Fader/VuMeter/ILed/SegRead/Osc/DetentRing/Led).
 * Then scans `tokens.css` + `tokens-extension.css` + `archetypes-extension.css`
 * for every `.{class_root}--{modifier}` selector — bucketed by axis (size /
 * tone / state / orientation / lit). Merges with `interactives.json` +
 * `interactives-extension.json` so each entry carries the full state-styling
 * surface the agent needs to bake `ThemedKnob` / `ThemedButton` / etc. without
 * eyeballing the rendered canvas.
 *
 * Output: `web/design-refs/step-1-game-ui/interactions.json`.
 *
 * Usage:
 *   npx tsx tools/scripts/extract-cd-interactions.ts
 *   npx tsx tools/scripts/extract-cd-interactions.ts --in <bundle-dir> --out <file>
 *
 * @packageDocumentation
 */
import * as fs from 'node:fs';
import * as path from 'node:path';
import * as process from 'node:process';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { parse as babelParse } from '@babel/parser';
import type {
  ArrowFunctionExpression,
  AssignmentPattern,
  FunctionDeclaration,
  Identifier,
  Node,
  ObjectPattern,
  ObjectProperty,
  StringLiteral,
} from '@babel/types';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, '..', '..');

const DEFAULT_BUNDLE_DIR = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/cd-bundle');
const DEFAULT_OUT = path.join(REPO_ROOT, 'web/design-refs/step-1-game-ui/interactions.json');

const JSX_FILES = ['archetypes.jsx', 'archetypes-extension.jsx'];
const CSS_FILES = ['tokens.css', 'tokens-extension.css', 'archetypes-extension.css'];
const INTERACTIVES_FILES = ['interactives.json', 'interactives-extension.json'];

/** Slug ↔ class-root mapping. CD bundle convention is fixed by archetype JSX. */
const SLUG_TO_CLASS_ROOT: Record<string, string> = {
  knob: 'knob',
  fader: 'fader',
  'vu-meter': 'vu',
  'illuminated-button': 'iled',
  'segmented-readout': 'segread',
  oscilloscope: 'osc',
  'detent-ring': 'detent-ring',
  led: 'led',
};

const CLASS_ROOT_TO_COMPONENT: Record<string, string> = {
  knob: 'Knob',
  fader: 'Fader',
  vu: 'VuMeter',
  iled: 'ILed',
  segread: 'SegRead',
  osc: 'Osc',
  'detent-ring': 'DetentRing',
  led: 'Led',
};

const STATE_KEYWORDS = new Set(['hover', 'focus', 'pressed', 'disabled', 'lit']);
const SIZE_KEYWORDS = new Set(['sm', 'md', 'lg']);
const ORIENTATION_KEYWORDS = new Set(['horizontal', 'vertical']);
const TONE_PREFIX = 'tone-';

export type DefaultPropValue = string | number | boolean | null;

export interface ComponentDefaults {
  component_name: string;
  class_root: string;
  default_props: Record<string, DefaultPropValue>;
  source_file: string;
}

export interface SelectorHit {
  selector: string;
  source_file: string;
  line: number;
}

export interface ModifierAxes {
  size: string[];
  tone: string[];
  state: string[];
  orientation: string[];
  lit: string[];
  other: string[];
}

export interface InteractionEntry {
  slug: string;
  kind: string;
  class_root: string;
  component_name: string | null;
  default_props: Record<string, DefaultPropValue>;
  modifiers: ModifierAxes;
  selectors: SelectorHit[];
  detail: Record<string, unknown>;
}

export interface InteractionsArtifact {
  schema_version: 1;
  source_files: string[];
  generated_at_utc: string;
  interactive_count: number;
  interactives: InteractionEntry[];
}

interface CliArgs {
  bundleDir: string;
  outPath: string;
}

function parseArgs(argv: string[]): CliArgs {
  let bundleDir = DEFAULT_BUNDLE_DIR;
  let outPath = DEFAULT_OUT;

  for (let i = 2; i < argv.length; i++) {
    const a = argv[i];
    if (a === '--in') {
      bundleDir = path.resolve(REPO_ROOT, argv[++i] ?? bundleDir);
    } else if (a === '--out') {
      outPath = path.resolve(REPO_ROOT, argv[++i] ?? outPath);
    } else if (a === '--help' || a === '-h') {
      console.log(`Usage: npx tsx tools/scripts/extract-cd-interactions.ts [options]

  --in  <dir>   CD bundle dir (default: web/design-refs/step-1-game-ui/cd-bundle)
  --out <file>  interactions.json output (default: web/design-refs/step-1-game-ui/interactions.json)

Walks archetype JSX via @babel/parser to recover component defaults +
class-root, then scans tokens/archetypes CSS for every BEM modifier on each
class-root. Merges with interactives.json detail blocks.
`);
      process.exit(0);
    }
  }
  return { bundleDir, outPath };
}

// -- JSX parsing -------------------------------------------------------------

function literalFromNode(node: Node | null | undefined): DefaultPropValue {
  if (!node) return null;
  if (node.type === 'StringLiteral') return node.value;
  if (node.type === 'NumericLiteral') return node.value;
  if (node.type === 'BooleanLiteral') return node.value;
  if (node.type === 'NullLiteral') return null;
  if (node.type === 'UnaryExpression' && node.operator === '-') {
    const inner = literalFromNode(node.argument);
    return typeof inner === 'number' ? -inner : null;
  }
  return null;
}

function extractDefaultsFromObjectPattern(pattern: ObjectPattern): Record<string, DefaultPropValue> {
  const out: Record<string, DefaultPropValue> = {};
  for (const prop of pattern.properties) {
    if (prop.type !== 'ObjectProperty') continue;
    const op = prop as ObjectProperty;
    const key = op.key.type === 'Identifier' ? (op.key as Identifier).name : null;
    if (!key) continue;
    if (op.value.type === 'AssignmentPattern') {
      const ap = op.value as AssignmentPattern;
      out[key] = literalFromNode(ap.right);
    } else {
      out[key] = null;
    }
  }
  return out;
}

function firstCxClassRoot(root: Node): string | null {
  // Manual DFS for the first `cx(...)` / `cx2(...)` call whose first arg is a
  // StringLiteral. Avoids @babel/traverse scope plumbing for sub-trees.
  const stack: Node[] = [root];
  while (stack.length) {
    const node = stack.pop() as Node;
    if (
      node.type === 'CallExpression' &&
      node.callee.type === 'Identifier' &&
      ((node.callee as Identifier).name === 'cx' || (node.callee as Identifier).name === 'cx2')
    ) {
      const arg0 = node.arguments[0];
      if (arg0 && arg0.type === 'StringLiteral') {
        return (arg0 as StringLiteral).value;
      }
    }
    for (const key of Object.keys(node) as (keyof Node)[]) {
      const child = (node as unknown as Record<string, unknown>)[key as string];
      if (!child) continue;
      if (Array.isArray(child)) {
        for (const c of child) {
          if (c && typeof c === 'object' && typeof (c as Node).type === 'string') {
            stack.push(c as Node);
          }
        }
      } else if (typeof child === 'object' && typeof (child as Node).type === 'string') {
        stack.push(child as Node);
      }
    }
  }
  return null;
}

export function parseArchetypeFile(absPath: string): ComponentDefaults[] {
  const src = fs.readFileSync(absPath, 'utf8');
  const ast = babelParse(src, { sourceType: 'script', plugins: ['jsx'] });
  const out: ComponentDefaults[] = [];

  function recordFromFunction(name: string, params: FunctionDeclaration['params'] | ArrowFunctionExpression['params'], body: Node): void {
    if (!params.length) return;
    const p0 = params[0];
    if (p0.type !== 'ObjectPattern') return;
    const defaults = extractDefaultsFromObjectPattern(p0 as ObjectPattern);
    const root = firstCxClassRoot(body);
    if (!root) return;
    out.push({
      component_name: name,
      class_root: root,
      default_props: defaults,
      source_file: path.relative(REPO_ROOT, absPath),
    });
  }

  for (const stmt of ast.program.body) {
    if (stmt.type === 'FunctionDeclaration' && stmt.id) {
      recordFromFunction(stmt.id.name, stmt.params, stmt.body);
    } else if (stmt.type === 'VariableDeclaration') {
      for (const decl of stmt.declarations) {
        if (
          decl.id.type === 'Identifier' &&
          decl.init &&
          (decl.init.type === 'ArrowFunctionExpression' || decl.init.type === 'FunctionExpression')
        ) {
          const fn = decl.init as ArrowFunctionExpression;
          recordFromFunction(decl.id.name, fn.params, fn.body);
        }
      }
    }
  }
  return out;
}

// -- CSS scanning ------------------------------------------------------------

const SELECTOR_PATTERN = /\.([a-z][a-z0-9-]*)(--[a-z0-9-]+)+/g;

export function scanCssForRoot(absPath: string, classRoot: string): SelectorHit[] {
  const src = fs.readFileSync(absPath, 'utf8');
  const lines = src.split('\n');
  const hits: SelectorHit[] = [];
  const sourceRel = path.relative(REPO_ROOT, absPath);
  for (let i = 0; i < lines.length; i++) {
    const line = lines[i];
    SELECTOR_PATTERN.lastIndex = 0;
    let m: RegExpExecArray | null;
    while ((m = SELECTOR_PATTERN.exec(line)) !== null) {
      if (m[1] !== classRoot) continue;
      const selector = m[0];
      // dedupe per-line per-selector
      if (hits.some((h) => h.selector === selector && h.source_file === sourceRel && h.line === i + 1)) {
        continue;
      }
      hits.push({ selector, source_file: sourceRel, line: i + 1 });
    }
  }
  return hits;
}

function classifyModifier(suffix: string): keyof ModifierAxes {
  if (SIZE_KEYWORDS.has(suffix)) return 'size';
  if (STATE_KEYWORDS.has(suffix)) return 'state';
  if (ORIENTATION_KEYWORDS.has(suffix)) return 'orientation';
  if (suffix.startsWith(TONE_PREFIX)) return 'tone';
  if (suffix === 'lit') return 'lit';
  return 'other';
}

export function bucketModifiers(hits: SelectorHit[], classRoot: string): ModifierAxes {
  const axes: ModifierAxes = { size: [], tone: [], state: [], orientation: [], lit: [], other: [] };
  const seen = new Set<string>();
  for (const h of hits) {
    // strip trailing pseudo / chained selectors after first whitespace or ::/:/.
    const trimmed = h.selector.split(/[\s>+~,]/)[0];
    const re = new RegExp(`^\\.${classRoot}(--[a-z0-9-]+)+$`);
    const exact = re.exec(trimmed);
    if (!exact) continue;
    if (seen.has(trimmed)) continue;
    seen.add(trimmed);
    // first modifier suffix (after the root)
    const mods = trimmed.slice(`.${classRoot}`.length).split('--').filter(Boolean);
    if (mods.length === 0) continue;
    const first = mods[0];
    const axis = classifyModifier(first);
    axes[axis].push(trimmed);
  }
  for (const k of Object.keys(axes) as (keyof ModifierAxes)[]) {
    axes[k] = Array.from(new Set(axes[k])).sort();
  }
  return axes;
}

// -- Merge + extract ---------------------------------------------------------

interface InteractiveJsonEntry {
  slug: string;
  kind: string;
  detail: Record<string, unknown>;
}

function loadInteractivesJson(bundleDir: string): InteractiveJsonEntry[] {
  // last-wins dedupe by slug — extension JSON overrides the base file when
  // both declare the same archetype (e.g. led / oscilloscope / detent-ring).
  const bySlug = new Map<string, InteractiveJsonEntry>();
  for (const name of INTERACTIVES_FILES) {
    const abs = path.join(bundleDir, name);
    if (!fs.existsSync(abs)) continue;
    const arr = JSON.parse(fs.readFileSync(abs, 'utf8')) as InteractiveJsonEntry[];
    for (const entry of arr) bySlug.set(entry.slug, entry);
  }
  return Array.from(bySlug.values());
}

export function buildInteractions(args: CliArgs): InteractionsArtifact {
  // Parse archetype JSX → component defaults indexed by class_root.
  const componentByRoot = new Map<string, ComponentDefaults>();
  const sourceFiles: string[] = [];
  for (const name of JSX_FILES) {
    const abs = path.join(args.bundleDir, name);
    if (!fs.existsSync(abs)) {
      throw new Error(`jsx_not_found: ${abs}`);
    }
    sourceFiles.push(path.relative(REPO_ROOT, abs));
    const defs = parseArchetypeFile(abs);
    for (const d of defs) {
      if (!componentByRoot.has(d.class_root)) componentByRoot.set(d.class_root, d);
    }
  }

  // Pre-scan each CSS file once per class_root.
  const cssAbs = CSS_FILES.map((f) => path.join(args.bundleDir, f)).filter((p) => fs.existsSync(p));
  for (const p of cssAbs) sourceFiles.push(path.relative(REPO_ROOT, p));

  // Merge with interactives.json + interactives-extension.json.
  const interactivesDocs = loadInteractivesJson(args.bundleDir);
  for (const f of INTERACTIVES_FILES) {
    const abs = path.join(args.bundleDir, f);
    if (fs.existsSync(abs)) sourceFiles.push(path.relative(REPO_ROOT, abs));
  }

  const out: InteractionEntry[] = [];
  for (const doc of interactivesDocs) {
    const classRoot = SLUG_TO_CLASS_ROOT[doc.slug];
    if (!classRoot) {
      throw new Error(`unknown_slug: ${doc.slug} (no class_root mapping)`);
    }
    const expectedComponent = CLASS_ROOT_TO_COMPONENT[classRoot] ?? null;
    const comp = componentByRoot.get(classRoot);
    if (expectedComponent && comp && comp.component_name !== expectedComponent) {
      throw new Error(
        `component_mismatch: slug=${doc.slug} expected=${expectedComponent} got=${comp.component_name}`,
      );
    }
    const allHits: SelectorHit[] = [];
    for (const cssPath of cssAbs) {
      allHits.push(...scanCssForRoot(cssPath, classRoot));
    }
    const modifiers = bucketModifiers(allHits, classRoot);
    out.push({
      slug: doc.slug,
      kind: doc.kind,
      class_root: classRoot,
      component_name: comp?.component_name ?? null,
      default_props: comp?.default_props ?? {},
      modifiers,
      selectors: allHits,
      detail: doc.detail,
    });
  }

  return {
    schema_version: 1,
    source_files: Array.from(new Set(sourceFiles)).sort(),
    generated_at_utc: new Date().toISOString(),
    interactive_count: out.length,
    interactives: out,
  };
}

async function main(): Promise<void> {
  const args = parseArgs(process.argv);
  let artifact: InteractionsArtifact;
  try {
    artifact = buildInteractions(args);
  } catch (e) {
    const msg = e instanceof Error ? e.message : String(e);
    process.stderr.write(`extract-cd-interactions: ${msg}\n`);
    process.exit(1);
    return;
  }
  fs.mkdirSync(path.dirname(args.outPath), { recursive: true });
  fs.writeFileSync(args.outPath, JSON.stringify(artifact, null, 2) + '\n', 'utf8');
  process.stdout.write(
    `extract-cd-interactions: wrote ${args.outPath} (${artifact.interactive_count} interactives)\n`,
  );
}

const entry = process.argv[1];
if (entry && import.meta.url === pathToFileURL(path.resolve(entry)).href) {
  void main();
}
