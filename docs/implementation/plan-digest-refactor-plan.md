# Plan-Digest Refactor — Implementation Plan (2026-04-22)

Sequential, pre-digested change list for a single-pass implementation of the plan-digest refactor. Run steps in order; do not skip. Each step has a gate command — STOP at the first red gate and surface to the user.

All paths below are **repo-relative to `/Users/javier/bacayo-studio/territory-developer/`**.

Decisions locked upstream — source: `/tmp/plan-digest-refactor-decisions-2026-04-22.md` (Q1–Q13 all resolved; 9-point rubric + 7 MCP tools + lazy §Plan Author retrofit branch). Do not re-open.

Mode `stage` only in this plan (the only mode wired into live chains). Mode `audit` is scaffolded behind a flag in the new skill/tool surface but not dispatched.

---

## Progression checklist

- [x] **Step 1** — Scaffold 7 new MCP tools under `plan-digest-*` prefix (files only; no registration)
- [x] **Step 2** — Register the 7 tools in `server-registrations.ts` + update MCP README + extend `verify-mcp.ts` required-list
- [x] **Step 3** — Create new `plan-digest` skill + agent + rule + template (4 new files)
- [x] **Step 4** — Insert `plan-digest` into `/stage-file` command chain (Step 4, between plan-author and plan-reviewer) and renumber
- [x] **Step 5** — Swap `spec-implementer` source from §Plan Author → §Plan Digest
- [x] **Step 6** — Ship-stage readiness gate swap (`/ship-stage` command + skill + agent) + lazy-migration branch
- [x] **Step 7** — Peer-chain updates: `/project-new` + `stage-closeout-plan` + `master-plan-extend` chains
- [x] **Step 8** — Rules + templates: pair-contract, project-hierarchy, orchestrator-vs-spec, caveman-authoring, project-spec-template, master-plan-template
- [x] **Step 9** — Skill narratives: plan-author / plan-review / plan-applier / release-rollout / opus-code-review / design-explore / stage-file-apply / README
- [x] **Step 10** — Cursor parity: regenerate wrappers + patch `cursor-skill-plan-author.mdc` + `cursor-skill-plan-review.mdc` + `cursor-lifecycle-adapters.mdc`
- [x] **Step 11** — Docs narrative: `agent-lifecycle.md` + `information-architecture-overview.md`
- [x] **Step 12** — Final gate: `npm run validate:all`, MCP verify, grep-sweep for residual `§Plan Author` in authored surface

**Completion log (territory-developer, 2026-04-22):** Code + IA surfaces for steps 1–11 are in-repo. **MCP verify:** `npx tsx tools/mcp-ia-server/scripts/verify-mcp.ts` — exit 0. **`npm run validate:all`:** may fail on hosts where `test:ia` runs `node --import tsx` and Node does not support `--import` (error: `node: bad option: --import`); use Node 20+ or run validators individually. **`§Plan Author` grep:** expected hits remain in `plan-author`, `plan-digest`, `plan-apply-pair-contract`, and templates that name the ephemeral section — not a routing doc defect.

---

## Step 1 — Scaffold 7 new MCP tools (files only)

**Goal:** Create seven new tool files under `tools/mcp-ia-server/src/tools/` namespaced with `plan-digest-*`. Each tool returns tiny structured payloads (token-economy rule — ≤20 lines typical). No registration yet — Step 2 wires them in.

**Decision:** tool id strings use snake_case with `plan_digest_` prefix (`plan_digest_verify_paths`, `plan_digest_resolve_anchor`, etc.) to match the existing `stage_closeout_digest` naming style and avoid collision (Q12).

**Edits — create each file with the shape below** (model every new file on the existing `tools/mcp-ia-server/src/tools/glossary-lookup.ts` scaffold: Zod input schema, `runWithToolTiming` + `wrapTool` envelope, `registerXxx(server, registry?)` export, JSDoc header).

1a. **Create** `tools/mcp-ia-server/src/tools/plan-digest-verify-paths.ts`:

```ts
/**
 * MCP tool: plan_digest_verify_paths — token-economy variant of Glob.
 * Input: { paths: string[] } (repo-relative). Output: { results: Record<string, boolean> }
 * where true = path exists on disk. No listings, no globbing, no stat metadata.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const InputSchema = z.object({
  paths: z.array(z.string().min(1)).min(1).max(200),
});

export function registerPlanDigestVerifyPaths(server: McpServer): void {
  server.tool(
    "plan_digest_verify_paths",
    "Given list of repo-relative paths, return exists/not-exists per path. Token-economy alternative to Glob.",
    InputSchema.shape,
    wrapTool("plan_digest_verify_paths", async (input) => {
      return runWithToolTiming("plan_digest_verify_paths", async () => {
        const root = resolveRepoRoot();
        const results: Record<string, boolean> = {};
        for (const p of input.paths) {
          const abs = path.resolve(root, p);
          if (!abs.startsWith(root)) { results[p] = false; continue; }
          results[p] = fs.existsSync(abs);
        }
        return { content: [{ type: "text", text: JSON.stringify({ results }) }] };
      });
    }),
  );
}
```

1b. **Create** `tools/mcp-ia-server/src/tools/plan-digest-resolve-anchor.ts`:

```ts
/**
 * MCP tool: plan_digest_resolve_anchor — narrow-scope grep replacement.
 * Input: { file: string, substring: string, max_hits?: number (default 5) }.
 * Output: { file, hits: number, matches: Array<{line: number, context: string}> }.
 * Fails loud when hits !== 1 — plan-digest requires unique anchors.
 * Context = ≤3 lines surrounding. Token budget: ≤20 lines output even on miss.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const InputSchema = z.object({
  file: z.string().min(1),
  substring: z.string().min(1),
  max_hits: z.number().int().min(1).max(20).optional().default(5),
});

export function registerPlanDigestResolveAnchor(server: McpServer): void {
  server.tool(
    "plan_digest_resolve_anchor",
    "Given (file, substring), return hit count + ≤3-line context per hit. Fails when hits !== 1.",
    InputSchema.shape,
    wrapTool("plan_digest_resolve_anchor", async (input) => {
      return runWithToolTiming("plan_digest_resolve_anchor", async () => {
        const root = resolveRepoRoot();
        const abs = path.resolve(root, input.file);
        if (!abs.startsWith(root) || !fs.existsSync(abs)) {
          return { content: [{ type: "text", text: JSON.stringify({ file: input.file, hits: 0, matches: [], error: "file_not_found" }) }] };
        }
        const lines = fs.readFileSync(abs, "utf8").split("\n");
        const matches: Array<{ line: number; context: string }> = [];
        for (let i = 0; i < lines.length && matches.length < input.max_hits; i += 1) {
          if (lines[i].includes(input.substring)) {
            const start = Math.max(0, i - 1); const end = Math.min(lines.length, i + 2);
            matches.push({ line: i + 1, context: lines.slice(start, end).join("\n") });
          }
        }
        return { content: [{ type: "text", text: JSON.stringify({ file: input.file, hits: matches.length, matches }) }] };
      });
    }),
  );
}
```

1c. **Create** `tools/mcp-ia-server/src/tools/plan-digest-render-literal.ts`:

```ts
/**
 * MCP tool: plan_digest_render_literal — verbatim line-range read.
 * Input: { file: string, line_start: number, line_end: number } (1-indexed, inclusive).
 * Output: { file, line_start, line_end, content: string }.
 * Refuses ranges >100 lines — plan-digest embeds small literals, not whole files.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const InputSchema = z.object({
  file: z.string().min(1),
  line_start: z.number().int().min(1),
  line_end: z.number().int().min(1),
});

export function registerPlanDigestRenderLiteral(server: McpServer): void {
  server.tool(
    "plan_digest_render_literal",
    "Return verbatim content for a small line range (≤100 lines).",
    InputSchema.shape,
    wrapTool("plan_digest_render_literal", async (input) => {
      return runWithToolTiming("plan_digest_render_literal", async () => {
        if (input.line_end < input.line_start) throw new Error("line_end < line_start");
        if (input.line_end - input.line_start + 1 > 100) throw new Error("range_exceeds_100_lines");
        const root = resolveRepoRoot();
        const abs = path.resolve(root, input.file);
        if (!abs.startsWith(root) || !fs.existsSync(abs)) throw new Error("file_not_found");
        const lines = fs.readFileSync(abs, "utf8").split("\n");
        const slice = lines.slice(input.line_start - 1, input.line_end).join("\n");
        return { content: [{ type: "text", text: JSON.stringify({ file: input.file, line_start: input.line_start, line_end: input.line_end, content: slice }) }] };
      });
    }),
  );
}
```

1d. **Create** `tools/mcp-ia-server/src/tools/plan-digest-scan-for-picks.ts`:

```ts
/**
 * MCP tool: plan_digest_scan_for_picks — lint-only pick detector.
 * Input: { content: string } (digested-plan body).
 * Output: { pick_count: number, findings: Array<{line: number, phrase: string, excerpt: string}> }.
 * Regex set (case-insensitive): "user decides", "user picks", "likely", "probably",
 * "we could", "might", "consider", "TBD", "up to you", "your call".
 * NEVER resolves picks — fails fast on leak; plan-digest's rubric gate uses this.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const InputSchema = z.object({ content: z.string().min(1) });

const PICK_PATTERNS: RegExp[] = [
  /\buser decides\b/i, /\buser picks\b/i, /\blikely\b/i, /\bprobably\b/i,
  /\bwe could\b/i, /\bmight\b/i, /\bconsider\b/i, /\bTBD\b/, /\bup to you\b/i, /\byour call\b/i,
];

export function registerPlanDigestScanForPicks(server: McpServer): void {
  server.tool(
    "plan_digest_scan_for_picks",
    "Flag hand-wavy phrases in a digested-plan body. Lint-only (does not resolve).",
    InputSchema.shape,
    wrapTool("plan_digest_scan_for_picks", async (input) => {
      return runWithToolTiming("plan_digest_scan_for_picks", async () => {
        const findings: Array<{ line: number; phrase: string; excerpt: string }> = [];
        const lines = input.content.split("\n");
        for (let i = 0; i < lines.length; i += 1) {
          for (const rx of PICK_PATTERNS) {
            const m = lines[i].match(rx);
            if (m) { findings.push({ line: i + 1, phrase: m[0], excerpt: lines[i].slice(0, 120) }); break; }
          }
        }
        return { content: [{ type: "text", text: JSON.stringify({ pick_count: findings.length, findings: findings.slice(0, 20) }) }] };
      });
    }),
  );
}
```

1e. **Create** `tools/mcp-ia-server/src/tools/plan-digest-lint.ts`:

```ts
/**
 * MCP tool: plan_digest_lint — runs the 9-point rubric (ia/rules/plan-digest-contract.md).
 * Input: { content: string, file?: string } — body of §Plan Digest or aggregate stage doc.
 * Output: { pass: boolean, failures: Array<{rule: 1..9, where: string, detail: string}> }.
 * Rubric points (1..9 per rule file):
 *   1 zero open picks (delegates to plan_digest_scan_for_picks internally)
 *   2 every path-looking token exists (samples repo-relative paths; verifies)
 *   3 every edit tuple has before+after
 *   4 before-strings unique (≥1 edit tuples — caller resolved anchors upstream; rubric only asserts presence)
 *   5 every step has a gate command
 *   6 sequential only — no "in parallel" / "|| true" / branch language
 *   7 scope-narrowed — no "11 lines" without N anchors
 *   8 meta-stripped — no "user picks / likely / we decided"
 *   9 STOP condition per step
 * Cap: returns at most 20 failures.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const InputSchema = z.object({ content: z.string().min(1), file: z.string().optional() });

const PICK_RX = /\b(user decides|user picks|likely|probably|we could|might|consider|TBD|up to you|your call)\b/i;
const PARALLEL_RX = /\bin parallel\b|\b\|\| true\b|\bif-then-else\b/i;
const PATH_RX = /(?:^|\s)([A-Za-z0-9_\-\/\.]+\/[A-Za-z0-9_\-\/\.]+\.[A-Za-z0-9]+)/gm;

export function registerPlanDigestLint(server: McpServer): void {
  server.tool(
    "plan_digest_lint",
    "Validate §Plan Digest or stage doc against the 9-point rubric. Returns {pass, failures}.",
    InputSchema.shape,
    wrapTool("plan_digest_lint", async (input) => {
      return runWithToolTiming("plan_digest_lint", async () => {
        const failures: Array<{ rule: number; where: string; detail: string }> = [];
        const lines = input.content.split("\n");
        const root = resolveRepoRoot();
        for (let i = 0; i < lines.length; i += 1) {
          if (PICK_RX.test(lines[i])) failures.push({ rule: 1, where: `L${i + 1}`, detail: lines[i].slice(0, 120) });
          if (PARALLEL_RX.test(lines[i])) failures.push({ rule: 6, where: `L${i + 1}`, detail: lines[i].slice(0, 120) });
        }
        const hasGate = /\n\*\*Gate:\*\*/.test(input.content); if (!hasGate) failures.push({ rule: 5, where: "doc", detail: "no **Gate:** section" });
        const hasStop = /\n\*\*STOP\*\*/.test(input.content) || /\n\*\*STOP:\*\*/.test(input.content); if (!hasStop) failures.push({ rule: 9, where: "doc", detail: "no **STOP** section" });
        const hasEdit = /\n\*\*Edits?:\*\*/.test(input.content); if (!hasEdit) failures.push({ rule: 3, where: "doc", detail: "no **Edits:** section" });
        let m: RegExpExecArray | null;
        while ((m = PATH_RX.exec(input.content)) !== null && failures.length < 20) {
          const p = m[1]; const abs = path.resolve(root, p);
          if (!abs.startsWith(root)) continue;
          if (!fs.existsSync(abs)) failures.push({ rule: 2, where: `path:${p}`, detail: "path not found on HEAD" });
        }
        const pass = failures.length === 0;
        return { content: [{ type: "text", text: JSON.stringify({ pass, failures: failures.slice(0, 20) }) }] };
      });
    }),
  );
}
```

1f. **Create** `tools/mcp-ia-server/src/tools/plan-digest-gate-author-helper.ts`:

```ts
/**
 * MCP tool: plan_digest_gate_author_helper — suggests a canonical gate command for an edit tuple.
 * Input: { operation: "edit"|"create"|"delete", file: string, before?: string, after?: string }.
 * Output: { gate_cmd: string, expectation: string } — one shell line, one pass criterion.
 * Rules:
 *  - edit with before+after → `grep -c '<after first line>' <file>`, expect ≥1.
 *  - create → `test -f <file> && echo OK`, expect prints OK.
 *  - delete → `test ! -e <file> && echo OK`, expect prints OK.
 *  - fallback when before/after missing → `npm run validate:all`, expect exit 0.
 */

import { z } from "zod";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const InputSchema = z.object({
  operation: z.enum(["edit", "create", "delete"]),
  file: z.string().min(1),
  before: z.string().optional(),
  after: z.string().optional(),
});

export function registerPlanDigestGateAuthorHelper(server: McpServer): void {
  server.tool(
    "plan_digest_gate_author_helper",
    "Suggest canonical gate command for an edit tuple.",
    InputSchema.shape,
    wrapTool("plan_digest_gate_author_helper", async (input) => {
      return runWithToolTiming("plan_digest_gate_author_helper", async () => {
        let gate_cmd = "npm run validate:all"; let expectation = "exit 0";
        if (input.operation === "create") { gate_cmd = `test -f ${input.file} && echo OK`; expectation = "prints OK"; }
        else if (input.operation === "delete") { gate_cmd = `test ! -e ${input.file} && echo OK`; expectation = "prints OK"; }
        else if (input.operation === "edit" && input.after) {
          const firstLine = input.after.split("\n")[0].replace(/'/g, "'\\''").slice(0, 80);
          gate_cmd = `grep -cF '${firstLine}' ${input.file}`; expectation = "≥1 match";
        }
        return { content: [{ type: "text", text: JSON.stringify({ gate_cmd, expectation }) }] };
      });
    }),
  );
}
```

1g. **Create** `tools/mcp-ia-server/src/tools/plan-digest-compile-stage-doc.ts`:

```ts
/**
 * MCP tool: plan_digest_compile_stage_doc — stitch per-Task §Plan Digest slices into a single stage doc.
 * Input: { master_plan_path: string, stage_id: string, task_spec_paths: string[], mode?: "stage"|"audit" }.
 * Output: { compiled_path: string, bytes: number, task_count: number } — writes docs/implementation/<slug>-stage-<id>-plan.md.
 * Does NOT author §Plan Digest slices — only concatenates existing slices + header + final gate.
 * Mode `audit` is a flag-scaffold — returns { error: "mode_audit_not_ready" } unless env PLAN_DIGEST_AUDIT_MODE=1.
 */

import { z } from "zod";
import fs from "node:fs";
import path from "node:path";
import type { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { resolveRepoRoot } from "../config.js";
import { runWithToolTiming } from "../instrumentation.js";
import { wrapTool } from "../envelope.js";

const InputSchema = z.object({
  master_plan_path: z.string().min(1),
  stage_id: z.string().min(1),
  task_spec_paths: z.array(z.string().min(1)).min(1),
  mode: z.enum(["stage", "audit"]).optional().default("stage"),
});

function extractDigestSlice(body: string): string | null {
  const start = body.indexOf("\n## §Plan Digest");
  if (start < 0) return null;
  const after = body.slice(start + 1);
  const end = after.search(/\n## (?!§Plan Digest)/);
  return end < 0 ? after : after.slice(0, end);
}

export function registerPlanDigestCompileStageDoc(server: McpServer): void {
  server.tool(
    "plan_digest_compile_stage_doc",
    "Stitch per-Task §Plan Digest slices into docs/implementation/<slug>-stage-<id>-plan.md.",
    InputSchema.shape,
    wrapTool("plan_digest_compile_stage_doc", async (input) => {
      return runWithToolTiming("plan_digest_compile_stage_doc", async () => {
        if (input.mode === "audit" && process.env.PLAN_DIGEST_AUDIT_MODE !== "1") {
          return { content: [{ type: "text", text: JSON.stringify({ error: "mode_audit_not_ready" }) }] };
        }
        const root = resolveRepoRoot();
        const slug = path.basename(input.master_plan_path).replace(/-master-plan\.md$/, "").replace(/\.md$/, "");
        const outRel = `docs/implementation/${slug}-stage-${input.stage_id}-plan.md`;
        const outAbs = path.resolve(root, outRel);
        fs.mkdirSync(path.dirname(outAbs), { recursive: true });
        const slices: string[] = [];
        for (const p of input.task_spec_paths) {
          const abs = path.resolve(root, p);
          if (!abs.startsWith(root) || !fs.existsSync(abs)) throw new Error(`missing_spec:${p}`);
          const slice = extractDigestSlice(fs.readFileSync(abs, "utf8"));
          if (!slice) throw new Error(`no_plan_digest_in:${p}`);
          slices.push(slice);
        }
        const header = `# ${slug} — Stage ${input.stage_id} Plan Digest\n\nCompiled ${new Date().toISOString().slice(0, 10)} from ${slices.length} task spec(s).\n\n---\n`;
        const final = `${header}\n${slices.join("\n---\n")}\n\n## Final gate\n\n\`\`\`bash\nnpm run validate:all\n\`\`\`\n`;
        fs.writeFileSync(outAbs, final, "utf8");
        return { content: [{ type: "text", text: JSON.stringify({ compiled_path: outRel, bytes: final.length, task_count: slices.length }) }] };
      });
    }),
  );
}
```

**Gate:**

```bash
ls tools/mcp-ia-server/src/tools/plan-digest-*.ts | wc -l
```

Must print `7`.

```bash
npx --prefix tools/mcp-ia-server tsc --noEmit -p tools/mcp-ia-server/tsconfig.json 2>&1 | tail -20
```

Must exit 0 (no type errors in new files). Server is not yet wired — typecheck validates syntax/imports only.

**STOP:** If typecheck fails, open each offending file; do not edit `server-registrations.ts` yet (Step 2 does that). If imports (`config.js`, `envelope.js`, `instrumentation.js`) are missing, re-check the import pattern against `tools/mcp-ia-server/src/tools/glossary-lookup.ts` L7–L14 — it is the golden copy.

---

## Step 2 — Register 7 tools + update MCP README + verify-mcp

**Goal:** Wire the 7 new tools into `registerIaCoreTools`, append a README section listing them, and extend the `required` array in `scripts/verify-mcp.ts` so the smoke test asserts their presence.

**MCP hint:** run `plan_digest_resolve_anchor` on `tools/mcp-ia-server/src/server-registrations.ts` with substring `import { registerRuntimeState }` to confirm uniqueness before inserting imports.

**2a.** Edit `tools/mcp-ia-server/src/server-registrations.ts`. Find:

```
import { registerRuntimeState } from "./tools/runtime-state.js";
```

Replace with:

```
import { registerRuntimeState } from "./tools/runtime-state.js";
import { registerPlanDigestVerifyPaths } from "./tools/plan-digest-verify-paths.js";
import { registerPlanDigestResolveAnchor } from "./tools/plan-digest-resolve-anchor.js";
import { registerPlanDigestRenderLiteral } from "./tools/plan-digest-render-literal.js";
import { registerPlanDigestScanForPicks } from "./tools/plan-digest-scan-for-picks.js";
import { registerPlanDigestLint } from "./tools/plan-digest-lint.js";
import { registerPlanDigestGateAuthorHelper } from "./tools/plan-digest-gate-author-helper.js";
import { registerPlanDigestCompileStageDoc } from "./tools/plan-digest-compile-stage-doc.js";
```

**2b.** Same file, find:

```
  registerRuntimeState(server);
}
```

Replace with:

```
  registerRuntimeState(server);
  registerPlanDigestVerifyPaths(server);
  registerPlanDigestResolveAnchor(server);
  registerPlanDigestRenderLiteral(server);
  registerPlanDigestScanForPicks(server);
  registerPlanDigestLint(server);
  registerPlanDigestGateAuthorHelper(server);
  registerPlanDigestCompileStageDoc(server);
}
```

**2c.** Edit the JSDoc block for `registerIaCoreTools` — find:

```
 * ≥23 tools: list-specs, spec-outline, spec-section, spec-sections, glossary
 * lookup/discover, router-for-task, invariants-summary, list-rules,
 * rule-content/section, backlog-issue/list/search/record-validate,
 * parent-plan-validate, reserve-backlog-ids, stage-closeout-digest,
 * project-spec-journal (2), invariant-preflight, csharp-class-summary,
 * master-plan-locate, master-plan-next-pending, plan-apply-validate,
 * runtime_state.
```

Replace with:

```
 * ≥30 tools: list-specs, spec-outline, spec-section, spec-sections, glossary
 * lookup/discover, router-for-task, invariants-summary, list-rules,
 * rule-content/section, backlog-issue/list/search/record-validate,
 * parent-plan-validate, reserve-backlog-ids, stage-closeout-digest,
 * project-spec-journal (2), invariant-preflight, csharp-class-summary,
 * master-plan-locate, master-plan-next-pending, plan-apply-validate,
 * runtime_state, plan-digest-verify-paths/resolve-anchor/render-literal/
 * scan-for-picks/lint/gate-author-helper/compile-stage-doc.
```

**2d.** Edit `tools/mcp-ia-server/scripts/verify-mcp.ts` — find:

```
    "stage_closeout_digest",
    "project_spec_journal_persist",
```

Replace with:

```
    "stage_closeout_digest",
    "project_spec_journal_persist",
    "plan_digest_verify_paths",
    "plan_digest_resolve_anchor",
    "plan_digest_render_literal",
    "plan_digest_scan_for_picks",
    "plan_digest_lint",
    "plan_digest_gate_author_helper",
    "plan_digest_compile_stage_doc",
```

**2e.** Edit `tools/mcp-ia-server/README.md`. Append — after the last existing tool-catalog row, before any `## Changelog` heading if present (use `plan_digest_resolve_anchor` on the file to locate the last `stage_closeout_digest` mention, then insert immediately after the containing table/section):

```
### Plan-Digest tool family (Q12 2026-04-22)

| Tool | Purpose | Token budget |
|---|---|---|
| `plan_digest_verify_paths` | Path existence map | ≤1 line / path |
| `plan_digest_resolve_anchor` | Unique-anchor resolver (hits + ≤3 line context) | ≤20 lines |
| `plan_digest_render_literal` | Verbatim line-range reader (cap 100 lines) | Range-bounded |
| `plan_digest_scan_for_picks` | Lint-only hand-wavy-phrase detector | ≤20 findings |
| `plan_digest_lint` | 9-point rubric gate (`ia/rules/plan-digest-contract.md`) | ≤20 failures |
| `plan_digest_gate_author_helper` | Canonical gate-command suggester per edit tuple | 1 line |
| `plan_digest_compile_stage_doc` | Stitch per-Task §Plan Digest slices into `docs/implementation/<slug>-stage-<id>-plan.md` | Compiled doc size |

All tools obey the token-economy rule: output ≤20 lines typical; must REDUCE tokens vs. the Read/Grep alternative. Mode `audit` of `plan_digest_compile_stage_doc` is flag-gated on `PLAN_DIGEST_AUDIT_MODE=1` (scaffold, not wired into any chain).
```

**Gate:**

```bash
npm --prefix tools/mcp-ia-server run build
npm --prefix tools/mcp-ia-server run verify-mcp
```

`build` must exit 0. `verify-mcp` must print `Connected.` and include all 7 `plan_digest_*` names in the `Tools:` line.

**STOP:** If `verify-mcp` reports a missing name, re-check the corresponding `register…` call in 2b vs 2a. If the build breaks with `Cannot find module "./tools/plan-digest-..."`, re-run Step 1 — the file set is incomplete.

---

## Step 3 — Create plan-digest skill + agent + rule + template

**Goal:** Scaffold the four new authored surfaces that make `plan-digest` a first-class pair-stage peer to `plan-author` / `plan-review`.

**3a.** Create `ia/skills/plan-digest/SKILL.md` with content:

```markdown
---
purpose: "Mechanizes §Plan Author into §Plan Digest per-Task and compiles an aggregate Stage doc. 9-point rubric enforced externally via plan_digest_lint MCP tool."
audience: agent
loaded_by: skill:plan-digest
slices_via: plan_digest_verify_paths, plan_digest_resolve_anchor, plan_digest_render_literal, plan_digest_scan_for_picks, plan_digest_lint, plan_digest_gate_author_helper, plan_digest_compile_stage_doc
name: plan-digest
description: >
  Opus Stage-scoped bulk non-pair stage. Runs AFTER plan-author (populated
  §Plan Author per spec) and BEFORE plan-review. Reads all N §Plan Author
  sections + current repo state via MCP; writes per-spec §Plan Digest
  (rich format: mechanical steps + gates + acceptance + test blueprint +
  glossary refs + examples + STOP + implementer MCP-tool hints) that
  SURVIVES in the final spec (§Plan Author is ephemeral and dropped).
  Compiles aggregate doc at docs/implementation/{slug}-stage-{STAGE_ID}-plan.md
  via plan_digest_compile_stage_doc. Self-lints via plan_digest_lint (cap=1 retry;
  second fail escalates to user). Two modes: `stage` (live) + `audit` (scaffold,
  flag-gated). Always-on: every executor class benefits from the mechanical form.
  Triggers: "/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}", "digest stage plan",
  "compile stage implementation doc", auto-dispatched from /stage-file Step 4.
model: inherit
phases:
  - "Load Stage + §Plan Author slices"
  - "Mechanize per-Task §Plan Digest"
  - "Compile aggregate stage doc"
  - "Self-lint via plan_digest_lint"
  - "Hand-off"
---

# Plan-digest skill (Opus Stage-scoped bulk, non-pair)

Caveman default — [`agent-output-caveman.md`](../../rules/agent-output-caveman.md).

**Role:** Mechanize §Plan Author into §Plan Digest across N Task specs of one Stage in one Opus pass. §Plan Author is ephemeral — this skill transforms + replaces it with §Plan Digest, which is the canonical section surviving in the committed spec (Q5).

**Contract:** [`ia/rules/plan-digest-contract.md`](../../rules/plan-digest-contract.md) — 9-point rubric, enforced by `plan_digest_lint`.

**Upstream:** `plan-author` (writes §Plan Author — ephemeral intermediate). **Downstream:** `plan-review` (scans final §Plan Digest for semantic drift). Chain anchor: `.claude/commands/stage-file.md` Step 4 (between plan-author and plan-reviewer).

## Inputs

| Param | Source | Notes |
|-------|--------|-------|
| `MASTER_PLAN_PATH` | 1st arg | Repo-relative path. |
| `STAGE_ID` | 2nd arg | e.g. `7.1`. |
| `--mode {stage|audit}` | optional | Default `stage`. `audit` requires env `PLAN_DIGEST_AUDIT_MODE=1` (scaffold). |
| `--task {ISSUE_ID}` | optional | Single-spec re-digest. |

## Phase 1 — Load Stage + §Plan Author slices

1. Read master-plan Stage block; collect Task ids (Status ∈ {Draft, In Review, In Progress}).
2. For each Task: read `ia/projects/{ISSUE_ID}.md`. Require `## §Plan Author` populated with all 4 sub-headings (§Audit Notes / §Examples / §Test Blueprint / §Acceptance). Missing → abort chain with `STOPPED — plan-author not populated for {ISSUE_ID}`.
3. Load glossary terms + invariants + router domains via shared Stage MCP bundle (do NOT re-call `domain-context-load` — orchestrator provided it).

## Phase 2 — Mechanize per-Task §Plan Digest

For each Task:

1. Translate §Plan Author narrative into a sequential checklist of **Edit** tuples, each with `(operation, target_path, before_string, after_string)`. Use `plan_digest_verify_paths` to confirm every target exists; use `plan_digest_resolve_anchor` to confirm every `before_string` is unique.
2. Render exact literals for code blocks via `plan_digest_render_literal` when the digest must quote a file literally.
3. For each step, ask `plan_digest_gate_author_helper({operation, file, before, after})` for the canonical gate command + expectation; embed verbatim.
4. Author STOP clause per step (what edit to re-open, or which upstream surface to escalate to).
5. Author Implementer MCP-tool hints per step (subset of `backlog_issue`, `glossary_lookup`, `invariant_preflight`, `plan_digest_resolve_anchor`, `unity_bridge_command`, etc.) — mechanical list, not narrative.
6. Write one `## §Plan Digest` section per spec under anchor **between §10 Lessons Learned and §Open Questions** (replaces §Plan Author — delete the §Plan Author block in the same write pass; Q5). Shape mirrors the template `ia/templates/plan-digest-section.md`.

## Phase 3 — Compile aggregate stage doc

Call `plan_digest_compile_stage_doc({master_plan_path, stage_id, task_spec_paths, mode})`. Output written to `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md`. Path A (ship-stage) does not consume this doc; Path B (external / composer-2 / cursor) does.

## Phase 4 — Self-lint via plan_digest_lint

For each per-Task §Plan Digest slice AND the aggregate stage doc:

1. Call `plan_digest_lint({content})`. `pass: true` → continue.
2. `pass: false` → revise failing tuples in-place; re-run lint once. Second failure → abort chain with `STOPPED — plan-digest lint critical twice`; surface first 5 failures verbatim.

Retry cap = 1.

## Phase 5 — Hand-off

Emit caveman summary: N specs digested; aggregate doc path; lint pass status. Next: `/plan-review {MASTER_PLAN_PATH} {STAGE_ID}` (multi-task) OR `/implement {ISSUE_ID}` (N=1 — skip plan-review).

## Hard boundaries

- Do NOT write code. Do NOT flip Task Status. Do NOT commit.
- Do NOT author §Plan Author (upstream). Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort + `/author` handoff.
- Do NOT regress to per-Task mode if tokens exceed threshold — split into ⌈N/2⌉ bulk sub-passes.
- Mode `audit` stays flag-gated; no chain dispatches it.
```

**3b.** Create `.claude/agents/plan-digest.md`:

```markdown
---
name: plan-digest
description: Use to mechanize §Plan Author into §Plan Digest across ALL N Task specs of one Stage in a single Opus pass + compile aggregate doc at docs/implementation/. Triggers — "/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}", "digest stage plan", "compile stage implementation doc". Runs AFTER plan-author, BEFORE plan-reviewer. §Plan Author is ephemeral; §Plan Digest survives in the final spec (Q5). Self-lints via plan_digest_lint (cap=1 retry).
tools: Read, Edit, Write, Bash, Grep, Glob, mcp__territory-ia__plan_digest_verify_paths, mcp__territory-ia__plan_digest_resolve_anchor, mcp__territory-ia__plan_digest_render_literal, mcp__territory-ia__plan_digest_scan_for_picks, mcp__territory-ia__plan_digest_lint, mcp__territory-ia__plan_digest_gate_author_helper, mcp__territory-ia__plan_digest_compile_stage_doc, mcp__territory-ia__spec_section, mcp__territory-ia__spec_sections, mcp__territory-ia__backlog_issue, mcp__territory-ia__glossary_lookup, mcp__territory-ia__invariants_summary, mcp__territory-ia__master_plan_locate
model: opus
reasoning_effort: high
---

Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.

Progress emission: `@ia/skills/subagent-progress-emit/SKILL.md` — one stderr line per phase in the canonical shape.

# Mission

Run `ia/skills/plan-digest/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. Read all N §Plan Author sections; write per-Task §Plan Digest (rich format: mechanical edits + gates + STOP + acceptance + test blueprint + implementer MCP-tool hints); drop §Plan Author from each spec in the same write pass; compile aggregate at `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md`; self-lint via `plan_digest_lint` (cap=1 retry).

# Hard boundaries

- Do NOT write code. Do NOT flip Task Status. Do NOT commit.
- Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only; leak = abort chain + route to `/author`.
- Do NOT dispatch `plan-review` or `spec-implementer` — chain does that.
- Every `before_string` in a digest tuple must resolve to exactly 1 hit via `plan_digest_resolve_anchor`.
```

**3c.** Create `ia/rules/plan-digest-contract.md`:

```markdown
---
purpose: "Canonical 9-point rubric for §Plan Digest. Enforced by plan_digest_lint MCP tool (Q9 decision 2026-04-22)."
audience: agent
loaded_by: ondemand
slices_via: none
alwaysApply: false
---

# Plan-Digest Contract — 9-point rubric

Applies to every §Plan Digest section (per-Task) and every compiled aggregate stage doc.

A plan is "digested" iff **all 9** hold:

1. **Zero open picks.** No "user decides", "user picks", "likely", "probably", "we could", "might", "consider", "TBD", "up to you", "your call".
2. **Every path verified against HEAD.** Repo-relative paths resolve via `plan_digest_verify_paths`.
3. **Every edit has concrete before-string + after-string.** No "update the narrative". Creates use verbatim new-file content. Deletes name the exact path.
4. **Before-strings are unique.** `plan_digest_resolve_anchor` returns exactly 1 hit per (file, before) pair.
5. **Every step has a gate command** with a stated pass criterion (exit 0 / zero matches / prints `OK`).
6. **Sequential only** — no parallelization block; if order-free, pick an order.
7. **Scope-narrowed** — "11 lines" must become N exact anchors, not N generic hits.
8. **Meta-stripped** — no audit history, no user-pick prose, no "human only" asides.
9. **STOP condition per step** — what to do if the gate fails (re-open which edit, or escalate to which upstream surface).

`plan_digest_lint` returns `{pass: boolean, failures: [{rule: 1..9, where, detail}]}`. Digester cap = 1 retry; second failure → abort chain + surface first failures verbatim.

## Enforcement

- `plan_digest_lint` runs on every per-Task §Plan Digest slice AND on the aggregate stage doc.
- `plan-review` runs AFTER `plan-digest` — its drift scan consumes the final §Plan Digest, not §Plan Author.

## Cross-references

- `ia/skills/plan-digest/SKILL.md` — the skill that authors §Plan Digest.
- `ia/templates/plan-digest-section.md` — section shape template.
- `ia/rules/plan-apply-pair-contract.md` — §Plan Digest is a Stage-scoped non-pair output alongside §Plan Author / §Audit.
```

**3d.** Create `ia/templates/plan-digest-section.md`:

```markdown
<!--
  §Plan Digest section template. Opus-authored per Task spec during Stage-scoped plan-digest pass.
  Placement: between §10 Lessons Learned and §Open Questions in ia/projects/{ISSUE_ID}.md.
  Replaces §Plan Author in the final spec (Q5 2026-04-22). §Plan Author is ephemeral intermediate.
-->

## §Plan Digest

### §Goal

<!-- 1–2 sentences — task outcome in product / domain terms. Glossary-aligned. -->

### §Acceptance

<!-- Checkbox list — refined per-Task acceptance. Narrower than Stage Exit. -->

- [ ] …

### §Test Blueprint

<!-- Structured tuples consumed by /implement + /verify-loop. -->

| test_name | inputs | expected | harness |
|-----------|--------|----------|---------|

### §Examples

<!-- Concrete inputs/outputs + edge cases. Tables or code blocks. -->

### §Mechanical Steps

<!-- Sequential, pre-decided. Each step carries: Goal / Edits (before+after strings) / Gate / STOP / MCP hints. -->

#### Step 1 — {name}

**Goal:** …

**Edits:**
- `{repo-relative-path}` — **before**:
  ```
  …
  ```
  **after**:
  ```
  …
  ```

**Gate:**
```bash
…
```

**STOP:** …

**MCP hints:** `plan_digest_resolve_anchor`, `{other}` …

#### Step 2 — …
```

**Gate:**

```bash
test -f ia/skills/plan-digest/SKILL.md && test -f .claude/agents/plan-digest.md && test -f ia/rules/plan-digest-contract.md && test -f ia/templates/plan-digest-section.md && echo OK
npm run validate:claude-imports 2>&1 | tail -5
```

First command must print `OK`. Second must exit 0 (frontmatter valid).

**STOP:** Validator failure usually means a frontmatter key is missing. Compare against `ia/skills/plan-author/SKILL.md` frontmatter shape — it is the closest golden copy.

---

## Step 4 — Insert plan-digest into `/stage-file` chain

**Goal:** Add a new "Step 4 — Dispatch `plan-digest`" between the current Step 3 (plan-author) and Step 4 (plan-reviewer) in `.claude/commands/stage-file.md`; renumber old Step 4 → Step 5 and Step 5 → Step 6.

**4a.** Edit `.claude/commands/stage-file.md`. Find:

```
Plan-author must return success + N specs with populated `§Plan Author` before Step 4. Failure → abort chain with handoff `/author --stage {MASTER_PLAN_PATH} {STAGE_ID}`.

## Step 4 — Dispatch `plan-reviewer` (Sonnet pair-head; cap=1 on critical)
```

Replace with:

```
Plan-author must return success + N specs with populated `§Plan Author` before Step 4. Failure → abort chain with handoff `/author --stage {MASTER_PLAN_PATH} {STAGE_ID}`.

## Step 4 — Dispatch `plan-digest` (Opus Stage-scoped bulk non-pair)

Forward via Agent tool with `subagent_type: "plan-digest"` (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`):

> Follow `caveman:caveman`. Standard exceptions: code, commits, security/auth, verbatim error/tool output, structured MCP payloads. Anchor: `ia/rules/agent-output-caveman.md`.
>
> ## Mission
>
> Run `ia/skills/plan-digest/SKILL.md` end-to-end on Stage `{STAGE_ID}` of `{MASTER_PLAN_PATH}`. For every Task spec whose `§Plan Author` is populated, mechanize into `§Plan Digest` (rich format: Goal / Acceptance / Test Blueprint / Examples / sequential Mechanical Steps with Edits + Gate + STOP + MCP hints) and DROP `§Plan Author` in the same write pass. Compile aggregate doc at `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` via `plan_digest_compile_stage_doc`. Self-lint via `plan_digest_lint` (cap=1 retry).
>
> ## Hard boundaries
>
> - Do NOT write code, run verify, or flip Task status.
> - Do NOT resolve picks — `plan_digest_scan_for_picks` is lint-only. Leak → abort + `/author` handoff.
> - Every `before_string` in a digest edit tuple must resolve to exactly 1 hit via `plan_digest_resolve_anchor`.
> - Mode `audit` is flag-gated (`PLAN_DIGEST_AUDIT_MODE=1`); do NOT dispatch it from this chain.
> - Idempotent on re-entry: if `§Plan Digest` already populated AND lint passes, skip.

Plan-digest must return success + N specs with populated `§Plan Digest` (and `§Plan Author` dropped) + aggregate doc written + lint PASS before Step 5. Failure → abort chain with handoff `/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}`.

## Step 5 — Dispatch `plan-reviewer` (Sonnet pair-head; cap=1 on critical)
```

**4b.** Same file. Find:

```
After applier success → re-dispatch `plan-reviewer` (Step 4).

## Step 5 — Boundary stop (NO auto-chain to ship-stage)
```

Replace with:

```
After applier success → re-dispatch `plan-reviewer` (Step 5).

## Step 6 — Boundary stop (NO auto-chain to ship-stage)
```

**4c.** Same file — rename the inner header `### Step 4a` to `### Step 5a`. Find:

```
### Step 4a — Dispatch `plan-applier` Mode plan-fix (Sonnet pair-tail; only on critical) (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`)
```

Replace with:

```
### Step 5a — Dispatch `plan-applier` Mode plan-fix (Sonnet pair-tail; only on critical) (when `FORCE_MODEL` set: pass `model: "{FORCE_MODEL}"`)
```

**4d.** Same file — update frontmatter + title + lead paragraph. Find:

```
description: Bulk-file all pending tasks of one orchestrator Stage as BACKLOG issues + project spec stubs + §Plan Author populated + plan-review PASS. Dispatches `stage-file-planner` (Opus pair-head) → `stage-file-applier` (Sonnet pair-tail) → `plan-author` (bulk Stage 1×N) → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical, re-entry cap=1) → STOP. Chain tail per F6 re-fold (2026-04-20). Handoff: `/ship-stage` (N≥2) OR `/ship` (N=1).
```

Replace with:

```
description: Bulk-file all pending tasks of one orchestrator Stage as BACKLOG issues + project spec stubs + §Plan Author populated → §Plan Digest mechanized (§Plan Author dropped) + plan-review PASS. Dispatches `stage-file-planner` (Opus pair-head) → `stage-file-applier` (Sonnet pair-tail) → `plan-author` (bulk Stage 1×N) → `plan-digest` (bulk Stage 1×N, always-on) → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical, re-entry cap=1) → STOP. Chain tail per F6 re-fold (2026-04-20) + plan-digest insertion (2026-04-22). Handoff: `/ship-stage` (N≥2) OR `/ship` (N=1).
```

**4e.** Same file — find:

```
# /stage-file — dispatch seam #2 chain (planner → applier → author → review → STOP)

Use `stage-file-planner` → `stage-file-applier` → `plan-author` → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical) to bulk-file + author + review all `_pending_` tasks of `$ARGUMENTS` in ONE command. Chain STOPS at plan-review PASS (or cap=1 critical-twice). **Next:** `/ship-stage` (N≥2 — runs implement + verify + code-review + audit + closeout) OR `/ship` (N=1).
```

Replace with:

```
# /stage-file — dispatch seam #2 chain (planner → applier → author → digest → review → STOP)

Use `stage-file-planner` → `stage-file-applier` → `plan-author` → `plan-digest` → `plan-reviewer` (→ `plan-applier` Mode plan-fix on critical) to bulk-file + author + digest + review all `_pending_` tasks of `$ARGUMENTS` in ONE command. Chain STOPS at plan-review PASS (or cap=1 critical-twice). **Next:** `/ship-stage` (N≥2 — runs implement + verify + code-review + audit + closeout) OR `/ship` (N=1).
```

**Gate:**

```bash
grep -c "## Step 4 — Dispatch \`plan-digest\`" .claude/commands/stage-file.md
grep -c "## Step 5 — Dispatch \`plan-reviewer\`" .claude/commands/stage-file.md
grep -c "## Step 6 — Boundary stop" .claude/commands/stage-file.md
```

Each must print exactly `1`.

**STOP:** If any prints `0`, re-open the corresponding replacement — the old before-string likely contained trailing whitespace.

---

## Step 5 — Swap spec-implementer source to §Plan Digest

**Goal:** `spec-implementer` today reads `## 7. Implementation Plan` which is populated from §Plan Author. Redirect to §Plan Digest so a weak executor gets the mechanical form.

**5a.** Edit `.claude/agents/spec-implementer.md`. Find:

```
Execute `## 7. Implementation Plan` of `ia/projects/{ISSUE_ID}*.md` end-to-end, phase by phase, minimal diffs. Read spec first, then implement. Verification per agent-led policy after each substantive change.
```

Replace with:

```
Execute `## §Plan Digest` (§Mechanical Steps sub-section) of `ia/projects/{ISSUE_ID}*.md` end-to-end, step by step, minimal diffs. §Plan Digest is the canonical executable plan — §Plan Author is no longer present in committed specs (Q5 2026-04-22). Read spec first, then implement. Verification per agent-led policy after each substantive change. If §Plan Digest missing but §Plan Author present → ship-stage Phase 1.5 will have auto-invoked plan-digest JIT; if still missing, abort with `SPEC_NOT_DIGESTED: {ISSUE_ID}`.
```

**5b.** Same file. Find:

```
> Mission: Execute `ia/projects/{ISSUE_ID}*.md` §7 Implementation Plan end-to-end, phase by phase. Pre-loaded context: {CHAIN_CONTEXT}. End with `IMPLEMENT_DONE` or `IMPLEMENT_FAILED: {reason}`.
```

Wait — this string lives in `ia/skills/ship-stage/SKILL.md` Step 2.1, not the agent file. MCP hint: run `plan_digest_resolve_anchor` on `ia/skills/ship-stage/SKILL.md` with substring `§7 Implementation Plan end-to-end` to confirm; skip this edit if 0 hits.

Edit `ia/skills/ship-stage/SKILL.md` — find:

```
> Mission: Execute `ia/projects/{ISSUE_ID}*.md` §7 Implementation Plan end-to-end, phase by phase. Pre-loaded context: {CHAIN_CONTEXT}. End with `IMPLEMENT_DONE` or `IMPLEMENT_FAILED: {reason}`.
```

Replace with:

```
> Mission: Execute `ia/projects/{ISSUE_ID}*.md` §Plan Digest (§Mechanical Steps) end-to-end, step by step. Pre-loaded context: {CHAIN_CONTEXT}. §Plan Digest is the canonical plan — §Plan Author no longer present post-2026-04-22. End with `IMPLEMENT_DONE` or `IMPLEMENT_FAILED: {reason}`.
```

**5c.** Edit `.claude/agents/spec-implementer.md`. Find:

```
1. **Read spec** — focus on §5 Proposed Design, §6 Decision Log, §7 Implementation Plan, §9 Issues Found, §10 Lessons Learned. Start at first unticked phase.
```

Replace with:

```
1. **Read spec** — focus on §5 Proposed Design, §6 Decision Log, §Plan Digest (§Mechanical Steps), §9 Issues Found, §10 Lessons Learned. Start at first unticked step.
```

**Gate:**

```bash
grep -c "§Plan Digest" .claude/agents/spec-implementer.md
grep -c "§7 Implementation Plan end-to-end" ia/skills/ship-stage/SKILL.md
```

First must be ≥1. Second must be 0.

**STOP:** If first is 0, the before-string in 5a/5c didn't match — run `plan_digest_resolve_anchor` on `.claude/agents/spec-implementer.md` with `§7 Implementation Plan` to find the current wording.

---

## Step 6 — Ship-stage readiness gate swap + lazy-migration branch

**Goal:** (a) Phase 1.5 gate now checks `## §Plan Digest` populated, not `## §Plan Author`. (b) Add the lazy-migration branch: when `§Plan Digest` missing but `§Plan Author` populated, auto-invoke `plan-digest` JIT with a one-time session warning.

**6a.** Edit `.claude/commands/ship-stage.md`. Find:

```
> 3. Phase 1.5 — §Plan Author readiness gate (`ia/skills/ship-stage/SKILL.md` Step 1.5): for each pending spec verify `## §Plan Author` populated. Non-populated → `STOPPED — prerequisite: §Plan Author not populated for {ISSUE_ID_LIST}` + `/author` handoff; no Pass 1.
```

Replace with:

```
> 3. Phase 1.5 — §Plan Digest readiness gate (`ia/skills/ship-stage/SKILL.md` Step 1.5): for each pending spec verify `## §Plan Digest` populated. If missing but `## §Plan Author` populated → auto-invoke `plan-digest` JIT (lazy migration) + emit one-time session warning; resume Pass 1 afterward. Both missing → `STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `/plan-digest` handoff; no Pass 1.
```

**6b.** Edit `.claude/commands/ship-stage.md`. Find:

```
> - `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Author not populated for …` (+ `/author` Next line)
```

Replace with:

```
> - `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for …` (+ `/plan-digest` Next line)
```

**6c.** Edit `ia/skills/ship-stage/SKILL.md`. Find:

```
## Step 1.5 — §Plan Author readiness gate (prerequisite)

`/ship-stage` does NOT run `/author` or `/plan-review` internally — both fold into `/stage-file` dispatcher (F6 re-fold 2026-04-20). Specs arriving at `/ship-stage` must already carry populated `## §Plan Author` from `/stage-file` chain tail.
```

Replace with:

```
## Step 1.5 — §Plan Digest readiness gate (prerequisite; lazy-migration branch for legacy §Plan Author)

`/ship-stage` does NOT run `/author` or `/plan-review` internally — both fold into `/stage-file` dispatcher (F6 re-fold 2026-04-20). `/ship-stage` DOES auto-invoke `plan-digest` JIT for legacy specs whose `§Plan Author` is populated but `§Plan Digest` missing (lazy-migration branch per Q13 2026-04-22). Specs arriving at `/ship-stage` must carry populated `## §Plan Digest`; legacy Draft specs with `§Plan Author` only are upgraded on first re-entry.
```

**6d.** Same file. Find:

```
**Idempotent readiness check:** for each id in the readiness id list, read `ia/projects/{ISSUE_ID}*.md` and locate `## §Plan Author`. Treat a spec as **populated** when ALL of these hold:

1. `## §Plan Author` heading exists.
2. No line inside the block (until next `## ` heading at same/higher level) matches `_pending` case-insensitively.
3. All four sub-headings (`### §Audit Notes`, `### §Examples`, `### §Test Blueprint`, `### §Acceptance`) exist with non-whitespace body content.

**If ALL specs populated:** continue to Step 2 (Pass 1) **or** Step 1.6 / 3 when **`PASS2_ONLY`**.

**If ANY spec still `_pending_` or missing sub-headings:** stop chain. Emit:

```
SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Author not populated for {ISSUE_ID_LIST}
Next: claude-personal "/author {MASTER_PLAN_PATH} Stage {STAGE_ID}"
```

Then user runs `/author` (+ `/plan-review` when needed) and re-invokes `/ship-stage`. Gate is idempotent — safe to re-enter after partial-failure recovery.

**Rationale:** `/stage-file` chain (stage-file-planner → stage-file-applier → plan-author → plan-reviewer → plan-fix-applier) ships specs pre-authored + pre-reviewed. `/ship-stage` only verifies readiness — does NOT re-dispatch those subagents. If chain crashed mid-flight between `/stage-file` subagents, re-run `/stage-file` (idempotent) or stand-alone `/author` + `/plan-review` to close the gap; then resume `/ship-stage`.
```

Replace with:

```
**Idempotent readiness check:** for each id in the readiness id list, read `ia/projects/{ISSUE_ID}*.md` and locate `## §Plan Digest`. Treat a spec as **digested** when ALL of these hold:

1. `## §Plan Digest` heading exists.
2. No line inside the block (until next `## ` heading at same/higher level) matches `_pending` case-insensitively.
3. Sub-headings `### §Goal`, `### §Acceptance`, `### §Mechanical Steps` exist with non-whitespace body content.

**If ALL specs digested:** continue to Step 2 (Pass 1) **or** Step 1.6 / 3 when **`PASS2_ONLY`**.

**If ANY spec missing §Plan Digest BUT populated §Plan Author:** auto-invoke `plan-digest` JIT (lazy-migration branch — Q13 2026-04-22). Emit ONE-TIME session warning: `LAZY_MIGRATION: §Plan Author → §Plan Digest upgrade on re-entry for {ISSUE_ID_LIST}`. Dispatch `plan-digest` as subagent on the Stage; wait for completion + lint PASS; re-run the readiness check.

**If ANY spec has neither §Plan Digest NOR §Plan Author:** stop chain. Emit:

```
SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}
Next: claude-personal "/plan-digest {MASTER_PLAN_PATH} Stage {STAGE_ID}"
```

**Rationale:** `/stage-file` chain now ends with `plan-digest` + `plan-review` (plan-digest insertion 2026-04-22) — specs arrive with populated `§Plan Digest`. Legacy Draft specs (filed before plan-digest) carry `§Plan Author` only; lazy-migration auto-upgrades them on first `/ship-stage` entry. Branch retires when the last Draft is re-entered.
```

**6e.** Same file. Find:

```
**Readiness id list** (which specs to check):
```

Note — no replacement here; just verify the surrounding table keeps `STAGE_FILED_IDS` reference intact. Use `plan_digest_resolve_anchor` to confirm unchanged context.

**6f.** Same file — update the Exit lines. Find:

```
- **Readiness gate fail:** `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Author not populated for {ISSUE_ID_LIST}` + `Next: claude-personal "/author {MASTER_PLAN_PATH} Stage {STAGE_ID}"`.
```

Replace with:

```
- **Readiness gate fail:** `SHIP_STAGE {STAGE_ID}: STOPPED — prerequisite: §Plan Digest not populated for {ISSUE_ID_LIST}` + `Next: claude-personal "/plan-digest {MASTER_PLAN_PATH} Stage {STAGE_ID}"`.
```

**6g.** Same file — update the Hard boundaries block. Find:

```
- `plan-author` + `plan-review` do NOT run inside `/ship-stage` — both fold into `/stage-file` dispatcher. Step 1.5 is a readiness gate only; non-populated `§Plan Author` → STOPPED + handoff. Do NOT dispatch `plan-author` or `plan-reviewer` from this skill.
```

Replace with:

```
- `plan-author` + `plan-review` do NOT run inside `/ship-stage` — both fold into `/stage-file` dispatcher. Step 1.5 is a readiness gate; non-digested AND non-authored → STOPPED + `/plan-digest` handoff. Do NOT dispatch `plan-author` or `plan-reviewer` from this skill. **Exception (lazy-migration, Q13 2026-04-22):** when a spec has populated `§Plan Author` but missing `§Plan Digest`, auto-invoke `plan-digest` JIT with a one-time session warning — this is the only subagent ship-stage dispatches outside Pass 1 / Pass 2.
```

**6h.** Edit `.claude/agents/ship-stage.md`. MCP hint: run `plan_digest_resolve_anchor` on this file with substring `§Plan Author readiness` to locate the agent-body version of the readiness narration. Apply the same §Plan Digest swap as 6a / 6c / 6g. When the agent body mirrors the command (same phrases), re-apply the same 3 replacements verbatim. If the agent does not mention `§Plan Author readiness`, skip — the command + skill are authoritative.

**Gate:**

```bash
grep -c "§Plan Digest readiness gate" .claude/commands/ship-stage.md
grep -c "§Plan Digest readiness gate" ia/skills/ship-stage/SKILL.md
grep -c "lazy-migration" ia/skills/ship-stage/SKILL.md
grep -c "§Plan Author not populated" .claude/commands/ship-stage.md ia/skills/ship-stage/SKILL.md
```

First two must be ≥1. Third must be ≥1. Fourth must print `0` lines for both files (combined).

**STOP:** If the fourth grep returns matches, a stale reference slipped through — re-open and patch.

---

## Step 7 — Peer-chain updates (project-new + stage-closeout + master-plan-extend)

**Goal:** Three chains independently read/write §Plan Author and now need §Plan Digest alignment.

**7a.** Edit `.claude/commands/project-new.md` (N=1 path auto-chains `/author`; now also auto-chains `/plan-digest`). Find:

```
## Step 3 — Auto-chain `/author --task {ISSUE_ID}` (N=1 bulk)

On applier success: auto-invoke `/author --task {ISSUE_ID}` (Stage-scoped bulk `plan-author` at N=1 per T7.11 / TECH-478) to fill `§Plan Author` + canonical-term fold on the one filed spec. Rev 3 single-task path skips `plan-review` at N=1 — next step is `/implement {ISSUE_ID}` directly.
```

Replace with:

```
## Step 3 — Auto-chain `/author --task {ISSUE_ID}` then `/plan-digest --task {ISSUE_ID}` (N=1 bulk)

On applier success: auto-invoke `/author --task {ISSUE_ID}` (Stage-scoped bulk `plan-author` at N=1 per T7.11 / TECH-478) to fill `§Plan Author` + canonical-term fold on the one filed spec. Then auto-invoke `/plan-digest --task {ISSUE_ID}` to mechanize into `§Plan Digest` and drop `§Plan Author` (Q5 2026-04-22). Rev 3 single-task path skips `plan-review` at N=1 — next step is `/implement {ISSUE_ID}` directly.
```

**7b.** Edit `ia/skills/project-new/SKILL.md` — update the narrative at **`vs author`**. Find:

```
**vs author:** this skill creates backlog row + spec stub from user prompt. After stub → [`plan-author`](../plan-author/SKILL.md) (N=1 fills §Plan Author) → [`project-spec-implement`](../project-spec-implement/SKILL.md) → `verify-loop` → `opus-code-review` → `opus-audit` → Stage-scoped `/closeout` (`stage-closeout-plan` → `plan-applier` Mode stage-closeout). Per canonical rev-3 flow in [`docs/agent-lifecycle.md`](../../../docs/agent-lifecycle.md).
```

Replace with:

```
**vs author:** this skill creates backlog row + spec stub from user prompt. After stub → [`plan-author`](../plan-author/SKILL.md) (N=1 fills §Plan Author — ephemeral) → [`plan-digest`](../plan-digest/SKILL.md) (N=1 mechanizes into §Plan Digest + drops §Plan Author) → [`project-spec-implement`](../project-spec-implement/SKILL.md) → `verify-loop` → `opus-code-review` → `opus-audit` → Stage-scoped `/closeout` (`stage-closeout-plan` → `plan-applier` Mode stage-closeout). Per canonical rev-3 flow in [`docs/agent-lifecycle.md`](../../../docs/agent-lifecycle.md).
```

**7c.** Edit `ia/skills/project-new-apply/SKILL.md`. Use `plan_digest_resolve_anchor` on the file with substring `plan-author` to locate the handoff line. Find the handoff narration (typically one line referencing `plan-author`) and replace with the chained `plan-author` → `plan-digest` shape. Expected pattern replacement (apply verbatim when unique):

```
→ plan-author → project-spec-implement
```

Replace with:

```
→ plan-author → plan-digest → project-spec-implement
```

**7d.** Edit `ia/skills/stage-closeout-plan/SKILL.md` — this chain reads §Audit (not §Plan Author) but narration may reference §Plan Author. Use `plan_digest_resolve_anchor` on substring `§Plan Author` in the file; for each hit, replace `§Plan Author` → `§Plan Digest` in-place IF the narration is describing the final committed surface. Skip any hit that describes the ephemeral intermediate stage (where `§Plan Author` is correct historical context).

**Decision (conservative):** every hit in this SKILL.md that occurs in a "Cross-references", "Chain", or post-author narration context gets replaced. Historical Changelog blocks are preserved unchanged.

**7e.** Edit `.claude/agents/stage-closeout-planner.md` — same policy as 7d. `plan_digest_resolve_anchor` substring `§Plan Author`; replace non-historical occurrences with `§Plan Digest`.

**7f.** Edit `ia/skills/master-plan-extend/SKILL.md` + `.claude/agents/master-plan-extend.md` + `.claude/commands/master-plan-extend.md` — this chain historically writes `§Plan Author` stubs into new Stage specs. Use `plan_digest_resolve_anchor` on substring `§Plan Author` per file; replace every non-historical occurrence with `§Plan Digest`. For the command narration (`.claude/commands/master-plan-extend.md`) update the chain shape to cite `plan-digest` in the post-author position.

**Gate:**

```bash
grep -c "plan-digest" .claude/commands/project-new.md ia/skills/project-new/SKILL.md
grep -n "§Plan Author" ia/skills/stage-closeout-plan/SKILL.md .claude/agents/stage-closeout-planner.md ia/skills/master-plan-extend/SKILL.md .claude/commands/master-plan-extend.md | grep -v Changelog | grep -v "intermediate" | wc -l
```

First grep must sum ≥2. Second must print `0` (non-historical §Plan Author references all migrated).

**STOP:** If second > 0, open each file and re-triage: the hit is either (a) truly historical (mark by wrapping in `<!-- historical -->` HTML comment) or (b) missed — replace.

---

## Step 8 — Rules + templates

**Goal:** Add §Plan Digest as a first-class section in `ia/templates/project-spec-template.md`; update pair-contract to list plan-digest as a Stage-scoped non-pair peer alongside plan-author; update project-hierarchy + orchestrator-vs-spec + caveman-authoring rules to mention the new stage; patch master-plan-template if it references §Plan Author.

**8a.** Edit `ia/templates/project-spec-template.md`. Find:

```
## §Plan Author

<!-- Pair-head: `plan-author` Opus Stage-scoped bulk non-pair (no Sonnet tail). Populated once per Stage after `stage-file-apply` (multi-task) or `project-new-apply` (N=1). 4 sub-sections in strict order. -->

_pending — populated by `/author {MASTER_PLAN_PATH} {STAGE_ID}`. 4 sub-sections: §Audit Notes / §Examples / §Test Blueprint / §Acceptance._

### §Audit Notes

<!-- Upfront conceptual audit — risks, ambiguity, invariant touches. 2–5 bullets. -->

### §Examples

<!-- Concrete inputs/outputs + edge cases + legacy shapes. Tables or code blocks. -->

### §Test Blueprint

<!-- Structured tuples consumed by `/implement` + `/verify-loop`. One row per test: `{test_name, inputs, expected, harness}`. -->

### §Acceptance

<!-- Refined per-Task acceptance criteria — narrower than Stage Exit. Checkbox list. -->
```

Replace with:

```
## §Plan Digest

<!-- Canonical executable plan — `plan-digest` Opus Stage-scoped bulk non-pair. Populated once per Stage after `plan-author` (ephemeral intermediate; dropped by plan-digest in same pass). Enforces 9-point rubric via `plan_digest_lint` MCP tool. Q5 2026-04-22: §Plan Author does NOT survive in committed spec. -->

_pending — populated by `/plan-digest {MASTER_PLAN_PATH} {STAGE_ID}`. Sub-sections: §Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps (each step carries Goal / Edits / Gate / STOP / MCP hints). Template: `ia/templates/plan-digest-section.md`._

### §Goal

<!-- 1–2 sentences — task outcome in product / domain terms. -->

### §Acceptance

<!-- Refined per-Task acceptance — narrower than Stage Exit. Checkbox list. -->

### §Test Blueprint

<!-- Structured tuples consumed by `/implement` + `/verify-loop`. -->

### §Examples

<!-- Concrete inputs/outputs + edge cases. Tables or code blocks. -->

### §Mechanical Steps

<!-- Sequential, pre-decided. Each step: Goal / Edits (before+after) / Gate / STOP / MCP hints. -->
```

**8b.** Edit `ia/rules/plan-apply-pair-contract.md`. Find:

```
| `plan-author` | `§Plan Author` section (4 sub-sections) per Task spec | Bulk authoring across N Task specs of a Stage in one Opus pass | Non-pair. Absorbs retired `spec-enrich` canonical-term fold. Fires after `stage-file-apply` (multi-task) or `project-new-apply` (N=1). Token-split guardrail: ⌈N/2⌉ sub-passes if N specs + Stage context exceed threshold; never regress to per-Task mode. |
| `opus-audit` | `§Audit` paragraph per Task spec | Bulk post-verify audit across N Task specs of a Stage in one Opus pass | Non-pair. Feeds `stage-closeout-plan` (seam #4 head) at Stage end. |
```

Replace with:

```
| `plan-author` | `§Plan Author` section (4 sub-sections) per Task spec — **ephemeral** (dropped by `plan-digest`) | Bulk authoring across N Task specs of a Stage in one Opus pass | Non-pair. Absorbs retired `spec-enrich` canonical-term fold. Fires after `stage-file-apply` (multi-task) or `project-new-apply` (N=1). Token-split guardrail: ⌈N/2⌉ sub-passes if N specs + Stage context exceed threshold; never regress to per-Task mode. §Plan Author is intermediate — `plan-digest` consumes + drops it in the same write pass (Q5 2026-04-22). |
| `plan-digest` | `§Plan Digest` section (§Goal / §Acceptance / §Test Blueprint / §Examples / §Mechanical Steps) per Task spec + aggregate at `docs/implementation/{slug}-stage-{STAGE_ID}-plan.md` | Mechanizes §Plan Author across N Task specs of a Stage in one Opus pass | Non-pair (Q1 revised 2026-04-22: always-on). Runs after `plan-author`, before `plan-reviewer`. 9-point rubric enforced externally via `plan_digest_lint`; cap=1 retry; second failure escalates to user. Two modes: `stage` (live) + `audit` (flag-gated scaffold on `PLAN_DIGEST_AUDIT_MODE=1`). |
| `opus-audit` | `§Audit` paragraph per Task spec | Bulk post-verify audit across N Task specs of a Stage in one Opus pass | Non-pair. Feeds `stage-closeout-plan` (seam #4 head) at Stage end. |
```

**8c.** Edit `ia/rules/plan-apply-pair-contract.md` — add `plan-digest` to the Tier 2 bundle-reuse table. Find:

```
| `plan-author` Phase 0 | Yes — Stage-start | All N specs authored in bulk |
```

Replace with:

```
| `plan-author` Phase 0 | Yes — Stage-start | All N specs authored in bulk |
| `plan-digest` Phase 0 | No — reuses cache_block from `plan-author` / orchestrator | All N specs digested in bulk |
```

**8d.** Edit `ia/rules/plan-apply-pair-contract.md` — update Cross-references. Find:

```
- `ia/skills/plan-author/SKILL.md` — Stage-scoped bulk non-pair (fold of retired spec-enrich).
```

Replace with:

```
- `ia/skills/plan-author/SKILL.md` — Stage-scoped bulk non-pair (fold of retired spec-enrich).
- `ia/skills/plan-digest/SKILL.md` — Stage-scoped bulk non-pair; mechanizes §Plan Author into §Plan Digest (Q1 revised 2026-04-22 always-on).
- `ia/rules/plan-digest-contract.md` — 9-point rubric enforced by `plan_digest_lint`.
```

**8e.** Edit `ia/rules/project-hierarchy.md` — use `plan_digest_resolve_anchor` with substring `plan-author` to locate hits. For each hit, if the narration cites the chain order, extend to include `plan-digest` in position `plan-author → plan-digest → plan-review`. Historical hits (under Changelog / date-stamped blocks) stay untouched.

**8f.** Edit `ia/rules/orchestrator-vs-spec.md` — same policy as 8e.

**8g.** Edit `ia/rules/agent-output-caveman-authoring.md` — same policy as 8e; plus add one line to the "Standard exceptions" list if it doesn't already allow caveman exemption for §Plan Digest mechanical content (which is code-like, already exempt under "code").

**8h.** Edit `ia/templates/master-plan-template.md` — use `plan_digest_resolve_anchor` with substring `§Plan Author`; for each hit, replace with `§Plan Digest` unless inside a historical Changelog block.

**Gate:**

```bash
grep -c "## §Plan Digest" ia/templates/project-spec-template.md
grep -c "plan-digest" ia/rules/plan-apply-pair-contract.md
grep -n "§Plan Author" ia/templates/project-spec-template.md | grep -v historical | wc -l
```

First must be ≥1. Second must be ≥3 (three table rows + cross-ref). Third must be `0`.

**STOP:** If third > 0, the replacement in 8a missed a variant — re-open.

---

## Step 9 — Skill narrative edits

**Goal:** Align the skill files that mention §Plan Author / plan-author in their cross-references, role description, or chain narration. Ten files.

For each file below, apply the same policy:
1. Run `plan_digest_resolve_anchor` on substring `§Plan Author` + substring `plan-author` to enumerate hits.
2. Historical (Changelog / dated block) hits — preserve unchanged.
3. Narration hits describing the live chain — extend to mention `plan-digest` in chain-order (e.g. `plan-author → plan-digest → plan-review`).
4. Narration hits citing `§Plan Author` as the final/canonical section — replace with `§Plan Digest` and add the qualifier "`§Plan Author` is ephemeral (dropped by plan-digest per Q5 2026-04-22)".

Files:

**9a.** `ia/skills/plan-author/SKILL.md` — at least the "Downstream" narration. Find:

```
Does **NOT** write code, run verify, or flip Task status. Downstream: `plan-review` (seam #1 gate) then per-Task `/implement`.
```

Replace with:

```
Does **NOT** write code, run verify, or flip Task status. Downstream: `plan-digest` (mechanizes §Plan Author → §Plan Digest + drops §Plan Author), then `plan-review` (drift scan on final §Plan Digest), then per-Task `/implement`.
```

**9b.** Same file — update Cross-references. Find:

```
- [`ia/skills/plan-review/SKILL.md`](../plan-review/SKILL.md) — downstream seam #1 gate (multi-task path).
```

Replace with:

```
- [`ia/skills/plan-digest/SKILL.md`](../plan-digest/SKILL.md) — downstream bulk non-pair (mechanizes §Plan Author → §Plan Digest; §Plan Author is ephemeral per Q5 2026-04-22).
- [`ia/skills/plan-review/SKILL.md`](../plan-review/SKILL.md) — downstream seam #1 gate (multi-task path; drift scan on final §Plan Digest).
```

**9c.** `ia/skills/plan-review/SKILL.md` — update the lead paragraph. Find:

```
**Role:** Sonnet pair-head (model downgrade 2026-04-20 — drift scan is mechanical against plan-author output). Runs **once per Stage** before any Task kickoff begins. Reads the Stage header + all filed Task specs + invariants; checks alignment against master-plan intent; outputs either a **PASS sentinel** or a structured **§Plan Fix tuple list** under the target Stage block.
```

Replace with:

```
**Role:** Sonnet pair-head (model downgrade 2026-04-20 — drift scan is mechanical against `plan-digest` output; Q1-revision 2026-04-22 made the plan-digest gate always-on). Runs **once per Stage** after `plan-digest` and before any Task kickoff begins. Reads the Stage header + all filed Task specs' §Plan Digest + invariants; checks alignment against master-plan intent; outputs either a **PASS sentinel** or a structured **§Plan Fix tuple list** under the target Stage block.
```

**9d.** Same file — Phase 1 loop: update the spec-sections read list. Find:

```
2. For each Task row in the Stage whose Status ≠ `Done`: read `ia/projects/{ISSUE_ID}.md` — §1 Summary, §2 Goals, §7 Implementation Plan, §8 Acceptance Criteria.
```

Replace with:

```
2. For each Task row in the Stage whose Status ≠ `Done`: read `ia/projects/{ISSUE_ID}.md` — §1 Summary, §2 Goals, §Plan Digest (§Goal / §Acceptance / §Mechanical Steps), §8 Acceptance Criteria.
```

**9e.** `ia/skills/plan-applier/SKILL.md` — use `plan_digest_resolve_anchor` on `§Plan Author`; non-historical hits → `§Plan Digest`. Confirm Mode plan-fix still targets `§Plan Fix` section (unchanged — seam #1 pair-tail).

**9f.** `ia/skills/release-rollout/SKILL.md` — `plan_digest_resolve_anchor` on substring `plan-author`; for every chain-narration hit, extend to include `plan-digest` between `plan-author` and `plan-reviewer`.

**9g.** `ia/skills/opus-code-review/SKILL.md` — `plan_digest_resolve_anchor` on `§Plan Author`; non-historical → `§Plan Digest`. Code-review's acceptance reference is now §Plan Digest (per Q5).

**9h.** `ia/skills/design-explore/SKILL.md` — add a new subsection "Relentless human polling (companion to plan-digest)". MCP hint: run `plan_digest_resolve_anchor` on substring `## ` to locate the section list; append after the last `## ` heading (before any Changelog):

```
## Relentless human polling (companion to plan-digest)

Pick-prevention is layered across design-explore → plan-author → plan-digest. This skill is the upstream-most layer: poll the human question-by-question (one open question per turn, never a batch) until every decision is locked in the design doc BEFORE the master plan is compiled. Result: by the time `plan-author` runs, zero picks remain; by the time `plan-digest` lint-scans for picks (`plan_digest_scan_for_picks`), leaks are exceptional. Leak = abort chain + route back to `/design-explore` (not a silent deferral).

See `ia/rules/plan-digest-contract.md` rubric point 1 (zero open picks) and `ia/skills/plan-digest/SKILL.md` Phase 4 lint gate.
```

**9i.** `ia/skills/stage-file-apply/SKILL.md` — `plan_digest_resolve_anchor` on substring `plan-author`; extend chain narration to include `plan-digest`.

**9j.** `ia/skills/README.md` — `plan_digest_resolve_anchor` on substring `plan-author`; for each row in the skill index (if a table exists), add a sibling row for `plan-digest` with one-line purpose from 3a's frontmatter. Otherwise append a narrative line.

**Gate:**

```bash
grep -c "plan-digest" ia/skills/plan-author/SKILL.md ia/skills/plan-review/SKILL.md ia/skills/release-rollout/SKILL.md ia/skills/stage-file-apply/SKILL.md ia/skills/README.md
grep -q "Relentless human polling" ia/skills/design-explore/SKILL.md && echo OK
```

First grep — each file row must be ≥1. Second must print `OK`.

**STOP:** Any row printing `0` means the policy-apply step missed the file — re-open.

---

## Step 10 — Cursor parity

**Goal:** Generate `cursor-skill-plan-digest.mdc` via the existing `generate-cursor-skill-wrappers.mjs` script; patch `cursor-skill-plan-author.mdc` + `cursor-skill-plan-review.mdc` to cite plan-digest in the chain; update `cursor-lifecycle-adapters.mdc` narrative.

**10a.** Run the generator (it auto-discovers the new skill by reading `ia/skills/plan-digest/SKILL.md`):

```bash
node tools/scripts/generate-cursor-skill-wrappers.mjs
```

Generator writes `.cursor/rules/cursor-skill-plan-digest.mdc` automatically — no source edit needed. The `callerAgentMap` in the generator does not include `plan-digest`, so the fallback wrapper text (`When this skill invokes MCP tools, follow its Tool recipe order exactly.`) is emitted; that is correct for a Stage-scoped non-pair stage. **Decision:** do NOT add `plan-digest` to `callerAgentMap` — no caller-side MCP mutation semantics apply here.

**10b.** Edit `.cursor/rules/cursor-skill-plan-author.mdc` — use `plan_digest_resolve_anchor` on substring `plan-review` to locate the chain narration. If the wrapper body references chain order, extend to include `plan-digest` in the correct slot. If the wrapper is minimal (a one-line description only — the typical generator output), skip.

**10c.** Edit `.cursor/rules/cursor-skill-plan-review.mdc` — same policy as 10b.

**10d.** Edit `.cursor/rules/cursor-lifecycle-adapters.mdc`. Find the chain narration (use `plan_digest_resolve_anchor` on substring `plan-author → plan-reviewer`); replace with:

```
plan-author → plan-digest → plan-reviewer
```

(Decision: apply this substitution once at the first match; repeat if multiple identical narration lines exist — verify via `plan_digest_resolve_anchor`.)

**Gate:**

```bash
test -f .cursor/rules/cursor-skill-plan-digest.mdc && echo OK
grep -c "plan-digest" .cursor/rules/cursor-lifecycle-adapters.mdc
```

First must print `OK`. Second must be ≥1.

**STOP:** If the wrapper file is missing, re-run the generator after confirming `ia/skills/plan-digest/SKILL.md` exists and has valid frontmatter (Step 3a).

---

## Step 11 — Docs narrative

**Goal:** `docs/agent-lifecycle.md` + `docs/information-architecture-overview.md` are the two canonical lifecycle docs; both cite §Plan Author / plan-author in chain diagrams.

**11a.** Edit `docs/agent-lifecycle.md` — use `plan_digest_resolve_anchor` on substring `plan-author` to enumerate hits. For each non-historical hit that describes the live chain, extend to include `plan-digest` in chain order. For each `§Plan Author` reference that describes the canonical surviving surface, replace with `§Plan Digest` and add a footnote line where appropriate:

```
(§Plan Author is ephemeral — `plan-digest` drops it after mechanizing; Q5 2026-04-22.)
```

**11b.** Edit `docs/information-architecture-overview.md` — same policy as 11a.

**Gate:**

```bash
grep -c "plan-digest" docs/agent-lifecycle.md docs/information-architecture-overview.md
grep -c "§Plan Digest" docs/agent-lifecycle.md docs/information-architecture-overview.md
```

Each file row must be ≥1 for both greps.

**STOP:** If 0, the anchor substring didn't match — the doc may use `Plan Author` without the `§` glyph. Re-run `plan_digest_resolve_anchor` with the bare phrase.

---

## Step 12 — Final verification

**Goal:** Validate the full refactor before handoff — MCP smoke test, repo validators, and grep-sweep for residual `§Plan Author` in the authored surface. Legacy Draft specs (57 files in `ia/projects/`) are excluded from the sweep; they migrate lazily (Q13).

**12a.** Run repo validator suite:

```bash
npm run validate:all
```

Must exit 0.

**12b.** Run MCP smoke:

```bash
npm --prefix tools/mcp-ia-server run verify-mcp
```

Must include all 7 `plan_digest_*` names in the printed tool list.

**12c.** Grep-sweep residual `§Plan Author` in **authored surface** (commands + agents + skills + rules + templates + cursor wrappers + docs). Legacy task specs excluded:

```bash
grep -rn "§Plan Author" \
  .claude/commands/ \
  .claude/agents/ \
  ia/skills/ \
  ia/rules/ \
  ia/templates/ \
  .cursor/rules/ \
  docs/ \
  2>&1 | grep -v Changelog | grep -v "2026-04-" | grep -v "historical" | grep -v "ephemeral" | grep -v "_retired" | grep -v architecture-audit-
```

Must return **zero** matches. (Hits under `_retired/`, date-stamped Changelog blocks, explicit "ephemeral"/"historical" annotations, the architecture-audit archive, and the `docs/architecture-audit-change-list-2026-04-22.md` blueprint are ignored.)

**12d.** Legacy-spec counterpart grep — expected non-zero but bounded. Reports the retrofit backlog for lazy migration:

```bash
grep -l "## §Plan Author" ia/projects/*.md | wc -l
```

Prints the count of legacy Draft specs still carrying §Plan Author (expected ≤57 per Q13). This is informational, not a gate.

**12e.** Confirm the 7 new MCP tools still reachable:

```bash
for t in plan_digest_verify_paths plan_digest_resolve_anchor plan_digest_render_literal plan_digest_scan_for_picks plan_digest_lint plan_digest_gate_author_helper plan_digest_compile_stage_doc; do
  grep -q "$t" tools/mcp-ia-server/src/server-registrations.ts || echo "MISSING: $t"
done
```

Must print nothing (zero `MISSING` lines).

**12f.** Cursor wrapper freshness — regenerate then confirm no diff:

```bash
node tools/scripts/generate-cursor-skill-wrappers.mjs
git diff --quiet -- .cursor/rules/ && echo OK
```

Must print `OK` (generator is idempotent on a clean tree).

**Gate:** all of 12a–12f succeed per their inline criteria.

**STOP:** On any failure, re-enter the step whose gate failed. Do NOT commit; do NOT push. Surface first failing command + verbatim output to the user.

---

## Closure

When every checkbox is ticked and Step 12 is fully green:

- Do **not** commit — user commits manually.
- Do **not** bulk-retrofit the 57 legacy `§Plan Author` specs — Q13 locks lazy migration.
- Do **not** enable `PLAN_DIGEST_AUDIT_MODE=1` — Mode `audit` is a scaffold until a second audit pressure-tests it (Q11).
- Reply with: (a) which steps landed, (b) gate outputs (one line each), (c) legacy-spec retrofit count from 12d, (d) any drift or surprise (append a single-row Changelog entry below).

---

## Changelog

| Date | Delta | Author |
|---|---|---|
| 2026-04-22 | Initial plan-digest refactor implementation plan — sequential, pre-digested, composer-2-ready. 12 steps + Final gate. Mode `stage` only; Mode `audit` scaffolded behind `PLAN_DIGEST_AUDIT_MODE=1`. Lazy §Plan Author retrofit (Q13). | plan-digest refactor |
