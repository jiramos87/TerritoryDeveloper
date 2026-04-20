---
title: "Post-Stage-1 telemetry sweep report"
generated_at: "2026-04-20"
stage: "Stage 1.3 — TECH-539"
status: "STUB — attribution deferred pending real sweep data"
depends_on:
  - "tools/scripts/agent-telemetry/baseline-summary.json"
  - "tools/scripts/agent-telemetry/baseline-summary-post-stage1.json"
---

# Post-Stage-1 telemetry sweep — per-theme attribution

## Status — STUB

TECH-538 post-stage sweep stubbed per user direction in `/ship-stage Stage 1.3` invocation. `baseline-summary-post-stage1.json` carries zero-valued metrics (schema-conformant placeholder). Per-theme attribution below is framework-only; real rows blocked on ≥10 human-driven representative lifecycle sessions (NB-10).

## Diff — pre vs post (stub)

| Metric | baseline p50 | post-stage1 p50 | delta | attribution (when real) |
|---|---|---|---|---|
| `total_input_tokens` | 251686 | 0 (stub) | — | B3 allowlist narrowing (non-pair-seam) + B1 split (IA-core vs bridge) |
| `cache_read_tokens` | 132492 | 0 (stub) | — | B1 split (smaller per-session cache footprint) |
| `cache_write_tokens` | 22075 | 0 (stub) | — | B3 (tool schema shrink) |
| `mcp_cold_start_ms` | 1401 | 0 (stub) | — | B1 split (IA-core cold boot time) |
| `hook_fork_count` | 11 | 0 (stub) | — | B7 harness overhead (new PostToolUse) |
| `hook_fork_total_ms` | 831 | 0 (stub) | — | B7 harness overhead |

## Per-theme attribution framework

- **B1 (server split)** — primary signal: `cache_read_tokens` + `mcp_cold_start_ms` drop for IA-core-only lifecycle seams (author / plan-review / closeout). Confirming row = IA-core-only session shows ≥X% cold-start reduction vs pre-split baseline. A/B via `MCP_SPLIT_SERVERS=0` vs `=1` per NB-9.
- **B3 (allowlist narrowing)** — primary signal: `total_input_tokens` + `cache_write_tokens` drop for the 7 narrowed agents (verifier, spec-implementer, stage-decompose, project-new-planner, project-new-applier, design-explore, test-mode-loop). Tool schema payload at spawn time.
- **B7 (harness overhead)** — primary signal: `hook_fork_count` unchanged or +1 (PostToolUse fires once per tool call); `hook_fork_total_ms` delta = observed overhead budget. Target: ≤5% increase; investigate if ≥10%.

## Flag-flip decision — DEFERRED

`MCP_SPLIT_SERVERS` default **remains `0`** in `.mcp.json`. Per NB-6 resolution, flip to `1` is gated on B1 attribution row showing expected IA-core session token reduction. Stub data cannot validate — deferred to follow-up issue.

## Follow-up

File a successor TECH issue when ready to execute real sweep: run ≥10 representative lifecycle sessions (or synthesize A/B pairs per NB-9), re-run `tools/scripts/agent-telemetry/aggregate-baseline.mjs` over real JSONL, replace `baseline-summary-post-stage1.json`, re-run this diff, then flip `MCP_SPLIT_SERVERS` default.
