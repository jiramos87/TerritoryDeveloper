/**
 * validate-ui-prefab-layout-coverage.ts
 *
 * Stage 13.7 — build-time anti-loss guard for baked Generated UI prefabs.
 * Stage 9.10 — source switched from ir.json (retired) to panels.json (DB snapshot).
 *              Extends coverage check to include layout_json.zone field on children.
 *
 * Enumerates panel slugs from `Assets/UI/Snapshots/panels.json` (schema_version 3)
 * + cross-checks against `layout-rects.json` / `layout-rects-overrides.json`,
 * surfacing slugs that the next bake would fall back to a sentinel 200×80 rect for.
 *
 * Exit codes:
 *   0  every panels.json panel either has a layout-rects entry OR no existing prefab
 *      to lose (warn-only output for uncovered slugs lacking on-disk prefabs)
 *   1  one or more panels are uncovered AND have an existing prefab on disk
 *   2  internal error (malformed JSON, missing source files, schema drift)
 *
 * Usage:
 *   npx tsx tools/scripts/validate-ui-prefab-layout-coverage.ts
 *
 * Wired into `npm run validate:all` after `validate:sprite-gen-schema`.
 */

import * as fs from "node:fs";
import * as path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");

// Stage 9.10: read from panels.json (DB snapshot) — ir.json retained on disk as design-ref archive.
const PANELS_PATH = path.join(
  REPO_ROOT,
  "Assets",
  "UI",
  "Snapshots",
  "panels.json",
);
const LAYOUT_RECTS_PATH = path.join(
  REPO_ROOT,
  "web",
  "design-refs",
  "step-1-game-ui",
  "layout-rects.json",
);
const LAYOUT_RECTS_OVERRIDES_PATH = path.join(
  REPO_ROOT,
  "web",
  "design-refs",
  "step-1-game-ui",
  "layout-rects-overrides.json",
);
const GENERATED_PREFAB_DIR = path.join(
  REPO_ROOT,
  "Assets",
  "UI",
  "Prefabs",
  "Generated",
);

// panels.json schema_version 3 types (top-level slug + fields + children shape).
type PanelSnapshotFields = {
  layout_template?: string;
  layout?: string;
  gap_px?: number;
};

type PanelSnapshotChild = {
  ord?: number;
  kind?: string;
  layout_json?: { zone?: string } | null;
};

type PanelSnapshotItem = {
  slug?: string;
  fields?: PanelSnapshotFields;
  // Legacy panel block (v1/v2); may coexist with top-level slug.
  panel?: { slug?: string };
  children?: PanelSnapshotChild[];
};

type PanelsSnapshot = {
  schema_version?: number;
  items?: PanelSnapshotItem[];
};

type LayoutNode = {
  node_kind: string;
  cd_slug: string;
};
type LayoutRects = {
  schema_version?: number;
  nodes?: LayoutNode[];
};
type LayoutRectsOverridePanel = { cd_slug?: string };
type LayoutRectsOverrides = {
  schema_version?: number;
  panels?: LayoutRectsOverridePanel[];
};

const VALID_ZONES = new Set(["left", "center", "right"]);

function readJson<T>(file: string): T {
  const raw = fs.readFileSync(file, "utf-8");
  return JSON.parse(raw) as T;
}

function main(): number {
  let panels: PanelsSnapshot;
  let layoutRects: LayoutRects;

  // Load panels.json.
  try {
    panels = readJson<PanelsSnapshot>(PANELS_PATH);
  } catch (e) {
    console.error(
      `[validate:ui-prefab-layout-coverage] cannot read panels.json at ${PANELS_PATH}: ${(e as Error).message}`,
    );
    return 2;
  }

  // Load layout-rects.json.
  try {
    layoutRects = readJson<LayoutRects>(LAYOUT_RECTS_PATH);
  } catch (e) {
    console.error(
      `[validate:ui-prefab-layout-coverage] cannot read layout-rects at ${LAYOUT_RECTS_PATH}: ${(e as Error).message}`,
    );
    return 2;
  }

  // Optional override file.
  let layoutOverrides: LayoutRectsOverrides | null = null;
  if (fs.existsSync(LAYOUT_RECTS_OVERRIDES_PATH)) {
    try {
      layoutOverrides = readJson<LayoutRectsOverrides>(LAYOUT_RECTS_OVERRIDES_PATH);
    } catch (e) {
      console.error(
        `[validate:ui-prefab-layout-coverage] cannot read overrides at ${LAYOUT_RECTS_OVERRIDES_PATH}: ${(e as Error).message}`,
      );
      return 2;
    }
  }

  if (!Array.isArray(panels.items)) {
    console.error(
      `[validate:ui-prefab-layout-coverage] panels.json has no items[] array — schema drift?`,
    );
    return 2;
  }

  if (!Array.isArray(layoutRects.nodes)) {
    console.error(
      `[validate:ui-prefab-layout-coverage] layout-rects has no nodes[] array — schema drift?`,
    );
    return 2;
  }

  // Resolve slug from each item — prefer top-level slug (v3), fall back to panel.slug (v1/v2).
  const panelSlugs: string[] = panels.items
    .map((item) => item?.slug ?? item?.panel?.slug)
    .filter((s): s is string => typeof s === "string" && s.length > 0);

  const layoutPanelSlugs = new Set<string>(
    layoutRects.nodes
      .filter((n) => n?.node_kind === "panel" && typeof n.cd_slug === "string")
      .map((n) => n.cd_slug),
  );
  const overrideSlugs = new Set<string>(
    (layoutOverrides?.panels ?? [])
      .map((p) => p?.cd_slug)
      .filter((s): s is string => typeof s === "string" && s.length > 0),
  );
  const coveredSlugs = new Set<string>([...layoutPanelSlugs, ...overrideSlugs]);

  // Layout-rects coverage check.
  const uncoveredWithPrefab: string[] = [];
  const uncoveredNoPrefab: string[] = [];
  for (const slug of panelSlugs) {
    if (coveredSlugs.has(slug)) continue;
    const prefabPath = path.join(GENERATED_PREFAB_DIR, `${slug}.prefab`);
    if (fs.existsSync(prefabPath)) {
      uncoveredWithPrefab.push(slug);
    } else {
      uncoveredNoPrefab.push(slug);
    }
  }

  // Stage 9.10: layout_json.zone coverage check.
  // Warn when a child carries a non-standard zone (not left/center/right).
  const zoneViolations: string[] = [];
  for (const item of panels.items) {
    const slug = item?.slug ?? item?.panel?.slug ?? "(unknown)";
    if (!Array.isArray(item?.children)) continue;
    for (const child of item.children!) {
      if (child?.layout_json == null) continue;
      const zone = child.layout_json.zone;
      if (zone !== undefined && !VALID_ZONES.has(zone)) {
        zoneViolations.push(`${slug} child ord=${child.ord} zone="${zone}"`);
      }
    }
  }

  console.log(
    `[validate:ui-prefab-layout-coverage] panels.json items: ${panelSlugs.length}; layout-rects panel entries: ${layoutPanelSlugs.size}; override entries: ${overrideSlugs.size}; uncovered+prefab: ${uncoveredWithPrefab.length}; uncovered+no-prefab: ${uncoveredNoPrefab.length}; zone-violations: ${zoneViolations.length}`,
  );

  if (uncoveredNoPrefab.length > 0) {
    console.log(
      `[validate:ui-prefab-layout-coverage] notice — panels without layout-rects/override entry AND no existing prefab (next bake emits 200×80 sentinel): ${uncoveredNoPrefab.join(", ")}`,
    );
  }

  if (zoneViolations.length > 0) {
    console.error(
      `[validate:ui-prefab-layout-coverage] WARN — ${zoneViolations.length} child(ren) carry layout_json.zone values outside {left, center, right}:`,
    );
    for (const v of zoneViolations) {
      console.error(`  - ${v}`);
    }
    // Zone violations are warnings only — do not fail the build; bake defaults to center with warning log.
  }

  if (uncoveredWithPrefab.length > 0) {
    console.error(
      `[validate:ui-prefab-layout-coverage] FAIL — ${uncoveredWithPrefab.length} panel(s) lack a layout-rects.json/overrides entry but have an existing prefab on disk. Next bake risks clobbering authored RectTransform state. Either regenerate via tools/scripts/extract-cd-layout-rects.ts OR add to web/design-refs/step-1-game-ui/layout-rects-overrides.json:`,
    );
    for (const slug of uncoveredWithPrefab) {
      console.error(`  - ${slug}  (prefab: Assets/UI/Prefabs/Generated/${slug}.prefab)`);
    }
    return 1;
  }

  console.log(
    `[validate:ui-prefab-layout-coverage] OK — every panels.json panel either has a layout-rects entry or no existing prefab to protect.`,
  );
  return 0;
}

process.exit(main());
