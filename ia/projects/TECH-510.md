---
purpose: "TECH-510 — Baseline collect script."
audience: both
loaded_by: ondemand
slices_via: none
parent_plan: "ia/projects/session-token-latency-master-plan.md"
task_key: "T1.1.1"
---
# TECH-510 — Baseline collect script

> **Issue:** [TECH-510](../../BACKLOG.md)
> **Status:** Draft
> **Created:** 2026-04-19
> **Last updated:** 2026-04-19

## 1. Summary

Author baseline-collect.sh shell harness emitting session-scoped telemetry JSONL under
.claude/telemetry/. Establishes capture format for all six baseline metrics gating
Stage 1.2 entry. No per-theme attribution — aggregate floor only.

## 2. Goals and Non-Goals

### 2.1 Goals

1. New dir tools/scripts/agent-telemetry/ with baseline-collect.sh (executable).
2. JSONL schema: 8 fields, one line per measurement event.
3. .gitignore tracks summary JSON, excludes raw .jsonl.
4. Script runs clean against a dummy session (no hook env → zero-row append, exit 0).

### 2.2 Non-Goals (Out of Scope)

1. Per-theme attribution — aggregate floor only at this stage.
2. Aggregation / percentile computation — handled by T1.1.3.

## 3. User / Developer Stories

| # | Role | Story | Acceptance criteria |
|---|------|-------|---------------------|
| 1 | Developer | Run baseline-collect.sh in any session so telemetry JSONL is captured | Script exits 0; .jsonl appended with 8-field record |

## 4. Current State

### 4.1 Domain behavior

No telemetry collection exists. No baseline metrics available for Stage 1.2 gating.

### 4.2 Systems map

New: tools/scripts/agent-telemetry/baseline-collect.sh.
Touches: .gitignore (root).
Reads: DEBUG_MCP_COMPUTE stderr, PostToolUse hook stdout (env-passed).
Writes: .claude/telemetry/{session-id}.jsonl (new dir, gitignored).
No Unity / C# / runtime surface touched.

### 4.3 Implementation investigation notes (optional)

8-field JSONL schema: ts, session_id, total_input_tokens, cache_read_tokens, cache_write_tokens,
mcp_cold_start_ms, hook_fork_count, hook_fork_total_ms.

## 5. Proposed Design

### 5.1 Target behavior (product)

Script parses hook env, computes per-session metrics, appends one JSONL line per invocation.
Missing env vars → zero values (graceful, exit 0).

### 5.2 Architecture / implementation (agent-owned unless fixed by design)

Phase 1 — Script + gitignore.
1. mkdir tools/scripts/agent-telemetry.
2. Author baseline-collect.sh: parse hook env, compute args per 8-field schema, append JSONL.
3. chmod +x; smoke test with synthetic session-id.
4. Edit .gitignore: add `.claude/telemetry/*.jsonl`; confirm *-summary.json not shadowed.
5. npm run validate:all.

## 6. Decision Log

| Date | Decision | Rationale | Alternatives considered |
|------|----------|-----------|------------------------|
| 2026-04-19 | 8-field JSONL schema locked by Stage 1.1 plan | Matches T1.1.2 validator + T1.1.3 aggregator requirements | N/A |

## 7. Implementation Plan

### Phase 1 — Script + gitignore

- [x] mkdir tools/scripts/agent-telemetry; author baseline-collect.sh
- [x] chmod +x; smoke test with synthetic session-id
- [x] Edit .gitignore: `.claude/telemetry/*.jsonl` excluded, `*-summary.json` tracked
- [x] npm run validate:all

## 7b. Test Contracts

| Acceptance / goal | Check type | Command or artifact | Notes |
|-------------------|------------|---------------------|-------|
| Script + gitignore + validate:all | Node | `npm run validate:all` | Tooling only; no Unity surface |

## 8. Acceptance Criteria

- [x] New dir tools/scripts/agent-telemetry/ with baseline-collect.sh (executable).
- [x] JSONL schema: 8 fields, one line per measurement event.
- [x] .gitignore tracks summary JSON, excludes raw .jsonl.
- [x] Script runs clean against a dummy session (no hook env → zero-row append, exit 0).

## 9. Issues Found During Development

| # | Description | Root cause | Resolution |
|---|-------------|------------|------------|

## 10. Lessons Learned

- …

## §Plan Author

### §Audit Notes

Risks:
- Missing hook env vars (first-run, no PostToolUse yet) must NOT crash — script sets zero defaults, still appends one row, exits 0.
- Session id collision across parallel sessions — use `$CLAUDE_SESSION_ID` if present, else fallback `$(date +%s)-$$` (pid suffix) to keep JSONL filenames unique.
- `.gitignore` glob ordering matters — `.claude/telemetry/*.jsonl` exclude must NOT shadow `*-summary.json` sibling (T1.1.3 output). Verify via `git check-ignore -v` on both sample paths post-edit.
- JSONL append race — multiple PostToolUse invocations in same session run concurrently. Use `>>` (O_APPEND atomic on POSIX for <4KB lines) — 8-field row well under threshold.
- Numeric field coercion — if hook env carries empty string, emit `0` (integer), never `""` or `null`; TECH-511 validator asserts typed integers.

Surfaces to watch:
- `tools/scripts/agent-telemetry/` — new dir, executable bit required (`chmod +x`).
- `.gitignore` root — telemetry glob rules.
- Bash denylist in `.claude/settings.json` — `rm -rf` of `.claude/**` blocked; safe for script's own writes since append-only.

Edge cases:
- Empty session (no MCP call, no hook fire) — script invoked manually → emits zero-row baseline.
- Clock skew / non-monotonic `ts` — use `date -u +%s%3N` (ms epoch UTC) to avoid locale drift.
- Large `total_input_tokens` (>2^31) — bash integer math overflow risk on 32-bit; treat as string in JSONL, validator accepts string-or-number.

### §Examples

Invocation (synthetic session, no hook env):
```
CLAUDE_SESSION_ID=smoke-001 bash tools/scripts/agent-telemetry/baseline-collect.sh
cat .claude/telemetry/smoke-001.jsonl
```

Expected JSONL row (zero-env baseline):
```
{"ts":1745020800123,"session_id":"smoke-001","total_input_tokens":0,"cache_read_tokens":0,"cache_write_tokens":0,"mcp_cold_start_ms":0,"hook_fork_count":0,"hook_fork_total_ms":0}
```

Invocation (env populated by PostToolUse hook):
```
CLAUDE_SESSION_ID=real-session \
TOTAL_INPUT_TOKENS=142301 CACHE_READ_TOKENS=98120 CACHE_WRITE_TOKENS=4410 \
MCP_COLD_START_MS=287 HOOK_FORK_COUNT=14 HOOK_FORK_TOTAL_MS=92 \
bash tools/scripts/agent-telemetry/baseline-collect.sh
```

`.gitignore` diff (additive):
```
+ # agent telemetry — raw captures excluded, summaries tracked
+ .claude/telemetry/*.jsonl
+ !.claude/telemetry/*-summary.json
```

### §Test Blueprint

`npm run validate:all` coverage (tooling-only fast-path per `ia/skills/verify-loop/SKILL.md` `--tooling-only` + `ia/projects/lifecycle-refactor-master-plan.md`):
- `validate:frontmatter` — no spec frontmatter touched; passes untouched.
- `validate:dead-project-specs` — TECH-510 yaml + row exist; passes.
- `validate:backlog` — BACKLOG row references TECH-510 correctly.
- Shellcheck (if wired) — script must lint clean.

Smoke scenarios (manual, pre-closeout):
1. Zero-env run: `unset CLAUDE_SESSION_ID; bash baseline-collect.sh` → exit 0, JSONL row with all-zero numerics.
2. Populated-env run: export 6 metric vars → row reflects values, types stay integer.
3. Repeat invocation same session → second JSONL line appended (no overwrite).
4. `.gitignore` check: `git check-ignore -v .claude/telemetry/foo.jsonl` matches rule; `git check-ignore -v .claude/telemetry/daily-summary.json` does NOT match.
5. Downstream handshake: run TECH-511 validator against produced JSONL → exit 0.

No Unity `unity:compile-check` / `testmode-batch` needed — no C# surface.

### §Acceptance

Maps 1:1 to §8:
- [ ] §8.1 new dir + executable script → `ls -l tools/scripts/agent-telemetry/baseline-collect.sh` shows `-rwxr-xr-x`.
- [ ] §8.2 8-field JSONL → sample row piped through `jq 'keys | length'` returns `8`; keys match ordered spec.
- [ ] §8.3 gitignore rules → `git check-ignore -v` passes on both `.jsonl` (ignored) + `*-summary.json` (tracked).
- [ ] §8.4 graceful empty-env → `env -i bash baseline-collect.sh; echo $?` returns `0` with zero-valued row appended.
- [ ] `npm run validate:all` exit 0.

## Open Questions (resolve before / during implementation)

None — tooling only; see §8 Acceptance criteria.

---

## §Audit

Shipped `tools/scripts/agent-telemetry/baseline-collect.sh` (executable, ~70 lines) plus `.gitignore` rules excluding raw `.claude/telemetry/*.jsonl` while keeping `*-summary.json` tracked. Script parses PostToolUse hook env, computes 8-field row (`ts`, `session_id`, three token counters, `mcp_cold_start_ms`, two hook-fork counters), appends one JSONL line per invocation; empty-env path emits zero-valued row and exits 0. Worked well: Plan §Audit Notes anticipated the atomic `>>` append, numeric coercion, and gitignore shadow rules — all held under smoke. Acceptance mapping was 1:1 with §8; verify-loop tooling-only fast-path (`npm run validate:all`) closed green. Watch: initial implementation hit the BSD `date` trap flagged in §Audit Notes — macOS lacks `%3N`, producing `1776648453N` literal plus a compounding `000` suffix (14-digit bad ts on macOS, 16-digit on GNU). Code review caught it critical; `code-fix-apply` installed a `python3` → `perl Time::HiRes` → `s*1000` fallback chain that yields canonical 13-digit epoch ms cross-platform (smoke confirmed `ts=1776648568760`). Follow-on watch items: (a) shellcheck is NOT yet wired — if added later, re-lint this script; (b) session-id collision fallback `$(date +%s)-$$` untested under truly concurrent Claude sessions; (c) `total_input_tokens` string-vs-number coercion relies on TECH-511 validator accepting Number.isFinite — any future schema tightening must update both scripts in lockstep; (d) bash integer overflow at `>2^31` tokens still theoretical, not exercised.

## §Code Review

**Verdict:** critical — 1 finding.

**Diff summary:** new `tools/scripts/agent-telemetry/baseline-collect.sh` (executable, 70 lines), `.gitignore` +2 lines telemetry section (`.claude/telemetry/*.jsonl` excluded, `!.claude/telemetry/*-summary.json` tracked).

**Acceptance criteria audit:**
- §8.1 exec bit → PASS (`-rwxr-xr-x`).
- §8.2 8-field JSONL → PASS structurally (`jq 'keys|length'` = 8, ordered per spec).
- §8.3 gitignore rules → PASS (`git check-ignore -v` confirms both rules hit correctly).
- §8.4 empty-env exit 0 + zero-row append → PASS (row appended, `$?` = 0).

**Invariants:** §1–11 N/A (no Unity / C# / runtime surface). §12 project-spec under `ia/projects/` ✓. §13 id-counter untouched; yaml record present ✓.

**Critical finding — `ts` field is NOT valid epoch ms:**
- macOS BSD `date` does NOT support `%3N`. Command `date -u +%s%3N` exits 0 with literal output `1776648453N` (10-digit seconds + literal `3N`). Fallback branch (`|| date -u +%s`) never triggers.
- Unconditional `000` suffix concatenation applies in BOTH branches, compounding the bug: macOS produces 14-digit `17766484533000` (off by factor 10); GNU `date` (Linux) produces 16-digit `17766484533123000` (off by factor 1000 since `%3N` already gives ms).
- Strip-non-digit sanitizer hides the bad format but does not correct the magnitude. Observed live output confirmed: `{"ts":17766484533000,...}`.
- Breaks §8 spec intent ("epoch ms UTC") and will fail TECH-511 validator's typed-integer-as-ms assertion. §Audit Notes already flagged "use `date -u +%s%3N` (ms epoch UTC) to avoid locale drift" — implementation did not reconcile macOS incompatibility.

**Glossary:** no new domain terms introduced; tooling-only.

**§Code Fix Plan written below. Spawning `code-fix-apply TECH-510`.**

**Fix applied (2026-04-19):** `code-fix-apply` replaced BSD-broken timestamp block with python3/perl/s*1000 fallback chain. Smoke test `env -i CLAUDE_SESSION_ID=smoke-fix-001 bash ...` → `ts=1776648568760` (13-digit epoch ms). `npm run validate:all` exit 0. Critical finding resolved.

## §Code Fix Plan

```yaml
- operation: replace_range
  target_path: tools/scripts/agent-telemetry/baseline-collect.sh
  target_anchor: |
    # --- timestamp --------------------------------------------------------------
    # date -u +%s%3N: epoch ms UTC. Avoid locale drift.
    TS="$(date -u +%s%3N 2>/dev/null || date -u +%s)000"
    # Trim to pure digits (guard against platform quirks).
    TS="${TS//[^0-9]/}"
  payload: |
    # --- timestamp --------------------------------------------------------------
    # Epoch ms UTC. Cross-platform: GNU date supports %3N directly; BSD date
    # (macOS) lacks %3N, so fall back to python/perl, else seconds*1000.
    TS="$(date -u +%s%3N 2>/dev/null)"
    if [[ -z "$TS" || "$TS" == *N* ]]; then
      # macOS / BSD fallback — python3 gives true ms; final fallback is s*1000.
      TS="$(python3 -c 'import time; print(int(time.time()*1000))' 2>/dev/null \
        || perl -MTime::HiRes=time -e 'printf("%d\n", time()*1000)' 2>/dev/null \
        || echo $(( $(date -u +%s) * 1000 )))"
    fi
    # Trim to pure digits (guard against stray chars).
    TS="${TS//[^0-9]/}"
```

Handoff: Sonnet `code-fix-apply` reads tuple verbatim, applies via `replace_range` on exact anchor, re-enters `/verify-loop` to confirm `ts` is 13-digit epoch ms on macOS + Linux.
