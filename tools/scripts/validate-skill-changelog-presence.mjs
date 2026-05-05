#!/usr/bin/env node
/**
 * validate:skill-changelog-presence — author-time gate for SKILL.md
 * persistent change history.
 *
 * db-lifecycle-extensions Stage 3 / TECH-3402.
 *
 * Scans the lifecycle-set SKILL.md files (declared inline below) and
 * asserts each contains a top-level `## Changelog` heading. Body may be
 * empty (SK=a inline lock — no synthesized history; future entries
 * appended on real changes). Exit 0 if all present; exit 1 with offending
 * paths otherwise.
 *
 * Wired into `validate:all` via package.json after `validate:skill-drift`
 * so generation parity is confirmed first; changelog presence is a
 * content gate.
 */

import { promises as fs, existsSync } from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const REPO_ROOT = path.resolve(__dirname, "..", "..");
const SKILLS_DIR = path.join(REPO_ROOT, "ia", "skills");

// Lifecycle SKILLs requiring `## Changelog` per Stage 3 acceptance.
// Updated for ship-protocol Stage 5: 6 retired slugs hard-removed via TECH-12648
// (master-plan-new, master-plan-extend, stage-file, stage-authoring, ship-stage,
// stage-decompose). Successors (ship-plan, ship-cycle, ship-final, design-explore)
// are the new lifecycle SKILLs requiring this gate.
const REQUIRED_SKILLS = [
  "design-explore",
  "ship-plan",
  "ship-cycle",
  "ship-final",
];

const CHANGELOG_HEADING_RE = /^##\s+Changelog\s*$/m;

async function main() {
  const failures = [];

  for (const slug of REQUIRED_SKILLS) {
    const skillPath = path.join(SKILLS_DIR, slug, "SKILL.md");
    if (!existsSync(skillPath)) {
      failures.push({
        slug,
        path: path.relative(REPO_ROOT, skillPath),
        reason: "SKILL.md not found",
      });
      continue;
    }

    const body = await fs.readFile(skillPath, "utf8");
    if (!CHANGELOG_HEADING_RE.test(body)) {
      failures.push({
        slug,
        path: path.relative(REPO_ROOT, skillPath),
        reason: "missing top-level `## Changelog` heading",
      });
    }
  }

  if (failures.length > 0) {
    console.error("[validate:skill-changelog-presence] FAIL");
    for (const f of failures) {
      console.error(`  - ${f.path}: ${f.reason}`);
    }
    console.error(
      `\n${failures.length} SKILL${failures.length === 1 ? "" : "s"} missing \`## Changelog\` section.`
    );
    console.error(
      "Append empty `## Changelog` heading at end of body (SK=a inline lock — no synthesized history)."
    );
    process.exit(1);
  }

  console.log(
    `[validate:skill-changelog-presence] OK — ${REQUIRED_SKILLS.length} lifecycle SKILLs carry \`## Changelog\` heading.`
  );
}

main().catch((err) => {
  console.error("[validate:skill-changelog-presence] ERROR:", err);
  process.exit(2);
});
