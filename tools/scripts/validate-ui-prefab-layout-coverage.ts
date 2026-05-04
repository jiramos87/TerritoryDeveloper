/**
 * validate-ui-prefab-layout-coverage.ts
 *
 * Stage 13.7 fallout — build-time half of the anti-loss guard around
 * `UiBakeHandler.SavePanelPrefab`. The runtime half (in
 * `Assets/Scripts/Editor/Bridge/UiBakeHandler.Frame.cs`) refuses to overwrite
 * a Generated UI prefab when CD truth source `layout-rects.json` has no entry
 * for the panel slug AND the existing prefab carries non-default
 * `RectTransform`. This validator is the proactive heads-up: enumerates IR
 * panel slugs from `web/design-refs/step-1-game-ui/ir.json` + cross-checks
 * against panel-kind entries in `web/design-refs/step-1-game-ui/layout-rects.json`,
 * surfacing slugs that the next bake would have to fall back to a sentinel
 * 200×80 top-left rect for.
 *
 * Exit codes:
 *   0  every IR panel either has a layout-rects entry OR no existing prefab
 *      to lose (warn-only output for uncovered slugs lacking on-disk prefabs)
 *   1  one or more IR panels are uncovered AND have an existing prefab on
 *      disk — designer must add to layout-rects.json (regenerate via
 *      `tools/scripts/extract-cd-layout-rects.ts`) before next bake
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
const IR_PATH = path.join(
  REPO_ROOT,
  "web",
  "design-refs",
  "step-1-game-ui",
  "ir.json",
);
const LAYOUT_RECTS_PATH = path.join(
  REPO_ROOT,
  "web",
  "design-refs",
  "step-1-game-ui",
  "layout-rects.json",
);
const GENERATED_PREFAB_DIR = path.join(
  REPO_ROOT,
  "Assets",
  "UI",
  "Prefabs",
  "Generated",
);

type IrPanel = { slug: string };
type Ir = { panels?: IrPanel[] };

type LayoutNode = {
  node_kind: string;
  cd_slug: string;
};
type LayoutRects = {
  schema_version?: number;
  nodes?: LayoutNode[];
};

function readJson<T>(file: string): T {
  const raw = fs.readFileSync(file, "utf-8");
  return JSON.parse(raw) as T;
}

function main(): number {
  let ir: Ir;
  let layoutRects: LayoutRects;
  try {
    ir = readJson<Ir>(IR_PATH);
  } catch (e) {
    console.error(
      `[validate:ui-prefab-layout-coverage] cannot read IR at ${IR_PATH}: ${(e as Error).message}`,
    );
    return 2;
  }
  try {
    layoutRects = readJson<LayoutRects>(LAYOUT_RECTS_PATH);
  } catch (e) {
    console.error(
      `[validate:ui-prefab-layout-coverage] cannot read layout-rects at ${LAYOUT_RECTS_PATH}: ${(e as Error).message}`,
    );
    return 2;
  }

  if (!Array.isArray(ir.panels)) {
    console.error(
      `[validate:ui-prefab-layout-coverage] IR has no panels[] array — schema drift?`,
    );
    return 2;
  }
  if (!Array.isArray(layoutRects.nodes)) {
    console.error(
      `[validate:ui-prefab-layout-coverage] layout-rects has no nodes[] array — schema drift?`,
    );
    return 2;
  }

  const irSlugs: string[] = ir.panels
    .map((p) => p?.slug)
    .filter((s): s is string => typeof s === "string" && s.length > 0);
  const layoutPanelSlugs = new Set<string>(
    layoutRects.nodes
      .filter((n) => n?.node_kind === "panel" && typeof n.cd_slug === "string")
      .map((n) => n.cd_slug),
  );

  const uncoveredWithPrefab: string[] = [];
  const uncoveredNoPrefab: string[] = [];
  for (const slug of irSlugs) {
    if (layoutPanelSlugs.has(slug)) continue;
    const prefabPath = path.join(GENERATED_PREFAB_DIR, `${slug}.prefab`);
    if (fs.existsSync(prefabPath)) {
      uncoveredWithPrefab.push(slug);
    } else {
      uncoveredNoPrefab.push(slug);
    }
  }

  console.log(
    `[validate:ui-prefab-layout-coverage] IR panels: ${irSlugs.length}; layout-rects panel entries: ${layoutPanelSlugs.size}; uncovered+prefab: ${uncoveredWithPrefab.length}; uncovered+no-prefab: ${uncoveredNoPrefab.length}`,
  );

  if (uncoveredNoPrefab.length > 0) {
    console.log(
      `[validate:ui-prefab-layout-coverage] notice — IR panels without layout-rects entry AND no existing prefab (next bake will emit top-left 200×80 sentinel): ${uncoveredNoPrefab.join(", ")}`,
    );
  }

  if (uncoveredWithPrefab.length > 0) {
    console.error(
      `[validate:ui-prefab-layout-coverage] FAIL — ${uncoveredWithPrefab.length} IR panel(s) lack a layout-rects.json entry but have an existing prefab on disk. Next bake risks clobbering authored RectTransform state. Add the slug(s) below to web/design-refs/step-1-game-ui/layout-rects.json (regenerate via tools/scripts/extract-cd-layout-rects.ts) before re-baking:`,
    );
    for (const slug of uncoveredWithPrefab) {
      console.error(`  - ${slug}  (prefab: Assets/UI/Prefabs/Generated/${slug}.prefab)`);
    }
    return 1;
  }

  console.log(
    `[validate:ui-prefab-layout-coverage] OK — every IR panel either has a layout-rects entry or no existing prefab to protect.`,
  );
  return 0;
}

process.exit(main());
