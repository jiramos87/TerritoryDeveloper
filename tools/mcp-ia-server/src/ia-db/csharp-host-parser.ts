/**
 * csharp-host-parser.ts — regex AST scan for UI Toolkit Host C# classes.
 *
 * Extracts:
 *   - serialized_fields: [SerializeField] / public fields
 *   - q_lookups: Q<T>() / Query<T>() calls grouped by element kind
 *   - click_bindings: RegisterCallback<ClickEvent> bindings
 *   - find_object_of_type_chain: FindObjectOfType<T>() usages
 *   - modal_slug: OpenModal("slug") / ShowPanel("slug") patterns
 *   - blip_bindings: Blip/EventBus subscriptions
 *   - runtime_ve_constructions: `new VisualElement()` / `new Label()` etc.
 *
 * Outside IUIToolkitPanelBackend per DEC-A28 I4 — Host C# is human-canonical, read-only.
 * Reuses pattern conventions from csharp-class-summary.ts.
 */

import fs from "node:fs";
import path from "node:path";

// ---------------------------------------------------------------------------
// Output types
// ---------------------------------------------------------------------------

export interface SerializedField {
  name: string;
  type: string;
  line: number;
}

export interface QLookup {
  kind: string;       // element type (e.g. "Button", "Label", "VisualElement")
  name: string | null; // Q<Button>("name") → "name"
  class_filter: string | null; // Q<Button>(className: ".foo") → ".foo"
  line: number;
}

export interface ClickBinding {
  target_name: string | null;  // variable/field the callback is registered on
  callback_body_hint: string;  // first 120 chars of callback (trimmed)
  line: number;
}

export interface FindObjectOfTypeEntry {
  type_name: string;
  line: number;
}

export interface BlipBinding {
  event_name: string;
  line: number;
}

export interface RuntimeVeConstruction {
  type_name: string;
  line: number;
}

export interface HostClassSummary {
  host_class: string;
  file: string | null;
  declaration_line: number | null;
  serialized_fields: SerializedField[];
  q_lookups: Record<string, QLookup[]>; // keyed by kind
  click_bindings: ClickBinding[];
  find_object_of_type_chain: FindObjectOfTypeEntry[];
  modal_slug: string | null;
  blip_bindings: BlipBinding[];
  runtime_ve_constructions: RuntimeVeConstruction[];
}

// ---------------------------------------------------------------------------
// File scanner
// ---------------------------------------------------------------------------

const ASSETS_SCRIPTS = "Assets/Scripts";

function globCsFiles(dir: string, results: string[] = []): string[] {
  if (!fs.existsSync(dir)) return results;
  const entries = fs.readdirSync(dir, { withFileTypes: true });
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      globCsFiles(full, results);
    } else if (entry.name.endsWith(".cs")) {
      results.push(full);
    }
  }
  return results;
}

function findFileForClass(className: string, repoRoot: string): string | null {
  // Primary: filename stem matches class name
  const scriptRoot = path.join(repoRoot, ASSETS_SCRIPTS);
  const allCs = globCsFiles(scriptRoot);
  const exact = allCs.find(
    (f) => path.basename(f, ".cs") === className,
  );
  if (exact) return exact;

  // Fallback: scan file content for `class ClassName`
  const classRe = new RegExp(`\\bclass\\s+${escapeRegex(className)}\\b`);
  for (const f of allCs) {
    const content = fs.readFileSync(f, "utf8");
    if (classRe.test(content)) return f;
  }
  return null;
}

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// ---------------------------------------------------------------------------
// Parse helpers
// ---------------------------------------------------------------------------

function parseSerializedFields(lines: string[]): SerializedField[] {
  const results: SerializedField[] = [];
  // Matches: `[SerializeField] private Type name;` (same line or next line)
  const sfAttrRe = /\[SerializeField\]/;
  const fieldRe = /\b(public|private|protected)\s+([\w.<>\[\]]+)\s+(\w+)\s*[;=]/;
  let pendingSf = false;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    const hasSfAttr = sfAttrRe.test(line);
    const mField = fieldRe.exec(line);

    if (hasSfAttr && mField) {
      // [SerializeField] and field on same line
      results.push({ name: mField[3]!, type: mField[2]!, line: i + 1 });
      pendingSf = false;
    } else if (hasSfAttr) {
      // [SerializeField] alone — field on next line
      pendingSf = true;
    } else if (pendingSf && mField) {
      results.push({ name: mField[3]!, type: mField[2]!, line: i + 1 });
      pendingSf = false;
    } else if (mField && mField[1] === "public") {
      results.push({ name: mField[3]!, type: mField[2]!, line: i + 1 });
      pendingSf = false;
    } else {
      pendingSf = false;
    }
  }
  return results;
}

function parseQLookups(lines: string[]): Record<string, QLookup[]> {
  const grouped: Record<string, QLookup[]> = {};
  // Matches: Q<Button>("name") or Query<Label>() or rootVisualElement.Q<VisualElement>()
  // Also plain: Q("name") with no type param
  const qRe = /\bQ(?:uery)?<([^>]+)>\s*\(([^)]*)\)/g;
  const qPlainRe = /\bQ\s*\(\s*"([^"]+)"\s*\)/g;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    let m: RegExpExecArray | null;

    qRe.lastIndex = 0;
    while ((m = qRe.exec(line)) !== null) {
      const kind = m[1]!.trim().split(".").pop()!; // strip namespace prefix
      const argsRaw = m[2]!.trim();
      let name: string | null = null;
      let classFilter: string | null = null;

      // Extract string arg as name
      const nameMatch = /"([^"]+)"/.exec(argsRaw);
      if (nameMatch) name = nameMatch[1]!;
      // Extract class: ".foo" pattern
      const classMatch = /className:\s*"([^"]+)"/.exec(argsRaw);
      if (classMatch) classFilter = classMatch[1]!;

      const entry: QLookup = { kind, name, class_filter: classFilter, line: i + 1 };
      (grouped[kind] ??= []).push(entry);
    }

    // Plain Q("name") → VisualElement kind (unknown)
    qPlainRe.lastIndex = 0;
    while ((m = qPlainRe.exec(line)) !== null) {
      const entry: QLookup = {
        kind: "VisualElement",
        name: m[1]!,
        class_filter: null,
        line: i + 1,
      };
      (grouped["VisualElement"] ??= []).push(entry);
    }
  }
  return grouped;
}

function parseClickBindings(lines: string[]): ClickBinding[] {
  const results: ClickBinding[] = [];
  // RegisterCallback<ClickEvent> — find the target variable from context
  const clickRe = /(\w+)?\s*\.?\s*RegisterCallback\s*<\s*ClickEvent\s*>\s*\(([^)]*)\)/;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    const m = clickRe.exec(line);
    if (m) {
      const target = m[1] ?? null;
      const callbackHint = (m[2] ?? "").trim().slice(0, 120);
      results.push({ target_name: target, callback_body_hint: callbackHint, line: i + 1 });
    }
  }
  return results;
}

function parseFindObjectOfType(lines: string[]): FindObjectOfTypeEntry[] {
  const results: FindObjectOfTypeEntry[] = [];
  const foRe = /FindObjectOfType\s*<\s*([^>]+)\s*>/g;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    let m: RegExpExecArray | null;
    foRe.lastIndex = 0;
    while ((m = foRe.exec(line)) !== null) {
      results.push({ type_name: m[1]!.trim(), line: i + 1 });
    }
  }
  return results;
}

function parseModalSlug(lines: string[]): string | null {
  // OpenModal("slug") / ShowPanel("slug") / NavigateTo("slug")
  const modalRe = /(?:OpenModal|ShowPanel|NavigateTo)\s*\(\s*"([^"]+)"\s*\)/;
  for (const line of lines) {
    const m = modalRe.exec(line);
    if (m) return m[1]!;
  }
  return null;
}

function parseBlipBindings(lines: string[]): BlipBinding[] {
  const results: BlipBinding[] = [];
  // Subscribe<EventName>(...) / On<EventName>(...) / EventBus.Subscribe("EventName")
  const blipRe = /(?:Subscribe|On)\s*<\s*([^>]+)\s*>\s*\(|EventBus\.Subscribe\s*\(\s*"([^"]+)"\s*,/g;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    let m: RegExpExecArray | null;
    blipRe.lastIndex = 0;
    while ((m = blipRe.exec(line)) !== null) {
      const eventName = (m[1] ?? m[2] ?? "").trim();
      if (eventName) {
        results.push({ event_name: eventName, line: i + 1 });
      }
    }
  }
  return results;
}

function parseRuntimeVeConstructions(lines: string[]): RuntimeVeConstruction[] {
  const results: RuntimeVeConstruction[] = [];
  const newVeRe = /new\s+(VisualElement|Label|Button|ScrollView|ListView|TextField|Toggle|Foldout|Image|ProgressBar)\s*\(/g;

  for (let i = 0; i < lines.length; i++) {
    const line = lines[i]!;
    let m: RegExpExecArray | null;
    newVeRe.lastIndex = 0;
    while ((m = newVeRe.exec(line)) !== null) {
      results.push({ type_name: m[1]!, line: i + 1 });
    }
  }
  return results;
}

function findDeclarationLine(lines: string[], className: string): number | null {
  const re = new RegExp(`\\bclass\\s+${escapeRegex(className)}\\b`);
  for (let i = 0; i < lines.length; i++) {
    if (re.test(lines[i]!)) return i + 1;
  }
  return null;
}

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

/**
 * Scan a Host C# class and return structured AST summary.
 * Returns null when the class cannot be located in Assets/Scripts/.
 */
export function scanHostClass(className: string, repoRoot: string): HostClassSummary | null {
  const filePath = findFileForClass(className, repoRoot);
  if (!filePath) {
    return {
      host_class: className,
      file: null,
      declaration_line: null,
      serialized_fields: [],
      q_lookups: {},
      click_bindings: [],
      find_object_of_type_chain: [],
      modal_slug: null,
      blip_bindings: [],
      runtime_ve_constructions: [],
    };
  }

  const content = fs.readFileSync(filePath, "utf8");
  const lines = content.split(/\r?\n/);
  const relFile = path.relative(repoRoot, filePath).split(path.sep).join("/");
  const declLine = findDeclarationLine(lines, className);

  return {
    host_class: className,
    file: relFile,
    declaration_line: declLine,
    serialized_fields: parseSerializedFields(lines),
    q_lookups: parseQLookups(lines),
    click_bindings: parseClickBindings(lines),
    find_object_of_type_chain: parseFindObjectOfType(lines),
    modal_slug: parseModalSlug(lines),
    blip_bindings: parseBlipBindings(lines),
    runtime_ve_constructions: parseRuntimeVeConstructions(lines),
  };
}
