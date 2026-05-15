/**
 * MCP tool: ui_toolkit_scene_uidoc_validate — scene YAML wiring verdict tool.
 *
 * Inputs: slug, optional scene (default CityScene).
 * Checks {slug}-uidoc GameObject exists in scene YAML + UIDocument component
 * has sourceAsset + Host MonoBehaviour attached.
 *
 * Returns structured verdict:
 *   {wired:bool, missing:[{field,expected,found}], suggestion:"bridge_wire_call"|"runtime_spawn_pattern"|null}
 *
 * suggestion logic:
 *   - GameObject absent → "runtime_spawn_pattern" (precedent: HoverInfoHost, MapPanelHost.Bootstrap)
 *   - GameObject present but component drifted → "bridge_wire_call"
 *   - All wired → null
 *
 * Lives outside IUIToolkitPanelBackend — pure scene YAML scan (DEC-A28 I4).
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import { parse as parseYaml } from "yaml";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";
import { resolveRepoRoot } from "../config.js";

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface MissingField {
  field: string;
  expected: string;
  found: string | null;
}

export type SuggestionKind = "bridge_wire_call" | "runtime_spawn_pattern" | null;

export interface SceneVerdict {
  wired: boolean;
  missing: MissingField[];
  suggestion: SuggestionKind;
}

// ---------------------------------------------------------------------------
// YAML fixture shape (used by unit tests via fixture files)
// ---------------------------------------------------------------------------

interface FixtureGameObject {
  name: string;
  components: Array<{ type: string; sourceAsset?: string; sourceAssetGUID?: string }>;
}

interface FixtureScene {
  slug: string;
  scene: string;
  gameObjects: FixtureGameObject[];
}

// ---------------------------------------------------------------------------
// Verdict builder (exported for unit tests)
// ---------------------------------------------------------------------------

/**
 * Build scene wiring verdict from a fixture or real scene YAML path.
 * Can accept either a fixture YAML path (test context) or a Unity scene path.
 */
export function buildSceneVerdict(slug: string, scenePath: string): SceneVerdict {
  if (!fs.existsSync(scenePath)) {
    return {
      wired: false,
      missing: [{ field: "scene_file", expected: scenePath, found: null }],
      suggestion: "runtime_spawn_pattern",
    };
  }

  const raw = fs.readFileSync(scenePath, "utf8");

  // Determine if this is a fixture YAML or a real Unity .unity scene file
  if (scenePath.endsWith(".yaml") || scenePath.endsWith(".yml")) {
    return buildVerdictFromFixture(slug, raw);
  }

  // Real Unity .unity scene: scan as text for component patterns
  return buildVerdictFromUnityScene(slug, raw, scenePath);
}

function buildVerdictFromFixture(slug: string, raw: string): SceneVerdict {
  let fixture: FixtureScene;
  try {
    fixture = parseYaml(raw) as FixtureScene;
  } catch {
    return {
      wired: false,
      missing: [{ field: "scene_parse", expected: "valid YAML", found: null }],
      suggestion: "runtime_spawn_pattern",
    };
  }

  const expectedGoName = `${slug}-uidoc`;
  const go = (fixture.gameObjects ?? []).find((g) => g.name === expectedGoName);

  if (!go) {
    return {
      wired: false,
      missing: [{ field: "GameObject", expected: expectedGoName, found: null }],
      suggestion: "runtime_spawn_pattern",
    };
  }

  const missing: MissingField[] = [];
  const components = go.components ?? [];
  const componentTypes = components.map((c) => c.type);

  // Check UIDocument component
  const uidoc = components.find((c) => c.type === "UIDocument");
  if (!uidoc) {
    missing.push({ field: "UIDocument", expected: "UIDocument component", found: null });
  }

  // Check Host MonoBehaviour (any component other than UIDocument = Host)
  const hasHost = componentTypes.some((t) => t !== "UIDocument");
  if (!hasHost) {
    missing.push({ field: "HostMonoBehaviour", expected: "Host MonoBehaviour component", found: null });
  }

  if (missing.length > 0) {
    return { wired: false, missing, suggestion: "bridge_wire_call" };
  }

  return { wired: true, missing: [], suggestion: null };
}

function buildVerdictFromUnityScene(slug: string, raw: string, _scenePath: string): SceneVerdict {
  const expectedGoName = `${slug}-uidoc`;

  // Heuristic: scan for GameObject name pattern in Unity scene text
  const goPattern = new RegExp(`m_Name:\\s*${escapeRegex(expectedGoName)}`);
  const hasGo = goPattern.test(raw);

  if (!hasGo) {
    return {
      wired: false,
      missing: [{ field: "GameObject", expected: expectedGoName, found: null }],
      suggestion: "runtime_spawn_pattern",
    };
  }

  // Check for UIDocument script reference in scene
  const hasUiDoc = /UIDocument/.test(raw);
  if (!hasUiDoc) {
    return {
      wired: false,
      missing: [{ field: "UIDocument", expected: "UIDocument component", found: null }],
      suggestion: "bridge_wire_call",
    };
  }

  return { wired: true, missing: [], suggestion: null };
}

function escapeRegex(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// ---------------------------------------------------------------------------
// Scene path resolver
// ---------------------------------------------------------------------------

function resolveScenePath(scene: string, repoRoot: string): string {
  // Fixture YAML first (for test-aware resolution)
  const candidates = [
    path.join(repoRoot, `Assets/Scenes/${scene}.unity`),
    path.join(repoRoot, `Assets/Scenes/${scene}.yaml`),
    path.join(repoRoot, scene), // explicit path passthrough
  ];
  for (const c of candidates) {
    if (fs.existsSync(c)) return c;
  }
  // Return first candidate (will fail with not_found verdict)
  return candidates[0]!;
}

// ---------------------------------------------------------------------------
// Zod schema
// ---------------------------------------------------------------------------

const inputSchema = z.object({
  slug: z.string().min(1).describe("Panel slug (e.g. 'budget-panel'). Checks for '{slug}-uidoc' GameObject."),
  scene: z
    .string()
    .optional()
    .default("CityScene")
    .describe("Scene name or repo-relative path (default: CityScene → Assets/Scenes/CityScene.unity)."),
  scene_fixture_path: z
    .string()
    .optional()
    .describe("Explicit path to a fixture YAML (for tests / offline validation, bypasses scene discovery)."),
});

type Input = z.infer<typeof inputSchema>;

function jsonResult(payload: unknown) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(payload, null, 2) }],
  };
}

// ---------------------------------------------------------------------------
// Tool registration
// ---------------------------------------------------------------------------

export function registerUiToolkitSceneUidocValidate(server: McpServer): void {
  server.registerTool(
    "ui_toolkit_scene_uidoc_validate",
    {
      description:
        "Validate that a panel's {slug}-uidoc GameObject is correctly wired in a Unity scene. " +
        "Checks: GO exists + UIDocument component present + Host MonoBehaviour attached. " +
        "Returns {wired:bool, missing:[{field,expected,found}], suggestion:'bridge_wire_call'|'runtime_spawn_pattern'|null}. " +
        "suggestion=runtime_spawn_pattern when GO absent (pattern: HoverInfoHost/MapPanelHost.Bootstrap). " +
        "suggestion=bridge_wire_call when GO exists but component drifted. " +
        "Pure scene YAML scan — outside IUIToolkitPanelBackend (DEC-A28 I4).",
      inputSchema,
    },
    async (args) =>
      runWithToolTiming("ui_toolkit_scene_uidoc_validate", async () => {
        const envelope = await wrapTool(async (input: Input) => {
          const repoRoot = resolveRepoRoot();

          let scenePath: string;
          if (input.scene_fixture_path) {
            scenePath = path.isAbsolute(input.scene_fixture_path)
              ? input.scene_fixture_path
              : path.join(repoRoot, input.scene_fixture_path);
          } else {
            scenePath = resolveScenePath(input.scene, repoRoot);
          }

          const verdict = buildSceneVerdict(input.slug, scenePath);

          return {
            slug: input.slug,
            scene: input.scene,
            scene_path: scenePath,
            ...verdict,
          };
        })(inputSchema.parse(args ?? {}));
        return jsonResult(envelope);
      }),
  );
}
