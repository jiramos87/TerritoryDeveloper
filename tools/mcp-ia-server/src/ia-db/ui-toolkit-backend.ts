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
  position?: "append" | "prepend";
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
  ): Promise<{ ok: boolean; error?: string }>;
  removeNode(slug: string, node_path: string): Promise<{ ok: boolean; error?: string }>;
  upsertUssRule(
    slug: string,
    selector: string,
    props: Record<string, string>,
    position?: "append" | "prepend",
  ): Promise<{ ok: boolean; error?: string }>;
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
    _parent_path: string,
    _node: NodeWrite,
    _ord?: number,
  ): Promise<{ ok: boolean; error?: string }> {
    // Stage 2 mutation — disk read-modify-write UXML. Stub for Stage 1.
    return { ok: false, error: `disk_upsert_node_not_implemented: Stage 2 covers UXML mutations for slug=${slug}` };
  }

  async removeNode(slug: string, _node_path: string): Promise<{ ok: boolean; error?: string }> {
    return { ok: false, error: `disk_remove_node_not_implemented: Stage 2 covers UXML mutations for slug=${slug}` };
  }

  async upsertUssRule(
    slug: string,
    _selector: string,
    _props: Record<string, string>,
    _position?: "append" | "prepend",
  ): Promise<{ ok: boolean; error?: string }> {
    return { ok: false, error: `disk_upsert_uss_rule_not_implemented: Stage 2 covers USS mutations for slug=${slug}` };
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
  async upsertNode(): Promise<{ ok: boolean; error?: string }> {
    throw DB_PARKED_ERROR;
  }
  async removeNode(): Promise<{ ok: boolean; error?: string }> {
    throw DB_PARKED_ERROR;
  }
  async upsertUssRule(): Promise<{ ok: boolean; error?: string }> {
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
