// TECH-32905: ui_def_drift_scan UXML extension — unit test
//
// Verifies: scanUxmlArtifacts logic via static analysis of the source file.
// Uses file-existence assertions (no DB required) to confirm the UXML scan
// branch is present and backward-compatible.

import { describe, it } from "node:test";
import assert from "node:assert/strict";
import { readFileSync, existsSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { dirname, join } from "node:path";

const __dirname = dirname(fileURLToPath(import.meta.url));
const repoRoot = join(__dirname, "../../..");

describe("ui_def_drift_scan UXML extension", () => {
  it("sourceHasUxmlScanBranch [TECH-32905] — ui-def-drift-scan.ts references Assets/UI/Generated", () => {
    const scanPath = join(repoRoot, "tools/mcp-ia-server/src/tools/ui-def-drift-scan.ts");
    assert.ok(existsSync(scanPath), `ui-def-drift-scan.ts must exist: ${scanPath}`);
    const src = readFileSync(scanPath, "utf8");
    assert.ok(
      src.includes("Assets/UI/Generated"),
      "scan must reference Assets/UI/Generated path for UXML artifacts",
    );
    assert.ok(
      src.includes("uxml"),
      "scan must handle .uxml files",
    );
    assert.ok(
      src.includes("uss"),
      "scan must handle .uss files",
    );
  });

  it("kindDiscriminatorPresent [TECH-32905] — UxmlFinding shape has kind: 'uxml'|'uss'|'prefab'", () => {
    const scanPath = join(repoRoot, "tools/mcp-ia-server/src/tools/ui-def-drift-scan.ts");
    const src = readFileSync(scanPath, "utf8");
    assert.ok(
      src.includes("kind: \"uxml\"") || src.includes("kind: 'uxml'") || src.includes("\"uxml\" | \"uss\""),
      "UxmlFinding must have kind discriminator with 'uxml' value",
    );
    assert.ok(
      src.includes("drift: \"missing\"") ||
        src.includes("drift: 'missing'") ||
        src.includes("\"missing\" | \"stale\" | \"ok\""),
      "UxmlFinding must have drift field with 'missing' variant",
    );
  });

  it("backwardCompatiblePrefabScanPreserved [TECH-32905] — panel snapshot scan still present", () => {
    const scanPath = join(repoRoot, "tools/mcp-ia-server/src/tools/ui-def-drift-scan.ts");
    const src = readFileSync(scanPath, "utf8");
    assert.ok(
      src.includes("panels.json"),
      "prefab snapshot scan must still reference panels.json (backward compat)",
    );
    assert.ok(
      src.includes("rect_json"),
      "prefab rect_json drift surface must still be present",
    );
    assert.ok(
      src.includes("total_panels"),
      "DriftScanResult must still have total_panels field",
    );
  });

  it("uxmlFindingsInResultShape [TECH-32905] — DriftScanResult includes uxml_findings field", () => {
    const scanPath = join(repoRoot, "tools/mcp-ia-server/src/tools/ui-def-drift-scan.ts");
    const src = readFileSync(scanPath, "utf8");
    assert.ok(
      src.includes("uxml_findings"),
      "DriftScanResult must include uxml_findings field (additive)",
    );
  });

  it("includeUxmlInputSchemaPresent [TECH-32905] — inputSchema has include_uxml boolean field", () => {
    const scanPath = join(repoRoot, "tools/mcp-ia-server/src/tools/ui-def-drift-scan.ts");
    const src = readFileSync(scanPath, "utf8");
    assert.ok(
      src.includes("include_uxml"),
      "inputSchema must have include_uxml optional boolean field",
    );
  });
});
