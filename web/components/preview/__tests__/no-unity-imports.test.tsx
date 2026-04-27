// TECH-2094 / Stage 10.1 — guardrail: web preview MUST NOT import Unity / canvas-3D.
//
// Per DEC-A44 + invariant guardrail: structural-fidelity preview is pure DOM/CSS.
// Static scan of every file under `web/components/preview/**` rejects any import
// containing `unity`, `three`, `webgl`, or `game-runtime`.

import { describe, expect, test } from "vitest";
import { readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";

const PREVIEW_DIR = join(__dirname, "..");
const BANNED = /\bfrom\s+["'](?:[^"']*\b(?:unity|three|webgl|game-runtime)\b[^"']*)["']/i;

function walk(dir: string): string[] {
  const out: string[] = [];
  for (const entry of readdirSync(dir)) {
    const abs = join(dir, entry);
    const st = statSync(abs);
    if (st.isDirectory()) {
      if (entry === "__tests__") continue;
      out.push(...walk(abs));
    } else if (/\.(ts|tsx|js|jsx|css)$/.test(entry)) {
      out.push(abs);
    }
  }
  return out;
}

describe("preview import scan (TECH-2094)", () => {
  test("no preview source imports Unity / three / webgl / game-runtime", () => {
    const files = walk(PREVIEW_DIR);
    expect(files.length).toBeGreaterThan(0);
    const offenders: string[] = [];
    for (const file of files) {
      const src = readFileSync(file, "utf8");
      if (BANNED.test(src)) {
        offenders.push(file);
      }
    }
    expect(offenders).toEqual([]);
  });
});
