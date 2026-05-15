/**
 * IUIToolkitPanelBackend — boundary interface + DiskBackend + DbBackend stub + factory.
 *
 * Backend selected via env flag UI_TOOLKIT_BACKEND=disk|db (default: disk).
 * DbBackend stub returns parked error until TECH-34678..86 lands DB emitter parity.
 */

import fs from "node:fs";
import path from "node:path";
import { resolveRepoRoot } from "../config.js";
import { parseUssFile, type UssRule } from "./uss-parser.js";
import {
  isIdempotentWrite,
  parseUssPosition,
  serializeUssRules,
  type SerializableUssRule,
} from "../tools/_ui-toolkit-shared.js";

// ---------------------------------------------------------------------------
// Shared types
// ---------------------------------------------------------------------------

export interface PanelGetResult {
  slug: string;
  uxml_content: string | null;
  uxml_path: string | null;
  uxml_tree: VisualElementNode[] | null;
  uss_rules: UssRule[];
  uss_paths: string[];
  scene_uidoc: SceneUiDocInfo | null;
  golden_manifest: GoldenManifestEntry | null;
  exists: boolean;
}

export interface VisualElementNode {
  tag: string;
  name: string | null;
  classes: string[];
  attrs: Record<string, string>;
  children: VisualElementNode[];
  line: number;
}

export interface SceneUiDocInfo {
  scene_path: string | null;
  game_object: string | null;
  panel_asset_path: string | null;
  sort_order: number | null;
}

export interface GoldenManifestEntry {
  slug: string;
  uxml_path: string | null;
  uss_paths: string[];
  host_class: string | null;
}

export interface PanelListItem {
  slug: string;
  uxml_path: string | null;
  exists: boolean;
}

export interface PanelWrite {
  slug: string;
  uxml_content?: string;
  uss_content?: Record<string, string>;
}

export interface NodeWrite {
  tag: string;
  name?: string;
  classes?: string[];
  attrs?: Record<string, string>;
}

export interface UssRuleWrite {
  selector: string;
  props: Record<string, string>;
  /** prepend | append | before:{selector} | after:{selector} */
  position?: string;
}

// ---------------------------------------------------------------------------
// Interface
// ---------------------------------------------------------------------------

export interface IUIToolkitPanelBackend {
  readonly kind: "disk" | "db";

  getPanel(slug: string): Promise<PanelGetResult>;
  listPanels(opts?: { limit?: number; offset?: number }): Promise<PanelListItem[]>;

  // Write surface (disk: file mutations; db: SQL upserts)
  writePanel(write: PanelWrite): Promise<{ ok: boolean; error?: string }>;
  upsertNode(
    slug: string,
    parent_path: string,
    node: NodeWrite,
    ord?: number,
  ): Promise<{ ok: boolean; error?: string; idempotent?: boolean }>;
  removeNode(
    slug: string,
    node_path: string,
  ): Promise<{ ok: boolean; error?: string; idempotent?: boolean; orphan_uss_rules?: string[] }>;
  upsertUssRule(
    slug: string,
    selector: string,
    props: Record<string, string>,
    /** prepend | append | before:{selector} | after:{selector} */
    position?: string,
  ): Promise<{ ok: boolean; error?: string; idempotent?: boolean }>;
}

// ---------------------------------------------------------------------------
// UXML parser (minimal — parses VisualElement tree from .uxml XML)
// ---------------------------------------------------------------------------

function parseUxmlTree(content: string): VisualElementNode[] {
  const nodes: VisualElementNode[] = [];
  // Simple line-by-line tag scanner — handles namespace-prefixed tags (ui:VisualElement).
  const tagRe = /<([A-Za-z][A-Za-z0-9._:-]*)([^>/]*?)(\/?)>/g;
  const closeTagRe = /<\/([A-Za-z][A-Za-z0-9._:-]*)>/g;
  const lines = content.split(/\r?\n/);

  /** Strip namespace prefix: ui:VisualElement → VisualElement */
  function stripNs(tagName: string): string {
    const colonIdx = tagName.indexOf(":");
    return colonIdx === -1 ? tagName : tagName.slice(colonIdx + 1);
  }

  interface StackFrame {
    node: VisualElementNode;
    rawTag: string; // original tag name (with ns prefix) for close-tag matching
  }

  const stack: StackFrame[] = [];
  const roots: VisualElementNode[] = [];

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    const lineNum = i + 1;

    // Process open tags
    let tagMatch: RegExpExecArray | null;
    tagRe.lastIndex = 0;
    while ((tagMatch = tagRe.exec(line)) !== null) {
      const rawTag = tagMatch[1]!;
      const tag = stripNs(rawTag);
      const attrsRaw = tagMatch[2]!;
      const selfClose = tagMatch[3] === "/";

      // Skip XML declaration + UXML root namespace declaration tag
      if (rawTag.startsWith("?") || tag === "UXML") continue;

      // Parse attrs
      const attrs: Record<string, string> = {};
      const attrRe = /(\S+?)="([^"]*)"/g;
      let am: RegExpExecArray | null;
      while ((am = attrRe.exec(attrsRaw)) !== null) {
        attrs[am[1]!] = am[2]!;
      }

      const name = attrs["name"] ?? null;
      const classes = (attrs["class"] ?? "").split(" ").filter(Boolean);
      delete attrs["name"];
      delete attrs["class"];

      const node: VisualElementNode = {
        tag,
        name,
        classes,
        attrs,
        children: [],
        line: lineNum,
      };

      if (stack.length > 0) {
        stack[stack.length - 1]!.node.children.push(node);
      } else {
        roots.push(node);
      }

      if (!selfClose) {
        stack.push({ node, rawTag });
      }
    }

    // Process close tags
    closeTagRe.lastIndex = 0;
    let closeMatch: RegExpExecArray | null;
    while ((closeMatch = closeTagRe.exec(line)) !== null) {
      const closingRawTag = closeMatch[1]!;
      const closingTag = stripNs(closingRawTag);
      if (closingTag === "UXML") continue;
      // Pop matching frame by stripped tag name
      for (let j = stack.length - 1; j >= 0; j--) {
        if (stack[j]!.node.tag === closingTag) {
          stack.splice(j, 1);
          break;
        }
      }
    }
  }

  return roots.length > 0 ? roots : nodes;
}

// ---------------------------------------------------------------------------
// UXML subtree removal helpers
// ---------------------------------------------------------------------------

/**
 * Remove the element (and its children) with the given name attribute from UXML content.
 * Handles both self-closing and paired open/close tags.
 */
function removeElementByName(content: string, name: string): string {
  const lines = content.split(/\r?\n/);
  const resultLines: string[] = [];
  let skipDepth = 0;
  let inSkipBlock = false;
  // Tag name regex that captures the element tag for close-tag matching
  let skipTag: string | null = null;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;

    if (!inSkipBlock) {
      // Check if this line opens the target element (self-closing)
      const selfCloseRe = new RegExp(`<([A-Za-z][A-Za-z0-9._:-]*)\\b[^>]*\\bname="${name}"[^>]*/>`);
      if (selfCloseRe.test(line)) {
        // Self-closing — remove this line
        continue;
      }
      // Check if this line opens the target element (paired)
      const openRe = new RegExp(`<([A-Za-z][A-Za-z0-9._:-]*)\\b[^>]*\\bname="${name}"[^>]*>`);
      const openMatch = openRe.exec(line);
      if (openMatch) {
        inSkipBlock = true;
        skipDepth = 1;
        skipTag = openMatch[1]!;
        continue;
      }
      resultLines.push(line);
    } else {
      // We are inside the block to remove
      // Count opens and closes for skipTag
      const openCount = (line.match(new RegExp(`<${skipTag!}[\\s/>]`, "g")) ?? []).length;
      const closeCount = (line.match(new RegExp(`</${skipTag!}>`, "g")) ?? []).length;
      skipDepth += openCount - closeCount;
      if (skipDepth <= 0) {
        inSkipBlock = false;
        skipTag = null;
        skipDepth = 0;
      }
      // Skip this line
    }
  }

  return resultLines.join("\n");
}

/**
 * Extract all class names from the subtree rooted at the element with the given name.
 */
function extractClassesFromSubtree(content: string, name: string): string[] {
  const classes: string[] = [];
  // Find the start of the element
  const startRe = new RegExp(`<[A-Za-z][A-Za-z0-9._:-]*\\b[^>]*\\bname="${name}"[^>]*(?:/>|>)`);
  const startMatch = startRe.exec(content);
  if (!startMatch) return classes;

  const startIdx = startMatch.index;
  // Collect everything until we find the matching close (simple heuristic: scan until close tag depth=0)
  const substr = content.slice(startIdx);
  const classRe = /\bclass="([^"]*)"/g;
  let m: RegExpExecArray | null;
  while ((m = classRe.exec(substr)) !== null) {
    for (const cls of m[1]!.split(" ").filter(Boolean)) {
      if (!classes.includes(cls)) classes.push(cls);
    }
  }
  return classes;
}

// ---------------------------------------------------------------------------
// DiskBackend — reads from filesystem Assets/UI/UXML + Assets/UI/USS
// ---------------------------------------------------------------------------

/**
 * Canonical search paths for panel UXML files.
 * slug → `{root}/{slug}.uxml` checked in order.
 */
const UXML_SEARCH_ROOTS = [
  "Assets/UI/UXML",
  "Assets/UI/Prefabs",
  "Assets/UI",
];
const USS_SEARCH_ROOTS = [
  "Assets/UI/USS",
  "Assets/UI",
];

function findUxmlPath(repoRoot: string, slug: string): string | null {
  for (const root of UXML_SEARCH_ROOTS) {
    const candidate = path.join(repoRoot, root, `${slug}.uxml`);
    if (fs.existsSync(candidate)) return candidate;
  }
  return null;
}

function findUssPaths(repoRoot: string, slug: string): string[] {
  const found: string[] = [];
  for (const root of USS_SEARCH_ROOTS) {
    const candidate = path.join(repoRoot, root, `${slug}.uss`);
    if (fs.existsSync(candidate)) {
      found.push(candidate);
    }
  }
  return found;
}

function toRelPath(repoRoot: string, absPath: string): string {
  return path.relative(repoRoot, absPath).split(path.sep).join("/");
}

export class DiskBackend implements IUIToolkitPanelBackend {
  readonly kind = "disk" as const;

  private readonly repoRoot: string;

  constructor(repoRoot?: string) {
    this.repoRoot = repoRoot ?? resolveRepoRoot();
  }

  async getPanel(slug: string): Promise<PanelGetResult> {
    const uxmlAbsPath = findUxmlPath(this.repoRoot, slug);
    const ussAbsPaths = findUssPaths(this.repoRoot, slug);

    const uxmlContent = uxmlAbsPath ? fs.readFileSync(uxmlAbsPath, "utf8") : null;
    const uxmlTree = uxmlContent ? parseUxmlTree(uxmlContent) : null;

    const ussRules: UssRule[] = [];
    for (const p of ussAbsPaths) {
      const content = fs.readFileSync(p, "utf8");
      ussRules.push(...parseUssFile(content));
    }

    const golden = this._readGoldenManifestEntry(slug);

    return {
      slug,
      uxml_content: uxmlContent,
      uxml_path: uxmlAbsPath ? toRelPath(this.repoRoot, uxmlAbsPath) : null,
      uxml_tree: uxmlTree,
      uss_rules: ussRules,
      uss_paths: ussAbsPaths.map((p) => toRelPath(this.repoRoot, p)),
      scene_uidoc: null, // scene scan delegated to ui_toolkit_host_inspect
      golden_manifest: golden,
      exists: !!uxmlAbsPath,
    };
  }

  async listPanels(opts?: { limit?: number; offset?: number }): Promise<PanelListItem[]> {
    const limit = opts?.limit ?? 100;
    const offset = opts?.offset ?? 0;
    const items: PanelListItem[] = [];

    for (const root of UXML_SEARCH_ROOTS) {
      const dir = path.join(this.repoRoot, root);
      if (!fs.existsSync(dir)) continue;
      const entries = fs.readdirSync(dir).filter((n) => n.endsWith(".uxml"));
      for (const entry of entries) {
        const slug = path.basename(entry, ".uxml");
        if (!items.some((i) => i.slug === slug)) {
          items.push({
            slug,
            uxml_path: toRelPath(this.repoRoot, path.join(dir, entry)),
            exists: true,
          });
        }
      }
    }

    return items.slice(offset, offset + limit);
  }

  async writePanel(write: PanelWrite): Promise<{ ok: boolean; error?: string }> {
    try {
      if (write.uxml_content !== undefined) {
        const uxmlPath =
          findUxmlPath(this.repoRoot, write.slug) ??
          path.join(this.repoRoot, UXML_SEARCH_ROOTS[0]!, `${write.slug}.uxml`);
        fs.mkdirSync(path.dirname(uxmlPath), { recursive: true });
        fs.writeFileSync(uxmlPath, write.uxml_content, "utf8");
      }
      if (write.uss_content) {
        for (const [name, content] of Object.entries(write.uss_content)) {
          const ussPath = path.join(this.repoRoot, USS_SEARCH_ROOTS[0]!, name);
          fs.mkdirSync(path.dirname(ussPath), { recursive: true });
          fs.writeFileSync(ussPath, content, "utf8");
        }
      }
      return { ok: true };
    } catch (e) {
      return { ok: false, error: e instanceof Error ? e.message : String(e) };
    }
  }

  async upsertNode(
    slug: string,
    parent_path: string,
    node: NodeWrite,
    ord?: number,
  ): Promise<{ ok: boolean; error?: string; idempotent?: boolean }> {
    try {
      const uxmlAbsPath =
        findUxmlPath(this.repoRoot, slug) ??
        path.join(this.repoRoot, UXML_SEARCH_ROOTS[0]!, `${slug}.uxml`);

      // Read existing content (or bootstrap empty UXML)
      let content: string;
      if (fs.existsSync(uxmlAbsPath)) {
        content = fs.readFileSync(uxmlAbsPath, "utf8");
      } else {
        content = `<ui:UXML xmlns:ui="UnityEngine.UIElements" xmlns:uie="UnityEditor.UIElements">\n</ui:UXML>\n`;
      }

      const tag = node.tag;
      const name = node.name ?? "";
      const classes = (node.classes ?? []).join(" ");
      const attrs = node.attrs ?? {};

      // Build attribute string
      const attrParts: string[] = [];
      if (name) attrParts.push(`name="${name}"`);
      if (classes) attrParts.push(`class="${classes}"`);
      for (const [k, v] of Object.entries(attrs)) {
        attrParts.push(`${k}="${v}"`);
      }
      const attrStr = attrParts.length > 0 ? " " + attrParts.join(" ") : "";

      // Check if node with same name already exists under parent_path
      // Natural key: (slug, parent_path, name)
      if (name) {
        const namePattern = new RegExp(`<[A-Za-z][A-Za-z0-9._:-]*[^>]*\\bname="${name}"[^>]*/?>`, "g");
        if (namePattern.test(content)) {
          // Node exists — rebuild its attributes in-place
          const updated = content.replace(
            new RegExp(`(<[A-Za-z][A-Za-z0-9._:-]*[^>]*\\bname="${name}"[^>]*)(/>|>)`, "g"),
            (_match: string, _open: string, close: string) => {
              return `<${tag}${attrStr}${close === "/>" ? " /" : ""}>`;
            },
          );
          if (isIdempotentWrite(content, updated)) {
            return { ok: true, idempotent: true };
          }
          fs.mkdirSync(path.dirname(uxmlAbsPath), { recursive: true });
          fs.writeFileSync(uxmlAbsPath, updated, "utf8");
          return { ok: true };
        }
      }

      // Insert new node. Find the insertion point by parent_path.
      // parent_path is XPath-style: e.g. "root/content-area" matches name="content-area"
      // Simple strategy: insert before </ui:UXML> when parent_path is root,
      // or after the opening tag of the element with the last segment name.
      const segments = parent_path.split("/").filter(Boolean);
      const parentName = segments[segments.length - 1] ?? null;

      const indent = "    ";
      const nodeXml = `${indent}<${tag}${attrStr} />`;

      let updated: string;
      if (!parentName || parentName === "root" || parentName === "") {
        // Insert before closing UXML tag
        updated = content.replace(/(<\/ui:UXML>)/, `${nodeXml}\n$1`);
      } else {
        // Find the opening tag of the parent element by name and insert inside it
        const parentTagRe = new RegExp(
          `(<[A-Za-z][A-Za-z0-9._:-]*[^>]*\\bname="${parentName}"[^>]*>)`,
          "m",
        );
        if (parentTagRe.test(content)) {
          updated = content.replace(parentTagRe, `$1\n${nodeXml}`);
        } else {
          // Parent not found — append before </ui:UXML>
          updated = content.replace(/(<\/ui:UXML>)/, `${nodeXml}\n$1`);
        }
      }

      if (isIdempotentWrite(content, updated)) {
        return { ok: true, idempotent: true };
      }

      fs.mkdirSync(path.dirname(uxmlAbsPath), { recursive: true });
      fs.writeFileSync(uxmlAbsPath, updated, "utf8");
      return { ok: true };
    } catch (e) {
      return { ok: false, error: e instanceof Error ? e.message : String(e) };
    }
  }

  async removeNode(
    slug: string,
    node_path: string,
  ): Promise<{ ok: boolean; error?: string; idempotent?: boolean; orphan_uss_rules?: string[] }> {
    try {
      const uxmlAbsPath = findUxmlPath(this.repoRoot, slug);
      if (!uxmlAbsPath) {
        // File doesn't exist — no-op
        return { ok: true, idempotent: true, orphan_uss_rules: [] };
      }

      const content = fs.readFileSync(uxmlAbsPath, "utf8");

      // Resolve target name from path (last segment)
      const segments = node_path.split("/").filter(Boolean);
      const targetName = segments[segments.length - 1];

      if (!targetName) {
        return { ok: false, error: "node_path must have at least one segment" };
      }

      // Check if node exists
      const namePattern = new RegExp(`name="${targetName}"`);
      if (!namePattern.test(content)) {
        // Already absent — no-op
        return { ok: true, idempotent: true, orphan_uss_rules: [] };
      }

      // Cascade remove: strip the entire element subtree for this name.
      // Strategy: find the opening tag, then track brace depth to find its close.
      const updated = removeElementByName(content, targetName);

      if (isIdempotentWrite(content, updated)) {
        return { ok: true, idempotent: true, orphan_uss_rules: [] };
      }

      // Collect class names that were in the removed subtree
      const removedClasses = extractClassesFromSubtree(content, targetName);

      fs.writeFileSync(uxmlAbsPath, updated, "utf8");

      // Scan USS files for orphan selectors
      const orphan_uss_rules = this._findOrphanUssRules(slug, removedClasses, updated);

      return { ok: true, orphan_uss_rules };
    } catch (e) {
      return { ok: false, error: e instanceof Error ? e.message : String(e) };
    }
  }

  async upsertUssRule(
    slug: string,
    selector: string,
    props: Record<string, string>,
    position?: string,
  ): Promise<{ ok: boolean; error?: string; idempotent?: boolean }> {
    try {
      // Resolve USS path — always write to Generated dir per spec
      const generatedDir = path.join(this.repoRoot, "Assets/UI/Generated");
      const ussAbsPath =
        findUssPaths(this.repoRoot, slug)[0] ??
        path.join(generatedDir, `${slug}.uss`);

      // Read existing content
      let existingContent = "";
      if (fs.existsSync(ussAbsPath)) {
        existingContent = fs.readFileSync(ussAbsPath, "utf8");
      }

      // Parse existing rules
      const existingRules = parseUssFile(existingContent);

      // Check if selector already exists
      const existingIdx = existingRules.findIndex((r) => r.selector === selector);

      const newRule: SerializableUssRule = { selector, props };

      let updatedRules: SerializableUssRule[];

      if (existingIdx !== -1) {
        // Idempotency check: same props?
        const existing = existingRules[existingIdx]!;
        const sameProps =
          JSON.stringify(Object.entries(existing.props).sort()) ===
          JSON.stringify(Object.entries(props).sort());
        if (sameProps) {
          return { ok: true, idempotent: true };
        }
        // Update in-place
        updatedRules = existingRules.map((r, i) =>
          i === existingIdx ? newRule : { selector: r.selector, props: r.props },
        );
      } else {
        // Insert at position
        const pos = parseUssPosition(position ?? "append");
        const baseRules: SerializableUssRule[] = existingRules.map((r) => ({
          selector: r.selector,
          props: r.props,
        }));

        if (pos.kind === "prepend") {
          updatedRules = [newRule, ...baseRules];
        } else if (pos.kind === "append") {
          updatedRules = [...baseRules, newRule];
        } else if (pos.kind === "before" && pos.ref) {
          const refIdx = baseRules.findIndex((r) => r.selector === pos.ref);
          if (refIdx === -1) {
            updatedRules = [...baseRules, newRule];
          } else {
            updatedRules = [...baseRules.slice(0, refIdx), newRule, ...baseRules.slice(refIdx)];
          }
        } else if (pos.kind === "after" && pos.ref) {
          const refIdx = baseRules.findIndex((r) => r.selector === pos.ref);
          if (refIdx === -1) {
            updatedRules = [...baseRules, newRule];
          } else {
            updatedRules = [
              ...baseRules.slice(0, refIdx + 1),
              newRule,
              ...baseRules.slice(refIdx + 1),
            ];
          }
        } else {
          updatedRules = [...baseRules, newRule];
        }
      }

      const proposed = serializeUssRules(updatedRules);

      if (isIdempotentWrite(existingContent, proposed)) {
        return { ok: true, idempotent: true };
      }

      fs.mkdirSync(path.dirname(ussAbsPath), { recursive: true });
      fs.writeFileSync(ussAbsPath, proposed, "utf8");
      return { ok: true };
    } catch (e) {
      return { ok: false, error: e instanceof Error ? e.message : String(e) };
    }
  }

  /** Scan USS files for class selectors that no longer appear in the UXML after removal. */
  private _findOrphanUssRules(slug: string, removedClasses: string[], remainingUxml: string): string[] {
    const orphans: string[] = [];
    const ussPaths = findUssPaths(this.repoRoot, slug);
    for (const ussPath of ussPaths) {
      const ussContent = fs.readFileSync(ussPath, "utf8");
      const rules = parseUssFile(ussContent);
      for (const rule of rules) {
        // Extract class names from selector (e.g. ".foo" → "foo", ".foo .bar" → ["foo","bar"])
        const selectorClasses = rule.selector.match(/\.([\w-]+)/g)?.map((c) => c.slice(1)) ?? [];
        const isOrphan = selectorClasses.some((cls) => {
          // Was in removed subtree AND no longer in remaining UXML
          return removedClasses.includes(cls) && !new RegExp(`\\b${cls}\\b`).test(remainingUxml);
        });
        if (isOrphan) orphans.push(rule.selector);
      }
    }
    return orphans;
  }

  private _readGoldenManifestEntry(slug: string): GoldenManifestEntry | null {
    // Optional golden manifest at ia/state/ui-toolkit-golden-manifest.jsonl
    const manifestPath = path.join(
      this.repoRoot,
      "ia/state/ui-toolkit-golden-manifest.jsonl",
    );
    if (!fs.existsSync(manifestPath)) return null;
    try {
      const raw = fs.readFileSync(manifestPath, "utf8");
      for (const line of raw.split("\n")) {
        if (!line.trim()) continue;
        const entry = JSON.parse(line) as GoldenManifestEntry;
        if (entry.slug === slug) return entry;
      }
    } catch {
      // ignore parse errors
    }
    return null;
  }
}

// ---------------------------------------------------------------------------
// DbBackend stub — parked until TECH-34678..86
// ---------------------------------------------------------------------------

const DB_PARKED_ERROR = {
  ok: false as const,
  error: "db_backend_not_implemented" as const,
  parked_until: "TECH-34678..86 (DB emitter parity)",
};

export class DbBackend implements IUIToolkitPanelBackend {
  readonly kind = "db" as const;

  async getPanel(_slug: string): Promise<PanelGetResult> {
    throw DB_PARKED_ERROR;
  }
  async listPanels(): Promise<PanelListItem[]> {
    throw DB_PARKED_ERROR;
  }
  async writePanel(): Promise<{ ok: boolean; error?: string }> {
    throw DB_PARKED_ERROR;
  }
  async upsertNode(): Promise<{ ok: boolean; error?: string; idempotent?: boolean }> {
    throw DB_PARKED_ERROR;
  }
  async removeNode(): Promise<{ ok: boolean; error?: string; idempotent?: boolean; orphan_uss_rules?: string[] }> {
    throw DB_PARKED_ERROR;
  }
  async upsertUssRule(): Promise<{ ok: boolean; error?: string; idempotent?: boolean }> {
    throw DB_PARKED_ERROR;
  }
}

// ---------------------------------------------------------------------------
// Factory
// ---------------------------------------------------------------------------

/**
 * Create backend from config.
 * Reads UI_TOOLKIT_BACKEND env (disk|db). Default: disk.
 */
export function createBackend(opts?: { kind?: "disk" | "db"; repoRoot?: string }): IUIToolkitPanelBackend {
  const kind = opts?.kind ?? ((process.env.UI_TOOLKIT_BACKEND as "disk" | "db" | undefined) ?? "disk");
  if (kind === "db") return new DbBackend();
  return new DiskBackend(opts?.repoRoot);
}
