---
purpose: "TECH-511 — Telemetry schema validator."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T1.1.2"
---
# TECH-511 — Telemetry schema validator

> **Issue:** [TECH-511](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Ship JSONL schema validator script reachable via `npm run validate:telemetry-schema`.
Asserts 8-field shape authored by TECH-510. Becomes a validate:all dependency so CI fails
on malformed baseline captures.

## 2. Goals and Non-Goals

### 2.1 Goals

1. validate:telemetry-schema script added to package.json.
2. Script iterates .claude/telemetry/*.jsonl, asserts each line parses + carries 8 typed fields.
3. Wired into validate:all chain.
4. Non-zero exit on missing field / type mismatch; zero exit on empty dir (graceful).

### 2.2 Non-Goals (Out of Scope)

1. Aggregation / percentile computation — handled by TECH-512.
2. Runtime telemetry collection — handled by TECH-510.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run validate:telemetry-schema to catch malformed JSONL captures early | Script exits non-zero on schema mismatch; zero on empty dir |

## 4. Current State

### 4.1 Domain behavior

No JSONL schema validation exists. Malformed captures would silently corrupt baseline aggregation.

### 4.2 Systems map

New: tools/scripts/validate-telemetry-schema.{sh|mjs}.
Touches: package.json (scripts section).
Reads: .claude/telemetry/*.jsonl.
No Unity / C# / runtime surface touched.

## 5. Proposed Design

### 5.1 Target behavior (product)

Validator iterates all JSONL files in .claude/telemetry/; asserts each line has 8 required
typed fields; exits non-zero on any violation; exits 0 on empty dir.

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 1 — Validator + wiring.
1. Author validator script (bash with jq, or Node mjs) — 8-field assertion.
2. Add "validate:telemetry-schema" to package.json scripts.
3. Append to validate:all composition.
4. Smoke: run on empty + synthetic JSONL.
5. npm run validate:all.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | Wire into validate:all | Prevents silent schema drift across sessions | Advisory-only lint |

## 7. Implementation Plan

### Phase 1 — Validator + wiring

- [x] Author validate-telemetry-schema script (bash+jq or Node mjs)
- [x] Add "validate:telemetry-schema" entry to package.json scripts
- [x] Append to validate:all composition in root package.json
- [x] Smoke test: empty dir (exit 0) + synthetic JSONL (exit 0 good, exit 1 bad)
- [x] npm run validate:all

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Validator + validate:all chain | Node | `npm run validate:all` | Tooling only; no Unity surface |

## 8. Acceptance Criteria

- [x] validate:telemetry-schema script added to package.json.
- [x] Script iterates .claude/telemetry/*.jsonl, asserts each line parses + carries 8 typed fields.
- [x] Wired into validate:all chain.
- [x] Non-zero exit on missing field / type mismatch; zero exit on empty dir (graceful).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

Risks:
- Empty-dir case MUST exit 0 — first-time run before TECH-510 ever fires leaves `.claude/telemetry/` empty or absent. Validator treats both `no dir` + `dir with zero .jsonl` as success.
- Dependency drift — if bash+jq path chosen, `jq` presence on CI matters. Preferred: Node `.mjs` (already required by `validate:all`); avoids adding CI apt install.
- Schema divergence from TECH-510 — both tasks must agree on 8 field names + types. Keep authoritative list in one place (JSDoc / const array at top of validator); TECH-510 copies comment reference.
- `validate:all` chain ordering — append telemetry validator AFTER `validate:dead-project-specs` (fast fail on doc drift first), BEFORE any slow Unity-adjacent step.
- Partial-write JSONL — if a PostToolUse hook crashed mid-append, last line could be truncated. Validator must report line number + which field parse failed, not silent skip.
- False-positive on whitespace-only blank trailing line — skip empty lines silently; only non-empty lines enter schema check.

Surfaces to watch:
- `package.json` `scripts` — `validate:telemetry-schema` new entry + appended to `validate:all` composition.
- `tools/scripts/validate-telemetry-schema.mjs` (or `.sh`) — new file.
- CI — `.github/workflows/**` inherits via `npm run validate:all` root alias; no workflow edit expected.

Edge cases:
- Unicode / newline inside a JSONL value — JSON.parse handles; validator must read line-by-line with strict `\n` split, not whitespace tokenization.
- Very large file (>100k rows) — streaming `readline` over `fs.createReadStream` preferred over `fs.readFileSync` to avoid heap spike.
- Numeric fields present but zero — must pass (NOT fail as "missing").

### §Examples

package.json diff (additive):
```
  "scripts": {
    ...
+   "validate:telemetry-schema": "node tools/scripts/validate-telemetry-schema.mjs",
-   "validate:all": "npm run validate:frontmatter && npm run validate:dead-project-specs && npm run validate:backlog"
+   "validate:all": "npm run validate:frontmatter && npm run validate:dead-project-specs && npm run validate:backlog && npm run validate:telemetry-schema"
  }
```

Invocation — empty dir (first run):
```
$ rm -rf .claude/telemetry; npm run validate:telemetry-schema
> node tools/scripts/validate-telemetry-schema.mjs
telemetry-schema: 0 files, 0 rows — ok (empty)
$ echo $?
0
```

Invocation — valid JSONL:
```
$ npm run validate:telemetry-schema
telemetry-schema: 2 files, 47 rows — ok
$ echo $?
0
```

Invocation — schema violation:
```
$ echo '{"ts":1,"session_id":"x"}' >> .claude/telemetry/bad.jsonl
$ npm run validate:telemetry-schema
telemetry-schema: .claude/telemetry/bad.jsonl:48 — missing fields: total_input_tokens, cache_read_tokens, cache_write_tokens, mcp_cold_start_ms, hook_fork_count, hook_fork_total_ms
$ echo $?
1
```

### §Test Blueprint

`npm run validate:all` coverage (tooling-only per MEMORY `feedback_refactor_tooling_only_verify`):
- Chain now includes `validate:telemetry-schema` as terminal step.
- `validate:frontmatter` — TECH-511 spec frontmatter intact.
- `validate:dead-project-specs` — TECH-511 yaml + row filed; passes.
- `validate:backlog` — BACKLOG row clean.

Smoke scenarios (exercised in Phase 1 step 4):
1. Empty state: remove `.claude/telemetry/` → `npm run validate:telemetry-schema` exit 0.
2. Zero-row dir: `mkdir -p .claude/telemetry; touch .claude/telemetry/empty.jsonl` → exit 0.
3. Happy path: populate via TECH-510 baseline-collect.sh (2 rows) → exit 0.
4. Missing-field violation: append row lacking `hook_fork_total_ms` → exit 1, error names file + line + missing field list.
5. Type violation: append row with `total_input_tokens: "abc"` → exit 1 with type mismatch diagnostic.
6. Malformed JSON (truncated): append `{"ts":1,`  → exit 1 with parse error + line number.
7. `npm run validate:all` end-to-end → exit 0 given clean inputs.

No Unity surface; `unity:compile-check` / `testmode-batch` skipped.

### §Acceptance

Maps 1:1 to §8:
- [ ] §8.1 script entry in package.json → `jq -r '.scripts."validate:telemetry-schema"' package.json` returns non-null.
- [ ] §8.2 8-field typed assertion → violation smoke (missing field) returns non-zero with field list.
- [ ] §8.3 wired into validate:all → `jq -r '.scripts."validate:all"' package.json` contains `validate:telemetry-schema` substring; chain run exits 0.
- [ ] §8.4 graceful empty-dir → smoke scenarios 1 + 2 both exit 0.
- [ ] `npm run validate:all` exit 0 post-wire.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

Shipped `tools/scripts/validate-telemetry-schema.mjs` — streaming readline validator over `.claude/telemetry/*.jsonl` with an explicit 8-field `REQUIRED_FIELDS` table (ts ≥ 1e12 epoch-ms guard, non-empty `session_id` string, six counters typed `number && Number.isFinite && >= 0`). Wired as terminal step of `validate:all` in root `package.json`; dir-absent and zero-jsonl branches both exit 0, blank trailing lines skipped silently, zero-valued counters pass. Worked well: Node `.mjs` choice (over bash+jq) sidestepped CI apt-install drift per §Audit Notes; file:line diagnostics on JSON.parse + field errors keep partial-write corruption debuggable; streaming readline avoids heap spike on future large captures. `Number.isFinite` guard rejects NaN/Infinity cleanly; array-as-row case correctly flags all fields missing. Code review returned PASS with no tail. Watch: (a) schema table is duplicated in spirit across TECH-510 (producer) + TECH-511 (consumer) — any 9th field or type tightening must touch both scripts atomically, else silent drift; (b) `ts ≥ 1e12` lower bound is permissive — accepts any ms epoch from year 2001 onward but will NOT catch seconds-scale 10-digit regressions under 1e12 if TECH-510's fallback chain ever breaks (tight upper bound would add safety); (c) pre-existing glossary-freshness test flake in `validate:all` is unrelated to this task but will mask true telemetry-schema regressions if it ever fails first in the chain — consider reorder so `validate:telemetry-schema` runs before flaky steps; (d) ordering inside `validate:all` landed terminal (after `validate:web`) rather than "before slow Unity-adjacent step" as Plan §Audit Notes suggested — benign today (no Unity step in chain yet) but revisit if chain expands.

## §Code Review

**Verdict:** PASS

**Diff summary:**
- `tools/scripts/validate-telemetry-schema.mjs` (new) — streaming readline validator over `.claude/telemetry/*.jsonl`; 8-field schema table with per-field type + range check; line-level error diagnostics.
- `package.json` — `validate:telemetry-schema` script added; appended as terminal step of `validate:all` chain.

**Acceptance criteria:**
- §8.1 script entry present → ok (`node tools/scripts/validate-telemetry-schema.mjs`).
- §8.2 8-field assertion → REQUIRED_FIELDS table matches TECH-510 shape exactly (ts 13-digit epoch ms via `>= 1e12`, session_id non-empty string, six counter fields `number >= 0 && Number.isFinite`). Missing-field + type-mismatch reported with file:line + field list.
- §8.3 wired into validate:all → appended after `validate:web` as terminal step.
- §8.4 graceful empty-dir → dir-absent branch exits 0; zero-jsonl branch exits 0; blank trailing lines skipped silently; zero-value counters pass (`>= 0` inclusive).

**Invariants:** no violations — tooling-only surface; no Unity/grid/road/HeightMap touched.

**Glossary:** canonical — "telemetry" / "validate:all chain" usage matches docs.

**Edge cases covered:**
- `Number.isFinite` guard rejects NaN/Infinity masquerading as numbers.
- Array rejected (non-object) → all fields reported missing (correct).
- `filePath:line` diagnostic for parse errors (JSON.parse catch) preserves debuggability on partial-write corruption.
- Streaming readline (not `readFileSync`) — heap-safe for large baseline captures per §Audit Notes risk.

**No tail triggered.** Return to caller.

## §Code Fix Plan

_pending — populated by `/code-review` only when fixes needed. Sonnet `code-fix-apply` reads tuples + applies + re-enters `/verify-loop`._
